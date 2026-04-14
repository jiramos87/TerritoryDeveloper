---
purpose: "TECH-122 — Per-invocation jitter (pitch / gain / pan) for BlipVoice."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-122 — Per-invocation jitter (pitch / gain / pan) for BlipVoice

> **Issue:** [TECH-122](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land per-invocation jitter for `BlipVoice.Render` — pitch cents ± jitter, gain dB ± jitter, pan ± jitter. Closes Stage 1.3 Phase 3. Honors `BlipPatchFlat.deterministic` flag — skip jitter + use fixed variant index for reproducible bakes. Xorshift RNG seeded deterministically per variant + voice from `BlipVoiceState.rngState`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Pitch jitter — `pitchJitterCents` (patch) → cents multiplier `pow(2, cents/1200)` scales per-slot freq increments.
2. Gain jitter — `gainJitterDb` (patch) → linear mult `pow(10, dB/20)` scales final sample before filter write.
3. Pan jitter — `panJitter` (patch) → adjusts output channel mix; MVP writes mono buffer (Unity stereo route via `AudioSource.PlayOneShot`), stash pan offset on state for Step 2 mixer consumption.
4. Deterministic flag — bypass jitter entirely + use `variantIndex` directly as RNG seed. Same seed → same output (sum-of-abs hash stable per Stage 1.4 T1.4.5).
5. RNG seed — xorshift32 seeded from `variantIndex * 0x9E3779B9 ^ voiceId` (or similar deterministic mix).

### 2.2 Non-Goals

1. Stereo kernel — MVP mono; stereo pan realized in Unity mixer (per Stage 1.1 bootstrap).
2. Modulation LFO (post-MVP per `docs/blip-post-mvp-extensions.md`).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Randomize per-play voice while keeping bake deterministic | Jitter applies on live play; deterministic flag yields stable output. |

## 4. Current State

### 4.2 Systems map

- Folded into `BlipVoice.Render` (TECH-121) — per-invocation block above per-sample loop.
- Consumes `BlipPatchFlat` jitter triplet (TECH-114) + `BlipVoiceState.rngState` (TECH-116).

## 5. Proposed Design

### 5.1 Target behavior

Jitter computed once per `Render` invocation (not per sample). Multipliers applied — pitch scales phase increments; gain scales final per-sample output; pan stashed on state for mixer consumption.

## 7. Implementation Plan

### Phase 1 — Jitter application

- [ ] Xorshift seed from `variantIndex` + voice hash when `!deterministic`; fixed seed when `deterministic`.
- [ ] Sample ± range for each of 3 jitter params.
- [ ] Apply scaled mults to freq increments (pitch) + output mult (gain) + state (pan).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Jitter compiles | Unity compile | `npm run unity:compile-check` | |
| Determinism honored | EditMode test | Stage 1.4 T1.4.5 (sum-of-abs hash stable w/ `deterministic=true`) | Deferred. |
| IA indexes green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Pitch / gain / pan jitter applied per invocation.
- [ ] `deterministic=true` → stable output across invocations.
- [ ] Zero allocs added to `Render` (verified Stage 1.4 T1.4.7).
- [ ] `unity:compile-check` + `validate:all` green.

## Open Questions

1. Pan on mono buffer — MVP stashes pan scalar on state for Step 2 mixer consumption; confirm OK vs writing stereo buffer. Implementer picks per Stage 1.3 Exit wording; default = mono kernel + mixer pan.
