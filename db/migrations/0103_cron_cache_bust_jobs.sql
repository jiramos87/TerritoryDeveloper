-- 0103_cron_cache_bust_jobs.sql
--
-- TECH-18104 / async-cron-jobs Stage 5.0.1
--
-- Queue table for async cache-bust invalidation runs processed by cron supervisor.
-- Cadence: * * * * * (every minute — post-write invalidation must be hot).
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   cache_kind        text NOT NULL (e.g. 'db_read_batch'),
--                 cache_key_pattern text NOT NULL (LIKE pattern; % wildcards OK).

BEGIN;

CREATE TABLE cron_cache_bust_jobs (
  job_id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status            cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at       timestamptz     NOT NULL DEFAULT now(),
  started_at        timestamptz,
  finished_at       timestamptz,
  heartbeat_at      timestamptz,
  error             text,
  attempts          int             NOT NULL DEFAULT 0,
  idempotency_key   text,
  -- kind-specific columns
  cache_kind        text            NOT NULL,
  cache_key_pattern text            NOT NULL,
  CONSTRAINT cron_cache_bust_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_cache_bust_claim_idx ON cron_cache_bust_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_cache_bust_idem_idx ON cron_cache_bust_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

-- Index on (cache_kind, cache_key_pattern) for pattern lookups
CREATE INDEX cron_cache_bust_kind_pattern_idx ON cron_cache_bust_jobs (cache_kind, cache_key_pattern);

COMMIT;
