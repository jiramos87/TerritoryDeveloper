-- 0102_cron_cache_warm_jobs_rollback.sql
-- Rolls back 0102_cron_cache_warm_jobs.sql

BEGIN;
DROP TABLE IF EXISTS cron_cache_warm_jobs;
COMMIT;
