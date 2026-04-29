/**
 * flow step — composite control flow.
 *
 * Kinds:
 *   - seq      : run children in order; abort on first failure
 *   - parallel : run children concurrently; aggregate results
 *   - when     : evaluate cond; run then[] if truthy, else else[]
 *   - until    : repeat steps[] until cond truthy or max_iters reached
 *   - foreach  : iterate items[] sequentially; expose ${as}/${index_as} per pass
 */

import type { FlowStep, RunContext, Step, StepResult } from "../types.js";
import { resolveString, resolvePath, coerceTruthy } from "../template.js";

type StepRunner = (step: Step, ctx: RunContext, parentPath: string) => Promise<StepResult>;

export async function runFlowStep(
  step: FlowStep,
  ctx: RunContext,
  parentPath: string,
  runStep: StepRunner,
): Promise<StepResult> {
  if (step.flow === "seq") {
    return await runSeq(step.steps, ctx, parentPath, runStep);
  }
  if (step.flow === "parallel") {
    return await runParallel(step.steps, ctx, parentPath, runStep);
  }
  if (step.flow === "when") {
    const v = resolveString(ctx.vars, step.cond);
    const branch = coerceTruthy(v) ? step.then ?? [] : step.else ?? [];
    return await runSeq(branch, ctx, parentPath, runStep);
  }
  if (step.flow === "until") {
    return await runUntil(step.cond, step.steps, step.max_iters, ctx, parentPath, runStep);
  }
  if (step.flow === "foreach") {
    return await runForeach(
      step.items,
      step.as ?? "item",
      step.index_as ?? "index",
      step.steps,
      ctx,
      parentPath,
      runStep,
    );
  }
  return { ok: false, error: { code: "unknown_flow", message: `Unknown flow: ${(step as { flow: string }).flow}` } };
}

async function runSeq(
  steps: Step[],
  ctx: RunContext,
  parentPath: string,
  runStep: StepRunner,
): Promise<StepResult> {
  const results: StepResult[] = [];
  for (const child of steps) {
    const r = await runStep(child, ctx, parentPath);
    results.push(r);
    if (!r.ok && !r.skipped) {
      return { ok: false, error: r.error, value: { results } };
    }
  }
  return { ok: true, value: { results } };
}

async function runParallel(
  steps: Step[],
  ctx: RunContext,
  parentPath: string,
  runStep: StepRunner,
): Promise<StepResult> {
  const results = await Promise.all(steps.map((s) => runStep(s, ctx, parentPath)));
  const failure = results.find((r) => !r.ok && !r.skipped);
  if (failure) {
    return { ok: false, error: failure.error, value: { results } };
  }
  return { ok: true, value: { results } };
}

async function runUntil(
  cond: string,
  steps: Step[],
  maxIters: number,
  ctx: RunContext,
  parentPath: string,
  runStep: StepRunner,
): Promise<StepResult> {
  let i = 0;
  while (i < maxIters) {
    const inner = await runSeq(steps, ctx, parentPath, runStep);
    if (!inner.ok) {
      return { ok: false, error: inner.error, value: { iterations: i + 1, last: inner } };
    }
    const v = resolveString(ctx.vars, cond);
    if (coerceTruthy(v)) {
      return { ok: true, value: { iterations: i + 1, last: inner } };
    }
    i += 1;
  }
  return {
    ok: false,
    error: { code: "until_exhausted", message: `flow.until exhausted ${maxIters} iterations without cond truthy` },
  };
}

async function runForeach(
  itemsExpr: string,
  asKey: string,
  indexKey: string,
  steps: Step[],
  ctx: RunContext,
  parentPath: string,
  runStep: StepRunner,
): Promise<StepResult> {
  const wholeMatch = itemsExpr.match(/^\$\{([^}]+)\}$/);
  const resolved = wholeMatch ? resolvePath(ctx.vars, wholeMatch[1]) : resolveString(ctx.vars, itemsExpr);
  if (!Array.isArray(resolved)) {
    return {
      ok: false,
      error: {
        code: "foreach_items_not_array",
        message: `flow.foreach items expression did not resolve to an array: ${itemsExpr}`,
      },
    };
  }
  const prevAs = ctx.vars[asKey];
  const prevIndex = ctx.vars[indexKey];
  const iterations: StepResult[] = [];
  try {
    for (let i = 0; i < resolved.length; i++) {
      ctx.vars[asKey] = resolved[i];
      ctx.vars[indexKey] = i;
      const inner = await runSeq(steps, ctx, parentPath, runStep);
      iterations.push(inner);
      if (!inner.ok) {
        return { ok: false, error: inner.error, value: { iterations, count: resolved.length, completed: i } };
      }
    }
  } finally {
    if (prevAs === undefined) delete ctx.vars[asKey];
    else ctx.vars[asKey] = prevAs;
    if (prevIndex === undefined) delete ctx.vars[indexKey];
    else ctx.vars[indexKey] = prevIndex;
  }
  return { ok: true, value: { iterations, count: resolved.length } };
}
