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
import type { AuditSink, RunContext, Step, StepResult } from "./types.js";
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
}): AuditSink {
  const begins = new Map<string, BeginRecord>();
  const pool = getIaDatabasePool();
  const fallbackDir = path.join(runIdRef.cwd, "ia", "state", "recipe-runs", runIdRef.run_id);
  const fallbackFile = path.join(fallbackDir, "audit.jsonl");

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
      };

      if (pool) {
        try {
          await pool.query(
            `INSERT INTO ia_recipe_runs
              (run_id, recipe_slug, step_id, parent_path, kind, status,
               input_hash, output_hash, started_at, finished_at, error_code,
               recipe_version)
             VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)`,
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
            ],
          );
          return;
        } catch {
          // fall through to file
        }
      }
      appendFallback(row);
    },
  };
}

function sha256(s: string): string {
  return crypto.createHash("sha256").update(s).digest("hex").slice(0, 16);
}
