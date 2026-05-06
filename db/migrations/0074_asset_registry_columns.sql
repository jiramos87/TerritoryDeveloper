-- 0074_asset_registry_columns.sql
-- Stage 9.2 game-ui-catalog-bake — asset-registry tier columns.
-- Adds ds_tokens (design token jsonb) + motion (motion enum jsonb) to catalog_entity.
-- Adds asset_registry view exposing ui-kind entities for the asset-pipeline-standard.
-- TECH-15215

ALTER TABLE catalog_entity
  ADD COLUMN IF NOT EXISTS ds_tokens jsonb NOT NULL DEFAULT '{}'::jsonb,
  ADD COLUMN IF NOT EXISTS motion    jsonb NOT NULL DEFAULT '{"enter":"fade","exit":"fade","hover":"none"}'::jsonb;

-- Validate motion enum values via check constraint (soft — allows extension).
-- Hard enum guard lives in validate:asset-pipeline (schema-only validator).
ALTER TABLE catalog_entity
  DROP CONSTRAINT IF EXISTS ck_catalog_entity_motion_keys;

ALTER TABLE catalog_entity
  ADD CONSTRAINT ck_catalog_entity_motion_keys
  CHECK (
    motion ? 'enter'
    AND motion ? 'exit'
    AND motion ? 'hover'
    AND motion->>'enter' = ANY(ARRAY['fade','slide','none'])
    AND motion->>'exit'  = ANY(ARRAY['fade','slide','none'])
    AND motion->>'hover' = ANY(ARRAY['fade','slide','none'])
  );

-- asset_registry view: canonical alias for ui-kind catalog_entity rows.
-- asset-pipeline-standard §Authority: asset-registry tier = kind IN ('panel','button','token','archetype').
CREATE OR REPLACE VIEW asset_registry AS
SELECT
  id,
  slug,
  kind,
  display_name,
  tags,
  ds_tokens,
  motion,
  lint_overrides_json,
  current_published_version_id,
  retired_at,
  created_at,
  updated_at
FROM catalog_entity
WHERE kind IN ('panel', 'button', 'token', 'archetype');

COMMENT ON VIEW asset_registry IS
  'asset-registry tier: ui-kind catalog entities (panel/button/token/archetype). '
  'DB-wins authority per asset-pipeline-standard §Authority (DEC-A25). '
  'Read-only alias — mutations go via catalog_entity.';
