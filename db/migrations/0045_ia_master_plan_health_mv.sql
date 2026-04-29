-- 0045_ia_master_plan_health_mv.sql
--
-- db-lifecycle-extensions Stage 2 / TECH-3226.
-- Materialized rollup of master-plan health for `master_plan_health` MCP tool
-- (TECH-3227) + `master_plan_cross_impact_scan` MCP tool (TECH-3229).
--
-- §Implementer Latitude (per task spec):
--   - Slot 0044 was claimed by 0044_ia_stages_depends_on (TECH-3225); locking
--     this MV on 0045.
--   - §Plan Digest cited `arch_surface_links` + `master_plan_extra` JSONB;
--     real schema (TECH-2960 / 0034_architecture_index + 0036) ships
--     `stage_arch_surfaces` (link table) + `arch_changelog` (drift events).
--     Adapted MV columns to use real surfaces:
--       missing_arch_surfaces text[]  → stages in slug with ZERO rows in
--                                       stage_arch_surfaces (proxy for
--                                       "stage declared no arch surfaces").
--       drift_events_open int          → arch_changelog rows joined via
--                                       stage_arch_surfaces, count of
--                                       kind='edit'|'spec_edit_commit'
--                                       where commit_sha IS NULL (open).
--       sibling_collisions text[]      → other slugs with status<>done plans
--                                       sharing surface_slug overlap on
--                                       stage_arch_surfaces.
--
-- Refresh strategy: sync trigger on stage_status changes + ia_tasks insert.
-- REFRESH MATERIALIZED VIEW CONCURRENTLY requires UNIQUE INDEX on slug.
--
-- Idempotent: IF NOT EXISTS / CREATE OR REPLACE / DROP TRIGGER IF EXISTS.

BEGIN;

-- 1. Materialized view ----------------------------------------------------

DROP MATERIALIZED VIEW IF EXISTS ia_master_plan_health;

CREATE MATERIALIZED VIEW ia_master_plan_health AS
WITH stage_rollup AS (
  SELECT s.slug,
         COUNT(*)::int AS n_stages,
         COUNT(*) FILTER (WHERE s.status = 'done')::int        AS n_done,
         COUNT(*) FILTER (WHERE s.status = 'in_progress')::int AS n_in_progress,
         COUNT(*) FILTER (WHERE s.status = 'pending')::int     AS n_pending,
         EXTRACT(DAY FROM (now() - MIN(s.updated_at) FILTER (WHERE s.status = 'in_progress')))::int
           AS oldest_in_progress_age_days
    FROM ia_stages s
   GROUP BY s.slug
),
missing_surfaces AS (
  -- Stages with zero stage_arch_surfaces rows (proxy for "no arch declared").
  SELECT s.slug,
         array_agg(s.stage_id ORDER BY s.stage_id) AS missing_arch_surfaces
    FROM ia_stages s
    LEFT JOIN stage_arch_surfaces sas
      ON sas.slug = s.slug AND sas.stage_id = s.stage_id
   WHERE sas.surface_slug IS NULL
   GROUP BY s.slug
),
open_drift AS (
  -- Open arch_changelog drift events touching this slug's surfaces.
  -- "Open" = commit_sha IS NULL (not yet committed).
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
  -- Other slugs sharing surface_slug with this slug, where the OTHER plan
  -- has at least one in_progress stage. Self-overlap excluded.
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
       now()                                               AS refreshed_at
  FROM ia_master_plans mp
  LEFT JOIN stage_rollup     sr ON sr.slug = mp.slug
  LEFT JOIN missing_surfaces ms ON ms.slug = mp.slug
  LEFT JOIN open_drift       od ON od.slug = mp.slug
  LEFT JOIN sibling_overlap  so ON so.slug = mp.slug;

COMMENT ON MATERIALIZED VIEW ia_master_plan_health IS
  'Rollup health metrics per master-plan slug. Powers master_plan_health + master_plan_cross_impact_scan MCP tools (db-lifecycle-extensions Stage 2 / TECH-3226). Refreshed sync via fn_ia_master_plan_health_refresh trigger on ia_stages / ia_tasks DML.';

-- Required for REFRESH MATERIALIZED VIEW CONCURRENTLY.
CREATE UNIQUE INDEX ia_master_plan_health_slug_idx
  ON ia_master_plan_health (slug);

-- Initial populate (CONCURRENTLY needs a non-empty view, so run plain refresh first).
REFRESH MATERIALIZED VIEW ia_master_plan_health;

-- 2. Refresh trigger function ---------------------------------------------

CREATE OR REPLACE FUNCTION fn_ia_master_plan_health_refresh()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  -- CONCURRENTLY: non-blocking; allows reads during refresh. Slug-keyed
  -- UNIQUE INDEX above is the precondition.
  REFRESH MATERIALIZED VIEW CONCURRENTLY ia_master_plan_health;
  RETURN NULL;
END
$$;

COMMENT ON FUNCTION fn_ia_master_plan_health_refresh() IS
  'Refresh ia_master_plan_health MV CONCURRENTLY. Fired post-statement on ia_stages / ia_tasks DML (db-lifecycle-extensions Stage 2 / TECH-3226).';

-- 3. Triggers -------------------------------------------------------------
-- AFTER STATEMENT: one refresh per multi-row DML batch (vs per-row spam).

DROP TRIGGER IF EXISTS ia_stages_health_refresh ON ia_stages;
CREATE TRIGGER ia_stages_health_refresh
AFTER INSERT OR UPDATE OR DELETE ON ia_stages
FOR EACH STATEMENT
EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

DROP TRIGGER IF EXISTS ia_tasks_health_refresh ON ia_tasks;
CREATE TRIGGER ia_tasks_health_refresh
AFTER INSERT OR UPDATE OR DELETE ON ia_tasks
FOR EACH STATEMENT
EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

DROP TRIGGER IF EXISTS stage_arch_surfaces_health_refresh ON stage_arch_surfaces;
CREATE TRIGGER stage_arch_surfaces_health_refresh
AFTER INSERT OR UPDATE OR DELETE ON stage_arch_surfaces
FOR EACH STATEMENT
EXECUTE FUNCTION fn_ia_master_plan_health_refresh();

COMMIT;

-- Rollback (manual, not auto-run):
--   BEGIN;
--   DROP TRIGGER IF EXISTS stage_arch_surfaces_health_refresh ON stage_arch_surfaces;
--   DROP TRIGGER IF EXISTS ia_tasks_health_refresh ON ia_tasks;
--   DROP TRIGGER IF EXISTS ia_stages_health_refresh ON ia_stages;
--   DROP FUNCTION IF EXISTS fn_ia_master_plan_health_refresh();
--   DROP MATERIALIZED VIEW IF EXISTS ia_master_plan_health;
--   COMMIT;
