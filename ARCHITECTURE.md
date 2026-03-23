# Territory Developer — Architecture

## Overview

Territory Developer is a 2D isometric city-builder built in Unity with C#. Players place roads, zones (residential/commercial/industrial), buildings, and manage their city's economy, resources, and growth.

All game logic lives in MonoBehaviour classes under `Assets/Scripts/`. There is no dependency injection framework — managers reference each other via Unity Inspector fields with `FindObjectOfType<T>()` fallback in Awake/Start.

## System Layers

```
┌─────────────────────────────────────────────────────────┐
│  UI Layer                                               │
│  UIManager, CursorManager, GameNotificationManager,     │
│  all Controllers (buttons, popups, sliders)             │
├─────────────────────────────────────────────────────────┤
│  Simulation Layer                                       │
│  SimulationManager, AutoRoadBuilder, AutoZoningManager, │
│  AutoResourcePlanner, GrowthManager, GrowthBudgetMgr,  │
│  UrbanCentroidService (AUTO roads/zoning rings)         │
├─────────────────────────────────────────────────────────┤
│  Gameplay Layer                                         │
│  ZoneManager, RoadManager, InterstateManager,           │
│  DemandManager, EconomyManager                          │
├─────────────────────────────────────────────────────────┤
│  Stats Layer                                            │
│  CityStats, EmploymentManager, StatisticsManager        │
├─────────────────────────────────────────────────────────┤
│  Terrain Layer                                          │
│  TerrainManager, WaterManager, HeightMap, WaterMap      │
├─────────────────────────────────────────────────────────┤
│  Forests Layer                                          │
│  ForestManager, ForestMap, Forest, IForest              │
├─────────────────────────────────────────────────────────┤
│  Core Layer                                             │
│  GridManager (hub), Cell, CellData                      │
├─────────────────────────────────────────────────────────┤
│  Persistence Layer                                      │
│  GameSaveManager, GameManager                           │
└─────────────────────────────────────────────────────────┘
```

## Folder Structure (`Assets/Scripts/`)

| Directory | Files | Purpose |
|-----------|-------|---------|
| `Managers/GameManagers/` | 94 | Core game logic: grid, terrain, zones, roads, economy, simulation, helpers |
| `Managers/UnitManagers/` | 64 | Data models: Cell, Zone, Building, Forest, HeightMap, CellData, WaterMap, etc. |
| `Controllers/GameControllers/` | 6 | CameraController, CityStatsUIController, etc. |
| `Controllers/UnitControllers/` | 38 | UI buttons, popups, sliders |
| `Utilities/` | 6 | DebugHelper, RoadPathCostConstants, etc. |

## Key Files by Size

| File | Lines | Role |
|------|-------|------|
| GridManager.cs | ~2070 | Central hub — grid, cells, coordinates, placement, sorting, pathfinding |
| TerrainManager.cs | ~2365 | Heightmap, slopes, terrain prefab selection |
| ZoneManager.cs | ~1360 | RCI zoning, zone tile placement |
| RoadManager.cs | ~1730 | Road drawing, prefab selection, road preview |
| UIManager.cs | ~1240 | Main UI, popups, tool state |
| CityStats.cs | ~1200 | Global statistics aggregator |
| AutoRoadBuilder.cs | ~1160 | Automatic road extension |
| GeographyManager.cs | ~980 | Terrain initialization orchestrator |
| InterstateManager.cs | ~1160 | Interstate highway connections |
| ForestManager.cs | ~860 | Forest generation and management |

## Helper Services (extracted from managers)

| Service | Role |
|---------|------|
| GridPathfinder | A* pathfinding for road routes |
| GridSortingOrderService | Sorting order computation |
| BuildingPlacementService | Building placement and load/restore |
| TerraformingService | Path-level terraform plan computation |
| PathTerraformPlan | Terraform Apply/Revert, cut-through mode |
| RoadPrefabResolver | Prefab selection for path and single-cell contexts |
| RoadPathCostConstants | Shared cost constants for road pathfinding |
| UrbanCentroidService | Urban centroid and ring calculation (FEAT-32) |
| GameBootstrap | Entry point, game loading flow |

## Data Flows

### Initialization
`GeographyManager.InitializeGeography()` orchestrates the full startup sequence:
1. `RegionalMapManager.InitializeRegionalMap()` — regional map with neighboring cities
2. `GridManager.InitializeGrid()` — creates cell grid, then internally calls `TerrainManager.InitializeHeightMap()` to generate terrain elevation (on maps **wider/taller than 40×40**, the **40×40** designer template is **centered**; procedural terrain fills the rest)
3. `WaterManager.InitializeWaterMap()` — builds `WaterMap` + lake bodies (depression-fill on `HeightMap` when `useLakeDepressionFill`; legacy: mask by `seaLevel`)
4. `InterstateManager.GenerateAndPlaceInterstate()` — interstate highways (up to 3 random attempts + deterministic fallback)
5. `ForestManager.InitializeForestMap()` — generates forests (conditional: `initializeForestsOnStart`)
6. Water desirability calculation, sorting order recalculation, border signs placement
7. `ZoneManager` is then ready for player zoning

### Simulation (each TimeManager tick)
`TimeManager` → `SimulationManager.ProcessSimulationTick()` → executes in order:
1. `GrowthBudgetManager.EnsureBudgetValid` (when present)
2. `UrbanCentroidService.RecalculateFromGrid` — urban centroid and rings for AUTO systems (FEAT-32)
3. `AutoRoadBuilder` — extends road network
4. `AutoZoningManager` — zones cells adjacent to roads
5. `AutoResourcePlanner` — plans resource buildings (water, power)

The legacy **UrbanizationProposal** system is **obsolete** and **not** invoked; removal is tracked as **TECH-13** in `BACKLOG.md`.

### Player Input
`GridManager.Update()` detects clicks → dispatches by active mode:
- Zoning mode → `ZoneManager.PlaceZone()`
- Road mode → `RoadManager.HandleRoadDrawing()`
- Building mode → `GridManager.HandleBuildingPlacement()`
- Bulldozer mode → `GridManager.HandleBulldozerMode()`

### Persistence
- Save: `GameSaveManager` → `GridManager.GetGridData()` → serializes `List<CellData>` + `WaterMap.GetSerializableData()` as `WaterMapData` on `GameSaveData`
- Load: `GameSaveManager` → `TerrainManager.RestoreHeightMapFromGridData` / `ApplyRestoredPositionsToGrid` → `WaterManager.RestoreWaterMapFromSaveData` (or legacy `RestoreFromLegacyCellData` when `waterMapData` absent) → `GridManager.RestoreGrid` (applies saved prefabs, `sortingOrder`, `waterBodyType`, shore secondary prefabs; does **not** run `RestoreWaterSlopesFromHeightMap`, `RestoreTerrainSlopesFromHeightMap`, or `ReCalculateSortingOrderBasedOnHeight`)
- **Load sorting:** **[BUG-34](BACKLOG.md)** completed (2026-03-22): snapshot restore, water/shore, building post-pass. **[BUG-35](BACKLOG.md)** completed (2026-03-22): `DestroyCellChildren(..., destroyFlatGrass: true)` when restoring/placing RCI and utility buildings (no flat grass child under the building); multi-cell footprint still uses `GridSortingOrderService.SetZoneBuildingSortingOrder` + per-cell `SyncCellTerrainLayersBelowBuilding` for any remaining grass tiles. See **[BUG-20](BACKLOG.md)** if utilities still mis-sort.

### UI / UX design system (program)

Cross-cutting effort to standardize HUD, popups, and interaction patterns: **charter** [`docs/ui-design-system-project.md`](docs/ui-design-system-project.md), **discovery** [`docs/ui-design-system-context.md`](docs/ui-design-system-context.md), **spec** [`.cursor/specs/ui-design-system.md`](.cursor/specs/ui-design-system.md). Executable work is tracked as normal `BACKLOG.md` issues linked from the charter (e.g. toolbar **ControlPanel** layout: **[TECH-07](BACKLOG.md)**).

### Water (current vs planned)

- **Today (FEAT-37a + FEAT-37b + FEAT-37c completed):** `WaterMap` stores **per-cell water body id** and **`WaterBody`** holds **surface height**; procedural lakes use **depression-fill**; `TerrainManager` may carve **minimal cardinal bowls** (`LakeFeasibility`). Lake **shore tiles** from **FEAT-37a**; **sorting / roads / bridges / `SEA_LEVEL` removal** (non-shore) from **FEAT-37b**; **FEAT-37c** persists `WaterMapData` on save and restores it on load with **CellData** snapshot restore (no slope regen / no global sorting recalc on load). **`WaterBodyType`** on `Cell` / `CellData` classifies lake vs sea for saves. Remaining shore art issues: **[BUG-33](BACKLOG.md)**. Load-time building sort: **[BUG-34](BACKLOG.md)** and **[BUG-35](BACKLOG.md)** completed (2026-03-22).
- **Epic ([FEAT-37](BACKLOG.md)):** Child issues **FEAT-37a** / **FEAT-37b** / **FEAT-37c** done; rivers/sea/sources/tools: **FEAT-38–FEAT-41**. Minimap water: **[BUG-32](BACKLOG.md)** completed (2026-03-23). See `.cursor/specs/water-system-refactor.md`.

### Isometric Geography

The terrain system uses a **diamond isometric projection** with an integer **height model** (0–5), **13 terrain slope types** (flat, 4 cardinal, 4 diagonal, 4 corner/upslope), and a **priority-based slope determination algorithm**. Roads interact with terrain via a terraforming system (scale-with-slopes or cut-through modes). Full technical reference: [`.cursor/specs/isometric-geography-system.md`](.cursor/specs/isometric-geography-system.md).

## Full Dependency Map

| Manager | Dependencies |
|---------|-------------|
| GridManager | ZoneManager, UIManager, CityStats, CursorManager, TerrainManager, DemandManager, WaterManager, GameNotificationManager, ForestManager, CameraController, RoadManager, InterstateManager, BuildingSelectorMenuController |
| ZoneManager | GridManager, RoadManager, CityStats, UIManager, GameNotificationManager, DemandManager, WaterManager, InterstateManager |
| RoadManager | TerrainManager, GridManager, CityStats, UIManager, ZoneManager, InterstateManager |
| TerrainManager | GridManager, ZoneManager, WaterManager |
| WaterManager | GridManager, TerrainManager, ZoneManager |
| ForestManager | GridManager, WaterManager, CityStats, EconomyManager, UIManager, GameNotificationManager, TerrainManager |
| GeographyManager | TerrainManager, WaterManager, ForestManager, GridManager, ZoneManager, InterstateManager, RegionalMapManager |
| TimeManager | UIManager, SpeedButtonsController, CityStats, EconomyManager, GridManager, AnimatorManager, ZoneManager, InterstateManager, SimulationManager |
| SimulationManager | CityStats, GrowthBudgetManager, UrbanCentroidService, AutoRoadBuilder, AutoZoningManager, AutoResourcePlanner |
| EconomyManager | CityStats, TimeManager, GameNotificationManager |
| DemandManager | EmploymentManager, CityStats, ForestManager, GridManager |
| InterstateManager | GridManager, TerrainManager, RoadManager |
| CityStats | TimeManager, WaterManager, ForestManager |
| AutoRoadBuilder | GridManager, RoadManager, GrowthBudgetManager, CityStats, InterstateManager, TerrainManager |
| AutoZoningManager | GridManager, ZoneManager, GrowthBudgetManager, CityStats, DemandManager |
| AutoResourcePlanner | CityStats, GridManager, GrowthBudgetManager, UIManager |
| GrowthManager | GridManager, DemandManager |
| GrowthBudgetManager | CityStats |
| EmploymentManager | CityStats, DemandManager |
| StatisticsManager | EmploymentManager, DemandManager, EconomyManager, CityStats |
| UIManager | ZoneManager, CursorManager, GridManager, TimeManager, EconomyManager, GameManager, TerrainManager, CityStats, various Controllers |
| GameSaveManager | GridManager, CityStats, TimeManager, InterstateManager, MiniMapController (+ runtime `FindObjectOfType` for WaterManager, etc.) |
| GameManager | GridManager, GameSaveManager |
| RegionalMapManager | InterstateManager, CityStats, GridManager |
| CursorManager | GridManager |

## Road and interstate routing (summary)

- **Manual streets:** `RoadManager.TryPrepareRoadPlacementPlanLongestValidPrefix` (partial paths), `PathTerraformPlan.TryValidatePhase1Heights`, preview terraform reverted before A* each frame. Spec: `.cursor/specs/road-drawing-fixes.md` (BACKLOG **BUG-25**).
- **Interstate:** `TryPrepareRoadPlacementPlan` with `RoadPathValidationContext.forbidCutThrough`; `InterstateManager` ranks border endpoints and runs dual A* (`PickLowerCostInterstateAStarPath`) with shared costs in `RoadPathCostConstants`. Spec: `.cursor/specs/interstate-prefab-and-pathfinding-fixes.md`. Cut-through void mitigation (historical): `.cursor/specs/archive/plan-cut-through-craters.md` (BACKLOG **BUG-29**, completed).

## Architectural Decisions

- **GridManager as hub**: GridManager is the central coordinator because nearly all game operations involve cells. This keeps cell access consistent but makes GridManager large.
- **FindObjectOfType pattern**: Used instead of DI for simplicity. Managers declare public/serialized fields wired in Inspector, with FindObjectOfType as null-check fallback in Awake/Start.
- **Single singleton**: Only GameNotificationManager uses the singleton pattern (with DontDestroyOnLoad). All other managers are resolved via Inspector references.
- **Namespaces (partial migration)**: Most scripts use `Territory.*` namespaces (`Territory.Core`, `Territory.Terrain`, `Territory.Roads`, `Territory.Zones`, `Territory.Forests`, `Territory.Buildings`, `Territory.Economy`, `Territory.UI`, `Territory.Geography`, `Territory.Timing`, `Territory.Utilities`, `Territory.Simulation`, `Territory.Persistence`). `TerraformingService` and `PathTerraformPlan` live in `Territory.Terrain`. A few legacy or utility scripts may still be in the global namespace; prefer new code under `Territory.*`.

## Known Trade-offs
- **High coupling**: Many managers reference each other directly, creating tight coupling
- **GridManager size**: At ~2070 lines, it handles too many responsibilities (placement, sorting, pathfinding, culling); decomposition tracked as **TECH-01** in `BACKLOG.md`
- **No event system**: Managers communicate via direct method calls rather than events
