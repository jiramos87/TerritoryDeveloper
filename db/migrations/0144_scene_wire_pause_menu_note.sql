-- Migration: 0144_scene_wire_pause_menu_note.sql
-- Stage 8.0 Wave B4 — TECH-27096
-- Scene-wire record: CityScene.unity Canvas mount of baked pause-menu prefab.
-- Actual prefab instantiation performed via unity_bridge_command at wave verification.
-- This migration records the intent + asserts pause-menu entity is published.

DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'pause-menu' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0144: pause-menu entity missing — run 0142 first';
  END IF;

  RAISE NOTICE '0144 OK: pause-menu entity (id=%) ready for scene-wire bake', v_panel_id;
END;
$$;
