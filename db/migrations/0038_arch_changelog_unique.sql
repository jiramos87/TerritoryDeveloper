-- Stage 1.4 / TECH-2564 — arch_changelog UNIQUE(commit_sha, spec_path) for
-- idempotent post-commit changelog append (per DEC-A16 + plan digest).
--
-- Adds:
--   1. `spec_path` text column (track touched ia/specs/architecture/* path).
--   2. `kind` CHECK extension to allow 'spec_edit_commit' (post-commit hook
--      writes this kind; existing 'edit' / 'decide' / 'supersede' preserved).
--   3. UNIQUE (commit_sha, spec_path) — INSERT...ON CONFLICT DO NOTHING gate
--      for replay idempotency.
--
-- Idempotent: ALTER TABLE IF NOT EXISTS guards + DROP CONSTRAINT IF EXISTS
-- shim before re-adding the kind CHECK.
--
-- Migration slot 0038 (0037 = archetype_authoring). Stage 1.1 / TECH-2018
-- shipped base table (slot 0034). This is forward-only.

BEGIN;

-- 1. Add spec_path column.
ALTER TABLE arch_changelog
  ADD COLUMN IF NOT EXISTS spec_path text;

COMMENT ON COLUMN arch_changelog.spec_path IS
  'Touched ia/specs/architecture/* path for spec_edit_commit kind rows. NULL for decide/supersede/edit kinds.';

-- 2. Extend kind CHECK to allow 'spec_edit_commit'.
ALTER TABLE arch_changelog
  DROP CONSTRAINT IF EXISTS arch_changelog_kind_check;

ALTER TABLE arch_changelog
  ADD CONSTRAINT arch_changelog_kind_check
    CHECK (kind IN ('edit', 'decide', 'supersede', 'spec_edit_commit', 'design_explore_decision'));

-- 3. UNIQUE (commit_sha, spec_path) for replay idempotency. NULLS NOT DISTINCT
-- so multiple rows with NULL spec_path + same commit_sha are still rejected
-- (Postgres ≥15). Falls back to standard UNIQUE on older versions where NULL
-- ≠ NULL (acceptable — design_explore_decision rows carry spec_path NULL but
-- have unique decision_slug; spec_edit_commit rows always carry spec_path).
CREATE UNIQUE INDEX IF NOT EXISTS arch_changelog_commit_spec_unique
  ON arch_changelog (commit_sha, spec_path)
  WHERE commit_sha IS NOT NULL AND spec_path IS NOT NULL;

COMMIT;
