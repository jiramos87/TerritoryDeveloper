-- 0108_seed_hud_bar_panel_v2.sql
--
-- hud-bar bake-test v2 — panel reseed + retire stale.
--
-- Brings the DB into shape with `docs/ui-element-definitions.md` §hud-bar
-- locked def (14 children: 11 buttons + 1 label + 2 readouts):
--
--   Left (3):    new, save, load
--   Center (3):  city-name-label, sim-date-readout, population-readout
--   Right (8):   zoom-in, zoom-out, budget, play-pause, speed-cycle,
--                stats, auto, map
--
-- Steps:
--   1. Insert 8 NEW button entities + 2 NEW token entities (readouts).
--   2. Insert button_detail rows for all 11 buttons in the locked def,
--      wiring sprite_icon_entity_id → 0107 sprite entities by slug.
--   3. Publish entity_version=1 for the new entities.
--   4. Bump panel hud-bar to a fresh version (v2 of the panel).
--   5. DELETE existing 19 panel_child rows + INSERT 14 fresh rows from
--      locked def, with bare-slug `params_json.icon` (matches 0107 sprite
--      slugs) so snapshot exporter can JOIN to populate sprite_ref.
--   6. Soft-retire 14 stale entities (set retired_at = now()) so the
--      snapshot exporter filter `retired_at IS NULL` excludes them.
--   7. Sanity assertions.
--
-- Idempotent: ON CONFLICT DO NOTHING / DELETE+INSERT panel_child for the
-- shape, retire UPDATE only flips NULL → now().

BEGIN;

-- ── 1. catalog_entity rows for 10 NEW children (8 buttons + 2 tokens) ───────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('button', 'hud-bar-new-button',          'New Game'),
  ('button', 'hud-bar-save-button',         'Save Game'),
  ('button', 'hud-bar-load-button',         'Load Game'),
  ('token',  'hud-bar-sim-date-readout',    'Sim Date Readout'),
  ('token',  'hud-bar-population-readout',  'Population Readout'),
  ('button', 'hud-bar-budget-button',       'Budget'),
  ('button', 'hud-bar-play-pause-button',   'Play / Pause'),
  ('button', 'hud-bar-speed-cycle-button',  'Speed Cycle'),
  ('button', 'hud-bar-stats-button',        'Stats Panel Toggle'),
  ('button', 'hud-bar-auto-button',         'AUTO Mode Toggle')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 2. button_detail for 11 buttons (8 new + 3 carry-over) ──────────────────

INSERT INTO button_detail (entity_id, sprite_icon_entity_id, size_variant, action_id)
SELECT btn.id, spr.id, 'md', m.action_id
FROM (VALUES
  ('hud-bar-new-button',          'new-game',  'action.game-new'),
  ('hud-bar-save-button',         'save-game', 'action.game-save'),
  ('hud-bar-load-button',         'load-game', 'action.game-load'),
  ('hud-bar-zoom-in-button',      'zoom-in',   'action.camera-zoom-in'),
  ('hud-bar-zoom-out-button',     'zoom-out',  'action.camera-zoom-out'),
  ('hud-bar-budget-button',       'long',      'action.budget-panel-toggle'),
  ('hud-bar-play-pause-button',   'pause',     'action.time-play-pause-toggle'),
  ('hud-bar-speed-cycle-button',  'empty',     'action.time-speed-cycle'),
  ('hud-bar-stats-button',        'stats',     'action.stats-panel-toggle'),
  ('hud-bar-auto-button',         'empty',     'action.auto-mode-toggle'),
  ('hud-bar-map-button',          'empty',     'action.map-panel-toggle')
) AS m(btn_slug, sprite_slug, action_id)
JOIN catalog_entity btn ON btn.kind = 'button' AND btn.slug = m.btn_slug
JOIN catalog_entity spr ON spr.kind = 'sprite' AND spr.slug = m.sprite_slug
ON CONFLICT (entity_id) DO UPDATE
  SET sprite_icon_entity_id = EXCLUDED.sprite_icon_entity_id,
      action_id             = EXCLUDED.action_id,
      updated_at            = now();

-- ── 3. entity_version + publish for 10 NEW children ─────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration": "0108_seed_hud_bar_panel_v2", "event": "initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.slug IN (
        'hud-bar-new-button','hud-bar-save-button','hud-bar-load-button',
        'hud-bar-sim-date-readout','hud-bar-population-readout',
        'hud-bar-budget-button','hud-bar-play-pause-button',
        'hud-bar-speed-cycle-button','hud-bar-stats-button','hud-bar-auto-button')
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.slug IN (
        'hud-bar-new-button','hud-bar-save-button','hud-bar-load-button',
        'hud-bar-sim-date-readout','hud-bar-population-readout',
        'hud-bar-budget-button','hud-bar-play-pause-button',
        'hud-bar-speed-cycle-button','hud-bar-stats-button','hud-bar-auto-button')
  AND ce.current_published_version_id IS NULL;

-- ── 4-5. Bump hud-bar panel version + reseed 14 panel_child rows ────────────

DO $$
DECLARE
  v_panel_id   bigint;
  v_ver_id     bigint;
  v_ver_number int;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE kind='panel' AND slug='hud-bar';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0108: hud-bar panel entity missing';
  END IF;

  SELECT COALESCE(MAX(version_number), 0) + 1 INTO v_ver_number
    FROM entity_version WHERE entity_id = v_panel_id;

  INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
  VALUES (
    v_panel_id, v_ver_number, 'published', '{}'::jsonb, '{}'::jsonb,
    '{"migration": "0108_seed_hud_bar_panel_v2", "event": "panel_v2_reseed"}'::jsonb
  )
  RETURNING id INTO v_ver_id;

  UPDATE catalog_entity SET current_published_version_id = v_ver_id
    WHERE id = v_panel_id;

  -- Wipe + reseed panel_child rows (14 from locked def).
  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  -- Left zone (ord 1-3): new, save, load
  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, instance_slug, params_json, layout_json)
  VALUES
    (v_panel_id, v_ver_id, 'main', 1, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-new-button'),
     'hud-bar-new-button',
     '{"kind":"illuminated-button","icon":"new-game","label":"NEW","action":"action.game-new"}'::jsonb,
     '{"zone":"left","ord":0}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 2, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-save-button'),
     'hud-bar-save-button',
     '{"kind":"illuminated-button","icon":"save-game","label":"SAVE","action":"action.game-save"}'::jsonb,
     '{"zone":"left","ord":1}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 3, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-load-button'),
     'hud-bar-load-button',
     '{"kind":"illuminated-button","icon":"load-game","label":"LOAD","action":"action.game-load"}'::jsonb,
     '{"zone":"left","ord":2}'::jsonb),

  -- Center zone (ord 4-6): city-name-label, sim-date-readout, population-readout
    (v_panel_id, v_ver_id, 'main', 4, 'label',
     (SELECT id FROM catalog_entity WHERE kind='token' AND slug='hud-bar-city-name-label'),
     'hud-bar-city-name-label',
     '{"kind":"label","bind":"cityStats.cityName","font":"display","align":"center"}'::jsonb,
     '{"zone":"center","row":0}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 5, 'label',
     (SELECT id FROM catalog_entity WHERE kind='token' AND slug='hud-bar-sim-date-readout'),
     'hud-bar-sim-date-readout',
     '{"kind":"readout","bind":"timeManager.currentDate","format":"text","cadence":"tick"}'::jsonb,
     '{"zone":"center","row":1}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 6, 'label',
     (SELECT id FROM catalog_entity WHERE kind='token' AND slug='hud-bar-population-readout'),
     'hud-bar-population-readout',
     '{"kind":"readout","bind":"cityStats.population","format":"integer","cadence":"tick"}'::jsonb,
     '{"zone":"center","row":2}'::jsonb),

  -- Right zone (ord 7-14): zoom-in, zoom-out, budget, play-pause, speed-cycle, stats, auto, map
    (v_panel_id, v_ver_id, 'main', 7, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-zoom-in-button'),
     'hud-bar-zoom-in-button',
     '{"kind":"illuminated-button","icon":"zoom-in","action":"action.camera-zoom-in"}'::jsonb,
     '{"zone":"right","col":0,"row":0}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 8, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-zoom-out-button'),
     'hud-bar-zoom-out-button',
     '{"kind":"illuminated-button","icon":"zoom-out","action":"action.camera-zoom-out"}'::jsonb,
     '{"zone":"right","col":0,"row":1}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 9, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-button'),
     'hud-bar-budget-button',
     '{"kind":"illuminated-button","icon":"long","bind":"economyManager.totalBudget","sub_bind":"economyManager.budgetDelta","format":"currency","sub_format":"currency-delta","action":"action.budget-panel-toggle"}'::jsonb,
     '{"zone":"right","col":1,"row":0}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 10, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-play-pause-button'),
     'hud-bar-play-pause-button',
     '{"kind":"illuminated-button","icon":"pause","alt_icon":"pause","bind_state":"timeManager.isPaused","action":"action.time-play-pause-toggle"}'::jsonb,
     '{"zone":"right","col":1,"row":1,"sub_col":0}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 11, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-cycle-button'),
     'hud-bar-speed-cycle-button',
     '{"kind":"illuminated-button","icon":"empty","label_bind":"timeManager.currentTimeSpeedLabel","action":"action.time-speed-cycle"}'::jsonb,
     '{"zone":"right","col":1,"row":1,"sub_col":1}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 12, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-stats-button'),
     'hud-bar-stats-button',
     '{"kind":"illuminated-button","icon":"stats","action":"action.stats-panel-toggle"}'::jsonb,
     '{"zone":"right","col":2,"row":0,"rowSpan":2}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 13, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-auto-button'),
     'hud-bar-auto-button',
     '{"kind":"illuminated-button","icon":"empty","label":"AUTO","bind_state":"uiManager.isAutoMode","action":"action.auto-mode-toggle"}'::jsonb,
     '{"zone":"right","col":3,"row":0,"rowSpan":2}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 14, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-map-button'),
     'hud-bar-map-button',
     '{"kind":"illuminated-button","icon":"empty","label":"MAP","action":"action.map-panel-toggle"}'::jsonb,
     '{"zone":"right","col":4,"row":0,"rowSpan":2}'::jsonb);

  RAISE NOTICE '0108: hud-bar v2 reseeded with 14 panel_child rows (panel_id=% ver=%)', v_panel_id, v_ver_id;
END;
$$;

-- ── 6. Soft-retire 14 stale entities ────────────────────────────────────────

UPDATE catalog_entity
SET retired_at = now(),
    retired_reason = '0108: hud-bar v2 reseed — entity removed from locked def'
WHERE retired_at IS NULL
  AND slug IN (
    'hud-bar-recenter-button',
    'hud-bar-auto-toggle',
    'hud-bar-budget-plus-button',
    'hud-bar-budget-minus-button',
    'hud-bar-budget-graph-button',
    'hud-bar-pause-button',
    'hud-bar-speed-1-button',
    'hud-bar-speed-2-button',
    'hud-bar-speed-3-button',
    'hud-bar-speed-4-button',
    'hud-bar-speed-5-button',
    'hud-bar-play-button',
    'hud-bar-build-residential-button',
    'hud-bar-build-commercial-button',
    'hud-bar-budget-readout-label'
  );

-- ── 7. Sanity assertions ────────────────────────────────────────────────────

DO $$
DECLARE
  n_kids       int;
  n_with_slug  int;
  n_retired    int;
  n_alive_kids int;
BEGIN
  SELECT COUNT(*) INTO n_kids
    FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
    WHERE ce.slug = 'hud-bar';

  SELECT COUNT(*) INTO n_with_slug
    FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
    WHERE ce.slug = 'hud-bar'
      AND pc.instance_slug IS NOT NULL
      AND pc.instance_slug <> '';

  SELECT COUNT(*) INTO n_retired
    FROM catalog_entity
    WHERE slug LIKE 'hud-bar-%' AND retired_at IS NOT NULL;

  -- Alive children referenced by panel_child must equal 14 (no orphan FKs to retired rows).
  SELECT COUNT(*) INTO n_alive_kids
    FROM panel_child pc
    JOIN catalog_entity hud ON hud.id = pc.panel_entity_id AND hud.slug = 'hud-bar'
    JOIN catalog_entity child ON child.id = pc.child_entity_id
    WHERE child.retired_at IS NULL;

  IF n_kids <> 14 THEN
    RAISE EXCEPTION '0108: expected 14 panel_child rows for hud-bar, got %', n_kids;
  END IF;

  IF n_with_slug <> 14 THEN
    RAISE EXCEPTION '0108: expected 14 rows with instance_slug, got %', n_with_slug;
  END IF;

  IF n_retired < 14 THEN
    RAISE EXCEPTION '0108: expected ≥14 retired hud-bar entities, got %', n_retired;
  END IF;

  IF n_alive_kids <> 14 THEN
    RAISE EXCEPTION '0108: expected 14 panel_child → alive entity links, got % (some children point at retired entities)', n_alive_kids;
  END IF;

  RAISE NOTICE '0108 OK: hud-bar v2 children=% with-slug=% retired=% alive-links=%',
    n_kids, n_with_slug, n_retired, n_alive_kids;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   Reset panel_child to 0105 shape + un-retire stale by setting retired_at=NULL.
--   See 0105_seed_hud_bar_catalog_9_15.sql for the prior 19-row INSERT block.
