-- Migration: 0137_seed_stats_panel.sql
-- Stage 6.0 Wave B2 — TECH-27082
-- Seed stats-panel catalog_entity + panel_detail + 21 panel_child rows.
-- Rewritten for actual schema: display_name not label; entity_id not panel_slug;
-- panel_child uses panel_entity_id / child_kind / instance_slug / order_idx.

BEGIN;

-- Extend layout_template constraint to include modal-card (idempotent).
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

-- Extend panel_child child_kind constraint to include Wave B2/B3/B4 kinds (idempotent).
ALTER TABLE panel_child
  DROP CONSTRAINT IF EXISTS panel_child_child_kind_check;

ALTER TABLE panel_child
  ADD CONSTRAINT panel_child_child_kind_check
  CHECK (child_kind = ANY (ARRAY[
    'button'::text,
    'panel'::text,
    'label'::text,
    'spacer'::text,
    'audio'::text,
    'sprite'::text,
    'label_inline'::text,
    'row'::text,
    'text'::text,
    'confirm-button'::text,
    'view-slot'::text,
    'tab-strip'::text,
    'range-tabs'::text,
    'chart'::text,
    'stacked-bar-row'::text,
    'service-row'::text,
    'section-header'::text,
    'slider-row-numeric'::text,
    'expense-row'::text,
    'readout-block'::text
  ]));

-- catalog_entity
INSERT INTO catalog_entity (slug, kind, display_name)
VALUES ('stats-panel', 'panel', 'Stats Panel')
ON CONFLICT (kind, slug) DO NOTHING;

DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'stats-panel' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0137: stats-panel entity missing after INSERT';
  END IF;

  -- panel_detail
  INSERT INTO panel_detail (
    entity_id, layout_template, layout, modal,
    padding_json, gap_px, params_json, rect_json, updated_at
  )
  VALUES (
    v_panel_id, 'modal-card', 'vstack', true,
    '{"top":0,"left":0,"right":0,"bottom":0}'::jsonb,
    0,
    '{"width":720,"height":560,"defaultTab":"population"}'::jsonb,
    '{"anchor_min":[0.5,0.5],"anchor_max":[0.5,0.5],"pivot":[0.5,0.5],"size_delta":[720,560],"anchored_position":[0,0]}'::jsonb,
    now()
  )
  ON CONFLICT (entity_id) DO NOTHING;

  -- Wipe stale rows
  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  -- 1: header label
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'header', 1, 'label', 'stats-header',
    '{"kind":"themed-label","variant":"modal-title","bindId":"stats.title"}'::jsonb);

  -- 2: close button
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'close', 2, 'button', 'stats-close',
    '{"kind":"themed-button","variant":"icon-close","actionId":"stats.close"}'::jsonb);

  -- 3: tab-strip
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'tab-strip', 3, 'tab-strip', 'stats-tab-strip',
    '{"kind":"tab-strip","tabs":["population","services","economy"],"bindId":"stats.activeTab"}'::jsonb);

  -- 4: range-tabs
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'range-tabs', 4, 'range-tabs', 'stats-range-tabs',
    '{"kind":"range-tabs","options":["3mo","12mo","all-time"],"bindId":"stats.range"}'::jsonb);

  -- 5: population chart
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'chart-population', 5, 'chart', 'stats-chart-population',
    '{"kind":"chart","seriesId":"population","bindId":"stats.chart.population","tabGroup":"population"}'::jsonb);

  -- 6: services chart
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'chart-services', 6, 'chart', 'stats-chart-services',
    '{"kind":"chart","seriesId":"services","bindId":"stats.chart.services","tabGroup":"services"}'::jsonb);

  -- 7: economy chart
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'chart-economy', 7, 'chart', 'stats-chart-economy',
    '{"kind":"chart","seriesId":"economy","bindId":"stats.chart.economy","tabGroup":"economy"}'::jsonb);

  -- 8: population stacked-bar
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'bar-population', 8, 'stacked-bar-row', 'stats-bar-population',
    '{"kind":"stacked-bar-row","bindId":"stats.bar.population","tabGroup":"population"}'::jsonb);

  -- 9: services stacked-bar
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'bar-services', 9, 'stacked-bar-row', 'stats-bar-services',
    '{"kind":"stacked-bar-row","bindId":"stats.bar.services","tabGroup":"services"}'::jsonb);

  -- 10: economy stacked-bar
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'bar-economy', 10, 'stacked-bar-row', 'stats-bar-economy',
    '{"kind":"stacked-bar-row","bindId":"stats.bar.economy","tabGroup":"economy"}'::jsonb);

  -- 11-21: service-rows
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES
    (v_panel_id, 'svc-power',     11, 'service-row', 'stats-svc-power',     '{"kind":"service-row","icon":"power","bindId":"stats.svc.power","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-water',     12, 'service-row', 'stats-svc-water',     '{"kind":"service-row","icon":"water","bindId":"stats.svc.water","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-waste',     13, 'service-row', 'stats-svc-waste',     '{"kind":"service-row","icon":"waste","bindId":"stats.svc.waste","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-police',    14, 'service-row', 'stats-svc-police',    '{"kind":"service-row","icon":"police","bindId":"stats.svc.police","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-fire',      15, 'service-row', 'stats-svc-fire',      '{"kind":"service-row","icon":"fire","bindId":"stats.svc.fire","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-health',    16, 'service-row', 'stats-svc-health',    '{"kind":"service-row","icon":"health","bindId":"stats.svc.health","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-education', 17, 'service-row', 'stats-svc-education', '{"kind":"service-row","icon":"education","bindId":"stats.svc.education","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-parks',     18, 'service-row', 'stats-svc-parks',     '{"kind":"service-row","icon":"parks","bindId":"stats.svc.parks","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-transit',   19, 'service-row', 'stats-svc-transit',   '{"kind":"service-row","icon":"transit","bindId":"stats.svc.transit","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-roads',     20, 'service-row', 'stats-svc-roads',     '{"kind":"service-row","icon":"roads","bindId":"stats.svc.roads","tabGroup":"services"}'::jsonb),
    (v_panel_id, 'svc-happiness', 21, 'service-row', 'stats-svc-happiness', '{"kind":"service-row","icon":"happiness","bindId":"stats.svc.happiness","tabGroup":"services"}'::jsonb);

  RAISE NOTICE '0137 OK: stats-panel seeded with 21 children (panel_id=%)', v_panel_id;
END;
$$;

COMMIT;
