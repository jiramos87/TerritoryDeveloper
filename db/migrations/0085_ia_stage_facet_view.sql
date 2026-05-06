-- TECH-15907: Faceted index materialized view for next-stage resolver
-- Replaces N+1 dep checks with single MV query over stage × task × deps.

CREATE MATERIALIZED VIEW IF NOT EXISTS ia_stage_facet_view AS
SELECT
  t.task_id,
  t.slug,
  t.stage_id,
  t.status,
  t.title,
  t.priority,
  t.updated_at,
  -- Dependency count (in-degree for Kahn's algorithm)
  COALESCE(dep_counts.dep_count, 0)::INT AS dep_count,
  -- Count of unresolved deps (not done/archived)
  COALESCE(dep_counts.unresolved_dep_count, 0)::INT AS unresolved_dep_count,
  -- Is parallel-ready: pending status + all deps resolved
  (t.status = 'pending'
   AND COALESCE(dep_counts.unresolved_dep_count, 0) = 0)::BOOLEAN AS parallel_ready,
  s.status AS stage_status,
  s.title  AS stage_title
FROM ia_tasks t
LEFT JOIN ia_stages s
  ON s.slug = t.slug AND s.stage_id = t.stage_id
LEFT JOIN (
  SELECT
    d.task_id,
    COUNT(*)::INT AS dep_count,
    COUNT(*) FILTER (
      WHERE dep_t.status NOT IN ('done', 'archived')
    )::INT AS unresolved_dep_count
  FROM ia_task_deps d
  JOIN ia_tasks dep_t ON dep_t.task_id = d.depends_on_task_id
  WHERE d.kind = 'depends_on'
  GROUP BY d.task_id
) dep_counts ON dep_counts.task_id = t.task_id
WITH DATA;

-- Index for next-stage resolver: (slug, stage_id, parallel_ready)
CREATE UNIQUE INDEX IF NOT EXISTS ia_stage_facet_view_task_id_idx
  ON ia_stage_facet_view (task_id);

CREATE INDEX IF NOT EXISTS ia_stage_facet_view_slug_stage_ready_idx
  ON ia_stage_facet_view (slug, stage_id, parallel_ready)
  WHERE parallel_ready = TRUE;

CREATE INDEX IF NOT EXISTS ia_stage_facet_view_slug_status_idx
  ON ia_stage_facet_view (slug, status);

COMMENT ON MATERIALIZED VIEW ia_stage_facet_view IS
  'Faceted index over ia_tasks × ia_stages × ia_task_deps. '
  'Precomputes dep_count, unresolved_dep_count, parallel_ready for Kahn''s algorithm. '
  'Refreshed by task_status_flip after each status transition. '
  'TECH-15907.';

-- Rollback:
--   DROP MATERIALIZED VIEW IF EXISTS ia_stage_facet_view CASCADE;
