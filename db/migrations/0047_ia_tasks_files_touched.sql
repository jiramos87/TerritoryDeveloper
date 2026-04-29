-- 0047_ia_tasks_files_touched.sql
--
-- db-lifecycle-extensions Stage 3 / TECH-3402.
-- Adds three JSONB columns powering author-time anomaly detection:
--   - `ia_tasks.expected_files_touched jsonb` — author-declared file list at
--     `/stage-decompose` time (nullable; pre-0047 Tasks tolerate NULL).
--   - `ia_tasks.actual_files_touched jsonb`   — commit-derived file list
--     populated by `/ship-stage` Pass B closeout (nullable; pre-0047 Tasks
--     tolerate NULL).
--   - `ia_master_plans.tolerance_globs jsonb` — minimatch-style glob array
--     filtering false-positive `unexpected_files` in T3.5 anomaly scan.
--     Default empty array `[]` (NOT NULL) — empty array means strict mode.
--
-- BF=forward-only: no historical row backfill. Pre-0047 Tasks remain
-- queryable; T3.5 `task_diff_anomaly_scan` skips NULL rows silently.
--
-- Idempotent: `ADD COLUMN IF NOT EXISTS` on all three. Re-applying is a
-- no-op.

BEGIN;

ALTER TABLE ia_tasks
  ADD COLUMN IF NOT EXISTS expected_files_touched jsonb;

ALTER TABLE ia_tasks
  ADD COLUMN IF NOT EXISTS actual_files_touched jsonb;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS tolerance_globs jsonb NOT NULL DEFAULT '[]'::jsonb;

COMMENT ON COLUMN ia_tasks.expected_files_touched IS
  'Author-declared expected file paths at /stage-decompose time. NULL on pre-0047 rows. Diffed against actual_files_touched by task_diff_anomaly_scan (db-lifecycle-extensions Stage 3 / TECH-3402).';

COMMENT ON COLUMN ia_tasks.actual_files_touched IS
  'Commit-derived actual file paths populated by /ship-stage Pass B closeout. NULL on pre-0047 rows. Diffed against expected_files_touched by task_diff_anomaly_scan.';

COMMENT ON COLUMN ia_master_plans.tolerance_globs IS
  'Minimatch-style glob array filtering false-positive unexpected_files in task_diff_anomaly_scan. Default empty array means strict mode.';

COMMIT;

-- Rollback (manual, not auto-run):
--   BEGIN;
--   ALTER TABLE ia_tasks DROP COLUMN IF EXISTS expected_files_touched;
--   ALTER TABLE ia_tasks DROP COLUMN IF EXISTS actual_files_touched;
--   ALTER TABLE ia_master_plans DROP COLUMN IF EXISTS tolerance_globs;
--   COMMIT;
