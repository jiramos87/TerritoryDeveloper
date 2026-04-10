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
| **EconomyManager** | **Tax base** collection, **monthly maintenance**, treasury **money** via `SpendMoney` / `AddMoney`, tax rates |
| **CityStats** | Global statistics aggregator: population, employment, water/power capacity, **roadCount**, registered **power plant** list, **happiness** (0–100, multi-factor, per-tick recalculation), **pollution** (city-wide aggregate) |
| **EmploymentManager** | Employment and unemployment calculation by zone |
| **SimulationManager** | Automatic simulation cycle orchestrator |
| **AutoRoadBuilder** | Automatic **street** network extension |
| **AutoZoningManager** | Automatic zoning of cells adjacent to **streets**/**interstates** |
| **AutoResourcePlanner** | Automatic resource building planning |
| **GrowthManager** | Zone growth logic |
| **GrowthBudgetManager** | Growth budget per category; monthly total pool from projected net cash flow (**EconomyManager**) when positive |
| **UIManager** | Main **city** UI: popups, toolbar/tool state, demand bar. **`partial`** class: **`UIManager.cs`** (lifecycle, fields), **`UIManager.PopupStack.cs`**, **`UIManager.Hud.cs`**, **`UIManager.Toolbar.cs`**, **`UIManager.Utilities.cs`**. Shared tokens: **`UiTheme`** + **`ui-design-system.md`** (**Main menu** uses **`MainMenuController`**, not **`UIManager`**) |
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
| **MetricsRecorder** | Optional **fire-and-forget** Postgres inserts into **`city_metrics_history`** after each **`SimulationManager.ProcessSimulationTick`** invocation when **`DATABASE_URL`** resolves (**glossary** **City metrics history**). Invoked from **`SimulationManager`** in a **`finally`** block so **test mode** batch ticks still emit rows when **`simulateGrowth`** is false. Does **not** replace **Save data**. |

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

On each in-game **day**, after employment and pollution updates, `CityStats` computes a **happiness** target (including **tax** pressure from the **highest** R/C/I rate above a comfort band), lerps the displayed score toward that target, then `EmploymentManager.RefreshRCIDemandAfterDailyStats()` runs `DemandManager.UpdateRCIDemand` so **demand** sees **same-tick** tax and happiness targets. **Per-sector** tax pressure scales each R/C/I demand channel by that sector’s rate on `EconomyManager` (tunable on `DemandManager`). A **city-wide** multiplier from the happiness **target** (not only the lerped display value) further scales all three channels. Changing **tax** from the HUD also calls `CityStats.RefreshHappinessAfterPolicyChange()` so the score and **demand** update without waiting for the calendar **day**.

**Happiness** — City-wide 0–100 satisfaction score recalculated each **day** and on **tax** UI changes from employment rate, **highest** of the three **tax** rates (penalty above a comfort threshold), service coverage, forest bonus, development base, and pollution penalty; converges smoothly via lerp. Tax vs development vs service weights are tunable on the **`CityStats`** component (Inspector). Feeds back into R/C/I demand via the target-based multiplier above (`CityStats`, `DemandManager`).

**Tax base** — RCI development and population contribute to taxable capacity read by `EconomyManager` / `CityStats`; **tax** rates reduce appetite through **happiness** (**highest** sector rate vs comfort band) and **per-sector demand** scaling, while income flows through monthly collection.

**Monthly maintenance** — On calendar day 1 (via `TimeManager` → `EconomyManager.ProcessDailyEconomy` → `ProcessMonthlyEconomy`), after monthly **tax base** income is credited, `EconomyManager` charges **street** upkeep from `CityStats.roadCount` and **utility building** upkeep from `CityStats.GetRegisteredPowerPlantCount()` (v1: **power plants** only). Successful payment posts an informational **game notification** with a category breakdown; if the treasury cannot afford the full amount, no debit occurs and a **game notification** error explains the shortfall. Tunable per-road and per-plant costs live on `EconomyManager`. **Growth budget** projections subtract this maintenance from projected tax when computing net monthly cash flow.

**Desirability** — per-cell attractiveness for zoning and AUTO growth based on terrain context (e.g. proximity to water, forests), computed after geography initialization. See `ARCHITECTURE.md` (initialization order, `GeographyManager` desirability pass) when changing how cells become more or less attractive.

## World features

> **Glossary index:** `glossary.md` cites this section as **mgrs §World**.

- **Forest** — Vegetation on land in **sparse**, **medium**, or **dense** states; affects demand and map tools (`ForestManager`).
- **Regional map** — Neighboring cities in the wider region; ties to regional systems and UI (`RegionalMapManager`).
- **Utility building** — Service structures (e.g. water treatment, power plants), distinct from RCI. Placement, multi-cell footprints, and AUTO placement follow `AutoResourcePlanner`, `ZoneManager`, and the same pivot rules as RCI buildings where applicable.
- **Pollution** — City-wide environmental degradation. Sources: industrial **buildings** (heavy > medium > light contribution), polluting **utility buildings** (power plants — nuclear emits medium pollution, fossil-fuel plants emit high; future plants vary). Sinks: **forest** coverage absorbs pollution (diminishing returns at scale), future parks. Geographic and climatic base pollution planned for later. Pollution feeds into the **happiness** formula as a negative factor.

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
