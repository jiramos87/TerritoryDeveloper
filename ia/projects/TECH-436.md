---
purpose: "TECH-436 — Biquad kernel in Render + NoAlloc BP test (Stage 5.4 Phase 2)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/blip-master-plan.md"
task_key: "T5.4.3"
---
# TECH-436 — Biquad kernel in `Render` + NoAlloc BP test (Stage 5.4 Phase 2)

> **Issue:** [TECH-436](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Wire per-sample DF-II transposed BP dispatch in `BlipVoice.Render` using pre-computed coefficients from TECH-435. Add `BlipNoAllocTests.Render_WithBiquadBP_ZeroManagedAlloc` (3 warm-up + 10 measured renders; `GC.GetAllocatedBytesForCurrentThread` delta/call ≤ 0). LP + None branches unchanged.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Per-sample DF-II transposed BP kernel: `float v = x - a1n * state.biquadZ1 - a2n * state.biquadZ2; float y = b0n * v - b0n * state.biquadZ2; state.biquadZ2 = state.biquadZ1; state.biquadZ1 = v; sample = y` (b1n = 0 for bandpass).
2. LP + None branches unchanged.
3. `Render_WithBiquadBP_ZeroManagedAlloc` test: BP patch (`cutoffHz = 1000`, `Q = 2`, deterministic seed), delta/call ≤ 0.

### 2.2 Non-Goals

- Coefficient pre-compute (TECH-435).
- Data model (TECH-434).
- Golden-fixture regen (TECH-437).

## 3. Acceptance Criteria

- Per-sample DF-II transposed BP kernel present.
- LP + None branches unchanged.
- `Render_WithBiquadBP_ZeroManagedAlloc` green (delta/call ≤ 0).
- Existing NoAlloc + golden-fixture tests still green.
- `npm run unity:compile-check` + `npm run validate:all` exit 0.

## 4. Dependencies

- TECH-434 — `resonanceQ` + `biquadZ1/Z2` fields.
- TECH-435 — pre-computed `a1n/a2n/b0n` coefficients.

## 5. Files

- `Assets/Scripts/Audio/Blip/BlipVoice.cs`
- `Assets/Tests/EditMode/Audio/BlipNoAllocTests.cs`

## 6. Implementation Plan

### Phase 1 — Per-sample kernel

- [ ] Add BP dispatch branch inside sample loop in `BlipVoice.Render` using pre-computed `a1n/a2n/b0n`.
- [ ] Read/write `state.biquadZ1/biquadZ2` each sample.
- [ ] LP + None branches unmodified.

### Phase 2 — NoAlloc BP test

- [ ] Add `Render_WithBiquadBP_ZeroManagedAlloc` in `BlipNoAllocTests.cs`.
- [ ] BP patch: `cutoffHz = 1000`, `Q = 2`, deterministic seed.
- [ ] 3 warm-up + 10 measured; assert `GC.GetAllocatedBytesForCurrentThread` delta/call ≤ 0.

## 7. Lessons Learned

_Populate on closeout._
