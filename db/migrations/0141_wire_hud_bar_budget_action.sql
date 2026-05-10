-- Migration: 0141_wire_hud_bar_budget_action.sql
-- Stage 7.0 Wave B3 — TECH-27090
-- Wire budget.open action onto hud-bar budget button (hud-bar-budget-button).
-- Replaces legacy action.budget-panel-toggle with budget.open to align with
-- BudgetPanelAdapter.RegisterActions (UiActionRegistry "budget.open" handler).
-- hud-bar-budget-readout-label entity exists but has no panel_child row in this slot;
-- wiring via the existing illuminated-button child (id=40) that owns the budget action.

UPDATE panel_child
SET params_json = jsonb_set(
  params_json,
  '{action}',
  '"budget.open"'
)
WHERE id = (
  SELECT pc.id
  FROM panel_child pc
  JOIN catalog_entity ce_child  ON ce_child.id  = pc.child_entity_id
  JOIN catalog_entity ce_panel  ON ce_panel.id  = pc.panel_entity_id
  WHERE ce_panel.slug  = 'hud-bar'
    AND ce_child.slug  = 'hud-bar-budget-button'
  LIMIT 1
);
