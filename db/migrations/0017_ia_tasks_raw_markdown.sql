-- IA dev system DB-primary refactor — step 5 (raw_markdown column).
-- Source: docs/ia-dev-db-refactor-implementation.md §Step 5
--
-- Adds ia_tasks.raw_markdown — the verbatim BACKLOG.md row block
-- (checklist line + sub-bullets) authored per issue. Columns `title`,
-- `type`, `priority`, `notes`, dep tables already hold the structured
-- fields; raw_markdown is the pre-composed display block preserved so
-- the Step 5 generator can emit byte-identical BACKLOG.md without
-- reconstructing prose from structured fields.

BEGIN;

ALTER TABLE ia_tasks
  ADD COLUMN IF NOT EXISTS raw_markdown text;

COMMIT;
