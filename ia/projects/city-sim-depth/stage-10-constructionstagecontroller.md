### Stage 10 — Construction + Density + Industrial / ConstructionStageController

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Fill in `ConstructionStageController` shell (from Stage 2.2) — stage machine with ScriptableObject curve tables and desirability-modulated construction time + `ZoneManager` placement hook.

**Exit:**

- New zone cell enters construction stage 0 on placement; advances to final stage after `effectiveTime` in-game days.
- `effectiveTime = baseTime / (0.5f + Mathf.Clamp01(desirability))` (Example 3 verified ±1 day).
- Sprite swap fires on stage boundary; placeholder sprite if art absent.
- EditMode test: R-medium at desirability=0.6 completes in 27.3±1 days; edge cases verified.
- Phase 1 — Stage machine + ScriptableObject curve tables + time formula.
- Phase 2 — ZoneManager placement hook + EditMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | ConstructionStageController stage machine | _pending_ | _pending_ | Fill `ConstructionStageController.cs` shell — per-cell `ConstructionState { int stageIndex; float daysInCurrentStage; }` dictionary keyed by cell coords; `ConstructionCurveTable` ScriptableObject (zone type × density → `int stageCount`, `float baseDays`, `Sprite[] stageSprites`); `Tick(CityCell cell, float desirability)` advances `daysInCurrentStage`; fires `OnStageComplete(cell, stageIndex)` event on stage boundary. |
| T10.2 | Construction time formula + edge guards | _pending_ | _pending_ | Implement `effectiveTime = baseDays / (0.5f + Mathf.Clamp01(desirability))` with guards: desirability clamp at `[0,1]` at composer boundary (Stage 2.2) ensures no negative input; per-stage time = effectiveTime / stageCount. Validate Example 3: R-medium baseDays=30, stages=4, d=0.6 → total≈27.3, per-stage≈6.8. Add `[SerializeField] DesirabilityComposer desirabilityComposer` in `ConstructionStageController`; fill in `SetDesirabilitySource` stub. |
| T10.3 | ZoneManager placement hook + sprite swap | _pending_ | _pending_ | Edit `ZoneManager` — on new zone cell placement, register cell with `ConstructionStageController.Register(cell)`; subscribe to `OnStageComplete`: swap cell's renderer sprite to `ConstructionCurveTable.stageSprites[stageIndex]` (placeholder `Sprite` if art asset absent); final stage complete → swap to full building sprite + mark cell `isFullyBuilt = true`. |
| T10.4 | ConstructionStage EditMode test | _pending_ | _pending_ | EditMode test — R-medium cell, desirability=0.6; advance `ConstructionStageController` tick loop 30 times (1 day per call); assert cell reaches final stage by day 28 (27.3±1). Boundary: d=0 → 60±1 days; d=1 → 20±1 days. Assert no divide-by-zero at d=0 (denominator = 0.5 + 0 = 0.5f). |

---
