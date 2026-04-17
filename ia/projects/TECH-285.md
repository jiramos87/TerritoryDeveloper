---
purpose: "TECH-285 — LFO types + BlipPatch/BlipPatchFlat extension."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-285 — LFO types + BlipPatch/BlipPatchFlat extension

> **Issue:** [TECH-285](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Blip Step 5 Stage 5.3 Phase 1 kickoff task. Add LFO type surface to Blip patch data model — `BlipLfoKind` + `BlipLfoRoute` enums + `BlipLfo [Serializable] struct` + `BlipLfoFlat readonly struct` — and extend `BlipPatch` + `BlipPatchFlat` with two LFO slots each. Zero kernel logic — pure data-model scaffold for routing matrix lands downstream tasks (TECH-287/288).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipLfoKind` enum rows: `Off=0, Sine=1, Triangle=2, Square=3, SampleAndHold=4` in `BlipPatchTypes.cs`.
2. `BlipLfoRoute` enum rows: `Pitch=0, Gain=1, FilterCutoff=2, Pan=3` in `BlipPatchTypes.cs`.
3. `BlipLfo [Serializable] struct`: `BlipLfoKind kind; float rateHz, depth; BlipLfoRoute route`.
4. `BlipLfoFlat readonly struct`: blittable mirror for hot-path; ctor from `BlipLfo`.
5. `BlipPatch` gains `[SerializeField] public BlipLfo lfo0, lfo1`; `OnValidate` clamps `rateHz ≥ 0` on both.
6. `BlipPatchFlat` gains `BlipLfoFlat lfo0Flat, lfo1Flat`; `BlipPatchFlat(BlipPatch so, …)` ctor copies both.
7. `npm run unity:compile-check` + `npm run validate:all` green.

### 2.2 Non-Goals

1. LFO per-sample advance — TECH-287.
2. Routing matrix dispatch in `BlipVoice.Render` — TECH-288.
3. `BlipVoiceState.lfoPhase0/1` fields — TECH-286.
4. `SmoothOnePole` helper — TECH-287.
5. Glossary rows — TECH-288.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Patch author | Author LFO on patch SO in Inspector | `lfo0`/`lfo1` visible; kind/rate/depth/route editable; clamp on save |
| 2 | Runtime | Flat copy available to voice hot path | `BlipPatchFlat.lfo0Flat/lfo1Flat` populated from SO ctor |

## 4. Current State

### 4.1 Domain behavior

No LFO surface today. `BlipPatch` exposes osc + env + filter + jitter + FX (post-Stage-5.2). Voice renders without modulators.

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipPatchTypes.cs` — enum + struct home; mirrors existing `BlipFxKind`/`BlipFilter` pattern.
- `Assets/Scripts/Audio/Blip/BlipPatch.cs` — SO with `OnValidate`; append `lfo0`/`lfo1` fields + clamp.
- `Assets/Scripts/Audio/Blip/BlipPatchFlat.cs` (or equiv) — blittable flat struct + ctor.
- Reference spec: `ia/specs/audio-blip.md §4.1` (BlipPatch SO fields).

## 5. Proposed Design

### 5.1 Target behavior (product)

LFO = Blip low-frequency oscillator. Up to 2 per patch. Each has waveform kind (Sine/Tri/Square/S&H/Off), rate (Hz), depth, and route target (Pitch/Gain/Cutoff/Pan). Author in Inspector; copied to flat struct at bake time.

### 5.2 Architecture / implementation

Enums match existing enum pattern in `BlipPatchTypes.cs` (`BlipFxKind` precedent — explicit int values for fixture stability). Structs mirror `BlipFilter`/`BlipFilterFlat` split: authoring struct (`[Serializable]` + mutable) + flat readonly struct for hot path. Ctor pattern on `BlipLfoFlat(BlipLfo src)` copies all 4 fields. `BlipPatchFlat` ctor gains 2 new assignments.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Fixed 2 LFO slots (not array) | Matches master plan Stage 5.3 Exit; blittable friendly | Array of N — variable-length, `BlipPatchFlat` blit hostile |
| 2026-04-17 | Explicit enum int values | Golden-fixture hash stability across enum reorders | Default sequential — risk of rename-time fixture drift |

## 7. Implementation Plan

### Phase 1 — Enums + structs in BlipPatchTypes.cs

- [ ] Add `BlipLfoKind` enum (Off=0..SampleAndHold=4).
- [ ] Add `BlipLfoRoute` enum (Pitch=0..Pan=3).
- [ ] Add `BlipLfo [Serializable] struct` with 4 fields.
- [ ] Add `BlipLfoFlat readonly struct` + ctor from `BlipLfo`.

### Phase 2 — BlipPatch + BlipPatchFlat extension

- [ ] `BlipPatch`: add `[SerializeField] public BlipLfo lfo0, lfo1`.
- [ ] `BlipPatch.OnValidate`: clamp `lfo0.rateHz = Mathf.Max(0f, lfo0.rateHz)` + same for `lfo1`.
- [ ] `BlipPatchFlat`: add `public readonly BlipLfoFlat lfo0Flat, lfo1Flat`.
- [ ] `BlipPatchFlat` ctor: copy both via `new BlipLfoFlat(so.lfo0)` / `new BlipLfoFlat(so.lfo1)`.
- [ ] Run `npm run unity:compile-check` + `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Enums + structs compile; `BlipPatch`/`BlipPatchFlat` extended | Unity compile | `npm run unity:compile-check` | Blittability preserved for `BlipPatchFlat` |
| Glossary / MCP indexes unchanged (glossary rows land TECH-288) | Node | `npm run validate:all` | exit 0 |
| Existing golden fixtures stay bit-exact (no runtime change) | EditMode | `BlipGoldenFixtureTests` via existing `unity-tests` suite | Passthrough — LFO not wired yet |

## 8. Acceptance Criteria

- [ ] `BlipLfoKind` + `BlipLfoRoute` enums present in `BlipPatchTypes.cs`.
- [ ] `BlipLfo` + `BlipLfoFlat` structs present; flat struct blittable.
- [ ] `BlipPatch.lfo0` / `lfo1` serialized; `OnValidate` clamps `rateHz ≥ 0`.
- [ ] `BlipPatchFlat.lfo0Flat` / `lfo1Flat` populated via ctor.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` exit 0.
- [ ] Existing `BlipGoldenFixtureTests` / `BlipNoAllocTests` still green.

## Open Questions

1. None — data-model-only; no gameplay-rule ambiguity. Consumer tasks (TECH-286/287/288) land the behavior.
