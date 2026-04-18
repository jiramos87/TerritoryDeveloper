/**
 * master_plan_next_pending — fixture coverage tests (TECH-416).
 *
 * Tests the pure core `findNextPendingRow(repoRoot, plan, stage?)` against
 * fixture mini-repos under tools/scripts/test-fixtures/master-plan-next-pending/:
 *
 *   first-pending-wins  — 3 rows: Done → _pending_ (wins) → _pending_
 *   draft-tops-pending  — 3 rows: Done → Draft+TECH-501 (wins) → _pending_
 *   stage-filter        — 2 stages; 4.1 all Done, 4.2 has _pending_
 *   fully-done          — all rows Done → null
 *   determinism         — reuses first-pending-wins; two calls → deep-equal
 *
 * Error paths:
 *   invalid_input       — empty plan arg
 *   plan_not_found      — missing file on disk
 *
 * Framework: node:test + node:assert/strict (matches MCP package test runner).
 *
 * Row-line anchors (1-based, frozen for each fixture):
 *   first-pending-wins  T4.1.2 → line 10
 *   draft-tops-pending  T4.1.2 → line 10
 *   stage-filter        T4.2.1 → line 16
 */

import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { findNextPendingRow } from "../../src/tools/master-plan-next-pending.js";
import type { NextPendingResult } from "../../src/tools/master-plan-next-pending.js";

// ---------------------------------------------------------------------------
// Helper
// ---------------------------------------------------------------------------

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const FIXTURE_ROOT = path.resolve(
  __dirname,
  "../../../scripts/test-fixtures/master-plan-next-pending",
);

const PLAN = "ia/projects/sample-master-plan.md";

function fixtureRoot(name: string): string {
  return path.join(FIXTURE_ROOT, name);
}

// ---------------------------------------------------------------------------
// 1. first-pending-wins — T4.1.2 (line 10), issue_id null, status _pending_
// ---------------------------------------------------------------------------

test("first-pending-wins: returns first _pending_ row (T4.1.2, line 10)", () => {
  const result = findNextPendingRow(fixtureRoot("first-pending-wins"), PLAN);

  assert.deepEqual(result, {
    issue_id: null,
    task_key: "T4.1.2",
    row_line: 10,
    status: "_pending_",
  } satisfies NextPendingResult);
});

// ---------------------------------------------------------------------------
// 2. draft-tops-pending — T4.1.2 Draft + TECH-501 wins over T4.1.3 _pending_
// ---------------------------------------------------------------------------

test("draft-tops-pending: returns Draft row (T4.1.2 TECH-501, line 10) before _pending_", () => {
  const result = findNextPendingRow(fixtureRoot("draft-tops-pending"), PLAN);

  assert.deepEqual(result, {
    issue_id: "TECH-501",
    task_key: "T4.1.2",
    row_line: 10,
    status: "Draft",
  } satisfies NextPendingResult);
});

// ---------------------------------------------------------------------------
// 3. stage-filter — 3 sub-assertions
// ---------------------------------------------------------------------------

test("stage-filter: stage=4.2 returns T4.2.1 (line 16)", () => {
  const result = findNextPendingRow(fixtureRoot("stage-filter"), PLAN, "4.2");

  assert.ok(result !== null, "expected non-null result for stage=4.2");
  assert.equal(result.task_key, "T4.2.1");
  assert.equal(result.row_line, 16);
  assert.equal(result.status, "_pending_");
  assert.equal(result.issue_id, null);
});

test("stage-filter: stage=4.1 (all Done) returns null", () => {
  const result = findNextPendingRow(fixtureRoot("stage-filter"), PLAN, "4.1");
  assert.equal(result, null);
});

test("stage-filter: stage=9.9 (missing heading) returns null without throwing", () => {
  const result = findNextPendingRow(fixtureRoot("stage-filter"), PLAN, "9.9");
  assert.equal(result, null);
});

// ---------------------------------------------------------------------------
// 4. fully-done — all rows Done → null
// ---------------------------------------------------------------------------

test("fully-done: returns null when all rows are Done", () => {
  const result = findNextPendingRow(fixtureRoot("fully-done"), PLAN);
  assert.equal(result, null);
});

// ---------------------------------------------------------------------------
// 5. determinism — two calls on same fixture produce identical results
// ---------------------------------------------------------------------------

test("determinism: two calls on first-pending-wins return deep-equal results", () => {
  const root = fixtureRoot("first-pending-wins");
  const r1 = findNextPendingRow(root, PLAN);
  const r2 = findNextPendingRow(root, PLAN);
  assert.deepEqual(r1, r2);
  // Extra: both must identify T4.1.2 at line 10
  assert.ok(r1 !== null);
  assert.equal(r1.task_key, "T4.1.2");
  assert.equal(r1.row_line, 10);
});

// ---------------------------------------------------------------------------
// 6. invalid_input — empty plan throws { code: "invalid_input" }
// ---------------------------------------------------------------------------

test("invalid_input: empty plan string throws { code: 'invalid_input' }", () => {
  assert.throws(
    () => findNextPendingRow(fixtureRoot("first-pending-wins"), ""),
    (e: unknown) => {
      assert.ok(e && typeof e === "object", "thrown value should be an object");
      const err = e as Record<string, unknown>;
      assert.equal(err["code"], "invalid_input", "code mismatch");
      return true;
    },
  );
});

// ---------------------------------------------------------------------------
// 7. plan_not_found — missing file throws { code: "plan_not_found" }
// ---------------------------------------------------------------------------

test("plan_not_found: missing plan file throws { code: 'plan_not_found' }", () => {
  assert.throws(
    () =>
      findNextPendingRow(
        fixtureRoot("first-pending-wins"),
        "ia/projects/ghost.md",
      ),
    (e: unknown) => {
      assert.ok(e && typeof e === "object", "thrown value should be an object");
      const err = e as Record<string, unknown>;
      assert.equal(err["code"], "plan_not_found", "code mismatch");
      assert.ok(
        typeof err["message"] === "string" &&
          err["message"].includes("ghost.md"),
        `expected message to include 'ghost.md', got: '${err["message"]}'`,
      );
      return true;
    },
  );
});
