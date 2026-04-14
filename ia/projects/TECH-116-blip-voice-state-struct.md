---
purpose: "TECH-116 — BlipVoiceState struct (per-voice blittable state)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-116 — BlipVoiceState struct (per-voice blittable state)

> **Issue:** [TECH-116](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land `BlipVoiceState` blittable struct — per-voice DSP state for `BlipVoice.Render` kernel. Opens Blip master-plan Stage 1.3 Phase 1 (voice DSP kernel). Caller owns struct; `Render` mutates via `ref`. Zero managed refs.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipVoiceState` struct — fields: `phaseA`, `phaseB`, `phaseC`, `phaseD` (double phase accumulators for up to 4 osc slots + LFO reserve); `envLevel` (float, 0..1); `envStage` (enum `BlipEnvStage { Idle, Attack, Hold, Decay, Sustain, Release }`); `filterZ1` (float, one-pole LP memory); `rngState` (uint xorshift seed); `samplesElapsed` (int, samples since current envelope-stage entry).
2. Blittable — zero managed refs (no class / array / string).
3. Caller-owned — lives outside `BlipVoice` static kernel. Feeds T1.3.2 oscillator bank + T1.3.3 AHDSR state machine + T1.3.5 filter + T1.3.6 driver.

### 2.2 Non-Goals

1. `Render` method body (T1.3.6).
2. Oscillator math (T1.3.2).
3. Envelope transitions (T1.3.3).
4. LUT oscillator state (post-MVP per `docs/blip-post-mvp-extensions.md` §1).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Hold per-voice DSP state outside the kernel | `BlipVoiceState` struct compiles, blittable, callable via `ref` on `Render`. |

## 4. Current State

### 4.2 Systems map

- New file: `Assets/Scripts/Audio/Blip/BlipVoiceState.cs`.
- Depends on `BlipPatchFlat` (TECH-114, closed) only indirectly — struct itself is independent.
- Consumed by `BlipVoice.Render` (T1.3.6 / TECH-121) + future `BlipLiveHost` (post-MVP).

## 5. Proposed Design

### 5.1 Target behavior

Plain blittable struct; all fields public or internally accessible. `BlipEnvStage` enum co-located. No ctor logic; default zero-init valid (Idle stage, silent).

## 7. Implementation Plan

### Phase 1 — Struct + enum

- [ ] Declare `BlipVoiceState` struct w/ all fields.
- [ ] Declare `BlipEnvStage` enum (6 values).
- [ ] Confirm blittable — no managed refs.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Struct compiles, blittable | Unity compile | `npm run unity:compile-check` | |
| IA indexes green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `BlipVoiceState.cs` lands; struct + `BlipEnvStage` enum compile.
- [ ] Zero managed refs in struct layout.
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. Phase accumulator count — 4 slots (A/B/C/D) match oscillator cap 3 + LFO reserve, or drop to 3 to match `BlipPatchFlat` triplet exactly? Implementer picks; default 4 for LFO headroom (post-MVP field, zero-cost in MVP).
