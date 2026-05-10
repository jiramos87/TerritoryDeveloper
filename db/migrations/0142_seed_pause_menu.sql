-- Migration: 0142_seed_pause_menu.sql
-- Stage 8.0 Wave B4 — TECH-27092
-- Seed pause-menu catalog_entity + panel_detail + 7 panel_child rows.
-- Children: 1 title label + 6 buttons (Resume / Settings / Save / Load / Main-menu / Quit).
-- layout_template=modal-card requires constraint extension (not yet in allowed set).

BEGIN;

-- ── Extend layout_template constraint to include modal-card ───────────────────
ALTER TABLE panel_detail
  DROP CONSTRAINT IF EXISTS panel_detail_layout_template_check;

ALTER TABLE panel_detail
  ADD CONSTRAINT panel_detail_layout_template_check
  CHECK (layout_template = ANY (ARRAY[
    'vstack'::text,
    'hstack'::text,
    'grid'::text,
    'free'::text,
    'fullscreen-stack'::text,
    'modal-card'::text
  ]));

-- ── catalog_entity ────────────────────────────────────────────────────────────
INSERT INTO catalog_entity (slug, kind, display_name)
VALUES (
  'pause-menu',
  'panel',
  'Pause Menu'
)
ON CONFLICT (kind, slug) DO NOTHING;

-- ── panel_detail ──────────────────────────────────────────────────────────────
DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'pause-menu' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0142: pause-menu entity not found after INSERT';
  END IF;

  INSERT INTO panel_detail (
    entity_id,
    layout_template,
    layout,
    modal,
    padding_json,
    gap_px,
    params_json,
    rect_json,
    updated_at
  )
  VALUES (
    v_panel_id,
    'modal-card',
    'vstack',
    true,
    '{"top":0,"left":0,"right":0,"bottom":0}'::jsonb,
    0,
    '{"width":480,"height":480,"modal_kind":"pause"}'::jsonb,
    '{"anchor_min":[0.5,0.5],"anchor_max":[0.5,0.5],"pivot":[0.5,0.5],"size_delta":[480,480],"anchored_position":[0,0]}'::jsonb,
    now()
  )
  ON CONFLICT (entity_id) DO NOTHING;
END;
$$;

-- ── panel_child rows (7 total) ────────────────────────────────────────────────
DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'pause-menu' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0142: pause-menu entity missing';
  END IF;

  -- Wipe any stale rows (idempotent re-run safety).
  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  -- 1: title label
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'title', 1, 'label', 'pause-menu-title-label',
    jsonb_build_object(
      'kind',        'label',
      'text_static', 'Paused',
      'size_token',  'size.text.modal-title',
      'align',       'center'
    ));

  -- 2: Resume button
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'resume', 2, 'button', 'pause-menu-resume-button',
    jsonb_build_object(
      'kind',    'illuminated-button',
      'label',   'Resume',
      'action',  'pause.resume',
      'tooltip', 'Return to game.'
    ));

  -- 3: Settings button
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'settings', 3, 'button', 'pause-menu-settings-button',
    jsonb_build_object(
      'kind',    'illuminated-button',
      'label',   'Settings',
      'action',  'pause.openSettings',
      'tooltip', 'Open settings.'
    ));

  -- 4: Save button
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'save', 4, 'button', 'pause-menu-save-button',
    jsonb_build_object(
      'kind',    'illuminated-button',
      'label',   'Save',
      'action',  'pause.openSave',
      'tooltip', 'Save the city.'
    ));

  -- 5: Load button
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'load', 5, 'button', 'pause-menu-load-button',
    jsonb_build_object(
      'kind',    'illuminated-button',
      'label',   'Load',
      'action',  'pause.openLoad',
      'tooltip', 'Load a save.'
    ));

  -- 6: Main Menu confirm-button (3 s countdown)
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'main-menu', 6, 'confirm-button', 'pause-menu-main-menu-button',
    jsonb_build_object(
      'kind',              'destructive-confirm-button',
      'label',             'Main Menu',
      'confirm_label',     'Click again to confirm',
      'confirm_window_ms', 3000,
      'action',            'pause.mainMenu',
      'action_confirm',    'pause.mainMenu.confirm',
      'tooltip',           'Return to main menu.'
    ));

  -- 7: Quit confirm-button (3 s countdown)
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'quit', 7, 'confirm-button', 'pause-menu-quit-button',
    jsonb_build_object(
      'kind',              'destructive-confirm-button',
      'label',             'Quit',
      'confirm_label',     'Click again to confirm',
      'confirm_window_ms', 3000,
      'action',            'pause.quit',
      'action_confirm',    'pause.quit.confirm',
      'tooltip',           'Exit to desktop.'
    ));

  RAISE NOTICE '0142 OK: pause-menu seeded with 7 children (panel_id=%)', v_panel_id;
END;
$$;

-- ── Sanity assertions ─────────────────────────────────────────────────────────
DO $$
DECLARE
  v_panel_id  bigint;
  n_total     int;
  n_template  text;
  n_modal     bool;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'pause-menu' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0142 assert: pause-menu entity missing';
  END IF;

  SELECT COUNT(*) INTO n_total FROM panel_child WHERE panel_entity_id = v_panel_id;
  IF n_total <> 7 THEN
    RAISE EXCEPTION '0142 assert: expected 7 children, got %', n_total;
  END IF;

  SELECT layout_template, modal INTO n_template, n_modal
  FROM panel_detail WHERE entity_id = v_panel_id;
  IF n_template <> 'modal-card' THEN
    RAISE EXCEPTION '0142 assert: expected layout_template=modal-card, got %', n_template;
  END IF;
  IF n_modal IS NOT TRUE THEN
    RAISE EXCEPTION '0142 assert: expected modal=true';
  END IF;

  RAISE NOTICE '0142 OK: 7 children + layout_template=modal-card + modal=true verified';
END;
$$;

COMMIT;
