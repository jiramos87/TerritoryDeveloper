---
purpose: "TECH-304 — Add ColumnarStatsStore ring-buffer store keyed by StatKey."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-304 — Add `ColumnarStatsStore` ring-buffer store keyed by `StatKey`

> **Issue:** [TECH-304](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add plain C# ring-buffer store `ColumnarStatsStore` — parallel `float[]` buffers keyed by `StatKey`, owns per-tick flush semantics. Second task of citystats-overhaul Stage 1.1 — depends on `StatKey` from TECH-303. Satisfies Stage 1.1 Exit criterion 3; composed into `CityStatsFacade` (Stage 1.2) + `RegionStatsFacade` / `CountryStatsFacade` (Stage 3.1).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `Assets/Scripts/Managers/GameManagers/ColumnarStatsStore.cs` compiles as plain C# class — no `MonoBehaviour`, no Unity deps (NUnit-testable).
2. Parallel `float[]` ring buffers keyed by `StatKey`; `int RingCapacity` default 256.
3. `Publish(StatKey, float delta)` accumulates running value across a tick.
4. `Set(StatKey, float value)` overwrites running value.
5. `FlushToSeries()` writes current running value into next ring slot + resets accumulator.
6. `GetScalar(StatKey) → float` returns current running value.
7. `GetSeries(StatKey, int windowTicks) → float[]` returns last N ring entries (newest-last, size min(windowTicks, filledCount)).

### 2.2 Non-Goals

1. No `MonoBehaviour` wrap — `CityStatsFacade` owns that (Stage 1.2).
2. No `EnumerateRows` dimension data — facade-level concern.
3. No save serialization — Stage 1.3 `SnapshotForBridge` handles scalar-only export.
4. No per-cell drill-down storage — deferred per master plan.
5. No dormant-scale capacity variant here — Stage 3.1 wires `RingCapacity = 64` on region/country facades via constructor arg.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As `CityStatsFacade` author (Stage 1.2), want `ColumnarStatsStore` composable via `new ColumnarStatsStore(capacity: 256)` so `BeginTick` / `Publish` / `EndTick` wrap it cleanly. | Store constructor accepts capacity; `Publish` / `Set` / `FlushToSeries` / `GetScalar` / `GetSeries` match Stage 1.1 Exit signatures. |

## 4. Current State

### 4.1 Domain behavior

No columnar store. `StatisticsManager.StatisticTrend` holds per-field ring buffers via reflection / ad-hoc field mirroring — deprecated in Stage 2.3.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/StatKey.cs` — TECH-303 dep.
- `Assets/Scripts/Managers/UnitManagers/IStatsReadModel.cs` — TECH-303 dep (signature compatibility for facade composition).
- Spec refs: `docs/citystats-overhaul-exploration.md §Design Expansion §Architecture`.

### 4.3 Implementation investigation notes

- Storage shape: `float[keyCount, ringCapacity]` 2D array OR `Dictionary<StatKey, float[]>`. 2D array — better cache, fewer allocs; dictionary — sparser if many unused keys. Pick 2D; `StatKey` enum count bounded and small.
- Running accumulator: `float[]` sized `keyCount`; reset in `FlushToSeries`.
- Write head: `int _writeIdx` mod `RingCapacity`; `int _filledCount` saturates at capacity.
- `GetSeries` copies window into a fresh `float[]` (alloc) — acceptable, not on hot per-frame path (called on UI tick end).

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible behavior. Ring buffer semantics defined fully by store — facade / consumers see typed pull API only.

### 5.2 Architecture / implementation

- Constructor: `ColumnarStatsStore(int ringCapacity = 256)`.
- Internal: `float[,] _ring; float[] _running; int _writeIdx; int _filledCount; int _keyCount = Enum.GetValues(typeof(StatKey)).Length;`.
- `Publish` → `_running[(int)key] += delta`.
- `Set` → `_running[(int)key] = value`.
- `FlushToSeries` → copy `_running` column into `_ring[*, _writeIdx]`; advance `_writeIdx` mod capacity; bump `_filledCount` until saturated. Accumulator reset policy: `Publish` semantics need accumulator reset post-flush (running = 0); `Set` semantics assume last-write-wins across ticks — split: only keys touched by `Publish` this tick reset, OR simpler — treat running as scalar state that persists across ticks and `Publish` / `Set` both update it; `FlushToSeries` snapshots the current scalar. **Implementer decision** — default to latter (scalar persists, snapshot per flush) unless Stage 1.3 EditMode test reveals mismatch w/ legacy `CityStats` semantics.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Plain C# (no MonoBehaviour) | NUnit-testable, facade composes via field; invariant #4 respected | MonoBehaviour — rejected, no scene lifecycle needed at store level |
| 2026-04-17 | 2D `float[,]` backing | Cache locality; small bounded key count | `Dictionary<StatKey, float[]>` — rejected, extra alloc + lookup cost |

## 7. Implementation Plan

### Phase 1 — Author store + smoke test

- [ ] Write `ColumnarStatsStore.cs` w/ constructor + five methods.
- [ ] Basic NUnit test (can live in Stage 1.3 test suite — `CityStatsFacadeShimTest` per master plan T1.3.4): `Publish` then `FlushToSeries` → `GetScalar` returns published value; `GetSeries(k, 1)[0]` equals scalar.
- [ ] `npm run unity:compile-check`.
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Store compiles + ring semantics correct | Agent report | `npm run unity:compile-check` | NUnit test lives in Stage 1.3 per master plan T1.3.4 |
| IA alignment | Node | `npm run validate:all` | Frontmatter + dead-spec check |

## 8. Acceptance Criteria

- [ ] `ColumnarStatsStore.cs` compiles as plain C# (no Unity deps).
- [ ] Constructor `ColumnarStatsStore(int ringCapacity = 256)`.
- [ ] `Publish` / `Set` / `FlushToSeries` / `GetScalar` / `GetSeries` signatures match Stage 1.1 Exit.
- [ ] `npm run unity:compile-check` clean.
- [ ] `npm run validate:all` clean.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. Does 256-tick capacity hold at max map size w/ expected consumer windows? Master plan Orchestration guardrails flag "before Stage 1.1 capacity constant lock: confirm 256-tick ring buffer acceptable at max map size." Confirm before TECH-304 implement flips to In Progress — if 256 insufficient, bump default + re-sync `ColumnarStatsStore` glossary row (Stage 3.3 T3.3.2).
2. `Publish` accumulator reset semantics on `FlushToSeries` — persist running scalar across ticks (default) vs reset to 0 (delta-per-tick). Current plan: persist — snapshot per flush. Confirm against legacy `CityStats` field semantics at Stage 1.3 EditMode test; flip policy if mismatch found.
