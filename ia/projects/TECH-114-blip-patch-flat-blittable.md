---
purpose: "TECH-114 — BlipPatchFlat blittable readonly struct mirror."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-114 — BlipPatchFlat blittable readonly struct mirror

> **Issue:** [TECH-114](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

`BlipPatchFlat` blittable readonly struct mirrors `BlipPatch` scalar fields. No managed refs. No `AudioMixerGroup` (held in `BlipMixerRouter` parallel structure — Step 2). Nested `BlipOscillatorFlat` / `BlipEnvelopeFlat` / `BlipFilterFlat`. Single `mixerGroupIndex` int reserved slot. First task of Stage 1.2 Phase 2; feeds kernel `BlipVoice.Render` (Stage 1.3).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipPatchFlat` readonly struct — all scalars from `BlipPatch`.
2. Nested `BlipOscillatorFlat` / `BlipEnvelopeFlat` / `BlipFilterFlat`.
3. `mixerGroupIndex` int slot (populated by router Step 2).
4. Flatten method / ctor from `BlipPatch`.
5. Zero managed refs (verified via readonly + value types).

### 2.2 Non-Goals

1. Mixer group lookup (Step 2 `BlipMixerRouter`).
2. Hash field (T1.2.5 — persisted on SO, copied into flat optionally).
3. LUT-osc data (reserved; not materialized MVP).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Pass patch data into kernel w/o GC | `in BlipPatchFlat` param on `BlipVoice.Render` compiles + zero-alloc. |

## 4. Current State

### 4.2 Systems map

- New file: `Assets/Scripts/Audio/Blip/BlipPatchFlat.cs`.
- Consumed by Stage 1.3 kernel + Step 2 baker.
- Parallel to `BlipMixerRouter` (Step 2) for managed refs separation.

## 5. Proposed Design

### 5.1 Target behavior

Pure data mirror. Flatten runs main-thread (on `BlipCatalog.Awake`). Post-flatten the struct is immutable + blittable.

## 7. Implementation Plan

### Phase 1 — Flat structs + flatten

- [ ] `BlipOscillatorFlat` / `BlipEnvelopeFlat` / `BlipFilterFlat` readonly structs.
- [ ] `BlipPatchFlat` readonly struct aggregating nested flats.
- [ ] Flatten ctor / static `FromSO(BlipPatch)` method.
- [ ] Reserve `mixerGroupIndex`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Blittable struct compiles | Unity compile | `npm run unity:compile-check` | |
| Zero-alloc contract | EditMode test | Stage 1.4 T1.4.7 no-alloc regression | Kernel-side verification. |
| IA indexes green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `BlipPatchFlat` + 3 nested flats compile as readonly structs.
- [ ] No managed refs (no class fields, no string).
- [ ] Flatten populates all scalars from SO.
- [ ] `mixerGroupIndex` present + default -1.
- [ ] `validate:all` + `unity:compile-check` green.

## Open Questions

1. None — tooling / data-layout; behavior covered by Stage 1.3 kernel + Stage 1.4 tests.
