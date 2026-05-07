/**
 * glossary-backlinks-cron-handler — processes one cron_glossary_backlinks_jobs row.
 *
 * Shells to `node tools/scripts/glossary-backlink-enrich.mjs --plan-id {slug}`
 * with plan_id from the job row. Non-zero exit → captures stdout+stderr to row.error.
 */

import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");

export interface GlossaryBacklinksJobRow {
  job_id: string;
  slug: string;
  plan_id?: string | null;
}

export async function run(row: GlossaryBacklinksJobRow): Promise<void> {
  const script = path.join(repoRoot, "tools/scripts/glossary-backlink-enrich.mjs");
  const args = ["--plan-id", row.slug];
  if (row.plan_id) {
    args.push("--plan-uuid", row.plan_id);
  }
  const cmd = `node ${script} ${args.map((a) => JSON.stringify(a)).join(" ")}`;
  try {
    execSync(cmd, {
      cwd: repoRoot,
      stdio: "pipe",
      timeout: 300_000, // 5 min max — glossary scan can be large
    });
  } catch (e: unknown) {
    const err = e as { stdout?: Buffer; stderr?: Buffer; message?: string };
    const stdout = err.stdout ? err.stdout.toString().trim() : "";
    const stderr = err.stderr ? err.stderr.toString().trim() : "";
    const combined = [stderr, stdout].filter(Boolean).join("\n");
    throw new Error(
      `glossary-backlink-enrich.mjs failed (slug=${row.slug}): ${combined || (err.message ?? "unknown error")}`,
    );
  }
}
