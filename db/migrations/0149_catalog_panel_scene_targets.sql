-- 0149_catalog_panel_scene_targets.sql
-- Layer 3 scene-wire keystone (TECH-28365):
--   catalog_panel_scene_targets: declarative DB ground truth for
--   (scene, canvas, slot) targets and (controller, adapter) bindings.
--   Bake emits scene-wire-plan.yaml from all rows.

BEGIN;

CREATE TABLE IF NOT EXISTS catalog_panel_scene_targets (
  panel_slug           text NOT NULL,
  scene_name           text NOT NULL,
  canvas_path          text NOT NULL,
  slot_anchor          text NOT NULL,
  controller_type      text NOT NULL,
  adapter_type         text,
  -- T3.0.3 canvas-layering audit: declares which sorting layer and order
  -- this panel's canvas occupies. Required hierarchy: HUD < SubViews < Modals < Notifications < Cursor.
  canvas_sorting_layer text,
  canvas_sorting_order int,
  PRIMARY KEY (panel_slug)
);

COMMENT ON TABLE catalog_panel_scene_targets IS
  'Layer 3 scene-wire (TECH-28365) — declarative DB ground truth per panel: '
  '(scene_name, canvas_path, slot_anchor) = where to wire, '
  '(controller_type, adapter_type) = what to wire. '
  'Bake emits Assets/Resources/UI/Generated/scene-wire-plan.yaml from all rows. '
  'Replaces tacit agent convention with declarative spec.';

DO $$
BEGIN
  RAISE NOTICE '0149 OK: catalog_panel_scene_targets created (Layer 3 scene-wire keystone + canvas-layering fields)';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DROP TABLE IF EXISTS catalog_panel_scene_targets;
