/**
 * parent_plan_validate — fixture coverage tests.
 *
 * TECH-410: Locks the cross-file invariant contract for validateParentPlanLocator
 * (shared core, TECH-406) using 5 fixture mini-repos under
 * tools/scripts/test-fixtures/parent-plan-validate/.
 *
 * Fixture matrix (§4.3 of TECH-410.md):
 *   plan-exists-pass           — clean record + matching back-ref → errors=[], warnings=[], exit_code=0
 *   plan-missing-fail          — parent_plan path absent on disk → errors=[1], warnings=[], exit_code=1
 *   task-key-bad-regex-fail    — malformed task_key → loader skips record → errors=[], warnings=[], exit_code=0
 *   task-key-drift-warn        — plan exists but no row matches task_key → advisory warn, strict error
 *   issue-back-ref-missing-warn— plan row back-ref wrong id → advisory warn, strict error
 *
 * Note on task-key-bad-regex-fail:
 *   The backlog-yaml-loader's validateTaskKey() throws on malformed task_key and
 *   the loader catches + skips that record (parse error, not added to records[]).
 *   The validator therefore receives an empty records list and emits no errors/warnings.
 *   The validator's own TASK_KEY_RE check (parent-plan-validator.ts:194) is thus
 *   unreachable via the normal load path — this fixture documents that behavior.
 *
 * Each fixture × 2 modes (advisory / strict) = 10 test cases.
 */

import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { validateParentPlanLocator } from "../../src/parser/parent-plan-validator.js";
import type { ValidateResult } from "../../src/parser/parent-plan-validator.js";

// ---------------------------------------------------------------------------
// Helper
// ---------------------------------------------------------------------------

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const FIXTURE_ROOT = path.resolve(
  __dirname,
  "../../../scripts/test-fixtures/parent-plan-validate",
);

function runFixture(name: string, strict: boolean): ValidateResult {
  const repoRoot = path.join(FIXTURE_ROOT, name);
  return validateParentPlanLocator({
    repoRoot,
    yamlDirs: ["ia/backlog"],
    planGlob: "ia/projects/*master-plan*.md",
    strict,
  });
}

// ---------------------------------------------------------------------------
// plan-exists-pass — clean record with correct back-ref
// ---------------------------------------------------------------------------

test("plan-exists-pass: advisory → errors=[], warnings=[], exit_code=0", () => {
  const result = runFixture("plan-exists-pass", false);
  assert.equal(result.exit_code, 0, "exit_code should be 0");
  assert.equal(result.errors.length, 0, "no errors expected");
  assert.equal(result.warnings.length, 0, "no warnings expected");
});

test("plan-exists-pass: strict → errors=[], warnings=[], exit_code=0", () => {
  const result = runFixture("plan-exists-pass", true);
  assert.equal(result.exit_code, 0, "exit_code should be 0 in strict mode");
  assert.equal(result.errors.length, 0, "no errors expected");
  assert.equal(result.warnings.length, 0, "no warnings expected");
});

// ---------------------------------------------------------------------------
// plan-missing-fail — parent_plan path not on disk
// ---------------------------------------------------------------------------

test("plan-missing-fail: advisory → errors=[1 'parent_plan not found'], exit_code=1", () => {
  const result = runFixture("plan-missing-fail", false);
  assert.equal(result.exit_code, 1, "exit_code should be 1");
  assert.equal(result.errors.length, 1, "exactly one error expected");
  assert.ok(
    result.errors[0]!.includes("parent_plan not found"),
    `expected 'parent_plan not found' in error, got: '${result.errors[0]}'`,
  );
  assert.equal(result.warnings.length, 0, "no warnings expected");
});

test("plan-missing-fail: strict → same as advisory (error, not warning)", () => {
  const result = runFixture("plan-missing-fail", true);
  assert.equal(result.exit_code, 1, "exit_code should be 1");
  assert.equal(result.errors.length, 1, "exactly one error expected");
  assert.ok(
    result.errors[0]!.includes("parent_plan not found"),
    `expected 'parent_plan not found' in error, got: '${result.errors[0]}'`,
  );
  assert.equal(result.warnings.length, 0, "no warnings expected");
});

// ---------------------------------------------------------------------------
// task-key-bad-regex-fail — malformed task_key (loader skips record)
//
// Actual behavior: loader's validateTaskKey() throws on "Stage-3", loader
// catches + skips the record. Validator receives records=[] → no issues emitted.
// ---------------------------------------------------------------------------

test("task-key-bad-regex-fail: advisory → loader skips record → errors=[], warnings=[], exit_code=0", () => {
  const result = runFixture("task-key-bad-regex-fail", false);
  assert.equal(result.exit_code, 0, "exit_code should be 0 (no records to validate)");
  assert.equal(result.errors.length, 0, "no errors: malformed record skipped by loader");
  assert.equal(result.warnings.length, 0, "no warnings");
});

test("task-key-bad-regex-fail: strict → loader skips record → errors=[], warnings=[], exit_code=0", () => {
  const result = runFixture("task-key-bad-regex-fail", true);
  assert.equal(result.exit_code, 0, "exit_code should be 0 in strict mode (no records)");
  assert.equal(result.errors.length, 0, "no errors: malformed record skipped by loader");
  assert.equal(result.warnings.length, 0, "no warnings");
});

// ---------------------------------------------------------------------------
// task-key-drift-warn — valid task_key T9.9.9 but plan has no matching row
// ---------------------------------------------------------------------------

test("task-key-drift-warn: advisory → warnings=[1 'not found in'], exit_code=0", () => {
  const result = runFixture("task-key-drift-warn", false);
  assert.equal(result.exit_code, 0, "exit_code should be 0 in advisory mode");
  assert.equal(result.errors.length, 0, "no errors in advisory mode");
  assert.equal(result.warnings.length, 1, "exactly one warning expected");
  assert.ok(
    result.warnings[0]!.includes("not found in"),
    `expected 'not found in' in warning, got: '${result.warnings[0]}'`,
  );
});

test("task-key-drift-warn: strict → errors=[1 'not found in'], warnings=[], exit_code=1", () => {
  const result = runFixture("task-key-drift-warn", true);
  assert.equal(result.exit_code, 1, "exit_code should be 1 in strict mode");
  assert.equal(result.errors.length, 1, "exactly one error expected (promoted warning)");
  assert.ok(
    result.errors[0]!.includes("not found in"),
    `expected 'not found in' in error, got: '${result.errors[0]}'`,
  );
  assert.equal(result.warnings.length, 0, "no warnings in strict mode");
});

// ---------------------------------------------------------------------------
// issue-back-ref-missing-warn — plan row exists but back-ref points to wrong id
// ---------------------------------------------------------------------------

test("issue-back-ref-missing-warn: advisory → warnings=[1 'references'], exit_code=0", () => {
  const result = runFixture("issue-back-ref-missing-warn", false);
  assert.equal(result.exit_code, 0, "exit_code should be 0 in advisory mode");
  assert.equal(result.errors.length, 0, "no errors in advisory mode");
  assert.equal(result.warnings.length, 1, "exactly one warning expected");
  assert.ok(
    result.warnings[0]!.includes("references"),
    `expected 'references' in warning, got: '${result.warnings[0]}'`,
  );
});

test("issue-back-ref-missing-warn: strict → errors=[1 'references'], warnings=[], exit_code=1", () => {
  const result = runFixture("issue-back-ref-missing-warn", true);
  assert.equal(result.exit_code, 1, "exit_code should be 1 in strict mode");
  assert.equal(result.errors.length, 1, "exactly one error expected (promoted warning)");
  assert.ok(
    result.errors[0]!.includes("references"),
    `expected 'references' in error, got: '${result.errors[0]}'`,
  );
  assert.equal(result.warnings.length, 0, "no warnings in strict mode");
});
