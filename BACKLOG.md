# Backlog — Territory Developer

> Single source of truth for project issues. Ordered by priority (highest first).
> To work on an issue: reference it with `@BACKLOG.md` in the Cursor conversation.

---

## In Progress
- [ ] **BUG-37** — Manual **street** drawing clears **buildings** and **zones** on cells adjacent to the **road stroke**
  - Type: bug
  - Files: `RoadManager.cs` (`HandleRoadDrawing`, **road validation pipeline** / commit path), `GridManager.cs` (road mode input, any demolish or clear calls near **street** segments), `TerrainManager.cs` / `TerraformingService.cs` if the **terraform plan** widens the affected region; `ZoneManager.cs` if **zoning** is cleared outside **street** cells
  - Spec: `.cursor/specs/isometric-geography-system.md` §14 (manual **streets**; **BUG-25** completed — regression)
  - Notes: **Observed:** In **street drawing mode**, tracing a **street** **removes** (or clears) **zone** prefabs, **buildings** (**RCI**), and **zoning** on cells **adjacent to the road stroke**, not only on the **street** cells themselves (same report: manual **street** trace wipes **zone** visuals and spawned **buildings**). **Expected:** Only cells that actually receive the **street** (and any explicitly required footprint for valid placement) should be modified; **neighboring** zoned or built cells should remain unless the design intentionally requires a wider clear (document if so). Likely causes: over-broad dirty rect, neighbor iteration calling `DemolishCellAt` / zone clear, **terraform plan** scope larger than 1×1, or preview vs commit mismatch. **Related:** completed **BUG-25** (manual **street** segment drawing).
  - Acceptance: Drawing a **street** through zoned/built area modifies only cells on the **road stroke**; adjacent **zones**, **buildings**, and prefabs remain intact
  - Depends on: none

- [ ] **TECH-22** — Canonical terminology pass on **reference specs** (`.cursor/specs`)
  - Type: documentation / refactor (IA)
  - Files: All **reference specs** under `.cursor/specs/` (`glossary.md`, `isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, `REFERENCE-SPEC-STRUCTURE.md`); [`AGENTS.md`](AGENTS.md) if inventory text changes; [`tools/mcp-ia-server/src/config.ts`](tools/mcp-ia-server/src/config.ts) and MCP docs only if spec keys/aliases change
  - Spec: [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) (canonical domain terms); [`.cursor/specs/REFERENCE-SPEC-STRUCTURE.md`](.cursor/specs/REFERENCE-SPEC-STRUCTURE.md) (authoring rules)
  - Notes: **Goal:** Replace non-canonical or ad-hoc wording across **reference specs** with **glossary** vocabulary so search, MCP, and backlog stay aligned. **Method:** Review **one file at a time** in dependency order: `glossary.md` → `isometric-geography-system.md` → `roads-system.md` / `water-terrain-system.md` → `simulation-system.md` / `persistence-system.md` → `managers-reference.md` → `ui-design-system.md` → `REFERENCE-SPEC-STRUCTURE.md`. Use targeted search for common synonyms (e.g. informal “road” vs **street**/**interstate**, “map edge” vs **map border**). **Conflict rule:** Where glossary and a spec disagree on meaning, the **reference spec** section wins — update the glossary to defer or align (per glossary header). **New concepts:** add glossary row **and** authoritative spec text; do not leave terms only in this issue. **Optional:** Maintain a single **deprecated synonym → canonical** table in TECH-22 notes or in `REFERENCE-SPEC-STRUCTURE.md` during the pass. **Out of scope (unless expanded):** `.cursor/rules/*.mdc`, `docs/`, `ARCHITECTURE.md` — track as a follow-up issue if needed.
  - Acceptance: Checklist in issue notes marks every inventory **reference spec** reviewed; no unresolved glossary ↔ spec contradictions; [`AGENTS.md`](AGENTS.md) inventory and MCP spec keys remain coherent
  - Depends on: none

- [ ] **FEAT-45** — MCP **`glossary_discover`**: keyword-style discovery over **glossary** rows
  - Type: feature (IA / tooling)
  - Files: `tools/mcp-ia-server/src/tools/glossary-discover.ts`, `tools/mcp-ia-server/src/parser/glossary-discover-rank.ts`, `tools/mcp-ia-server/tests/parser/glossary-discover-rank.test.ts`, `tools/mcp-ia-server/tests/tools/glossary-discover.test.ts`, `tools/mcp-ia-server/scripts/verify-mcp.ts`, [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md), [`AGENTS.md`](AGENTS.md), [`.cursor/rules/agent-router.mdc`](.cursor/rules/agent-router.mdc)
  - Spec: [`.cursor/projects/FEAT-45.md`](.cursor/projects/FEAT-45.md)
  - Notes: **Phase A (shipped):** deterministic ranking using **Term**, **Definition**, **Spec**, and category text; optional **`spec`** alias + **`registryKey`** from the Spec cell; complements **`glossary_lookup`**. **Phase B** (spec body scoring) deferred. Verify: `npm test` and `npm run verify` under `tools/mcp-ia-server/`.
  - Acceptance: Fixture proves definition-only keywords surface the correct **Term**; `npm test` / `npm run verify` pass; docs and router text list the tool
  - Depends on: **TECH-17** (MCP IA server — baseline)

## High Priority
- [ ] **BUG-49** — Manual **street** drawing: preview builds the **road stroke** cell-by-cell (animated); should show full path at once
  - Type: bug (UX / preview)
  - Files: `RoadManager.cs` (`HandleRoadDrawing`, preview placement / ghost or temp prefab updates per frame), `GridManager.cs` if road mode input drives incremental preview; any coroutine or per-tick preview extension of the **road stroke**
  - Spec: `.cursor/specs/isometric-geography-system.md` §14 (manual **streets** — preview behavior)
  - Notes: **Observed:** While drawing a **street**, **preview mode** visually **extends the road stroke one cell at a time**, like an animation, instead of updating the full proposed **road stroke** in one step. **Expected:** **No** step-by-step or staggered preview animation. The game should **compute the full valid road stroke** (same rules as commit / **road validation pipeline** / `TryPrepareRoadPlacementPlan` or equivalent) for the current **stroke**, **then** instantiate or refresh **preview** prefabs for that complete **road stroke** in a single update — or batch updates without visible per-cell delay. **Related:** **BUG-37** (adjacent clear during trace — ensure preview vs commit paths stay consistent when fixing).
  - Acceptance: **Street** preview shows the full computed **road stroke** in one visual update; no visible cell-by-cell animation during drag
  - Depends on: none

- [ ] **BUG-44** — **Cliff** prefabs: black gaps when a **water body** (**river** or **lake**) meets the **east** or **south** **map border**
  - Type: bug
  - Files: `TerrainManager.cs` (`PlaceCliffWalls`, `PlaceCliffWallStack`, **map border** / max-X / max-Y edge cases vs **open water** cells), `WaterManager.cs` / `WaterMap.cs` if edge water placement interacts with **shore refresh**; **cliff** / **water-shore** prefabs under `Assets/Prefabs/` (per `.cursor/rules/coding-conventions.mdc` for new or adjusted assets)
  - Spec: `.cursor/specs/isometric-geography-system.md` (**map border**, water, **cliffs**, **sorting order** — sections covering shore/**cliff** stacks at boundaries)
  - Notes: **Observed:** Where a **river** channel or **lake** reaches the **east** or **south** **map border**, the **cliff** geometry that seals the edge is **missing or too short** under the water tiles, exposing **black void**; **grass cells** on the same **map border** still show correct **cliff** faces. Suggests **map border** **cliff** stacks or prefab variants do not account for **lower river bed** (`H_bed`) elevation at those edges. **Expected:** Continuous **cliff** wall to the same depth as neighboring land **cliffs**, or dedicated **map border** + water prefabs so no holes at east/south × water. **Related:** completed **BUG-42** (virtual foot / edge **cliffs** — may share root cause with **map border** × water placement).
  - Depends on: none

- [ ] **BUG-31** — Wrong prefabs at interstate entry/exit (border)
  - Type: fix
  - Files: `RoadPrefabResolver.cs`, `RoadManager.cs`
  - Notes: **Interstate** must be able to enter/exit at **map border** in any direction. Incorrect prefab selection at entry/exit cells. Isolated from BUG-30 for separate work.

- [ ] **BUG-28** — **Sorting order** between **slope** cell and **interstate** cell
  - Type: fix
  - Files: `GridManager.cs` (**Sorting order** region), `TerrainManager.cs`, `RoadManager.cs`
  - Notes: **Slope** cells and **interstate** cells render in wrong **sorting order**; one draws over the other incorrectly.

- [ ] **BUG-20** — **Utility buildings** (power plant, 3×3/2×2 multi-cell **buildings**) load incorrectly in LoadGame: end up under **grass cells** (**visual restore**)
  - Type: fix
  - Files: `GeographyManager.cs` (GetMultiCellBuildingMaxSortingOrder, ReCalculateSortingOrderBasedOnHeight), `BuildingPlacementService.cs` (LoadBuildingTile, RestoreBuildingTile), `GridManager.cs` (RestoreGridCellVisuals)
  - Notes: Overlaps **BUG-35** (completed 2026-03-22): flat **grass** removed with **buildings** on load. **BUG-34** addressed general load/**building** **sorting order**. Re-verify in Unity after **BUG-35** closure; close if power plants / multi-cell **utility buildings** sort correctly.

  - [ ] **TECH-01** — Extract responsibilities from large files (focus: **GridManager** decomposition next)
  - Type: refactor
  - Files: `GridManager.cs` (~2070 lines), `TerrainManager.cs` (~3500), `CityStats.cs` (~1200), `ZoneManager.cs` (~1360), `UIManager.cs` (~1240), `RoadManager.cs` (~1730)
  - Notes: Helpers already extracted (`GridPathfinder`, `GridSortingOrderService`, `ChunkCullingSystem`, `RoadCacheService`, `BuildingPlacementService`, etc.). **Next candidates from GridManager:** `BulldozeHandler` (~200 lines), `GridInputHandler` (~130 lines), `CoordinateConversionService` (~230 lines). Prioritize this workstream; see `ARCHITECTURE.md` (GridManager hub trade-off).

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

- [ ] **BUG-16** — Possible race condition in GeographyManager vs TimeManager initialization (**geography initialization**)
  - Type: fix
  - Files: `GeographyManager.cs`, `TimeManager.cs`, `GridManager.cs`
  - Notes: Unity does not guarantee Start() order. If TimeManager.Update() runs before GeographyManager completes **geography initialization**, it may access non-existent data. Use Script Execution Order or gate with `isInitialized`.

- [ ] **BUG-17** — `cachedCamera` is null when creating `ChunkCullingSystem`
  - Type: fix
  - Files: `GridManager.cs`
  - Notes: In InitializeGrid() ChunkCullingSystem is created with `cachedCamera`, but it is only assigned in Update(). May cause NullReferenceException.

- [ ] **BUG-48** — Minimap stays stale until toggling a layer (e.g. data-visualization / **desirability** / **urban centroid**)
  - Type: bug
  - Files: `MiniMapController.cs` (`RebuildTexture`, `Update`; layer toggles call `RebuildTexture` but nothing runs on **simulation tick**), `TimeManager.cs` / `SimulationManager.cs` if wiring refresh to the **simulation tick** or a shared event
  - Notes: **Observed:** The procedural minimap **does not refresh** as the city changes unless the player **toggles a minimap layer** (or other actions that call `RebuildTexture`, such as opening the panel). **Expected:** The minimap should track **zones**, **streets**, **open water**, **forests**, etc. **without** requiring layer toggles. **Implementation:** Rebuild at least **once per simulation tick** while the minimap is visible, **or** a **performance-balanced** approach (throttled full rebuild, dirty rect / incremental update, or event-driven refresh when grid/**zone**/**street**/**water body** data changes) — profile full `RebuildTexture` cost first. Class summary in code states rebuilds on **geography initialization** completion, grid restore, panel open, and layer changes **not** on a fixed timer — that gap is this bug. **Related:** completed **BUG-32** (water on minimap); **FEAT-42** (optional **HeightMap** layer).
  - Depends on: none

- [ ] **FEAT-21** — Expenses and maintenance system
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`
  - Notes: No expenses: no **street** maintenance, no service costs, no salaries. Without expenses there is no economic tension. Add upkeep for **streets**, **utility buildings**, and services.

- [ ] **FEAT-22** — **Tax base** feedback on **demand (R / C / I)** and happiness
  - Type: feature
  - Files: `EconomyManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: High taxes do not affect **demand (R / C / I)** or happiness. Loop: high taxes → less residential **demand** → less growth → less income.
  - Depends on: BUG-02

- [ ] **FEAT-23** — Dynamic happiness based on city conditions
  - Type: feature
  - Files: `CityStats.cs`, `DemandManager.cs`, `EmploymentManager.cs`
  - Notes: Happiness only increases when placing **zones** (+100 per **building**). No effect from unemployment, **tax base**, services or pollution. Should be continuous multi-factor calculation with decay.
  - Depends on: BUG-12

- [ ] **FEAT-36** — Expand **AUTO** zoning and **AUTO** road candidates to include **forests** and cells meeting **land slope eligibility**
  - Type: feature
  - Files: `GridManager.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`
  - Notes: Treat **grass cells**, **forest (coverage)** cells, and cardinal-ramp **slopes** (per **land slope eligibility**) as valid candidates for **AUTO** zoning and **AUTO** road expansion. Capture any design notes in this issue or in `.cursor/specs/isometric-geography-system.md` if rules become stable.

- [ ] **FEAT-43** — **Urban growth rings**: tune **AUTO** road/zoning weights for a gradual center → edge gradient
  - Type: feature (simulation / balance)
  - Files: `UrbanCentroidService.cs` (**growth ring** boundaries, **urban centroid** distance), `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `SimulationManager.cs` (`ProcessSimulationTick` order), `GrowthBudgetManager.cs` if per-ring **growth budgets** apply; `GridManager.cs` / `DemandManager.cs` only if **desirability** or placement must align with **growth rings**
  - Notes: **Observed:** In **AUTO** simulation, cities tend toward a **dense core**, **under-developed middle growth rings**, and **outer rings that are more zoned than the middle** — not a smooth radial gradient. **Expected:** Development should fall off **gradually from the urban centroid**: **highest** **street** density and **AUTO** zoning pressure **near the centroid**, **moderate** in **mid growth rings**, and **lowest** in **outer growth rings**. Revisit **growth ring** radii/thresholds, per-ring weights for **AUTO** road growth vs zoning, and any caps or priorities that invert mid vs outer activity. **Related:** completed **FEAT-32** (**streets**/intersections by area), **FEAT-29** (**zone density** gradient around **urban centroids**), **FEAT-31** (roads toward **desirability**); completed **BUG-47** (2026-04-01, **AUTO** perpendicular stubs and junction refresh).
  - Depends on: none

- [ ] **FEAT-35** — Area demolition tool (bulldozer drag-to-select)
  - Type: feature
  - Files: `GridManager.cs`, `UIManager.cs`, `CursorManager.cs`
  - Notes: Manual tool to demolish all **buildings** and **zoning** in a rectangular area at once. Use the same area selection mechanism as **zoning**: hold mouse button, drag to define rectangle, release to demolish. Reuse **zoning**'s start/end position logic (zoningStartGridPosition, zoningEndGridPosition pattern). Demolish each **cell** in the selected area via DemolishCellAt. **Interstate** cells must remain non-demolishable. Consider preview overlay (e.g. red tint) during drag.

- [ ] **FEAT-03** — **Forest (coverage)** mode hold-to-place
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`
  - Notes: Currently requires click per **cell**. Allow continuous drag.

- [ ] **FEAT-04** — Random **forest (coverage)** spray tool
  - Type: feature
  - Files: `ForestManager.cs`, `GridManager.cs`, `CursorManager.cs`
  - Notes: Place **forest (coverage)** in area with random spray/brush distribution.

- [ ] **FEAT-06** — **Forest (coverage)** that grows over **simulation ticks**: sparse → medium → dense
  - Type: feature
  - Files: `ForestManager.cs`, `ForestMap.cs`, `SimulationManager.cs`
  - Notes: **Forest (coverage)** maturation system over **simulation ticks**.

- [ ] **FEAT-08** — **Zone density** and **desirability** simulation: evolution to larger **buildings**
  - Type: feature
  - Files: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notes: Existing **buildings** evolve to larger versions based on **zone density** and **desirability**.

- [ ] **TECH-15** — New Game / **geography initialization** performance
  - Type: performance / optimization
  - Files: `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `GridManager.cs`, `InterstateManager.cs`, `ForestManager.cs`, `RegionalMapManager.cs`, `ProceduralRiverGenerator.cs` (as applicable)
  - Notes: Reduce wall-clock time and frame spikes when starting a **New Game** (**geography initialization**): **HeightMap**, lakes, procedural **rivers** (**FEAT-38**), **interstate**, **forests**, **map border** signs, **sorting order** passes, etc. Profile the pipeline; consider batched or deferred work across frames, fewer redundant passes, algorithmic improvements, and deferring non-critical visuals until after the map is interactive. **Related:** **FEAT-37c** optimizes **Load Game** (no regen) — this issue targets **geography initialization** cost only.

- [ ] **TECH-16** — **Simulation tick** performance v2 (per-tick **AUTO systems** pipeline)
  - Type: performance / optimization
  - Files: `SimulationManager.cs`, `TimeManager.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `AutoResourcePlanner.cs`, `UrbanCentroidService.cs`, `GrowthBudgetManager.cs`, `DemandManager.cs`, `CityStats.cs` (as applicable)
  - Notes: Second-pass optimization of the **simulation tick** after early **Simulation optimization** work (completed). Profile `ProcessSimulationTick` and callees (**tick execution order**); reduce redundant work, hot-path cost, spatial queries, and per-tick allocations; preserve gameplay unless changes are explicitly agreed. **Related:** **BUG-14** (per-frame UI `FindObjectOfType`); **TECH-01** (manager decomposition may help profiling and hotspots).


## Code Health (technical debt)

- [ ] **TECH-13** — Remove obsolete **urbanization proposal** system (dead code, UI, models)
  - Type: refactor (cleanup)
  - Files: `UrbanizationProposalManager.cs`, `ProposalUIController.cs`, `UrbanizationProposal.cs` (and related), `SimulationManager.cs`, `UIManager.cs`, scene references, **save data** if any
  - Notes: The **urbanization proposal** feature is **obsolete** and intentionally **disabled**; the game is stable without it. **Keep** `UrbanizationProposalManager` disconnected from the simulation — do **not** re-enable proposals. **Keep** `UrbanCentroidService` / **urban growth rings** for **AUTO** roads and zoning (FEAT-32). This issue tracks **full removal** of proposal-specific code and UI after a safe audit (no **save data** breakage). Supersedes former **BUG-15** / **BUG-13**.

- [ ] **TECH-04** — Remove direct access to `gridArray`/`cellArray` outside GridManager
  - Type: refactor
  - Files: `WaterManager.cs`, `GridSortingOrderService.cs`, `GeographyManager.cs`, `BuildingPlacementService.cs`
  - Notes: Project rule: use `GetCell(x, y)` instead of direct array access to the **cell** grid. Several classes violate this. Risk of subtle bugs when grid or **HeightMap** changes.

- [ ] **TECH-02** — Change public fields to `[SerializeField] private` in managers
  - Type: refactor
  - Files: `ZoneManager.cs`, `RoadManager.cs`, `GridManager.cs`, `CityStats.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `UIManager.cs`, `WaterManager.cs`
  - Notes: Dependencies and prefabs exposed as `public` allow accidental access from any class. Use `[SerializeField] private` to encapsulate.

- [ ] **TECH-03** — Extract magic numbers to constants or ScriptableObjects
  - Type: refactor
  - Files: multiple (GridManager, CityStats, RoadManager, UIManager, TimeManager, TerrainManager, WaterManager, EconomyManager, ForestManager, InterstateManager, etc.)
  - Notes: **Building** costs, economic balance, **height generation** parameters, **sorting order** offsets (**type offsets**, **DEPTH_MULTIPLIER**, **HEIGHT_MULTIPLIER**), **pathfinding cost model** weights, initial dates, probabilities — all hardcoded. Extract to named constants or configuration ScriptableObject for easier tuning.

- [ ] **TECH-05** — Extract duplicated dependency resolution pattern
  - Type: refactor
  - Files: ~25+ managers with `if (X == null) X = FindObjectOfType<X>()` block
  - Notes: Consider helper method, base class, or extension method to reduce duplication of Inspector + FindObjectOfType fallback pattern.

- [ ] **TECH-07** — ControlPanel: left vertical sidebar layout (category rows)
  - Type: refactor (UI/UX)
  - Files: `MainScene.unity` (`ControlPanel` hierarchy, RectTransform anchors, `LayoutGroup` / `ContentSizeFitter` as needed), `UIManager.cs` (only if toolbar/submenu positioning or references must follow the new dock), `UnitControllers/*SelectorButton.cs` (only if button wiring or parent references break after reparenting)
  - Spec sections: `.cursor/specs/ui-design-system.md` — **§3.3** (toolbar), **§1.3** (anchors/margins), **§4.3** (Canvas Scaler) as applicable.
  - Notes: Replace the bottom-centered horizontal **ribbon** with a **left-docked vertical** panel. Structure: **one row per category** (demolition, **RCI** **zoning**, **utility buildings**, **streets**, environment/**forests**, etc.), with **buttons laid out horizontally within each row** (e.g. `VerticalLayoutGroup` of rows, each row `HorizontalLayoutGroup`, or equivalent manual layout). Re-anchor dependent UI (e.g. **zone density** / tool option overlays) so they align to the new sidebar instead of the old bottom bar. Verify safe area and Canvas Scaler at reference resolutions; avoid overlapping the mini-map and debug readouts. Document final hierarchy in `docs/ui-design-system-context.md`. Link program charter: `docs/ui-design-system-project.md` (Backlog bridge). Spec/docs ticketed and cross-linked in **TECH-08** (completed).

## Low Priority

- [ ] **FEAT-09** — Trade / Production / Salaries
  - Type: feature (new system)
  - Files: `EconomyManager.cs`, `CityStats.cs` (+ new managers)
  - Notes: Economic system of production, trade between **RCI** **zones** and salaries.

- [ ] **FEAT-18** — **Height generation** (improved terrain generator)
  - Type: feature
  - Files: `TerrainManager.cs`, `GeographyManager.cs`, `HeightMap.cs`
  - Notes: Improved **height generation** with more control and variety over the **HeightMap**.

- [ ] **FEAT-10** — **Regional map** contribution: monthly bonus for belonging to the region
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`, `RegionalMapManager.cs`
  - Notes: Additional monthly income for belonging to the **regional map** network.

- [ ] **FEAT-19** — Map rotation / prefabs
  - Type: feature
  - Files: `CameraController.cs`, `GridManager.cs`, all rendering managers
  - Notes: Isometric view rotation. High impact on **sorting order** (**sorting formula**, **cliff face visibility**) and rendering.

- [ ] **TECH-14** — Remove residual placeholder / test scripts
  - Type: refactor (cleanup)
  - Files: `CityManager.cs` (namespace-only stub), `TestScript.cs` (compile smoke test)
  - Notes: Delete or replace with real content only if nothing references them; verify no scene/Inspector references.

- [ ] **FEAT-11** — Education level / Schools
  - Type: feature (new system)
  - Files: new managers + `CityStats.cs`, `DemandManager.cs`
  - Notes: Education system affecting **demand (R / C / I)** and growth.

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
  - Notes: Vehicles circulating on **streets** and **interstate**.

- [ ] **FEAT-15** — Port system / cargo ship animations
  - Type: feature (new system)
  - Files: new manager + `WaterManager.cs`
  - Notes: Requires **water body** system with defined **sea** (**water body kind**). Depends on BUG-08.

- [ ] **FEAT-16** — Train system / train animations
  - Type: feature (new system)
  - Files: new manager + `GridManager.cs`
  - Notes: Railway network and animations.

- [ ] **FEAT-39** — Sea / **shore band**: **map border** region, infinite reservoir, tide direction (data)
  - Type: feature
  - Files: `WaterManager.cs`, `WaterMap.cs`, `TerrainManager.cs`, `GeographyManager.cs`
  - Notes: Define **sea** as a **water body kind** at the **map border** with **surface height (S)** and **shore band** rules. Coordinate with **FEAT-15** (ports). Depends on **FEAT-37c**.

- [ ] **FEAT-40** — Water sources & drainage (snowmelt, rain, overflow) — simulation
  - Type: feature
  - Files: new helpers + `WaterMap.cs`, `WaterManager.cs`, `SimulationManager.cs`
  - Notes: Not full fluid simulation; data-driven flow affecting **water bodies**, **surface height (S)**, and **depression-fill** dynamics. Depends on **FEAT-37c** and possibly **FEAT-38**.

- [ ] **FEAT-41** — **Water body** terrain tools (manual paint/modify, **AUTO** terraform) — extended
  - Type: feature
  - Files: `GridManager.cs`, `WaterManager.cs`, `UIManager.cs`, `TerraformingService.cs` (as needed)
  - Notes: Beyond legacy paint-at-**sea level**. Tools to create/modify **water bodies** with proper **surface height (S)**, **shore band**, and **water map** registration. Depends on **FEAT-37c**.

- [ ] **FEAT-42** — Minimap: optional **HeightMap** / relief shading layer
  - Type: feature (UI)
  - Files: `MiniMapController.cs`, `HeightMap` / `GridManager` read access as needed
  - Notes: Visualize terrain elevation (**HeightMap**) on the minimap (distinct from **zones**/**streets**/**open water** layers). Does not replace logical **water map** / **zone** data; base layer reliability stays in **FEAT-37a** / **FEAT-30** scope.
  - Depends on: none (can follow **FEAT-37a** polish)

- [ ] **ART-01** — Missing prefabs: **forest (coverage)** on SE, NE, SW, NW **slope types**
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ForestManager.cs`

- [ ] **ART-02** — Missing prefabs: residential **buildings** (2 heavy 1×1/2×2, light 2×2, medium 1×1 per **zone density**)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-03** — Missing prefabs: commercial **buildings** (2 heavy 2×2/1×1, light 2×2, medium 2×2 per **zone density**)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-04** — Missing prefabs: industrial **buildings** (2 heavy 2×2/1×1, light 1×1, 2 medium 1×1/2×2 per **zone density**)
  - Type: art/assets
  - Files: prefabs in `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **TECH-19** — Game PostgreSQL database; first milestone — IA schema for MCP + basic tools
  - Type: infrastructure / tooling
  - Files: new project outside `Assets/Scripts/` (PostgreSQL schema, migrations, optional small service or MCP-adjacent module); seed scripts as needed
  - Notes: **Goal:** Introduce a **game-owned** PostgreSQL database (long-term: not only AI — analytics, metagame, ops, etc.; document intended product uses as they land). **First concrete milestone:** tables and migrations for **Information Architecture** data that MCP will eventually query: e.g. `glossary` (term, conceptual_def, technical_def, spec_address, section, category), `spec_sections` (spec_abbrev, section_id, title, content, parent_section), `invariants`, `relationships` (term_a, relation, term_b) — adjust names/types to match implementation. Ship a **minimal** programmatic surface (SQL views, repo functions, or thin API) plus a **basic** tool set (same *families* as **TECH-17**, but **wired to Postgres** where applicable) to prove read paths. **Optional:** seed a small subset from `.cursor/specs/glossary.md` to validate the pipeline. **Does not** ingest full specs or replace Markdown as source of truth — that is **TECH-18**. **Stack:** PostgreSQL (psql / DBeaver compatible), migrations (tool of choice).
  - Depends on: none

- [ ] **TECH-18** — Migrate Information Architecture from Markdown to PostgreSQL (MCP evolution)
  - Type: infrastructure / tooling
  - Files: All `.cursor/specs/*.md`, `.cursor/rules/agent-router.mdc`, `.cursor/rules/invariants.mdc`, `ARCHITECTURE.md`; MCP server from **TECH-17** (initially **file-backed**); schema / migrations / seed from **TECH-19**
  - Notes: **Goal:** After **TECH-17** (MCP over **`.md` / `.mdc`**) and **TECH-19** (Postgres + IA tables), **migrate authoritative IA content** into PostgreSQL and evolve the **same MCP** so **primary** retrieval is DB-backed. Markdown becomes **generated or secondary** for human reading. **Explicit dependency:** This work **extends the MCP built first on Markdown** in **TECH-17** — same tool contracts where possible, swapping implementation to query **TECH-19**’s database. **Scope:** (1) Parse and ingest spec sections (`isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, etc.) into `spec_sections`. (2) Populate `relationships` (e.g. HeightMap↔Cell.height, PathTerraformPlan→Phase-1→Apply). (3) Populate `invariants` from `invariants.mdc`. (4) Extend tools: `what_do_i_need_to_know(task_description)`, `search_specs(query)`, `dependency_chain(term)`. (5) Script to regenerate `.md` from DB for review. (6) Update `agent-router.mdc` — MCP tools first, Markdown fallback second. **Acceptance:** Agent resolves a multi-spec task (e.g. “bridge over multi-level lake”) via MCP reading ≤ ~500 tokens of context instead of many full-file reads.
  - Depends on: TECH-17, TECH-19

- [ ] **TECH-20** — In-repo Unity development context for agents (spec + concept index)
  - Type: documentation / agent tooling
  - Files: new `.cursor/specs/unity-development-context.md` (or agreed name); `AGENTS.md` and `.cursor/rules/agent-router.mdc` (pointer + “read when…” row); optional short glossary subsection or cross-links to `.cursor/specs/glossary.md` for shared terms
  - Notes: **Goal:** Give agents a **first-party** reference for **Unity concepts that routinely come up in this codebase** (e.g. MonoBehaviour lifecycle, `SerializeField` / Inspector wiring, scenes & prefabs, 2D sorting layers vs `sortingOrder`, `ScriptableObject` when used, `FindObjectOfType` policy here, execution order pitfalls, common Unity patterns **as constrained by this project** — not a full Unity manual). **Policy:** Agents should **default to this spec + existing `.cursor/` docs** for Unity API and workflow questions and **avoid opening general web/docs searches** unless the task **requires** version-specific behavior, undocumented APIs, or verification outside what the repo states. **Scope:** Curated sections, stable anchors, and links to **in-repo** examples (file/class references) where helpful. **Out of scope:** Duplicating Microsoft/Unity manual pages verbatim; replacing official docs when the user explicitly asks for external authority. **Acceptance:** `agent-router` lists when to read this spec; a new contributor agent can implement a typical Inspector + manager change using only repo context for Unity basics. **Related:** **TECH-17** (MCP can later expose this document like other specs).
  - Depends on: none

- [ ] **TECH-21** — Extend and leverage **JSON** in the game system (serialization, tooling, future backend)
  - Type: technical / data interchange
  - Files: `.cursor/specs/persistence-system.md` (when save or interchange shapes stabilize); save/load and **geography initialization** code paths that already emit or consume structured data; optional `StreamingAssets`/config JSON; `tools/mcp-ia-server/` and other Node **scripts** that benefit from shared JSON schemas; future backend modules from **TECH-19** (align payloads and migrations)
  - Notes: **Goals:** (1) **Runtime** — improve **performance** and maintainability where **JSON** or other **serialized** DTO-style data beats ad-hoc string/binary formats or repeated heavy reflection (profile before wide refactors). (2) **Development** — use **JSON** artifacts and TypeScript/JavaScript parsing for **agentic** tools, batch **scripts**, and validation (schemas, fixtures, golden files). (3) **Future backend** — define which **domain models** are safe to represent as **JSON** (save subsets, telemetry, IA exports, API bodies) and how they will map to **database** tables or documents when **TECH-19**+ lands; document versioning and migration expectations. **Out of scope until decided:** replacing the entire **save/load** pipeline without a phased plan; duplicating authoritative **spec** sources in JSON (see **TECH-18**). **Related:** **TECH-15** (**New Game** / **geography initialization** performance), **TECH-16** (**simulation tick** performance), **FEAT-37c** (**Load Game** optimizations), **TECH-19** / **TECH-18** (Postgres + MCP evolution).
  - Acceptance: Written plan (issue update or short doc under `docs/` agreed in review) listing prioritized **JSON** use cases (game vs tooling vs future API); at least one **pilot** implementation or exported schema that proves the pattern (e.g. config slice, tool fixture format, or save-adjacent export) without breaking existing **save data**; glossary/spec terms used consistently for any new named payloads
  - Depends on: none (coordinate with **TECH-19** when backend schema work starts)

- [ ] **AUDIO-01** — Audio FX: demolition, placement, **zoning**, **forest (coverage)**, 3 music themes, ambient effects
  - Type: audio/feature
  - Files: new AudioManager + audio assets
  - Notes: Ambient effects must vary by camera position and **height** (**HeightMap**) over the map.

---

## Completed (last 30 days)

- [x] **TECH-17** — MCP server for agentic Information Architecture (Markdown sources) (2026-04-02)
  - Type: infrastructure / tooling
  - Files: `tools/mcp-ia-server/`; `.cursor/mcp.json`; `.cursor/specs/*.md`, `.cursor/rules/*.mdc`, `AGENTS.md`, `ARCHITECTURE.md` as sources; `docs/mcp-ia-server.md`; docs updates in `AGENTS.md`, `ARCHITECTURE.md`, `.cursor/rules/project-overview.mdc`, `agent-router.mdc` (MCP subsection)
  - Notes: **Shipped:** Node + `@modelcontextprotocol/sdk` stdio server with tools including `list_specs`, `spec_outline`, `spec_section`, `glossary_lookup`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`, `backlog_issue` (BACKLOG.md by id); spec aliases; fuzzy glossary/section fallbacks; `spec_section` input aliases for LLM mis-keys; parse cache; stderr timing; `node:test` + c8 coverage on `src/parser/**`; `npm run verify`. **Reference:** `docs/mcp-ia-server.md`, `docs/mcp-markdown-ia-pattern.md` (generic pattern), `tools/mcp-ia-server/README.md`. **Retrospective / design history:** `.cursor/projects/TECH-17a.md`, `TECH-17b.md`, `TECH-17c.md` (§9–11 post-ship; delete when no longer needed).
  - Depends on: none

- [x] **BUG-51** — Diagonal / corner-up land slopes vs roads: design closure (2026-04-01)
  - Type: bug (closed by policy + implementation, not by fixing prefab-on-diagonal art)
  - Files: `RoadStrokeTerrainRules.cs`, `RoadManager.cs` (`TryBuildFilteredPathForRoadPlan`, `TryPrepareRoadPlacementPlanLongestValidPrefix`, `TryPrepareDeckSpanPlanFromAdjacentStroke`), `GridPathfinder.cs`, `InterstateManager.cs` (`IsCellAllowedForInterstate`), `RoadPrefabResolver.cs`, `TerraformingService.cs`, `Cell.cs` (route-first / BUG-51 technical work — see spec)
  - Spec: `.cursor/specs/roads-system.md` (land slope stroke policy, route-first paragraph), `.cursor/specs/isometric-geography-system.md` §3.3.3–§3.3.4, §13.10
  - Notes: **Closed (verified):** The original report asked for **correct road prefabs on diagonal and corner-up terrain**. The chosen resolution was **not** to fully support roads on those land slope types. Instead, **road strokes are invalid on land that is not flat and not a cardinal ramp** (`TerrainSlopeType`: `Flat`, `North`, `South`, `East`, `West` only). Pure diagonals (`NorthEast`, …) and corner-up types (`*Up`) are excluded. **Behavior:** silent **prefix truncation** — preview and commit only include cells up to the last allowed cell; cursor may keep moving diagonally without extending preview. **Scope:** manual, AUTO, and interstate. **First cell blocked:** no placement, no notification. **`Road cannot extend further…`** is **not** posted when the only issue is no slope-valid prefix (e.g. stroke starts on diagonal). **Exceptions in stroke truncation / walkability:** path cells at `HeightMap` height ≤ 0 (wet span) and `IsWaterSlopeCell` shore tiles still pass the truncator so FEAT-44 bridges are not cut. **Still in codebase:** BUG-51 **route-first** resolver topology (`pathOnlyNeighbors`), `Cell` path hints, terraform preservation on diagonal wedge when `preferSlopeClimb && dSeg == 0`, `GetWorldPositionForPrefab` anchoring — documented under roads spec **BUG-51 (route-first)**.
  - Depends on: none

- [x] **BUG-47** — AUTO simulation: perpendicular street stubs, reservations, junction prefab refresh (2026-04-01)
  - Type: bug / feature
  - Files: `AutoRoadBuilder.cs` (`FindPath*ForAutoSimulation`, `HasParallelRoadTooClose` + `excludeAlongDir`, batch prefab refresh), `AutoSimulationRoadRules.cs`, `AutoZoningManager.cs`, `RoadCacheService.cs`, `GridPathfinder.cs`, `GridManager.cs`, `IGridManager.cs`, `RoadManager.cs` (`RefreshRoadPrefabsAfterBatchPlacement`, bridge-deck skip); `.cursor/specs/isometric-geography-system.md` §13.9, `.cursor/rules/roads.mdc`, `.cursor/rules/simulation.mdc`
  - Spec: `.cursor/specs/isometric-geography-system.md` §13.9
  - Notes: **Completed (verified in-game):** AUTO can trace perpendicular stubs/connectors and crossings: land = grass/forest/undeveloped light zoning; dedicated AUTO pathfinder; road frontier and extension cells include that class; perpendicular branches pass parent-axis `excludeAlongDir` in `HasParallelRoadTooClose`; auto-zoning skips axial corridor and extension cells. **Visual:** `PlaceRoadTileFromResolved` did not refresh neighbors; added deduplicated per-tick refresh (`RefreshRoadPrefabsAfterBatchPlacement`), skipping bridge deck re-resolve. **Lessons:** any batch `FromResolved` flow must document explicit junction refresh; keep generic `FindPath` separate from AUTO pathfinding.
  - Depends on: none

- [x] **FEAT-44** — High-deck water bridges: cliff banks, uniform deck height, manual + AUTO placement (2026-03-30)
  - Type: feature
  - Files: `RoadManager.cs` (`TryPrepareDeckSpanPlanFromAdjacentStroke`, `TryPrepareLockedDeckSpanBridgePlacement`, `TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord`, `TryExtendCardinalStreetPathWithBridgeChord`, `StrokeHasWaterOrWaterSlopeCells`, `StrokeLastCellIsFirmDryLand`, FEAT-44 validation / chord walk), `TerraformingService.cs` (`TryBuildDeckSpanOnlyWaterBridgePlan`, `TryAssignWaterBridgeDeckDisplayHeight`), `AutoRoadBuilder.cs` (`TryGetStreetPlacementPlan`, `BuildFullSegmentInOneTick` — atomic water-bridge completion), `PathTerraformPlan.cs` (`HasTerraformHeightMutation`, deck display height docs), `RoadPrefabResolver.cs` (bridge deck resolution); rules/spec: `.cursor/rules/roads.mdc`, `.cursor/specs/isometric-geography-system.md` §13
  - Spec: `.cursor/specs/isometric-geography-system.md` §13 (bridges, shared validation, AUTO behavior)
  - Notes: **Completed (verified per user):** **Manual:** locked lip→chord preview uses a **deck-span-only** plan (`TerraformAction.None`, `TryBuildDeckSpanOnlyWaterBridgePlan`) so valid crossings are not blocked by cut-through / Phase-1 on complex tails; commit matches preview via shared `TryPrepareDeckSpanPlanFromAdjacentStroke`. **AUTO:** extends cardinal strokes with the same `WalkStraightChordFromLipThroughWetToFarDry` when the next step is wet/shore; runs longest-prefix plus programmatic deck-span and **prefers** deck-span when the stroke is wet or yields a longer expanded path. **AUTO water crossings** are **all-or-nothing in one tick**: require a **firm dry exit**, enough remaining tile budget for every new tile, a **single lump** `TrySpend` for the bridge, otherwise **`Revert`** — no half bridges. **Uniform deck:** one `waterBridgeDeckDisplayHeight` for all bridge deck prefabs on the span; assignment **prefers the exit (mesa) dry cell** after the wet run, then entry, then legacy lip fallback. **Description (issue):** Elevated road / bridge crossings across cliff-separated banks and variable terrain with correct clearance, FEAT-44 path rules, and consistent sorting/pathfinding per geography spec.

- [x] **BUG-50** — River–river junction: shore Moore topology, junction post-pass diagonal SlopeWater, upper-brink cliff water stacks + isometric anchor at shore grid (2026-03-28)
  - Type: bug / polish
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `IsOpenWaterForShoreTopology`, `NeighborMatchesShoreOwnerForJunctionTopology`, `ApplyJunctionCascadeShorePostPass`, `ApplyUpperBrinkShoreWaterCascadeCliffStacks`, `TryPlaceWaterCascadeCliffStack` / `waterSurfaceAnchorGrid`, `PlaceCliffWallStackCore` sorting reference), `WaterManager.Membership.cs`, `WaterMap.cs` (`TryFindRiverRiverSurfaceStepBetweenBodiesNear`)
  - Spec: `.cursor/specs/isometric-geography-system.md` **§12.8.1**
  - Notes: **Completed (verified):** Default shore masks use **`IsOpenWaterForShoreTopology`** (junction-brink dry land not counted). **`RefreshShoreTerrainAfterWaterUpdate`** runs **`ApplyJunctionCascadeShorePostPass`** (extended topology + **`forceJunctionDiagonalSlopeForCascade`**) then **`ApplyUpperBrinkShoreWaterCascadeCliffStacks`** ( **`CliffSouthWater`** / **`CliffEastWater`** on **`UpperBrink`** only). Cascade **Y** anchor and sorting use **`waterSurfaceAnchorGrid`** at the **shore** cell so wide-river banks align with the isometric water plane. **`ARCHITECTURE.md`** Water bullet and **§12.8.1** document pipeline and authority.

- [x] **BUG-45** — Adjacent water bodies at different surface heights: merge, prefab refresh at intersections, straight slope/cliff transitions (2026-03-27)
  - Type: bug / polish
  - Files: `WaterManager.cs` (`UpdateWaterVisuals` — Pass A/B, `ApplyLakeHighToRiverLowContactFallback`), `WaterMap.cs` (`ApplyMultiBodySurfaceBoundaryNormalization`, `ApplyWaterSurfaceJunctionMerge`, `IsLakeSurfaceStepContactForbidden`, lake–river fallback), `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `SelectPerpendicularWaterCornerPrefabs`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`), `ProceduralRiverGenerator.cs` / `TestRiverGenerator.cs` as applicable; `docs/water-junction-merge-implementation-plan.md`
  - Spec: `.cursor/specs/isometric-geography-system.md` — **§5.6.2**, **§12.7**
  - Notes: **Completed (verified):** Pass A/B multi-body surface handling; lake-at-step exclusions; full-cardinal **`RefreshWaterCascadeCliffs`** (incl. mirror N/W lower pool); perpendicular multi-surface shore corner preference; lake-high vs river-low rim fallback. **Assign** `cliffWaterSouthPrefab` / **`cliffWaterEastPrefab`** on `TerrainManager` for visible cascades (west→east steps use **East**). Residual: map-edge water × cliff **BUG-44**; bridges × cliff-water **BUG-43**; optional N/W cascade art (camera).

- [x] **BUG-42** — Water shores & cliffs: terrain + water (lakes + rivers); water–water cascades; shore coherence — merged **BUG-33** + **BUG-41** (2026-03-26)
  - Type: bug / feature
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `PlaceCliffWalls`, `PlaceCliffWallStackCore`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`, `ClampShoreLandHeightsToAdjacentWaterSurface`, `IsLandEligibleForWaterShorePrefabs`), `WaterManager.cs` (`PlaceWater`, `UpdateWaterVisuals`), `ProceduralRiverGenerator.cs` (inner-corner shore continuity §13.5), `ProceduralRiverGenerator` / `WaterMap` as applicable; `cliffWaterSouthPrefab` & `cliffWaterEastPrefab` under `Assets/Prefabs/`
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

> Older completed items archived in `BACKLOG-ARCHIVE.md`.

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
- **Acceptance** (optional): concrete pass/fail criteria for verification
- **Depends on** (optional): IDs of issues that must be completed first

### Section Order
1. In progress (actively being developed)
2. High priority (critical bugs, core gameplay blockers)
3. Medium priority (important features, balance, improvements)
4. Code Health (technical debt, refactors, performance)
5. Low priority (new systems, polish, content)
6. Completed (last 30 days)
