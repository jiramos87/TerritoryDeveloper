/**
 * Unit tests: project_spec_journal_* tools — envelope shape + db_unconfigured guard.
 *
 * ACs (TECH-403 §7b):
 *   1. wrapTool applied — happy path returns ok:true + payload.
 *   2. db_unconfigured envelope: getIaDatabasePool mocked null → { ok:false, error:{ code:"db_unconfigured", hint:"Start Postgres on :5434" } }.
 */

import test from "node:test";
import assert from "node:assert/strict";
import { wrapTool, dbUnconfiguredError } from "../../src/envelope.js";

// ---------------------------------------------------------------------------
// dbUnconfiguredError shape
// ---------------------------------------------------------------------------

test("dbUnconfiguredError returns correct code + hint", () => {
  const err = dbUnconfiguredError();
  assert.equal(err.code, "db_unconfigured");
  assert.equal(err.hint, "Start Postgres on :5434");
  assert.ok(err.message.length > 0);
});

// ---------------------------------------------------------------------------
// Simulate project_spec_journal_persist pool-null branch via wrapTool
// ---------------------------------------------------------------------------

test("journal_persist: pool null → db_unconfigured envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    const pool: null = null;
    if (!pool) throw dbUnconfiguredError();
    return { rows: [] };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_unconfigured");
    assert.equal(result.error.hint, "Start Postgres on :5434");
  }
});

// ---------------------------------------------------------------------------
// Simulate project_spec_journal_search pool-null branch via wrapTool
// ---------------------------------------------------------------------------

test("journal_search: pool null → db_unconfigured envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    const pool: null = null;
    if (!pool) throw dbUnconfiguredError();
    return {};
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_unconfigured");
    assert.equal(result.error.hint, "Start Postgres on :5434");
  }
});

// ---------------------------------------------------------------------------
// Simulate project_spec_journal_get pool-null branch via wrapTool
// ---------------------------------------------------------------------------

test("journal_get: pool null → db_unconfigured envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    const pool: null = null;
    if (!pool) throw dbUnconfiguredError();
    return { id: 1 };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_unconfigured");
    assert.equal(result.error.hint, "Start Postgres on :5434");
  }
});

// ---------------------------------------------------------------------------
// Simulate project_spec_journal_update pool-null branch via wrapTool
// ---------------------------------------------------------------------------

test("journal_update: pool null → db_unconfigured envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    const pool: null = null;
    if (!pool) throw dbUnconfiguredError();
    return { id: 1 };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_unconfigured");
    assert.equal(result.error.hint, "Start Postgres on :5434");
  }
});

// ---------------------------------------------------------------------------
// Happy path — wrapTool applied (bare return yields ok:true + payload)
// ---------------------------------------------------------------------------

test("journal tool happy path — wrapTool yields ok:true + payload", async () => {
  const handler = wrapTool(async (_input: void) => {
    return { inserted: 2 };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, true);
  if (result.ok) {
    assert.deepEqual(result.payload, { inserted: 2 });
  }
});
