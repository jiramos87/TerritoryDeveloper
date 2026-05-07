-- 0101_cron_drift_lint_jobs_rollback.sql
-- Rolls back 0101_cron_drift_lint_jobs.sql

BEGIN;
DROP TABLE IF EXISTS cron_drift_lint_jobs;
COMMIT;
