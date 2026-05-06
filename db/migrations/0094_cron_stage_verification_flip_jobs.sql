-- 0094_cron_stage_verification_flip_jobs.sql
--
-- TECH-18093 / async-cron-jobs Stage 2.0.1
--
-- Queue table for async stage-verification-flip writes processed by cron supervisor.
-- Cron handler drains rows and calls INSERT INTO ia_stage_verifications.
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   slug, stage_id, verdict, commit_sha, actor
--                 (mirrors stage_verification_flip MCP payload).

BEGIN;

CREATE TABLE cron_stage_verification_flip_jobs (
  job_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status          cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at     timestamptz     NOT NULL DEFAULT now(),
  started_at      timestamptz,
  finished_at     timestamptz,
  heartbeat_at    timestamptz,
  error           text,
  attempts        int             NOT NULL DEFAULT 0,
  idempotency_key text,
  -- kind-specific columns (stage_verification_flip shape)
  slug            text,
  stage_id        text,
  verdict         text,
  commit_sha      text,
  actor           text,
  notes           text,
  CONSTRAINT cron_stage_verification_flip_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_stage_verification_flip_claim_idx ON cron_stage_verification_flip_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_stage_verification_flip_idem_idx ON cron_stage_verification_flip_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
