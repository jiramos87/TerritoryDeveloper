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
