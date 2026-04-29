-- 0052_drop_session_id_from_claims.sql
--
-- Parallel-carcass V2 simplification: drop holder-identity column from the
-- two-tier claim mutex. The section IS the holder. Multi-sequential agents on
-- the same task and concurrent agents on disjoint sections are both supported
-- via row-key-only addressing.
--
-- Refactor scope (parallel-carcass-exploration.md §6.4 V2 rewrite):
--   - ia_section_claims: drop session_id column + session index.
--   - ia_stage_claims:   drop session_id column + session index.
--   - ia_master_plan_health mat view: rebuild without session_id in
--     `sections_in_flight` (emit raw section_id strings).
--   - section_claim / stage_claim mutex enforced by UNIQUE(slug, section_id|stage_id)
--     on open rows (PRIMARY KEY already provides this).
--   - claim_heartbeat / *_release callable by row key alone.
--   - claims_sweep: time-based only — release rows where last_heartbeat is past
--     `carcass_config.claim_heartbeat_timeout_minutes`.
--   - Threat model: same-machine, same-user agents. No adversarial parties to
--     authenticate. Token-match buys complexity for zero gain.
--
-- Idempotent: DROP MATERIALIZED VIEW IF EXISTS, ALTER TABLE ... DROP COLUMN
-- IF EXISTS, DROP INDEX IF EXISTS. Re-run produces zero schema/row diff after
-- first apply (mat view body identical on second run).
--
-- Migration slot 0052 (0051 = pg_trgm_search). 0049 introduced session_id;
-- this migration walks it back. Mat view body lifted from 0050 with the
-- `format('%s@%s', section_id, session_id)` aggregator simplified to plain
-- `section_id` ordering.

BEGIN;

-- =========================================================================
-- 1. Drop mat view (depends on session_id via in_flight CTE)
-- =========================================================================

DROP MATERIALIZED VIEW IF EXISTS ia_master_plan_health;

-- =========================================================================
-- 2. ia_section_claims — drop session_id + session index
-- =========================================================================

DROP INDEX IF EXISTS ia_section_claims_session_idx;

ALTER TABLE ia_section_claims
  DROP COLUMN IF EXISTS session_id;

COMMENT ON TABLE ia_section_claims IS
  'Section-level claim mutex (D4, V2 row-only). One open row per (slug, section_id) — the section IS the holder. Active row = released_at IS NULL. Heartbeat refreshed via claim_heartbeat MCP. Stale rows swept via claims_sweep using carcass_config.claim_heartbeat_timeout_minutes. Mig 0052.';

-- =========================================================================
-- 3. ia_stage_claims — drop session_id + session index
-- =========================================================================

DROP INDEX IF EXISTS ia_stage_claims_session_idx;

ALTER TABLE ia_stage_claims
  DROP COLUMN IF EXISTS session_id;

COMMENT ON TABLE ia_stage_claims IS
  'Stage-level claim mutex (D4, V2 row-only). Asserts section claim row open for same (slug, section_id) before insert (enforced by stage_claim MCP, not DB FK). Heartbeat + sweep semantics match ia_section_claims. Mig 0052.';

-- =========================================================================
-- 4. Recreate mat view (sections_in_flight without session_id)
-- =========================================================================

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
  -- Active section claims: released_at IS NULL. V2 row-only — emit raw
  -- section_id strings (no `@session` suffix; the row IS the holder).
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
  'Rollup health metrics per master-plan slug. Powers master_plan_health + master_plan_cross_impact_scan MCP tools. Mig 0052 (V2 row-only): sections_in_flight emits raw section_id (no @session suffix). Refreshed sync via fn_ia_master_plan_health_refresh on ia_stages / ia_tasks / stage_arch_surfaces / ia_section_claims / stage_carcass_signals DML.';

CREATE UNIQUE INDEX ia_master_plan_health_slug_idx
  ON ia_master_plan_health (slug);

REFRESH MATERIALIZED VIEW ia_master_plan_health;

COMMIT;

-- =========================================================================
-- Rollback (manual, not auto-run):
--   Re-apply 0050 body (it carries the session_id-aware mat view + adds
--   columns back via separate ALTER TABLE statements). Then:
--   BEGIN;
--   ALTER TABLE ia_stage_claims   ADD COLUMN session_id text NOT NULL DEFAULT 'legacy-unknown';
--   ALTER TABLE ia_section_claims ADD COLUMN session_id text NOT NULL DEFAULT 'legacy-unknown';
--   CREATE INDEX ia_section_claims_session_idx ON ia_section_claims (session_id) WHERE released_at IS NULL;
--   CREATE INDEX ia_stage_claims_session_idx   ON ia_stage_claims   (session_id) WHERE released_at IS NULL;
--   COMMIT;
