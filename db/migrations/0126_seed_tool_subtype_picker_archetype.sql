-- 0126_seed_tool_subtype_picker_archetype.sql
-- Wave B1 (TECH-27079) — 1 archetype row for tool-subtype-picker.
--
-- Archetype:
--   subtype-picker-strip — 3-density variant grid, encodes R/C/I evolution slugs.
--                          Covers cards-density families (R/C/I). cards-kind families
--                          (StateZoning, Road, Power, etc.) share same archetype shell
--                          with different picker_variant payload.
--
-- Idempotent: ON CONFLICT DO NOTHING / DO UPDATE throughout.

BEGIN;

-- ─── 1. catalog_entity row ────────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name, tags)
VALUES (
  'archetype',
  'subtype-picker-strip',
  'Subtype Picker Strip',
  ARRAY['cityscene', 'toolbar', 'wave-b1']
)
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published',
  '{
    "picker_variant": "cards-density",
    "layout": "hstack",
    "strip_h_px": 96,
    "card_w_px": 80,
    "card_h_px": 80,
    "gap_px": 8,
    "anchor": "bottom-left",
    "hidden_default": true,
    "open_trigger": "action.tool-select",
    "close_triggers": ["action.tool-deselect", "key.escape"],
    "density_slots": ["light", "medium", "heavy"],
    "density_families": ["ResidentialZoning", "CommercialZoning", "IndustrialZoning"]
  }'::jsonb,
  '{}'::jsonb,
  '{"migration":"0126_seed_tool_subtype_picker_archetype","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'archetype' AND ce.slug = 'subtype-picker-strip'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'archetype'
  AND ce.slug = 'subtype-picker-strip'
  AND ce.current_published_version_id IS NULL;

-- ─── 3. Sanity assertion ──────────────────────────────────────────────────────

DO $$
DECLARE
  n_rows int;
BEGIN
  SELECT COUNT(*) INTO n_rows
  FROM catalog_entity ce
  JOIN entity_version ev ON ev.entity_id = ce.id AND ev.version_number = 1
  WHERE ce.kind = 'archetype'
    AND ce.slug = 'subtype-picker-strip'
    AND ev.status = 'published';

  IF n_rows < 1 THEN
    RAISE EXCEPTION '0126: expected 1 subtype-picker-strip archetype published, got %', n_rows;
  END IF;

  RAISE NOTICE '0126 OK: subtype-picker-strip archetype seeded';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM entity_version WHERE entity_id IN (SELECT id FROM catalog_entity WHERE kind='archetype' AND slug='subtype-picker-strip');
--   DELETE FROM catalog_entity WHERE kind='archetype' AND slug='subtype-picker-strip';
