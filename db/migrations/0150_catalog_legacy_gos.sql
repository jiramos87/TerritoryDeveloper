-- 0150_catalog_legacy_gos.sql
-- Layer 3 legacy-GO purge planner (TECH-28369):
--   catalog_legacy_gos: declares (scene_name, hierarchy_path, retired_by_panel, retire_after_stage).
--   Drift detector treats legacy-GO-still-active as ERROR (not WARN) once
--   retire_after_stage has closed. Forces v3 repair extension to actually delete
--   SubtypePickerRoot / GrowthBudgetPanelRoot / city-stats-handoff.

BEGIN;

CREATE TABLE IF NOT EXISTS catalog_legacy_gos (
  scene_name        text NOT NULL,
  hierarchy_path    text NOT NULL,
  retired_by_panel  text NOT NULL,
  retire_after_stage text NOT NULL,
  PRIMARY KEY (scene_name, hierarchy_path)
);

COMMENT ON TABLE catalog_legacy_gos IS
  'Layer 3 legacy-GO purge planner (TECH-28369) — declares legacy GOs that must be '
  'deleted once retire_after_stage closes. Drift detector escalates to ERROR severity '
  'when the stage is closed but the GO is still in the scene. '
  'Forces cityscene v3 repair to delete SubtypePickerRoot / GrowthBudgetPanelRoot / '
  'city-stats-handoff before merge.';

-- Seed known legacy GOs from cityscene that must be purged in v3 repair.
INSERT INTO catalog_legacy_gos (scene_name, hierarchy_path, retired_by_panel, retire_after_stage)
VALUES
  ('MainScene', '/Canvas/SubtypePickerRoot',    'budget-panel',      'stage-11.0'),
  ('MainScene', '/Canvas/GrowthBudgetPanelRoot', 'budget-panel',     'stage-11.0'),
  ('MainScene', '/Canvas/city-stats-handoff',   'city-stats-panel',  'stage-11.0')
ON CONFLICT DO NOTHING;

DO $$
BEGIN
  RAISE NOTICE '0150 OK: catalog_legacy_gos created + seeded (Layer 3 legacy-GO purge planner)';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM catalog_legacy_gos WHERE scene_name = 'MainScene';
--   DROP TABLE IF EXISTS catalog_legacy_gos;
