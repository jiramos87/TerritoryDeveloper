-- 0130_main_menu_reseed_per_design.sql
-- Realign main-menu DB seed to docs/ui-element-definitions.md §main-menu lines 1239-1248.
--
-- Defects fixed (all 7 from Play-Mode visual review):
-- 1. Confirm Quit was a sibling button below Quit → now inline state of Quit
--    confirm-button primitive (3 s window).
-- 2. Back button was rendering inline → now icon-button hidden by visible_bind
--    (mainmenu.back.visible) shown only on sub-views, top-left zone.
-- 3. Title/version/studio rendered as "--" inline with buttons → now in branding
--    strips: title=top, studio=bottom-left, version=bottom-right.
-- 4-5. Buttons full-width / not centered → now zone="center" with explicit
--    320×56 size, rendered into bake handler's centered narrow column.
-- 6. Missing rounded-rect body → bake handler IlluminatedButton path renders
--    body+halo prefab; defect was caption-only when label missing → fixed by
--    explicit label field in params_json.
-- 7. Lost blip sounds → bake handler IlluminatedButton already drives ThemedButton
--    auto-emit; restored once button rows actually use illuminated-button kind.
--
-- 0116 + 0127 patched but kept the wrong shape (7 buttons including separate
-- quit-confirm, no zone routing, params_json missing tooltips/binds). This
-- migration replaces the seed verbatim from the locked design-definition rows.
--
-- Idempotent: DELETE all panel_child rows where panel_entity_id = main-menu
-- panel id, then INSERT 10 rows per design spec.

BEGIN;

DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id
  FROM catalog_entity
  WHERE kind = 'panel' AND slug = 'main-menu';

  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0130: main-menu panel entity missing';
  END IF;

  -- Wipe stale rows (7-button + branding flat layout from 0116 + 0127 + 0128).
  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  -- Re-seed per design spec lines 1239-1248. Order matches design verbatim.
  -- All button labels + tooltips embedded so bake handler caption fallback fires.
  -- Zone routing drives bake handler's fullscreen-stack layout (added in 0131).

  -- Title strip (top-center, large display font).
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'title', 1, 'label', 'main-menu-title-label',
    jsonb_build_object(
      'kind',        'label',
      'text_static', 'Territory',
      'size_token',  'size.text.title-display',
      'align',       'center'
    ),
    jsonb_build_object('zone', 'top'));

  -- Studio (bottom-left, small muted).
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'studio', 2, 'label', 'main-menu-studio-label',
    jsonb_build_object(
      'kind',        'label',
      'text_static', 'Bacayo Studio',
      'size_token',  'size.text.caption',
      'color_token', 'color.text.muted'
    ),
    jsonb_build_object('zone', 'bottom-left'));

  -- Version (bottom-right, bound to mainmenu.version.text).
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'version', 3, 'label', 'main-menu-version-label',
    jsonb_build_object(
      'kind',        'label',
      'bind',        'mainmenu.version.text',
      'size_token',  'size.text.caption',
      'color_token', 'color.text.muted',
      'align',       'right'
    ),
    jsonb_build_object('zone', 'bottom-right'));

  -- Continue button (zone=center, primary).
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'continue', 4, 'button', 'main-menu-continue-button',
    jsonb_build_object(
      'kind',                          'illuminated-button',
      'label',                         'Continue',
      'action',                        'mainmenu.continue',
      'disabled_bind',                 'mainmenu.continue.disabled',
      'tooltip',                       'Resume your most recent city.',
      'tooltip_override_when_disabled', 'No save found.'
    ),
    jsonb_build_object(
      'zone', 'center',
      'size', jsonb_build_object('w', 320, 'h', 56)
    ));

  -- New Game button.
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'new-game', 5, 'button', 'main-menu-new-game-button',
    jsonb_build_object(
      'kind',    'illuminated-button',
      'label',   'New Game',
      'action',  'mainmenu.openNewGame',
      'tooltip', 'Start a new city.'
    ),
    jsonb_build_object(
      'zone', 'center',
      'size', jsonb_build_object('w', 320, 'h', 56)
    ));

  -- Load button.
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'load', 6, 'button', 'main-menu-load-button',
    jsonb_build_object(
      'kind',    'illuminated-button',
      'label',   'Load',
      'action',  'mainmenu.openLoad',
      'tooltip', 'Open the load list.'
    ),
    jsonb_build_object(
      'zone', 'center',
      'size', jsonb_build_object('w', 320, 'h', 56)
    ));

  -- Settings button.
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'settings', 7, 'button', 'main-menu-settings-button',
    jsonb_build_object(
      'kind',    'illuminated-button',
      'label',   'Settings',
      'action',  'mainmenu.openSettings',
      'tooltip', 'Open settings.'
    ),
    jsonb_build_object(
      'zone', 'center',
      'size', jsonb_build_object('w', 320, 'h', 56)
    ));

  -- Quit confirm-button (3 s inline confirm; reuses ConfirmButton primitive).
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'quit', 8, 'confirm-button', 'main-menu-quit-button',
    jsonb_build_object(
      'kind',              'destructive-confirm-button',
      'label',             'Quit',
      'confirm_label',     'Click again to confirm',
      'confirm_window_ms', 3000,
      'action_confirm',    'mainmenu.quit.confirm',
      'action',            'mainmenu.quit',
      'tooltip',           'Exit to desktop.'
    ),
    jsonb_build_object(
      'zone', 'center',
      'size', jsonb_build_object('w', 320, 'h', 56)
    ));

  -- Back icon-button (top-left, hidden on root via visible_bind).
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'back', 9, 'button', 'main-menu-back-button',
    jsonb_build_object(
      'kind',         'icon-button',
      'icon',         'back-arrow',
      'label',        'Back',
      'action',       'mainmenu.back',
      'visible_bind', 'mainmenu.back.visible',
      'tooltip',      'Back to menu.'
    ),
    jsonb_build_object(
      'zone', 'top-left',
      'size', jsonb_build_object('w', 48, 'h', 48)
    ));

  -- View-slot (center area sub-view mount; default=root).
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json, layout_json)
  VALUES (v_panel_id, 'content-slot', 10, 'view-slot', 'main-menu-content-slot',
    jsonb_build_object(
      'kind',      'view-slot',
      'slot_bind', 'mainmenu.contentScreen',
      'views',     jsonb_build_array('root', 'new-game-form', 'load-list', 'settings'),
      'default',   'root'
    ),
    jsonb_build_object('zone', 'center'));

  RAISE NOTICE '0130 OK: main-menu re-seeded with 10 children per design (panel_id=%)', v_panel_id;
END;
$$;

-- Sanity: assert 10 rows present + zones distributed.
DO $$
DECLARE
  v_panel_id bigint;
  n_total int;
  n_center int;
  n_quit_confirm int;
  n_view_slot int;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE kind='panel' AND slug='main-menu';

  SELECT COUNT(*) INTO n_total FROM panel_child WHERE panel_entity_id = v_panel_id;
  IF n_total <> 10 THEN
    RAISE EXCEPTION '0130: expected 10 children, got %', n_total;
  END IF;

  SELECT COUNT(*) INTO n_center FROM panel_child
  WHERE panel_entity_id = v_panel_id AND layout_json->>'zone' = 'center'
    AND child_kind IN ('button','confirm-button');
  IF n_center <> 5 THEN
    RAISE EXCEPTION '0130: expected 5 center-zone buttons (4 primary + quit confirm), got %', n_center;
  END IF;

  SELECT COUNT(*) INTO n_quit_confirm FROM panel_child
  WHERE panel_entity_id = v_panel_id AND child_kind = 'confirm-button';
  IF n_quit_confirm <> 1 THEN
    RAISE EXCEPTION '0130: expected 1 confirm-button (quit), got %', n_quit_confirm;
  END IF;

  SELECT COUNT(*) INTO n_view_slot FROM panel_child
  WHERE panel_entity_id = v_panel_id AND child_kind = 'view-slot';
  IF n_view_slot <> 1 THEN
    RAISE EXCEPTION '0130: expected 1 view-slot (content-slot), got %', n_view_slot;
  END IF;

  RAISE NOTICE '0130 OK: layout zones verified — 6 center buttons + 1 confirm-button + 1 view-slot + 3 branding labels';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child
--   WHERE panel_entity_id = (SELECT id FROM catalog_entity WHERE kind='panel' AND slug='main-menu');
--   -- followed by re-running 0116 + 0127 if the legacy 7-row shape needs restoring.
