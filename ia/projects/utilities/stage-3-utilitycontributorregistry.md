### Stage 3 — Pool core + contributor registry / UtilityContributorRegistry

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Central registration surface for producers / consumers. Separates bookkeeping from pool math. Exposes `RegisterWithMultiplier` for landmarks hook.

**Exit:**

- `UtilityContributorRegistry : MonoBehaviour` w/ `[SerializeField] private UtilityPoolService cityPool / regionPool / countryPool` and internal `List<(IUtilityContributor, float mult)>` / `List<IUtilityConsumer>` keyed by `ScaleTag`.
- API: `Register(IUtilityContributor)`, `RegisterWithMultiplier(IUtilityContributor, float)`, `Deregister(IUtilityContributor)`, `RegisterConsumer(IUtilityConsumer)`, `DeregisterConsumer(IUtilityConsumer)`.
- `GetContributors(ScaleTag)` / `GetConsumers(ScaleTag)` — read-only views the `SimulationManager` hands to `UtilityPoolService.TickPools`.
- EditMode tests: round-trip register / deregister; multiplier applied to `ProductionRate` in the view; scale filtering correct.
- Phase 1 — Registry data structures + register/deregister API.
- Phase 2 — Scale-filtered view helpers + multiplier application.
- Phase 3 — EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Registry MonoBehaviour scaffold | _pending_ | _pending_ | Add `UtilityContributorRegistry.cs` — `[SerializeField]` slots for the three `UtilityPoolService` refs, internal `Dictionary<ScaleTag, List<(IUtilityContributor, float)>>` + consumer list. `Awake` with `FindObjectOfType` fallbacks. |
| T3.2 | Register / deregister API | _pending_ | _pending_ | Implement `Register` / `Deregister` / `RegisterConsumer` / `DeregisterConsumer`. Guard against duplicate add; log warn on missing remove. |
| T3.3 | RegisterWithMultiplier + view helpers | _pending_ | _pending_ | Implement `RegisterWithMultiplier(IUtilityContributor, float)`; add `GetContributors(ScaleTag)` returning a wrapped `IUtilityContributor` whose `ProductionRate` = raw × multiplier. Landmarks consume this in Step 4. |
| T3.4 | Registry EditMode tests | _pending_ | _pending_ | Add `UtilityContributorRegistryTests.cs` — register / deregister round trip; multiplier applied; scale filtering; duplicate-add guard. |
