-- Spawn pool tables (TECH-615 / T1.1.4). See docs/grid-asset-visual-registry-exploration.md §8.1.

BEGIN;

CREATE TABLE IF NOT EXISTS catalog_spawn_pool (
  id             bigserial PRIMARY KEY,
  slug           text    NOT NULL UNIQUE,
  owner_category text    NOT NULL,
  owner_subtype  text
);

CREATE TABLE IF NOT EXISTS catalog_pool_member (
  pool_id   bigint NOT NULL REFERENCES catalog_spawn_pool (id) ON DELETE CASCADE,
  asset_id  bigint NOT NULL REFERENCES catalog_asset (id) ON DELETE RESTRICT,
  weight    int    NOT NULL DEFAULT 1 CHECK (weight > 0),
  PRIMARY KEY (pool_id, asset_id)
);

CREATE INDEX IF NOT EXISTS idx_pool_member_by_asset ON catalog_pool_member (asset_id);
CREATE INDEX IF NOT EXISTS idx_spawn_pool_category ON catalog_spawn_pool (owner_category);

COMMIT;
