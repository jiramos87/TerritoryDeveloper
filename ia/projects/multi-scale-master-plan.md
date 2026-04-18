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

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — Parent-scale conceptual stubs

**Status:** Final

**Objectives:** surface parent region + country identity in city code + save. Land cell-type split as refactor base for parent scales. Plant neighbor-city stub + interstate-border read contract (inert). Zero behavior shift at city scale; no playable parent scales.

**Exit criteria:**

- Every city save carries non-null `region_id` + `country_id` (placeholders OK).
- ≥1 neighbor-city stub at interstate border, readable by city sim (inert).
- Interstate connections admit "flow to/from parent-region neighbor" interpretation.
- Save/load round-trips, no regression.
- Cell-type split: `Cell` API → `CityCell` / `RegionCell` / `CountryCell`; city sim builds + runs against new types, no behavior regression.

**Art:** None (code-only stubs).

**Relevant surfaces (load when step opens):** `Assets/Scripts/Grid/Cell.cs`, `Assets/Scripts/SaveSystem/GameSaveData.cs`, `Assets/Scripts/GridManager.cs`, `Assets/Scripts/InterstateManager.cs`, `ia/specs/save-system.md` (§schema), `ia/rules/invariants.md` (#1, #5).

#### Stage 1.1 — Parent-scale identity fields

**Status:** Final

**Objectives:** city save + `GridManager` carry non-null `region_id` + `country_id` (placeholder GUIDs). Legacy saves migrate cleanly.

**Exit:**

- `GameSaveData` has non-null `region_id` + `country_id` (GUID).
- `GridManager` exposes read-only `ParentRegionId` / `ParentCountryId` set at load / new-game.
- Save/load round-trips both ids.
- Legacy saves migrate w/ placeholder ids; no data loss; save version bumped.
- Glossary rows land for **parent region id** + **parent country id**.

**Phases:**

- Phase 1 — Schema + migration (data shape, version bump, legacy load path).
- Phase 2 — Runtime surface (`GridManager` properties + new-game placeholder allocation).
- Phase 3 — Round-trip + migration tests (testmode batch).

**Tasks:**


| Task   | Name                    | Phase | Issue       | Status | Intent                                                                                        |
| ------ | ----------------------- | ----- | ----------- | ------ | --------------------------------------------------------------------------------------------- |
| T1.1.1 | Parent-id fields        | 1     | **TECH-87** | Done   | `GameSaveData` parent-id fields + save version bump + legacy migration + glossary rows.       |
| T1.1.2 | GridManager parent-id   | 2     | **TECH-88** | Done   | `GridManager` `ParentRegionId` / `ParentCountryId` surface + new-game placeholder allocation. |
| T1.1.3 | Round-trip migration    | 3     | **TECH-89** | Done   | Round-trip + legacy-migration tests (testmode batch scenario).                                |


#### Stage 1.2 — Cell-type split

**Status:** Final

**Objectives:** `Cell` → `CityCell` / `RegionCell` / `CountryCell`. City sim unchanged in behavior. Invariants #1 (`HeightMap` ↔ `Cell.height` sync) and #5 (`GetCell` only) preserved.

**Exit:**

- `Cell` base type (abstract class or interface) carries coord + shared primitives.
- `CityCell` carries all existing city-scale fields.
- `RegionCell` + `CountryCell` land as thin placeholders (coord + parent id refs; no behavior).
- City sim compiles + runs against `CityCell`. Zero behavior regression (testmode smoke).
- `GridManager` typed surface — generic `GetCell<T>(x,y)` or scale-indexed overloads; existing `GetCell(x,y)` back-compat defaults to `CityCell`.
- Glossary rows land for three cell types.

**Phases:**

- Phase 1 — Base type extraction + `Cell` → `CityCell` rename (compile-only refactor).
- Phase 2 — `RegionCell` + `CountryCell` placeholder types + glossary rows.
- Phase 3 — `GridManager` typed surface + back-compat default.
- Phase 4 — Regression gate (`unity:compile-check` + testmode smoke + `HeightMap` integrity).

**Tasks:**


| Task   | Name                     | Phase | Issue       | Status | Intent                                                                                                       |
| ------ | ------------------------ | ----- | ----------- | ------ | ------------------------------------------------------------------------------------------------------------ |
| T1.2.1 | Extract Cell base        | 1     | **TECH-90** | Done   | Extract `Cell` abstract base (coord, height, shared primitives). Compile-only; no rename yet.                |
| T1.2.2 | Cell → CityCell rename   | 1     | **TECH-91** | Done   | Rename `Cell` → `CityCell` across all city sim files. Preserve `HeightMap` sync (invariant #1).              |
| T1.2.3 | RegionCell placeholder   | 2     | **TECH-92** | Done   | `RegionCell` placeholder type (coord + parent-region-id; no behavior). Glossary row.                         |
| T1.2.4 | CountryCell placeholder  | 2     | **TECH-93** | Done   | `CountryCell` placeholder type (coord + parent-country-id; no behavior). Glossary rows for all 3 cell types. |
| T1.2.5 | GetCell generic overloads| 3     | **TECH-94** | Done   | Generic `GetCell<T>(x,y)` or scale-indexed overloads on `GridManager`. Compile gate.                         |
| T1.2.6 | GetCell back-compat      | 3     | **TECH-95** | Done   | Back-compat `GetCell(x,y)` defaults to `CityCell`. Update all callers. Invariant #5 preserved.               |
| T1.2.7 | City load smoke test     | 4     | **TECH-96** | Done   | Testmode smoke — city load + sim tick, no regression.                                                        |
| T1.2.8 | HeightMap integrity test | 4     | **TECH-97** | Done   | Testmode assertion — `HeightMap` / `CityCell.height` integrity (invariant #1).                               |


#### Stage 1.3 — Neighbor-city stub + interstate-border semantics

**Status:** Final (2026-04-14 — all tasks archived TECH-102→TECH-109)

**Objectives:** ≥1 neighbor stub per city at interstate border. Inert read contract for future cross-scale flow.

**Exit:**

- `NeighborCityStub` struct: `id` (GUID), display name, border side enum.
- New-game init places ≥1 stub at random interstate border (seed-deterministic).
- Interstate road exit binds to stub ref (lookup by border side).
- Flow consumer reads stub via inert API (returns 0 / empty; no behavior).
- Save/load preserves stubs + bindings round-trip.
- Glossary rows land for **neighbor-city stub** + **interstate border**.

**Phases:**

- Phase 1 — Stub schema + save wiring.
- Phase 2 — Interstate-border binding (new-game init + on-road-build at border).
- Phase 3 — City-sim inert read surface + glossary rows.
- Phase 4 — Round-trip + testmode smoke.

**Tasks:**


| Task   | Name                      | Phase | Issue        | Status          | Intent                                                                                       |
| ------ | ------------------------- | ----- | ------------ | --------------- | -------------------------------------------------------------------------------------------- |
| T1.3.1 | NeighborCityStub struct   | 1     | **TECH-102** | Done            | `NeighborCityStub` struct (id GUID, display name, border side enum) + serialize schema.      |
| T1.3.2 | neighborStubs save field  | 1     | **TECH-103** | Done            | `GameSaveData.neighborStubs` list + save version bump.                                       |
| T1.3.3 | New-game stub placement   | 2     | **TECH-104** | Done            | New-game init: place ≥1 stub at random interstate border (seed-deterministic).               |
| T1.3.4 | Road exit border bind     | 2     | **TECH-105** | Done            | On-road-build: road exit at border binds to stub ref by border side.                         |
| T1.3.5 | GetNeighborStub API       | 3     | **TECH-106** | Done            | `GridManager.GetNeighborStub(side)` inert read contract (returns stub or null; no behavior). |
| T1.3.6 | Stub + border glossary    | 3     | **TECH-107** | Done            | Glossary rows for `neighbor-city stub` + `interstate border`.                                |
| T1.3.7 | Save/load round-trip      | 4     | **TECH-108** | Done            | Save/load round-trip test (stubs + bindings preserved).                                      |
| T1.3.8 | Border smoke test         | 4     | **TECH-109** | Done (archived) | Testmode smoke — stub at border after new-game; binding intact after road build at border.   |


**Backlog state (Step 1):** Stage 1.1 filed + archived (TECH-87 / TECH-88 / TECH-89). Stage 1.2 filed + archived (TECH-90 → TECH-97). Stage 1.3 filed + archived (TECH-102 → TECH-109) under `§ Multi-scale simulation lane`.

### Step 2 — City MVP close

**Status:** In Progress — Stage 2.2

**Backlog state (Step 2):** Stage 2.1 archived (BUG-55 / BUG-14 / BUG-16 / BUG-17). Stage 2.2 filed 2026-04-17 (TECH-290..TECH-293).

**Objectives:** City scale stable + readable enough to serve as aggregation source + reconstruction target. Not a finished city-builder loop.

**Exit criteria:**

- No crasher / data-corruption bug open at city scale.
- Player reads city state at-a-glance (minimal dashboard + handful of charts).
- Single city tick cheap enough that one dormant city alongside one active city is credible (target set in Step 3 parity harness).
- Parent-scale stubs from Step 1 consumed by ≥1 city system.

**Art:** None.

**Relevant surfaces (load when step opens):**
- Step 1 outputs: `Assets/Scripts/Grid/CityCell.cs`, `Assets/Scripts/SaveSystem/GameSaveData.cs` (region_id / country_id), `Assets/Scripts/GridManager.cs` (ParentRegionId / ParentCountryId / GetNeighborStub)
- BUG-55 files: `Assets/Scripts/Managers/GameManagers/EmploymentManager.cs`, `Assets/Scripts/Managers/GameManagers/AutoZoningManager.cs`, `Assets/Scripts/Grid/CellData.cs`, `Assets/Scripts/Managers/GameManagers/GrowthBudgetManager.cs`, `Assets/Scripts/Managers/AutoRoadBuilder.cs`, `Assets/Scripts/Managers/GameManagers/DemandManager.cs`, `Assets/Scripts/UI/GrowthBudgetSlidersController.cs`, `Assets/Scripts/UI/CityStatsUIController.cs`
- BUG-16/17 files: `Assets/Scripts/Managers/GeographyManager.cs`, `Assets/Scripts/TimeManagement/TimeManager.cs`
- BUG-14 file: `Assets/Scripts/UI/UIManager.cs`
- `ia/projects/FEAT-51.md`, `ia/projects/TECH-82.md` (BUG-55 archived — see `BACKLOG-ARCHIVE.md`)
- `ia/specs/simulation-system.md` (§tick-loop), `ia/specs/ui-design-system.md` (§tokens, §patterns)
- Invariants: #3 (no FindObjectOfType per-frame), #5 (GetCell only), #6 (extract to helper)

#### Stage 2.1 — Bug stabilization

**Status:** Done (2026-04-17 — all 4 tasks archived)

**Objectives:** All open crasher, data-corruption, initialization-race, and per-frame-cache bugs at city scale fixed. Invariants #3 and #5 clean.

**Exit:**

- BUG-55 (10 fixes): no crash on New Game or Load Game; growth budget and demand stabilize; `OnDestroy` listener leaks closed in `SimulateGrowthToggle`, `GrowthBudgetSlidersController`, `CityStatsUIController`.
- BUG-14: `UIManager.UpdateUI()` caches `EmploymentManager`, `DemandManager`, `StatisticsManager` in `Awake`/`Start`; zero per-frame `FindObjectOfType` violations (invariant #3).
- BUG-16: `GeographyManager` init race fixed — `isInitialized` gate or Script Execution Order prevents `TimeManager.Update()` reading uninit data.
- BUG-17: `cachedCamera` assigned in `GridManager.Awake()` / `Start()` before `InitializeGrid()` constructs `ChunkCullingSystem`.
- `unity:compile-check` passes; testmode smoke — New Game + Load Game no crash.

**Phases:**

- [x] Phase 1 — Crashers + data integrity (BUG-55 + BUG-14).
- [x] Phase 2 — Init races + null refs (BUG-16 + BUG-17).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | BUG-55 10 fixes | 1 | **BUG-55** | Done (archived) | All 10 fixes landed per `BACKLOG-ARCHIVE.md` BUG-55 row: `EmploymentManager` div/0 already guarded; `CityCell` `TryParse` fallback; `AutoZoningManager` placement-first ordering; `CellData` height-0 floor; `GrowthBudgetManager` min enforced; `DemandManager` empty-zone subtraction; `AutoRoadBuilder` cache invalidation + re-fetch; water height `< 0` strict; demand symmetry 1.2; `OnDestroy` cleanup in 3 controllers. |
| T2.1.2 | BUG-14 per-frame cache | 1 | **BUG-14** | Done (archived) | `UIManager.UpdateUI()` caches `EmploymentManager`, `DemandManager` in `Start` (`StatisticsManager` lookup was dead — removed); `UpdateGridCoordinatesDebugText` uses cached `gameDebugInfoBuilder` + `waterManager`. Zero per-frame `FindObjectOfType` in `UIManager.Hud.cs` (verified). Invariant #3. |
| T2.1.3 | BUG-16 init race | 2 | **BUG-16** | Done (archived) | `GeographyManager.IsInitialized` flips true at tail of `InitializeGeography()`; `TimeManager` caches ref via `[SerializeField]` + `FindObjectOfType` fallback in `Awake` (invariant #3); daily-tick block early-returns pre-init. UI/input responsive during load. Bridge smoke: 0 NRE, compile clean. |
| T2.1.4 | BUG-17 cachedCamera null | 2 | **BUG-17** | Done (archived) | `cachedCamera` promoted to `[SerializeField] private Camera`; new `GridManager.Awake()` resolves via `Camera.main` fallback before `InitializeGrid()` constructs `ChunkCullingSystem`; redundant lazy null-checks removed at `GridManager.cs:366` + `:1294`. Matches canonical init-race guard per `unity-development-context §6`. Compile clean; chunk visibility unchanged. |


#### Stage 2.2 — Tick performance + metrics foundation

**Status:** In Progress (tasks filed 2026-04-17 — TECH-290..TECH-293)

**Objectives:** City tick profiled; egregious non-BUG-55 allocators patched; `MetricsRecorder` Phase 1 integrated (game remains playable without Postgres); EditMode tick budget test establishes Step 3 parity baseline.

**Exit:**

- `docs/city-tick-perf-notes.md` (new): top-5 hotspots + GC allocs + baseline ms/tick after Stage 2.1 fixes.
- Top allocator(s) beyond BUG-55/BUG-14 scope patched (or confirmed acceptable with note).
- TECH-82 Phase 1: `MetricsRecorder.cs` fires per-tick in `SimulationManager`; `city_metrics_history` migration applied; `mcp__territory-ia__city_metrics_query` tool returns time-series; game playable without Postgres.
- EditMode test `TickBudgetTests.cs` (new): isolated tick completes within configured budget threshold; baseline recorded for Step 3 parity harness.

**Phases:**

- [ ] Phase 1 — Profiler run + alloc audit.
- [ ] Phase 2 — MetricsRecorder + tick budget test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | Tick profiler baseline | 1 | **TECH-290** | Draft | Unity Profiler run on `SimulationManager` tick path post Stage 2.1; document top-5 hotspots + GC allocs + baseline ms/tick in `docs/city-tick-perf-notes.md` (new). |
| T2.2.2 | Tick alloc audit + patch | 1 | **TECH-291** | Draft | Scan `SimulationManager` + tick-path managers for avoidable GC alloc (LINQ, boxing, list recreation per-tick); patch top-2 allocators found; annotate `SimulationManager.Update()` with budget note. |
| T2.2.3 | TECH-82 Phase 1 integration | 2 | **TECH-292** | Draft | `MetricsRecorder.cs` (new) fires fire-and-forget per `SimulationManager` tick; `db/migrations/` `city_metrics_history` schema + bridge scripts; `mcp__territory-ia__city_metrics_query` tool per `ia/projects/TECH-82.md` Phase 1 acceptance. Scope-slice of **TECH-82** — does NOT subsume TECH-82 Phases 2–4. |
| T2.2.4 | Tick budget EditMode test | 2 | **TECH-293** | Draft | `Assets/Tests/EditMode/Simulation/TickBudgetTests.cs` (new): isolated tick invocation completes within configured threshold (ms read from profiler notes); threshold field documents Step 3 parity target. |


#### Stage 2.3 — City readability dashboard

**Status:** In Progress (FEAT-51 filed)

**Objectives:** Player reads city state at-a-glance: minimal HUD + ≥3 time-series charts. Delivers FEAT-51 §2.1–§2.5. Chart library decision recorded.

**Exit:**

- `UiTheme.cs` carries `chartLineColor`, `chartAxisColor`, `chartLabelFont`, `chartBackground` fields; `ia/specs/ui-design-system.md` §tokens chart subsection added.
- Chart library decision (XCharts or equivalent) recorded in `ia/projects/FEAT-51.md` Decision Log.
- FEAT-51 acceptance (§8): history ringbuffer + derived metrics + chart engine + HUD card layout; ≥3 charts (population trend, employment rate, treasury balance) visible; no per-frame `FindObjectOfType`; `UiTheme` tokens applied throughout.
- Testmode smoke: ≥3 charts render after New Game tick.

**Phases:**

- [ ] Phase 1 — UiTheme chart tokens + chart library spike.
- [ ] Phase 2 — Full dashboard delivery + acceptance gate.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | UiTheme chart tokens | 1 | _pending_ | _pending_ | Add `chartLineColor`, `chartAxisColor`, `chartLabelFont`, `chartBackground` fields to `UiTheme.cs`; add chart-tokens subsection to `ia/specs/ui-design-system.md` §tokens. |
| T2.3.2 | Chart library spike | 1 | _pending_ | _pending_ | Evaluate XCharts vs alternatives in Unity; create `ChartDemo` prefab (new) with `LineChart` wired to dummy data; validate `UiTheme` token bind; record library decision in `ia/projects/FEAT-51.md` Decision Log. |
| T2.3.3 | FEAT-51 dashboard delivery | 2 | **FEAT-51** | Draft | Full game data dashboard per `ia/projects/FEAT-51.md` §8: history ringbuffer + derived metrics + chart engine + HUD card layout; ≥3 charts; UiTheme tokens applied; no per-frame `FindObjectOfType`. |
| T2.3.4 | Dashboard acceptance gate | 2 | _pending_ | _pending_ | Testmode smoke: ≥3 charts render after New Game tick; token audit — all chart colors sourced from `UiTheme`; `unity:compile-check`; confirm FEAT-51 §8 acceptance met; Decision Log entry verified complete. |


#### Stage 2.4 — Parent-stub consumption

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** ≥1 city UI panel + ≥1 sim system actively consume Step 1 stubs (ParentRegionId / ParentCountryId / GetNeighborStub). Establishes consumer pattern for Step 3 to flesh out.

**Exit:**

- `ParentContextPanel.cs` (new) in city HUD: reads `GridManager.ParentRegionId` + `ParentCountryId`; displays region + country placeholder names.
- `NeighborCityStubPanel.cs` (new) in city HUD sidebar: reads `GridManager.GetNeighborStub(side)` for all border sides; renders ≥1 stub card (display name + border direction); inert.
- `DemandManager.GetExternalDemandModifier()` (new method): reads `GetNeighborStub()` list; returns `1.0f + 0.05f * stubCount` as placeholder; called in demand calculation; `GridManager` cached in `Awake` (invariant #3). Establishes consumption pattern for Step 3.
- Testmode smoke: after New Game, `ParentContextPanel` shows non-null values; `GetNeighborStub()` returns ≥1 stub; `GetExternalDemandModifier()` returns > 1.0f.

**Phases:**

- [ ] Phase 1 — Parent context + neighbor stub UI panels.
- [ ] Phase 2 — Sim consumer + integration smoke.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.4.1 | Parent context panel | 1 | _pending_ | _pending_ | `ParentContextPanel.cs` (new) MonoBehaviour in city HUD: reads `GridManager.ParentRegionId` + `ParentCountryId`; displays region + country placeholder name; binds on scene load. Follows `ia/specs/ui-design-system.md` §HUD patterns. |
| T2.4.2 | Neighbor stub panel | 1 | _pending_ | _pending_ | `NeighborCityStubPanel.cs` (new): iterates border sides via `GridManager.GetNeighborStub(side)`; renders ≥1 HUD stub card (display name, border direction enum); inert — no behavior, no data mutation. |
| T2.4.3 | DemandManager parent modifier | 2 | _pending_ | _pending_ | `DemandManager.GetExternalDemandModifier()` (new): reads neighbor stub list; returns `1.0f + 0.05f * stubCount`; wired into demand calculation. Cache `GridManager` in `Awake` (invariant #3). Pattern seeded for Step 3 expansion. |
| T2.4.4 | Parent-stub integration smoke | 2 | _pending_ | _pending_ | Testmode smoke scenario: New Game → assert `ParentContextPanel` non-null display; assert `GetNeighborStub()` count ≥ 1; assert `GetExternalDemandModifier()` > 1.0f. Confirms Step 1 stubs consumed end-to-end. |

### Step 3 — Multi-scale infrastructure

**Status:** Draft (decomposition deferred until Step 2 → `Final`)

Scale-neutral spine. After Step 3: pure-compute sim modules, `SimulationScale` enum + `ISimulationModel` contract, per-scale snapshot schema (city first), relational multi-scale save, deterministic city evolution algorithm, snapshot freeze/reconstruct, procedural-scale generator, scale-switch UX skeleton — all exercised by one scale (city).

**Exit criteria:**

- City dormant → snapshot + evolution → reconstruct round-trip inside parity budget (empirical playtest).
- Procedural city generated from region-like params + seed, loadable as normal `GameSaveData`.
- Save format holds cities inside region inside country node (relational), even if region/country evolution = stubs.
- Switch out of city + back round-trips correctly (Δt = 0 → same state modulo parity budget).
- Compute-lib modules callable headless for ≥1 non-trivial city sub-system AND city evolution algorithm.
- Every scale reads same real-time calendar via single shared clock API.
- Scale-switch UX — semantic zoom: continuous camera zoom across per-scale zoom bands (fixed transition points, same regardless of map size). Zoom bands: city 2–30, transition 30–60, region 60–200, transition 200–400, country 400+. Procedural fog/cloud mask (fullscreen noise shader) hides scene swap in transition band. Player cancels mid-transition by scrolling back (fog reverses). Scale label appears near threshold (e.g. "Entering Region View"). Reconstruction latency mitigation: region shell pre-cached low-res; progressive reconstruction; snapshot cache per city node. Post-MVP alternatives (truly continuous rendering, animated fly-to, minimap click, asymmetric zoom-out vs zoom-in) → `multi-scale-post-mvp-expansion.md` §6.4.
- Per-scale tool panel swap via `ScaleToolProvider`: toolbar rebuilds during fog mask per active scale. Minimal MVP tool sets — city: existing tools; region: found city + draw highway + budget; country: priorities + budget. Fixed always-visible strip for shared tools (demolish, inspect, speed control). Consistent semantic keybindings across scales. Per-scale tool state preserved in-session (not in save — save persistence post-MVP).
- Speed control unchanged across all scales.

**Art:** Procedural fog/cloud transition shader (fullscreen noise quad). Scale label UI. Per-scale toolbar icons (region + country tool sets).

**Relevant surfaces:** `Assets/Scripts/SimulationManager.cs`, `Assets/Scripts/TimeManagement/TimeManager.cs`, `Assets/Scripts/SaveSystem/`, `ia/specs/simulation-system.md` (§tick-loop); `backlog_issue TECH-38` / `TECH-82` / `TECH-18` / `TECH-31` / `TECH-15` / `TECH-34` / `FEAT-46`; `ia/projects/multi-scale-post-mvp-expansion.md` §6.4 (scale-switch UX alternatives).

### Step 4 — Region MVP

**Status:** Draft (decomposition deferred until Step 3 → `Final`)

Region **playable as active scale** w/ own live-sim tick loop + deterministic evolution algorithm.

**Exit criteria:**

- Player switches city → region, sees other cities reconstructed from snapshots + pending deltas, plays region as active scale, switches back into any city (visited or procedural) w/o state loss + inside parity budget.
- Region active-scale tick: migration pressure, basic trade flow, founding new cities.
- Region evolution algorithm: deterministic `evolve` analogous to city algorithm.
- ≥1 economic flow crosses scales: city exports feed inter-city trade in region layer, balance feeds back into city evolution params on switch-down.
- Region has 1 natural resource type + supports founding new cities.
- Player-authored dormant control (min): from region view, player sets budget allocation per dormant child city.
- Save/load preserves city + region end-to-end in relational schema.
- Parity-budget checks for region evolution algorithm.
- City event bubble-up at switch-out visible in region dashboard (plain text summary OK).

**Art:** Region cell sprites, city-node visual at region zoom, region UI elements, procedural region art templates.

**Relevant surfaces:** `backlog_issue FEAT-09` / `FEAT-47`; `ia/specs/simulation-system.md`; region sim contracts land in Step 3 — fetch then.

### Step 5 — Country MVP

**Status:** Draft (decomposition deferred until Step 4 → `Final`)

Country **playable as active scale**. After Step 5: three-scale MVP complete.

**Exit criteria:**

- Player switches to country map, plays country as active scale, exercises min head-of-state loop.
- Head-of-state loop (min): assign national budget across small fixed category set, launch ≥1 national infrastructure project propagating down to region/city, create ≥1 new region node.
- Country policy change while active baked into region + city evolution params on switch-down.
- Country evolution algorithm: deterministic fast-forward via long-period economic drift.
- Player-authored dormant control (min): from country view, player sets budget allocation per dormant region.
- Save/load preserves all three scales end-to-end in relational schema.
- Parity-budget checks for country evolution algorithm.
- Region/city events bubbled up at switch-out visible in country dashboard (plain text summary OK).

**Art:** Country cell sprites, region-node visual at country zoom, country UI elements, head-of-state UI.

**Relevant surfaces:** country sim contracts land in Step 3 — fetch then; `ia/projects/multi-scale-post-mvp-expansion.md` (head-of-state scope boundary).

---

## Deferred decomposition

- **Step 2 — City MVP close:** decomposed 2026-04-16. Stages: Bug stabilization, Tick performance + metrics foundation, City readability dashboard, Parent-stub consumption.
- **Steps 3–5:** remain at skeleton granularity (Objectives implicit in step blurb + Exit criteria + Relevant surfaces). Full Stage / Phase / Task decomposition lands when parent step → `In Progress`. Candidate-issue pointers live inline on each step's **Relevant surfaces** line; new-feature-row candidates surface during that step's decomposition pass, filed under `§ Multi-scale simulation lane` in `BACKLOG.md`. Do NOT pre-file Step 3–5 rows.

---

## Orchestration guardrails

**Do:**

- Propose edits to step skeletons when stage exposes missing load-bearing item.
- Push MVP-scope-creep into `multi-scale-post-mvp-expansion.md`.
- Create step/stage orchestrators lazily when parent enters "in progress".

**Do not:**

- Resurrect N-tick aggregate publish model. Dormant scales evolve only via deterministic evolution algorithm.
- Resurrect time-dilation framing. Single shared real-time clock.
- Resurrect single-jsonb save tree. Save is relational.
- Resurrect NPC leader modeling. Player = only actor in MVP.
- Reintroduce climate, shaping events, defense structures, expropriation, agricultural zones, progressive loading, shared cross-scale dashboard, auto mode, scale unlock, or process-engineering gap closures into MVP stages. All post-MVP.
- File BACKLOG rows for new FEAT ideas outside backlog triage pass.
- Give time estimates.

