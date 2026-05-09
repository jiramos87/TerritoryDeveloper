-- 0117_seed_view_slot_confirm_button_archetypes.sql
-- Wave A1 (TECH-27064) — seed view-slot + confirm-button archetype entities.
--
-- view-slot     : renders one of N declared sub-views by enum-bind (mainmenu.contentScreen).
-- confirm-button: button variant with confirm_action + confirm_seconds inline countdown.
--
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ─── 1. archetype catalog_entity rows ────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('archetype', 'view-slot',      'View Slot'),
  ('archetype', 'confirm-button', 'Confirm Button')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published',
  CASE ce.slug
    WHEN 'view-slot' THEN
      '{"bind_enum":"<required>","host_slots":{}}'::jsonb
    WHEN 'confirm-button' THEN
      '{"action":"<required>","confirm_action":"<required>","confirm_seconds":3}'::jsonb
  END,
  '{}'::jsonb,
  '{"migration":"0117_seed_view_slot_confirm_button_archetypes","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'archetype'
  AND ce.slug IN ('view-slot', 'confirm-button')
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'archetype'
  AND ce.slug IN ('view-slot', 'confirm-button')
  AND ce.current_published_version_id IS NULL;

-- ─── 3. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_archetypes int;
BEGIN
  SELECT COUNT(*) INTO n_archetypes
  FROM catalog_entity ce
  JOIN entity_version ev ON ev.entity_id = ce.id AND ev.status = 'published'
  WHERE ce.kind = 'archetype'
    AND ce.slug IN ('view-slot', 'confirm-button');

  IF n_archetypes <> 2 THEN
    RAISE EXCEPTION '0117: expected 2 published archetype rows, got %', n_archetypes;
  END IF;

  RAISE NOTICE '0117 OK: view-slot + confirm-button archetypes seeded';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='archetype' AND slug IN ('view-slot','confirm-button'));
--   DELETE FROM catalog_entity WHERE kind='archetype' AND slug IN ('view-slot','confirm-button');
