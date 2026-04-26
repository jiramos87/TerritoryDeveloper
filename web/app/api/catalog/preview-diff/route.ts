import { NextResponse, type NextRequest } from "next/server";
import { loadCatalogAssetById } from "@/lib/catalog/fetch-asset-composite";
import { computeCatalogAssetPreview } from "@/lib/catalog/preview-diff";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import type { CatalogPreviewDiffRequest } from "@/types/api/catalog-api";
export const dynamic = "force-dynamic";
export const routeMeta = { POST: { requires: 'render.run' } } as const;

/**
 * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `POST /api/catalog/preview-diff` (read-only; no `INSERT`/`UPDATE`).
 */
export async function POST(request: NextRequest) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return catalogJsonError(400, "bad_request", "Invalid JSON body");
  }
  const b = body as CatalogPreviewDiffRequest;
  if (!b || typeof b.asset_id !== "string" || !/^\d+$/.test(b.asset_id)) {
    return catalogJsonError(400, "bad_request", "asset_id required (numeric string)");
  }
  if (!b.patch || typeof b.patch !== "object") {
    return catalogJsonError(400, "bad_request", "patch must be an object");
  }
  try {
    const cur = await loadCatalogAssetById(b.asset_id);
    if (cur === "notfound" || cur === "badid") {
      return catalogJsonError(404, "not_found", "Asset not found");
    }
    const result = computeCatalogAssetPreview(cur, b.patch);
    return NextResponse.json(result, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "preview" });
    }
    return responseFromPostgresError(e, "Preview diff failed");
  }
}
