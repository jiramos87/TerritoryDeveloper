/**
 * Postgres advisory-lock helper for parallel `node --test` workers.
 *
 * Multiple `*.test.ts` files run in parallel processes against the same
 * `territory_ia_dev` database. Their `before()`/`after()` hooks seed/teardown
 * fixtures that cascade-delete from `ia_master_plans`. Concurrent cascading
 * DELETEs across overlapping FK-bound tables (`ia_stages`,
 * `stage_carcass_signals`, `ia_section_claims`, `ia_stage_claims`,
 * `ia_red_stage_proofs`, `stage_arch_surfaces`) can acquire row/table locks
 * in different orders, producing `deadlock detected` (40P01) and
 * `ia_stages_slug_fkey` violations mid-seed.
 *
 * `withFixtureLock` serializes ALL parallel test workers' fixture mutations
 * via a single global `pg_advisory_lock` key. Test bodies still run in
 * parallel after their `before()` resolves — only the seed/teardown windows
 * serialize.
 *
 * The lock is session-scoped (held on a dedicated checked-out client across
 * the whole `fn()` body, regardless of whether `fn()` runs queries on the
 * pool or the locked client). On error or timeout the `finally` block
 * releases + checks the client back in.
 *
 * DB-less CI: passing a null pool short-circuits to bare `fn()` invocation.
 */

import type pg from "pg";

// Stable 64-bit key — `hashtext` derives the same int for every worker.
// Single shared key serializes ALL fixture work across all test files.
const FIXTURE_LOCK_LITERAL = "ia_test_fixture_lock";

export async function withFixtureLock<T>(
  pool: pg.Pool | null,
  fn: () => Promise<T>,
): Promise<T> {
  if (pool === null) return fn();
  const client = await pool.connect();
  try {
    await client.query(
      `SELECT pg_advisory_lock(hashtext($1)::bigint)`,
      [FIXTURE_LOCK_LITERAL],
    );
    try {
      return await fn();
    } finally {
      await client.query(
        `SELECT pg_advisory_unlock(hashtext($1)::bigint)`,
        [FIXTURE_LOCK_LITERAL],
      );
    }
  } finally {
    client.release();
  }
}
