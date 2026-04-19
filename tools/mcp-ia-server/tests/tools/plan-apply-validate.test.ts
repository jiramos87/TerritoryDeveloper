/**
 * plan_apply_validate — branch coverage tests (TECH-460).
 *
 * Tests the pure core `validatePlanApply(repoRoot, sectionHeader, targetPath)`
 * against 3 inline markdown fixtures written to a tmpdir:
 *
 *   missing-heading — heading absent        → { ok:false, found:false, tuple_count:0 }
 *   empty-section   — heading present, 0 tuples → { ok:false, found:true, tuple_count:0 }
 *   valid-section   — heading + N>0 tuples     → { ok:true,  found:true, tuple_count:N }
 *
 * Extra coverage:
 *   read-error     — absent file path           → { ok:false, found:false, tuple_count:0, error }
 *   invalid-input  — empty section_header       → { ok:false, ..., error }
 *
 * Framework: node:test + node:assert/strict.
 */

import test from "node:test";
import assert from "node:assert/strict";
import * as fs from "fs";
import * as os from "os";
import * as path from "path";
import { validatePlanApply } from "../../src/tools/plan-apply-validate.js";

// ---------------------------------------------------------------------------
// Helper — write a markdown fixture to a tmp file, return abs path.
// ---------------------------------------------------------------------------

function writeFixture(name: string, body: string): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), `plan-apply-validate-${name}-`));
  const file = path.join(dir, "fixture.md");
  fs.writeFileSync(file, body, "utf8");
  return file;
}

// ---------------------------------------------------------------------------
// Branch 1 — missing heading
// ---------------------------------------------------------------------------

test("missing-heading: returns found=false, ok=false, tuple_count=0", () => {
  const file = writeFixture(
    "missing",
    [
      "# Some Spec",
      "",
      "## Other Section",
      "- tuple line one",
      "- tuple line two",
      "",
    ].join("\n"),
  );

  const result = validatePlanApply("/", "Plan Fix", file);

  assert.equal(result.found, false, "heading should not be found");
  assert.equal(result.ok, false, "ok must be false when heading absent");
  assert.equal(result.tuple_count, 0);
  assert.equal(result.error, undefined, "no fs error expected");
});

// ---------------------------------------------------------------------------
// Branch 2 — heading present, empty tuple body
// ---------------------------------------------------------------------------

test("empty-section: heading present but no tuples → found=true, ok=false, tuple_count=0", () => {
  const file = writeFixture(
    "empty",
    [
      "# Some Spec",
      "",
      "## Plan Fix",
      "",
      "Opus planner writes here.",
      "",
      "## Next Section",
      "- this tuple is under a different heading",
      "",
    ].join("\n"),
  );

  const result = validatePlanApply("/", "Plan Fix", file);

  assert.equal(result.found, true, "heading must be found");
  assert.equal(result.ok, false, "ok must be false when tuple_count=0");
  assert.equal(result.tuple_count, 0, "prose line must not be counted as tuple");
});

// ---------------------------------------------------------------------------
// Branch 3 — heading present, N > 0 tuples
// ---------------------------------------------------------------------------

test("valid-section: heading + 3 tuples → found=true, ok=true, tuple_count=3", () => {
  const file = writeFixture(
    "valid",
    [
      "# Some Spec",
      "",
      "## Plan Fix",
      "",
      "- {operation: edit, target_path: foo.md, target_anchor: L10, payload: bar}",
      "- {operation: insert, target_path: baz.md, target_anchor: end, payload: qux}",
      "{operation: delete, target_path: quux.md, target_anchor: L5, payload: null}",
      "",
      "## Next Section",
      "- unrelated tuple",
      "",
    ].join("\n"),
  );

  const result = validatePlanApply("/", "Plan Fix", file);

  assert.equal(result.found, true);
  assert.equal(result.ok, true, "ok must be true when tuple_count > 0");
  assert.equal(result.tuple_count, 3, "3 tuples under Plan Fix heading");
});

// ---------------------------------------------------------------------------
// Extra — mixed tuple shapes + fenced-code exclusion
// ---------------------------------------------------------------------------

test("mixed shapes: list bullets + operation-yaml form + fenced-code exclusion", () => {
  const file = writeFixture(
    "mixed",
    [
      "## Plan Fix",
      "",
      "- list bullet tuple",
      "operation: edit",
      "```",
      "- bullet inside fenced code (MUST NOT count)",
      "operation: inside-fence",
      "```",
      "- another bullet tuple after fence",
      "",
      "## Next",
    ].join("\n"),
  );

  const result = validatePlanApply("/", "Plan Fix", file);

  assert.equal(result.found, true);
  assert.equal(result.ok, true);
  assert.equal(
    result.tuple_count,
    3,
    "expect 3 tuples: 2 list bullets outside fence + 1 operation: line",
  );
});

// ---------------------------------------------------------------------------
// Extra — read error surfaces in error field (no throw)
// ---------------------------------------------------------------------------

test("read-error: absent file path returns ok=false + error populated", () => {
  const result = validatePlanApply(
    "/",
    "Plan Fix",
    "/nonexistent/does/not/exist/file.md",
  );

  assert.equal(result.ok, false);
  assert.equal(result.found, false);
  assert.equal(result.tuple_count, 0);
  assert.ok(
    result.error && result.error.includes("Cannot read target_path"),
    `expected fs error message, got: ${result.error}`,
  );
});

// ---------------------------------------------------------------------------
// Extra — empty section_header rejected with error
// ---------------------------------------------------------------------------

test("invalid-input: empty section_header returns error", () => {
  const result = validatePlanApply("/", "   ", "/tmp/whatever.md");

  assert.equal(result.ok, false);
  assert.ok(
    result.error && result.error.includes("section_header is required"),
    `expected section_header error, got: ${result.error}`,
  );
});

test("invalid-input: empty target_path returns error", () => {
  const result = validatePlanApply("/", "Plan Fix", "   ");

  assert.equal(result.ok, false);
  assert.ok(
    result.error && result.error.includes("target_path is required"),
    `expected target_path error, got: ${result.error}`,
  );
});
