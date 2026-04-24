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
