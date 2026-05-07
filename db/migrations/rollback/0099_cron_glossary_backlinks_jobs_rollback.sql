-- 0099_cron_glossary_backlinks_jobs_rollback.sql
-- Rolls back 0099_cron_glossary_backlinks_jobs.sql

BEGIN;
DROP TABLE IF EXISTS cron_glossary_backlinks_jobs;
COMMIT;
