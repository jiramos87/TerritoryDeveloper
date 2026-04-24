### Stage 1 — Facade + Store Infra (additive, no consumer migration) / Core types (IStatsReadModel, StatKey, ColumnarStatsStore)

**Status:** Final (TECH-303, TECH-304 archived 2026-04-21)

**Objectives:** Define the typed contract and ring-buffer store before any MonoBehaviour is touched. No Unity scene changes.

**Exit:**

- `IStatsReadModel.cs`: `GetScalar(StatKey)`, `GetSeries(StatKey, int windowTicks)`, `EnumerateRows(string dimension, Predicate<object> filter)` compile.
- `StatKey.cs`: one entry per current `CityStats` public field + `RegionPopulation` / `CountryPopulation` stubs.
- `ColumnarStatsStore.cs`: `Publish(StatKey, float)`, `Set(StatKey, float)`, `GetScalar(StatKey)`, `GetSeries(StatKey, int)`, `FlushToSeries()` compile; default capacity 256; plain C# class, no MonoBehaviour dependency.
- Phase 1 — Define contract types + store implementation.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T1.1 | **TECH-303** | Done (archived) | Add `IStatsReadModel.cs`: scalar `GetScalar(StatKey) → float`, series `GetSeries(StatKey, int windowTicks) → float[]`, row enumeration `EnumerateRows(string dimension, Predicate<object> filter) → IEnumerable<object>`. Add `StatKey.cs` enum: one entry per current `CityStats` public field (population, money, happiness, forestCoverage, unemployment, etc.) + stubs `RegionPopulation`, `CountryPopulation`. No runtime wiring. |
| T1.2 | **TECH-304** | Done (archived) | Add `ColumnarStatsStore.cs` (plain C# class, no MonoBehaviour): parallel `float[]` ring buffers keyed by `StatKey` (capacity settable via `int RingCapacity`, default 256); `Publish(StatKey, float delta)` accumulates running value; `Set(StatKey, float value)` overwrites; `FlushToSeries()` writes net running value to ring and resets accumulator; `GetScalar(StatKey) → float` returns running value; `GetSeries(StatKey, int windowTicks) → float[]` returns last N ring entries. |

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — TECH-303 + TECH-304 §Plan Author aligned w/ Stage 1 block, Exit criteria, invariants #3/#4/#6. Spec frontmatter `parent_plan` + `task_key` mirror orchestrator T1.1 / T1.2. No fix tuples. Downstream: `/ship-stage ia/projects/citystats-overhaul-master-plan.md 1`.

---
