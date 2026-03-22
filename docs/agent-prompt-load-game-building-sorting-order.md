# AI Agent Prompt — Load Game: Building vs Terrain Sorting (BUG-34 & BUG-35, completed)

Reference for **save/load** cell visuals and **sprite sorting** after **Load Game**. Issues **[BUG-34](../BACKLOG.md)** and **[BUG-35](../BACKLOG.md)** are **completed** (2026-03-22).

## Goal (achieved)

- **BUG-34:** Buildings and utilities render with correct **depth** vs terrain/water/shores after load; deterministic restore order; building sort post-pass.
- **BUG-35:** No **flat grass** prefab left as a sibling under **RCI or utility** buildings (runtime and load); multi-cell footprint sorting remains consistent.

## Current behavior (save / load)

- **FEAT-37c:** `CellData.sortingOrder` is persisted; load **does not** call `GeographyManager.ReCalculateSortingOrderBasedOnHeight` globally (by design).
- **`GridManager.SortCellDataForVisualRestore`:** phases — water → grass/shore/slope → RCI zoning overlays → roads → building pivots/singles → multi-cell footprint non-pivots (tie-break `y`, then `x`).
- **Open water** (`RestoreGridCellVisuals` Water branch): `SpriteRenderer.sortingOrder` from `TerrainManager.CalculateTerrainSortingOrder(x, y, visualSurfaceHeight)` where applicable.
- **Shore / water-slope:** `TerrainManager.RestoreWaterShorePrefabsFromSave` / `PlaceWaterShore` own per-sprite orders.
- **`ZoneManager.PlaceZoneBuildingTile`:** passes **`buildingSize`** into `SetZoneBuildingSortingOrder` for multi-cell RCI.
- **Post-pass** `RecalculateBuildingSortingAfterLoad`: re-runs `GridSortingOrderService.SetZoneBuildingSortingOrder` on every pivot building so neighbor `GetCellMaxContentSortingOrder` matches a fully restored grid (**BUG-34**).
- **Flat grass under buildings (**BUG-35**):** `GridManager.DestroyCellChildren` accepts **`destroyFlatGrass`**. Default **`false`** (bulldozer/demolish preserve grass). When **`true`**, used by **`ZoneManager.PlaceZoneBuilding`**, **`PlaceZoneBuildingTile`**, and **`BuildingPlacementService.UpdateBuildingTilesAttributes`** so the building cell is not **grass + building** at once. Multi-cell **`SetZoneBuildingSortingOrder`** still calls **`SyncCellTerrainLayersBelowBuilding`** for each footprint cell for any remaining grass sprites.

## Related backlog

- **BUG-34** — completed (2026-03-22).
- **BUG-35** — completed (2026-03-22).
- **BUG-20** — may overlap utilities/multi-cell; re-verify if anything still mis-sorts.

## Specs (read first)

- [`.cursor/specs/isometric-geography-system.md`](../.cursor/specs/isometric-geography-system.md) — §7 Sorting Order, **§7.4 Save / Load Game**.
- [`.cursor/specs/water-system-refactor.md`](../.cursor/specs/water-system-refactor.md) — FEAT-37c save/load notes.
- [`ARCHITECTURE.md`](../ARCHITECTURE.md) — Persistence, Water.

## Code map

1. **`GridManager`:** `GetCellDataRestoreVisualPhase`, `SortCellDataForVisualRestore`, `RestoreGrid`, `DestroyCellChildren(..., destroyFlatGrass)`, `RecalculateBuildingSortingAfterLoad`; Inspector **`restoreGridDebugLogs`**.
2. **`ZoneManager`:** `PlaceZoneBuilding`, `PlaceZoneBuildingTile`, `RestoreZoneTile`.
3. **`BuildingPlacementService`:** `LoadBuildingTile`, `RestoreBuildingTile`, `UpdateBuildingTilesAttributes`.
4. **`GridSortingOrderService`:** `SetZoneBuildingSortingOrder`, `SyncCellTerrainLayersBelowBuilding`.
5. **`TerrainManager.RestoreWaterShorePrefabsFromSave`** — shore sorting.

## Manual test checklist

- New Game → place multi-cell RCI, power plant, water near slopes/shores → Save → Load → confirm no grass/building z-fight on building cells; compare sorting to pre-save.
- Enable **`restoreGridDebugLogs`** on `GridManager` for optional pivot delta samples.

## Constraints

- Keep **English** in code/comments/docs.
- Prefer **targeted** building + water/shore fixes over global `ReCalculateSortingOrderBasedOnHeight` on load.
