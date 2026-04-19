# CityStats Overhaul — Master Plan (MVP)

> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Replace the `CityStats` god-class with a typed read-model facade (`CityStatsFacade`) backed by a columnar ring-buffer store (`ColumnarStatsStore`), migrate all consumers to the facade, add region/country scale rollup facades, and surface city metrics in a new `web/app/stats` route. Overlays, per-cell drill-down, history persistence in save files, and region/country Postgres tables are out of scope (see Deferred section of `docs/citystats-overhaul-exploration.md`).
>
> **Exploration source:** `docs/citystats-overhaul-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points are ground truth).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach E (hybrid facade + columnar store) selected; Approach A (incremental) and D (web-first) ruled out.
> - Legacy `CityStats` MonoBehaviour becomes shim implementing `ICityStats`; field signature preserved verbatim during migration.
> - `CityMetricsInsertPayload` Postgres row schema unchanged; `MetricsRecorder` only swaps data source.
> - `GameSaveData` MVP: scalar-only snapshot via `facade.ExportSaveSlice()`; no history in save; no `schemaVersion` bump unless fields added.
> - Performance budget default: 256-tick ring buffer; revisit before Stage 1.1 capacity constants locked.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/citystats-overhaul-exploration.md` — full design + architecture + examples. Design Expansion block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — **#3** (no `FindObjectOfType` in `Update` or per-frame loops), **#4** (no new singletons — `CityStatsFacade` is `MonoBehaviour` with Inspector wire, not singleton), **#6** (no bloat on `GridManager` — facade lives in its own `GameManagers` class).
> - `ia/specs/simulation-system.md §Tick execution order` (lines 11–26) — steps 1-5 in `ProcessSimulationTick` NOT reordered; `BeginTick`/`EndTick` bracket wraps outside steps.
> - `ia/specs/persistence-system.md §Save` — `schemaVersion` bump required if any fields added to `GameSaveData`.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

---

### Stage 1 — Facade + Store Infra (additive, no consumer migration) / Core types (IStatsReadModel, StatKey, ColumnarStatsStore)

**Status:** In Progress (tasks filed: TECH-303, TECH-304)

**Objectives:** Define the typed contract and ring-buffer store before any MonoBehaviour is touched. No Unity scene changes.

**Exit:**

- `IStatsReadModel.cs`: `GetScalar(StatKey)`, `GetSeries(StatKey, int windowTicks)`, `EnumerateRows(string dimension, Predicate<object> filter)` compile.
- `StatKey.cs`: one entry per current `CityStats` public field + `RegionPopulation` / `CountryPopulation` stubs.
- `ColumnarStatsStore.cs`: `Publish(StatKey, float)`, `Set(StatKey, float)`, `GetScalar(StatKey)`, `GetSeries(StatKey, int)`, `FlushToSeries()` compile; default capacity 256; plain C# class, no MonoBehaviour dependency.
- Phase 1 — Define contract types + store implementation.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T1.1 | **TECH-303** | Draft | Add `IStatsReadModel.cs`: scalar `GetScalar(StatKey) → float`, series `GetSeries(StatKey, int windowTicks) → float[]`, row enumeration `EnumerateRows(string dimension, Predicate<object> filter) → IEnumerable<object>`. Add `StatKey.cs` enum: one entry per current `CityStats` public field (population, money, happiness, forestCoverage, unemployment, etc.) + stubs `RegionPopulation`, `CountryPopulation`. No runtime wiring. |
| T1.2 | **TECH-304** | Draft | Add `ColumnarStatsStore.cs` (plain C# class, no MonoBehaviour): parallel `float[]` ring buffers keyed by `StatKey` (capacity settable via `int RingCapacity`, default 256); `Publish(StatKey, float delta)` accumulates running value; `Set(StatKey, float value)` overwrites; `FlushToSeries()` writes net running value to ring and resets accumulator; `GetScalar(StatKey) → float` returns running value; `GetSeries(StatKey, int windowTicks) → float[]` returns last N ring entries. |

---

### Stage 2 — Facade + Store Infra (additive, no consumer migration) / CityStatsFacade MonoBehaviour + tick bracket

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire `CityStatsFacade` into the scene and thread `BeginTick`/`EndTick` into `SimulationManager` without altering tick execution order.

**Exit:**

- `CityStatsFacade : MonoBehaviour, IStatsReadModel` compiles; `[SerializeField]`-wired in scene Inspector alongside existing `CityStats`.
- `SimulationManager.ProcessSimulationTick` calls `_facade.BeginTick()` before step 1 and `_facade.EndTick()` inside existing `finally` block (`SimulationManager.cs:85`); steps 1-5 order unchanged (per `sim §Tick execution order`).
- `Action OnTickEnd` event fires on each `EndTick`; zero consumers yet (wired in Stage 2.1).
- Phase 1 — Add CityStatsFacade + wire tick bracket in SimulationManager.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T2.1 | _pending_ | _pending_ | Add `CityStatsFacade.cs` : `MonoBehaviour`, `IStatsReadModel`; owns `ColumnarStatsStore _store` (composition, instantiated in `Awake`); exposes `BeginTick()` (resets per-tick accumulator), `Publish(StatKey, float delta)`, `Set(StatKey, float)`, `EndTick()` (calls `_store.FlushToSeries()` + fires `public event Action OnTickEnd`); delegates `GetScalar`/`GetSeries`/`EnumerateRows` to `_store`. `[SerializeField]` Inspector wire — no singleton (invariant #4). |
| T2.2 | _pending_ | _pending_ | Add `[SerializeField] private CityStatsFacade _facade` to `SimulationManager.cs`; call `_facade?.BeginTick()` before step 1 inside `try` body (`SimulationManager.cs:63`) and `_facade?.EndTick()` in the existing `finally` block (`:85`). Null-guard throughout. Tick execution order (steps 1-5 per `sim §Tick execution order`) unchanged — bracket wraps, does not reorder. |

---

### Stage 3 — Facade + Store Infra (additive, no consumer migration) / CityStats shim dual-write + MetricsRecorder swap + EditMode test

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Legacy `CityStats` public fields become property wrappers forwarding into facade; `MetricsRecorder` uses `SnapshotForBridge()`. EditMode test confirms end-to-end data flow.

**Exit:**

- All `CityStats` public fields compile as properties forwarding to `_facade.Set(StatKey.X, value)`; `ICityStats` signature (`ICityStats.cs:9`) unchanged.
- `MetricsRecorder.BuildPayload` removed; `_facade.SnapshotForBridge(tick)` returns same `CityMetricsInsertPayload`; Postgres row schema unchanged.
- EditMode test passes: one tick → facade series length 1; scalar matches legacy field value.
- `npm run unity:compile-check` clean.
- Phase 1 — CityStats property wrappers + validation helper.
- Phase 2 — MetricsRecorder SnapshotForBridge + EditMode test.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T3.1 | _pending_ | _pending_ | Convert `CityStats.cs` public fields to properties: backing field `_xValue`; getter returns `_xValue`; setter calls `_facade?.Set(StatKey.X, value)` then `_xValue = value`. Add `[SerializeField] private CityStatsFacade _facade`. Preserve `ICityStats` signature (`ICityStats.cs:9`) verbatim — no method or property name changes. Cover all public fields (population, money, happiness, etc.). |
| T3.2 | _pending_ | _pending_ | Add `[ContextMenu("Verify Shim Wiring")]` debug helper on `CityStats` asserting `_facade != null && _facade.enabled`. Fire `Debug.LogWarning` in `Awake` if `_facade` null — Inspector wire only; no `FindObjectOfType` (invariant #3). |
| T3.3 | _pending_ | _pending_ | Add `SnapshotForBridge(int tickIndex) → CityMetricsInsertPayload` on `CityStatsFacade`: copies `GetScalar(StatKey.X)` for each payload field matching `MetricsRecorder.BuildPayload` output shape (`MetricsRecorder.cs:66–92` — population, money, happiness, game_date, demand, employment, forest01, happiness01). Replace `MetricsRecorder.BuildPayload(tick)` call (`MetricsRecorder.cs:54`) with `_facade.SnapshotForBridge(tick)`; add `[SerializeField] private CityStatsFacade _facade` to `MetricsRecorder`; null guard → early return matching existing null-cityStats guard. |
| T3.4 | _pending_ | _pending_ | Add EditMode test `CityStatsFacadeShimTest`: create `CityStatsFacade` + `CityStats` in test context; call `_facade.BeginTick()`; set `cityStats.population = 1000` (triggers shim setter `→ _facade.Set(StatKey.Population, 1000)`); call `_facade.EndTick()`; assert `_facade.GetSeries(StatKey.Population, 1)[0] == 1000f` and `_facade.GetScalar(StatKey.Population) == 1000f`. |

---

### Stage 4 — Consumer Migration / CityStatsUIController per-tick subscription

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Drop per-frame `Update` stat polling; subscribe to `_facade.OnTickEnd`; handle initial paint and paused-state edge case.

**Exit:**

- `CityStatsUIController.Update` no longer calls `UpdateStatisticsDisplay`.
- `OnEnable` subscribes to `_facade.OnTickEnd`; `OnDisable` unsubscribes.
- Labels populated on first `OnEnable` regardless of pause state.
- `UpdateStatisticsDisplay` reads via `_facade.GetScalar(StatKey.X)` not direct `cityStats.*` fields.
- Invariant #3: `_facade` cached via Inspector wire in `Awake`; no `FindObjectOfType` in hot path.
- Phase 1 — Subscribe to OnTickEnd + initial paint fix.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T4.1 | _pending_ | _pending_ | In `CityStatsUIController.cs`: add `[SerializeField] private CityStatsFacade _facade`; in `OnEnable` subscribe `_facade.OnTickEnd += OnFacadeEndTick`; in `OnDisable` unsubscribe; add `void OnFacadeEndTick() => UpdateStatisticsDisplay()`; remove `UpdateStatisticsDisplay()` call from `Update()` (`:58`). Wire via Inspector (invariant #4, not `FindObjectOfType`). |
| T4.2 | _pending_ | _pending_ | Handle initial paint: at end of `OnEnable`, after subscribing, call `UpdateStatisticsDisplay()` once (covers `simulateGrowth == false` / paused edge case — no `EndTick` fires until unpaused). In `UpdateStatisticsDisplay` (`:176`), replace direct `cityStats.*` reads with `_facade.GetScalar(StatKey.Population)`, `GetScalar(StatKey.Money)`, `GetScalar(StatKey.Happiness)`, `GetScalar(StatKey.Unemployment)` etc. Remove `cityStats` field ref from this controller. |

---

### Stage 5 — Consumer Migration / Producer managers publish via facade

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** All producer managers dual-write into facade alongside existing `CityStats` field writes (shim already forwards those writes, but explicit `_facade.Set` calls at each write site give grep-auditable migration confidence).

**Exit:**

- `EconomyManager`, `EmploymentManager`, `DemandManager`: `[SerializeField] CityStatsFacade _facade` wired; `_facade.Set(StatKey.X, value)` called at each `cityStats.*` write site.
- `ZoneManager`, `RoadManager`, `ForestManager`, `WaterManager`: same pattern.
- `npm run unity:compile-check` clean after all managers updated.
- Phase 1 — Economy + Employment + Demand managers.
- Phase 2 — Zone + Road + Forest + Water managers.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T5.1 | _pending_ | _pending_ | `EconomyManager.cs`: grep `cityStats\.` write sites; add `[SerializeField] private CityStatsFacade _facade`; at each write site add `_facade?.Set(StatKey.Money, cityStats.money)` immediately after the existing write. `EmploymentManager.cs`: same → `StatKey.Unemployment`, `StatKey.Jobs`. |
| T5.2 | _pending_ | _pending_ | `DemandManager.cs`: grep `cityStats\.` write sites (residential, commercial, industrial demand fields); add `_facade?.Set(StatKey.DemandR/C/I, value)` at each site. Confirm `StatKey` enum covers demand fields; add missing entries to `StatKey.cs` if gap found. |
| T5.3 | _pending_ | _pending_ | `ZoneManager.cs` + `RoadManager.cs`: grep `cityStats\.` write sites in each; add `[SerializeField] private CityStatsFacade _facade`; call `_facade?.Set(StatKey.X, value)` at each site. Identify any ZoneManager-specific stats (zonedResidential etc.) and add corresponding `StatKey` stubs if missing. |
| T5.4 | _pending_ | _pending_ | `ForestManager.cs` + `WaterManager.cs`: same pattern; `StatKey.ForestCoverage` + `StatKey.WaterCoverage`; confirm `GetForestCoveragePercentage()` source value (`MetricsRecorder.cs:81`) matches the value being set — use same computation site for the `_facade.Set` call. |

---

### Stage 6 — Consumer Migration / StatisticsManager migration + deletion

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Migrate all `StatisticTrend` consumers to facade series getters; delete `StatisticsManager` + `StatisticTrend` once no compile-time reference remains.

**Exit:**

- Zero compile-time references to `StatisticTrend` or `StatisticsManager`.
- `StatisticsManager.cs` (and `.meta`) deleted.
- `npm run unity:compile-check` clean.
- EditMode test: `facade.GetSeries(StatKey.Population, 2)` returns data after two ticks.
- Phase 1 — Migrate StatisticTrend consumers to facade; mark obsolete.
- Phase 2 — Delete StatisticsManager + StatisticTrend + compile-check + EditMode test.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T6.1 | _pending_ | _pending_ | Grep all `populationTrend`, `unemploymentTrend`, `jobsTrend`, `residentialDemandTrend`, `commercialDemandTrend`, `industrialDemandTrend`, `incomeTrend`, `happinessTrend` consumers; for each replace `xTrend.values` / `xTrend.currentValue` reads with `_facade.GetSeries(StatKey.X, windowTicks: 30)` / `_facade.GetScalar(StatKey.X)`; wire `_facade` ref where not yet present. |
| T6.2 | _pending_ | _pending_ | Stop `StatisticsManager.UpdateStatistics()` (or equivalent update loop) from writing to `StatisticTrend` objects: guard body with early `return`. Add `[Obsolete("Migrated to CityStatsFacade — pending deletion")]` to `StatisticsManager` and `StatisticTrend` classes. Do NOT delete yet. |
| T6.3 | _pending_ | _pending_ | Delete `Assets/Scripts/Managers/GameManagers/StatisticsManager.cs` (+ `.meta`); grep `StatisticsManager\ | StatisticTrend` to confirm zero remaining references; remove `StatisticsManager` component from any scene Inspector references; run `npm run unity:compile-check`. |
| T6.4 | _pending_ | _pending_ | Add EditMode test `StatisticsManagerMigrationTest`: fire two ticks; assert `facade.GetSeries(StatKey.Population, 2)` length == 2 and values > 0; assert `facade.GetSeries(StatKey.DemandR, 2)` non-zero (confirms demand manager publishing). Verifies facade fully replaces `StatisticTrend` ring buffer. |

---

### Stage 7 — Multi-scale Rollup + Web Stats Surface / RegionStatsFacade + CountryStatsFacade rollup

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Add dormant-scale facades with typed rollup aggregation; wire into **Scale switch** save-leaving hook.

**Exit:**

- `RegionStatsFacade` + `CountryStatsFacade` compile; `Rollup()` aggregates correctly.
- Rollup wired in Scale switch save-leaving hook (per `multi-scale-master-plan.md` Step 3); dormant snapshot frozen until re-entry.
- `StatKey` extended with live region/country entries (`RegionPopulation`, `RegionHappiness`, `RegionMoney`, `CountryPopulation`, `CountryHappiness`, `CountryMoney`) replacing stubs from Stage 1.1.
- PlayMode smoke: scale switch → `regionFacade.GetScalar(StatKey.RegionPopulation)` > 0.
- Phase 1 — Scaffold RegionStatsFacade + CountryStatsFacade + StatKey live entries.
- Phase 2 — Rollup wiring in Scale switch hook + PlayMode smoke.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T7.1 | _pending_ | _pending_ | Add `RegionStatsFacade.cs` : `MonoBehaviour, IStatsReadModel`; composition `ColumnarStatsStore _store` (capacity default 64 — dormant scales tick rarely); `Rollup(IEnumerable<CityStatsFacade> cities)` aggregates: `Set(StatKey.RegionPopulation, cities.Sum(c => c.GetScalar(StatKey.Population)))`, `Set(StatKey.RegionHappiness, cities.Average(...))`, `Set(StatKey.RegionMoney, cities.Sum(...))`; no `BeginTick`/`EndTick`. `[SerializeField]` Inspector wire. |
| T7.2 | _pending_ | _pending_ | Add `CountryStatsFacade.cs` symmetrically (aggregates `RegionStatsFacade` children). Extend `StatKey.cs`: replace `RegionPopulation`/`CountryPopulation` stubs with real entries + add `RegionHappiness`, `RegionMoney`, `CountryHappiness`, `CountryMoney`. |
| T7.3 | _pending_ | _pending_ | Wire `regionFacade.Rollup(activeCityFacades)` in the **Scale switch** save-leaving step; read `multi-scale-master-plan.md` Step 3 save-leaving section before editing to confirm exact hook method and call site. Dormant facade holds frozen snapshot — do NOT call `Rollup` again until scale re-entry. Wire `CountryStatsFacade.Rollup(regionFacades)` symmetrically. |
| T7.4 | _pending_ | _pending_ | Add PlayMode smoke test: switch from city → region scale; assert `regionFacade.GetScalar(StatKey.RegionPopulation) > 0`; switch back to city; assert `cityFacade.GetScalar(StatKey.Population) > 0` (city facade still live). Mirrors scale-switch test pattern in `multi-scale-master-plan.md`. |

---

### Stage 8 — Multi-scale Rollup + Web Stats Surface / web/app/stats route

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** New `/stats` page in Next.js app: time-series line chart + sortable table backed by `city_metrics_history` Postgres table. Reuses existing web components without modification.

**Exit:**

- `web/app/stats/page.tsx` renders without runtime errors; `export const revalidate = 60`.
- Line chart: population + happiness + money over last 30 ticks via `PlanChartClient`.
- Sortable table: all `city_metrics_history` columns (tick index, date, population, money, happiness, demand_r/c/i, employment) newest-first via `DataTable`.
- `web/lib/db/statsQueries.ts` queries compile; typed return matches `CityMetricsInsertPayload` columns.
- `npm run validate:web` clean.
- Phase 1 — Route scaffold + Postgres query helpers.
- Phase 2 — Line chart + sortable table UI components.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T8.1 | _pending_ | _pending_ | Add `web/app/stats/page.tsx` (new, Server Component): `export const revalidate = 60`; call `getLatestCityMetrics(100)` + `getCityMetricsSeries('population', 30)` from `statsQueries.ts`; pass arrays as props to client components. No UI yet — confirms data shape and `npm run validate:web` build succeeds. Follow ISR pattern from `web/app/dashboard/page.tsx`. |
| T8.2 | _pending_ | _pending_ | Add `web/lib/db/statsQueries.ts` (new): `getLatestCityMetrics(limit: number): Promise<CityMetricsRow[]>` (SELECT * FROM city_metrics_history ORDER BY simulation_tick_index DESC LIMIT $1) + `getCityMetricsSeries(metric: string, ticks: number): Promise<{tick: number, value: number}[]>`; use `web/lib/db/client.ts` client; define `CityMetricsRow` type matching `CityMetricsInsertPayload` columns. |
| T8.3 | _pending_ | _pending_ | Wire time-series chart in `web/app/stats/page.tsx`: pass `getCityMetricsSeries` data to `PlanChartClient` (`web/components/PlanChartClient.tsx` reused as-is); render three series — population, happiness, money — over last 30 ticks. Follow `PlanChartClient` prop contract from existing dashboard usage. |
| T8.4 | _pending_ | _pending_ | Wire sortable table: pass `getLatestCityMetrics(100)` rows to `DataTable` (`web/components/DataTable.tsx` reused as-is); columns: tick index, date, population, money, happiness, demand_r/c/i, employment; default sort descending by tick index. Follow `DataTable` prop contract from existing dashboard usage. |

---

### Stage 9 — Multi-scale Rollup + Web Stats Surface / Glossary + spec updates

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Add canonical glossary rows for all new types; cross-link from existing entries; update `managers-reference §Helper Services` table. Validate IA clean.

**Exit:**

- `glossary_discover` returns **StatsFacade**, **ColumnarStatsStore**, **StatKey**, **IStatsReadModel** for relevant queries.
- **City metrics history** glossary entry updated to note `StatsFacade.SnapshotForBridge()` as data source.
- `ia/specs/managers-reference.md §Helper Services` table includes `CityStatsFacade`, `RegionStatsFacade`, `CountryStatsFacade` rows.
- `npm run validate:all` clean.
- Phase 1 — Add new glossary rows.
- Phase 2 — Cross-link existing entries + managers-reference update + validate:all.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T9.1 | _pending_ | _pending_ | Add glossary row **StatsFacade**: "Typed read-model facade (`CityStatsFacade` / `RegionStatsFacade` / `CountryStatsFacade`) implementing `IStatsReadModel`; backed by `ColumnarStatsStore`; `[SerializeField]` Inspector-wired; exposes `BeginTick`/`Publish`/`Set`/`EndTick` + `GetScalar`/`GetSeries`/`SnapshotForBridge`." Category: City systems. Spec ref: `mgrs §Helper Services`. |
| T9.2 | _pending_ | _pending_ | Add glossary rows: **ColumnarStatsStore** ("Plain C# ring-buffer store keyed by `StatKey`; capacity 256 city / 64 dormant scale; `FlushToSeries()` on `EndTick`."), **StatKey** ("Enum of canonical metric identifiers shared across facade, store, and consumers."), **IStatsReadModel** ("Pull contract for facade consumers: `GetScalar`, `GetSeries`, `EnumerateRows`."); update **City metrics history** entry: append "Data sourced via `CityStatsFacade.SnapshotForBridge(tick)` since citystats-overhaul." |
| T9.3 | _pending_ | _pending_ | Cross-link in existing glossary: append to **Simulation tick** definition "Fires `CityStatsFacade.EndTick` on each tick (since citystats-overhaul)."; append to **Scale switch** definition "Triggers `RegionStatsFacade.Rollup(activeCities)` in save-leaving step (since citystats-overhaul).". Add `CityStatsFacade`, `RegionStatsFacade`, `CountryStatsFacade` rows to `ia/specs/managers-reference.md §Helper Services` table with role descriptions. |
| T9.4 | _pending_ | _pending_ | Run `npm run validate:all`; confirm zero errors on frontmatter + link checks. Run `npm run validate:web` to confirm web build still clean after Stage 3.2 additions. Report exit codes. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/citystats-overhaul-master-plan.md Stage 1.1"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/citystats-overhaul-exploration.md`.
- Before Stage 3.1: read `multi-scale-master-plan.md` Step 3 save-leaving section to confirm exact **Scale switch** hook method name before editing.
- Before Stage 1.1 capacity constant lock: confirm 256-tick ring buffer acceptable at max map size (open question from exploration review notes).

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote Deferred items (overlay migration to `GetRasterView`, per-cell drill-down, history persistence in save, `region_metrics_history` / `country_metrics_history` Postgres tables, dark-mode palette) into MVP stages.
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Add responsibilities to `GridManager` (invariant #6) — `CityStatsFacade` and store live in `GameManagers/`, not in `GridManager`.
- Re-enable `UrbanizationProposal` (invariant #11 — permanently obsolete).
