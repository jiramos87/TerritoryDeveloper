### Stage 10 — Deficit response + UI dashboard / CityStats + DemandManager readers

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** `CityStats.HappinessTarget` subtracts `HappinessPenalty`. `DemandManager` floors RCI demand when matching utility Deficit (e.g. Industrial floor while Power Deficit). Complete Subsystem Impact wiring.

**Exit:**

- `CityStats.ComputeHappinessTarget()` subtracts `deficitResponse.HappinessPenalty` before existing lerp.
- `DemandManager.GetDemand(RCI kind)` applies a multiplier floor (e.g. `0.3f`) when the mapping table (`RCI → UtilityKind`) reports Deficit: R → Water, C → Power, I → Power + Sewage (table documented in method XML).
- EditMode tests: penalty subtracted from happiness target; demand floor applied when matching Deficit raised.
- Phase 1 — CityStats wiring.
- Phase 2 — DemandManager floor + mapping.
- Phase 3 — Reader EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | CityStats happiness subtract | _pending_ | _pending_ | Edit `CityStats.cs` — `ComputeHappinessTarget` fetches `deficitResponse.HappinessPenalty` (cached ref, invariant #3) and subtracts before existing lerp. |
| T10.2 | RCI→UtilityKind mapping | _pending_ | _pending_ | Add `DemandManager.RciUtilityDependency` static readonly table: R→{Water}, C→{Power}, I→{Power, Sewage}. |
| T10.3 | DemandManager demand floor | _pending_ | _pending_ | `DemandManager.GetDemand(rci)` multiplies by `0.3f` if any mapped utility reports Deficit at city scale. Reads `UtilityPoolService.pools[kind].status`. |
| T10.4 | Reader EditMode tests | _pending_ | _pending_ | Add reader tests — happiness target subtracts penalty correctly; demand floored when Power Deficit (I), Water Deficit (R), etc. |
