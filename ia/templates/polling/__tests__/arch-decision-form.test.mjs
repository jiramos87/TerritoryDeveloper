/**
 * arch-decision-form.test.mjs
 *
 * unit-test:ia/templates/polling/__tests__/arch-decision-form.test.mjs::single_poll_fires_not_four
 *
 * TECH-15913 — Assert arch-decision.json template:
 *   - Loads and parses correctly
 *   - Contains all 4 form axes (problem/chosen/alternatives/consequences)
 *   - Slot fill for {{topic}} works
 *   - `form_axes` field present (distinguishes from verb polling templates)
 *
 * Note: full end-to-end assertion that "poll count == 1" requires a live LLM
 * run. This test validates the template contract — the structural guarantee
 * that downstream skill code can emit a single poll from the template.
 */

import { readFileSync } from "fs";
import { resolve, join } from "path";
import { fileURLToPath } from "url";
import assert from "node:assert/strict";

const __dirname = fileURLToPath(new URL(".", import.meta.url));
const TEMPLATE_PATH = resolve(__dirname, "../arch-decision.json");

function fillSlots(str, slots) {
  return str.replace(/\{\{(\w+)\}\}/g, (_, key) => slots[key] ?? `{{${key}}}`);
}

// ---------------------------------------------------------------------------
// single_poll_fires_not_four
// ---------------------------------------------------------------------------
// Structural assertion: template has all 4 axes in form_axes array.
// Downstream: skill emits ONE AskUserQuestion from this template.

const raw = readFileSync(TEMPLATE_PATH, "utf8");
const template = JSON.parse(raw);

// Template type check
assert.equal(template.verb, "arch-decision", "verb must be 'arch-decision'");
assert(typeof template.question === "string", "question required");
assert(typeof template.plain_language_preface === "string", "preface required");
assert(Array.isArray(template.form_axes), "form_axes must be array (single-form design)");
assert.equal(template.form_axes.length, 4, "must have exactly 4 form axes");

const axisIds = template.form_axes.map((a) => a.id);
assert(axisIds.includes("problem"), "form_axes must include 'problem'");
assert(axisIds.includes("chosen"), "form_axes must include 'chosen'");
assert(axisIds.includes("alternatives"), "form_axes must include 'alternatives'");
assert(axisIds.includes("consequences"), "form_axes must include 'consequences'");

console.log("PASS single_poll_fires_not_four[form_axes_count=4]");

// Each axis has required shape
for (const axis of template.form_axes) {
  assert(typeof axis.id === "string", `axis.id required (${JSON.stringify(axis)})`);
  assert(typeof axis.label === "string", `axis.label required for ${axis.id}`);
  assert(typeof axis.hint === "string", `axis.hint required for ${axis.id}`);
  assert(typeof axis.max_chars === "number" && axis.max_chars > 0, `axis.max_chars must be positive int for ${axis.id}`);
}

console.log("PASS single_poll_fires_not_four[axis_shape_valid]");

// Slot fill
const filledPreface = fillSlots(template.plain_language_preface, { topic: "status cascade trigger" });
assert(filledPreface.includes("status cascade trigger"), "{{topic}} slot should be filled");
assert(!filledPreface.includes("{{"), "all slots should be filled");

console.log("PASS single_poll_fires_not_four[slot_fill]");

// instructions field present (used to build the single AskUserQuestion prompt)
assert(typeof template.instructions === "string" && template.instructions.length > 0, "instructions field required for form-fill prompt construction");

console.log("PASS single_poll_fires_not_four[instructions_present]");

// Round-trip
const roundTripped = JSON.parse(JSON.stringify(template));
assert.deepEqual(roundTripped, template, "round-trip JSON should equal original");

console.log("PASS single_poll_fires_not_four[round_trip]");

// ---------------------------------------------------------------------------
// no_verb_polls_in_form_template
// ---------------------------------------------------------------------------
// arch-decision.json must NOT have an `options` field (verb-poll shape).
// It has `form_axes` instead. This enforces the structural distinction.
assert(
  !Object.prototype.hasOwnProperty.call(template, "options"),
  "arch-decision template should use form_axes, not options (verb-poll field)",
);
console.log("PASS no_verb_polls_in_form_template");

console.log("All arch-decision-form tests passed.");
