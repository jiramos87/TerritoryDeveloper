/**
 * Archetype list + create (TECH-2459 / Stage 11.1).
 *
 *  GET  /api/catalog/archetypes?status=active|retired|all&limit=50&cursor=...
 *  POST /api/catalog/archetypes body: CatalogArchetypeCreateBody
 *
 * Envelope per DEC-A48: `{ ok:true, data, audit_id }` on mutate.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2459 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  createArchetype,
  listArchetypes,
  type ArchetypeListFilter,
} from "@/lib/catalog/archetype-repo";
import type { CatalogArchetypeCreateBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  POST: { requires: "catalog.entity.create" },
} as const;

const DEFAULT_LIMIT = 200;
const MAX_LIMIT = 500;

export async function GET(request: NextRequest) {
  const params = request.nextUrl.searchParams;
  const statusRaw = params.get("status") ?? "active";
  if (statusRaw !== "active" && statusRaw !== "retired" && statusRaw !== "all") {
    return catalogJsonError(400, "bad_request", "status must be one of active|retired|all");
  }
  const filter = statusRaw as ArchetypeListFilter;

  const limitRaw = params.get("limit");
  let limit = DEFAULT_LIMIT;
  if (limitRaw !== null) {
    const n = Number.parseInt(limitRaw, 10);
    if (!Number.isInteger(n) || n <= 0 || n > MAX_LIMIT) {
      return catalogJsonError(
        400,
        "bad_request",
        `limit must be a positive integer <= ${MAX_LIMIT}`,
      );
    }
    limit = n;
  }
  const cursorRaw = params.get("cursor");
  if (cursorRaw !== null && !/^\d+$/.test(cursorRaw)) {
    return catalogJsonError(400, "bad_request", "cursor must be a non-negative integer string");
  }
  try {
    const out = await listArchetypes({ filter, limit, cursor: cursorRaw });
    return NextResponse.json({ ok: true, data: out }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "archetypes-list",
      });
    }
    return responseFromPostgresError(e, "Archetype list query failed");
  }
}

const wrappedPost = withAudit(async (request, { emit, sql }) => {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    throw new Error("validation: Invalid JSON body");
  }
  const result = await createArchetype(body as CatalogArchetypeCreateBody, sql);
  if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
  if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
  if (result.ok === "notfound") throw new Error("internal: unexpected notfound on create");
  await emit("catalog.archetype.create", "catalog_entity", result.data.entity_id, {
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
      return catalogJsonError(
        409,
        reason === "duplicate_slug" ? "unique_violation" : "conflict",
        `Archetype ${reason}`,
      );
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "archetypes-post",
      });
    }
    return responseFromPostgresError(e, "Create archetype failed");
  }
}
