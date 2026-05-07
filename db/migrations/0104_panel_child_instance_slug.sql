-- 0104_panel_child_instance_slug.sql
--
-- TECH-23571 / game-ui-catalog-bake Stage 9.15
--
-- Adds instance_slug TEXT NULL to panel_child so per-child semantic names
-- survive snapshot-exporter → panels.json → PanelSnapshotChild DTO → bake handler.

BEGIN;

ALTER TABLE panel_child ADD COLUMN IF NOT EXISTS instance_slug TEXT NULL;

-- Sanity: column present
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name='panel_child' AND column_name='instance_slug'
  ) THEN
    RAISE EXCEPTION '0104: panel_child.instance_slug column missing after ALTER';
  END IF;
  RAISE NOTICE '0104 OK: panel_child.instance_slug column present';
END;
$$;

COMMIT;
