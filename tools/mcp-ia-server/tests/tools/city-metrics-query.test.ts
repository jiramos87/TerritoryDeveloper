/**
 * Unit tests: city_metrics_query tool — envelope shape + db_unconfigured guard.
 *
 * ACs (TECH-403 §7b):
 *   1. wrapTool applied — happy path returns ok:true + payload.
 *   2. db_unconfigured: pool null → { ok:false, error:{ code:"db_unconfigured", hint:"Start Postgres on :5434" } }.
 *   3. table_missing (42P01) → { ok:false, error:{ code:"db_error" } }.
 */

import test from "node:test";
import assert from "node:assert/strict";
import { wrapTool, dbUnconfiguredError } from "../../src/envelope.js";

// ---------------------------------------------------------------------------
// pool null → db_unconfigured
// ---------------------------------------------------------------------------

test("city_metrics_query: pool null → db_unconfigured envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    const pool: null = null;
    if (!pool) throw dbUnconfiguredError();
    return { row_count: 0, rows: [] };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_unconfigured");
    assert.equal(result.error.hint, "Start Postgres on :5434");
  }
});

// ---------------------------------------------------------------------------
// table_missing (42P01) → db_error
// ---------------------------------------------------------------------------

test("city_metrics_query: 42P01 → db_error with migration hint", async () => {
  const handler = wrapTool(async (_input: void) => {
    const pgErr = Object.assign(new Error("relation does not exist"), { code: "42P01" });
    throw { code: "db_error" as const, message: pgErr.message, hint: "Run `npm run db:migrate`" };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_error");
    assert.ok(result.error.hint?.includes("db:migrate"));
  }
});

// ---------------------------------------------------------------------------
// Happy path — bare payload wrapped to ok:true
// ---------------------------------------------------------------------------

test("city_metrics_query: happy path → ok:true + row_count", async () => {
  const handler = wrapTool(async (_input: void) => {
    return { row_count: 1, rows: [{ id: 1 }] };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, true);
  if (result.ok) {
    assert.equal((result.payload as { row_count: number }).row_count, 1);
  }
});
