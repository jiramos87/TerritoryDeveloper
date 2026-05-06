-- Rollback for 0093_cron_task_commit_record_jobs.sql
DROP INDEX IF EXISTS cron_task_commit_record_idem_idx;
DROP INDEX IF EXISTS cron_task_commit_record_claim_idx;
DROP TABLE IF EXISTS cron_task_commit_record_jobs;
