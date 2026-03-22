# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first).
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.

---

## In Progress

  - [ ] **FEAT-37** — Multi-level water bodies and water system refactor (terrain-hosted water) — **lakes MVP epic**
  - Type: feature (epic) + refactor
  - Files: `WaterManager.cs`, `WaterMap.cs`, `WaterBody.cs`, `GeographyManager.cs`, `TerrainManager.cs`, `HeightMap.cs`, `GridManager.cs`, `GridSortingOrderService.cs`, `Cell.cs`, `CellData.cs`, `GameSaveManager.cs`; later `RoadManager.cs` (bridges), `ZoneManager.cs`, `ForestManager.cs`
  - Notes: **Scope (MVP):** **Lakes only** at variable surface heights; rivers, sea, flow, and manual terrain tools are **out of scope** (see **FEAT-38–FEAT-41**). **Problem:** Water used to behave as a single global surface tied to height 0. **Goal:** Water overlay (`WaterMap` + `WaterBody`) with per-body **surface height**, depression-fill generation from `HeightMap`, terrain floor stays in `HeightMap` (depth = surface − terrain). **Child issues (order):** **FEAT-37a** ✅ (data + generation — completed 2026-03-22) → **FEAT-37b** (rendering / sorting / coast prefabs / roads) → **FEAT-37c** (`WaterMapData` save-load + gameplay rules). **Spec:** `.cursor/specs/water-system-refactor.md`. **Epic closure:** **FEAT-37** is **completed** only when **FEAT-37a**, **FEAT-37b**, and **FEAT-37c** are all done. **Shore prefab polish:** **BUG-33**.
  - Depends on: **TECH-12** (planning pass — recommended)

## High Priority

- [ ] **BUG-31** — Wrong prefabs at interstate entry/exit (border)
  - Type: fix
  - Files: `RoadPrefabResolver.cs`, `RoadManager.cs`
  - Notes: Road must be able to enter/exit at border in any direction. Incorrect prefab selection at entry/exit cells. Isolated from BUG-30 for separate work.

- [ ] **BUG-28** — Sorting order between slope cell and interstate cell
  - Type: fix
  - Files: `GridManager.cs` (Sorting Order region), `TerrainManager.cs`, `RoadManager.cs`
  - Notes: Slope cells and interstate road cells render in wrong order; one draws over the other incorrectly.

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

- [ ] **FEAT-36** — Expand auto-zoning and auto-road candidates to include forests and slopes
  - Type: feature
  - Files: `GridManager.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`
  - Notes: Treat Grass, Forest, and N-S/E-W slopes as valid candidates for zoning and road expansion. Plan: `docs/plan-zoning-road-candidates-grass-forest-slopes.md`.

- [ ] **FEAT-35** — Area demolition tool (bulldozer drag-to-select)
  - Type: feature
  - Files: `GridManager.cs`, `UIManager.cs`, `CursorManager.cs`
  - Notes: Manual tool to demolish all buildings and zoning in a rectangular area at once. Use the same area selection mechanism as zoning: hold mouse button, drag to define rectangle, release to demolish. Reuse zoning's start/end position logic (zoningStartGridPosition, zoningEndGridPosition pattern). Demolish each cell in the selected area via DemolishCellAt. Interstate Highway cells must remain non-demolishable. Consider preview overlay (e.g. red tint) during drag.

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



- [ ] **BUG-32** — Lakes / `WaterMap` water not shown on minimap (desync with main map)
  - Type: fix (UX / consistency)
  - Files: `MiniMapController.cs`, `GeographyManager.cs`, `WaterManager.cs`, `WaterMap.cs`
  - Notes: Water visible in the game view (procedural lakes, sea-level merge) but missing or wrong on the minimap water layer (e.g. no blue where `WaterManager.IsWaterAt` / `WaterMap` is true). Investigate: null `WaterManager` on `MiniMapController`, `RebuildTexture` before `InitializeWaterMap`, `GetCellColor` not consulting water, water layer toggle / visibility. If **no** procedural lakes appear in the **world** view, check `LakeFillSettings.MaxLakeBoundingExtent` (too-small bbox rejects entire flood basins). Distinct from **FEAT-42** (height relief on minimap). World-view **shore / edge prefab** issues: **BUG-33**. Related: **FEAT-30**.
  - Depends on: nothing

- [ ] **BUG-33** — Lake shore / edge prefab bugs (incorrect tiles, gaps, alignment)
  - Type: fix
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `RefreshLakeShoreAfterLakePlacement`, `GetLakeShoreExtraWorldYOffset`, …), `GridSortingOrderService.cs`, `GeographyManager.cs` (sorting helpers), lake/coast prefabs under `Assets/Prefabs/` as needed
  - Notes: Fix incorrect or missing shore tiles at lake boundaries (cardinal/diagonal water slopes, Bay corners, upslope pairs), z-order / sorting glitches, and visual gaps after procedural lake placement. Overlaps **FEAT-37b** (generalize variable-height water rendering); this ticket tracks **prefab-edge defects** specifically. Related: **BUG-32** (minimap vs world).
  - Depends on: nothing

- [ ] **FEAT-37b** — Variable-height water rendering: slopes, sorting, bridges (no `height == 0` / `SEA_LEVEL` assumptions)
  - Type: feature + refactor
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `IsAdjacentToWaterHeight`, …), `GridSortingOrderService.cs`, `RoadPrefabResolver.cs`, `AutoRoadBuilder.cs`, `ForestManager.cs`
  - Notes: Generalize coast/water-slope prefabs and road bridge logic to use `WaterManager` / surface height instead of `SEA_LEVEL` and `cell.height == 0`. Align with lakes MVP rules (same prefabs, valid shores/bridges). Edge prefab defects: coordinate with **BUG-33**.
  - Depends on: **FEAT-37a** (completed)

- [ ] **FEAT-37c** — Persist `WaterMapData` in saves + gameplay rules on load
  - Type: feature
  - Files: `GameSaveManager.cs`, `GameManager.cs` / save payload, `WaterManager.cs`, `GeographyManager.cs`
  - Notes: Save/load v2 water bodies; restore order with height map; keep legacy save path working where applicable. Building/zoning adjacency vs water as agreed in spec.
  - Depends on: **FEAT-37b**

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
  - Files: `GridManager.cs` (~1870 lines), `TerrainManager.cs` (~1740), `CityStats.cs` (~1200), `ZoneManager.cs` (~1360), `UIManager.cs` (~1240), `RoadManager.cs` (~1730)
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

- [ ] **TECH-07** — ControlPanel: left vertical sidebar layout (category rows)
  - Type: refactor (UI/UX)
  - Files: `MainScene.unity` (`ControlPanel` hierarchy, RectTransform anchors, `LayoutGroup` / `ContentSizeFitter` as needed), `UIManager.cs` (only if toolbar/submenu positioning or references must follow the new dock), `UnitControllers/*SelectorButton.cs` (only if button wiring or parent references break after reparenting)
  - Spec sections: `.cursor/specs/ui-design-system.md` — **§3.3** (toolbar), **§1.3** (anchors/margins), **§4.3** (Canvas Scaler) as applicable.
  - Notes: Replace the bottom-centered horizontal **ribbon** with a **left-docked vertical** panel. Structure: **one row per category** (demolition, RCI zoning, utilities, roads, environment/forests, etc.), with **buttons laid out horizontally within each row** (e.g. `VerticalLayoutGroup` of rows, each row `HorizontalLayoutGroup`, or equivalent manual layout). Re-anchor dependent UI (e.g. zoning density / tool option overlays) so they align to the new sidebar instead of the old bottom bar. Verify safe area and Canvas Scaler at reference resolutions; avoid overlapping the mini-map and debug readouts. Document final hierarchy in `docs/ui-design-system-context.md`. Link program charter: `docs/ui-design-system-project.md` (Backlog bridge). Spec/docs ticketed and cross-linked in **TECH-08** (completed).

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

- [ ] **FEAT-38** — Rivers: downhill flow, connect lakes, generation polish
  - Type: feature
  - Files: `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `GeographyManager.cs`
  - Notes: Gradient-based paths, merge with **BUG-08** where appropriate. Depends on **FEAT-37c** (stable multi-level water model).

- [ ] **FEAT-39** — Sea / coast: edge region, infinite reservoir, tide direction (data)
  - Type: feature
  - Files: `WaterManager.cs`, `WaterMap.cs`, `TerrainManager.cs`, `GeographyManager.cs`
  - Notes: Coordinate with **FEAT-15** (ports). Depends on **FEAT-37c**.

- [ ] **FEAT-40** — Water sources & drainage (snowmelt, rain, overflow) — simulation
  - Type: feature
  - Files: new helpers + `WaterMap.cs`, `WaterManager.cs`, `SimulationManager.cs`
  - Notes: Not full fluid simulation; data-driven flow. Depends on **FEAT-37c** and possibly **FEAT-38**.

- [ ] **FEAT-41** — Water terrain tools (manual paint/modify, AUTO terraform) — extended
  - Type: feature
  - Files: `GridManager.cs`, `WaterManager.cs`, `UIManager.cs`, `TerraformingService.cs` (as needed)
  - Notes: Beyond legacy paint-at-sea-level. Depends on **FEAT-37c**.

- [ ] **FEAT-42** — Minimap: optional height / relief shading layer
  - Type: feature (UI)
  - Files: `MiniMapController.cs`, `HeightMap` / `GridManager` read access as needed
  - Notes: Visualize terrain elevation on the minimap (distinct from zones/roads/water layers). Does not replace logical water/zone data; base layer reliability stays in **FEAT-37a** / **FEAT-30** scope.
  - Depends on: none (can follow **FEAT-37a** polish)

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

- [x] **FEAT-37a** — WaterBody + WaterMap depression-fill (lake data & procedural placement) (2026-03-22)
  - Type: feature + refactor
  - Files: `WaterBody.cs`, `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `LakeFeasibility.cs`
  - Notes: `WaterBody` + per-cell body ids; `WaterMap.InitializeLakesFromDepressionFill` + `LakeFillSettings` (depression-fill, bounded pass, artificial fallback, merge); `LakeFeasibility` / `EnsureGuaranteedLakeDepressions` terrain bowls; `WaterMapData` v2 + legacy load; centered 40×40 template + extended terrain. **Follow-up:** shore prefab fixes **BUG-33**; rendering/save: **FEAT-37b** / **FEAT-37c**.

- [x] **TECH-12** — Water system refactor: planning pass (objectives, rules, scope, child issues) (2026-03-21)
  - Type: planning / documentation
  - Files: `.cursor/specs/water-system-refactor.md`, `BACKLOG.md` (FEAT-37, BUG-08 splits), `ARCHITECTURE.md` (Terrain / Water as needed)
  - Notes: **Goal:** Before implementation of **FEAT-37**, produce a single agreed definition of **objectives**, **rules** (data + gameplay + rendering), **known bugs** to fold in, **non-goals / phases**, and **concrete child issues** (IDs) ordered for development. Link outcomes in this spec and in `FEAT-37`. Overlaps **BUG-08** (generation), **FEAT-15** (ports/sea). **Does not** implement code — only backlog + spec updates and issue breakdown.
  - Depends on: nothing (blocks structured FEAT-37 execution)

- [x] **BUG-30** — Incorrect road prefabs when interstate climbs slopes (2026-03-20)
  - Type: fix
  - Files: `TerraformingService.cs`, `RoadPrefabResolver.cs`, `PathTerraformPlan.cs`, `RoadManager.cs` (shared pipeline)
  - Notes: Segment-based Δh for scale-with-slopes; corner/upslope cells use `GetPostTerraformSlopeTypeAlongExit` (aligned with travel); live-terrain fallback + `RestoreTerrainForCell` force orthogonal ramp when `action == None` and cardinal `postTerraformSlopeType`. See `docs/agent-prompt-interstate-slope-prefabs.md`. Verified in Unity.

- [x] **TECH-09** — Remove obsolete `TerraformNeeded` from TerraformingService (2026-03-20)
  - Type: refactor (dead code removal)
  - Files: `TerraformingService.cs`
  - Notes: Removed `[Obsolete]` `TerraformNeeded` and `GetOrthogonalFromRoadDirection` (only used by it). Path-based terraforming uses `ComputePathPlan` only.

- [x] **TECH-10** — Fix `TerrainManager.DetermineWaterSlopePrefab` north/south sea logic (2026-03-20)
  - Type: fix (code health)
  - Files: `TerrainManager.cs`
  - Notes: Replaced impossible `if (!hasSeaLevelAtNorth)` under `hasSeaLevelAtNorth` with NE/NW corner handling and East-style branch for sea north+south strips (`southEast` / `southEastUpslope`). South-only coast mirrors East; removed unreachable `hasSeaLevelAtSouth` else (handled by North block first).

- [x] **TECH-11** — Namespace `Territory.Terrain` for TerraformingService and PathTerraformPlan (2026-03-20)
  - Type: refactor
  - Files: `TerraformingService.cs`, `PathTerraformPlan.cs`, `ARCHITECTURE.md`, `.cursor/rules/project-overview.mdc`
  - Notes: Wrapped both types in `namespace Territory.Terrain`. Dependents already had `using Territory.Terrain`. Docs updated to drop “global namespace” examples for these files.

- [x] **TECH-08** — UI design system docs: TECH-07 (ControlPanel sidebar) ticketed and wired (2026-03-20)
  - Type: documentation
  - Files: `BACKLOG.md` (TECH-07), `docs/ui-design-system-project.md` (Backlog bridge), `docs/ui-design-system-context.md` (Toolbar — ControlPanel), `.cursor/specs/ui-design-system.md` (§3.3 layout variants), `ARCHITECTURE.md`, `AGENTS.md`, `.cursor/rules/managers-guide.mdc`
  - Notes: Executable toolbar refactor remains **TECH-07** (open). This issue records the documentation and cross-links only.

- [x] **BUG-25** — Fix bugs in manual street segment drawing (2026-03-19)
  - Type: fix
  - Files: `RoadManager.cs`, `RoadPrefabResolver.cs` (also: `GridManager.cs`, `TerraformingService.cs`, `PathTerraformPlan.cs`, `GridPathfinder.cs` for prior spec work)
  - Notes: Junction/T/cross prefabs: `HashSet` path membership + `SelectFromConnectivity` for 3+ cardinal neighbors in `RoadPrefabResolver`; post-placement `RefreshRoadPrefabAt` pass on placed cells in `TryFinalizeManualRoadPlacement`. Spec: `.cursor/specs/road-drawing-fixes.md`. Optional follow-up: `postTerraformSlopeType` on refresh (spec 2.1), crossroads prefab audit.
- [x] **BUG-27** — Interstate pathfinding bugs (2026-03-19)
  - Border endpoint scoring (`ComputeInterstateBorderEndpointScore`), sorted candidates, `PickLowerCostInterstateAStarPath` (avoid-high vs not, pick cheaper), `InterstateAwayFromGoalPenalty` and cost tuning in `RoadPathCostConstants`. Spec: `.cursor/specs/interstate-prefab-and-pathfinding-fixes.md` Phase 2.
- [x] **BUG-29** — Cut-through: high hills cut through disappear leaving crater (2026-03-19)
  - Reject cut-through when `maxHeight - baseHeight > 1`; cliff/corridor context in `TerrainManager` / `PathTerraformPlan`; map-edge margin `cutThroughMinCellsFromMapEdge`; Phase 1 validation ring in `PathTerraformPlan`; interstate uses `forbidCutThrough`. See `docs/plan-cut-through-craters.md`.
- [x] **FEAT-24** — Auto-zoning for Medium and Heavy density (2026-03-19)
- [x] **BUG-23** — Interstate route generation is flaky; never created in New Game flow (2026-03-19)
- [x] **BUG-26** — Interstate prefab selection and pathfinding improvements (2026-03-19)
  - Elbow audit, validation, straightness bonus, slope cost, parallel sampling, bridge approach (Rule F), cut-through expansion. Follow-up: BUG-27 / BUG-29 / **BUG-30** completed 2026-03-19–2026-03-20; remaining: BUG-28 (sorting), BUG-31 (prefabs at entry/exit).
- [x] **TECH-06** — Documentation sync: specs aligned with backlog and rules; BUG-26, FEAT-36 added; ARCHITECTURE, file counts, helper services updated; zoning plan translated to English (2026-03-19)
- [x] **FEAT-05** — Streets must be able to climb diagonal slopes using orthogonal prefabs (2026-03-18)
- [x] **FEAT-34** — Zoning and building on slopes (2026-03-16)
- [x] **FEAT-33** — Urban remodeling: expropriations and redevelopment (2026-03-12)
- [x] **FEAT-31** — Auto roads grow toward high desirability areas (2026-03-12)
- [x] **FEAT-30** — Mini map layer toggles + desirability visualization (2026-03-12)
- [x] **BUG-24** — Growth budget not recalculated when income changes (2026-03-12)
- [x] **BUG-06** — Streets should not cost so much energy (2026-03-12)
- [x] **FEAT-32** — More streets and intersections in central and mid-urban areas (AUTO mode) (2026-03-12)
- [x] **BUG-22** — Auto zoning must not block street segment ends (AUTO mode) (2026-03-11)
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
