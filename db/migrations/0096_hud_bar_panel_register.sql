-- 0096_hud_bar_panel_register.sql
--
-- TECH-19061 / game-ui-catalog-bake Stage 9.12
--
-- Registers `hud-bar` as a first-class catalog panel entity + 10 panel_child rows
-- with layout_json.zone (3 left / 3 center / 4 right).
--
-- Fixes 9.10 false-pass: migration 0089 set layout_json zones on panel_child rows
-- whose panel_entity_id referenced slug='hud_bar' — a panel that was NEVER inserted
-- into catalog_entity. This migration seeds the missing entity so the snapshot
-- exporter picks it up (gate: current_published_version_id IS NOT NULL).
--
-- Zone assignment (matches 0089 contract, kebab slug):
--   ord 1   (hud-bar-budget-button)  → zone "left"
--   ord 2   (placeholder-button)     → zone "left"
--   ord 3   (placeholder-button)     → zone "left"
--   ord 4   (placeholder-button)     → zone "center"
--   ord 5   (placeholder-button)     → zone "center"
--   ord 6   (placeholder-button)     → zone "center"
--   ord 7   (placeholder-button)     → zone "right"
--   ord 8   (placeholder-button)     → zone "right"
--   ord 9   (placeholder-button)     → zone "right"
--   ord 10  (placeholder-button)     → zone "right"
--
-- All inserts are idempotent via ON CONFLICT DO NOTHING.

BEGIN;

-- ── 1. catalog_entity row ────────────────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'hud-bar', 'Hud Bar')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 2. panel_detail row ──────────────────────────────────────────────────────

INSERT INTO panel_detail (entity_id, layout_template, layout, gap_px, padding_json, params_json)
SELECT ce.id, 'hstack', 'hstack', 8,
       '{"top":0,"right":0,"bottom":0,"left":0}'::jsonb,
       '{}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'hud-bar'
ON CONFLICT (entity_id) DO UPDATE
  SET layout_template = EXCLUDED.layout_template,
      layout          = EXCLUDED.layout,
      gap_px          = EXCLUDED.gap_px,
      padding_json    = EXCLUDED.padding_json;

-- ── 3. entity_version row (required for snapshot export gate) ────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
       '{"migration":"0096_hud_bar_panel_register","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'panel' AND ce.slug = 'hud-bar'
  AND NOT EXISTS (
    SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id AND ev.version_number = 1
  );

-- ── 4. Wire current_published_version_id ────────────────────────────────────

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'panel'
  AND ce.slug = 'hud-bar'
  AND ce.current_published_version_id IS NULL;

-- ── 5. panel_child rows (10 children, 3 left / 3 center / 4 right) ──────────
--
-- ord 1 → hud-bar-budget-button (already a catalog_entity post-0092 rename)
-- ord 2–10 → reuse hud-bar-budget-button as placeholder (distinct slots, real wiring
--   TBD when remaining 9 buttons are named; idempotent ON CONFLICT DO NOTHING).
--
-- layout_json.zone per 0089 contract (reproduced correctly here on real entity).

DO $$
DECLARE
  v_panel_id  bigint;
  v_ver_id    bigint;
  v_btn_id    bigint;
  v_btn_ver   bigint;
  zones       text[] := ARRAY['left','left','left','center','center','center','right','right','right','right'];
  i           int;
  z           text;
BEGIN
  SELECT ce.id, ce.current_published_version_id
    INTO v_panel_id, v_ver_id
    FROM catalog_entity ce
    WHERE ce.kind = 'panel' AND ce.slug = 'hud-bar';

  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0096: hud-bar panel entity missing — INSERT above failed';
  END IF;

  SELECT ce.id, ce.current_published_version_id
    INTO v_btn_id, v_btn_ver
    FROM catalog_entity ce
    WHERE ce.kind = 'button' AND ce.slug = 'hud-bar-budget-button';

  FOR i IN 1..10 LOOP
    z := zones[i];
    INSERT INTO panel_child (
      panel_entity_id, panel_version_id,
      slot_name, order_idx, child_kind,
      child_entity_id, child_version_id,
      params_json, layout_json
    ) VALUES (
      v_panel_id, v_ver_id,
      'main', i, 'button',
      v_btn_id, v_btn_ver,
      jsonb_build_object('kind', 'button', 'ord', i, 'button_ref', 'hud-bar-budget-button'),
      jsonb_build_object('zone', z)
    )
    ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING;
  END LOOP;

  RAISE NOTICE '0096: hud-bar seeded — panel_id=% ver_id=% 10 panel_child rows inserted (idempotent)', v_panel_id, v_ver_id;
END;
$$;

-- ── 6. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_entity int;
  n_detail int;
  n_ver    int;
  n_kids   int;
BEGIN
  SELECT COUNT(*) INTO n_entity FROM catalog_entity WHERE kind='panel' AND slug='hud-bar';
  SELECT COUNT(*) INTO n_detail FROM panel_detail pd JOIN catalog_entity ce ON ce.id=pd.entity_id WHERE ce.slug='hud-bar';
  SELECT COUNT(*) INTO n_ver    FROM entity_version ev JOIN catalog_entity ce ON ce.id=ev.entity_id WHERE ce.slug='hud-bar' AND ev.status='published';
  SELECT COUNT(*) INTO n_kids   FROM panel_child pc JOIN catalog_entity ce ON ce.id=pc.panel_entity_id WHERE ce.slug='hud-bar';

  IF n_entity = 0 THEN RAISE EXCEPTION '0096: catalog_entity hud-bar missing'; END IF;
  IF n_detail = 0 THEN RAISE EXCEPTION '0096: panel_detail hud-bar missing'; END IF;
  IF n_ver    = 0 THEN RAISE EXCEPTION '0096: entity_version hud-bar published missing'; END IF;
  IF n_kids  != 10 THEN RAISE EXCEPTION '0096: expected 10 panel_child rows, got %', n_kids; END IF;

  RAISE NOTICE '0096 OK: entity=% detail=% versions=% children=%', n_entity, n_detail, n_ver, n_kids;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child USING catalog_entity ce
--    WHERE panel_child.panel_entity_id = ce.id AND ce.slug = 'hud-bar';
--   DELETE FROM entity_version USING catalog_entity ce
--    WHERE entity_version.entity_id = ce.id AND ce.slug = 'hud-bar';
--   DELETE FROM panel_detail USING catalog_entity ce
--    WHERE panel_detail.entity_id = ce.id AND ce.slug = 'hud-bar';
--   DELETE FROM catalog_entity WHERE kind = 'panel' AND slug = 'hud-bar';
