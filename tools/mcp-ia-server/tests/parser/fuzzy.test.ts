import test from "node:test";
import assert from "node:assert/strict";
import {
  levenshteinDistance,
  fuzzyFind,
  fuzzyFindByHeadingTitle,
  fuzzyScoreAgainstTextOrTokens,
  normalizeGlossaryQuery,
} from "../../src/parser/fuzzy.js";

test("levenshteinDistance identical", () => {
  assert.equal(levenshteinDistance("a", "a"), 0);
});

test("levenshteinDistance one edit", () => {
  assert.equal(levenshteinDistance("a", "b"), 1);
});

test("fuzzyFind exact wins", () => {
  const items = ["alpha", "beta"];
  const r = fuzzyFind("beta", items, (x) => x, { maxResults: 3 });
  assert.equal(r[0]?.item, "beta");
  assert.equal(r[0]?.score, 0);
  assert.equal(r[0]?.matchType, "exact");
});

test("fuzzyFind filters by threshold", () => {
  const items = ["zzzzzzzz"];
  const r = fuzzyFind("aaaa", items, (x) => x, { threshold: 0.4, maxResults: 5 });
  assert.equal(r.length, 0);
});

test("normalizeGlossaryQuery strips brackets", () => {
  assert.equal(
    normalizeGlossaryQuery("HeightMap[x,y]").toLowerCase(),
    "heightmap",
  );
});

test("fuzzyScoreAgainstTextOrTokens typo Briges vs bridges token", () => {
  const p = fuzzyScoreAgainstTextOrTokens("Briges", "### 13.4 Bridges and water approach");
  assert.ok(p);
  assert.ok(p!.score < 0.3);
  assert.equal(p!.matchType, "fuzzy");
});

test("fuzzyFindByHeadingTitle picks shorter title on tie", () => {
  const items = [
    { id: "a", t: "## 13. Streets, interstates, bridges, shared validation" },
    { id: "b", t: "### 13.4 Bridges and water approach" },
  ];
  const r = fuzzyFindByHeadingTitle("Briges", items, (x) => x.t, {
    threshold: 0.4,
    maxResults: 3,
  });
  assert.equal(r[0]?.item.id, "b");
});

test("fuzzyFind typo hight map vs HeightMap collapsed", () => {
  const collapse = (s: string) => s.toLowerCase().replace(/\s+/g, "");
  const items = [{ term: "HeightMap" }];
  const cq = collapse("hight map");
  const r = fuzzyFind(cq, items, (e) => collapse(e.term), {
    threshold: 0.4,
    maxResults: 3,
  });
  assert.ok(r[0]);
  assert.ok(r[0]!.score < 0.3);
});
