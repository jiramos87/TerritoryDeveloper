-- 0134_cron_unity_compile_verify.sql
--
-- Lifecycle skills refactor — Phase 1 / weak-spot #9.
--
-- Queue table for non-blocking live-Editor compile poll. Replaces the
-- synchronous 60s block in ship-cycle Phase 8 step 0a. Cron handler polls
-- unity_bridge_command(kind="get_compilation_status") and writes verdict
-- back to job row + ia_stage_verifications.
--
-- Resume gate of next /ship-cycle reads verdict via master_plan_state extension.

BEGIN;

CREATE TABLE cron_unity_compile_verify_jobs (
  job_id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status              cron_job_status NOT NULL DEFAULT 'queued',
  enqueued_at         timestamptz     NOT NULL DEFAULT now(),
  started_at          timestamptz,
  finished_at         timestamptz,
  heartbeat_at        timestamptz,
  error               text,
  attempts            int             NOT NULL DEFAULT 0,
  idempotency_key     text,
  -- kind-specific payload
  slug                text            NOT NULL,
  stage_id            text            NOT NULL,
  commit_sha          text,
  bridge_lease_id     text,
  verdict_out         text,
  last_error_excerpt  text,
  CONSTRAINT cron_unity_compile_verify_status_check CHECK (status IN ('queued', 'running', 'done', 'failed'))
);

-- FIFO claim index
CREATE INDEX cron_unity_compile_verify_claim_idx ON cron_unity_compile_verify_jobs (status, enqueued_at);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_unity_compile_verify_idem_idx ON cron_unity_compile_verify_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

COMMIT;
