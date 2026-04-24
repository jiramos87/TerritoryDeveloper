### Stage 5 — Settings sliders + Credits + first-run toast + resume polish / Settings Master + Music sliders

**Status:** _pending_

**Objectives:** Extend Settings panel w/ two sliders (Master + Music). Bind to mixer params (`MasterVolume` + `MusicVolume`). Persist via PlayerPrefs keys from `MusicBootstrap`. Verify no collision w/ in-flight Blip SFX slider work. Slider range covers −80 dB … 0 dB standard mixer scale.

**Exit:**

- Settings panel UI has 2 new sliders: Master (top) + Music (below Master). SFX slider co-exists (Blip-owned — do not touch).
- Slider handlers — `OnValueChanged` → convert normalized [0,1] to dB (e.g. `db = Mathf.Lerp(-80f, 0f, t)`) → `blipMixer.SetFloat(paramName, db)` → `PlayerPrefs.SetFloat(keyName, db)`. Paramname + keyname sourced from `MusicBootstrap` constants.
- Awake restore — on settings panel open, read PlayerPrefs + set slider values to match persisted dB (reverse `Lerp` via `Mathf.InverseLerp(-80f, 0f, db)`).
- No re-declaration of `SfxVolumeDbKey` or `SfxVolumeParam` — Music panel code references `BlipBootstrap.SfxVolumeDbKey` if interop needed (clean separation).
- `npm run unity:compile-check` green.
- Phase 1 — Slider placement + layout under Settings panel.
- Phase 2 — Slider handlers + dB conversion + PlayerPrefs persistence + restore-on-open.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Settings panel slider rows | _pending_ | _pending_ | Open existing Settings panel scene / prefab. Add 2 slider rows (Master + Music) above SFX slider (Blip-owned — do not reorder Blip rows). Each row: TMP label ("Master Volume" / "Music Volume") + `Slider` UI component, min=0, max=1, whole-numbers=false. Theme per `UiTheme`. Exact path TBD at authoring — check for in-flight Blip Settings work first + coordinate placement (exploration doc §5.3 — "reuse Blip Settings panel scaffolding when it lands"). |
| T5.2 | SFX slider collision check | _pending_ | _pending_ | Verify SFX slider (if present from Blip Settings step) reads `BlipBootstrap.SfxVolumeDbKey = "BlipSfxVolumeDb"` + writes `SfxVolume` mixer param — both Blip-owned. Music sliders MUST use different keys (`MasterVolumeDb` / `MusicVolumeDb`) + different params (`MasterVolume` / `MusicVolume`) — zero collision. If SFX slider not yet implemented, leave placeholder row + doc dependency in Stage 3.1 exit notes. |
| T5.3 | Master slider handler + restore | _pending_ | _pending_ | Attach `MasterVolumeSliderController : MonoBehaviour` (or inline on Settings panel script). `OnValueChanged(float t)` body: `float db = Mathf.Lerp(-80f, 0f, t); musicBootstrap.BlipMixer.SetFloat(MusicBootstrap.MasterVolumeParam, db); PlayerPrefs.SetFloat(MusicBootstrap.MasterVolumeDbKey, db);`. Restore on `OnEnable`: `float db = PlayerPrefs.GetFloat(MusicBootstrap.MasterVolumeDbKey, 0f); slider.value = Mathf.InverseLerp(-80f, 0f, db);`. Use `FindObjectOfType<MusicBootstrap>()` (cached in Awake; invariant #3) for mixer ref. |
| T5.4 | Music slider handler + restore | _pending_ | _pending_ | Mirror T3.1.3 structure for Music slider — use `MusicBootstrap.MusicVolumeParam` + `MusicBootstrap.MusicVolumeDbKey`. Keep identical dB conversion (−80..0). `npm run unity:compile-check` green. Manual smoke: move Master slider → Master group dB changes in Audio Mixer window; restart scene → slider position restored. |

---
