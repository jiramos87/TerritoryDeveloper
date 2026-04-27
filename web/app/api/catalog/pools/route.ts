/**
 * Pool list + create (TECH-1788).
 *
 *  GET  /api/catalog/pools?status=active|retired|all&limit=50&cursor=...
 *  POST /api/catalog/pools body: CatalogPoolCreateBody
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  createPoolSpine,
  listPoolsSpine,
  type PoolSpineListFilter,
} from "@/lib/catalog/pool-spine-repo";
import type { CatalogPoolCreateBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  POST: { requires: "catalog.entity.create" },
} as const;

const DEFAULT_LIMIT = 50;
const MAX_LIMIT = 200;

export async function GET(request: NextRequest) {
  const params = request.nextUrl.searchParams;
  const statusRaw = params.get("status") ?? "active";
  if (statusRaw !== "active" && statusRaw !== "retired" && statusRaw !== "all") {
    return catalogJsonError(400, "bad_request", "status must be one of active|retired|all");
  }
  const filter = statusRaw as PoolSpineListFilter;
  const limitRaw = params.get("limit");
  let limit = DEFAULT_LIMIT;
  if (limitRaw !== null) {
    const n = Number.parseInt(limitRaw, 10);
    if (!Number.isInteger(n) || n <= 0 || n > MAX_LIMIT) {
      return catalogJsonError(400, "bad_request", `limit must be a positive integer ≤ ${MAX_LIMIT}`);
    }
    limit = n;
  }
  const cursorRaw = params.get("cursor");
  if (cursorRaw !== null && !/^\d+$/.test(cursorRaw)) {
    return catalogJsonError(400, "bad_request", "cursor must be a non-negative integer string");
  }
  try {
    const out = await listPoolsSpine({ filter, limit, cursor: cursorRaw });
    return NextResponse.json({ ok: true, data: out }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "pools-list" });
    }
    return responseFromPostgresError(e, "Pool list query failed");
  }
}

const wrappedPost = withAudit(async (request, { emit, sql }) => {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    throw new Error("validation: Invalid JSON body");
  }
  const result = await createPoolSpine(body as CatalogPoolCreateBody, sql);
  if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
  if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
  if (result.ok === "notfound") throw new Error("internal: unexpected notfound on create");
  await emit("catalog.pool.create", "catalog_entity", result.data.entity_id, {
    slug: result.data.slug,
  });
  return { status: 201, data: result.data };
});

export async function POST(request: NextRequest) {
  try {
    return await wrappedPost(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      return catalogJsonError(409, reason === "duplicate_slug" ? "unique_violation" : "conflict", `Pool ${reason}`);
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "pools-post" });
    }
    return responseFromPostgresError(e, "Create pool failed");
  }
}
