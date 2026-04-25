-- HELD migration — Stage-1b. NOT picked up by tools/postgres-ia/apply-migrations.mjs
-- (which only scans db/migrations/*.sql top-level). Promote to db/migrations/
-- ONLY AFTER:
--   1. All consumers of `catalog_asset` / `catalog_sprite` / `catalog_economy`
--      / `catalog_spawn_pool` / `catalog_pool_member` / `catalog_asset_sprite`
--      have switched to spine queries (or the `catalog_asset_compat` view).
--   2. Unity `ZoneSubTypeRegistry` keys off slug or `legacy_asset_id` exposed
--      via API, not raw `catalog_asset.id`.
--   3. Stage 0 audit re-run on the post-spine DB confirms no legacy reads.
--
-- Drop order matches FK direction: leaves first.

BEGIN;

DROP VIEW  IF EXISTS catalog_asset_compat;

DROP TABLE IF EXISTS catalog_pool_member;
DROP TABLE IF EXISTS catalog_spawn_pool;
DROP TABLE IF EXISTS catalog_economy;
DROP TABLE IF EXISTS catalog_asset_sprite;
DROP TABLE IF EXISTS catalog_sprite;
DROP TABLE IF EXISTS catalog_asset;

COMMIT;
