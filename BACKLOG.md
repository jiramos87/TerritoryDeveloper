# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first).
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.

---

## In Progress

_No active issues._

## High Priority

- [ ] **BUG-02** — Taxes do not work
  - Type: fix
  - Files: `EconomyManager.cs`, `CityStats.cs`
  - Notes: Without taxes there is no functional economic loop.

- [ ] **BUG-03** — Growth % sets amount instead of percentage of total budget
  - Type: fix
  - Files: `GrowthManager.cs`, `GrowthBudgetManager.cs`, `CityStats.cs`
  - Notes: Must be percentage of total city budget.

- [ ] **BUG-05** — Do not remove cursor from buildings when constructing
  - Type: fix
  - Files: `GridManager.cs`, `CursorManager.cs`, `UIManager.cs`
  - Notes: User may want to keep building the same building. Keep selection active post-placement.

- [ ] **BUG-20** — Power plant (and 3x3/2x2 buildings) load incorrectly in LoadGame: end up under grass
  - Type: fix
  - Files: `GeographyManager.cs` (GetMultiCellBuildingMaxSortingOrder, ReCalculateSortingOrderBasedOnHeight), `BuildingPlacementService.cs` (LoadBuildingTile, RestoreBuildingTile), `GridManager.cs` (RestoreGridCellVisuals)
  - Notes: When loading a game, the power plant prefab (and possibly 2x2 water plant) is drawn under the grass tiles of the footprint. The pivot (64,102) has both grass and building as children; the building's sorting order can end up below the grass due to the "cap" in GetMultiCellBuildingMaxSortingOrder. Not yet fixed.

- [ ] **BUG-10** — `IndustrialHeavyZoning` never generates buildings
  - Type: fix
  - Files: `TimeManager.cs` (PlaceAllZonedBuildings)
  - Notes: `PlaceAllZonedBuildings` calls 8 of 9 zone types but omits `IndustrialHeavyZoning`. Heavy industrial buildings are never built.

- [ ] **BUG-11** — Demand uses `Time.deltaTime` causing framerate dependency
  - Type: fix
  - Files: `DemandManager.cs`
  - Notes: `Mathf.Lerp(..., demandSensitivity * Time.deltaTime)` makes demand change differently at 30 FPS vs 120 FPS. Must use fixed daily delta.

- [ ] **BUG-12** — Happiness UI always shows 50%
  - Type: fix
  - Files: `CityStatsUIController.cs` (GetHappiness)
  - Notes: `GetHappiness()` returns hardcoded `50.0f` instead of reading `cityStats.happiness`.

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

- [ ] **BUG-13** — `FindObjectOfType<TimeManager>()` called every tick in UrbanizationProposalManager
  - Type: fix (performance)
  - Files: `UrbanizationProposalManager.cs` (ProcessTick)
  - Notes: `FindObjectOfType` is expensive and runs every simulation tick. Cache in Start().

- [ ] **BUG-16** — Possible race condition in GeographyManager vs TimeManager initialization
  - Type: fix
  - Files: `GeographyManager.cs`, `TimeManager.cs`, `GridManager.cs`
  - Notes: Unity does not guarantee Start() order. If TimeManager.Update() runs before GeographyManager creates the grid, it may access non-existent data. Use Script Execution Order or gate with `isInitialized`.

- [ ] **BUG-17** — `cachedCamera` is null when creating `ChunkCullingSystem`
  - Type: fix
  - Files: `GridManager.cs`
  - Notes: In InitializeGrid() ChunkCullingSystem is created with `cachedCamera`, but it is only assigned in Update(). May cause NullReferenceException.

- [ ] **FEAT-01** — Add delta change to total budget (e.g. $25,000 (+$1,200))
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStatsUIController.cs`, `UIManager.cs`
  - Notes: Visual feedback of economic flow per turn.

- [ ] **FEAT-21** — Expenses and maintenance system
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`
  - Notes: No expenses: no street maintenance, no service costs, no salaries. Without expenses there is no economic tension. Add upkeep for streets, public buildings and services.

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

- [ ] **FEAT-24** — Auto-zoning for Medium and Heavy density
  - Type: feature
  - Files: `AutoZoningManager.cs`, `DemandManager.cs`
  - Notes: AutoZoningManager only places Light zones. Should support Medium/Heavy based on demand or zone development level.

- [ ] **FEAT-25** — Growth budget tied to real income
  - Type: feature
  - Files: `GrowthBudgetManager.cs`, `EconomyManager.cs`
  - Notes: Budget uses fixed amount (default 5000) unrelated to income. Should be percentage of projected monthly income.
  - Depends on: BUG-02, BUG-03

- [ ] **FEAT-26** — Use desirability for building spawn selection
  - Type: feature
  - Files: `ZoneManager.cs`, `DemandManager.cs` (GetCellDesirabilityBonus)
  - Notes: `DemandManager.GetCellDesirabilityBonus()` exists but is not used to decide where to build. Buildings should prefer zones with higher desirability.

- [ ] **BUG-06** — Streets should not cost so much energy
  - Type: fix/balance
  - Files: `RoadManager.cs`, `CityStats.cs`, `EconomyManager.cs`
  - Notes: Rebalance street energy cost.

- [ ] **BUG-07** — Better zone distribution: less random, more homogeneous by neighbourhoods/sectors
  - Type: fix
  - Files: `AutoZoningManager.cs`, `ZoneManager.cs`, `DemandManager.cs`
  - Notes: Zones are distributed very randomly and mixed. Should be grouped in coherent sectors.

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

- [ ] **FEAT-05** — Streets must be able to climb diagonal slopes using orthogonal prefabs
  - Type: feature
  - Files: `RoadManager.cs`, `TerrainManager.cs`, `GridManager.cs`
  - Notes: Streets currently do not climb diagonal slopes.

- [ ] **FEAT-06** — Forest that grows over time: sparse → medium → dense
  - Type: feature
  - Files: `ForestManager.cs`, `ForestMap.cs`, `SimulationManager.cs`
  - Notes: Forest maturation system over simulation time.

- [ ] **FEAT-07** — Test that randomized spawning works for zones
  - Type: feature/test
  - Files: `ZoneManager.cs`, `GrowthManager.cs`
  - Notes: Verify that random building spawning in zones works correctly.

- [ ] **FEAT-08** — Property value simulation, respawning and evolution to larger buildings
  - Type: feature
  - Files: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: Existing buildings evolve to larger versions based on zone property value.

## Code Health (technical debt)

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

- [ ] **TECH-04** — Remove direct access to `gridArray`/`cellArray` outside GridManager
  - Type: refactor
  - Files: `WaterManager.cs`, `GridSortingOrderService.cs`, `GeographyManager.cs`, `BuildingPlacementService.cs`
  - Notes: Project rule: use `GetCell(x, y)` instead of direct array access. Several classes violate this.

- [ ] **TECH-05** — Extract duplicated dependency resolution pattern
  - Type: refactor
  - Files: ~25+ managers with `if (X == null) X = FindObjectOfType<X>()` block
  - Notes: Consider helper method, base class, or extension method to reduce duplication of Inspector + FindObjectOfType fallback pattern.

## Low Priority

- [ ] **FEAT-09** — Trade / Production / Salaries
  - Type: feature (new system)
  - Files: `EconomyManager.cs`, `CityStats.cs` (+ new managers)
  - Notes: Economic system of production, trade between zones and salaries.

- [ ] **FEAT-10** — Regional contribution: monthly bonus for belonging to the state
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`, `RegionalMapManager.cs`
  - Notes: Additional monthly income for belonging to regional network.

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

- [ ] **FEAT-17** — Mini-map
  - Type: feature
  - Files: `CameraController.cs`, `UIManager.cs` (+ new controller)
  - Notes: Miniature view of full map for quick navigation.

- [ ] **FEAT-18** — Terrain generator (improved)
  - Type: feature
  - Files: `TerrainManager.cs`, `GeographyManager.cs`, `HeightMap.cs`
  - Notes: Terrain generator with more control and variety.

- [ ] **FEAT-19** — Map rotation / prefabs
  - Type: feature
  - Files: `CameraController.cs`, `GridManager.cs`, all rendering managers
  - Notes: Isometric view rotation. High impact on sorting order and rendering.

- [ ] **FEAT-20** — Start screen (superseded by FEAT-27)
  - Type: feature
  - Files: new scene + UI managers
  - Notes: Main menu with New Game, Load Game, Settings. Superseded by FEAT-27 (main menu with Continue, New Game, Load City, Options).

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

- [x] **BUG-21** — Zoning cost is not charged when placing zones (2026-03-09)
- [x] **FEAT-02** — Add construction cost counter to mouse cursor (2026-03-09)
- [x] **FEAT-28** — Right-click drag-to-pan (grab and drag map) with inertia/fling (2026-03-09)
- [x] **BUG-04** — Pause mode stops camera movement; camera speed tied to simulation speed (2026-03-09)
- [x] **BUG-18** — Road preview and placement draw discontinuous lines instead of continuous paths (2026-03-09)
- [x] **FEAT-27** — Main menu with Continue, New Game, Load City, Options (2026-03-08)
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
