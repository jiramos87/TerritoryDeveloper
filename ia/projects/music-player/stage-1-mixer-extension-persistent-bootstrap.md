### Stage 1 — Audio infra + playlist pipeline / Mixer extension + persistent bootstrap

**Status:** In Progress — tasks Draft (TECH-316..TECH-321 filed 2026-04-17)

**Objectives:** Extend `BlipMixer.mixer` w/ `Blip-Music` group + `MusicVolume` + `MasterVolume` params. Author `MusicBootstrap.cs` mirroring Blip pattern. Create + place `MusicBootstrap` prefab at `MainMenu.unity` root. Headless volume binding on `Awake` reads `MasterVolumeDb` + `MusicVolumeDb` from `PlayerPrefs` + calls `blipMixer.SetFloat(param, db)` twice. Zero playback yet — just mixer ready + persistent root alive.

**Exit:**

- `Assets/Audio/BlipMixer.mixer` w/ new `Blip-Music` group routed to Master. Exposed params: `MusicVolume` (binds `Blip-Music`), `MasterVolume` (binds Master). Existing `Blip-UI` / `Blip-World` / `Blip-Ambient` groups + `SfxVolume` param untouched.
- `MusicBootstrap.cs` authored — `[SerializeField] private AudioMixer blipMixer`, `Instance` MB accessor, `DontDestroyOnLoad(transform.root.gameObject)` in `Awake`, PlayerPrefs read (3 keys: `MasterVolumeDb`, `MusicVolumeDb`, plus placeholder reads for `MusicLastTrackId` + `MusicEnabled` + `MusicFirstRunDone` — hand-off to `MusicPlayer` lands Step 2). Constants: `MasterVolumeDbKey`, `MasterVolumeParam`, `MusicVolumeDbKey`, `MusicVolumeParam`, `MusicLastTrackIdKey`, `MusicEnabledKey`, `MusicFirstRunDoneKey`, defaults.
- `MusicBootstrap` prefab at `Assets/Prefabs/Audio/MusicBootstrap.prefab` placed in `MainMenu.unity` root (sibling to `BlipBootstrap`). Inspector wires `blipMixer` → `BlipMixer.mixer`.
- `npm run unity:compile-check` green.
- Invariant #4 satisfied — MB Inspector-placed; `Instance` static accessor set in `Awake` after `DontDestroyOnLoad`, cleared in `OnDestroy`. No constructor singleton.
- Phase 1 — Mixer asset extension (`Blip-Music` group + 2 new exposed params).
- Phase 2 — `MusicBootstrap.cs` script + constants + headless mixer binding.
- Phase 3 — Prefab creation + scene placement + compile verify.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Add Blip-Music mixer group | **TECH-316** | Draft | Extend `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer`). Add new group `Blip-Music` under Master, routed through Master. Do NOT touch existing `Blip-UI` / `Blip-World` / `Blip-Ambient` groups. Binary YAML asset — commit edit. |
| T1.2 | Expose MusicVolume + MasterVolume params | **TECH-317** | Draft | Expose `MusicVolume` dB param bound to `Blip-Music` group volume slider. Expose `MasterVolume` dB param bound to Master group volume slider. Defaults 0 dB. `SfxVolume` stays as-is (Blip-owned). Verify exposed params panel shows 3 entries: `MasterVolume`, `MusicVolume`, `SfxVolume`. |
| T1.3 | Author MusicBootstrap constants | **TECH-318** | Draft | Author `public const string` fields on `MusicBootstrap`: `MasterVolumeDbKey = "MasterVolumeDb"`, `MasterVolumeParam = "MasterVolume"`, `MusicVolumeDbKey = "MusicVolumeDb"`, `MusicVolumeParam = "MusicVolume"`, `MusicLastTrackIdKey = "MusicLastTrackId"`, `MusicEnabledKey = "MusicEnabled"`, `MusicFirstRunDoneKey = "MusicFirstRunDone"`. Default float constants (`MasterVolumeDbDefault = 0f`, `MusicVolumeDbDefault = 0f`). No re-declaration of Blip's `SfxVolumeDbKey` / `SfxVolumeParam` — consumed from `BlipBootstrap` when Settings panel lands Step 3. |
| T1.4 | MusicBootstrap.Awake shape | **TECH-319** | Draft | Author `MusicBootstrap : MonoBehaviour` at `Assets/Scripts/Audio/Music/MusicBootstrap.cs`. `Instance` static MB accessor (invariant #4 — Inspector-placed, not `new`). `Awake` body: `DontDestroyOnLoad(transform.root.gameObject)` (pattern per `BlipBootstrap.cs` L53), `Instance = this`, read `MasterVolumeDb` + `MusicVolumeDb` from `PlayerPrefs`, call `blipMixer.SetFloat(MasterVolumeParam, db)` + `blipMixer.SetFloat(MusicVolumeParam, db)`. Warn if `blipMixer` null (mirror `BlipBootstrap.cs` L59-62). `OnDestroy` clears `Instance` if match. |
| T1.5 | MusicBootstrap prefab creation | **TECH-320** | Draft | Create `Assets/Prefabs/Audio/MusicBootstrap.prefab` (new asset). Attach `MusicBootstrap.cs` component. Inspector wires `[SerializeField] private AudioMixer blipMixer` → `Assets/Audio/BlipMixer.mixer`. Commit prefab `.prefab` + `.meta`. No scene diff yet. |
| T1.6 | MainMenu scene placement + compile verify | **TECH-321** | Draft | Place `MusicBootstrap.prefab` instance at `MainMenu.unity` root (sibling to existing `BlipBootstrap` prefab instance). Commit `MainMenu.unity` scene diff. Run `npm run unity:compile-check` — verify green. Manual smoke: launch MainMenu → `MusicBootstrap.Instance != null` post-Awake → warn log absent (mixer ref wired). Invariant #4 satisfied at stage close. |

---
