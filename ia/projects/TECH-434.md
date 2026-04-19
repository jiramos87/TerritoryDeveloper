---
purpose: "TECH-434 — Biquad data model + BlipVoiceState delay elements (Stage 5.4 Phase 1)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/blip-master-plan.md"
task_key: "T5.4.1"
---
# TECH-434 — Biquad data model + `BlipVoiceState` delay elements (Stage 5.4 Phase 1)

> **Issue:** [TECH-434](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Pure data-model scaffold for Biquad BP: `BlipFilterKind.BandPass = 2`, `BlipFilter.resonanceQ` (clamped `0.1..20`), `BlipFilterFlat.resonanceQ` (readonly + copy ctor), `BlipVoiceState.biquadZ1/biquadZ2` (DF-II transposed delay elements, blittable). No kernel logic — coefficient pre-compute lands in TECH-435, per-sample kernel in TECH-436.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipFilterKind.BandPass = 2` added to enum.
2. `BlipFilter.resonanceQ` serialized, `OnValidate` clamps `0.1..20`.
3. `BlipFilterFlat.resonanceQ` readonly; `BlipFilterFlat(BlipFilter src, …)` ctor copies value.
4. `BlipVoiceState` gains `float biquadZ1, biquadZ2` (blittable, default 0).
5. `BlipPatchFlat` ctor copies `resonanceQ` through updated `BlipFilterFlat` field.

### 2.2 Non-Goals

- Coefficient pre-compute (TECH-435).
- Per-sample kernel (TECH-436).
- Golden-fixture regen (TECH-437).

## 3. Acceptance Criteria

- `BlipFilterKind.BandPass = 2` present.
- `BlipFilter.resonanceQ` serialized + clamped `0.1..20`.
- `BlipFilterFlat.resonanceQ` readonly + copy through ctor.
- `BlipVoiceState.biquadZ1/biquadZ2` blittable.
- Existing `BlipGoldenFixtureTests` + `BlipNoAllocTests` still green.
- `npm run unity:compile-check` + `npm run validate:all` exit 0.

## 4. Files

- `Assets/Scripts/Audio/Blip/BlipPatchTypes.cs`
- `Assets/Scripts/Audio/Blip/BlipPatch.cs`
- `Assets/Scripts/Audio/Blip/BlipPatchFlat.cs`
- `Assets/Scripts/Audio/Blip/BlipVoiceState.cs`

## 5. Implementation Plan

### Phase 1 — Data model scaffold

- [ ] Add `BandPass = 2` to `BlipFilterKind` enum in `BlipPatchTypes.cs`.
- [ ] Add `[SerializeField] public float resonanceQ = 1f;` to `BlipFilter`; clamp in `BlipPatch.OnValidate`.
- [ ] Add `public readonly float resonanceQ;` to `BlipFilterFlat`; update ctor to copy from `BlipFilter`.
- [ ] Add `public float biquadZ1, biquadZ2;` to `BlipVoiceState`.
- [ ] Update `BlipPatchFlat` ctor to pass updated `BlipFilterFlat`.

## 6. Lessons Learned

_Populate on closeout._
