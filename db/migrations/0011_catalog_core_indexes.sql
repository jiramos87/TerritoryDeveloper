-- TECH-613 / T1.1.2 — Secondary indexes for catalog list/join and remap chains.
-- Depends on: 0011_catalog_core.sql (tables).

BEGIN;

-- List filters (published/draft) and owner grouping.
CREATE INDEX IF NOT EXISTS idx_catalog_asset_status ON catalog_asset (status);
CREATE INDEX IF NOT EXISTS idx_catalog_asset_category ON catalog_asset (category);

-- Join from sprite to bindings; asset_id in PK of catalog_asset_sprite already supports asset-side lookups.
CREATE INDEX IF NOT EXISTS idx_catalog_asset_sprites_by_sprite ON catalog_asset_sprite (sprite_id);

-- Load-time remap: walk replaced_by without full table scan.
CREATE INDEX IF NOT EXISTS idx_catalog_asset_replaced_by ON catalog_asset (replaced_by) WHERE replaced_by IS NOT NULL;

COMMIT;
