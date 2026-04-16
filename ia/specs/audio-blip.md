---
purpose: "Reference spec for Blip — Procedural SFX synthesis subsystem."
audience: agent
loaded_by: ondemand
slices_via: spec_section
---
# Blip — Audio SFX Reference Spec

> Canonical architecture, DSP kernel, invariants, and runtime rules for the **Blip** procedural SFX subsystem. Covers MVP (baked-to-clip, 10 sounds) and post-MVP extension hooks. Detailed recipe tables + post-MVP sketches stay in [`docs/blip-procedural-sfx-exploration.md`](../../docs/blip-procedural-sfx-exploration.md).

## 1. Purpose

**Blip** synthesizes all SFX in-engine from parameter-only `ScriptableObject` patches. Zero `.wav` / `.ogg` files under `Assets/Audio/Sfx/`.

**MVP scope (v1 — shipped):** 10 baked sounds, `Baked` mode only, `BlipVoice.Render` kernel, one-pole LP filter, no FX chain, no LFOs. Live DSP (`OnAudioFilterRead`) deferred post-MVP.

**Non-goals (MVP):** music, dialogue, dynamic tension mix, VoIP, streamed ambience, live DSP, FX chain, LFOs, biquad filter, bit-exact LUT oscillators, custom editor window.

**Lifecycle:** exploration (`docs/blip-procedural-sfx-exploration.md`, 2026-04-13) → shipped (Step 1–3, blip-master-plan.md) → regression-gated (golden fixtures Stage 3.4) → spec-promoted here (2026-04-16).

For post-MVP deferred work see [`docs/blip-post-mvp-extensions.md`](../../docs/blip-post-mvp-extensions.md).

## 2. Domain vocabulary

| Term | Kind | Definition |
|------|------|-----------|
| **Blip** | subsystem | Procedural SFX synthesis subsystem. Covers one-shots + deferred continuous voices. "Blip" scope = any procedural SFX, not only short events. |
| **Blip id** | enum (`BlipId`) | Central SFX identifier. Prefixed by domain (`Ui*`, `Tool*`, `World*`, `Eco*`, `Sys*`). |
| **Blip patch** | `ScriptableObject` (`BlipPatch`) | Authored patch asset. Holds oscillators, envelope, filter, FX chain, jitter params, mixer group. `AnimationCurve`s used for editor tweaking — never touched on audio thread. |
| **Blip patch flat** | blittable struct (`BlipPatchFlat`) | Audio-thread-safe snapshot of a **Blip patch**. Curves pre-sampled to `float[]` LUTs by catalog on main thread. No managed refs. |
| **Blip voice** | static DSP kernel (`BlipVoice`) | Stateless `static` class. Single `Render` method; state passed by `ref BlipVoiceState`. Shared by **Blip baker** (offline) and **Blip live host** (post-MVP audio thread). |
| **BlipVoiceState** | blittable struct | Per-voice mutable DSP state: `phaseA..D`, `envLevel`, `envStage`, `filterZ1`, `rngState`, `samplesElapsed`. Passed by `ref` to `BlipVoice.Render`; lives in caller. No statics, no heap allocs inside `Render`. |
| **Blip catalog** | `MonoBehaviour` (`BlipCatalog`) | Maps `BlipId` → `BlipPatchFlat`. Flattens all patches on `Awake`. Owns `BlipMixerRouter` + `BlipCooldownRegistry`. Registers with `BlipEngine`. |
| **Blip engine** | static facade (`BlipEngine`) | Entry point. `Play`, `PlayAt`, `PlayLoop`, `Stop`, `SetParam`, `StopAll`. Stateless; asserts main thread. Resolves catalog/player/liveHost once via `FindObjectOfType` fallback, then caches. |
| **Blip baker** | plain class (`BlipBaker`) | Offline renders **Blip patch flat** to `AudioClip`. LRU cache keyed by `(patchHash, variantIndex)`. Memory budget default 4 MB. Main-thread-only. |
| **Blip player** | `MonoBehaviour` (`BlipPlayer`) | Pool of 16 `AudioSource`s. `PlayOneShot` wrapper. Round-robin cursor. |
| **Blip mixer router** | plain class (`BlipMixerRouter`) | Maps `AudioMixerGroup` enum to mixer group refs. Instantiated and populated by **Blip catalog** on `Awake`. Parallel to catalog — not a singleton (invariant #4). |
| **Blip mixer group** | `AudioMixerGroup` | Three MVP groups: `Blip-UI`, `Blip-World`, `Blip-Ambient`. Patches route via **Blip patch**`.mixerGroup`. |
| **Blip bootstrap** | prefab + `MonoBehaviour` | Persistent bootstrap `GameObject` (`DontDestroyOnLoad`). Carries **Blip catalog**, **Blip player**, and future **Blip live host** slots. Lives in main scene. |
| **patch hash** | `int` field on `BlipPatchFlat` | Content hash of serialized fields + LUT samples. Cache key for **Blip baker**. Not Unity GUID + version (fragile across upgrades). |

## 3. DSP kernel

### 3.1 Render signature

```csharp
public static void BlipVoice.Render(
    Span<float> buffer, int offset, int count,
    int sampleRate, in BlipPatchFlat patch,
    int variantIndex, ref BlipVoiceState state)
```

No managed allocs. No Unity API calls inside. Accumulates into `buffer[offset..offset+count]` (not write-only — supports mixing).

### 3.2 BlipVoiceState fields

`BlipVoiceState` is blittable; passed by `ref` from caller. Fields:

| Field | Type | Role |
|-------|------|------|
| `phaseA..phaseD` | `double` | Per-oscillator phase accumulator (0–1 cycle) |
| `envLevel` | `float` | Current envelope amplitude |
| `envStage` | `BlipEnvStage` | `Idle / Attack / Hold / Decay / Sustain / Release` |
| `filterZ1` | `float` | One-pole LP delay element |
| `rngState` | `uint` | xorshift PRNG seed — drives jitter + noise oscillator |
| `samplesElapsed` | `int` | Running count since NoteOn; drives envelope + pitch LUT lookup |

No statics, no heap inside `Render`. Pattern validated Stage 1.3; reuse for any future voice type (e.g. `BlipLiveHost`). Promoted from orchestrator Lessons 2026-04-14.

### 3.3 Oscillator bank (MVP)

MVP oscillators — `Math.Sin` path; LUT oscillators (`useLutOscillators = true`) reserved post-MVP:

| `BlipWaveform` | Implementation note |
|---|---|
| `Sine` | `Math.Sin(2π × phaseN)` |
| `Triangle` | Piecewise linear via phase |
| `Square` | Sign of sine |
| `Pulse` | `phase < pulseDuty ? 1 : -1` |
| `NoiseWhite` | Xorshift on `rngState` → uniform in `[-1, 1]` |

Phase accumulates: `phaseN += freq / sampleRate` per sample. Up to 4 oscillators per patch, summed.

### 3.4 AHDSR envelope

Stages: `Idle → Attack → Hold → Decay → Sustain → Release → Idle`.

Transitions driven by `samplesElapsed` + per-stage duration from patch. `OnValidate` clamps ensure all stage durations ≥ 1 ms (Decision Log 2026-04-13).

Per-stage `BlipEnvShape` selector:

| Shape | Formula |
|-------|---------|
| `Linear` | Straight ramp (attack 0→1, decay 1→sustain, release sustain→0) |
| `Exponential` | `target + (start − target) × exp(−t/τ)`, τ = stageDuration / 4 (≈98 % settled at stage end; perceptual loudness log curve) |

τ = stageDuration/4 validated analytically (`exp(−4) ≈ 0.018`). No tuning needed for MVP.

### 3.5 One-pole low-pass filter (MVP)

`y[n] = y[n-1] + α × (x[n] - y[n-1])` where `α = 1 - exp(-2π × cutoff / sampleRate)`.

`z1` stored in `BlipVoiceState.filterZ1`. `filter.kind == None` → `α = 1.0` passthrough (single kernel, no branch).

### 3.6 Jitter

Per-invocation: pitch ± `pitchJitterCents` (cents), gain ± `gainJitterDb` (dB), pan ± `panJitter`. Applied by xorshift on `rngState`. Honors `deterministic` flag — skip jitter + fix variant index to 0 when set.

## 4. Authoring

### 4.1 BlipPatch SO fields

`BlipPatch : ScriptableObject` — key authoring surface:

| Field | Unit | Notes |
|-------|------|-------|
| `oscillators[0..3]` | struct array | Up to 4 mixed oscillators |
| `envelope` | struct | AHDSR + `BlipEnvShape` per stage |
| `filter` | struct | Kind + cutoffHz + Q |
| `variantCount` | int | 1–8; round-robin on play |
| `pitchJitterCents` | cents | Per-invocation ± jitter |
| `gainJitterDb` | dB | Per-invocation ± jitter |
| `panJitter` | [-1..1] | Per-invocation ± jitter |
| `voiceLimit` | int | Max concurrent voices for this patch |
| `priority` | int | Higher = survives voice steal |
| `cooldownMs` | ms | Per-`BlipId` minimum inter-play gap |
| `deterministic` | bool | Disables jitter; test mode |
| `useLutOscillators` | bool | Bit-exact mode (post-MVP) |
| `mixerGroup` | ref | Target `AudioMixerGroup` |
| `patchHash` | int | Content hash of serialized fields + LUTs |

Full field list + `AnimationCurve` details: [`docs/blip-procedural-sfx-exploration.md §11.5`](../../docs/blip-procedural-sfx-exploration.md#115-variables--fields-blippatch-authoring-surface).

### 4.2 OnValidate clamps

`BlipPatch.OnValidate` enforces: `attackMs`, `holdMs`, `decayMs`, `releaseMs` all ≥ 1 ms. Prevents zero-duration stage divide-by-zero in envelope τ. Decision locked 2026-04-13.

### 4.3 patchHash policy

Hash computed from serialized numeric fields + pre-sampled LUT contents. Not Unity asset GUID + version (fragile across Unity upgrades). `BlipBaker` uses `(patchHash, variantIndex)` as cache key; stale-fixture guard compares fixture `patchHash` against recompute at test time.

### 4.4 Inspector-only workflow (MVP)

Authoring = tweaking SO fields in Inspector. No custom `EditorWindow` in MVP. Post-MVP editor window (waveform preview, spectrum, LUFS, A/B compare) gated on 20-patch authoring pain — see §8.

Recipe tables with concrete param examples: [`docs/blip-procedural-sfx-exploration.md §9`](../../docs/blip-procedural-sfx-exploration.md#9-twenty-concrete-examples).

## 5. Runtime architecture

### 5.1 Component map

```
[game code] → BlipEngine.Play(BlipId.UiClick)
                    │
                    ▼
              BlipCatalog.Resolve(id) → BlipPatchFlat
                    │
                    ▼
              [cooldown check via BlipCooldownRegistry]
                    │
                    ▼  (Baked mode, MVP)
              BlipBaker.BakeOrGet(patch, variantIndex) → AudioClip
                    │
                    ▼
              BlipPlayer.PlayOneShot(clip, pitch, gain, mixerGroup)
                    │
                    ▼
              AudioSource (pool of 16, round-robin)
```

`BlipVoice.Render` runs inside `BlipBaker` (offline bake, main thread). Post-MVP live path: `BlipLiveHost.OnAudioFilterRead` pulls from SPSC ring + calls `BlipVoice.Render` on audio thread.

### 5.2 Init order and lifecycle

1. `BlipBootstrap` prefab loaded (`DontDestroyOnLoad`). Carries `BlipCatalog`, `BlipPlayer`.
2. `BlipCatalog.Awake` — flattens all `BlipPatch` assets to `BlipPatchFlat`. Builds `BlipMixerRouter`. Calls `BlipEngine.Bind(this)`. Sets `isReady = true` last — scene-load suppression contract.
3. Game code calls `BlipEngine.Play(id)` — asserts main thread → checks `BlipCooldownRegistry` → `BlipCatalog.Resolve(id)` → `BlipBaker.BakeOrGet` → `BlipPlayer.PlayOneShot`.
4. On scene unload: `BlipCatalog.OnDestroy` → `BlipEngine.Unbind(this)`.

### 5.3 Thread rules

- `BlipEngine.Play*` — main-thread only; asserts `Thread.CurrentThread == mainThread`.
- `BlipVoice.Render` inside baker — main thread.
- All `AnimationCurve.Evaluate` calls — main thread only (Unity contract); never inside `Render`.
- `BlipPatchFlat` — blittable; safe to pass by `in` to any thread after catalog build.
- Post-MVP live path: `BlipVoice.Render` runs on audio thread via `OnAudioFilterRead`; zero managed allocs, no Unity API inside.

### 5.4 BlipMixerRouter placement rationale

`BlipMixerRouter` is a plain class instantiated and owned by `BlipCatalog`, not a separate `MonoBehaviour` singleton. Parallel to `BlipCatalog` — avoids new singleton per invariant #4. Decision locked 2026-04-13.

### 5.5 BlipCooldownRegistry placement

`BlipCooldownRegistry` lives on `BlipCatalog` (composition reference), not as a standalone singleton. Per-`BlipId` cooldown ms + last-play timestamp. Queried by `BlipEngine` before dispatch. Decision locked 2026-04-13.

## 6. Call-site integration

Other subsystems fire Blips via `BlipEngine.Play(BlipId)` on the main thread. Key integration points:

| Domain | BlipId | Notes |
|--------|--------|-------|
| MainMenu / HUD buttons | `UiButtonHover`, `UiButtonClick` | Cooldown 120 ms per button |
| Road draw (per-tile) | `ToolRoadTick` | Cooldown 30 ms; 4 variants round-robin |
| Road path complete | `ToolRoadComplete` | On `PathTerraformPlan` apply |
| Building place / deny | `ToolBuildingPlace`, `ToolBuildingDenied` | Hook placement result |
| Cell selected | `WorldCellSelected` | Cooldown 80 ms |
| Economy | `EcoMoneyEarned`, `EcoMoneySpent` | Gate on non-zero delta |
| Save / load | `SysSaveGame`, `SysLoadGame` | Suppress manual-save SFX during autosave burst |

Full subsystem impact table: [`docs/blip-procedural-sfx-exploration.md §8`](../../docs/blip-procedural-sfx-exploration.md#8-related-subsystems--impact--work).

Rate-limiting: per-id cooldown enforced in `BlipEngine` via `BlipCooldownRegistry`. Multi-select step capped at 8 Hz via per-id cooldown.

## 7. Fixture + regression gate

### 7.1 Fixture schema

`tools/fixtures/blip/{id}-v0.json` fields:

| Field | Type | Notes |
|-------|------|-------|
| `patchHash` | int | From `BlipPatchFlat.patchHash` at bake time |
| `sampleRate` | int | 48000 Hz |
| `sampleCount` | int | Samples in rendered buffer |
| `sumAbsHash` | float | Sum of absolute values of buffer |
| `zeroCrossings` | int | Zero-crossing count |

### 7.2 Tolerance

- `sumAbsHash` within 1e-6 absolute (covers `Math.Sin` / `Math.Exp` LSB drift across ARM vs x86).
- `zeroCrossings` within ±2.
- `patchHash` exact match — mismatch = stale fixture, message points at bake script.

Rationale: byte equality impractical across platforms; sum-of-abs tolerance + first-256 byte gate provides statistical collision resistance. Decision locked 2026-04-13.

### 7.3 Bake script

`tools/scripts/blip-bake-fixtures.ts` — TypeScript port of `BlipVoice.Render` scalar loop. Bakes variant 0 for each of 10 MVP patch param sets. Run: `npx ts-node tools/scripts/blip-bake-fixtures.ts`. Writes 10 JSON files to `tools/fixtures/blip/`.

### 7.4 EditMode test

`Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` — namespace `Territory.Tests.EditMode.Audio`. Parameterized `[TestCase(BlipId.*)]` × 10: parse fixture JSON via `JsonUtility.FromJson<BlipFixtureDto>`, load SO via `AssetDatabase.LoadAssetAtPath<BlipPatch>`, re-render via `BlipTestFixtures.RenderPatch(in flat, sampleRate=48000, seconds, variant)`, assert tolerance.

## 8. Post-MVP extensions pointer

Post-MVP deferred work tracked in [`docs/blip-post-mvp-extensions.md`](../../docs/blip-post-mvp-extensions.md). Four selected post-MVP steps from `ia/projects/blip-master-plan.md`:

- **Step 4** — Settings UI + SFX volume slider + mute toggle.
- **Step 5** — DSP kernel v2: FX chain, LFOs, biquad BP, param smoothing.
- **Step 6** — 10 additional patches (doubles catalog 10 → 20) + call sites.
- **Step 7** — `BlipPatchEditorWindow`: waveform preview, spectrum FFT, LUFS meter, A/B compare. Gated on Step 5 + Step 6 shipped.

Unity 6.3 `IAudioGenerator` interface re-evaluation on engine upgrade before shipping live DSP path — see master plan Decision Log 2026-04-13 + `docs/blip-post-mvp-extensions.md §2`.

## 9. Invariants + guardrails

The following invariants apply to all Blip subsystem code. Violations = reject in code review.

1. **No `FindObjectOfType` in hot loops** (`ia/rules/invariants.md` #3) — `BlipCatalog`, `BlipPlayer` refs cached in `Awake`. `BlipEngine` facade caches on first call.
2. **No new singletons** (`ia/rules/invariants.md` #4) — `BlipEngine` static facade is stateless (no singleton state). Instance state lives on `BlipCatalog` / `BlipPlayer` `MonoBehaviour`s. `BlipMixerRouter` and `BlipCooldownRegistry` owned by `BlipCatalog` (composition), not singletons.
3. **Main-thread-only ingress** — `BlipEngine.Play*` asserts `Thread.CurrentThread == mainThread`. Background thread calls = bug. SPSC queue design makes this assertion cheap.
4. **`BlipPatchFlat` stays blittable** — no managed refs (`AnimationCurve`, `string`, `UnityEngine.Object`) on audio thread. LUT handles are `int` indices into catalog-owned `float[]` pools.
5. **`OnValidate` clamps ≥ 1 ms per AHDSR stage** — enforced in `BlipPatch.OnValidate`. Prevents zero-duration stage divide-by-zero in τ formula.
6. **No allocs / no Unity API inside `BlipVoice.Render`** — kernel must be audio-thread-safe for post-MVP live path. Verified by test (Stage 1.4).

## 10. Cross-refs

- [`ia/specs/glossary.md`](glossary.md) — Audio block; term rows for **Bake-to-clip**, **Blip bootstrap**, **Blip cooldown**, **Blip mixer group**, **Blip patch**, **Blip patch flat**, **Blip variant**, **Patch flatten**, **patch hash**.
- [`ia/rules/invariants.md`](../rules/invariants.md) — invariants #3 (no `FindObjectOfType` per-frame) and #4 (no new singletons).
- [`ia/projects/blip-master-plan.md`](../projects/blip-master-plan.md) — orchestrator; lifecycle / stage history only.
- [`docs/blip-procedural-sfx-exploration.md`](../../docs/blip-procedural-sfx-exploration.md) — detailed recipes (§9), locked decisions (§13), post-MVP sketches (§10–§12, §15). Historical implementer reference; superseded by this spec for canonical architecture.
- [`docs/blip-post-mvp-extensions.md`](../../docs/blip-post-mvp-extensions.md) — deferred scope boundary for all post-v1 work.
