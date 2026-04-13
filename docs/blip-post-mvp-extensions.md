# Blip — Post-MVP extensions

> Companion to `ia/projects/blip-master-plan.md` (MVP orchestrator).
> Exploration source: `docs/blip-procedural-sfx-exploration.md` §15.
>
> Scope: everything **out** of Blip MVP. Ship after v1 lands + validates. Nothing here blocks MVP close.

---

## 1. DSP features

Extensions to `BlipVoice` kernel + patch data model. MVP ships sine/triangle/square/pulse/noise-white + AHDSR + one-pole LP only.

- **FX chain** — bit-crush, ring-mod, comb, allpass, chorus, flanger, soft-clip, DC blocker. Ordered per-patch slots (`fxChain[0..3]`). DC blocker pinned to tail on live path.
- **LFOs** — up to 2 per patch, waveform + rate Hz + depth. Routes: `Pitch` / `Gain` / `FilterCutoff` / `Pan`.
- **Biquad band-pass filter** — Q-controlled. Time-varying cutoff via pre-sampled LUT.
- **Param smoothing** — 1-pole 20 ms on live-mod params. Kills zipper noise.
- **LUT-based oscillators** — bit-exact determinism across ARM + x86. Opt-in via `useLutOscillators` bool on `BlipPatch`. MVP already carries the field (reserved slot) but `BlipVoice.Render` ignores it and always uses `Math.Sin` path; flipping the flag + shipping LUT osc path lands here without schema churn. Default stays `Math.Sin` until authoring pain forces switch.
- **`BlipMode` enum** — `Baked | Live | Both` per-patch selector. MVP has no enum (implicit Baked path everywhere); re-introduced here when Live DSP path lands so the same `BlipPatch` can author both playback paths.
- **Voice-steal crossfade** — 5 ms fade-out on stolen voice. Prevents click on voice-limit hit.
- **Cache pre-warm allowlist** — per-patch eager-bake flag. Boot-time bake for hot patches.
- **Pool infrastructure** — `BlipLutPool` (curve LUTs) + `BlipDelayPool` (delay-line buffers) + LRU eviction on baked clip cache (memory budget default 4 MB).

---

## 2. Live DSP path

Entire `BlipLiveHost` + `OnAudioFilterRead` surface deferred. MVP = baked-to-clip only.

- **`OnAudioFilterRead` host** — single silent `AudioSource`; pulls from SPSC ring; renders active voices. Unity 2022.3 LTS native audio callback — stable + documented.
- **`BlipEventQueue`** — SPSC lock-free ring, main-thread-asserted on enqueue. Fixed capacity.
- **`BlipEngine.PlayLoop` + `Stop` + `SetParam`** — live voice API surface. `BlipHandle` (voiceId + generation).
- **`BlipVoiceSlot[]` pool** — per-live-voice state w/ active flag + patch handle + voiceId.
- **Required for:** `UiSliderScrub`, engine loops, drones, continuous-mod voices. None shipped in MVP.
- **Revisit trigger — `IAudioGenerator` on engine upgrade.** Unity 6.3 LTS ships `IAudioGenerator` interface — cleaner per-voice plug-in path than `OnAudioFilterRead`. Current project on Unity 2022.3.62f3 so `IAudioGenerator` unavailable. On engine upgrade to Unity 6.x, evaluate migrating live host from `OnAudioFilterRead` → `IAudioGenerator` before shipping live path (cleaner thread semantics, explicit voice lifecycle). Keep `OnAudioFilterRead` path as fallback if upgrade slips past live-DSP delivery.

---

## 3. Additional patches — 10 post-MVP sounds

Wired after MVP validates. Mixer group assignment per exploration doc §9 recipes.

| `BlipId` | Group | Source recipe |
|---|---|---|
| `UiTabSwitch` | Blip-UI | §9 ex 3 |
| `UiTooltipAppear` | Blip-UI | §9 ex 4 |
| `ToolRoadErase` | Blip-World | §9 ex 7 |
| `ToolDemolish` | Blip-World | §9 ex 8 |
| `ToolWaterPaint` | Blip-World | §9 ex 11 |
| `ToolTerrainRaise` | Blip-World | §9 ex 12 |
| `ToolTerrainLower` | Blip-World | §9 ex 13 |
| `WorldCliffCreated` | Blip-World | §9 ex 14 |
| `WorldMultiSelectStep` | Blip-World | §9 ex 16 |
| `SysLoadGame` | Blip-UI | §9 ex 20 inverse |

Each patch: parameter recipe in exploration §9. Cliff thud debounced per-batch (one thud per `RefreshShoreTerrainAfterWaterUpdate` apply). Multi-select accumulator rate-limited to 8 Hz.

---

## 4. Integration extensions

Additional call sites + `BlipId` rows. Each lands with glossary rows + integration hooks.

- `UiModalOpen` / `UiModalClose` — modal controller hooks.
- `UiToastInfo` / `UiToastError` — toast notification hooks.
- `UiTutorialPrompt` — tutorial driver hook.
- `UiAchievementUnlock` — achievement system hook.
- `UiInputFeedback` — keyboard/gamepad press on focused interactive; 80 ms cooldown.
- `UiDragStart` / `UiDragEnd` — drag handler hooks.
- `SysUndo` / `SysRedo` — command-stack hooks.
- `SysAutosaveIndicator` — autosave pulse; 10 s cooldown. Suppresses manual-save SFX during autosave burst.
- `WorldCameraClamp` — zoom/pan clamp hit; 200 ms cooldown.

---

## 5. Editor tooling

MVP authoring = Inspector only. Custom EditorWindow deferred.

- **`BlipPatchEditorWindow`** — waveform preview, spectrum view, LUFS meter, A/B patch compare.
- **Auto-rebake on patch change** — subscribe to SO field dirty; re-render baked cache entry.
- **Patch hash display** — Inspector-visible content hash for cache-invalidation debugging.
- **Preview button** — offline render + play without Play Mode.

Gate on real authoring pain. MVP 10 sounds likely authorable in Inspector without window.

---

## 6. Testing extensions

MVP tests = EditMode DSP unit + PlayMode smoke + golden fixture hash. Extensions below lift coverage + CI integration.

- **Agent-Unity bridge smoke** — via `mcp__territory-ia__unity_bridge_command` (requires Play Mode + IDE bridge active + Postgres `agent_bridge_job`). Verifies runtime `BlipEngine.Play` from bridge context.
- **Per-`BlipId` integration tests** — one test per wired call site. Exercises tool + verifies Blip fires.
- **CI headless bake validation** — fold into `npm run validate:all`. Bake all patches headless + compare hashes vs committed fixtures. Fails CI on DSP drift.

---

## 7. Scope guardrail

MVP decisions (exploration doc §13) are load-bearing. Changes to MVP scope require explicit re-decision + sync edit to both orchestrator + this doc. Do not silently promote a post-MVP item into an MVP stage.

Locked MVP decisions (do not reopen during v1):

- Pure C#, no Burst, no native plugin.
- No spatialization (`PlayAt` excluded from v1 API surface).
- No Live DSP path (`BlipLiveHost` + `PlayLoop` deferred entire).
- 3 mixer groups only: `Blip-UI` / `Blip-World` / `Blip-Ambient`.
- Persistent bootstrap (`DontDestroyOnLoad`) — one prefab in boot scene.
- Inspector-only authoring.
- MVP = 10 sounds listed in exploration §14; baked mode only.

Candidate extensions surfaced during MVP implementation (bug fixes, refactors exposed by real authoring) land here as new rows under the matching section.
