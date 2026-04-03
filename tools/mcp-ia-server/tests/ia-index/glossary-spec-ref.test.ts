/**
 * Glossary Spec column → spec_key / anchor parsing for I2 index.
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  glossarySpecCellToIndex,
  primarySpecSegment,
} from "../../src/ia-index/glossary-spec-ref.js";

describe("glossarySpecCellToIndex", () => {
  it("maps geo §1 to geography spec and section 1", () => {
    assert.deepEqual(glossarySpecCellToIndex("geo §1, §2"), {
      spec_key: "isometric-geography-system",
      anchor: "1",
    });
  });

  it("maps persist §Save to persistence spec and save slug", () => {
    assert.deepEqual(glossarySpecCellToIndex("persist §Save"), {
      spec_key: "persistence-system",
      anchor: "save",
    });
  });

  it("maps ARCHITECTURE.md to architecture key", () => {
    assert.deepEqual(glossarySpecCellToIndex("ARCHITECTURE.md"), {
      spec_key: "architecture",
      anchor: "",
    });
  });

  it("returns null for empty or em dash", () => {
    assert.equal(glossarySpecCellToIndex(""), null);
    assert.equal(glossarySpecCellToIndex("—"), null);
  });
});

describe("primarySpecSegment", () => {
  it("takes first comma-separated segment", () => {
    assert.equal(primarySpecSegment("geo §1, §2"), "geo §1");
  });
});
