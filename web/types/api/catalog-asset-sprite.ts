import type { CatalogSpriteSlot } from "./catalog-enums";

/**
 * Row shape for `catalog_asset_sprite` — `db/migrations/0011_catalog_core.sql`.
 * Composite PK `(asset_id, slot)`.
 */
export interface CatalogAssetSpriteRow {
  asset_id: string;
  sprite_id: string;
  slot: CatalogSpriteSlot;
}
