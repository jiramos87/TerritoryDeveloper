/**
 * materialize-backlog-cron-handler — processes one cron_materialize_backlog_jobs row.
 *
 * Shells to `bash tools/scripts/materialize-backlog.sh`.
 * Preserves the .materialize-backlog.lock flock guard already in the script.
 * Non-zero exit → captures stdout+stderr to row.error.
 */

import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

export interface MaterializeBacklogJobRow {
  job_id: string;
  triggered_by?: string | null;
}

export async function run(row: MaterializeBacklogJobRow): Promise<void> {
  const script = path.join(repoRoot, "tools/scripts/materialize-backlog.sh");
  try {
    execSync(`bash ${script}`, {
      cwd: repoRoot,
      stdio: "pipe",
      timeout: 120_000, // 2 min max — heavy op
    });
  } catch (e: unknown) {
    const err = e as { stdout?: Buffer; stderr?: Buffer; message?: string };
    const stdout = err.stdout ? err.stdout.toString().trim() : "";
    const stderr = err.stderr ? err.stderr.toString().trim() : "";
    const combined = [stderr, stdout].filter(Boolean).join("\n");
    throw new Error(
      `materialize-backlog.sh failed (triggered_by=${row.triggered_by ?? "unknown"}): ${combined || (err.message ?? "unknown error")}`,
    );
  }
}
