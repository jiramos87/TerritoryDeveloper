-- 0103_cron_cache_bust_jobs_rollback.sql
-- Rolls back 0103_cron_cache_bust_jobs.sql

BEGIN;
DROP TABLE IF EXISTS cron_cache_bust_jobs;
COMMIT;
