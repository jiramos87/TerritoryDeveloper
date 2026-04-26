import { NextResponse, type NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";
import { mapRowToCatalogAsset } from "@/lib/catalog/row-mappers";
import { parseListQueryParams } from "@/lib/catalog/parse-list-query";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import type { CatalogCreateAssetBody } from "@/types/api/catalog-api";
import {
  createCatalogAssetTransaction,
  getCreatedResponse,
  validateCreateBody,
} from "@/lib/catalog/create-asset";
import { withAudit } from "@/lib/audit/with-audit";
export const dynamic = "force-dynamic";
export const routeMeta = { GET: { requires: 'catalog.entity.create' }, POST: { requires: 'catalog.entity.create' } } as const;

type Sql = ReturnType<typeof getSql>;

function statusFragment(sql: Sql, opts: { includeDraft: boolean; statusFilter: string | null }) {
  if (opts.statusFilter) {
    return sql` status = ${opts.statusFilter} `;
  }
  if (opts.includeDraft) {
    return sql` (status in ('draft', 'published')) `;
  }
  return sql` status = 'published' `;
}

/**
 * @see `ia/rules/web-backend-logic.md#pagination-contract` — `GET /api/catalog/assets`
 */
export async function GET(request: NextRequest) {
  const parsed = parseListQueryParams(request.nextUrl.searchParams);
  if (!parsed.ok) {
    return parsed.response;
  }
  const { includeDraft, statusFilter, category, limit, cursor } = parsed;
  const sql = getSql();
  try {
    const andCategory = category ? sql` and category = ${category} ` : sql``;
    // Keyset on bigserial: cursor validated `^\d+$` in `parseListQueryParams`; `Number` exact for 53-bit id space.
    const andCursor =
      cursor != null && cursor.length > 0 ? sql` and id > ${Number.parseInt(cursor, 10)}` : sql``;
    const statusCond = statusFragment(sql, { includeDraft, statusFilter: statusFilter ?? null });
    const rows = await sql`
      select id, category, slug, display_name, status, replaced_by, footprint_w, footprint_h,
             placement_mode, unlocks_after, has_button, updated_at
      from catalog_asset
      where
        ${statusCond} ${andCategory} ${andCursor}
      order by id asc
      limit ${limit}
    `;
    const list = (rows as unknown as Record<string, unknown>[]).map((r) => mapRowToCatalogAsset(r as never));
    const nextCursor = list.length === limit ? list[list.length - 1]?.id ?? null : null;
    return NextResponse.json(
      { assets: list, next_cursor: nextCursor, limit },
      { status: 200 },
    );
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "get" });
    }
    return responseFromPostgresError(e, "List query failed");
  }
}

/**
 * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `POST /api/catalog/assets`
 *
 * Wrapped with `withAudit` (TECH-1351) — emits `catalog.asset.create` audit
 * row inside the same tx as the mutation. Response envelope per DEC-A48:
 *   `{ ok: true, data, audit_id }`.
 */
const wrappedPost = withAudit<unknown>(async (request, { emit, sql }) => {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    throw new Error("validation: Invalid JSON body");
  }
  const v = validateCreateBody(body);
  if (v) throw new Error(`validation: ${v}`);
  const id = await createCatalogAssetTransaction(body as CatalogCreateAssetBody, sql);
  await emit("catalog.asset.create", "catalog_asset", id, {
    slug: (body as CatalogCreateAssetBody).slug,
  });
  const dto = await getCreatedResponse(id, sql);
  if (dto === "badid" || dto === "notfound") {
    throw new Error("internal: Read-after-create failed");
  }
  return { status: 201, data: dto };
});

export async function POST(request: NextRequest) {
  try {
    return await wrappedPost(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("internal:")) {
      return catalogJsonError(500, "internal", e.message.replace(/^internal:\s*/i, ""), {
        logContext: "post",
      });
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "post" });
    }
    return responseFromPostgresError(e, "Create asset failed");
  }
}
