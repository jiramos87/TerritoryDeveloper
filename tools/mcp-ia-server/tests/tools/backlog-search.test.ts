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
    const bug14 = issues.find((i) => i.issue_id === "BUG-14");
    assert.ok(bug14, "BUG-14 should be in open backlog");
    assert.equal(bug14!.status, "open");
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
