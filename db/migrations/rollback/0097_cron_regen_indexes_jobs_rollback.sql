-- Rollback for 0097_cron_regen_indexes_jobs.sql
DROP INDEX IF EXISTS cron_regen_indexes_idem_idx;
DROP INDEX IF EXISTS cron_regen_indexes_claim_idx;
DROP TABLE IF EXISTS cron_regen_indexes_jobs;
