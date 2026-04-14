---
purpose: "TECH-141 — Blip no-alloc regression test."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-141 — Blip no-alloc regression test

> **Issue:** [TECH-141](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

EditMode regression test asserting steady-state `BlipVoice.Render` calls produce zero managed allocations per call. Uses `GC.GetAllocatedBytesForCurrentThread` delta after warm-up loop. Satisfies Stage 1.4 Exit bullet 7 + locks in Step 1 zero-alloc invariant from `blip-master-plan.md` §126.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New test file `Assets/Tests/EditMode/Audio/BlipNoAllocTests.cs`.
2. Flow — warm-up loop (3 renders, discard any one-time JIT / init allocation) → measure `GC.GetAllocatedBytesForCurrentThread` delta across 10 steady-state renders → assert delta per call ≤ 0 bytes.
3. Tolerates NUnit infrastructure allocation outside the measured window.
4. Test green in Unity Test Runner.

### 2.2 Non-Goals (Out of Scope)

1. No managed-heap growth tracking across frame boundaries.
2. No native allocation tracking (out of scope for managed-alloc invariant).
3. No `BlipBaker` / `BlipCatalog` alloc coverage (Step 2 surfaces).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Regression guard against accidental heap allocation (boxing, closures, LINQ, array re-alloc) inside per-sample render loop. | Test fails if delta > 0 bytes/call in steady state. |

## 3. User / Developer Stories

## 4. Current State

### 4.1 Domain behavior

Stage 1.3 `BlipVoice.Render` written for zero managed alloc — `ref state` parameter, preallocated buffer, no LINQ / closures. No automated regression coverage.

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipVoice.cs` — render under test.
- `Assets/Tests/EditMode/Audio/` — TECH-137 fixtures (`RenderPatch`).
- .NET `System.GC.GetAllocatedBytesForCurrentThread` — measurement API.

## 5. Proposed Design

### 5.1 Target behavior (product)

Tooling-only.

### 5.2 Architecture / implementation

- Preallocate output `float[]` buffer once outside measured loop.
- Warm-up — call `BlipVoice.Render` 3× ignoring alloc delta (JIT compile, first-call lazy init).
- Measure — `long before = GC.GetAllocatedBytesForCurrentThread(); for (i < 10) Render(...); long delta = GC.GetAllocatedBytesForCurrentThread() - before;`.
- `Assert.That(delta, Is.LessThanOrEqualTo(0L))` — (`≤ 0` to tolerate GC reclaim within window; strict `== 0` flaky in JIT-instrumented Editor mode).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | `≤ 0` tolerance (not `== 0`) | Editor JIT emits inlining deltas that occasionally flip < 0; strict equality flaky | Strict `== 0`; rolling-average window |

## 7. Implementation Plan

### Phase 1 — Test

- [ ] Create `BlipNoAllocTests.cs` w/ one `[Test]` method.
- [ ] Verify pass locally — test runs last in suite to minimize cross-test GC noise.
- [ ] `npm run unity:compile-check` + `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Test compiles + passes | Unity EditMode | Unity Test Runner → EditMode | |
| Repo health | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] No-alloc test passes locally — delta ≤ 0 bytes/call post-warm-up.
- [ ] `unity:compile-check` + `validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.
