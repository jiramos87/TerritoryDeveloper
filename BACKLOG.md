# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first).
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.

---

## In Progress

(none)

## High Priority

- [ ] **FEAT-31** — Auto roads grow toward high desirability areas
  - Type: feature
  - Files: `AutoRoadBuilder.cs`, `DemandManager.cs` (GetCellDesirabilityBonus), `GridManager.cs`
  - Notes: Terrain desirability already affects building spawn in zones. In AUTO mode, roads should also tend to grow toward sectors with higher desirability, like in real life. Integrate desirability into road extension decisions.
  - Note: Partially implemented (roads prefer high-desirability directions). May need verification or refinement.

- [ ] **FEAT-33** — Urban remodeling: expropriations and redevelopment
  - Type: feature
  - Files: `GridManager.cs`, `ZoneManager.cs`, `RoadManager.cs`, `EconomyManager.cs`, new expropriation/remodeling logic
  - Notes: Expropriate buildings to demolish and build new routes; expropriate to demolish and de-zone for new neighborhoods according to updated desirability. Driven by variables: desirability, proximity to urban center, street density, etc. Needs further definition and design.
  - Related: FEAT-29, FEAT-31

- [ ] **FEAT-30** — Mini map layer toggles + desirability visualization
  - Type: feature
  - Files: `MiniMapController.cs`, `ShowMiniMapButton.cs`, `UIManager.cs`, `DemandManager.cs` (GetCellDesirabilityBonus), new layer-toggle UI
  - Notes: SimCity 2000-style mini map with toggle buttons at the edge. Each button toggles a layer: streets, urban zones, desirability, etc. Desirability layer: green (high/positive) to red (low/negative) color scale. Work order: (1) Create mini map button abstraction and panel; (2) Add toggle buttons for current data (streets, zones); (3) Add desirability layer as a specific case.

- [ ] **FEAT-32** — More streets and intersections in central urban areas (AUTO mode)
  - Type: feature
  - Files: `AutoRoadBuilder.cs`, `UrbanizationProposalManager.cs`, `CityStats.cs`, possible `UrbanCentroidService` or similar
  - Notes: Central urban sectors should have higher street density and more intersections. Far from centroids: lower density, longer straight roads (rural style). Define how to detect "central" areas and modulate AutoRoadBuilder behavior accordingly.
  - Related: FEAT-29 (density gradient around urban centroids)

- [ ] **FEAT-05** — Streets must be able to climb diagonal slopes using orthogonal prefabs
  - Type: feature
  - Files: `RoadManager.cs`, `TerrainManager.cs`, `GridManager.cs`
  - Notes: Streets currently do not climb diagonal slopes.


- [ ] **BUG-20** — Power plant (and 3x3/2x2 buildings) load incorrectly in LoadGame: end up under grass
  - Type: fix
  - Files: `GeographyManager.cs` (GetMultiCellBuildingMaxSortingOrder, ReCalculateSortingOrderBasedOnHeight), `BuildingPlacementService.cs` (LoadBuildingTile, RestoreBuildingTile), `GridManager.cs` (RestoreGridCellVisuals)
  - Notes: When loading a game, the power plant prefab (and possibly 2x2 water plant) is drawn under the grass tiles of the footprint. The pivot (64,102) has both grass and building as children; the building's sorting order can end up below the grass due to the "cap" in GetMultiCellBuildingMaxSortingOrder. Not yet fixed.

- [ ] **BUG-12** — Happiness UI always shows 50%
  - Type: fix
  - Files: `CityStatsUIController.cs` (GetHappiness)
  - Notes: `GetHappiness()` returns hardcoded `50.0f` instead of reading `cityStats.happiness`. Blocks FEAT-23 (dynamic happiness).

- [ ] **BUG-14** — `FindObjectOfType` in Update/per-frame degrades performance
  - Type: fix (performance)
  - Files: `CursorManager.cs` (Update), `UIManager.cs` (UpdateUI)
  - Notes: `CursorManager.Update()` calls `FindObjectOfType<UIManager>()` every frame. `UIManager.UpdateUI()` calls `FindObjectOfType` for 4 managers repeatedly. Must be cached in Start().

- [ ] **BUG-15** — `UrbanizationProposalManager` not connected to simulation
  - Type: fix
  - Files: `SimulationManager.cs`, `UrbanizationProposalManager.cs`
  - Notes: `SimulationManager.ProcessSimulationTick()` never calls `UrbanizationProposalManager.ProcessTick()`. Urbanization proposals are disabled.

## Medium Priority

- [ ] **BUG-19** — Mouse scroll wheel in Load Game scrollable menu also triggers camera zoom
  - Type: fix (UX)
  - Files: `CameraController.cs` (HandleScrollZoom), `UIManager.cs` (loadGameMenu, savedGamesListContainer), `MainScene.unity` (LoadGameMenuPanel / Scroll View hierarchy)
  - Notes: When scrolling over the Load Game save list, the mouse wheel scrolls the list AND zooms the camera. The scroll should only move the list up/down, not affect camera zoom or other game mechanisms that use the scroll wheel.
  - Proposed solution: In `CameraController.HandleScrollZoom()`, check `EventSystem.current.IsPointerOverGameObject()` before processing scroll. If the pointer is over UI (e.g. Load Game panel, Building Selector, any scrollable popup), skip the zoom logic and let the UI consume the scroll. This mirrors how `GridManager` already gates mouse clicks via `IsPointerOverGameObject()`. Requires `using UnityEngine.EventSystems`. Verify that the Load Game ScrollRect (Scroll View) has proper raycast target so `IsPointerOverGameObject()` returns true when hovering over it.

- [ ] **BUG-16** — Possible race condition in GeographyManager vs TimeManager initialization
  - Type: fix
  - Files: `GeographyManager.cs`, `TimeManager.cs`, `GridManager.cs`
  - Notes: Unity does not guarantee Start() order. If TimeManager.Update() runs before GeographyManager creates the grid, it may access non-existent data. Use Script Execution Order or gate with `isInitialized`.

- [ ] **BUG-17** — `cachedCamera` is null when creating `ChunkCullingSystem`
  - Type: fix
  - Files: `GridManager.cs`
  - Notes: In InitializeGrid() ChunkCullingSystem is created with `cachedCamera`, but it is only assigned in Update(). May cause NullReferenceException.

- [ ] **BUG-13** — `FindObjectOfType<TimeManager>()` called every tick in UrbanizationProposalManager
  - Type: fix (performance)
  - Files: `UrbanizationProposalManager.cs` (ProcessTick)
  - Notes: `FindObjectOfType` is expensive and runs every simulation tick. Cache in Start().

- [ ] **FEAT-21** — Expenses and maintenance system
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`
  - Notes: No expenses: no street maintenance, no service costs, no salaries. Without expenses there is no economic tension. Add upkeep for streets, public buildings and services.

- [ ] **FEAT-24** — Auto-zoning for Medium and Heavy density
  - Type: feature
  - Files: `AutoZoningManager.cs`, `DemandManager.cs`
  - Notes: AutoZoningManager only places Light zones. Should support Medium/Heavy based on demand or zone development level.

- [ ] **BUG-06** — Streets should not cost so much energy
  - Type: fix/balance
  - Files: `RoadManager.cs`, `CityStats.cs`, `EconomyManager.cs`
  - Notes: Rebalance street energy cost.

- [ ] **FEAT-22** — Tax feedback on demand and happiness
  - Type: feature
  - Files: `EconomyManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: High taxes do not affect demand or happiness. Loop: high taxes → less residential demand → less growth → less income.
  - Depends on: BUG-02

- [ ] **FEAT-23** — Dynamic happiness based on city conditions
  - Type: feature
  - Files: `CityStats.cs`, `DemandManager.cs`, `EmploymentManager.cs`
  - Notes: Happiness only increases when placing zones (+100 per building). No effect from unemployment, taxes, services or pollution. Should be continuous multi-factor calculation with decay.
  - Depends on: BUG-12

- [ ] **FEAT-03** — Forest mode hold-to-place
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`
  - Notes: Currently requires click per cell. Allow continuous drag.

- [ ] **FEAT-04** — Random forest spray tool
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`, `CursorManager.cs`
  - Notes: Place forest in area with random spray/brush distribution.

- [ ] **BUG-08** — More small rivers, rivers reach lakes, define sea at corner/edge
  - Type: fix/feature
  - Files: `WaterManager.cs`, `WaterMap.cs`, `GeographyManager.cs`
  - Notes: Improve map water generation.

- [ ] **FEAT-06** — Forest that grows over time: sparse → medium → dense
  - Type: feature
  - Files: `ForestManager.cs`, `ForestMap.cs`, `SimulationManager.cs`
  - Notes: Forest maturation system over simulation time.

- [ ] **FEAT-08** — Property value simulation, respawning and evolution to larger buildings
  - Type: feature
  - Files: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: Existing buildings evolve to larger versions based on zone property value.


## Code Health (technical debt)

- [ ] **TECH-04** — Remove direct access to `gridArray`/`cellArray` outside GridManager
  - Type: refactor
  - Files: `WaterManager.cs`, `GridSortingOrderService.cs`, `GeographyManager.cs`, `BuildingPlacementService.cs`
  - Notes: Project rule: use `GetCell(x, y)` instead of direct array access. Several classes violate this. Risk of subtle bugs when grid changes.

- [ ] **TECH-01** — Extract responsibilities from large files (GridManager, TerrainManager, CityStats, ZoneManager, UIManager, RoadManager)
  - Type: refactor
  - Files: `GridManager.cs` (1538 lines), `TerrainManager.cs` (1330), `CityStats.cs` (1199), `ZoneManager.cs` (1170), `UIManager.cs` (1054), `RoadManager.cs` (1019)
  - Notes: Helpers already extracted (GridPathfinder, GridSortingOrderService, etc.). Pending candidates: BulldozeHandler (~200 lines), GridInputHandler (~130 lines), CoordinateConversionService (~230 lines) from GridManager.

- [ ] **TECH-02** — Change public fields to `[SerializeField] private` in managers
  - Type: refactor
  - Files: `ZoneManager.cs`, `RoadManager.cs`, `GridManager.cs`, `CityStats.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `UIManager.cs`, `WaterManager.cs`, `UrbanizationProposalManager.cs`
  - Notes: Dependencies and prefabs exposed as `public` allow accidental access from any class. Use `[SerializeField] private` to encapsulate.

- [ ] **TECH-03** — Extract magic numbers to constants or ScriptableObjects
  - Type: refactor
  - Files: multiple (GridManager, CityStats, RoadManager, UIManager, TimeManager, TerrainManager, WaterManager, EconomyManager, ForestManager, InterstateManager, etc.)
  - Notes: Building costs, economic balance, generation parameters, sorting order offsets, initial dates, probabilities — all hardcoded. Extract to named constants or configuration ScriptableObject for easier tuning.

- [ ] **TECH-05** — Extract duplicated dependency resolution pattern
  - Type: refactor
  - Files: ~25+ managers with `if (X == null) X = FindObjectOfType<X>()` block
  - Notes: Consider helper method, base class, or extension method to reduce duplication of Inspector + FindObjectOfType fallback pattern.

## Low Priority

- [ ] **FEAT-09** — Trade / Production / Salaries
  - Type: feature (new system)
  - Files: `EconomyManager.cs`, `CityStats.cs` (+ new managers)
  - Notes: Economic system of production, trade between zones and salaries.

- [ ] **FEAT-18** — Terrain generator (improved)
  - Type: feature
  - Files: `TerrainManager.cs`, `GeographyManager.cs`, `HeightMap.cs`
  - Notes: Terrain generator with more control and variety.

- [ ] **FEAT-10** — Regional contribution: monthly bonus for belonging to the state
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`, `RegionalMapManager.cs`
  - Notes: Additional monthly income for belonging to regional network.

- [ ] **FEAT-19** — Map rotation / prefabs
  - Type: feature
  - Files: `CameraController.cs`, `GridManager.cs`, all rendering managers
  - Notes: Isometric view rotation. High impact on sorting order and rendering.

- [ ] **FEAT-11** — Education level / Schools
  - Type: feature (new system)
  - Files: new managers + `CityStats.cs`, `DemandManager.cs`
  - Notes: Education system affecting demand and growth.

- [ ] **FEAT-12** — Security / Order / Police
  - Type: feature (new system)
  - Files: new managers + `CityStats.cs`
  - Notes: Public security system.

- [ ] **FEAT-13** — Fire / Fire risk / Firefighters
  - Type: feature (new system)
  - Files: new managers + `CityStats.cs`
  - Notes: Fire risk and firefighter service system.

- [ ] **FEAT-14** — Vehicle traffic system / traffic animations
  - Type: feature (new system)
  - Files: new manager + `RoadManager.cs`, `GridManager.cs`
  - Notes: Vehicles circulating on streets.

- [ ] **FEAT-15** — Port system / cargo ship animations
  - Type: feature (new system)
  - Files: new manager + `WaterManager.cs`
  - Notes: Requires water system with defined sea (depends on BUG-08).

- [ ] **FEAT-16** — Train system / train animations
  - Type: feature (new system)
  - Files: new manager + `GridManager.cs`
  - Notes: Railway network and animations.

- [ ] **ART-01** — Missing prefabs: forests on SE, NE, SW, NW slopes
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ForestManager.cs`

- [ ] **ART-02** — Missing prefabs: residential (2 heavy 1x1/2x2, light 2x2, medium 1x1)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-03** — Missing prefabs: commercial (2 heavy 2x2/1x1, light 2x2, medium 2x2)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-04** — Missing prefabs: industrial (2 heavy 2x2/1x1, light 1x1, 2 medium 1x1/2x2)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **AUDIO-01** — Audio FX: demolition, placement, zoning, forest, 3 music themes, ambient effects
  - Type: audio/feature
  - Files: new AudioManager + audio assets
  - Notes: Ambient effects must vary by camera position and height over the map.

---

## Completed (last 30 days)

- [x] **FEAT-25** — Growth budget tied to real income (2026-03-11)
- [x] **BUG-10** — `IndustrialHeavyZoning` never generates buildings (2026-03-11)
- [x] **FEAT-26** — Use desirability for building spawn selection (2026-03-10)
- [x] **BUG-07** — Better zone distribution: less random, more homogeneous by neighbourhoods/sectors (2026-03-10)
- [x] **FEAT-29** — Density gradient around urban centroids (AUTO mode) (2026-03-10)
- [x] **FEAT-17** — Mini-map (2026-03-09)
- [x] **FEAT-01** — Add delta change to total budget (e.g. $25,000 (+$1,200)) (2026-03-09)
- [x] **BUG-03** — Growth % sets amount instead of percentage of total budget (2026-03-09)
- [x] **BUG-02** — Taxes do not work (2026-03-09)
- [x] **BUG-05** — Do not remove cursor preview from buildings when constructing (2026-03-09)
- [x] **BUG-21** — Zoning cost is not charged when placing zones (2026-03-09)
- [x] **FEAT-02** — Add construction cost counter to mouse cursor (2026-03-09)
- [x] **FEAT-28** — Right-click drag-to-pan (grab and drag map) with inertia/fling (2026-03-09)
- [x] **BUG-04** — Pause mode stops camera movement; camera speed tied to simulation speed (2026-03-09)
- [x] **BUG-18** — Road preview and placement draw discontinuous lines instead of continuous paths (2026-03-09)
- [x] **FEAT-27** — Main menu with Continue, New Game, Load City, Options (2026-03-08)
- [x] **BUG-11** — Demand uses `Time.deltaTime` causing framerate dependency (2026-03-11)
- [x] **BUG-21** — Demand fix: unemployment-based RCI, remove environmental from demand, desirability for density (2026-03-11)
- [x] **BUG-01** — Save game, Load game and New game were broken (2026-03-07)
- [x] **BUG-09** — `Cell.GetCellData()` does not serialize cell state (2026-03-07)
- [x] **DONE** — Forest cannot be placed adjacent to water (2026-03)
- [x] **DONE** — Demolish forests at all heights + all building types (2026-03)
- [x] **DONE** — When demolishing forest on slope, correct terrain prefab restored via heightMap read (2026-03)
- [x] **DONE** — Interstate Road (2026-03)
- [x] **DONE** — CityNetwork sim (2026-03)
- [x] **DONE** — Forests on slopes (2026-03)
- [x] **DONE** — Growth simulation — AUTO mode (2026-03)
- [x] **DONE** — Simulation optimization (2026-03)
- [x] **DONE** — Codebase improvement for efficient AI agent contextualization (2026-03)

---

## How to Use This Backlog

1. **Work on an issue**: Open chat in Cursor, reference `@BACKLOG.md` and request analysis or implementation of the issue by ID (e.g. "Analyze BUG-01 and propose a plan").
2. **Reprioritize**: Move the issue up or down within its section, or change section.
3. **Add new issue**: Assign the next available ID in the appropriate category and place in the correct priority section.
4. **Complete issue**: Move to "Completed" section with date, mark checkbox as `[x]`.
5. **In progress**: Move to "In progress" section when starting work.
6. **Dependencies**: Use `Depends on: ID` field when an issue requires another to be completed first. Check dependencies before starting.

### ID Convention
| Prefix | Category |
|--------|----------|
| `BUG-XX` | Bugs and broken functionality |
| `FEAT-XX` | Features and enhancements |
| `TECH-XX` | Technical debt, refactors, code health |
| `ART-XX` | Art assets, prefabs, sprites |
| `AUDIO-XX` | Audio assets and audio system features |

### Issue Fields
- **Type**: fix, feature, refactor, art/assets, audio/feature, etc.
- **Files**: main files involved
- **Notes**: context, problem description or expected solution
- **Depends on** (optional): IDs of issues that must be completed first

### Section Order
1. In progress (actively being developed)
2. High priority (critical bugs, core gameplay blockers)
3. Medium priority (important features, balance, improvements)
4. Code Health (technical debt, refactors, performance)
5. Low priority (new systems, polish, content)
6. Completed (last 30 days)
