-- Migration 0106: cron_jobs_all — cross-kind union view across all 12 cron queue tables.
-- Projects common columns present on every table:
--   kind, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts
-- Used by /cron dashboard to surface queue depth + recent failures per kind.

CREATE OR REPLACE VIEW cron_jobs_all AS
SELECT 'audit_log'::text AS kind, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_audit_log_jobs
UNION ALL
SELECT 'journal_append'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_journal_append_jobs
UNION ALL
SELECT 'task_commit_record'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_task_commit_record_jobs
UNION ALL
SELECT 'stage_verification_flip'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_stage_verification_flip_jobs
UNION ALL
SELECT 'arch_changelog_append'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_arch_changelog_append_jobs
UNION ALL
SELECT 'materialize_backlog'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_materialize_backlog_jobs
UNION ALL
SELECT 'regen_indexes'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_regen_indexes_jobs
UNION ALL
SELECT 'glossary_backlinks'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_glossary_backlinks_jobs
UNION ALL
SELECT 'anchor_reindex'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_anchor_reindex_jobs
UNION ALL
SELECT 'drift_lint'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_drift_lint_jobs
UNION ALL
SELECT 'cache_warm'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_cache_warm_jobs
UNION ALL
SELECT 'cache_bust'::text, job_id, status, enqueued_at, started_at, finished_at, heartbeat_at, error, attempts FROM cron_cache_bust_jobs;
