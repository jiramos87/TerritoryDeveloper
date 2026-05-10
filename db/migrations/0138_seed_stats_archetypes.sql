-- Migration: 0138_seed_stats_archetypes.sql
-- Stage 6.0 Wave B2 — TECH-27083
-- 5 new archetype rows: tab-strip, chart, range-tabs, stacked-bar-row, service-row.

INSERT INTO catalog_archetype (kind, label, description, params_json)
VALUES
  (
    'tab-strip',
    'Tab Strip',
    'Horizontal tab bar — N tabs + active-tab bind. Aliases to illuminated-button group per bake coverage alias map.',
    '{"tabCount":3,"activeTabBindId":"","variant":"default"}'
  ),
  (
    'chart',
    'Line Chart',
    'Read-only line-series chart with range bind + axis labels. Aliases to themed-label for display-only stub.',
    '{"seriesId":"","bindId":"","axisLabels":[],"tabGroup":""}'
  ),
  (
    'range-tabs',
    'Range Tabs',
    'Time-range chip selector (3mo / 12mo / all-time) with range bind. Aliases to illuminated-button group.',
    '{"options":["3mo","12mo","all-time"],"bindId":"","variant":"chip"}'
  ),
  (
    'stacked-bar-row',
    'Stacked Bar Row',
    'Segmented horizontal data bar — label + value + segment-color list. Aliases to segmented-readout.',
    '{"bindId":"","tabGroup":"","segments":[]}'
  ),
  (
    'service-row',
    'Service Row',
    'Icon + label + value bind row for services tab. Aliases to themed-label for stub display.',
    '{"icon":"","bindId":"","tabGroup":"services"}'
  )
ON CONFLICT (kind) DO NOTHING;
