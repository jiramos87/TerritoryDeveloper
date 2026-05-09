-- 0119_seed_new_game_settings_panels.sql
-- Wave A2 (TECH-27068) — seed new-game-form + settings-view panels with host_slots wiring.
--
-- new-game-form  : 3-card map picker + 3-chip budget picker + text-input cityName + section-headers.
-- settings-view  : host_slots=[main-menu-content-slot, pause-menu-content-slot] + 9 controls
--                  (3 toggles + 3 sliders + 1 dropdown + 1 reset-button + section-headers).
--
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ─── 1. catalog_entity rows (kind=panel) ─────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('panel', 'new-game-form',  'New Game Form'),
  ('panel', 'settings-view',  'Settings View')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. panel_detail rows ────────────────────────────────────────────────────

INSERT INTO panel_detail (entity_id, panel_kind, host_slots_json, params_json)
SELECT
  ce.id,
  m.panel_kind,
  m.host_slots_json::jsonb,
  m.params_json::jsonb
FROM (VALUES
  ('new-game-form',
   'screen',
   '["main-menu-content-slot"]',
   '{"title":"New Game","bind_enum":"mainmenu.contentScreen","bind_value":"new-game"}'
  ),
  ('settings-view',
   'screen',
   '["main-menu-content-slot","pause-menu-content-slot"]',
   '{"title":"Settings","bind_enum":"mainmenu.contentScreen","bind_value":"settings"}'
  )
) AS m(slug, panel_kind, host_slots_json, params_json)
JOIN catalog_entity ce ON ce.kind = 'panel' AND ce.slug = m.slug
ON CONFLICT (entity_id) DO UPDATE
  SET panel_kind       = EXCLUDED.panel_kind,
      host_slots_json  = EXCLUDED.host_slots_json,
      params_json      = EXCLUDED.params_json,
      updated_at       = now();

-- ─── 3. panel_child rows — new-game-form ─────────────────────────────────────
-- Children: section-header (Map Size) + 3 map cards + section-header (Starting Budget) +
--           3 budget chips + section-header (City Name) + text-input cityName

INSERT INTO panel_child (entity_id, ord, child_slug, child_kind, layout_json)
SELECT
  ce.id,
  m.ord,
  m.child_slug,
  m.child_kind,
  m.layout_json::jsonb
FROM (VALUES
  (1,  'map-size-header',    'section-header',  '{"label":"Map Size"}'),
  (2,  'map-small-card',     'card-picker',     '{"bind":"newgame.mapSize","value":"small","label":"Small","description":"64×64"}'),
  (3,  'map-medium-card',    'card-picker',     '{"bind":"newgame.mapSize","value":"medium","label":"Medium","description":"128×128"}'),
  (4,  'map-large-card',     'card-picker',     '{"bind":"newgame.mapSize","value":"large","label":"Large","description":"256×256"}'),
  (5,  'budget-header',      'section-header',  '{"label":"Starting Budget"}'),
  (6,  'budget-low-chip',    'chip-picker',     '{"bind":"newgame.budget","value":"low","label":"$10,000"}'),
  (7,  'budget-mid-chip',    'chip-picker',     '{"bind":"newgame.budget","value":"medium","label":"$50,000"}'),
  (8,  'budget-high-chip',   'chip-picker',     '{"bind":"newgame.budget","value":"high","label":"$200,000"}'),
  (9,  'cityname-header',    'section-header',  '{"label":"City Name"}'),
  (10, 'cityname-input',     'text-input',      '{"bind":"newgame.cityName","placeholder":"Enter city name...","reroll_action":"newgame.cityName.reroll"}')
) AS m(ord, child_slug, child_kind, layout_json)
JOIN catalog_entity ce ON ce.kind = 'panel' AND ce.slug = 'new-game-form'
ON CONFLICT (entity_id, ord) DO NOTHING;

-- ─── 4. panel_child rows — settings-view ─────────────────────────────────────
-- Children: section-header (Audio) + 3 volume sliders + section-header (Display) +
--           fullscreen toggle + vsync toggle + resolution dropdown +
--           section-header (Gameplay) + scroll-edge toggle + reset-to-defaults button

INSERT INTO panel_child (entity_id, ord, child_slug, child_kind, layout_json)
SELECT
  ce.id,
  m.ord,
  m.child_slug,
  m.child_kind,
  m.layout_json::jsonb
FROM (VALUES
  (1,  'audio-header',         'section-header',  '{"label":"Audio"}'),
  (2,  'master-volume-slider', 'slider-row',       '{"bind":"settings.masterVolume","label":"Master","min":0,"max":1,"step":0.01}'),
  (3,  'music-volume-slider',  'slider-row',       '{"bind":"settings.musicVolume","label":"Music","min":0,"max":1,"step":0.01,"linearToDecibel":true}'),
  (4,  'sfx-volume-slider',    'slider-row',       '{"bind":"settings.sfxVolume","label":"SFX","min":0,"max":1,"step":0.01,"linearToDecibel":true}'),
  (5,  'display-header',       'section-header',  '{"label":"Display"}'),
  (6,  'fullscreen-toggle',    'toggle-row',       '{"bind":"settings.fullscreen","label":"Fullscreen"}'),
  (7,  'vsync-toggle',         'toggle-row',       '{"bind":"settings.vsync","label":"VSync"}'),
  (8,  'resolution-dropdown',  'dropdown-row',     '{"bind":"settings.resolution","label":"Resolution","options_action":"settings.resolution.options"}'),
  (9,  'gameplay-header',      'section-header',  '{"label":"Gameplay"}'),
  (10, 'scroll-edge-toggle',   'toggle-row',       '{"bind":"settings.scrollEdgePan","label":"Edge Scroll"}'),
  (11, 'monthly-notif-toggle', 'toggle-row',       '{"bind":"settings.monthlyBudgetNotifications","label":"Budget Alerts"}'),
  (12, 'auto-save-toggle',     'toggle-row',       '{"bind":"settings.autoSave","label":"Auto-Save"}'),
  (13, 'reset-button',         'confirm-button',   '{"action":"settings.reset","confirm_action":"settings.reset.confirmed","confirm_seconds":3,"label":"Reset to Defaults"}')
) AS m(ord, child_slug, child_kind, layout_json)
JOIN catalog_entity ce ON ce.kind = 'panel' AND ce.slug = 'settings-view'
ON CONFLICT (entity_id, ord) DO NOTHING;

-- ─── 5. entity_version + publish ─────────────────────────────────────────────

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

-- ─── 6. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_panels int;
  n_children_ngf int;
  n_children_sv int;
BEGIN
  SELECT COUNT(*) INTO n_panels
  FROM catalog_entity ce
  JOIN panel_detail pd ON pd.entity_id = ce.id
  WHERE ce.kind = 'panel'
    AND ce.slug IN ('new-game-form', 'settings-view');

  IF n_panels <> 2 THEN
    RAISE EXCEPTION '0119: expected 2 panel rows with panel_detail, got %', n_panels;
  END IF;

  SELECT COUNT(*) INTO n_children_ngf
  FROM panel_child pc
  JOIN catalog_entity ce ON ce.id = pc.entity_id AND ce.kind = 'panel' AND ce.slug = 'new-game-form';

  IF n_children_ngf < 10 THEN
    RAISE EXCEPTION '0119: expected >=10 new-game-form children, got %', n_children_ngf;
  END IF;

  SELECT COUNT(*) INTO n_children_sv
  FROM panel_child pc
  JOIN catalog_entity ce ON ce.id = pc.entity_id AND ce.kind = 'panel' AND ce.slug = 'settings-view';

  IF n_children_sv < 13 THEN
    RAISE EXCEPTION '0119: expected >=13 settings-view children, got %', n_children_sv;
  END IF;

  RAISE NOTICE '0119 OK: new-game-form (% children) + settings-view (% children) seeded',
    n_children_ngf, n_children_sv;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='panel' AND slug IN ('new-game-form','settings-view'));
--   DELETE FROM panel_detail WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='panel' AND slug IN ('new-game-form','settings-view'));
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='panel' AND slug IN ('new-game-form','settings-view'));
--   DELETE FROM catalog_entity WHERE kind='panel' AND slug IN ('new-game-form','settings-view');
