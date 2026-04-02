/**
 * Tests for spec_section argument normalization (LLM-mistyped parameter names).
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { normalizeSpecSectionInput } from "../../src/tools/spec-section.js";

describe("normalizeSpecSectionInput", () => {
  it("uses spec and section when present", () => {
    const r = normalizeSpecSectionInput({
      spec: "geo",
      section: "14.5",
    });
    assert.ok(!("error" in r));
    assert.equal(r.spec, "geo");
    assert.equal(r.section, "14.5");
    assert.equal(r.max_chars, 3000);
  });

  it("maps key and section_heading to spec and section", () => {
    const r = normalizeSpecSectionInput({
      key: "geo",
      section_heading: 14,
    });
    assert.ok(!("error" in r));
    assert.equal(r.spec, "geo");
    assert.equal(r.section, "14");
  });

  it("maps doc and heading aliases", () => {
    const r = normalizeSpecSectionInput({
      doc: "roads-system",
      heading: "validation",
    });
    assert.ok(!("error" in r));
    assert.equal(r.spec, "roads-system");
    assert.equal(r.section, "validation");
  });

  it("returns error when spec missing", () => {
    const r = normalizeSpecSectionInput({ section: "1" });
    assert.ok("error" in r);
    assert.match(r.error, /spec/i);
  });

  it("returns error when section missing", () => {
    const r = normalizeSpecSectionInput({ spec: "geo" });
    assert.ok("error" in r);
    assert.match(r.error, /section/i);
  });

  it("respects maxChars alias", () => {
    const r = normalizeSpecSectionInput({
      spec: "g",
      section: "1",
      maxChars: 100,
    });
    assert.ok(!("error" in r));
    assert.equal(r.max_chars, 100);
  });
});
