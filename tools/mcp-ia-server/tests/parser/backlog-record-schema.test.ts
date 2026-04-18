import test from "node:test";
import assert from "node:assert/strict";
import {
  validateBacklogRecord,
  parseYamlScalars,
  E_MISSING_FIELD,
  E_BAD_ID_FORMAT,
  E_BAD_STATUS,
  E_EMPTY_DEPENDS_ON_RAW,
  E_BAD_TASK_KEY_FORMAT,
  E_BAD_LOCATOR_ARRAY_TYPE,
  E_EMPTY_PARENT_PLAN,
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

// ---------------------------------------------------------------------------
// Schema-v2 locator fields — positive cases
// ---------------------------------------------------------------------------

const GOOD_V2_FULL = `
id: TECH-409
type: tech
title: Schema v2 full
status: open
section: Backlog YAML
parent_plan: ia/projects/backlog-yaml-mcp-alignment-master-plan.md
task_key: T3.3.4
surfaces:
  - tools/mcp-ia-server/src/parser/backlog-record-schema.ts
mcp_slices:
  - backlog_record_validate
skill_hints:
  - project-spec-implement
`.trim();

const GOOD_V2_MINIMAL = `
id: TECH-409
type: tech
title: Schema v2 minimal
status: open
section: Backlog YAML
parent_plan: ia/projects/backlog-yaml-mcp-alignment-master-plan.md
task_key: T3.3.4
`.trim();

const GOOD_V1_LEGACY = `
id: TECH-409
type: tech
title: Schema v1 legacy — no locator fields
status: open
section: Backlog YAML
`.trim();

test("schema-v2 full record (all locator fields) → ok: true", () => {
  const result = validateBacklogRecord(GOOD_V2_FULL);
  assert.equal(result.ok, true, `errors: ${JSON.stringify(result.errors)}`);
  assert.deepEqual(result.errors, []);
});

test("schema-v2 minimal record (parent_plan + task_key only) → ok: true", () => {
  const result = validateBacklogRecord(GOOD_V2_MINIMAL);
  assert.equal(result.ok, true, `errors: ${JSON.stringify(result.errors)}`);
  assert.deepEqual(result.errors, []);
});

test("schema-v1 legacy record (zero locator fields) → ok: true (back-compat)", () => {
  const result = validateBacklogRecord(GOOD_V1_LEGACY);
  assert.equal(result.ok, true, `errors: ${JSON.stringify(result.errors)}`);
  assert.deepEqual(result.errors, []);
});

// ---------------------------------------------------------------------------
// E_BAD_TASK_KEY_FORMAT
// ---------------------------------------------------------------------------

test("task_key missing T prefix → E_BAD_TASK_KEY_FORMAT", () => {
  const yaml = `
id: TECH-409
type: tech
title: Bad task_key
status: open
section: Backlog YAML
task_key: 3.3.4
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(
    result.errors.some((e) => e.startsWith(E_BAD_TASK_KEY_FORMAT)),
    `expected ${E_BAD_TASK_KEY_FORMAT}, got: ${JSON.stringify(result.errors)}`,
  );
});

test("task_key with extra text suffix → E_BAD_TASK_KEY_FORMAT", () => {
  const yaml = `
id: TECH-409
type: tech
title: Bad task_key suffix
status: open
section: Backlog YAML
task_key: T3.3.4-extra
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_BAD_TASK_KEY_FORMAT)));
});

test("task_key valid 3-segment format T1.2.3 → ok: true", () => {
  const yaml = `
id: TECH-409
type: tech
title: 3-segment task_key
status: open
section: Backlog YAML
task_key: T1.2.3
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.ok(!result.errors.some((e) => e.startsWith(E_BAD_TASK_KEY_FORMAT)));
});

// ---------------------------------------------------------------------------
// E_BAD_LOCATOR_ARRAY_TYPE
// ---------------------------------------------------------------------------

test("surfaces is a plain string (not array) → E_BAD_LOCATOR_ARRAY_TYPE", () => {
  const yaml = `
id: TECH-409
type: tech
title: Surfaces string
status: open
section: Backlog YAML
surfaces: string-not-array
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(
    result.errors.some((e) => e.startsWith(E_BAD_LOCATOR_ARRAY_TYPE) && e.includes("surfaces")),
    `expected ${E_BAD_LOCATOR_ARRAY_TYPE} for surfaces, got: ${JSON.stringify(result.errors)}`,
  );
});

test("mcp_slices is a plain string → E_BAD_LOCATOR_ARRAY_TYPE", () => {
  const yaml = `
id: TECH-409
type: tech
title: mcp_slices string
status: open
section: Backlog YAML
mcp_slices: backlog_record_validate
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_BAD_LOCATOR_ARRAY_TYPE) && e.includes("mcp_slices")));
});

test("skill_hints is a plain string → E_BAD_LOCATOR_ARRAY_TYPE", () => {
  const yaml = `
id: TECH-409
type: tech
title: skill_hints string
status: open
section: Backlog YAML
skill_hints: project-spec-implement
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some((e) => e.startsWith(E_BAD_LOCATOR_ARRAY_TYPE) && e.includes("skill_hints")));
});

// ---------------------------------------------------------------------------
// E_EMPTY_PARENT_PLAN
// ---------------------------------------------------------------------------

test("parent_plan present but empty string → E_EMPTY_PARENT_PLAN", () => {
  const yaml = `
id: TECH-409
type: tech
title: Empty parent_plan
status: open
section: Backlog YAML
parent_plan: ""
`.trim();
  const result = validateBacklogRecord(yaml);
  assert.equal(result.ok, false);
  assert.ok(
    result.errors.some((e) => e.startsWith(E_EMPTY_PARENT_PLAN)),
    `expected ${E_EMPTY_PARENT_PLAN}, got: ${JSON.stringify(result.errors)}`,
  );
});

test("parent_plan absent entirely → no E_EMPTY_PARENT_PLAN (back-compat)", () => {
  const result = validateBacklogRecord(GOOD_V1_LEGACY);
  assert.ok(!result.errors.some((e) => e.startsWith(E_EMPTY_PARENT_PLAN)));
});
