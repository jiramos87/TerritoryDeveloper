### Stage 3 — DSP foundations + audio infra / Voice DSP kernel


**Status:** Final — all tasks complete (TECH-116..120 Done, TECH-135 Done; TECH-121 + TECH-122 compressed into TECH-135)

**Objectives:** `BlipVoice.Render` kernel. Single static method, stateful via `ref BlipVoiceState`. MVP oscillator bank + AHDSR envelope (per-stage `Linear` or `Exponential` shape) + one-pole LP filter. Per-invocation pitch / gain / pan jitter. No allocs inside `Render`. No Unity API. Shared kernel — used by `BlipBaker` Step 2 + `BlipLiveHost` post-MVP.

**Exit:**

- `BlipVoice` static class — `Render(Span<float> buffer, int offset, int count, int sampleRate, in BlipPatchFlat patch, int variantIndex, ref BlipVoiceState state)`.
- Oscillators — sine, triangle, square, pulse (duty 0–1), noise-white (xorshift RNG on `BlipVoiceState.rngState`). `Math.Sin` path MVP; LUT osc reserved post-MVP per `docs/blip-post-mvp-extensions.md` §1.
- AHDSR envelope state machine — `Idle → Attack → Hold → Decay → Sustain → Release → Idle`. Per-stage shape selectable via `BlipEnvShape` (`Linear` = straight ramp; `Exponential` = `1 - exp(-t/τ)` on attack, `exp(-t/τ)` on decay/release, τ = stage duration / 4 — reads "natural" to ear per perceptual loudness log curve).
- One-pole LP filter — `z1` on `BlipVoiceState`; cutoff from patch scalar. `filter.kind == None` handled via alpha=1 passthrough (single kernel, no branch).
- Jitter applied per-invocation — `pitchJitterCents`, `gainJitterDb`, `panJitter`. Honors `deterministic` flag (skip jitter + use fixed variant index).
- Zero managed allocs inside `Render` (verified via test; see Stage 1.4 T1.4.5 for measurement method).
- No Unity API calls inside `Render` (no `Time.time`, no `Debug.Log`).
- Phase 1 — Oscillator bank + voice state.
- Phase 2 — AHDSR envelope state machine + per-stage shape.
- Phase 3 — Render driver (LP filter + jitter + per-sample loop).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | BlipVoiceState struct | **TECH-116** | Done | `BlipVoiceState` struct — `phaseA..phaseD` (double), `envLevel`, `envStage`, `filterZ1`, `rngState` (xorshift seed), `samplesElapsed`. Blittable; lives in caller. |
| T3.2 | Oscillator bank | **TECH-117** | Done | Oscillator bank — sine (`Math.Sin` MVP), triangle, square, pulse (duty param), noise-white (xorshift on `rngState`). Phase-accumulator; frequency from patch osc + `pitchMult`. |
| T3.3 | AHDSR state machine | **TECH-118** | Done | AHDSR stage machine — `Idle → Attack → Hold → Decay → Sustain → Release → Idle`. Transitions via `samplesElapsed` + per-stage duration from patch (durations already ≥ 1 ms by `BlipPatch.OnValidate` clamp — see T1.2.3). |
| T3.4 | Envelope level math | **TECH-119** | Done | Envelope level math — per-stage `BlipEnvShape` selector. Linear: straight ramp (attack 0→1, decay 1→sustain, release sustain→0). Exponential: `target + (start - target) * exp(-t/τ)` with τ = stageDuration/4 (≈98 % settled at stage end; perceptual linear). Multiplies output sample. |
| T3.5 | One-pole LP filter | _archived_ | Done | One-pole LP filter in-loop — `y[n] = y[n-1] + a * (x[n] - y[n-1])` where `a = 1 - exp(-2π * cutoff / sampleRate)`. `z1` on `BlipVoiceState`. `filter.kind == None` → `a = 1.0` (passthrough, single kernel, no branch). |
| T3.6 | Render driver + jitter (consolidated) | **TECH-135** | Done | `BlipVoice.Render` driver w/ integrated per-invocation jitter — per-sample loop (osc × envelope × filter → buffer, `ref state`, zero alloc) + pitch cents / gain dB / pan ± jitter via xorshift `rngState`, honors `deterministic` flag. Consolidates former T1.3.6 (TECH-121) + T1.3.7 (TECH-122) per stage compress (2026-04-14). |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._
