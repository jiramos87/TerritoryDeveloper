/**
 * master-plan-new-phase-a recipe shape regression (Stage 2.4 / TECH-5248).
 *
 * Asserts the YAML carries plan_slug input + `verify_seeded_decisions` +
 * `verify_count_gate` steps + plan_slug threading into seed_decisions
 * foreach body. Runtime end-to-end (live DB) covered by
 * `tools/recipe-engine/selftest.mjs` smokes; this test is the structural
 * contract gate so future recipe edits cannot drop the verify steps.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import yaml from "js-yaml";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..", "..");
const RECIPE_PATH = path.join(
  REPO_ROOT,
  "tools",
  "recipes",
  "master-plan-new-phase-a.yaml",
);

interface RecipeStep {
  id: string;
  bash?: string;
  mcp?: string;
  sql?: string;
  flow?: string;
  when?: string;
  args?: Record<string, unknown>;
  query?: string;
  steps?: RecipeStep[];
}

interface Recipe {
  recipe: string;
  inputs?: { properties?: Record<string, { type: string }> };
  steps: RecipeStep[];
  outputs?: Record<string, string>;
}

function loadRecipe(): Recipe {
  const raw = fs.readFileSync(RECIPE_PATH, "utf8");
  return yaml.load(raw) as Recipe;
}

test("recipe declares plan_slug input as optional string", () => {
  const recipe = loadRecipe();
  const props = recipe.inputs?.properties ?? {};
  assert.ok(props.plan_slug, "plan_slug input property missing");
  assert.equal(props.plan_slug.type, "string");
});

test("seed_decisions foreach threads plan_slug into arch_decision_write call", () => {
  const recipe = loadRecipe();
  const seed = recipe.steps.find((s) => s.id === "seed_decisions");
  assert.ok(seed, "seed_decisions step missing");
  assert.equal(seed?.flow, "foreach");
  const writeStep = (seed?.steps ?? []).find((s) => s.id === "write_decision");
  assert.ok(writeStep, "write_decision inner step missing");
  assert.equal(writeStep?.mcp, "arch_decision_write");
  // plan_slug arg must be templated to inputs.plan_slug — propagates from
  // recipe input down to the MCP call.
  const planSlugArg = (writeStep?.args ?? {}).plan_slug;
  assert.equal(planSlugArg, "${inputs.plan_slug}");
});

test("verify_seeded_decisions step exists, gated on plan_slug, queries arch_decisions", () => {
  const recipe = loadRecipe();
  const verify = recipe.steps.find((s) => s.id === "verify_seeded_decisions");
  assert.ok(verify, "verify_seeded_decisions step missing");
  // when-cond skips legacy global-only callers.
  assert.equal(verify?.when, "${inputs.plan_slug}");
  assert.equal(verify?.sql, "query");
  assert.match(verify?.query ?? "", /FROM arch_decisions/);
  assert.match(verify?.query ?? "", /plan_slug = \$1/);
});

test("verify_count_gate enforces ≥3 minimum via predicate script", () => {
  const recipe = loadRecipe();
  const gate = recipe.steps.find((s) => s.id === "verify_count_gate");
  assert.ok(gate, "verify_count_gate step missing");
  assert.equal(gate?.when, "${inputs.plan_slug}");
  assert.match(
    String(gate?.bash ?? ""),
    /verify-seeded-count\.sh$/,
  );
  // min_count fixed at 3 (boundaries + end-state-contract + shared-seams).
  assert.equal((gate?.args ?? {}).min_count, 3);
  assert.equal(
    (gate?.args ?? {}).seeded_count,
    "${verify_seeded_decisions.rows[0].seeded_count}",
  );
});

test("verify steps land before lock_arch (enforce ordering)", () => {
  const recipe = loadRecipe();
  const ids = recipe.steps.map((s) => s.id);
  const verifyIdx = ids.indexOf("verify_seeded_decisions");
  const gateIdx = ids.indexOf("verify_count_gate");
  const lockIdx = ids.indexOf("lock_arch");
  assert.ok(verifyIdx > -1 && gateIdx > -1 && lockIdx > -1);
  assert.ok(
    verifyIdx < gateIdx && gateIdx < lockIdx,
    `step order drift: verify=${verifyIdx} gate=${gateIdx} lock=${lockIdx}`,
  );
});

test("predicate helper script exists + executable", () => {
  const scriptPath = path.join(
    REPO_ROOT,
    "tools",
    "scripts",
    "recipe-engine",
    "master-plan-new-phase-a",
    "verify-seeded-count.sh",
  );
  assert.ok(fs.existsSync(scriptPath), "verify-seeded-count.sh missing");
  const stat = fs.statSync(scriptPath);
  // Owner exec bit (0o100) at minimum.
  assert.ok((stat.mode & 0o100) !== 0, "predicate script not executable");
});
