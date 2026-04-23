import { NextResponse, type NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";
import { loadCatalogAssetById } from "@/lib/catalog/fetch-asset-composite";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import type { CatalogRetireBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";

type Ctx = { params: Promise<{ id: string }> };

/**
 * @see `ia/rules/web-backend-logic.md#retire-idempotency` — `POST /api/catalog/assets/:id/retire`
 */
export async function POST(request: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;
  if (!/^\d{1,32}$/.test(id) || !Number.isSafeInteger(Number(id))) {
    return catalogJsonError(400, "bad_request", "Invalid asset id");
  }
  const idNum = Number.parseInt(id, 10);
  let body: CatalogRetireBody = {};
  try {
    body = (await request.json()) as CatalogRetireBody;
  } catch {
    // empty body ok
  }
  const repRaw = body.replaced_by;
  const rep: number | null =
    repRaw == null || repRaw === "" ? null : Number.parseInt(String(repRaw), 10);
  if (rep != null && (Number.isNaN(rep) || rep < 1)) {
    return catalogJsonError(400, "bad_request", "replaced_by must be a valid asset id");
  }
  if (rep != null && rep === idNum) {
    return catalogJsonError(400, "bad_request", "replaced_by must differ from this asset");
  }
  const sql = getSql();
  try {
    if (rep != null) {
      const ex = await sql`select status from catalog_asset where id = ${rep} limit 1`;
      if (ex.length === 0) {
        return catalogJsonError(409, "conflict", "replaced_by asset not found");
      }
      if ((ex[0] as { status: string }).status === "retired") {
        return catalogJsonError(409, "conflict", "replaced_by asset is retired");
      }
    }
    const [r] = await sql`
      update catalog_asset
      set status = 'retired', replaced_by = ${rep}, updated_at = now()
      where id = ${idNum}
      returning id
    `;
    if (!r) {
      return catalogJsonError(404, "not_found", "Asset not found");
    }
    const out = await loadCatalogAssetById(id, { includeRetired: true });
    if (out === "notfound" || out === "badid") {
      return catalogJsonError(500, "internal", "Read after retire failed", { logContext: "retire" });
    }
    return NextResponse.json(out, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "retire" });
    }
    return responseFromPostgresError(e, "Retire failed");
  }
}
