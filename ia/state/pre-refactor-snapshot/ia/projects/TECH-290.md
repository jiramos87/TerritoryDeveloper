---
purpose: "TECH-290 — Tick profiler baseline."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-290 — Tick profiler baseline

> **Issue:** [TECH-290](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Unity Profiler run on `SimulationManager` **tick** path post Stage 2.1 fixes. Document top-5 hotspots + **GC alloc** per tick + baseline **ms/tick** in new `docs/city-tick-perf-notes.md`. Feeds Step 3 **parity budget** harness. Feeds TECH-291 allocator patch list + TECH-293 EditMode budget threshold.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Profiler capture on city tick post Stage 2.1 — crasher fixes settled, cache-in-Awake applied.
2. Top-5 hotspots surfaced w/ sampled ms.
3. GC alloc per tick recorded (bytes/tick).
4. Baseline ms/tick number captured (median of sampled ticks).
5. Results written to new `docs/city-tick-perf-notes.md` (stable location for TECH-291/293 to reference).

### 2.2 Non-Goals (Out of Scope)

1. Patching allocators — TECH-291 owns that slice.
2. EditMode test implementation — TECH-293.
3. Profiler automation / CI capture — manual dev run suffices for baseline.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Need baseline ms/tick + hotspot list to scope Step 3 parity harness | `docs/city-tick-perf-notes.md` exists w/ numbers |

## 4. Current State

### 4.1 Domain behavior

Stage 2.1 closed — BUG-55 / BUG-14 / BUG-16 / BUG-17 all archived. **SimulationManager** tick path stable but un-profiled post-fix. No baseline captured.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/SimulationManager.cs` — tick owner (`Update()` dispatch).
- Tick-path managers: `DemandManager`, `GrowthBudgetManager`, `EmploymentManager`, `AutoZoningManager`, `AutoRoadBuilder`.
- Reference spec: `ia/specs/simulation-system.md` §tick-loop (MCP `spec_section sim tick`).
- Invariants: #3 (no `FindObjectOfType` per-frame — already enforced Stage 2.1).

## 5. Proposed Design

### 5.1 Target behavior (product)

No runtime behavior change. Observability-only.

### 5.2 Architecture / implementation

1. Open Unity Profiler (Deep Profile OFF for alloc numbers; ON for hotspot detail).
2. New Game → run ≥60 ticks → capture frame samples from tick path.
3. Record top-5 self-ms functions + total GC alloc per tick + median ms/tick.
4. Author `docs/city-tick-perf-notes.md` w/ sections: Baseline (date, commit, ms/tick, GC/tick), Hotspots (table), Method (profiler settings), Next steps (TECH-291 allocator patch targets).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Manual dev-machine profiler run, not CI | Baseline is one-shot; CI perf harness lands Step 3 | Scripted profiler API — deferred |

## 7. Implementation Plan

### Phase 1 — Capture + document

- [ ] Profiler run on New Game, ≥60 ticks, record samples.
- [ ] Create `docs/city-tick-perf-notes.md` w/ Baseline + Hotspots + Method + Next steps sections.
- [ ] Commit perf notes; link from Stage 2.2 task entry in master plan.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Perf notes doc lands | File exists | `docs/city-tick-perf-notes.md` | Manual review |
| IA doc links resolve | Node | `npm run validate:all` | validate:dead-project-specs |

## 8. Acceptance Criteria

- [ ] `docs/city-tick-perf-notes.md` exists w/ top-5 hotspots, GC alloc/tick, baseline ms/tick.
- [ ] Profiler method section records Unity version + Deep Profile settings.
- [ ] TECH-291 + TECH-293 can reference baseline number directly.
- [ ] `npm run validate:all` clean.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
