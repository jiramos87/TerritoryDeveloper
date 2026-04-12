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
  resolveProjectSpecFile,
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

  it("builds digest with spec_path", () => {
    const d = buildProjectSpecCloseoutDigest(
      SAMPLE,
      "ia/projects/TECH-99.md",
      null,
    );
    assert.equal(d.schema_version, 1);
    assert.equal(d.issue_id, "TECH-99");
    assert.equal(d.spec_path, "ia/projects/TECH-99.md");
    assert.ok(d.suggested_english_keywords.length > 0);
    assert.ok(d.checklist_hints?.G1?.length);
  });

  it("resolveProjectSpecFile accepts issue_id only", () => {
    const r = resolveProjectSpecFile("/repo", { issue_id: "tech-75" });
    assert.equal(r.ok, true);
    if (r.ok) {
      assert.match(r.absPath, /TECH-75\.md$/);
      // /repo is fake here, so neither lookup hits and the default applies.
      assert.equal(r.relPosix, "ia/projects/TECH-75.md");
    }
  });

  it("resolveProjectSpecFile accepts ia/projects descriptive spec_path", () => {
    const r = resolveProjectSpecFile("/repo", {
      spec_path: "ia/projects/TECH-99-descriptive-name.md",
    });
    assert.equal(r.ok, true);
    if (r.ok) {
      assert.equal(r.relPosix, "ia/projects/TECH-99-descriptive-name.md");
      assert.equal(r.issue_id, "TECH-99");
    }
  });

  it("resolveProjectSpecFile rejects traversal", () => {
    const r = resolveProjectSpecFile("/repo", {
      spec_path: "ia/projects/../secrets.md",
    });
    assert.equal(r.ok, false);
  });

  it("splitProjectSpecH2Sections keeps body under headings", () => {
    const parts = splitProjectSpecH2Sections("## A\n\nx\n\n## B\n\ny");
    assert.equal(parts.length, 2);
    assert.equal(parts[0].title, "A");
    assert.match(parts[0].body, /x/);
  });
});
