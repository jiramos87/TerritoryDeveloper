/**
 * Unit tests: project_spec_closeout_digest tool — envelope shape + error branches.
 *
 * ACs (TECH-403 §7b):
 *   1. wrapTool applied — resolve failure → { ok:false, error:{ code:"invalid_input" } }.
 *   2. read failure → { ok:false, error:{ code:"internal_error" } }.
 *   3. Happy path → ok:true + payload.
 *
 * ACs (TECH-458 §7b — G3):
 *   4. Fixture spec with all 4 new sections → audit, code_review, code_fix_plan, closeout_plan populated.
 *   5. Fixture spec without new sections → those keys absent or empty string.
 */

import test from "node:test";
import assert from "node:assert/strict";
import { wrapTool } from "../../src/envelope.js";
import {
  sectionKeyFromH2Title,
  buildProjectSpecCloseoutDigest,
} from "../../src/parser/project-spec-closeout-parse.js";

// ---------------------------------------------------------------------------
// resolve failure → invalid_input
// ---------------------------------------------------------------------------

test("closeout_digest: resolve failure → invalid_input envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    throw { code: "invalid_input" as const, message: "Could not resolve spec path" };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "invalid_input");
  }
});

// ---------------------------------------------------------------------------
// read failure → internal_error
// ---------------------------------------------------------------------------

test("closeout_digest: read failure → internal_error envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    throw {
      code: "internal_error" as const,
      message: "Could not read project spec: ENOENT",
      details: { spec_path: "ia/projects/TECH-X.md" },
    };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "internal_error");
    assert.ok(result.error.message.includes("Could not read"));
  }
});

// ---------------------------------------------------------------------------
// Happy path — ok:true + digest payload
// ---------------------------------------------------------------------------

test("closeout_digest: happy path → ok:true + payload", async () => {
  const digest = { sections: [], issue_ids: [], spec_path: "ia/projects/TECH-1.md" };
  const handler = wrapTool(async (_input: void) => digest);
  const result = await handler(undefined as void);
  assert.equal(result.ok, true);
  if (result.ok) {
    assert.ok("sections" in (result.payload as object));
  }
});

// ---------------------------------------------------------------------------
// TECH-458 G3 — 4 new §sections recognised by sectionKeyFromH2Title
// ---------------------------------------------------------------------------

test("sectionKeyFromH2Title maps 'Audit' → 'audit'", () => {
  assert.equal(sectionKeyFromH2Title("Audit"), "audit");
  assert.equal(sectionKeyFromH2Title("AUDIT"), "audit");
});

test("sectionKeyFromH2Title maps 'Code Review' → 'code_review'", () => {
  assert.equal(sectionKeyFromH2Title("Code Review"), "code_review");
  assert.equal(sectionKeyFromH2Title("code review"), "code_review");
});

test("sectionKeyFromH2Title maps 'Code Fix Plan' → 'code_fix_plan'", () => {
  assert.equal(sectionKeyFromH2Title("Code Fix Plan"), "code_fix_plan");
  assert.equal(sectionKeyFromH2Title("CODE FIX PLAN"), "code_fix_plan");
});

test("sectionKeyFromH2Title maps 'Closeout Plan' → 'closeout_plan'", () => {
  assert.equal(sectionKeyFromH2Title("Closeout Plan"), "closeout_plan");
  assert.equal(sectionKeyFromH2Title("CLOSEOUT PLAN"), "closeout_plan");
});

// ---------------------------------------------------------------------------
// TECH-458 G3 — buildProjectSpecCloseoutDigest populates all 4 new keys
// ---------------------------------------------------------------------------

const FIXTURE_WITH_NEW_SECTIONS = `
# TECH-1 — Test spec

> **Issue:** TECH-1
> **Status:** In Review

## 1. Summary

This is the summary.

## Audit

Audit content here.

## Code Review

Code review content here.

## Code Fix Plan

Code fix plan content here.

## Closeout Plan

Closeout plan content here.

## 10. Lessons Learned

Some lessons.
`.trim();

test("buildProjectSpecCloseoutDigest populates 4 new sections when present", () => {
  const digest = buildProjectSpecCloseoutDigest(
    FIXTURE_WITH_NEW_SECTIONS,
    "ia/projects/TECH-1.md",
    "TECH-1",
  );
  assert.ok(digest.sections.audit && digest.sections.audit.length > 0, "audit should be non-empty");
  assert.ok(digest.sections.code_review && digest.sections.code_review.length > 0, "code_review should be non-empty");
  assert.ok(digest.sections.code_fix_plan && digest.sections.code_fix_plan.length > 0, "code_fix_plan should be non-empty");
  assert.ok(digest.sections.closeout_plan && digest.sections.closeout_plan.length > 0, "closeout_plan should be non-empty");
});

const FIXTURE_WITHOUT_NEW_SECTIONS = `
# TECH-2 — Test spec no new sections

> **Issue:** TECH-2
> **Status:** In Review

## 1. Summary

Summary text only.

## 10. Lessons Learned

Nothing yet.
`.trim();

test("buildProjectSpecCloseoutDigest returns undefined/absent for missing new sections", () => {
  const digest = buildProjectSpecCloseoutDigest(
    FIXTURE_WITHOUT_NEW_SECTIONS,
    "ia/projects/TECH-2.md",
    "TECH-2",
  );
  // Missing sections should be absent (undefined) — not error, consistent with existing behavior
  assert.equal(digest.sections.audit, undefined);
  assert.equal(digest.sections.code_review, undefined);
  assert.equal(digest.sections.code_fix_plan, undefined);
  assert.equal(digest.sections.closeout_plan, undefined);
});
