/**
 * GET /api/render/runs/[job_id] — poll one render job.
 *
 * Returns DEC-A48 envelope:
 *   { ok: true, data: {
 *       job_id, status,
 *       queue_position?, started_at?, finished_at?, error?, run_id?
 *     } }
 *
 * - `queue_position` is 1-based, only present when `status='queued'`.
 *   Computed as the count of earlier-enqueued queued render_run rows + 1.
 * - `run_id` is present only when `status='done'` — joined from
 *   `render_run` (FK by convention; same job_id key).
 * - `failed` rows surface `error` text + `retry_hint.after_seconds=0` so
 *   the UI re-enqueues immediately.
 *
 * Capability: `audit.read` (gated upstream by `proxy.ts`).
 */
import { type NextRequest, NextResponse } from "next/server";
import { getSql } from "@/lib/db/client";
import { renderError } from "@/lib/render/envelope";
import { RENDER_QUEUE_KIND } from "@/lib/render/enqueue";

export const dynamic = "force-dynamic";
export const routeMeta = { GET: { requires: "audit.read" } } as const;

type Ctx = { params: Promise<{ job_id: string }> };

const UUID_RE = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

type JobQueueRow = {
  job_id: string;
  status: "queued" | "running" | "done" | "failed";
  enqueued_at: string;
  started_at: string | null;
  finished_at: string | null;
  error: string | null;
};

type RenderRunRow = { run_id: string };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { job_id } = await ctx.params;
  if (!UUID_RE.test(job_id)) {
    return renderError(400, "validation", "job_id must be a UUID", {
      details: ["job_id format invalid"],
    });
  }

  const sql = getSql();
  try {
    const rows = await sql`
      select job_id::text as job_id,
             status,
             enqueued_at,
             started_at,
             finished_at,
             error
        from job_queue
       where job_id = ${job_id}::uuid
         and kind = ${RENDER_QUEUE_KIND}
       limit 1
    `;
    const row = (rows as unknown as JobQueueRow[])[0];
    if (!row) {
      return renderError(404, "not_found", "Render job not found");
    }

    const data: {
      job_id: string;
      status: JobQueueRow["status"];
      queue_position?: number;
      started_at?: string;
      finished_at?: string;
      error?: string;
      run_id?: string;
      retry_hint?: { after_seconds: number };
    } = {
      job_id: row.job_id,
      status: row.status,
    };

    if (row.started_at) data.started_at = row.started_at;
    if (row.finished_at) data.finished_at = row.finished_at;

    if (row.status === "queued") {
      const posRows = await sql`
        select count(*)::int as ahead
          from job_queue
         where kind = ${RENDER_QUEUE_KIND}
           and status = 'queued'
           and enqueued_at < ${row.enqueued_at}
      `;
      const ahead = (posRows as unknown as Array<{ ahead: number }>)[0]?.ahead ?? 0;
      data.queue_position = ahead + 1;
    }

    if (row.status === "done") {
      // render_run is the success-side provenance row (DEC-A26). The worker
      // writes one render_run per successful job; the linkage is
      // `render_run.run_id = job_queue.job_id` (FK by convention — see
      // migration 0027 column comments).
      const rrRows = await sql`
        select run_id::text as run_id
          from render_run
         where run_id = ${job_id}::uuid
         limit 1
      `;
      const rr = (rrRows as unknown as RenderRunRow[])[0];
      if (rr) data.run_id = rr.run_id;
    }

    if (row.status === "failed") {
      data.error = row.error ?? "unknown error";
      data.retry_hint = { after_seconds: 0 };
    }

    return NextResponse.json({ ok: true, data }, { status: 200 });
  } catch (e) {
    console.error("[render-api] GET /api/render/runs/[job_id] failed", e);
    return renderError(500, "internal", "Failed to read render job");
  }
}
