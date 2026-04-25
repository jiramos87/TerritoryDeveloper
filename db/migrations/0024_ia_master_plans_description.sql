-- 0024_ia_master_plans_description.sql
--
-- Add `description` column to ia_master_plans — short product-terminology
-- overview + main goals (≤200 chars soft target, advisory only — not enforced
-- via DB CHECK so manual repairs don't trip the validator).
--
-- Authored case-by-case by master-plan-new from the preamble; required for
-- new plans by skill convention. Existing rows stay NULL until re-touched.

BEGIN;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS description text;

COMMENT ON COLUMN ia_master_plans.description IS
  'Short product overview + main goals (≤200 chars soft target). Authored from preamble; replaces preamble as primary dashboard subtitle.';

COMMIT;
