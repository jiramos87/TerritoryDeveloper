import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  findIssueHeaderLine,
  sliceIssueBlock,
  scrapeIssueFields,
  normalizeIssueId,
  parseBacklogIssue,
} from "../../src/parser/backlog-parser.js";

const FIXTURE = `## High Priority

- [ ] **BUG-20** — Parent issue
  - Type: fix
  - Files: \`A.cs\`
  - [ ] **TECH-01** — Nested child
  - Type: refactor
  - Files: \`B.cs\`

- [ ] **BUG-12** — Other

## Completed (last 30 days)

- [x] **FEAT-99** — Shipped (2026-04-02)
  - Type: feature
  - Notes: Done.
`;

test("normalizeIssueId uppercases prefix and lowercases letter suffix", () => {
  assert.equal(normalizeIssueId("bug-37"), "BUG-37");
  assert.equal(normalizeIssueId("FEAT-37B"), "FEAT-37b");
});

test("findIssueHeaderLine tracks section and finds nested TECH-01", () => {
  const lines = FIXTURE.split("\n");
  const t = findIssueHeaderLine(lines, "TECH-01");
  assert.ok(t);
  assert.equal(t!.backlog_section, "High Priority");
  assert.equal(t!.lineIndex, lines.findIndex((l) => l.includes("**TECH-01**")));
});

test("sliceIssueBlock for nested TECH-01 stops at sibling indent", () => {
  const lines = FIXTURE.split("\n");
  const idx = lines.findIndex((l) => l.includes("**TECH-01**"));
  const block = sliceIssueBlock(lines, idx);
  assert.ok(block.every((l) => !l.includes("**BUG-12**")));
  assert.equal(block.length, 4);
  assert.ok(block[0]!.includes("TECH-01"));
});

test("sliceIssueBlock for BUG-20 includes nested TECH-01", () => {
  const lines = FIXTURE.split("\n");
  const idx = lines.findIndex((l) => l.includes("**BUG-20**"));
  const block = sliceIssueBlock(lines, idx);
  const text = block.join("\n");
  assert.ok(text.includes("TECH-01"));
  assert.ok(text.includes("BUG-20"));
  assert.ok(!text.includes("BUG-12"));
});

test("scrapeIssueFields first line per key", () => {
  const lines = [
    "- [ ] **X-1** — T",
    "  - Type: bug",
    "  - Files: `a.cs`",
    "  - Notes: line one",
    "  - Notes: ignored second",
  ];
  const f = scrapeIssueFields(lines);
  assert.equal(f.type, "bug");
  assert.equal(f.files, "`a.cs`");
  assert.equal(f.notes, "line one");
});

test("findIssueHeaderLine returns null for missing id", () => {
  const lines = FIXTURE.split("\n");
  assert.equal(findIssueHeaderLine(lines, "BUG-99"), null);
});

test("completed status from header line via full parse path", () => {
  const lines = FIXTURE.split("\n");
  const idx = lines.findIndex((l) => l.includes("**FEAT-99**"));
  const block = sliceIssueBlock(lines, idx);
  assert.ok(block[0]!.includes("[x]"));
});

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

test(
  "parseBacklogIssue loads open TECH-41 from repo BACKLOG.md",
  { skip: !fs.existsSync(path.join(repoRoot, "BACKLOG.md")) },
  () => {
    const prev = process.env.REPO_ROOT;
    process.env.REPO_ROOT = repoRoot;
    try {
      const p = parseBacklogIssue(repoRoot, "TECH-41");
      assert.ok(p);
      assert.equal(p!.issue_id, "TECH-41");
      assert.equal(p!.status, "open");
      assert.ok(
        p!.title.toLowerCase().includes("json") ||
          p!.title.toLowerCase().includes("payload"),
      );
      assert.ok(p!.raw_markdown.includes("TECH-41"));
    } finally {
      process.env.REPO_ROOT = prev;
    }
  },
);
