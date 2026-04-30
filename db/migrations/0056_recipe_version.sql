-- 0056_recipe_version.sql
--
-- recipe-runner-phase-e Stage 1.3 / TECH-6962.
-- Additive column: recipe_version int NOT NULL DEFAULT 1 on ia_recipe_runs.
-- Legacy rows inherit default 1 (pre-versioning = DSL version 1).
-- Unblocks TECH-6963 engine field-read + P3 recipe versioning.
--
-- Idempotent: ADD COLUMN IF NOT EXISTS.

BEGIN;

ALTER TABLE ia_recipe_runs
  ADD COLUMN IF NOT EXISTS recipe_version INT NOT NULL DEFAULT 1;

COMMENT ON COLUMN ia_recipe_runs.recipe_version IS
  'Recipe DSL version written at run time. Default 1 for legacy rows (pre-versioning). Explicit version unlocks future kind behaviours in the engine.';

COMMIT;

-- =========================================================================
-- Rollback (manual, not auto-run):
--   BEGIN;
--   ALTER TABLE ia_recipe_runs DROP COLUMN IF EXISTS recipe_version;
--   COMMIT;
-- =========================================================================
