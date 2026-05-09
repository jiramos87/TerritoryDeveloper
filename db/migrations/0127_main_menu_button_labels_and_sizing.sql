-- 0127_main_menu_button_labels_and_sizing.sql
-- Bake regression fix — main-menu buttons render blank 64×64 squares.
--
-- Defect 1: 0116 seed buttons params_json has no `label` key + kind="button"
--           (instead of "illuminated-button"). UiBakeHandler.NormalizeChildKind
--           passes pj.kind through when set; "button" hits default case → no
--           IlluminatedButton wired → blank face. Patch sets kind=illuminated-button
--           and adds label so SpawnIlluminatedButtonCaption fires.
-- Defect 2: 0116 seed buttons layout_json has no `size` key. ResolveSnapshotChildDims
--           defaults to 64×64. Vertical menu strip needs wide rows ~480×56.
--
-- Fix: idempotent UPDATE per (panel_id, slot_name). Patches params_json
-- (label + kind=illuminated-button) and layout_json.size on existing 7 button rows
-- (5 primary + quit-confirm + back). Re-snapshot panels.json + re-bake main-menu
-- prefab after applying.

BEGIN;

DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id
  FROM catalog_entity
  WHERE kind = 'panel' AND slug = 'main-menu';

  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0127: main-menu panel entity missing';
  END IF;

  -- Continue button (primary, 480x56)
  UPDATE panel_child
  SET params_json = params_json || jsonb_build_object('label', 'Continue', 'kind', 'illuminated-button'),
      layout_json = layout_json || jsonb_build_object('size', jsonb_build_object('w', 480, 'h', 56))
  WHERE panel_entity_id = v_panel_id AND slot_name = 'continue';

  -- New Game button
  UPDATE panel_child
  SET params_json = params_json || jsonb_build_object('label', 'New Game', 'kind', 'illuminated-button'),
      layout_json = layout_json || jsonb_build_object('size', jsonb_build_object('w', 480, 'h', 56))
  WHERE panel_entity_id = v_panel_id AND slot_name = 'new-game';

  -- Load City button
  UPDATE panel_child
  SET params_json = params_json || jsonb_build_object('label', 'Load City', 'kind', 'illuminated-button'),
      layout_json = layout_json || jsonb_build_object('size', jsonb_build_object('w', 480, 'h', 56))
  WHERE panel_entity_id = v_panel_id AND slot_name = 'load';

  -- Settings button
  UPDATE panel_child
  SET params_json = params_json || jsonb_build_object('label', 'Settings', 'kind', 'illuminated-button'),
      layout_json = layout_json || jsonb_build_object('size', jsonb_build_object('w', 480, 'h', 56))
  WHERE panel_entity_id = v_panel_id AND slot_name = 'settings';

  -- Quit button
  UPDATE panel_child
  SET params_json = params_json || jsonb_build_object('label', 'Quit', 'kind', 'illuminated-button'),
      layout_json = layout_json || jsonb_build_object('size', jsonb_build_object('w', 480, 'h', 56))
  WHERE panel_entity_id = v_panel_id AND slot_name = 'quit';

  -- Quit-confirm button
  UPDATE panel_child
  SET params_json = params_json || jsonb_build_object('label', 'Confirm Quit', 'kind', 'illuminated-button'),
      layout_json = layout_json || jsonb_build_object('size', jsonb_build_object('w', 480, 'h', 56))
  WHERE panel_entity_id = v_panel_id AND slot_name = 'quit-confirm';

  -- Back button (smaller — nav zone)
  UPDATE panel_child
  SET params_json = params_json || jsonb_build_object('label', 'Back', 'kind', 'illuminated-button'),
      layout_json = layout_json || jsonb_build_object('size', jsonb_build_object('w', 320, 'h', 48))
  WHERE panel_entity_id = v_panel_id AND slot_name = 'back';

  RAISE NOTICE '0127 OK: main-menu button labels + sizes patched (panel_id=%)', v_panel_id;
END;
$$;

-- Sanity: verify all 7 button rows now have label + size.
DO $$
DECLARE
  v_panel_id bigint;
  n_missing  int;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE kind='panel' AND slug='main-menu';

  SELECT COUNT(*) INTO n_missing
  FROM panel_child
  WHERE panel_entity_id = v_panel_id
    AND slot_name IN ('continue','new-game','load','settings','quit','quit-confirm','back')
    AND (
      NOT (params_json ? 'label')
      OR NOT (layout_json ? 'size')
    );

  IF n_missing > 0 THEN
    RAISE EXCEPTION '0127: % button rows still missing label or size', n_missing;
  END IF;

  RAISE NOTICE '0127 OK: all 7 main-menu button rows have label + size';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   UPDATE panel_child SET params_json = params_json - 'label',
--                          layout_json = layout_json - 'size'
--   WHERE panel_entity_id = (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='main-menu')
--     AND slot_name IN ('continue','new-game','load','settings','quit','quit-confirm','back');
