---
purpose: "TECH-437 — Golden fixture regression + spec + 6 glossary rows (Stage 5.4 Phase 2)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/blip-master-plan.md"
task_key: "T5.4.4"
---
# TECH-437 — Golden fixture regression + spec + 6 glossary rows (Stage 5.4 Phase 2)

> **Issue:** [TECH-437](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Stage 5.4 regression gate + documentation closeout. Confirm all 10 MVP golden fixture hashes pass (passthrough invariant — empty FX chain, zero LFOs, None/LowPass filter, bit-exact vs Step 3 baselines; no new hashes). Add 6 glossary rows to `ia/specs/glossary.md`. Update `audio-blip.md §4.2` filter section with BandPass enum value + `resonanceQ`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BlipGoldenFixtureTests` 10/10 hashes green (passthrough invariant).
2. 6 glossary rows: **Blip FX chain**, **Blip LFO**, **Biquad band-pass**, **Param smoothing**, **Blip delay pool**, **Blip LUT pool**.
3. `audio-blip.md §4.2` documents `BlipFilterKind.BandPass` + `resonanceQ`.
4. `npm run validate:all` exit 0.

### 2.2 Non-Goals

- New kernel code (TECH-434–436).
- NoAlloc BP test (TECH-436).

## 3. Acceptance Criteria

- 10/10 golden fixture hashes green (passthrough invariant).
- 6 glossary rows present with cross-refs to `audio-blip.md`.
- `audio-blip.md §4.2` updated with BandPass + `resonanceQ`.
- `npm run validate:all` exit 0.

## 4. Dependencies

- TECH-434, TECH-435, TECH-436 — all Stage 5.4 code must land first.

## 5. Files

- `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs`
- `ia/specs/glossary.md`
- `ia/specs/audio-blip.md`

## 6. Implementation Plan

### Phase 1 — Regression gate

- [ ] Run `BlipGoldenFixtureTests` — confirm 10/10 hashes pass.
- [ ] If any fail, diagnose root cause (no hash regen; passthrough must be bit-exact).

### Phase 2 — Docs + glossary

- [ ] Add 6 glossary rows to `ia/specs/glossary.md`: **Blip FX chain**, **Blip LFO**, **Biquad band-pass**, **Param smoothing**, **Blip delay pool**, **Blip LUT pool**.
- [ ] Update `audio-blip.md §4.2` — add BandPass enum value + `resonanceQ` clamping note.
- [ ] Run `npm run validate:all` — exit 0.

## 7. Lessons Learned

_Populate on closeout._
