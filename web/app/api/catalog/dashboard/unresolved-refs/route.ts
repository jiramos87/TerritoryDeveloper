/**
 * Dashboard widget — unresolved catalog ref count (TECH-4183 / Stage 15.1).
 *
 * GET /api/catalog/dashboard/unresolved-refs
 *   -> 200 { count: number }
 *
 * Counts `catalog_ref_edge` rows whose dst_id points at a retired or missing
 * `catalog_entity` (DEC-A42). Reuses `catalog.entity.read` capability gate.
 */

import { NextResponse } from "next/server";

import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.read" },
} as const;

export type UnresolvedRefsResponse = { count: number };

export async function GET() {
  try {
    const sql = getSql();
    const rows = (await sql`
      select count(*)::int as count
      from catalog_ref_edge cre
      left join catalog_entity ce on ce.id = cre.dst_id
      where ce.id is null or ce.retired_at is not null
    `) as Array<{ count: number }>;
    const count = rows[0]?.count ?? 0;
    const body: UnresolvedRefsResponse = { count };
    return NextResponse.json({ ok: true, data: body }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "dashboard-unresolved-refs" });
    }
    return responseFromPostgresError(e, "Unresolved refs query failed");
  }
}
