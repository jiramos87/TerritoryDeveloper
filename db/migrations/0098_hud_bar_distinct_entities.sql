-- 0098_hud_bar_distinct_entities.sql
--
-- TECH-19975 / game-ui-catalog-bake Stage 9.13
--
-- Replaces 9.12 stub state (10 panel_child rows all referencing same
-- hud-bar-budget-button slug) with one distinct catalog_entity per real
-- HUD control + 16 distinct panel_child rows (3 left / 8 center / 5 right).
--
-- Entities inserted (idempotent ON CONFLICT DO NOTHING):
--   Buttons (14):
--     hud-bar-build-residential-button, hud-bar-build-commercial-button,
--     hud-bar-build-industrial-button, hud-bar-auto-toggle,
--     hud-bar-budget-plus-button, hud-bar-budget-minus-button,
--     hud-bar-budget-graph-button, hud-bar-map-button,
--     hud-bar-pause-button, hud-bar-play-button,
--     hud-bar-speed-1-button, hud-bar-speed-2-button,
--     hud-bar-speed-3-button, hud-bar-speed-4-button
--   Labels (2):
--     hud-bar-city-name-label, hud-bar-budget-readout-label
--
-- panel_child layout:
--   Left (3):   build-residential, build-commercial, build-industrial
--   Center (8): city-name-label, auto-toggle, budget-plus, budget-minus,
--               budget-graph, map-button, budget-readout-label, budget-button (existing)
--   Right (6):  pause, play, speed-1, speed-2, speed-3, speed-4
--   Total: 17 panel_child rows, 17 distinct refs
--
-- Note: hud-bar-budget-button (existing from 0096) retained in DB; not deleted.
-- The 10 stub panel_child rows are DELETED and replaced with 16 distinct rows.

BEGIN;

-- ── 1. Insert new catalog_entity rows ────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  ('button', 'hud-bar-build-residential-button', 'Build Residential'),
  ('button', 'hud-bar-build-commercial-button',  'Build Commercial'),
  ('button', 'hud-bar-build-industrial-button',  'Build Industrial'),
  ('button', 'hud-bar-auto-toggle',              'AUTO Toggle'),
  ('button', 'hud-bar-budget-plus-button',       'Budget +'),
  ('button', 'hud-bar-budget-minus-button',      'Budget -'),
  ('button', 'hud-bar-budget-graph-button',      'Budget Graph'),
  ('button', 'hud-bar-map-button',               'Map'),
  ('button', 'hud-bar-pause-button',             'Pause'),
  ('button', 'hud-bar-play-button',              'Play'),
  ('button', 'hud-bar-speed-1-button',           'Speed 1'),
  ('button', 'hud-bar-speed-2-button',           'Speed 2'),
  ('button', 'hud-bar-speed-3-button',           'Speed 3'),
  ('button', 'hud-bar-speed-4-button',           'Speed 4'),
  ('token',  'hud-bar-city-name-label',          'City Name'),
  ('token',  'hud-bar-budget-readout-label',     'Budget Readout')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 2. Delete stub panel_child rows (10 rows all referencing hud-bar-budget-button) ──

DELETE FROM panel_child
WHERE panel_entity_id = (
  SELECT id FROM catalog_entity WHERE kind = 'panel' AND slug = 'hud-bar'
);

-- ── 3. Insert 16 distinct panel_child rows ────────────────────────────────────

DO $$
DECLARE
  v_panel_id  bigint;
  v_ver_id    bigint;
BEGIN
  SELECT ce.id, ce.current_published_version_id
    INTO v_panel_id, v_ver_id
    FROM catalog_entity ce
    WHERE ce.kind = 'panel' AND ce.slug = 'hud-bar';

  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0098: hud-bar panel entity missing — run migration 0096 first';
  END IF;

  -- Left zone (3): build buttons
  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 1, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-build-residential-button'),
         '{"kind":"button","button_ref":"hud-bar-build-residential-button"}'::jsonb,
         '{"zone":"left"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 2, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-build-commercial-button'),
         '{"kind":"button","button_ref":"hud-bar-build-commercial-button"}'::jsonb,
         '{"zone":"left"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 3, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-build-industrial-button'),
         '{"kind":"button","button_ref":"hud-bar-build-industrial-button"}'::jsonb,
         '{"zone":"left"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  -- Center zone (8): city-name-label, auto-toggle, budget+, budget-, graph, map, budget-readout-label, budget-button
  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 4, 'label',
         (SELECT id FROM catalog_entity WHERE kind='token' AND slug='hud-bar-city-name-label'),
         '{"kind":"label","label_ref":"hud-bar-city-name-label"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 5, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-auto-toggle'),
         '{"kind":"button","button_ref":"hud-bar-auto-toggle"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 6, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-plus-button'),
         '{"kind":"button","button_ref":"hud-bar-budget-plus-button"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 7, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-minus-button'),
         '{"kind":"button","button_ref":"hud-bar-budget-minus-button"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 8, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-graph-button'),
         '{"kind":"button","button_ref":"hud-bar-budget-graph-button"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 9, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-map-button'),
         '{"kind":"button","button_ref":"hud-bar-map-button"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 10, 'label',
         (SELECT id FROM catalog_entity WHERE kind='token' AND slug='hud-bar-budget-readout-label'),
         '{"kind":"label","label_ref":"hud-bar-budget-readout-label"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 11, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-button'),
         '{"kind":"button","button_ref":"hud-bar-budget-button"}'::jsonb,
         '{"zone":"center"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  -- Right zone (5): pause, play, speed-1..4
  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 12, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-pause-button'),
         '{"kind":"button","button_ref":"hud-bar-pause-button"}'::jsonb,
         '{"zone":"right"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 13, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-play-button'),
         '{"kind":"button","button_ref":"hud-bar-play-button"}'::jsonb,
         '{"zone":"right"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 14, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-1-button'),
         '{"kind":"button","button_ref":"hud-bar-speed-1-button"}'::jsonb,
         '{"zone":"right"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 15, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-2-button'),
         '{"kind":"button","button_ref":"hud-bar-speed-2-button"}'::jsonb,
         '{"zone":"right"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 16, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-3-button'),
         '{"kind":"button","button_ref":"hud-bar-speed-3-button"}'::jsonb,
         '{"zone":"right"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, params_json, layout_json)
  SELECT v_panel_id, v_ver_id, 'main', 17, 'button',
         (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-4-button'),
         '{"kind":"button","button_ref":"hud-bar-speed-4-button"}'::jsonb,
         '{"zone":"right"}'::jsonb
  ON CONFLICT (panel_entity_id, slot_name, order_idx) DO UPDATE
    SET child_entity_id = EXCLUDED.child_entity_id,
        params_json     = EXCLUDED.params_json,
        layout_json     = EXCLUDED.layout_json;

  RAISE NOTICE '0098: 17 distinct hud-bar panel_child rows inserted (panel_id=%)', v_panel_id;
END;
$$;

-- ── 4. Sanity assertions ──────────────────────────────────────────────────────

DO $$
DECLARE
  n_entities   int;
  n_kids       int;
  n_distinct   int;
BEGIN
  -- ≥16 distinct button/label entities for hud-bar controls (14 buttons + 2 labels = 16 new + 1 existing)
  SELECT COUNT(*) INTO n_entities
    FROM catalog_entity
    WHERE kind IN ('button','token')
      AND slug LIKE 'hud-bar-%'
      AND retired_at IS NULL;

  SELECT COUNT(*) INTO n_kids
    FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
    WHERE ce.slug = 'hud-bar';

  -- Distinct button_ref + label_ref across all panel_child rows
  SELECT COUNT(DISTINCT COALESCE(
    pc.params_json->>'button_ref',
    pc.params_json->>'label_ref',
    pc.params_json->>'sprite_ref'
  )) INTO n_distinct
    FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
    WHERE ce.slug = 'hud-bar';

  IF n_kids < 17 THEN
    RAISE EXCEPTION '0098: expected ≥17 panel_child rows for hud-bar, got %', n_kids;
  END IF;

  IF n_distinct < 17 THEN
    RAISE EXCEPTION '0098: expected ≥17 distinct refs across hud-bar children, got % (duplicate slug detected)', n_distinct;
  END IF;

  RAISE NOTICE '0098 OK: hud-bar-entities=% children=% distinct-refs=%', n_entities, n_kids, n_distinct;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM panel_child USING catalog_entity ce
--    WHERE panel_child.panel_entity_id = ce.id AND ce.slug = 'hud-bar';
--   DELETE FROM catalog_entity WHERE kind IN ('button','token') AND slug IN (
--     'hud-bar-build-residential-button','hud-bar-build-commercial-button',
--     'hud-bar-build-industrial-button','hud-bar-auto-toggle',
--     'hud-bar-budget-plus-button','hud-bar-budget-minus-button',
--     'hud-bar-budget-graph-button','hud-bar-map-button',
--     'hud-bar-pause-button','hud-bar-play-button',
--     'hud-bar-speed-1-button','hud-bar-speed-2-button',
--     'hud-bar-speed-3-button','hud-bar-speed-4-button',
--     'hud-bar-city-name-label','hud-bar-budget-readout-label'
--   );
