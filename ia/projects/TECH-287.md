---
purpose: "TECH-287 — SmoothOnePole helper + LFO per-sample advance."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-287 — SmoothOnePole helper + LFO per-sample advance

> **Issue:** [TECH-287](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Blip Stage 5.3 Phase 2 first task. Add `SmoothOnePole` 1-pole param-smoothing helper to `BlipVoice.cs` (20 ms fc ≈ 50 Hz cutoff coefficient) + per-sample LFO phase advance block in `BlipVoice.Render` for both `lfoPhase0` / `lfoPhase1`. Waveform sampling + routing lands TECH-288 (this task only advances phases — values unused yet).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `public static float SmoothOnePole(ref float z, float target, float coef)` on `BlipVoice`: `z += coef * (target - z); return z`.
2. Pre-computed coefficient `float lfoSmCoef = 1f - (float)Math.Exp(-TwoPi * 50.0 / sampleRate)` once per `Render` invocation (outside sample loop).
3. Per-sample phase advance block in `BlipVoice.Render`: `state.lfoPhase0 += TwoPi * patch.lfo0Flat.rateHz / sampleRate; if (state.lfoPhase0 >= TwoPi) state.lfoPhase0 -= TwoPi;` — mirror for `lfoPhase1`.
4. Pre-compute `lfoPhaseInc0 = TwoPi * rateHz / sampleRate` outside sample loop (avoid per-sample div).
5. Zero managed allocation; applies to both deterministic + live render branches (Stage 5.1 precedent).
6. `npm run unity:compile-check` + `npm run validate:all` green; MVP golden fixtures stay bit-exact (phases advance but unroute yet).

### 2.2 Non-Goals

1. Waveform dispatch (Sine/Tri/Square/S&H) — TECH-288.
2. Route target modulation (pitch/gain/cutoff/pan) — TECH-288.
3. EditMode LFO tests — TECH-288.
4. Glossary rows — TECH-288.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Runtime | Per-sample LFO phases advance deterministically | `state.lfoPhase0/1` increment by `rateHz * 2π / sr`; wrap at `2π` |
| 2 | Patch author | Param changes smooth over ~20 ms (no zipper noise) | `SmoothOnePole` present + hot-path ready for TECH-288 |

## 4. Current State

### 4.1 Domain behavior

No LFO advance today. `BlipVoice.Render` has deterministic + live sample loops (Stage 5.1 FX dispatch mirrored into both). `BlipVoiceState.lfoPhase0/1` exist post-TECH-286 but never incremented.

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipVoice.cs` — add static helper method + pre-compute block + per-sample advance (mirror into both branches near FX dispatch).
- `Assets/Scripts/Audio/Blip/BlipPatchTypes.cs` — consumed (`BlipLfoFlat.rateHz`).
- `Assets/Scripts/Audio/Blip/BlipVoiceState.cs` — consumed (`lfoPhase0`, `lfoPhase1`).
- Reference spec: `ia/specs/audio-blip.md §3.2` (deterministic-branch parity).
- Glossary: **Param smoothing** row lands TECH-288.

## 5. Proposed Design

### 5.1 Target behavior (product)

Param smoothing = Blip 1-pole filter w/ ~20 ms time constant on LFO-modulated params (pitch/gain/cutoff/pan). Prevents zipper noise when LFO steps + when patch params change. Helper is pure static — `z` passed by ref. Coef `1 - exp(-2π·fc/sr)` with fc=50 Hz matches standard 20 ms smoothing.

### 5.2 Architecture / implementation

Helper placed on `BlipVoice` as `public static` — callable from `Render` + future FX code. Phase advance pre-computes `phaseInc` once per invocation to avoid per-sample `* / sampleRate` divide (hot-path cost). Wrap uses single-subtract `if (phase >= 2π) phase -= 2π` (not `fmod` — Stage 5.1 ring-mod precedent). Mirror advance block into deterministic + live branches per Stage 5.1 pattern.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | `public static` helper on `BlipVoice` | Reusable from FX + routing (TECH-288); zero-alloc; no instance overhead | Instance method — extra `this` param; separate class — extra file churn |
| 2026-04-17 | Pre-compute `phaseInc` outside sample loop | Removes per-sample div (hot path) | Inline — wastes cycles at 48 kHz × N voices |
| 2026-04-17 | Single `if`-subtract wrap (not `fmod`) | Matches Stage 5.1 ring-mod phase wrap | `fmod` — slower, external call |
| 2026-04-17 | fc = 50 Hz (20 ms τ) | Standard param-smoothing τ; inaudible |  Higher fc — zipper noise; lower — laggy patch tweaks |

## 7. Implementation Plan

### Phase 1 — SmoothOnePole helper

- [ ] Add `public static float SmoothOnePole(ref float z, float target, float coef)` to `BlipVoice`.

### Phase 2 — LFO per-sample advance

- [ ] In `BlipVoice.Render`, pre-compute `float lfoSmCoef = 1f - (float)Math.Exp(-TwoPi * 50.0 / sampleRate)` outside sample loop.
- [ ] Pre-compute `double lfoPhaseInc0 = TwoPi * patch.lfo0Flat.rateHz / sampleRate;` + same for slot 1.
- [ ] Inside sample loop (deterministic branch): `state.lfoPhase0 += lfoPhaseInc0; if (state.lfoPhase0 >= TwoPi) state.lfoPhase0 -= TwoPi;` + mirror for `lfoPhase1`.
- [ ] Mirror same block into live branch.
- [ ] Run `npm run unity:compile-check` + `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Helper + advance compile; zero allocs | Unity compile + EditMode | `npm run unity:compile-check`; existing `BlipNoAllocTests` | Alloc-delta/call ≤ 0 |
| MVP goldens bit-exact (LFO not routed yet) | EditMode | `BlipGoldenFixtureTests` | Phase advance alone does not affect output sample |
| IA indexes unaffected | Node | `npm run validate:all` | exit 0 |

## 8. Acceptance Criteria

- [ ] `SmoothOnePole` static helper present on `BlipVoice`.
- [ ] `lfoSmCoef` + `lfoPhaseInc0/1` pre-computed once per `Render` invocation.
- [ ] Per-sample phase advance + wrap mirrored into deterministic + live branches.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` exit 0.
- [ ] Existing `BlipGoldenFixtureTests` + `BlipNoAllocTests` + `BlipDeterminismTests` still green (bit-exact, zero-alloc).

## Open Questions

1. None — kernel scaffolding with zero behavioral effect until TECH-288 wires routing.
