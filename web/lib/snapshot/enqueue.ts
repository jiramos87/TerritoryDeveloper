/**
 * `enqueueSnapshotRebuild` — TECH-2674 / asset-pipeline Stage 13.1.
 *
 * Inserts one `job_queue` row with `kind='snapshot_rebuild'` carrying the
 * caller-provided trigger payload. Designed to run inside an existing
 * `withAudit` transaction so the publish-side enqueue rolls back together
 * with the audit row + entity_version freeze on any tx error.
 *
 * Trigger shape (per DEC-A39 + spec §2.1 #1):
 *   - `trigger='publish'` → carries `source_kind` + `source_entity_id`
 *     + `source_version_id` (auto-trigger after publish freeze).
 *   - `trigger='manual'`  → no `source_*` fields (operator-initiated export).
 *
 * Worker dequeue + execution lives in TECH-2675 + future stages; this lib
 * is write-side only.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import type { Sql } from "postgres";

/** Discriminator literal — matches `job_queue.kind` filter in worker. */
export const SNAPSHOT_REBUILD_KIND = "snapshot_rebuild" as const;

/** Trigger kinds carried in `payload_json.trigger`. */
export type SnapshotRebuildTrigger = "publish" | "manual";

/**
 * Payload shape persisted to `job_queue.payload_json`. `source_*` fields are
 * required when `trigger='publish'` and absent when `trigger='manual'`.
 */
export type SnapshotRebuildPayload =
  | {
      trigger: "publish";
      source_kind: string;
      source_entity_id: string;
      source_version_id: string;
    }
  | { trigger: "manual" };

export type EnqueueSnapshotRebuildResult = { jobId: string };

/**
 * Insert one `job_queue` row carrying the snapshot-rebuild payload. Returns
 * the generated `job_id` for caller logging / audit linkage.
 *
 * Caller owns the SQL client (typically the tx client passed by `withAudit`).
 * This function does NOT begin / commit a tx; mutations participate in the
 * caller's tx.
 */
export async function enqueueSnapshotRebuild(
  client: Sql,
  payload: SnapshotRebuildPayload,
): Promise<EnqueueSnapshotRebuildResult> {
  const inserted = (await client`
    insert into job_queue (kind, payload_json)
    values (
      ${SNAPSHOT_REBUILD_KIND},
      ${client.json(payload as unknown as Parameters<typeof client.json>[0])}
    )
    returning job_id::text as job_id
  `) as unknown as Array<{ job_id: string }>;

  if (inserted.length !== 1 || inserted[0] === undefined) {
    throw new Error(
      "enqueueSnapshotRebuild: job_queue insert returned no row.",
    );
  }
  return { jobId: inserted[0].job_id };
}
