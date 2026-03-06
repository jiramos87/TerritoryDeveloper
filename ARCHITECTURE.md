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
│  UrbanizationProposalManager                            │
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
| `Managers/GameManagers/` | 33 | Core game logic: grid, terrain, zones, roads, economy, simulation |
| `Managers/UnitManagers/` | 23 | Data models: Cell, Zone, Building, Forest, HeightMap, CellData, etc. |
| `Controllers/GameControllers/` | 2 | CameraController, CityStatsUIController |
| `Controllers/UnitControllers/` | 17 | UI buttons, popups, sliders |
| `Utilities/` | 1 | DebugHelper |

## Key Files by Size

| File | Lines | Role |
|------|-------|------|
| GridManager.cs | ~2100 | Central hub — grid, cells, coordinates, placement, sorting, pathfinding |
| TerrainManager.cs | ~1270 | Heightmap, slopes, terrain prefab selection |
| ZoneManager.cs | ~1025 | RCI zoning, zone tile placement |
| UIManager.cs | ~1020 | Main UI, popups, tool state |
| RoadManager.cs | ~965 | Road drawing, prefab selection, road preview |
| CityStats.cs | ~895 | Global statistics aggregator |
| GeographyManager.cs | ~750 | Terrain initialization orchestrator |
| ForestManager.cs | ~720 | Forest generation and management |
| AutoRoadBuilder.cs | ~620 | Automatic road extension |
| InterstateManager.cs | ~560 | Interstate highway connections |

## Data Flows

### Initialization
`GeographyManager.InitializeGeography()` orchestrates the full startup sequence:
1. `TerrainManager.GenerateHeightMap()` — generates terrain elevation
2. `WaterManager.InitializeWater()` — places water bodies
3. `ForestManager.InitializeForests()` — generates forests
4. `GridManager.InitializeGrid()` — creates the cell grid with terrain applied
5. `InterstateManager.GenerateInterstateConnections()` — connects interstate highways
6. `ZoneManager` is then ready for player zoning

### Simulation (each TimeManager tick)
`TimeManager` → `SimulationManager.RunSimulationStep()` → executes in order:
1. `AutoRoadBuilder` — extends road network
2. `AutoZoningManager` — zones cells adjacent to roads
3. `AutoResourcePlanner` — plans resource buildings (water, power)
4. `UrbanizationProposalManager` — proposes urban expansions

### Player Input
`GridManager.Update()` detects clicks → dispatches by active mode:
- Zoning mode → `ZoneManager.PlaceZone()`
- Road mode → `RoadManager.HandleRoadDrawing()`
- Building mode → `GridManager.HandleBuildingPlacement()`
- Bulldozer mode → `GridManager.HandleBulldozerMode()`

### Persistence
- Save: `GameSaveManager` → `GridManager.GetGridData()` → serializes `List<CellData>`
- Load: `GameSaveManager` → `GridManager.RestoreGrid(List<CellData>)` → rebuilds grid

## Full Dependency Map

| Manager | Dependencies |
|---------|-------------|
| GridManager | ZoneManager, UIManager, CityStats, CursorManager, TerrainManager, DemandManager, WaterManager, GameNotificationManager, ForestManager, CameraController, RoadManager, InterstateManager |
| ZoneManager | GridManager, RoadManager, CityStats, UIManager, GameNotificationManager, DemandManager, WaterManager, InterstateManager |
| RoadManager | TerrainManager, GridManager, CityStats, UIManager, ZoneManager, InterstateManager |
| TerrainManager | GridManager, ZoneManager, WaterManager |
| WaterManager | GridManager, TerrainManager, ZoneManager |
| ForestManager | GridManager, WaterManager, CityStats, EconomyManager, UIManager, GameNotificationManager, TerrainManager |
| GeographyManager | TerrainManager, WaterManager, ForestManager, GridManager, ZoneManager, InterstateManager, RegionalMapManager |
| TimeManager | UIManager, SpeedButtonsController, CityStats, EconomyManager, GridManager, AnimatorManager, ZoneManager, InterstateManager, SimulationManager |
| SimulationManager | CityStats, GrowthBudgetManager, AutoRoadBuilder, AutoZoningManager, AutoResourcePlanner, UrbanizationProposalManager |
| EconomyManager | CityStats, TimeManager, GameNotificationManager |
| DemandManager | EmploymentManager, CityStats, ForestManager, GridManager |
| InterstateManager | GridManager, TerrainManager, RoadManager |
| CityStats | TimeManager, WaterManager, ForestManager |
| AutoRoadBuilder | GridManager, RoadManager, GrowthBudgetManager, CityStats, InterstateManager, TerrainManager |
| AutoZoningManager | GridManager, ZoneManager, GrowthBudgetManager, CityStats, DemandManager |
| AutoResourcePlanner | CityStats, GridManager, GrowthBudgetManager, UIManager |
| UrbanizationProposalManager | GridManager, RoadManager, ZoneManager, CityStats, DemandManager |
| GrowthManager | GridManager, DemandManager |
| GrowthBudgetManager | CityStats |
| EmploymentManager | CityStats, DemandManager |
| StatisticsManager | EmploymentManager, DemandManager, EconomyManager, CityStats |
| UIManager | ZoneManager, CursorManager, GridManager, TimeManager, EconomyManager, GameManager, TerrainManager, CityStats, various Controllers |
| GameSaveManager | GridManager, CityStats, TimeManager, InterstateManager |
| GameManager | GridManager, GameSaveManager |
| RegionalMapManager | InterstateManager, CityStats, GridManager |
| CursorManager | GridManager |

## Architectural Decisions

- **GridManager as hub**: GridManager is the central coordinator because nearly all game operations involve cells. This keeps cell access consistent but makes GridManager large.
- **FindObjectOfType pattern**: Used instead of DI for simplicity. Managers declare public/serialized fields wired in Inspector, with FindObjectOfType as null-check fallback in Awake/Start.
- **Single singleton**: Only GameNotificationManager uses the singleton pattern (with DontDestroyOnLoad). All other managers are resolved via Inspector references.
- **No namespaces**: All 77 scripts share the global namespace. This is a known limitation being addressed.

## Known Trade-offs
- **High coupling**: Many managers reference each other directly, creating tight coupling
- **GridManager size**: At ~2100 lines, it handles too many responsibilities (placement, sorting, pathfinding, culling)
- **No event system**: Managers communicate via direct method calls rather than events
