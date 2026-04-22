import type { CatalogAssetByIdResult } from "@/types/api/catalog-asset-by-id";
import type { CatalogAssetRow } from "@/types/api/catalog-asset";
import type { CatalogPreviewDiffResult } from "@/types/api/catalog-api";

/** Patchable top-level asset fields (immutables like `id` excluded). */
const ASSET_KEYS = new Set<keyof CatalogAssetRow>([
  "category",
  "slug",
  "display_name",
  "status",
  "replaced_by",
  "footprint_w",
  "footprint_h",
  "placement_mode",
  "unlocks_after",
  "has_button",
  "updated_at",
]);

/**
 * In-memory diff only (no database writes) — @see `ia/projects/TECH-645.md`.
 */
export function computeCatalogAssetPreview(
  before: CatalogAssetByIdResult,
  patch: Record<string, unknown>,
): CatalogPreviewDiffResult<CatalogAssetByIdResult, CatalogAssetByIdResult> {
  const afterAsset: CatalogAssetRow = { ...before.asset };
  const diffKeys: string[] = [];
  for (const k of Object.keys(patch)) {
    if (!ASSET_KEYS.has(k as keyof CatalogAssetRow)) continue;
    const key = k as keyof CatalogAssetRow;
    if (patch[k] === undefined) continue;
    const next = patch[k] as never;
    if (before.asset[key] !== (next as never)) {
      diffKeys.push(k);
    }
    (afterAsset as unknown as Record<string, unknown>)[k] = next;
  }
  const after: CatalogAssetByIdResult = { ...before, asset: afterAsset };
  return {
    changed: diffKeys.length > 0,
    before,
    after,
    diff_keys: diffKeys,
  };
}
