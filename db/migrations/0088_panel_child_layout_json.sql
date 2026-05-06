-- 0088_panel_child_layout_json.sql
--
-- TECH-17990 / game-ui-catalog-bake Stage 9.10 §Plan Digest.
--
-- Adds `layout_json` jsonb column to `panel_child`.
-- Stores per-child layout routing metadata (e.g. {"zone": "left"}).
-- Default NULL — absent on non-hud-bar children; projected by exporter.
--
-- Idempotent: ADD COLUMN IF NOT EXISTS.

BEGIN;

ALTER TABLE panel_child
  ADD COLUMN IF NOT EXISTS layout_json jsonb DEFAULT NULL;

COMMENT ON COLUMN panel_child.layout_json IS
  'Per-child layout routing metadata (e.g. {"zone":"left"}). NULL on children that do not require zone routing (non-hud-bar panels).';

COMMIT;

-- Rollback (dev only):
--   ALTER TABLE panel_child DROP COLUMN IF EXISTS layout_json;
