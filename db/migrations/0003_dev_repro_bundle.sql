-- TECH-44c — E1 dev repro bundle registry (B1: scalars + payload jsonb).
-- Apply after 0001/0002 via tools/postgres-ia/apply-migrations.mjs

BEGIN;

CREATE TABLE IF NOT EXISTS dev_repro_bundle (
  id                    bigserial PRIMARY KEY,
  backlog_issue_id    text NOT NULL,
  git_sha               text NOT NULL,
  exported_at_utc       timestamptz NOT NULL DEFAULT now(),
  interchange_revision  integer NOT NULL DEFAULT 1,
  payload               jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS dev_repro_bundle_issue_exported_idx
  ON dev_repro_bundle (backlog_issue_id, exported_at_utc DESC);

CREATE INDEX IF NOT EXISTS dev_repro_bundle_git_sha_idx
  ON dev_repro_bundle (git_sha);

COMMENT ON TABLE dev_repro_bundle IS
  'TECH-44c E1: metadata linking tools/reports exports to BACKLOG issue id + git SHA; payload uses Interchange JSON-style artifact/schema_version inside jsonb.';

CREATE OR REPLACE FUNCTION dev_repro_list_by_issue(p_backlog_issue_id text, p_limit integer DEFAULT 20)
RETURNS SETOF dev_repro_bundle
LANGUAGE sql
STABLE
AS $$
  SELECT *
  FROM dev_repro_bundle
  WHERE backlog_issue_id = p_backlog_issue_id
  ORDER BY exported_at_utc DESC
  LIMIT LEAST(GREATEST(COALESCE(p_limit, 20), 1), 500);
$$;

COMMENT ON FUNCTION dev_repro_list_by_issue(text, integer) IS
  'Returns recent dev_repro_bundle rows for a canonical backlog_issue_id (normalize in app — same rules as territory-ia normalizeIssueId).';

COMMIT;
