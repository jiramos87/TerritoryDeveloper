/**
 * Dashboard widget — publish queue depth (TECH-4183 / Stage 15.1).
 *
 * GET /api/catalog/dashboard/queue-depth
 *   -> 200 { queued: number; running: number; total: number }
 *
 * Counts job_queue rows with status IN ('queued','running') for render_run
 * and snapshot_rebuild kinds, grouped by status.
 */

import { NextResponse } from "next/server";

import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.read" },
} as const;

export type QueueDepthResponse = { queued: number; running: number; total: number };

export async function GET() {
  try {
    const sql = getSql();
    const rows = (await sql`
      select status, count(*)::int as count
      from job_queue
      where status in ('queued', 'running')
        and kind in ('render_run', 'snapshot_rebuild')
      group by status
    `) as Array<{ status: string; count: number }>;
    let queued = 0;
    let running = 0;
    for (const r of rows) {
      if (r.status === "queued") queued = r.count;
      else if (r.status === "running") running = r.count;
    }
    const body: QueueDepthResponse = { queued, running, total: queued + running };
    return NextResponse.json({ ok: true, data: body }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "dashboard-queue-depth" });
    }
    return responseFromPostgresError(e, "Queue depth query failed");
  }
}
