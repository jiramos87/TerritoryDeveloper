/**
 * POST /api/render/runs/[run_id]/identical — enqueue a verbatim re-render
 * of a prior `render_run` row (DEC-A26 identical re-render).
 *
 * Path segment is the SOURCE `run_id`. The new job inherits source's
 * `archetype_id` + `archetype_version_id` + `params_json` byte-for-byte
 * with no client-side override capability. Body MUST be empty.
 *
 * Errors:
 *   400 validation        — body not empty, bad path-segment uuid
 *   403 forbidden         — no session
 *   404 not_found         — source render_run absent
 *   409 conflict          — source archetype version retired (DEC-A23)
 *   429 queue_full        — backpressure cap (DEC-A40), retry_hint=30s
 *   500 internal          — unexpected error
 *
 * Audit: `render.run.identical_enqueued` with payload
 *   `{ source_run_id, parent_run_id, archetype_id, archetype_version_id,
 *      params_hash }`
 *
 * Capability: `render.run` (gated upstream by `proxy.ts`).
 */
import { type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { getSessionUser } from "@/lib/auth/get-session";
import {
  enqueueRenderJob,
  RETRY_HINT_AFTER_SECONDS,
} from "@/lib/render/enqueue";
import { renderError } from "@/lib/render/envelope";
import { validateIdenticalBody } from "@/lib/render/validate-body";
import { isArchetypeVersionRetired } from "@/lib/render/archetype-retired";

export const dynamic = "force-dynamic";
export const routeMeta = { POST: { requires: "render.run" } } as const;

type Ctx = { params: Promise<{ run_id: string }> };

const UUID_RE = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

type EnqueueResponse = { job_id: string };

type SourceRow = {
  run_id: string;
  archetype_id: string;
  archetype_version_id: string;
  params_json: Record<string, unknown>;
};

function makeHandler(run_id: string) {
  return withAudit<EnqueueResponse>(async (request, { emit, sql }) => {
    if (!UUID_RE.test(run_id)) {
      throw new Error(
        `validation: ${JSON.stringify(["run_id must be a UUID"])}`,
      );
    }

    let body: unknown = null;
    const raw = await request.text();
    if (raw && raw.length > 0) {
      try {
        body = JSON.parse(raw);
      } catch {
        throw new Error("validation: body must be valid JSON");
      }
    }
    const v = validateIdenticalBody(body);
    if (v) throw new Error(`validation: ${JSON.stringify(v.details)}`);

    const session = await getSessionUser();
    if (!session) {
      throw new Error("forbidden: no session user");
    }

    const sourceRows = await sql`
      select run_id::text as run_id,
             archetype_id::text as archetype_id,
             archetype_version_id::text as archetype_version_id,
             params_json
        from render_run
       where run_id = ${run_id}::uuid
       limit 1
    `;
    const source = (sourceRows as unknown as SourceRow[])[0];
    if (!source) {
      throw new Error("not_found: source render_run not found");
    }

    if (await isArchetypeVersionRetired(sql, source.archetype_version_id)) {
      throw new Error("conflict: archetype_retired");
    }

    const idempotency_key = request.headers.get("Idempotency-Key") ?? null;

    const result = await enqueueRenderJob(sql, {
      actor_user_id: session.id,
      idempotency_key,
      payload: {
        archetype_id: source.archetype_id,
        archetype_version_id: source.archetype_version_id,
        params_json: source.params_json,
        parent_run_id: source.run_id,
        mode: "identical",
      },
    });

    if (result.kind === "queue_full") {
      throw new Error(
        `queue_full: ${JSON.stringify({ active_count: result.active_count })}`,
      );
    }

    if (result.kind === "inserted") {
      await emit("render.run.identical_enqueued", "job_queue", result.job_id, {
        source_run_id: source.run_id,
        parent_run_id: source.run_id,
        archetype_id: source.archetype_id,
        archetype_version_id: source.archetype_version_id,
        params_hash: result.params_hash,
      });
    }

    return { status: 200, data: { job_id: result.job_id } };
  });
}

export async function POST(request: NextRequest, ctx: Ctx) {
  try {
    const { run_id } = await ctx.params;
    const wrapped = makeHandler(run_id);
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      const raw = e.message.replace(/^validation:\s*/i, "");
      let details: unknown = raw;
      try {
        details = JSON.parse(raw);
      } catch {
        details = [raw];
      }
      return renderError(400, "validation", "Invalid request body", { details });
    }
    if (e instanceof Error && e.message?.startsWith("not_found:")) {
      return renderError(404, "not_found", e.message.replace(/^not_found:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      return renderError(
        409,
        "conflict",
        e.message.replace(/^conflict:\s*/i, ""),
      );
    }
    if (e instanceof Error && e.message?.startsWith("queue_full:")) {
      const raw = e.message.replace(/^queue_full:\s*/i, "");
      let details: unknown = {};
      try {
        details = JSON.parse(raw);
      } catch {
        // best effort
      }
      return renderError(429, "queue_full", "Render queue at capacity", {
        details,
        retry_hint: { after_seconds: RETRY_HINT_AFTER_SECONDS },
      });
    }
    if (e instanceof Error && e.message?.startsWith("forbidden:")) {
      return renderError(403, "forbidden", e.message.replace(/^forbidden:\s*/i, ""));
    }
    console.error("[render-api] POST /api/render/runs/[run_id]/identical failed", e);
    return renderError(500, "internal", "Failed to enqueue identical run");
  }
}
