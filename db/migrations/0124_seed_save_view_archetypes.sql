-- 0124_seed_save_view_archetypes.sql
-- Wave A3 (TECH-27074) — 2 archetype rows for save-load-view.
--
-- Archetypes:
--   save-controls-strip — mode-driven control row (save / load header strip)
--   save-list           — scrollable rows bound to saveload.list array,
--                         per-row trash + select actions
--
-- Idempotent: ON CONFLICT DO NOTHING / DO UPDATE throughout.

BEGIN;

-- ─── 1. catalog_entity rows ──────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('archetype', 'save-controls-strip', 'Save Controls Strip'),
  ('archetype', 'save-list',           'Save List')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published',
  m.params_json::jsonb,
  '{}'::jsonb,
  ('{"migration":"0124_seed_save_view_archetypes","event":"initial_seed"}'::jsonb)
FROM (VALUES
  ('save-controls-strip', '{"bindId":"saveload.mode","saveLabel":"Save","loadLabel":"Load"}'),
  ('save-list',           '{"listBindId":"saveload.list","selectedSlotBindId":"saveload.selectedSlot","trashAction":"saveload.delete","selectAction":"saveload.selectSlot","sortNewestFirst":true}')
) AS m(slug, params_json)
JOIN catalog_entity ce ON ce.kind = 'archetype' AND ce.slug = m.slug
WHERE NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'archetype'
  AND ce.slug IN ('save-controls-strip','save-list')
  AND ce.current_published_version_id IS NULL;

-- ─── 3. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_archetypes int;
BEGIN
  SELECT COUNT(*) INTO n_archetypes
  FROM catalog_entity ce
  WHERE ce.kind = 'archetype'
    AND ce.slug IN ('save-controls-strip','save-list');

  IF n_archetypes <> 2 THEN
    RAISE EXCEPTION '0124: expected 2 archetype rows, got %', n_archetypes;
  END IF;

  RAISE NOTICE '0124 OK: 2 save-view archetypes seeded';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='archetype' AND slug IN ('save-controls-strip','save-list'));
--   DELETE FROM catalog_entity WHERE kind='archetype' AND slug IN ('save-controls-strip','save-list');
