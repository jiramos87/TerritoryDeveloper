-- IA dev system DB-primary refactor — step 9.6.6 (master plan preamble + change log).
-- Source: docs/ia-dev-db-refactor-implementation.md §Step 9.6 — Option A pivot
--
-- Adds two surfaces required for full DB pivot of master plan + stage data
-- (retires ia/projects/{slug}/{index.md, stage-X.Y-*.md} foldering at 9.6.11):
--
--   1. ia_master_plans.preamble — verbatim everything-above-`## Stages` block:
--      title heading, status note, scope, vision pointers, hierarchy rules,
--      sibling orchestrators in flight, parallel-work rules,
--      "Read first if landing cold" pointers. Single text blob — agents render
--      via MCP master_plan_render; no per-section parsing required.
--
--   2. ia_master_plan_change_log — append-only history rows. Replaces the
--      manual "Change log" sections currently scattered through index.md and
--      stage-*.md files. Captures closeout digests, sha backfills, status
--      flips, retired-skill notes, etc.
--
-- ia_stages.objective + ia_stages.exit_criteria already exist (added earlier
-- in Step 9 schema work); only the preamble + change_log surfaces are new.

BEGIN;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS preamble text;

CREATE TABLE IF NOT EXISTS ia_master_plan_change_log (
  entry_id    bigserial PRIMARY KEY,
  slug        text NOT NULL REFERENCES ia_master_plans(slug) ON DELETE CASCADE,
  ts          timestamptz NOT NULL DEFAULT now(),
  kind        text NOT NULL,
  body        text NOT NULL,
  actor       text,
  commit_sha  text
);

CREATE INDEX IF NOT EXISTS ia_master_plan_change_log_slug_ts_idx
  ON ia_master_plan_change_log (slug, ts DESC);

CREATE INDEX IF NOT EXISTS ia_master_plan_change_log_kind_idx
  ON ia_master_plan_change_log (kind);

COMMIT;
