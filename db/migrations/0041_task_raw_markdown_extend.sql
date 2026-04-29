-- DB Lifecycle Extensions Stage 1 / TECH-2973 — raw_markdown column safety net.
-- Source: docs/db-lifecycle-extensions-exploration.md §Approach A
--
-- Idempotent verify-or-add for `ia_tasks.raw_markdown`. The column was first
-- introduced in 0017_ia_tasks_raw_markdown.sql; this migration is a no-op on
-- modern DBs and a forward-only safety net for legacy clones (e.g. snapshots
-- restored from older freeze points) that may have skipped 0017.
--
-- ADD COLUMN IF NOT EXISTS — guaranteed idempotent on existing schema.

BEGIN;

ALTER TABLE ia_tasks
  ADD COLUMN IF NOT EXISTS raw_markdown text;

COMMIT;
