/**
 * Tests for spec_section argument normalization (Stage 2.3 TECH-426: alias rejection).
 * Aliases `key`/`doc`/`document_key`/`section_heading`/`section_id`/`heading`/`maxChars` now
 * reject with { code: "invalid_input" } and a canonical-name hint.
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

  it("respects max_chars canonical param", () => {
    const r = normalizeSpecSectionInput({ spec: "g", section: "1", max_chars: 500 });
    assert.equal(r.max_chars, 500);
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

  // Alias rejection tests (Stage 2.3 TECH-426)
  it("rejects alias 'key' with canonical hint 'spec'", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ key: "geo", section: "1" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /key/);
        assert.match(e.message ?? "", /spec/);
        return true;
      },
    );
  });

  it("rejects alias 'document_key' with canonical hint 'spec'", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ document_key: "geo", section: "1" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /document_key/);
        assert.match(e.message ?? "", /spec/);
        return true;
      },
    );
  });

  it("rejects alias 'doc' with canonical hint 'spec'", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ doc: "roads-system", section: "1" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /doc/);
        assert.match(e.message ?? "", /spec/);
        return true;
      },
    );
  });

  it("rejects alias 'section_heading' with canonical hint 'section'", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ spec: "geo", section_heading: "intro" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /section_heading/);
        assert.match(e.message ?? "", /section/);
        return true;
      },
    );
  });

  it("rejects alias 'section_id' with canonical hint 'section'", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ spec: "geo", section_id: "14" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /section_id/);
        assert.match(e.message ?? "", /section/);
        return true;
      },
    );
  });

  it("rejects alias 'heading' with canonical hint 'section'", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ spec: "geo", heading: "validation" }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /heading/);
        assert.match(e.message ?? "", /section/);
        return true;
      },
    );
  });

  it("rejects alias 'maxChars' with canonical hint 'max_chars'", () => {
    assert.throws(
      () => normalizeSpecSectionInput({ spec: "geo", section: "1", maxChars: 100 }),
      (e: { code?: string; message?: string }) => {
        assert.equal(e.code, "invalid_input");
        assert.match(e.message ?? "", /maxChars/);
        assert.match(e.message ?? "", /max_chars/);
        return true;
      },
    );
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
