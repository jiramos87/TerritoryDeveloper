-- 0062_master_plan_grandfathered.sql
--
-- tdd-red-green-methodology Stage 6 / TECH-10907.
-- Adds `tdd_red_green_grandfathered` boolean column to `ia_master_plans`.
-- Backfills TRUE for every row that exists at apply time so all in-flight
-- plans (incl. tdd-red-green-methodology itself) are exempt from the
-- §Red-Stage Proof gate.
-- New rows default to FALSE → forward-only enforcement for plans authored
-- after this migration applies.

BEGIN;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS tdd_red_green_grandfathered BOOLEAN NOT NULL DEFAULT FALSE;

-- Backfill: every pre-existing plan is grandfathered.
UPDATE ia_master_plans
   SET tdd_red_green_grandfathered = TRUE
 WHERE created_at <= now();

COMMENT ON COLUMN ia_master_plans.tdd_red_green_grandfathered IS
  'TRUE = plan predates tdd-red-green-methodology Stage 6 cutover; '
  'validate:plan-red-stage and Pass A entry gate skip §Red-Stage Proof checks. '
  'FALSE (default) = plan authored after Stage 6 ships; gate enforced.';

COMMIT;
