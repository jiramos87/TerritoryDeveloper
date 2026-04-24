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
