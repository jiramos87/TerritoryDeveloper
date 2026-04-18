/**
 * backlog_list — filter matrix, ordering, scope switch, empty-result shape.
 * Locks filter semantics before Stage 2.3 backlog_search extensions.
 *
 * Two tiers:
 *   Unit  — applyFilters / compareIssues with synthetic ParsedBacklogIssue[].
 *   Integration — tmpdir yaml fixtures + REPO_ROOT env override → scope switch.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  applyFilters,
  compareIssues,
} from "../../src/tools/backlog-list.js";
import {
  parseAllBacklogIssuesWithMeta,
  type ParsedBacklogIssue,
} from "../../src/parser/backlog-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeIssue(overrides: Partial<ParsedBacklogIssue>): ParsedBacklogIssue {
  return {
    issue_id: "TECH-1",
    title: "Test issue",
    status: "open",
    backlog_section: "Test section A",
    raw_markdown: "",
    ...overrides,
  };
}

function ids(issues: ParsedBacklogIssue[]): string[] {
  return issues.map((i) => i.issue_id);
}

// ---------------------------------------------------------------------------
// Unit tier — applyFilters
// ---------------------------------------------------------------------------

const FIXTURE_ISSUES: ParsedBacklogIssue[] = [
  makeIssue({ issue_id: "TECH-100", backlog_section: "Test section A", priority: "high", type: "infrastructure / MCP tooling", status: "open" }),
  makeIssue({ issue_id: "TECH-200", backlog_section: "Test section B", priority: "medium", type: "infrastructure / MCP tooling", status: "open" }),
  makeIssue({ issue_id: "FEAT-50",  backlog_section: "Test section A", priority: "high", type: "game / roads", status: "open" }),
  makeIssue({ issue_id: "BUG-10",   backlog_section: "Test section B", priority: "low",  type: "game / roads", status: "open" }),
  makeIssue({ issue_id: "AUDIO-5",  backlog_section: "Test section A", priority: "medium", type: "audio / SFX", status: "open" }),
];

test("U1 — no filters returns all issues", () => {
  const result = applyFilters(FIXTURE_ISSUES, {});
  assert.equal(result.length, FIXTURE_ISSUES.length);
});

test("U2 — section filter substring case-insensitive", () => {
  const result = applyFilters(FIXTURE_ISSUES, { section: "section a" });
  assert.deepEqual(ids(result).sort(), ["AUDIO-5", "FEAT-50", "TECH-100"]);
});

test("U3 — priority filter exact case-insensitive", () => {
  const result = applyFilters(FIXTURE_ISSUES, { priority: "HIGH" });
  assert.deepEqual(ids(result).sort(), ["FEAT-50", "TECH-100"]);
});

test("U4 — type filter exact case-insensitive", () => {
  const result = applyFilters(FIXTURE_ISSUES, { type: "INFRASTRUCTURE / MCP TOOLING" });
  assert.deepEqual(ids(result).sort(), ["TECH-100", "TECH-200"]);
});

test("U5 — status filter exact case-insensitive", () => {
  const result = applyFilters(FIXTURE_ISSUES, { status: "OPEN" });
  assert.equal(result.length, FIXTURE_ISSUES.length);
});

test("U6 — multi-filter AND (section + priority)", () => {
  const result = applyFilters(FIXTURE_ISSUES, { section: "section b", priority: "medium" });
  assert.deepEqual(ids(result), ["TECH-200"]);
});

test("U7 — empty result (no matches) → [], no throw", () => {
  const result = applyFilters(FIXTURE_ISSUES, { section: "nonexistent-xyz" });
  assert.deepEqual(result, []);
  assert.equal(result.length, 0);
});

// ---------------------------------------------------------------------------
// Unit tier — compareIssues ordering
// ---------------------------------------------------------------------------

test("U8 — compareIssues: prefix asc (AUDIO < BUG < FEAT < TECH), numeric id desc within prefix", () => {
  const issues: ParsedBacklogIssue[] = [
    makeIssue({ issue_id: "TECH-100" }),
    makeIssue({ issue_id: "BUG-10" }),
    makeIssue({ issue_id: "FEAT-50" }),
    makeIssue({ issue_id: "AUDIO-5" }),
    makeIssue({ issue_id: "TECH-200" }),
    makeIssue({ issue_id: "TECH-50" }),
  ];
  const sorted = [...issues].sort(compareIssues);
  const sortedIds = ids(sorted);
  // AUDIO first, then BUG, then FEAT, then TECH (desc: 200 > 100 > 50)
  assert.deepEqual(sortedIds, ["AUDIO-5", "BUG-10", "FEAT-50", "TECH-200", "TECH-100", "TECH-50"]);
});

// ---------------------------------------------------------------------------
// Integration tier — scope switch via tmpdir yaml fixtures
// ---------------------------------------------------------------------------

function makeTempBacklog(): { dir: string; cleanup: () => void } {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "backlog-list-test-"));
  fs.mkdirSync(path.join(dir, "ia", "backlog"), { recursive: true });
  fs.mkdirSync(path.join(dir, "ia", "backlog-archive"), { recursive: true });
  return { dir, cleanup: () => fs.rmSync(dir, { recursive: true, force: true }) };
}

function writeYaml(dir: string, subdir: string, id: string, fields: Record<string, string>): void {
  const lines = Object.entries(fields).map(([k, v]) => `${k}: ${v}`);
  const content = lines.join("\n") + "\n";
  fs.writeFileSync(path.join(dir, "ia", subdir, `${id}.yaml`), content, "utf8");
}

test("I1 — scope: open returns only open ids", () => {
  const { dir, cleanup } = makeTempBacklog();
  const prevRoot = process.env["REPO_ROOT"];
  try {
    process.env["REPO_ROOT"] = dir;
    // Open records
    writeYaml(dir, "backlog", "TECH-900", { id: "TECH-900", title: "Open A", type: "infrastructure / MCP tooling", status: "open", section: "Test section A", priority: "high" });
    writeYaml(dir, "backlog", "TECH-901", { id: "TECH-901", title: "Open B", type: "infrastructure / MCP tooling", status: "open", section: "Test section B", priority: "medium" });
    // Archive record
    writeYaml(dir, "backlog-archive", "TECH-800", { id: "TECH-800", title: "Archived", type: "infrastructure / MCP tooling", status: "completed", section: "Test section A", priority: "high" });

    const { records } = parseAllBacklogIssuesWithMeta(dir, "open");
    const resultIds = ids(records).sort();
    assert.deepEqual(resultIds, ["TECH-900", "TECH-901"]);
  } finally {
    if (prevRoot === undefined) delete process.env["REPO_ROOT"];
    else process.env["REPO_ROOT"] = prevRoot;
    cleanup();
  }
});

test("I2 — scope: archive returns only archive ids", () => {
  const { dir, cleanup } = makeTempBacklog();
  const prevRoot = process.env["REPO_ROOT"];
  try {
    process.env["REPO_ROOT"] = dir;
    writeYaml(dir, "backlog", "TECH-900", { id: "TECH-900", title: "Open A", type: "infrastructure / MCP tooling", status: "open", section: "Test section A", priority: "high" });
    writeYaml(dir, "backlog-archive", "TECH-800", { id: "TECH-800", title: "Archived", type: "infrastructure / MCP tooling", status: "completed", section: "Test section A", priority: "high" });

    const { records } = parseAllBacklogIssuesWithMeta(dir, "archive");
    assert.deepEqual(ids(records), ["TECH-800"]);
  } finally {
    if (prevRoot === undefined) delete process.env["REPO_ROOT"];
    else process.env["REPO_ROOT"] = prevRoot;
    cleanup();
  }
});

test("I3 — scope: all returns union of open + archive ids", () => {
  const { dir, cleanup } = makeTempBacklog();
  const prevRoot = process.env["REPO_ROOT"];
  try {
    process.env["REPO_ROOT"] = dir;
    writeYaml(dir, "backlog", "TECH-900", { id: "TECH-900", title: "Open A", type: "infrastructure / MCP tooling", status: "open", section: "Test section A", priority: "high" });
    writeYaml(dir, "backlog", "TECH-901", { id: "TECH-901", title: "Open B", type: "infrastructure / MCP tooling", status: "open", section: "Test section B", priority: "medium" });
    writeYaml(dir, "backlog-archive", "TECH-800", { id: "TECH-800", title: "Archived", type: "infrastructure / MCP tooling", status: "completed", section: "Test section A", priority: "high" });

    const { records } = parseAllBacklogIssuesWithMeta(dir, "all");
    const resultIds = ids(records).sort();
    assert.deepEqual(resultIds, ["TECH-800", "TECH-900", "TECH-901"]);
  } finally {
    if (prevRoot === undefined) delete process.env["REPO_ROOT"];
    else process.env["REPO_ROOT"] = prevRoot;
    cleanup();
  }
});
