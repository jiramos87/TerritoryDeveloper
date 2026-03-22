# AI Agent Prompt — Load Game: Building vs Terrain Sorting Order (BUG-34, completed)

**Follow-up:** multi-cell footprint sorting on load — **[BUG-35](../BACKLOG.md)** (see root `BACKLOG.md`).

## Goal

Fix incorrect **sprite sorting order** after **Load Game** so **buildings** (and zone buildings) render **above** adjacent **terrain / grass** tiles, matching **New Game** behavior. Align **open water** and **lake/coast shore** tiles with **runtime** sorting formulas where snapshot drift caused water vs slope issues.

## Context

- **FEAT-37c** snapshot save/load: `CellData.sortingOrder` is still persisted for most cells; load **does not** call `GeographyManager.ReCalculateSortingOrderBasedOnHeight` globally (by design).
- **Implemented behavior (BUG-34)**:
  - **`GridManager.SortCellDataForVisualRestore`**: deterministic restore order — water → grass/shore/slope → RCI zoning overlays → roads → building pivots/singles → multi-cell footprint non-pivots (tie-break `y`, then `x`).
  - **Open water** (`RestoreGridCellVisuals` Water branch): `SpriteRenderer.sortingOrder` from `TerrainManager.CalculateTerrainSortingOrder(x, y, visualSurfaceHeight)` (same idea as `WaterManager` when placing water), not raw `cellData.sortingOrder`.
  - **Shore / water-slope** (`TerrainManager.RestoreWaterShorePrefabsFromSave`): `PlaceWaterShore` assigns per-sprite orders; **saved** primary sort is **not** applied over those sprites (legacy parameter kept for API compatibility).
  - **`ZoneManager.PlaceZoneBuildingTile`**: passes **`buildingSize`** into `SetZoneBuildingSortingOrder` so multi-cell RCI uses footprint max + caps.
  - **Post-pass** `RecalculateBuildingSortingAfterLoad`: re-runs `GridSortingOrderService.SetZoneBuildingSortingOrder` on every pivot building (and single-cell buildings) so neighbor `GetCellMaxContentSortingOrder` matches a fully restored grid.

## Related backlog

- **BUG-34** — completed (2026-03-22).
- **BUG-35** — multi-cell footprint grass vs building on load (active).
- **BUG-20** — verify after **BUG-35**; overlaps utilities / multi-cell.

## Specs (read first)

- [`.cursor/specs/isometric-geography-system.md`](../.cursor/specs/isometric-geography-system.md) — isometric depth, height, sorting conventions.
- [`.cursor/specs/water-system-refactor.md`](../.cursor/specs/water-system-refactor.md) — FEAT-37c save/load notes.
- [`ARCHITECTURE.md`](../ARCHITECTURE.md) — Persistence, Water, dependency map.

## Code map (post-fix)

1. **`GridManager`**: `GetCellDataRestoreVisualPhase`, `SortCellDataForVisualRestore`, `RestoreGrid`, `FindBuildingRootOnPivotCell`, `RecalculateBuildingSortingAfterLoad`; Inspector **`restoreGridDebugLogs`** for summary + sample pivot order deltas.
2. **`ZoneManager.PlaceZoneBuildingTile`** — `SetZoneBuildingSortingOrder(..., buildingSize)`.
3. **`TerrainManager.RestoreWaterShorePrefabsFromSave`** — delegates sorting to `PlaceWaterShore`.
4. **`GridSortingOrderService.SetZoneBuildingSortingOrder`** — footprint + front cap logic unchanged.

## Manual test checklist

- New Game → place multi-cell RCI, power plant, water near slopes/shores → Save → Load → compare sorting to pre-save.
- Enable **`restoreGridDebugLogs`** on `GridManager` to inspect one-line summary and optional pivot delta samples.

## Constraints

- Keep **English** in code/comments/docs.
- Prefer **targeted** building + water/shore fixes over global `ReCalculateSortingOrderBasedOnHeight` on load.
