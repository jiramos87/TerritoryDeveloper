/**
 * bash step — spawn shell script and capture stdout/stderr.
 *
 * Phase B MVP: synchronous-style spawn via child_process.spawn (Promise wrap).
 * No streaming surface. Working dir = ctx.cwd. Failure surfaces as ok:false
 * with exit code in details.
 */

import { spawn } from "node:child_process";
import path from "node:path";
import type { BashStep, RunContext, StepResult } from "../types.js";
import { resolveTree } from "../template.js";

export async function runBashStep(step: BashStep, ctx: RunContext): Promise<StepResult> {
  const scriptPath = String(resolveTree(ctx.vars, step.bash));
  const argsObj = (resolveTree(ctx.vars, step.args ?? {}) as Record<string, unknown>) ?? {};
  const argv = flattenArgs(argsObj);

  const abs = path.isAbsolute(scriptPath) ? scriptPath : path.join(ctx.cwd, scriptPath);

  if (ctx.dry_run) {
    return { ok: true, value: { dry_run: true, script: abs, argv } };
  }

  return await new Promise<StepResult>((resolve) => {
    const child = spawn("bash", [abs, ...argv], { cwd: ctx.cwd, env: process.env });
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (d) => (stdout += d.toString("utf8")));
    child.stderr.on("data", (d) => (stderr += d.toString("utf8")));
    child.on("error", (err) =>
      resolve({ ok: false, error: { code: "spawn_error", message: err.message } }),
    );
    child.on("close", (code) => {
      if (code === 0) {
        resolve({ ok: true, value: { stdout, stderr, code } });
      } else {
        resolve({
          ok: false,
          error: { code: "nonzero_exit", message: `bash ${abs} exited ${code}`, details: { stdout, stderr, code } },
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
    else if (Array.isArray(v)) {
      for (const item of v) {
        out.push(`--${k}`, item !== null && typeof item === "object" ? JSON.stringify(item) : String(item));
      }
    } else if (typeof v === "object") {
      out.push(`--${k}`, JSON.stringify(v));
    } else {
      out.push(`--${k}`, String(v));
    }
  }
  return out;
}
