import type { CatalogAssetStatus } from "@/types/api/catalog-enums";
import { catalogJsonError } from "./catalog-api-errors";
import type { NextResponse } from "next/server";

const STATUSES: CatalogAssetStatus[] = ["draft", "published", "retired"];

function isCatalogAssetStatus(s: string): s is CatalogAssetStatus {
  return (STATUSES as string[]).includes(s);
}

export type ParsedListQuery =
  | { ok: true; includeDraft: boolean; statusFilter: CatalogAssetStatus | null; category: string | null; limit: number; cursor: string | null }
  | { ok: false; response: ReturnType<typeof catalogJsonError> };

/**
 * @see `ia/projects/TECH-640.md` — default published; optional `include_draft` for admin-style listing.
 */
export function parseListQueryParams(search: URLSearchParams): ParsedListQuery {
  const includeDraft =
    search.get("include_draft") === "1" || search.get("include_draft")?.toLowerCase() === "true";
  const statusRaw = search.get("status");
  let statusFilter: CatalogAssetStatus | null = null;
  if (statusRaw != null && statusRaw.length > 0) {
    if (!isCatalogAssetStatus(statusRaw)) {
      return {
        ok: false,
        response: catalogJsonError(400, "bad_request", "Invalid status query (use draft, published, or retired)"),
      };
    }
    statusFilter = statusRaw;
  }
  const category = search.get("category")?.trim() || null;
  const limitRaw = search.get("limit");
  let limit = 200;
  if (limitRaw != null) {
    const n = Number.parseInt(limitRaw, 10);
    if (!Number.isFinite(n) || n < 1) {
      return { ok: false, response: catalogJsonError(400, "bad_request", "Invalid limit") };
    }
    limit = Math.min(n, 500);
  }
  const cursor = search.get("cursor")?.trim() || null;
  if (cursor != null && !/^\d+$/.test(cursor)) {
    return { ok: false, response: catalogJsonError(400, "bad_request", "Invalid cursor (numeric id string)") };
  }
  return { ok: true, includeDraft, statusFilter, category, limit, cursor };
}
