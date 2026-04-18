/**
 * master_plan_locate — fixture coverage tests (TECH-414).
 *
 * Tests the pure core `locateMasterPlanRow(repoRoot, issueId)` against 4
 * fixture mini-repos under tools/scripts/test-fixtures/master-plan-locate/:
 *
 *   happy-path          — full v2 yaml + matching plan row → full envelope
 *   missing-parent-plan — yaml lacks parent_plan → missing_locator_fields
 *   plan-not-found      — parent_plan path absent on disk → plan_not_found
 *   task-key-drift      — task_key T9.9.9 not in plan → task_key_drift
 *
 * Framework: node:test + node:assert/strict (matches MCP package test runner).
 */

import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { locateMasterPlanRow } from "../../src/tools/master-plan-locate.js";
import type { MasterPlanLocateResult } from "../../src/tools/master-plan-locate.js";

// ---------------------------------------------------------------------------
// Helper
// ---------------------------------------------------------------------------

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const FIXTURE_ROOT = path.resolve(
  __dirname,
  "../../../scripts/test-fixtures/master-plan-locate",
);

function runFixture(name: string): MasterPlanLocateResult {
  const repoRoot = path.join(FIXTURE_ROOT, name);
  return locateMasterPlanRow(repoRoot, "TECH-999");
}

// ---------------------------------------------------------------------------
// happy-path — full envelope returned
// ---------------------------------------------------------------------------

test("happy-path: returns full envelope with correct fields", () => {
  const result = runFixture("happy-path");

  assert.equal(result.plan, "ia/projects/sample-master-plan.md");
  assert.equal(result.step, 3);
  assert.equal(result.stage, "3.3");
  assert.equal(result.phase, 1);
  assert.equal(result.task_key, "T3.3.1");
  // row_line is 1-based; T3.3.1 row is line 7 in the fixture plan
  assert.equal(result.row_line, 7, "row_line should be 1-based line number of T3.3.1 row");
  assert.ok(
    result.row_raw.startsWith("| T3.3.1 |"),
    `expected row_raw to start with '| T3.3.1 |', got: '${result.row_raw}'`,
  );
});

// ---------------------------------------------------------------------------
// missing-parent-plan — yaml omits parent_plan
// ---------------------------------------------------------------------------

test("missing-parent-plan: throws missing_locator_fields with field=parent_plan", () => {
  assert.throws(
    () => runFixture("missing-parent-plan"),
    (e: unknown) => {
      assert.ok(e && typeof e === "object", "thrown value should be an object");
      const err = e as Record<string, unknown>;
      assert.equal(err["code"], "missing_locator_fields", "code mismatch");
      assert.ok(
        err["details"] &&
          typeof err["details"] === "object" &&
          (err["details"] as Record<string, unknown>)["field"] === "parent_plan",
        `expected details.field === 'parent_plan', got: ${JSON.stringify(err["details"])}`,
      );
      return true;
    },
  );
});

// ---------------------------------------------------------------------------
// plan-not-found — parent_plan path absent on disk
// ---------------------------------------------------------------------------

test("plan-not-found: throws plan_not_found with message including plan path", () => {
  assert.throws(
    () => runFixture("plan-not-found"),
    (e: unknown) => {
      assert.ok(e && typeof e === "object", "thrown value should be an object");
      const err = e as Record<string, unknown>;
      assert.equal(err["code"], "plan_not_found", "code mismatch");
      assert.ok(
        typeof err["message"] === "string" &&
          err["message"].includes("ghost-master-plan.md"),
        `expected message to include plan path, got: '${err["message"]}'`,
      );
      return true;
    },
  );
});

// ---------------------------------------------------------------------------
// task-key-drift — task_key T9.9.9 absent from plan rows
// ---------------------------------------------------------------------------

test("task-key-drift: throws task_key_drift with message including task_key and plan path", () => {
  assert.throws(
    () => runFixture("task-key-drift"),
    (e: unknown) => {
      assert.ok(e && typeof e === "object", "thrown value should be an object");
      const err = e as Record<string, unknown>;
      assert.equal(err["code"], "task_key_drift", "code mismatch");
      const msg = String(err["message"] ?? "");
      assert.ok(
        msg.includes("T9.9.9"),
        `expected message to include task_key 'T9.9.9', got: '${msg}'`,
      );
      assert.ok(
        msg.includes("sample-master-plan.md"),
        `expected message to include plan path, got: '${msg}'`,
      );
      return true;
    },
  );
});
