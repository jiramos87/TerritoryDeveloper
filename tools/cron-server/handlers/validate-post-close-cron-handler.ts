/**
 * validate-post-close-cron-handler — processes one cron_validate_post_close_jobs row.
 *
 * Shells `npm run validate:fast --diff-paths {csv}` scoped to the stage commit.
 * Captures stdout/stderr; failure populates row.error via thrown Error.
 *
 * Lifecycle skills refactor — Phase 3 / weak-spot #10.
 */

import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { getCronDbPool } from "../lib/index.js";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

export interface ValidatePostCloseJobRow {
  job_id: string;
  slug: string;
  stage_id: string;
  commit_sha?: string | null;
  diff_paths?: unknown;
  validate_kind?: string | null;
}

function paths(input: unknown): string[] {
  if (!input) return [];
  if (Array.isArray(input)) return input.filter((x): x is string => typeof x === "string");
  if (typeof input === "string") {
    try {
      const parsed = JSON.parse(input);
      return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === "string") : [];
    } catch {
      return [];
    }
  }
  return [];
}

export async function run(row: ValidatePostCloseJobRow): Promise<void> {
  const kind = row.validate_kind || "fast";
  const target = `validate:${kind}`;
  const csv = paths(row.diff_paths).join(",");

  const args = ["run", target];
  if (csv) {
    // npm run X -- --diff-paths value
    args.push("--", "--diff-paths", csv);
  }
  const cmd = `npm ${args.map((a) => JSON.stringify(a)).join(" ")}`;

  let stdout = "";
  let stderr = "";
  let exitCode = 0;
  try {
    const out = execSync(cmd, {
      cwd: repoRoot,
      stdio: "pipe",
      timeout: 600_000, // 10 min cap — validate:fast on a stage diff is small
      env: { ...process.env, FORCE_COLOR: "0" },
    });
    stdout = out.toString();
  } catch (e: unknown) {
    const err = e as { stdout?: Buffer; stderr?: Buffer; status?: number; message?: string };
    stdout = err.stdout ? err.stdout.toString() : "";
    stderr = err.stderr ? err.stderr.toString() : "";
    exitCode = typeof err.status === "number" ? err.status : 1;
    // Write outcome to row before throwing so the failed row carries diagnostics.
    await persistOutcome(row.job_id, exitCode, stdout, stderr);
    const combined = [stderr, stdout].filter(Boolean).join("\n").slice(0, 4000);
    throw new Error(
      `${target} failed (slug=${row.slug} stage=${row.stage_id} exit=${exitCode}): ${combined || (err.message ?? "unknown error")}`,
    );
  }

  await persistOutcome(row.job_id, exitCode, stdout, stderr);
}

async function persistOutcome(
  job_id: string,
  exitCode: number,
  stdout: string,
  stderr: string,
): Promise<void> {
  const pool = getCronDbPool();
  const excerpt = [stderr.trim(), stdout.trim()]
    .filter(Boolean)
    .join("\n")
    .slice(0, 8000);
  await pool.query(
    `UPDATE cron_validate_post_close_jobs
       SET exit_code = $1,
           stdout_excerpt = $2
     WHERE job_id = $3`,
    [exitCode, excerpt, job_id],
  );
}
