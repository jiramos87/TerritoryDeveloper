-- 0091_cron_journal_append_jobs.sql
--
-- TECH-18089 / async-cron-jobs Stage 1.0.1
--
-- Queue table for async journal-append writes processed by cron supervisor.
-- Cron handler drains rows and calls INSERT INTO ia_ship_stage_journal.
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   session_id, slug, stage_id, phase, payload_kind, payload
--                 (mirrors journal_append payload).

BEGIN;

CREATE TABLE cron_journal_append_jobs (
  job_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status          cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at     timestamptz     NOT NULL DEFAULT now(),
  started_at      timestamptz,
  finished_at     timestamptz,
  heartbeat_at    timestamptz,
  error           text,
  attempts        int             NOT NULL DEFAULT 0,
  idempotency_key text,
  -- kind-specific columns (journal_append shape)
  session_id      uuid,
  slug            text,
  stage_id        text,
  phase           text,
  payload_kind    text,
  payload         jsonb,
  task_id         text,
  CONSTRAINT cron_journal_append_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_journal_append_claim_idx ON cron_journal_append_jobs (status, enqueued_at);

-- Idempotency dedup index (partial)
CREATE UNIQUE INDEX cron_journal_append_idem_idx ON cron_journal_append_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
