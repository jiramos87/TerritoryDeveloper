-- 0093_cron_task_commit_record_jobs.sql
--
-- TECH-18093 / async-cron-jobs Stage 2.0.1
--
-- Queue table for async task-commit-record writes processed by cron supervisor.
-- Cron handler drains rows and calls INSERT INTO ia_task_commits.
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   task_id, commit_sha, commit_kind, slug, stage_id
--                 (mirrors task_commit_record MCP payload).

BEGIN;

CREATE TABLE cron_task_commit_record_jobs (
  job_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status          cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at     timestamptz     NOT NULL DEFAULT now(),
  started_at      timestamptz,
  finished_at     timestamptz,
  heartbeat_at    timestamptz,
  error           text,
  attempts        int             NOT NULL DEFAULT 0,
  idempotency_key text,
  -- kind-specific columns (task_commit_record shape)
  task_id         text            NOT NULL,
  commit_sha      text            NOT NULL,
  commit_kind     text            NOT NULL,
  message         text,
  slug            text,
  stage_id        text,
  CONSTRAINT cron_task_commit_record_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_task_commit_record_claim_idx ON cron_task_commit_record_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_task_commit_record_idem_idx ON cron_task_commit_record_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
