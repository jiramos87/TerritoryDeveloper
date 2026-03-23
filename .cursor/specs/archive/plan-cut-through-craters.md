# BUG-29 — Cut-through craters: implementation summary

**Backlog:** [BUG-29](../../../BACKLOG.md) marked **completed** (2026-03-19).

> **Archived:** Historical implementation summary. Active specs live under `.cursor/specs/`; do not treat this as pending work.

This document aligns with [agent-prompt-cut-through-craters.md](agent-prompt-cut-through-craters.md) and records what was implemented.

## Problem

Black voids at cut-through boundaries when roads/interstates flatten a path through a hill (typically height 2 → base 1).

## Root cause (code)

- `PlaceCliffWalls` only treated land–land drops when `currentHeight - neighborHeight > 1`. One-step drops toward the lowered corridor had no cliff face.
- `DetermineSlopePrefab` only considers **higher** neighbors, so rim cells at the top of the cut did not get a lower-neighbor slope; visuals relied on missing cliff geometry.

## Implementation

### 1. Cut corridor context

- [PathTerraformPlan.cs](../Assets/Scripts/Managers/GameManagers/PathTerraformPlan.cs) builds `BuildTerraformCutCorridorSet()` when `isCutThrough`: all `pathCells` and `adjacentCells` with a non-`None` flatten action.
- Passed as `terraformCutCorridorCells` into every `RestoreTerrainForCell` call during **Apply** Phase 2–3.
- **Revert** passes `null` (original landscape does not need cut-through cliffs).

### 2. One-step cliff walls (BUG-29)

- [TerrainManager.cs](../Assets/Scripts/Managers/GameManagers/TerrainManager.cs): `NeedsCutThroughOneStepCliffToCorridor` + extended `NeedsCliffWall*` to place existing cliff prefabs when:
  - `terraformCutCorridorCells` is non-null,
  - the cardinal neighbor is in that set,
  - `currentHeight - neighborHeight == 1`,
  - neighbor is land (not sea).

### 3. Neighbor refresh waves

- Phase 3 uses `RefreshTerrainNeighborWaves`: **1 wave** for normal terraform, **2 waves** for cut-through so a second ring of cells updates after the first refresh.

### 4. Optional wider cut

- [TerraformingService.cs](../Assets/Scripts/Managers/GameManagers/TerraformingService.cs): `expandCutThroughAdjacentByOneStep` (default **false**). When enabled, `ExpandAdjacentFlattenCellsRecursively` also flattens neighbors at `baseHeight + 1`, widening the cut.

### 5. Diagnostics

- `TerrainManager.LogTerraformRestoreDiagnostics` (static, default `false`). Set to `true` in the debugger to log early exits from `RestoreTerrainForCell` (null cell, zoning overlay, invalid position, null heightmap). Use to distinguish voids from **BUG-28** sorting issues.

### 6. Map edge (cut-through + land slopes)

- **Slope prefab selection**: `GetNeighborHeightForLandSlope` treats out-of-map neighbors as the **current** cell height (not `HeightMap`’s out-of-bounds `MIN_HEIGHT`), so `DetermineSlopePrefab` / `GetTerrainSlopeTypeAt` do not invent corner/upslope prefabs toward the void.
- **Cliff walls**: `NeedsCliffWall*` only require the **dropped-toward** neighbor to be in-bounds for the main cliff and cut-through rules; the opposite cardinal is required only for the legacy sea-level special case.
- **Terraform**: `TerraformingService.cutThroughMinCellsFromMapEdge` (default **2**) invalidates a cut-through plan if any path cell or expanded `adjacentCells` flatten lies inside that margin. Set to **0** to disable. If width or height ≤ `2 * margin`, the check is skipped so tiny grids are not blocked entirely.

## API

- `ITerrainManager.RestoreTerrainForCell` / `TerrainManager.RestoreTerrainForCell` gained optional `ISet<Vector2Int> terraformCutCorridorCells = null`.

## Manual verification

1. Generate cut-through interstate across a 2/1 hill; confirm no black holes at the rim.
2. Toggle `LogTerraformRestoreDiagnostics` if problems persist; check for skipped cells (e.g. zoning overlay).
3. Optionally enable `expandCutThroughAdjacentByOneStep` on the `TerraformingService` component if the designer wants a wider cut.

## Out of scope (unchanged)

- Bridge prefab issues.
- Pathfinding cost tuning for detours (validation still guides A* via `CanPlaceRoad` / plan validity).
