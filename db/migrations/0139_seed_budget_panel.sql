-- Migration: 0139_seed_budget_panel.sql
-- Stage 7.0 Wave B3 — TECH-27087
-- Seed budget-panel catalog_entity + panel_detail + 25 panel_child rows.
-- Rewritten for actual schema: display_name not label; entity_id not panel_slug;
-- panel_child uses panel_entity_id / child_kind / instance_slug / order_idx.

BEGIN;

-- catalog_entity
INSERT INTO catalog_entity (slug, kind, display_name)
VALUES ('budget-panel', 'panel', 'Budget Panel')
ON CONFLICT (kind, slug) DO NOTHING;

DO $$
DECLARE
  v_panel_id bigint;
BEGIN
  SELECT id INTO v_panel_id FROM catalog_entity WHERE slug = 'budget-panel' AND kind = 'panel';
  IF v_panel_id IS NULL THEN
    RAISE EXCEPTION '0139: budget-panel entity missing after INSERT';
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
    '{"width":760,"height":600,"quadrants":["tax-rates","service-funding","expenses","forecast"]}'::jsonb,
    '{"anchor_min":[0.5,0.5],"anchor_max":[0.5,0.5],"pivot":[0.5,0.5],"size_delta":[760,600],"anchored_position":[0,0]}'::jsonb,
    now()
  )
  ON CONFLICT (entity_id) DO NOTHING;

  -- Wipe stale rows
  DELETE FROM panel_child WHERE panel_entity_id = v_panel_id;

  -- 1: header label
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'header', 1, 'label', 'budget-header',
    '{"kind":"themed-label","variant":"modal-title","bindId":"budget.title"}'::jsonb);

  -- 2: close button
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'close', 2, 'button', 'budget-close',
    '{"kind":"themed-button","variant":"icon-close","actionId":"budget.close"}'::jsonb);

  -- 3: Tax Rates section header
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'section-tax', 3, 'section-header', 'budget-section-tax',
    '{"kind":"section-header","quadrant":"tax-rates"}'::jsonb);

  -- 4: Service Funding section header
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'section-funding', 4, 'section-header', 'budget-section-funding',
    '{"kind":"section-header","quadrant":"service-funding"}'::jsonb);

  -- 5: Expenses section header
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'section-expenses', 5, 'section-header', 'budget-section-expenses',
    '{"kind":"section-header","quadrant":"expenses"}'::jsonb);

  -- 6: Forecast section header
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'section-forecast', 6, 'section-header', 'budget-section-forecast',
    '{"kind":"section-header","quadrant":"forecast"}'::jsonb);

  -- 7: Residential tax slider
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'tax-residential', 7, 'slider-row-numeric', 'budget-tax-residential',
    '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.residential","actionId":"taxRate.set","actionPayload":{"zone":"residential"}}'::jsonb);

  -- 8: Commercial tax slider
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'tax-commercial', 8, 'slider-row-numeric', 'budget-tax-commercial',
    '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.commercial","actionId":"taxRate.set","actionPayload":{"zone":"commercial"}}'::jsonb);

  -- 9: Industrial tax slider
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'tax-industrial', 9, 'slider-row-numeric', 'budget-tax-industrial',
    '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.industrial","actionId":"taxRate.set","actionPayload":{"zone":"industrial"}}'::jsonb);

  -- 10: General tax slider
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'tax-general', 10, 'slider-row-numeric', 'budget-tax-general',
    '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.general","actionId":"taxRate.set","actionPayload":{"zone":"general"}}'::jsonb);

  -- 11: Power funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-power', 11, 'expense-row', 'budget-fund-power',
    '{"kind":"expense-row","icon":"power","bindId":"budget.funding.power"}'::jsonb);

  -- 12: Water funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-water', 12, 'expense-row', 'budget-fund-water',
    '{"kind":"expense-row","icon":"water","bindId":"budget.funding.water"}'::jsonb);

  -- 13: Waste funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-waste', 13, 'expense-row', 'budget-fund-waste',
    '{"kind":"expense-row","icon":"waste","bindId":"budget.funding.waste"}'::jsonb);

  -- 14: Police funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-police', 14, 'expense-row', 'budget-fund-police',
    '{"kind":"expense-row","icon":"police","bindId":"budget.funding.police"}'::jsonb);

  -- 15: Fire funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-fire', 15, 'expense-row', 'budget-fund-fire',
    '{"kind":"expense-row","icon":"fire","bindId":"budget.funding.fire"}'::jsonb);

  -- 16: Health funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-health', 16, 'expense-row', 'budget-fund-health',
    '{"kind":"expense-row","icon":"health","bindId":"budget.funding.health"}'::jsonb);

  -- 17: Education funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-education', 17, 'expense-row', 'budget-fund-education',
    '{"kind":"expense-row","icon":"education","bindId":"budget.funding.education"}'::jsonb);

  -- 18: Parks funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-parks', 18, 'expense-row', 'budget-fund-parks',
    '{"kind":"expense-row","icon":"parks","bindId":"budget.funding.parks"}'::jsonb);

  -- 19: Transit funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-transit', 19, 'expense-row', 'budget-fund-transit',
    '{"kind":"expense-row","icon":"transit","bindId":"budget.funding.transit"}'::jsonb);

  -- 20: Roads funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-roads', 20, 'expense-row', 'budget-fund-roads',
    '{"kind":"expense-row","icon":"roads","bindId":"budget.funding.roads"}'::jsonb);

  -- 21: Maintenance funding
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'fund-maintenance', 21, 'expense-row', 'budget-fund-maintenance',
    '{"kind":"expense-row","icon":"maintenance","bindId":"budget.funding.maintenance"}'::jsonb);

  -- 22: Treasury readout
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'readout-treasury', 22, 'readout-block', 'budget-readout-treasury',
    '{"kind":"readout-block","bindId":"budget.treasury","deltaColorRule":"positive-green"}'::jsonb);

  -- 23: Projected balance readout
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'readout-projected', 23, 'readout-block', 'budget-readout-projected',
    '{"kind":"readout-block","bindId":"budget.forecast.balance","deltaColorRule":"positive-green"}'::jsonb);

  -- 24: Forecast chart
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'forecast-chart', 24, 'chart', 'budget-forecast-chart',
    '{"kind":"chart","seriesId":"forecast","bindId":"budget.forecast.chart","axisLabels":["Month 1","Month 2","Month 3"]}'::jsonb);

  -- 25: Range tabs
  INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, instance_slug, params_json)
  VALUES (v_panel_id, 'range-tabs', 25, 'range-tabs', 'budget-range-tabs',
    '{"kind":"range-tabs","options":["1mo","3mo","6mo"],"bindId":"budget.range"}'::jsonb);

  RAISE NOTICE '0139 OK: budget-panel seeded with 25 children (panel_id=%)', v_panel_id;
END;
$$;

COMMIT;
