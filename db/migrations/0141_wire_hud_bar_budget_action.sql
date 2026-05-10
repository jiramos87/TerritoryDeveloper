-- Migration: 0141_wire_hud_bar_budget_action.sql
-- Stage 7.0 Wave B3 — TECH-27090
-- Wire budget.open action onto hud-bar budget button (hud-bar-budget-button).
-- Uses actual schema: panel_entity_id / child_entity_id joins via catalog_entity slug.

UPDATE panel_child pc
SET params_json = jsonb_set(
  pc.params_json,
  '{action}',
  '"budget.open"'
)
FROM catalog_entity ce_panel
WHERE pc.panel_entity_id = ce_panel.id
  AND ce_panel.slug = 'hud-bar'
  AND pc.instance_slug = 'hud-bar-budget-button';
