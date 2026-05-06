-- Rollback for 0094_cron_stage_verification_flip_jobs.sql
DROP INDEX IF EXISTS cron_stage_verification_flip_idem_idx;
DROP INDEX IF EXISTS cron_stage_verification_flip_claim_idx;
DROP TABLE IF EXISTS cron_stage_verification_flip_jobs;
