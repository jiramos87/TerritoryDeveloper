/**
 * drift-lint-findings-cron-handler — processes one cron_drift_lint_findings_jobs row.
 *
 * Two-phase commit pattern: rows enter at status='staged' (BEFORE bundle_apply);
 * master_plan_bundle_apply success calls promote_drift_lint_staged() which flips
 * matching rows → 'queued'. claimBatch only picks up 'queued' rows, so 'staged'
 * rows are naturally skipped — no special-case needed here.
 *
 * Handler writes findings to ia_master_plan_change_log with kind='drift_lint_summary'.
 *
 * Lifecycle skills refactor — Phase 3 / weak-spot #2.
 */

import { getCronDbPool } from "../lib/index.js";

export interface DriftLintFindingsJobRow {
  job_id: string;
  slug: string;
  version: number;
  findings: unknown;
  n_resolved?: number | null;
  n_unresolved?: number | null;
}

export async function run(row: DriftLintFindingsJobRow): Promise<void> {
  const pool = getCronDbPool();

  const body = {
    slug: row.slug,
    version: row.version,
    findings: row.findings ?? [],
    n_resolved: row.n_resolved ?? 0,
    n_unresolved: row.n_unresolved ?? 0,
  };

  // Mirror dedup logic from audit-log-cron-handler:
  // UNIQUE (slug, stage_id, kind, commit_sha) — stage_id + commit_sha NULL here.
  await pool.query(
    `INSERT INTO ia_master_plan_change_log
       (slug, kind, body, actor, commit_sha, stage_id)
     VALUES ($1, $2, $3, $4, $5, $6)
     ON CONFLICT (slug, stage_id, kind, commit_sha) DO NOTHING`,
    [
      row.slug,
      "drift_lint_summary",
      JSON.stringify(body, null, 2),
      "cron:drift-lint-findings",
      null,
      null,
    ],
  );
}
