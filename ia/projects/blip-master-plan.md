# Blip — Master Plan (MVP)

> **Last updated:** 2026-04-16
>
> **Status:** In Progress — Step 1 Done; Step 2 Final (closed 2026-04-15); Step 3 Final (fully shipped 2026-04-16); Step 4 Final (closed 2026-04-16); Step 5 decomposed (tasks _pending_, ready to file when Step 4 Final confirmed); Steps 6–7 skeleton (stages named, tasks _pending_)
>
> **Scope:** Procedural SFX synthesis subsystem. Ten baked sounds, parameter-only patches, zero `.wav` / `.ogg` assets under `Assets/Audio/Sfx/`. Post-MVP extensions (Live DSP, FX chain, LFOs, editor window, 10 more sounds) → `docs/blip-post-mvp-extensions.md`.
>
> **Exploration source:** `docs/blip-procedural-sfx-exploration.md` (§7 architecture, §11 names registry, §13 locked decisions, §14 MVP scope).
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
> - `ia/projects/multi-scale-master-plan.md` — mutates `GridManager.cs` + `GameSaveManager.cs` + save schema. Blip Step 3.3 World lane kickoff must re-read `GridManager` selection surface now that multi-scale Stage 1.3 is archived. `WorldCellSelected` stays scale-agnostic in MVP; per-scale variants tracked in `docs/blip-post-mvp-extensions.md` §4.
> - `ia/projects/sprite-gen-master-plan.md` — Python tool + new `Assets/Sprites/Generated/` output. Disjoint C# surface; no blip collision on runtime code.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
> - `docs/blip-procedural-sfx-exploration.md` — full design + pseudo-code + 20 concrete examples. §13 (locked decisions) + §14 (MVP scope) are ground truth.
> - `docs/blip-post-mvp-extensions.md` — scope boundary (what's OUT of MVP).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

## Step 1 — DSP foundations + audio infra

**Status:** Final

### Stage 1 — DSP foundations + audio infra / Audio infrastructure + persistent bootstrap


**Status:** Final

**Objectives:** Mixer asset + three routing groups wired. `BlipBootstrap` prefab instantiated in `MainMenu.unity` boot scene, survives scene loads. Headless SFX volume binding via `PlayerPrefs` → `AudioMixer.SetFloat` at `BlipBootstrap.Awake` (no Settings UI in MVP — visible slider + mute toggle post-MVP per `docs/blip-post-mvp-extensions.md` §4). Scene-load suppression policy documented so Blip stays silent until `BlipCatalog.Awake` completes.

**Exit:**

- `Assets/Audio/BlipMixer.mixer` w/ three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) + exposed master `SfxVolume` dB param (default 0 dB). Authored via Unity Editor (`Window → Audio → Audio Mixer`); committed as binary YAML asset.
- `BlipBootstrap` GameObject prefab at `MainMenu.unity` root; `DontDestroyOnLoad(transform.root.gameObject)` in `Awake` (pattern per `GameNotificationManager.cs`).
- `SfxVolume` bound headless — `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)`. No UI touched in MVP.
- Scene-load suppression policy doc'd in glossary row + catalog comment.
- Glossary rows land for **Blip mixer group** + **Blip bootstrap**.
- Phase 1 — Mixer asset + three groups + exposed SFX volume param.
- Phase 2 — Persistent bootstrap prefab + headless volume binding + scene-load suppression policy.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | BlipMixer asset | **TECH-98** | Done | Create `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer` — binary YAML, not hand-written). Three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`), each routed through master. Expose master `SfxVolume` dB param (`Exposed Parameters` panel, default 0 dB). |
| T1.2 | Headless volume binding | **TECH-99** | Done | Headless SFX volume binding — `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)`. No Settings UI in MVP (visible slider + mute toggle deferred post-MVP per `docs/blip-post-mvp-extensions.md` §4). Key string constant on `BlipBootstrap`. |
| T1.3 | BlipBootstrap prefab | **TECH-100** | Done | `BlipBootstrap` GameObject prefab + `DontDestroyOnLoad(transform.root.gameObject)` in `Awake` (pattern per `GameNotificationManager.cs`). Empty Catalog / Player / MixerRouter / CooldownRegistry child slots (populated Step 2). Placed at root of `MainMenu.unity` (boot scene; build index 0 per `MainMenuController.cs`). |
| T1.4 | Scene-load suppression | **TECH-101** | Done (archived) | Scene-load suppression policy — no Blip fires until `BlipCatalog.Awake` sets ready flag. Document in glossary rows for **Blip mixer group** + **Blip bootstrap**. |

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

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

### Stage 4 — DSP foundations + audio infra / EditMode DSP tests


**Status:** Done (closed 2026-04-15 — all 5 tasks archived)

**Objectives:** Unity Test Runner EditMode harness covering `BlipVoice.Render`. Owner has no prior game-audio testing experience — tasks scaffolded w/ explicit fixture helpers + assertion patterns. Tests run headless; no Unity audio system dependency (pure `float[]` math). Determinism test uses sum-of-abs tolerance hash (not byte equality — byte-equality within-run is brittle against JIT / `Math.Sin` LSB drift; bit-exact path lands post-MVP w/ LUT osc per `docs/blip-post-mvp-extensions.md` §1).

**Exit:**

- `Assets/Tests/EditMode/Audio/` asmdef w/ refs to `Blip` runtime asmdef + `UnityEngine.TestRunner` + `nunit.framework`.
- Test fixture helpers — render-to-buffer util, zero-crossing counter, envelope-slope sampler, sum-of-abs hash.
- Oscillator tests pass — sine / triangle / square / pulse @ known freq × duration ≈ expected zero-crossings (± tolerance).
- Envelope tests pass — `Linear` shape: attack rises monotonic, hold flat, decay falls, sustain holds, release tails to zero. `Exponential` shape: same monotonicity; additionally attack slope peaks in first quarter (ear-perceived-linear signature).
- Silence test passes — `gainMult = 0` → all samples == 0 (exact).
- Determinism test passes — same seed + patch + variant twice → sum-of-abs hash equal within epsilon (1e-6) + first 256 samples byte-equal (cheap early-signal gate).
- No-alloc regression test passes — steady-state `GC.GetAllocatedBytesForCurrentThread` delta per render call == 0 after warm-up.
- `npm run unity:compile-check` green (loads `.env` / `.env.local` per `CLAUDE.md` §5 — do not skip on empty `$UNITY_EDITOR_PATH`).
- Phase 1 — Test asmdef + fixture helpers.
- Phase 2 — Oscillator + envelope + silence assertions.
- Phase 3 — Determinism + no-alloc regression tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | asmdef + fixture helpers bootstrap | **TECH-137** | Done (archived) | `Assets/Tests/EditMode/Audio/Blip.Tests.EditMode.asmdef` (Editor-only; refs `Blip` runtime + `UnityEngine.TestRunner` + `nunit.framework`) + fixture helper utilities — `RenderPatch(in BlipPatchFlat, int sampleRate, int seconds) → float[]`, `CountZeroCrossings(float[]) → int`, `SampleEnvelopeLevels(float[], int stride) → float[]`, `SumAbsHash(float[]) → double`. Consolidates former T1.4.1 (asmdef) + T1.4.2 (helpers) per stage compress (2026-04-14). |
| T4.2 | Oscillator crossing tests | **TECH-138** | Done (archived) | Oscillator zero-crossing tests — sine @ 440 Hz × 1 s @ 48 kHz ≈ 880 crossings (± 2). Repeat triangle / square / pulse duty=0.5. |
| T4.3 | Envelope shape + silence tests | **TECH-139** | Done (archived) | Envelope shape tests — both `Linear` + `Exponential` shapes. A=50ms/H=0/D=50ms/S=0.5/R=50ms. Assert attack monotonic rising, decay monotonic falling to sustain, release monotonic falling to zero. Exponential-shape extra assert — attack slope in first quarter > slope in last quarter. Silence case — `gainMult = 0` → all-zero buffer (exact equality, not tolerance). Consolidates former T1.4.4 (envelope) + T1.4.5 (silence) per stage compress (2026-04-14). |
| T4.4 | Determinism test | **TECH-140** | Done (archived) | Determinism test — render same patch + seed + variant twice; assert `SumAbsHash` equal within 1e-6 + first 256 samples byte-equal. Validates voice-state reset + RNG determinism without depending on JIT stability of trailing samples. |
| T4.5 | No-alloc regression | **TECH-141** | Done (archived) | No-alloc regression — warm-up loop (3 renders, discard allocation), then measure `GC.GetAllocatedBytesForCurrentThread` delta across 10 steady-state renders; assert delta constant ≤ 0 bytes/call (tolerates NUnit infra alloc outside the measured window). |

**Backlog state (Step 1):** All Step 1 task rows stay in this doc as `_pending_`. File BACKLOG rows + project specs when parent stage → `In Progress` via `stage-file` skill. Stages 2.x + 3.x task decomposition deferred until Step 2 + Step 3 open.

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

## Step 2 — Bake + facade + PlayMode smoke

**Status:** Final

### Stage 5 — Bake + facade + PlayMode smoke / Bake-to-clip pipeline


**Status:** Done — TECH-159 / TECH-160 / TECH-161 / TECH-162 Done (archived) 2026-04-15

**Objectives:** `BlipBaker` plain class ships. Renders `BlipPatchFlat` through `BlipVoice.Render` into `float[]` then wraps via `AudioClip.Create` + `AudioClip.SetData`. LRU cache keyed by `(patchHash, variantIndex)` with 4 MB default memory budget + eviction on overflow. Main-thread only; no MonoBehaviour. Consumed by `BlipCatalog` (Stage 2.2) + `BlipEngine.Play` (Stage 2.3).

**Exit:**

- `BlipBaker` plain class at `Assets/Scripts/Audio/Blip/BlipBaker.cs` — `BakeOrGet(in BlipPatchFlat patch, int variantIndex) → AudioClip`.
- Cache hit path: O(1) `Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>>` lookup + LRU-tail promote.
- Cache miss path: render → `AudioClip.Create(name, lengthSamples, 1, sampleRate, stream: false)` + `SetData(buffer, 0)` → insert at tail + evict head until under budget.
- Memory budget enforced (default 4 MB via ctor param `long budgetBytes = 4 * 1024 * 1024`). Evicted clips destroyed via `UnityEngine.Object.Destroy(clip)`.
- Phase 1 — Baker core + bake key + cache hit/miss dispatch.
- Phase 2 — LRU eviction + memory budget accounting.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | BlipBaker core + render path | **TECH-159** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipBaker.cs`. Plain class (not MonoBehaviour). `BakeOrGet(in BlipPatchFlat patch, int patchHash, int variantIndex) → AudioClip`. `sampleRate` is baker ctor param (default `AudioSettings.outputSampleRate`) — not per-call, not a flat field. `patchHash` passed per-call (flat struct defers hash per Stage 1.2 source line 162). Main-thread assert at entry via `BlipBootstrap.MainThreadId` — TECH-159 lands the minimal static prop + `Awake` capture (T2.3.1 reuses). Computes `lengthSamples = (int)(patch.durationSeconds * _sampleRate)`, allocates `float[lengthSamples]`, initializes `BlipVoiceState` default + calls `BlipVoice.Render(buffer, 0, lengthSamples, _sampleRate, in patch, variantIndex, ref state)`, wraps via `AudioClip.Create(name, lengthSamples, 1, _sampleRate, stream: false)` + `clip.SetData(buffer, 0)`. |
| T5.2 | Bake key + cache hit dispatch | **TECH-160** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipBakeKey.cs` — `readonly struct BlipBakeKey(int patchHash, int variantIndex)` w/ `IEquatable<BlipBakeKey>` + hash combine. In `BlipBaker`: `Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>> _index` + `LinkedList<BlipBakeEntry> _lru`. `BakeOrGet` first probes `_index`; hit → move node to tail + return cached `AudioClip`; miss → invoke render path (T2.1.1) + handoff to Phase 2 eviction. |
| T5.3 | LRU ordering + access tracking | **TECH-161** (archived) | Done (archived) | `BlipBakeEntry` private nested class/struct holding `BlipBakeKey key`, `AudioClip clip`, `long byteCount`. `_lru` access order: newest at tail, oldest at head. Hit → `_lru.Remove(node); _lru.AddLast(node)`. Miss insert → `_lru.AddLast(entry)` after render. Unit-test-able helper `TryEvictHead() → bool` for Phase 2 budget loop. |
| T5.4 | Memory budget + eviction loop | **TECH-162** (archived) | Done (archived) | Ctor param `long budgetBytes = 4L * 1024 * 1024`. Track `_totalBytes` running sum. Each entry `byteCount = lengthSamples * sizeof(float)`. On insert, loop: while `_totalBytes + newByteCount > budgetBytes && _lru.First != null` → pop head, `UnityEngine.Object.Destroy(evicted.clip)`, subtract `evicted.byteCount` from `_totalBytes`, remove from `_index`. Then add new entry + `_totalBytes += newByteCount`. |

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

---

### Stage 6 — Bake + facade + PlayMode smoke / Catalog + mixer router + cooldown registry + player pool


**Status:** Done (6 tasks archived 2026-04-15 — **TECH-169**..**TECH-174**)

**Objectives:** MonoBehaviour hosts + plain-class services wire together under `BlipBootstrap`. `BlipCatalog` flattens authoring SOs, owns `BlipBaker` + `BlipMixerRouter` + `BlipCooldownRegistry` as instance fields (invariant #4 — no new singletons). `BlipPlayer` exposes 16-source pool. No static facade yet; `BlipEngine.Bind` callbacks reserved for Stage 2.3.

**Exit:**

- `BlipPatchEntry` serializable struct at `Assets/Scripts/Audio/Blip/BlipPatchEntry.cs` — `public BlipId id; public BlipPatch patch;`.
- `BlipCatalog : MonoBehaviour` at `Assets/Scripts/Audio/Blip/BlipCatalog.cs` — `SerializeField BlipPatchEntry[] entries`, flattens to `BlipPatchFlat[]`, builds `Dictionary<BlipId, int>` index, owns `BlipBaker` + `BlipMixerRouter` + `BlipCooldownRegistry` instance fields, `Resolve(BlipId) → ref readonly BlipPatchFlat`. Ready flag set last in `Awake`.
- `BlipMixerRouter` plain class at `Assets/Scripts/Audio/Blip/BlipMixerRouter.cs` — `Get(BlipId) → AudioMixerGroup`, built from authoring-only `BlipPatch.mixerGroup` refs.
- `BlipCooldownRegistry` plain class at `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs` — `TryConsume(BlipId, double nowDspTime, double cooldownMs) → bool`.
- `BlipPlayer : MonoBehaviour` at `Assets/Scripts/Audio/Blip/BlipPlayer.cs` — child of `BlipBootstrap`, 16-source pool + round-robin `PlayOneShot(AudioClip, float pitch, float gain, AudioMixerGroup)`.
- Phase 1 — `BlipPatchEntry` + `BlipCatalog` flatten + resolve + ready flag.
- Phase 2 — `BlipMixerRouter` + `BlipCooldownRegistry` plain-class services owned by catalog.
- Phase 3 — `BlipPlayer` 16-source pool + round-robin `PlayOneShot`.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | BlipPatchEntry + catalog flatten | **TECH-169** | Done (archived) | New files `Assets/Scripts/Audio/Blip/BlipPatchEntry.cs` (`[Serializable] public struct BlipPatchEntry { public BlipId id; public BlipPatch patch; }`) + `BlipCatalog.cs` (`sealed : MonoBehaviour`). `[SerializeField] private BlipPatchEntry[] entries`. `Awake` iterates `entries`, builds parallel `BlipPatchFlat[] _flat` via `BlipPatchFlat.FromSO(entry.patch)` (Stage 1.2 helper) + `Dictionary<BlipId, int> _indexById`. Throws `InvalidOperationException` w/ index + id on duplicate `BlipId` or null patch ref. |
| T6.2 | Catalog Resolve + ready flag + Engine bind | **TECH-170** | Done (archived) | `BlipCatalog.Resolve(BlipId id) → ref readonly BlipPatchFlat` via `_indexById` lookup (throws on unknown id). `bool isReady` private field set to `true` as the last statement in `Awake` — scene-load suppression contract per Stage 1.1 T1.1.4. Calls `BlipEngine.Bind(this)` (method added Stage 2.3 T2.3.2 — declare stub signature here; null-safe). `OnDestroy` → `BlipEngine.Unbind(this)` stub. |
| T6.3 | BlipMixerRouter plain class | **TECH-171** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipMixerRouter.cs`. `public sealed class BlipMixerRouter` plain class. Ctor takes `BlipPatchEntry[] entries` + builds `Dictionary<BlipId, AudioMixerGroup> _map` reading authoring-only `entry.patch.mixerGroup` ref (NOT in `BlipPatchFlat` — Stage 1.2 T1.2.4 Decision Log). `Get(BlipId) → AudioMixerGroup` lookup (throws on unknown id). Instantiated in `BlipCatalog.Awake` + held as instance field `_mixerRouter`. |
| T6.4 | BlipCooldownRegistry plain class | **TECH-172** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs`. `public sealed class BlipCooldownRegistry` plain class. `Dictionary<BlipId, double> _lastPlayDspTime`. `TryConsume(BlipId id, double nowDspTime, double cooldownMs) → bool` — if `!_lastPlayDspTime.TryGetValue(id, out var last) |  | (nowDspTime - last) * 1000.0 >= cooldownMs` → write `_lastPlayDspTime[id] = nowDspTime` + return `true`; else return `false`. Instantiated in `BlipCatalog.Awake` + held as instance field `_cooldownRegistry`. |
| T6.5 | BlipPlayer pool construction | **TECH-173** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipPlayer.cs` (`: MonoBehaviour`). `[SerializeField] private int poolSize = 16`. `Awake` instantiates `poolSize` child GameObjects (`new GameObject("BlipVoice_0".."BlipVoice_15")`) parented under this transform, each with `AudioSource` component (`playOnAwake = false`, `loop = false`). Holds `AudioSource[] _pool` + `int _cursor = 0`. Placed as child of `BlipBootstrap` prefab. Calls `BlipEngine.Bind(this)` at end of `Awake`. |
| T6.6 | BlipPlayer PlayOneShot dispatch | **TECH-174** | Done (archived) | `BlipPlayer.PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)` — selects `var source = _pool[_cursor]; _cursor = (_cursor + 1) % _pool.Length;`, stops prior clip if still playing (voice-steal overwrite — no crossfade, post-MVP per orchestration guardrails), sets `source.clip = clip; source.pitch = pitch; source.volume = gain; source.outputAudioMixerGroup = group;` then `source.Play()`. |

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

---

### Stage 7 — Bake + facade + PlayMode smoke / BlipEngine facade + main-thread gate


**Status:** Done — 4 tasks archived (TECH-188..TECH-191) 2026-04-15

**Objectives:** Static `BlipEngine` facade lands. Stateless dispatch per invariant #4 — state lives on `BlipCatalog` / `BlipPlayer` MonoBehaviours; facade caches refs in static fields per invariant #3 (no `FindObjectOfType` on hot path). Main-thread assert gates all entry points. `Play` routes catalog → cooldown → baker → router → player.

**Exit:**

- `BlipEngine` static class at `Assets/Scripts/Audio/Blip/BlipEngine.cs` — `Play(BlipId, float pitchMult = 1f, float gainMult = 1f)` + `StopAll(BlipId)`.
- Main-thread assert at every entry point — compares `Thread.CurrentThread.ManagedThreadId` to cached main-thread id (captured in `BlipBootstrap.Awake`; new `BlipBootstrap.MainThreadId` static read-only accessor).
- `Bind(BlipCatalog)` / `Bind(BlipPlayer)` / `Unbind(*)` static setters consumed by Stage 2.2 hosts + lazy `FindObjectOfType` fallback on first call if not bound. Cached in static fields — no per-frame lookup.
- `Play` dispatch queries cooldown, bails silently when blocked; picks variant; bakes via `BlipBaker.BakeOrGet`; resolves mixer group; forwards to `BlipPlayer.PlayOneShot`.
- Phase 1 — Facade skeleton + main-thread assert + Bind/Unbind + cached lazy resolution.
- Phase 2 — Play + StopAll dispatch bodies through catalog → cooldown → baker → router → player.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | Facade skeleton + main-thread gate | **TECH-188** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipEngine.cs` — `public static class BlipEngine`. Declares `Play(BlipId id, float pitchMult = 1f, float gainMult = 1f)` + `StopAll(BlipId id)` w/ empty bodies for now. Private `AssertMainThread()` helper compares `Thread.CurrentThread.ManagedThreadId` to cached `BlipBootstrap.MainThreadId` (new static read-only prop set in `BlipBootstrap.Awake` → `Thread.CurrentThread.ManagedThreadId`). Throws `InvalidOperationException` w/ diagnostic message on mismatch. Invoked first line of every entry point. |
| T7.2 | Bind/Unbind + cached lazy resolution | **TECH-189** | Done (archived) | In `BlipEngine`: `static BlipCatalog _catalog; static BlipPlayer _player;`. `Bind(BlipCatalog c)` / `Bind(BlipPlayer p)` setters (null-safe overwrite). `Unbind(BlipCatalog)` / `Unbind(BlipPlayer)` nullers. Private `ResolveCatalog() → BlipCatalog` / `ResolvePlayer() → BlipPlayer` — return cached field if non-null, else `FindObjectOfType<BlipCatalog>()` / `FindObjectOfType<BlipPlayer>()` fallback + cache (invariant #3 — one-time lookup, not per-frame). Consumed by `BlipCatalog.Awake` (T2.2.2) + `BlipPlayer.Awake` (T2.2.5). |
| T7.3 | Play dispatch body | **TECH-190** | Done (archived) | `BlipEngine.Play(BlipId id, float pitchMult, float gainMult)` body: `AssertMainThread()` → `var cat = ResolveCatalog(); if (cat == null \ | \ | !cat.IsReady) return;` → `var nowDsp = AudioSettings.dspTime; ref readonly var patch = ref cat.Resolve(id); if (!cat.CooldownRegistry.TryConsume(id, nowDsp, patch.cooldownMs)) return;` → variant index = deterministic (fixed 0) if `patch.deterministic` else xorshift on per-id RNG state held on catalog → `AudioClip clip = cat.Baker.BakeOrGet(in patch, variantIndex);` → `AudioMixerGroup group = cat.MixerRouter.Get(id);` → `ResolvePlayer().PlayOneShot(clip, pitchMult, gainMult, group);`. Expose `cat.IsReady`, `cat.CooldownRegistry`, `cat.Baker`, `cat.MixerRouter` internals via `internal` props on `BlipCatalog`. |
| T7.4 | StopAll dispatch body | **TECH-191** (archived) | Done (archived) | `BlipEngine.StopAll(BlipId id)` body: `AssertMainThread()` → resolve catalog + player → query `cat.Baker` for all cached `AudioClip` refs matching `(patchHash, *)` for this `id` (expose `BlipBaker.EnumerateClipsForPatchHash(int patchHash) → IEnumerable<AudioClip>` helper). Iterate `BlipPlayer._pool`; call `source.Stop()` where `source.clip` matches any enumerated clip. Non-destructive — does not evict baked clips from cache. |

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

---

### Stage 8 — Bake + facade + PlayMode smoke / PlayMode smoke test


**Status:** Done — TECH-196..TECH-199 all archived 2026-04-15.

**Objectives:** New PlayMode test asmdef + smoke fixture exercises full boot path through `BlipEngine.Play` in-scene. Verifies: catalog ready flag, 10 MVP id resolution, mixer routing, 16-source pool non-exhaustion under rapid play, 17th-call cooldown block. Uses `[UnityTest]` + `yield return null` frame waits; no audio listener output assertions (headless-safe).

**Exit:**

- `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef` — Editor+Standalone, `testAssemblies: true`, refs `Blip` runtime asmdef + `UnityEngine.TestRunner` + `nunit.framework`.
- `BlipPlayModeSmokeTests.cs` fixture loads `MainMenu.unity` (boot scene, build index 0) + polls `BlipCatalog.IsReady`.
- All 10 MVP `BlipId` rows resolve non-null patch + non-null `AudioMixerGroup`.
- 16 rapid plays on a zero-cooldown fixture `BlipId` advance `BlipPlayer._cursor` full wrap w/o exception; 17th play within `cooldownMs` window returns silently (no `AudioSource.Play` increment observed).
- `npm run unity:compile-check` green after fixture lands.
- Phase 1 — PlayMode asmdef + boot-scene fixture setup.
- Phase 2 — Resolution + routing + pool + cooldown assertions.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | PlayMode asmdef bootstrap | **TECH-196** | Done (archived) | New file `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef` w/ `"testAssemblies": true`, `"includePlatforms": ["Editor"]` (PlayMode runs in Editor per Unity conv), references: `Blip` runtime asmdef (GUID) + `UnityEngine.TestRunner` + `UnityEditor.TestRunner` + `nunit.framework`. Create companion `.meta`. Empty placeholder `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs` declaring `public sealed class BlipPlayModeSmokeTests` + namespace to anchor asmdef resolution. |
| T8.2 | Boot-scene fixture SetUp | **TECH-197** | Done (archived) | In `BlipPlayModeSmokeTests`: `[UnitySetUp] public IEnumerator SetUp()` → `SceneManager.LoadScene("MainMenu", LoadSceneMode.Single)` then `yield return null` × 2 (one frame for `Awake` cascade, one for ready flag). Assert `BlipBootstrap.Instance != null`, `Object.FindObjectOfType<BlipCatalog>().IsReady == true`. Hold catalog + player refs as private fields for per-test access. `[UnityTearDown]` unloads scene cleanly. |
| T8.3 | Resolution + routing assertions | **TECH-198** (archived) | Done (archived) | `[UnityTest] public IEnumerator Play_AllMvpIds_ResolvesAndRoutes()` — for each `BlipId` in `{UiButtonHover, UiButtonClick, ToolRoadTick, ToolRoadComplete, ToolBuildingPlace, ToolBuildingDenied, WorldCellSelected, EcoMoneyEarned, EcoMoneySpent, SysSaveGame}`: assert `catalog.Resolve(id)` returns non-null patch ref (patchHash != 0), `catalog.MixerRouter.Get(id) != null`, `Assert.DoesNotThrow(() => BlipEngine.Play(id))`. `yield return null` once after loop to drain AudioSource.Play side-effects. |
| T8.4 | Pool + cooldown assertions | **TECH-199** (archived) | Done (archived) | `[UnityTest] public IEnumerator Play_RapidFire_ExhaustsPoolAndBlocksOnCooldown()` — use a fixture `BlipId` w/ near-zero cooldown (e.g. `ToolRoadTick` 30 ms) plus one w/ long cooldown. Fire 16 rapid `BlipEngine.Play(tickId)` within one frame (no yield between) — assert no exception + `player._cursor == 0` after wrap (expose via `internal` accessor). For cooldown: fire `Play(longCooldownId)` once, immediately fire again; verify second call returns silently (track `catalog.CooldownRegistry` last-play dict didn't update, OR expose a debug counter `BlipCooldownRegistry.BlockedCount` incremented on block). |

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

## Step 3 — Patches + integration + golden fixtures + promotion

**Status:** Final

### Stage 9 — Patches + integration + golden fixtures + promotion / Patch authoring + catalog wiring


**Status:** Done (all tasks archived 2026-04-15 — TECH-209..TECH-212)

**Objectives:** Ten `BlipPatch` SO assets authored + `BlipCatalog.entries[]` wired in Inspector. After this stage all 10 MVP `BlipId` values resolve a non-null patch + non-null `AudioMixerGroup` from the catalog; `BlipEngine.Play` is unblocked but no call sites exist yet.

**Exit:**

- `Assets/Audio/BlipPatches/` dir + 10 `BlipPatch` SO asset files. Each SO: envelope/oscillator/filter params per exploration §9 recipes; `cooldownMs` per Exit criteria (ToolRoadTick 30 ms, WorldCellSelected 80 ms, SysSaveGame 2000 ms; others per §9); `patchHash` non-zero after `OnValidate`.
- `mixerGroup` authoring ref set on each SO per exploration §14 routing table (`Blip-UI` for `UiButtonHover` + `UiButtonClick`; `Blip-World` for `ToolRoad*` + `ToolBuilding*` + `WorldCellSelected`; confirm §14 for Eco/Sys ids).
- `BlipCatalog.entries[]` array populated in Inspector — 10 `BlipPatchEntry` rows (each: `BlipId` enum + `BlipPatch` asset ref). `BlipBootstrap` prefab Catalog + Player child slots confirmed wired.
- PlayMode smoke: `BlipCatalog.IsReady == true`; all 10 ids resolve non-null patch + non-null `AudioMixerGroup` via `BlipMixerRouter`.
- `npm run unity:compile-check` green.
- Phase 1 — Author 10 `BlipPatch` SO assets with envelope/oscillator params + cooldown from §9 recipes.
- Phase 2 — Assign `mixerGroup` refs + wire `BlipCatalog.entries[]` in Inspector + smoke verify.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | UI/Eco/Sys patch SOs | **TECH-209** | Done (archived) | Create `Assets/Audio/BlipPatches/` dir + author 5 UI/Eco/Sys `BlipPatch` SOs via CreateAssetMenu (`Territory/Audio/Blip Patch`): `UiButtonHover` (§9 ex 1), `UiButtonClick` (§9 ex 2), `EcoMoneyEarned` (§9 ex 17), `EcoMoneySpent` (§9 ex 18), `SysSaveGame` (§9 ex 20). Fill all envelope/oscillator/filter/jitter params from §9 recipe table. `patchHash` recomputed on `OnValidate` — verify non-zero in Inspector after fill. |
| T9.2 | World patch SOs | **TECH-210** | Done (archived) | Author 5 World `BlipPatch` SOs: `ToolRoadTick` (§9 ex 5; `cooldownMs` 30), `ToolRoadComplete` (§9 ex 6), `ToolBuildingPlace` (§9 ex 9), `ToolBuildingDenied` (§9 ex 10), `WorldCellSelected` (§9 ex 15; `cooldownMs` 80). Set all envelope/oscillator/filter/jitter/variantCount/voiceLimit params per §9. `patchHash` non-zero after `OnValidate`. |
| T9.3 | MixerGroup refs + catalog wire | **TECH-211** | Done (archived) | Set `mixerGroup` authoring ref on all 10 SOs per exploration §14 routing table (open each SO in Inspector, assign `AudioMixerGroup` from `BlipMixer.mixer` asset). Wire `BlipCatalog.entries[]` in Inspector — 10 `BlipPatchEntry` rows (`BlipId` + `BlipPatch` asset ref). Open `BlipBootstrap` prefab; confirm Catalog + Player child slots populated. |
| T9.4 | PlayMode smoke verify | **TECH-212** | Done (archived) | PlayMode smoke: enter Play Mode, load `MainMenu.unity`, poll `BlipCatalog.IsReady`; for all 10 `BlipId` values assert `catalog.Resolve(id).patchHash != 0` + `catalog.MixerRouter.Get(id) != null`. `npm run unity:compile-check` green. Confirms SO → catalog → mixer-router chain complete before any call site lands. |

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

### Stage 10 — Patches + integration + golden fixtures + promotion / UI + Eco + Sys call sites


**Status:** Done — all tasks archived 2026-04-15 (TECH-215..TECH-218)

**Objectives:** `BlipEngine.Play` wired at MainMenu button hover/click + money earn/spend + save-complete. Six `BlipId` values active in game: `UiButtonHover`, `UiButtonClick`, `EcoMoneyEarned`, `EcoMoneySpent`, `SysSaveGame`. No world-lane sounds yet.

**Exit:**

- `MainMenuController.cs` — `BlipEngine.Play(BlipId.UiButtonClick)` as first statement in each of `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`. `EventTrigger` PointerEnter callbacks on each `Button` reference fire `BlipEngine.Play(BlipId.UiButtonHover)` — registered programmatically alongside `onClick.AddListener` calls (get-or-add `EventTrigger` component, add `EventTriggerType.PointerEnter` entry). No new singletons (invariant #4); `BlipEngine` static facade self-caches (invariant #3).
- `EconomyManager.cs` — `BlipEngine.Play(BlipId.EcoMoneyEarned)` after `cityStats.AddMoney(amount)` (line ~205); `BlipEngine.Play(BlipId.EcoMoneySpent)` after `cityStats.RemoveMoney(amount)` in the success branch of `SpendMoney` (line ~169). Monthly-maintenance `SpendMoney` path excluded (non-interactive budget charge — guard by `notifyInsufficientFunds` param or call-context flag).
- `GameSaveManager.cs` — `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText` in `SaveGame` (line ~69) and in `TryWriteGameSaveToPath` (line ~91). 2 s cooldown enforced by `BlipCooldownRegistry` via patch SO — no additional guard.
- `npm run unity:compile-check` green.
- Phase 1 — UI lane: `MainMenuController` click + hover call sites.
- Phase 2 — Eco + Sys lane: `EconomyManager` earn/spend + `GameSaveManager` save-complete.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | MainMenu click call sites | **TECH-215** | Done | `MainMenuController.cs` — add `BlipEngine.Play(BlipId.UiButtonClick)` as first statement in each of: `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`. No `FindObjectOfType` introduced — `BlipEngine` is static facade (invariant #3). |
| T10.2 | MainMenu hover EventTrigger | **TECH-216** | Done (archived) | `MainMenuController.cs` — in `RegisterButtonListeners` / `Start` (where `onClick.AddListener` calls live, line ~133): for each `Button` field (`continueButton`, `newGameButton`, `loadCityButton`, `optionsButton`, `loadCityBackButton`, `optionsBackButton`), call `GetOrAddComponent<EventTrigger>()` + add `EventTriggerType.PointerEnter` entry invoking `BlipEngine.Play(BlipId.UiButtonHover)`. No new fields; no new singletons (invariant #4). |
| T10.3 | Economy earn/spend call sites | **TECH-217** | Done (archived) | `EconomyManager.cs` — add `BlipEngine.Play(BlipId.EcoMoneyEarned)` after `cityStats.AddMoney(amount)` in `AddMoney` (line ~205). Add `BlipEngine.Play(BlipId.EcoMoneySpent)` after `cityStats.RemoveMoney(amount)` in success branch of `SpendMoney` (line ~169). Monthly-maintenance path (`ChargeMonthlyMaintenance` → `SpendMoney`) must NOT fire — guard with `notifyInsufficientFunds == true` condition or add private overload with `bool fireBlip = true`. |
| T10.4 | SaveGame call sites | **TECH-218** | Done (archived) | `GameSaveManager.cs` — add `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText(path, ...)` in `SaveGame` (line ~69) and after equivalent write in `TryWriteGameSaveToPath` (line ~91). 2 s cooldown in patch SO `cooldownMs = 2000`; `BlipCooldownRegistry` gates rapid manual saves — no additional guard. `npm run unity:compile-check` green. |

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

### Stage 11 — Patches + integration + golden fixtures + promotion / World lane call sites


**Status:** Done — all 4 tasks archived (TECH-219 + TECH-220 archived 2026-04-15, TECH-221 + TECH-222 archived 2026-04-16)

**Objectives:** `BlipEngine.Play` wired at road per-tile tick + stroke complete + building place/denied + cell select. Five remaining `BlipId` values active: `ToolRoadTick`, `ToolRoadComplete`, `ToolBuildingPlace`, `ToolBuildingDenied`, `WorldCellSelected`.

**Exit:**

- `RoadManager.cs` — `BlipEngine.Play(BlipId.ToolRoadTick)` at per-tile road commit inside `HandleRoadDrawing` (line 141) or `PlaceRoadTileFromResolved` (line 2706). `BlipCooldownRegistry` at 30 ms gates rapid ticks — no additional guard. `BlipEngine.Play(BlipId.ToolRoadComplete)` at road-stroke-complete/apply site (grep `CommitStroke`/`ApplyRoadPlan`/`ConfirmStroke` or `PathTerraformPlan.Apply` call site in `HandleRoadDrawing`). `BlipEngine` self-caches — safe to call per tile (invariant #3).
- `BuildingPlacementService.cs` — `BlipEngine.Play(BlipId.ToolBuildingPlace)` at end of success path in `PlaceBuilding` (line 234). `BlipEngine.Play(BlipId.ToolBuildingDenied)` at failure-notification site where `TryValidateBuildingPlacement` returns non-null reason (in `HandleBuildingPlacement`, `GridManager.cs` line 874 or equivalent caller).
- `GridManager.cs` — `BlipEngine.Play(BlipId.WorldCellSelected)` immediately after each `selectedPoint = mouseGridPosition` assignment (lines 391, 399). One-liner side-effect — not new GridManager logic (invariant #6 carve-out). 80 ms cooldown enforced by `BlipCooldownRegistry`.
- `npm run unity:compile-check` green.
- Phase 1 — Road lane: per-tile tick + stroke complete in `RoadManager.cs`.
- Phase 2 — Building + grid: place/denied in `BuildingPlacementService.cs` + cell-select in `GridManager.cs`.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | Road per-tile tick | **TECH-219** | Done (archived) | `RoadManager.cs` — locate per-tile commit site: grep callers of `PlaceRoadTileFromResolved` (line 2706) inside `HandleRoadDrawing` (line 141). Add `BlipEngine.Play(BlipId.ToolRoadTick)` at the point each confirmed road tile is committed to the grid. Cooldown 30 ms enforced by `BlipCooldownRegistry` via patch SO — no additional rate-limit guard. |
| T11.2 | Road stroke complete | **TECH-220** | Done (archived) | `RoadManager.cs` — locate stroke-complete hook: grep `CommitStroke`, `ApplyRoadPlan`, `ConfirmStroke`, or `PathTerraformPlan.Apply` call in `HandleRoadDrawing` (line 141 area) or `GridManager.HandleBulldozerMode` vicinity. Add `BlipEngine.Play(BlipId.ToolRoadComplete)` at end of success path (after all tiles placed, before `InvalidateRoadCache()`). `npm run unity:compile-check` green after road edits. |
| T11.3 | Building place/denied call sites | **TECH-221** | Done (archived) | `BuildingPlacementService.cs` — add `using Territory.Audio;` import, `BlipEngine.Play(BlipId.ToolBuildingPlace)` in `PlaceBuilding` success branch (after `PostBuildingConstructed`, line ~251), `BlipEngine.Play(BlipId.ToolBuildingDenied)` in `else` branch (after `PostBuildingPlacementError`, line ~258). Kickoff audit 2026-04-16 relocated denied call from GridManager caller — `HandleBuildingPlacement` line 874 is a 4-line delegate with no fail-reason branch. Insufficient-funds early-return stays silent. Scope: 1 file, 3 line-additions. |
| T11.4 | GridManager cell-select | **TECH-222** | Done | `GridManager.cs` — add `using Territory.Audio;` import + `BlipEngine.Play(BlipId.WorldCellSelected)` after line 391 (`selectedPoint = mouseGridPosition`, left-click-down) + line 399 (`selectedPoint = pendingRightClickGridPosition`, right-click-up non-pan). Kickoff 2026-04-16 confirmed file lacks `Territory.Audio` import — sibling TECH-221 lesson propagated. Invariant #6 carve-out: one-liner side-effect, not new GridManager logic. Invariant #3: `BlipEngine` self-caches — no per-frame lookup added. 80 ms cooldown in patch SO. `npm run unity:compile-check` green. |

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

### Stage 12 — Patches + integration + golden fixtures + promotion / Golden fixtures + spec promotion + glossary


**Status:** Done (closed 2026-04-16) — all tasks archived (TECH-227..TECH-230)

**Objectives:** Fixture harness gates DSP output regression. Exploration doc promoted to canonical spec. Glossary rows complete + cross-referenced to spec. After this stage Blip subsystem fully shipped + regression-gated.

**Exit:**

- `tools/fixtures/blip/` dir + 10 JSON fixture files (one per MVP `BlipId`, variant 0). Each: `{ "id": "<BlipId>", "variant": 0, "patchHash": <int>, "sampleRate": 44100, "sampleCount": <int>, "sumAbsHash": <double>, "zeroCrossings": <int> }`.
- `tools/scripts/blip-bake-fixtures.ts` (dev-only) — bakes each patch via `BlipVoice.Render` logic (TS port or Unity batchmode shim) + writes fixture JSONs. CI does NOT run this script; CI runs regression test only.
- `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` in `Blip.Tests.EditMode.asmdef` (Stage 1.4 asmdef — no new asmdef). One `[Test]` per `BlipId`: parse fixture JSON, re-render via `BlipVoice.Render`, assert `sumAbsHash` within 1e-6 + `zeroCrossings` within ±2 + `patchHash` equality (stale-fixture guard).
- `ia/specs/audio-blip.md` exists — structure matches `ia/specs/*.md` conventions. `docs/blip-procedural-sfx-exploration.md` has "Superseded by `ia/specs/audio-blip.md`" banner.
- `ia/specs/glossary.md` — new rows: **Blip variant**, **Blip cooldown**, **Bake-to-clip**, **Patch flatten**. All existing blip rows updated to cross-ref `ia/specs/audio-blip.md`.
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | Fixtures dir + bake script | **TECH-227** | Done (archived) | Create `tools/fixtures/blip/` dir + author `tools/scripts/blip-bake-fixtures.ts` — pure TypeScript port of `BlipVoice.Render` scalar loop (oscillator bank + AHDSR + one-pole LP; float32 math matching C# kernel) or Node shim invoking Unity batchmode. Bakes variant 0 for each of 10 MVP patch param sets (hardcoded from exploration §9 recipes). Writes `tools/fixtures/blip/{id}-v0.json` per id. Run once: `npx ts-node tools/scripts/blip-bake-fixtures.ts`; verify 10 JSON files produced. |
| T12.2 | Golden fixture regression test | **TECH-228** | Done (archived) | `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` in existing `Blip.Tests.EditMode.asmdef` (Stage 1.4 — no new asmdef; namespace `Territory.Tests.EditMode.Audio`). Parameterized `[TestCase(BlipId.*)]` × 10: parse `tools/fixtures/blip/{id}-v0.json` via `JsonUtility.FromJson<BlipFixtureDto>`, load SO via `AssetDatabase.LoadAssetAtPath<BlipPatch>("Assets/Audio/Blip/Patches/BlipPatch_{id}.asset")`, re-render via existing `BlipTestFixtures.RenderPatch(in flat, sampleRate=48000, seconds=sampleCount/sampleRate, variant)`, assert `SumAbsHash` within 1e-6 + zero-crossing count within ±2 + `patch.PatchHash == fx.patchHash` (fails if fixture stale — msg points at TECH-227 bake script). Kickoff 2026-04-16: aligned spec sample-rate 44100→48000, namespace `Blip.*`→`Territory.*`, helper class `BlipTestHelpers`→`BlipTestFixtures`, asset path `Assets/Audio/BlipPatches/`→`Assets/Audio/Blip/Patches/`, `RenderPatch` 3rd arg sampleCount→seconds. |
| T12.3 | Exploration → spec promotion | **TECH-229** | Done (archived) | Promote `docs/blip-procedural-sfx-exploration.md` → `ia/specs/audio-blip.md`. Restructure to match `ia/specs/*.md` conventions (section numbering, header format). Add "Superseded by `ia/specs/audio-blip.md`" banner at top of exploration doc. `npm run validate:all` — checks dead spec refs + frontmatter. |
| T12.4 | Glossary rows + cross-refs | **TECH-230** | Done (archived) | `ia/specs/glossary.md` — add rows: **Blip variant** (per-patch randomized sound selection index 0..variantCount-1), **Blip cooldown** (minimum ms between same-id plays; enforced by `BlipCooldownRegistry`), **Bake-to-clip** (on-demand render of `BlipPatchFlat` to `AudioClip` via `BlipBaker.BakeOrGet`), **Patch flatten** (`BlipPatch` SO → `BlipPatchFlat` blittable mirror in `BlipCatalog.Awake`). Rewrite Spec col on 5 existing Audio rows from `ia/projects/blip-master-plan.md` Stage 1.x → `ia/specs/audio-blip.md §N` per kickoff §5.2 mapping. Refresh Index row line 32 to list all 9 Audio terms. `npm run validate:all` green. Kickoff 2026-04-16: corrected over-claim (spec listed 13 existing rows; only 5 exist) + glossary 3-col format (was 4-col) + bundled Index refresh. |

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

---

## Step 4 — Settings UI + volume controls (post-MVP)

**Status:** Done (Stage 4.1 closed 2026-04-16 — TECH-235..TECH-238 all archived; Stage 4.2 closed 2026-04-16 — TECH-243..TECH-246 all archived)

**Backlog state (Step 4):** 8 archived (Stage 4.1 — TECH-235, TECH-236, TECH-237, TECH-238; Stage 4.2 — TECH-243, TECH-244, TECH-245, TECH-246)

**Objectives:** Surface SFX volume + mute to player. MVP binds `BlipSfxVolumeDb` headless via `PlayerPrefs` (Stage 1.1 T1.1.2) — no visible control today. Options-menu slider (normalized 0..1) + mute toggle write `PlayerPrefs` + `AudioMixer.SetFloat("SfxVolume")` live; persist across runs. Small isolated Step — ships independent of Steps 5–7.

**Exit criteria:**

- `MainMenu.unity` Options panel exposes SFX volume slider + mute toggle — mounted inside existing `OptionsPanel` surface.
- Slider domain 0..1 normalized; internal dB conversion `20 * Log10(v)` w/ `-80 dB` floor at `v == 0`; mute toggle hard-clamps to `-80 dB`.
- Slider callback writes `PlayerPrefs.SetFloat("BlipSfxVolumeDb", db)` + `BlipMixer.SetFloat("SfxVolume", db)` on change. Mixer ref cached in `Awake` (invariant #3).
- Mute persists as `PlayerPrefs.GetInt("BlipSfxMuted", 0)`; read at `BlipBootstrap.Awake` ahead of volume apply.
- No new MonoBehaviour singletons (invariant #4); settings controller mounts on `OptionsPanel` GameObject.
- `npm run unity:compile-check` green; `npm run validate:all` green.
- Glossary row updated — **Blip bootstrap** notes visible-volume-UI path alongside headless PlayerPrefs binding.
- Phase 1 — Fixture infrastructure: bake script + fixture JSON files.
- Phase 2 — Fixture regression test + spec promotion + glossary.

**Art:** None — reuses existing UI design system.

**Relevant surfaces (load when step opens):**
- Step 3 outputs on disk: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` (lines 29–32: `SfxVolumeDbKey`, `SfxVolumeParam`, `SfxVolumeDbDefault` constants; line 52: `PlayerPrefs.GetFloat` in `Awake` — mute key not yet present).
- `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — `CreateOptionsPanel` at line 308 (Title + Back button only; `sizeDelta = (300, 200)` at line 323); `OnOptionsClicked` at line 511.
- `ia/specs/audio-blip.md §5.1`, `§5.2` — component map + init lifecycle.
- `ia/rules/invariants.md` #3 (mixer ref cached in `Awake`, not per-frame), #4 (no new singletons — controller on OptionsPanel, not static).
- New file: `Assets/Scripts/Audio/Blip/BlipVolumeController.cs` (new).

### Stage 13 — Patches + integration + golden fixtures + promotion / Options panel UI (slider + mute toggle + controller stub)


**Status:** Final (4 tasks filed 2026-04-16 — TECH-235..TECH-238 all archived; closed 2026-04-16)

**Objectives:** Add SFX volume `Slider` (0..1) + mute `Toggle` to `OptionsPanel` programmatic construction in `MainMenuController.CreateOptionsPanel`. Land `BlipVolumeController` stub MonoBehaviour (fields + listener wire-up, no persist/apply logic). `BlipBootstrap` exposes `BlipMixer` accessor. No persist or apply logic yet.

**Exit:**

- `Assets/Scripts/Audio/Blip/BlipVolumeController.cs` — stub `sealed class BlipVolumeController : MonoBehaviour` with `Slider _sfxSlider`, `Toggle _sfxToggle`, `AudioMixer _mixer` fields; `public void Bind(Slider s, Toggle t)` assigns refs; `public void InitListeners()` wires `onValueChanged` delegates to empty stubs `OnSliderChanged(float)` + `OnToggleChanged(bool)`; `public void OnPanelOpen()` empty stub.
- `BlipBootstrap.cs` gains `public AudioMixer BlipMixer => blipMixer;` accessor after `SfxVolumeDbDefault` constant (line ~34).
- `MainMenuController.CreateOptionsPanel` (line 308): `sizeDelta` expanded to `(300, 260)`; `Slider` child `"SfxVolumeSlider"` + label `"SFX Volume"` added at y=-65; `Toggle` child `"SfxMuteToggle"` + label `"Mute SFX"` at y=-100; `panel.AddComponent<BlipVolumeController>()` → `controller.Bind(sfxSlider, sfxToggle)` → `controller.InitListeners()`. Back button still wires and works.
- `MainMenuController` gains `private BlipVolumeController _volumeController;` field; `OnOptionsClicked` calls `_volumeController?.OnPanelOpen()` before `SetActive(true)`.
- `npm run unity:compile-check` green.
- Phase 1 — `BlipVolumeController` stub class + `BlipBootstrap.BlipMixer` accessor + `Slider` / `Toggle` GameObjects added in `CreateOptionsPanel`.
- Phase 2 — `Bind` + `InitListeners` wiring in `CreateOptionsPanel` + `OnPanelOpen` lifecycle hook in `OnOptionsClicked`.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | BlipVolumeController stub + mixer accessor | **TECH-235** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipVolumeController.cs` — `public sealed class BlipVolumeController : MonoBehaviour`. Fields: `private Slider _sfxSlider; private Toggle _sfxToggle; private AudioMixer _mixer;`. Methods: `public void Bind(Slider s, Toggle t)` assigns `_sfxSlider = s; _sfxToggle = t;`; `public void InitListeners()` calls `_sfxSlider.onValueChanged.AddListener(OnSliderChanged)` + `_sfxToggle.onValueChanged.AddListener(OnToggleChanged)`; empty stubs `private void OnSliderChanged(float v) {}` + `private void OnToggleChanged(bool mute) {}` + `public void OnPanelOpen() {}`. Also add `public AudioMixer BlipMixer => blipMixer;` to `BlipBootstrap.cs` after `SfxVolumeDbDefault` constant (line ~34). `npm run unity:compile-check` green. |
| T13.2 | OptionsPanel slider + toggle | **TECH-236** | Done (archived) | In `MainMenuController.CreateOptionsPanel` (line 308): expand `contentRect.sizeDelta` from `(300, 200)` to `(300, 260)` (line ~323). Add `Slider` child `new GameObject("SfxVolumeSlider")` parented to content; `RectTransform anchoredPosition = (40, -65)`, `sizeDelta = (120, 20)`; `var sfxSlider = go.AddComponent<Slider>(); sfxSlider.minValue = 0; sfxSlider.maxValue = 1; sfxSlider.value = 1; sfxSlider.wholeNumbers = false`. Add `Text` label `"SfxVolumeLabel"` at `(-55, -65)`, `sizeDelta = (90, 20)`, `text = "SFX Volume"`, `fontSize = 14`, `color = Color.white`, same `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`. Add `Toggle` child `"SfxMuteToggle"` at `(10, -100)`, `sizeDelta = (60, 20)`, `isOn = false`. Add label `"SfxMuteLabel"` at `(-45, -100)`, `text = "Mute SFX"`, same font style. Hold `sfxSlider` + `sfxToggle` as locals for Phase 2. |
| T13.3 | Bind + InitListeners wire-up | **TECH-237** | Done (archived) | In `MainMenuController.CreateOptionsPanel` replace placeholder discards `_ = sfxSlider; _ = sfxToggle;` (lines 393–394) with: `var controller = panel.AddComponent<BlipVolumeController>(); controller.Bind(sfxSlider, sfxToggle); controller.InitListeners(); _volumeController = controller;`. Add `private BlipVolumeController _volumeController;` (no `[SerializeField]`, runtime-only) after `optionsBackButton` decl (line 34). Back button (lines 396–397) and `panel.SetActive(false)` (line 399) unchanged. `npm run unity:compile-check` green. Kickoff 2026-04-16: real line numbers (back button 396 not 339, SetActive 399 not 342); insertion site is TECH-236 placeholder discards, not generic "before SetActive"; call-order rationale locked in spec Decision Log. |
| T13.4 | OnPanelOpen lifecycle hook | **TECH-238** | Done (archived) | In `MainMenuController.OnOptionsClicked` (line 569): insert `_volumeController?.OnPanelOpen();` immediately before `optionsPanel.SetActive(true)` (line 573), inside the existing `if (optionsPanel != null)` guard (single-statement `if` becomes a block). Guard is null-safe — `CreateOptionsPanel` standard path sets `_volumeController`; `?.` covers fallback / first-frame edge cases. Stub body fires lifecycle (Stage 4.2 T4.2.1 replaces with real `OnEnable` — `SetActive(true)` triggers `OnEnable` automatically so this call becomes a pre-open prime before show). Confirm `CloseOptionsPanel` (line 576) requires no symmetrical hook (`OnDisable` lifecycle covers cleanup). Kickoff 2026-04-16: real line numbers (569 / 576 not ~511 / ~517); insertion site is inside the null guard block, not before; Decision Log locks ordering (blip → prime → activate). |

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

### Stage 14 — Patches + integration + golden fixtures + promotion / Settings controller + persistence + mute semantics


**Status:** Done (TECH-243..TECH-246 all archived 2026-04-16)

**Objectives:** Fill `BlipVolumeController` logic bodies. `Awake` caches mixer via `BlipBootstrap.Instance.BlipMixer` (invariant #3). `OnEnable` primes slider/toggle from `PlayerPrefs`. `OnSliderChanged` applies dB conversion + writes `PlayerPrefs` + calls `_mixer.SetFloat`. `OnToggleChanged` clamps/restores mixer + writes mute key. Boot-time mute restore in `BlipBootstrap.Awake`. Glossary row updated.

**Exit:**

- `BlipVolumeController.Awake` caches `_mixer = BlipBootstrap.Instance?.BlipMixer`; logs warning + sets `enabled = false` if null (invariant #3 — one-time lookup, not per-frame).
- `BlipVolumeController.OnEnable` (fired on `optionsPanel.SetActive(true)`) reads `PlayerPrefs.GetFloat(BlipBootstrap.SfxVolumeDbKey, 0f)` → converts to linear (`Mathf.Pow(10f, db / 20f)`, clamped 0..1, floor 0 when db ≤ -79f) → `_sfxSlider.SetValueWithoutNotify(linear)`. Reads `PlayerPrefs.GetInt(BlipBootstrap.SfxMutedKey, 0)` → `_sfxToggle.SetValueWithoutNotify(muted != 0)`. `OnPanelOpen` stub removed (Unity `OnEnable` replaces; `MainMenuController.OnOptionsClicked` stub call removed).
- `OnSliderChanged(float v)` — `db = v > 0.0001f ? 20f * Mathf.Log10(v) : -80f`; `PlayerPrefs.SetFloat(SfxVolumeDbKey, db)`; if `!_sfxToggle.isOn` → `_mixer.SetFloat(SfxVolumeParam, db)`.
- `OnToggleChanged(bool mute)` — `PlayerPrefs.SetInt(SfxMutedKey, mute ? 1 : 0)`; if mute → `_mixer.SetFloat(SfxVolumeParam, -80f)`; else → re-read `PlayerPrefs.GetFloat(SfxVolumeDbKey, 0f)` + `_mixer.SetFloat(SfxVolumeParam, db)`.
- `BlipBootstrap.cs` — new `public const string SfxMutedKey = "BlipSfxMuted"` constant; `Awake` reads `PlayerPrefs.GetInt(SfxMutedKey, 0)` after volume read; if muted, overrides `db = -80f` before `blipMixer.SetFloat`. `npm run unity:compile-check` green.
- `ia/specs/glossary.md` **Blip bootstrap** row updated with `SfxMutedKey` boot-time restore + `BlipVolumeController` visible-UI path. `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T14.1 | Awake mixer cache + OnEnable prime | **TECH-243** | Done (archived) | Fill `BlipVolumeController.Awake` — `_mixer = BlipBootstrap.Instance?.BlipMixer; if (_mixer == null) { Debug.LogWarning("[Blip] BlipVolumeController: BlipBootstrap.BlipMixer null — volume UI disabled"); enabled = false; return; }`. Fill `OnEnable` — `float db = PlayerPrefs.GetFloat(BlipBootstrap.SfxVolumeDbKey, 0f); float linear = db <= -79f ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f)); _sfxSlider.SetValueWithoutNotify(linear); bool muted = PlayerPrefs.GetInt(BlipBootstrap.SfxMutedKey, 0) != 0; _sfxToggle.SetValueWithoutNotify(muted);`. Remove `OnPanelOpen` stub from `BlipVolumeController` + remove its call from `MainMenuController.OnOptionsClicked` (Unity `OnEnable` fires automatically on `SetActive(true)`). |
| T14.2 | Slider + Toggle handler bodies | **TECH-244** | Done (archived) | Fill `OnSliderChanged(float v)` — `float db = v > 0.0001f ? 20f * Mathf.Log10(v) : -80f; PlayerPrefs.SetFloat(BlipBootstrap.SfxVolumeDbKey, db); if (!_sfxToggle.isOn && _mixer != null) _mixer.SetFloat(BlipBootstrap.SfxVolumeParam, db);`. Fill `OnToggleChanged(bool mute)` — `PlayerPrefs.SetInt(BlipBootstrap.SfxMutedKey, mute ? 1 : 0); if (_mixer == null) return; if (mute) { _mixer.SetFloat(BlipBootstrap.SfxVolumeParam, -80f); } else { float db = PlayerPrefs.GetFloat(BlipBootstrap.SfxVolumeDbKey, 0f); _mixer.SetFloat(BlipBootstrap.SfxVolumeParam, db); }`. `npm run unity:compile-check` green. |
| T14.3 | Bootstrap mute-key + boot restore | **TECH-245** | Done (archived) | `BlipBootstrap.cs` — const `public const string SfxMutedKey = "BlipSfxMuted";` already landed at line 33 with TECH-243. Remaining: in `Awake` after `float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, SfxVolumeDbDefault)` (current line 58): insert `int muted = PlayerPrefs.GetInt(SfxMutedKey, 0); if (muted != 0) db = -80f;` before `blipMixer.SetFloat(SfxVolumeParam, db)`. Adds boot-time mute restore so muted state persists across app launches even before `BlipVolumeController.OnEnable` fires. `npm run unity:compile-check` green. |
| T14.4 | Glossary bootstrap row update | **TECH-246** | Done (archived) | `ia/specs/glossary.md` — **Blip bootstrap** row: append to definition "Boot-time: also reads `SfxMutedKey` (`PlayerPrefs.GetInt`) and clamps dB to −80 if muted, ahead of mixer apply. Visible-volume-UI path: `BlipVolumeController` (mounted on `OptionsPanel`) primes slider/toggle from `PlayerPrefs` on `OnEnable` and writes back on change." Spec cross-ref already points `ia/specs/audio-blip.md §5.1`, `§5.2` — confirm no change needed. `npm run validate:all` green. |

**Dependencies:** None. Step 4 independent of Steps 5–7.

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

---

## Step 5 — DSP kernel v2 — FX chain + LFOs + biquad BP + param smoothing (post-MVP)

**Status:** In Progress — Stage 5.3

**Backlog state (Step 5):** 0 filed

**Objectives:** Extend `BlipVoice.Render` + `BlipPatchFlat` + SO schema. Ordered FX chain (bit-crush / ring-mod / comb / allpass / chorus / flanger / soft-clip / DC blocker). Up to 2 LFOs per patch routed to pitch / gain / cutoff / pan. Biquad BP w/ Q. 1-pole 20 ms param smoothing. Unlocks Step 6 post-MVP patches — cliff thud bit-crush, terrain scrape ring-mod, tooltip LFO tremolo.

**Exit criteria:**

- `BlipPatch` SO gains `fxChain[0..3]` slots (AnimationCurve still banned per exploration §13).
- `BlipPatchFlat` + `BlipVoiceState` extended w/ FX state + LFO phase + biquad z1/z2 (blittable).
- `BlipVoice.Render` wires FX post-envelope; LFOs run per-sample; biquad BP selectable via new `BlipFilterKind.BandPass`.
- `SmoothOnePole(ref float z, float target, float coef)` helper per `docs/blip-post-mvp-extensions.md` §1.
- `BlipDelayPool` + `BlipLutPool` plain-class services land (owned by catalog per invariant #4).
- All MVP golden fixtures still pass (empty FX + zero LFOs = passthrough bit-exact against Step 3 baselines).
- No managed allocs in `Render` (existing `NoAlloc` test extended to v2 kernel).
- Glossary rows: **Blip FX chain**, **Blip LFO**, **Biquad band-pass**, **Param smoothing**, **Blip delay pool**, **Blip LUT pool**.
- `npm run unity:compile-check` + `npm run validate:all` green.
- Phase 1 — `BlipVolumeController` full logic: `Awake` mixer cache + `OnEnable` prime + `OnSliderChanged` + `OnToggleChanged` bodies.
- Phase 2 — Boot-time mute restore in `BlipBootstrap.Awake` + `SfxMutedKey` constant + glossary update.

**Art:** None — pure DSP.

**Relevant surfaces (load when step opens):**
- Step 4 outputs on disk: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` (lines 29–32: volume constants; `SfxMutedKey` added in Stage 4.2), `Assets/Scripts/Audio/Blip/BlipVolumeController.cs`.
- Step 1 DSP foundations: `Assets/Scripts/Audio/Blip/BlipVoice.cs`, `BlipPatchTypes.cs`, `BlipVoiceState.cs`, `BlipPatchFlat.cs`, `BlipPatch.cs`, `BlipEnvelope.cs`, `BlipOscillatorBank.cs`.
- Step 2/3 pipeline: `Assets/Scripts/Audio/Blip/BlipCatalog.cs`, `BlipBaker.cs`.
- Test suite: `Assets/Tests/EditMode/Audio/BlipNoAllocTests.cs`, `BlipGoldenFixtureTests.cs`, `BlipDeterminismTests.cs`.
- Design: `docs/blip-post-mvp-extensions.md` §1 (FX chain, LFOs, biquad BP, param smoothing, pool infrastructure).
- `ia/specs/audio-blip.md §4.1` (patch data model), `§4.2` (filter section — biquad BandPass lands here).
- `ia/rules/invariants.md` #4 (no new singletons — `BlipDelayPool` + `BlipLutPool` owned by `BlipCatalog`).
- New files (Step 5 output): `Assets/Scripts/Audio/Blip/BlipFxChain.cs` (new), `BlipDelayPool.cs` (new), `BlipLutPool.cs` (new).

### Stage 15 — Patches + integration + golden fixtures + promotion / FX data model + memoryless cores


**Status:** Done (all 5 tasks TECH-256..TECH-260 archived)

**Objectives:** New `BlipFxKind` enum + `BlipFxSlot` / `BlipFxSlotFlat` structs establish the per-patch FX chain data model. `BlipPatch.fxChain` + `BlipPatchFlat` inline FX fields added. `BlipVoiceState` gains per-slot FX state. New `BlipFxChain.cs` implements 4 no-delay-buffer processors (bit-crush, ring-mod, soft-clip, DC blocker); Comb/Allpass/Chorus/Flanger return passthrough stubs until Stage 5.2. `BlipVoice.Render` FX loop wired post-envelope — empty chain = passthrough, MVP golden fixtures unaffected.

**Exit:**

- `BlipFxKind` enum in `BlipPatchTypes.cs`: None=0/BitCrush=1/RingMod=2/SoftClip=3/DcBlocker=4/Comb=5/Allpass=6/Chorus=7/Flanger=8 (full set; delay-line kinds implemented in Stage 5.2).
- `BlipFxSlot [Serializable] struct` (BlipFxKind kind; float param0, param1, param2) + `BlipFxSlotFlat readonly struct` (mirrors scalars, blittable) — both in `BlipPatchTypes.cs`.
- `BlipPatch` gains `[SerializeField] private BlipFxSlot[] fxChain` (max 4, truncated in `OnValidate`); `BlipPatchFlat` gains `BlipFxSlotFlat fx0,fx1,fx2,fx3` + `int fxSlotCount` inline (matching oscillator inline-triplet pattern at lines 170–181 of `BlipPatchFlat.cs`) + ctor extension.
- `BlipVoiceState` gains `float dcZ1_0..3` (DC blocker per-slot input z-1), `float dcY1_0..3` (DC blocker output z-1), `float ringModPhase_0..3` (ring-mod carrier phase 0..2π). All blittable.
- `Assets/Scripts/Audio/Blip/BlipFxChain.cs` (new): `internal static class BlipFxChain` with `ProcessFx(ref float x, BlipFxKind kind, float p0, float p1, ref float dcZ1, ref float dcY1, ref float ringPhase, int sampleRate)`: BitCrush/RingMod/SoftClip/DcBlocker implemented; Comb/Allpass/Chorus/Flanger return passthrough. Zero allocs; no Unity API.
- `BlipVoice.Render` post-envelope FX loop: unrolled 4-slot dispatch; `BlipNoAllocTests` still green.
- Phase 1 — Types + data model: `BlipFxKind` / `BlipFxSlot` / `BlipFxSlotFlat` in `BlipPatchTypes.cs`; `BlipPatch.fxChain` + `BlipPatchFlat` FX inline fields; `BlipVoiceState` FX state extension.
- Phase 2 — FX kernel + render wire: `BlipFxChain.cs` memoryless cores + `BlipVoice.Render` FX loop + `BlipNoAllocTests` FX variant.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T15.1 | FX types | **TECH-256** | Done | `BlipFxKind` enum (None=0/BitCrush=1/RingMod=2/SoftClip=3/DcBlocker=4/Comb=5/Allpass=6/Chorus=7/Flanger=8) + `BlipFxSlot [Serializable] struct` (BlipFxKind kind; float param0, param1, param2) + `BlipFxSlotFlat readonly struct` (mirrors scalars, blittable copy ctor) — all added to `BlipPatchTypes.cs`. |
| T15.2 | BlipPatch fxChain + BlipPatchFlat FX inline | **TECH-257** | Done (archived) | `BlipPatch` gains `[SerializeField] private BlipFxSlot[] fxChain`; `OnValidate` truncates to max 4 slots. `BlipPatchFlat` gains `BlipFxSlotFlat fx0,fx1,fx2,fx3` + `int fxSlotCount` inline (matching oscillator inline-triplet at lines 170–181 of `BlipPatchFlat.cs`). `BlipPatchFlat(BlipPatch so, …)` ctor extended to flatten `fxChain`. |
| T15.3 | BlipVoiceState FX state fields | **TECH-258** | Done (archived) | `BlipVoiceState` extended: `float dcZ1_0, dcZ1_1, dcZ1_2, dcZ1_3` (DC blocker input z-1 per slot) + `float dcY1_0, dcY1_1, dcY1_2, dcY1_3` (DC blocker output z-1) + `float ringModPhase_0, ringModPhase_1, ringModPhase_2, ringModPhase_3` (ring-mod carrier phase). All blittable. Delay write-heads (`delayWritePos_N`) land in Stage 5.2 T5.2.1; LFO phases in Stage 5.3 T5.3.2. |
| T15.4 | BlipFxChain.cs memoryless cores | **TECH-259** | Done (archived) | New `Assets/Scripts/Audio/Blip/BlipFxChain.cs`: `internal static class BlipFxChain`. `static void ProcessFx(ref float x, BlipFxKind kind, float p0, float p1, ref float dcZ1, ref float dcY1, ref float ringPhase, int sampleRate)`: BitCrush `x=Mathf.Round(x*steps)/steps, steps=1<<(int)p0`; RingMod `ringPhase+=2π*p0/sampleRate; x*=Mathf.Sin(ringPhase)`; SoftClip `x=x/(1f+Mathf.Abs(x))`; DcBlocker `float y=x-dcZ1+0.9995f*dcY1; dcZ1=x; dcY1=y; x=y`; Comb/Allpass/Chorus/Flanger → passthrough (stubs). Zero allocs; no Unity API. |
| T15.5 | BlipVoice.Render FX loop + NoAlloc extension | **TECH-260** | Done (archived) | Post-envelope FX dispatch in `BlipVoice.Render`: unrolled `if (patch.fxSlotCount >= 1) BlipFxChain.ProcessFx(ref sample, patch.fx0.kind, patch.fx0.param0, patch.fx0.param1, ref state.dcZ1_0, ref state.dcY1_0, ref state.ringModPhase_0, sampleRate)` … (4 slots, no array alloc). Empty chain (`fxSlotCount=0`) fast-exits. `BlipNoAllocTests` gains `Render_WithFxChain_ZeroManagedAlloc` — 2-slot BitCrush+DcBlocker patch; assert delta/call ≤ 0. |

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

### Stage 16 — Patches + integration + golden fixtures + promotion / Delay-line FX + BlipDelayPool


**Status:** Done (closed 2026-04-17 — TECH-270..TECH-275 all archived)

**Objectives:** `BlipDelayPool` plain-class service (owned by `BlipCatalog`) allocates float[] delay-line buffers outside `Render` — zero alloc in hot path. Implement comb, allpass, chorus, flanger in `BlipFxChain.ProcessFx` replacing Stage 5.1 stubs. `BlipVoice.Render` gains nullable delay buffer params via new overload.

**Exit:**

- `Assets/Scripts/Audio/Blip/BlipDelayPool.cs` (new): `internal sealed class BlipDelayPool` — `float[] Lease(int sampleRate, float maxDelayMs)` + `void Return(float[])` via `ArrayPool<float>.Shared`. `BlipCatalog` gains `private BlipDelayPool _delayPool = new BlipDelayPool()` (init in `Awake`; invariant #4 compliant).
- `BlipVoiceState` gains `int delayWritePos_0, delayWritePos_1, delayWritePos_2, delayWritePos_3` (circular write-head per FX slot, blittable).
- `BlipVoice` gains new `Render` overload with `float[]? d0, float[]? d1, float[]? d2, float[]? d3` nullable delay params; existing 7-param signature delegates with all-null. `BlipFxChain.ProcessFx` signature extended with `float[]? delayBuf, int bufLen, ref int writePos`.
- `BlipBaker.BakeOrGet` pre-leases delay buffers before `Render`; returns in `finally`.
- Comb: `y=x+g*d[(wp-D+len)%len]; d[wp]=x; wp=(wp+1)%len`; `g=p1` clamped 0..0.97, `D=(int)(p0/1000f*sampleRate)`. Allpass: Schroeder `v=d[(wp-D+len)%len]; d[wp]=x+g*v; y=v-g*d[wp]; wp=(wp+1)%len`.
- Chorus: 2-tap LFO-modulated delay (rate `p0` Hz, depth `p1` ms, mix `p2`). Flanger: same structure, depth 1–10 ms.
- `BlipNoAllocTests` gains chorus patch variant; buffers pre-leased outside measurement window; assert delta/call ≤ 0.
- Phase 1 — `BlipDelayPool` service + `BlipVoiceState` write-heads + `BlipCatalog` ownership + `BlipVoice.Render` overload + `BlipBaker` call-site.
- Phase 2 — Comb + allpass kernels in `BlipFxChain.ProcessFx`.
- Phase 3 — Chorus + flanger kernels + `BlipNoAllocTests` delay-FX variant.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T16.1 | BlipDelayPool + catalog wiring + VoiceState write-heads | **TECH-270** | Done (archived) | New `Assets/Scripts/Audio/Blip/BlipDelayPool.cs`: `internal sealed class BlipDelayPool` with `float[] Lease(int sampleRate, float maxDelayMs)` (sizes to `(int)Math.Ceiling(maxDelayMs/1000f*sampleRate)+1`; delegates to `ArrayPool<float>.Shared.Rent`) + `void Return(float[])`. `BlipCatalog` gains `private BlipDelayPool _delayPool = new BlipDelayPool()`. `BlipVoiceState` gains `int delayWritePos_0, delayWritePos_1, delayWritePos_2, delayWritePos_3`. |
| T16.2 | BlipVoice.Render delay overload + BlipBaker lease | **TECH-271** | Done (archived) | `BlipVoice` gains `Render` overload with `float[]? d0, float[]? d1, float[]? d2, float[]? d3`; existing 7-param overload delegates with all-null (backward compat shim). `BlipFxChain.ProcessFx` extended: `float[]? delayBuf, int bufLen, ref int writePos` params (null = skip delay op). `BlipBaker.BakeOrGet` pre-leases up to 4 buffers from `_catalog._delayPool`; passes to `Render`; returns in `finally`. |
| T16.3 | Comb filter kernel | **TECH-272** | Done (archived) | `BlipFxChain.ProcessFx` Comb case: feedback comb `y=x+g*d[(wp-D+len)%len]; d[wp]=x; wp=(wp+1)%len`; `D=(int)(p0/1000f*sampleRate)`, `g=p1` clamped 0..0.97 (enforce in `BlipPatch.OnValidate` for Comb slots). EditMode test `BlipFxChainTests.Comb_FeedbackAttenuation`: impulse, 10 ms delay, g=0.5 — 2nd echo amplitude ≈ 0.5 ± 0.05 relative to 1st. |
| T16.4 | Allpass filter kernel | **TECH-273** | Done (archived) | `BlipFxChain.ProcessFx` Allpass case: Schroeder `v=d[(wp-D+len)%len]; d[wp]=x+g*v; y=v-g*d[wp]; wp=(wp+1)%len`. EditMode test `BlipFxChainTests.Allpass_FlatMagnitude`: 1024 samples pink noise through allpass, assert RMS output ≈ RMS input ± 15% (flat magnitude response of ideal allpass). |
| T16.5 | Chorus + flanger kernels | **TECH-274** | Done (archived) | Chorus (`BlipFxChain.ProcessFx` Chorus case): 2-tap read at `offset±(p1_samples*sin(ringModPhase_N))`; write input; output `=(1-p2)*x+p2*0.5*(tap0+tap1)`; `ringModPhase_N+=2π*p0/sampleRate` (ring-mod phase repurposed for LFO — ring-mod and chorus/flanger are mutually exclusive per slot; enforced in `BlipPatch.OnValidate`). Flanger: same, depth clamped 1..10 ms. |
| T16.6 | NoAlloc delay-FX test + Render overload clean-up | **TECH-275** | Done (archived) | `BlipNoAllocTests.Render_WithChorus_ZeroManagedAlloc`: pre-lease 1 chorus delay buf outside `GC.GetAllocatedBytesForCurrentThread` window; 10 renders; assert delta/call ≤ 0. Confirm 7-param `BlipVoice.Render` overload still compiles; `BlipBakerTests` + `BlipDeterminismTests` suites still green after overload addition. |

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

### Stage 18 — Patches + integration + golden fixtures + promotion / Biquad BP + integration + golden-fixture regression gate


**Status:** In Progress — 4 tasks filed 2026-04-18 (TECH-434..TECH-437)

**Objectives:** `BlipFilterKind.BandPass` 2nd-order biquad selectable via `resonanceQ`. Integration smoke: all 10 MVP golden fixture hashes pass (passthrough invariant with empty FX + zero LFOs). All 6 Step 5 glossary rows landed and spec updated.

**Exit:**

- `BlipFilterKind.BandPass = 2` in `BlipPatchTypes.cs`; `BlipFilter` + `BlipFilterFlat` gain `float resonanceQ` (clamped 0.1..20 in `OnValidate`).
- `BlipVoiceState` gains `float biquadZ1, biquadZ2` (DF-II transposed delay elements, blittable).
- Biquad BP coefficients pre-computed once per `Render` invocation (1 `Math.Sin` + 1 `Math.Cos`; zero per-sample trig): `w0=2π*cutoffHz/sr; α=sin(w0)/(2Q); b0n=sin(w0)/2/(1+α); a1n=-2cos(w0)/(1+α); a2n=(1-α)/(1+α)`.
- `BlipVoice.Render` BandPass per-sample: DF-II transposed `v=x-a1n*z1-a2n*z2; y=b0n*v-b0n*z2; z2=z1; z1=v` (b1n=0 for bandpass). LP + None unchanged.
- All 10 MVP golden fixture hashes pass (`BlipGoldenFixtureTests` green — empty FX + zero LFOs + LowPass/None = passthrough bit-exact vs Step 3 baselines).
- `BlipNoAllocTests` gains `Render_WithBiquadBP_ZeroManagedAlloc`; assert delta/call ≤ 0.
- 6 glossary rows: **Blip FX chain**, **Blip LFO**, **Biquad band-pass**, **Param smoothing**, **Blip delay pool**, **Blip LUT pool** to `ia/specs/glossary.md` + cross-refs to `ia/specs/audio-blip.md`. `ia/specs/audio-blip.md §4.2` filter section updated: BandPass enum value + `resonanceQ` noted.
- `npm run unity:compile-check` + `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T18.1 | Biquad data model + BlipVoiceState delay elements | **TECH-434** | Draft | `BlipFilterKind.BandPass = 2` in `BlipPatchTypes.cs`. `BlipFilter` gains `public float resonanceQ` (clamped 0.1..20 in `BlipPatch.OnValidate`). `BlipFilterFlat` gains `public readonly float resonanceQ`; `BlipFilterFlat(BlipFilter src)` ctor copies it. `BlipVoiceState` gains `float biquadZ1, biquadZ2`. `BlipPatchFlat(BlipPatch so, …)` ctor copies `resonanceQ` through the new `BlipFilterFlat` field. |
| T18.2 | Biquad coefficient pre-compute block | **TECH-435** | Draft | Biquad BP pre-compute in `BlipVoice.Render` (outside sample loop, alongside existing `alpha` LP block at lines 59–71 of `BlipVoice.cs`): `double w0=TwoPi*cutoffHz/sampleRate; float sinW=(float)Math.Sin(w0); float cosW=(float)Math.Cos(w0); float alp=sinW/(2f*Q); float b0n=sinW*0.5f/(1f+alp); float a1n=-2f*cosW/(1f+alp); float a2n=(1f-alp)/(1f+alp)`. Computed only when `filter.kind == BandPass`; LP/None branches unchanged. |
| T18.3 | Biquad kernel in Render + NoAlloc BP test | **TECH-436** | Draft | `BlipVoice.Render` per-sample BandPass dispatch: `float v=x-a1n*state.biquadZ1-a2n*state.biquadZ2; float y=b0n*v-b0n*state.biquadZ2; state.biquadZ2=state.biquadZ1; state.biquadZ1=v; sample=y`. `BlipNoAllocTests.Render_WithBiquadBP_ZeroManagedAlloc`: BP patch (cutoffHz=1000, Q=2, deterministic) — 3 warm-up + 10 measured renders; assert delta/call ≤ 0. |
| T18.4 | Golden fixture regression + spec + all 6 glossary rows | **TECH-437** | Draft | Confirm `BlipGoldenFixtureTests` all 10 MVP hashes pass (empty FX chain + zero LFOs + None/LowPass filter = passthrough). 6 glossary rows to `ia/specs/glossary.md`: **Blip FX chain** (`BlipFxChain.ProcessFx` ordered per-patch FX processors), **Blip LFO** (`BlipLfo`/`BlipLfoFlat` per-sample modulator), **Biquad band-pass** (`BlipFilterKind.BandPass` DF-II transposed 2nd-order BP), **Param smoothing** (`BlipVoice.SmoothOnePole` 20 ms 1-pole), **Blip delay pool** (`BlipDelayPool` float[] lease service), **Blip LUT pool** (`BlipLutPool` stub). `ia/specs/audio-blip.md §4.2`: BandPass enum value + `resonanceQ`. `npm run validate:all` green. |

**Dependencies:** Step 1 Done. Ships BEFORE Step 6 (patches depend on FX / LFO / biquad surfaces).

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---

## Step 6 — 10 post-MVP sound patches + call sites (post-MVP)

**Status:** Draft (tasks _pending_ — not yet filed).

**Objectives:** Author 10 additional patches + enum rows + catalog entries + call-site wiring. Catalog 10 → 20 sounds. Covers MVP gaps (tab switch, tooltip appear, demolish, road erase, water paint, terrain raise/lower, cliff created, multi-select step, load game). Leans on Step 5 kernel v2 (cliff thud needs bit-crush; terrain scrape needs ring-mod; tooltip needs LFO tremolo).

**Exit criteria:**

- `BlipId` enum gains 10 rows: `UiTabSwitch`, `UiTooltipAppear`, `ToolRoadErase`, `ToolDemolish`, `ToolWaterPaint`, `ToolTerrainRaise`, `ToolTerrainLower`, `WorldCliffCreated`, `WorldMultiSelectStep`, `SysLoadGame`.
- `Assets/Audio/Blip/Patches/` gains 10 SO assets authored per `docs/blip-post-mvp-extensions.md` §3 recipes.
- `BlipCatalog.entries[]` → 20 rows; mixer-group assignments match `docs/blip-post-mvp-extensions.md` §3 table.
- Call sites fire `BlipEngine.Play(id)` at respective tool / UI hosts (tab switcher, tooltip controller, demolish tool, road-erase tool, water-paint tool, terrain up/down tools, cliff generator, multi-select controller, `GameSaveManager.LoadGame`).
- Cliff thud debounced per terrain-refresh batch (one play per batch, not per cliff cell).
- Multi-select rate-limited via `BlipCooldownRegistry` 125 ms (8 Hz cap).
- Golden fixtures extended to 20 ids; `tools/scripts/blip-bake-fixtures.ts` regenerated; `BlipGoldenFixtureTests` parameterized over full set.
- `npm run unity:compile-check` + `npm run validate:all` green.
- Glossary: new rows for any Step-6-introduced terms (e.g. **Cliff thud debounce**).

**Art:** None — parameter-only patches.

**Relevant surfaces:** `Assets/Scripts/Audio/Blip/BlipId.cs`, `BlipCatalog.cs`, `Assets/Audio/Blip/Patches/*.asset`, tool / UI / world call-site hosts (enumerated at stage decompose), `tools/scripts/blip-bake-fixtures.ts`, `tools/fixtures/blip/*.json`, `docs/blip-post-mvp-extensions.md` §3.

**Stages (skeleton — decompose via `/stage-decompose` when Step → `In Progress`):**

- Stage 6.1 — UI lane (tab switch, tooltip appear).
- Stage 6.2 — Tool lane (demolish, road erase, water paint, terrain raise, terrain lower).
- Stage 6.3 — World lane (cliff created w/ batch debounce; multi-select step w/ 8 Hz cap).
- Stage 6.4 — Sys lane + golden-fixture + catalog + glossary closeout (load game, bake regen, test expansion, glossary rows).

**Dependencies:** Step 5 closed. Stage 3.4 spec promotion closed (Step 6 call sites reference canonical `ia/specs/audio-blip.md`). Multi-scale `WorldCellSelected` per-scale variants stay OUT of this Step — they land via multi-scale orchestrator coupling, not here.

---

## Step 7 — BlipPatchEditorWindow — waveform / spectrum / LUFS / A-B compare (post-MVP)

**Status:** Draft (tasks _pending_ — not yet filed).

**Objectives:** Custom `EditorWindow` replaces Inspector authoring once 20-patch catalog + FX + LFO + biquad surfaces make Inspector tuning painful. Waveform preview (1 s offline render), spectrum FFT, LUFS meter (simplified EBU R128 mono), A/B compare across two patches, auto-rebake on SO dirty, patch-hash live readout. Overrides exploration §13 "Inspector only" lock — gate documented in Decision Log.

**Exit criteria:**

- `Assets/Editor/Blip/BlipPatchEditorWindow.cs` w/ `Territory/Audio/Blip Patch Editor` menu item.
- Window panels: waveform oscilloscope, spectrum FFT (power-of-two bins), LUFS meter (momentary + integrated readouts), A/B dropdown w/ side-by-side waveform.
- Preview renders offline via `BlipVoice.Render` → `AudioClip` → hidden Editor `AudioSource` (no runtime `BlipEngine` dependency).
- Auto-rebake on `OnValidate` broadcast from `BlipPatch.OnValidate`.
- Patch-hash live readout mirrors `BlipPatchFlat.patchHash` used by golden fixture test.
- New `Assets/Editor/Blip/Blip.Editor.asmdef` (editor-only, depends on `Blip.asmdef` + `Blip.Tests.EditMode.asmdef` helpers).
- Glossary row: **Blip patch editor window**.
- `npm run unity:compile-check` green.
- Phase 1 — Biquad data model: `BlipFilterKind.BandPass` enum value + `resonanceQ` field + `BlipVoiceState` delay elements + coefficient pre-compute block.
- Phase 2 — Biquad kernel in `Render` + `BlipNoAllocTests` BP variant + golden fixture regression + spec + all 6 glossary rows.

**Art:** None — editor tooling only.

**Relevant surfaces:** `Assets/Editor/Blip/BlipPatchEditorWindow.cs` (new), `Assets/Editor/Blip/Blip.Editor.asmdef` (new), `BlipPatch.cs` `OnValidate` broadcast hook, `BlipTestFixtures.RenderPatch` (reuse), `docs/blip-post-mvp-extensions.md` §5.

**Stages (skeleton — decompose via `/stage-decompose` when Step → `In Progress`):**

- Stage 7.1 — Editor asmdef + window shell + offline preview + auto-rebake hook.
- Stage 7.2 — Waveform + spectrum + LUFS panels.
- Stage 7.3 — A/B compare + polish + glossary row.

**Dependencies:** Step 6 closed (20-patch pain threshold — Decision Log documents override of §13 "Inspector only"). Step 5 closed (FX / LFO / biquad surfaces to visualize).

---

## Deferred decomposition

- **Step 2 — Bake + facade + PlayMode smoke:** decomposed 2026-04-15. Stages: Bake-to-clip pipeline, Catalog + mixer router + cooldown registry + player pool, BlipEngine facade + main-thread gate, PlayMode smoke test.
- **Step 3 — Patches + integration + golden fixtures + promotion:** decomposed 2026-04-15. Stages: Patch authoring + catalog wiring, UI + Eco + Sys call sites, World lane call sites, Golden fixtures + spec promotion + glossary.
- **Step 4 — Settings UI + volume controls:** decomposed 2026-04-16. Stages: Options panel UI (slider + mute toggle + controller stub), Settings controller + persistence + mute semantics.
- **Step 5 — DSP kernel v2 — FX chain + LFOs + biquad BP + param smoothing:** decomposed 2026-04-16. Stages: FX data model + memoryless cores, Delay-line FX + BlipDelayPool, LFOs + routing matrix + param smoothing, Biquad BP + integration + golden-fixture regression gate.
- **Step 6 — 10 post-MVP sound patches + call sites:** skeleton only (2026-04-16). Stages named (UI lane; Tool lane; World lane; Sys lane + golden-fixture + catalog + glossary closeout); decompose via `/stage-decompose` when Step → `In Progress` AND Step 5 closed.
- **Step 7 — BlipPatchEditorWindow:** skeleton only (2026-04-16). Stages named (Editor asmdef + window shell + preview + auto-rebake; Waveform + spectrum + LUFS; A/B compare + polish); decompose via `/stage-decompose` when Step → `In Progress` AND Step 6 closed.

Do NOT pre-file Step 3–7 BACKLOG rows. Candidate-issue pointers live inline on each step's **Relevant surfaces** line; new-feature-row candidates surface during that step's decomposition pass, filed under `§ Audio / Blip lane` in `BACKLOG.md`.

Step 1 + Step 2 stages decomposed above w/ phases + tasks. Steps 4–7 carry stage names only — phases + tasks decompose lazily. Use `stage-file` skill to create BACKLOG rows + project spec stubs when a given stage → `In Progress`.

---

## Orchestration guardrails

**Do:**

- Propose edits to step / stage skeletons when a phase exposes missing load-bearing item (e.g. Stage 1.3 reveals need for extra voice-state field → edit stage objectives + add task).
- Push MVP-scope-creep into `docs/blip-post-mvp-extensions.md`. Edits to that doc are cheap; edits to MVP stages require explicit re-decision against exploration §13.
- Create Stage 2.x / Stage 3.x orchestrator content lazily when parent step → `In Progress`.
- Keep task rows `_pending_` until `stage-file` runs for that stage. Never hand-author BACKLOG rows ahead of stage open.

**Do not:**

- Resurrect Live DSP path (`BlipLiveHost`, `OnAudioFilterRead`, `BlipEventQueue`, `PlayLoop`, `BlipHandle`) inside MVP stages. Entire surface deferred to post-MVP per exploration §13 + §15.
- Resurrect FX chain, LFOs, biquad BP filter, param smoothing, LUT oscillators, voice-steal crossfade, cache pre-warm, `BlipLutPool` / `BlipDelayPool` inside MVP. All post-MVP.
- Add spatialization (`BlipEngine.PlayAt`) to MVP API surface. Flat stereo only.
- Add sounds beyond the 10 MVP list (exploration §14). 11th sound → post-MVP extensions list first.
- Introduce custom `EditorWindow` w/ waveform preview / spectrum / LUFS / A/B compare inside MVP. Inspector-only authoring per exploration §13.
- Rely on byte-equality cross-platform determinism in MVP golden fixtures. Use sum-of-abs tolerance hash. LUT-osc bit-exact path lands post-MVP.
- Bypass `BlipEngine` main-thread assert. Background-thread `Play` = bug. Enforced at facade entry.
- Violate invariant #3 — `BlipEngine` caches `BlipCatalog` / `BlipPlayer` refs after first lookup; no `FindObjectOfType` in per-frame paths.
- Violate invariant #4 — `BlipEngine` is a static facade (stateless dispatch); all state lives on MonoBehaviour hosts under `BlipBootstrap`. Not a singleton pattern.
- File BACKLOG rows for future-step Blip FEAT ideas outside an open stage. Use `docs/blip-post-mvp-extensions.md` as the holding pen.
- Give time estimates on steps / stages / phases / tasks.
- Close this orchestrator via `/closeout` — orchestrators are permanent per `ia/rules/orchestrator-vs-spec.md`. Individual task specs close normally; stages close via the `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`); the umbrella orchestrator never deletes.

---

## Decision Log

> **Pattern:** append rows as stages close (via the `/closeout` pair) or when orchestrator-level pivots surface in task authoring. Format: `{YYYY-MM-DD} — {short title}. {1–3 sentence rationale}. Source: {task id | stage id | author | review}.`

- `2026-04-13 — MVP drops BlipMode enum.` Single implicit baked path for MVP. `BlipMode` enum re-lands post-MVP when `BlipLiveHost` + `OnAudioFilterRead` Live DSP path ships. Source: pre-implementation review of this orchestrator.
- `2026-04-13 — BlipMixerRouter parallel to BlipCatalog.` `BlipPatchFlat` must stay blittable (no managed refs → no `AudioMixerGroup` in flat struct). `BlipMixerRouter` holds `BlipId → AudioMixerGroup` map built at `BlipCatalog.Awake` from authoring-only `BlipPatch.mixerGroup` ref. Source: pre-implementation review.
- `2026-04-13 — BlipCooldownRegistry lives on BlipCatalog.` Instance field on MonoBehaviour host (plain class, owned by catalog) — not static — to honor invariant #4 (no new singletons). `BlipEngine.Play` queries via cached catalog ref. Source: pre-implementation review.
- `2026-04-13 — MVP Settings UI deferred; headless PlayerPrefs binding.` `BlipBootstrap.Awake` reads `BlipSfxVolumeDb` from `PlayerPrefs` + calls `AudioMixer.SetFloat("SfxVolume", db)`. Visible slider + mute toggle post-MVP per `docs/blip-post-mvp-extensions.md` §4. Source: repo audit (no existing Settings surface found).
- `2026-04-13 — Boot scene = MainMenu.unity (build index 0).` `BlipBootstrap` prefab placed at root of `MainMenu.unity`; survives load via `DontDestroyOnLoad(transform.root.gameObject)` per `GameNotificationManager.cs` pattern. Source: `MainMenuController.cs` reads `SceneManager.LoadScene(MainSceneBuildIndex)`.
- `2026-04-13 — Determinism test uses sum-of-abs tolerance + first-256 byte gate.` Byte-equality on full buffer brittle against JIT / `Math.Sin` LSB drift. Sum-of-abs hash within 1e-6 epsilon + first-256-samples byte-equal (cheap early-signal gate) gives deterministic regression signal without platform-brittleness. Bit-exact path post-MVP w/ LUT oscillators per `docs/blip-post-mvp-extensions.md` §1. Source: pre-implementation research.
- `2026-04-13 — IAudioGenerator not available (Unity 2022.3.62f3).` Unity 6.3 LTS introduces `IAudioGenerator` cleanup for live DSP; current project on Unity 2022.3 so bake-to-clip stays MVP path + `OnAudioFilterRead` remains post-MVP Live DSP path. Revisit on engine upgrade. Source: `ProjectSettings/ProjectVersion.txt` + Unity 6 research.
- `2026-04-13 — AHDSR per-stage shape enum (Linear | Exponential).` Exponential shape (`1 - exp(-t/τ)` on attack; τ = stageDuration/4) reads perceptually linear per ear's log loudness response. Keeps scope tight (no curves) while giving natural-sounding envelopes. Source: audio perception literature.
- `2026-04-13 — OnValidate clamps attack/decay/release ≥ 1 ms.` Prevents snap-onset click at default 48 kHz mix rate (≈48-sample ramp floor). Source: DSP best practice for step-free transitions.
- `2026-04-14 — Stage 1.3 closed; TECH-121 + TECH-122 compressed into TECH-135.` Render-driver and per-invocation jitter were originally two separate tasks (T1.3.6 / T1.3.7); merged into single TECH-135 during implementation because jitter is computed inside the same per-sample loop — splitting produced no useful parallel track. Compression approved during stage execution; spec updated in-place. Source: Stage 1.3 project-stage-close.
- `2026-04-16 — Step 4 (Settings UI + volume controls) selected.` Smallest-blast-radius post-MVP win — replaces today's headless `PlayerPrefs` binding w/ player-visible slider + mute toggle. Independent of Steps 5–7, ships anytime. Source: post-MVP expansion review (handoff 1).
- `2026-04-16 — Step 5 (DSP kernel v2) selected; must precede Step 6.` FX chain + LFOs + biquad BP + param smoothing unlock cliff bit-crush, terrain ring-mod, tooltip LFO tremolo in Step 6 patches. Gate held (not merged into Step 6) to avoid double-fixture churn when kernel change lands alongside 10 new patches. Source: post-MVP expansion review.
- `2026-04-16 — Step 6 (10 post-MVP patches + call sites) selected.` Highest player-audible impact — doubles catalog 10 → 20 + fills MVP gaps (demolish, tooltip, terrain, cliff, multi-select, load). Depends on Step 5 (FX surfaces) + Stage 3.4 (spec promotion). Source: post-MVP expansion review.
- `2026-04-16 — Step 7 (BlipPatchEditorWindow) selected; gated on 20-patch pain.` Overrides exploration §13 "Inspector only" lock once 20 patches × FX chain × LFO routing × biquad params make Inspector tuning untenable. Gate explicit via Step 6 closed + Step 5 closed deps. Source: post-MVP expansion review.
- `2026-04-16 — Live DSP path (candidate #4 — BlipLiveHost / OnAudioFilterRead) rejected for post-MVP pick.` No MVP or Step 6 sound requires live voice modulation post-trigger. Unity 6.3 `IAudioGenerator` revisit-on-upgrade per Decision Log 2026-04-13. Deferred to future orchestrator pass. Source: post-MVP expansion review.
- `2026-04-16 — LUT osc + voice-steal crossfade + cache pre-warm (candidate #5) rejected.` Internal quality polish — not user-facing. Sum-of-abs golden fixture (Decision Log 2026-04-13) covers regression w/o bit-exact LUT path. Deferred. Source: post-MVP expansion review.
- `2026-04-16 — Multi-scale WorldCellSelected variants + SysScaleTransition (candidate #7) deferred.` Sibling `multi-scale-master-plan.md` Step 3 still Draft (decomposition deferred until Step 2 → Final). Couple via future Step when multi-scale Step 3/4 → `In Progress`; not this pass. Source: post-MVP expansion review + `multi-scale-master-plan.md` Step 3 status.
- `2026-04-16 — "CI headless bake integration tests" rejected as standalone Step.` Hard constraint per handoff — existing test infra (`Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` from Stage 3.4 + `tools/scripts/blip-bake-fixtures.ts`) already covers bake determinism. Folds into Stage 6.4 task-level asks when catalog grows to 20. Source: handoff 1 hard constraint.
- `2026-04-16 — Stage 3.4 T3.4.3: exploration doc promoted to ia/specs/audio-blip.md.` Canonical DSP kernel + architecture + invariants now under `ia/specs/`; exploration doc retains §9 recipe tables + §10–§12 live-DSP sketches + §13 locked decisions + §15 post-MVP extensions as historical / implementer reference. `docs/blip-procedural-sfx-exploration.md` gains "Superseded by" banner. Source: TECH-229 closeout.

## Lessons Learned

> **Pattern:** append rows as stages close, migrate actionable ones to canonical IA (`ia/specs/`, `ia/rules/`, glossary) via the `/closeout` pair. Keep the lesson here if it's orchestrator-local (applies only inside Blip MVP); promote if it generalizes. Format: `{YYYY-MM-DD} — {short title}. {1–3 sentence summary}. {Action: where promoted, or "orchestrator-local"}.`

- `2026-04-14 — Compress co-located tasks before filing.` When two pending tasks share the same implementation surface (same file, same loop), merge them into one TECH issue at stage-file time rather than filing both then closing one early. Avoids orphan issues + simplifies history. Action: orchestrator-local (Blip MVP).
- `2026-04-14 — BlipVoiceState carries all per-voice mutable DSP state.` `phaseA..D`, `envLevel`, `envStage`, `filterZ1`, `rngState`, `samplesElapsed` all live in a single blittable struct passed by ref — no statics, no heap alloc inside `Render`. Pattern validated by Stage 1.3; reuse for any future voice-type addition (e.g. `BlipLiveHost` post-MVP). Action: promoted to `ia/specs/audio-blip.md` §3 DSP kernel (TECH-229).
- `2026-04-14 — Exponential τ = stageDuration/4 gives ≈98 % settled at stage end.` Validated analytically (`exp(-4) ≈ 0.018`). No tuning pass required for MVP; perceptual loudness log curve satisfied. Action: orchestrator-local (Blip MVP).

