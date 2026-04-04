-- UI design system baseline: UI inventory JSON (multi-scene uGUI snapshot) in Editor export registry.
-- Apply after 0005 via tools/postgres-ia/apply-migrations.mjs

BEGIN;

CREATE TABLE IF NOT EXISTS editor_export_ui_inventory (
  id                    bigserial PRIMARY KEY,
  backlog_issue_id      text,
  git_sha               text NOT NULL,
  exported_at_utc       timestamptz NOT NULL DEFAULT now(),
  interchange_revision  integer NOT NULL DEFAULT 1,
  payload               jsonb NOT NULL,
  document              jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS editor_export_ui_inventory_issue_exported_idx
  ON editor_export_ui_inventory (backlog_issue_id, exported_at_utc DESC);

CREATE INDEX IF NOT EXISTS editor_export_ui_inventory_git_sha_idx
  ON editor_export_ui_inventory (git_sha);

CREATE INDEX IF NOT EXISTS editor_export_ui_inventory_document_gin
  ON editor_export_ui_inventory USING gin (document jsonb_path_ops);

COMMENT ON TABLE editor_export_ui_inventory IS
  'UI design system baseline: one row per Territory Developer → Reports → Export UI Inventory (JSON); document.artifact ui_inventory_dev, scenes[] uGUI snapshot.';

COMMIT;
