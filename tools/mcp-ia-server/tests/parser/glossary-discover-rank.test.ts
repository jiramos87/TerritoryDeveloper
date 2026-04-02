/**
 * glossary_discover Phase A ranking: fixed weights and definition-only hits (FEAT-45 AC).
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { GlossaryEntry } from "../../src/parser/types.js";
import {
  DISCOVER_WEIGHT_DEFINITION,
  rankGlossaryDiscover,
  tokenizeDiscoverText,
} from "../../src/parser/glossary-discover-rank.js";

const fixture: GlossaryEntry[] = [
  {
    term: "ZebraTermOnly",
    definition: "Unrelated definition about zebras.",
    specReference: "simulation-system §1",
    category: "Test",
  },
  {
    term: "NeighborWipeConcept",
    definition:
      "Manual road trace preparation may adjust adjacent cells; neighbors and wipe semantics for stroke.",
    specReference: "roads-system §3",
    category: "Roads & Bridges",
  },
  {
    term: "QuietRow",
    definition: "Something else entirely.",
    specReference: "ui-design-system §1",
    category: "UI",
  },
];

describe("tokenizeDiscoverText", () => {
  it("splits on punctuation and dedupes", () => {
    const t = tokenizeDiscoverText("manual road, trace  trace!");
    assert.deepEqual(t.sort(), ["manual", "road", "trace"].sort());
  });
});

describe("rankGlossaryDiscover", () => {
  it("surfaces a term when keywords appear only in definition (FEAT-45)", () => {
    const q =
      "manual road trace wipes neighbors adjacent cells stroke semantics";
    const ranked = rankGlossaryDiscover(fixture, q, { maxResults: 5 });
    assert.ok(ranked.length >= 1);
    assert.equal(ranked[0]!.entry.term, "NeighborWipeConcept");
    assert.ok(ranked[0]!.matchReasons.includes("definition"));
    assert.ok(!ranked[0]!.matchReasons.includes("term"));
  });

  it("ranks definition hits using fixed weight constant (regression on scale)", () => {
    const q = "neighbors wipe";
    const ranked = rankGlossaryDiscover(fixture, q, { maxResults: 3 });
    const hit = ranked.find((r) => r.entry.term === "NeighborWipeConcept");
    assert.ok(hit);
    const neighborAlone = rankGlossaryDiscover(fixture, "neighbors", {
      maxResults: 1,
    });
    assert.ok(neighborAlone[0]);
    assert.equal(
      neighborAlone[0]!.entry.term,
      "NeighborWipeConcept",
      "substring token neighbors matches definition",
    );
    assert.ok(
      hit!.score >= DISCOVER_WEIGHT_DEFINITION,
      "at least one definition token matched at full weight",
    );
  });

  it("returns empty when nothing matches", () => {
    const ranked = rankGlossaryDiscover(fixture, "quantum plasma xyz", {
      maxResults: 5,
    });
    assert.equal(ranked.length, 0);
  });
});
