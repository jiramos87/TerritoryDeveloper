/**
 * Shared Postgres pool for cron-server.
 * Reads DATABASE_URL from environment (loaded from repo .env by supervisor index.ts).
 */

import pg from "pg";

let pool: pg.Pool | null | undefined;

export function getCronDbPool(): pg.Pool {
  if (pool === undefined) {
    const url =
      process.env["DATABASE_URL"] ??
      "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";
    pool = new pg.Pool({
      connectionString: url,
      max: 4,
      idleTimeoutMillis: 10_000,
    });
  }
  if (!pool) {
    throw new Error("[cron-server] DB pool unavailable — DATABASE_URL not set.");
  }
  return pool;
}
