/**
 * arch-changelog-append-cron-handler — processes one cron_arch_changelog_append_jobs row.
 *
 * Calls INSERT INTO arch_changelog mirroring the arch_changelog_append MCP tool shape.
 * Idempotent on (commit_sha, spec_path) via UNIQUE partial index (migration 0038).
 */

import { getCronDbPool } from "../lib/index.js";

export interface ArchChangelogAppendJobRow {
  job_id: string;
  decision_slug: string;
  kind: string;
  surface_path?: string | null;
  body: unknown;
  commit_sha?: string | null;
  plan_slug?: string | null;
}

export async function run(row: ArchChangelogAppendJobRow): Promise<void> {
  const pool = getCronDbPool();

  const bodyStr =
    typeof row.body === "string"
      ? row.body
      : JSON.stringify(row.body, null, 2);

  // Mirror the INSERT from runArchChangelogAppend + UNIQUE partial index dedup.
  // decision_slug maps to arch_changelog.decision_slug.
  // surface_path maps to spec_path in the canonical table.
  await pool.query(
    `INSERT INTO arch_changelog (kind, decision_slug, commit_sha, spec_path, body, plan_slug)
     VALUES ($1, $2, $3, $4, $5, $6)
     ON CONFLICT (commit_sha, spec_path) WHERE commit_sha IS NOT NULL AND spec_path IS NOT NULL
     DO NOTHING`,
    [
      row.kind,
      row.decision_slug,
      row.commit_sha ?? null,
      row.surface_path ?? null,
      bodyStr,
      row.plan_slug ?? null,
    ],
  );
}
