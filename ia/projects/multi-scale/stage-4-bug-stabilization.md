### Stage 4 — City MVP close / Bug stabilization

**Status:** Done (2026-04-17 — all 4 tasks archived)

**Objectives:** All open crasher, data-corruption, initialization-race, and per-frame-cache bugs at city scale fixed. Invariants #3 and #5 clean.

**Exit:**

- BUG-55 (10 fixes): no crash on New Game or Load Game; growth budget and demand stabilize; `OnDestroy` listener leaks closed in `SimulateGrowthToggle`, `GrowthBudgetSlidersController`, `CityStatsUIController`.
- BUG-14: `UIManager.UpdateUI()` caches `EmploymentManager`, `DemandManager`, `StatisticsManager` in `Awake`/`Start`; zero per-frame `FindObjectOfType` violations (invariant #3).
- BUG-16: `GeographyManager` init race fixed — `isInitialized` gate or Script Execution Order prevents `TimeManager.Update()` reading uninit data.
- BUG-17: `cachedCamera` assigned in `GridManager.Awake()` / `Start()` before `InitializeGrid()` constructs `ChunkCullingSystem`.
- `unity:compile-check` passes; testmode smoke — New Game + Load Game no crash.
- Phase 1 — Crashers + data integrity (BUG-55 + BUG-14).
- Phase 2 — Init races + null refs (BUG-16 + BUG-17).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | BUG-55 10 fixes | **BUG-55** | Done (archived) | All 10 fixes landed per `BACKLOG-ARCHIVE.md` BUG-55 row: `EmploymentManager` div/0 already guarded; `CityCell` `TryParse` fallback; `AutoZoningManager` placement-first ordering; `CellData` height-0 floor; `GrowthBudgetManager` min enforced; `DemandManager` empty-zone subtraction; `AutoRoadBuilder` cache invalidation + re-fetch; water height `< 0` strict; demand symmetry 1.2; `OnDestroy` cleanup in 3 controllers. |
| T4.2 | BUG-14 per-frame cache | **BUG-14** | Done (archived) | `UIManager.UpdateUI()` caches `EmploymentManager`, `DemandManager` in `Start` (`StatisticsManager` lookup was dead — removed); `UpdateGridCoordinatesDebugText` uses cached `gameDebugInfoBuilder` + `waterManager`. Zero per-frame `FindObjectOfType` in `UIManager.Hud.cs` (verified). Invariant #3. |
| T4.3 | BUG-16 init race | **BUG-16** | Done (archived) | `GeographyManager.IsInitialized` flips true at tail of `InitializeGeography()`; `TimeManager` caches ref via `[SerializeField]` + `FindObjectOfType` fallback in `Awake` (invariant #3); daily-tick block early-returns pre-init. UI/input responsive during load. Bridge smoke: 0 NRE, compile clean. |
| T4.4 | BUG-17 cachedCamera null | **BUG-17** | Done (archived) | `cachedCamera` promoted to `[SerializeField] private Camera`; new `GridManager.Awake()` resolves via `Camera.main` fallback before `InitializeGrid()` constructs `ChunkCullingSystem`; redundant lazy null-checks removed at `GridManager.cs:366` + `:1294`. Matches canonical init-race guard per `unity-development-context §6`. Compile clean; chunk visibility unchanged. |
