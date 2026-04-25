-- Drop legacy filesystem-anchor columns from IA tables.
--
-- The IA system is DB-authoritative; master plans + stages + task specs live
-- in `ia_master_plans` / `ia_stages` / `ia_tasks` / `ia_task_specs` rows, not
-- on disk. The `source_spec_path` / `source_file_path` columns retained
-- legacy `ia/projects/{slug}-master-plan.md` strings that no callers use as
-- real paths — kept only as diagnostic lint. Removing them eliminates the
-- mismatch between schema shape and substrate.
--
-- Tables touched:
--   1. ia_master_plans       — DROP source_spec_path
--   2. ia_stages             — DROP source_file_path
--   3. ia_project_spec_journal — DROP source_spec_path
--
-- Wrap in single tx for atomic rollback on any caller breakage.

BEGIN;

ALTER TABLE ia_master_plans       DROP COLUMN IF EXISTS source_spec_path;
ALTER TABLE ia_stages             DROP COLUMN IF EXISTS source_file_path;
ALTER TABLE ia_project_spec_journal DROP COLUMN IF EXISTS source_spec_path;

COMMIT;
