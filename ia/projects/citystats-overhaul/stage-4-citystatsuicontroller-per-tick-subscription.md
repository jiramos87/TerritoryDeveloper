### Stage 4 — Consumer Migration / CityStatsUIController per-tick subscription

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Drop per-frame `Update` stat polling; subscribe to `_facade.OnTickEnd`; handle initial paint and paused-state edge case.

**Exit:**

- `CityStatsUIController.Update` no longer calls `UpdateStatisticsDisplay`.
- `OnEnable` subscribes to `_facade.OnTickEnd`; `OnDisable` unsubscribes.
- Labels populated on first `OnEnable` regardless of pause state.
- `UpdateStatisticsDisplay` reads via `_facade.GetScalar(StatKey.X)` not direct `cityStats.*` fields.
- Invariant #3: `_facade` cached via Inspector wire in `Awake`; no `FindObjectOfType` in hot path.
- Phase 1 — Subscribe to OnTickEnd + initial paint fix.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T4.1 | _pending_ | _pending_ | In `CityStatsUIController.cs`: add `[SerializeField] private CityStatsFacade _facade`; in `OnEnable` subscribe `_facade.OnTickEnd += OnFacadeEndTick`; in `OnDisable` unsubscribe; add `void OnFacadeEndTick() => UpdateStatisticsDisplay()`; remove `UpdateStatisticsDisplay()` call from `Update()` (`:58`). Wire via Inspector (invariant #4, not `FindObjectOfType`). |
| T4.2 | _pending_ | _pending_ | Handle initial paint: at end of `OnEnable`, after subscribing, call `UpdateStatisticsDisplay()` once (covers `simulateGrowth == false` / paused edge case — no `EndTick` fires until unpaused). In `UpdateStatisticsDisplay` (`:176`), replace direct `cityStats.*` reads with `_facade.GetScalar(StatKey.Population)`, `GetScalar(StatKey.Money)`, `GetScalar(StatKey.Happiness)`, `GetScalar(StatKey.Unemployment)` etc. Remove `cityStats` field ref from this controller. |

---
