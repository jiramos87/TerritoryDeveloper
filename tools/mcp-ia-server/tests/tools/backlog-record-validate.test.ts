/**
 * backlog_record_validate — fixture coverage tests.
 *
 * TECH-325: Locks the lint contract for validateBacklogRecord before downstream
 * consumers (IP6 backlog_record_create) depend on specific error text.
 *
 * Fixture matrix (§5.3):
 *   G1 — valid open record → ok: true, errors: []
 *   B1 — missing required field (title omitted) → E_MISSING_FIELD
 *   B2 — bad id format (tech_325, underscore/lowercase) → E_BAD_ID_FORMAT
 *   B3 — invalid status enum (status: wip) → E_BAD_STATUS
 *   B4 — depends_on non-empty + depends_on_raw empty → E_EMPTY_DEPENDS_ON_RAW
 */

import test from "node:test";
import assert from "node:assert/strict";
import {
  validateBacklogRecord,
  E_MISSING_FIELD,
  E_BAD_ID_FORMAT,
  E_BAD_STATUS,
  E_EMPTY_DEPENDS_ON_RAW,
  E_BAD_TASK_KEY_FORMAT,
  E_BAD_LOCATOR_ARRAY_TYPE,
  E_EMPTY_PARENT_PLAN,
} from "../../src/parser/backlog-record-schema.js";

// ---------------------------------------------------------------------------
// G1 — Valid open record
// ---------------------------------------------------------------------------

test("G1: valid open record → ok: true, errors: []", () => {
  const yaml = [
    "id: TECH-325",
    "type: tech",
    "title: Test backlog_record_validate against fixtures",
    "status: open",
    "section: Backlog YAML and MCP alignment program",
    "priority: high",
    "created: 2026-04-17",
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, true);
  assert.equal(result.errors.length, 0);
});

// ---------------------------------------------------------------------------
// B1 — Missing required field (title omitted)
// ---------------------------------------------------------------------------

test("B1: missing required field (title) → E_MISSING_FIELD error", () => {
  const yaml = [
    "id: TECH-325",
    "type: tech",
    "status: open",
    "section: Backlog YAML and MCP alignment program",
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.length >= 1, "at least one error expected");
  assert.ok(
    result.errors[0]!.startsWith(E_MISSING_FIELD),
    `expected error to start with '${E_MISSING_FIELD}', got: '${result.errors[0]}'`,
  );
});

// ---------------------------------------------------------------------------
// B2 — Bad id format (underscore + lowercase: tech_325)
// ---------------------------------------------------------------------------

test("B2: bad id format (tech_325) → E_BAD_ID_FORMAT error", () => {
  const yaml = [
    "id: tech_325",
    "type: tech",
    "title: Bad id format fixture",
    "status: open",
    "section: Backlog YAML and MCP alignment program",
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.length >= 1, "at least one error expected");
  assert.ok(
    result.errors.some((e) => e.startsWith(E_BAD_ID_FORMAT)),
    `expected an error starting with '${E_BAD_ID_FORMAT}', got: ${JSON.stringify(result.errors)}`,
  );
});

// ---------------------------------------------------------------------------
// B3 — Invalid status enum (status: wip)
// ---------------------------------------------------------------------------

test("B3: invalid status enum (wip) → E_BAD_STATUS error", () => {
  const yaml = [
    "id: TECH-325",
    "type: tech",
    "title: Invalid status fixture",
    "status: wip",
    "section: Backlog YAML and MCP alignment program",
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.length >= 1, "at least one error expected");
  assert.ok(
    result.errors.some((e) => e.startsWith(E_BAD_STATUS)),
    `expected an error starting with '${E_BAD_STATUS}', got: ${JSON.stringify(result.errors)}`,
  );
});

// ---------------------------------------------------------------------------
// B4 — depends_on non-empty + depends_on_raw empty → E_EMPTY_DEPENDS_ON_RAW
// ---------------------------------------------------------------------------

test("B4: depends_on non-empty with empty depends_on_raw → E_EMPTY_DEPENDS_ON_RAW error", () => {
  const yaml = [
    "id: TECH-325",
    "type: tech",
    "title: Empty depends_on_raw fixture",
    "status: open",
    "section: Backlog YAML and MCP alignment program",
    "depends_on:",
    "  - TECH-1",
    'depends_on_raw: ""',
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.length >= 1, "at least one error expected");
  assert.ok(
    result.errors.some((e) => e.startsWith(E_EMPTY_DEPENDS_ON_RAW)),
    `expected an error starting with '${E_EMPTY_DEPENDS_ON_RAW}', got: ${JSON.stringify(result.errors)}`,
  );
});

// ---------------------------------------------------------------------------
// B5 — bad task_key → E_BAD_TASK_KEY_FORMAT surfaces via shared core
// ---------------------------------------------------------------------------

test("B5: bad task_key (missing T prefix) → E_BAD_TASK_KEY_FORMAT surfaces via shared import", () => {
  const yaml = [
    "id: TECH-325",
    "type: tech",
    "title: Bad task_key via MCP wrapper",
    "status: open",
    "section: Backlog YAML and MCP alignment program",
    "task_key: 3.3.4",
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(
    result.errors.some((e) => e.startsWith(E_BAD_TASK_KEY_FORMAT)),
    `expected ${E_BAD_TASK_KEY_FORMAT}, got: ${JSON.stringify(result.errors)}`,
  );
});

// ---------------------------------------------------------------------------
// B6 — non-string locator array → E_BAD_LOCATOR_ARRAY_TYPE surfaces
// ---------------------------------------------------------------------------

test("B6: surfaces is plain string → E_BAD_LOCATOR_ARRAY_TYPE surfaces via shared import", () => {
  const yaml = [
    "id: TECH-325",
    "type: tech",
    "title: Bad surfaces type",
    "status: open",
    "section: Backlog YAML and MCP alignment program",
    "surfaces: not-an-array",
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(
    result.errors.some((e) => e.startsWith(E_BAD_LOCATOR_ARRAY_TYPE)),
    `expected ${E_BAD_LOCATOR_ARRAY_TYPE}, got: ${JSON.stringify(result.errors)}`,
  );
});

// ---------------------------------------------------------------------------
// B7 — empty parent_plan → E_EMPTY_PARENT_PLAN surfaces
// ---------------------------------------------------------------------------

test("B7: empty parent_plan → E_EMPTY_PARENT_PLAN surfaces via shared import", () => {
  const yaml = [
    "id: TECH-325",
    "type: tech",
    "title: Empty parent_plan",
    "status: open",
    "section: Backlog YAML and MCP alignment program",
    'parent_plan: ""',
  ].join("\n");

  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(
    result.errors.some((e) => e.startsWith(E_EMPTY_PARENT_PLAN)),
    `expected ${E_EMPTY_PARENT_PLAN}, got: ${JSON.stringify(result.errors)}`,
  );
});
