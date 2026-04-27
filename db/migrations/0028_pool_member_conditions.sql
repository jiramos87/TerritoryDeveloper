-- Add `conditions_json` to `pool_member` (TECH-1788).
--
-- Additive, NULL-safe, idempotent. Stage 7.1 pool member editor binds
-- per-row predicate vocab into this column per DEC-A10.
--
-- @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest

BEGIN;

ALTER TABLE pool_member
  ADD COLUMN IF NOT EXISTS conditions_json jsonb NOT NULL DEFAULT '{}'::jsonb;

COMMIT;

-- Rollback: ALTER TABLE pool_member DROP COLUMN IF EXISTS conditions_json;
