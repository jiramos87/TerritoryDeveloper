-- 0100_cron_anchor_reindex_jobs_rollback.sql
-- Rolls back 0100_cron_anchor_reindex_jobs.sql

BEGIN;
DROP TABLE IF EXISTS cron_anchor_reindex_jobs;
COMMIT;
