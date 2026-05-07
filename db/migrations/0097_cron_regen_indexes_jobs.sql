-- 0097_cron_regen_indexes_jobs.sql
--
-- TECH-18097 / async-cron-jobs Stage 3.0.1
--
-- Queue table for async regen-indexes runs processed by cron supervisor.
-- Cadence: */5 * * * * (every 5 min).
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   scope text — one of 'all', 'glossary', 'specs'.

BEGIN;

CREATE TABLE cron_regen_indexes_jobs (
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
  scope           text            NOT NULL DEFAULT 'all',
  CONSTRAINT cron_regen_indexes_status_check CHECK (status IN ('queued', 'running', 'done', 'failed')),
  CONSTRAINT cron_regen_indexes_scope_check CHECK (scope IN ('all', 'glossary', 'specs'))
);

-- FIFO claim index
CREATE INDEX cron_regen_indexes_claim_idx ON cron_regen_indexes_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_regen_indexes_idem_idx ON cron_regen_indexes_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
