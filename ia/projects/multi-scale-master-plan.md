# Multi-Scale Simulation — Master Plan (MVP)

> **Status:** In Progress — Step 2 (Step 1 Final 2026-04-14; Step 2 decomposed 2026-04-16, tasks _pending_)
>
> **Scope:** Min load-bearing work to prove city ↔ region ↔ country game loop (dormant evolution + reconstruction). Rest → `multi-scale-post-mvp-expansion.md`.
>
> **Vision + design principles:** `ia/specs/game-overview.md`
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
>
> - `ia/projects/blip-master-plan.md` — audio subsystem. Stage 1.1 archived (TECH-98..101); Stages 1.2–1.4 pending. Blip Step 3.3 (World lane call sites) wires into `GridManager.cs` cell-select + road/building tools + save hooks — coordinate so blip Step 3 kickoff lands after multi-scale `GridManager` mutations settle.
> - `ia/projects/sprite-gen-master-plan.md` — Python sprite generator (`tools/sprite-gen/`) + `Assets/Sprites/Generated/` output. City-scale 1×1 building footprints only in v1; region / country scale sprite needs surface when this orchestrator's Step 4 opens — not yet scoped anywhere.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
>
> - `ia/specs/game-overview.md` — vision + principles
> - `ia/specs/simulation-system.md` — current single-scale tick loop (MCP `spec_section`)
> - `ia/projects/multi-scale-post-mvp-expansion.md` — scope boundary (what's OUT of MVP)
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics
> - MCP: `backlog_issue {id}` per referenced id; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Stage 1 — Parent-scale conceptual stubs / Parent-scale identity fields

**Status:** Final

**Objectives:** city save + `GridManager` carry non-null `region_id` + `country_id` (placeholder GUIDs). Legacy saves migrate cleanly.

**Exit:**

- `GameSaveData` has non-null `region_id` + `country_id` (GUID).
- `GridManager` exposes read-only `ParentRegionId` / `ParentCountryId` set at load / new-game.
- Save/load round-trips both ids.
- Legacy saves migrate w/ placeholder ids; no data loss; save version bumped.
- Glossary rows land for **parent region id** + **parent country id**.
- Phase 1 — Schema + migration (data shape, version bump, legacy load path).
- Phase 2 — Runtime surface (`GridManager` properties + new-game placeholder allocation).
- Phase 3 — Round-trip + migration tests (testmode batch).

**Tasks:**


| Task | Name | Issue | Status | Intent |
| ------ | ----------------------- | ----------- | ------ | --------------------------------------------------------------------------------------------- |
| T1.1 | Parent-id fields | **TECH-87** | Done | `GameSaveData` parent-id fields + save version bump + legacy migration + glossary rows. |
| T1.2 | GridManager parent-id | **TECH-88** | Done | `GridManager` `ParentRegionId` / `ParentCountryId` surface + new-game placeholder allocation. |
| T1.3 | Round-trip migration | **TECH-89** | Done | Round-trip + legacy-migration tests (testmode batch scenario). |


### Stage 2 — Parent-scale conceptual stubs / Cell-type split

**Status:** Final

**Objectives:** `Cell` → `CityCell` / `RegionCell` / `CountryCell`. City sim unchanged in behavior. Invariants #1 (`HeightMap` ↔ `Cell.height` sync) and #5 (`GetCell` only) preserved.

**Exit:**

- `Cell` base type (abstract class or interface) carries coord + shared primitives.
- `CityCell` carries all existing city-scale fields.
- `RegionCell` + `CountryCell` land as thin placeholders (coord + parent id refs; no behavior).
- City sim compiles + runs against `CityCell`. Zero behavior regression (testmode smoke).
- `GridManager` typed surface — generic `GetCell<T>(x,y)` or scale-indexed overloads; existing `GetCell(x,y)` back-compat defaults to `CityCell`.
- Glossary rows land for three cell types.
- Phase 1 — Base type extraction + `Cell` → `CityCell` rename (compile-only refactor).
- Phase 2 — `RegionCell` + `CountryCell` placeholder types + glossary rows.
- Phase 3 — `GridManager` typed surface + back-compat default.
- Phase 4 — Regression gate (`unity:compile-check` + testmode smoke + `HeightMap` integrity).

**Tasks:**


| Task | Name | Issue | Status | Intent |
| ------ | ------------------------ | ----------- | ------ | ------------------------------------------------------------------------------------------------------------ |
| T2.1 | Extract Cell base | **TECH-90** | Done | Extract `Cell` abstract base (coord, height, shared primitives). Compile-only; no rename yet. |
| T2.2 | Cell → CityCell rename | **TECH-91** | Done | Rename `Cell` → `CityCell` across all city sim files. Preserve `HeightMap` sync (invariant #1). |
| T2.3 | RegionCell placeholder | **TECH-92** | Done | `RegionCell` placeholder type (coord + parent-region-id; no behavior). Glossary row. |
| T2.4 | CountryCell placeholder | **TECH-93** | Done | `CountryCell` placeholder type (coord + parent-country-id; no behavior). Glossary rows for all 3 cell types. |
| T2.5 | GetCell generic overloads | **TECH-94** | Done | Generic `GetCell<T>(x,y)` or scale-indexed overloads on `GridManager`. Compile gate. |
| T2.6 | GetCell back-compat | **TECH-95** | Done | Back-compat `GetCell(x,y)` defaults to `CityCell`. Update all callers. Invariant #5 preserved. |
| T2.7 | City load smoke test | **TECH-96** | Done | Testmode smoke — city load + sim tick, no regression. |
| T2.8 | HeightMap integrity test | **TECH-97** | Done | Testmode assertion — `HeightMap` / `CityCell.height` integrity (invariant #1). |


### Stage 3 — Parent-scale conceptual stubs / Neighbor-city stub + interstate-border semantics

**Status:** Final (2026-04-14 — all tasks archived TECH-102→TECH-109)

**Objectives:** ≥1 neighbor stub per city at interstate border. Inert read contract for future cross-scale flow.

**Exit:**

- `NeighborCityStub` struct: `id` (GUID), display name, border side enum.
- New-game init places ≥1 stub at random interstate border (seed-deterministic).
- Interstate road exit binds to stub ref (lookup by border side).
- Flow consumer reads stub via inert API (returns 0 / empty; no behavior).
- Save/load preserves stubs + bindings round-trip.
- Glossary rows land for **neighbor-city stub** + **interstate border**.
- Phase 1 — Stub schema + save wiring.
- Phase 2 — Interstate-border binding (new-game init + on-road-build at border).
- Phase 3 — City-sim inert read surface + glossary rows.
- Phase 4 — Round-trip + testmode smoke.

**Tasks:**


| Task | Name | Issue | Status | Intent |
| ------ | ------------------------- | ------------ | --------------- | -------------------------------------------------------------------------------------------- |
| T3.1 | NeighborCityStub struct | **TECH-102** | Done | `NeighborCityStub` struct (id GUID, display name, border side enum) + serialize schema. |
| T3.2 | neighborStubs save field | **TECH-103** | Done | `GameSaveData.neighborStubs` list + save version bump. |
| T3.3 | New-game stub placement | **TECH-104** | Done | New-game init: place ≥1 stub at random interstate border (seed-deterministic). |
| T3.4 | Road exit border bind | **TECH-105** | Done | On-road-build: road exit at border binds to stub ref by border side. |
| T3.5 | GetNeighborStub API | **TECH-106** | Done | `GridManager.GetNeighborStub(side)` inert read contract (returns stub or null; no behavior). |
| T3.6 | Stub + border glossary | **TECH-107** | Done | Glossary rows for `neighbor-city stub` + `interstate border`. |
| T3.7 | Save/load round-trip | **TECH-108** | Done | Save/load round-trip test (stubs + bindings preserved). |
| T3.8 | Border smoke test | **TECH-109** | Done (archived) | Testmode smoke — stub at border after new-game; binding intact after road build at border. |


**Backlog state (Step 1):** Stage 1.1 filed + archived (TECH-87 / TECH-88 / TECH-89). Stage 1.2 filed + archived (TECH-90 → TECH-97). Stage 1.3 filed + archived (TECH-102 → TECH-109) under `§ Multi-scale simulation lane`.

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


### Stage 5 — City MVP close / Tick performance + metrics foundation

**Status:** In Progress (tasks filed 2026-04-17 — TECH-290..TECH-293)

**Objectives:** City tick profiled; egregious non-BUG-55 allocators patched; `MetricsRecorder` Phase 1 integrated (game remains playable without Postgres); EditMode tick budget test establishes Step 3 parity baseline.

**Exit:**

- `docs/city-tick-perf-notes.md` (new): top-5 hotspots + GC allocs + baseline ms/tick after Stage 2.1 fixes.
- Top allocator(s) beyond BUG-55/BUG-14 scope patched (or confirmed acceptable with note).
- TECH-82 Phase 1: `MetricsRecorder.cs` fires per-tick in `SimulationManager`; `city_metrics_history` migration applied; `mcp__territory-ia__city_metrics_query` tool returns time-series; game playable without Postgres.
- EditMode test `TickBudgetTests.cs` (new): isolated tick completes within configured budget threshold; baseline recorded for Step 3 parity harness.
- Phase 1 — Profiler run + alloc audit.
- Phase 2 — MetricsRecorder + tick budget test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Tick profiler baseline | **TECH-290** | Draft | Unity Profiler run on `SimulationManager` tick path post Stage 2.1; document top-5 hotspots + GC allocs + baseline ms/tick in `docs/city-tick-perf-notes.md` (new). |
| T5.2 | Tick alloc audit + patch | **TECH-291** | Draft | Scan `SimulationManager` + tick-path managers for avoidable GC alloc (LINQ, boxing, list recreation per-tick); patch top-2 allocators found; annotate `SimulationManager.Update()` with budget note. |
| T5.3 | TECH-82 Phase 1 integration | **TECH-292** | Draft | `MetricsRecorder.cs` (new) fires fire-and-forget per `SimulationManager` tick; `db/migrations/` `city_metrics_history` schema + bridge scripts; `mcp__territory-ia__city_metrics_query` tool per `ia/projects/TECH-82.md` Phase 1 acceptance. Scope-slice of **TECH-82** — does NOT subsume TECH-82 Phases 2–4. |
| T5.4 | Tick budget EditMode test | **TECH-293** | Draft | `Assets/Tests/EditMode/Simulation/TickBudgetTests.cs` (new): isolated tick invocation completes within configured threshold (ms read from profiler notes); threshold field documents Step 3 parity target. |


### Stage 6 — City MVP close / City readability dashboard

**Status:** In Progress (FEAT-51 filed)

**Objectives:** Player reads city state at-a-glance: minimal HUD + ≥3 time-series charts. Delivers FEAT-51 §2.1–§2.5. Chart library decision recorded.

**Exit:**

- `UiTheme.cs` carries `chartLineColor`, `chartAxisColor`, `chartLabelFont`, `chartBackground` fields; `ia/specs/ui-design-system.md` §tokens chart subsection added.
- Chart library decision (XCharts or equivalent) recorded in `ia/projects/FEAT-51.md` Decision Log.
- FEAT-51 acceptance (§8): history ringbuffer + derived metrics + chart engine + HUD card layout; ≥3 charts (population trend, employment rate, treasury balance) visible; no per-frame `FindObjectOfType`; `UiTheme` tokens applied throughout.
- Testmode smoke: ≥3 charts render after New Game tick.
- Phase 1 — UiTheme chart tokens + chart library spike.
- Phase 2 — Full dashboard delivery + acceptance gate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | UiTheme chart tokens | _pending_ | _pending_ | Add `chartLineColor`, `chartAxisColor`, `chartLabelFont`, `chartBackground` fields to `UiTheme.cs`; add chart-tokens subsection to `ia/specs/ui-design-system.md` §tokens. |
| T6.2 | Chart library spike | _pending_ | _pending_ | Evaluate XCharts vs alternatives in Unity; create `ChartDemo` prefab (new) with `LineChart` wired to dummy data; validate `UiTheme` token bind; record library decision in `ia/projects/FEAT-51.md` Decision Log. |
| T6.3 | FEAT-51 dashboard delivery | **FEAT-51** | Draft | Full game data dashboard per `ia/projects/FEAT-51.md` §8: history ringbuffer + derived metrics + chart engine + HUD card layout; ≥3 charts; UiTheme tokens applied; no per-frame `FindObjectOfType`. |
| T6.4 | Dashboard acceptance gate | _pending_ | _pending_ | Testmode smoke: ≥3 charts render after New Game tick; token audit — all chart colors sourced from `UiTheme`; `unity:compile-check`; confirm FEAT-51 §8 acceptance met; Decision Log entry verified complete. |


### Stage 7 — City MVP close / Parent-stub consumption

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** ≥1 city UI panel + ≥1 sim system actively consume Step 1 stubs (ParentRegionId / ParentCountryId / GetNeighborStub). Establishes consumer pattern for Step 3 to flesh out.

**Exit:**

- `ParentContextPanel.cs` (new) in city HUD: reads `GridManager.ParentRegionId` + `ParentCountryId`; displays region + country placeholder names.
- `NeighborCityStubPanel.cs` (new) in city HUD sidebar: reads `GridManager.GetNeighborStub(side)` for all border sides; renders ≥1 stub card (display name + border direction); inert.
- `DemandManager.GetExternalDemandModifier()` (new method): reads `GetNeighborStub()` list; returns `1.0f + 0.05f * stubCount` as placeholder; called in demand calculation; `GridManager` cached in `Awake` (invariant #3). Establishes consumption pattern for Step 3.
- Testmode smoke: after New Game, `ParentContextPanel` shows non-null values; `GetNeighborStub()` returns ≥1 stub; `GetExternalDemandModifier()` returns > 1.0f.
- Phase 1 — Parent context + neighbor stub UI panels.
- Phase 2 — Sim consumer + integration smoke.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | Parent context panel | _pending_ | _pending_ | `ParentContextPanel.cs` (new) MonoBehaviour in city HUD: reads `GridManager.ParentRegionId` + `ParentCountryId`; displays region + country placeholder name; binds on scene load. Follows `ia/specs/ui-design-system.md` §HUD patterns. |
| T7.2 | Neighbor stub panel | _pending_ | _pending_ | `NeighborCityStubPanel.cs` (new): iterates border sides via `GridManager.GetNeighborStub(side)`; renders ≥1 HUD stub card (display name, border direction enum); inert — no behavior, no data mutation. |
| T7.3 | DemandManager parent modifier | _pending_ | _pending_ | `DemandManager.GetExternalDemandModifier()` (new): reads neighbor stub list; returns `1.0f + 0.05f * stubCount`; wired into demand calculation. Cache `GridManager` in `Awake` (invariant #3). Pattern seeded for Step 3 expansion. |
| T7.4 | Parent-stub integration smoke | _pending_ | _pending_ | Testmode smoke scenario: New Game → assert `ParentContextPanel` non-null display; assert `GetNeighborStub()` count ≥ 1; assert `GetExternalDemandModifier()` > 1.0f. Confirms Step 1 stubs consumed end-to-end. |
