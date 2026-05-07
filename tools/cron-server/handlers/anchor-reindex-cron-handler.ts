/**
 * anchor-reindex-cron-handler — processes one cron_anchor_reindex_jobs row.
 *
 * Shells to `npm run generate:ia-indexes -- --write-anchors` to upsert
 * ia_spec_anchors rows. The `paths` column is informational (logged) and
 * reserved for future per-path sub-scan support when generate-ia-indexes
 * gains that capability.
 *
 * Non-zero exit → captures stdout+stderr to row.error.
 */

import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

export interface AnchorReindexJobRow {
  job_id: string;
  paths: string[];
}

export async function run(row: AnchorReindexJobRow): Promise<void> {
  if (row.paths && row.paths.length > 0) {
    console.log(`[anchor-reindex] job_id=${row.job_id} paths=${JSON.stringify(row.paths)}`);
  }
  const cmd = "npm run generate:ia-indexes -- --write-anchors";
  try {
    execSync(cmd, {
      cwd: repoRoot,
      stdio: "pipe",
      timeout: 300_000, // 5 min max — full spec tree scan
    });
  } catch (e: unknown) {
    const err = e as { stdout?: Buffer; stderr?: Buffer; message?: string };
    const stdout = err.stdout ? err.stdout.toString().trim() : "";
    const stderr = err.stderr ? err.stderr.toString().trim() : "";
    const combined = [stderr, stdout].filter(Boolean).join("\n");
    throw new Error(
      `generate-ia-indexes --write-anchors failed (paths=${JSON.stringify(row.paths)}): ${combined || (err.message ?? "unknown error")}`,
    );
  }
}
