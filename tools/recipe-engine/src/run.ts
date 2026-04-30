/**
 * Recipe engine entrypoint — DEC-A19 Phase B MVP.
 *
 * Pipeline:
 *   1. Load YAML at tools/recipes/{name}.yaml
 *   2. Validate against tools/recipe-engine/schema/recipe.schema.json
 *   3. Validate inputs against recipe.inputs (when present)
 *   4. Walk steps top-to-bottom; dispatch by kind
 *   5. Resolve outputs map and return final bundle
 *
 * Step kinds: mcp.{tool} | bash.{script} | sql.{op} | seam.{name} |
 * gate.{validator} | flow.{seq|parallel|when|until}.
 *
 * Audit: every step persists a row to ia_recipe_runs (DB) or audit.jsonl (fallback).
 */

import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import yaml from "js-yaml";
import Ajv from "ajv";
import type { Recipe, RunContext, Step, StepResult } from "./types.js";
import { stepKind } from "./types.js";
import { resolveString, resolveTree, coerceTruthy } from "./template.js";
import { createAuditSink } from "./audit.js";
import { runMcpStep } from "./steps/mcp.js";
import { runBashStep } from "./steps/bash.js";
import { runSqlStep } from "./steps/sql.js";
import { runSeamStep } from "./steps/seam.js";
import { runGateStep } from "./steps/gate.js";
import { runFlowStep } from "./steps/flow.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const RECIPE_SCHEMA_PATH = path.join(__dirname, "..", "schema", "recipe.schema.json");

export interface RunRecipeOptions {
  inputs?: Record<string, unknown>;
  cwd?: string;
  dry_run?: boolean;
  emit_trace?: boolean;
  run_id?: string;
}

export interface RunRecipeResult {
  ok: boolean;
  run_id: string;
  recipe: string;
  recipe_version?: number;
  outputs?: Record<string, unknown>;
  error?: { code: string; message: string; details?: unknown; failed_step?: string };
  step_results: Array<{ step_id: string; result: StepResult }>;
}

export async function loadRecipe(name: string, cwd: string): Promise<Recipe> {
  const recipePath = path.join(cwd, "tools", "recipes", `${name}.yaml`);
  if (!fs.existsSync(recipePath)) {
    throw new Error(`Recipe not found: ${path.relative(cwd, recipePath)}`);
  }
  const raw = fs.readFileSync(recipePath, "utf8");
  const parsed = yaml.load(raw) as Recipe;
  return parsed;
}

export function validateRecipe(recipe: Recipe): { ok: true } | { ok: false; errors: unknown } {
  const schema = JSON.parse(fs.readFileSync(RECIPE_SCHEMA_PATH, "utf8"));
  const ajv = new Ajv({ allErrors: true, strict: false });
  const validate = ajv.compile(schema);
  if (!validate(recipe)) return { ok: false, errors: validate.errors };
  for (const s of recipe.steps) {
    const seamRetryViolation = walk(s, (st) => "seam" in st && Boolean(st.retry));
    if (seamRetryViolation) {
      return {
        ok: false,
        errors: [
          {
            instancePath: `/steps`,
            message: `seam step has retry policy — rejected per Q5 escalate-to-human policy`,
            params: { step_id: seamRetryViolation.id },
          },
        ],
      };
    }
  }
  return { ok: true };
}

function walk(step: Step, predicate: (s: Step) => boolean): Step | undefined {
  if (predicate(step)) return step;
  if ("flow" in step) {
    if (
      step.flow === "seq" ||
      step.flow === "parallel" ||
      step.flow === "until" ||
      step.flow === "foreach"
    ) {
      for (const c of step.steps) {
        const hit = walk(c, predicate);
        if (hit) return hit;
      }
    } else if (step.flow === "when") {
      for (const c of [...(step.then ?? []), ...(step.else ?? [])]) {
        const hit = walk(c, predicate);
        if (hit) return hit;
      }
    }
  }
  return undefined;
}

export async function runRecipe(name: string, options: RunRecipeOptions = {}): Promise<RunRecipeResult> {
  const cwd = options.cwd ?? REPO_ROOT;
  const recipe = await loadRecipe(name, cwd);
  const validation = validateRecipe(recipe);
  if (!validation.ok) {
    return {
      ok: false,
      run_id: "",
      recipe: name,
      step_results: [],
      error: { code: "schema_invalid", message: `Recipe ${name} failed schema validation`, details: validation.errors },
    };
  }

  const run_id = options.run_id ?? crypto.randomBytes(8).toString("hex");
  const inputs = options.inputs ?? {};
  if (recipe.inputs) {
    const ajv = new Ajv({ allErrors: true, strict: false });
    const inputValidate = ajv.compile({ type: "object", ...recipe.inputs });
    if (!inputValidate(inputs)) {
      return {
        ok: false,
        run_id,
        recipe: recipe.recipe,
        step_results: [],
        error: { code: "inputs_invalid", message: "Recipe inputs failed schema", details: inputValidate.errors },
      };
    }
  }

  const recipe_version = recipe.recipe_version ?? 1;
  const emit_trace = Boolean(options.emit_trace);
  const audit = createAuditSink({ run_id, recipe_slug: recipe.recipe, recipe_version, cwd, emit_trace });
  const ctx: RunContext = {
    run_id,
    recipe_slug: recipe.recipe,
    recipe_version,
    inputs,
    vars: { inputs },
    cwd,
    dry_run: Boolean(options.dry_run),
    emit_trace,
    audit,
  };

  const stepResults: Array<{ step_id: string; result: StepResult }> = [];

  async function runStep(step: Step, parentPath: string): Promise<StepResult> {
    if (step.when !== undefined) {
      const truthy = coerceTruthy(resolveString(ctx.vars, step.when));
      if (!truthy) {
        const sk: StepResult = { ok: true, skipped: true };
        await audit.begin(step, parentPath);
        await audit.end(step, parentPath, sk);
        stepResults.push({ step_id: step.id, result: sk });
        return sk;
      }
    }
    await audit.begin(step, parentPath);

    let attempt = 0;
    const maxAttempts = "retry" in step && step.retry ? step.retry.max : 1;
    let result: StepResult = { ok: false, error: { code: "never_ran", message: "no attempt" } };
    while (attempt < maxAttempts) {
      attempt += 1;
      result = await dispatchOne(step, parentPath);
      if (result.ok) break;
      if (attempt < maxAttempts) {
        const backoff = (step as { retry?: { backoff_ms?: number } }).retry?.backoff_ms ?? 0;
        if (backoff > 0) await new Promise((r) => setTimeout(r, backoff));
      }
    }

    const bindKey = step.bind ?? step.id;
    if (result.ok && result.value !== undefined) {
      ctx.vars[bindKey] = result.value;
    }

    await audit.end(step, parentPath, result);
    stepResults.push({ step_id: step.id, result });
    return result;
  }

  async function dispatchOne(step: Step, parentPath: string): Promise<StepResult> {
    const kind = stepKind(step);
    if (kind === "mcp") return runMcpStep(step as Extract<Step, { mcp: string }>, ctx);
    if (kind === "bash") return runBashStep(step as Extract<Step, { bash: string }>, ctx);
    if (kind === "sql") return runSqlStep(step as Extract<Step, { sql: "query" | "exec" }>, ctx);
    if (kind === "seam") return runSeamStep(step as Extract<Step, { seam: string }>, ctx);
    if (kind === "gate") return runGateStep(step as Extract<Step, { gate: string }>, ctx);
    if (kind === "flow") {
      return runFlowStep(
        step as Extract<Step, { flow: string }>,
        ctx,
        `${parentPath}/${step.id}`,
        (child, c, parent) => runStep(child, parent),
      );
    }
    return { ok: false, error: { code: "unknown_kind", message: `Unknown step kind: ${kind}` } };
  }

  for (const s of recipe.steps) {
    const r = await runStep(s, "");
    if (!r.ok && !r.skipped) {
      return {
        ok: false,
        run_id,
        recipe: recipe.recipe,
        step_results: stepResults,
        error: { ...r.error!, failed_step: s.id },
      };
    }
  }

  const outputs: Record<string, unknown> = {};
  for (const [k, expr] of Object.entries(recipe.outputs ?? {})) {
    outputs[k] = resolveTree(ctx.vars, expr);
  }

  return { ok: true, run_id, recipe: recipe.recipe, recipe_version, outputs, step_results: stepResults };
}
