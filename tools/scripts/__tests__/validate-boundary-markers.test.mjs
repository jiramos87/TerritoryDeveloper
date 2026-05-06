// TECH-15905 — unit tests for validate-boundary-markers.mjs
// Tests the boundary marker linter that gates ship-cycle Pass A pre-flip.

import assert from "node:assert";
import { test } from "node:test";
import { validateBoundaryMarkers } from "../validate-boundary-markers.mjs";

// ---------------------------------------------------------------------------
// Happy-path tests
// ---------------------------------------------------------------------------

test("well-formed single task markers pass", () => {
  const text = [
    "<!-- TASK:TECH-100 START -->",
    "Some implementation body",
    "<!-- TASK:TECH-100 END -->",
  ].join("\n");
  const result = validateBoundaryMarkers(text);
  assert.strictEqual(result.ok, true);
  assert.deepStrictEqual(result.starts, ["TECH-100"]);
  assert.deepStrictEqual(result.ends, ["TECH-100"]);
  assert.deepStrictEqual(result.errors, []);
});

test("multiple balanced task markers pass", () => {
  const text = [
    "<!-- TASK:TECH-100 START -->",
    "body A",
    "<!-- TASK:TECH-100 END -->",
    "<!-- TASK:FEAT-200 START -->",
    "body B",
    "<!-- TASK:FEAT-200 END -->",
  ].join("\n");
  const result = validateBoundaryMarkers(text);
  assert.strictEqual(result.ok, true);
  assert.strictEqual(result.starts.length, 2);
});

test("empty text is valid (no markers)", () => {
  const result = validateBoundaryMarkers("");
  assert.strictEqual(result.ok, true);
  assert.deepStrictEqual(result.starts, []);
});

test("prose with no markers is valid", () => {
  const result = validateBoundaryMarkers("Some text without any markers.");
  assert.strictEqual(result.ok, true);
});

// ---------------------------------------------------------------------------
// Failure tests (red-stage proof: malformed_marker_blocks_flip)
// ---------------------------------------------------------------------------

test("malformed_marker_blocks_flip — missing closing --> detected", () => {
  const text = [
    "<!-- TASK:FEAT-99 START",
    "Some body",
    "<!-- TASK:FEAT-99 END -->",
  ].join("\n");
  const result = validateBoundaryMarkers(text);
  assert.strictEqual(result.ok, false);
  assert.ok(result.errors.some((e) => e.includes("malformed_marker")));
});

test("missing END marker detected as unbalanced", () => {
  const text = [
    "<!-- TASK:TECH-500 START -->",
    "body without end",
  ].join("\n");
  const result = validateBoundaryMarkers(text);
  assert.strictEqual(result.ok, false);
  assert.ok(result.errors.some((e) => e.includes("TECH-500") && e.includes("unbalanced")));
});

test("orphan END marker detected", () => {
  const text = "<!-- TASK:TECH-600 END -->";
  const result = validateBoundaryMarkers(text);
  assert.strictEqual(result.ok, false);
  assert.ok(result.errors.some((e) => e.includes("orphan_end_marker")));
});

test("extra END marker detected", () => {
  const text = [
    "<!-- TASK:TECH-700 START -->",
    "body",
    "<!-- TASK:TECH-700 END -->",
    "<!-- TASK:TECH-700 END -->",
  ].join("\n");
  const result = validateBoundaryMarkers(text);
  assert.strictEqual(result.ok, false);
  assert.ok(result.errors.some((e) => e.includes("TECH-700")));
});
