/**
 * Shared Postgres pool for optional IA DB tools (journal, future DB-backed IA).
 */

import pg from "pg";
import { resolveIaDatabaseUrl } from "./resolve-database-url.js";

let pool: pg.Pool | null | undefined;

/**
 * Returns a small connection pool when `DATABASE_URL` or `config/postgres-dev.json` supplies a URI; otherwise `null`.
 */
export function getIaDatabasePool(): pg.Pool | null {
  if (pool === undefined) {
    const url = resolveIaDatabaseUrl();
    pool = url
      ? new pg.Pool({
          connectionString: url,
          max: 4,
          idleTimeoutMillis: 10_000,
        })
      : null;
  }
  return pool;
}
