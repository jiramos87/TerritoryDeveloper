---
purpose: "TECH-435 — Biquad coefficient pre-compute block (Stage 5.4 Phase 1)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/blip-master-plan.md"
task_key: "T5.4.2"
---
# TECH-435 — Biquad coefficient pre-compute block (Stage 5.4 Phase 1)

> **Issue:** [TECH-435](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Add Biquad BP coefficient pre-compute block to `BlipVoice.Render`, gated on `filter.kind == BandPass`. Computes `w0 / sinW / cosW / alp / b0n / a1n / a2n` once per `Render` call (alongside existing LP `alpha` block). Zero per-sample trig. LP + None branches unchanged. No per-sample dispatch yet (TECH-436).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Pre-compute block inserted at existing filter-coefficient site in `Render` (lines 59–71 area).
2. Gated strictly on `filter.kind == BandPass`.
3. LP + None branches unchanged.
4. 1 `Math.Sin` + 1 `Math.Cos` per `Render` invocation — no per-sample trig.

### 2.2 Non-Goals

- Per-sample dispatch (TECH-436).
- Enum / field additions (TECH-434).

## 3. Acceptance Criteria

- Pre-compute block present + kind-gated.
- LP + None branches unchanged.
- `npm run unity:compile-check` green.
- Existing golden-fixture + NoAlloc tests still green.
- `npm run validate:all` exit 0.

## 4. Dependencies

- TECH-434 — `BlipFilterKind.BandPass` enum value + `resonanceQ` field must exist.

## 5. Files

- `Assets/Scripts/Audio/Blip/BlipVoice.cs`

## 6. Implementation Plan

### Phase 1 — Coefficient pre-compute

- [ ] Locate existing LP `alpha` pre-compute block in `BlipVoice.Render`.
- [ ] Add BP branch: `if (filter.kind == BandPass) { double w0 = TwoPi * cutoffHz / sampleRate; float sinW = (float)Math.Sin(w0); float cosW = (float)Math.Cos(w0); float alp = sinW / (2f * Q); float b0n = sinW * 0.5f / (1f + alp); float a1n = -2f * cosW / (1f + alp); float a2n = (1f - alp) / (1f + alp); }`.
- [ ] LP + None branches unmodified.

## 7. Lessons Learned

_Populate on closeout._
