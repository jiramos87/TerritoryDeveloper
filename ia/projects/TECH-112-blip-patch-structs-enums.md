---
purpose: "TECH-112 — MVP struct + enum definitions for BlipPatch."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-112 — MVP struct + enum definitions for BlipPatch

> **Issue:** [TECH-112](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Define nested `BlipOscillator` / `BlipEnvelope` / `BlipFilter` structs + MVP enums (`BlipId`, `BlipWaveform`, `BlipFilterKind`, `BlipEnvStage`, `BlipEnvShape`). Consumed by `BlipPatch` (T1.2.1) + flatten (T1.2.4) + kernel (Stage 1.3). No curve fields.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipOscillator` struct — scalar fields (waveform, freq, detune cents, pulse duty, gain). No `pitchEnvCurve`.
2. `BlipEnvelope` struct — AHDSR timings + `sustainLevel` + per-stage `BlipEnvShape`. No curve field.
3. `BlipFilter` struct — `kind` + static `cutoffHz`. No `cutoffEnv`.
4. Enums — `BlipId` (10 MVP rows + `None`), `BlipWaveform` (Sine/Triangle/Square/Pulse/NoiseWhite), `BlipFilterKind` (None/LowPass), `BlipEnvStage` (Idle/Attack/Hold/Decay/Sustain/Release), `BlipEnvShape` (Linear/Exponential).

### 2.2 Non-Goals

1. `AnimationCurve` fields (post-MVP §1).
2. LUT-osc enum variants (slot reserved, enum values fixed MVP).
3. Runtime logic (pure data).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Author BlipPatch variants via enum + struct fields | Inspector shows typed enum dropdowns + struct fields. |

## 4. Current State

### 4.2 Systems map

- Same file as T1.2.1 (`Assets/Scripts/Audio/Blip/BlipPatch.cs`) or sibling `BlipPatchTypes.cs`.
- Enum rows must match glossary + master plan Stage 1.2 Exit.

## 5. Proposed Design

### 5.1 Target behavior

10 MVP `BlipId` rows from master plan naming registry (`docs/blip-procedural-sfx-exploration.md` §11). All structs plain `[Serializable]`; no managed refs beyond `BlipFilterKind`.

## 7. Implementation Plan

### Phase 1 — Struct + enum surface

- [ ] Declare `BlipOscillator` / `BlipEnvelope` / `BlipFilter` structs.
- [ ] Declare 5 MVP enums.
- [ ] 10 `BlipId` rows + `None`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Structs + enums compile | Unity compile | `npm run unity:compile-check` | |
| IA indexes green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] 3 structs + 5 enums compile.
- [ ] No curve fields anywhere.
- [ ] `validate:all` + `unity:compile-check` green.

## Open Questions

1. Exact 10 `BlipId` row names come from `docs/blip-procedural-sfx-exploration.md` §11 registry — resolve at kickoff if registry drifted.
