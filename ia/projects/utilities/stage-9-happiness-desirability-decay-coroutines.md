### Stage 9 — Deficit response + UI dashboard / Happiness + desirability decay coroutines

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement the two decay effects under `DeficitResponseService`. Happiness penalty accumulator + desirability decay through new `GeographyManager` helper (invariant #6 extraction).

**Exit:**

- `DeficitResponseService.HappinessPenalty` (int, -20..0) accumulates −1/game-day while `AnyDeficit`; resets to 0 when all pools recover.
- `GeographyManager.ApplyGlobalDesirabilityDelta(float mult)` — new public method, loops `grid.cellArray`, `cell.desirability = max(0, cell.desirability * mult)`. XML doc cites invariant #5 carve-out rationale.
- `DeficitResponseService` calls `ApplyGlobalDesirabilityDelta(0.98f)` per game-day while Deficit.
- EditMode tests: penalty arithmetic (floor -20, rises at -1/day); desirability floor 0; no decay when pools healthy.
- Phase 1 — Happiness penalty accumulator.
- Phase 2 — `GeographyManager.ApplyGlobalDesirabilityDelta` helper.
- Phase 3 — Decay EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | HappinessPenalty accumulator | _pending_ | _pending_ | Add `HappinessPenalty` field + `OnGameDay` handler on `DeficitResponseService` — `-=1` while `AnyDeficit`, clamp `[-20, 0]`; reset to 0 when all scales Healthy. |
| T9.2 | AnyDeficit helper | _pending_ | _pending_ | Add `DeficitResponseService.AnyDeficit` property — true when any tracked `(scale, kind)` pool has `status == Deficit` OR `forcedDeficit == true`. |
| T9.3 | GeographyManager desirability helper | _pending_ | _pending_ | Edit `GeographyManager.cs` — add `public void ApplyGlobalDesirabilityDelta(float multiplier)`. Loop `grid.cellArray`, `cell.desirability = Mathf.Max(0f, cell.desirability * multiplier)`. XML doc invariant #5 carve-out. |
| T9.4 | Desirability decay hook | _pending_ | _pending_ | `DeficitResponseService.OnGameDay` — while `AnyDeficit`, call `geography.ApplyGlobalDesirabilityDelta(0.98f)`. Cache `GeographyManager` ref in `Awake` (invariant #3). |
| T9.5 | Decay EditMode tests | _pending_ | _pending_ | Add `DeficitResponseTests.cs` — penalty arithmetic per day w/ deficit on/off transitions; desirability floor 0; decay skipped when healthy. |
