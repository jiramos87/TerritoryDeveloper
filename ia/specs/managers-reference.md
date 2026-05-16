---
purpose: "Reference spec for Managers & Services — Reference."
audience: agent
loaded_by: router
slices_via: spec_section
---
# Managers & Services — Reference

> Complete reference of all managers + helper services: responsibilities, dependencies, patterns.
> Full dependency map → `ARCHITECTURE.md`.

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
> Domain model for RCI zoning, building placement, multi-cell footprints. AUTO **street** walkability over light zoning → `isometric-geography-system.md` §13.9.

### RCI model

- **Residential (R), Commercial (C), Industrial (I)** = three zone categories. Each drives different demand, employment, building sets in economy layer (`DemandManager`, `ZoneManager`).
- Zoning placed per cell; buildings spawn on zoned cells when simulation + demand allow.

### Zone lifecycle

1. **Empty developable cell** — grass, forest, or other land player or AUTO may zone.
2. **Zoned cell** — `Zone` component marks cell with zone type + density tier (light / medium / heavy where applicable).
3. **Building** — growth rules fire → building prefab placed on zone footprint; zone tracks level + building reference.
4. **Upgrade** — `GrowthManager` may replace building with larger variant when property value / demand supports (see backlog issues for happiness + property value).

### Zone density

- **Light / medium / heavy** tiers control which building prefabs + footprints eligible. Higher tiers generally = larger or denser structures.
- AUTO simulation treats **undeveloped light zoning** (light tier, **no** building spawned) as pass-through terrain for **street** pathfinding only — see geography spec §13.9 + `AutoSimulationRoadRules`.

### Pivot cell + multi-cell buildings

- Buildings may occupy **1×1** or **2×2** (+ utility footprints as designed) cells.
- **Pivot cell** = anchor cell for multi-cell building. Other footprint cells reference pivot for sorting, save data, demolition. Non-pivot cells must stay consistent with pivot's building reference.

### Building footprint

Set of grid cells covered by single building instance (one tile or rectangle / multi-tile utility layout). Bulldozing, sorting, save/load, zone growth treat footprint as one unit anchored at **pivot cell**.

### Building placement + restore

- Runtime placement + load-game restore go through `BuildingPlacementService` + `GridManager` restore paths. Visual sorting on load follows geography spec §7.4 (visual restore).

## Demand (R / C / I)

> **Glossary index:** `glossary.md` cites this section as **mgrs §Demand**.

Residential, commercial, industrial **demand** scores express how strongly each zone type wants to grow. Derived from population, employment, forest cover, taxes, related aggregates (`DemandManager`, `CityStats`, `EmploymentManager`). Demand bar in UI + `AutoZoningManager` use these values when choosing where to zone.

On each in-game **day**, after employment + pollution updates, `CityStats` computes **happiness** target (including **tax** pressure from **highest** R/C/I rate above comfort band), lerps displayed score toward target, then `EmploymentManager.RefreshRCIDemandAfterDailyStats()` runs `DemandManager.UpdateRCIDemand` so **demand** sees **same-tick** tax + happiness targets. **Per-sector** tax pressure scales each R/C/I demand channel by that sector's rate on `EconomyManager` (tunable on `DemandManager`). **City-wide** multiplier from happiness **target** (not only lerped display value) further scales all three channels. Changing **tax** from HUD also calls `CityStats.RefreshHappinessAfterPolicyChange()` so score + **demand** update without waiting for calendar **day**.

**Happiness** — City-wide 0–100 satisfaction score recalculated each **day** + on **tax** UI changes from employment rate, **highest** of three **tax** rates (penalty above comfort threshold), service coverage, forest bonus, development base, pollution penalty; converges smoothly via lerp. Tax vs development vs service weights tunable on **`CityStats`** component (Inspector). Feeds back into R/C/I demand via target-based multiplier above (`CityStats`, `DemandManager`).

**Tax base** — RCI development + population contribute to taxable capacity read by `EconomyManager` / `CityStats`; **tax** rates reduce appetite through **happiness** (**highest** sector rate vs comfort band) + **per-sector demand** scaling, while income flows through monthly collection.

**Monthly maintenance** — On calendar day 1 (via `TimeManager` → `EconomyManager.ProcessDailyEconomy` → `ProcessMonthlyEconomy`), after monthly **tax base** income credited, `EconomyManager` charges **street** upkeep from `CityStats.roadCount` + **utility building** upkeep from `CityStats.GetRegisteredPowerPlantCount()` (v1: **power plants** only). Successful payment posts informational **game notification** with category breakdown; treasury can't afford full amount → no debit + **game notification** error explains shortfall. Tunable per-road + per-plant costs live on `EconomyManager`. **Growth budget** projections subtract maintenance from projected tax when computing net monthly cash flow.

**Desirability** — per-cell attractiveness for zoning + AUTO growth based on terrain context (e.g. proximity to water, forests), computed after geography initialization. See `ARCHITECTURE.md` (initialization order, `GeographyManager` desirability pass) when changing how cells become more or less attractive.

## World features

> **Glossary index:** `glossary.md` cites this section as **mgrs §World**.

- **Forest** — Vegetation on land in **sparse**, **medium**, or **dense** states; affects demand + map tools (`ForestManager`).
- **Regional map** — Neighboring cities in wider region; ties to regional systems + UI (`RegionalMapManager`).
- **Utility building** — Service structures (e.g. water treatment, power plants), distinct from RCI. Placement, multi-cell footprints, AUTO placement follow `AutoResourcePlanner`, `ZoneManager`, same pivot rules as RCI buildings where applicable.
- **Pollution** — City-wide environmental degradation. Sources: industrial **buildings** (heavy > medium > light contribution), polluting **utility buildings** (power plants — nuclear emits medium pollution, fossil-fuel plants emit high; future plants vary). Sinks: **forest** coverage absorbs pollution (diminishing returns at scale), future parks. Geographic + climatic base pollution planned for later. Pollution feeds into **happiness** formula as negative factor.

## Game notifications

> **Glossary index:** `glossary.md` cites this section as **mgrs §Notifications**.

In-game toasts + alerts (funds, placement errors, hints). Delivered only through **`GameNotificationManager.Instance`** — project's sole notification singleton. See **Architectural patterns** below for access rules.

## Architectural patterns

- Every manager = **MonoBehaviour** living as component on scene GameObject.
- **Never** instantiate managers with `new` — always scene components.
- **GridManager** = central hub — all cell operations go through it.
- Notifications: `GameNotificationManager.Instance` (only singleton).
- **CityStats** = global data aggregator — read city-wide stats from here.
- Dependencies: `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`.

## Obsolete

- **`UrbanizationProposalManager`** and related proposal UI — obsolete, not run from `ProcessSimulationTick()`. Do not re-enable. **Glossary** **Urbanization proposal**; removal tracked on [`BACKLOG.md`](../../BACKLOG.md).
