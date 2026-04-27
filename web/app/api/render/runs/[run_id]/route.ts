/**
 * GET /api/render/runs/[run_id] — poll one render job.
 *
 * URL slug is `run_id` to match sibling `[run_id]/{identical,replay}` routes
 * (Next.js App Router forbids parallel slug names at the same path level).
 * The value addresses a `job_queue.job_id` row; on success the same UUID
 * appears as `render_run.run_id` (FK by convention — DEC-A26).
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
import { withAudit } from "@/lib/audit/with-audit";
import { getSql } from "@/lib/db/client";
import { renderError } from "@/lib/render/envelope";
import { RENDER_QUEUE_KIND } from "@/lib/render/enqueue";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "audit.read" },
  PATCH: { requires: "render.run" },
} as const;

type Ctx = { params: Promise<{ run_id: string }> };

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
  const { run_id } = await ctx.params;
  // Internally the same UUID is the `job_queue.job_id` we look up.
  const job_id = run_id;
  if (!UUID_RE.test(job_id)) {
    return renderError(400, "validation", "run_id must be a UUID", {
      details: ["run_id format invalid"],
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
    console.error("[render-api] GET /api/render/runs/[run_id] failed", e);
    return renderError(500, "internal", "Failed to read render job");
  }
}

/**
 * PATCH /api/render/runs/[run_id] — update `variant_disposition_json` (DEC-A41).
 *
 * Body: `{ variant_disposition_json: Record<string, "kept"|"discarded"|"saved"> }`
 *
 * Merges the supplied keys into the existing `render_run.variant_disposition_json`
 * column (jsonb concat). Used by the Stage 6.1 variant-grid Discard action.
 *
 * Capability: `render.run` (gated upstream by `proxy.ts`).
 *
 * Audit: emits `render.run.disposition_updated` with `{ run_id, disposition }`.
 */
type DispositionMap = Record<string, "kept" | "discarded" | "saved">;

const VALID_DISPOSITIONS = ["kept", "discarded", "saved"] as const;

function validateDispositionBody(body: unknown): { details: string[] } | DispositionMap {
  if (body === null || typeof body !== "object") return { details: ["body must be an object"] };
  const obj = body as Record<string, unknown>;
  const dm = obj.variant_disposition_json;
  if (dm === null || typeof dm !== "object" || Array.isArray(dm)) {
    return { details: ["variant_disposition_json must be an object"] };
  }
  const out: DispositionMap = {};
  for (const [k, v] of Object.entries(dm as Record<string, unknown>)) {
    if (!/^v?\d+$/.test(k)) return { details: [`invalid variant key: ${k}`] };
    if (typeof v !== "string" || !VALID_DISPOSITIONS.includes(v as DispositionMap[string])) {
      return { details: [`invalid disposition for ${k}: ${String(v)}`] };
    }
    out[k] = v as DispositionMap[string];
  }
  if (Object.keys(out).length === 0) return { details: ["variant_disposition_json must be non-empty"] };
  return out;
}

type PatchResponse = { run_id: string; variant_disposition_json: DispositionMap };

export async function PATCH(request: NextRequest, ctx: Ctx) {
  const { run_id } = await ctx.params;
  if (!UUID_RE.test(run_id)) {
    return renderError(400, "validation", "run_id must be a UUID", {
      details: ["run_id format invalid"],
    });
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return renderError(400, "validation", "Body must be valid JSON");
  }
  const v = validateDispositionBody(body);
  if ("details" in v) {
    return renderError(400, "validation", "Invalid request body", { details: v.details });
  }
  const incoming = v;

  const wrapped = withAudit<PatchResponse>(async (_request, { emit, sql }) => {
    const rows = await sql`
      update render_run
         set variant_disposition_json = variant_disposition_json || ${sql.json(incoming)}::jsonb
       where run_id = ${run_id}::uuid
       returning run_id::text as run_id, variant_disposition_json
    `;
    const updated = (rows as unknown as Array<{ run_id: string; variant_disposition_json: DispositionMap }>)[0];
    if (!updated) throw new Error("not_found: render_run not found");
    await emit("render.run.disposition_updated", "render_run", updated.run_id, {
      run_id: updated.run_id,
      disposition: incoming,
    });
    return {
      status: 200,
      data: { run_id: updated.run_id, variant_disposition_json: updated.variant_disposition_json },
    };
  });

  try {
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message.startsWith("not_found:")) {
      return renderError(404, "not_found", "Render run not found");
    }
    console.error("[render-api] PATCH /api/render/runs/[run_id] failed", e);
    return renderError(500, "internal", "Failed to update render run");
  }
}
