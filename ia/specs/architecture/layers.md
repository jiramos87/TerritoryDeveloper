# Layers

## System Layers

```
┌─────────────────────────────────────────────────────────┐
│ UI Layer (legacy uGUI — DEC-A24 prefab bake path) │
│ UIManager, CursorManager, GameNotificationManager, │
│ all Controllers (buttons, popups, sliders) │
├─────────────────────────────────────────────────────────┤
│ UI Toolkit overlay layer (current UI baseline / DEC-A28)│
│ Per-panel Host MonoBehaviours: HudBarHost, │
│ BudgetPanelHost, MapPanelHost, StatsPanelHost, │
│ PauseMenuHost, MainMenuHost, NotificationsToastHost, │
│ HoverInfoHost, InfoPanelHost, ToolbarHost, │
│ ToolSubtypePickerHost, MiniMapController-Runtime. │
│ ModalCoordinator owns Show/HideMigrated routing. │
├─────────────────────────────────────────────────────────┤
│ Simulation Layer │
│ SimulationManager, AutoRoadBuilder, AutoZoningManager, │
│ AutoResourcePlanner, GrowthManager, GrowthBudgetMgr, │
│ UrbanCentroidService (AUTO roads/zoning rings) │
├─────────────────────────────────────────────────────────┤
│ Gameplay Layer │
│ ZoneManager, RoadManager, InterstateManager, │
│ DemandManager, EconomyManager │
├─────────────────────────────────────────────────────────┤
│ Stats Layer │
│ CityStats, EmploymentManager, StatisticsManager │
├─────────────────────────────────────────────────────────┤
│ Terrain Layer │
│ TerrainManager, WaterManager, HeightMap, WaterMap │
├─────────────────────────────────────────────────────────┤
│ Forests Layer │
│ ForestManager, ForestMap, Forest, IForest │
├─────────────────────────────────────────────────────────┤
│ Core Layer │
│ GridManager (hub), Cell, CellData │
├─────────────────────────────────────────────────────────┤
│ Persistence Layer │
│ GameSaveManager, GameManager │
└─────────────────────────────────────────────────────────┘
```

## Helper Services

| Service | Role |
|---------|------|
| GridPathfinder | A* pathfinding for road routes |
| GridSortingOrderService | Sorting order computation |
| BuildingPlacementService | Building placement and load/restore |
| TerraformingService, PathTerraformPlan | Terraform plan computation, apply/revert, cut-through |
| RoadPrefabResolver | Prefab selection for path and single-cell contexts |
| RoadPathCostConstants | Shared cost constants for road pathfinding |
| RoadStrokeTerrainRules | Land slope allowlist and stroke truncation (flat + cardinal ramps only for road cells) |
| UrbanCentroidService | Urban centroid and ring calculation |
| GameBootstrap | Entry point, game loading flow |
| **Compute** utilities (`Utilities/Compute/`) | **Pure** **C#** **World ↔ Grid**, **growth ring**, **grid distance**, **`PathfindingCostKernel`**, **WaterAdjacency**, **DesirabilityFieldSampler**, etc. **Edit Mode** **UTF** parity-checks **`tools/compute-lib`**. Charter trace: [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md); ongoing rows: [`BACKLOG.md`](../../BACKLOG.md) **§ Compute-lib program**. |

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
| CityStats | TimeManager, WaterManager, ForestManager, EconomyManager, EmploymentManager |
| AutoRoadBuilder | GridManager, RoadManager, GrowthBudgetManager, CityStats, InterstateManager, TerrainManager |
| AutoZoningManager | GridManager, ZoneManager, GrowthBudgetManager, CityStats, DemandManager |
| AutoResourcePlanner | CityStats, GridManager, GrowthBudgetManager, UIManager |
| GrowthManager | GridManager, DemandManager |
| GrowthBudgetManager | CityStats, EconomyManager |
| EmploymentManager | CityStats, DemandManager |
| StatisticsManager | EmploymentManager, DemandManager, EconomyManager, CityStats |
| UIManager | ZoneManager, CursorManager, GridManager, TimeManager, EconomyManager, GameManager, TerrainManager, CityStats, various Controllers |
| GameSaveManager | GridManager, CityStats, TimeManager, InterstateManager, MiniMapController |
| GameManager | GridManager, GameSaveManager |
| RegionalMapManager | InterstateManager, CityStats, GridManager |
| CursorManager | GridManager |
