import type { CatalogAssetRow } from "./catalog-asset";
import type { CatalogAssetSpriteRow } from "./catalog-asset-sprite";
import type { CatalogEconomyRow } from "./catalog-economy";
import type { CatalogSpriteRow } from "./catalog-sprite";

/**
 * Joined read model for a future `GET /api/catalog/assets/:id` (Stage 1.3):
 * one asset row, economy extension, and sprite slots resolved to sprite rows.
 */
export interface CatalogAssetByIdResult {
  asset: CatalogAssetRow;
  economy: CatalogEconomyRow | null;
  /** Bindings with resolved `CatalogSpriteRow` for each slot (order is API-defined). */
  sprite_slots: Array<{
    binding: CatalogAssetSpriteRow;
    sprite: CatalogSpriteRow;
  }>;
}
