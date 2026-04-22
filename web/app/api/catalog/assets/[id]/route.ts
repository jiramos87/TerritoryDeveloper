import { NextResponse, type NextRequest } from "next/server";
import { loadCatalogAssetById } from "@/lib/catalog/fetch-asset-composite";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";

export const dynamic = "force-dynamic";

type Ctx = { params: Promise<{ id: string }> };

/**
 * @see `ia/projects/TECH-641.md` — `GET /api/catalog/assets/:id`
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
