---
purpose: "TECH-121 — BlipVoice.Render driver (per-sample loop integrator)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-121 — BlipVoice.Render driver (per-sample loop integrator)

> **Issue:** [TECH-121](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land `BlipVoice.Render` static method — per-sample driver loop integrating oscillator bank + envelope + filter. Stage 1.3 Phase 3 central task. Signature — `Render(Span<float> buffer, int offset, int count, int sampleRate, in BlipPatchFlat patch, int variantIndex, ref BlipVoiceState state)`. Zero managed allocs inside; no Unity API; single static method (shared kernel for `BlipBaker` Step 2 + `BlipLiveHost` post-MVP).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Signature matches Stage 1.3 Exit — `Span<float>` buffer, `in BlipPatchFlat`, `ref BlipVoiceState`.
2. Per-sample loop — osc sum × envelope × filter → buffer[offset + i].
3. Pre-computes per-invocation scalars (α filter coeff, freq increments, envelope stage sample budgets) outside loop.
4. Advances envelope stage machine (TECH-118) + level (TECH-119) per sample.
5. Zero managed allocs verified — per Stage 1.4 T1.4.7.
6. No Unity API (no `Time.time`, no `Debug.Log`).

### 2.2 Non-Goals

1. Jitter (TECH-122 / T1.3.7).
2. Buffer scheduling / ring buffer (Step 2 `BlipBaker`).
3. Multi-voice polyphony (caller responsibility).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Fill audio buffer from patch | `Render` writes `count` samples to `buffer[offset..offset+count]`; state advances. |

## 4. Current State

### 4.2 Systems map

- New file: `Assets/Scripts/Audio/Blip/BlipVoice.cs`.
- Depends on TECH-116 (state), TECH-117 (osc bank), TECH-118 (AHDSR FSM), TECH-119 (env level), TECH-120 (LP filter).
- Consumed by `BlipBaker` (Step 2) + `BlipLiveHost` (post-MVP).

## 5. Proposed Design

### 5.1 Target behavior

Static method, stateless in itself. All per-voice state in `ref BlipVoiceState`. Oscillator slots iterated per sample; sum scaled by per-slot `BlipOscillatorFlat.amplitude` (part of TECH-114). Envelope multiplies sum. Filter recursion writes buffer.

## 7. Implementation Plan

### Phase 1 — Driver

- [ ] Declare `BlipVoice` static class + `Render` signature.
- [ ] Pre-compute α filter coeff + stage sample budgets.
- [ ] Per-sample loop integrates osc bank + envelope advance + level math + filter + write.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Render compiles | Unity compile | `npm run unity:compile-check` | |
| Zero-alloc | EditMode test | Stage 1.4 T1.4.7 (`Assert.That(() => …, Is.Not.AllocatingGCMemory)`) | Deferred. |
| Determinism | EditMode test | Stage 1.4 T1.4.5 (sum-of-abs hash) | Deferred. |
| IA indexes green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `BlipVoice.Render` signature matches Stage 1.3 Exit.
- [ ] Zero allocs inside loop (verified Stage 1.4).
- [ ] No Unity API inside kernel.
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. None — tooling / DSP math only; behavioral contracts verified in Stage 1.4.
