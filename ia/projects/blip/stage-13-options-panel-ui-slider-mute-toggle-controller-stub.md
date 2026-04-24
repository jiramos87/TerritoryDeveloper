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
