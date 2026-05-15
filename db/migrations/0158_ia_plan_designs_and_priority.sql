-- 0158_ia_plan_designs_and_priority.sql
--
-- DB-primary design seed storage + master-plan priority triage.
--
-- Three additive changes:
--
-- 1. New table `ia_plan_designs` — stores design-explore output as
--    first-class DB rows. Owns the final master-plan slug + priority +
--    lifecycle status (draft / ready / consumed / archived). Replaces
--    the ephemeral YAML frontmatter at `docs/explorations/{slug}.md`
--    as the source of truth (YAML kept as transient projection for
--    legacy HTML renderer; cutover deferred).
--
-- 2. `ia_master_plans.priority` — P0/P1/P2/P3 enum (CHECK constraint).
--    Default P2. Enables ship-order triage on open plans.
--
-- 3. `ia_master_plans.design_id` — nullable FK to ia_plan_designs(id).
--    Existing rows = NULL (no backfill). New plans link through ship-plan
--    Phase C via master_plan_bundle_apply (see mig 0159).
--
-- Idempotent: CREATE TABLE / ADD COLUMN / CREATE INDEX all use IF NOT EXISTS.

BEGIN;

-- 1. ia_plan_designs — design-explore seed rows.
CREATE TABLE IF NOT EXISTS ia_plan_designs (
  id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  slug             TEXT NOT NULL UNIQUE,
  title            TEXT NOT NULL,
  priority         TEXT NOT NULL DEFAULT 'P2'
                   CHECK (priority IN ('P0','P1','P2','P3')),
  status           TEXT NOT NULL DEFAULT 'draft'
                   CHECK (status IN ('draft','ready','consumed','archived')),
  body_md          TEXT,
  stages_yaml      JSONB,
  parent_plan_slug TEXT REFERENCES ia_master_plans(slug) ON DELETE SET NULL,
  target_version   INTEGER NOT NULL DEFAULT 1,
  created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_ia_plan_designs_priority
  ON ia_plan_designs (priority);
CREATE INDEX IF NOT EXISTS idx_ia_plan_designs_status
  ON ia_plan_designs (status);
CREATE INDEX IF NOT EXISTS idx_ia_plan_designs_parent_plan_slug
  ON ia_plan_designs (parent_plan_slug);

COMMENT ON TABLE ia_plan_designs IS
  'Mig 0158: DB-primary design seed storage. Owns slug + priority + lifecycle status (draft/ready/consumed/archived). Replaces YAML frontmatter at docs/explorations/{slug}.md as source of truth.';

-- 2. ia_master_plans.priority — P0/P1/P2/P3 triage enum.
ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS priority TEXT NOT NULL DEFAULT 'P2'
    CHECK (priority IN ('P0','P1','P2','P3'));

CREATE INDEX IF NOT EXISTS idx_ia_master_plans_priority
  ON ia_master_plans (priority);

-- 3. ia_master_plans.design_id — nullable FK to seed row.
ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS design_id BIGINT
    REFERENCES ia_plan_designs(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_ia_master_plans_design_id
  ON ia_master_plans (design_id);

COMMIT;
