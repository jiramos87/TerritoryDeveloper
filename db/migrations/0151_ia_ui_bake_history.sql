-- 0151_ia_ui_bake_history.sql
-- Layer 6 auditability (TECH-28378):
--   ia_ui_bake_history: one row per panel bake (slug, ts, version, diff_summary, commit_sha).
--   ia_bake_diffs:      per-field change rows FK → ia_ui_bake_history.

BEGIN;

CREATE TABLE IF NOT EXISTS ia_ui_bake_history (
  id                   bigserial PRIMARY KEY,
  panel_slug           text        NOT NULL,
  baked_at             timestamptz NOT NULL DEFAULT now(),
  bake_handler_version text        NOT NULL,
  diff_summary         jsonb       NOT NULL DEFAULT '{}',
  commit_sha           text        NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS ia_ui_bake_history_panel_baked_at
  ON ia_ui_bake_history (panel_slug, baked_at DESC);

COMMENT ON TABLE ia_ui_bake_history IS
  'Layer 6 auditability (TECH-28378) — one row per UiBakeHandler run. '
  'diff_summary = BakeDiffer.Diff output (jsonb). '
  'commit_sha = HEAD sha at bake time.';

CREATE TABLE IF NOT EXISTS ia_bake_diffs (
  id           bigserial PRIMARY KEY,
  history_id   bigint      NOT NULL REFERENCES ia_ui_bake_history(id) ON DELETE CASCADE,
  change_kind  text        NOT NULL,
  child_kind   text        NOT NULL DEFAULT '',
  slug         text        NOT NULL DEFAULT '',
  before       jsonb,
  after        jsonb
);

CREATE INDEX IF NOT EXISTS ia_bake_diffs_history_id
  ON ia_bake_diffs (history_id);

COMMENT ON TABLE ia_bake_diffs IS
  'Layer 6 auditability (TECH-28378) — per-field change rows for each bake. '
  'FK to ia_ui_bake_history. change_kind ∈ {added,removed,modified}.';

DO $$
BEGIN
  RAISE NOTICE '0151 OK: ia_ui_bake_history + ia_bake_diffs created (Layer 6 auditability)';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DROP TABLE IF EXISTS ia_bake_diffs;
--   DROP TABLE IF EXISTS ia_ui_bake_history;
