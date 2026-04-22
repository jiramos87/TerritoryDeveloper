/**
 * Shared enum-like unions for `0011` / `0012` catalog tables (`db/migrations/0011_catalog_core.sql`).
 */

/** `catalog_asset.status` — CHECK in migration. */
export type CatalogAssetStatus = "draft" | "published" | "retired";

/** `catalog_sprite.provenance` — CHECK in migration. */
export type CatalogSpriteProvenance = "hand" | "generator";

/** `catalog_asset_sprite.slot` — CHECK in migration. */
export type CatalogSpriteSlot =
  | "world"
  | "button_target"
  | "button_pressed"
  | "button_disabled"
  | "button_hover";
