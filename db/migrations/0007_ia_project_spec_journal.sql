-- IA project spec journal — verbose Decision Log + Lessons learned at project-spec-close.
-- Full-text search over body_markdown; keywords array for token overlap queries.

BEGIN;

CREATE TABLE IF NOT EXISTS ia_project_spec_journal (
  id                 bigserial PRIMARY KEY,
  backlog_issue_id   text NOT NULL,
  entry_kind         text NOT NULL CHECK (entry_kind IN ('decision_log', 'lessons_learned')),
  body_markdown      text NOT NULL,
  keywords           text[] NOT NULL DEFAULT '{}',
  source_spec_path   text NOT NULL,
  recorded_at        timestamptz NOT NULL DEFAULT now(),
  git_sha            text,
  body_tsv           tsvector GENERATED ALWAYS AS (
    to_tsvector('english', coalesce(body_markdown, ''))
  ) STORED
);

CREATE INDEX IF NOT EXISTS ia_project_spec_journal_issue_idx
  ON ia_project_spec_journal (backlog_issue_id);

CREATE INDEX IF NOT EXISTS ia_project_spec_journal_kind_idx
  ON ia_project_spec_journal (entry_kind);

CREATE INDEX IF NOT EXISTS ia_project_spec_journal_recorded_idx
  ON ia_project_spec_journal (recorded_at DESC);

CREATE INDEX IF NOT EXISTS ia_project_spec_journal_body_tsv_idx
  ON ia_project_spec_journal USING GIN (body_tsv);

CREATE INDEX IF NOT EXISTS ia_project_spec_journal_keywords_idx
  ON ia_project_spec_journal USING GIN (keywords);

COMMIT;
