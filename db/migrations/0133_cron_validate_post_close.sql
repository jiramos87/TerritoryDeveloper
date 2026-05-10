-- 0133_cron_validate_post_close.sql
--
-- Lifecycle skills refactor — Phase 1 / weak-spot #10.
--
-- Queue table for non-blocking post-close validate runs. ship-cycle Pass B
-- recipe enqueues; cron handler shells `npm run validate:fast --diff-paths {csv}`
-- scoped to the stage commit. Mirrors mig 0090 cron_audit_log_jobs shape.
--
-- Reuses cron_job_status enum from mig 0090 (single shared enum across all
-- queues; new migrations DO NOT recreate it).

BEGIN;

CREATE TABLE cron_validate_post_close_jobs (
  job_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status          cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at     timestamptz     NOT NULL DEFAULT now(),
  started_at      timestamptz,
  finished_at     timestamptz,
  heartbeat_at    timestamptz,
  error           text,
  attempts        int             NOT NULL DEFAULT 0,
  idempotency_key text,
  -- kind-specific payload
  slug            text            NOT NULL,
  stage_id        text            NOT NULL,
  commit_sha      text,
  diff_paths      jsonb           NOT NULL DEFAULT '[]'::jsonb,
  validate_kind   text            NOT NULL DEFAULT 'fast',
  exit_code       int,
  stdout_excerpt  text,
  CONSTRAINT cron_validate_post_close_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_validate_post_close_claim_idx ON cron_validate_post_close_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_validate_post_close_idem_idx ON cron_validate_post_close_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
