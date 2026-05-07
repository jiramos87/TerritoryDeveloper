-- 0105_seed_hud_bar_catalog_9_15.sql
--
-- TECH-23573 / game-ui-catalog-bake Stage 9.15
--
-- Seeds hud-bar as a published panel_detail with 19 panel_child rows carrying
-- semantic instance_slugs. Safe to run on empty catalog_entity (idempotent ON CONFLICT).
--
-- Child roster (19):
--   Left  (3): zoom-in, zoom-out, recenter
--   Center(8): city-name-label, auto-toggle, budget-plus, budget-minus,
--               budget-graph, map, budget-readout-label, pause
--   Right (8): speed-1, speed-2, speed-3, speed-4, speed-5, play,
--               build-residential, build-commercial
-- Total: 3 + 8 + 8 = 19

BEGIN;

-- ── 1. catalog_entity: hud-bar panel ─────────────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'hud-bar', 'HUD Bar')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 2. catalog_entity: child button entities (19 distinct slugs) ─────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES
  -- Left zone
  ('button', 'hud-bar-zoom-in-button',            'Zoom In'),
  ('button', 'hud-bar-zoom-out-button',           'Zoom Out'),
  ('button', 'hud-bar-recenter-button',           'Recenter'),
  -- Center zone
  ('token',  'hud-bar-city-name-label',           'City Name'),
  ('button', 'hud-bar-auto-toggle',               'AUTO Toggle'),
  ('button', 'hud-bar-budget-plus-button',        'Budget +'),
  ('button', 'hud-bar-budget-minus-button',       'Budget -'),
  ('button', 'hud-bar-budget-graph-button',       'Budget Graph'),
  ('button', 'hud-bar-map-button',                'Map'),
  ('token',  'hud-bar-budget-readout-label',      'Budget Readout'),
  ('button', 'hud-bar-pause-button',              'Pause'),
  -- Right zone
  ('button', 'hud-bar-speed-1-button',            'Speed 1'),
  ('button', 'hud-bar-speed-2-button',            'Speed 2'),
  ('button', 'hud-bar-speed-3-button',            'Speed 3'),
  ('button', 'hud-bar-speed-4-button',            'Speed 4'),
  ('button', 'hud-bar-speed-5-button',            'Speed 5'),
  ('button', 'hud-bar-play-button',               'Play'),
  ('button', 'hud-bar-build-residential-button',  'Build Residential'),
  ('button', 'hud-bar-build-commercial-button',   'Build Commercial')
ON CONFLICT (kind, slug) DO NOTHING;

-- ── 3. entity_version + panel_detail + publish ───────────────────────────────

DO $$
DECLARE
  v_panel_id      bigint;
  v_ver_id        bigint;
  v_ver_number    int;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE kind='panel' AND slug='hud-bar';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0105: hud-bar panel entity missing after INSERT';
  END IF;

  -- Pick version number: max existing + 1, or 1.
  SELECT COALESCE(MAX(version_number), 0) + 1 INTO v_ver_number
    FROM entity_version WHERE entity_id = v_panel_id;

  INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
  VALUES (
    v_panel_id, v_ver_number, 'published', '{}', '{}',
    '{"migration": "0105_seed_hud_bar_catalog_9_15", "event": "initial_seed"}'::jsonb
  )
  ON CONFLICT DO NOTHING
  RETURNING id INTO v_ver_id;

  -- If ON CONFLICT hit (already exists), fetch existing id.
  IF v_ver_id IS NULL THEN
    SELECT id INTO v_ver_id FROM entity_version
      WHERE entity_id=v_panel_id AND version_number=v_ver_number;
  END IF;

  -- panel_detail
  INSERT INTO panel_detail (entity_id, layout_template, layout, gap_px, padding_json, params_json)
  VALUES (
    v_panel_id, 'hstack', 'hstack', 8,
    '{"top":4,"left":8,"right":8,"bottom":4}'::jsonb,
    '{}'::jsonb
  )
  ON CONFLICT (entity_id) DO NOTHING;

  -- Publish
  UPDATE catalog_entity SET current_published_version_id = v_ver_id
    WHERE id = v_panel_id AND current_published_version_id IS NULL;

  -- ── 4. panel_child rows (19) ─────────────────────────────────────────────

  -- Clear existing rows first (idempotent re-seed).
  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  -- Left zone (ord 1-3)
  INSERT INTO panel_child (panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, instance_slug, params_json, layout_json)
  VALUES
    (v_panel_id, v_ver_id, 'main', 1, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-zoom-in-button'),
     'hud-bar-zoom-in-button',
     '{"kind":"illuminated-button","icon":"zoom-in"}'::jsonb, '{"zone":"left"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 2, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-zoom-out-button'),
     'hud-bar-zoom-out-button',
     '{"kind":"illuminated-button","icon":"zoom-out"}'::jsonb, '{"zone":"left"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 3, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-recenter-button'),
     'hud-bar-recenter-button',
     '{"kind":"illuminated-button","icon":"recenter"}'::jsonb, '{"zone":"left"}'::jsonb),

  -- Center zone (ord 4-11)
    (v_panel_id, v_ver_id, 'main', 4, 'label',
     (SELECT id FROM catalog_entity WHERE kind='token' AND slug='hud-bar-city-name-label'),
     'hud-bar-city-name-label',
     '{"kind":"label"}'::jsonb, '{"zone":"center"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 5, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-auto-toggle'),
     'hud-bar-auto-toggle',
     '{"kind":"illuminated-button","icon":"auto","label":"AUTO"}'::jsonb, '{"zone":"center"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 6, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-plus-button'),
     'hud-bar-budget-plus-button',
     '{"kind":"illuminated-button","icon":"budget-plus","label":"+"}'::jsonb, '{"zone":"center"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 7, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-minus-button'),
     'hud-bar-budget-minus-button',
     '{"kind":"illuminated-button","icon":"budget-minus","label":"-"}'::jsonb, '{"zone":"center"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 8, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-budget-graph-button'),
     'hud-bar-budget-graph-button',
     '{"kind":"illuminated-button","icon":"budget-graph"}'::jsonb, '{"zone":"center"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 9, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-map-button'),
     'hud-bar-map-button',
     '{"kind":"illuminated-button","icon":"map","label":"MAP"}'::jsonb, '{"zone":"center"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 10, 'label',
     (SELECT id FROM catalog_entity WHERE kind='token' AND slug='hud-bar-budget-readout-label'),
     'hud-bar-budget-readout-label',
     '{"kind":"label"}'::jsonb, '{"zone":"center"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 11, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-pause-button'),
     'hud-bar-pause-button',
     '{"kind":"illuminated-button","icon":"pause"}'::jsonb, '{"zone":"center"}'::jsonb),

  -- Right zone (ord 12-19)
    (v_panel_id, v_ver_id, 'main', 12, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-1-button'),
     'hud-bar-speed-1-button',
     '{"kind":"illuminated-button","icon":"speed-1","label":"1x"}'::jsonb, '{"zone":"right"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 13, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-2-button'),
     'hud-bar-speed-2-button',
     '{"kind":"illuminated-button","icon":"speed-2","label":"2x"}'::jsonb, '{"zone":"right"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 14, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-3-button'),
     'hud-bar-speed-3-button',
     '{"kind":"illuminated-button","icon":"speed-3","label":"3x"}'::jsonb, '{"zone":"right"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 15, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-4-button'),
     'hud-bar-speed-4-button',
     '{"kind":"illuminated-button","icon":"speed-4","label":"4x"}'::jsonb, '{"zone":"right"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 16, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-speed-5-button'),
     'hud-bar-speed-5-button',
     '{"kind":"illuminated-button","icon":"speed-5","label":"5x"}'::jsonb, '{"zone":"right"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 17, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-play-button'),
     'hud-bar-play-button',
     '{"kind":"illuminated-button","icon":"play"}'::jsonb, '{"zone":"right"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 18, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-build-residential-button'),
     'hud-bar-build-residential-button',
     '{"kind":"illuminated-button","icon":"build-residential"}'::jsonb, '{"zone":"right"}'::jsonb),

    (v_panel_id, v_ver_id, 'main', 19, 'button',
     (SELECT id FROM catalog_entity WHERE kind='button' AND slug='hud-bar-build-commercial-button'),
     'hud-bar-build-commercial-button',
     '{"kind":"illuminated-button","icon":"build-commercial"}'::jsonb, '{"zone":"right"}'::jsonb);

  RAISE NOTICE '0105: hud-bar seeded with 19 panel_child rows (panel_id=%)', v_panel_id;
END;
$$;

-- ── 5. Sanity assertions ──────────────────────────────────────────────────────

DO $$
DECLARE
  n_kids      int;
  n_with_slug int;
BEGIN
  SELECT COUNT(*) INTO n_kids
    FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
    WHERE ce.slug = 'hud-bar';

  SELECT COUNT(*) INTO n_with_slug
    FROM panel_child pc
    JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
    WHERE ce.slug = 'hud-bar'
      AND pc.instance_slug IS NOT NULL
      AND pc.instance_slug <> '';

  IF n_kids < 19 THEN
    RAISE EXCEPTION '0105: expected 19 panel_child rows for hud-bar, got %', n_kids;
  END IF;

  IF n_with_slug < 19 THEN
    RAISE EXCEPTION '0105: expected 19 rows with instance_slug for hud-bar, got %', n_with_slug;
  END IF;

  RAISE NOTICE '0105 OK: hud-bar children=% with-instance-slug=%', n_kids, n_with_slug;
END;
$$;

COMMIT;
