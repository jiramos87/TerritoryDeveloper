/**
 * Tests for project spec closeout parsing (TECH-58).
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  buildProjectSpecCloseoutDigest,
  extractCitedIssueIds,
  extractProjectSpecSections,
  parseIssueIdFromSpecHeader,
  sectionKeyFromH2Title,
  splitProjectSpecH2Sections,
} from "../../src/parser/project-spec-closeout-parse.js";

const SAMPLE = `# TECH-99 — Sample

> **Issue:** [TECH-99](../../BACKLOG.md)

## 1. Summary

Uses **HeightMap** and **TECH-48** for discovery.

## 6. Decision Log

| Date | Decision |
|------|----------|
| 2026-01-01 | See BUG-01 |

## 10. Lessons Learned

- Migrate to glossary.
- Link FEAT-02.

## Open Questions (resolve before / during implementation)

None — tooling only.
`;

describe("project-spec-closeout-parse", () => {
  it("classifies H2 titles", () => {
    assert.equal(sectionKeyFromH2Title("1. Summary"), "summary");
    assert.equal(sectionKeyFromH2Title("Goals and Non-Goals"), "goals");
    assert.equal(
      sectionKeyFromH2Title(
        "Open Questions (resolve before / during implementation)",
      ),
      "open_questions",
    );
  });

  it("extracts sections and issue id from header", () => {
    const sections = extractProjectSpecSections(SAMPLE);
    assert.ok(sections.summary?.includes("HeightMap"));
    assert.ok(sections.decision_log?.includes("BUG-01"));
    assert.ok(sections.lessons_learned?.includes("glossary"));
    assert.equal(parseIssueIdFromSpecHeader(SAMPLE), "TECH-99");
  });

  it("extracts cited issue ids uniquely", () => {
    const ids = extractCitedIssueIds(SAMPLE);
    assert.ok(ids.includes("TECH-48"));
    assert.ok(ids.includes("TECH-99"));
    assert.ok(ids.includes("BUG-01"));
    assert.ok(ids.includes("FEAT-02"));
  });

  it("builds digest from markdown body", () => {
    const d = buildProjectSpecCloseoutDigest(SAMPLE, null);
    assert.equal(d.schema_version, 1);
    assert.equal(d.issue_id, "TECH-99");
    assert.ok(d.suggested_english_keywords.length > 0);
    assert.ok(d.checklist_hints?.G1?.length);
  });

  it("splitProjectSpecH2Sections keeps body under headings", () => {
    const parts = splitProjectSpecH2Sections("## A\n\nx\n\n## B\n\ny");
    assert.equal(parts.length, 2);
    assert.equal(parts[0].title, "A");
    assert.match(parts[0].body, /x/);
  });
});
