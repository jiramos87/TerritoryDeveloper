-- 0095_cron_arch_changelog_append_jobs.sql
--
-- TECH-18093 / async-cron-jobs Stage 2.0.1
--
-- Queue table for async arch-changelog-append writes processed by cron supervisor.
-- Cron handler drains rows and calls INSERT INTO arch_changelog.
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   decision_slug, kind, surface_path, body
--                 (mirrors arch_changelog_append MCP payload; body stored as jsonb).

BEGIN;

CREATE TABLE cron_arch_changelog_append_jobs (
  job_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status          cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at     timestamptz     NOT NULL DEFAULT now(),
  started_at      timestamptz,
  finished_at     timestamptz,
  heartbeat_at    timestamptz,
  error           text,
  attempts        int             NOT NULL DEFAULT 0,
  idempotency_key text,
  -- kind-specific columns (arch_changelog_append shape)
  decision_slug   text            NOT NULL,
  kind            text            NOT NULL,
  surface_path    text,
  body            jsonb           NOT NULL,
  commit_sha      text,
  plan_slug       text,
  CONSTRAINT cron_arch_changelog_append_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_arch_changelog_append_claim_idx ON cron_arch_changelog_append_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_arch_changelog_append_idem_idx ON cron_arch_changelog_append_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
