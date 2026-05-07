/**
 * regen-indexes-cron-handler — processes one cron_regen_indexes_jobs row.
 *
 * Shells to `npm run generate:ia-indexes` for scope='all' (and currently
 * all scopes — generate-ia-indexes.ts does not support per-scope args yet;
 * scope column reserved for future sub-commands when the script gains that support).
 * Non-zero exit → captures stdout+stderr to row.error.
 */

import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

export interface RegenIndexesJobRow {
  job_id: string;
  scope: "all" | "glossary" | "specs";
}

export async function run(row: RegenIndexesJobRow): Promise<void> {
  // generate:ia-indexes currently regenerates all indexes; scope is reserved for future
  // per-scope sub-commands (glossary-graph, spec-index) when the script adds that support.
  const cmd = "npm run generate:ia-indexes";
  try {
    execSync(cmd, {
      cwd: repoRoot,
      stdio: "pipe",
      timeout: 120_000,
    });
  } catch (e: unknown) {
    const err = e as { stdout?: Buffer; stderr?: Buffer; message?: string };
    const stdout = err.stdout ? err.stdout.toString().trim() : "";
    const stderr = err.stderr ? err.stderr.toString().trim() : "";
    const combined = [stderr, stdout].filter(Boolean).join("\n");
    throw new Error(
      `generate:ia-indexes failed (scope=${row.scope}): ${combined || (err.message ?? "unknown error")}`,
    );
  }
}
