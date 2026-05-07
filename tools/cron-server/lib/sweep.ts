/**
 * sweepStale — flip running-but-stale cron rows to failed.
 *
 * Stale = status='running' AND heartbeat_at < now() - interval '${timeoutMin} minutes'.
 * Per Decision 3: single shared timeout from carcass_config.claim_heartbeat_timeout_minutes.
 */

import { getCronDbPool } from "./pool.js";

/**
 * Read stale-sweep timeout from carcass_config table.
 * Falls back to 10 minutes if row absent.
 */
export async function readSweepTimeoutMinutes(): Promise<number> {
  const pool = getCronDbPool();
  const res = await pool.query<{ value: string }>(
    `SELECT value FROM carcass_config WHERE key = 'claim_heartbeat_timeout_minutes' LIMIT 1`,
  );
  if (res.rows.length === 0) return 10;
  const n = parseInt(res.rows[0].value, 10);
  return Number.isFinite(n) && n > 0 ? n : 10;
}

/**
 * Flip stale running rows in `table` to failed.
 * Returns count of rows swept.
 */
export async function sweepStale(table: string, timeoutMin: number): Promise<number> {
  const pool = getCronDbPool();
  const res = await pool.query(
    `UPDATE ${table}
     SET status     = 'failed',
         error      = 'stale_heartbeat_sweep',
         finished_at = now()
     WHERE status = 'running'
       AND heartbeat_at < now() - ($1 || ' minutes')::interval
     RETURNING job_id`,
    [String(timeoutMin)],
  );
  return res.rowCount ?? 0;
}
