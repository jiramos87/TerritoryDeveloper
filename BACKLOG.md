# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first).
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.

---

## In Progress

_(none)_

## High Priority

- [ ] **TECH-01** — Extract responsibilities from large files (focus: **GridManager** decomposition next)
  - Type: refactor
  - Files: `GridManager.cs` (~2070 lines), `TerrainManager.cs` (~3500), `CityStats.cs` (~1200), `ZoneManager.cs` (~1360), `UIManager.cs` (~1240), `RoadManager.cs` (~1730)
  - Notes: Helpers already extracted (`GridPathfinder`, `GridSortingOrderService`, `ChunkCullingSystem`, `RoadCacheService`, `BuildingPlacementService`, etc.). **Next candidates from GridManager:** `BulldozeHandler` (~200 lines), `GridInputHandler` (~130 lines), `CoordinateConversionService` (~230 lines). Prioritize this workstream; see `ARCHITECTURE.md` (GridManager hub trade-off).

- [ ] **BUG-37** — Manual street drawing clears buildings and zones on cells adjacent to the traced path
  - Type: bug
  - Files: `RoadManager.cs` (`HandleRoadDrawing`, road placement / commit path), `GridManager.cs` (road mode input, any demolish or clear calls near road segments), `TerrainManager.cs` / `TerraformingService.cs` if road placement widens the affected region; `ZoneManager.cs` if zoning is cleared outside road cells
  - Spec: `.cursor/specs/isometric-geography-system.md` §14 (manual streets; **BUG-25** completed — regression)
  - Notes: **Observed:** In **road drawing mode**, tracing a street **removes** (or clears) **zoning prefabs**, **zone buildings** (RCI), and **zoning** on cells **adjacent to the route**, not only on the road cells themselves (same report: manual street trace wipes zone visuals and spawned buildings). **Expected:** Only cells that actually receive the road (and any explicitly required footprint for valid placement) should be modified; **neighboring** zoned or built cells should remain unless the design intentionally requires a wider clear (document if so). Likely causes: over-broad dirty rect, neighbor iteration calling `DemolishCellAt` / zone clear, terraform brush larger than 1×1, or preview vs commit mismatch. **Related:** completed **BUG-25** (manual street segment drawing).
  - Depends on: none

- [ ] **BUG-49** — Manual road drawing: preview builds the route cell-by-cell (animated); should show full path at once
  - Type: bug (UX / preview)
  - Files: `RoadManager.cs` (`HandleRoadDrawing`, preview placement / ghost or temp prefab updates per frame), `GridManager.cs` if road mode input drives incremental preview; any coroutine or per-tick preview extension of the traced path
  - Spec: `.cursor/specs/isometric-geography-system.md` §14 (manual streets — preview behavior)
  - Notes: **Observed:** While drawing a street, **preview mode** visually **extends the route one cell at a time**, like an animation, instead of updating the full proposed path in one step. **Expected:** **No** step-by-step or staggered preview animation. The game should **compute the full valid path** (same rules as commit / `TryPrepareRoadPlacementPlan` or equivalent) for the current stroke, **then** instantiate or refresh **preview** prefabs for that complete path in a single update — or batch updates without visible per-cell delay. **Related:** **BUG-37** (adjacent clear during trace — ensure preview vs commit paths stay consistent when fixing).
  - Depends on: none

- [ ] **BUG-43** — Bridges over rivers/lakes adjacent to cliffs: gaps, floating segments, wrong alignment
  - Type: bug
  - Files: `RoadManager.cs` (`ValidateBridgePath`, `StraightenBridgeSegments` / bridge placement and commit), `RoadPrefabResolver.cs` (bridge vs land prefabs at height/water boundaries), `TerrainManager.cs` (cliff stacks, water shores, height continuity at cliff–water edges), `GridSortingOrderService.cs` (bridge `sortingOrder` over water vs land), `GridManager.cs` (road tile placement / refresh near cliffs); `InterstateManager.cs` if interstate bridge segments hit the same cases; `WaterManager.cs` / `WaterMap.cs` if body-type or surface rules affect placement
  - Spec: `.cursor/specs/isometric-geography-system.md` §14 (roads, bridges, validation); cliff/water interaction with roads as documented in §4–§7 and §12–§13 where relevant
  - Notes: **Observed:** When a street or bridge path crosses **water** (river or lake) **next to a vertical cliff** (height drop), bridge/road visuals break: **floating** segments at the upper level, **gaps** between cliff-top approach and lower water segments, **disconnected** path continuity, occasional **grass/terrain patches** on water under misplaced road tiles, and **misaligned** bridge tiles relative to cliff faces and water surface. **Expected:** A single continuous, correctly elevated bridge run with prefabs and **sorting** consistent with per-cell height and water vs land; no floating or orphaned road pieces at cliff–water junctions. **Related:** completed **BUG-42** (shore/cascade pipeline — ensure bridge refresh does not fight **`RefreshWaterCascadeCliffs`** / `RefreshLakeShoreAfterLakePlacement`).
  - Depends on: none

- [ ] **BUG-44** — Cliff prefabs: black gaps when a river or lake meets the **east** or **south** map edge
  - Type: bug
  - Files: `TerrainManager.cs` (`PlaceCliffWalls`, `PlaceCliffWallStack`, map-boundary / max-X / max-Y edge cases vs water cells), `WaterManager.cs` / `WaterMap.cs` if edge water placement interacts with cliff refresh; brown cliff / water-shore prefabs under `Assets/Prefabs/` (per `.cursor/rules/coding-conventions.mdc` for new or adjusted assets)
  - Spec: `.cursor/specs/isometric-geography-system.md` (map edges, water, cliffs, sorting — sections covering shore/cliff stacks at boundaries)
  - Notes: **Observed:** Where a **river channel** or **lake** reaches the **east** or **south** boundary of the grid, the **brown vertical cliff** geometry that seals the map edge is **missing or too short** under the water tiles, exposing **black void**; **grass** cells on the same edge still show correct cliff faces. Suggests boundary cliff stacks or prefab variants do not account for **lower water-bed elevation** at those edges. **Expected:** Continuous cliff wall to the same depth as neighboring land cliffs, or dedicated boundary + water prefabs so no holes at east/south × water. **Related:** completed **BUG-42** (virtual foot / edge cliffs — may share root cause with boundary × water placement).
  - Depends on: none

- [ ] **BUG-46** — Parallel rivers (same map-border exit): minimum spacing at entry and along course
  - Type: fix (procedural generation)
  - Files: **`ProceduralRiverGenerator.cs`** (Chebyshev dilation of prior corridors for BFS `avoid`, min **|Δx|**/**|Δy|** between same-border entry anchors, **`BuildForcedCenterline`** respects `avoid`), **`WaterManager.cs`** (`MergeAdjacentBodiesWithSameSurface` after `Generate`), **`WaterMap.cs`** (public **`MergeAdjacentBodiesWithSameSurface`**)
  - Spec: `.cursor/specs/isometric-geography-system.md` §13.3–§13.4 (BUG-46 row)
  - Notes: When multiple rivers are generated such that their **exit** cells lie on the **same map border** (parallel outflow), they can cluster: **entry** points too close on the interior border and/or **centerline paths** that run too near each other for too long. **Expected:** Enforce a **minimum grid distance** between **entry** points for rivers that share the same **exit** border, and a **minimum separation** between their **routes**. **Implementation (2026-03-27):** dilation radius **2** on prior corridor cells; entry separation **5** on north (N–S) or west (E–W) border; forced path skips blocked cells or aborts that river pass; post-pass **`MergeAdjacentBodiesWithSameSurface`** unifies touching river cells at same **S**. **Verify in Unity** (wide maps, forced-BFS mix); tune constants if needed. **Related:** **FEAT-38** (rivers — completed); completed **BUG-42** (re-verify shores/cascades if spacing changes).
  - Depends on: none

- [ ] **BUG-47** — AUTO simulation: perpendicular street stubs from auto-zoning gaps never built (orthogonal intersections missing)
  - Type: bug / feature
  - Files: `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs` (`ProcessSimulationTick` order), `UrbanCentroidService.cs` if ring/candidate logic gates stubs; `RoadManager.cs` / `GridPathfinder.cs` only if AUTO road placement reuses manual validation and rejects valid stubs
  - Spec: `.cursor/specs/isometric-geography-system.md` §14 (AUTO vs manual roads) — extend Notes here while **BUG-47** is open; no parallel spec file
  - Notes: **Design intent (observed in-game):** Along traced routes where **auto-zoning** runs, the system leaves **free land segments perpendicular** to the road so that **later** perpendicular connectors can be drawn, forming **orthogonal street crossings**. **Observed:** In **AUTO** simulation mode, those **perpendicular connectors are never built** even when **space remains clear**; the player must switch to **manual** road mode to complete the grid. **Expected:** Either **AUTO road growth** should propose and commit **short perpendicular stubs** (or equivalent path steps) from reserved gaps toward orthogonal intersections when placement rules allow, or document and implement an explicit alternative (e.g. dedicated AUTO pass after zoning) so behavior matches the layout auto-zoning prepares. **Related:** completed **BUG-22** (auto zoning must not block street ends); **FEAT-36** (expand auto-zoning / auto-road candidates — forests/slopes).
  - Depends on: none

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
  - Notes: Overlaps **BUG-35** (completed 2026-03-22): flat grass removed with buildings on load. **BUG-34** addressed general load/building sort. Re-verify in Unity after **BUG-35** closure; close if power plants / multi-cell utilities sort correctly.

- [ ] **BUG-12** — Happiness UI always shows 50%
  - Type: fix
  - Files: `CityStatsUIController.cs` (GetHappiness)
  - Notes: `GetHappiness()` returns hardcoded `50.0f` instead of reading `cityStats.happiness`. Blocks FEAT-23 (dynamic happiness).

- [ ] **BUG-14** — `FindObjectOfType` in Update/per-frame degrades performance
  - Type: fix (performance)
  - Files: `CursorManager.cs` (Update), `UIManager.cs` (UpdateUI)
  - Notes: `CursorManager.Update()` calls `FindObjectOfType<UIManager>()` every frame. `UIManager.UpdateUI()` calls `FindObjectOfType` for 4 managers repeatedly. Must be cached in Start().

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

- [ ] **BUG-48** — Minimap stays stale until toggling a layer (e.g. data-visualization / desirability / centroid)
  - Type: bug
  - Files: `MiniMapController.cs` (`RebuildTexture`, `Update`; layer toggles call `RebuildTexture` but nothing runs on simulation time), `TimeManager.cs` / `SimulationManager.cs` if wiring refresh to the simulation tick or a shared event
  - Notes: **Observed:** The procedural minimap **does not refresh** as the city changes unless the player **toggles a minimap layer** (or other actions that call `RebuildTexture`, such as opening the panel). **Expected:** The minimap should track **zones, roads, water, forests**, etc. **without** requiring layer toggles. **Implementation:** Rebuild at least **once per simulation tick** while the minimap is visible, **or** a **performance-balanced** approach (throttled full rebuild, dirty rect / incremental update, or event-driven refresh when grid/zone/road/water data changes) — profile full `RebuildTexture` cost first. Class summary in code states rebuilds on geography completion, grid restore, panel open, and layer changes **not** on a fixed timer — that gap is this bug. **Related:** completed **BUG-32** (water on minimap); **FEAT-42** (optional height layer).
  - Depends on: none

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
  - Notes: Treat Grass, Forest, and N-S/E-W slopes as valid candidates for zoning and road expansion. Capture any design notes in this issue or in `.cursor/specs/isometric-geography-system.md` if rules become stable.

- [ ] **FEAT-43** — Urban rings: tune AUTO road/zoning weights for a gradual center → edge gradient
  - Type: feature (simulation / balance)
  - Files: `UrbanCentroidService.cs` (ring boundaries, centroid distance), `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs` (`ProcessSimulationTick` order), `GrowthBudgetManager.cs` if per-ring budgets apply; `GridManager.cs` / `DemandManager.cs` only if desirability or placement must align with rings
  - Notes: **Observed:** In **AUTO** simulation, cities tend toward a **dense core**, **under-developed middle rings**, and **outer rings that are more zoned than the middle** — not a smooth radial gradient. **Expected:** Development should fall off **gradually from the urban center**: **highest** street density and zoning pressure **near the centroid**, **moderate** in **mid** rings, and **lowest** in **outer** rings. Revisit ring radii/thresholds, per-ring weights for road growth vs zoning, and any caps or priorities that invert mid vs outer activity. **Related:** completed **FEAT-32** (streets/intersections by area), **FEAT-29** (density gradient around centroids), **FEAT-31** (roads toward desirability); coordinate with **BUG-47** (AUTO perpendicular stubs) if road patterns depend on the same passes.
  - Depends on: none

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

- [ ] **FEAT-06** — Forest that grows over time: sparse → medium → dense
  - Type: feature
  - Files: `ForestManager.cs`, `ForestMap.cs`, `SimulationManager.cs`
  - Notes: Forest maturation system over simulation time.

- [ ] **FEAT-08** — Property value simulation, respawning and evolution to larger buildings
  - Type: feature
  - Files: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: Existing buildings evolve to larger versions based on zone property value.

- [ ] **TECH-15** — New Game / geography initialization performance (generation pipeline)
  - Type: performance / optimization
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `GridManager.cs`, `InterstateManager.cs`, `ForestManager.cs`, `RegionalMapManager.cs`, `ProceduralRiverGenerator.cs` (as applicable)
  - Notes: Reduce wall-clock time and frame spikes when starting a **New Game**: height map, lakes, procedural rivers (**FEAT-38**), interstate, forests, border signs, sorting passes, etc. Profile the pipeline; consider batched or deferred work across frames, fewer redundant passes, algorithmic improvements, and deferring non-critical visuals until after the map is interactive. **Related:** **FEAT-37c** optimizes **Load Game** (no regen) — this issue targets **generation** cost only.

- [ ] **TECH-16** — Simulation performance v2 (per-tick AUTO pipeline)
  - Type: performance / optimization
  - Files: `SimulationManager.cs`, `TimeManager.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `AutoResourcePlanner.cs`, `UrbanCentroidService.cs`, `GrowthBudgetManager.cs`, `DemandManager.cs`, `CityStats.cs` (as applicable)
  - Notes: Second-pass optimization of the simulation tick after early **Simulation optimization** work (completed). Profile `ProcessSimulationTick` and callees; reduce redundant work, hot-path cost, spatial queries, and per-tick allocations; preserve gameplay unless changes are explicitly agreed. **Related:** **BUG-14** (per-frame UI `FindObjectOfType`); **TECH-01** (manager decomposition may help profiling and hotspots).


## Code Health (technical debt)

- [ ] **TECH-13** — Remove obsolete **UrbanizationProposal** system (dead code, UI, models)
  - Type: refactor (cleanup)
  - Files: `UrbanizationProposalManager.cs`, `ProposalUIController.cs`, `UrbanizationProposal.cs` (and related), `SimulationManager.cs`, `UIManager.cs`, scene references, save data if any
  - Notes: The **urban expansion proposal** feature is **obsolete** and intentionally **disabled**; the game is stable without it. **Keep** `UrbanizationProposalManager` disconnected from the simulation — do **not** re-enable proposals. **Keep** `UrbanCentroidService` / urban **rings** for AUTO roads and zoning (FEAT-32). This issue tracks **full removal** of proposal-specific code and UI after a safe audit (no save-game breakage). Supersedes former **BUG-15** / **BUG-13**.

- [ ] **TECH-04** — Remove direct access to `gridArray`/`cellArray` outside GridManager
  - Type: refactor
  - Files: `WaterManager.cs`, `GridSortingOrderService.cs`, `GeographyManager.cs`, `BuildingPlacementService.cs`
  - Notes: Project rule: use `GetCell(x, y)` instead of direct array access. Several classes violate this. Risk of subtle bugs when grid changes.

- [ ] **TECH-02** — Change public fields to `[SerializeField] private` in managers
  - Type: refactor
  - Files: `ZoneManager.cs`, `RoadManager.cs`, `GridManager.cs`, `CityStats.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `UIManager.cs`, `WaterManager.cs`
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

- [ ] **TECH-14** — Remove residual placeholder / test scripts
  - Type: refactor (cleanup)
  - Files: `CityManager.cs` (namespace-only stub), `TestScript.cs` (compile smoke test)
  - Notes: Delete or replace with real content only if nothing references them; verify no scene/Inspector references.

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

- [x] **BUG-45** — Adjacent water bodies at different surface heights: merge, prefab refresh at intersections, straight slope/cliff transitions (2026-03-27)
  - Type: bug / polish
  - Files: `WaterManager.cs` (`UpdateWaterVisuals` — Pass A/B, `ApplyLakeHighToRiverLowContactFallback`), `WaterMap.cs` (`ApplyMultiBodySurfaceBoundaryNormalization`, `ApplyWaterSurfaceJunctionMerge`, `IsLakeSurfaceStepContactForbidden`, lake–river fallback), `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `SelectPerpendicularWaterCornerPrefabs`, `RefreshWaterCascadeCliffs`, `RefreshLakeShoreAfterLakePlacement`), `ProceduralRiverGenerator.cs` / `TestRiverGenerator.cs` as applicable; `docs/water-junction-merge-implementation-plan.md`
  - Spec: `.cursor/specs/isometric-geography-system.md` — **§5.6.2**, **§12.7**
  - Notes: **Completed (verified):** Pass A/B multi-body surface handling; lake-at-step exclusions; full-cardinal **`RefreshWaterCascadeCliffs`** (incl. mirror N/W lower pool); perpendicular multi-surface shore corner preference; lake-high vs river-low rim fallback. **Assign** `cliffWaterSouthPrefab` / **`cliffWaterEastPrefab`** on `TerrainManager` for visible cascades (west→east steps use **East**). Residual: map-edge water × cliff **BUG-44**; bridges × cliff-water **BUG-43**; optional N/W cascade art (camera).

- [x] **BUG-42** — Water shores & cliffs: terrain + water (lakes + rivers); water–water cascades; shore coherence — merged **BUG-33** + **BUG-41** (2026-03-26)
  - Type: bug / feature
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `PlaceCliffWalls`, `PlaceCliffWallStackCore`, `RefreshWaterCascadeCliffs`, `RefreshLakeShoreAfterLakePlacement`, `ClampShoreLandHeightsToAdjacentWaterSurface`, `IsLandEligibleForWaterShorePrefabs`), `WaterManager.cs` (`PlaceWater`, `UpdateWaterVisuals`), `ProceduralRiverGenerator.cs` (inner-corner shore continuity §13.5), `ProceduralRiverGenerator` / `WaterMap` as applicable; `cliffWaterSouthPrefab` & `cliffWaterEastPrefab` under `Assets/Prefabs/`
  - Spec: `.cursor/specs/isometric-geography-system.md` (§2.4.1 shore band height coherence, §4.2 gate, §5.6–§5.7, §5.6.2 water–water cascades, §12–§13, §15)
  - Notes: **Completed (verified):** **Shore band height coherence** — `HeightMap` clamp on Moore shore ring vs adjacent logical surface; water-shore prefab gate uses **`V = max(MIN_HEIGHT, S−1)`** vs **land height**. **River** inner-corner promotion + bed assignment guard. **Water–water cascades** — `RefreshWaterCascadeCliffs` after full `UpdateWaterVisuals`; **`PlaceCliffWallStackCore`** shared with brown cliffs; cascade Y anchor matches **water tile** (`GetWorldPositionVector` at `visualSurfaceHeight` + `tileHeight×0.25`). **Out of scope / follow-up:** visible **north/west** cliff meshes (camera); map edge water × cliff (**BUG-44**); bridges × cliff-water (**BUG-43**); optional **N/S/E/W** “waterfall” art beyond **S/E** stacks — track separately if needed. **Multi-body junctions:** completed **[BUG-45](#bug-45)** (2026-03-27).

- [x] **BUG-33** — Lake shore / edge prefab bugs — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **BUG-41** — River corridors: shore prefabs + cliff stacks — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **FEAT-38** — Procedural rivers during geography / terrain generation (2026-03-24)
  - Type: feature
  - Files: `GeographyManager.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterMap.cs`, `WaterManager.cs`, `WaterBody.cs`, `Cell.cs` / `CellData.cs` (as needed)
  - Spec: `.cursor/specs/isometric-geography-system.md` §12–§13
  - Notes: **Completed:** `WaterBody` classification + merge (river vs lake/sea); `GenerateProceduralRiversForNewGame()` after `InitializeWaterMap`, before interstate; `ProceduralRiverGenerator` (BFS / forced centerline, border margin, transverse + longitudinal monotonicity, `WaterMap` river bodies). **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (merged **BUG-33** + **BUG-41**, 2026-03-26).

- [x] **BUG-39** — Bay / inner-corner shore prefabs: cliff art alignment vs stacked cliffs (2026-03-24)
  - Type: fix (art vs code)
  - Files: `TerrainManager.cs` (`GetCliffWallSegmentWorldPositionOnSharedEdge`, `PlaceCliffWallStack`), `Assets/Sprites/Cliff/CliffEast.png`, `Assets/Sprites/Cliff/CliffSouth.png`, cliff prefabs under `Assets/Prefabs/Cliff/`
  - Notes: **Resolved:** Inspector-tunable per-face placement (`cliffWallSouthFaceNudgeTileWidthFraction` / `HeightFraction`, `cliffWallEastFaceNudgeTileWidthFraction` / `HeightFraction`) and water-shore Y offset (`cliffWallWaterShoreYOffsetTileHeightFraction`) so cliff sprites align with the south/east diamond faces and water-shore cells after art was moved inside the textures. Further shore/gap / cascade work → completed **[BUG-42](#bug-42)** (2026-03-26) where applicable.

- [x] **BUG-40** — Shore cliff walls draw in front of nearer (foreground) water tiles (2026-03-24)
  - Type: fix (sorting / layers)
  - Files: `TerrainManager.cs` (`PlaceCliffWallStack`, `GetMaxCliffSortingOrderFromForegroundWaterNeighbors`)
  - Notes: **Resolved:** Cliff `sortingOrder` is capped against registered **foreground** water neighbors (`nx+ny < highX+highY`) using their `Cell.sortingOrder`, so brown cliff segments do not draw above nearer water tiles. See `.cursor/specs/isometric-geography-system.md` §15.2.

- [x] **BUG-36** — Lake generation: seeded RNG (reproducible + varied per New Game) (2026-03-24)
  - Type: fix
  - Files: `WaterMap.cs` (`InitializeLakesFromDepressionFill`, `LakeFillSettings`), `WaterManager.cs`, `MapGenerationSeed.cs` (`GetLakeFillRandomSeed`), `TerrainManager.cs` (`EnsureGuaranteedLakeDepressions` shuffle)
  - Notes: `LakeFillSettings.RandomSeed` comes from map generation seed; depression-fill uses a seeded `System.Random`; bowl shuffle uses a derived seed. Same template no longer forces identical lake bodies across unrelated runs; fixed seed still reproduces. Spec: `.cursor/specs/isometric-geography-system.md` §12.3. **Related:** **BUG-08**, **FEAT-38**.

- [x] **BUG-35** — Load Game: multi-cell buildings — grass on footprint (non-pivot) could draw above building; 1×1 grass + building under one cell (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs` (`DestroyCellChildren`), `ZoneManager.cs` (`PlaceZoneBuilding`, `PlaceZoneBuildingTile`), `BuildingPlacementService.cs` (`UpdateBuildingTilesAttributes`), `GridSortingOrderService.cs` (`SetZoneBuildingSortingOrder`, `SyncCellTerrainLayersBelowBuilding`)
  - Notes: `DestroyCellChildren(..., destroyFlatGrass: true)` when placing/restoring **RCI and utility** buildings so flat grass prefabs are not kept alongside the building (runtime + load). Multi-cell `SetZoneBuildingSortingOrder` still calls **grass-only** `SyncCellTerrainLayersBelowBuilding` for each footprint cell. **BUG-20** may be re-verified against this. Spec: [`.cursor/specs/isometric-geography-system.md`](.cursor/specs/isometric-geography-system.md) §7.4.

- [x] **BUG-34** — Load Game: zone buildings / utilities render under terrain or water edges (`sortingOrder` snapshot vs building layer) (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs`, `ZoneManager.cs`, `TerrainManager.cs`, `BuildingPlacementService.cs`, `GridSortingOrderService.cs`, `Cell.cs`, `CellData.cs`, `GameSaveManager.cs`
  - Notes: Deterministic restore order; open water and shores aligned with runtime sorting; multi-cell RCI passes `buildingSize`; post-load building sort pass; optional grass sync via `SyncCellTerrainLayersBelowBuilding`. **BUG-35** (completed 2026-03-22) adds `destroyFlatGrass` on building placement/restore. Spec summary: `.cursor/specs/isometric-geography-system.md` §7.4.

- [x] **FEAT-37c** — Persist `WaterMapData` in saves + snapshot load (no terrain/water regen on load) (2026-03-22)
  - Type: feature
  - Files: `GameSaveManager.cs`, `WaterManager.cs`, `TerrainManager.cs`, `GridManager.cs`, `Cell.cs`, `CellData.cs`, `WaterBodyType.cs`
  - Notes: `GameSaveData.waterMapData`; `WaterManager.RestoreWaterMapFromSaveData`; `RestoreGridCellVisuals` applies saved `sortingOrder` and prefabs; legacy saves without `waterMapData` supported. **Follow-up:** building vs terrain sorting on load — **BUG-34** (completed); multi-cell footprint / grass under building — **BUG-35** (completed 2026-03-22).

- [x] **FEAT-37b** — Variable-height water: sorting, roads/bridges, `SEA_LEVEL` removal (no lake shore prefab scope) (2026-03-24)
  - Type: feature + refactor
  - Files: `GridSortingOrderService.cs`, `RoadPrefabResolver.cs`, `RoadManager.cs`, `AutoRoadBuilder.cs`, `ForestManager.cs`, `TerrainManager.cs` (water height queries, bridge/adjacency paths — **exclude** shore placement methods)
  - Notes: Legacy `SEA_LEVEL` / `cell.height == 0` assumptions removed or generalized for sorting, roads, bridges, non-shore water adjacency. Shore tiles **not** in scope (37a + completed **[BUG-42](#bug-42)**). Verified in Unity.

- [x] **BUG-32** — Lakes / `WaterMap` water not shown on minimap (desync with main map) (2026-03-23)
  - Type: fix (UX / consistency)
  - Files: `MiniMapController.cs`, `GeographyManager.cs`, `WaterManager.cs`, `WaterMap.cs`
  - Notes: Minimap water layer aligned with `WaterManager` / `WaterMap` (rebuild timing, `GetCellColor`, layer toggles). Verified in Unity.

- [x] **FEAT-37a** — WaterBody + WaterMap depression-fill (lake data & procedural placement) (2026-03-22)
  - Type: feature + refactor
  - Files: `WaterBody.cs`, `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `LakeFeasibility.cs`
  - Notes: `WaterBody` + per-cell body ids; `WaterMap.InitializeLakesFromDepressionFill` + `LakeFillSettings` (depression-fill, bounded pass, artificial fallback, merge); `LakeFeasibility` / `EnsureGuaranteedLakeDepressions` terrain bowls; `WaterMapData` v2 + legacy load; centered 40×40 template + extended terrain. **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (2026-03-26); **FEAT-37b** / **FEAT-37c** completed; building sort on load **BUG-34** (completed); multi-cell footprint / grass under building **BUG-35** (completed 2026-03-22).

- [x] **TECH-12** — Water system refactor: planning pass (objectives, rules, scope, child issues) (2026-03-21)
  - Type: planning / documentation
  - Files: `.cursor/specs/isometric-geography-system.md` (§12), `BACKLOG.md` (FEAT-37, BUG-08 splits), `ARCHITECTURE.md` (Terrain / Water as needed)
  - Notes: **Goal:** Before implementation of **FEAT-37**, produce a single agreed definition of **objectives**, **rules** (data + gameplay + rendering), **known bugs** to fold in, **non-goals / phases**, and **concrete child issues** (IDs) ordered for development. Link outcomes in this spec and in `FEAT-37`. Overlaps **BUG-08** (generation), **FEAT-15** (ports/sea). **Does not** implement code — only backlog + spec updates and issue breakdown.
  - Depends on: nothing (blocks structured FEAT-37 execution)

- [x] **BUG-30** — Incorrect road prefabs when interstate climbs slopes (2026-03-20)
  - Type: fix
  - Files: `TerraformingService.cs`, `RoadPrefabResolver.cs`, `PathTerraformPlan.cs`, `RoadManager.cs` (shared pipeline)
  - Notes: Segment-based Δh for scale-with-slopes; corner/upslope cells use `GetPostTerraformSlopeTypeAlongExit` (aligned with travel); live-terrain fallback + `RestoreTerrainForCell` force orthogonal ramp when `action == None` and cardinal `postTerraformSlopeType`. Spec: `.cursor/specs/isometric-geography-system.md` §14.7. Verified in Unity.

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
  - Notes: Junction/T/cross prefabs: `HashSet` path membership + `SelectFromConnectivity` for 3+ cardinal neighbors in `RoadPrefabResolver`; post-placement `RefreshRoadPrefabAt` pass on placed cells in `TryFinalizeManualRoadPlacement`. Spec: `.cursor/specs/isometric-geography-system.md` §14. Optional follow-up: `postTerraformSlopeType` on refresh, crossroads prefab audit.
- [x] **BUG-27** — Interstate pathfinding bugs (2026-03-19)
  - Border endpoint scoring (`ComputeInterstateBorderEndpointScore`), sorted candidates, `PickLowerCostInterstateAStarPath` (avoid-high vs not, pick cheaper), `InterstateAwayFromGoalPenalty` and cost tuning in `RoadPathCostConstants`. Spec: `.cursor/specs/isometric-geography-system.md` §14.5.
- [x] **BUG-29** — Cut-through: high hills cut through disappear leaving crater (2026-03-19)
  - Reject cut-through when `maxHeight - baseHeight > 1`; cliff/corridor context in `TerrainManager` / `PathTerraformPlan`; map-edge margin `cutThroughMinCellsFromMapEdge`; Phase 1 validation ring in `PathTerraformPlan`; interstate uses `forbidCutThrough`. Spec: `.cursor/specs/isometric-geography-system.md` §14.6.

- [x] **BUG-15** / **BUG-13** — UrbanizationProposal not wired / per-tick FindObjectOfType (2026-03-22 superseded)
  - Notes: The **UrbanizationProposal** system is **obsolete** and stays **disabled** by design. Do not fix by re-enabling. Full removal tracked under **TECH-13**.
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
