### Stage 2 — DSP foundations + audio infra / Patch data model


**Status:** Done — TECH-111..TECH-115 Done

**Objectives:** `BlipPatch` ScriptableObject authoring surface + `BlipPatchFlat` blittable mirror + content-hash. MVP skips all `AnimationCurve` fields (no pitch-env curve, no cutoff-env curve, no envelope shape curve) — AHDSR uses parametric ramps (linear or exp per `BlipEnvShape` enum, no curves), filter uses static cutoff Hz. Keeps Step 3 authoring simple + Step 1 scope tight. Curve / LUT infrastructure lands post-MVP per `docs/blip-post-mvp-extensions.md` §1. `BlipMode` enum omitted MVP (single implicit baked path) — added post-MVP when `BlipLiveHost` lands. `useLutOscillators` field reserved / unused MVP to prevent schema churn when post-MVP LUT osc lands.

**Exit:**

- `BlipPatch` SO w/ MVP fields — `oscillators[0..3]`, `envelope` (AHDSR w/ `BlipEnvShape` per-stage), `filter` (one-pole LP), `variantCount`, `pitchJitterCents`, `gainJitterDb`, `panJitter`, `voiceLimit`, `priority`, `cooldownMs`, `deterministic`, `mixerGroup` (ref — authoring only, not flattened), `durationSeconds`, `useLutOscillators` (reserved, unused MVP), `patchHash` (`[SerializeField] private int` — persisted). `CreateAssetMenu` attribute.
- MVP enums — `BlipId` (10 MVP rows + `None`), `BlipWaveform` (`Sine`, `Triangle`, `Square`, `Pulse`, `NoiseWhite`), `BlipFilterKind` (`None`, `LowPass`), `BlipEnvStage` (`Idle`, `Attack`, `Hold`, `Decay`, `Sustain`, `Release`), `BlipEnvShape` (`Linear`, `Exponential`).
- `BlipPatchFlat` blittable readonly struct mirrors SO scalars. No `AnimationCurve`. No `AudioMixerGroup` ref (separate `BlipMixerRouter` owns `BlipId → AudioMixerGroup` map — see Step 2). No managed refs. `mixerGroupIndex` int slot reserved.
- `patchHash` = content hash over serialized fields. Stable across Unity GUID churn + version bumps. Persisted as `[SerializeField] private int` + recomputed on `OnValidate`; re-verified on `Awake` (assert matches recompute; log warning on mismatch).
- Attack/decay/release timing clamp in `OnValidate` — min 1 ms per stage (≈48 samples @ 48 kHz) to prevent snap-onset click. Sustain-only case uses A=1 ms / D=0 / R=1 ms.
- Glossary rows for **Blip patch**, **Blip patch flat**, **patch hash**.
- Phase 1 — `BlipPatch` SO authoring surface + MVP enums + `OnValidate` clamps.
- Phase 2 — `BlipPatchFlat` flatten + content-hash persistence.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | BlipPatch SO scaffold | **TECH-111** | Done | `BlipPatch : ScriptableObject` class + MVP fields + `CreateAssetMenu("Territory/Audio/Blip Patch")`. No `AnimationCurve` fields. No `mode` field (`BlipMode` enum deferred post-MVP). `useLutOscillators` bool present but unread (reserved slot). |
| T2.2 | MVP structs + enums | **TECH-112** | Done | MVP struct + enum definitions — `BlipOscillator` (no `pitchEnvCurve`), `BlipEnvelope` (no `shape` curve; per-stage `BlipEnvShape` + `sustainLevel`), `BlipFilter` (no `cutoffEnv`) + `BlipId`, `BlipWaveform`, `BlipFilterKind`, `BlipEnvStage`, `BlipEnvShape` (`Linear`, `Exponential`). |
| T2.3 | OnValidate clamp guards | **TECH-113** | Done | `OnValidate` guards on `BlipPatch` — clamp `attackMs` / `releaseMs` to ≥ 1 ms (≈48 samples @ 48 kHz mix rate — kills snap-onset click); `decayMs` ≥ 0 ms (sustain-only A=1/D=0/R=1 allowed). Clamp `variantCount` 1..8, `voiceLimit` 1..16, `sustainLevel` 0..1, `cooldownMs` ≥ 0. Oscillator array resize guard caps `oscillators[]` at 3 (matches `BlipPatchFlat` MVP budget). |
| T2.4 | BlipPatchFlat struct | **TECH-114** | Done | `BlipPatchFlat` blittable readonly struct — mirrors SO scalars; no managed refs; no `AudioMixerGroup` ref (held in `BlipMixerRouter` parallel to catalog — Step 2). `BlipOscillatorFlat` / `BlipEnvelopeFlat` / `BlipFilterFlat` nested. Single `mixerGroupIndex` int slot. |
| T2.5 | patchHash content hash | **TECH-115** | Done | `patchHash` content hash — FNV-1a 32-bit digest over serialized scalar fields (osc freqs, env timings, env shapes, filter cutoff, jitter values, cooldown). Stable; ignores Unity GUID + version. `[SerializeField] private int patchHash` persisted on `OnValidate`; `Awake` / `OnEnable` recomputes + asserts match (warn-only). Glossary rows for **Blip patch**, **Blip patch flat**, **patch hash**. |

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
