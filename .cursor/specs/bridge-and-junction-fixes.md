# Bridge Disappearing & Junction Update Bugs — Analysis & Fixes

**Status: COMPLETED** — All fixes implemented and verified (PlaceWaterSlope, IsBayObject, RefreshAllAdjacentRoadsOutsidePath).

---

## Bug 1: Bridge prefabs disappear on coastal cells (height 1) when drawing new routes

### Root cause

`PlaceWaterSlope` in `TerrainManager.cs` calls `DestroyCellChildren(cell)`, which destroys **all** children of the cell, including the road (bridge) tile.

**Flow:**
1. User draws a new road near an existing bridge.
2. `PathTerraformPlan.Apply` Phase 3 (or `Revert` Phase 3) refreshes **neighbors** of path/adjacent cells.
3. Coastal bridge cells (height 1, SlopeWater/Bay terrain) are neighbors of the path.
4. `RestoreTerrainForCell` is called on those coastal cells.
5. For coastal cells, it calls `PlaceWaterSlope`.
6. `PlaceWaterSlope` calls `DestroyCellChildren(cell)` → **destroys the bridge road tile**.
7. It then places only the water slope terrain, so the road is gone.

Cells at height 0 (plain water) are skipped in Phase 3 because they are not "neighbors" in the same way, or the path does not trigger a refresh there. Coastal cells (height 1) are refreshed and thus hit by this bug.

### Fix

In `TerrainManager.PlaceWaterSlope`, replace `DestroyCellChildren(cell)` with `DestroyTerrainChildrenOnly(cell)` so roads (and forests, buildings) are preserved when updating water slope terrain.

---

## Bug 2: Existing road prefabs don't update when a new adjacent route is drawn

### Analysis

`UpdateAdjacentRoadPrefabsAt` should refresh existing road cells when a new road is placed next to them. With the current logic:

- `placementPathPositions` excludes path cells from refresh (correct for bridge/slope).
- Adjacent cells **not** in the path are refreshed.

Possible causes:

1. **Phase 3 terrain refresh** – `RestoreTerrainForCell` on junction cells uses `DestroyTerrainChildrenOnly`, which preserves roads. So terrain refresh alone should not remove roads.
2. **Refresh order** – Refreshes happen per placed tile. When the last tile is placed, its neighbors (including the junction cell) are refreshed, so connectivity should be correct.
3. **Bay prefabs** – `SouthWestBay` and similar bay prefabs are not in `IsWaterSlopeObject`. If `PlaceWaterSlope` is ever used for bay cells and still calls `DestroyCellChildren`, it would destroy roads there too. The main fix (using `DestroyTerrainChildrenOnly` in `PlaceWaterSlope`) addresses this as well.

### Additional fix for Bug 2

Add a **final refresh pass** after all tiles are placed: refresh every cell that is adjacent to the path and has a road, but is **not** in the path. This ensures junction prefabs are correct even if earlier per-tile refreshes missed some cases.

---

## Summary of changes (implemented)

| File | Change |
|------|--------|
| `TerrainManager.cs` | In `PlaceWaterSlope`, use `DestroyTerrainChildrenOnly(cell)` instead of `DestroyCellChildren(cell)` — **FIXED** |
| `TerrainManager.cs` | Add `IsBayObject` and include bay prefabs in `DestroyTerrainChildrenOnly` so coastal bay terrain is replaced correctly |
| `RoadManager.cs` | Add `RefreshAllAdjacentRoadsOutsidePath()` — final pass after placement to refresh all adjacent road cells not in the path — **FIXED** |
