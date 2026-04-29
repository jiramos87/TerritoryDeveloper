-- 0050_master_plan_health_carcass_extension.sql
--
-- Parallel-carcass master-plan primitives (docs/parallel-carcass-exploration.md
-- §6.1; Wave 0 Phase 1 — companion to 0049).
--
-- Extends `ia_master_plan_health` MV with carcass + section rollup columns:
--   carcass_done                bool   — ≥1 carcass stage AND every carcass stage status='done'
--                                        (NULL = no carcass stages declared)
--   n_carcass                   int    — count of carcass-role stages
--   n_carcass_done              int    — count of carcass-role stages with status='done'
--   n_sections                  int    — DISTINCT non-NULL section_id count
--   n_sections_done             int    — sections where ALL section-role stages status='done'
--   sections_in_flight          text[] — active section claims `{section_id}@{session_id}`
--                                        (released_at IS NULL)
--   carcass_cardinality_breach  bool   — n_carcass > carcass_config.max_carcass_stages_per_plan
--
-- Refresh strategy: piggy-back on existing fn_ia_master_plan_health_refresh()
-- (mig 0045) — function name unchanged, MV name unchanged, only column shape
-- extended. Add AFTER STATEMENT triggers on ia_section_claims (refresh on
-- INSERT + UPDATE OF released_at only — skip heartbeat chatter) +
-- stage_carcass_signals (any DML).
--
-- Idempotent: DROP MV IF EXISTS + CREATE; DROP TRIGGER IF EXISTS + CREATE.
-- Re-run produces zero schema diff.

BEGIN;

-- =========================================================================
-- 1. Replace MV with carcass-aware shape
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
  -- One row per (slug, section_id). all_done = every stage in that section
  -- has status='done'.
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
  -- Active section claims: released_at IS NULL.
  SELECT slug,
         array_agg(format('%s@%s', section_id, session_id)
                   ORDER BY section_id, session_id) AS sections_in_flight
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
       now()                                               AS refreshed_at
  FROM ia_master_plans mp
  LEFT JOIN stage_rollup     sr  ON sr.slug  = mp.slug
  LEFT JOIN carcass_rollup   cr  ON cr.slug  = mp.slug
  LEFT JOIN section_agg      sa  ON sa.slug  = mp.slug
  LEFT JOIN in_flight        ifl ON ifl.slug = mp.slug
  LEFT JOIN missing_surfaces ms  ON ms.slug  = mp.slug
  LEFT JOIN open_drift       od  ON od.slug  = mp.slug
  LEFT JOIN sibling_overlap  so  ON so.slug  = mp.slug;

COMMENT ON MATERIALIZED VIEW ia_master_plan_health IS
  'Rollup health metrics per master-plan slug. Powers master_plan_health + master_plan_cross_impact_scan MCP tools. Mig 0050 extended with carcass + section + claim columns (parallel-carcass exploration §6.1). Refreshed sync via fn_ia_master_plan_health_refresh on ia_stages / ia_tasks / stage_arch_surfaces / ia_section_claims / stage_carcass_signals DML.';

CREATE UNIQUE INDEX ia_master_plan_health_slug_idx
  ON ia_master_plan_health (slug);

REFRESH MATERIALIZED VIEW ia_master_plan_health;

-- =========================================================================
-- 2. Refresh triggers for new tables (function fn_ia_master_plan_health_refresh
--    already created in mig 0045 — reuse it).
-- =========================================================================

-- ia_section_claims: refresh only on INSERT + on UPDATE OF released_at.
-- Heartbeat updates (last_heartbeat) skipped to avoid MV churn during long
-- holds. Sweep / release flips released_at, which IS observed.
DROP TRIGGER IF EXISTS ia_section_claims_health_refresh_ins ON ia_section_claims;
CREATE TRIGGER ia_section_claims_health_refresh_ins
AFTER INSERT ON ia_section_claims
FOR EACH STATEMENT
EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

DROP TRIGGER IF EXISTS ia_section_claims_health_refresh_upd ON ia_section_claims;
CREATE TRIGGER ia_section_claims_health_refresh_upd
AFTER UPDATE OF released_at ON ia_section_claims
FOR EACH STATEMENT
EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

DROP TRIGGER IF EXISTS ia_section_claims_health_refresh_del ON ia_section_claims;
CREATE TRIGGER ia_section_claims_health_refresh_del
AFTER DELETE ON ia_section_claims
FOR EACH STATEMENT
EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

-- stage_carcass_signals: any DML changes the signal-link surface, which the
-- MV does NOT currently project, but which can affect future shape (e.g.
-- "carcass stages without ≥1 signal kind" derived col). Wire trigger now to
-- avoid retro-fit when projection lands.
DROP TRIGGER IF EXISTS stage_carcass_signals_health_refresh ON stage_carcass_signals;
CREATE TRIGGER stage_carcass_signals_health_refresh
AFTER INSERT OR UPDATE OR DELETE ON stage_carcass_signals
FOR EACH STATEMENT
EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

COMMIT;

-- =========================================================================
-- Rollback (manual, not auto-run):
--   BEGIN;
--   DROP TRIGGER IF EXISTS stage_carcass_signals_health_refresh ON stage_carcass_signals;
--   DROP TRIGGER IF EXISTS ia_section_claims_health_refresh_del ON ia_section_claims;
--   DROP TRIGGER IF EXISTS ia_section_claims_health_refresh_upd ON ia_section_claims;
--   DROP TRIGGER IF EXISTS ia_section_claims_health_refresh_ins ON ia_section_claims;
--   DROP MATERIALIZED VIEW IF EXISTS ia_master_plan_health;
--   -- Then re-run mig 0045 to restore prior MV shape (or restore via 0045 SQL inline).
--   COMMIT;
