-- TECH-55 — per-export Editor Reports registry (B1: scalars + payload jsonb).
-- Apply after 0003 via tools/postgres-ia/apply-migrations.mjs

BEGIN;

CREATE TABLE IF NOT EXISTS editor_export_agent_context (
  id                    bigserial PRIMARY KEY,
  backlog_issue_id      text NOT NULL,
  git_sha               text NOT NULL,
  exported_at_utc       timestamptz NOT NULL DEFAULT now(),
  interchange_revision  integer NOT NULL DEFAULT 1,
  payload               jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS editor_export_agent_context_issue_exported_idx
  ON editor_export_agent_context (backlog_issue_id, exported_at_utc DESC);

CREATE INDEX IF NOT EXISTS editor_export_agent_context_git_sha_idx
  ON editor_export_agent_context (git_sha);

COMMENT ON TABLE editor_export_agent_context IS
  'TECH-55: one row per Territory Developer → Reports → Export Agent Context (tools/reports/agent-context-*.json).';

CREATE TABLE IF NOT EXISTS editor_export_sorting_debug (
  id                    bigserial PRIMARY KEY,
  backlog_issue_id      text NOT NULL,
  git_sha               text NOT NULL,
  exported_at_utc       timestamptz NOT NULL DEFAULT now(),
  interchange_revision  integer NOT NULL DEFAULT 1,
  payload               jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS editor_export_sorting_debug_issue_exported_idx
  ON editor_export_sorting_debug (backlog_issue_id, exported_at_utc DESC);

CREATE INDEX IF NOT EXISTS editor_export_sorting_debug_git_sha_idx
  ON editor_export_sorting_debug (git_sha);

COMMENT ON TABLE editor_export_sorting_debug IS
  'TECH-55: one row per Export Sorting Debug (Markdown) (tools/reports/sorting-debug-*.md).';

CREATE TABLE IF NOT EXISTS editor_export_terrain_cell_chunk (
  id                    bigserial PRIMARY KEY,
  backlog_issue_id      text NOT NULL,
  git_sha               text NOT NULL,
  exported_at_utc       timestamptz NOT NULL DEFAULT now(),
  interchange_revision  integer NOT NULL DEFAULT 1,
  payload               jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS editor_export_terrain_cell_chunk_issue_exported_idx
  ON editor_export_terrain_cell_chunk (backlog_issue_id, exported_at_utc DESC);

CREATE INDEX IF NOT EXISTS editor_export_terrain_cell_chunk_git_sha_idx
  ON editor_export_terrain_cell_chunk (git_sha);

COMMENT ON TABLE editor_export_terrain_cell_chunk IS
  'TECH-55: one row per Export Cell Chunk (Interchange); payload mirrors artifact terrain_cell_chunk + relative_path.';

CREATE TABLE IF NOT EXISTS editor_export_world_snapshot_dev (
  id                    bigserial PRIMARY KEY,
  backlog_issue_id      text NOT NULL,
  git_sha               text NOT NULL,
  exported_at_utc       timestamptz NOT NULL DEFAULT now(),
  interchange_revision  integer NOT NULL DEFAULT 1,
  payload               jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS editor_export_world_snapshot_dev_issue_exported_idx
  ON editor_export_world_snapshot_dev (backlog_issue_id, exported_at_utc DESC);

CREATE INDEX IF NOT EXISTS editor_export_world_snapshot_dev_git_sha_idx
  ON editor_export_world_snapshot_dev (git_sha);

COMMENT ON TABLE editor_export_world_snapshot_dev IS
  'TECH-55: one row per Export World Snapshot (Dev Interchange); payload mirrors artifact world_snapshot_dev + relative_path.';

COMMIT;
