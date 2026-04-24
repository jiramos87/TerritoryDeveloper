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

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---


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
