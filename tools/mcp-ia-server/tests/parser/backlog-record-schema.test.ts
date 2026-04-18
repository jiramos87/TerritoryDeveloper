import test from "node:test";
import assert from "node:assert/strict";
import {
  validateBacklogRecord,
  parseYamlScalars,
  E_MISSING_FIELD,
  E_BAD_ID_FORMAT,
  E_BAD_STATUS,
  E_EMPTY_DEPENDS_ON_RAW,
} from "../../src/parser/backlog-record-schema.js";

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const GOOD_OPEN = `
id: TECH-100
type: tech
title: Some open task
status: open
section: Infrastructure
`.trim();

const GOOD_CLOSED = `
id: TECH-101
type: tech
title: Some closed task
status: closed
`.trim();

const GOOD_WITH_DEPENDS_ON = `
id: FEAT-200
type: feat
title: Feature with deps
status: open
section: Features
depends_on:
  - TECH-100
depends_on_raw: TECH-100
`.trim();

// ---------------------------------------------------------------------------
// Happy path
// ---------------------------------------------------------------------------

test("validateBacklogRecord: good open record returns ok=true, no errors", () => {
  const result = validateBacklogRecord(GOOD_OPEN);
  assert.equal(result.ok, true);
  assert.deepEqual(result.errors, []);
});

test("validateBacklogRecord: good closed record returns ok=true (no section required)", () => {
  const result = validateBacklogRecord(GOOD_CLOSED);
  assert.equal(result.ok, true);
  assert.deepEqual(result.errors, []);
});

test("validateBacklogRecord: record with depends_on + depends_on_raw returns ok=true", () => {
  const result = validateBacklogRecord(GOOD_WITH_DEPENDS_ON);
  assert.equal(result.ok, true);
  assert.deepEqual(result.errors, []);
});

// ---------------------------------------------------------------------------
// E_MISSING_FIELD
// ---------------------------------------------------------------------------

test("validateBacklogRecord: missing 'id' triggers E_MISSING_FIELD", () => {
  const yaml = `
type: tech
title: No id
status: open
section: Infra
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  const hit = result.errors.some((e) => e.startsWith(E_MISSING_FIELD) && e.includes("'id'"));
  assert.ok(hit, `expected error containing ${E_MISSING_FIELD} + 'id', got: ${JSON.stringify(result.errors)}`);
});

test("validateBacklogRecord: missing 'type' triggers E_MISSING_FIELD", () => {
  const yaml = `
id: TECH-100
title: No type
status: open
section: Infra
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_MISSING_FIELD) && e.includes("'type'")));
});

test("validateBacklogRecord: missing 'title' triggers E_MISSING_FIELD", () => {
  const yaml = `
id: TECH-100
type: tech
status: open
section: Infra
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_MISSING_FIELD) && e.includes("'title'")));
});

test("validateBacklogRecord: missing 'status' triggers E_MISSING_FIELD", () => {
  const yaml = `
id: TECH-100
type: tech
title: No status
section: Infra
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_MISSING_FIELD) && e.includes("'status'")));
});

test("validateBacklogRecord: open record missing 'section' triggers E_MISSING_FIELD", () => {
  const yaml = `
id: TECH-100
type: tech
title: No section
status: open
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_MISSING_FIELD) && e.includes("'section'")));
});

test("validateBacklogRecord: closed record missing 'section' does NOT trigger error", () => {
  const result = validateBacklogRecord(GOOD_CLOSED);
  assert.equal(result.ok, true);
  assert.ok(!result.errors.some((e) => e.includes("'section'")));
});

// ---------------------------------------------------------------------------
// E_BAD_ID_FORMAT
// ---------------------------------------------------------------------------

test("validateBacklogRecord: bad id format triggers E_BAD_ID_FORMAT", () => {
  const yaml = `
id: BAD-NOPE
type: tech
title: Bad id
status: open
section: Infra
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_BAD_ID_FORMAT)));
});

test("validateBacklogRecord: valid id formats pass (TECH, FEAT, BUG, ART, AUDIO)", () => {
  for (const prefix of ["TECH", "FEAT", "BUG", "ART", "AUDIO"]) {
    const yaml = `
id: ${prefix}-1
type: ${prefix.toLowerCase()}
title: Valid prefix test
status: open
section: Infra
`.trim();
    const result = validateBacklogRecord(yaml);
    const badIdErrors = result.errors.filter((e) => e.startsWith(E_BAD_ID_FORMAT));
    assert.deepEqual(badIdErrors, [], `expected no E_BAD_ID_FORMAT for prefix ${prefix}`);
  }
});

// ---------------------------------------------------------------------------
// E_BAD_STATUS
// ---------------------------------------------------------------------------

test("validateBacklogRecord: invalid status triggers E_BAD_STATUS", () => {
  const yaml = `
id: TECH-100
type: tech
title: Bad status
status: in_progress
section: Infra
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_BAD_STATUS)));
});

test("validateBacklogRecord: 'open' and 'closed' are valid statuses", () => {
  for (const status of ["open", "closed"]) {
    const yaml = `
id: TECH-100
type: tech
title: Status test
status: ${status}
section: Infra
`.trim();
    const result = validateBacklogRecord(yaml);
    const statusErrors = result.errors.filter((e) => e.startsWith(E_BAD_STATUS));
    assert.deepEqual(statusErrors, [], `expected no E_BAD_STATUS for status '${status}'`);
  }
});

// ---------------------------------------------------------------------------
// E_EMPTY_DEPENDS_ON_RAW
// ---------------------------------------------------------------------------

test("validateBacklogRecord: depends_on non-empty without depends_on_raw triggers E_EMPTY_DEPENDS_ON_RAW", () => {
  const yaml = `
id: FEAT-200
type: feat
title: Missing depends_on_raw
status: open
section: Features
depends_on:
  - TECH-100
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_EMPTY_DEPENDS_ON_RAW)));
});

test("validateBacklogRecord: depends_on empty list does NOT trigger E_EMPTY_DEPENDS_ON_RAW", () => {
  const yaml = `
id: FEAT-201
type: feat
title: Empty depends_on
status: open
section: Features
depends_on: []
`.trim();
  const result = validateBacklogRecord(yaml);
  const depErrors = result.errors.filter((e) => e.startsWith(E_EMPTY_DEPENDS_ON_RAW));
  assert.deepEqual(depErrors, []);
});

// ---------------------------------------------------------------------------
// parseYamlScalars (exported helper)
// ---------------------------------------------------------------------------

test("parseYamlScalars: parses scalar fields correctly", () => {
  const scalars = parseYamlScalars(GOOD_OPEN);
  assert.equal(scalars["id"], "TECH-100");
  assert.equal(scalars["type"], "tech");
  assert.equal(scalars["title"], "Some open task");
  assert.equal(scalars["status"], "open");
  assert.equal(scalars["section"], "Infrastructure");
});

test("parseYamlScalars: parses list fields correctly", () => {
  const scalars = parseYamlScalars(GOOD_WITH_DEPENDS_ON);
  assert.ok(Array.isArray(scalars["depends_on"]));
  assert.deepEqual(scalars["depends_on"], ["TECH-100"]);
  assert.equal(scalars["depends_on_raw"], "TECH-100");
});
