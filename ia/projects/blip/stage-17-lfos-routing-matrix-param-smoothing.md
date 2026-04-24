### Stage 17 — Patches + integration + golden fixtures + promotion / LFOs + routing matrix + param smoothing


**Status:** Final

**Objectives:** Up to 2 LFOs per patch (Off/Sine/Triangle/Square/SampleAndHold) routed to pitch/gain/cutoff/pan. `SmoothOnePole` 1-pole 20 ms helper. LFO phases advance per-sample inside `BlipVoice.Render`. `BlipLutPool` plain-class stub wired to `BlipCatalog`.

**Exit:**

- `BlipLfoKind` enum (Off/Sine/Triangle/Square/SampleAndHold) + `BlipLfoRoute` enum (Pitch/Gain/FilterCutoff/Pan) + `BlipLfo [Serializable] struct` (kind, rateHz, depth, route) + `BlipLfoFlat readonly struct` — all in `BlipPatchTypes.cs`.
- `BlipPatch` gains `[SerializeField] public BlipLfo lfo0, lfo1`; `BlipPatchFlat` gains `BlipLfoFlat lfo0Flat, lfo1Flat` + ctor extension.
- `BlipVoiceState.phaseD` renamed `lfoPhase0`; `double lfoPhase1` added. Both blittable.
- `static float SmoothOnePole(ref float z, float target, float coef)` on `BlipVoice.cs`: `z += coef * (target - z); return z`. `lfoSmCoef = 1f - (float)Math.Exp(-TwoPi * 50.0 / sampleRate)` pre-computed per invocation.
- LFO per-sample phase advance + waveform sample in `BlipVoice.Render`; routed to target param before FX stage with `SmoothOnePole` applied.
- `Assets/Scripts/Audio/Blip/BlipLutPool.cs` (new): `internal sealed class BlipLutPool` stub — `float[] Lease(int size)` + `void Return(float[])` via `ArrayPool<float>.Shared`. `BlipCatalog` gains `private BlipLutPool _lutPool = new BlipLutPool()`.
- Glossary rows: **Blip LFO**, **Param smoothing**, **Blip LUT pool** added to `ia/specs/glossary.md` + cross-refs to `ia/specs/audio-blip.md`.
- `npm run validate:all` green.
- Phase 1 — LFO types + data model + `BlipPatch`/`BlipPatchFlat` extension + `BlipVoiceState` LFO phases + `BlipLutPool` stub.
- Phase 2 — `SmoothOnePole` helper + LFO per-sample advance + routing matrix + EditMode LFO test + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T17.1 | LFO types + BlipPatch/BlipPatchFlat extension | **TECH-285** | Done (archived) | `BlipLfoKind` enum (Off=0/Sine=1/Triangle=2/Square=3/SampleAndHold=4) + `BlipLfoRoute` enum (Pitch=0/Gain=1/FilterCutoff=2/Pan=3) + `BlipLfo [Serializable] struct` (BlipLfoKind kind; float rateHz, depth; BlipLfoRoute route) + `BlipLfoFlat readonly struct` — all in `BlipPatchTypes.cs`. `BlipPatch` gains `[SerializeField] public BlipLfo lfo0, lfo1`; `OnValidate` clamps `rateHz ≥ 0`. `BlipPatchFlat` gains `BlipLfoFlat lfo0Flat, lfo1Flat`; ctor copies both. |
| T17.2 | BlipLutPool stub + BlipVoiceState LFO phase fields | **TECH-286** | Done (archived) | New `Assets/Scripts/Audio/Blip/BlipLutPool.cs`: `internal sealed class BlipLutPool` stub with `float[] Lease(int size)` + `void Return(float[])` (via `ArrayPool<float>.Shared`). `BlipCatalog` gains `private BlipLutPool _lutPool = new BlipLutPool()`. `BlipVoiceState.phaseD` renamed → `lfoPhase0` (field rename; update all refs in `BlipVoice.cs` + test files); `double lfoPhase1` added. |
| T17.3 | SmoothOnePole helper + LFO per-sample advance | **TECH-287** | Done (archived) | `public static float SmoothOnePole(ref float z, float target, float coef)` added to `BlipVoice.cs`: `z += coef * (target - z); return z`. Pre-compute `float lfoSmCoef = 1f - (float)Math.Exp(-TwoPi * 50.0 / sampleRate)` outside sample loop. Per-sample phase advance: `state.lfoPhase0 += TwoPi * patch.lfo0Flat.rateHz / sampleRate; if (state.lfoPhase0 >= TwoPi) state.lfoPhase0 -= TwoPi` (same for `lfoPhase1`). |
| T17.4 | LFO routing matrix + EditMode test + glossary | **TECH-288** | Done (archived) | LFO output dispatch in `BlipVoice.Render`: sample waveform per `BlipLfoKind` (Sine `Math.Sin(phase)`, Triangle `2/π*Math.Asin(Math.Sin(phase))`, Square `Math.Sign(Math.Sin(phase))`, S&H on zero-crossing) → scale by `depth` → route: Pitch adds to `pitchCents` applied in jitter block, Gain multiplies `gainMult`, FilterCutoff offsets `cutoffHz` before α compute, Pan offsets `panOffset`. Apply `SmoothOnePole` on each. `BlipLfoTests.cs` (new): sine LFO zero-crossing count + monotonic rise/fall asserts. Glossary rows: **Blip LFO**, **Param smoothing**, **Blip LUT pool** to `ia/specs/glossary.md`. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._
