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
| **RoadManager** | **Street**/**interstate** drawing, prefab selection by neighbors, preview |
| **InterstateManager** | **Interstate** placement linking the grid to the **map border** |
| **GeographyManager** | Orchestrator for all terrain initialization (terrain + water + forest + grid) |
| **DemandManager** | R/C/I demand calculation based on population, employment, forests |
| **EconomyManager** | Taxes, money, financial transactions |
| **CityStats** | Global statistics aggregator: population, employment, water/power capacity |
| **EmploymentManager** | Employment and unemployment calculation by zone |
| **SimulationManager** | Automatic simulation cycle orchestrator |
| **AutoRoadBuilder** | Automatic **street** network extension |
| **AutoZoningManager** | Automatic zoning of cells adjacent to **streets**/**interstates** |
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
| **GridPathfinder** | A* pathfinding for **street**/**interstate** routes; walkability excludes non-cardinal land slopes (see `roads-system.md` land slope policy) |
| **GridSortingOrderService** | Sorting order computation |
| **BuildingPlacementService** | Building placement and load/restore |
| **TerraformingService** | Path-level terraform plan computation |
| **PathTerraformPlan** | Terraform Apply/Revert, cut-through mode |
| **RoadPrefabResolver** | Prefab selection for path and single-cell contexts |
| **RoadPathCostConstants** | Shared cost constants for **street**/**interstate** pathfinding |
| **RoadStrokeTerrainRules** | Static allowlist (flat + cardinal ramps) and stroke truncation for **street**/**interstate** placement |
| **UrbanCentroidService** | Urban centroid + ring metrics for AUTO **streets**/zoning (active, not obsolete) |
| **GameBootstrap** | Entry point, game loading flow |

## Zones & Buildings

> **Glossary index:** `glossary.md` cites this section as **mgrs §Zones**.
>
> Domain model for RCI zoning, building placement, and multi-cell footprints. For AUTO **street** walkability over light zoning, see `isometric-geography-system.md` §13.9.

### RCI model

- **Residential (R), Commercial (C), Industrial (I)** are the three zone categories. Each drives different demand, employment, and building sets in the economy layer (`DemandManager`, `ZoneManager`).
- Zoning is placed per cell; buildings spawn on zoned cells when simulation and demand allow.

### Zone lifecycle

1. **Empty developable cell** — grass, forest, or other land the player or AUTO may zone.
2. **Zoned cell** — a `Zone` component marks the cell with a zone type and density tier (light / medium / heavy where applicable).
3. **Building** — when growth rules fire, a building prefab is placed on the zone footprint; the zone tracks level and building reference.
4. **Upgrade** — `GrowthManager` may replace a building with a larger variant when property value / demand supports it (see backlog issues for happiness and property value).

### Zone density

- **Light / medium / heavy** tiers control which building prefabs and footprints are eligible. Higher tiers generally mean larger or denser structures.
- AUTO simulation treats **undeveloped light zoning** (light tier, **no** building spawned) as pass-through terrain for **street** pathfinding only — see geography spec §13.9 and `AutoSimulationRoadRules`.

### Pivot cell and multi-cell buildings

- Buildings may occupy **1×1** or **2×2** (and utility footprints as designed) cells.
- The **pivot cell** is the anchor cell for a multi-cell building. Other footprint cells reference the pivot for sorting, save data, and demolition. Non-pivot cells must stay consistent with the pivot’s building reference.

### Building footprint

The set of grid cells covered by a single building instance (one tile or a rectangle/multi-tile utility layout). Bulldozing, sorting, save/load, and zone growth treat the footprint as one unit anchored at the **pivot cell**.

### Building placement and restore

- Runtime placement and load-game restore go through `BuildingPlacementService` and `GridManager` restore paths. Visual sorting on load follows geography spec §7.4 (visual restore).

## Demand (R / C / I)

> **Glossary index:** `glossary.md` cites this section as **mgrs §Demand**.

Residential, commercial, and industrial **demand** scores express how strongly each zone type wants to grow. They are derived from population, employment, forest cover, taxes, and related aggregates (`DemandManager`, `CityStats`, `EmploymentManager`). The demand bar in the UI and `AutoZoningManager` use these values when choosing where to zone.

**Tax base** — RCI development and population contribute to taxable capacity read by `EconomyManager` / `CityStats`; tax rates and income loop back into happiness and demand (see backlog for planned depth).

**Desirability** — per-cell attractiveness for zoning and AUTO growth based on terrain context (e.g. proximity to water, forests), computed after geography initialization. See `ARCHITECTURE.md` (initialization order, `GeographyManager` desirability pass) when changing how cells become more or less attractive.

## World features

> **Glossary index:** `glossary.md` cites this section as **mgrs §World**.

- **Forest** — Vegetation on land in **sparse**, **medium**, or **dense** states; affects demand and map tools (`ForestManager`).
- **Regional map** — Neighboring cities in the wider region; ties to regional systems and UI (`RegionalMapManager`).
- **Utility building** — Service structures (e.g. water treatment, power plants), distinct from RCI. Placement, multi-cell footprints, and AUTO placement follow `AutoResourcePlanner`, `ZoneManager`, and the same pivot rules as RCI buildings where applicable.

## Game notifications

> **Glossary index:** `glossary.md` cites this section as **mgrs §Notifications**.

In-game toasts and alerts (funds, placement errors, hints). Delivered only through **`GameNotificationManager.Instance`** — the project’s sole notification singleton. See **Architectural patterns** below for access rules.

## Architectural patterns

- Every manager is a **MonoBehaviour** living as a component on a scene GameObject.
- **Never** instantiate managers with `new` — always scene components.
- **GridManager** is the central hub — all cell operations go through it.
- Notifications: `GameNotificationManager.Instance` (only singleton).
- **CityStats** is the global data aggregator — read city-wide stats from here.
- Dependencies: `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`.

## Obsolete

- **`UrbanizationProposalManager`** and related proposal UI — obsolete, not run from `ProcessSimulationTick()`. Do not re-enable. **Glossary** **Urbanization proposal**; removal tracked on [`BACKLOG.md`](../../BACKLOG.md).
