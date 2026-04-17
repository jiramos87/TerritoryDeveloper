---
purpose: "TECH-288 — LFO routing matrix + EditMode test + glossary."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-288 — LFO routing matrix + EditMode test + glossary

> **Issue:** [TECH-288](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Blip Stage 5.3 closeout task. Wire the LFO routing matrix in `BlipVoice.Render` — sample waveform per `BlipLfoKind`, scale by `depth`, route to target param (Pitch / Gain / FilterCutoff / Pan) via `SmoothOnePole`. Add `BlipLfoTests` EditMode suite (sine zero-crossing count + monotonic rise/fall). Land 3 glossary rows (**Blip LFO**, **Param smoothing**, **Blip LUT pool**) + `ia/specs/audio-blip.md` cross-refs.

## 2. Goals and Non-Goals

### 2.1 Goals

1. LFO waveform dispatch in `BlipVoice.Render` for both slots: Sine `Math.Sin(phase)`; Triangle `2/π * Math.Asin(Math.Sin(phase))`; Square `Math.Sign(Math.Sin(phase))`; SampleAndHold — re-sample on zero-crossing of internal tick.
2. Output scale by `depth`; skip branch when `kind == Off`.
3. Routing dispatch per `BlipLfoRoute`:
   - Pitch: adds modulated cents to `pitchCents` before jitter block.
   - Gain: multiplies `gainMult` (1 + smoothed value * depth).
   - FilterCutoff: offsets `cutoffHz` before α compute (pre-filter coef).
   - Pan: offsets `panOffset` before stereo split.
4. `SmoothOnePole(ref z, target, lfoSmCoef)` applied on each routed param target.
5. `Assets/Tests/EditMode/Audio/BlipLfoTests.cs` (new): sine-LFO 1 s render — zero-crossing count matches rateHz; monotonic rise (phase 0..π/2) + fall (π/2..π) asserts.
6. Glossary rows in `ia/specs/glossary.md` (Audio section): **Blip LFO** (`ia/specs/audio-blip.md §4.1`), **Param smoothing** (`ia/specs/audio-blip.md §3.2`), **Blip LUT pool** (`ia/specs/audio-blip.md §5.1`).
7. `ia/specs/audio-blip.md` cross-refs to new terms (short LFO note in §4.1 or a new §4.5).
8. `npm run unity:compile-check` + `npm run validate:all` green.

### 2.2 Non-Goals

1. LUT-backed waveform (post-Stage-5.3).
2. Biquad BP — Stage 5.4.
3. More LFO routes (filter Q, FX param0, …) — post-Stage-5.3.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Patch author | Hear LFO modulate pitch / gain / cutoff / pan on a live patch | Zero-crossing count matches rate; no zipper (smoothing on) |
| 2 | Runtime | Zero managed allocs on LFO-routed render | `BlipNoAllocTests` green |
| 3 | IA consumer | Look up **Blip LFO** / **Param smoothing** / **Blip LUT pool** via MCP | Glossary rows present; `validate:all` green |

## 4. Current State

### 4.1 Domain behavior

Pre-TECH-288: LFO data model exists (TECH-285), state fields exist (TECH-286), phases advance (TECH-287). No output effect — phases spin unrouted. No glossary term for LFO.

### 4.2 Systems map

- `Assets/Scripts/Audio/Blip/BlipVoice.cs` — routing matrix block post-phase-advance, pre-FX (applies to pitch/gain/cutoff/pan before existing jitter + filter stages).
- `Assets/Tests/EditMode/Audio/BlipLfoTests.cs` — new EditMode test suite.
- `ia/specs/glossary.md` — 3 new Audio-section rows.
- `ia/specs/audio-blip.md` — cross-ref update (§4.1 LFO note or new §4.5).
- Reference spec: `ia/specs/audio-blip.md §4` (Authoring surface).
- Depends: TECH-285 (data model), TECH-286 (phase fields), TECH-287 (advance + smoothing helper).

## 5. Proposed Design

### 5.1 Target behavior (product)

LFO = per-voice modulator advanced at sample rate. Waveform → scaled by depth → smoothed by 20 ms 1-pole → added/multiplied into target param. Square + S&H produce hard-step values; Sine/Triangle are continuous. Pan LFO offsets stereo position [-1..1] clamp.

### 5.2 Architecture / implementation

Inline dispatch (not delegate) — matches Stage 5.1 FX unrolled-if precedent (blittable + zero-alloc). Per-sample branches:

```
float lfo0Val = 0f;
switch (patch.lfo0Flat.kind) {
  case BlipLfoKind.Sine:          lfo0Val = (float)Math.Sin(state.lfoPhase0); break;
  case BlipLfoKind.Triangle:      lfo0Val = (float)(2.0/Math.PI * Math.Asin(Math.Sin(state.lfoPhase0))); break;
  case BlipLfoKind.Square:        lfo0Val = Math.Sign((float)Math.Sin(state.lfoPhase0)); break;
  case BlipLfoKind.SampleAndHold: /* sampled at wrap boundary into state.lfoShVal0 */ break;
  default:                        lfo0Val = 0f; break;
}
lfo0Val *= patch.lfo0Flat.depth;
SmoothOnePole(ref state.lfoSm0, lfo0Val, lfoSmCoef);
```

Routing block applies smoothed value to the param indexed by `lfo0Flat.route`. Mirror for slot 1. Add `float lfoShVal0, lfoShVal1, lfoSm0, lfoSm1` to `BlipVoiceState` if TECH-286 did not already allocate them (coordinate at implementation time — amend TECH-286 or add here).

### 5.3 Method / algorithm notes

Triangle via `2/π·asin(sin(φ))` avoids branchy peak-detect; costs one extra `asin` per sample but matches master-plan spec. S&H resamples when `lfoPhase0` wraps through 0 (edge-detect via sign of `phaseBefore - phaseAfter`).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Inline `switch` dispatch (not virtual / delegate) | Zero-alloc; blittable patch flat; Stage 5.1 FX precedent | Delegate array — alloc + boxing |
| 2026-04-17 | Route applies PRE-filter / PRE-jitter | FilterCutoff offset must hit α pre-compute; Pitch offset must hit jitter input | POST — cutoff offset ineffective |
| 2026-04-17 | `SmoothOnePole` per route target (not pre-route) | Smooths the ROUTED param, not the raw LFO; matches 20 ms intent | Pre-route — smooths waveform itself (defeats Square intent) |
| 2026-04-17 | Glossary rows cite §4.1 / §3.2 / §5.1 (not new §4.5) | Minimize spec churn; Stage 5.4 will amend §4 batch | New §4.5 — extra section for 3 terms premature |

## 7. Implementation Plan

### Phase 1 — Routing matrix in Render

- [ ] Per-sample waveform `switch` for both LFO slots in deterministic branch.
- [ ] Scale by `depth`; apply `SmoothOnePole` per slot.
- [ ] Route to target: Pitch adds cents; Gain multiplies; FilterCutoff offsets pre-α; Pan offsets stereo pre-split.
- [ ] Mirror block into live branch.
- [ ] Add any missing voice-state scratch fields (`lfoShVal0/1`, `lfoSm0/1`) if not covered by TECH-286.

### Phase 2 — Tests + glossary + spec

- [ ] Author `Assets/Tests/EditMode/Audio/BlipLfoTests.cs` — 2 tests: `SineLfo_ZeroCrossings_MatchRate` + `SineLfo_MonotonicRiseFall_WithinQuarterPeriod`.
- [ ] Add **Blip LFO** row to `ia/specs/glossary.md` Audio category.
- [ ] Add **Param smoothing** row.
- [ ] Add **Blip LUT pool** row.
- [ ] Update `ia/specs/audio-blip.md §4.1` (short note on `lfo0/lfo1` authoring fields).
- [ ] Run `npm run unity:compile-check` + `npm run validate:all`.
- [ ] Run `npm run unity:testmode-batch` (LFO tests green).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| `BlipLfoTests` green (zero-crossing + monotonic) | EditMode | `npm run unity:testmode-batch` | 1 s sine render @ 48 kHz, rate 5 Hz → 10 zero-crossings ±1 tolerance |
| Zero managed alloc on LFO-routed render | EditMode | `BlipNoAllocTests.Render_WithLfoRouted_ZeroManagedAlloc` (amend existing or add) | Alloc delta/call ≤ 0 |
| Goldens stay bit-exact for empty-LFO patches | EditMode | `BlipGoldenFixtureTests` | `kind == Off` short-circuits |
| Glossary + spec cross-refs indexed | Node | `npm run validate:all` | Chains `test:ia`, `generate:ia-indexes --check` |
| `audio-blip.md` edit stays ≤ reference-spec rules | Node | `npm run validate:frontmatter` | via `validate:all` |

## 8. Acceptance Criteria

- [ ] LFO waveform `switch` + route dispatch wired in both render branches.
- [ ] `SmoothOnePole` applied on all 4 route target params.
- [ ] `BlipLfoTests` present + green (zero-crossing + monotonic).
- [ ] MVP `BlipGoldenFixtureTests` still bit-exact (empty-LFO patches unaffected).
- [ ] `BlipNoAllocTests` still green (zero-alloc preserved).
- [ ] 3 glossary rows in `ia/specs/glossary.md` (Audio); `audio-blip.md §4.1` cross-ref updated.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` exit 0.
- [ ] `npm run unity:testmode-batch` green.

## Open Questions

1. S&H re-sample trigger — wrap-through-zero edge-detect OR internal tick counter? Decision at implementation time; master-plan text says "zero-crossing". Default to phase-wrap edge unless test fails.
