-- 0123_seed_save_load_view.sql
-- Wave A3 (TECH-27073) — save-load-view panel seed.
--
-- Panel children:
--   save-controls-strip (archetype: save-controls-strip)
--   save-list           (archetype: save-list)
--   footer-load-button  (themed-button)
--   save-name-input     (text-input)
--
-- host_slots: main-menu-content-slot, pause-menu-content-slot
-- Mode bind: saveload.mode drives save vs load variant.
--
-- Idempotent: ON CONFLICT DO NOTHING / DO UPDATE throughout.

BEGIN;

-- ─── 1. catalog_entity ───────────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'save-load-view', 'Save / Load View')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. panel_detail ─────────────────────────────────────────────────────────

INSERT INTO panel_detail (entity_id, panel_kind, host_slots_json, params_json)
SELECT
  ce.id,
  'screen',
  '["main-menu-content-slot","pause-menu-content-slot"]'::jsonb,
  '{"modeBindId":"saveload.mode","defaultMode":"load"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'save-load-view'
ON CONFLICT (entity_id) DO UPDATE
  SET panel_kind       = EXCLUDED.panel_kind,
      host_slots_json  = EXCLUDED.host_slots_json,
      params_json      = EXCLUDED.params_json,
      updated_at       = now();

-- ─── 3. panel_child rows ─────────────────────────────────────────────────────

INSERT INTO panel_child (entity_id, child_slug, archetype_slug, sort_order, params_json)
SELECT
  ce.id,
  m.child_slug,
  m.archetype_slug,
  m.sort_order,
  m.params_json::jsonb
FROM (VALUES
  ('save-controls-strip', 'save-controls-strip', 1, '{"bindId":"saveload.mode"}'),
  ('save-list',           'save-list',            2, '{"listBindId":"saveload.list","selectedSlotBindId":"saveload.selectedSlot","trashAction":"saveload.delete","selectAction":"saveload.selectSlot"}'),
  ('save-name-input',     'text-input',           3, '{"bind":"saveload.saveName","placeholder":"City-YYYY-MM-DD-HHmm"}'),
  ('footer-load-button',  'themed-button',        4, '{"label":"Load","action":"saveload.load","disabledBindId":"saveload.loadDisabled"}')
) AS m(child_slug, archetype_slug, sort_order, params_json)
JOIN catalog_entity ce ON ce.kind = 'panel' AND ce.slug = 'save-load-view'
ON CONFLICT (entity_id, child_slug) DO UPDATE
  SET archetype_slug = EXCLUDED.archetype_slug,
      sort_order     = EXCLUDED.sort_order,
      params_json    = EXCLUDED.params_json,
      updated_at     = now();

-- ─── 4. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration":"0123_seed_save_load_view","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'save-load-view'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'save-load-view'
  AND ce.current_published_version_id IS NULL;

-- ─── 5. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_children int;
BEGIN
  SELECT COUNT(*) INTO n_children
  FROM panel_child pc
  JOIN catalog_entity ce ON ce.id = pc.entity_id
  WHERE ce.kind = 'panel' AND ce.slug = 'save-load-view';

  IF n_children <> 4 THEN
    RAISE EXCEPTION '0123: expected 4 panel_child rows for save-load-view, got %', n_children;
  END IF;

  RAISE NOTICE '0123 OK: save-load-view panel seeded (4 children)';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='save-load-view');
--   DELETE FROM panel_child WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='save-load-view');
--   DELETE FROM panel_detail WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='save-load-view');
--   DELETE FROM catalog_entity WHERE kind='panel' AND slug='save-load-view';
