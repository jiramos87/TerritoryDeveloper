---
purpose: "TECH-291 — Tick alloc audit + patch top-2 allocators."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-291 — Tick alloc audit + patch top-2 allocators

> **Issue:** [TECH-291](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Scan `SimulationManager` + tick-path managers for avoidable **GC alloc** (LINQ in hot path, boxing, list recreation per-tick, string concat). Patch top-2 allocators surfaced by TECH-290 profiler run. Annotate `SimulationManager.Update()` w/ budget note. Preserves invariants #3 (no `FindObjectOfType` per-frame) + #6 (helper extraction if touching `GridManager`).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Top-2 allocators from TECH-290 list patched (or documented acceptable w/ rationale).
2. `SimulationManager.Update()` carries inline budget note referencing `docs/city-tick-perf-notes.md`.
3. Re-profile shows measurable reduction OR no-regression w/ note.
4. No new singletons; no `GridManager` bloat; no invariant #3 violation.

### 2.2 Non-Goals

1. Patching all allocators — top-2 only per scope gate.
2. Rewriting tick architecture — targeted reductions.
3. New EditMode test — TECH-293 owns.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Stage 3 parity harness needs tick GC floor low enough one dormant + one active city credible | Top-2 allocators patched; re-profile documented |

## 4. Current State

### 4.1 Domain behavior

Stage 2.1 closed per-frame `FindObjectOfType` already removed (BUG-14). Remaining allocators unknown until TECH-290 baseline lands.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/SimulationManager.cs`
- Likely offenders (TBD per TECH-290): `DemandManager.cs`, `GrowthBudgetManager.cs`, `EmploymentManager.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`.
- Reference spec: `ia/specs/simulation-system.md` §tick-loop.
- Invariants: #3, #6.

## 5. Proposed Design

### 5.1 Target behavior (product)

Zero behavior change. GC floor reduced.

### 5.2 Architecture / implementation

1. Read TECH-290 perf notes — pick top-2 allocators.
2. Common patches: replace LINQ `.Where().ToList()` w/ preallocated `List<T>` cleared per tick; eliminate boxing on int/struct iterators; string interp → `StringBuilder` cached; `new List<T>()` per tick → field + `Clear()`.
3. Annotate `SimulationManager.Update()` w/ one-line budget comment pointing at perf notes.
4. Re-profile w/ same method as TECH-290; append "Patch round 1" section to perf notes.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Top-2 allocator slice | Budget gate — avoid rewriting whole tick | Full alloc sweep — post-MVP |

## 7. Implementation Plan

### Phase 1 — Patch top-2 allocators

- [ ] Read TECH-290 perf notes; select top-2 allocators.
- [ ] Patch per-site; preserve behavior.
- [ ] Annotate `SimulationManager.Update()`.
- [ ] Re-profile; append "Patch round 1" numbers to perf notes.
- [ ] `unity:compile-check` + testmode smoke (no regression).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile clean | Unity | `npm run unity:compile-check` | After C# edits |
| No tick regression | Testmode | `npm run unity:testmode-batch` | Smoke on New Game |
| IA consistency | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Top-2 allocators patched OR documented acceptable w/ rationale.
- [ ] `SimulationManager.Update()` budget annotation present.
- [ ] Perf notes updated w/ post-patch numbers.
- [ ] Invariant #3 + #6 preserved.
- [ ] `unity:compile-check` + `validate:all` clean.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

None — perf hygiene only; allocator list sourced from TECH-290.
