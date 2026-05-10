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
        const kv = parseStdoutKv(stdout);
        resolve({ ok: true, value: { stdout, stderr, code, ...kv } });
      } else {
        resolve({
          ok: false,
          error: { code: "nonzero_exit", message: `bash ${abs} exited ${code}`, details: { stdout, stderr, code } },
        });
      }
    });
  });
}

/**
 * Parse trailing `key=value` tokens from bash stdout so recipe steps can
 * reference them via `${step.key}`. Convention: scripts emit a final line
 * (or any line) of whitespace-separated `key=value` pairs. Value may be
 * bare (no quotes) — first `=` splits. Reserved keys (stdout/stderr/code)
 * are skipped to avoid clobbering the raw envelope.
 */
function parseStdoutKv(stdout: string): Record<string, string> {
  const out: Record<string, string> = {};
  const lines = stdout.split(/\r?\n/);
  const reserved = new Set(["stdout", "stderr", "code"]);
  for (const line of lines) {
    // Only parse lines that look like one-or-more `key=value` tokens.
    if (!/^\s*[a-zA-Z_][a-zA-Z0-9_]*=/.test(line)) continue;
    const tokens = line.trim().split(/\s+/);
    for (const tok of tokens) {
      const eq = tok.indexOf("=");
      if (eq <= 0) continue;
      const k = tok.slice(0, eq);
      const v = tok.slice(eq + 1);
      if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(k)) continue;
      if (reserved.has(k)) continue;
      out[k] = v;
    }
  }
  return out;
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
