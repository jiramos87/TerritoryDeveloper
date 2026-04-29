-- 0049_parallel_carcass_primitives.sql
--
-- Parallel-carcass master-plan primitives (docs/parallel-carcass-exploration.md
-- §6.1; Wave 0 Phase 1).
--
-- Three moves:
--   1. Architecture-first lock — `arch_decisions.plan_slug` + lock seal columns
--      on `ia_master_plans` + BEFORE UPDATE guard trigger that freezes locked
--      plan-scoped decisions (single legal exit: status='superseded').
--   2. Carcass milestone — `ia_stages.carcass_role` enum column +
--      `carcass_signal_kinds` table (extensible enum) + `carcass_config` table
--      (cardinality cap + thresholds) + `stage_carcass_signals` link table +
--      DEFERRED constraint trigger enforcing cardinality cap +
--      sections-imply-carcass invariant.
--   3. Sections — `ia_stages.section_id` text column + two-tier claim mutex
--      tables `ia_section_claims` + `ia_stage_claims` with heartbeat columns.
--
-- Rationale (decision log §5):
--   D2 — arch_decisions.plan_slug column (NULL = global decision).
--   D3 — carcass_role enum on ia_stages.
--   D4 — two-tier claim (section + stage).
--   D14 — architecture_locked_at + locked_commit_sha lock seal columns.
--   D15 — carcass cardinality cap (config row, default 3).
--   D17 — trigger blocks in-place UPDATE on locked plan-scoped arch_decisions.
--   D18 — sections-imply-carcass DB CHECK (deferred constraint trigger).
--   D19 — explicit ia_stages.section_id (NULL for carcass + legacy).
--
-- Idempotent: ALTER TABLE ... ADD COLUMN IF NOT EXISTS pattern, CREATE TABLE
-- IF NOT EXISTS, INSERT ... ON CONFLICT DO NOTHING, DROP TRIGGER IF EXISTS +
-- CREATE TRIGGER. Re-run produces zero schema/row diff.
--
-- Migration slot 0049 (0048 = skill-changelog-validator). Plan-health MV
-- extension lives in 0050.

BEGIN;

-- =========================================================================
-- Move 1 — arch_decisions plan-scope + lock seal
-- =========================================================================

-- arch_decisions.plan_slug: NULL = global decision (DEC-A* rows).
--                           non-NULL = plan-scoped lock (e.g. plan-foo-boundaries).
ALTER TABLE arch_decisions
  ADD COLUMN IF NOT EXISTS plan_slug text;

CREATE INDEX IF NOT EXISTS arch_decisions_plan_slug_idx
  ON arch_decisions (plan_slug)
  WHERE plan_slug IS NOT NULL;

-- ia_master_plans lock seal columns. Set by master_plan_lock_arch MCP at end
-- of authoring Phase A; arms the locked_guard trigger below.
ALTER TABLE ia_master_plans
  ADD COLUMN IF NOT EXISTS architecture_locked_at timestamptz,
  ADD COLUMN IF NOT EXISTS locked_commit_sha      text;

-- Guard trigger (D17): block in-place UPDATE on locked plan-scoped
-- arch_decisions rows. Single legal exit — status='superseded' flip with
-- title + rationale + plan_slug + surface_id unchanged. INSERT untouched
-- (supersession path is "flip old row + insert new row").
CREATE OR REPLACE FUNCTION arch_decisions_locked_guard()
RETURNS TRIGGER AS $$
DECLARE
  v_locked timestamptz;
BEGIN
  IF NEW.plan_slug IS NULL THEN
    RETURN NEW;
  END IF;

  SELECT architecture_locked_at INTO v_locked
    FROM ia_master_plans
   WHERE slug = NEW.plan_slug;

  IF v_locked IS NULL THEN
    RETURN NEW;
  END IF;

  IF NEW.status = 'superseded'
     AND OLD.status = 'active'
     AND NEW.title     IS NOT DISTINCT FROM OLD.title
     AND NEW.rationale IS NOT DISTINCT FROM OLD.rationale
     AND NEW.plan_slug IS NOT DISTINCT FROM OLD.plan_slug
     AND NEW.surface_id IS NOT DISTINCT FROM OLD.surface_id THEN
    RETURN NEW;
  END IF;

  RAISE EXCEPTION
    'arch_decisions row % is locked (plan % locked at %); only status flip to superseded with title+rationale unchanged is allowed',
    NEW.slug, NEW.plan_slug, v_locked
    USING ERRCODE = 'P0001';
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS arch_decisions_locked_guard_t ON arch_decisions;
CREATE TRIGGER arch_decisions_locked_guard_t
  BEFORE UPDATE ON arch_decisions
  FOR EACH ROW EXECUTE FUNCTION arch_decisions_locked_guard();

COMMENT ON FUNCTION arch_decisions_locked_guard() IS
  'D17: block in-place UPDATE on locked plan-scoped arch_decisions rows. Single legal exit: status=superseded with title+rationale+plan_slug+surface_id unchanged. Mig 0049.';

-- =========================================================================
-- Move 2 — carcass role + signal kinds + cardinality cap
-- =========================================================================

ALTER TABLE ia_stages
  ADD COLUMN IF NOT EXISTS carcass_role text,
  ADD COLUMN IF NOT EXISTS section_id   text;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
     WHERE conname = 'ia_stages_carcass_role_check'
  ) THEN
    ALTER TABLE ia_stages
      ADD CONSTRAINT ia_stages_carcass_role_check
        CHECK (carcass_role IN ('carcass','section') OR carcass_role IS NULL);
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS ia_stages_carcass_role_idx
  ON ia_stages (slug, carcass_role)
  WHERE carcass_role IS NOT NULL;

CREATE INDEX IF NOT EXISTS ia_stages_section_id_idx
  ON ia_stages (slug, section_id)
  WHERE section_id IS NOT NULL;

-- Extensible enum of "noticeable" carcass signals. Authors link each carcass
-- stage to ≥1 signal kind via stage_carcass_signals.
CREATE TABLE IF NOT EXISTS carcass_signal_kinds (
  slug         text PRIMARY KEY,
  label        text NOT NULL,
  verify_hint  text,
  created_at   timestamptz NOT NULL DEFAULT now()
);

INSERT INTO carcass_signal_kinds (slug, label, verify_hint) VALUES
  ('visible_ui',          'Visible UI/UX change',           'open game/web; eyeball'),
  ('dev_loop_affordance', 'New dev-loop affordance',        'invoke new command/MCP'),
  ('agent_capability',    'New agent capability via bridge','call new MCP tool'),
  ('runnable_prototype',  'Running prototype humans poke',  'launch standalone artifact')
ON CONFLICT (slug) DO NOTHING;

-- Config rows: cardinality cap + thresholds + heartbeat timeout. Extensible
-- without schema change (D15).
CREATE TABLE IF NOT EXISTS carcass_config (
  key    text PRIMARY KEY,
  value  text NOT NULL
);

INSERT INTO carcass_config (key, value) VALUES
  ('max_carcass_stages_per_plan',     '3'),
  ('section_count_warn_threshold',    '6'),
  ('claim_heartbeat_timeout_minutes', '10')
ON CONFLICT (key) DO NOTHING;

-- Link table: stage → signal kinds (≥1 required for carcass stages, enforced
-- by Phase B authoring; not at DB level since the FK only fires after
-- INSERT).
CREATE TABLE IF NOT EXISTS stage_carcass_signals (
  slug          text NOT NULL,
  stage_id      text NOT NULL,
  signal_kind   text NOT NULL REFERENCES carcass_signal_kinds (slug),
  PRIMARY KEY (slug, stage_id, signal_kind),
  FOREIGN KEY (slug, stage_id) REFERENCES ia_stages (slug, stage_id)
    ON DELETE CASCADE
);

-- Cardinality + sections-imply-carcass invariants (D15 + D18). Deferred
-- constraint trigger so multi-row authoring transactions can violate
-- mid-flight; check at COMMIT.
CREATE OR REPLACE FUNCTION ia_stages_carcass_invariants()
RETURNS TRIGGER AS $$
DECLARE
  v_slug          text;
  v_carcass_count int;
  v_section_count int;
  v_cap           int;
BEGIN
  v_slug := COALESCE(NEW.slug, OLD.slug);

  SELECT value::int INTO v_cap
    FROM carcass_config
   WHERE key = 'max_carcass_stages_per_plan';

  SELECT
    COUNT(*) FILTER (WHERE carcass_role = 'carcass'),
    COUNT(*) FILTER (WHERE carcass_role = 'section')
    INTO v_carcass_count, v_section_count
    FROM ia_stages
   WHERE slug = v_slug;

  IF v_carcass_count > v_cap THEN
    RAISE EXCEPTION
      'plan % has % carcass stages; cap is %',
      v_slug, v_carcass_count, v_cap
      USING ERRCODE = 'P0001';
  END IF;

  IF v_section_count > 0 AND v_carcass_count = 0 THEN
    RAISE EXCEPTION
      'plan % has section stages but zero carcass stages',
      v_slug
      USING ERRCODE = 'P0001';
  END IF;

  RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS ia_stages_carcass_invariants_t ON ia_stages;
CREATE CONSTRAINT TRIGGER ia_stages_carcass_invariants_t
  AFTER INSERT OR UPDATE OR DELETE ON ia_stages
  DEFERRABLE INITIALLY DEFERRED
  FOR EACH ROW EXECUTE FUNCTION ia_stages_carcass_invariants();

COMMENT ON FUNCTION ia_stages_carcass_invariants() IS
  'D15 + D18: enforce carcass cardinality cap + sections-imply-carcass invariant. Deferred constraint trigger; checked at COMMIT. Mig 0049.';

-- =========================================================================
-- Move 3 — two-tier claim mutex
-- =========================================================================

CREATE TABLE IF NOT EXISTS ia_section_claims (
  slug            text NOT NULL,
  section_id      text NOT NULL,
  session_id      text NOT NULL,
  claimed_at      timestamptz NOT NULL DEFAULT now(),
  last_heartbeat  timestamptz NOT NULL DEFAULT now(),
  released_at     timestamptz,
  PRIMARY KEY (slug, section_id),
  FOREIGN KEY (slug) REFERENCES ia_master_plans (slug) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ia_section_claims_session_idx
  ON ia_section_claims (session_id)
  WHERE released_at IS NULL;

CREATE TABLE IF NOT EXISTS ia_stage_claims (
  slug            text NOT NULL,
  stage_id        text NOT NULL,
  session_id      text NOT NULL,
  claimed_at      timestamptz NOT NULL DEFAULT now(),
  last_heartbeat  timestamptz NOT NULL DEFAULT now(),
  released_at     timestamptz,
  PRIMARY KEY (slug, stage_id),
  FOREIGN KEY (slug, stage_id) REFERENCES ia_stages (slug, stage_id)
    ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ia_stage_claims_session_idx
  ON ia_stage_claims (session_id)
  WHERE released_at IS NULL;

COMMENT ON TABLE ia_section_claims IS
  'Section-level claim mutex (D4). One open row per (slug, section_id). Active row = released_at IS NULL. Heartbeat refreshed via claim_heartbeat MCP. Stale rows swept via claims_sweep using carcass_config.claim_heartbeat_timeout_minutes. Mig 0049.';

COMMENT ON TABLE ia_stage_claims IS
  'Stage-level claim mutex (D4). Asserts section claim by same session_id before insert (enforced by stage_claim MCP, not DB FK). Heartbeat + sweep semantics match ia_section_claims. Mig 0049.';

COMMIT;

-- =========================================================================
-- Rollback (manual, not auto-run):
--   BEGIN;
--   DROP TABLE IF EXISTS ia_stage_claims;
--   DROP TABLE IF EXISTS ia_section_claims;
--   DROP TRIGGER IF EXISTS ia_stages_carcass_invariants_t ON ia_stages;
--   DROP FUNCTION IF EXISTS ia_stages_carcass_invariants();
--   DROP TABLE IF EXISTS stage_carcass_signals;
--   DROP TABLE IF EXISTS carcass_config;
--   DROP TABLE IF EXISTS carcass_signal_kinds;
--   DROP INDEX IF EXISTS ia_stages_section_id_idx;
--   DROP INDEX IF EXISTS ia_stages_carcass_role_idx;
--   ALTER TABLE ia_stages
--     DROP CONSTRAINT IF EXISTS ia_stages_carcass_role_check,
--     DROP COLUMN IF EXISTS section_id,
--     DROP COLUMN IF EXISTS carcass_role;
--   DROP TRIGGER IF EXISTS arch_decisions_locked_guard_t ON arch_decisions;
--   DROP FUNCTION IF EXISTS arch_decisions_locked_guard();
--   ALTER TABLE ia_master_plans
--     DROP COLUMN IF EXISTS locked_commit_sha,
--     DROP COLUMN IF EXISTS architecture_locked_at;
--   DROP INDEX IF EXISTS arch_decisions_plan_slug_idx;
--   ALTER TABLE arch_decisions DROP COLUMN IF EXISTS plan_slug;
--   COMMIT;
