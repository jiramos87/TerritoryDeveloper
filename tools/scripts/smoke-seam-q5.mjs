#!/usr/bin/env node
/**
 * smoke-seam-q5 — Q5 escalation path smoke test (DEC-A19 Phase E).
 *
 * Injects a SubagentDispatchHandler that forces malformed output for
 * align-arch-decision, then asserts:
 *   1. seams_run returns ok={ok:false} envelope with invalid_input code
 *   2. ia/state/recipe-runs/{run_id}/seam-s1-error.md exists
 *   3. The engine did NOT retry (single invocation)
 *
 * Run: npm run smoke:seam-q5
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import fs from "node:fs";
import assert from "node:assert/strict";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");

// We import from the recipe-engine source via tsx-resolved imports.
// Since this is an .mjs file (no tsx transform), we use dynamic imports
// which work because the recipe-engine source files are .ts → tsx handles them
// when invoked via `npx tsx`.

const { setMcpInvoker } = await import(`${REPO_ROOT}/tools/recipe-engine/src/steps/mcp.js`);
const { runSeamStep } = await import(`${REPO_ROOT}/tools/recipe-engine/src/steps/seam.js`);

const RUN_ID = `smoke-seam-q5-${Date.now()}`;

const malformedOutput = { wrong_field: "this is not a valid AlignArchDecisionOutput" };

let invocations = 0;
setMcpInvoker(async (tool, _args) => {
  if (tool !== "seams_run") throw new Error(`Unexpected tool: ${tool}`);
  invocations++;
  // Return what looks like a valid envelope but with bad output
  return {
    seam: "align-arch-decision",
    dispatch_mode: "subagent",
    validated: false,
    output: malformedOutput,
    token_totals: { input_tokens: 5, output_tokens: 3 },
  };
});

const noop = { async begin() {}, async end() {} };
const ctx = {
  run_id: RUN_ID,
  recipe_slug: "smoke-seam-q5",
  inputs: {},
  vars: {},
  cwd: REPO_ROOT,
  dry_run: false,
  audit: noop,
};

const step = {
  id: "s1",
  seam: "align-arch-decision",
  input: {
    decision_id: "DEC-A01",
    current_record: { title: "T", status: "active", body: "B" },
    proposed_change: "Smoke test forced-malform.",
  },
};

const result = await runSeamStep(step, ctx);

// 1. Result must be ok=false
assert.equal(result.ok, false, `Expected ok=false, got ok=${result.ok}`);
assert.equal(result.error?.code, "schema_out", `Expected code=schema_out, got ${result.error?.code}`);

// 2. Handoff file must exist
const handoffPath = path.join(REPO_ROOT, "ia", "state", "recipe-runs", RUN_ID, "seam-s1-error.md");
assert.ok(fs.existsSync(handoffPath), `Handoff file missing: ${handoffPath}`);
const body = fs.readFileSync(handoffPath, "utf8");
assert.ok(body.includes("schema_out"), "Handoff body missing schema_out");
assert.ok(body.includes("recipe: smoke-seam-q5"), "Handoff body missing recipe slug");
assert.ok(body.includes("human-options:"), "Handoff body missing human-options line");

// 3. No retry — exactly 1 invocation
assert.equal(invocations, 1, `Expected 1 MCP invocation (no retry), got ${invocations}`);

// Cleanup
fs.rmSync(path.join(REPO_ROOT, "ia", "state", "recipe-runs", RUN_ID), { recursive: true, force: true });

console.log("smoke:seam-q5 PASS — Q5 escalation path verified (schema_out → handoff file, no retry)");
