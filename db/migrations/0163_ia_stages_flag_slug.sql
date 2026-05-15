-- 0163_ia_stages_flag_slug.sql
--
-- Wave D (vibe-coding-safety stage-5-0) — optional flag_slug pointer on ia_stages.
-- Lets a Stage row reference its associated feature flag in ia_feature_flags.
--
-- Idempotent: ADD COLUMN IF NOT EXISTS.

BEGIN;

ALTER TABLE ia_stages
  ADD COLUMN IF NOT EXISTS flag_slug TEXT
    REFERENCES ia_feature_flags(slug) ON DELETE SET NULL;

COMMENT ON COLUMN ia_stages.flag_slug IS 'Optional FK to ia_feature_flags(slug); links a Stage to its gate flag.';

COMMIT;
