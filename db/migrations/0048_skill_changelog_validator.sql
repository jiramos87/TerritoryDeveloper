-- 0048_skill_changelog_validator.sql
--
-- db-lifecycle-extensions Stage 3 / TECH-3402.
-- Comment-only migration recording validator wiring intent for
-- `validate:skill-changelog-presence`.
--
-- The validator itself is a Node.js script
-- (`tools/scripts/validate-skill-changelog-presence.mjs`) joined into
-- `validate:all` via package.json. No DB schema mutation here — this slot
-- exists to honor the Stage 3 acceptance criteria explicitly listing
-- migration `0048` as the validator-wiring landmark and to keep migration
-- counters monotonic for downstream stages.
--
-- Idempotent (no-op): wraps a trivial `SELECT 1` so re-applying succeeds.

BEGIN;

SELECT 1 AS skill_changelog_validator_wiring_marker;

COMMIT;
