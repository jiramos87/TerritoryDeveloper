-- Migration: 0137_seed_stats_panel.sql
-- Stage 6.0 Wave B2 — TECH-27082
-- Seed stats-panel catalog_entity + panel_detail + 21 panel_child rows.

-- catalog_entity
INSERT INTO catalog_entity (slug, kind, label, description)
VALUES (
  'stats-panel',
  'panel',
  'Stats Panel',
  'City statistics modal — 3 tabs: Population / Services / Economy. HUD-triggered modal, pauses sim.'
)
ON CONFLICT (slug) DO NOTHING;

-- panel_detail
INSERT INTO panel_detail (panel_slug, layout_template, params_json)
VALUES (
  'stats-panel',
  'modal-card',
  '{"width":720,"height":560,"defaultTab":"population"}'
)
ON CONFLICT (panel_slug) DO NOTHING;

-- panel_child rows (21 total)
-- 1: header label
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-header', 'themed-label', 'City Statistics', 1,
  '{"kind":"themed-label","variant":"modal-title","bindId":"stats.title"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 2: close button
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-close', 'themed-button', 'Close', 2,
  '{"kind":"themed-button","variant":"icon-close","actionId":"stats.close"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 3: tab-strip (Population / Services / Economy)
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-tab-strip', 'tab-strip', 'Stats Tabs', 3,
  '{"kind":"tab-strip","tabs":["population","services","economy"],"bindId":"stats.activeTab"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 4: range-tabs (3mo / 12mo / all-time)
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-range-tabs', 'range-tabs', 'Range', 4,
  '{"kind":"range-tabs","options":["3mo","12mo","all-time"],"bindId":"stats.range"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 5: population line-chart
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-chart-population', 'chart', 'Population Chart', 5,
  '{"kind":"chart","seriesId":"population","bindId":"stats.chart.population","tabGroup":"population"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 6: services line-chart
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-chart-services', 'chart', 'Services Chart', 6,
  '{"kind":"chart","seriesId":"services","bindId":"stats.chart.services","tabGroup":"services"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 7: economy line-chart
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-chart-economy', 'chart', 'Economy Chart', 7,
  '{"kind":"chart","seriesId":"economy","bindId":"stats.chart.economy","tabGroup":"economy"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 8: stacked-bar-row (population breakdown)
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-bar-population', 'stacked-bar-row', 'Population Breakdown', 8,
  '{"kind":"stacked-bar-row","bindId":"stats.bar.population","tabGroup":"population"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 9: stacked-bar-row (services saturation)
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-bar-services', 'stacked-bar-row', 'Services Saturation', 9,
  '{"kind":"stacked-bar-row","bindId":"stats.bar.services","tabGroup":"services"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 10: stacked-bar-row (economy breakdown)
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES (
  'stats-panel', 'stats-bar-economy', 'stacked-bar-row', 'Economy Breakdown', 10,
  '{"kind":"stacked-bar-row","bindId":"stats.bar.economy","tabGroup":"economy"}'
)
ON CONFLICT (panel_slug, slug) DO NOTHING;

-- 11–21: 11 service-rows (services tab)
INSERT INTO panel_child (panel_slug, slug, kind, label, sort_order, params_json)
VALUES
  ('stats-panel', 'stats-svc-power',      'service-row', 'Power',        11, '{"kind":"service-row","icon":"power","bindId":"stats.svc.power","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-water',      'service-row', 'Water',        12, '{"kind":"service-row","icon":"water","bindId":"stats.svc.water","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-waste',      'service-row', 'Waste',        13, '{"kind":"service-row","icon":"waste","bindId":"stats.svc.waste","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-police',     'service-row', 'Police',       14, '{"kind":"service-row","icon":"police","bindId":"stats.svc.police","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-fire',       'service-row', 'Fire',         15, '{"kind":"service-row","icon":"fire","bindId":"stats.svc.fire","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-health',     'service-row', 'Health',       16, '{"kind":"service-row","icon":"health","bindId":"stats.svc.health","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-education',  'service-row', 'Education',    17, '{"kind":"service-row","icon":"education","bindId":"stats.svc.education","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-parks',      'service-row', 'Parks',        18, '{"kind":"service-row","icon":"parks","bindId":"stats.svc.parks","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-transit',    'service-row', 'Transit',      19, '{"kind":"service-row","icon":"transit","bindId":"stats.svc.transit","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-roads',      'service-row', 'Roads',        20, '{"kind":"service-row","icon":"roads","bindId":"stats.svc.roads","tabGroup":"services"}'),
  ('stats-panel', 'stats-svc-happiness',  'service-row', 'Happiness',    21, '{"kind":"service-row","icon":"happiness","bindId":"stats.svc.happiness","tabGroup":"services"}')
ON CONFLICT (panel_slug, slug) DO NOTHING;
