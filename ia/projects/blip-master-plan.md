# Blip — Master Plan (MVP)

> **Last updated:** 2026-04-15
>
> **Status:** In Progress — Step 1 Done; Step 2 Final (closed 2026-04-15); Step 3 decomposed 2026-04-15 — 4 stages · 8 phases · 16 tasks (all `_pending_`)
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

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — DSP foundations + audio infra

**Status:** In Progress — Stage 1.4

**Objectives:** Land scaffolding. Audio mixer asset + persistent bootstrap prefab. Authoring data model (`BlipPatch` ScriptableObject + `BlipPatchFlat` blittable mirror + content-hash). DSP kernel (`BlipVoice.Render`) w/ MVP oscillator set + AHDSR envelope + one-pole LP filter. EditMode tests gate kernel behavior + determinism. No playback wiring yet; no sounds heard in game.

**Exit criteria:**

- `Assets/Audio/BlipMixer.mixer` w/ three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) + master SFX volume exposed param.
- `BlipBootstrap` prefab in boot scene; `DontDestroyOnLoad`; carries Catalog + Player host slots (empty until Step 2).
- `BlipPatch` SO w/ MVP fields + Inspector-authorable.
- `BlipPatchFlat` blittable readonly struct mirrors SO scalars; no managed refs; `patchHash` deterministic over serialized fields.
- `BlipVoice.Render(Span<float>, int, int, int, in BlipPatchFlat, int, ref BlipVoiceState)` kernel w/ oscillators (sine, triangle, square, pulse, noise-white), AHDSR envelope, one-pole LP filter. No FX chain, no LFOs, no curves.
- EditMode tests pass: envelope stage transitions, oscillator zero-crossings at target freq, silence when `gainMult = 0`, determinism (same seed + patch → identical buffer).
- `npm run unity:compile-check` green.
- Glossary rows land for MVP terms (Blip, Blip patch, Blip patch flat, Blip voice, Blip mixer group, patch hash).

**Art:** None. Code + SOs + tests only.

**Relevant surfaces (load when step opens):**
- `docs/blip-procedural-sfx-exploration.md` §7 (architecture), §11 (names registry), §14 (MVP scope).
- `ia/rules/invariants.md` (#3 no `FindObjectOfType` in hot loops, #4 no new singletons).
- New namespaces: `Assets/Audio/` (mixer + bootstrap prefab), `Assets/Scripts/Audio/Blip/` (runtime code), `Assets/Tests/EditMode/Audio/` (test asmdef).

#### Stage 1.1 — Audio infrastructure + persistent bootstrap

**Status:** In Progress — all tasks archived (TECH-98..TECH-101 Done)

**Objectives:** Mixer asset + three routing groups wired. `BlipBootstrap` prefab instantiated in `MainMenu.unity` boot scene, survives scene loads. Headless SFX volume binding via `PlayerPrefs` → `AudioMixer.SetFloat` at `BlipBootstrap.Awake` (no Settings UI in MVP — visible slider + mute toggle post-MVP per `docs/blip-post-mvp-extensions.md` §4). Scene-load suppression policy documented so Blip stays silent until `BlipCatalog.Awake` completes.

**Exit:**

- `Assets/Audio/BlipMixer.mixer` w/ three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) + exposed master `SfxVolume` dB param (default 0 dB). Authored via Unity Editor (`Window → Audio → Audio Mixer`); committed as binary YAML asset.
- `BlipBootstrap` GameObject prefab at `MainMenu.unity` root; `DontDestroyOnLoad(transform.root.gameObject)` in `Awake` (pattern per `GameNotificationManager.cs`).
- `SfxVolume` bound headless — `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)`. No UI touched in MVP.
- Scene-load suppression policy doc'd in glossary row + catalog comment.
- Glossary rows land for **Blip mixer group** + **Blip bootstrap**.

**Phases:**

- [x] Phase 1 — Mixer asset + three groups + exposed SFX volume param.
- [x] Phase 2 — Persistent bootstrap prefab + headless volume binding + scene-load suppression policy.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | BlipMixer asset | 1 | **TECH-98** | Done | Create `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer` — binary YAML, not hand-written). Three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`), each routed through master. Expose master `SfxVolume` dB param (`Exposed Parameters` panel, default 0 dB). |
| T1.1.2 | Headless volume binding | 1 | **TECH-99** | Done | Headless SFX volume binding — `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)`. No Settings UI in MVP (visible slider + mute toggle deferred post-MVP per `docs/blip-post-mvp-extensions.md` §4). Key string constant on `BlipBootstrap`. |
| T1.1.3 | BlipBootstrap prefab | 2 | **TECH-100** | Done | `BlipBootstrap` GameObject prefab + `DontDestroyOnLoad(transform.root.gameObject)` in `Awake` (pattern per `GameNotificationManager.cs`). Empty Catalog / Player / MixerRouter / CooldownRegistry child slots (populated Step 2). Placed at root of `MainMenu.unity` (boot scene; build index 0 per `MainMenuController.cs`). |
| T1.1.4 | Scene-load suppression | 2 | **TECH-101** | Done (archived) | Scene-load suppression policy — no Blip fires until `BlipCatalog.Awake` sets ready flag. Document in glossary rows for **Blip mixer group** + **Blip bootstrap**. |

#### Stage 1.2 — Patch data model

**Status:** Done — TECH-111..TECH-115 Done

**Objectives:** `BlipPatch` ScriptableObject authoring surface + `BlipPatchFlat` blittable mirror + content-hash. MVP skips all `AnimationCurve` fields (no pitch-env curve, no cutoff-env curve, no envelope shape curve) — AHDSR uses parametric ramps (linear or exp per `BlipEnvShape` enum, no curves), filter uses static cutoff Hz. Keeps Step 3 authoring simple + Step 1 scope tight. Curve / LUT infrastructure lands post-MVP per `docs/blip-post-mvp-extensions.md` §1. `BlipMode` enum omitted MVP (single implicit baked path) — added post-MVP when `BlipLiveHost` lands. `useLutOscillators` field reserved / unused MVP to prevent schema churn when post-MVP LUT osc lands.

**Exit:**

- `BlipPatch` SO w/ MVP fields — `oscillators[0..3]`, `envelope` (AHDSR w/ `BlipEnvShape` per-stage), `filter` (one-pole LP), `variantCount`, `pitchJitterCents`, `gainJitterDb`, `panJitter`, `voiceLimit`, `priority`, `cooldownMs`, `deterministic`, `mixerGroup` (ref — authoring only, not flattened), `durationSeconds`, `useLutOscillators` (reserved, unused MVP), `patchHash` (`[SerializeField] private int` — persisted). `CreateAssetMenu` attribute.
- MVP enums — `BlipId` (10 MVP rows + `None`), `BlipWaveform` (`Sine`, `Triangle`, `Square`, `Pulse`, `NoiseWhite`), `BlipFilterKind` (`None`, `LowPass`), `BlipEnvStage` (`Idle`, `Attack`, `Hold`, `Decay`, `Sustain`, `Release`), `BlipEnvShape` (`Linear`, `Exponential`).
- `BlipPatchFlat` blittable readonly struct mirrors SO scalars. No `AnimationCurve`. No `AudioMixerGroup` ref (separate `BlipMixerRouter` owns `BlipId → AudioMixerGroup` map — see Step 2). No managed refs. `mixerGroupIndex` int slot reserved.
- `patchHash` = content hash over serialized fields. Stable across Unity GUID churn + version bumps. Persisted as `[SerializeField] private int` + recomputed on `OnValidate`; re-verified on `Awake` (assert matches recompute; log warning on mismatch).
- Attack/decay/release timing clamp in `OnValidate` — min 1 ms per stage (≈48 samples @ 48 kHz) to prevent snap-onset click. Sustain-only case uses A=1 ms / D=0 / R=1 ms.
- Glossary rows for **Blip patch**, **Blip patch flat**, **patch hash**.

**Phases:**

- [x] Phase 1 — `BlipPatch` SO authoring surface + MVP enums + `OnValidate` clamps.
- [x] Phase 2 — `BlipPatchFlat` flatten + content-hash persistence.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | BlipPatch SO scaffold | 1 | **TECH-111** | Done | `BlipPatch : ScriptableObject` class + MVP fields + `CreateAssetMenu("Territory/Audio/Blip Patch")`. No `AnimationCurve` fields. No `mode` field (`BlipMode` enum deferred post-MVP). `useLutOscillators` bool present but unread (reserved slot). |
| T1.2.2 | MVP structs + enums | 1 | **TECH-112** | Done | MVP struct + enum definitions — `BlipOscillator` (no `pitchEnvCurve`), `BlipEnvelope` (no `shape` curve; per-stage `BlipEnvShape` + `sustainLevel`), `BlipFilter` (no `cutoffEnv`) + `BlipId`, `BlipWaveform`, `BlipFilterKind`, `BlipEnvStage`, `BlipEnvShape` (`Linear`, `Exponential`). |
| T1.2.3 | OnValidate clamp guards | 1 | **TECH-113** | Done | `OnValidate` guards on `BlipPatch` — clamp `attackMs` / `releaseMs` to ≥ 1 ms (≈48 samples @ 48 kHz mix rate — kills snap-onset click); `decayMs` ≥ 0 ms (sustain-only A=1/D=0/R=1 allowed). Clamp `variantCount` 1..8, `voiceLimit` 1..16, `sustainLevel` 0..1, `cooldownMs` ≥ 0. Oscillator array resize guard caps `oscillators[]` at 3 (matches `BlipPatchFlat` MVP budget). |
| T1.2.4 | BlipPatchFlat struct | 2 | **TECH-114** | Done | `BlipPatchFlat` blittable readonly struct — mirrors SO scalars; no managed refs; no `AudioMixerGroup` ref (held in `BlipMixerRouter` parallel to catalog — Step 2). `BlipOscillatorFlat` / `BlipEnvelopeFlat` / `BlipFilterFlat` nested. Single `mixerGroupIndex` int slot. |
| T1.2.5 | patchHash content hash | 2 | **TECH-115** | Done | `patchHash` content hash — FNV-1a 32-bit digest over serialized scalar fields (osc freqs, env timings, env shapes, filter cutoff, jitter values, cooldown). Stable; ignores Unity GUID + version. `[SerializeField] private int patchHash` persisted on `OnValidate`; `Awake` / `OnEnable` recomputes + asserts match (warn-only). Glossary rows for **Blip patch**, **Blip patch flat**, **patch hash**. |

#### Stage 1.3 — Voice DSP kernel

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

**Phases:**

- [x] Phase 1 — Oscillator bank + voice state.
- [x] Phase 2 — AHDSR envelope state machine + per-stage shape.
- [x] Phase 3 — Render driver (LP filter + jitter + per-sample loop).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | BlipVoiceState struct | 1 | **TECH-116** | Done | `BlipVoiceState` struct — `phaseA..phaseD` (double), `envLevel`, `envStage`, `filterZ1`, `rngState` (xorshift seed), `samplesElapsed`. Blittable; lives in caller. |
| T1.3.2 | Oscillator bank | 1 | **TECH-117** | Done | Oscillator bank — sine (`Math.Sin` MVP), triangle, square, pulse (duty param), noise-white (xorshift on `rngState`). Phase-accumulator; frequency from patch osc + `pitchMult`. |
| T1.3.3 | AHDSR state machine | 2 | **TECH-118** | Done | AHDSR stage machine — `Idle → Attack → Hold → Decay → Sustain → Release → Idle`. Transitions via `samplesElapsed` + per-stage duration from patch (durations already ≥ 1 ms by `BlipPatch.OnValidate` clamp — see T1.2.3). |
| T1.3.4 | Envelope level math | 2 | **TECH-119** | Done | Envelope level math — per-stage `BlipEnvShape` selector. Linear: straight ramp (attack 0→1, decay 1→sustain, release sustain→0). Exponential: `target + (start - target) * exp(-t/τ)` with τ = stageDuration/4 (≈98 % settled at stage end; perceptual linear). Multiplies output sample. |
| T1.3.5 | One-pole LP filter | 3 | _archived_ | Done | One-pole LP filter in-loop — `y[n] = y[n-1] + a * (x[n] - y[n-1])` where `a = 1 - exp(-2π * cutoff / sampleRate)`. `z1` on `BlipVoiceState`. `filter.kind == None` → `a = 1.0` (passthrough, single kernel, no branch). |
| T1.3.6 | Render driver + jitter (consolidated) | 3 | **TECH-135** | Done | `BlipVoice.Render` driver w/ integrated per-invocation jitter — per-sample loop (osc × envelope × filter → buffer, `ref state`, zero alloc) + pitch cents / gain dB / pan ± jitter via xorshift `rngState`, honors `deterministic` flag. Consolidates former T1.3.6 (TECH-121) + T1.3.7 (TECH-122) per stage compress (2026-04-14). |

#### Stage 1.4 — EditMode DSP tests

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

**Phases:**

- [x] Phase 1 — Test asmdef + fixture helpers.
- [x] Phase 2 — Oscillator + envelope + silence assertions.
- [x] Phase 3 — Determinism + no-alloc regression tests.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.4.1 | asmdef + fixture helpers bootstrap | 1 | **TECH-137** | Done (archived) | `Assets/Tests/EditMode/Audio/Blip.Tests.EditMode.asmdef` (Editor-only; refs `Blip` runtime + `UnityEngine.TestRunner` + `nunit.framework`) + fixture helper utilities — `RenderPatch(in BlipPatchFlat, int sampleRate, int seconds) → float[]`, `CountZeroCrossings(float[]) → int`, `SampleEnvelopeLevels(float[], int stride) → float[]`, `SumAbsHash(float[]) → double`. Consolidates former T1.4.1 (asmdef) + T1.4.2 (helpers) per stage compress (2026-04-14). |
| T1.4.2 | Oscillator crossing tests | 2 | **TECH-138** | Done (archived) | Oscillator zero-crossing tests — sine @ 440 Hz × 1 s @ 48 kHz ≈ 880 crossings (± 2). Repeat triangle / square / pulse duty=0.5. |
| T1.4.3 | Envelope shape + silence tests | 2 | **TECH-139** | Done (archived) | Envelope shape tests — both `Linear` + `Exponential` shapes. A=50ms/H=0/D=50ms/S=0.5/R=50ms. Assert attack monotonic rising, decay monotonic falling to sustain, release monotonic falling to zero. Exponential-shape extra assert — attack slope in first quarter > slope in last quarter. Silence case — `gainMult = 0` → all-zero buffer (exact equality, not tolerance). Consolidates former T1.4.4 (envelope) + T1.4.5 (silence) per stage compress (2026-04-14). |
| T1.4.4 | Determinism test | 3 | **TECH-140** | Done (archived) | Determinism test — render same patch + seed + variant twice; assert `SumAbsHash` equal within 1e-6 + first 256 samples byte-equal. Validates voice-state reset + RNG determinism without depending on JIT stability of trailing samples. |
| T1.4.5 | No-alloc regression | 3 | **TECH-141** | Done (archived) | No-alloc regression — warm-up loop (3 renders, discard allocation), then measure `GC.GetAllocatedBytesForCurrentThread` delta across 10 steady-state renders; assert delta constant ≤ 0 bytes/call (tolerates NUnit infra alloc outside the measured window). |

**Backlog state (Step 1):** All Step 1 task rows stay in this doc as `_pending_`. File BACKLOG rows + project specs when parent stage → `In Progress` via `stage-file` skill. Stages 2.x + 3.x task decomposition deferred until Step 2 + Step 3 open.

### Step 2 — Bake + facade + PlayMode smoke

**Status:** Final — all stages archived (Stage 2.1: TECH-159..TECH-162; Stage 2.2: TECH-169..TECH-174; Stage 2.3: TECH-188..TECH-191; Stage 2.4: TECH-196..TECH-199). Closed 2026-04-15.

**Backlog state (Step 2):** 4 filed (Stage 2.1)

**Objectives:** Bake-to-clip pipeline + runtime facade. After Step 2: `BlipEngine.Play(BlipId)` dispatches through catalog → cached `AudioClip` via baker → pooled `AudioSource` via player. Playable from game code, but no call sites wired yet + no fixtures (those land Step 3).

**Exit criteria:**

- `BlipBaker` — `BakeOrGet(in BlipPatchFlat, int variantIndex) → AudioClip`. Main-thread only. Renders via `BlipVoice.Render` into `float[]` then wraps via `AudioClip.Create`. LRU cache keyed by `(patchHash, variantIndex)`. Memory budget default 4 MB; evicts LRU on overflow. Plain class, owned by `BlipCatalog` MonoBehaviour (instance field, not static — honors invariant #4).
- `BlipCatalog : MonoBehaviour` — `SerializeField BlipPatchEntry[] entries` mapping `BlipId` → `BlipPatch`. `Awake` flattens all patches to `BlipPatchFlat`, constructs `BlipMixerRouter` + `BlipCooldownRegistry` + `BlipBaker` instances, registers self w/ `BlipEngine` (facade caches ref). Sets ready flag last (scene-load suppression per Stage 1.1 T1.1.4).
- `BlipPlayer : MonoBehaviour` — pool of 16 `AudioSource` children, round-robin cursor. `PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)`. Child of `BlipBootstrap`.
- `BlipMixerRouter` — plain class, owned by `BlipCatalog`. Builds `BlipId → AudioMixerGroup` map at `BlipCatalog.Awake` from authoring-only `BlipPatch.mixerGroup` ref (which is NOT in `BlipPatchFlat` — see Stage 1.2 T1.2.4). `BlipEngine.Play` resolves group via `router.Get(id)` + passes to `BlipPlayer.PlayOneShot`. Keeps `BlipPatchFlat` blittable + mixer routing main-thread-only.
- `BlipCooldownRegistry` — plain class, owned by `BlipCatalog` (instance field, not static — honors invariant #4). Holds `Dictionary<BlipId, double>` (last-play `AudioSettings.dspTime`) + reads per-`BlipId` `cooldownMs` from patch. `BlipEngine.Play` queries via cached catalog ref + bails silently if `(now - lastPlay) * 1000 < cooldownMs`.
- `BlipEngine` static facade — `Play(BlipId, float pitchMult = 1f, float gainMult = 1f)`, `StopAll(BlipId)`. Main-thread assert on entry (`Thread.CurrentThread.ManagedThreadId == 1`). Resolves `BlipCatalog` / `BlipPlayer` lazily via `FindObjectOfType` fallback; caches refs in static fields; honors invariant #3 (no per-frame lookup) + #4 (static facade is stateless dispatch; state lives on `BlipCatalog` / `BlipPlayer` MonoBehaviours).
- PlayMode smoke scene passes — boot → `BlipBootstrap` alive → `BlipEngine.Play(BlipId.UiButtonClick)` fires without exception → `BlipCatalog.Resolve` returns non-null for all 10 MVP ids → mixer router returns non-null group per id → 16 rapid plays don't exhaust pool → 17th play within cooldown window is blocked silently.
- `npm run unity:compile-check` green.

**Art:** None.

**Relevant surfaces (load when step opens):**
- Step 1 outputs on disk: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs`, `BlipPatch.cs`, `BlipPatchFlat.cs`, `BlipVoice.cs`, `BlipVoiceState.cs`, `BlipPatchTypes.cs`, `BlipOscillatorBank.cs`, `BlipEnvelope.cs`. `Assets/Audio/BlipMixer.mixer` + `BlipBootstrap` prefab in `MainMenu.unity`.
- `docs/blip-procedural-sfx-exploration.md` §7 (architecture — baker / catalog / player / engine layering), §14 (MVP scope — 10 ids + mixer group assignments).
- `ia/rules/invariants.md` #3 (no `FindObjectOfType` in hot loops — facade must cache), #4 (no new singletons — `BlipEngine` stateless dispatch, state on MonoBehaviours).
- Unity API: `AudioClip.Create`, `AudioClip.SetData`, `AudioSource.PlayOneShot` / `.Play` / `.outputAudioMixerGroup`, `AudioSettings.dspTime`, `UnityEngine.Object.Destroy` (for evicted clips).
- New files: `Assets/Scripts/Audio/Blip/BlipBaker.cs`, `BlipBakeKey.cs`, `BlipCatalog.cs`, `BlipPatchEntry.cs`, `BlipMixerRouter.cs`, `BlipCooldownRegistry.cs`, `BlipPlayer.cs`, `BlipEngine.cs` (all `(new)`). New dir: `Assets/Tests/PlayMode/Audio/` w/ `Blip.Tests.PlayMode.asmdef` `(new)`.

#### Stage 2.1 — Bake-to-clip pipeline

**Status:** Done — TECH-159 / TECH-160 / TECH-161 / TECH-162 Done (archived) 2026-04-15

**Objectives:** `BlipBaker` plain class ships. Renders `BlipPatchFlat` through `BlipVoice.Render` into `float[]` then wraps via `AudioClip.Create` + `AudioClip.SetData`. LRU cache keyed by `(patchHash, variantIndex)` with 4 MB default memory budget + eviction on overflow. Main-thread only; no MonoBehaviour. Consumed by `BlipCatalog` (Stage 2.2) + `BlipEngine.Play` (Stage 2.3).

**Exit:**

- `BlipBaker` plain class at `Assets/Scripts/Audio/Blip/BlipBaker.cs` — `BakeOrGet(in BlipPatchFlat patch, int variantIndex) → AudioClip`.
- Cache hit path: O(1) `Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>>` lookup + LRU-tail promote.
- Cache miss path: render → `AudioClip.Create(name, lengthSamples, 1, sampleRate, stream: false)` + `SetData(buffer, 0)` → insert at tail + evict head until under budget.
- Memory budget enforced (default 4 MB via ctor param `long budgetBytes = 4 * 1024 * 1024`). Evicted clips destroyed via `UnityEngine.Object.Destroy(clip)`.

**Phases:**

- [x] Phase 1 — Baker core + bake key + cache hit/miss dispatch.
- [x] Phase 2 — LRU eviction + memory budget accounting.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | BlipBaker core + render path | 1 | **TECH-159** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipBaker.cs`. Plain class (not MonoBehaviour). `BakeOrGet(in BlipPatchFlat patch, int patchHash, int variantIndex) → AudioClip`. `sampleRate` is baker ctor param (default `AudioSettings.outputSampleRate`) — not per-call, not a flat field. `patchHash` passed per-call (flat struct defers hash per Stage 1.2 source line 162). Main-thread assert at entry via `BlipBootstrap.MainThreadId` — TECH-159 lands the minimal static prop + `Awake` capture (T2.3.1 reuses). Computes `lengthSamples = (int)(patch.durationSeconds * _sampleRate)`, allocates `float[lengthSamples]`, initializes `BlipVoiceState` default + calls `BlipVoice.Render(buffer, 0, lengthSamples, _sampleRate, in patch, variantIndex, ref state)`, wraps via `AudioClip.Create(name, lengthSamples, 1, _sampleRate, stream: false)` + `clip.SetData(buffer, 0)`. |
| T2.1.2 | Bake key + cache hit dispatch | 1 | **TECH-160** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipBakeKey.cs` — `readonly struct BlipBakeKey(int patchHash, int variantIndex)` w/ `IEquatable<BlipBakeKey>` + hash combine. In `BlipBaker`: `Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>> _index` + `LinkedList<BlipBakeEntry> _lru`. `BakeOrGet` first probes `_index`; hit → move node to tail + return cached `AudioClip`; miss → invoke render path (T2.1.1) + handoff to Phase 2 eviction. |
| T2.1.3 | LRU ordering + access tracking | 2 | **TECH-161** (archived) | Done (archived) | `BlipBakeEntry` private nested class/struct holding `BlipBakeKey key`, `AudioClip clip`, `long byteCount`. `_lru` access order: newest at tail, oldest at head. Hit → `_lru.Remove(node); _lru.AddLast(node)`. Miss insert → `_lru.AddLast(entry)` after render. Unit-test-able helper `TryEvictHead() → bool` for Phase 2 budget loop. |
| T2.1.4 | Memory budget + eviction loop | 2 | **TECH-162** (archived) | Done (archived) | Ctor param `long budgetBytes = 4L * 1024 * 1024`. Track `_totalBytes` running sum. Each entry `byteCount = lengthSamples * sizeof(float)`. On insert, loop: while `_totalBytes + newByteCount > budgetBytes && _lru.First != null` → pop head, `UnityEngine.Object.Destroy(evicted.clip)`, subtract `evicted.byteCount` from `_totalBytes`, remove from `_index`. Then add new entry + `_totalBytes += newByteCount`. |

---

#### Stage 2.2 — Catalog + mixer router + cooldown registry + player pool

**Status:** Done (6 tasks archived 2026-04-15 — **TECH-169**..**TECH-174**)

**Objectives:** MonoBehaviour hosts + plain-class services wire together under `BlipBootstrap`. `BlipCatalog` flattens authoring SOs, owns `BlipBaker` + `BlipMixerRouter` + `BlipCooldownRegistry` as instance fields (invariant #4 — no new singletons). `BlipPlayer` exposes 16-source pool. No static facade yet; `BlipEngine.Bind` callbacks reserved for Stage 2.3.

**Exit:**

- `BlipPatchEntry` serializable struct at `Assets/Scripts/Audio/Blip/BlipPatchEntry.cs` — `public BlipId id; public BlipPatch patch;`.
- `BlipCatalog : MonoBehaviour` at `Assets/Scripts/Audio/Blip/BlipCatalog.cs` — `SerializeField BlipPatchEntry[] entries`, flattens to `BlipPatchFlat[]`, builds `Dictionary<BlipId, int>` index, owns `BlipBaker` + `BlipMixerRouter` + `BlipCooldownRegistry` instance fields, `Resolve(BlipId) → ref readonly BlipPatchFlat`. Ready flag set last in `Awake`.
- `BlipMixerRouter` plain class at `Assets/Scripts/Audio/Blip/BlipMixerRouter.cs` — `Get(BlipId) → AudioMixerGroup`, built from authoring-only `BlipPatch.mixerGroup` refs.
- `BlipCooldownRegistry` plain class at `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs` — `TryConsume(BlipId, double nowDspTime, double cooldownMs) → bool`.
- `BlipPlayer : MonoBehaviour` at `Assets/Scripts/Audio/Blip/BlipPlayer.cs` — child of `BlipBootstrap`, 16-source pool + round-robin `PlayOneShot(AudioClip, float pitch, float gain, AudioMixerGroup)`.

**Phases:**

- [x] Phase 1 — `BlipPatchEntry` + `BlipCatalog` flatten + resolve + ready flag.
- [x] Phase 2 — `BlipMixerRouter` + `BlipCooldownRegistry` plain-class services owned by catalog.
- [x] Phase 3 — `BlipPlayer` 16-source pool + round-robin `PlayOneShot`.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | BlipPatchEntry + catalog flatten | 1 | **TECH-169** | Done (archived) | New files `Assets/Scripts/Audio/Blip/BlipPatchEntry.cs` (`[Serializable] public struct BlipPatchEntry { public BlipId id; public BlipPatch patch; }`) + `BlipCatalog.cs` (`sealed : MonoBehaviour`). `[SerializeField] private BlipPatchEntry[] entries`. `Awake` iterates `entries`, builds parallel `BlipPatchFlat[] _flat` via `BlipPatchFlat.FromSO(entry.patch)` (Stage 1.2 helper) + `Dictionary<BlipId, int> _indexById`. Throws `InvalidOperationException` w/ index + id on duplicate `BlipId` or null patch ref. |
| T2.2.2 | Catalog Resolve + ready flag + Engine bind | 1 | **TECH-170** | Done (archived) | `BlipCatalog.Resolve(BlipId id) → ref readonly BlipPatchFlat` via `_indexById` lookup (throws on unknown id). `bool isReady` private field set to `true` as the last statement in `Awake` — scene-load suppression contract per Stage 1.1 T1.1.4. Calls `BlipEngine.Bind(this)` (method added Stage 2.3 T2.3.2 — declare stub signature here; null-safe). `OnDestroy` → `BlipEngine.Unbind(this)` stub. |
| T2.2.3 | BlipMixerRouter plain class | 2 | **TECH-171** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipMixerRouter.cs`. `public sealed class BlipMixerRouter` plain class. Ctor takes `BlipPatchEntry[] entries` + builds `Dictionary<BlipId, AudioMixerGroup> _map` reading authoring-only `entry.patch.mixerGroup` ref (NOT in `BlipPatchFlat` — Stage 1.2 T1.2.4 Decision Log). `Get(BlipId) → AudioMixerGroup` lookup (throws on unknown id). Instantiated in `BlipCatalog.Awake` + held as instance field `_mixerRouter`. |
| T2.2.4 | BlipCooldownRegistry plain class | 2 | **TECH-172** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs`. `public sealed class BlipCooldownRegistry` plain class. `Dictionary<BlipId, double> _lastPlayDspTime`. `TryConsume(BlipId id, double nowDspTime, double cooldownMs) → bool` — if `!_lastPlayDspTime.TryGetValue(id, out var last) || (nowDspTime - last) * 1000.0 >= cooldownMs` → write `_lastPlayDspTime[id] = nowDspTime` + return `true`; else return `false`. Instantiated in `BlipCatalog.Awake` + held as instance field `_cooldownRegistry`. |
| T2.2.5 | BlipPlayer pool construction | 3 | **TECH-173** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipPlayer.cs` (`: MonoBehaviour`). `[SerializeField] private int poolSize = 16`. `Awake` instantiates `poolSize` child GameObjects (`new GameObject("BlipVoice_0".."BlipVoice_15")`) parented under this transform, each with `AudioSource` component (`playOnAwake = false`, `loop = false`). Holds `AudioSource[] _pool` + `int _cursor = 0`. Placed as child of `BlipBootstrap` prefab. Calls `BlipEngine.Bind(this)` at end of `Awake`. |
| T2.2.6 | BlipPlayer PlayOneShot dispatch | 3 | **TECH-174** | Done (archived) | `BlipPlayer.PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)` — selects `var source = _pool[_cursor]; _cursor = (_cursor + 1) % _pool.Length;`, stops prior clip if still playing (voice-steal overwrite — no crossfade, post-MVP per orchestration guardrails), sets `source.clip = clip; source.pitch = pitch; source.volume = gain; source.outputAudioMixerGroup = group;` then `source.Play()`. |

---

#### Stage 2.3 — BlipEngine facade + main-thread gate

**Status:** Done — 4 tasks archived (TECH-188..TECH-191) 2026-04-15

**Objectives:** Static `BlipEngine` facade lands. Stateless dispatch per invariant #4 — state lives on `BlipCatalog` / `BlipPlayer` MonoBehaviours; facade caches refs in static fields per invariant #3 (no `FindObjectOfType` on hot path). Main-thread assert gates all entry points. `Play` routes catalog → cooldown → baker → router → player.

**Exit:**

- `BlipEngine` static class at `Assets/Scripts/Audio/Blip/BlipEngine.cs` — `Play(BlipId, float pitchMult = 1f, float gainMult = 1f)` + `StopAll(BlipId)`.
- Main-thread assert at every entry point — compares `Thread.CurrentThread.ManagedThreadId` to cached main-thread id (captured in `BlipBootstrap.Awake`; new `BlipBootstrap.MainThreadId` static read-only accessor).
- `Bind(BlipCatalog)` / `Bind(BlipPlayer)` / `Unbind(*)` static setters consumed by Stage 2.2 hosts + lazy `FindObjectOfType` fallback on first call if not bound. Cached in static fields — no per-frame lookup.
- `Play` dispatch queries cooldown, bails silently when blocked; picks variant; bakes via `BlipBaker.BakeOrGet`; resolves mixer group; forwards to `BlipPlayer.PlayOneShot`.

**Phases:**

- [x] Phase 1 — Facade skeleton + main-thread assert + Bind/Unbind + cached lazy resolution.
- [x] Phase 2 — Play + StopAll dispatch bodies through catalog → cooldown → baker → router → player.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | Facade skeleton + main-thread gate | 1 | **TECH-188** | Done (archived) | New file `Assets/Scripts/Audio/Blip/BlipEngine.cs` — `public static class BlipEngine`. Declares `Play(BlipId id, float pitchMult = 1f, float gainMult = 1f)` + `StopAll(BlipId id)` w/ empty bodies for now. Private `AssertMainThread()` helper compares `Thread.CurrentThread.ManagedThreadId` to cached `BlipBootstrap.MainThreadId` (new static read-only prop set in `BlipBootstrap.Awake` → `Thread.CurrentThread.ManagedThreadId`). Throws `InvalidOperationException` w/ diagnostic message on mismatch. Invoked first line of every entry point. |
| T2.3.2 | Bind/Unbind + cached lazy resolution | 1 | **TECH-189** | Done (archived) | In `BlipEngine`: `static BlipCatalog _catalog; static BlipPlayer _player;`. `Bind(BlipCatalog c)` / `Bind(BlipPlayer p)` setters (null-safe overwrite). `Unbind(BlipCatalog)` / `Unbind(BlipPlayer)` nullers. Private `ResolveCatalog() → BlipCatalog` / `ResolvePlayer() → BlipPlayer` — return cached field if non-null, else `FindObjectOfType<BlipCatalog>()` / `FindObjectOfType<BlipPlayer>()` fallback + cache (invariant #3 — one-time lookup, not per-frame). Consumed by `BlipCatalog.Awake` (T2.2.2) + `BlipPlayer.Awake` (T2.2.5). |
| T2.3.3 | Play dispatch body | 2 | **TECH-190** | Done (archived) | `BlipEngine.Play(BlipId id, float pitchMult, float gainMult)` body: `AssertMainThread()` → `var cat = ResolveCatalog(); if (cat == null \|\| !cat.IsReady) return;` → `var nowDsp = AudioSettings.dspTime; ref readonly var patch = ref cat.Resolve(id); if (!cat.CooldownRegistry.TryConsume(id, nowDsp, patch.cooldownMs)) return;` → variant index = deterministic (fixed 0) if `patch.deterministic` else xorshift on per-id RNG state held on catalog → `AudioClip clip = cat.Baker.BakeOrGet(in patch, variantIndex);` → `AudioMixerGroup group = cat.MixerRouter.Get(id);` → `ResolvePlayer().PlayOneShot(clip, pitchMult, gainMult, group);`. Expose `cat.IsReady`, `cat.CooldownRegistry`, `cat.Baker`, `cat.MixerRouter` internals via `internal` props on `BlipCatalog`. |
| T2.3.4 | StopAll dispatch body | 2 | **TECH-191** (archived) | Done (archived) | `BlipEngine.StopAll(BlipId id)` body: `AssertMainThread()` → resolve catalog + player → query `cat.Baker` for all cached `AudioClip` refs matching `(patchHash, *)` for this `id` (expose `BlipBaker.EnumerateClipsForPatchHash(int patchHash) → IEnumerable<AudioClip>` helper). Iterate `BlipPlayer._pool`; call `source.Stop()` where `source.clip` matches any enumerated clip. Non-destructive — does not evict baked clips from cache. |

---

#### Stage 2.4 — PlayMode smoke test

**Status:** Done — TECH-196..TECH-199 all archived 2026-04-15.

**Objectives:** New PlayMode test asmdef + smoke fixture exercises full boot path through `BlipEngine.Play` in-scene. Verifies: catalog ready flag, 10 MVP id resolution, mixer routing, 16-source pool non-exhaustion under rapid play, 17th-call cooldown block. Uses `[UnityTest]` + `yield return null` frame waits; no audio listener output assertions (headless-safe).

**Exit:**

- `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef` — Editor+Standalone, `testAssemblies: true`, refs `Blip` runtime asmdef + `UnityEngine.TestRunner` + `nunit.framework`.
- `BlipPlayModeSmokeTests.cs` fixture loads `MainMenu.unity` (boot scene, build index 0) + polls `BlipCatalog.IsReady`.
- All 10 MVP `BlipId` rows resolve non-null patch + non-null `AudioMixerGroup`.
- 16 rapid plays on a zero-cooldown fixture `BlipId` advance `BlipPlayer._cursor` full wrap w/o exception; 17th play within `cooldownMs` window returns silently (no `AudioSource.Play` increment observed).
- `npm run unity:compile-check` green after fixture lands.

**Phases:**

- [x] Phase 1 — PlayMode asmdef + boot-scene fixture setup.
- [x] Phase 2 — Resolution + routing + pool + cooldown assertions.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.4.1 | PlayMode asmdef bootstrap | 1 | **TECH-196** | Done (archived) | New file `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef` w/ `"testAssemblies": true`, `"includePlatforms": ["Editor"]` (PlayMode runs in Editor per Unity conv), references: `Blip` runtime asmdef (GUID) + `UnityEngine.TestRunner` + `UnityEditor.TestRunner` + `nunit.framework`. Create companion `.meta`. Empty placeholder `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs` declaring `public sealed class BlipPlayModeSmokeTests` + namespace to anchor asmdef resolution. |
| T2.4.2 | Boot-scene fixture SetUp | 1 | **TECH-197** | Done (archived) | In `BlipPlayModeSmokeTests`: `[UnitySetUp] public IEnumerator SetUp()` → `SceneManager.LoadScene("MainMenu", LoadSceneMode.Single)` then `yield return null` × 2 (one frame for `Awake` cascade, one for ready flag). Assert `BlipBootstrap.Instance != null`, `Object.FindObjectOfType<BlipCatalog>().IsReady == true`. Hold catalog + player refs as private fields for per-test access. `[UnityTearDown]` unloads scene cleanly. |
| T2.4.3 | Resolution + routing assertions | 2 | **TECH-198** (archived) | Done (archived) | `[UnityTest] public IEnumerator Play_AllMvpIds_ResolvesAndRoutes()` — for each `BlipId` in `{UiButtonHover, UiButtonClick, ToolRoadTick, ToolRoadComplete, ToolBuildingPlace, ToolBuildingDenied, WorldCellSelected, EcoMoneyEarned, EcoMoneySpent, SysSaveGame}`: assert `catalog.Resolve(id)` returns non-null patch ref (patchHash != 0), `catalog.MixerRouter.Get(id) != null`, `Assert.DoesNotThrow(() => BlipEngine.Play(id))`. `yield return null` once after loop to drain AudioSource.Play side-effects. |
| T2.4.4 | Pool + cooldown assertions | 2 | **TECH-199** (archived) | Done (archived) | `[UnityTest] public IEnumerator Play_RapidFire_ExhaustsPoolAndBlocksOnCooldown()` — use a fixture `BlipId` w/ near-zero cooldown (e.g. `ToolRoadTick` 30 ms) plus one w/ long cooldown. Fire 16 rapid `BlipEngine.Play(tickId)` within one frame (no yield between) — assert no exception + `player._cursor == 0` after wrap (expose via `internal` accessor). For cooldown: fire `Play(longCooldownId)` once, immediately fire again; verify second call returns silently (track `catalog.CooldownRegistry` last-play dict didn't update, OR expose a debug counter `BlipCooldownRegistry.BlockedCount` incremented on block). |

### Step 3 — Patches + integration + golden fixtures + promotion

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Author 10 MVP patches + wire to call sites + golden fixture harness + glossary + spec promotion. After Step 3: player hears Blip in game, DSP output is regression-gated by hash fixtures, subsystem promoted from `docs/` exploration to `ia/specs/audio-blip.md`.

**Exit criteria:**

- Ten `BlipPatch` SO assets authored under `Assets/Audio/BlipPatches/` — one per MVP `BlipId` (recipes in exploration doc §9): `UiButtonHover` (ex 1), `UiButtonClick` (ex 2), `ToolRoadTick` (ex 5), `ToolRoadComplete` (ex 6), `ToolBuildingPlace` (ex 9), `ToolBuildingDenied` (ex 10), `WorldCellSelected` (ex 15), `EcoMoneyEarned` (ex 17), `EcoMoneySpent` (ex 18), `SysSaveGame` (ex 20).
- Each patch routes to correct mixer group per exploration doc §14 (authored `mixerGroup` ref — picked up by `BlipMixerRouter` at `BlipCatalog.Awake`).
- Call sites fire `BlipEngine.Play(BlipId)` at — MainMenu button hover / click, road-draw tool per-tile commit + plan-apply, building placement confirm + denial, `GridManager` single-cell select, money ledger earn + spend, save-complete hook.
- Road per-tile tick rate-limited to its patch `cooldownMs` (30 ms). Cell-select cooldown 80 ms. Manual save cooldown 2 s. (No autosave wiring in MVP; autosave-burst suppression deferred post-MVP.)
- Golden fixture harness — `tools/fixtures/blip/*.json` per patch + variant; each carries `patchHash` + sum-of-abs tolerance hash + sample count + sample rate + oscillator zero-crossing count. Regression test renders patch offline (same `BlipVoice.Render` kernel) + compares to fixture; fails on drift beyond epsilon (1e-6).
- Fixture regeneration script — `tools/scripts/blip-bake-fixtures.ts` (or equivalent) bakes all 10 patches + writes fixtures. CI runs test only, never regen.
- Glossary rows — **Blip**, **Blip patch** (if not from Step 1), **Blip patch flat** (if not from Step 1), **Blip voice**, **Blip catalog**, **Blip engine**, **Blip baker**, **Blip player**, **Blip id**, **Blip mixer router**, **Blip mixer group** (if not from Step 1.1), **Blip bootstrap** (if not from Step 1.1), **Blip variant**, **Blip cooldown**, **Bake-to-clip**, **Patch flatten**, **Patch hash** (if not from Step 1). Cross-refs to `ia/specs/audio-blip.md`.
- `docs/blip-procedural-sfx-exploration.md` promoted to `ia/specs/audio-blip.md` (structure matches existing `ia/specs/*.md` conventions). Exploration doc either archived or left w/ "superseded by" pointer to spec.
- `npm run validate:all` green + `npm run unity:compile-check` green.
- Issue row closes via `/closeout` umbrella once subsystem + spec lands (per-task closes via `/closeout` as they complete; this orchestrator never closes — see `ia/rules/orchestrator-vs-spec.md`).

**Art:** None (SO parameter tuning is authoring, not art assets).

**Relevant surfaces (load when step opens):**
- Step 2 outputs on disk: `Assets/Scripts/Audio/Blip/BlipBaker.cs`, `BlipCatalog.cs`, `BlipPlayer.cs`, `BlipMixerRouter.cs`, `BlipCooldownRegistry.cs`, `BlipEngine.cs`, `BlipBootstrap.cs` (all under `Assets/Scripts/Audio/Blip/`).
- `docs/blip-procedural-sfx-exploration.md` §9 (20 concrete examples — MVP recipes match 1, 2, 5, 6, 9, 10, 15, 17, 18, 20), §8 (related subsystems — call site map), §14 (MVP scope + mixer group routing table).
- Call-site host files: `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` (515 lines; `OnContinueClicked` + `OnNewGameClicked` + `OnLoadCityClicked` + `OnOptionsClicked` + `CloseLoadCityPanel` + `CloseOptionsPanel`; button listener registration line ~133), `Assets/Scripts/Managers/GameManagers/RoadManager.cs` (3212 lines; `HandleRoadDrawing` line 141 for tick; `PlaceRoadTileFromResolved` line 2706; stroke-complete hook — grep `CommitStroke`/`ApplyRoadPlan`/`ConfirmStroke`), `Assets/Scripts/Managers/GameManagers/BuildingPlacementService.cs` (430 lines; `PlaceBuilding` line 234; `TryValidateBuildingPlacement` line 53 for deny), `Assets/Scripts/Managers/GameManagers/GridManager.cs` (`selectedPoint` assignment lines 391 + 399), `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` (664 lines; `AddMoney` line 191; `SpendMoney` line 153), `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` (418 lines; `SaveGame` line 64; `TryWriteGameSaveToPath` line 86).
- `ia/rules/invariants.md` #3 (no `FindObjectOfType` in hot loops — `BlipEngine` self-caches after first lookup), #4 (static facade, no new singletons), #6 (don't add responsibilities to `GridManager` — one-liner `BlipEngine.Play` at select site is side-effect only, not new logic).
- New paths: `Assets/Audio/BlipPatches/` (new dir, 10 SO assets), `tools/fixtures/blip/` (new dir), `tools/scripts/blip-bake-fixtures.ts` (new), `ia/specs/audio-blip.md` (new — promoted from exploration doc).

#### Stage 3.1 — Patch authoring + catalog wiring

**Status:** Done (all tasks archived 2026-04-15 — TECH-209..TECH-212)

**Objectives:** Ten `BlipPatch` SO assets authored + `BlipCatalog.entries[]` wired in Inspector. After this stage all 10 MVP `BlipId` values resolve a non-null patch + non-null `AudioMixerGroup` from the catalog; `BlipEngine.Play` is unblocked but no call sites exist yet.

**Exit:**

- `Assets/Audio/BlipPatches/` dir + 10 `BlipPatch` SO asset files. Each SO: envelope/oscillator/filter params per exploration §9 recipes; `cooldownMs` per Exit criteria (ToolRoadTick 30 ms, WorldCellSelected 80 ms, SysSaveGame 2000 ms; others per §9); `patchHash` non-zero after `OnValidate`.
- `mixerGroup` authoring ref set on each SO per exploration §14 routing table (`Blip-UI` for `UiButtonHover` + `UiButtonClick`; `Blip-World` for `ToolRoad*` + `ToolBuilding*` + `WorldCellSelected`; confirm §14 for Eco/Sys ids).
- `BlipCatalog.entries[]` array populated in Inspector — 10 `BlipPatchEntry` rows (each: `BlipId` enum + `BlipPatch` asset ref). `BlipBootstrap` prefab Catalog + Player child slots confirmed wired.
- PlayMode smoke: `BlipCatalog.IsReady == true`; all 10 ids resolve non-null patch + non-null `AudioMixerGroup` via `BlipMixerRouter`.
- `npm run unity:compile-check` green.

**Phases:**

- [x] Phase 1 — Author 10 `BlipPatch` SO assets with envelope/oscillator params + cooldown from §9 recipes.
- [x] Phase 2 — Assign `mixerGroup` refs + wire `BlipCatalog.entries[]` in Inspector + smoke verify.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.1.1 | 1 | **TECH-209** | Done (archived) | Create `Assets/Audio/BlipPatches/` dir + author 5 UI/Eco/Sys `BlipPatch` SOs via CreateAssetMenu (`Territory/Audio/Blip Patch`): `UiButtonHover` (§9 ex 1), `UiButtonClick` (§9 ex 2), `EcoMoneyEarned` (§9 ex 17), `EcoMoneySpent` (§9 ex 18), `SysSaveGame` (§9 ex 20). Fill all envelope/oscillator/filter/jitter params from §9 recipe table. `patchHash` recomputed on `OnValidate` — verify non-zero in Inspector after fill. |
| T3.1.2 | 1 | **TECH-210** | Done (archived) | Author 5 World `BlipPatch` SOs: `ToolRoadTick` (§9 ex 5; `cooldownMs` 30), `ToolRoadComplete` (§9 ex 6), `ToolBuildingPlace` (§9 ex 9), `ToolBuildingDenied` (§9 ex 10), `WorldCellSelected` (§9 ex 15; `cooldownMs` 80). Set all envelope/oscillator/filter/jitter/variantCount/voiceLimit params per §9. `patchHash` non-zero after `OnValidate`. |
| T3.1.3 | 2 | **TECH-211** | Done (archived) | Set `mixerGroup` authoring ref on all 10 SOs per exploration §14 routing table (open each SO in Inspector, assign `AudioMixerGroup` from `BlipMixer.mixer` asset). Wire `BlipCatalog.entries[]` in Inspector — 10 `BlipPatchEntry` rows (`BlipId` + `BlipPatch` asset ref). Open `BlipBootstrap` prefab; confirm Catalog + Player child slots populated. |
| T3.1.4 | 2 | **TECH-212** | Done (archived) | PlayMode smoke: enter Play Mode, load `MainMenu.unity`, poll `BlipCatalog.IsReady`; for all 10 `BlipId` values assert `catalog.Resolve(id).patchHash != 0` + `catalog.MixerRouter.Get(id) != null`. `npm run unity:compile-check` green. Confirms SO → catalog → mixer-router chain complete before any call site lands. |

#### Stage 3.2 — UI + Eco + Sys call sites

**Status:** In Progress — tasks filed 2026-04-15 (TECH-215..TECH-218 Draft)

**Objectives:** `BlipEngine.Play` wired at MainMenu button hover/click + money earn/spend + save-complete. Six `BlipId` values active in game: `UiButtonHover`, `UiButtonClick`, `EcoMoneyEarned`, `EcoMoneySpent`, `SysSaveGame`. No world-lane sounds yet.

**Exit:**

- `MainMenuController.cs` — `BlipEngine.Play(BlipId.UiButtonClick)` as first statement in each of `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`. `EventTrigger` PointerEnter callbacks on each `Button` reference fire `BlipEngine.Play(BlipId.UiButtonHover)` — registered programmatically alongside `onClick.AddListener` calls (get-or-add `EventTrigger` component, add `EventTriggerType.PointerEnter` entry). No new singletons (invariant #4); `BlipEngine` static facade self-caches (invariant #3).
- `EconomyManager.cs` — `BlipEngine.Play(BlipId.EcoMoneyEarned)` after `cityStats.AddMoney(amount)` (line ~205); `BlipEngine.Play(BlipId.EcoMoneySpent)` after `cityStats.RemoveMoney(amount)` in the success branch of `SpendMoney` (line ~169). Monthly-maintenance `SpendMoney` path excluded (non-interactive budget charge — guard by `notifyInsufficientFunds` param or call-context flag).
- `GameSaveManager.cs` — `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText` in `SaveGame` (line ~69) and in `TryWriteGameSaveToPath` (line ~91). 2 s cooldown enforced by `BlipCooldownRegistry` via patch SO — no additional guard.
- `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — UI lane: `MainMenuController` click + hover call sites.
- [ ] Phase 2 — Eco + Sys lane: `EconomyManager` earn/spend + `GameSaveManager` save-complete.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.2.1 | 1 | **TECH-215** | Done | `MainMenuController.cs` — add `BlipEngine.Play(BlipId.UiButtonClick)` as first statement in each of: `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`. No `FindObjectOfType` introduced — `BlipEngine` is static facade (invariant #3). |
| T3.2.2 | 1 | **TECH-216** | Done (archived) | `MainMenuController.cs` — in `RegisterButtonListeners` / `Start` (where `onClick.AddListener` calls live, line ~133): for each `Button` field (`continueButton`, `newGameButton`, `loadCityButton`, `optionsButton`, `loadCityBackButton`, `optionsBackButton`), call `GetOrAddComponent<EventTrigger>()` + add `EventTriggerType.PointerEnter` entry invoking `BlipEngine.Play(BlipId.UiButtonHover)`. No new fields; no new singletons (invariant #4). |
| T3.2.3 | 2 | **TECH-217** | Draft | `EconomyManager.cs` — add `BlipEngine.Play(BlipId.EcoMoneyEarned)` after `cityStats.AddMoney(amount)` in `AddMoney` (line ~205). Add `BlipEngine.Play(BlipId.EcoMoneySpent)` after `cityStats.RemoveMoney(amount)` in success branch of `SpendMoney` (line ~169). Monthly-maintenance path (`ChargeMonthlyMaintenance` → `SpendMoney`) must NOT fire — guard with `notifyInsufficientFunds == true` condition or add private overload with `bool fireBlip = true`. |
| T3.2.4 | 2 | **TECH-218** | Draft | `GameSaveManager.cs` — add `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText(path, ...)` in `SaveGame` (line ~69) and after equivalent write in `TryWriteGameSaveToPath` (line ~91). 2 s cooldown in patch SO `cooldownMs = 2000`; `BlipCooldownRegistry` gates rapid manual saves — no additional guard. `npm run unity:compile-check` green. |

#### Stage 3.3 — World lane call sites

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** `BlipEngine.Play` wired at road per-tile tick + stroke complete + building place/denied + cell select. Five remaining `BlipId` values active: `ToolRoadTick`, `ToolRoadComplete`, `ToolBuildingPlace`, `ToolBuildingDenied`, `WorldCellSelected`.

**Exit:**

- `RoadManager.cs` — `BlipEngine.Play(BlipId.ToolRoadTick)` at per-tile road commit inside `HandleRoadDrawing` (line 141) or `PlaceRoadTileFromResolved` (line 2706). `BlipCooldownRegistry` at 30 ms gates rapid ticks — no additional guard. `BlipEngine.Play(BlipId.ToolRoadComplete)` at road-stroke-complete/apply site (grep `CommitStroke`/`ApplyRoadPlan`/`ConfirmStroke` or `PathTerraformPlan.Apply` call site in `HandleRoadDrawing`). `BlipEngine` self-caches — safe to call per tile (invariant #3).
- `BuildingPlacementService.cs` — `BlipEngine.Play(BlipId.ToolBuildingPlace)` at end of success path in `PlaceBuilding` (line 234). `BlipEngine.Play(BlipId.ToolBuildingDenied)` at failure-notification site where `TryValidateBuildingPlacement` returns non-null reason (in `HandleBuildingPlacement`, `GridManager.cs` line 874 or equivalent caller).
- `GridManager.cs` — `BlipEngine.Play(BlipId.WorldCellSelected)` immediately after each `selectedPoint = mouseGridPosition` assignment (lines 391, 399). One-liner side-effect — not new GridManager logic (invariant #6 carve-out). 80 ms cooldown enforced by `BlipCooldownRegistry`.
- `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — Road lane: per-tile tick + stroke complete in `RoadManager.cs`.
- [ ] Phase 2 — Building + grid: place/denied in `BuildingPlacementService.cs` + cell-select in `GridManager.cs`.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.3.1 | 1 | _pending_ | _pending_ | `RoadManager.cs` — locate per-tile commit site: grep callers of `PlaceRoadTileFromResolved` (line 2706) inside `HandleRoadDrawing` (line 141). Add `BlipEngine.Play(BlipId.ToolRoadTick)` at the point each confirmed road tile is committed to the grid. Cooldown 30 ms enforced by `BlipCooldownRegistry` via patch SO — no additional rate-limit guard. |
| T3.3.2 | 1 | _pending_ | _pending_ | `RoadManager.cs` — locate stroke-complete hook: grep `CommitStroke`, `ApplyRoadPlan`, `ConfirmStroke`, or `PathTerraformPlan.Apply` call in `HandleRoadDrawing` (line 141 area) or `GridManager.HandleBulldozerMode` vicinity. Add `BlipEngine.Play(BlipId.ToolRoadComplete)` at end of success path (after all tiles placed, before `InvalidateRoadCache()`). `npm run unity:compile-check` green after road edits. |
| T3.3.3 | 2 | _pending_ | _pending_ | `BuildingPlacementService.cs` — add `BlipEngine.Play(BlipId.ToolBuildingPlace)` at end of success path in `PlaceBuilding` (line 234, after placement loop). Locate failure-notification site where `GetBuildingPlacementFailReason` returns non-null (in `GridManager.HandleBuildingPlacement` line 874 or caller); add `BlipEngine.Play(BlipId.ToolBuildingDenied)` at that point. |
| T3.3.4 | 2 | _pending_ | _pending_ | `GridManager.cs` — add `BlipEngine.Play(BlipId.WorldCellSelected)` immediately after each `selectedPoint = mouseGridPosition` assignment (lines 391 + 399). Invariant #6 carve-out: one-liner side-effect, not new GridManager logic. Invariant #3: `BlipEngine` self-caches — no per-frame lookup added. 80 ms cooldown in patch SO. `npm run unity:compile-check` green. |

#### Stage 3.4 — Golden fixtures + spec promotion + glossary

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Fixture harness gates DSP output regression. Exploration doc promoted to canonical spec. Glossary rows complete + cross-referenced to spec. After this stage Blip subsystem fully shipped + regression-gated.

**Exit:**

- `tools/fixtures/blip/` dir + 10 JSON fixture files (one per MVP `BlipId`, variant 0). Each: `{ "id": "<BlipId>", "variant": 0, "patchHash": <int>, "sampleRate": 44100, "sampleCount": <int>, "sumAbsHash": <double>, "zeroCrossings": <int> }`.
- `tools/scripts/blip-bake-fixtures.ts` (dev-only) — bakes each patch via `BlipVoice.Render` logic (TS port or Unity batchmode shim) + writes fixture JSONs. CI does NOT run this script; CI runs regression test only.
- `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` in `Blip.Tests.EditMode.asmdef` (Stage 1.4 asmdef — no new asmdef). One `[Test]` per `BlipId`: parse fixture JSON, re-render via `BlipVoice.Render`, assert `sumAbsHash` within 1e-6 + `zeroCrossings` within ±2 + `patchHash` equality (stale-fixture guard).
- `ia/specs/audio-blip.md` exists — structure matches `ia/specs/*.md` conventions. `docs/blip-procedural-sfx-exploration.md` has "Superseded by `ia/specs/audio-blip.md`" banner.
- `ia/specs/glossary.md` — new rows: **Blip variant**, **Blip cooldown**, **Bake-to-clip**, **Patch flatten**. All existing blip rows updated to cross-ref `ia/specs/audio-blip.md`.
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Fixture infrastructure: bake script + fixture JSON files.
- [ ] Phase 2 — Fixture regression test + spec promotion + glossary.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.4.1 | 1 | _pending_ | _pending_ | Create `tools/fixtures/blip/` dir + author `tools/scripts/blip-bake-fixtures.ts` — pure TypeScript port of `BlipVoice.Render` scalar loop (oscillator bank + AHDSR + one-pole LP; float32 math matching C# kernel) or Node shim invoking Unity batchmode. Bakes variant 0 for each of 10 MVP patch param sets (hardcoded from exploration §9 recipes). Writes `tools/fixtures/blip/{id}-v0.json` per id. Run once: `npx ts-node tools/scripts/blip-bake-fixtures.ts`; verify 10 JSON files produced. |
| T3.4.2 | 1 | _pending_ | _pending_ | `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` in existing `Blip.Tests.EditMode.asmdef` (Stage 1.4 — no new asmdef). `[TestFixture]` + one `[Test]` per `BlipId`: parse `tools/fixtures/blip/{id}-v0.json` via `System.IO.File.ReadAllText` + minimal JSON parse, re-render via `BlipVoice.Render` with same params, assert `SumAbsHash` within 1e-6 + zero-crossing count within ±2 + `patchHash` equality (fails if fixture is stale). |
| T3.4.3 | 2 | _pending_ | _pending_ | Promote `docs/blip-procedural-sfx-exploration.md` → `ia/specs/audio-blip.md`. Restructure to match `ia/specs/*.md` conventions (section numbering, header format). Add "Superseded by `ia/specs/audio-blip.md`" banner at top of exploration doc. `npm run validate:all` — checks dead spec refs + frontmatter. |
| T3.4.4 | 2 | _pending_ | _pending_ | `ia/specs/glossary.md` — add rows: **Blip variant** (per-patch randomized sound selection index 0..variantCount-1), **Blip cooldown** (minimum ms between same-id plays; enforced by `BlipCooldownRegistry`), **Bake-to-clip** (on-demand render of `BlipPatchFlat` to `AudioClip` via `BlipBaker.BakeOrGet`), **Patch flatten** (`BlipPatch` SO → `BlipPatchFlat` blittable mirror in `BlipCatalog.Awake`). Update all existing blip rows to cross-ref `ia/specs/audio-blip.md`. `npm run validate:all` green. |

---

## Deferred decomposition

- **Step 2 — Bake + facade + PlayMode smoke:** decomposed 2026-04-15. Stages: Bake-to-clip pipeline, Catalog + mixer router + cooldown registry + player pool, BlipEngine facade + main-thread gate, PlayMode smoke test.
- **Step 3 — Patches + integration + golden fixtures + promotion:** decomposed 2026-04-15. Stages: Patch authoring + catalog wiring, UI + Eco + Sys call sites, World lane call sites, Golden fixtures + spec promotion + glossary.

Do NOT pre-file Step 3 BACKLOG rows. Candidate-issue pointers live inline on each step's **Relevant surfaces** line; new-feature-row candidates surface during that step's decomposition pass, filed under `§ Audio / Blip lane` in `BACKLOG.md`.

Step 1 + Step 2 stages decomposed above w/ phases + tasks. Use `stage-file` skill to create BACKLOG rows + project spec stubs when a given stage → `In Progress`.

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
- Close this orchestrator via `/closeout` — orchestrators are permanent per `ia/rules/orchestrator-vs-spec.md`. Individual task specs close normally; stages close via `project-stage-close`; the umbrella orchestrator never deletes.

---

## Decision Log

> **Pattern:** append rows as stages close (via `project-stage-close`) or when orchestrator-level pivots surface in task kickoffs. Format: `{YYYY-MM-DD} — {short title}. {1–3 sentence rationale}. Source: {task id | stage id | kickoff | review}.`

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

## Lessons Learned

> **Pattern:** append rows as stages close, migrate actionable ones to canonical IA (`ia/specs/`, `ia/rules/`, glossary) via `project-stage-close` or `/closeout`. Keep the lesson here if it's orchestrator-local (applies only inside Blip MVP); promote if it generalizes. Format: `{YYYY-MM-DD} — {short title}. {1–3 sentence summary}. {Action: where promoted, or "orchestrator-local"}.`

- `2026-04-14 — Compress co-located tasks before filing.` When two pending tasks share the same implementation surface (same file, same loop), merge them into one TECH issue at stage-file time rather than filing both then closing one early. Avoids orphan issues + simplifies history. Action: orchestrator-local (Blip MVP).
- `2026-04-14 — BlipVoiceState carries all per-voice mutable DSP state.` `phaseA..D`, `envLevel`, `envStage`, `filterZ1`, `rngState`, `samplesElapsed` all live in a single blittable struct passed by ref — no statics, no heap alloc inside `Render`. Pattern validated by Stage 1.3; reuse for any future voice-type addition (e.g. `BlipLiveHost` post-MVP). Action: promote to `ia/specs/audio-blip.md` §DSP kernel when Step 3 spec-promotion runs.
- `2026-04-14 — Exponential τ = stageDuration/4 gives ≈98 % settled at stage end.` Validated analytically (`exp(-4) ≈ 0.018`). No tuning pass required for MVP; perceptual loudness log curve satisfied. Action: orchestrator-local (Blip MVP).

