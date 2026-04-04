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
  extractCitedIssueIds,
  isSoftDependencyMention,
  resolveDependsOnStatus,
} from "../../src/parser/backlog-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

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

test("extractCitedIssueIds dedupes and preserves order", () => {
  assert.deepEqual(extractCitedIssueIds("Depends on: **TECH-37**, **TECH-38**"), [
    "TECH-37",
    "TECH-38",
  ]);
  assert.deepEqual(
    extractCitedIssueIds("none (soft: **TECH-50** **§ Completed**)"),
    ["TECH-50"],
  );
  assert.deepEqual(extractCitedIssueIds("no ids here"), []);
});

test("isSoftDependencyMention true only when id appears after soft:", () => {
  const line =
    "Depends on: **TECH-37** (soft: **TECH-38** for **heavy** tools)";
  assert.equal(isSoftDependencyMention(line, "TECH-37"), false);
  assert.equal(isSoftDependencyMention(line, "TECH-38"), true);
});

test("resolveDependsOnStatus marks completed TECH-61 for TECH-62 line", {
  skip: !fs.existsSync(path.join(repoRoot, "BACKLOG.md")),
}, () => {
  const line =
    "Depends on: **TECH-61** **§ Completed** (soft: shared **Node** helpers)";
  const rows = resolveDependsOnStatus(repoRoot, line);
  const m61 = rows.find((r) => r.id === "TECH-61");
  assert.ok(m61);
  assert.equal(m61!.status, "completed");
  assert.equal(m61!.satisfied, true);
  assert.equal(m61!.soft_only, false);
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

test(
  "parseBacklogIssue loads open TECH-36 from repo BACKLOG.md",
  { skip: !fs.existsSync(path.join(repoRoot, "BACKLOG.md")) },
  () => {
    const prev = process.env.REPO_ROOT;
    process.env.REPO_ROOT = repoRoot;
    try {
      const p = parseBacklogIssue(repoRoot, "TECH-36");
      assert.ok(p);
      assert.equal(p!.issue_id, "TECH-36");
      assert.equal(p!.status, "open");
      assert.ok(
        p!.title.toLowerCase().includes("computational") ||
          p!.title.toLowerCase().includes("program") ||
          p!.title.toLowerCase().includes("umbrella"),
      );
      assert.ok(p!.raw_markdown.includes("TECH-36"));
    } finally {
      process.env.REPO_ROOT = prev;
    }
  },
);
