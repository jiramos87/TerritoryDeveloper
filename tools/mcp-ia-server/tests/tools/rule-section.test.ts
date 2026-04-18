/**
 * Tests for rule_section tool (TECH-399 Phase 3).
 *
 * Covers: happy path envelope, section_not_found, spec_not_found, invalid_input.
 * Uses real registry when ia/rules/*.md files are present (same guard as spec-section-args.test.ts).
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";
import {
  normalizeRuleSectionInput,
  runRuleSectionExtract,
} from "../../src/tools/rule-content.js";
import { buildRegistry } from "../../src/config.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");
const hasRules = fs.existsSync(
  path.join(repoRoot, "ia", "rules", "invariants.md"),
);

// ---------------------------------------------------------------------------
// normalizeRuleSectionInput
// ---------------------------------------------------------------------------

describe("normalizeRuleSectionInput", () => {
  it("maps rule and section when present", () => {
    const r = normalizeRuleSectionInput({ rule: "invariants", section: "3" });
    assert.equal(r.rule, "invariants");
    assert.equal(r.section, "3");
    assert.equal(r.max_chars, 3000);
  });

  it("maps section_heading alias", () => {
    const r = normalizeRuleSectionInput({
      rule: "roads",
      section_heading: "guardrails",
    });
    assert.equal(r.section, "guardrails");
  });

  it("maps heading alias", () => {
    const r = normalizeRuleSectionInput({
      rule: "roads",
      heading: "guardrails",
    });
    assert.equal(r.section, "guardrails");
  });

  it("coerces numeric section to string", () => {
    const r = normalizeRuleSectionInput({ rule: "invariants", section: 1 });
    assert.equal(r.section, "1");
  });

  it("throws invalid_input when rule missing", () => {
    assert.throws(
      () => normalizeRuleSectionInput({ section: "guardrails" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /rule/i);
        return true;
      },
    );
  });

  it("throws invalid_input when section missing", () => {
    assert.throws(
      () => normalizeRuleSectionInput({ rule: "invariants" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /section/i);
        return true;
      },
    );
  });

  it("throws invalid_input when both missing", () => {
    assert.throws(
      () => normalizeRuleSectionInput({}),
      (e: { code?: string }) => {
        assert.equal(e.code, "invalid_input");
        return true;
      },
    );
  });

  it("respects maxChars alias", () => {
    const r = normalizeRuleSectionInput({
      rule: "invariants",
      section: "1",
      maxChars: 500,
    });
    assert.equal(r.max_chars, 500);
  });
});

// ---------------------------------------------------------------------------
// runRuleSectionExtract (integration — skipped when ia/rules absent)
// ---------------------------------------------------------------------------

describe("runRuleSectionExtract envelope (TECH-399)", () => {
  it(
    "happy path: ok true + meta fields populated",
    { skip: !hasRules },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        // "invariants" rule has a "Guardrails" heading (or similar top-level section).
        // Use a known heading from invariants.md — "System Invariants" or "Guardrails".
        const result = runRuleSectionExtract(
          registry,
          "invariants",
          "Guardrails",
          1000,
        ) as {
          ok: boolean;
          payload: {
            key: string;
            sectionId: string;
            title: string;
            content: string;
          };
          meta: {
            section_id: string;
            line_range: [number, number];
            truncated: boolean;
            total_chars: number;
          };
        };
        assert.equal(result.ok, true);
        assert.ok(result.payload.content.length > 0);
        assert.equal(result.payload.key, "invariants");
        assert.ok(
          typeof result.meta.section_id === "string" &&
            result.meta.section_id.length > 0,
        );
        assert.ok(
          Array.isArray(result.meta.line_range) &&
            result.meta.line_range.length === 2,
        );
        assert.equal(typeof result.meta.truncated, "boolean");
        assert.ok(
          typeof result.meta.total_chars === "number" &&
            result.meta.total_chars > 0,
        );
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "spec_not_found: unknown rule key throws with details.available_rules",
    { skip: !hasRules },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        assert.throws(
          () =>
            runRuleSectionExtract(
              registry,
              "__no_such_rule__",
              "guardrails",
              500,
            ),
          (e: {
            code?: string;
            details?: { available_rules?: { key: string }[] };
          }) => {
            assert.equal(e.code, "spec_not_found");
            assert.ok(Array.isArray(e.details?.available_rules));
            return true;
          },
        );
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "section_not_found: bogus heading throws with details.suggestions",
    { skip: !hasRules },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        assert.throws(
          () =>
            runRuleSectionExtract(
              registry,
              "invariants",
              "__no_such_section_xyz__",
              500,
            ),
          (e: { code?: string; details?: { suggestions?: string[] } }) => {
            assert.equal(e.code, "section_not_found");
            assert.ok(Array.isArray(e.details?.suggestions));
            return true;
          },
        );
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );
});
