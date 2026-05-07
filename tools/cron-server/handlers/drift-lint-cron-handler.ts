/**
 * drift-lint-cron-handler — processes one cron_drift_lint_jobs row.
 *
 * Shells to `node tools/scripts/drift-lint-sweep.mjs` to run the drift-lint
 * sweep asynchronously. commit_sha + slug from the row payload are passed as
 * informational CLI args for audit trail in logs.
 *
 * Non-zero exit → captures stdout+stderr to row.error.
 *
 * TECH-18105 / async-cron-jobs Stage 5.0.2
 */

import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

export interface DriftLintJobRow {
  job_id: string;
  commit_sha?: string | null;
  slug?: string | null;
}

export async function run(row: DriftLintJobRow): Promise<void> {
  const extraArgs: string[] = [];
  if (row.commit_sha) extraArgs.push("--commit-sha", row.commit_sha);
  if (row.slug) extraArgs.push("--slug", row.slug);

  const cmd = `node tools/scripts/drift-lint-sweep.mjs ${extraArgs.join(" ")}`.trimEnd();
  try {
    execSync(cmd, {
      cwd: repoRoot,
      stdio: "pipe",
      timeout: 120_000, // 2 min max — DB + file scan
    });
  } catch (e: unknown) {
    const err = e as { stdout?: Buffer; stderr?: Buffer; message?: string };
    const stdout = err.stdout ? err.stdout.toString().trim() : "";
    const stderr = err.stderr ? err.stderr.toString().trim() : "";
    const combined = [stderr, stdout].filter(Boolean).join("\n");
    throw new Error(
      `drift-lint-sweep failed (job_id=${row.job_id}): ${combined || (err.message ?? "unknown error")}`,
    );
  }
}
