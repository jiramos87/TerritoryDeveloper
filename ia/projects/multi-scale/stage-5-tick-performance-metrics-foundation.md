### Stage 5 — City MVP close / Tick performance + metrics foundation

**Status:** In Progress (tasks filed 2026-04-17 — TECH-290..TECH-293)

**Objectives:** City tick profiled; egregious non-BUG-55 allocators patched; `MetricsRecorder` Phase 1 integration verified (already landed 2026-04-22 — game remains playable without Postgres); EditMode tick budget test establishes Step 3 parity baseline.

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
| T5.3 | TECH-82 Phase 1 integration | **TECH-292** | Draft | Verify + document TECH-82 Phase 1 integration (all three acceptance criteria already landed as of 2026-04-22 audit): (1) `Assets/Scripts/Managers/GameManagers/MetricsRecorder.cs` fires fire-and-forget per `SimulationManager` tick — present; (2) `db/migrations/0009_city_metrics_history.sql` applied — present; (3) `mcp__territory-ia__city_metrics_query` tool returns time-series — present at `tools/mcp-ia-server/src/tools/city-metrics-query.ts`. Task scope = verification pass + acceptance-criteria sign-off in closeout notes, not new authoring. Cross-ref: citystats Stage 3 T3.3 plans to rewire `MetricsRecorder.BuildPayload` via `CityStatsFacade.SnapshotForBridge(tick)` — sequencing note, no edit here. Scope-slice of **TECH-82** — does NOT subsume TECH-82 Phases 2–4. |
| T5.4 | Tick budget EditMode test | **TECH-293** | Draft | `Assets/Tests/EditMode/Simulation/TickBudgetTests.cs` (new): isolated tick invocation completes within configured threshold (ms read from profiler notes); threshold field documents Step 3 parity target. |
