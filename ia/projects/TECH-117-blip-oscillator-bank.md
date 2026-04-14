---
purpose: "TECH-117 — BlipVoice oscillator bank (sine / triangle / square / pulse / noise)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-117 — BlipVoice oscillator bank (sine / triangle / square / pulse / noise)

> **Issue:** [TECH-117](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land oscillator bank math for `BlipVoice` kernel — phase-accumulator osc family covering sine, triangle, square, pulse (duty 0..1), noise-white. Blip master-plan Stage 1.3 Phase 1 second task. Consumed per-sample by `Render` driver (T1.3.6).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Five oscillator kinds — sine (`Math.Sin` MVP), triangle, square, pulse (duty), noise-white (xorshift on `BlipVoiceState.rngState`).
2. Phase-accumulator model — per-invocation phase advanced via `2π * freq / sampleRate` (wraps at 2π for `Math.Sin`; normalized 0..1 for triangle/square/pulse).
3. Frequency from `BlipOscillatorFlat.frequency * BlipOscillatorFlat.pitchMult` (per slot).
4. Per-sample static helper — pure, no Unity API, no allocs.

### 2.2 Non-Goals

1. LUT sine (post-MVP per `docs/blip-post-mvp-extensions.md` §1).
2. Envelope application (T1.3.3 / T1.3.4).
3. Filter (T1.3.5).
4. Jitter (T1.3.7).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Compute one oscillator sample per kind | Static helper returns float sample given kind + phase + duty + rngState. |

## 4. Current State

### 4.2 Systems map

- New file: `Assets/Scripts/Audio/Blip/BlipOscillatorBank.cs` (static helpers) — OR inlined into `BlipVoice.cs` when T1.3.6 lands (implementer picks).
- Depends on `BlipPatchFlat` / `BlipOscillatorFlat` (TECH-114, closed) for freq + duty + kind fields.
- Depends on `BlipVoiceState` (TECH-116) for `rngState` + phase fields.

## 5. Proposed Design

### 5.1 Target behavior

Static per-kind sample functions. Phase passed by `ref` (advanced in helper). Xorshift RNG for noise-white — `rngState ^= rngState << 13; rngState ^= rngState >> 17; rngState ^= rngState << 5;` then map to `[-1, 1]`.

## 7. Implementation Plan

### Phase 1 — Oscillator sample helpers

- [ ] Sine via `Math.Sin`.
- [ ] Triangle via abs-ramp on normalized phase.
- [ ] Square via `phase < 0.5 ? 1 : -1`.
- [ ] Pulse — duty threshold split.
- [ ] Noise-white via xorshift on `ref uint rngState`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Oscillators compile | Unity compile | `npm run unity:compile-check` | |
| IA indexes green | Node | `npm run validate:all` | |
| Oscillator shape correctness | EditMode test | Stage 1.4 T1.4.2 (zero-crossing count) | Deferred to Stage 1.4 harness. |

## 8. Acceptance Criteria

- [ ] Five osc kinds emit expected per-sample values (verified via Stage 1.4 tests).
- [ ] Zero allocs per sample.
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. None — tooling / DSP math only; shape contracts verified in Stage 1.4.
