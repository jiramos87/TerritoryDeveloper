/**
 * Dashboard widget — snapshot freshness per kind (TECH-4183 / Stage 15.1).
 *
 * GET /api/catalog/dashboard/snapshot-freshness
 *   -> 200 { items: SnapshotFreshnessRow[] }
 *
 * Returns latest catalog_snapshot.created_at per entity kind extracted
 * from entity_counts_json keys. Max 8 rows. Flags entries older than 24h.
 */

import { NextResponse } from "next/server";

import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.read" },
} as const;

export type SnapshotFreshnessRow = {
  kind: string;
  latest_at: string;
  stale: boolean;
};

export type SnapshotFreshnessResponse = { items: SnapshotFreshnessRow[] };

const STALE_THRESHOLD_MS = 24 * 60 * 60 * 1000;

export async function GET() {
  try {
    const sql = getSql();
    const rows = (await sql`
      select
        key as kind,
        max(created_at) as latest_at
      from catalog_snapshot,
           jsonb_object_keys(entity_counts_json) as key
      where status = 'active'
      group by key
      order by latest_at desc
      limit 8
    `) as Array<{ kind: string; latest_at: Date }>;
    const now = Date.now();
    const items: SnapshotFreshnessRow[] = rows.map((r) => ({
      kind: r.kind,
      latest_at: r.latest_at.toISOString(),
      stale: now - r.latest_at.getTime() > STALE_THRESHOLD_MS,
    }));
    const body: SnapshotFreshnessResponse = { items };
    return NextResponse.json({ ok: true, data: body }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "dashboard-snapshot-freshness" });
    }
    return responseFromPostgresError(e, "Snapshot freshness query failed");
  }
}
