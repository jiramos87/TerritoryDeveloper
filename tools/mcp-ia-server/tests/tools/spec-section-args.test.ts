/**
 * Tests for spec_section argument normalization (LLM-mistyped parameter names).
 * Phase 1 (TECH-398): normalizeSpecSectionInput now throws { code: "invalid_input" } instead of
 * returning { error: string } when args are missing.
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { normalizeSpecSectionInput, runSpecSectionExtract } from "../../src/tools/spec-section.js";
import { buildRegistry } from "../../src/config.js";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");
const hasSpecs = fs.existsSync(path.join(repoRoot, "ia/specs/glossary.md"));

describe("normalizeSpecSectionInput", () => {
  it("uses spec and section when present", () => {
    const r = normalizeSpecSectionInput({
      spec: "geo",
      section: "14.5",
    });
    assert.equal(r.spec, "geo");
    assert.equal(r.section, "14.5");
    assert.equal(r.max_chars, 3000);
  });

  it("maps key and section_heading to spec and section", () => {
    const r = normalizeSpecSectionInput({
      key: "geo",
      section_heading: 14,
    });
    assert.equal(r.spec, "geo");
    assert.equal(r.section, "14");
  });

  it("maps doc and heading aliases", () => {
    const r = normalizeSpecSectionInput({
      doc: "roads-system",
      heading: "validation",
    });
    assert.equal(r.spec, "roads-system");
    assert.equal(r.section, "validation");
  });

  it("throws invalid_input when spec missing", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ section: "1" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /spec/i);
        return true;
      },
    );
  });

  it("throws invalid_input when section missing", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ spec: "geo" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /section/i);
        return true;
      },
    );
  });

  it("throws invalid_input when both missing (empty object)", () => {
    assert.throws(
      () => normalizeSpecSectionInput({}),
      (e: { code?: string }) => {
        assert.equal(e.code, "invalid_input");
        return true;
      },
    );
  });

  it("respects maxChars alias", () => {
    const r = normalizeSpecSectionInput({
      spec: "g",
      section: "1",
      maxChars: 100,
    });
    assert.equal(r.max_chars, 100);
  });
});

describe("runSpecSectionExtract envelope (Phase 1+2, TECH-398)", () => {
  it(
    "success path: ok true + meta fields populated",
    { skip: !hasSpecs },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const result = runSpecSectionExtract(registry, "glossary", "Grid & Coordinates", 800) as unknown as {
          ok: boolean;
          payload: { key: string; sectionId: string; content: string };
          meta: { section_id: string; line_range: [number, number]; truncated: boolean; total_chars: number };
        };
        assert.equal(result.ok, true);
        assert.ok(result.payload.content.length > 0);
        assert.ok(typeof result.meta.section_id === "string" && result.meta.section_id.length > 0);
        assert.ok(Array.isArray(result.meta.line_range) && result.meta.line_range.length === 2);
        assert.equal(typeof result.meta.truncated, "boolean");
        assert.ok(typeof result.meta.total_chars === "number" && result.meta.total_chars > 0);
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "spec_not_found: throws with code and details.available_keys",
    { skip: !hasSpecs },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        assert.throws(
          () => runSpecSectionExtract(registry, "__no_such_spec__", "intro", 500),
          (e: { code?: string; details?: { available_keys?: string[] } }) => {
            assert.equal(e.code, "spec_not_found");
            assert.ok(Array.isArray(e.details?.available_keys));
            return true;
          },
        );
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "section_not_found: throws with code and details.suggestions",
    { skip: !hasSpecs },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        assert.throws(
          () => runSpecSectionExtract(registry, "glossary", "__no_such_section_xyz__", 500),
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
