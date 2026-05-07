-- 0100_cron_anchor_reindex_jobs.sql
--
-- TECH-18101 / async-cron-jobs Stage 4.0.1
--
-- Queue table for async anchor reindex runs processed by cron supervisor.
-- Cadence: */5 * * * * (every 5 min — rebuild job, moderate latency tolerance).
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   paths text[] (changed spec file paths to reindex).

BEGIN;

CREATE TABLE cron_anchor_reindex_jobs (
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
  paths           text[]          NOT NULL DEFAULT '{}',
  CONSTRAINT cron_anchor_reindex_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_anchor_reindex_claim_idx ON cron_anchor_reindex_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_anchor_reindex_idem_idx ON cron_anchor_reindex_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

-- GIN index on paths for path-pattern lookups (e.g. WHERE paths @> ARRAY['ia/specs/glossary.md'])
CREATE INDEX cron_anchor_reindex_paths_gin_idx ON cron_anchor_reindex_jobs USING GIN (paths);

COMMIT;
