# Road Drawing Fixes — Spec

## Overview

Manual road drawing has multiple bugs affecting terrain interaction, existing infrastructure preservation, zoning integrity, and visual consistency between preview and built roads. This spec organizes the fixes into incremental phases ordered by severity and dependency.

**Related specs:** `bridge-and-junction-fixes.md` (completed), `interstate-prefab-and-pathfinding-fixes.md` (Phases 1–3 + pathfinding; see BACKLOG BUG-27/BUG-29).

## BUG-25 — Spec task status (2026-03-19)

**BUG-25** is **completed** in [BACKLOG.md](../../BACKLOG.md) (2026-03-19). **Implementation for this document’s core scope is complete:**

| Area | Status |
|------|--------|
| Shared route validation, longest valid prefix, Phase 1 height check | Done |
| Preview pipeline: revert → A*, build revalidation | Done |
| Bridge geometry / approach rules | Done |
| Slope climb vs carve; gorge-beside-slope P1–P3, P5 | Done |
| Optional polish (not blocking BUG-25 closure) | Open: **1.2** crossroads/`IsRoadAt` pass, **2.1** `postTerraformSlopeType` on refresh, **3.3** expansion precondition, **4.1** `DestroyImmediate` |

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

## Slope climb vs carve (manual / streets)

When the path has **no** consecutive land cells with `|Δh| > 1` (`preferSlopeClimb` in `TerraformingService.ComputePathPlan`), **ascending** steps (`h_curr == h_prev + 1` on land) no longer use `Flatten` to `baseHeight` on orthogonal (and related) slope tiles—terraform stays `None` and `postTerraformSlopeType` follows road direction so the road **rides the slope** instead of digging a cut-through trench.

### Gorge-beside-slope mitigations (P1–P3, P5)

| Item | Behavior |
|------|----------|
| **P1** | `ExpandAdjacentFlattenCellsRecursively` runs only if **not** (`preferSlopeClimb` with **no** Flatten on path or pre-expansion `adjacentCells`). Avoids recursive flatten pulling gorges into the corridor when slope-climb mode scheduled no digs. |
| **P2** | After main path loop (non–cut-through), `InvalidatePlanIfPathBesideSteepLandCliff`: each **land** path cell’s **cardinal** neighbors **off** the path must differ in height by at most 1 vs that cell on the **current** heightmap (reject gorge beside corridor before expansion). |
| **P3** | `PathTerraformPlan.ValidateNoHeightDiffGreaterThanOne` adds a **one-ring** cardinal expansion of planned cells so Phase 1 validation sees cliff edges just outside the plan. |
| **P5** | `TerraformingService.LogTerraformPlanDiagnostics` (static): when `true`, logs plan validity, cut-through, flatten counts after `ComputePathPlan`. |

## Shared route validation (P0)

All persistent road placement that uses terraforming should go through **`RoadManager.TryPrepareRoadPlacementPlan`** (bridge straightening, adjacency, `ComputePathPlan`, optional `RoadPathValidationContext.forbidCutThrough`, **`PathTerraformPlan.TryValidatePhase1Heights`** so prep matches `Apply` Phase 1 rules):

- **Manual draw:** preview and mouse-up use **`TryPrepareRoadPlacementPlanLongestValidPrefix`** with `forbidCutThrough: false` so the preview stops at the last buildable cell when the full A* path would need invalid terraform (avoids crater previews). Placement uses the same helper + shared `manualRoadLongestPrefixHint` across drag and release.
- **Interstate:** `ValidateInterstatePathForPlacement` / `PlaceInterstateFromPath` use `TryPrepareRoadPlacementPlan` with `forbidCutThrough: true` (full path only; no cut-through trenches).
- **Auto road:** `AutoRoadBuilder.TryGetStreetPlacementPlan` uses **`TryPrepareRoadPlacementPlanLongestValidPrefix`** (hint 0) when `RoadManager` is present so partial segments can still build.

## Road Drawing Pipeline (Reference)

1. **Click** → `HandleRoadDrawing`: sets `startPosition`, `currentDrawCursorGrid`, `isDrawingRoad = true`
2. **Drag** (each frame) → `ClearPreview(false)` **first** (revert preview terraform) → `GetLine()` (A* on **original** heightmap) → `DrawPreviewLineCore(path)`:
   - `TryPrepareRoadPlacementPlanLongestValidPrefix(..., forbidCutThrough: false)` (stops at last valid prefix; `TryValidatePhase1Heights` inside prep)
   - `plan.Apply()` — preview terraform + terrain visuals
   - `ResolveForPath` — prefabs + world positions
   - Instantiate preview tiles
3. **Release** → handled **before** `CanPlaceRoad` guard so state never sticks:
   - `ClearPreview(false)` — revert preview
   - `TryFinalizeManualRoadPlacement()` — **`TryPrepareRoadPlacementPlanLongestValidPrefix`** (same hint as drag) + afford check + `Apply` + place tiles (`ResolveForPath` after apply)
   - `ClearPreview(true)` — cleanup only

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

### Problem 2.2: A* pathfinding runs on terraformed heightmap instead of original — FIXED (P0)

**Symptom**: Preview route may flicker between different paths on consecutive frames. The path shown can differ from what the terraform plan actually processes.

**Root cause**: In `HandleRoadDrawing`, `GetLine()` (A*) ran BEFORE `ClearPreview(false)` reverted the previous terraform, so A* alternated between original and terraformed heightmaps.

**Fix applied**: Drag branch now calls `ClearPreview(false)` then `GetLine` then `DrawPreviewLineCore`. Mouse-up calls `TryFinalizeManualRoadPlacement()` after a full revert so build matches validation on the original map.

**Files**: `RoadManager.cs` — `HandleRoadDrawing`, `DrawPreviewLineCore`, `TryFinalizeManualRoadPlacement`, `TryPrepareRoadPlacementPlan`

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

### Problem 3.2: `CanPlaceRoad` blocks mouse-up event, leaving drawing state stuck — FIXED

**Symptom**: If the player releases the mouse over an invalid cell (water, building, incompatible slope), the road is never placed and the preview tiles remain visible as ghost objects in the scene. `isDrawingRoad` stays `true`.

**Root cause**: `CanPlaceRoad` check at the top of `HandleRoadDrawing` returned early for the entire method, including the `GetMouseButtonUp` handler.

**Fix applied**: Mouse release and cancel branches run **before** the `CanPlaceRoad` guard so drawing state and preview always clear. See `RoadManager.HandleRoadDrawing`.

**Files**: `RoadManager.cs` — `HandleRoadDrawing()`

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
