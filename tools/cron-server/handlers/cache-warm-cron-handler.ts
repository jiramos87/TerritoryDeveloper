/**
 * cache-warm-cron-handler — processes one cron_cache_warm_jobs row.
 *
 * Pre-populates ia_mcp_context_cache for a hot db_read_batch key so that
 * the next agent read is a cache hit rather than a live DB round-trip.
 *
 * Strategy:
 *   - cache_kind = 'db_read_batch': the cache_key encodes the normalized SQL
 *     hash as `db_read_batch:<sha256hex>`. The handler re-executes the SQL
 *     stored in the row (passed via cache_key containing the raw SQL after the
 *     separator), INSERTs the result into ia_mcp_context_cache using
 *     plan_id = slug (or 'global' when slug absent).
 *   - Other cache_kinds: logged + skipped (no-op success) — extensible later.
 *
 * For db_read_batch warm: cache_key format expected by callers:
 *   `db_read_batch:<normalized_sql>` — the handler splits on first ':',
 *   uses the remainder as the SQL to execute, hashes it, and writes the cache
 *   entry with the same key pattern that db_read_batch tool reads.
 *
 * Non-zero exit → throws so the supervisor marks row.error.
 *
 * TECH-18105 / async-cron-jobs Stage 5.0.2
 */

import { createHash } from "node:crypto";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

const pgRequire = createRequire(path.join(repoRoot, "tools/postgres-ia/package.json"));
const pg = pgRequire("pg") as typeof import("pg");

export interface CacheWarmJobRow {
  job_id: string;
  cache_kind: string;
  cache_key: string;
  slug?: string | null;
}

function sha256hex(s: string): string {
  return createHash("sha256").update(s, "utf8").digest("hex");
}

function normalizeSql(sql: string): string {
  return sql
    .replace(/\s+/g, " ")
    .trim()
    .replace(
      /\b(SELECT|FROM|WHERE|JOIN|LEFT|RIGHT|INNER|OUTER|CROSS|ON|AND|OR|NOT|IN|AS|GROUP|BY|ORDER|HAVING|LIMIT|OFFSET|DISTINCT|UNION|ALL|EXISTS|CASE|WHEN|THEN|ELSE|END|WITH|INSERT|UPDATE|DELETE|DROP|ALTER|TRUNCATE|GRANT|REVOKE|SET|INTO|VALUES|RETURNING)\b/gi,
      (m) => m.toLowerCase(),
    );
}

async function resolveDatabaseUrl(): Promise<string> {
  const { resolveDatabaseUrl: resolve } = await import(
    path.join(repoRoot, "tools/postgres-ia/resolve-database-url.mjs") as string
  ) as { resolveDatabaseUrl: (root: string) => Promise<string> };
  return resolve(repoRoot) ?? "postgres://postgres:postgres@localhost:5434/territory_ia_dev";
}

export async function run(row: CacheWarmJobRow): Promise<void> {
  if (row.cache_kind !== "db_read_batch") {
    // Unknown kind — no-op (extensible for future cache_kind values).
    console.log(
      `[cache-warm] job_id=${row.job_id} cache_kind=${row.cache_kind} — skipped (no warm strategy for this kind)`,
    );
    return;
  }

  // cache_key format: `db_read_batch:<sql>` where <sql> is the raw SELECT.
  const separatorIdx = row.cache_key.indexOf(":");
  if (separatorIdx < 0) {
    throw new Error(
      `[cache-warm] job_id=${row.job_id} invalid cache_key format — expected 'db_read_batch:<sql>'`,
    );
  }
  const rawSql = row.cache_key.slice(separatorIdx + 1).trim();
  if (!rawSql) {
    throw new Error(
      `[cache-warm] job_id=${row.job_id} empty SQL in cache_key`,
    );
  }

  const normalized = normalizeSql(rawSql);
  const hash = sha256hex(normalized);
  const cacheKey = `db_read_batch:${hash}`;
  const planId = row.slug ?? "global";

  const dbUrl = await resolveDatabaseUrl();
  const client = new pg.Client({ connectionString: dbUrl });
  await client.connect();
  try {
    // Execute the SQL in a read-only transaction.
    await client.query("BEGIN");
    await client.query("SET TRANSACTION READ ONLY");
    const execRes = await client.query(rawSql);
    const rows = execRes.rows;
    await client.query("COMMIT");

    // Write through to ia_mcp_context_cache.
    await client.query(
      `INSERT INTO ia_mcp_context_cache (plan_id, key, payload, content_hash, updated_at)
       VALUES ($1, $2, $3, $4, now())
       ON CONFLICT (plan_id, key)
       DO UPDATE SET payload = EXCLUDED.payload, content_hash = EXCLUDED.content_hash, updated_at = now()`,
      [planId, cacheKey, JSON.stringify(rows), hash],
    );

    console.log(
      `[cache-warm] job_id=${row.job_id} warmed plan_id=${planId} key=${cacheKey} rows=${rows.length}`,
    );
  } catch (e) {
    try { await client.query("ROLLBACK"); } catch { /* ignore */ }
    throw e;
  } finally {
    await client.end().catch(() => { /* ignore */ });
  }
}
