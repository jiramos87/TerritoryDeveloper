-- 0128_main_menu_rect_fullscreen.sql
-- Bake regression fix continuation — main-menu panel renders as 480×320 box
-- in canvas center, squishing 11 children inside a fixed Modal-default rect.
--
-- Root cause: UiBakeHandler.MapLayoutTemplateToPanelKind maps `vstack` →
-- PanelKind.Modal. ApplyPanelKindRectDefaults(Modal) hard-codes
-- anchorMin=(0.5,0.5), anchorMax=(0.5,0.5), sizeDelta=(480,320). Migration
-- 0116 set params_json.fullscreen=true but bake handler ignores that flag.
--
-- Fix: write panel_detail.rect_json overlay (schema from 0109) so bake
-- handler stretches main-menu rect fullscreen via ApplyPanelRectJsonOverlay.
-- DB-only fix — no C# change needed.
--
-- Idempotent: UPDATE per (kind=panel, slug=main-menu); empty/partial
-- rect_json overwritten with full fullscreen shape.

BEGIN;

UPDATE panel_detail pd
SET rect_json = jsonb_build_object(
  'anchor_min',        jsonb_build_array(0,   0),
  'anchor_max',        jsonb_build_array(1,   1),
  'pivot',             jsonb_build_array(0.5, 0.5),
  'anchored_position', jsonb_build_array(0,   0),
  'size_delta',        jsonb_build_array(0,   0)
)
FROM catalog_entity ce
WHERE ce.id = pd.entity_id
  AND ce.kind = 'panel'
  AND ce.slug = 'main-menu';

-- Sanity assertion.
DO $$
DECLARE
  v_rect jsonb;
BEGIN
  SELECT pd.rect_json INTO v_rect
  FROM panel_detail pd
  JOIN catalog_entity ce ON ce.id = pd.entity_id
  WHERE ce.kind = 'panel' AND ce.slug = 'main-menu';

  IF v_rect IS NULL OR v_rect = '{}'::jsonb THEN
    RAISE EXCEPTION '0128: main-menu rect_json failed to seed';
  END IF;

  IF (v_rect->'anchor_max'->>0)::numeric <> 1
     OR (v_rect->'anchor_max'->>1)::numeric <> 1 THEN
    RAISE EXCEPTION '0128: main-menu anchor_max expected (1,1), got %', v_rect->'anchor_max';
  END IF;

  RAISE NOTICE '0128 OK: main-menu rect_json fullscreen seeded (rect=%)', v_rect;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   UPDATE panel_detail pd
--   SET rect_json = '{}'::jsonb
--   FROM catalog_entity ce
--   WHERE ce.id = pd.entity_id AND ce.kind='panel' AND ce.slug='main-menu';
