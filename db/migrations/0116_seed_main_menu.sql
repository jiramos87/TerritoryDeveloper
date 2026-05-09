-- 0116_seed_main_menu.sql
-- Wave A1 (TECH-27063) — seed main-menu panel + 10 panel_child rows.
--
-- Entities created:
--   catalog_entity (kind=panel, slug=main-menu, status=published)
--   panel_detail   (layout_template=fullscreen-stack, params_json.bg_color_token=color-bg-menu)
--   10 panel_child rows:
--     5 primary buttons : mainmenu-continue, mainmenu-new-game, mainmenu-load,
--                         mainmenu-settings, mainmenu-quit
--     1 confirm-button  : mainmenu-quit-confirm
--     1 back-button     : mainmenu-back
--     3 labels          : mainmenu-title-label, mainmenu-version-label, mainmenu-studio-label
--     1 view-slot       : mainmenu-content-slot
--
-- Action/bind refs resolve against Wave A0 registry (0115_ui_registry_log).
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ─── 1. catalog_entity rows for 10 child entities ────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('button',    'mainmenu-continue-button',    'Continue'),
  ('button',    'mainmenu-new-game-button',    'New Game'),
  ('button',    'mainmenu-load-button',        'Load City'),
  ('button',    'mainmenu-settings-button',    'Settings'),
  ('button',    'mainmenu-quit-button',        'Quit'),
  ('button',    'mainmenu-quit-confirm-button','Quit Confirm'),
  ('button',    'mainmenu-back-button',        'Back'),
  ('token',     'mainmenu-title-label',        'Title Label'),
  ('token',     'mainmenu-version-label',      'Version Label'),
  ('token',     'mainmenu-studio-label',       'Studio Label'),
  ('component', 'mainmenu-content-slot',       'Main Menu Content Slot')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. entity_version + publish for children ────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration":"0116_seed_main_menu","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.slug IN (
  'mainmenu-continue-button','mainmenu-new-game-button','mainmenu-load-button',
  'mainmenu-settings-button','mainmenu-quit-button','mainmenu-quit-confirm-button',
  'mainmenu-back-button','mainmenu-title-label','mainmenu-version-label',
  'mainmenu-studio-label','mainmenu-content-slot'
)
AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.slug IN (
    'mainmenu-continue-button','mainmenu-new-game-button','mainmenu-load-button',
    'mainmenu-settings-button','mainmenu-quit-button','mainmenu-quit-confirm-button',
    'mainmenu-back-button','mainmenu-title-label','mainmenu-version-label',
    'mainmenu-studio-label','mainmenu-content-slot'
  )
AND ce.current_published_version_id IS NULL;

-- ─── 3. Main-menu panel entity + panel_detail ────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'main-menu', 'Main Menu')
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px, params_json)
SELECT
  ce.id,
  'vstack',
  'vstack',
  '{"top":0,"left":0,"right":0,"bottom":0}'::jsonb,
  0,
  '{"fullscreen":true,"bg_color_token":"color-bg-menu"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'main-menu'
ON CONFLICT (entity_id) DO UPDATE
  SET params_json = EXCLUDED.params_json,
      updated_at  = now();

-- Store bg_color_token in params_json on the entity_version.
INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published',
  '{"bg_color_token":"color-bg-menu"}'::jsonb,
  '{}'::jsonb,
  '{"migration":"0116_seed_main_menu","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'main-menu'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'main-menu'
  AND ce.current_published_version_id IS NULL;

-- ─── 4. panel_child rows ─────────────────────────────────────────────────────

DO $$
DECLARE
  v_panel_id  bigint;
  v_ver_id    bigint;
BEGIN
  SELECT ce.id INTO v_panel_id
  FROM catalog_entity ce
  WHERE ce.kind = 'panel' AND ce.slug = 'main-menu';

  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0116: main-menu panel entity missing';
  END IF;

  SELECT ev.id INTO v_ver_id
  FROM entity_version ev
  WHERE ev.entity_id = v_panel_id AND ev.version_number = 1;

  IF v_ver_id IS NULL THEN
    RAISE EXCEPTION '0116: main-menu entity_version missing';
  END IF;

  -- Wipe and reseed panel_child for idempotency.
  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  INSERT INTO panel_child (
    panel_entity_id, panel_version_id,
    slot_name, order_idx, child_kind, child_entity_id,
    instance_slug, params_json, layout_json
  )
  SELECT
    v_panel_id,
    v_ver_id,
    m.slot_name,
    m.order_idx,
    m.child_kind,
    ce.id,
    m.slot_name,
    m.params_json::jsonb,
    m.layout_json::jsonb
  FROM (VALUES
    -- 5 primary buttons (child_kind=button, catalog_kind=button)
    ('continue',     1, 'button',    'button',    'mainmenu-continue-button',
     '{"kind":"button","action":"mainmenu.continue","disabled_bind":"mainmenu.continue.disabled"}',
     '{"zone":"main"}'),
    ('new-game',     2, 'button',    'button',    'mainmenu-new-game-button',
     '{"kind":"button","action":"mainmenu.new-game"}',
     '{"zone":"main"}'),
    ('load',         3, 'button',    'button',    'mainmenu-load-button',
     '{"kind":"button","action":"mainmenu.load"}',
     '{"zone":"main"}'),
    ('settings',     4, 'button',    'button',    'mainmenu-settings-button',
     '{"kind":"button","action":"mainmenu.settings"}',
     '{"zone":"main"}'),
    ('quit',         5, 'button',    'button',    'mainmenu-quit-button',
     '{"kind":"button","action":"mainmenu.quit"}',
     '{"zone":"main"}'),
    -- 1 confirm-button (child_kind=button, catalog_kind=button)
    ('quit-confirm', 6, 'button',    'button',    'mainmenu-quit-confirm-button',
     '{"kind":"button","action":"mainmenu.quit-confirm","confirm_action":"mainmenu.quit-confirmed","confirm_seconds":3}',
     '{"zone":"confirm"}'),
    -- 1 back-button (child_kind=button)
    ('back',         7, 'button',    'button',    'mainmenu-back-button',
     '{"kind":"button","action":"mainmenu.back"}',
     '{"zone":"nav"}'),
    -- 3 labels (child_kind=label, catalog_kind=token — tokens used as label descriptors)
    ('title',        8, 'label',     'token',     'mainmenu-title-label',
     '{"kind":"label","text_token":"size-text-title-display"}',
     '{"zone":"branding"}'),
    ('version',      9, 'label',     'token',     'mainmenu-version-label',
     '{"kind":"label","bind":"mainmenu.version"}',
     '{"zone":"branding"}'),
    ('studio',      10, 'label',     'token',     'mainmenu-studio-label',
     '{"kind":"label","text_static":"Bacayo Studio"}',
     '{"zone":"branding"}'),
    -- 1 view-slot (child_kind=panel, catalog_kind=component — slot is a sub-panel container)
    ('content-slot',11, 'panel',     'component', 'mainmenu-content-slot',
     '{"kind":"panel","bind":"mainmenu.contentScreen"}',
     '{"zone":"content"}')
  ) AS m(slot_name, order_idx, child_kind, cat_kind, child_slug, params_json, layout_json)
  JOIN catalog_entity ce ON ce.kind = m.cat_kind AND ce.slug = m.child_slug;

  RAISE NOTICE '0116 OK: main-menu panel seeded (panel_id=% ver=%)', v_panel_id, v_ver_id;
END;
$$;

-- ─── 5. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  v_panel_id  bigint;
  v_pub_id    bigint;
  n_kids      int;
BEGIN
  SELECT ce.id, ce.current_published_version_id
    INTO v_panel_id, v_pub_id
  FROM catalog_entity ce
  WHERE ce.kind = 'panel' AND ce.slug = 'main-menu';

  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0116: main-menu entity missing after seed';
  END IF;
  IF v_pub_id IS NULL THEN
    RAISE EXCEPTION '0116: main-menu current_published_version_id NULL after seed';
  END IF;

  SELECT COUNT(*) INTO n_kids
  FROM panel_child pc
  WHERE pc.panel_entity_id = v_panel_id;

  IF n_kids <> 11 THEN
    RAISE EXCEPTION '0116: expected 11 panel_child rows, got %', n_kids;
  END IF;

  RAISE NOTICE '0116 OK: assertions passed (panel_id=% pub_version=% children=%)',
    v_panel_id, v_pub_id, n_kids;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child WHERE panel_entity_id = (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='main-menu');
--   DELETE FROM entity_version WHERE entity_id = (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='main-menu');
--   DELETE FROM panel_detail WHERE entity_id = (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='main-menu');
--   DELETE FROM catalog_entity WHERE kind='panel' AND slug='main-menu';
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE slug IN ('mainmenu-continue-button','mainmenu-new-game-button','mainmenu-load-button','mainmenu-settings-button','mainmenu-quit-button','mainmenu-quit-confirm-button','mainmenu-back-button','mainmenu-title-label','mainmenu-version-label','mainmenu-studio-label','mainmenu-content-slot'));
--   DELETE FROM catalog_entity WHERE slug IN ('mainmenu-continue-button','mainmenu-new-game-button','mainmenu-load-button','mainmenu-settings-button','mainmenu-quit-button','mainmenu-quit-confirm-button','mainmenu-back-button','mainmenu-title-label','mainmenu-version-label','mainmenu-studio-label','mainmenu-content-slot');
