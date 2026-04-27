-- Add `primary_subtype_pool_id` to `asset_detail` (TECH-1789).
--
-- Additive, NULL-safe, idempotent. Stage 7.1 binds the asset's canonical
-- "home" pool here per DEC-A11. App-level enforcement (see
-- web/lib/catalog/patch-asset-spine.ts) requires the referenced pool to
-- have a matching `pool_member` row before persisting the pointer.
--
-- @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1789 §Plan Digest

BEGIN;

ALTER TABLE asset_detail
  ADD COLUMN IF NOT EXISTS primary_subtype_pool_id bigint
    REFERENCES catalog_entity (id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS asset_detail_primary_subtype_pool_idx
  ON asset_detail (primary_subtype_pool_id)
  WHERE primary_subtype_pool_id IS NOT NULL;

COMMIT;

-- Rollback:
--   DROP INDEX IF EXISTS asset_detail_primary_subtype_pool_idx;
--   ALTER TABLE asset_detail DROP COLUMN IF EXISTS primary_subtype_pool_id;
