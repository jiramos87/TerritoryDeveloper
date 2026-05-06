/**
 * flip — mark a cron queue row done or failed after processing.
 */

import { getCronDbPool } from "./pool.js";

export async function markDone(table: string, jobId: string): Promise<void> {
  const pool = getCronDbPool();
  await pool.query(
    `UPDATE ${table}
     SET status = 'done', finished_at = now(), heartbeat_at = now()
     WHERE job_id = $1`,
    [jobId],
  );
}

export async function markFailed(
  table: string,
  jobId: string,
  error: string,
): Promise<void> {
  const pool = getCronDbPool();
  await pool.query(
    `UPDATE ${table}
     SET status = 'failed', finished_at = now(), heartbeat_at = now(), error = $2
     WHERE job_id = $1`,
    [jobId, error],
  );
}
