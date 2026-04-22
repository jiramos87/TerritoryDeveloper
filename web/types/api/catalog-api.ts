import type { CatalogAssetRow } from "./catalog-asset";
import type { CatalogEconomyRow } from "./catalog-economy";
import type { CatalogAssetStatus, CatalogSpriteSlot } from "./catalog-enums";

/**
 * Shared query / body types for Stage 1.3 ` /api/catalog/*` handlers (this file is types-only).
 */

/** List filters for catalog asset collections (aligns with `catalog_asset` columns + visibility story). */
export interface CatalogAssetListFilters {
  /** When set, restrict to a single lifecycle status. */
  status?: CatalogAssetStatus;
  /** When set, `WHERE category = :category` */
  category?: string;
}

/**
 * Request bodies that carry an optimistic lock token. Mutations compare to row `updated_at`
 * and return 409 on mismatch (Stage 1.3).
 */
export interface CatalogOptimisticLockFields {
  /** ISO-8601 timestamptz string — must match current row `updated_at` for successful PATCH. */
  updated_at: string;
}

/**
 * JSON-serializable preview-diff result for “what would change” calls (no migration changes in Stage 1.2).
 * `patch` is a loose record so route code can diff concrete DTO fields in Stage 1.3.
 */
export interface CatalogPreviewDiffResult<TBefore = unknown, TAfter = unknown> {
  /** Whether any field would change. */
  changed: boolean;
  before: TBefore;
  after: TAfter;
  /** Optional list of top-level field keys that differ. */
  diff_keys?: string[];
}

/** `POST /api/catalog/assets` (Stage 1.3). */
export interface CatalogCreateAssetBody {
  category: string;
  slug: string;
  display_name: string;
  status: CatalogAssetStatus;
  replaced_by?: string | null;
  footprint_w?: number;
  footprint_h?: number;
  placement_mode?: string | null;
  unlocks_after?: string | null;
  has_button?: boolean;
  economy: {
    base_cost_cents: number;
    monthly_upkeep_cents: number;
    demolition_refund_pct?: number;
    construction_ticks?: number;
    budget_envelope_id?: number | null;
    cost_catalog_row_id?: string | null;
  };
  sprite_binds: Array<{
    slot: CatalogSpriteSlot;
    sprite_id: string;
  }>;
}

/** `PATCH /api/catalog/assets/:id` (Stage 1.3) — versioned partial update. */
export type CatalogPatchAssetBody = Partial<
  Pick<
    CatalogAssetRow,
    | "display_name"
    | "status"
    | "replaced_by"
    | "footprint_w"
    | "footprint_h"
    | "placement_mode"
    | "unlocks_after"
    | "has_button"
  >
> & {
  updated_at: string;
  economy?: Partial<
    Pick<
      CatalogEconomyRow,
      | "base_cost_cents"
      | "monthly_upkeep_cents"
      | "demolition_refund_pct"
      | "construction_ticks"
      | "budget_envelope_id"
      | "cost_catalog_row_id"
    >
  >;
};

/** `POST /api/catalog/assets/:id/retire` (Stage 1.3). */
export interface CatalogRetireBody {
  replaced_by?: string | null;
}

/** `POST /api/catalog/preview-diff` (Stage 1.3) — `patch` is shallow asset-field overrides only. */
export interface CatalogPreviewDiffRequest {
  asset_id: string;
  patch: Record<string, unknown>;
}
