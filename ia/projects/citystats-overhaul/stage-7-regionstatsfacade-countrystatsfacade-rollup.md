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
