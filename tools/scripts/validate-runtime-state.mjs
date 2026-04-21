#!/usr/bin/env node
/**
 * Validates committed ia/state/runtime-state.example.json against
 * tools/schemas/runtime-state.schema.json (structural checks).
 * If ia/state/runtime-state.json exists (gitignored), validates it too.
 *
 * Exit 0: all present files conform.
 * Exit 1: validation error.
 */

import { existsSync, readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "..", "..");
const schemaPath = join(repoRoot, "tools/schemas/runtime-state.schema.json");
const examplePath = join(repoRoot, "ia/state/runtime-state.example.json");
const livePath = join(repoRoot, "ia/state/runtime-state.json");

function iso8601UtcOk(s) {
  if (typeof s !== "string") return false;
  const t = Date.parse(s);
  if (Number.isNaN(t)) return false;
  return s.endsWith("Z") || /[+-]\d{2}:\d{2}$/.test(s);
}

/**
 * @param {unknown} data
 * @param {string} label
 */
function validateShape(data, label) {
  if (data === null || typeof data !== "object" || Array.isArray(data)) {
    console.error(`${label}: must be a JSON object`);
    process.exit(1);
  }
  const o = /** @type {Record<string, unknown>} */ (data);
  const keys = new Set(Object.keys(o));
  const required = [
    "last_verify_exit_code",
    "last_bridge_preflight_exit_code",
    "queued_test_scenario_id",
    "updated_at",
  ];
  for (const k of required) {
    if (!keys.has(k)) {
      console.error(`${label}: missing required key ${k}`);
      process.exit(1);
    }
  }
  for (const k of keys) {
    if (!required.includes(k)) {
      console.error(`${label}: unexpected key ${k} (additionalProperties: false)`);
      process.exit(1);
    }
  }
  if (typeof o.last_verify_exit_code !== "number" || !Number.isInteger(o.last_verify_exit_code)) {
    console.error(`${label}: last_verify_exit_code must be integer`);
    process.exit(1);
  }
  if (
    typeof o.last_bridge_preflight_exit_code !== "number" ||
    !Number.isInteger(o.last_bridge_preflight_exit_code)
  ) {
    console.error(`${label}: last_bridge_preflight_exit_code must be integer`);
    process.exit(1);
  }
  const q = o.queued_test_scenario_id;
  if (q !== null && typeof q !== "string") {
    console.error(`${label}: queued_test_scenario_id must be string or null`);
    process.exit(1);
  }
  if (typeof o.updated_at !== "string" || !iso8601UtcOk(o.updated_at)) {
    console.error(`${label}: updated_at must be ISO-8601 parseable timestamp`);
    process.exit(1);
  }
}

// Sanity: schema file exists
if (!existsSync(schemaPath)) {
  console.error(`Missing schema: ${schemaPath}`);
  process.exit(1);
}

const exampleRaw = readFileSync(examplePath, "utf8");
let example;
try {
  example = JSON.parse(exampleRaw);
} catch (e) {
  console.error(`parse ${examplePath}:`, e);
  process.exit(1);
}
validateShape(example, "ia/state/runtime-state.example.json");

if (existsSync(livePath)) {
  let live;
  try {
    live = JSON.parse(readFileSync(livePath, "utf8"));
  } catch (e) {
    console.error(`parse ${livePath}:`, e);
    process.exit(1);
  }
  validateShape(live, "ia/state/runtime-state.json");
}

console.log("validate-runtime-state: ok");
