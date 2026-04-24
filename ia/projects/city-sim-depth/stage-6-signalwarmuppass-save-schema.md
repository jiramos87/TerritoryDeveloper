### Stage 6 — Happiness Migration + Warmup / SignalWarmupPass + Save Schema

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `SignalWarmupPass` deterministic recompute on load; bump `GameSaveManager` schema version to persist `DistrictMap` + tuning weights; verify round-trip integrity.

**Exit:**

- `SignalWarmupPass.Run()` called by `GameSaveManager` after grid restore; signal fields stable and idempotent (two consecutive calls produce identical output).
- `DistrictMap` round-trips through save/load.
- Old saves without `DistrictMap` field auto-migrate (re-derive on first tick).
- EditMode integration test passes.
- Phase 1 — SignalWarmupPass implementation + idempotency test.
- Phase 2 — GameSaveManager schema bump + migration + integration test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | SignalWarmupPass MonoBehaviour | _pending_ | _pending_ | `SignalWarmupPass` MonoBehaviour — `Run()` iterates all `ISignalProducer` implementors (via `SignalTickScheduler` producer list), calls `EmitSignals` for each, then runs `DiffusionKernel.Apply` per signal; does NOT call consumers (no happiness/desirability update on load); safe to call from `GameSaveManager` before first normal tick. |
| T6.2 | SignalWarmupPass idempotency test | _pending_ | _pending_ | EditMode test — construct minimal scene (GridManager + SignalFieldRegistry + PollutionAir producer); call `SignalWarmupPass.Run()` twice; assert `SignalField[PollutionAir]` bit-identical after run-1 and run-2 (deterministic); assert no NaN in any signal field after warmup. |
| T6.3 | GameSaveManager schema version bump | _pending_ | _pending_ | Edit `GameSaveManager.cs` — add `districtMapData int[]` serialization field to save DTO (flatten `DistrictMap` 2D array); increment schema version int; `OnAfterLoad`: if field present, restore `DistrictMap` + call `SignalWarmupPass.Run()` after grid restore (per `ia/specs/persistence-system.md` load pipeline order); if absent (old save), set `districtMap.needsRederive = true`. |
| T6.4 | Save/load integration test | _pending_ | _pending_ | EditMode integration test — create city with 2 I-heavy cells + 1 forest cell; save via `GameSaveManager`; reload; assert `DistrictMap` restored (district ids match pre-save); assert `SignalField[PollutionAir]` non-zero after warmup; assert `DistrictSignalCache.Get(0, PollutionAir)` > 0 after first post-load tick. |

---
