-- 0101_cron_drift_lint_jobs.sql
--
-- TECH-18104 / async-cron-jobs Stage 5.0.1
--
-- Queue table for async drift-lint sweep runs processed by cron supervisor.
-- Cadence: */10 * * * * (every 10 min — sweep, low urgency).
--
-- Common columns: job_id, status, enqueued/started/finished/heartbeat timestamps,
--                 error, attempts, idempotency_key.
-- Kind columns:   commit_sha text (optional — context for triggered sweep),
--                 slug text (optional — scope to one plan slug).

BEGIN;

CREATE TABLE cron_drift_lint_jobs (
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
  commit_sha      text,
  slug            text,
  CONSTRAINT cron_drift_lint_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_drift_lint_claim_idx ON cron_drift_lint_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_drift_lint_idem_idx ON cron_drift_lint_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
