-- Rollback for 0096_cron_materialize_backlog_jobs.sql
DROP INDEX IF EXISTS cron_materialize_backlog_idem_idx;
DROP INDEX IF EXISTS cron_materialize_backlog_claim_idx;
DROP TABLE IF EXISTS cron_materialize_backlog_jobs;
