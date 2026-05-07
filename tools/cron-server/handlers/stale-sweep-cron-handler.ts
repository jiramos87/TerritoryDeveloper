/**
 * stale-sweep-cron-handler — sweep stale running rows across all cron_*_jobs tables.
 *
 * Runs every minute (cadence "* * * * *").
 * Reads timeout from carcass_config.claim_heartbeat_timeout_minutes (default 10).
 * Flips status='running' rows older than timeout to status='failed'.
 */

import { readSweepTimeoutMinutes, sweepStale } from "../lib/sweep.js";

const CRON_TABLES = [
  "cron_audit_log_jobs",
  "cron_journal_append_jobs",
  "cron_task_commit_record_jobs",
  "cron_stage_verification_flip_jobs",
  "cron_arch_changelog_append_jobs",
  "cron_materialize_backlog_jobs",
  "cron_regen_indexes_jobs",
  "cron_glossary_backlinks_jobs",
  "cron_anchor_reindex_jobs",
  "cron_drift_lint_jobs",
  "cron_cache_warm_jobs",
  "cron_cache_bust_jobs",
] as const;

export async function runStaleSweep(): Promise<void> {
  const timeoutMin = await readSweepTimeoutMinutes();
  let totalSwept = 0;

  for (const table of CRON_TABLES) {
    const swept = await sweepStale(table, timeoutMin);
    if (swept > 0) {
      console.log(`[cron:stale-sweep] ${table} swept=${swept} timeout=${timeoutMin}min`);
      totalSwept += swept;
    }
  }

  if (totalSwept > 0) {
    console.log(`[cron:stale-sweep] total_swept=${totalSwept}`);
  }
}
