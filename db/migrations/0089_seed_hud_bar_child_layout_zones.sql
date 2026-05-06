-- 0089_seed_hud_bar_child_layout_zones.sql
--
-- TECH-17990 / game-ui-catalog-bake Stage 9.10 §Plan Digest.
--
-- Sets layout_json.zone on hud_bar panel_child rows.
-- Zone assignment (hud-bar has 10 children after 0087):
--   ord 1   (hud_bar_btn_budget) → zone "left"
--   ord 2   (hud_bar_btn_1)      → zone "left"
--   ord 3   (hud_bar_btn_2)      → zone "left"
--   ord 4   (hud_bar_btn_3)      → zone "center"
--   ord 5   (hud_bar_btn_4)      → zone "center"
--   ord 6   (hud_bar_btn_5)      → zone "center"
--   ord 7   (hud_bar_btn_6)      → zone "right"
--   ord 8   (hud_bar_btn_7)      → zone "right"
--   ord 9   (hud_bar_btn_8)      → zone "right"
--   ord 10  (hud_bar_btn_9)      → zone "right"
--
-- Idempotent: UPDATE with explicit zone values.

BEGIN;

UPDATE panel_child
SET layout_json = jsonb_build_object('zone',
  CASE order_idx
    WHEN 1  THEN 'left'
    WHEN 2  THEN 'left'
    WHEN 3  THEN 'left'
    WHEN 4  THEN 'center'
    WHEN 5  THEN 'center'
    WHEN 6  THEN 'center'
    WHEN 7  THEN 'right'
    WHEN 8  THEN 'right'
    WHEN 9  THEN 'right'
    WHEN 10 THEN 'right'
    ELSE 'center'
  END
)
WHERE panel_entity_id = (
  SELECT id FROM catalog_entity WHERE kind = 'panel' AND slug = 'hud_bar'
);

COMMIT;

-- Rollback (dev only):
--   UPDATE panel_child SET layout_json = NULL
--   WHERE panel_entity_id = (
--     SELECT id FROM catalog_entity WHERE kind = 'panel' AND slug = 'hud_bar'
--   );
