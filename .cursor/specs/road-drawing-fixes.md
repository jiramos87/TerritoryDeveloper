# Road Drawing Fixes — Spec

## Overview

Manual road drawing has multiple bugs affecting terrain interaction, existing infrastructure preservation, zoning integrity, and visual consistency between preview and built roads. This spec organizes the fixes into incremental phases ordered by severity and dependency.

**Related specs:** `bridge-and-junction-fixes.md` (completed), `interstate-prefab-and-pathfinding-fixes.md` (Phase 1.1 elbow fix done).

## Related Files

| File | Role |
|------|------|
| `Assets/Scripts/Managers/GameManagers/RoadManager.cs` | Road drawing lifecycle: input handling, preview, placement, prefab refresh |
| `Assets/Scripts/Managers/GameManagers/TerrainManager.cs` | Terrain tile placement, slope detection, `RestoreTerrainForCell`, `PlaceSlopeFromPrefab`, `CanPlaceRoad` |
| `Assets/Scripts/Managers/GameManagers/TerraformingService.cs` | Path-level terraform plan computation, diagonal expansion |
| `Assets/Scripts/Managers/GameManagers/PathTerraformPlan.cs` | Terraform plan Apply/Revert with 3-phase neighbor refresh |
| `Assets/Scripts/Managers/GameManagers/RoadPrefabResolver.cs` | Prefab selection for path-based (`ResolveForPath`) and single-cell (`ResolveForCell`) contexts |
| `Assets/Scripts/Managers/GameManagers/GridPathfinder.cs` | A* pathfinding for road routes |
| `Assets/Scripts/Managers/GameManagers/GridManager.cs` | Grid operations, road cache, `FindPath` delegation |
| `Assets/Scripts/Utilities/RoadPathCostConstants.cs` | Shared cost constants for road pathfinding |

## Road Drawing Pipeline (Reference)

1. **Click** → `HandleRoadDrawing`: sets `startPosition`, `isDrawingRoad = true`
2. **Drag** (each frame) → `GetLine()` (A* pathfinding) → `DrawPreviewLine(path)`:
   - `ClearPreview(false)` — reverts previous terraform plan
   - Filter path for valid cells, check adjacency
   - `StraightenBridgeSegments` + `IsBridgePathValid`
   - `ExpandDiagonalStepsToCardinal` — convert diagonal steps to cardinal pairs
   - `ComputePathPlan` — analyze terrain, decide flatten/slope actions per cell
   - `plan.Apply()` — modify heightmap + terrain visuals (3 phases)
   - `ResolveForPath` — select prefab + world position per cell using plan's `postTerraformSlopeType`
   - Instantiate preview tiles
3. **Release** → `DrawRoadLine()`:
   - Uses `previewResolvedTiles` from last preview
   - `PlaceRoadTileFromResolved` for each tile
   - `UpdateAdjacentRoadPrefabsAt` for each tile
   - `ClearPreview(true)` — keeps terraform, destroys preview GameObjects

---

## Phase 1 — Protect existing infrastructure from destruction

**Priority**: P0 — Data loss and game state corruption.

### Problem 1.1: Zoning visually disappears when drawing roads nearby — FIXED

**Symptom**: Drawing a road near zoned areas causes adjacent zones to visually disappear. The zoning data in `ZoneManager` lists becomes stale (positions still tracked but tiles gone).

**Original hypothesis**: `PlaceSlopeFromPrefab` used `DestroyCellChildren` which destroys all children. Fix: use `DestroyTerrainChildrenOnly` (implemented).

**Actual root cause**: Phase 3 neighbor refresh calls `RestoreTerrainForCell` on zoned cells. `PlaceFlatTerrain` was adding grass tiles on top of zoning every frame during preview drag. Grass prefabs without `Zone.Grass` were not destroyed by `DestroyTerrainChildrenOnly`, so hundreds of grass clones accumulated and covered the zoning visually.

**Fix applied**: `RestoreTerrainForCell` now skips cells that have zoning overlays (`CellHasZoningOverlay`). Zoned cells are no longer refreshed during road preview. Also: `PlaceSlopeFromPrefab` uses `DestroyTerrainChildrenOnly` for consistency.

**Files**: `TerrainManager.cs` — `RestoreTerrainForCell()`, `CellHasZoningOverlay()`, `PlaceSlopeFromPrefab()`, `DestroyTerrainChildrenOnly()`

### Problem 1.2: Existing roads destroyed when new route crosses them — PARTIALLY FIXED

**Symptom**: Drawing a new road that crosses an existing road causes the existing road tiles (including bridge tiles) to be replaced with incorrect prefabs or disappear entirely.

**Implemented:** `RefreshAllAdjacentRoadsOutsidePath()` and `placementPathPositions` skip refreshing path cells during placement; final pass refreshes adjacent roads. **Pending:** Augment `IsPathNeighbor` with `IsRoadAt` in `ResolvePrefabForPathCell` so path cells at crossroads get correct prefabs.

**Root cause**: The A* pathfinder (`GridPathfinder.IsWalkable`) treats road cells as walkable, so computed paths routinely cross existing roads. `PlaceRoadTileFromResolved` unconditionally calls `DestroyPreviousRoadTile` on each path cell, destroying existing road tiles and replacing them with prefabs resolved only from path context (not existing connectivity).

The prefab selection for path cells uses `RoadPrefabResolver.ResolvePrefabForPathCell` which checks `IsPathNeighbor` — only looking at whether adjacent cells are in the **current path**, ignoring existing roads outside the path. So a crossroads becomes a straight road.

After placement, `UpdateAdjacentRoadPrefabsAt` refreshes adjacent cells but NOT the cell itself. The last cell in the path is never refreshed with full connectivity.

**Files**: `RoadManager.cs` — `PlaceRoadTileFromResolved()` (line ~864), `DrawRoadLine()` (line ~154), `UpdateAdjacentRoadPrefabs()` (line ~250); `RoadPrefabResolver.cs` — `ResolvePrefabForPathCell()` (line ~173), `IsPathNeighbor()` (line ~517)

**Fix approach**:
- In `ResolvePrefabForPathCell`, augment `IsPathNeighbor` checks with `IsRoadAt` checks to account for existing roads. The resolved prefab should reflect ALL connections (path + existing roads).
- After the placement loop in `DrawRoadLine`, add a final refresh pass over all placed cells using `RefreshRoadPrefabAt` (which uses full connectivity via `ResolveForCell`) to correct any remaining mismatches.
- For cells that already have a road and the new path crosses them: instead of destroy-and-replace, update the existing tile's prefab in-place (similar to `ReplaceRoadTileAt`).

### Problem 1.3: `RefreshRoadPrefabAt` leaks road cache entries — FIXED

**Symptom**: Over time, `GetAllRoadPositions()` returns fewer roads than actually exist. Affects road spacing in A* and any feature using the road cache.

**Root cause**: `RefreshRoadPrefabAt` calls `DestroyPreviousRoadTile` (which calls `RemoveRoadFromCache`) but never calls `AddRoadToCache` after instantiating the replacement tile.

**Fix applied**: `RefreshRoadPrefabAt` now calls `AddRoadToCache` at the end after instantiating the replacement tile.

**Files**: `RoadManager.cs` — `RefreshRoadPrefabAt()` (line ~371)

---

## Phase 2 — Fix preview-to-build visual consistency

**Priority**: P1 — Visual mismatch between preview and built road.

### Problem 2.1: `RefreshRoadPrefabAt` overrides plan-based prefabs with live terrain slope — PARTIALLY FIXED

**Symptom**: Cut-through routes (road through a hill) show correct flat road prefabs in preview, but after building, some tiles switch to slope road prefabs.

**Implemented:** `placementPathPositions` excludes path cells from refresh during placement; only adjacent roads outside the path are refreshed. **Pending:** `postTerraformSlopeType` not yet passed to refresh; edge cases may still occur.

**Root cause**: During `DrawRoadLine`, each tile placed triggers `UpdateAdjacentRoadPrefabsAt` which can refresh previously placed path tiles. `RefreshRoadPrefabAt` → `ResolveForCell` reads slope type from `GetTerrainSlopeTypeAt` (which computes from live heightmap neighbors). In a cut-through, path cells are at `baseHeight` but have non-path neighbors at `baseHeight + 1`. `GetTerrainSlopeTypeAt` detects the height difference and returns a slope type, causing `ResolveForCell` to select a slope road prefab instead of the flat one from the plan.

The effect is cascading: when tile `[i]` is placed, the adjacent refresh hits tile `[i-1]`, replacing its correct flat prefab with a slope prefab. This corrupts tiles from the end of the path backwards.

**Files**: `RoadManager.cs` — `DrawRoadLine()` (line ~154), `RefreshRoadPrefabAt()` (line ~283); `RoadPrefabResolver.cs` — `ResolveForCell()` (line ~93); `TerrainManager.cs` — `GetTerrainSlopeTypeAt()` (line ~1429)

**Fix approach**:
- During `DrawRoadLine`, collect the set of grid positions being placed in this operation.
- In `UpdateAdjacentRoadPrefabsAt`, skip refreshing cells that belong to the current placement batch.
- After ALL tiles are placed, do a single final refresh pass that considers full connectivity but uses the correct slope context. Two options:
  - (A) Pass the terraform plan's `postTerraformSlopeType` into the refresh (requires extending `RefreshRoadPrefabAt` with an optional override).
  - (B) Simpler: don't refresh path cells at all during placement. The resolved tiles from `ResolveForPath` already have the correct prefabs. Only refresh cells OUTSIDE the path (existing adjacent roads that need to update their connectivity).

### Problem 2.2: A* pathfinding runs on terraformed heightmap instead of original

**Symptom**: Preview route may flicker between different paths on consecutive frames. The path shown can differ from what the terraform plan actually processes.

**Root cause**: In `HandleRoadDrawing`, `GetLine()` (A*) runs BEFORE `DrawPreviewLine()` calls `ClearPreview(false)` to revert the previous terraform. So the A* sees the terraformed heightmap from the previous frame. Then `ClearPreview(false)` reverts, and the new terraform plan is computed on the original heightmap. On alternating frames, A* alternates between seeing original and terraformed terrain, potentially finding different routes.

**Files**: `RoadManager.cs` — `HandleRoadDrawing()` (line ~98), `DrawPreviewLine()` (line ~345)

**Fix approach**: Move the terraform revert before the A* call. Restructure the drag branch:
```
else if (isDrawingRoad && Input.GetMouseButton(0))
{
    ClearPreview(false);  // ← revert terraform FIRST
    List<Vector2> path = GetLine(startPosition, currentMousePosition);  // ← A* on original terrain
    DrawPreviewLineAfterClear(path);  // ← new method that skips internal ClearPreview
}
```
Or: extract the revert-only logic from `ClearPreview` so `DrawPreviewLine` can be called after manual revert.

---

## Phase 3 — Bridge validation and edge cases

**Priority**: P1 — Invalid bridge geometry accepted.

### Problem 3.1: Non-straight bridges accepted by validation — FIXED

**Symptom**: Bridge preview shows a "kinked" or "staircase" pattern over water instead of a straight bridge.

**Root cause**: `IsBridgePathValid` checked that Bresenham line cells are water/water-slope, but did NOT check that the bridge segment is purely horizontal or vertical. Diagonal crossings produced broken bridges.

**Fix applied**:
- `StraightenBridgeSegments`: diagonal water runs are aligned to the dominant axis; bridge extends to land so turns occur on land.
- `IsBridgePathValid`: rejects bridge segments that are not axis-aligned (entry and exit must share same X or same Y).
- `HasTurnOnWaterOrCoast`, `HasElbowTooCloseToWater`: elbows cannot be on water/water-slope; must be at least 2 cells from water.
- Same rules enforced for manual drawing, InterstateManager, and AutoRoadBuilder.

**Files**: `RoadManager.cs`, `InterstateManager.cs`

### Problem 3.2: `CanPlaceRoad` blocks mouse-up event, leaving drawing state stuck

**Symptom**: If the player releases the mouse over an invalid cell (water, building, incompatible slope), the road is never placed and the preview tiles remain visible as ghost objects in the scene. `isDrawingRoad` stays `true`.

**Root cause**: `CanPlaceRoad` check at the top of `HandleRoadDrawing` returns early for the entire method, including the `GetMouseButtonUp` handler. The drawing state and preview tiles are never cleaned up.

**Files**: `RoadManager.cs` — `HandleRoadDrawing()` (line ~98)

**Fix approach**: Move the `GetMouseButtonUp` and `GetMouseButtonUp(1)` (cancel) checks BEFORE the `CanPlaceRoad` guard, so input state transitions always complete:
```csharp
public void HandleRoadDrawing(Vector2 gridPosition)
{
    Vector2 pos = new Vector2((int)gridPosition.x, (int)gridPosition.y);

    // Always process release events to prevent stuck state
    if (Input.GetMouseButtonUp(0) && isDrawingRoad)
    {
        isDrawingRoad = false;
        DrawRoadLine(true);
        ClearPreview(true);
        uiManager?.RestoreGhostPreview();
        return;
    }
    if (Input.GetMouseButtonUp(1)) { /* cancel logic */ }

    if (!terrainManager.CanPlaceRoad((int)pos.x, (int)pos.y))
        return;

    // ... rest of click/drag logic
}
```

### Problem 3.3: Double diagonal expansion in `ComputePathPlan`

**Symptom**: Normally harmless (second expansion is a no-op), but creates a risk of index misalignment between `plan.pathCells` and the path used by `ResolveForPath`.

**Root cause**: `DrawPreviewLine` expands the path, then passes it to `ComputePathPlan` which expands again internally (line 125-126 of `TerraformingService.cs`).

**Files**: `TerraformingService.cs` — `ComputePathPlan()` (line ~120)

**Fix approach**: Remove the internal expansion from `ComputePathPlan` since all callers already expand before calling. Add a comment documenting the precondition.

### Problem 3.4: `CanPlaceRoad` not validated per-cell along the path

**Symptom**: Preview can show road on cells that `CanPlaceRoad` would reject (e.g., water slope cells that get penalized by A* but not blocked).

**Root cause**: `CanPlaceRoad` only checks the cursor position. `IsWalkable` in the pathfinder allows grass and road but doesn't match `CanPlaceRoad`'s slope/water-slope restrictions.

**Files**: `RoadManager.cs` — `HandleRoadDrawing()` (line ~100); `GridPathfinder.cs` — `IsWalkable()` (line ~145)

**Fix approach**: After `GetLine` returns a path, filter out cells where `CanPlaceRoad` returns false. If this creates gaps, reject the path or truncate it at the first invalid cell.

---

## Phase 4 — Deferred Destroy consistency

**Priority**: P2 — Subtle same-frame race conditions.

### Problem 4.1: `Destroy` vs `DestroyImmediate` in `DestroyPreviousRoadTile`

**Symptom**: During the same frame, stale road children (marked for `Destroy` but not yet removed) coexist with newly instantiated road tiles. `IsAnyChildRoad` and connectivity checks may find duplicates, leading to incorrect prefab selection.

**Root cause**: `DestroyPreviousRoadTile` uses `Destroy()` (deferred). Multiple tiles are placed and refreshed within the same frame in `DrawRoadLine`.

**Files**: `RoadManager.cs` — `DestroyPreviousRoadTile()` (line ~946)

**Fix approach**: Change `Destroy(t.go)` to `DestroyImmediate(t.go)` in `DestroyPreviousRoadTile`, matching the pattern already used in `TerrainManager.DestroyTerrainChildrenOnly`. Verify no code holds references to the destroyed objects after the call.

---

## Implementation Notes

### Testing Strategy
Each phase should be tested in Unity before moving to the next:
- **Phase 1**: Place zones adjacent to slopes, draw roads near them → zones must survive. Draw roads crossing existing roads → existing roads must keep correct connectivity. *(1.1 zoning fix verified)*
- **Phase 2**: Draw cut-through roads on hills → built road must match preview exactly. Rapidly drag road preview → no flickering between different routes.
- **Phase 3**: Draw road across water at an angle → must reject or show only straight bridge. Release mouse over water while drawing → must cancel cleanly. *(3.1 bridge geometry verified)*
- **Phase 4**: Draw complex intersections → all tiles must have correct prefabs immediately after placement.

### Dependency Order
- Phase 1 has no dependencies and is the most critical.
- Phase 2 depends on Phase 1 (the `PlaceSlopeFromPrefab` fix ensures terrain refresh doesn't corrupt data during the placement flow).
- Phase 3 items are independent of each other and can be done in any order.
- Phase 4 is a polish pass that benefits from all prior fixes being in place.
