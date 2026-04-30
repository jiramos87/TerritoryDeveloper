/**
 * Unit tests for checkCaller gate in auth/caller-allowlist.ts.
 *
 * ACs:
 *   1. Authorized caller → no throw.
 *   2. Unauthorized caller → throws POJO { code: "unauthorized_caller", message, hint }.
 *   3. caller_agent undefined → throws "unauthorized_caller"; message contains "<missing>".
 *   4. Tool absent from ALLOWLIST (read-only bypass) → no throw for any caller.
 *   5. Thrown object is plain POJO (not instanceof Error).
 */

import test from "node:test";
import assert from "node:assert/strict";
import { checkCaller } from "../src/auth/caller-allowlist.js";

// ---------------------------------------------------------------------------
// AC1 — Authorized caller
// ---------------------------------------------------------------------------

test("authorized caller — does not throw", () => {
  assert.doesNotThrow(() => checkCaller("glossary_row_create", "spec-kickoff"));
});

// ---------------------------------------------------------------------------
// AC2 — Unauthorized caller throws POJO with expected shape
// ---------------------------------------------------------------------------

test("unauthorized caller — throws POJO with unauthorized_caller code", () => {
  assert.throws(
    () => checkCaller("glossary_row_create", "verifier"),
    (err: unknown) => {
      const e = err as { code?: string; message?: string; hint?: string };
      assert.equal(e.code, "unauthorized_caller");
      assert.ok(typeof e.message === "string" && e.message.includes("verifier"));
      assert.ok(typeof e.hint === "string" && e.hint.includes("spec-kickoff"));
      // AC5 — plain POJO, not Error subclass
      assert.equal(err instanceof Error, false);
      return true;
    },
  );
});

// ---------------------------------------------------------------------------
// AC3 — undefined caller_agent → unauthorized_caller with <missing> in message
// ---------------------------------------------------------------------------

test("catalog_upsert — ship-stage allowed; random caller rejected", () => {
  assert.doesNotThrow(() => checkCaller("catalog_upsert", "ship-stage"));
  assert.throws(
    () => checkCaller("catalog_upsert", "verifier"),
    (err: unknown) =>
      err !== null &&
      typeof err === "object" &&
      (err as { code?: string }).code === "unauthorized_caller",
  );
});

test("catalog_spawn_pool_upsert — stage-file allowed", () => {
  assert.doesNotThrow(() => checkCaller("catalog_spawn_pool_upsert", "stage-file"));
});

test("undefined caller_agent — throws unauthorized_caller mentioning <missing>", () => {
  assert.throws(
    () => checkCaller("glossary_row_create", undefined),
    (err: unknown) => {
      const e = err as { code?: string; message?: string; hint?: string };
      assert.equal(e.code, "unauthorized_caller");
      assert.ok(typeof e.message === "string" && e.message.includes("<missing>"));
      assert.ok(typeof e.hint === "string");
      return true;
    },
  );
});

// ---------------------------------------------------------------------------
// AC4 — Tool absent from ALLOWLIST (read-only bypass)
// ---------------------------------------------------------------------------

test("tool not in ALLOWLIST — no throw for arbitrary caller", () => {
  assert.doesNotThrow(() => checkCaller("backlog_search", "literally-any-string"));
});
