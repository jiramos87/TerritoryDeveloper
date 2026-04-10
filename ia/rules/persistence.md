---
purpose: "Persistence domain — guardrails and spec pointers"
audience: agent
loaded_by: router
slices_via: none
description: Persistence domain — guardrails and spec pointers
globs:
  - "**/GameSaveManager*.cs"
  - "**/GameManager.cs"
  - "**/GameBootstrap*.cs"
  - "**/CellData*.cs"
alwaysApply: false
---

# Persistence Domain

**Deep spec:** `ia/specs/persistence-system.md`
**Geography spec:** `ia/specs/isometric-geography-system.md` §7.4, §11.5

## Guardrails

- Load restore order is strict — do not reorder (heightmap → water map → grid → water body sync)
- Load does NOT run global slope restoration or sorting recalculation
- Legacy saves without `waterMapData` must still work via fallback path
- Visual restore phase order must be stable: water → grass/shore/slope → RCI → roads → building pivots → multi-cell non-pivots
