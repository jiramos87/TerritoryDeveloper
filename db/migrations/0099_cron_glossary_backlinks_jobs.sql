-- 0099_cron_glossary_backlinks_jobs.sql
--
-- TECH-18101 / async-cron-jobs Stage 4.0.1
--
-- Queue table for async glossary back-link enrichment runs processed by cron supervisor.
-- Cadence: */5 * * * * (every 5 min — rebuild job, moderate latency tolerance).
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   slug text (master-plan slug), plan_id uuid (ia_master_plans.plan_id FK).

BEGIN;

CREATE TABLE cron_glossary_backlinks_jobs (
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
  plan_id         uuid,
  CONSTRAINT cron_glossary_backlinks_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_glossary_backlinks_claim_idx ON cron_glossary_backlinks_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_glossary_backlinks_idem_idx ON cron_glossary_backlinks_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
