### Stage 4 — Happiness Migration + Warmup / HappinessComposer Migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Replace `CityStats` inline happiness/pollution scalar with `HappinessComposer`; register existing industrial buildings, power plants, forests as `PollutionAir` producers/sinks; verify output parity.

**Exit:**

- `HappinessComposer.Current` within 5 points of pre-migration scalar on golden fixture.
- `CityStats.happiness` getter delegates to `HappinessComposer.Current` — no external API break.
- Industrial buildings + `PowerPlant` registered as `PollutionAir` producers; `ForestManager` registered as `PollutionAir` sink.
- EditMode parity test passes; no NaN / Infinity from composer.
- Phase 1 — HappinessComposer MonoBehaviour + producer/sink registration.
- Phase 2 — CityStats migration wiring + parity test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | HappinessComposer MonoBehaviour | _pending_ | _pending_ | `HappinessComposer` MonoBehaviour — reads `SignalFieldRegistry` for all 12 signals; weighted sum via `[SerializeField] float[] signalWeights` array (Inspector-tunable, indexed by `SimulationSignal` ordinal); `Current` property returns normalized 0–100 score; initial weights matched to current `CityStats` scalar formula (pollution penalty, forest bonus, service bonus); `[SerializeField] SignalFieldRegistry` + `FindObjectOfType` fallback. |
| T4.2 | Producer/sink registration | _pending_ | _pending_ | Implement `ISignalProducer` on `PowerPlant.cs` (`PollutionAir`: nuclear=medium weight 1.5f, fossil=high weight 3.0f per glossary), on `ZoneManager` industrial building emit (heavy=2.5f, medium=1.5f, light=0.8f), and on `ForestManager` (negative PollutionAir emission per forest cell: sparse=−0.3f, medium=−0.5f, dense=−0.8f). `EmitSignals` calls `registry.GetField(PollutionAir).Add(x,y,weight)`. |
| T4.3 | CityStats happiness migration | _pending_ | _pending_ | Edit `CityStats.cs` — replace inline scalar happiness compute (lines ~715–856) with `happinessComposer.Current` call; add `[SerializeField] HappinessComposer happinessComposer` + `FindObjectOfType` fallback; preserve `happiness` getter signature exactly; preserve `RefreshHappinessAfterPolicyChange()` public API; inject `HappinessComposer` into `DemandManager` via Inspector. |
| T4.4 | Happiness parity EditMode test | _pending_ | _pending_ | EditMode test — construct minimal CityStats + producers state matching a known-good scenario; run 1 tick old scalar path to capture baseline happiness; enable `HappinessComposer` path (set weights to match); run same tick; assert delta < 5 on [0,100] scale; assert no NaN / Infinity. |

---
