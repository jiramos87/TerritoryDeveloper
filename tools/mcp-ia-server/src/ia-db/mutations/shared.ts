/**
 * Shared internals for mutations/* cluster modules.
 *
 * Not part of the public export surface — import from mutations/index.ts.
 */

import type pg from "pg";
import { getIaDatabasePool } from "../pool.js";
import { IaDbUnavailableError } from "../queries.js";

// ---------------------------------------------------------------------------
// Cache-bust helper (TECH-18106 / async-cron-jobs Stage 5.0.3)
// ---------------------------------------------------------------------------

/**
 * enqueueCacheBust — fire-and-forget INSERT into cron_cache_bust_jobs.
 *
 * Called post-commit via setImmediate so the enqueue cannot roll back the
 * originating write. Errors are swallowed (best-effort invalidation).
 *
 * cache_key_pattern: SQL LIKE pattern matching keys in ia_mcp_context_cache.
 * E.g. 'db_read_batch:%' busts all cached db_read_batch results.
 */
export function enqueueCacheBust(cache_kind: string, cache_key_pattern: string): void {
  setImmediate(() => {
    const pool = getIaDatabasePool();
    if (!pool) return;
    pool
      .query(
        `INSERT INTO cron_cache_bust_jobs (cache_kind, cache_key_pattern) VALUES ($1, $2)`,
        [cache_kind, cache_key_pattern],
      )
      .catch(() => { /* best-effort — enqueue failure is non-fatal */ });
  });
}

// ---------------------------------------------------------------------------
// Shared helpers
// ---------------------------------------------------------------------------

export function poolOrThrow(): pg.Pool {
  const pool = getIaDatabasePool();
  if (!pool) throw new IaDbUnavailableError();
  return pool;
}

export const PREFIX_SEQ: Record<string, string> = {
  TECH: "tech_id_seq",
  FEAT: "feat_id_seq",
  BUG: "bug_id_seq",
  ART: "art_id_seq",
  AUDIO: "audio_id_seq",
};

export class IaDbValidationError extends Error {
  code = "invalid_input";
  constructor(message: string) {
    super(message);
  }
}

export async function withTx<T>(fn: (c: pg.PoolClient) => Promise<T>): Promise<T> {
  const pool = poolOrThrow();
  const client = await pool.connect();
  try {
    await client.query("BEGIN");
    const out = await fn(client);
    await client.query("COMMIT");
    return out;
  } catch (e) {
    await client.query("ROLLBACK").catch(() => {});
    throw e;
  } finally {
    client.release();
  }
}
