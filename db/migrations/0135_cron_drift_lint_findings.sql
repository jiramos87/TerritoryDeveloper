-- 0135_cron_drift_lint_findings.sql
--
-- Lifecycle skills refactor — Phase 1 / weak-spot #2.
--
-- Crash-safe drift-lint findings stash. Replaces in-memory drift buffer in
-- ship-plan Phase 6.
--
-- Two-phase commit pattern:
--   1. Agent enqueues findings BEFORE master_plan_bundle_apply with status='staged'.
--   2. master_plan_bundle_apply success calls promote_drift_lint_staged()
--      which flips matching staged rows → 'queued'.
--   3. Cron drainer skips 'staged' rows; only drains 'queued'.
--
-- Distinct from cron_drift_lint_jobs (TECH-18105 sweep): that queue runs
-- drift-lint over committed code; this one stashes pre-bundle findings tied
-- to a specific (slug, version) authoring pass.
--
-- 'staged' status is added to the cron_job_status enum here (additive — no
-- existing handler reads it; drainer skips unknown statuses by virtue of
-- WHERE status='queued' filters).

BEGIN;

-- Add 'staged' value to shared enum (additive, safe).
ALTER TYPE cron_job_status ADD VALUE IF NOT EXISTS 'staged';

COMMIT;

-- Separate transaction — Postgres requires enum value commit before use.
BEGIN;

CREATE TABLE cron_drift_lint_findings_jobs (
  job_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  status          cron_job_status NOT NULL DEFAULT 'staged',
  enqueued_at     timestamptz     NOT NULL DEFAULT now(),
  started_at      timestamptz,
  finished_at     timestamptz,
  heartbeat_at    timestamptz,
  error           text,
  attempts        int             NOT NULL DEFAULT 0,
  idempotency_key text,
  -- kind-specific payload
  slug            text            NOT NULL,
  version         int             NOT NULL DEFAULT 1,
  findings        jsonb           NOT NULL DEFAULT '[]'::jsonb,
  n_resolved      int             NOT NULL DEFAULT 0,
  n_unresolved    int             NOT NULL DEFAULT 0,
  CONSTRAINT cron_drift_lint_findings_status_check
    CHECK (status IN ('staged', 'queued', 'running', 'done', 'failed'))
);

-- FIFO claim index — drainer filters status='queued' (skips 'staged')
CREATE INDEX cron_drift_lint_findings_claim_idx ON cron_drift_lint_findings_jobs (status, enqueued_at);

-- Lookup index for the promote SQL fn
CREATE INDEX cron_drift_lint_findings_slug_version_idx ON cron_drift_lint_findings_jobs (slug, version, status);

-- Idempotency dedup index (partial — only when key provided)
CREATE UNIQUE INDEX cron_drift_lint_findings_idem_idx ON cron_drift_lint_findings_jobs (idempotency_key) WHERE idempotency_key IS NOT NULL;

-- Two-phase commit promotion fn — flips staged → queued for a given (slug, version).
-- Called inside master_plan_bundle_apply tx after plan version row insert.
CREATE OR REPLACE FUNCTION promote_drift_lint_staged(p_slug text, p_version int)
RETURNS int
LANGUAGE plpgsql
AS $$
DECLARE
  flipped int;
BEGIN
  UPDATE cron_drift_lint_findings_jobs
     SET status      = 'queued',
         enqueued_at = now()
   WHERE slug    = p_slug
     AND version = p_version
     AND status  = 'staged';
  GET DIAGNOSTICS flipped = ROW_COUNT;
  RETURN flipped;
END;
$$;

COMMIT;
