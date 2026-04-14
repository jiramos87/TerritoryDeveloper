---
purpose: "TECH-111 — BlipPatch ScriptableObject authoring surface (MVP fields)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-111 — BlipPatch ScriptableObject authoring surface (MVP fields)

> **Issue:** [TECH-111](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Land `BlipPatch : ScriptableObject` authoring class w/ MVP-scoped fields. First task of Blip master-plan Stage 1.2 (Patch data model). Feeds flatten + hash (T1.2.4 / T1.2.5) + DSP kernel (Stage 1.3). No `AnimationCurve` fields. No `mode` (deferred post-MVP).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipPatch` SO w/ MVP scalar fields — `oscillators[0..3]`, `envelope` (AHDSR), `filter` (LP), `variantCount`, jitter triplet, `voiceLimit`, `priority`, `cooldownMs`, `deterministic`, `mixerGroup` (ref), `durationSeconds`, `useLutOscillators` (reserved), `patchHash` (`[SerializeField] private int`).
2. `CreateAssetMenu("Territory/Audio/Blip Patch")` attribute.
3. Inspector-authorable — MVP patches authorable via Unity Editor.

### 2.2 Non-Goals (Out of Scope)

1. `AnimationCurve` fields (post-MVP per `docs/blip-post-mvp-extensions.md` §1).
2. `BlipMode` enum (post-MVP — lands w/ `BlipLiveHost`).
3. Flatten logic (T1.2.4).
4. Hash computation (T1.2.5).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Author BlipPatch assets via Editor | `Assets/Create/Territory/Audio/Blip Patch` menu works; SO fields visible in Inspector. |

## 4. Current State

### 4.2 Systems map

- New file: `Assets/Scripts/Audio/Blip/BlipPatch.cs`.
- Depends on nested types from T1.2.2 (`BlipOscillator`, `BlipEnvelope`, `BlipFilter` structs + enums).
- Consumed by `BlipPatchFlat` (T1.2.4) + `BlipBaker` (Step 2).

## 5. Proposed Design

### 5.1 Target behavior

MVP-scoped ScriptableObject. All scalar fields public-read / serialized; no curves, no runtime-mutable state.

## 7. Implementation Plan

### Phase 1 — Field surface

- [ ] Declare `BlipPatch : ScriptableObject` w/ MVP fields.
- [ ] `CreateAssetMenu` attribute.
- [ ] `useLutOscillators` bool reserved (unread MVP).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| SO compiles + Inspector visible | Unity compile | `npm run unity:compile-check` | Editor menu entry reachable. |
| IA indexes green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `BlipPatch.cs` compiles.
- [ ] `CreateAssetMenu` works.
- [ ] No `AnimationCurve` / no `mode` field.
- [ ] `validate:all` + `unity:compile-check` green.

## Open Questions

1. None — tooling / scaffolding; game logic lands with kernel (Stage 1.3).
