/**
 * enqueue.ts — shared render-job enqueue helper.
 *
 * Used by `/api/render/runs` POST (TECH-1469) and the replay / identical
 * re-render endpoints (TECH-1470). Centralises:
 *   - idempotency-key short-circuit (per-actor 24h window — partial unique
 *     index from migration 0027 makes the lookup sub-ms),
 *   - backpressure cap (DEC-A40 — 50 active queued render_run jobs returns
 *     429 with retry_hint.after_seconds=30),
 *   - canonical params_hash (sha256 of canonical-JSON body) for audit payload
 *     parity with the worker's later insert into render_run,
 *   - transactional INSERT into job_queue.
 *
 * @see DEC-A40 single-FIFO render queue
 * @see DEC-A48 mutate envelope (idempotency-key window + retry_hint)
 */
import { createHash } from "node:crypto";
import type { Sql } from "postgres";

export const RENDER_QUEUE_KIND = "render_run";
export const BACKPRESSURE_CAP = 50;
export const IDEMPOTENCY_WINDOW_HOURS = 24;
export const RETRY_HINT_AFTER_SECONDS = 30;

export type RenderJobPayload = {
  archetype_id: string;
  archetype_version_id: string;
  params_json: Record<string, unknown>;
  parent_run_id?: string | null;
  mode?: "standard" | "replay" | "identical";
};

export type EnqueueResult =
  | { kind: "inserted"; job_id: string; params_hash: string }
  | { kind: "idempotent_replay"; job_id: string; params_hash: string }
  | { kind: "queue_full"; active_count: number };

/**
 * Stable JSON serialisation — sorts object keys recursively so the same
 * params hash regardless of property-order noise in the request body.
 */
export function canonicalJson(value: unknown): string {
  if (value === null || typeof value !== "object") return JSON.stringify(value);
  if (Array.isArray(value)) {
    return "[" + value.map((v) => canonicalJson(v)).join(",") + "]";
  }
  const obj = value as Record<string, unknown>;
  const keys = Object.keys(obj).sort();
  return (
    "{" +
    keys.map((k) => JSON.stringify(k) + ":" + canonicalJson(obj[k])).join(",") +
    "}"
  );
}

export function paramsHashFor(payload: RenderJobPayload): string {
  return createHash("sha256").update(canonicalJson(payload.params_json)).digest("hex");
}

/**
 * Insert one row into `job_queue` (kind='render_run') inside `tx`.
 *
 * - When `idempotency_key` is provided and a prior row matches
 *   `(actor_user_id, idempotency_key)` within 24 h, the prior `job_id` is
 *   returned (no new row inserted).
 * - When the active `queued` count of kind='render_run' is >= 50, returns
 *   `{ kind: 'queue_full', active_count }` so the caller can map it to a 429
 *   envelope. Backpressure check is in-tx so two concurrent requests cannot
 *   both pass the cap.
 */
export async function enqueueRenderJob(
  tx: Sql,
  args: {
    actor_user_id: string;
    payload: RenderJobPayload;
    idempotency_key?: string | null;
  },
): Promise<EnqueueResult> {
  const params_hash = paramsHashFor(args.payload);

  // Idempotency short-circuit — partial unique index covers the lookup.
  if (args.idempotency_key) {
    const existing = await tx`
      select job_id::text as job_id
        from job_queue
       where actor_user_id = ${args.actor_user_id}
         and idempotency_key = ${args.idempotency_key}
         and enqueued_at > now() - (${IDEMPOTENCY_WINDOW_HOURS} || ' hours')::interval
       limit 1
    `;
    const row = (existing as unknown as Array<{ job_id: string }>)[0];
    if (row) {
      return { kind: "idempotent_replay", job_id: row.job_id, params_hash };
    }
  }

  // Backpressure check — same tx so two concurrent inserts cannot both pass.
  const counts = await tx`
    select count(*)::int as active_count
      from job_queue
     where kind = ${RENDER_QUEUE_KIND}
       and status = 'queued'
  `;
  const active_count = (counts as unknown as Array<{ active_count: number }>)[0]?.active_count ?? 0;
  if (active_count >= BACKPRESSURE_CAP) {
    return { kind: "queue_full", active_count };
  }

  const payloadWithMode: RenderJobPayload = {
    archetype_id: args.payload.archetype_id,
    archetype_version_id: args.payload.archetype_version_id,
    params_json: args.payload.params_json,
    mode: args.payload.mode ?? "standard",
    ...(args.payload.parent_run_id ? { parent_run_id: args.payload.parent_run_id } : {}),
  };

  const inserted = await tx`
    insert into job_queue (kind, status, payload_json, actor_user_id, idempotency_key)
    values (
      ${RENDER_QUEUE_KIND},
      'queued',
      ${tx.json(payloadWithMode as never)},
      ${args.actor_user_id},
      ${args.idempotency_key ?? null}
    )
    returning job_id::text as job_id
  `;
  const row = (inserted as unknown as Array<{ job_id: string }>)[0]!;
  return { kind: "inserted", job_id: row.job_id, params_hash };
}
