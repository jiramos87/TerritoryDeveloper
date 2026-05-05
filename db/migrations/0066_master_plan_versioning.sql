-- 0066_master_plan_versioning.sql
--
-- ship-protocol Stage 1.0 / TECH-12629 — versioning + closeout columns on
-- `ia_master_plans` for the new ship-protocol plan-pipeline:
--
--   parent_plan_slug text NULL REFERENCES ia_master_plans(slug) ON DELETE SET NULL
--     — non-NULL = this plan is a versioned successor of the parent.
--     — ON DELETE SET NULL preserves child rows when parent retired.
--
--   version          INTEGER NOT NULL DEFAULT 1
--     — monotonically increases per-slug version chain. Default covers all
--       pre-existing rows (in-place backfill needs no UPDATE).
--
--   closed_at        TIMESTAMPTZ NULL
--     — NULL = open plan; non-NULL = closed-at-this-moment.
--     — matches ia_tasks.closed_at precedent.
--
-- Plus btree index `idx_ia_master_plans_parent_plan_slug` on `(parent_plan_slug)`
-- for parent-lookup queries / ON DELETE SET NULL scan.
--
-- Idempotent: ADD COLUMN IF NOT EXISTS / CREATE INDEX IF NOT EXISTS — re-run
-- produces zero schema diff. No row migration required.

BEGIN;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS parent_plan_slug text
    REFERENCES ia_master_plans(slug) ON DELETE SET NULL;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS version INTEGER NOT NULL DEFAULT 1;

ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS closed_at TIMESTAMPTZ NULL;

CREATE INDEX IF NOT EXISTS idx_ia_master_plans_parent_plan_slug
  ON ia_master_plans (parent_plan_slug);

COMMIT;
