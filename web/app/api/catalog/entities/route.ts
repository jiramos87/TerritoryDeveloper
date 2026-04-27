/**
 * Entity catalog search endpoint for `<EntityRefPicker>` (TECH-1787).
 *
 * GET /api/catalog/entities?kind=sprite[,token]&q=...&limit=50
 *   -> { items: EntityRefSearchRow[] }
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1787 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { searchEntitiesForPicker, validateKindList } from "@/lib/catalog/entity-search";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

const DEFAULT_LIMIT = 50;
const MAX_LIMIT = 200;

export async function GET(request: NextRequest) {
  const params = request.nextUrl.searchParams;
  const kindRaw = params.get("kind") ?? "";
  if (kindRaw === "") {
    return catalogJsonError(400, "bad_request", "kind query param required");
  }
  const kinds = validateKindList(kindRaw);
  if (!kinds) {
    return catalogJsonError(400, "bad_request", "kind must be a comma-separated list of valid kinds");
  }
  const q = params.get("q");
  const limitRaw = params.get("limit");
  let limit = DEFAULT_LIMIT;
  if (limitRaw !== null) {
    const n = Number.parseInt(limitRaw, 10);
    if (!Number.isInteger(n) || n <= 0 || n > MAX_LIMIT) {
      return catalogJsonError(400, "bad_request", `limit must be a positive integer ≤ ${MAX_LIMIT}`);
    }
    limit = n;
  }
  try {
    const items = await searchEntitiesForPicker({ kinds, q, limit });
    return NextResponse.json({ ok: true, data: { items } }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "entities-search" });
    }
    return responseFromPostgresError(e, "Entity search failed");
  }
}
