-- Migration 0156: ia_visual_baseline + ia_visual_diff tables for pixel-diff visual regression.
-- ui-visual-regression Stage 1.0 — TECH-31890

CREATE TABLE IF NOT EXISTS ia_visual_baseline (
  id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  panel_entity_id    bigint REFERENCES catalog_entity(id) ON DELETE SET NULL,
  panel_version_id   bigint REFERENCES entity_version(id) ON DELETE SET NULL,
  panel_slug         text NOT NULL,
  image_ref          text NOT NULL,
  image_sha256       char(64) NOT NULL,
  resolution         text NOT NULL DEFAULT '1920x1080',
  theme              text NOT NULL DEFAULT 'dark',
  tolerance_pct      numeric(5,4) NOT NULL DEFAULT 0.0050,
  captured_at        timestamptz NOT NULL DEFAULT now(),
  captured_by        text,
  supersedes_id      uuid REFERENCES ia_visual_baseline(id) ON DELETE SET NULL,
  status             text NOT NULL DEFAULT 'active'
                       CHECK (status IN ('active', 'retired', 'candidate'))
);

CREATE INDEX IF NOT EXISTS ia_visual_baseline_slug_idx
  ON ia_visual_baseline (panel_slug, resolution, theme, status);

CREATE TABLE IF NOT EXISTS ia_visual_diff (
  id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  baseline_id      uuid NOT NULL REFERENCES ia_visual_baseline(id) ON DELETE CASCADE,
  candidate_hash   char(64) NOT NULL,
  diff_pct         numeric(6,5) NOT NULL DEFAULT 0.0,
  verdict          text NOT NULL
                     CHECK (verdict IN ('match', 'regression', 'new_baseline_needed')),
  diff_image_ref   text,
  region_map       jsonb,
  ran_at           timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ia_visual_diff_baseline_idx
  ON ia_visual_diff (baseline_id, ran_at DESC);
