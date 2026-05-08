-- 0109_panel_detail_rect_json.sql
--
-- DB-first panel rect — TECH (hud-bar bake-test followup).
--
-- Moves PanelKind RectTransform defaults from hard-coded
-- `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs:ApplyPanelKindRectDefaults`
-- into `panel_detail.rect_json` so the source of truth is the catalog DB.
--
-- Schema:
--   rect_json jsonb NOT NULL DEFAULT '{}'::jsonb  — open-shape per-panel rect
--     {
--       "anchor_min":         [0, 1],
--       "anchor_max":         [1, 1],
--       "pivot":              [0.5, 1],
--       "anchored_position":  [0, -8],
--       "size_delta":         [-16, 144]
--     }
--   Empty `{}`     => bake handler falls back to hard-coded PanelKind defaults.
--   Partial keys   => bake handler applies the kind default first, then
--                     overlays whichever keys are present (last write wins).
--
-- Seed:
--   hud-bar (kind=panel) gets the rebake-6 visual: top-anchored full-width strip,
--   144 px tall (Right zone Col stack 64+4+64=132 + 4+4 padding fits;
--   Center 3-row label stack 3*32 + 2*4 = 104 fits) with 8 px top breathing room.
--
-- Idempotent: ADD COLUMN IF NOT EXISTS, UPDATE no-op when row already carries
-- the seed shape.

BEGIN;

ALTER TABLE panel_detail
  ADD COLUMN IF NOT EXISTS rect_json jsonb NOT NULL DEFAULT '{}'::jsonb;

-- Seed hud-bar with rebake-6 values (preserve current visual exactly).
UPDATE panel_detail pd
SET rect_json = jsonb_build_object(
  'anchor_min',        jsonb_build_array(0,    1),
  'anchor_max',        jsonb_build_array(1,    1),
  'pivot',             jsonb_build_array(0.5,  1),
  'anchored_position', jsonb_build_array(0,   -8),
  'size_delta',        jsonb_build_array(-16, 144)
)
FROM catalog_entity ce
WHERE ce.id = pd.entity_id
  AND ce.kind = 'panel'
  AND ce.slug = 'hud-bar'
  AND (pd.rect_json IS NULL OR pd.rect_json = '{}'::jsonb);

-- Sanity assertion.
DO $$
DECLARE
  v_rect jsonb;
BEGIN
  SELECT pd.rect_json INTO v_rect
  FROM panel_detail pd
  JOIN catalog_entity ce ON ce.id = pd.entity_id
  WHERE ce.kind = 'panel' AND ce.slug = 'hud-bar';

  IF v_rect IS NULL OR v_rect = '{}'::jsonb THEN
    RAISE EXCEPTION '0109: hud-bar rect_json failed to seed';
  END IF;

  IF (v_rect->'size_delta'->>1)::numeric <> 144 THEN
    RAISE EXCEPTION '0109: hud-bar size_delta.y expected 144, got %', v_rect->'size_delta'->>1;
  END IF;

  RAISE NOTICE '0109 OK: hud-bar rect_json seeded (size_delta=%)', v_rect->'size_delta';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   ALTER TABLE panel_detail DROP COLUMN rect_json;
