/**
 * Audit sink for recipe runs.
 *
 * Phase B: writes per-step rows to `ia_recipe_runs` (migration 0046).
 * When DB unavailable, falls back to JSONL under `ia/state/recipe-runs/{run_id}/audit.jsonl`.
 *
 * Captures: run_id, recipe_slug, step_id, parent_path, kind, status,
 * input_hash (sha256 of resolved args), output_hash (sha256 of resolved
 * value), started_at, finished_at, error_code.
 */

import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import type { AuditSink, Step, StepResult, SeamStepValue } from "./types.js";
import { stepKind } from "./types.js";
import { getIaDatabasePool } from "../../mcp-ia-server/src/ia-db/pool.js";

interface BeginRecord {
  startedAt: number;
}

export function createAuditSink(runIdRef: {
  run_id: string;
  recipe_slug: string;
  recipe_version: number;
  cwd: string;
  emit_trace?: boolean;
}): AuditSink {
  const begins = new Map<string, BeginRecord>();
  const pool = getIaDatabasePool();
  const fallbackDir = path.join(runIdRef.cwd, "ia", "state", "recipe-runs", runIdRef.run_id);
  const fallbackFile = path.join(fallbackDir, "audit.jsonl");
  const emit_trace = Boolean(runIdRef.emit_trace);

  function key(step: Step, parentPath: string): string {
    return `${parentPath}/${step.id}`;
  }

  function appendFallback(record: Record<string, unknown>): void {
    fs.mkdirSync(fallbackDir, { recursive: true });
    fs.appendFileSync(fallbackFile, JSON.stringify(record) + "\n");
  }

  return {
    async begin(step, parentPath) {
      begins.set(key(step, parentPath), { startedAt: Date.now() });
    },
    async end(step, parentPath, result) {
      const k = key(step, parentPath);
      const begin = begins.get(k);
      const startedAt = begin?.startedAt ?? Date.now();
      const finishedAt = Date.now();
      begins.delete(k);

      const inputHash = sha256(JSON.stringify((step as { args?: unknown }).args ?? null));
      const outputHash = sha256(JSON.stringify(result.value ?? null));
      const status = result.skipped ? "skipped" : result.ok ? "ok" : "failed";

      const tokenTotals = extractTokenTotals(step, result);

      const row = {
        run_id: runIdRef.run_id,
        recipe_slug: runIdRef.recipe_slug,
        recipe_version: runIdRef.recipe_version,
        step_id: step.id,
        parent_path: parentPath,
        kind: stepKind(step),
        status,
        input_hash: inputHash,
        output_hash: outputHash,
        started_at: new Date(startedAt).toISOString(),
        finished_at: new Date(finishedAt).toISOString(),
        error_code: result.error?.code ?? null,
        prompt_tokens: tokenTotals?.input_tokens ?? null,
        completion_tokens: tokenTotals?.output_tokens ?? null,
        total_tokens: tokenTotals
          ? (tokenTotals.input_tokens ?? 0) + (tokenTotals.output_tokens ?? 0)
          : null,
      };

      const traceExtra = emit_trace ? buildTraceExtra(step, result) : {};

      if (pool) {
        try {
          await pool.query(
            `INSERT INTO ia_recipe_runs
              (run_id, recipe_slug, step_id, parent_path, kind, status,
               input_hash, output_hash, started_at, finished_at, error_code,
               recipe_version, prompt_tokens, completion_tokens, total_tokens)
             VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15)`,
            [
              row.run_id,
              row.recipe_slug,
              row.step_id,
              row.parent_path,
              row.kind,
              row.status,
              row.input_hash,
              row.output_hash,
              row.started_at,
              row.finished_at,
              row.error_code,
              row.recipe_version,
              row.prompt_tokens,
              row.completion_tokens,
              row.total_tokens,
            ],
          );
          if (emit_trace) appendFallback({ ...row, ...traceExtra });
          return;
        } catch {
          // fall through to file
        }
      }
      appendFallback({ ...row, ...traceExtra });
    },
  };
}

function extractTokenTotals(
  step: Step,
  result: StepResult,
): import("./types.js").TokenTotals | undefined {
  if (!("seam" in step)) return undefined;
  const val = result.value as SeamStepValue | undefined;
  return val?.token_totals;
}

function sha256(s: string): string {
  return crypto.createHash("sha256").update(s).digest("hex").slice(0, 16);
}

function buildTraceExtra(step: Step, result: StepResult): Record<string, unknown> {
  const extra: Record<string, unknown> = {};
  if ("seam" in step) {
    const val = result.value as SeamStepValue | undefined;
    extra["seam"] = val?.seam ?? String(step.seam);
    extra["dispatch_mode"] = val?.dispatch_mode ?? null;
    const sections = (val?.output as Record<string, unknown> | undefined)?.sections;
    extra["output_keys"] = sections && typeof sections === "object" ? Object.keys(sections) : [];
    if (val?.token_totals) extra["token_totals"] = val.token_totals;
  } else if ("mcp" in step) {
    extra["mcp"] = String(step.mcp);
    const val = result.value as Record<string, unknown> | undefined;
    if (val?.["task_id"]) extra["task_id"] = val["task_id"];
    const rawSection = (step as { args?: Record<string, unknown> }).args?.["section"];
    if (rawSection) extra["section"] = rawSection;
  }
  return extra;
}
