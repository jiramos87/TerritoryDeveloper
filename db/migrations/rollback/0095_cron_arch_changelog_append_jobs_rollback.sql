-- Rollback for 0095_cron_arch_changelog_append_jobs.sql
DROP INDEX IF EXISTS cron_arch_changelog_append_idem_idx;
DROP INDEX IF EXISTS cron_arch_changelog_append_claim_idx;
DROP TABLE IF EXISTS cron_arch_changelog_append_jobs;
