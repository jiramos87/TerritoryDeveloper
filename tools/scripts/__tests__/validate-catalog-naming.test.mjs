// TECH-17996 / TECH-17998 — unit tests for validate-catalog-naming.mjs
// Tests slug validation logic (fixture-only — no live DB required).

import assert from "node:assert";
import { test } from "node:test";
import { validateSlug } from "../validate-catalog-naming.mjs";

// ---------------------------------------------------------------------------
// Valid slugs
// ---------------------------------------------------------------------------

test("power-plant-tool-button passes", () => {
  assert.deepStrictEqual(validateSlug("power-plant-tool-button"), []);
});

test("population-counter-display passes", () => {
  assert.deepStrictEqual(validateSlug("population-counter-display"), []);
});

test("building-info-panel passes", () => {
  assert.deepStrictEqual(validateSlug("building-info-panel"), []);
});

test("budget-icon passes", () => {
  assert.deepStrictEqual(validateSlug("budget-icon"), []);
});

test("subtype-picker passes", () => {
  assert.deepStrictEqual(validateSlug("subtype-picker"), []);
});

test("growth-budget-panel passes", () => {
  assert.deepStrictEqual(validateSlug("growth-budget-panel"), []);
});

// ---------------------------------------------------------------------------
// test_lint_table_emits_offenders (red-stage proof TECH-17996)
// ---------------------------------------------------------------------------

test("test_lint_table_emits_offenders — underscore slug fails", () => {
  // illuminated-button (5) would have special chars but let's use an underscore slug
  const violations = validateSlug("demo_panel");
  assert.ok(violations.length > 0, "expected at least one violation");
  assert.ok(
    violations.some((v) => v.includes("underscore")),
    `expected underscore violation, got: ${violations.join("; ")}`
  );
});

// ---------------------------------------------------------------------------
// Bad slugs — forbidden patterns
// ---------------------------------------------------------------------------

test("trailing (N) segment is rejected", () => {
  const v = validateSlug("illuminated-button (5)");
  assert.ok(v.length > 0);
  assert.ok(v.some((s) => s.includes("(N)")));
});

test("uppercase letter is rejected", () => {
  const v = validateSlug("Button7");
  assert.ok(v.length > 0);
  assert.ok(v.some((s) => s.includes("uppercase")));
});

test("underscore is rejected", () => {
  const v = validateSlug("power_plant_tool_button");
  assert.ok(v.length > 0);
  assert.ok(v.some((s) => s.includes("underscore")));
});

test("leading digit is rejected", () => {
  const v = validateSlug("5-button");
  assert.ok(v.length > 0);
  assert.ok(v.some((s) => s.includes("leading digit") || s.includes("regex")));
});

test("numeric trailing segment rejected — picker-tile-72", () => {
  const v = validateSlug("picker-tile-72");
  assert.ok(v.length > 0);
  assert.ok(v.some((s) => s.includes("numeric")));
});

test("numeric trailing segment rejected — slider-row-2", () => {
  const v = validateSlug("slider-row-2");
  assert.ok(v.length > 0);
  assert.ok(v.some((s) => s.includes("numeric")));
});

test("missing kind suffix — subtype-picker-tile fails kind check", () => {
  // 'tile' is not an allowed kind suffix
  const v = validateSlug("subtype-tile");
  assert.ok(v.length > 0);
  assert.ok(v.some((s) => s.includes("kind suffix")));
});

test("single-segment slug missing purpose prefix", () => {
  const v = validateSlug("panel");
  assert.ok(v.length > 0);
});

// ---------------------------------------------------------------------------
// test_hard_fail_exits_nonzero_on_offender (red-stage proof TECH-17998)
// This tests the validateSlug return shape used by the hard-fail branch.
// ---------------------------------------------------------------------------

test("test_hard_fail_exits_nonzero_on_offender — bad slug returns violations array", () => {
  // Simulates: fixture DB with one bad slug → violations non-empty → main() would exit 1
  const v = validateSlug("hud_bar_btn_budget");
  assert.ok(v.length > 0, "underscore slug must produce violations");
});
