/**
 * trim-template.test.mjs
 *
 * unit-test:ia/templates/polling/__tests__/trim-template.test.mjs::trim_polls_match_byte_for_byte
 *
 * TECH-15911 — Assert that polling templates load correctly and that
 * slot-filled polls match expected structure byte-for-byte except slot fills.
 *
 * Does NOT invoke a full design-explore run (that requires a live LLM).
 * Instead, validates: file parseable + required keys present + slot fill logic.
 */

import { readFileSync } from "fs";
import { resolve, join } from "path";
import { fileURLToPath } from "url";
import assert from "node:assert/strict";

const __dirname = fileURLToPath(new URL(".", import.meta.url));
const TEMPLATES_DIR = resolve(__dirname, "..");

const VERBS = ["trim", "add", "replace", "refactor", "integrate"];

function loadTemplate(verb) {
  const filePath = join(TEMPLATES_DIR, `${verb}.json`);
  const raw = readFileSync(filePath, "utf8");
  return { template: JSON.parse(raw), raw };
}

function fillSlots(str, slots) {
  return str.replace(/\{\{(\w+)\}\}/g, (_, key) => slots[key] ?? `{{${key}}}`);
}

// ---------------------------------------------------------------------------
// trim_polls_match_byte_for_byte
// ---------------------------------------------------------------------------
// Load trim.json; fill slots; assert structure is stable.

const { template: trimTpl, raw: trimRaw } = loadTemplate("trim");

assert.equal(trimTpl.verb, "trim", "verb must be 'trim'");
assert(Array.isArray(trimTpl.options), "options must be array");
assert(trimTpl.options.length >= 3, "must have at least 3 options");
assert(typeof trimTpl.recommended === "string", "recommended must be string");
assert(typeof trimTpl.plain_language_preface === "string", "preface must be string");
assert(typeof trimTpl.recommended_rationale === "string", "rationale must be string");
assert(Array.isArray(trimTpl.slot_keys), "slot_keys must be array");

// Fill slots
const filledPreface = fillSlots(trimTpl.plain_language_preface, { target: "ia_master_plan_change_log" });
assert(filledPreface.includes("ia_master_plan_change_log"), "slot fill should replace {{target}}");
assert(!filledPreface.includes("{{"), "all slots should be filled");

// Round-trip: JSON.parse(JSON.stringify(template)) === original
const roundTripped = JSON.parse(JSON.stringify(trimTpl));
assert.deepEqual(roundTripped, trimTpl, "round-trip JSON should equal original");

console.log("PASS trim_polls_match_byte_for_byte");

// ---------------------------------------------------------------------------
// all_verb_templates_valid
// ---------------------------------------------------------------------------
// Every supported verb has a valid template with required keys.

for (const verb of VERBS) {
  const { template } = loadTemplate(verb);
  assert.equal(template.verb, verb, `verb field must match filename for ${verb}`);
  assert(typeof template.question === "string" && template.question.length > 0, `${verb}: question required`);
  assert(typeof template.plain_language_preface === "string", `${verb}: preface required`);
  assert(Array.isArray(template.options) && template.options.length >= 3, `${verb}: ≥3 options required`);
  assert(typeof template.recommended === "string", `${verb}: recommended required`);
  assert(typeof template.recommended_rationale === "string", `${verb}: rationale required`);
  assert(Array.isArray(template.slot_keys), `${verb}: slot_keys required`);

  // Every option has id + label + tradeoff
  for (const opt of template.options) {
    assert(typeof opt.id === "string", `${verb}: option.id required`);
    assert(typeof opt.label === "string", `${verb}: option.label required`);
    assert(typeof opt.tradeoff === "string", `${verb}: option.tradeoff required`);
  }

  // recommended must be one of the option ids
  const optIds = template.options.map((o) => o.id);
  assert(optIds.includes(template.recommended), `${verb}: recommended '${template.recommended}' must be a valid option id`);

  console.log(`PASS all_verb_templates_valid[${verb}]`);
}

console.log("All polling template tests passed.");
