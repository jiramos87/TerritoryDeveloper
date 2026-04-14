---
purpose: "TECH-120 — One-pole LP filter in BlipVoice render loop."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-120 — One-pole LP filter in BlipVoice render loop

> **Issue:** [TECH-120](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land one-pole LP filter inline in `BlipVoice.Render` per-sample loop. Stage 1.3 Phase 3 opener. Classic difference equation `y[n] = y[n-1] + α * (x[n] - y[n-1])` w/ `α = 1 - exp(-2π * cutoff / sampleRate)`. `filter.kind == None` handled via `α = 1.0` (passthrough) — single kernel, no branch.

## 2. Goals and Non-Goals

### 2.1 Goals

1. One-pole recursion — `z1 += α * (x - z1); output = z1`. `z1` stored on `BlipVoiceState.filterZ1` (TECH-116).
2. α coefficient — `1 - exp(-2π * cutoff / sampleRate)` computed once per `Render` invocation from `BlipFilterFlat.cutoff` (TECH-114).
3. `filter.kind == None` → α = 1.0 (passthrough single-kernel branchless).
4. Zero allocs inside loop.

### 2.2 Non-Goals

1. HP / BP / resonant filters (post-MVP per `docs/blip-post-mvp-extensions.md`).
2. Biquad / state-variable (post-MVP).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Apply LP filter per sample in kernel | `filterZ1` state persists; output attenuates highs above cutoff. |

## 4. Current State

### 4.2 Systems map

- Inline in `BlipVoice.Render` driver (TECH-121).
- Consumes `BlipFilterFlat` (TECH-114) + `BlipVoiceState.filterZ1` (TECH-116).

## 5. Proposed Design

### 5.1 Target behavior

Pre-compute α outside sample loop. Per sample — single mul + add + store on `state.filterZ1`. `Math.Exp` OK per invocation (not per sample).

## 7. Implementation Plan

### Phase 1 — Coefficient + recursion

- [ ] α = 1 - `Math.Exp(-2π * cutoff / sampleRate)` (or α = 1 for `kind == None`).
- [ ] Per-sample recursion on `state.filterZ1`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Filter compiles | Unity compile | `npm run unity:compile-check` | |
| IA indexes green | Node | `npm run validate:all` | |
| Cutoff attenuation | EditMode test | Stage 1.4 T1.4.4 (FFT or energy-ratio) | Deferred. |

## 8. Acceptance Criteria

- [ ] LP filter math lands inline in driver.
- [ ] `None` kind passthrough branchless.
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. None — tooling / DSP math only.
