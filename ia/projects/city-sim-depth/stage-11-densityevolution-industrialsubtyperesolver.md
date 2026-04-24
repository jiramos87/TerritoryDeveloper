### Stage 11 — Construction + Density + Industrial / DensityEvolution + IndustrialSubtypeResolver

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Fill in `DensityEvolutionTicker` shell (from Stage 2.2) — refactor to consume `DesirabilityComposer` with upgrade/downgrade hysteresis; ship `IndustrialSubtypeResolver` (4 sub-types wired into pollution + tax + desirability weights).

**Exit:**

- High-desirability cells (> 0.7) upgrade density tier within 1 tick.
- Low-desirability cells (< 0.3) downgrade after ≥60-day hold; hysteresis `[0.3, 0.7]` band prevents oscillation.
- I-zone cells resolved to one of 4 sub-types; sub-type persisted to `CellData.industrialSubtype`.
- Manufacturing cell produces higher `PollutionAir` than tech cell at same density.
- `npm run validate:all` passes.
- Phase 1 — DensityEvolutionTicker desirability refactor + decay/hysteresis.
- Phase 2 — IndustrialSubtypeResolver + weight wiring + test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | DensityEvolutionTicker desirability refactor | _pending_ | _pending_ | Fill `DensityEvolutionTicker.cs` shell — monthly tick; replace old growth heuristic with `desirabilityComposer.CellValue(x,y)` consumption; upgrade threshold `[SerializeField] float upgradeAt = 0.7f`; upgrade fires immediately when condition met. Fill `SetDesirabilitySource` stub from Stage 2.2; `[SerializeField] DesirabilityComposer desirabilityComposer` + `FindObjectOfType`. |
| T11.2 | Density decay + hysteresis | _pending_ | _pending_ | Add downgrade path — cells at desirability < `[SerializeField] float downgradeAt = 0.3f` for ≥ `[SerializeField] int decayWaitDays = 60` consecutive days downgrade one density tier; per-cell `int daysBelow` counter reset when desirability rises above `downgradeAt`; hysteresis band `[downgradeAt, upgradeAt]` prevents oscillation; both thresholds tunable Inspector. |
| T11.3 | IndustrialSubtypeResolver MonoBehaviour | _pending_ | _pending_ | `IndustrialSubtypeResolver` MonoBehaviour — on I-zone cell placement (or monthly re-eval for unresolved cells): score 4 sub-types (agriculture=farm adjacency bonus, manufacturing=high road density, tech=high desirability + low pollution, tourism=high LandValue + low crime); assign highest-scoring sub-type; persist to `CellData.industrialSubtype` field; `[SerializeField] DesirabilityComposer` + `FindObjectOfType`. |
| T11.4 | Sub-type weight wiring + test | _pending_ | _pending_ | Edit `ZoneManager` industrial `ISignalProducer.EmitSignals` — look up `cell.industrialSubtype` and apply per-sub-type `PollutionAir` weight (manufacturing=2.5f, agriculture=1.0f, tech=0.5f, tourism=0f); apply per-sub-type `LandValue` bonus weight similarly. EditMode test: manufacturing cell → higher PollutionAir than tech cell at same zone level. |

---
