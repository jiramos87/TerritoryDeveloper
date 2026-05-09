-- 0119_seed_new_game_settings_panels.sql
-- Wave A2 (TECH-27068) — seed new-game-form + settings-view panels.
--
-- Conforms to actual panel_detail + panel_child schema (0116_seed_main_menu pattern).
-- panel_detail columns: entity_id, layout_template, layout, padding_json, gap_px, params_json
-- panel_child columns: panel_entity_id, panel_version_id, slot_name, order_idx, child_kind,
--                      child_entity_id, instance_slug, params_json, layout_json
--
-- host_slots + panel_kind stored in params_json (no dedicated columns in actual schema).
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ─── 1. catalog_entity rows (kind=panel) ─────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('panel', 'new-game-form',  'New Game Form'),
  ('panel', 'settings-view',  'Settings View')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. panel_detail rows ────────────────────────────────────────────────────

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px, params_json)
SELECT
  ce.id,
  'vstack',
  'vstack',
  '{"top":8,"left":8,"right":8,"bottom":8}'::jsonb,
  8,
  m.params_json::jsonb
FROM (VALUES
  ('new-game-form',
   '{"panel_kind":"screen","host_slots":["main-menu-content-slot"],"title":"New Game","bind_enum":"mainmenu.contentScreen","bind_value":"new-game"}'
  ),
  ('settings-view',
   '{"panel_kind":"screen","host_slots":["main-menu-content-slot","pause-menu-content-slot"],"title":"Settings","bind_enum":"mainmenu.contentScreen","bind_value":"settings"}'
  )
) AS m(slug, params_json)
JOIN catalog_entity ce ON ce.kind = 'panel' AND ce.slug = m.slug
ON CONFLICT (entity_id) DO UPDATE
  SET params_json = EXCLUDED.params_json,
      updated_at  = now();

-- ─── 3. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration":"0119_seed_new_game_settings_panels","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel'
  AND ce.slug IN ('new-game-form', 'settings-view')
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug IN ('new-game-form', 'settings-view')
  AND ce.current_published_version_id IS NULL;

-- ─── 4. panel_child rows — new-game-form ─────────────────────────────────────

DO $$
DECLARE
  v_panel_id bigint;
  v_ver_id   bigint;
BEGIN
  SELECT ce.id INTO v_panel_id FROM catalog_entity ce WHERE ce.kind='panel' AND ce.slug='new-game-form';
  IF v_panel_id IS NULL THEN RAISE EXCEPTION '0119: new-game-form entity missing'; END IF;
  SELECT ev.id INTO v_ver_id FROM entity_version ev WHERE ev.entity_id=v_panel_id AND ev.version_number=1;
  IF v_ver_id IS NULL THEN RAISE EXCEPTION '0119: new-game-form entity_version missing'; END IF;

  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES
    (v_panel_id, v_ver_id, 'map-size-header',    1,  'label', 'map-size-header',
     '{"kind":"section-header","label":"Map Size"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'map-small-card',     2,  'panel', 'map-small-card',
     '{"kind":"card-picker","bind":"newgame.mapSize","value":"small","label":"Small","description":"64x64"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'map-medium-card',    3,  'panel', 'map-medium-card',
     '{"kind":"card-picker","bind":"newgame.mapSize","value":"medium","label":"Medium","description":"128x128"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'map-large-card',     4,  'panel', 'map-large-card',
     '{"kind":"card-picker","bind":"newgame.mapSize","value":"large","label":"Large","description":"256x256"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'budget-header',      5,  'label', 'budget-header',
     '{"kind":"section-header","label":"Starting Budget"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'budget-low-chip',    6,  'panel', 'budget-low-chip',
     '{"kind":"chip-picker","bind":"newgame.budget","value":"low","label":"$10,000"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'budget-mid-chip',    7,  'panel', 'budget-mid-chip',
     '{"kind":"chip-picker","bind":"newgame.budget","value":"medium","label":"$50,000"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'budget-high-chip',   8,  'panel', 'budget-high-chip',
     '{"kind":"chip-picker","bind":"newgame.budget","value":"high","label":"$200,000"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'cityname-header',    9,  'label', 'cityname-header',
     '{"kind":"section-header","label":"City Name"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'cityname-input',     10, 'panel', 'cityname-input',
     '{"kind":"text-input","bind":"newgame.cityName","placeholder":"Enter city name...","reroll_action":"newgame.cityName.reroll"}'::jsonb, '{}'::jsonb);

  RAISE NOTICE '0119 OK: new-game-form children seeded (panel_id=%)', v_panel_id;
END;
$$;

-- ─── 5. panel_child rows — settings-view ─────────────────────────────────────

DO $$
DECLARE
  v_panel_id bigint;
  v_ver_id   bigint;
BEGIN
  SELECT ce.id INTO v_panel_id FROM catalog_entity ce WHERE ce.kind='panel' AND ce.slug='settings-view';
  IF v_panel_id IS NULL THEN RAISE EXCEPTION '0119: settings-view entity missing'; END IF;
  SELECT ev.id INTO v_ver_id FROM entity_version ev WHERE ev.entity_id=v_panel_id AND ev.version_number=1;
  IF v_ver_id IS NULL THEN RAISE EXCEPTION '0119: settings-view entity_version missing'; END IF;

  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES
    (v_panel_id, v_ver_id, 'audio-header',         1,  'label', 'audio-header',
     '{"kind":"section-header","label":"Audio"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'master-volume-slider', 2,  'panel', 'master-volume-slider',
     '{"kind":"slider-row","bind":"settings.masterVolume","label":"Master","min":0,"max":1,"step":0.01}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'music-volume-slider',  3,  'panel', 'music-volume-slider',
     '{"kind":"slider-row","bind":"settings.musicVolume","label":"Music","min":0,"max":1,"step":0.01,"linearToDecibel":true}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'sfx-volume-slider',    4,  'panel', 'sfx-volume-slider',
     '{"kind":"slider-row","bind":"settings.sfxVolume","label":"SFX","min":0,"max":1,"step":0.01,"linearToDecibel":true}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'display-header',       5,  'label', 'display-header',
     '{"kind":"section-header","label":"Display"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'fullscreen-toggle',    6,  'panel', 'fullscreen-toggle',
     '{"kind":"toggle-row","bind":"settings.fullscreen","label":"Fullscreen"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'vsync-toggle',         7,  'panel', 'vsync-toggle',
     '{"kind":"toggle-row","bind":"settings.vsync","label":"VSync"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'resolution-dropdown',  8,  'panel', 'resolution-dropdown',
     '{"kind":"dropdown-row","bind":"settings.resolution","label":"Resolution","options_action":"settings.resolution.options"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'gameplay-header',      9,  'label', 'gameplay-header',
     '{"kind":"section-header","label":"Gameplay"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'scroll-edge-toggle',   10, 'panel', 'scroll-edge-toggle',
     '{"kind":"toggle-row","bind":"settings.scrollEdgePan","label":"Edge Scroll"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'monthly-notif-toggle', 11, 'panel', 'monthly-notif-toggle',
     '{"kind":"toggle-row","bind":"settings.monthlyBudgetNotifications","label":"Budget Alerts"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'auto-save-toggle',     12, 'panel', 'auto-save-toggle',
     '{"kind":"toggle-row","bind":"settings.autoSave","label":"Auto-Save"}'::jsonb, '{}'::jsonb),
    (v_panel_id, v_ver_id, 'reset-button',         13, 'button', 'reset-button',
     '{"kind":"confirm-button","action":"settings.reset","confirm_action":"settings.reset.confirmed","confirm_seconds":3,"label":"Reset to Defaults"}'::jsonb, '{}'::jsonb);

  RAISE NOTICE '0119 OK: settings-view children seeded (panel_id=%)', v_panel_id;
END;
$$;

-- ─── 6. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_panels int;
  n_ngf    int;
  n_sv     int;
BEGIN
  SELECT COUNT(*) INTO n_panels
  FROM catalog_entity ce
  JOIN panel_detail pd ON pd.entity_id = ce.id
  WHERE ce.kind = 'panel' AND ce.slug IN ('new-game-form', 'settings-view');
  IF n_panels <> 2 THEN
    RAISE EXCEPTION '0119: expected 2 panel rows with panel_detail, got %', n_panels;
  END IF;

  SELECT COUNT(*) INTO n_ngf
  FROM panel_child pc
  JOIN catalog_entity ce ON ce.id = pc.panel_entity_id AND ce.slug = 'new-game-form';
  IF n_ngf < 10 THEN
    RAISE EXCEPTION '0119: expected >=10 new-game-form children, got %', n_ngf;
  END IF;

  SELECT COUNT(*) INTO n_sv
  FROM panel_child pc
  JOIN catalog_entity ce ON ce.id = pc.panel_entity_id AND ce.slug = 'settings-view';
  IF n_sv < 13 THEN
    RAISE EXCEPTION '0119: expected >=13 settings-view children, got %', n_sv;
  END IF;

  RAISE NOTICE '0119 OK: new-game-form (% children) + settings-view (% children) seeded', n_ngf, n_sv;
END;
$$;

COMMIT;
