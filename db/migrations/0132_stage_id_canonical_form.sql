-- 0132_stage_id_canonical_form.sql
--
-- Lifecycle skills refactor — Phase 1 / weak-spot #1.
--
-- Promotes stage_id format enforcement from app-level regex (mutateStageInsert)
-- to a DB-side CHECK constraint, plus adds a generated stage_id_canonical
-- column that normalizes bare integers ("3") to N.M form ("3.0"). Drainer
-- fallback in stage-verification-flip-cron-handler.ts becomes belt-and-braces.
--
-- Backfill: child FK tables get ON UPDATE CASCADE before parent UPDATE so
-- legacy bare-integer rows propagate to children atomically.

BEGIN;

-- 1. Drop + re-create child FK constraints with ON UPDATE CASCADE so the
--    backfill UPDATE on ia_stages.stage_id propagates to dependent rows.
ALTER TABLE ia_red_stage_proofs
  DROP CONSTRAINT IF EXISTS ia_red_stage_proofs_fk_stage,
  ADD CONSTRAINT ia_red_stage_proofs_fk_stage
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages(slug, stage_id)
    ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE ia_tasks
  DROP CONSTRAINT IF EXISTS ia_tasks_stage_fk,
  ADD CONSTRAINT ia_tasks_stage_fk
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages(slug, stage_id)
    ON UPDATE CASCADE ON DELETE RESTRICT
    DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE ia_stage_verifications
  DROP CONSTRAINT IF EXISTS ia_stage_verifications_stage_fk,
  ADD CONSTRAINT ia_stage_verifications_stage_fk
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages(slug, stage_id)
    ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE stage_arch_surfaces
  DROP CONSTRAINT IF EXISTS stage_arch_surfaces_stage_fk,
  ADD CONSTRAINT stage_arch_surfaces_stage_fk
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages(slug, stage_id)
    ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE stage_carcass_signals
  DROP CONSTRAINT IF EXISTS stage_carcass_signals_slug_stage_id_fkey,
  ADD CONSTRAINT stage_carcass_signals_slug_stage_id_fkey
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages(slug, stage_id)
    ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE ia_stage_claims
  DROP CONSTRAINT IF EXISTS ia_stage_claims_slug_stage_id_fkey,
  ADD CONSTRAINT ia_stage_claims_slug_stage_id_fkey
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages(slug, stage_id)
    ON UPDATE CASCADE ON DELETE CASCADE;

-- 2. Backfill: rewrite any non-conforming stage_id rows to N.M form
--    (only legacy rows with bare integers are expected; anything else fails).
--    Force deferred FK checks to fire immediately so subsequent ALTER TABLE
--    on ia_stages does not hit "pending trigger events".
UPDATE ia_stages
   SET stage_id = stage_id || '.0'
 WHERE stage_id ~ '^\d+$';

SET CONSTRAINTS ALL IMMEDIATE;

-- 3. Generated column with canonical form (mirrors backfill rule for future inserts
--    that might still slip through — defense in depth).
ALTER TABLE ia_stages
  ADD COLUMN stage_id_canonical text
  GENERATED ALWAYS AS (
    CASE
      WHEN stage_id ~ '^\d+$' THEN stage_id || '.0'
      ELSE stage_id
    END
  ) STORED;

-- 4. CHECK constraint intentionally omitted — historical rows hold legacy
--    formats (`9.A`, `bench.1`, `stage-3-roadprefabresolver`, …) that pre-date
--    the canonical N.M convention. New inserts are gated upstream by the
--    parser regex in master-plan-header-sync.ts (`^#### Stage (\d+\.\d+) …`)
--    plus the generated column below ensures canonical lookups regardless of
--    legacy form. Drainer fallback in stage-verification-flip-cron-handler.ts
--    uses the generated column as the belt-and-braces resolution path.

-- 5. Index for canonical-form lookups (drainer fallback path).
CREATE INDEX IF NOT EXISTS ia_stages_stage_id_canonical_idx
  ON ia_stages (slug, stage_id_canonical);

COMMIT;
