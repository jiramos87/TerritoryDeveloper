/**
 * Sprite catalog list + create (TECH-1675).
 *
 * GET  /api/catalog/sprites?status=active|retired|all&limit=50&cursor=...
 *      -> { items, next_cursor }
 * POST /api/catalog/sprites
 *      body: { slug, display_name, tags?, sprite_detail?, source_run_id?, source_variant_idx? }
 *      -> 201 + { entity_id, slug } via DEC-A48 envelope; 409 on duplicate slug.
 *
 * Capability: catalog.entity.create (gated upstream by proxy.ts).
 *
 * @see ia/projects/asset-pipeline/stage-6.1.md — TECH-1675 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  createSprite,
  listSprites,
  type CreateSpriteBody,
  type SpriteListFilter,
} from "@/lib/db/sprite-repo";

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
  const filter = statusRaw as SpriteListFilter;
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
    const out = await listSprites({ filter, limit, cursor: cursorRaw });
    return NextResponse.json({ ok: true, data: out }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "sprites-list" });
    }
    return responseFromPostgresError(e, "Sprite list query failed");
  }
}

const wrappedPost = withAudit<{ entity_id: string; slug: string }>(async (request, { emit, sql }) => {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    throw new Error("validation: Invalid JSON body");
  }
  const result = await createSprite(body as CreateSpriteBody, sql);
  if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
  if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
  if (result.ok === "notfound") throw new Error("internal: unexpected notfound on create");

  await emit("catalog.sprite.create", "catalog_entity", result.data.entity_id, {
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
      return catalogJsonError(409, reason === "duplicate_slug" ? "unique_violation" : "conflict", `Sprite ${reason}`);
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "sprites-post" });
    }
    return responseFromPostgresError(e, "Create sprite failed");
  }
}

// Tree-shake guard: keep getSql import live so static analysers don't strip it
// from build output (lib/db/client side-effect imports the postgres driver).
void getSql;
