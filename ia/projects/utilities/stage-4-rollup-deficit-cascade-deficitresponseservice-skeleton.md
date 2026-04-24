### Stage 4 — Pool core + contributor registry / Rollup + deficit cascade + DeficitResponseService skeleton

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire cross-scale rollup (child surplus → parent, 1:1 lossless) and deficit cascade (parent Deficit → child `FreezeFlags.Expansion = true`). Land `DeficitResponseService` as event subscriber w/ `FreezeFlags.Expansion` flag — happiness / desirability coroutines land in Step 3, just the flag + event plumbing here. Integrate tick order into `SimulationManager` so pools update each sim tick.

**Exit:**

- `UtilityPoolService.RollupToParent()` — adds child `net` (clamped ≥ 0 — surplus only) to parent's synthetic producer list. Country-scale service has no rollup step.
- `UtilityPoolService.InheritDeficitFromParent(PoolStatus)` — when any parent pool `Deficit`, child raises `ForcedDeficit` flag (does not overwrite own status math; stacks w/ local status for freeze-flag purposes).
- `DeficitResponseService : MonoBehaviour` subscribes to all three `UtilityPoolService.PoolStatusChanged` + also listens to parent's forced-deficit broadcasts. Sets `public bool ExpansionFrozen { get; private set; }` when ANY scale reports Deficit OR ForcedDeficit for ANY kind.
- `FreezeFlags.Expansion` exposed as `DeficitResponseService.ExpansionFrozen` (no global singleton — consumers `FindObjectOfType` at Awake).
- `SimulationManager.Tick()` updated to call registry → `TickPools` in order City → Region → Country, then rollup Country ← Region ← City.
- EditMode tests: child surplus rolls to parent; country deficit freezes all children; recovery clears freeze.
- Phase 1 — Rollup + cascade math on `UtilityPoolService`.
- Phase 2 — `DeficitResponseService` flag + event subscriptions.
- Phase 3 — `SimulationManager` tick integration.
- Phase 4 — Integration EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Rollup math | _pending_ | _pending_ | Implement `UtilityPoolService.RollupToParent()` — for each kind, `parent.pools[kind].net += max(0, this.pools[kind].net)`. Document 0% loss invariant (grid loss deferred post-MVP). |
| T4.2 | Deficit cascade | _pending_ | _pending_ | Implement `InheritDeficitFromParent` — parent raises event `ForcedDeficitChanged(UtilityKind, bool)`, child service sets a per-kind `bool forcedDeficit` mask. |
| T4.3 | DeficitResponseService skeleton | _pending_ | _pending_ | Add `DeficitResponseService.cs` MonoBehaviour — subscribe to three pools' `PoolStatusChanged` + forced-deficit events in `OnEnable`; expose `public bool ExpansionFrozen`; unsubscribe in `OnDisable`. No coroutines yet. |
| T4.4 | SimulationManager tick wiring | _pending_ | _pending_ | Edit `SimulationManager.cs` tick loop — after demand compute, before CityStats read: registry views per scale → `TickPools(city)` → `TickPools(region)` → `TickPools(country)` → rollups bottom-up. Cache refs in `Awake` (invariant #3). |
| T4.5 | Integration EditMode tests | _pending_ | _pending_ | Add `UtilityRollupCascadeTests.cs` — surplus rolls 1:1; country deficit → all scales' `ExpansionFrozen = true`; recovery clears. Use test-scene fixture w/ three services + registry + response service. |
