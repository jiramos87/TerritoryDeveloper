/**
 * Unit tests for Tarjan SCC implementation (TECH-2976).
 *
 * Covers the §Test Blueprint cases: empty graph, linear chain (singleton
 * SCCs only), single multi-node SCC, multiple disjoint SCCs, self-loops.
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { tarjanScc } from "../../src/ia-db/tarjan-scc.js";

function adj(pairs: Array<[string, string]>): Map<string, string[]> {
  const m = new Map<string, string[]>();
  for (const [u, v] of pairs) {
    const list = m.get(u) ?? [];
    list.push(v);
    m.set(u, list);
  }
  return m;
}

describe("tarjanScc (TECH-2976)", () => {
  it("empty graph returns empty SCC list", () => {
    const r = tarjanScc([], new Map());
    assert.deepEqual(r.sccs, []);
    assert.deepEqual(r.multiNodeSccs, []);
    assert.deepEqual(r.selfLoops, []);
  });

  it("linear chain A→B→C produces 3 singleton SCCs and no cycles", () => {
    const edges = adj([
      ["A", "B"],
      ["B", "C"],
    ]);
    const r = tarjanScc(["A", "B", "C"], edges);
    assert.equal(r.sccs.length, 3);
    assert.ok(r.sccs.every((c) => c.length === 1));
    assert.deepEqual(r.multiNodeSccs, []);
    assert.deepEqual(r.selfLoops, []);
  });

  it("cycle A→B→C→A produces one multi-node SCC of size 3", () => {
    const edges = adj([
      ["A", "B"],
      ["B", "C"],
      ["C", "A"],
    ]);
    const r = tarjanScc(["A", "B", "C"], edges);
    assert.equal(r.multiNodeSccs.length, 1);
    assert.equal(r.multiNodeSccs[0]!.length, 3);
    assert.deepEqual(
      r.multiNodeSccs[0]!.slice().sort(),
      ["A", "B", "C"],
    );
    assert.deepEqual(r.selfLoops, []);
  });

  it("self-loop A→A produces singleton SCC and selfLoops entry", () => {
    const edges = adj([["A", "A"]]);
    const r = tarjanScc(["A"], edges);
    assert.deepEqual(r.selfLoops, ["A"]);
    assert.equal(r.sccs.length, 1);
    assert.deepEqual(r.multiNodeSccs, []);
  });

  it("multiple disjoint SCCs surface independently", () => {
    // Graph: {A↔B} and {C↔D}, plus singleton E.
    const edges = adj([
      ["A", "B"],
      ["B", "A"],
      ["C", "D"],
      ["D", "C"],
    ]);
    const r = tarjanScc(["A", "B", "C", "D", "E"], edges);
    assert.equal(r.multiNodeSccs.length, 2);
    const flatSorted = r.multiNodeSccs
      .map((c) => c.slice().sort().join(","))
      .sort();
    assert.deepEqual(flatSorted, ["A,B", "C,D"]);
    // E is its own singleton SCC.
    assert.ok(r.sccs.some((c) => c.length === 1 && c[0] === "E"));
  });

  it("does not stack-overflow on deep linear chains (iterative impl)", () => {
    const N = 5000;
    const nodes = Array.from({ length: N }, (_, i) => `n${i}`);
    const pairs: Array<[string, string]> = [];
    for (let i = 0; i < N - 1; i++) pairs.push([nodes[i]!, nodes[i + 1]!]);
    const r = tarjanScc(nodes, adj(pairs));
    assert.equal(r.sccs.length, N);
    assert.deepEqual(r.multiNodeSccs, []);
  });
});
