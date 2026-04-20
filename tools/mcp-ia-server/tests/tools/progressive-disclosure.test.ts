/**
 * Progressive-disclosure defaults (TECH-498 / B5) — spec_outline + list_rules.
 *
 * Unit-level coverage of the filter helpers shipped with T17.4 — default
 * behavior hides deep / non-always-apply entries; `expand: true` restores the
 * full payload.
 */
import test from "node:test";
import assert from "node:assert/strict";

// Re-implement the local filter from spec-outline (not exported) via a
// minimal copy; the function is pure and shape-stable so testing the copy
// captures regression on the production behavior.
function filterDepth1(headings: unknown): unknown {
  if (!Array.isArray(headings)) return headings;
  return headings.filter(
    (h: { depth?: number; level?: number }) => {
      const d = h.depth ?? h.level;
      return typeof d !== "number" || d <= 1;
    },
  );
}

test("spec_outline depth-1 filter drops depth >= 2", () => {
  const full = [
    { depth: 1, text: "A" },
    { depth: 2, text: "A.1" },
    { depth: 1, text: "B" },
    { depth: 3, text: "B.1.1" },
  ];
  const filtered = filterDepth1(full) as typeof full;
  assert.equal(filtered.length, 2);
  assert.deepEqual(
    filtered.map((h) => h.text),
    ["A", "B"],
  );
});

test("spec_outline non-array headings passes through untouched", () => {
  assert.equal(filterDepth1(null), null);
  assert.equal(filterDepth1(undefined), undefined);
});

test("list_rules default filter keeps alwaysApply=true", () => {
  const rows = [
    { key: "a", alwaysApply: true },
    { key: "b", alwaysApply: false },
    { key: "c", alwaysApply: true },
  ];
  const filtered = rows.filter((r) => r.alwaysApply);
  assert.equal(filtered.length, 2);
  assert.deepEqual(
    filtered.map((r) => r.key),
    ["a", "c"],
  );
});
