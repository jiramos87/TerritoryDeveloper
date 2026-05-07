-- 0096_cron_materialize_backlog_jobs.sql
--
-- TECH-18097 / async-cron-jobs Stage 3.0.1
--
-- Queue table for async materialize-backlog runs processed by cron supervisor.
-- Cadence: */2 * * * * (every 2 min — ops-heavy, throttle to even minutes).
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   triggered_by text (caller tag e.g. 'project-new-apply', 'closeout-tail').

BEGIN;

CREATE TABLE cron_materialize_backlog_jobs (
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
  triggered_by    text,
  CONSTRAINT cron_materialize_backlog_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_materialize_backlog_claim_idx ON cron_materialize_backlog_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_materialize_backlog_idem_idx ON cron_materialize_backlog_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
