# Roads System — Reference Spec

> Deep reference for road placement, pathfinding, bridge validation, and prefab resolution.
> For terrain-level road rules (prefab selection on slopes, cost model), see `isometric-geography-system.md` §9, §10, §13.

## Shared validation surface (geography spec §13.1)

All persistent road placement must produce a `PathTerraformPlan`, pass Phase-1 height validation, and commit via `Apply` / `ResolveForPath`. Never use `ComputePathPlan` alone as the placement decision.

### Two ways to build the plan

| Plan source | When | Notes |
|-------------|------|-------|
| `TerraformingService.ComputePathPlan` | Default land/slope/cut-through strokes | Used inside `TryPrepareFromFilteredPathList` after filtered path checks. |
| `TerraformingService.TryBuildDeckSpanOnlyWaterBridgePlan` | Manual draw with locked lip→exit chord over water/shore (FEAT-44) | All path cells `TerraformAction.None`; `waterBridgeTerraformRelaxation` + deck display height. |

### Validation by mode

| Mode | Validation method | `forbidCutThrough` |
|------|-------------------|-------------------|
| Manual streets / preview | `TryPrepareLockedDeckSpanBridgePlacement` then `TryPrepareRoadPlacementPlanLongestValidPrefix` | `false` |
| Interstate | `TryPrepareRoadPlacementPlan` (full path) | `true` |
| AUTO streets | Same as manual; water crossings require full segment budget in one tick + firm dry exit. `AutoRoadBuilder` reverts plan if it cannot place every tile (no half bridges). Uniform `waterBridgeDeckDisplayHeight` for all deck prefabs. | `false` |

## Land slope stroke policy (BUG-51 closure)

**Allowed:** `TerrainSlopeType.Flat` and cardinal ramps (`North`, `South`, `East`, `West`) on **land** stroke cells.

**Disallowed:** Pure diagonal slopes (`NorthEast`, `NorthWest`, `SouthEast`, `SouthWest`) and corner-up types (`NorthEastUp`, …). Preview and commit use the **longest valid prefix** (cursor may move; preview does not extend over blocked terrain). **No** toast when the stroke starts on blocked terrain or when the only failure mode is “no slope-valid prefix.”

**Implementation:**

| Piece | Behavior |
|-------|----------|
| `RoadStrokeTerrainRules` | `IsLandSlopeAllowedForRoadStroke`; `TruncatePathAtFirstDisallowedLandSlope` |
| `RoadManager.TryBuildFilteredPathForRoadPlan` | Truncates after non-null cell filter; empty → fail silent |
| `RoadManager.TryPrepareRoadPlacementPlanLongestValidPrefix` | “Road cannot extend further…” only if a non-empty slope-valid prefix exists on the raw stroke |
| `RoadManager.TryPrepareDeckSpanPlanFromAdjacentStroke` | Same truncation before bridge/deck validation |
| `GridPathfinder` | Non-walkable for disallowed land slopes (manual + AUTO A*) |
| `InterstateManager.IsCellAllowedForInterstate` | Same land rule; `IsWaterSlopeCell` still allowed |

**Truncator exceptions (do not cut FEAT-44 spans):** cells with `HeightMap` height ≤ 0 on the path, and cells where `TerrainManager.IsWaterSlopeCell` is true — still counted so chord/wet runs stay contiguous.

Geography spec cross-reference: `isometric-geography-system.md` §3.3.3–§3.3.4, §13.10.

## AUTO simulation pathfinding (spec §13.9)

- **Manual / generic A\*:** `GridManager.FindPath` and `FindPathWithRoadSpacing` — walkable cells: grass + road only, plus **land slope eligibility** (flat + cardinal ramps only; see Land slope stroke policy).
- **AUTO simulation A\*:** `FindPathForAutoSimulation` and `FindPathWithRoadSpacingForAutoSimulation` — also allows undeveloped light zoning (no building), via `AutoSimulationRoadRules` + `GridPathfinder`. Only `AutoRoadBuilder` should use these. Same land slope walkability as manual A\*.
- **Placement validation** for committed roads: `PathTerraformPlan` + Phase-1 + `Apply` / resolve (§13.1).
- **AUTO batch junction refresh:** After `PlaceRoadTileFromResolved`, `AutoRoadBuilder` flushes `RefreshRoadPrefabsAfterBatchPlacement` once per tick (deduped); bridge deck tiles skipped.

## Resolver rules (spec §13.7)

| Rule | Description |
|------|------------|
| A | Elbow connectivity matches exactly two path neighbors |
| B | Prefab exits align with path in/out |
| C | Terraform wins — cut-through uses flat prefabs from plan, not live slope misread |
| D | Prefer offset paths avoiding hills when costs close |
| E | Interstate prefers straight segments |
| F | Bridge approach perpendicular to water; no turn on last land cells before water |

**BUG-51 (route-first):** `RoadPrefabResolver.ResolveForPath` classifies each path cell (straight-through, corner-90, junction, end, isolated) using **only** cells in the current stroke’s `pathCellSet` for topology (`pathOnlyNeighbors`), so adjacent unrelated roads do not create spurious T/elbows. Straights use travel `curr - prev` for ramp axis; junctions still use `SelectFromConnectivity`. `Cell` stores runtime hints: predecessor/successor grid, `roadRouteEntryStep` / `roadRouteExitStep`; `RefreshRoadPrefabAt` invalidates hints when topology is no longer straight/dead-end, and uses hints to pick `prev` for `ResolveForPath`-consistent slopes. `TerraformingService` may preserve diagonal wedge cells on the path when `preferSlopeClimb && dSeg == 0` instead of flattening. Cardinal ramp prefabs on diagonal/corner-up terrain use the same upper-cell anchor as elbows in `GetWorldPositionForPrefab` where applicable.

## Domain vocabulary (glossary)

Canonical definitions: **`isometric-geography-system.md` §14.5** — road **stroke**, **bridge lip**, **wet run**, **baseHeight**, **grass cell**, **street** vs interstate, **map border**, **Chebyshev distance**. This spec owns truncation, validation entry points, and `RoadCacheService` behavior tied to those terms.

## Key files

| File | Role |
|------|------|
| `RoadManager.cs` | Road drawing, preview, placement commit |
| `RoadPrefabResolver.cs` | Prefab selection for path and single-cell contexts |
| `TerraformingService.cs` | Path-level terraform plan computation |
| `PathTerraformPlan.cs` | Plan object: Apply/Revert, cut-through mode |
| `GridPathfinder.cs` | A* pathfinding for road routes |
| `RoadPathCostConstants.cs` | Shared cost constants |
| `RoadStrokeTerrainRules.cs` | Land slope allowlist + stroke truncation for roads |
| `RoadCacheService.cs` | Cached road queries — invalidate after modifications |
| `InterstateManager.cs` | Interstate highway generation and placement |
| `AutoRoadBuilder.cs` | Automatic road extension during simulation |
