-- Migration: 0140_seed_budget_archetypes.sql
-- Stage 7.0 Wave B3 — TECH-27088
-- 3 new archetype rows: slider-row-numeric, expense-row, readout-block.
-- chart + range-tabs reused from Wave B2 (0138_seed_stats_archetypes.sql).

INSERT INTO catalog_archetype (kind, label, description, params_json)
VALUES
  (
    'slider-row-numeric',
    'Slider Row (Numeric)',
    'Slider row with live numeric value readout left-aligned. Alias of slider-row + numeric=true flag. min/max/step from params; value-bind + actionId dispatch on change.',
    '{"numeric":true,"min":0,"max":100,"step":1,"bindId":"","actionId":"","variant":"default"}'
  ),
  (
    'expense-row',
    'Expense Row',
    'Icon + label + amount row for service funding / expense display. Aliases to segmented-readout for display-only stub.',
    '{"icon":"","label":"","bindId":"","amount":0}'
  ),
  (
    'readout-block',
    'Readout Block',
    'Label + value bind block with delta-color rule (positive-green / negative-red). Aliases to segmented-readout.',
    '{"bindId":"","deltaColorRule":"positive-green","label":""}'
  )
ON CONFLICT (kind) DO NOTHING;
