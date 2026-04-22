import type { CatalogAssetStatus } from "./catalog-enums";

/**
 * Row shape for `catalog_asset` — `db/migrations/0011_catalog_core.sql`.
 * IDs are `string` for JSON/API safety with `bigserial` values.
 */
export interface CatalogAssetRow {
  id: string;
  category: string;
  slug: string;
  display_name: string;
  status: CatalogAssetStatus;
  /** FK `catalog_asset.id` — soft replace chain */
  replaced_by: string | null;
  footprint_w: number;
  footprint_h: number;
  placement_mode: string | null;
  unlocks_after: string | null;
  has_button: boolean;
  /** Application-owned; ISO-8601 string in DTOs */
  updated_at: string;
}
