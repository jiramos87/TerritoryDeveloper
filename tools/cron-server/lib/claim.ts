/**
 * claimBatch — FIFO claim N rows from a cron queue table.
 *
 * Uses UPDATE ... FOR UPDATE SKIP LOCKED to safely claim rows
 * across concurrent supervisor instances without double-processing.
 *
 * Returns the claimed rows (status already flipped to 'running').
 */

import { getCronDbPool } from "./pool.js";

export interface CronJobRow {
  job_id: string;
  [key: string]: unknown;
}

export async function claimBatch(
  table: string,
  limit = 50,
): Promise<CronJobRow[]> {
  const pool = getCronDbPool();
  const res = await pool.query<CronJobRow>(
    `UPDATE ${table}
     SET status = 'running',
         started_at = now(),
         heartbeat_at = now(),
         attempts = attempts + 1
     WHERE job_id IN (
       SELECT job_id FROM ${table}
       WHERE status = 'queued'
       ORDER BY enqueued_at ASC
       LIMIT $1
       FOR UPDATE SKIP LOCKED
     )
     RETURNING *`,
    [limit],
  );
  return res.rows;
}
