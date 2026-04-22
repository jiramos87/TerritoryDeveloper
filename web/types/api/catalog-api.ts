import type { CatalogAssetStatus } from "./catalog-enums";

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
