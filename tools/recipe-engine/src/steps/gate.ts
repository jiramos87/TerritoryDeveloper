/**
 * gate step — run a validator (npm script).
 *
 * Phase B MVP: spawns `npm run {gate}` from repo root. Non-zero exit = step
 * failure. Stdout/stderr captured for audit.
 */

import { spawn } from "node:child_process";
import type { GateStep, RunContext, StepResult } from "../types.js";
import { resolveTree } from "../template.js";

export async function runGateStep(step: GateStep, ctx: RunContext): Promise<StepResult> {
  const gateName = String(resolveTree(ctx.vars, step.gate));
  const argsObj = (resolveTree(ctx.vars, step.args ?? {}) as Record<string, unknown>) ?? {};
  const argv = flattenArgs(argsObj);

  if (ctx.dry_run) {
    return { ok: true, value: { dry_run: true, gate: gateName, argv } };
  }

  return await new Promise<StepResult>((resolve) => {
    const args = ["run", gateName];
    if (argv.length > 0) args.push("--", ...argv);
    const child = spawn("npm", args, { cwd: ctx.cwd, env: process.env });
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (d) => (stdout += d.toString("utf8")));
    child.stderr.on("data", (d) => (stderr += d.toString("utf8")));
    child.on("error", (err) =>
      resolve({ ok: false, error: { code: "spawn_error", message: err.message } }),
    );
    child.on("close", (code) => {
      if (code === 0) {
        resolve({ ok: true, value: { gate: gateName, code, stdout, stderr } });
      } else {
        resolve({
          ok: false,
          error: { code: "gate_failed", message: `gate ${gateName} exited ${code}`, details: { stdout, stderr, code } },
        });
      }
    });
  });
}

function flattenArgs(obj: Record<string, unknown>): string[] {
  const out: string[] = [];
  for (const [k, v] of Object.entries(obj)) {
    if (v === undefined || v === null) continue;
    if (v === true) out.push(`--${k}`);
    else if (v === false) continue;
    else if (Array.isArray(v)) for (const item of v) out.push(`--${k}`, String(item));
    else out.push(`--${k}`, String(v));
  }
  return out;
}
