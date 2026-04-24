-- 0016 — title trigram index on ia_tasks.
--
-- Step 3 of ia-dev-db-refactor: `task_spec_search` trgm mode is more useful
-- against short title strings than whole-body text (whole-body similarity is
-- near-zero under the default threshold). Add a dedicated GIN index so fuzzy
-- id/name lookup stays indexed.

CREATE INDEX IF NOT EXISTS ia_tasks_title_trgm_idx
  ON ia_tasks USING GIN (title gin_trgm_ops);
