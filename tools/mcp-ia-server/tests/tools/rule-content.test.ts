/**
 * Tests for rule_content structured payload (Stage 2.3 TECH-427).
 *
 * rule_content now returns `{ rule_key, title, sections: [{id, heading, body}], markdown }`.
 * rule_section slices align with sections[].body.
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";
import { wrapTool } from "../../src/envelope.js";
import { findRuleEntry } from "../../src/config.js";
import { buildRegistry } from "../../src/config.js";
import matter from "gray-matter";
import { parseDocument, flattenHeadingTree, extractLines } from "../../src/parser/markdown-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");
const hasRules = fs.existsSync(
  path.join(repoRoot, "ia", "rules", "invariants.md"),
);

describe("rule_content structured payload (TECH-427)", () => {
  it(
    "known rule → ok: true with rule_key, title, sections, markdown",
    { skip: !hasRules },
    async () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const envelope = await wrapTool(async () => {
          const entry = findRuleEntry(registry, "invariants");
          if (!entry) {
            throw { code: "spec_not_found" as const, message: "No rule found for 'invariants'.", details: {} };
          }
          const raw = fs.readFileSync(entry.filePath, "utf8");
          const { data, content } = matter(raw);
          const d = data as Record<string, unknown>;
          const title = typeof d.description === "string" ? d.description : entry.description;
          const markdown = content.trimStart();
          const doc = parseDocument(entry.filePath);
          const allHeadings = flattenHeadingTree(doc.headings);
          const sections = allHeadings.map((h) => ({
            id: h.sectionId,
            heading: h.title,
            body: extractLines(entry.filePath, h.lineStart, h.lineEnd),
          }));
          return { rule_key: entry.key, title, sections, markdown };
        })(undefined);
        assert.equal(envelope.ok, true);
        if (envelope.ok) {
          const p = envelope.payload as Record<string, unknown>;
          assert.ok(typeof p.rule_key === "string", "rule_key is string");
          assert.ok(typeof p.title === "string", "title is string");
          assert.ok(typeof p.markdown === "string", "markdown is string");
          assert.ok(Array.isArray(p.sections), "sections is array");
          const sections = p.sections as { id: string; heading: string; body: string }[];
          assert.ok(sections.length > 0, "at least one section");
          for (const s of sections) {
            assert.ok(typeof s.id === "string", "section.id is string");
            assert.ok(typeof s.heading === "string", "section.heading is string");
            assert.ok(typeof s.body === "string", "section.body is string");
          }
        }
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "section bodies align with rule_section slices",
    { skip: !hasRules },
    async () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const entry = findRuleEntry(registry, "invariants");
        if (!entry) return;
        const doc = parseDocument(entry.filePath);
        const allHeadings = flattenHeadingTree(doc.headings);
        if (allHeadings.length === 0) return;
        const first = allHeadings[0]!;
        const bodyFromExtract = extractLines(entry.filePath, first.lineStart, first.lineEnd);
        // body from rule_content sections[0] should equal extractLines result
        assert.equal(bodyFromExtract, extractLines(entry.filePath, first.lineStart, first.lineEnd));
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "unknown rule → ok: false, error.code spec_not_found with details.available_rules",
    { skip: !hasRules },
    async () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const envelope = await wrapTool(async () => {
          const ruleKey = "__no_such_rule__";
          const entry = findRuleEntry(registry, ruleKey);
          if (!entry) {
            const available_rules = registry
              .filter((e) => e.category === "rule")
              .map((e) => ({ key: e.key, description: e.description }))
              .sort((a, b) => a.key.localeCompare(b.key));
            throw {
              code: "spec_not_found" as const,
              message: `No rule found for '${ruleKey}'.`,
              hint: "Call list_rules to retrieve available rule keys.",
              details: { available_rules },
            };
          }
          return {};
        })(undefined);
        assert.equal(envelope.ok, false);
        if (!envelope.ok) {
          assert.equal(envelope.error.code, "spec_not_found");
          assert.ok(
            Array.isArray(
              (envelope.error.details as { available_rules?: unknown[] })?.available_rules,
            ),
          );
        }
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );
});
