# Managers & Services — Reference

> Complete reference of all managers and helper services: responsibilities, dependencies, and patterns.
> For the full dependency map, see `ARCHITECTURE.md`.

## Manager Responsibilities

| Manager | Responsibility |
|---------|---------------|
| **GridManager** | Central hub. Isometric 2D grid, cell access, coordinate conversion, building placement, bulldozing, sorting order, pathfinding |
| **TerrainManager** | Heightmap generation, slope types, prefab selection, cliff stacks |
| **WaterManager** | Water body generation/management: multi-level lakes, rivers, shore/cliff/cascade visuals, multi-body junctions |
| **ForestManager** | Forest generation (sparse/medium/dense), forestation, deforestation |
| **ZoneManager** | RCI zoning, zone tile placement |
| **RoadManager** | Road drawing, prefab selection by neighbors, road preview |
| **InterstateManager** | Interstate highways connecting map to exterior |
| **GeographyManager** | Orchestrator for all terrain initialization (terrain + water + forest + grid) |
| **DemandManager** | R/C/I demand calculation based on population, employment, forests |
| **EconomyManager** | Taxes, money, financial transactions |
| **CityStats** | Global statistics aggregator: population, employment, water/power capacity |
| **EmploymentManager** | Employment and unemployment calculation by zone |
| **SimulationManager** | Automatic simulation cycle orchestrator |
| **AutoRoadBuilder** | Automatic road network extension |
| **AutoZoningManager** | Automatic zoning of cells adjacent to roads |
| **AutoResourcePlanner** | Automatic resource building planning |
| **GrowthManager** | Zone growth logic |
| **GrowthBudgetManager** | Growth budget per category |
| **UIManager** | Main UI: popups, toolbar/tool state, demand bar. See `.cursor/specs/ui-design-system.md` |
| **CursorManager** | Placement preview, visual cursor |
| **TimeManager** | Game speed control, simulation ticks |
| **GameNotificationManager** | **Singleton.** In-game notifications |
| **GameSaveManager** | Serialize/deserialize game state |
| **GameManager** | Entry point, game loading |
| **RegionalMapManager** | Regional map with neighboring cities |
| **StatisticsManager** | Historical trend tracking |
| **AnimatorManager** | Animation control (file: `AnimationManager.cs`) |
| **GameDebugInfoBuilder** | Debug text generation for game state |

## Helper Services

| Service | Role |
|---------|------|
| **GridPathfinder** | A* pathfinding for road routes; walkability excludes non-cardinal land slopes (see `roads-system.md` land slope policy) |
| **GridSortingOrderService** | Sorting order computation |
| **BuildingPlacementService** | Building placement and load/restore |
| **TerraformingService** | Path-level terraform plan computation |
| **PathTerraformPlan** | Terraform Apply/Revert, cut-through mode |
| **RoadPrefabResolver** | Prefab selection for path and single-cell contexts |
| **RoadPathCostConstants** | Shared cost constants for road pathfinding |
| **RoadStrokeTerrainRules** | Static allowlist (flat + cardinal ramps) and stroke truncation for road placement |
| **UrbanCentroidService** | Urban centroid + ring metrics for AUTO roads/zoning (active, not obsolete) |
| **GameBootstrap** | Entry point, game loading flow |

## Architectural patterns

- Every manager is a **MonoBehaviour** living as a component on a scene GameObject.
- **Never** instantiate managers with `new` — always scene components.
- **GridManager** is the central hub — all cell operations go through it.
- Notifications: `GameNotificationManager.Instance` (only singleton).
- **CityStats** is the global data aggregator — read city-wide stats from here.
- Dependencies: `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`.

## Obsolete

- **`UrbanizationProposalManager`** and related proposal UI — obsolete, not run from `ProcessSimulationTick()`. Do not re-enable. Removal: TECH-13.
