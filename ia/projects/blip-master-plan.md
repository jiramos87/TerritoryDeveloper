# Blip — Master Plan (MVP)

> **Status:** In Progress — Step 1 / Stage 1.1 archived (TECH-98..TECH-101 closed; stage ready for rollup)
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

**Status:** In Progress — Stage 1.2

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

- [ ] Phase 1 — Mixer asset + three groups + exposed SFX volume param.
- [ ] Phase 2 — Persistent bootstrap prefab + headless volume binding + scene-load suppression policy.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.1.1 | 1 | **TECH-98** | Done | Create `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer` — binary YAML, not hand-written). Three groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`), each routed through master. Expose master `SfxVolume` dB param (`Exposed Parameters` panel, default 0 dB). |
| T1.1.2 | 1 | **TECH-99** | Done | Headless SFX volume binding — `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)`. No Settings UI in MVP (visible slider + mute toggle deferred post-MVP per `docs/blip-post-mvp-extensions.md` §4). Key string constant on `BlipBootstrap`. |
| T1.1.3 | 2 | **TECH-100** | Done | `BlipBootstrap` GameObject prefab + `DontDestroyOnLoad(transform.root.gameObject)` in `Awake` (pattern per `GameNotificationManager.cs`). Empty Catalog / Player / MixerRouter / CooldownRegistry child slots (populated Step 2). Placed at root of `MainMenu.unity` (boot scene; build index 0 per `MainMenuController.cs`). |
| T1.1.4 | 2 | **TECH-101** | Done (archived) | Scene-load suppression policy — no Blip fires until `BlipCatalog.Awake` sets ready flag. Document in glossary rows for **Blip mixer group** + **Blip bootstrap**. |

#### Stage 1.2 — Patch data model

**Status:** In Progress — tasks filed (TECH-111..TECH-115 Draft)

**Objectives:** `BlipPatch` ScriptableObject authoring surface + `BlipPatchFlat` blittable mirror + content-hash. MVP skips all `AnimationCurve` fields (no pitch-env curve, no cutoff-env curve, no envelope shape curve) — AHDSR uses parametric ramps (linear or exp per `BlipEnvShape` enum, no curves), filter uses static cutoff Hz. Keeps Step 3 authoring simple + Step 1 scope tight. Curve / LUT infrastructure lands post-MVP per `docs/blip-post-mvp-extensions.md` §1. `BlipMode` enum omitted MVP (single implicit baked path) — added post-MVP when `BlipLiveHost` lands. `useLutOscillators` field reserved / unused MVP to prevent schema churn when post-MVP LUT osc lands.

**Exit:**

- `BlipPatch` SO w/ MVP fields — `oscillators[0..3]`, `envelope` (AHDSR w/ `BlipEnvShape` per-stage), `filter` (one-pole LP), `variantCount`, `pitchJitterCents`, `gainJitterDb`, `panJitter`, `voiceLimit`, `priority`, `cooldownMs`, `deterministic`, `mixerGroup` (ref — authoring only, not flattened), `durationSeconds`, `useLutOscillators` (reserved, unused MVP), `patchHash` (`[SerializeField] private int` — persisted). `CreateAssetMenu` attribute.
- MVP enums — `BlipId` (10 MVP rows + `None`), `BlipWaveform` (`Sine`, `Triangle`, `Square`, `Pulse`, `NoiseWhite`), `BlipFilterKind` (`None`, `LowPass`), `BlipEnvStage` (`Idle`, `Attack`, `Hold`, `Decay`, `Sustain`, `Release`), `BlipEnvShape` (`Linear`, `Exponential`).
- `BlipPatchFlat` blittable readonly struct mirrors SO scalars. No `AnimationCurve`. No `AudioMixerGroup` ref (separate `BlipMixerRouter` owns `BlipId → AudioMixerGroup` map — see Step 2). No managed refs. `mixerGroupIndex` int slot reserved.
- `patchHash` = content hash over serialized fields. Stable across Unity GUID churn + version bumps. Persisted as `[SerializeField] private int` + recomputed on `OnValidate`; re-verified on `Awake` (assert matches recompute; log warning on mismatch).
- Attack/decay/release timing clamp in `OnValidate` — min 1 ms per stage (≈48 samples @ 48 kHz) to prevent snap-onset click. Sustain-only case uses A=1 ms / D=0 / R=1 ms.
- Glossary rows for **Blip patch**, **Blip patch flat**, **patch hash**.

**Phases:**

- [ ] Phase 1 — `BlipPatch` SO authoring surface + MVP enums + `OnValidate` clamps.
- [ ] Phase 2 — `BlipPatchFlat` flatten + content-hash persistence.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.2.1 | 1 | **TECH-111** | Draft | `BlipPatch : ScriptableObject` class + MVP fields + `CreateAssetMenu("Territory/Audio/Blip Patch")`. No `AnimationCurve` fields. No `mode` field (`BlipMode` enum deferred post-MVP). `useLutOscillators` bool present but unread (reserved slot). |
| T1.2.2 | 1 | **TECH-112** | Draft | MVP struct + enum definitions — `BlipOscillator` (no `pitchEnvCurve`), `BlipEnvelope` (no `shape` curve; per-stage `BlipEnvShape` + `sustainLevel`), `BlipFilter` (no `cutoffEnv`) + `BlipId`, `BlipWaveform`, `BlipFilterKind`, `BlipEnvStage`, `BlipEnvShape` (`Linear`, `Exponential`). |
| T1.2.3 | 1 | **TECH-113** | Draft | `OnValidate` guards on `BlipPatch` — clamp `attackMs`, `decayMs`, `releaseMs` to ≥ 1 ms (kills snap-onset click at default 48 kHz mix rate). Clamp `variantCount` to 1..8, `voiceLimit` to 1..16, `sustainLevel` to 0..1, `cooldownMs` to ≥ 0. |
| T1.2.4 | 2 | **TECH-114** | Draft | `BlipPatchFlat` blittable readonly struct — mirrors SO scalars; no managed refs; no `AudioMixerGroup` ref (held in `BlipMixerRouter` parallel to catalog — Step 2). `BlipOscillatorFlat` / `BlipEnvelopeFlat` / `BlipFilterFlat` nested. Single `mixerGroupIndex` int slot. |
| T1.2.5 | 2 | **TECH-115** | Draft | `patchHash` content hash — FNV-1a / xxhash64 digest over serialized scalar fields (osc freqs, env timings, env shapes, filter cutoff, jitter values, cooldown). Stable; ignores Unity GUID + version. `[SerializeField] private int patchHash` persisted on `OnValidate`; `Awake` (or first flatten) recomputes + asserts match. Glossary rows for **Blip patch**, **Blip patch flat**, **patch hash**. |

#### Stage 1.3 — Voice DSP kernel

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** `BlipVoice.Render` kernel. Single static method, stateful via `ref BlipVoiceState`. MVP oscillator bank + AHDSR envelope (per-stage `Linear` or `Exponential` shape) + one-pole LP filter. Per-invocation pitch / gain / pan jitter. No allocs inside `Render`. No Unity API. Shared kernel — used by `BlipBaker` Step 2 + `BlipLiveHost` post-MVP.

**Exit:**

- `BlipVoice` static class — `Render(Span<float> buffer, int offset, int count, int sampleRate, in BlipPatchFlat patch, int variantIndex, ref BlipVoiceState state)`.
- Oscillators — sine, triangle, square, pulse (duty 0–1), noise-white (xorshift RNG on `BlipVoiceState.rngState`). `Math.Sin` path MVP; LUT osc reserved post-MVP per `docs/blip-post-mvp-extensions.md` §1.
- AHDSR envelope state machine — `Idle → Attack → Hold → Decay → Sustain → Release → Idle`. Per-stage shape selectable via `BlipEnvShape` (`Linear` = straight ramp; `Exponential` = `1 - exp(-t/τ)` on attack, `exp(-t/τ)` on decay/release, τ = stage duration / 4 — reads "natural" to ear per perceptual loudness log curve).
- One-pole LP filter — `z1` on `BlipVoiceState`; cutoff from patch scalar. `filter.kind == None` handled via alpha=1 passthrough (single kernel, no branch).
- Jitter applied per-invocation — `pitchJitterCents`, `gainJitterDb`, `panJitter`. Honors `deterministic` flag (skip jitter + use fixed variant index).
- Zero managed allocs inside `Render` (verified via test; see Stage 1.4 T1.4.7 for measurement method).
- No Unity API calls inside `Render` (no `Time.time`, no `Debug.Log`).

**Phases:**

- [ ] Phase 1 — Oscillator bank + voice state.
- [ ] Phase 2 — AHDSR envelope state machine + per-stage shape.
- [ ] Phase 3 — Render driver (LP filter + jitter + per-sample loop).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.3.1 | 1 | _pending_ | _pending_ | `BlipVoiceState` struct — `phaseA..phaseD` (double), `envLevel`, `envStage`, `filterZ1`, `rngState` (xorshift seed), `samplesElapsed`. Blittable; lives in caller. |
| T1.3.2 | 1 | _pending_ | _pending_ | Oscillator bank — sine (`Math.Sin` MVP), triangle, square, pulse (duty param), noise-white (xorshift on `rngState`). Phase-accumulator; frequency from patch osc + `pitchMult`. |
| T1.3.3 | 2 | _pending_ | _pending_ | AHDSR stage machine — `Idle → Attack → Hold → Decay → Sustain → Release → Idle`. Transitions via `samplesElapsed` + per-stage duration from patch (durations already ≥ 1 ms by `BlipPatch.OnValidate` clamp — see T1.2.3). |
| T1.3.4 | 2 | _pending_ | _pending_ | Envelope level math — per-stage `BlipEnvShape` selector. Linear: straight ramp (attack 0→1, decay 1→sustain, release sustain→0). Exponential: `target + (start - target) * exp(-t/τ)` with τ = stageDuration/4 (≈98 % settled at stage end; perceptual linear). Multiplies output sample. |
| T1.3.5 | 3 | _pending_ | _pending_ | One-pole LP filter in-loop — `y[n] = y[n-1] + a * (x[n] - y[n-1])` where `a = 1 - exp(-2π * cutoff / sampleRate)`. `z1` on `BlipVoiceState`. `filter.kind == None` → `a = 1.0` (passthrough, single kernel, no branch). |
| T1.3.6 | 3 | _pending_ | _pending_ | `BlipVoice.Render` driver — per-sample loop: osc sum × envelope × filter → buffer. Uses `ref state`. Zero alloc verified. |
| T1.3.7 | 3 | _pending_ | _pending_ | Per-invocation jitter — pitch cents ± jitter, gain dB ± jitter, pan ± jitter. Honors `deterministic` flag. RNG from `rngState` (xorshift, seeded deterministically per variant + voice). |

#### Stage 1.4 — EditMode DSP tests

**Status:** Draft (tasks _pending_ — not yet filed)

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

- [ ] Phase 1 — Test asmdef + fixture helpers.
- [ ] Phase 2 — Oscillator + envelope + silence assertions.
- [ ] Phase 3 — Determinism + no-alloc regression tests.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.4.1 | 1 | _pending_ | _pending_ | `Assets/Tests/EditMode/Audio/Blip.Tests.EditMode.asmdef` — refs `Blip` runtime + `UnityEngine.TestRunner` + `nunit.framework`. Platform: `Editor` only. |
| T1.4.2 | 1 | _pending_ | _pending_ | Test fixture helpers — `RenderPatch(in BlipPatchFlat, int sampleRate, int seconds) → float[]`, `CountZeroCrossings(float[]) → int`, `SampleEnvelopeLevels(float[], int stride) → float[]`, `SumAbsHash(float[]) → double`. |
| T1.4.3 | 2 | _pending_ | _pending_ | Oscillator zero-crossing tests — sine @ 440 Hz × 1 s @ 48 kHz ≈ 880 crossings (± 2). Repeat triangle / square / pulse duty=0.5. |
| T1.4.4 | 2 | _pending_ | _pending_ | Envelope shape tests — both `Linear` + `Exponential` shapes. A=50ms/H=0/D=50ms/S=0.5/R=50ms. Assert attack monotonic rising, decay monotonic falling to sustain, release monotonic falling to zero. Exponential-shape extra assert — attack slope in first quarter > slope in last quarter. |
| T1.4.5 | 2 | _pending_ | _pending_ | Silence test — `gainMult = 0` → all-zero buffer (exact equality, not tolerance). |
| T1.4.6 | 3 | _pending_ | _pending_ | Determinism test — render same patch + seed + variant twice; assert `SumAbsHash` equal within 1e-6 + first 256 samples byte-equal. Validates voice-state reset + RNG determinism without depending on JIT stability of trailing samples. |
| T1.4.7 | 3 | _pending_ | _pending_ | No-alloc regression — warm-up loop (3 renders, discard allocation), then measure `GC.GetAllocatedBytesForCurrentThread` delta across 10 steady-state renders; assert delta constant ≤ 0 bytes/call (tolerates NUnit infra alloc outside the measured window). |

**Backlog state (Step 1):** All Step 1 task rows stay in this doc as `_pending_`. File BACKLOG rows + project specs when parent stage → `In Progress` via `stage-file` skill. Stages 2.x + 3.x task decomposition deferred until Step 2 + Step 3 open.

### Step 2 — Bake + facade + PlayMode smoke

**Status:** Draft (decomposition deferred until Step 1 → `Final`)

Bake-to-clip pipeline + runtime facade. After Step 2: `BlipEngine.Play(BlipId)` dispatches through catalog → cached `AudioClip` via baker → pooled `AudioSource` via player. Playable from game code, but no call sites wired yet + no fixtures (those land Step 3).

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

**Relevant surfaces:** Step 1 outputs (`BlipPatch`, `BlipPatchFlat`, `BlipVoice.Render`, `BlipMixer.mixer`, `BlipBootstrap` prefab). Unity `AudioSource` + `AudioClip.Create` + `AudioSettings.dspTime` APIs. `ia/rules/invariants.md` #3 + #4. New PlayMode test asmdef under `Assets/Tests/PlayMode/Audio/`.

### Step 3 — Patches + integration + golden fixtures + promotion

**Status:** Draft (decomposition deferred until Step 2 → `Final`)

Author 10 MVP patches + wire to call sites + golden fixture harness + glossary + spec promotion. After Step 3: player hears Blip in game, DSP output is regression-gated by hash fixtures, subsystem promoted from `docs/` exploration to `ia/specs/audio-blip.md`.

**Stage skeleton (decomposed when Step 3 opens):**

- **Stage 3.1 — Patch authoring + catalog wiring.** Ten `BlipPatch` SO assets under `Assets/Audio/BlipPatches/` per exploration §9 recipes. `BlipCatalog.entries` wired in Inspector. Each patch routes to correct mixer group per exploration §14.
- **Stage 3.2 — UI lane call sites.** `BlipEngine.Play` at MainMenu button hover / click. No per-frame lookups (invariant #3). Uses existing `UiButtonHover` + `UiButtonClick` ids.
- **Stage 3.3 — World lane call sites.** Road-draw tool per-tile commit + plan-apply (`ToolRoadTick` cooldown 30 ms; `ToolRoadComplete` on stroke end). Building placement confirm + denial. `GridManager` single-cell select (cooldown 80 ms). Honor invariant #3 (cache `BlipEngine` access path if per-frame; facade self-caches).
- **Stage 3.4 — Eco + Sys call sites.** Money ledger earn + spend. Save-complete hook (`SysSaveGame`, 2 s cooldown on manual save — MVP has no autosave wiring, so no autosave-burst suppression needed).
- **Stage 3.5 — Golden fixtures + spec promotion + glossary.** Fixture harness + regeneration script + exploration → spec promotion + glossary rows.

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

**Relevant surfaces:**
- Exploration doc §9 (20 concrete examples — MVP recipes match 1, 2, 5, 6, 9, 10, 15, 17, 18, 20).
- Exploration doc §8 (related subsystems — call site map).
- Call-site host files: `Assets/Scripts/UI/MainMenu/*`, `Assets/Scripts/Tools/RoadTool*`, building placement flow, `Assets/Scripts/GridManager.cs` (selection hook), money ledger, `Assets/Scripts/SaveSystem/*`.
- `tools/fixtures/blip/` (new fixture dir) + `tools/scripts/` (new fixture-bake script).
- `ia/specs/audio-blip.md` (new spec; promoted from exploration).
- `ia/specs/glossary.md` (new rows).

---

## Deferred decomposition

Steps 2 + 3 stay at skeleton granularity (Objectives implicit in step blurb + Exit criteria + Relevant surfaces). Full Stage / Phase / Task decomposition lands when parent step → `In Progress`. Do NOT pre-file Step 2 / Step 3 BACKLOG rows. Candidate-issue pointers live inline on each step's **Relevant surfaces** line; new-feature-row candidates surface during that step's decomposition pass, filed under `§ Audio / Blip lane` in `BACKLOG.md`.

Step 1 stages 1.1–1.4 already decomposed above w/ phases + tasks but rows not yet filed. Use `stage-file` skill to create BACKLOG rows + project spec stubs when a given stage → `In Progress`.

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

## Lessons Learned

> **Pattern:** append rows as stages close, migrate actionable ones to canonical IA (`ia/specs/`, `ia/rules/`, glossary) via `project-stage-close` or `/closeout`. Keep the lesson here if it's orchestrator-local (applies only inside Blip MVP); promote if it generalizes. Format: `{YYYY-MM-DD} — {short title}. {1–3 sentence summary}. {Action: where promoted, or "orchestrator-local"}.`

_(Empty. First entries land when Stage 1.1 → `Final`.)_

