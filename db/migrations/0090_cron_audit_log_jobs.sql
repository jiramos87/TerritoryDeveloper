-- 0090_cron_audit_log_jobs.sql
--
-- TECH-18089 / async-cron-jobs Stage 1.0.1
--
-- Queue table for async audit-log writes processed by cron supervisor.
-- Cron handler drains rows and calls INSERT INTO ia_master_plan_change_log.
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   slug, version, audit_kind, body (mirrors change_log_append payload).

BEGIN;

CREATE TYPE cron_job_status AS ENUM ('queued', 'running', 'done', 'failed');

CREATE TABLE cron_audit_log_jobs (
  job_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status          cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at     timestamptz     NOT NULL DEFAULT now(),
  started_at      timestamptz,
  finished_at     timestamptz,
  heartbeat_at    timestamptz,
  error           text,
  attempts        int             NOT NULL DEFAULT 0,
  idempotency_key text,
  -- kind-specific columns
  slug            text            NOT NULL,
  version         int             NOT NULL DEFAULT 1,
  audit_kind      text            NOT NULL,
  body            jsonb           NOT NULL,
  actor           text,
  commit_sha      text,
  stage_id        text,
  CONSTRAINT cron_audit_log_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_audit_log_claim_idx ON cron_audit_log_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_audit_log_idem_idx ON cron_audit_log_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
