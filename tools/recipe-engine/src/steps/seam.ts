/**
 * seam step — Phase E: real subagent dispatch via seams_run MCP tool.
 *
 * When expected_output present: validate-only path (regression/dry-run).
 * When expected_output absent: call seams_run with dispatch_mode="subagent".
 *   - dispatch_unavailable in response → fall back to validate-only if
 *     expected_output is present; otherwise escalate with code "dispatch_unavailable".
 *   - schema_out validation failure → escalate with handoff file (Q5 policy).
 *
 * Escalation handoff at ia/state/recipe-runs/{run_id}/seam-{step_id}-error.md
 * per Q5 policy. retry attribute is rejected in the schema for seam steps.
 */

import fs from "node:fs";
import path from "node:path";
import Ajv from "ajv";
import type { SeamStep, RunContext, StepResult, SeamStepValue } from "../types.js";
import { resolveTree } from "../template.js";
import { getMcpInvoker } from "./mcp.js";

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

  // Validate-only path: expected_output provided (regression/golden fixture)
  if (expected !== undefined) {
    const validateOutput = ajv.compile(outputSchema as object);
    if (!validateOutput(expected)) {
      return await escalate(ctx, step.id, seamName, "schema_out", validateOutput.errors, input, expected);
    }
    const value: SeamStepValue = {
      seam: seamName,
      output: expected,
      validated: true,
      dispatch_mode: "validate-only",
    };
    return { ok: true, value };
  }

  // Subagent dispatch path
  const invoker = getMcpInvoker();
  if (!invoker) {
    return await escalate(
      ctx,
      step.id,
      seamName,
      "dispatch_unavailable" as "schema_in",
      { reason: "no_mcp_invoker" },
      input,
    );
  }

  let seamsResult: unknown;
  try {
    seamsResult = await invoker("seams_run", {
      name: seamName,
      dispatch_mode: "subagent",
      input,
    });
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return await escalate(ctx, step.id, seamName, "schema_in", { reason: "seams_run_invoke_failed", message: msg }, input);
  }

  // Handle dispatch_unavailable envelope
  const resultObj = seamsResult as Record<string, unknown> | null;
  if (resultObj && resultObj["dispatch_unavailable"] === true) {
    return await escalate(
      ctx,
      step.id,
      seamName,
      "dispatch_unavailable" as "schema_in",
      { reason: "subagent_env_not_available" },
      input,
    );
  }

  // Validate output from subagent
  const output = resultObj?.["output"];
  const validateOutput = ajv.compile(outputSchema as object);
  if (!validateOutput(output)) {
    return await escalate(ctx, step.id, seamName, "schema_out", validateOutput.errors, input, output);
  }

  const value: SeamStepValue = {
    seam: seamName,
    output,
    validated: true,
    dispatch_mode: "subagent",
  };
  if (resultObj?.["token_totals"]) {
    value.token_totals = resultObj["token_totals"] as SeamStepValue["token_totals"];
  }
  return { ok: true, value };
}

async function escalate(
  ctx: RunContext,
  stepId: string,
  seamName: string,
  code: "schema_in" | "schema_out" | "refusal" | "timeout" | "dispatch_unavailable",
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
      message: `Seam ${seamName} failed (${code}) — escalation handoff at ${path.relative(ctx.cwd, handoffPath)}`,
      details,
    },
  };
}
