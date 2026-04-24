### Stage 6 — LandmarkProgressionService (unlock-only) / Tick ordering + bootstrap integration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Register `LandmarkProgressionService.Tick` into `GameManager` / `SimulationManager` tick bus AFTER `ScaleTierController.Tick`. Per Review Notes NON-BLOCKING item — tier-defining unlock fires one tick late if ordering wrong.

**Exit:**

- `GameManager` (or `SimulationManager.Tick` if that's the canonical bus) calls `scaleTier.Tick()` → `population.Tick()` → `landmarkProgression.Tick()` in that order. Code comment at touch site cites Review Notes.
- `LandmarkProgressionService` ref cached in `Awake` per invariant #3.
- Integration EditMode test — run one tick where scale transition + intra-tier threshold cross happen simultaneously; assert tier-defining fires THIS tick (not next).
- Phase 1 — Bootstrap tick registration + integration tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Bootstrap tick wiring | _pending_ | _pending_ | Edit `GameManager.cs` (or `SimulationManager.cs` — check canonical tick bus at stage-file time). Cache `LandmarkProgressionService` ref in `Awake` (invariant #3); invoke `Tick()` AFTER `ScaleTierController.Tick()`. Code comment cites Review Notes NON-BLOCKING. |
| T6.2 | Same-tick ordering integration test | _pending_ | _pending_ | Add integration test fixture w/ real `GameManager` bootstrap; drive tick where scale crosses AND intra-tier pop crosses same tick; assert both events fire same tick in scale-first order (tier-defining before intra-tier). |
| T6.3 | Boot-null fallback test | _pending_ | _pending_ | Add test — bootstrap scene missing a service ref (scaleTier null); assert `LandmarkProgressionService.Awake` logs error + subsequent `Tick()` short-circuits (no NPE). Guard against boot-order drift. |
