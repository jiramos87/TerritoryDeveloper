/**
 * POST /api/render/runs — enqueue one render job.
 *
 * Body:
 *   { archetype_id: uuid, archetype_version_id: uuid, params_json: object }
 *
 * Headers:
 *   Idempotency-Key (optional) — if present and a prior `(actor_user_id,
 *   idempotency_key)` row exists in `job_queue` within 24 h, that prior
 *   `job_id` is returned and no second row is inserted (DEC-A48).
 *
 * Capability: `render.run` (gated upstream by `proxy.ts` via
 * `route-meta-map.ts`). The handler still trusts that gate and does not
 * re-enforce capability here.
 *
 * Backpressure: cap of 50 active queued render_run jobs returns 429
 * with `retry_hint.after_seconds = 30` (DEC-A40).
 *
 * Audit: emits `render.run.enqueued` with payload
 *   `{ job_id, archetype_id, archetype_version_id, params_hash }`
 * inside the same tx as the insert (DEC-A33 + DEC-A48).
 */
import { type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { getSessionUser } from "@/lib/auth/get-session";
import {
  enqueueRenderJob,
  RETRY_HINT_AFTER_SECONDS,
} from "@/lib/render/enqueue";
import { renderError } from "@/lib/render/envelope";
import { validateRenderRunBody } from "@/lib/render/validate-body";

export const dynamic = "force-dynamic";
export const routeMeta = { POST: { requires: "render.run" } } as const;

type EnqueueResponse = { job_id: string };

const wrappedPost = withAudit<EnqueueResponse>(async (request, { emit, sql }) => {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    throw new Error("validation: body must be valid JSON");
  }
  const v = validateRenderRunBody(body);
  if (v) throw new Error(`validation: ${JSON.stringify(v.details)}`);

  const session = await getSessionUser();
  if (!session) {
    // Capability proxy already 403s when there is no session; reaching here
    // would be a misconfiguration. Surface as forbidden rather than crash.
    throw new Error("forbidden: no session user");
  }

  const idempotency_key = request.headers.get("Idempotency-Key") ?? null;
  const b = body as {
    archetype_id: string;
    archetype_version_id: string;
    params_json: Record<string, unknown>;
  };
  const result = await enqueueRenderJob(sql, {
    actor_user_id: session.id,
    idempotency_key,
    payload: {
      archetype_id: b.archetype_id,
      archetype_version_id: b.archetype_version_id,
      params_json: b.params_json,
    },
  });

  if (result.kind === "queue_full") {
    throw new Error(
      `queue_full: ${JSON.stringify({ active_count: result.active_count })}`,
    );
  }

  // Inserted or idempotent_replay — both return the job_id; only fresh
  // inserts emit a new audit row (replays are observed via the prior row).
  if (result.kind === "inserted") {
    await emit("render.run.enqueued", "job_queue", result.job_id, {
      job_id: result.job_id,
      archetype_id: b.archetype_id,
      archetype_version_id: b.archetype_version_id,
      params_hash: result.params_hash,
    });
  }

  return { status: 200, data: { job_id: result.job_id } };
});

export async function POST(request: NextRequest) {
  try {
    return await wrappedPost(request);
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
    console.error("[render-api] POST /api/render/runs failed", e);
    return renderError(500, "internal", "Failed to enqueue render run");
  }
}
