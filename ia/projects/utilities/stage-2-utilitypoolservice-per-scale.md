### Stage 2 — Pool core + contributor registry / UtilityPoolService (per-scale)

**Objectives:** Implement the per-scale service: tick `ComputeNet()`, `UpdateEma()` (5-tick window), threshold state machine w/ hysteresis, `PoolStatusChanged` event. Parent pointer for rollup; does NOT yet consume contributors (Step 2 wires real producers) — use an internal `Sum(IEnumerable<IUtilityContributor>)` + `Sum(IEnumerable<IUtilityConsumer>)` so tests inject fakes.

**Status:** Draft (tasks _pending_ — not yet filed)

**Exit:**

- `UtilityPoolService : MonoBehaviour` w/ `[SerializeField] private ScaleTag scale`, `[SerializeField] private UtilityPoolService parent` (nullable — Country has none), `Dictionary<UtilityKind, PoolState> pools`.
- `OnEnable` / `Awake` seeds three pools (Water / Power / Sewage) to `Healthy` defaults; no singletons, `FindObjectOfType` fallback pattern per invariant #4.
- `TickPools(IReadOnlyList<IUtilityContributor> prods, IReadOnlyList<IUtilityConsumer> cons)` — testable entry point.
- Threshold rule per Implementation Points §1: `Warning` on EMA < 0 for ≥3 consecutive ticks; `Deficit` on `net ≤ 0 AND ema ≤ -0.2 × max(prodSum, consSum)`. Exit Deficit only when EMA > 0 for ≥3 consecutive ticks.
- `event Action<UtilityKind, PoolStatus, PoolStatus> PoolStatusChanged` fires on transition (kind, from, to).
- EditMode tests: Healthy→Warning→Deficit→Warning→Healthy round trip w/ synthetic tick stream.
- Phase 1 — Service scaffolding + pool initialization.
- Phase 2 — EMA + threshold state machine.
- Phase 3 — EditMode tests for transitions.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Service scaffold + seed | _pending_ | _pending_ | Add `UtilityPoolService.cs` as MonoBehaviour; `[SerializeField] private ScaleTag scale`, `[SerializeField] private UtilityPoolService parent`; `Awake` initializes `Dictionary<UtilityKind, PoolState>` w/ all three kinds in `Healthy`. |
| T2.2 | TickPools entry point | _pending_ | _pending_ | Implement `TickPools(prods, cons)` summing by kind; updates `net` on each pool. No EMA / status transition yet — that's T1.2.3. |
| T2.3 | EMA + threshold state machine | _pending_ | _pending_ | Implement 5-tick EMA (`α = 2/(5+1) ≈ 0.333`); apply state-machine rule per Implementation Points §1 w/ hysteresis counters. Fire `PoolStatusChanged` on transition. |
| T2.4 | EditMode transition tests | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Utilities/UtilityPoolServiceTests.cs` — drive synthetic contributor/consumer lists across ≥20 ticks, assert Healthy→Warning→Deficit→Warning→Healthy with correct hysteresis gate counts. |
