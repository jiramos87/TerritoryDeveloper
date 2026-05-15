-- 0161_ia_master_plan_specs.sql
--
-- Create ia_master_plan_specs table for /spec-freeze gate.
--
-- Stores frozen spec snapshots per (slug, version) with open questions count
-- and frozen_at timestamp. /ship-plan Phase A queries this table and rejects
-- if the latest row has frozen_at IS NULL or open_questions_count > 0.
--
-- Idempotent: CREATE TABLE IF NOT EXISTS.

BEGIN;

CREATE TABLE IF NOT EXISTS ia_master_plan_specs (
  id                   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  slug                 TEXT NOT NULL
                         REFERENCES ia_master_plans(slug) ON DELETE CASCADE,
  version              INTEGER NOT NULL DEFAULT 1,
  frozen_at            TIMESTAMPTZ,
  body                 TEXT NOT NULL DEFAULT '',
  open_questions_count INTEGER NOT NULL DEFAULT 0,
  created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (slug, version)
);

CREATE INDEX IF NOT EXISTS idx_ia_master_plan_specs_slug
  ON ia_master_plan_specs (slug);

CREATE INDEX IF NOT EXISTS idx_ia_master_plan_specs_frozen_at
  ON ia_master_plan_specs (frozen_at);

COMMENT ON TABLE ia_master_plan_specs IS
  'Mig 0161: frozen spec snapshots per (slug, version). /spec-freeze MCP inserts row with frozen_at=NOW(). /ship-plan Phase A rejects if frozen_at IS NULL or open_questions_count > 0.';

COMMENT ON COLUMN ia_master_plan_specs.open_questions_count IS
  'Count of unresolved open questions parsed from Design Expansion section at freeze time. Freeze rejected when > 0.';

COMMIT;
