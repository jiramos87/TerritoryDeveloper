import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  mergeJournalKeywords,
  tokenizeForJournalSearch,
} from "../../src/ia-db/journal-repo.js";

describe("mergeJournalKeywords", () => {
  it("dedupes and merges summary keywords with body tokens", () => {
    const k = mergeJournalKeywords(
      ["HeightMap", "heightmap"],
      "Road stroke and wet run validation.",
      20,
    );
    assert.ok(k.includes("heightmap"));
    assert.ok(k.includes("stroke") || k.includes("road"));
  });
});

describe("tokenizeForJournalSearch", () => {
  it("extracts distinct tokens from prose", () => {
    const t = tokenizeForJournalSearch(
      "We need persistence for Decision Log entries in Postgres.",
    );
    assert.ok(t.length >= 2);
    assert.ok(t.every((x) => x === x.toLowerCase()));
  });
});
