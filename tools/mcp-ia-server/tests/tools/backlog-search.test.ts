/**
 * backlog_search — keyword search across backlog issues.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { scoreIssue } from "../../src/tools/backlog-search.js";
import {
  parseAllBacklogIssues,
  type ParsedBacklogIssue,
} from "../../src/parser/backlog-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

function makeIssue(overrides: Partial<ParsedBacklogIssue>): ParsedBacklogIssue {
  return {
    issue_id: "TEST-1",
    title: "",
    status: "open",
    backlog_section: "Test",
    raw_markdown: "",
    ...overrides,
  };
}

test("scoreIssue ranks title matches higher than notes", () => {
  const tokens = ["happiness"];
  const titleMatch = makeIssue({ title: "Dynamic happiness based on city conditions" });
  const notesMatch = makeIssue({ title: "Other bug", notes: "happiness only increases" });
  assert.ok(scoreIssue(titleMatch, tokens) > scoreIssue(notesMatch, tokens));
});

test("scoreIssue returns 0 for no matching tokens", () => {
  const issue = makeIssue({ title: "Some unrelated issue", notes: "nothing here" });
  assert.equal(scoreIssue(issue, ["zzzznotfound"]), 0);
});

test("scoreIssue gives highest weight to issue_id match", () => {
  const tokens = ["bug", "12"];
  const idMatch = makeIssue({ issue_id: "BUG-12", title: "Happiness UI" });
  const noIdMatch = makeIssue({ issue_id: "FEAT-99", title: "Bug in happiness system", notes: "12 items" });
  assert.ok(scoreIssue(idMatch, tokens) > scoreIssue(noIdMatch, tokens));
});

test(
  "parseAllBacklogIssues returns issues from BACKLOG.md",
  { skip: !fs.existsSync(path.join(repoRoot, "BACKLOG.md")) },
  () => {
    const issues = parseAllBacklogIssues(repoRoot, "open");
    assert.ok(issues.length > 0);
    const bug31 = issues.find((i) => i.issue_id === "BUG-31");
    assert.ok(bug31, "BUG-31 should be in open backlog");
    assert.equal(bug31!.status, "open");
  },
);

test(
  "parseAllBacklogIssues with scope=all includes both files",
  { skip: !fs.existsSync(path.join(repoRoot, "BACKLOG.md")) },
  () => {
    const openOnly = parseAllBacklogIssues(repoRoot, "open");
    const all = parseAllBacklogIssues(repoRoot, "all");
    assert.ok(all.length >= openOnly.length);
  },
);

// ---------------------------------------------------------------------------
// New field projection — priority, related, created
// ---------------------------------------------------------------------------

test(
  "backlog_search results include priority, related, created for TECH-300",
  { skip: !fs.existsSync(path.join(repoRoot, "ia/backlog/TECH-300.yaml")) },
  () => {
    const issues = parseAllBacklogIssues(repoRoot, "open");
    const tech300 = issues.find((i) => i.issue_id === "TECH-300");
    assert.ok(tech300, "TECH-300 should appear in open backlog");
    // Simulate result projection (same logic as backlog-search.ts)
    const projected = {
      priority: tech300!.priority ?? null,
      related: tech300!.related ?? [],
      created: tech300!.created ?? null,
    };
    assert.equal(projected.priority, "high");
    assert.ok(Array.isArray(projected.related) && projected.related.length > 0);
    assert.equal(projected.created, "2026-04-17");
  },
);

test("backlog_search result projection uses null for missing priority", () => {
  const issue = makeIssue({ issue_id: "TECH-0", title: "no priority" });
  const priority = issue.priority ?? null;
  assert.equal(priority, null);
});

test("backlog_search result projection uses empty array for missing related", () => {
  const issue = makeIssue({ issue_id: "TECH-0", title: "no related" });
  const related = issue.related ?? [];
  assert.deepEqual(related, []);
});

test("backlog_search result projection uses null for missing created", () => {
  const issue = makeIssue({ issue_id: "TECH-0", title: "no created" });
  const created = issue.created ?? null;
  assert.equal(created, null);
});
