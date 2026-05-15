-- 0160_ia_master_plans_ears_grandfathered.sql
--
-- Add ears_grandfathered BOOLEAN column to ia_master_plans.
--
-- Plans created before Wave B ship (stage-3-0 of vibe-coding-safety) are
-- backfilled to TRUE so validate:plan-digest-coverage skips EARS rubric
-- enforcement on them. Plans created after = FALSE by default.
--
-- Mirrors existing tdd_red_green_grandfathered column pattern.
--
-- Idempotent: ADD COLUMN IF NOT EXISTS + UPDATE are both safe to re-run.

BEGIN;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS ears_grandfathered BOOLEAN NOT NULL DEFAULT FALSE;

-- Backfill: all plans created before this migration run get grandfathered.
-- $WAVE_B_SHIP_TS = captured at migration apply time via now().
UPDATE ia_master_plans
  SET ears_grandfathered = TRUE
  WHERE created_at < now();

COMMENT ON COLUMN ia_master_plans.ears_grandfathered IS
  'Mig 0160: TRUE = plan predates EARS rubric (Wave B); validate:plan-digest-coverage skips EARS prefix check. Mirrors tdd_red_green_grandfathered pattern.';

COMMIT;
