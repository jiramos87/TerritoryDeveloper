---
purpose: "Simulation domain — guardrails and spec pointers"
audience: agent
loaded_by: router
slices_via: none
description: Simulation domain — guardrails and spec pointers
globs:
  - "**/SimulationManager*.cs"
  - "**/AutoRoadBuilder*.cs"
  - "**/AutoZoningManager*.cs"
  - "**/AutoResourcePlanner*.cs"
  - "**/GrowthManager*.cs"
  - "**/GrowthBudgetManager*.cs"
  - "**/UrbanCentroidService*.cs"
alwaysApply: false
---

# Simulation Domain

**Deep spec:** `ia/specs/simulation-system.md`

## Guardrails

- NEVER re-enable `UrbanizationProposalManager` — obsolete (**glossary** **Urbanization proposal**)
- `UrbanCentroidService` and ring-based AUTO growth remain supported — NOT part of the obsolete system
- Tick order is strict — do not reorder steps in `ProcessSimulationTick()`
- AUTO roads use the same validation pipeline as manual: `PathTerraformPlan` + Phase-1 + `Apply`
- `AutoZoningManager` must skip cells in `GetRoadExtensionCells()` / `GetRoadAxialCorridorCells()` (BUG-47)
