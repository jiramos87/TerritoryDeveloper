import { NextResponse, type NextRequest } from "next/server";
import { loadCatalogAssetById } from "@/lib/catalog/fetch-asset-composite";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import type { CatalogPatchAssetBody } from "@/types/api/catalog-api";
import { patchCatalogAsset } from "@/lib/catalog/patch-asset";
export const dynamic = "force-dynamic";
export const routeMeta = { GET: { requires: 'catalog.entity.create' }, PATCH: { requires: 'catalog.entity.edit' } } as const;

type Ctx = { params: Promise<{ id: string }> };

/**
 * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `GET /api/catalog/assets/:id`
 */
export async function GET(_request: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;
  try {
    const out = await loadCatalogAssetById(id);
    if (out === "badid") {
      return catalogJsonError(400, "bad_request", "Invalid asset id");
    }
    if (out === "notfound") {
      return catalogJsonError(404, "not_found", "Asset not found");
    }
    return NextResponse.json(out, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "get-id" });
    }
    return responseFromPostgresError(e, "Get asset query failed");
  }
}

/**
 * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `PATCH /api/catalog/assets/:id`
 */
export async function PATCH(request: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return catalogJsonError(400, "bad_request", "Invalid JSON body");
  }
  try {
    const out = await patchCatalogAsset(id, body as CatalogPatchAssetBody);
    if (out.ok === "badid") {
      return catalogJsonError(400, "bad_request", "Invalid id or updated_at (need valid numeric id + ISO updated_at)");
    }
    if (out.ok === "unknown_fields") {
      return catalogJsonError(400, "bad_request", "PATCH body contains unknown fields", {
        details: { unknown_fields: out.unknownFields },
      });
    }
    if (out.ok === "no_fields") {
      return catalogJsonError(400, "bad_request", "PATCH body must include at least one field to update");
    }
    if (out.ok === "notfound") {
      return catalogJsonError(404, "not_found", "Asset not found");
    }
    if (out.ok === "conflict") {
      return catalogJsonError(409, "conflict", "Stale updated_at", { current: out.current });
    }
    return NextResponse.json(out.composite, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "patch" });
    }
    return responseFromPostgresError(e, "Patch asset failed");
  }
}
