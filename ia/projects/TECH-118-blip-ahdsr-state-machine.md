---
purpose: "TECH-118 — AHDSR envelope state machine for BlipVoice."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-118 — AHDSR envelope state machine for BlipVoice

> **Issue:** [TECH-118](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land AHDSR stage-machine transitions for `BlipVoice` envelope. Stage 1.3 Phase 2 opener. Drives `BlipVoiceState.envStage` through `Idle → Attack → Hold → Decay → Sustain → Release → Idle` via `samplesElapsed` vs per-stage duration from `BlipPatchFlat.envelope`. Durations already ≥ 1 ms per TECH-113 clamp (sustain-only fallback: A=1, D=0, R=1 w/ instant decay allowed).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Stage transitions driven by `samplesElapsed` >= stageDurationSamples.
2. `decayMs == 0` case — transitions Attack → Hold → Sustain directly (skip Decay stage), honoring TECH-113 sustain-only fallback.
3. `Release` trigger — caller-driven (future `BlipEngine.Stop`); MVP release fires on `samplesElapsed` >= patch `durationSeconds` sample count (one-shot mode).
4. Stage entry resets `samplesElapsed` to 0.

### 2.2 Non-Goals

1. Envelope level math (T1.3.4 / TECH-119).
2. Note-off / gate API (post-MVP w/ `BlipLiveHost`).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Advance envelope stage per sample | `AdvanceEnvelope(ref state, in patch, sampleRate)` mutates `envStage` + `samplesElapsed` correctly. |

## 4. Current State

### 4.2 Systems map

- New helper: static method on `BlipVoice` (or sibling `BlipEnvelope.cs`).
- Consumes `BlipEnvelopeFlat` (TECH-114) + `BlipVoiceState` (TECH-116).
- Feeds T1.3.4 envelope-level math (TECH-119).

## 5. Proposed Design

### 5.1 Target behavior

Per-sample state-machine step. Converts stage `attackMs` / `holdMs` / `decayMs` / `releaseMs` → sample counts via `sampleRate * ms / 1000`. `Decay == 0` → skip to Sustain. Sustain stage has no sample budget (persists until Release trigger).

## 7. Implementation Plan

### Phase 1 — Transitions

- [ ] `AdvanceEnvelope` helper — increments `samplesElapsed`, compares vs stage sample budget, transitions stage when exceeded.
- [ ] `decayMs == 0` → Attack → Hold → Sustain shortcut.
- [ ] MVP release trigger — `samplesElapsed` vs patch `durationSeconds` drives Release entry.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| State transitions compile | Unity compile | `npm run unity:compile-check` | |
| IA indexes green | Node | `npm run validate:all` | |
| Stage transitions correct | EditMode test | Stage 1.4 T1.4.3 (envelope-slope sampler) | Deferred to Stage 1.4 harness. |

## 8. Acceptance Criteria

- [ ] Six-stage FSM advances correctly on synthetic samples.
- [ ] Sustain-only patch (A=1 / D=0 / R=1) routes Attack → Sustain in one transition.
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. One-shot release trigger — MVP fires at `durationSeconds`? Or envelope ignores duration and relies on explicit `BlipEngine.Stop`? Implementer picks per Stage 1.3 Exit wording; default = `durationSeconds` (MVP auto-release).
