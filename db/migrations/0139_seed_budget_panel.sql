-- Migration: 0139_seed_budget_panel.sql
-- Stage 7.0 Wave B3 — TECH-27087
-- Seed budget-panel catalog_entity + panel_detail + 25 panel_child rows.
-- Pre-req: chart + range-tabs archetypes published by 0138_seed_stats_archetypes.sql (T6.0.2).

-- catalog_entity
INSERT INTO catalog_entity (slug, kind, label, description)
VALUES (
  'budget-panel',
  'panel',
  'Budget Panel',
  'City budget modal — 4 quadrants: Tax Rates / Service Funding / Expenses / Forecast. HUD-triggered modal, pauses sim.'
)
ON CONFLICT (slug) DO NOTHING;

-- panel_detail
INSERT INTO panel_detail (panel_slug, layout_template, params_json)
VALUES (
  'budget-panel',
  'modal-card',
  '{"width":760,"height":600,"quadrants":["tax-rates","service-funding","expenses","forecast"]}'
)
ON CONFLICT (panel_slug) DO NOTHING;

-- ── Header + close ────────────────────────────────────────────────────────────

-- 1: header label
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-header', 'themed-label', 'City Budget', 1,
  '{"kind":"themed-label","variant":"modal-title","bindId":"budget.title"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 2: close button
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-close', 'themed-button', 'Close', 2,
  '{"kind":"themed-button","variant":"icon-close","actionId":"budget.close"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- ── Section headers (4) ───────────────────────────────────────────────────────

-- 3: Tax Rates section header
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-section-tax', 'section-header', 'Tax Rates', 3,
  '{"kind":"section-header","quadrant":"tax-rates"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 4: Service Funding section header
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-section-funding', 'section-header', 'Service Funding', 4,
  '{"kind":"section-header","quadrant":"service-funding"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 5: Expenses section header
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-section-expenses', 'section-header', 'Expenses', 5,
  '{"kind":"section-header","quadrant":"expenses"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 6: Forecast section header
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-section-forecast', 'section-header', 'Forecast', 6,
  '{"kind":"section-header","quadrant":"forecast"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- ── Tax slider-row-numerics (4: R / C / I / general) ─────────────────────────

-- 7: Residential tax slider
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-tax-residential', 'slider-row-numeric', 'Residential', 7,
  '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.residential","actionId":"taxRate.set","actionPayload":{"zone":"residential"}}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 8: Commercial tax slider
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-tax-commercial', 'slider-row-numeric', 'Commercial', 8,
  '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.commercial","actionId":"taxRate.set","actionPayload":{"zone":"commercial"}}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 9: Industrial tax slider
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-tax-industrial', 'slider-row-numeric', 'Industrial', 9,
  '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.industrial","actionId":"taxRate.set","actionPayload":{"zone":"industrial"}}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 10: General tax slider
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-tax-general', 'slider-row-numeric', 'General', 10,
  '{"kind":"slider-row-numeric","numeric":true,"min":0,"max":50,"step":1,"bindId":"budget.tax.general","actionId":"taxRate.set","actionPayload":{"zone":"general"}}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- ── Service Funding expense-rows (11) ─────────────────────────────────────────

-- 11: Power funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-power', 'expense-row', 'Power', 11,
  '{"kind":"expense-row","icon":"power","bindId":"budget.funding.power"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 12: Water funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-water', 'expense-row', 'Water', 12,
  '{"kind":"expense-row","icon":"water","bindId":"budget.funding.water"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 13: Waste funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-waste', 'expense-row', 'Waste', 13,
  '{"kind":"expense-row","icon":"waste","bindId":"budget.funding.waste"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 14: Police funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-police', 'expense-row', 'Police', 14,
  '{"kind":"expense-row","icon":"police","bindId":"budget.funding.police"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 15: Fire funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-fire', 'expense-row', 'Fire', 15,
  '{"kind":"expense-row","icon":"fire","bindId":"budget.funding.fire"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 16: Health funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-health', 'expense-row', 'Health', 16,
  '{"kind":"expense-row","icon":"health","bindId":"budget.funding.health"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 17: Education funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-education', 'expense-row', 'Education', 17,
  '{"kind":"expense-row","icon":"education","bindId":"budget.funding.education"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 18: Parks funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-parks', 'expense-row', 'Parks', 18,
  '{"kind":"expense-row","icon":"parks","bindId":"budget.funding.parks"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 19: Transit funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-transit', 'expense-row', 'Transit', 19,
  '{"kind":"expense-row","icon":"transit","bindId":"budget.funding.transit"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 20: Roads funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-roads', 'expense-row', 'Roads', 20,
  '{"kind":"expense-row","icon":"roads","bindId":"budget.funding.roads"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 21: Maintenance funding
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-fund-maintenance', 'expense-row', 'Maintenance', 21,
  '{"kind":"expense-row","icon":"maintenance","bindId":"budget.funding.maintenance"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- ── Readout blocks (2) ────────────────────────────────────────────────────────

-- 22: Treasury readout
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-readout-treasury', 'readout-block', 'Treasury', 22,
  '{"kind":"readout-block","bindId":"budget.treasury","deltaColorRule":"positive-green"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 23: Projected balance readout
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-readout-projected', 'readout-block', 'Projected Balance', 23,
  '{"kind":"readout-block","bindId":"budget.forecast.balance","deltaColorRule":"positive-green"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- ── Chart + range-tabs (reused from Wave B2) ──────────────────────────────────

-- 24: Forecast chart (3-month projection)
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-forecast-chart', 'chart', 'Forecast Chart', 24,
  '{"kind":"chart","seriesId":"forecast","bindId":"budget.forecast.chart","axisLabels":["Month 1","Month 2","Month 3"]}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 25: Range tabs
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'budget-panel', 'budget-range-tabs', 'range-tabs', 'Range', 25,
  '{"kind":"range-tabs","options":["1mo","3mo","6mo"],"bindId":"budget.range"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;
