/**
 * seam step — Phase B MVP: validate-only against tools/seams/{name}/*.schema.json.
 *
 * Per DEC-A19 Q3.b, LLM dispatch flows through a plan-covered subagent — not
 * available from a Node CLI. Phase B keeps this step kind validation-only:
 *   - Validate provided seam.input against input.schema.json
 *   - When seam.expected_output present, validate against output.schema.json
 *
 * Real LLM dispatch lands in Phase C when the recipe-runner subagent wraps the
 * engine. Until then, recipes that need a live seam call must invoke the
 * subagent at the outer layer; this step validates the round-trip envelope.
 *
 * Failure → escalation handoff file under
 * `ia/state/recipe-runs/{run_id}/seam-{step_id}-error.md` per Q5 policy. retry
 * attribute is rejected in the schema for seam steps.
 */

import fs from "node:fs";
import path from "node:path";
import Ajv from "ajv";
import type { SeamStep, RunContext, StepResult } from "../types.js";
import { resolveTree } from "../template.js";

interface SeamStepExtended extends SeamStep {
  expected_output?: Record<string, unknown>;
}

export async function runSeamStep(step: SeamStep, ctx: RunContext): Promise<StepResult> {
  const seamName = String(resolveTree(ctx.vars, step.seam));
  const input = resolveTree(ctx.vars, step.input);
  const extended = step as SeamStepExtended;
  const expected = extended.expected_output
    ? resolveTree(ctx.vars, extended.expected_output)
    : undefined;

  const seamDir = path.join(ctx.cwd, "tools", "seams", seamName);
  if (!fs.existsSync(seamDir)) {
    return await escalate(ctx, step.id, seamName, "schema_in", { reason: "unknown_seam" }, input);
  }
  const inputSchemaPath = path.join(seamDir, "input.schema.json");
  const outputSchemaPath = path.join(seamDir, "output.schema.json");

  let inputSchema: unknown;
  let outputSchema: unknown;
  try {
    inputSchema = JSON.parse(fs.readFileSync(inputSchemaPath, "utf8"));
    outputSchema = JSON.parse(fs.readFileSync(outputSchemaPath, "utf8"));
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return await escalate(ctx, step.id, seamName, "schema_in", { reason: "schema_load_failed", message: msg }, input);
  }

  if (ctx.dry_run) {
    return { ok: true, value: { dry_run: true, seam: seamName, validated: false } };
  }

  const ajv = new Ajv({ allErrors: true, strict: false });
  const validateInput = ajv.compile(inputSchema as object);
  if (!validateInput(input)) {
    return await escalate(ctx, step.id, seamName, "schema_in", validateInput.errors, input);
  }

  if (expected !== undefined) {
    const validateOutput = ajv.compile(outputSchema as object);
    if (!validateOutput(expected)) {
      return await escalate(ctx, step.id, seamName, "schema_out", validateOutput.errors, input, expected);
    }
    return { ok: true, value: { seam: seamName, output: expected, validated: true } };
  }

  return {
    ok: false,
    error: {
      code: "phase_b_no_dispatch",
      message: `Phase B: seam ${seamName} requires plan-covered subagent dispatch (not yet wired). Pass expected_output for validate-only round-trip.`,
    },
  };
}

async function escalate(
  ctx: RunContext,
  stepId: string,
  seamName: string,
  code: "schema_in" | "schema_out" | "refusal" | "timeout",
  details: unknown,
  input: unknown,
  attemptedOutput?: unknown,
): Promise<StepResult> {
  const dir = path.join(ctx.cwd, "ia", "state", "recipe-runs", ctx.run_id);
  fs.mkdirSync(dir, { recursive: true });
  const inputPath = path.join(dir, `seam-${stepId}-input.json`);
  fs.writeFileSync(inputPath, JSON.stringify(input, null, 2));
  let outputPath: string | undefined;
  if (attemptedOutput !== undefined) {
    outputPath = path.join(dir, `seam-${stepId}-attempted-output.json`);
    fs.writeFileSync(outputPath, JSON.stringify(attemptedOutput, null, 2));
  }
  const handoffPath = path.join(dir, `seam-${stepId}-error.md`);
  const body = [
    `recipe: ${ctx.recipe_slug}          step: ${stepId}          seam: ${seamName}`,
    `input: ${path.relative(ctx.cwd, inputPath)}`,
    outputPath ? `attempted-output: ${path.relative(ctx.cwd, outputPath)}` : "attempted-output: (not produced)",
    `validation-error: ${code} ${JSON.stringify(details)}`,
    `resume-cursor: ${stepId}`,
    `human-options: [1] fix-in-place [2] accept-as-is [3] abort`,
    "",
  ].join("\n");
  fs.writeFileSync(handoffPath, body);
  return {
    ok: false,
    error: {
      code,
      message: `Seam ${seamName} validation failed — escalation handoff at ${path.relative(ctx.cwd, handoffPath)}`,
      details,
    },
  };
}
