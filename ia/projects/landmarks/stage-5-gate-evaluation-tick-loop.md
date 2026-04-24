### Stage 5 — LandmarkProgressionService (unlock-only) / Gate evaluation + tick loop

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement the `Tick()` gate evaluator per `LandmarkPopGate` discriminator. Raise `LandmarkUnlocked` event on false→true transitions. Idempotent — once unlocked stays unlocked.

**Exit:**

- `Tick()` walks catalog rows; dispatches per-gate via pattern match on `ScaleTransitionGate` vs `IntraTierGate`.
- `ScaleTransitionGate` check — `scaleTier.CurrentTier` compares > `gate.fromTier` (Region > City, Country > Region).
- `IntraTierGate` check — `population.GetPopForCurrentScale() >= gate.pop`.
- On false→true transition, fires `event Action<string> LandmarkUnlocked`. Once true, the per-row evaluator skips re-evaluation (idempotent short-circuit).
- EditMode test — synthetic `ScaleTierController` fake + `PopulationAggregator` fake; drive tick stream across scale transition + pop crossing; assert unlock event order + idempotency.
- Phase 1 — Gate pattern-match evaluator.
- Phase 2 — Tick loop + event emission.
- Phase 3 — EditMode tests for unlock order.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Gate evaluator per discriminator | _pending_ | _pending_ | Add `private bool EvaluateGate(LandmarkPopGate gate)` — pattern match: `ScaleTransitionGate g` → `scaleTier.CurrentTier > g.fromTier`; `IntraTierGate g` → `population.GetPopForCurrentScale() >= g.pop`. XML doc both branches. |
| T5.2 | Tick loop + event | _pending_ | _pending_ | Implement `public void Tick()` — foreach catalog row, skip if `unlockedById[row.id] == true`; else `if (EvaluateGate(row.popGate)) { unlockedById[row.id] = true; LandmarkUnlocked?.Invoke(row.id); }`. Add `public event Action<string> LandmarkUnlocked`. |
| T5.3 | Unlock order EditMode tests | _pending_ | _pending_ | Add `LandmarkProgressionServiceTests.cs` — fake ScaleTierController + PopulationAggregator; drive ticks: pop rises below threshold (no event), pop crosses intra-tier threshold (1 event), scale transitions (tier-defining event), re-tick (no re-emit = idempotent). |
| T5.4 | Catalog re-entry guard test | _pending_ | _pending_ | Test case: call `Tick()` 100× after unlock; assert event fires exactly once per row. Guard against future edits that might accidentally re-evaluate. |
