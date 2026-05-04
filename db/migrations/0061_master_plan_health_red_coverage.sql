-- 0061_master_plan_health_red_coverage.sql
--
-- tdd-red-green-methodology Stage 5 / TECH-10905.
-- Extends `ia_master_plan_health` MV with red-stage coverage column:
--   red_stage_coverage  NUMERIC  — (failed_as_expected + not_applicable) /
--                                   total_stages × 100, per plan.
--                                   NULL when plan has zero §Red-Stage Proof rows
--                                   across all stages (grandfathered back-compat)
--                                   OR when every stage's latest proof carries
--                                   target_kind='design_only' AND
--                                   proof_status='not_applicable'.
--
-- Coverage formula: latest proof_status per (slug, stage_id) via DISTINCT ON,
-- then sum (failed_as_expected + not_applicable) / total_stages × 100.
--
-- Refresh strategy: piggy-back on existing fn_ia_master_plan_health_refresh()
-- (mig 0045). New trigger on ia_red_stage_proofs so capture/finalize rows
-- also refresh the MV.
--
-- MV rebuild strategy: DROP + CREATE (ALTER MV ADD COLUMN not supported for
-- SELECT-form MVs in Postgres). Matches mig 0050 and 0055 convention.
--
-- Idempotent: DROP MV IF EXISTS + CREATE; CREATE TRIGGER IF NOT EXISTS analog
-- via DROP TRIGGER IF EXISTS + CREATE TRIGGER.
-- Re-run produces zero schema diff.

BEGIN;

-- =========================================================================
-- 1. Replace MV with red-coverage-aware shape
-- =========================================================================

DROP MATERIALIZED VIEW IF EXISTS ia_master_plan_health;

CREATE MATERIALIZED VIEW ia_master_plan_health AS
WITH stage_rollup AS (
  SELECT s.slug,
         COUNT(*)::int                                                AS n_stages,
         COUNT(*) FILTER (WHERE s.status = 'done')::int               AS n_done,
         COUNT(*) FILTER (WHERE s.status = 'in_progress')::int        AS n_in_progress,
         COUNT(*) FILTER (WHERE s.status = 'pending')::int            AS n_pending,
         EXTRACT(DAY FROM (now() - MIN(s.updated_at) FILTER (WHERE s.status = 'in_progress')))::int
           AS oldest_in_progress_age_days
    FROM ia_stages s
   GROUP BY s.slug
),
carcass_rollup AS (
  SELECT s.slug,
         COUNT(*) FILTER (WHERE s.carcass_role = 'carcass')::int                              AS n_carcass,
         COUNT(*) FILTER (WHERE s.carcass_role = 'carcass' AND s.status = 'done')::int        AS n_carcass_done
    FROM ia_stages s
   GROUP BY s.slug
),
section_rollup AS (
  SELECT slug,
         section_id,
         bool_and(status = 'done') AS all_done
    FROM ia_stages
   WHERE section_id IS NOT NULL
   GROUP BY slug, section_id
),
section_agg AS (
  SELECT slug,
         COUNT(*)::int                                  AS n_sections,
         COUNT(*) FILTER (WHERE all_done)::int          AS n_sections_done
    FROM section_rollup
   GROUP BY slug
),
in_flight AS (
  SELECT slug,
         array_agg(section_id ORDER BY section_id) AS sections_in_flight
    FROM ia_section_claims
   WHERE released_at IS NULL
   GROUP BY slug
),
missing_surfaces AS (
  SELECT s.slug,
         array_agg(s.stage_id ORDER BY s.stage_id) AS missing_arch_surfaces
    FROM ia_stages s
    LEFT JOIN stage_arch_surfaces sas
      ON sas.slug = s.slug AND sas.stage_id = s.stage_id
   WHERE sas.surface_slug IS NULL
   GROUP BY s.slug
),
open_drift AS (
  SELECT sas.slug,
         COUNT(DISTINCT ac.id)::int AS drift_events_open
    FROM arch_changelog ac
    JOIN stage_arch_surfaces sas
      ON sas.surface_slug = ac.surface_slug
   WHERE ac.commit_sha IS NULL
     AND ac.kind IN ('edit', 'spec_edit_commit')
   GROUP BY sas.slug
),
sibling_overlap AS (
  SELECT a.slug,
         array_agg(DISTINCT b.slug ORDER BY b.slug) AS sibling_collisions
    FROM stage_arch_surfaces a
    JOIN stage_arch_surfaces b
      ON a.surface_slug = b.surface_slug
     AND a.slug <> b.slug
    JOIN ia_stages bs
      ON bs.slug = b.slug AND bs.stage_id = b.stage_id
   WHERE bs.status = 'in_progress'
   GROUP BY a.slug
),
carcass_cap AS (
  SELECT value::int AS cap FROM carcass_config WHERE key = 'max_carcass_stages_per_plan'
),
-- Red-stage coverage: latest proof_status per (slug, stage_id)
latest_proofs AS (
  SELECT DISTINCT ON (slug, stage_id)
         slug, stage_id, proof_status, target_kind
    FROM ia_red_stage_proofs
   ORDER BY slug, stage_id, captured_at DESC
),
red_coverage AS (
  SELECT s.slug,
         COUNT(*)::numeric                                                                   AS total_stages,
         COUNT(*) FILTER (WHERE lp.proof_status IN ('failed_as_expected', 'not_applicable'))::numeric
                                                                                            AS covered_stages,
         -- all-design-only carve-out: every stage's latest proof is design_only/not_applicable
         bool_and(lp.target_kind = 'design_only' AND lp.proof_status = 'not_applicable')   AS all_design_only,
         COUNT(lp.slug)                                                                     AS proof_row_count
    FROM ia_stages s
    LEFT JOIN latest_proofs lp
      ON lp.slug = s.slug AND lp.stage_id = s.stage_id
   GROUP BY s.slug
)
SELECT mp.slug,
       COALESCE(sr.n_stages, 0)                            AS n_stages,
       COALESCE(sr.n_done, 0)                              AS n_done,
       COALESCE(sr.n_in_progress, 0)                       AS n_in_progress,
       COALESCE(sr.n_pending, 0)                           AS n_pending,
       sr.oldest_in_progress_age_days,
       COALESCE(ms.missing_arch_surfaces, '{}'::text[])    AS missing_arch_surfaces,
       COALESCE(od.drift_events_open, 0)                   AS drift_events_open,
       COALESCE(so.sibling_collisions, '{}'::text[])       AS sibling_collisions,
       COALESCE(cr.n_carcass, 0)                           AS n_carcass,
       COALESCE(cr.n_carcass_done, 0)                      AS n_carcass_done,
       CASE
         WHEN COALESCE(cr.n_carcass, 0) = 0 THEN NULL
         ELSE cr.n_carcass = cr.n_carcass_done
       END                                                 AS carcass_done,
       COALESCE(sa.n_sections, 0)                          AS n_sections,
       COALESCE(sa.n_sections_done, 0)                     AS n_sections_done,
       COALESCE(ifl.sections_in_flight, '{}'::text[])      AS sections_in_flight,
       (COALESCE(cr.n_carcass, 0) > (SELECT cap FROM carcass_cap))
                                                           AS carcass_cardinality_breach,
       (SELECT p95_ms FROM ia_arch_drift_bench ORDER BY measured_at DESC LIMIT 1)
                                                           AS arch_drift_scan_p95_ms,
       -- red_stage_coverage: NULL when zero proof rows OR all-design-only plan
       CASE
         WHEN COALESCE(rc.proof_row_count, 0) = 0 THEN NULL
         WHEN rc.all_design_only IS TRUE THEN NULL
         ELSE ROUND((rc.covered_stages / NULLIF(rc.total_stages, 0)) * 100, 2)
       END                                                 AS red_stage_coverage,
       now()                                               AS refreshed_at
  FROM ia_master_plans mp
  LEFT JOIN stage_rollup     sr  ON sr.slug  = mp.slug
  LEFT JOIN carcass_rollup   cr  ON cr.slug  = mp.slug
  LEFT JOIN section_agg      sa  ON sa.slug  = mp.slug
  LEFT JOIN in_flight        ifl ON ifl.slug = mp.slug
  LEFT JOIN missing_surfaces ms  ON ms.slug  = mp.slug
  LEFT JOIN open_drift       od  ON od.slug  = mp.slug
  LEFT JOIN sibling_overlap  so  ON so.slug  = mp.slug
  LEFT JOIN red_coverage     rc  ON rc.slug  = mp.slug;

COMMENT ON MATERIALIZED VIEW ia_master_plan_health IS
  'Rollup health metrics per master-plan slug. Powers master_plan_health + master_plan_cross_impact_scan MCP tools. '
  'Mig 0055 extended with arch_drift_scan_p95_ms (parallel-carcass Stage 2.1). '
  'Mig 0061 extended with red_stage_coverage NUMERIC — (failed_as_expected + not_applicable) / total_stages × 100 '
  '(tdd-red-green-methodology Stage 5 / TECH-10905). NULL for grandfathered plans (zero proof rows) or all-design-only.';

CREATE UNIQUE INDEX ia_master_plan_health_slug_idx
  ON ia_master_plan_health (slug);

REFRESH MATERIALIZED VIEW ia_master_plan_health;

-- =========================================================================
-- 2. New trigger: refresh MV on ia_red_stage_proofs mutations
-- =========================================================================

DROP TRIGGER IF EXISTS ia_red_stage_proofs_health_refresh ON ia_red_stage_proofs;

CREATE TRIGGER ia_red_stage_proofs_health_refresh
  AFTER INSERT OR UPDATE OR DELETE ON ia_red_stage_proofs
  FOR EACH STATEMENT
  EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

COMMIT;

-- =========================================================================
-- Rollback (manual, not auto-run):
--   BEGIN;
--   DROP TRIGGER IF EXISTS ia_red_stage_proofs_health_refresh ON ia_red_stage_proofs;
--   DROP MATERIALIZED VIEW IF EXISTS ia_master_plan_health;
--   -- Then re-run mig 0055 to restore prior MV shape.
--   COMMIT;
-- =========================================================================
