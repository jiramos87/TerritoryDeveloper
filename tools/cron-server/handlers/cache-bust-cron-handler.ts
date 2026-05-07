/**
 * cache-bust-cron-handler — processes one cron_cache_bust_jobs row.
 *
 * DELETEs rows from ia_mcp_context_cache matching:
 *   WHERE cache_kind = row.cache_kind AND key LIKE row.cache_key_pattern
 *
 * Note: ia_mcp_context_cache has no `cache_kind` column — the cache_kind is
 * encoded as a prefix in the `key` column (e.g. `db_read_batch:<hash>`).
 * The handler therefore uses:
 *   WHERE key LIKE row.cache_key_pattern
 * and the pattern should include the cache_kind prefix if scoping to a kind
 * (e.g. `db_read_batch:%`). The cache_kind column in the job row is informational
 * and used for logging.
 *
 * Non-zero exit → throws so the supervisor marks row.error.
 *
 * TECH-18105 / async-cron-jobs Stage 5.0.2
 */

import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

const pgRequire = createRequire(path.join(repoRoot, "tools/postgres-ia/package.json"));
const pg = pgRequire("pg") as typeof import("pg");

export interface CacheBustJobRow {
  job_id: string;
  cache_kind: string;
  cache_key_pattern: string;
}

async function resolveDatabaseUrl(): Promise<string> {
  const { resolveDatabaseUrl: resolve } = await import(
    path.join(repoRoot, "tools/postgres-ia/resolve-database-url.mjs") as string
  ) as { resolveDatabaseUrl: (root: string) => Promise<string> };
  return resolve(repoRoot) ?? "postgres://postgres:postgres@localhost:5434/territory_ia_dev";
}

export async function run(row: CacheBustJobRow): Promise<void> {
  const dbUrl = await resolveDatabaseUrl();
  const client = new pg.Client({ connectionString: dbUrl });
  await client.connect();
  try {
    const res = await client.query<{ rowcount: string }>(
      `WITH deleted AS (
         DELETE FROM ia_mcp_context_cache
         WHERE key LIKE $1
         RETURNING key
       )
       SELECT COUNT(*)::text AS rowcount FROM deleted`,
      [row.cache_key_pattern],
    );
    const deleted = res.rows[0]?.rowcount ?? "0";
    console.log(
      `[cache-bust] job_id=${row.job_id} cache_kind=${row.cache_kind} pattern=${row.cache_key_pattern} deleted=${deleted}`,
    );
  } finally {
    await client.end().catch(() => { /* ignore */ });
  }
}
