-- 0162_ia_feature_flags.sql
--
-- Wave D (vibe-coding-safety stage-5-0) — ia_feature_flags table.
-- Stores per-stage safety/experimental flags; Unity runtime hydrates from
-- interchange snapshot; web dashboard reads directly.
--
-- Idempotent: CREATE TABLE IF NOT EXISTS.

BEGIN;

CREATE TABLE IF NOT EXISTS ia_feature_flags (
  slug          TEXT        PRIMARY KEY,
  stage_id      BIGINT      REFERENCES ia_stages(id) ON DELETE SET NULL,
  enabled       BOOLEAN     NOT NULL DEFAULT FALSE,
  default_value BOOLEAN     NOT NULL DEFAULT FALSE,
  owner         TEXT,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE  ia_feature_flags                IS 'Per-feature flags driving vibe-coding safety proposals; hydrated into Unity via interchange snapshot.';
COMMENT ON COLUMN ia_feature_flags.slug           IS 'Stable kebab-case identifier — primary key.';
COMMENT ON COLUMN ia_feature_flags.stage_id       IS 'FK to ia_stages(id); NULL = global flag.';
COMMENT ON COLUMN ia_feature_flags.enabled        IS 'Current live value.';
COMMENT ON COLUMN ia_feature_flags.default_value  IS 'Fallback when Unity cannot read snapshot.';
COMMENT ON COLUMN ia_feature_flags.owner          IS 'Team / role that owns the flag lifecycle.';

COMMIT;
