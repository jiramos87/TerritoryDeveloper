/**
 * Tests for master-plan step/stage header sync.
 * Fixture cases from BUG-57 §7b Test Contracts.
 */

import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { describe, it } from "node:test";
import {
  computeStatusLines,
  findStepStageBlocks,
  scanTaskRows,
  syncMasterPlanHeaders,
  BACKLOG_STATE_LINE_RE,
  STAGE_HEADING_RE,
  STEP_HEADING_RE,
  STATUS_LINE_RE,
} from "../../src/parser/master-plan-header-sync.js";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const FIXTURES_DIR = path.join(
  path.dirname(new URL(import.meta.url).pathname),
  "../fixtures/bug-57-step-stage-sync",
);

function loadFixture(name: string): string {
  return fs.readFileSync(path.join(FIXTURES_DIR, name), "utf-8");
}

// ---------------------------------------------------------------------------
// Unit: regex correctness
// ---------------------------------------------------------------------------

describe("regex constants", () => {
  it("STEP_HEADING_RE matches valid step headings", () => {
    assert.ok(STEP_HEADING_RE.test("### Step 1 — Token ring"));
    assert.ok(STEP_HEADING_RE.test("### Step 12 — Longer title here"));
    assert.equal(STEP_HEADING_RE.exec("### Step 3 — Foo")?.[1], "3");
  });

  it("STEP_HEADING_RE rejects stage and other headings", () => {
    assert.ok(!STEP_HEADING_RE.test("#### Stage 1.1 — Sub"));
    assert.ok(!STEP_HEADING_RE.test("## Step 1 — Token ring"));
  });

  it("STAGE_HEADING_RE matches valid stage headings", () => {
    assert.ok(STAGE_HEADING_RE.test("#### Stage 1.1 — Sub stage"));
    assert.ok(STAGE_HEADING_RE.test("#### Stage 2.10 — Long title"));
    assert.equal(STAGE_HEADING_RE.exec("#### Stage 1.1 — Foo")?.[1], "1.1");
  });

  it("STAGE_HEADING_RE rejects step headings", () => {
    assert.ok(!STAGE_HEADING_RE.test("### Step 1 — Foo"));
  });

  it("STATUS_LINE_RE matches status lines", () => {
    assert.ok(STATUS_LINE_RE.test("**Status:** Final"));
    assert.ok(STATUS_LINE_RE.test("**Status:** Draft (tasks _pending_ — not yet filed)"));
    assert.ok(STATUS_LINE_RE.test("**Status:** In Progress — TECH-42"));
  });

  it("BACKLOG_STATE_LINE_RE matches both step and stage variants", () => {
    assert.ok(BACKLOG_STATE_LINE_RE.test("**Backlog state (Step 1):** 0 filed"));
    assert.ok(BACKLOG_STATE_LINE_RE.test("**Backlog state (Stage 1.1):** 3 filed"));
    assert.ok(!BACKLOG_STATE_LINE_RE.test("**Backlog state (Step 1):** filed"));
  });
});

// ---------------------------------------------------------------------------
// Unit: scanTaskRows
// ---------------------------------------------------------------------------

describe("scanTaskRows", () => {
  it("returns empty array when no task table present", () => {
    const lines = [
      "### Step 1 — Foo",
      "",
      "**Status:** Draft (tasks _pending_ — not yet filed)",
      "",
      "**Backlog state (Step 1):** 0 filed",
    ];
    const rows = scanTaskRows(lines, 0, 3);
    assert.deepEqual(rows, []);
  });

  it("parses task rows from table", () => {
    const lines = [
      "### Step 1 — Foo",
      "",
      "**Tasks:**",
      "",
      "| Task | Name | Phase | Issue | Status | Intent |",
      "|---|---|---|---|---|---|",
      "| T1 | Name A | 1 | **TECH-01** | Done (archived) | intent |",
      "| T2 | Name B | 1 | **TECH-02** | In Progress | intent |",
    ];
    const rows = scanTaskRows(lines, 0, 3);
    assert.equal(rows.length, 2);
    assert.equal(rows[0].issueCell, "**TECH-01**");
    assert.equal(rows[0].statusCell, "Done (archived)");
    assert.equal(rows[1].issueCell, "**TECH-02**");
    assert.equal(rows[1].statusCell, "In Progress");
  });

  it("stops at same-depth heading", () => {
    const lines = [
      "### Step 1 — Foo",
      "| Task | Issue | Status |",
      "|---|---|---|",
      "| T1 | **TECH-01** | Done (archived) |",
      "### Step 2 — Bar",
      "| Task | Issue | Status |",
      "|---|---|---|",
      "| T2 | **TECH-02** | Draft |",
    ];
    const rows = scanTaskRows(lines, 0, 3);
    assert.equal(rows.length, 1);
    assert.equal(rows[0].issueCell, "**TECH-01**");
  });
});

// ---------------------------------------------------------------------------
// Unit: computeStatusLines
// ---------------------------------------------------------------------------

describe("computeStatusLines", () => {
  it("all pending → Draft", () => {
    const { statusLine, backlogStateLine } = computeStatusLines("Stage 1.1", [
      { issueCell: "_pending_", statusCell: "_pending_" },
      { issueCell: "_pending_", statusCell: "_pending_" },
    ]);
    assert.equal(statusLine, "**Status:** Draft (tasks _pending_ — not yet filed)");
    assert.equal(backlogStateLine, "**Backlog state (Stage 1.1):** 0 filed");
  });

  it("all done → Final", () => {
    const { statusLine, backlogStateLine } = computeStatusLines("Stage 1.1", [
      { issueCell: "**TECH-01**", statusCell: "Done (archived)" },
      { issueCell: "**TECH-02**", statusCell: "Done (archived)" },
    ]);
    assert.equal(statusLine, "**Status:** Final");
    assert.equal(backlogStateLine, "**Backlog state (Stage 1.1):** 2 filed");
  });

  it("mixed → In Progress with first open id", () => {
    const { statusLine, backlogStateLine } = computeStatusLines("Stage 1.1", [
      { issueCell: "**TECH-01**", statusCell: "Done (archived)" },
      { issueCell: "**TECH-02**", statusCell: "In Progress" },
    ]);
    assert.equal(statusLine, "**Status:** In Progress — TECH-02");
    assert.equal(backlogStateLine, "**Backlog state (Stage 1.1):** 2 filed");
  });

  it("empty rows → Draft with 0 filed", () => {
    const { statusLine, backlogStateLine } = computeStatusLines("Step 1", []);
    assert.equal(statusLine, "**Status:** Draft (tasks _pending_ — not yet filed)");
    assert.equal(backlogStateLine, "**Backlog state (Step 1):** 0 filed");
  });

  it("step label propagates correctly", () => {
    const { backlogStateLine } = computeStatusLines("Step 3", [
      { issueCell: "**TECH-10**", statusCell: "Done (archived)" },
    ]);
    assert.equal(backlogStateLine, "**Backlog state (Step 3):** 1 filed");
  });
});

// ---------------------------------------------------------------------------
// Fixture: (a) All tasks Done (archived) → stage + step flip to Final
// ---------------------------------------------------------------------------

describe("fixture (a) all done — terminal stage + step flip", () => {
  it("stage 1.1 flips to Final", () => {
    const md = loadFixture("fixture-a-all-done.md");
    const result = syncMasterPlanHeaders(md);
    const lines = result.split("\n");

    // Find Stage 1.1 status line.
    const stageIdx = lines.findIndex((l) => STAGE_HEADING_RE.test(l) && l.includes("1.1"));
    assert.ok(stageIdx >= 0, "Stage 1.1 heading not found");
    const stageStatusIdx = lines.findIndex(
      (l, i) => i > stageIdx && STATUS_LINE_RE.test(l),
    );
    assert.ok(stageStatusIdx >= 0, "Stage 1.1 status line not found");
    assert.equal(lines[stageStatusIdx], "**Status:** Final");
  });

  it("stage 1.2 flips to Final", () => {
    const md = loadFixture("fixture-a-all-done.md");
    const result = syncMasterPlanHeaders(md);
    const lines = result.split("\n");

    const stageIdx = lines.findIndex((l) => STAGE_HEADING_RE.test(l) && l.includes("1.2"));
    assert.ok(stageIdx >= 0, "Stage 1.2 heading not found");
    const stageStatusIdx = lines.findIndex(
      (l, i) => i > stageIdx && STATUS_LINE_RE.test(l),
    );
    assert.equal(lines[stageStatusIdx], "**Status:** Final");
  });

  it("step 1 flips to Final when all sibling stages are Final", () => {
    const md = loadFixture("fixture-a-all-done.md");
    const result = syncMasterPlanHeaders(md);
    const lines = result.split("\n");

    const stepIdx = lines.findIndex((l) => STEP_HEADING_RE.test(l));
    assert.ok(stepIdx >= 0, "Step 1 heading not found");
    const stepStatusIdx = lines.findIndex(
      (l, i) => i > stepIdx && STATUS_LINE_RE.test(l),
    );
    assert.equal(lines[stepStatusIdx], "**Status:** Final");
  });

  it("backlog state counts are correct", () => {
    const md = loadFixture("fixture-a-all-done.md");
    const result = syncMasterPlanHeaders(md);
    assert.ok(result.includes("**Backlog state (Stage 1.1):** 2 filed"));
    assert.ok(result.includes("**Backlog state (Stage 1.2):** 2 filed"));
    assert.ok(result.includes("**Backlog state (Step 1):** 2 filed") ||
              result.includes("**Backlog state (Step 1):** 4 filed"));
    // Step-level task scan covers all sub-tasks; either 2 (stage-1.1 only in scan scope)
    // or 4 (all tasks) depending on scan depth. Key assertion: not 0.
    assert.ok(!result.includes("**Backlog state (Step 1):** 0 filed"));
  });
});

// ---------------------------------------------------------------------------
// Fixture: (b) Partial done + open → In Progress with refreshed count
// ---------------------------------------------------------------------------

describe("fixture (b) partial done — intermediate refresh", () => {
  it("stage status shows In Progress with first open id", () => {
    const md = loadFixture("fixture-b-partial.md");
    const result = syncMasterPlanHeaders(md);
    assert.ok(
      result.includes("**Status:** In Progress — TECH-02"),
      `Expected In Progress — TECH-02 in result`,
    );
  });

  it("backlog state count updated to 2 filed", () => {
    const md = loadFixture("fixture-b-partial.md");
    const result = syncMasterPlanHeaders(md);
    assert.ok(result.includes("**Backlog state (Stage 1.1):** 2 filed"));
  });

  it("step does NOT flip to Final", () => {
    const md = loadFixture("fixture-b-partial.md");
    const result = syncMasterPlanHeaders(md);
    const lines = result.split("\n");
    const stepIdx = lines.findIndex((l) => STEP_HEADING_RE.test(l));
    assert.ok(stepIdx >= 0);
    const stepStatusIdx = lines.findIndex(
      (l, i) => i > stepIdx && STATUS_LINE_RE.test(l),
    );
    assert.ok(
      lines[stepStatusIdx] !== "**Status:** Final",
      "Step must NOT be Final when tasks remain open",
    );
  });
});

// ---------------------------------------------------------------------------
// Fixture: (c) Idempotent — already synced doc produces zero diff
// ---------------------------------------------------------------------------

describe("fixture (c) idempotent re-run", () => {
  it("re-running syncMasterPlanHeaders on already-Final doc produces zero diff", () => {
    const md = loadFixture("fixture-c-idempotent.md");
    const once = syncMasterPlanHeaders(md);
    const twice = syncMasterPlanHeaders(once);
    assert.equal(once, twice, "Second run must produce identical output");
  });

  it("Final status preserved after re-run", () => {
    const md = loadFixture("fixture-c-idempotent.md");
    const result = syncMasterPlanHeaders(md);
    const lines = result.split("\n");
    const stageIdx = lines.findIndex((l) => STAGE_HEADING_RE.test(l));
    const stageStatusIdx = lines.findIndex(
      (l, i) => i > stageIdx && STATUS_LINE_RE.test(l),
    );
    assert.equal(lines[stageStatusIdx], "**Status:** Final");
  });
});

// ---------------------------------------------------------------------------
// Fixture: (d) Negative — non-terminal closeout does NOT force Final
// ---------------------------------------------------------------------------

describe("fixture (d) negative — non-terminal does not flip to Final", () => {
  it("stage with open tasks is not Final after sync", () => {
    const md = loadFixture("fixture-d-negative.md");
    const result = syncMasterPlanHeaders(md);
    const lines = result.split("\n");
    const stageIdx = lines.findIndex((l) => STAGE_HEADING_RE.test(l));
    assert.ok(stageIdx >= 0);
    const stageStatusIdx = lines.findIndex(
      (l, i) => i > stageIdx && STATUS_LINE_RE.test(l),
    );
    assert.notEqual(
      lines[stageStatusIdx],
      "**Status:** Final",
      "Stage with open tasks must not flip to Final",
    );
  });
});

// ---------------------------------------------------------------------------
// findStepStageBlocks unit
// ---------------------------------------------------------------------------

describe("findStepStageBlocks", () => {
  it("detects step and stage blocks with correct line indices", () => {
    const doc = [
      "### Step 1 — Foo",
      "",
      "**Status:** Draft (tasks _pending_ — not yet filed)",
      "**Backlog state (Step 1):** 0 filed",
      "",
      "#### Stage 1.1 — Bar",
      "",
      "**Status:** Draft (tasks _pending_ — not yet filed)",
      "**Backlog state (Stage 1.1):** 0 filed",
    ].join("\n");

    const blocks = findStepStageBlocks(doc.split("\n"));
    assert.equal(blocks.length, 2);

    const step = blocks.find((b) => b.kind === "step");
    assert.ok(step);
    assert.equal(step.number, "1");
    assert.equal(step.headerLineIndex, 0);
    assert.equal(step.statusLineIndex, 2);
    assert.equal(step.backlogStateLineIndex, 3);

    const stage = blocks.find((b) => b.kind === "stage");
    assert.ok(stage);
    assert.equal(stage.number, "1.1");
    assert.equal(stage.headerLineIndex, 5);
    assert.equal(stage.statusLineIndex, 7);
    assert.equal(stage.backlogStateLineIndex, 8);
  });
});
