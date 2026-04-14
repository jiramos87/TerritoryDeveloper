---
purpose: "TECH-119 — Envelope level math (Linear + Exponential shapes) for BlipVoice."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-119 — Envelope level math (Linear + Exponential shapes) for BlipVoice

> **Issue:** [TECH-119](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land per-stage envelope level math. Stage 1.3 Phase 2 second task. Converts current `envStage` + `samplesElapsed` + stage duration + stage `BlipEnvShape` into `envLevel` ∈ [0, 1]. Two shapes — `Linear` (straight ramp) + `Exponential` (`target + (start - target) * exp(-t/τ)`, τ = stageDuration/4, ≈98% settled at stage end; perceptually linear per loudness log curve).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `Linear` shape — Attack 0→1, Decay 1→sustainLevel, Release sustainLevel→0 (straight ramp on `samplesElapsed / stageDurationSamples`).
2. `Exponential` shape — `target + (start - target) * exp(-t / τ)` with τ = stageDurationSamples / 4.
3. Per-stage shape — selected from `BlipEnvelopeFlat.attackShape` / `decayShape` / `releaseShape` (BlipEnvShape enum per TECH-112, already closed).
4. Output multiplies osc sum in Render driver (TECH-121).

### 2.2 Non-Goals

1. Stage transitions (TECH-118).
2. Custom curves / AnimationCurve (post-MVP per `docs/blip-post-mvp-extensions.md` §1).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Compute envelope level for a sample | `ComputeEnvLevel(in envelope, stage, samplesElapsed, stageDurSamples) → float`. |

## 4. Current State

### 4.2 Systems map

- Static helper on `BlipVoice` (or `BlipEnvelope.cs` sibling).
- Consumes `BlipEnvelopeFlat` + `BlipEnvStage` (TECH-116).

## 5. Proposed Design

### 5.1 Target behavior

Pure function. `Math.Exp` OK (no LUT MVP). Hold stage holds at 1. Sustain stage holds at `sustainLevel`. Idle stage returns 0.

## 7. Implementation Plan

### Phase 1 — Level math

- [ ] Linear branch per stage (attack / decay / release).
- [ ] Exponential branch per stage — τ = stageDurSamples / 4.
- [ ] Hold / Sustain / Idle flat-level cases.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Level math compiles | Unity compile | `npm run unity:compile-check` | |
| IA indexes green | Node | `npm run validate:all` | |
| Slope correctness | EditMode test | Stage 1.4 T1.4.3 (envelope-slope sampler) | Deferred. |

## 8. Acceptance Criteria

- [ ] Linear + Exponential shapes land per stage.
- [ ] Exponential ≈98% settled at stage end (verified in Stage 1.4).
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. None — tooling / DSP math only.
