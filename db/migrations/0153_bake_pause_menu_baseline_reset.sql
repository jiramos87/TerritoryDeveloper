-- 0153_bake_pause_menu_baseline_reset.sql
-- Stage 12.0 — TECH-29761
-- Re-bake pause-menu: insert ia_ui_bake_history row recording the
-- MainMenu-aligned token update as the new canonical bake baseline.
-- Resets Layer 5 B.7c visual diff baseline to Stage 12.0 state.

BEGIN;

DO $$
DECLARE
  v_history_id bigint;
BEGIN
  -- Insert bake history row — baseline reset after token alignment (0152).
  INSERT INTO ia_ui_bake_history (
    panel_slug,
    baked_at,
    bake_handler_version,
    diff_summary,
    commit_sha
  )
  VALUES (
    'pause-menu',
    now(),
    'stage12-token-align-baseline',
    jsonb_build_object(
      'baseline_reset',    true,
      'reason',            'Stage 12.0 theme conformance — MainMenu token alignment',
      'params_json_delta', jsonb_build_object(
        'added',   jsonb_build_array('bg_color_token'),
        'changed', jsonb_build_array('title.size_token')
      ),
      'new_token_added',   'size-text-modal-title'
    ),
    'stage12-baseline'
  )
  RETURNING id INTO v_history_id;

  -- Insert per-field bake diff rows.
  INSERT INTO ia_bake_diffs (history_id, change_kind, child_kind, slug, before, after)
  VALUES
    (v_history_id, 'added',    'panel',  'pause-menu',       NULL,
     '{"bg_color_token": "token-color-bg-menu"}'::jsonb),
    (v_history_id, 'modified', 'label',  'pause-menu-title-label',
     '{"size_token": "size.text.modal-title"}'::jsonb,
     '{"size_token": "token-size-text-modal-title"}'::jsonb),
    (v_history_id, 'added',    'token',  'size-text-modal-title', NULL,
     '{"token_kind": "type-scale", "value_json": {"pt": 24, "weight": "bold"}}'::jsonb);

  RAISE NOTICE '0153 OK: pause-menu bake baseline reset (history_id=%, bake_diffs=3)', v_history_id;
END;
$$;

-- ── Sanity: bake history row exists ─────────────────────────────────────────
DO $$
DECLARE
  v_count int;
BEGIN
  SELECT COUNT(*)::int INTO v_count
  FROM ia_ui_bake_history
  WHERE panel_slug = 'pause-menu'
    AND bake_handler_version = 'stage12-token-align-baseline';
  IF v_count < 1 THEN
    RAISE EXCEPTION '0153 assert: bake history row for pause-menu stage12 baseline missing';
  END IF;
  RAISE NOTICE '0153 OK: bake history assertion passed';
END;
$$;

COMMIT;
