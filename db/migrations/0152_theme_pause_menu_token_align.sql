-- 0152_theme_pause_menu_token_align.sql
-- Stage 12.0 — TECH-29760
-- Theme conformance: pause-menu matches MainMenu aesthetic.
-- 1. Add size-text-modal-title token entity (type-scale 24pt bold).
-- 2. Update pause-menu panel_detail.params_json → add bg_color_token (token-* ref).
-- 3. Update pause-menu title panel_child → size_token uses token-* prefixed slug.

BEGIN;

-- ── 1. Add size-text-modal-title token ───────────────────────────────────────
INSERT INTO catalog_entity (slug, kind, display_name)
VALUES ('size-text-modal-title', 'token', 'Size Text Modal Title')
ON CONFLICT (kind, slug) DO NOTHING;

DO $$
DECLARE
  v_token_id bigint;
BEGIN
  SELECT id INTO v_token_id FROM catalog_entity WHERE slug = 'size-text-modal-title' AND kind = 'token';
  IF v_token_id IS NULL THEN
    RAISE EXCEPTION '0152: size-text-modal-title token entity not found';
  END IF;

  INSERT INTO token_detail (entity_id, token_kind, value_json)
  VALUES (v_token_id, 'type-scale', '{"pt": 24, "weight": "bold"}'::jsonb)
  ON CONFLICT (entity_id) DO NOTHING;

  RAISE NOTICE '0152: size-text-modal-title token seeded (id=%)', v_token_id;
END;
$$;

-- ── 2a. Normalize main-menu bg_color_token to token-* prefix ────────────────
DO $$
DECLARE
  v_mm_id bigint;
BEGIN
  SELECT id INTO v_mm_id FROM catalog_entity WHERE slug = 'main-menu' AND kind = 'panel';
  IF v_mm_id IS NULL THEN
    RAISE EXCEPTION '0152: main-menu panel entity not found';
  END IF;

  UPDATE panel_detail
  SET params_json = jsonb_set(params_json, '{bg_color_token}', '"token-color-bg-menu"'::jsonb)
  WHERE entity_id = v_mm_id;

  RAISE NOTICE '0152: main-menu bg_color_token normalized to token-color-bg-menu';
END;
$$;

-- ── 2b. Update pause-menu panel_detail.params_json ────────────────────────────
DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'pause-menu' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0152: pause-menu panel entity not found';
  END IF;

  UPDATE panel_detail
  SET params_json = params_json || '{"bg_color_token": "token-color-bg-menu"}'::jsonb
  WHERE entity_id = v_panel_id;

  RAISE NOTICE '0152: pause-menu panel_detail.params_json updated with bg_color_token';
END;
$$;

-- ── 3. Update pause-menu title panel_child size_token ────────────────────────
DO $$
DECLARE
  v_panel_id bigint;
  v_child_id bigint;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'pause-menu' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0152: pause-menu panel entity not found';
  END IF;

  SELECT id INTO v_child_id
  FROM panel_child
  WHERE panel_entity_id = v_panel_id
    AND slot_name = 'title'
    AND child_kind = 'label';
  IF v_child_id IS NULL THEN
    RAISE EXCEPTION '0152: pause-menu title label child not found';
  END IF;

  UPDATE panel_child
  SET params_json = jsonb_set(
    params_json,
    '{size_token}',
    '"token-size-text-modal-title"'::jsonb
  )
  WHERE id = v_child_id;

  RAISE NOTICE '0152: pause-menu title child size_token updated to token-size-text-modal-title';
END;
$$;

-- ── Sanity assertions ─────────────────────────────────────────────────────────
DO $$
DECLARE
  v_panel_id    bigint;
  v_token_id    bigint;
  v_bg_token    text;
  v_title_token text;
BEGIN
  -- token entity exists
  SELECT id INTO v_token_id FROM catalog_entity WHERE slug = 'size-text-modal-title' AND kind = 'token';
  IF v_token_id IS NULL THEN
    RAISE EXCEPTION '0152 assert: size-text-modal-title token missing';
  END IF;

  -- token_detail row exists
  IF NOT EXISTS (SELECT 1 FROM token_detail WHERE entity_id = v_token_id) THEN
    RAISE EXCEPTION '0152 assert: size-text-modal-title token_detail row missing';
  END IF;

  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'pause-menu' AND kind = 'panel';

  -- pause-menu bg_color_token in params_json
  SELECT params_json->>'bg_color_token' INTO v_bg_token
  FROM panel_detail WHERE entity_id = v_panel_id;
  IF v_bg_token IS DISTINCT FROM 'token-color-bg-menu' THEN
    RAISE EXCEPTION '0152 assert: bg_color_token mismatch — expected token-color-bg-menu, got %', v_bg_token;
  END IF;

  -- title child size_token updated
  SELECT params_json->>'size_token' INTO v_title_token
  FROM panel_child
  WHERE panel_entity_id = v_panel_id AND slot_name = 'title' AND child_kind = 'label';
  IF v_title_token IS DISTINCT FROM 'token-size-text-modal-title' THEN
    RAISE EXCEPTION '0152 assert: title size_token mismatch — expected token-size-text-modal-title, got %', v_title_token;
  END IF;

  RAISE NOTICE '0152 OK: all assertions pass — pause-menu token alignment complete';
END;
$$;

COMMIT;
