# Blip — Procedural SFX Exploration

Status: exploration / pre-spec
Owner: Javier
Date: 2026-04-13
Revision: r2 (post opus review)

## 0. Context

Territory = 2D iso city builder. Owner = synth-head. Wants SFX (not music) synthesized in-engine, not loaded as `.wav`/`.ogg`. Doc scopes feasibility, weighs two shapes, picks one, spells out arch + examples + integration cost.

Out of scope: music, dialogue, dynamic tension mix, VoIP, streamed ambience.

---

## 1. Recommended Approach — Bake-to-Clip Hybrid

### 1.1 Summary

Procedural synth engine. Renders deterministic `float[]` buffers from param-only patches. Caches results as Unity `AudioClip` at first-use (lazy) or at boot (eager allowlist). Playback = `AudioSource.PlayOneShot` on pooled sources. Escape hatch = `OnAudioFilterRead` live DSP, reserved for continuous-mod voices only.

Primary path: offline bake → `AudioClip` → `PlayOneShot`.
Escape hatch: online DSP via `OnAudioFilterRead` for live-mod voices only.

### 1.2 Requirements

- **Deterministic synth** — same patch + seed → same samples. Golden fixtures diff via tolerance hash (sum-of-absolutes with epsilon), not byte equality — `Math.Sin`/`Math.Exp` drift LSB across ARM vs x86. LUT-based oscillators recommended for bit-exact runs.
- **Param-only patches** — `ScriptableObject`. No `.wav` under `Assets/Audio/Sfx/`.
- **Patch flattening** — before audio thread touches anything, catalog flattens `BlipPatch` assets into `BlipPatchFlat` blittable structs. `AnimationCurve` becomes pre-sampled `float[]` LUTs. `AnimationCurve.Evaluate` is main-thread-only + sometimes allocates; must never run on audio thread.
- **Thread-safe live path** — `OnAudioFilterRead` on audio thread. Zero managed allocs. No Unity API calls inside. Ring buffer main → audio thread.
- **Main-thread-only ingress** — `BlipEngine.Play*` asserts `Thread.CurrentThread == mainThread`. Keeps queue SPSC. Background thread calls = bug. Policy doc'd in glossary + catalog comment.
- **Mixer integration** — each patch routes to an `AudioMixerGroup` (`Blip-UI` / `Blip-World` / `Blip-Ambient`). Respects SFX volume setting.
- **Budgeted cache** — baked clips memory-capped (default 4 MB). LRU eviction when exceeded. Patch-hash key for invalidation = digest of serialized fields (stable), not Unity GUID + version (fragile).
- **Authoring loop** — editor tool previews patch without Play Mode. Rebakes on patch change. Shows waveform + LUFS.
- **Variation** — per-invocation pitch/gain jitter + round-robin variant clip selection. Prevents phaser on identical consecutive plays.
- **Determinism toggle** — per-patch flag disables jitter. Used for test fixtures and golden runs.
- **Global cooldown policy** — per-`BlipId` minimum interval (ms). Rate-limits high-frequency triggers (multi-select, per-tile ticks). Enforced in `BlipEngine` before dispatch.
- **Lifecycle hooks** — `OnApplicationPause(true)` → flush live voices + `AudioListener.pause`. `OnApplicationFocus(false)` → optional mute. Scene-load suppresses Blip until `BlipCatalog.Awake` ready.
- **No `FindObjectOfType` in hot loops** — invariant 3. Cache `BlipCatalog` / `BlipPlayer` / `BlipLiveHost` refs in `Awake`.
- **No new singletons** — invariant 4. State lives on `MonoBehaviour` components. `BlipEngine` static = facade only; resolves instances once via `FindObjectOfType` fallback, then caches. Documented in §5.

### 1.3 Implementation Points

- `BlipPatch : ScriptableObject` — oscillators, envelope, filter, fx chain, mode (Baked/Live), mixer group, jitter, variant count, cooldown ms, priority, voice limit, determinism flag.
- `BlipPatchFlat` — blittable snapshot of `BlipPatch` with curves pre-sampled to LUTs. Built main-thread. Consumed by `BlipVoice` on either path.
- `BlipVoice` — DSP kernel. `Render(ref BlipVoiceState, Span<float> out, ...)`. Stateful via passed `ref state`. Shared between baker (offline) and live host (audio thread).
- `BlipBaker` — for `Baked` patches, runs `BlipVoice` offline against a fresh buffer, wraps via `AudioClip.Create` (main thread), caches by `(patchHash, variantIndex)`. LRU eviction. `BakeOrGet(patch, variantIndex) → AudioClip`.
- `BlipCatalog : MonoBehaviour` — `SerializeField` array maps `BlipId` → `BlipPatch`. `Awake` flattens all patches to `BlipPatchFlat` + registers with `BlipEngine`.
- `BlipPlayer : MonoBehaviour` — pool of ~16 `AudioSource`s, round-robin. `PlayOneShot(AudioClip, pitch, gain, mixerGroup)`.
- `BlipLiveHost : MonoBehaviour` — single `AudioSource` with silent clip, `OnAudioFilterRead` pulls from SPSC ring, renders active voices. Owns `BlipVoiceSlot[]` pool + pooled delay-line storage.
- `BlipEventQueue` — SPSC ring of `BlipEvent` structs. `Enqueue` asserts main thread.
- `BlipEngine` — static facade. `Play`, `PlayAt`, `PlayLoop → BlipHandle`, `Stop`, `SetParam`, `StopAll`. Dispatches to catalog + player + live host. Resolves instances lazily, caches. State = zero (no singleton state).
- `BlipId` — central enum. Grow policy: add rows under clear prefix (`Ui*`, `Tool*`, `World*`, `Eco*`, `Sys*`). Accept churn cost. Reconsider nested-namespace enum if rows > 128.

---

## 2. Critique of Recommended Approach

- **Authoring friction vs. `.wav`** — first `.wav` = drag-and-drop. First synth patch = tune params. Synth pays off via variation, consistency, proc control — not for one-off foley.
- **Organic ceiling** — pure synth splashes/crunches skew sterile vs. sampled reality. Lo-fi aesthetic hides this; ceiling still exists.
- **Two code paths** — Baked + Live doubles surface. Mitigation: single `BlipVoice` DSP kernel, shared by both.
- **Cache management** — budget + LRU + variant tracking = infra weight. For a ship-fast SFX pass this is non-trivial.
- **Audio-thread hazards** — `OnAudioFilterRead` bugs = silent crashes, hard fails. `AnimationCurve`, allocs, locks, Unity API all forbidden inside. Flatten-to-struct pass critical.
- **Determinism cost** — bit-exact across CPUs requires LUT oscillators (no `Math.Sin` in audio path) or tolerance-hash fixtures. Either way, a constraint.
- **No middleware** — no FMOD/Wwise = no mix snapshots, no profiler, no parameter automation beyond what we build. Accepted for lo-fi scope; blocks future middleware adoption unless we wrap behind an interface.
- **Patch hash fragility** — Unity asset GUID + version unstable across Unity upgrades. Must hash serialized fields directly. Non-trivial for nested `AnimationCurve`s (hash sampled LUT, not the curve).
- **WebGL/iOS lifecycle** — WebGL autoplay policy can drop the first Blip post-load until user gesture. iOS audio-session interruption (call, Siri) must flush live voices. Editor domain reload wipes static facade state.
- **Low-end warm-up** — eager bake = 20 patches × 4 variants × ~200 ms @ 48 kHz ≈ 7.7 M samples of DSP at boot. Chromebook/low-end hardware notices. Mitigation: lazy bake + priority pre-warm allowlist.

---

## 3. Alternate Approach — Pure Live DSP via `OnAudioFilterRead`

### 3.1 Summary

All SFX synthesized live on audio thread. No baked clips. One or few "host" `AudioSource`s with `OnAudioFilterRead`. Voice pool handles polyphony. Main thread enqueues `BlipEvent` structs; audio thread drains queue + mixes active voices into each buffer.

### 3.2 Requirements

- Lock-free SPSC ring main → audio thread. Main-thread assert on `Enqueue`.
- Voice pool with priority-aware steal policy (oldest non-priority voice dies first).
- Per-trigger xorshift PRNG seed, seeded externally for determinism.
- Bounded per-buffer DSP budget (hard cap ~32 voices).
- `BlipPatchFlat` pre-flattened at load. No `AnimationCurve`, no allocs, no Unity API inside voices.
- Mixer routing via one host source per group, or single host with per-voice gain (no true middleware sends).
- DC blocker + param smoothing on every live voice to prevent zipper noise when main thread yanks params.

### 3.3 Implementation Points

- `BlipLiveHost` = only playback surface.
- `BlipVoice[]` pool allocated once at scene init. Pooled delay-line buffers attached by handle index.
- `BlipEvent` struct: `{ PatchHandle, Pitch, Gain, VoiceId, Kind, ParamId, ParamValue, Seed }`.
- Audio thread loop:
  ```
  drain queue → voice slots
  for each active voice:
    render via BlipVoice.Render(ref state, out scratch)
    mix scratch * voice.gain into host output
  ```
- No `AudioClip`s except silent placeholder on host.

---

## 4. Comparison

| Axis                          | Bake-to-Clip Hybrid           | Pure Live DSP                      |
| ----------------------------- | ----------------------------- | ---------------------------------- |
| Engineering effort            | Medium (two paths)            | High (audio-thread discipline)     |
| CPU at runtime                | Near-zero (PlayOneShot)       | Low-Medium (N voices render)       |
| Memory at runtime             | ~1–4 MB cache                 | Negligible                         |
| Bake latency                  | First-use spike or warm-up    | None                               |
| Continuous modulation         | Escape hatch only             | Native                             |
| Unity mixer integration       | Native (AudioSource group)    | Native (one source per group)      |
| Debuggability                 | Baked clips inspectable       | Audio-thread bugs silent & severe  |
| Crash blast radius            | Low                           | High                               |
| Variation / round-robin       | Bake N variants               | Trivial per-event                  |
| Determinism                   | Hash-keyed cache              | Seeded PRNG + LUT osc              |
| Ship velocity                 | Faster to first SFX           | Slower to first SFX                |
| Aligns with lo-fi scope       | Yes                           | Yes, but overkill                  |

---

## 5. Decision

**Ship Bake-to-Clip Hybrid.** Live DSP reserved for short allowlist (sliders, engine loops, continuous drones).

Reasoning:

1. 90% of planned SFX = short one-shots. Bake-to-clip = zero audio-thread risk.
2. Shared `BlipVoice` kernel — two paths, one DSP. Surface cost small.
3. Live path gated behind `BlipMode.Live`. Most patches never touch it.
4. `AudioSource.PlayOneShot` gives pool + 3D spatialize + mixer routing for free.
5. Baked clips visible as `AudioClip` refs → Inspector-previewable.

Singleton discipline (invariant 4): `BlipEngine` is a **stateless static facade**, not a singleton. Instance state lives on `BlipCatalog` / `BlipPlayer` / `BlipLiveHost` MonoBehaviours, placed in scene. Facade finds them once via `FindObjectOfType` fallback on first call, then caches refs. Catalog registers itself on `Awake` to short-circuit the fallback. Matches existing Inspector + `FindObjectOfType` pattern in the codebase.

Risk accepted: audio-thread bugs in live path = scary. Mitigation: keep live path small, unit-test `BlipVoice` offline against golden LUTs, gate merges touching `BlipLiveHost` behind smoke test.

---

## 6. Feature List (v1 target scope)

**Oscillators**
- Sine, triangle, square, saw, pulse (duty 0–1), white noise, pink noise.
- LUT-backed for determinism (optional mode; `Math.Sin` path kept for authoring speed).
- Per-osc: frequency, phase, gain, pitch-env (linear/exp), pulse duty.
- Osc mix bus: up to 4 per patch, summed.

**Envelopes**
- AHDSR (attack / hold / decay / sustain / release). ADSR = AHDSR with `holdMs = 0`. One mode, two presets.
- Loopable envelope for live drones (loops sustain segment).

**Filters**
- One-pole low-pass, high-pass.
- Biquad band-pass with Q.
- Time-varying cutoff (cutoff env, pre-sampled LUT).
- Filter state struct holds up to 2 delay elements (`z1`, `z2`) — unused for one-pole.

**Modulators**
- LFO per patch (up to 2): waveform + rate Hz + depth. Routes to `Pitch` / `Gain` / `FilterCutoff` / `Pan`.
- Param smoothing on live-mod params (1-pole 20 ms). Prevents zipper noise.

**FX chain**
- Bit-crush (N-bit quantize).
- Sample-rate reduction (hold-and-sample).
- Ring modulation.
- Single-tap feedback delay (pooled delay line).
- Short comb (2–30 ms, multi-tap variant for granular crumble).
- Allpass (for metallic shimmer).
- Chorus / flanger (short LFO-modulated delay).
- Soft clip / tanh.
- DC blocker (always-on tail of fx chain on live path).

**Performance features**
- Baked variant count (1–8) + round-robin.
- Pitch jitter (cents), gain jitter (dB), pan jitter.
- Voice limiter per-patch (max concurrent).
- Priority field (voice-steal).
- Voice-steal crossfade (short 5 ms fade-out on stolen voice) = avoids click.
- Global per-`BlipId` cooldown.

**Integration features**
- Mixer group routing (`Blip-UI`, `Blip-World`, `Blip-Ambient`).
- 3D spatialize toggle (world-position overload).
- Sidechain duck ambient when world bus peaks (v2).
- `AudioListener.pause` honored. `OnApplicationPause(true)` → flush live voices. `OnApplicationFocus(false)` → optional mute.

**Editor features**
- Patch preview button (offline render + play).
- Waveform render in Inspector.
- Spectrum view.
- A/B patch compare.
- LUFS meter on preview + per-patch target (prevents gain drift).
- Auto-rebake on patch change.
- Patch hash shown in Inspector.

**Testability**
- Golden-sample tests: render patch → tolerance hash (sum-of-abs, epsilon) vs fixture. Byte equality optional when LUT-osc mode active.
- Determinism toggle per-patch.
- Headless bake for CI.

---

## 7. Architecture & Pseudo-code

### 7.1 Component map

```
[game code] → BlipEngine.Play(BlipId.UiClick)
                    │
                    ▼
              BlipCatalog.Resolve(id) → BlipPatchFlat
                    │
         ┌──────────┴──────────┐
         ▼                     ▼
   patch.mode == Baked    patch.mode == Live
         │                     │
         ▼                     ▼
    BlipBaker            BlipLiveHost
    .BakeOrGet            .Enqueue(BlipEvent)   [main thread assert]
         │                     │
         ▼                     ▼
    AudioClip            SPSC ring
         │                     │
         ▼                     ▼
    BlipPlayer          OnAudioFilterRead
    .PlayOneShot         (audio thread)
    (AudioSource pool)         │
                               ▼
                    for each voice slot:
                      BlipVoice.Render(ref state, ...)
                      mix into host buffer
```

Both paths share `BlipVoice` — one DSP kernel.

### 7.2 Key types (pseudo-C#)

```csharp
public enum BlipId {
    None = 0,

    // UI
    UiButtonHover, UiButtonClick, UiTabSwitch, UiTooltipAppear,
    UiModalOpen, UiModalClose, UiToastInfo, UiToastError,
    UiSliderScrub, UiInputFeedback, UiDragStart, UiDragEnd,
    UiAchievementUnlock, UiTutorialPrompt,

    // Tools
    ToolRoadTick, ToolRoadComplete, ToolRoadErase,
    ToolDemolish, ToolBuildingPlace, ToolBuildingDenied,
    ToolWaterPaint, ToolTerrainRaise, ToolTerrainLower,

    // World events
    WorldCliffCreated, WorldCellSelected, WorldMultiSelectStep,
    WorldCameraClamp,

    // Economy
    EcoMoneyEarned, EcoMoneySpent,

    // System
    SysSaveGame, SysLoadGame, SysAutosaveIndicator,
    SysUndo, SysRedo,
}

public enum BlipMode     { Baked, Live }
public enum BlipWaveform { Sine, Triangle, Square, Saw, Pulse, NoiseWhite, NoisePink }
public enum BlipFilterKind { None, LowPass, HighPass, BandPass }
public enum BlipFxKind   { None, BitCrush, SampleRate, RingMod, Delay, Comb, Allpass, Chorus, Flanger, SoftClip, DcBlocker }
public enum BlipParam    { Pitch, Gain, FilterCutoff, Pan, LfoRate, LfoDepth }

[CreateAssetMenu(menuName = "Territory/Audio/Blip Patch")]
public sealed class BlipPatch : ScriptableObject {
    public BlipMode mode;
    public BlipOscillator[] oscillators;        // up to 4
    public BlipEnvelope envelope;
    public BlipFilter filter;
    public BlipLfo[] lfos;                      // up to 2
    public BlipFx[] fxChain;                    // up to 4
    public float durationSeconds;
    public int variantCount = 1;
    public float pitchJitterCents;
    public float gainJitterDb;
    public float panJitter;
    public int voiceLimit = 8;
    public int priority = 0;
    public float cooldownMs;
    public bool deterministic;                  // disables jitter
    public bool useLutOscillators;              // bit-exact mode
    public AudioMixerGroup mixerGroup;
    public int patchHash;                       // computed from serialized fields
}

[Serializable] public struct BlipOscillator {
    public BlipWaveform waveform;
    public float frequencyHz;
    public float pitchEnvStartSemitones;
    public float pitchEnvEndSemitones;
    public AnimationCurve pitchEnvCurve;        // editor only
    public float gain;
    public float pulseDuty;
}

[Serializable] public struct BlipEnvelope {
    public float attackMs;
    public float holdMs;
    public float decayMs;
    public float sustainLevel;
    public float releaseMs;
    public bool loop;
    public AnimationCurve shape;                // editor only
}

[Serializable] public struct BlipFilter {
    public BlipFilterKind kind;
    public float cutoffHz;
    public float q;
    public AnimationCurve cutoffEnv;            // editor only
}

[Serializable] public struct BlipLfo {
    public BlipWaveform waveform;
    public float rateHz;
    public float depth;
    public BlipParam routeTo;
}

[Serializable] public struct BlipFx {
    public BlipFxKind kind;
    public float paramA;
    public float paramB;
    public float paramC;
}

// Blittable, audio-thread-safe. Built by catalog from BlipPatch on main thread.
public readonly struct BlipPatchFlat {
    public readonly BlipMode mode;
    public readonly BlipOscillatorFlat osc0, osc1, osc2, osc3;
    public readonly BlipEnvelopeFlat env;
    public readonly BlipFilterFlat filter;
    public readonly BlipLfoFlat lfo0, lfo1;
    public readonly BlipFxFlat fx0, fx1, fx2, fx3;
    public readonly float durationSeconds;
    public readonly int variantCount;
    public readonly int voiceLimit;
    public readonly int priority;
    public readonly float cooldownMs;
    public readonly BlipLutHandle pitchLut;     // pre-sampled from pitchEnvCurve
    public readonly BlipLutHandle cutoffLut;    // pre-sampled from cutoffEnv
    public readonly BlipLutHandle envShapeLut;  // pre-sampled from envelope.shape
    public readonly int patchHash;
    // ... no AnimationCurve, no managed refs beyond LUT handles
}

public readonly struct BlipLutHandle { public readonly int index; }  // into BlipLutPool

public readonly struct BlipHandle {
    public readonly int voiceId;
    public readonly int generation;
}

public static class BlipEngine {
    public static void Play(BlipId id, float pitchMult = 1f, float gainMult = 1f);
    public static void PlayAt(BlipId id, Vector3 worldPos, float pitchMult = 1f);
    public static BlipHandle PlayLoop(BlipId id);
    public static void Stop(BlipHandle h);
    public static void SetParam(BlipHandle h, BlipParam p, float value);
    public static void StopAll(BlipId id);
    // asserts main thread; resolves catalog/player/liveHost lazily + caches
}

public sealed class BlipCatalog : MonoBehaviour {
    [SerializeField] private BlipPatchEntry[] entries;
    [Serializable] private struct BlipPatchEntry { public BlipId id; public BlipPatch patch; }
    private Dictionary<BlipId, BlipPatchFlat> flatMap;
    private BlipLutPool lutPool;
    private void Awake() { /* flatten all patches; pre-sample curves into LUT pool */ }
    public BlipPatchFlat Resolve(BlipId id);
}

public sealed class BlipPlayer : MonoBehaviour {
    [SerializeField] private int poolSize = 16;
    [SerializeField] private AudioMixerGroup defaultGroup;
    private AudioSource[] pool;
    private int cursor;
    public void PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group);
}

public sealed class BlipBaker {
    private readonly Dictionary<(int patchHash, int variant), AudioClip> cache = new();
    private readonly LinkedList<AudioClip> lru = new();
    private readonly int memoryBudgetBytes;
    public AudioClip BakeOrGet(in BlipPatchFlat patch, int variantIndex);
    private AudioClip Render(in BlipPatchFlat patch, int variantIndex);   // main thread
}

public static class BlipVoice {
    public static void Render(
        Span<float> buffer, int offset, int count,
        int sampleRate, in BlipPatchFlat patch,
        int variantIndex, ref BlipVoiceState state);
}

public struct BlipVoiceState {
    public double phaseA, phaseB, phaseC, phaseD;
    public float envLevel;
    public BlipEnvStage envStage;
    public float filterZ1, filterZ2;
    public BlipDelayHandle delayHandle;        // index into pooled delay buffer pool
    public float pitchSmoothed, gainSmoothed, cutoffSmoothed;
    public uint rngState;
    public int samplesElapsed;
}

public sealed class BlipLiveHost : MonoBehaviour {
    [SerializeField] private int maxVoices = 32;
    private BlipVoiceSlot[] voices;
    private BlipEventQueue queue;              // SPSC, main-thread-asserted
    private BlipDelayPool delayPool;
    private int mainThreadId;
    private void Awake() { mainThreadId = Thread.CurrentThread.ManagedThreadId; }
    public void Enqueue(in BlipEvent e) {
        Debug.Assert(Thread.CurrentThread.ManagedThreadId == mainThreadId);
        queue.TryEnqueue(e);
    }
    private void OnAudioFilterRead(float[] data, int channels);  // audio thread
}

public struct BlipEvent {
    public BlipPatchHandle patch;
    public BlipEventKind kind;                 // NoteOn, NoteOff, SetParam
    public int voiceId;
    public BlipParam paramId;
    public float paramValue;
    public float pitchMult;
    public float gainMult;
    public uint seed;
}
```

### 7.3 Main control flow

```csharp
// One-shot baked (fast path)
BlipEngine.Play(BlipId.UiButtonClick);
  ↳ assert main thread
  ↳ check cooldown for id → bail if in-cooldown
  ↳ patch = catalog.Resolve(id)           // returns BlipPatchFlat
  ↳ variant = RoundRobin(patch.variantCount)
  ↳ clip = baker.BakeOrGet(patch, variant)   // lazy bake or cache hit
  ↳ pitch = 1 + Jitter(patch.pitchJitterCents)
  ↳ gain = 1 + JitterDb(patch.gainJitterDb)
  ↳ player.PlayOneShot(clip, pitch, gain, patch.mixerGroup)

// Live continuous (slider scrub)
var h = BlipEngine.PlayLoop(BlipId.UiSliderScrub);
BlipEngine.SetParam(h, BlipParam.Pitch, slider.value * 800 + 200);
BlipEngine.Stop(h);
  ↳ liveHost.Enqueue(new BlipEvent { kind = NoteOn,   voiceId = AllocVoice(), ... })
  ↳ liveHost.Enqueue(new BlipEvent { kind = SetParam, paramId = Pitch, paramValue = ... })
  ↳ liveHost.Enqueue(new BlipEvent { kind = NoteOff,  voiceId = h.voiceId })
```

### 7.4 `BlipVoice.Render` outline

```
// pre-hoist pitch env (evaluated per-block, not per-sample)
pitchCurrent = SampleLut(patch.pitchLut, state.samplesElapsed)
for i in [offset .. offset+count):
    osc_sum = 0
    for each osc in patch.osc0..osc3 (where active):
        freq = osc.frequencyHz * pitchCurrent * smoothed(pitchMult)
        state.phaseN += freq / sampleRate
        sample = WaveformLookup(osc.waveform, state.phaseN, osc.pulseDuty)
        osc_sum += sample * osc.gain
    env = EvalEnvelope(patch.env, state.envStage, state.envLevel, dt)
    filtered = ApplyFilter(patch.filter, osc_sum, ref state.filterZ1, ref state.filterZ2)
    processed = ApplyFxChain(patch.fx0..fx3, filtered, ref state)
    processed = DcBlock(processed, ref state)
    buffer[i] += processed * env * smoothed(gainMult)
    state.samplesElapsed++
```

All curve LUTs are read-only `float[]`. Smoothed params use 1-pole coefficients. No `Math.Sin` if `useLutOscillators = true`. No allocs. No Unity API.

---

## 8. Related Subsystems — Impact & Work

| Subsystem                                     | Impact                                                                                                        | Work required                                                |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| **Unity AudioMixer**                          | New groups: `Blip-UI`, `Blip-World`, `Blip-Ambient`. SFX volume slider routes to these.                       | Add mixer asset, expose params.                              |
| **Settings / Preferences**                    | SFX volume, mute-all-Blip toggle, determinism toggle for test runs.                                           | Add entries, bind to mixer params.                           |
| **MainMenu + HUD (uGUI)**                     | Buttons fire `BlipEngine.Play(UiButtonClick)` on click, `UiButtonHover` on pointer enter.                     | Ship small `BlipButton` uGUI helper or event-hook.           |
| **Tooltip system**                            | Show → `UiTooltipAppear`. Debounce 150 ms per instance.                                                       | One call site.                                               |
| **Modal dialogs**                             | Open → `UiModalOpen`. Close → `UiModalClose`.                                                                 | Two hooks in modal controller.                               |
| **Toast notifications**                       | Info toast → `UiToastInfo`. Error → `UiToastError`.                                                           | Two hooks.                                                   |
| **Tutorial / onboarding prompts**             | Prompt appears → `UiTutorialPrompt`.                                                                          | One hook in tutorial driver.                                 |
| **Achievements**                              | Unlock → `UiAchievementUnlock`.                                                                               | One hook.                                                    |
| **Input feedback**                            | Keyboard/gamepad button press on interactive focus → `UiInputFeedback`. Cooldown 80 ms.                       | Input router hook.                                           |
| **Drag interactions**                         | Drag start → `UiDragStart`. Drag end → `UiDragEnd`.                                                           | Input router or drag handler hooks.                          |
| **Undo / redo**                               | `SysUndo` / `SysRedo`.                                                                                        | Two hooks in command stack.                                  |
| **Autosave indicator**                        | `SysAutosaveIndicator`. Cooldown 10 s to avoid machine-gun.                                                   | One hook.                                                    |
| **Road drawing (`PathTerraformPlan` family)** | Per-stroke tick on cell commit. Path-complete chime on plan apply.                                            | Hook stroke-commit + apply events.                           |
| **Demolition tool**                           | Crunch on apply. Denial on invalid target.                                                                    | Hook tool events.                                            |
| **Building placement**                        | Confirm on placement. Denial on invalid cell / insufficient funds.                                            | Hook placement result.                                       |
| **Water / terrain tools**                     | Paint/raise/lower each emit on tile commit.                                                                   | Hook tool apply callbacks.                                   |
| **Grid selection (`GridManager`)**            | Single-cell pip on select. Multi-select = rising pitch step. Rate-limited to 8 Hz.                            | Hook selection events.                                       |
| **Camera controller**                         | Zoom/pan clamp hit → `WorldCameraClamp`. Cooldown 200 ms.                                                     | Hook clamp event.                                            |
| **Economy / Money system**                    | Earn → coin ding. Spend → withdrawal. Gate on non-zero delta.                                                 | Hook ledger-change events.                                   |
| **Save/Load system**                          | Save → upward arpeggio. Load → downward arpeggio. Suppress manual-save SFX during autosave burst.             | Hook save-complete, load-complete, autosave state.           |
| **Cliff generation**                          | Thud on cliff auto-generation apply. Debounce per-batch (one thud per `RefreshShoreTerrainAfterWaterUpdate`). | Hook cliff-batch-apply, add debounce.                        |
| **Scene lifecycle / boot**                    | Boot sting on main scene loaded. Silence during loading.                                                      | One hook in bootstrap.                                       |
| **`FindObjectOfType` discipline (invariant 3)**| `BlipCatalog`, `BlipPlayer`, `BlipLiveHost` cached in `Awake`. No per-`Update` lookups.                       | Follow existing Inspector + fallback pattern.                |
| **Test mode fixtures**                        | Determinism flag pinned for golden runs. Blips silent in headless scenarios.                                  | Add env gate. Extend test harness if assertions inspect audio.|
| **MCP IA catalog**                            | New glossary rows: Blip, Blip Patch, Blip Voice, Bake-to-Clip, Live DSP host, Voice pool, Patch hash.         | Glossary edit + `ia/specs/audio-blip.md` on productionize.   |
| **Global cooldown registry**                  | Per-`BlipId` cooldown ms. Enforced in `BlipEngine` before dispatch.                                           | Registry asset + lookup path.                                |

No impact expected on: pathfinding, desirability, growth-ring classification, geography init params, Postgres bridge.

---

## 9. Twenty Concrete Examples

Numbering 1–20. Each: *sound description* + *tech sketch*. Mixer column = target group.

1. **Main menu button hover** — soft mid-high triangle blip, intimate, non-intrusive, ~40 ms.
   *Tech:* triangle osc @ 2000 Hz, AHDSR A=5/H=0/D=30/S=0/R=5, gain 0.3, one-pole LP 4 kHz. Baked, 1 variant. Cooldown 120 ms per button (prevents re-fire on flicker). Mixer: `Blip-UI`.

2. **Main menu button click** — short down-chirp, crisp, decisive, ~80 ms.
   *Tech:* square osc, pitch env 1000 → 600 Hz linear 60 ms, AHDSR A=1/D=70/S=0/R=10, gain 0.5. Baked, 1 variant. Mixer: `Blip-UI`.

3. **HUD tab switch** — two-note pip C5 → G5, light, navigational.
   *Tech:* two sequential square notes (523, 784 Hz), each 50 ms AHDSR A=2/D=48/S=0, 10 ms gap, stereo pan −0.2 → +0.2. Baked, 1 variant. Cooldown 200 ms.

4. **Tooltip appear** — whisper-soft sine swell, 400 Hz, 120 ms.
   *Tech:* sine 400 Hz, AHDSR A=40/D=80/S=0, gain 0.25. Baked. Cooldown 150 ms per tooltip instance.

5. **Road draw per-tile tick** — wood-tap click, 30 ms, varies each tile.
   *Tech:* modal synth — decaying sine 2500 Hz AHDSR A=0/D=25/S=0 gain 0.35 + noise transient 5 ms HP 4 kHz gain 0.15 summed. Pitch jitter ±8 %. Baked, 4 variants, round-robin. Cooldown 30 ms. (Replaces prior BP-noise-only sketch — BP 3 kHz Q=8 reads tonal, not woody.)

6. **Road path complete (plan applied)** — rising arpeggio C-E-G, warm triangle, ~180 ms.
   *Tech:* three sequential triangle notes (523, 659, 784 Hz), each 50 ms AHDSR A=2/D=48/S=0, 8 ms gap, single delay tap @ 90 ms feedback 0.18 for tail (fits within 180 ms). Baked.

7. **Road erase / unpave sweep** — descending noise sweep, 200 ms, brush-off.
   *Tech:* white noise, LP cutoff env 8000 → 500 Hz exponential, AHDSR A=5/D=195, gain 0.4. Baked, 2 variants.

8. **Demolish crunch** — granular crumble burst, 300 ms, punchy then dusty.
   *Tech:* white noise burst through multi-tap comb (taps 2 ms, 5 ms, 8 ms, feedback 0.3 each), AHDSR A=0/D=300, cutoff env 4 k → 800 Hz, bit-crush 8-bit. Baked, 3 variants. (Replaces prior 30 ms single-tap comb — that tap rate produces a 33 Hz hum, not crumble.)

9. **Building placed confirm** — two-note major third C5 → E5, soft triangle, ~100 ms.
   *Tech:* two triangle notes (523 → 659 Hz), each 50 ms AHDSR A=5/D=45/S=0, gain 0.45. Baked.

10. **Building denied (invalid cell / insufficient funds)** — short down-buzz, 120 ms pulse.
    *Tech:* pulse osc duty 0.2, pitch 400 → 200 Hz linear 120 ms, AHDSR A=0/D=120, LP 1.5 kHz, gain 0.5. Baked.

11. **Water tile painted splash** — filtered noise burst + low body, ~200 ms.
    *Tech:* noise through BP sweeping 1500 → 500 Hz over 200 ms + sine 400 Hz body AHDSR A=0/D=80, mix 0.7/0.3. Baked, 4 pitch-jittered variants.

12. **Terrain raise** — rising sine glide 300 → 600 Hz, 250 ms, breathy.
    *Tech:* sine osc, pitch env 300 → 600 Hz exp, AHDSR A=10/D=240, LFO vibrato 5 Hz depth 10 cents on `Pitch`. Baked.

13. **Terrain lower** — inverse: sine glide 600 → 300 Hz, 250 ms.
    *Tech:* same kernel, inverted pitch env. Baked as separate patch for authoring clarity.

14. **Cliff auto-generated thud** — low body + high transient, 200 ms, earthy.
    *Tech:* sine 120 Hz AHDSR A=0/D=200 gain 0.3 (not 80 Hz / 0.6 — avoids laptop-speaker woof + clip) + noise 20 ms BP 1 kHz transient gain 0.3. Baked, 2 variants. Debounced per-batch: one thud per `RefreshShoreTerrainAfterWaterUpdate` apply, not per cell.

15. **Single cell selected** — short sine pip 800 Hz, 30 ms.
    *Tech:* sine 800 Hz, AHDSR A=1/D=29/S=0, gain 0.35. Baked. Cooldown 80 ms.

16. **Multi-select drag accumulator** — per added cell, pitch rises with count, capped.
    *Tech:* sine osc, freq = 400 + min(count, 16) × 50 Hz, AHDSR A=1/D=24, per-cell trigger rate-limited to **8 Hz** (not 30 Hz — 30 Hz reads machine-gun stutter). Single baked patch + per-play `pitchMult`. No N-variants.

17. **Money earned coin** — bright two-note ding with metallic shimmer, ~140 ms.
    *Tech:* ring-mod sine 1319 Hz × sine **1975 Hz** (non-integer ratio — prior 2637 produces sum 3956 + difference 1318, collapsing back to the carrier). Two notes 60 ms each, AHDSR A=0/D=60, 80 ms delay tap feedback 0.25 for shimmer. Baked.

18. **Money spent withdrawal** — short low tone + soft swish, ~100 ms.
    *Tech:* triangle 200 Hz AHDSR A=0/D=80 gain 0.6 + BP noise @ 3 kHz AHDSR A=0/D=40 gain 0.2, summed. Baked.

19. **Settings slider scrub** — continuous sine pitch following slider, gated on drag.
    *Tech:* **Live DSP** patch. Sine freq = 200 + sliderValue × 800 Hz, smoothed (20 ms 1-pole), envelope loop-sustain while dragging, 40 ms release on drag-end. `PlayLoop` + `SetParam(Pitch, ...)` + `Stop`. **Excluded from the SFX-volume slider itself** — would feedback-loop on its own volume.

20. **Save game upward arpeggio** — C-E-G-C major arpeggio, clean triangle, ~240 ms.
    *Tech:* four triangle notes (523, 659, 784, 1047 Hz), each 60 ms AHDSR A=2/D=58/S=0, 2 ms gap, stereo widen ±0.15 per note. Baked. Suppressed during autosave bursts (global 2 s cooldown shared with `SysAutosaveIndicator`).

---

## 10. Open Questions

- **Mixer topology** — 3 groups (UI / World / Ambient) or 4 (add Ui-diegetic)?
- **Asset path** — `Assets/Audio/BlipPatches/*.asset` or `Assets/ScriptableObjects/Blip/*.asset`?
- **Patch authoring tool** — v1 = Inspector-only. v2 = custom EditorWindow with waveform + spectrum + LUFS. Gate on real authoring pain.
- **Spatialization** — 2.5D iso-cell panning or stay 2D flat?
- **LUT vs. `Math.Sin`** — default LUT for determinism, or opt-in? Tradeoff = LUT quality (size vs. aliasing).
- **Patch-hash scheme** — content-hash of flattened LUTs + numeric fields. Not Unity GUID + version.
- **Platform lifecycle** — WebGL autoplay first-gesture warm-up pattern? iOS audio-session interruption handling. Editor domain reload resets facade state — document or work around?
- **Glossary promotion** — on v1 ship, promote doc from `docs/` to `ia/specs/audio-blip.md` + glossary rows.

---

## 11. Names Registry

Single source of truth for subsystem names. Use verbatim in code, specs, glossary.

### 11.1 Subsystem

- **Blip** — procedural SFX synthesis subsystem. Covers one-shots + continuous voices. "Blip" scope = any procedural SFX, not only short events. Live-continuous naming (`BlipLiveHost`, `BlipHandle`) kept for consistency.

### 11.2 Components

| Name                | Kind                  | Role                                                                                           |
| ------------------- | --------------------- | ---------------------------------------------------------------------------------------------- |
| `BlipEngine`        | static facade         | Entry point. `Play`, `PlayLoop`, `Stop`, `SetParam`. Stateless. Asserts main thread.           |
| `BlipCatalog`       | `MonoBehaviour`       | Maps `BlipId` → `BlipPatchFlat`. Flattens patches on `Awake`. Owns `BlipLutPool`.              |
| `BlipPlayer`        | `MonoBehaviour`       | Pool of `AudioSource`s. `PlayOneShot` wrapper.                                                 |
| `BlipBaker`         | plain class           | Offline renders patches to `AudioClip`. LRU cache. Main-thread-only.                           |
| `BlipLiveHost`      | `MonoBehaviour`       | `OnAudioFilterRead` host. Drains `BlipEventQueue`, renders live voices.                        |
| `BlipVoice`         | static DSP kernel     | `Render(ref state, ...)`. Shared by baker + live host.                                         |
| `BlipPatch`         | `ScriptableObject`    | Authored patch asset. Holds `AnimationCurve`s for editor tweaks.                               |
| `BlipPatchFlat`     | blittable struct      | Audio-thread-safe flattened patch. LUTs replace curves. Built at load.                         |
| `BlipId`            | enum                  | Central SFX identifier. Prefixed by domain (`Ui*`, `Tool*`, `World*`, `Eco*`, `Sys*`).         |
| `BlipHandle`        | struct                | Opaque handle for live voices (`voiceId` + `generation`).                                      |
| `BlipEvent`         | struct                | Main → audio-thread message (NoteOn / NoteOff / SetParam).                                     |
| `BlipEventQueue`    | SPSC ring             | Main-thread-asserted. Lock-free. Fixed capacity.                                               |
| `BlipVoiceState`    | struct                | Per-voice DSP state. Phases, envelope, filter Z, smoothed params, RNG, delay handle.           |
| `BlipVoiceSlot`     | struct                | Live-host slot: `BlipVoiceState` + active flag + patch handle + voiceId.                       |
| `BlipLutPool`       | plain class           | Pool of pre-sampled curve LUTs (`float[]`). Handle-indexed.                                    |
| `BlipLutHandle`     | struct                | Index into `BlipLutPool`.                                                                      |
| `BlipDelayPool`     | plain class           | Pool of delay-line buffers for FX chain. Handle-indexed.                                       |
| `BlipDelayHandle`   | struct                | Index into `BlipDelayPool`.                                                                    |
| `BlipCooldownRegistry` | plain class        | Per-`BlipId` cooldown ms + last-play timestamp. Queried by `BlipEngine`.                       |
| `BlipMixerRouter`   | plain class           | Maps `AudioMixerGroup` enum to mixer group refs. Set at `BlipCatalog.Awake`.                   |

### 11.3 Enums

- `BlipMode` — `Baked`, `Live`.
- `BlipWaveform` — `Sine`, `Triangle`, `Square`, `Saw`, `Pulse`, `NoiseWhite`, `NoisePink`.
- `BlipFilterKind` — `None`, `LowPass`, `HighPass`, `BandPass`.
- `BlipFxKind` — `None`, `BitCrush`, `SampleRate`, `RingMod`, `Delay`, `Comb`, `Allpass`, `Chorus`, `Flanger`, `SoftClip`, `DcBlocker`.
- `BlipParam` — `Pitch`, `Gain`, `FilterCutoff`, `Pan`, `LfoRate`, `LfoDepth`.
- `BlipEnvStage` — `Idle`, `Attack`, `Hold`, `Decay`, `Sustain`, `Release`.
- `BlipEventKind` — `NoteOn`, `NoteOff`, `SetParam`.

### 11.4 Methods (facade)

| Method                                           | Purpose                                                         |
| ------------------------------------------------ | --------------------------------------------------------------- |
| `BlipEngine.Play(id, pitchMult, gainMult)`       | Fire one-shot. Resolves path (Baked / Live).                    |
| `BlipEngine.PlayAt(id, worldPos, pitchMult)`     | Spatialized one-shot.                                           |
| `BlipEngine.PlayLoop(id) → BlipHandle`           | Start continuous voice. Live mode.                              |
| `BlipEngine.Stop(handle)`                        | Stop continuous voice.                                          |
| `BlipEngine.SetParam(handle, param, value)`      | Mutate live voice param.                                        |
| `BlipEngine.StopAll(id)`                         | Kill all active voices for id.                                  |
| `BlipCatalog.Resolve(id)`                        | Return flattened patch.                                         |
| `BlipBaker.BakeOrGet(patch, variantIndex)`       | Cached render.                                                  |
| `BlipVoice.Render(buffer, offset, count, sr, patch, variantIndex, ref state)` | Single DSP kernel. |
| `BlipPlayer.PlayOneShot(clip, pitch, gain, group)` | AudioSource wrapper.                                          |
| `BlipLiveHost.Enqueue(event)`                    | Main-thread-asserted queue push.                                |
| `BlipLiveHost.OnAudioFilterRead(data, channels)` | Audio-thread drain + mix.                                       |

### 11.5 Variables / fields (`BlipPatch` authoring surface)

| Field                    | Unit         | Notes                                                       |
| ------------------------ | ------------ | ----------------------------------------------------------- |
| `mode`                   | enum         | `Baked` or `Live`.                                          |
| `oscillators[0..3]`      | struct array | Up to 4 mixed oscillators.                                  |
| `envelope`               | struct       | AHDSR + optional shape curve.                               |
| `filter`                 | struct       | Kind + cutoff + Q + cutoff curve.                           |
| `lfos[0..1]`             | struct array | Up to 2 LFOs routed to a `BlipParam`.                       |
| `fxChain[0..3]`          | struct array | Ordered FX. Last = DC blocker on live path.                 |
| `durationSeconds`        | s            | Bake length for `Baked`. Ignored for `Live` loop-sustain.   |
| `variantCount`           | int          | 1–8. Round-robin selection on play.                         |
| `pitchJitterCents`       | cents        | Per-invocation ± jitter.                                    |
| `gainJitterDb`           | dB           | Per-invocation ± jitter.                                    |
| `panJitter`              | [-1..1]      | Per-invocation ± jitter.                                    |
| `voiceLimit`             | int          | Max concurrent voices for this patch.                       |
| `priority`               | int          | Higher = survives voice steal.                              |
| `cooldownMs`             | ms           | Per-`BlipId` minimum inter-play gap.                        |
| `deterministic`          | bool         | Disables jitter. Test mode.                                 |
| `useLutOscillators`      | bool         | Bit-exact mode for golden fixtures.                         |
| `mixerGroup`             | ref          | Target `AudioMixerGroup`.                                   |
| `patchHash`              | int          | Content hash of serialized fields + LUTs. Cache key.        |

---

## 13. Decisions (locked, from design session 2026-04-13)

These override open questions in §10. Next agent uses these as ground truth.

| Decision | Chosen | Rationale |
|---|---|---|
| Implementation language | Pure C# | No C++ native plugin. IL2CPP compiles C# to native in builds. |
| Burst compiler | **Out** — not in manifest.json | Plain C# static `BlipVoice.Render`. No `IJob`, no `NativeArray`. |
| Mixer topology | 3 groups: `Blip-UI` / `Blip-World` / `Blip-Ambient` | No 4th diegetic group needed at this scope. |
| Spatialization | None — flat stereo | No `BlipEngine.PlayAt`. Remove from v1 API surface. |
| Scene structure | Persistent bootstrap (DontDestroyOnLoad) | Single `BlipBootstrap` GameObject; survives scene loads. |
| Editor tooling v1 | Inspector only | No custom EditorWindow. Authoring = tweaking SO fields. |
| v1 scope | MVP — 10 sounds, Baked mode only | See §14. Live DSP path deferred to post-MVP. |
| Testing | Unity Test Runner (EditMode + PlayMode) + golden fixture hashes | Owner has no prior game-audio testing experience; needs scaffolded test plan. |

---

## 14. MVP Scope (v1)

### In scope

**System features:**
- `BlipPatch` ScriptableObject + `BlipPatchFlat` flatten.
- `BlipVoice.Render` — oscillators: sine, triangle, square, pulse, noise-white. Envelope: AHDSR. Filter: one-pole LP only. No FX chain.
- `BlipBaker` — lazy bake + flat LRU cache. No priority pre-warm.
- `BlipCatalog` — `BlipId` → `BlipPatch`. Flatten on `Awake`. Register with `BlipEngine`.
- `BlipPlayer` — 16-source pool, `PlayOneShot`, round-robin.
- `BlipEngine` — static facade: `Play(id)`, `StopAll(id)`. Main-thread assert. Per-id cooldown.
- Persistent bootstrap prefab (`BlipBootstrap`) with all MonoBehaviours. `DontDestroyOnLoad`.
- 3 `AudioMixerGroup` wires: `Blip-UI` / `Blip-World` / `Blip-Ambient`.
- Pitch jitter + gain jitter + variant round-robin.
- Unity Test Runner tests (see §14 Testing).

**10 MVP patches (one patch per `BlipId`):**

| # | `BlipId` | Group |
|---|---|---|
| 1 | `UiButtonHover` | Blip-UI |
| 2 | `UiButtonClick` | Blip-UI |
| 3 | `ToolRoadTick` | Blip-World |
| 4 | `ToolRoadComplete` | Blip-World |
| 5 | `ToolBuildingPlace` | Blip-World |
| 6 | `ToolBuildingDenied` | Blip-World |
| 7 | `WorldCellSelected` | Blip-World |
| 8 | `EcoMoneyEarned` | Blip-UI |
| 9 | `EcoMoneySpent` | Blip-UI |
| 10 | `SysSaveGame` | Blip-UI |

**Testing MVP:**
- **EditMode tests** (`BlipVoiceTests`): `BlipVoice.Render` against known buffers. Assert envelope shape (attack rises, decay falls, sustain holds, release tails). Assert oscillator zero-crossings for pitch. Assert silence when `gainMult = 0`. Determinism test: same seed → identical buffer.
- **PlayMode smoke test** (`BlipEngineSmoke`): `BlipEngine.Play(UiButtonClick)` fires without exception. `BlipCatalog` resolves all 10 ids. AudioSource pool does not exhaust on 16 rapid plays. Cooldown blocks 17th play within cooldown window.
- **Golden fixture test**: render each of the 10 patches offline → save `float[]` sum-of-abs hash. Fails if DSP changes break output. Lives in `tools/fixtures/blip/`.

### Out of scope for v1

See §15 for full post-MVP list.

---

## 15. Post-MVP Extensions

Ship after v1 validates the system.

**DSP features:**
- FX chain: bit-crush, ring-mod, comb, allpass, chorus, flanger, soft-clip, DC blocker.
- LFOs (up to 2 per patch, routed to pitch/gain/cutoff/pan).
- Biquad BP filter (Q-controlled).
- Param smoothing on live-mod paths.
- LUT-based oscillators for bit-exact determinism across platforms.
- Voice-steal crossfade (5 ms fade-out on stolen voice, kills click).
- Cache pre-warm allowlist (per-patch eager flag).
- LRU eviction + `BlipLutPool` + `BlipDelayPool`.

**Live DSP path (entire `BlipLiveHost`):**
- `OnAudioFilterRead` host.
- `BlipEventQueue` SPSC ring.
- `BlipEngine.PlayLoop` + `Stop` + `SetParam`.
- `BlipHandle` tracking.
- Required for: `UiSliderScrub`, engine loops, drones.

**Additional patches (10 post-MVP sounds):**
`UiTabSwitch`, `UiTooltipAppear`, `ToolRoadErase`, `ToolDemolish`, `ToolWaterPaint`, `ToolTerrainRaise`, `ToolTerrainLower`, `WorldCliffCreated`, `WorldMultiSelectStep`, `SysLoadGame`.

**Integration extensions:**
`UiModalOpen/Close`, `UiToastInfo/Error`, `UiTutorialPrompt`, `UiAchievementUnlock`, `UiInputFeedback`, `UiDragStart/End`, `SysUndo/Redo`, `SysAutosaveIndicator`, `WorldCameraClamp`.

**Editor tooling:**
- Custom EditorWindow: waveform preview, spectrum view, LUFS meter, A/B patch compare.
- Auto-rebake on patch change.
- Patch hash display.

**Testing extensions:**
- Agent-Unity bridge smoke (requires Play Mode + IDE bridge active).
- Per-Id integration tests for each wired game system.
- CI headless bake validation in `npm run validate:all`.

---

## 16. Handoff — Next Agent Prompt

Next session goal: convert this exploration doc into a master plan TECH/FEAT spec under `ia/projects/{ISSUE_ID}.md` using the standard structure (Steps → Stages → Phases → Tasks).

**Copy-paste prompt for next session:**

---

```
Read docs/blip-procedural-sfx-exploration.md in full, especially §13 (locked decisions),
§14 (MVP scope), §15 (post-MVP extensions), and §11 (names registry).

Then use the /project-new skill to create a new FEAT issue in BACKLOG.md for the Blip
procedural SFX subsystem. Title suggestion: "Blip — procedural SFX synthesis subsystem (MVP)".

After the issue exists, use /kickoff to enrich the spec into a full master plan with the
structure: Steps → Stages → Phases → Tasks. The plan must cover:

1. Infrastructure: AudioMixer asset, BlipBootstrap prefab, persistent scene setup.
2. DSP core: BlipPatch SO, BlipPatchFlat flatten, BlipVoice.Render (MVP oscillators +
   AHDSR + one-pole LP).
3. Bake + playback: BlipBaker, BlipPlayer, BlipCatalog, BlipEngine facade.
4. 10 MVP patches authored and wired to call sites in the game.
5. Testing: Unity Test Runner EditMode (DSP unit tests) + PlayMode smoke + golden fixture
   hashes. Owner has no prior game-audio testing experience — testing phases must be
   thorough and scaffolded with clear task descriptions.
6. Post-MVP: create a companion doc docs/blip-post-mvp-extensions.md summarising §15.

Decisions already locked (do not reopen):
- Pure C#, no Burst (not in manifest.json), no spatialization, no Live DSP in v1.
- 3 mixer groups: Blip-UI, Blip-World, Blip-Ambient.
- Persistent bootstrap scene (DontDestroyOnLoad).
- Inspector-only authoring.
- MVP = 10 sounds listed in §14.
```

---

## 12. Revision Log

- **r1 (initial draft)** — bake-to-clip hybrid decision, 20 examples, architecture skeleton.
- **r2 (post opus review)** — fixes applied:
  - `BlipPatchFlat` + `BlipLutHandle` introduced. `AnimationCurve` never crosses audio thread. §1.2, §7.2.
  - `BlipVoice.Render` signature corrected to `ref BlipVoiceState`. §7.2.
  - Pitch env hoisted out of inner osc loop in `Render`. §7.4.
  - Delay line ownership clarified — `BlipDelayPool` + `BlipDelayHandle`. §7.2, §11.2.
  - Queue discipline declared — SPSC with main-thread assert on `Enqueue`. §1.2, §3.2, §7.2.
  - `BlipEngine` static-facade justification vs. invariant 4. §5.
  - LFO + allpass + chorus/flanger + param smoothing + DC blocker + voice-steal crossfade added. §6.
  - Subsystem table extended: tutorial, achievements, modals, toasts, input feedback, drag, undo/redo, autosave, camera clamp, global cooldown registry. §8.
  - Example 5 reworked — modal synth replaces BP-noise. §9 ex 5.
  - Example 8 reworked — multi-tap comb 2/5/8 ms replaces single 30 ms tap. §9 ex 8.
  - Example 14 — cliff thud lowered gain + raised freq to avoid laptop woof. §9 ex 14.
  - Example 16 — drag accumulator rate dropped 30 → 8 Hz. §9 ex 16.
  - Example 17 — coin ring-mod ratio changed 1319 × 2637 → 1319 × 1975 (non-integer). §9 ex 17.
  - Example 19 — slider scrub exclusion on SFX-volume slider itself. §9 ex 19.
  - Determinism note — byte equality replaced with tolerance hash + LUT osc option. §1.2, §6.
  - Patch-hash scheme — content hash of serialized fields + LUTs. §1.2, §10.
  - Platform + lifecycle open Qs — WebGL autoplay, iOS interruption, editor domain reload. §10.
  - Caveman prose pass on §0–§5, §10. Code blocks + headings + tables stay normal English per agent-output-caveman exceptions.
  - Names Registry added. §11.

---
