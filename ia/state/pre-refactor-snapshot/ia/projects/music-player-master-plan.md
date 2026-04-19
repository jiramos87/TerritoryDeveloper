# Music Player — Master Plan (MVP)

> **Last updated:** 2026-04-16
>
> **Status:** In Progress — Step 1 pending (Stage 1.1 ready for `/stage-file`); Steps 2 + 3 fully decomposed (tasks `_pending_`)
>
> **Scope:** Authored-jazz music subsystem. `.ogg` streaming from `Assets/Resources/Music/`, shuffle no-repeat-until-exhausted, single `AudioSource` + coroutine advance, C# event-driven `NowPlayingWidget` HUD row, Settings Master + Music sliders + Credits sub-screen, first-run toast, resume-by-track-id. Separate from Blip (procedural SFX) — single overlap = shared `Assets/Audio/BlipMixer.mixer` asset (new `Blip-Music` group + `MusicVolume` + `MasterVolume` params). No scope-boundary doc — `docs/music-player-jazz-exploration.md` §2.2 + §10 carry non-scope lock.
>
> **Exploration source:** `docs/music-player-jazz-exploration.md` (§10 locked decisions — 27 items; Design Expansion §Chosen Approach, §Components, §Data flow, §Interfaces, §Non-scope, §Architecture mermaid, §Subsystem impact table, §Implementation points P1–P6, §Examples, §Review Notes — 3 blocking resolved inline).
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable via `/closeout`).
>
> **Sibling orchestrator in flight:**
> - `ia/projects/blip-master-plan.md` — Blip (procedural SFX). Shares `Assets/Audio/BlipMixer.mixer` asset. Music Stage 1.1 mixer edit must land between Blip stages, not during — confirm Blip boundary state before authoring mixer param additions. `SfxVolume` param + `BlipSfxVolumeDb` PlayerPrefs key stay Blip-owned — Music does not re-declare.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
> - `docs/music-player-jazz-exploration.md` — full design + 5 worked examples + 3 resolved blocking items. §10 (27 locked decisions) is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 ≤6 tasks per phase).
> - `ia/rules/invariants.md` — **#3** (no `FindObjectOfType` in `Update` / per-frame — widget caches `MusicPlayer` ref in `Awake`) and **#4** (no new singletons — `MusicBootstrap.Instance` is Inspector-placed MB, not `new`). Both gate every Step.
> - `ia/specs/audio-blip.md` §5.1 + §5.4 — BlipBootstrap pattern + existing mixer group layout. `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` is the template mirror. Note: spec present on disk but not indexed by MCP `list_specs` — read directly.
> - `ia/specs/unity-development-context.md` §3 + §6 — `[SerializeField] private` + `FindObjectOfType` fallback pattern in `Awake`; Script Execution Order + initialization races.
> - `ia/specs/ui-design-system.md` §1.3.1 (HUD / uGUI hygiene — no full-stretch anchors on small strips, corner anchors = (1,1)), §3.1 (HUD density — `Canvas/DataPanelButtons` as-built cluster), §5.2 (`UiTheme` script + theme asset paths).
> - `ia/specs/managers-reference.md` §Game notifications — `GameNotificationManager.Instance` singleton; actual API = `PostNotification(message, type, duration)` (exploration doc §6.2 + P6 use informal "ShowMessage" — real method name = `PostNotification`; Step 3 task intent locks the correct signature).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — Audio infra + playlist pipeline

**Status:** In Progress — Stage 1.1 pending

**Objectives:** Land scaffolding — mixer extension + persistent `MusicBootstrap` prefab + playlist data model + loader + `.ogg` import postprocessor. Zero playback wiring yet; no music heard. Mixer param binding headless on `Awake` (mirrors Blip `SfxVolume` pattern). Playlist loader fail-soft on malformed JSON / missing clips. Editor import postprocessor stamps `LoadType=Streaming` + Vorbis settings automatically on `.ogg` drops under `Assets/Resources/Music/`.

**Exit criteria:**

- `Assets/Audio/BlipMixer.mixer` extended — new `Blip-Music` group under Master + exposed `MusicVolume` + `MasterVolume` dB params (SFX wiring untouched; `Blip-UI` / `Blip-World` / `Blip-Ambient` + `SfxVolume` unchanged).
- `MusicBootstrap.cs` at `Assets/Scripts/Audio/Music/MusicBootstrap.cs` (new); mirrors `BlipBootstrap.cs` shape — `[SerializeField] private AudioMixer blipMixer`, `Instance` MB accessor, `DontDestroyOnLoad(transform.root.gameObject)` in `Awake`, PlayerPrefs read + mixer `SetFloat` binding, constants for key + param names.
- `MusicBootstrap` prefab at `Assets/Prefabs/Audio/MusicBootstrap.prefab` (new sibling to `BlipBootstrap.prefab`); placed at `MainMenu.unity` root.
- `MusicTrack`, `MusicPlaylist`, `MusicPlaylistLoader` authored under `Assets/Scripts/Audio/Music/`. Loader fail-soft paths — malformed JSON → empty list + warn; missing `.ogg` → row skipped + warn.
- `Assets/Resources/Music/playlist.json` seeded w/ 3 placeholder rows (dummy clip names).
- `MusicOggImportPostprocessor.cs` at `Assets/Editor/MusicOggImportPostprocessor.cs` — scoped to `Assets/Resources/Music/` only; stamps `LoadType=Streaming`, `PreloadAudioData=false`, `CompressionFormat=Vorbis` (quality 0.5).
- `npm run unity:compile-check` green.
- Coordination w/ Blip master plan — confirmed Blip at stable stage boundary (no in-flight mixer edits) before Stage 1.1 lands.

**Art:** None. Code + mixer asset edit + JSON + prefab only.

**Relevant surfaces (load when step opens):**
- `Assets/Audio/BlipMixer.mixer` — binary YAML asset, edit via Unity Editor `Window → Audio → Audio Mixer`.
- `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` — pattern template (constants layout, `Awake` shape, headless mixer binding, `Instance` MB accessor).
- `Assets/Scripts/Audio/Music/` **(new)** — runtime code namespace.
- `Assets/Prefabs/Audio/` — existing directory; add `MusicBootstrap.prefab` sibling to `BlipBootstrap.prefab`.
- `Assets/Resources/Music/` **(new)** — JSON manifest + `.ogg` drop zone.
- `Assets/Editor/` **(new)** — postprocessor scope.
- `Assets/Scenes/MainMenu.unity` — scene hosting persistent bootstrap roots.
- `ia/specs/audio-blip.md` §5.1 (Blip bootstrap), §5.4 (Blip mixer group).
- `ia/specs/unity-development-context.md` §3 (`[SerializeField] private` + `FindObjectOfType` fallback), §6 (Script Execution Order — `DontDestroyOnLoad` Awake timing).
- `ia/rules/invariants.md` — #4 (MusicBootstrap MB in scene, never `new`).

#### Stage 1.1 — Mixer extension + persistent bootstrap

**Status:** In Progress — tasks Draft (TECH-316..TECH-321 filed 2026-04-17)

**Objectives:** Extend `BlipMixer.mixer` w/ `Blip-Music` group + `MusicVolume` + `MasterVolume` params. Author `MusicBootstrap.cs` mirroring Blip pattern. Create + place `MusicBootstrap` prefab at `MainMenu.unity` root. Headless volume binding on `Awake` reads `MasterVolumeDb` + `MusicVolumeDb` from `PlayerPrefs` + calls `blipMixer.SetFloat(param, db)` twice. Zero playback yet — just mixer ready + persistent root alive.

**Exit:**

- `Assets/Audio/BlipMixer.mixer` w/ new `Blip-Music` group routed to Master. Exposed params: `MusicVolume` (binds `Blip-Music`), `MasterVolume` (binds Master). Existing `Blip-UI` / `Blip-World` / `Blip-Ambient` groups + `SfxVolume` param untouched.
- `MusicBootstrap.cs` authored — `[SerializeField] private AudioMixer blipMixer`, `Instance` MB accessor, `DontDestroyOnLoad(transform.root.gameObject)` in `Awake`, PlayerPrefs read (3 keys: `MasterVolumeDb`, `MusicVolumeDb`, plus placeholder reads for `MusicLastTrackId` + `MusicEnabled` + `MusicFirstRunDone` — hand-off to `MusicPlayer` lands Step 2). Constants: `MasterVolumeDbKey`, `MasterVolumeParam`, `MusicVolumeDbKey`, `MusicVolumeParam`, `MusicLastTrackIdKey`, `MusicEnabledKey`, `MusicFirstRunDoneKey`, defaults.
- `MusicBootstrap` prefab at `Assets/Prefabs/Audio/MusicBootstrap.prefab` placed in `MainMenu.unity` root (sibling to `BlipBootstrap`). Inspector wires `blipMixer` → `BlipMixer.mixer`.
- `npm run unity:compile-check` green.
- Invariant #4 satisfied — MB Inspector-placed; `Instance` static accessor set in `Awake` after `DontDestroyOnLoad`, cleared in `OnDestroy`. No constructor singleton.

**Phases:**

- [ ] Phase 1 — Mixer asset extension (`Blip-Music` group + 2 new exposed params).
- [ ] Phase 2 — `MusicBootstrap.cs` script + constants + headless mixer binding.
- [ ] Phase 3 — Prefab creation + scene placement + compile verify.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Add Blip-Music mixer group | 1 | **TECH-316** | Draft | Extend `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer`). Add new group `Blip-Music` under Master, routed through Master. Do NOT touch existing `Blip-UI` / `Blip-World` / `Blip-Ambient` groups. Binary YAML asset — commit edit. |
| T1.1.2 | Expose MusicVolume + MasterVolume params | 1 | **TECH-317** | Draft | Expose `MusicVolume` dB param bound to `Blip-Music` group volume slider. Expose `MasterVolume` dB param bound to Master group volume slider. Defaults 0 dB. `SfxVolume` stays as-is (Blip-owned). Verify exposed params panel shows 3 entries: `MasterVolume`, `MusicVolume`, `SfxVolume`. |
| T1.1.3 | Author MusicBootstrap constants | 2 | **TECH-318** | Draft | Author `public const string` fields on `MusicBootstrap`: `MasterVolumeDbKey = "MasterVolumeDb"`, `MasterVolumeParam = "MasterVolume"`, `MusicVolumeDbKey = "MusicVolumeDb"`, `MusicVolumeParam = "MusicVolume"`, `MusicLastTrackIdKey = "MusicLastTrackId"`, `MusicEnabledKey = "MusicEnabled"`, `MusicFirstRunDoneKey = "MusicFirstRunDone"`. Default float constants (`MasterVolumeDbDefault = 0f`, `MusicVolumeDbDefault = 0f`). No re-declaration of Blip's `SfxVolumeDbKey` / `SfxVolumeParam` — consumed from `BlipBootstrap` when Settings panel lands Step 3. |
| T1.1.4 | MusicBootstrap.Awake shape | 2 | **TECH-319** | Draft | Author `MusicBootstrap : MonoBehaviour` at `Assets/Scripts/Audio/Music/MusicBootstrap.cs`. `Instance` static MB accessor (invariant #4 — Inspector-placed, not `new`). `Awake` body: `DontDestroyOnLoad(transform.root.gameObject)` (pattern per `BlipBootstrap.cs` L53), `Instance = this`, read `MasterVolumeDb` + `MusicVolumeDb` from `PlayerPrefs`, call `blipMixer.SetFloat(MasterVolumeParam, db)` + `blipMixer.SetFloat(MusicVolumeParam, db)`. Warn if `blipMixer` null (mirror `BlipBootstrap.cs` L59-62). `OnDestroy` clears `Instance` if match. |
| T1.1.5 | MusicBootstrap prefab creation | 3 | **TECH-320** | Draft | Create `Assets/Prefabs/Audio/MusicBootstrap.prefab` (new asset). Attach `MusicBootstrap.cs` component. Inspector wires `[SerializeField] private AudioMixer blipMixer` → `Assets/Audio/BlipMixer.mixer`. Commit prefab `.prefab` + `.meta`. No scene diff yet. |
| T1.1.6 | MainMenu scene placement + compile verify | 3 | **TECH-321** | Draft | Place `MusicBootstrap.prefab` instance at `MainMenu.unity` root (sibling to existing `BlipBootstrap` prefab instance). Commit `MainMenu.unity` scene diff. Run `npm run unity:compile-check` — verify green. Manual smoke: launch MainMenu → `MusicBootstrap.Instance != null` post-Awake → warn log absent (mixer ref wired). Invariant #4 satisfied at stage close. |

---

#### Stage 1.2 — Playlist data model + loader + import postprocessor

**Status:** _pending_

**Objectives:** Authoring data model — `MusicTrack` record struct + `MusicPlaylist` class. `MusicPlaylistLoader` static parses `Assets/Resources/Music/playlist.json` → `List<MusicTrack>` + resolves `AudioClip` handles via `Resources.Load<AudioClip>`. Fail-soft on malformed JSON (empty list + warn) + missing clips (drop row + warn). Seed `playlist.json` w/ 3 placeholder rows. Editor-only `MusicOggImportPostprocessor` auto-stamps `LoadType=Streaming` / `PreloadAudioData=false` / `CompressionFormat=Vorbis` on `.ogg` drops under `Assets/Resources/Music/`. Answers exploration doc open question Q7.

**Exit:**

- `MusicTrack` record struct at `Assets/Scripts/Audio/Music/MusicTrack.cs` — `{ string id, string filename, string title, string artist, string licenseBlurb }`.
- `MusicPlaylist` class at `Assets/Scripts/Audio/Music/MusicPlaylist.cs` — `List<MusicTrack> Tracks` + `AudioClip[] Clips` cache (parallel index).
- `MusicPlaylistLoader.LoadFromResources()` at `Assets/Scripts/Audio/Music/MusicPlaylistLoader.cs` — `Resources.Load<TextAsset>("Music/playlist")`, `JsonUtility.FromJson<MusicPlaylistJson>`, try/catch on parse. Per row: `Resources.Load<AudioClip>("Music/" + Path.GetFileNameWithoutExtension(filename))`. Null clip → log warn + skip row. Empty list on parse failure (warn).
- `Assets/Resources/Music/playlist.json` seeded w/ 3 placeholder rows (ids `placeholder-01..03`, filenames `placeholder-a.ogg` etc., dummy titles / artists / blurbs). No actual `.ogg` files yet — loader will warn + return empty list at runtime until Step 2 seed OR real tracks arrive.
- `MusicOggImportPostprocessor` at `Assets/Editor/MusicOggImportPostprocessor.cs` — `AssetPostprocessor.OnPreprocessAudio` guards `assetPath.StartsWith("Assets/Resources/Music/")` + `.ogg` suffix; sets `AudioImporter.defaultSampleSettings` to `{ loadType = AudioClipLoadType.Streaming, preloadAudioData = false, compressionFormat = AudioCompressionFormat.Vorbis, quality = 0.5f }`.
- `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — `MusicTrack` record + `MusicPlaylist` class.
- [ ] Phase 2 — `MusicPlaylistLoader` static + JSON parse + clip resolution + fail-soft paths.
- [ ] Phase 3 — `playlist.json` seed + `MusicOggImportPostprocessor` editor-only stamp.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | MusicTrack record struct | 1 | _pending_ | _pending_ | Author `public readonly record struct MusicTrack(string Id, string Filename, string Title, string Artist, string LicenseBlurb)` at `Assets/Scripts/Audio/Music/MusicTrack.cs`. Also author `[Serializable] class MusicTrackJson` + `MusicPlaylistJson` DTOs for `JsonUtility.FromJson` (record struct not Unity-serializable — DTO pair needed). Map `MusicTrackJson → MusicTrack` in loader. |
| T1.2.2 | MusicPlaylist class | 1 | _pending_ | _pending_ | Author `MusicPlaylist` at `Assets/Scripts/Audio/Music/MusicPlaylist.cs` — `public IReadOnlyList<MusicTrack> Tracks`, `public IReadOnlyList<AudioClip> Clips` (parallel index). Constructor takes both lists (loader-produced). Static `Empty` factory for fail-soft paths. |
| T1.2.3 | MusicPlaylistLoader parse + fail-soft | 2 | _pending_ | _pending_ | Author `MusicPlaylistLoader.LoadFromResources()` static at `Assets/Scripts/Audio/Music/MusicPlaylistLoader.cs`. Steps: (1) `TextAsset json = Resources.Load<TextAsset>("Music/playlist")`; null → log warn `"[Music] playlist.json missing — player disabled"` + return `MusicPlaylist.Empty`. (2) `try { var dto = JsonUtility.FromJson<MusicPlaylistJson>(json.text); } catch (Exception e) { Debug.LogWarning($"[Music] playlist.json malformed — player disabled: {e.Message}"); return MusicPlaylist.Empty; }`. |
| T1.2.4 | MusicPlaylistLoader clip resolution | 2 | _pending_ | _pending_ | Per-row clip resolution. `var stem = Path.GetFileNameWithoutExtension(row.filename); var clip = Resources.Load<AudioClip>("Music/" + stem);`. If `clip == null` → `Debug.LogWarning($"[Music] Track '{stem}' clip missing — skipping row")` + continue. Else append `MusicTrack` + `AudioClip` to parallel accumulator lists. Final: construct `MusicPlaylist(tracks, clips)`. Empty inputs allowed (returns empty playlist silently). |
| T1.2.5 | Seed playlist.json placeholders | 3 | _pending_ | _pending_ | Create `Assets/Resources/Music/playlist.json` w/ 3 placeholder rows: `{"tracks": [{"id": "placeholder-01", "filename": "placeholder-a.ogg", "title": "Placeholder A", "artist": "Dev Stub", "licenseBlurb": "Placeholder — replace at Step 3"}, ...]}`. No `.ogg` files — Resources.Load will null-return + loader warns + returns empty playlist at runtime. Real tracks dropped in at dev-time or Step 3. Commit JSON + .meta. |
| T1.2.6 | MusicOggImportPostprocessor | 3 | _pending_ | _pending_ | Author `MusicOggImportPostprocessor : AssetPostprocessor` at `Assets/Editor/MusicOggImportPostprocessor.cs`. Override `void OnPreprocessAudio()`. Guards: `if (!assetPath.StartsWith("Assets/Resources/Music/")) return; if (!assetPath.EndsWith(".ogg")) return;`. Body: `var importer = (AudioImporter)assetImporter; var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.Streaming; settings.preloadAudioData = false; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = 0.5f; importer.defaultSampleSettings = settings;`. Wrap in `#if UNITY_EDITOR`. Verify `npm run unity:compile-check` green. Answers exploration doc open question Q7. |

---

### Step 2 — MusicPlayer runtime + NowPlayingWidget

**Status:** _pending_

**Objectives:** Runtime playback + HUD surface. `MusicPlayer` owns single `AudioSource`, shuffle history ring, coroutine-based advance-on-end, debounced skip-next, restart-vs-pop skip-prev, first-track 2s fade-in, C# events (`OnTrackChanged`, `OnPlayStateChanged`). `MusicBootstrap` hands off to `MusicPlayer.BootstrapAutoplay(lastTrackId, enabled)` on `Awake`. `NowPlayingWidget` HUD row top-right (corner anchor (1,1), fixed 32px height, no full-stretch per ui-design-system §1.3.1) — `Title • Artist` TMP + EQ-bars icon (4 bars, sine pulse while playing) + 3 buttons (prev/play-pause/next). Widget subscribes events in `OnEnable`, unsubscribes `OnDisable`. Empty playlist → widget `SetActive(false)`. First music heard in game at end of stage 2.1; HUD interactive at end of stage 2.2.

**Exit criteria:**

- `MusicPlayer.cs` at `Assets/Scripts/Audio/Music/MusicPlayer.cs` — public API: `BootstrapAutoplay(string lastTrackId, bool enabled)`, `Play()`, `Pause()`, `TogglePlayPause()`, `Next()`, `Prev()`, `IsPlaying { get; }`, `CurrentTrack { get; }`. Events: `public event Action<MusicTrack> OnTrackChanged; public event Action<bool> OnPlayStateChanged;`.
- `MusicBootstrap.Awake` extended — loads playlist via `MusicPlaylistLoader.LoadFromResources()`, hands off to `MusicPlayer.BootstrapAutoplay(lastTrackId, enabled)` after mixer binding.
- Shuffle — no-repeat-until-exhausted Fisher-Yates on `List<MusicTrack>`. On exhaustion reshuffle forbidding last-played as head. History ring bounded (~8 entries).
- `AdvanceOnEndLoop` coroutine — `!isPlaying && time >= length - 0.05f` + explicit `_pauseRequested` flag guard (pause-vs-end disambiguation per exploration doc Review Notes blocking-resolved #1).
- Mixer group binding — `[SerializeField] private AudioMixerGroup musicGroup` (Inspector-wired) + `blipMixer.FindMatchingGroups("Blip-Music")[0]` fallback; assign to `musicSource.outputAudioMixerGroup` before first `Play()` (blocking-resolved #2).
- Skip-next — 500ms `Time.unscaledTime` debounce; skip-prev — `>3f` → restart, else pop history tail; first-track 2s linear fade-in coroutine (only on first track after boot, not subsequent).
- `NowPlayingWidget.cs` at `Assets/Scripts/UI/Hud/NowPlayingWidget.cs` — `[SerializeField] private MusicPlayer musicPlayer` + `FindObjectOfType<MusicPlayer>()` fallback in `Awake` (invariant #3 — caches ref; no per-frame `FindObjectOfType`). Subscribes events `OnEnable`, unsubscribes `OnDisable`. Renders `Title • Artist` TMP ellipsis + EQ-bars icon + 3 buttons; consumes `UiTheme`.
- HUD layout — top-right, `anchorMin = anchorMax = (1, 1)` corner anchor, fixed 32px height, no full-stretch (ui-design-system §1.3.1). Sibling under `Canvas/DataPanelButtons` or new `HudAudioRow` parent per §3.1 as-built cluster.
- Empty playlist → `gameObject.SetActive(false)` in `Start` early-return.
- `PlayerPrefs` writes on state change — `MusicLastTrackId` (string) + `MusicEnabled` (int 0/1).
- `npm run unity:compile-check` green.

**Art:** EQ-bars icon (4-rect `Image` strip, programmatic; no external asset). 3 button icons (prev / play-pause / next) — reuse existing `Assets/UI/` button sprites if present, else placeholder TMP glyphs (`⏮ ▶ ⏭`) + post-MVP art pass.

**Relevant surfaces (load when step opens):**
- `Assets/Scripts/Audio/Music/MusicBootstrap.cs` — existing at Step 1 close; add playlist load + `MusicPlayer.BootstrapAutoplay` handoff.
- `Assets/Scripts/Audio/Music/` **(new files)** — `MusicPlayer.cs`.
- `Assets/Scripts/UI/Hud/` **(new)** — `NowPlayingWidget.cs`.
- `Assets/Scenes/MainMenu.unity` — `MusicPlayer` child GO under `MusicBootstrap` prefab; `NowPlayingWidget` child under `Canvas/DataPanelButtons` (or `HudAudioRow` sibling).
- `ia/specs/ui-design-system.md` §1.3.1 (HUD/uGUI hygiene — corner anchors, no full-stretch), §3.1 (HUD density cluster), §5.2 (`UiTheme.cs` + default theme asset paths).
- `ia/specs/unity-development-context.md` §3 (widget caches ref — invariant #3 guard), §6 (SEO + init races).
- `ia/rules/invariants.md` — #3 gates widget ref caching; #4 gates `MusicPlayer` + `NowPlayingWidget` MB shape.

#### Stage 2.1 — MusicPlayer runtime (playback, shuffle, coroutines, events)

**Status:** _pending_

**Objectives:** `MusicPlayer` MB owns one `AudioSource`. Shuffle history ring. Advance-on-end coroutine w/ pause guard. Debounced skip-next. Restart-vs-pop skip-prev. First-track 2s fade-in coroutine. Mixer group binding on Awake. C# events fire on transitions. PlayerPrefs writes on state change. `MusicBootstrap.Awake` hands off via `BootstrapAutoplay(lastTrackId, enabled)`. Music plays in game at end of stage (assumes at least one valid `.ogg` + row in playlist; otherwise no-op + warn).

**Exit:**

- `MusicPlayer.cs` authored w/ full public API (`BootstrapAutoplay`, `Play`, `Pause`, `TogglePlayPause`, `Next`, `Prev`, `IsPlaying`, `CurrentTrack`) + both events.
- `[SerializeField] private AudioSource musicSource` + `[SerializeField] private AudioMixerGroup musicGroup` + `[SerializeField] private MusicBootstrap bootstrap` (Inspector ref for mixer lookup); `FindObjectOfType` fallback in `Awake` per invariant #3 guardrail.
- Shuffle — Fisher-Yates on `List<MusicTrack>` copy; no-repeat-until-exhausted; reshuffle on exhaustion forbids last-played as head (regenerate until head ≠ last).
- `AdvanceOnEndLoop` coroutine — guards: `musicSource.clip != null && !musicSource.isPlaying && !_pauseRequested && musicSource.time >= musicSource.clip.length - 0.05f`. Invokes `AdvanceToNext()` on trigger.
- `_pauseRequested` flag set by `Pause()`, cleared by `Play()` — prevents coroutine false-advance during explicit pause (blocking-resolved #1).
- Skip-next — `if (Time.unscaledTime - _lastSkipTime < 0.5f) return; _lastSkipTime = Time.unscaledTime;` then advance.
- Skip-prev — `if (musicSource.time > 3f) { musicSource.time = 0f; musicSource.Play(); return; }` else pop `_history` tail + push current back to front of shuffle remainder + play popped.
- `FirstTrackFadeInCoroutine` — 2s linear ramp on `musicSource.volume` from 0f → 1f; gated by `_firstTrackAfterBoot` bool (set in `BootstrapAutoplay`, cleared after first advance).
- `BootstrapAutoplay(lastTrackId, enabled)` — resolve `lastTrackId` via `playlist.Tracks.FirstOrDefault(t => t.Id == lastTrackId)`; miss → log warn + fall through to shuffle-fresh (exploration doc Example "Missing track on resume"). If `enabled == false` → stop after load (don't `Play`). Invoke `OnTrackChanged` + set `_firstTrackAfterBoot = true` + start both coroutines.
- Mixer group binding — `Awake` resolves `musicGroup` via Inspector first, else `blipMixer.FindMatchingGroups("Blip-Music")[0]`; assigns `musicSource.outputAudioMixerGroup = musicGroup` before any `Play()` call (blocking-resolved #2).
- PlayerPrefs writes — `OnTrackChanged` path writes `MusicLastTrackId`; `OnPlayStateChanged` path writes `MusicEnabled` (0/1).
- `MusicBootstrap.Awake` extended — after mixer binding, loads playlist + hands off to `MusicPlayer.BootstrapAutoplay(PlayerPrefs.GetString(MusicLastTrackIdKey, ""), PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1)`.
- `npm run unity:compile-check` green. Manual smoke: playlist.json + ≥ 1 `.ogg` → music starts on MainMenu load, fades in 2s, advances on track end.

**Phases:**

- [ ] Phase 1 — `MusicPlayer` scaffold + `AudioSource` + mixer group binding + shuffle core.
- [ ] Phase 2 — Playback API + coroutines (`AdvanceOnEndLoop`, `FirstTrackFadeIn`) + pause guard.
- [ ] Phase 3 — Skip controls (next debounce + prev history) + events + PlayerPrefs writes.
- [ ] Phase 4 — `BootstrapAutoplay` method + `MusicBootstrap.Awake` hand-off + smoke verification.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | MusicPlayer scaffold + refs | 1 | _pending_ | _pending_ | Author `MusicPlayer : MonoBehaviour` at `Assets/Scripts/Audio/Music/MusicPlayer.cs`. Fields: `[SerializeField] private AudioSource musicSource; [SerializeField] private AudioMixerGroup musicGroup; [SerializeField] private MusicBootstrap bootstrap;`. Private state: `MusicPlaylist _playlist`, `List<MusicTrack> _remaining`, `List<MusicTrack> _history` (bounded ~8 via trim-on-push), `MusicTrack _current`, `bool _pauseRequested`, `bool _firstTrackAfterBoot`, `float _lastSkipTime`. `Awake` resolves refs via Inspector-first + `FindObjectOfType` fallback (invariant #3). |
| T2.1.2 | Mixer group binding | 1 | _pending_ | _pending_ | `Awake` body: `if (musicGroup == null && bootstrap != null) { var groups = bootstrap.BlipMixer.FindMatchingGroups("Blip-Music"); musicGroup = groups.Length > 0 ? groups[0] : null; }`. Assign `musicSource.outputAudioMixerGroup = musicGroup` before any `Play` call. Warn if `musicGroup` null — `"[Music] Blip-Music mixer group not resolvable — audio routes to Master direct"`. Must add `public AudioMixer BlipMixer => blipMixer;` accessor to `MusicBootstrap` if absent (mirror `BlipBootstrap.BlipMixer` getter L37). |
| T2.1.3 | Shuffle core + history ring | 1 | _pending_ | _pending_ | Private `void Reshuffle(MusicTrack forbidHead = default)` — copy `_playlist.Tracks` → `_remaining`; Fisher-Yates in-place; if `forbidHead.Id != null` && `_remaining.Count > 1`, re-swap position 0 with random other index when head matches forbid. History ring push — `_history.Add(track); if (_history.Count > 8) _history.RemoveAt(0);`. `PickNext()` pops `_remaining[0]`; empty → `Reshuffle(_current); Pop`. |
| T2.1.4 | Play / Pause / TogglePlayPause API | 2 | _pending_ | _pending_ | `public void Play() { _pauseRequested = false; musicSource.UnPause(); OnPlayStateChanged?.Invoke(true); PlayerPrefs.SetInt(MusicBootstrap.MusicEnabledKey, 1); }`. `public void Pause() { _pauseRequested = true; musicSource.Pause(); OnPlayStateChanged?.Invoke(false); PlayerPrefs.SetInt(MusicBootstrap.MusicEnabledKey, 0); }`. `public void TogglePlayPause() { if (IsPlaying) Pause(); else Play(); }`. `public bool IsPlaying => musicSource.isPlaying && !_pauseRequested;`. |
| T2.1.5 | AdvanceOnEndLoop coroutine | 2 | _pending_ | _pending_ | `private IEnumerator AdvanceOnEndLoop() { while (true) { yield return null; if (_current.Id == null) continue; if (musicSource.clip == null) continue; if (_pauseRequested) continue; if (musicSource.isPlaying) continue; if (musicSource.time < musicSource.clip.length - 0.05f) continue; AdvanceToNext(); } }`. Private `AdvanceToNext()` — pick next, stop current, load clip, `musicSource.clip = clip; musicSource.Play();` (hard cut), clear `_firstTrackAfterBoot`, push to history, invoke `OnTrackChanged`, write `MusicLastTrackId`. |
| T2.1.6 | FirstTrackFadeIn coroutine | 2 | _pending_ | _pending_ | `private IEnumerator FirstTrackFadeInCoroutine() { if (!_firstTrackAfterBoot) yield break; const float duration = 2f; float elapsed = 0f; musicSource.volume = 0f; while (elapsed < duration) { elapsed += Time.unscaledDeltaTime; musicSource.volume = Mathf.Clamp01(elapsed / duration); yield return null; } musicSource.volume = 1f; _firstTrackAfterBoot = false; }`. Started inside `BootstrapAutoplay` after first `musicSource.Play()`. Not started on subsequent advances (hard-cut transitions per doc §4). |
| T2.1.7 | Skip-next debounce | 3 | _pending_ | _pending_ | `public void Next() { if (Time.unscaledTime - _lastSkipTime < 0.5f) return; _lastSkipTime = Time.unscaledTime; AdvanceToNext(); }`. Shares `AdvanceToNext()` w/ `AdvanceOnEndLoop` — no fade-in (hard cut per doc §4 "skip-next = instant advance + 500ms debounce"). Writes `MusicLastTrackId` via `AdvanceToNext`. |
| T2.1.8 | Skip-prev restart-or-pop | 3 | _pending_ | _pending_ | `public void Prev() { if (musicSource.time > 3f) { musicSource.time = 0f; musicSource.Play(); return; } if (_history.Count < 2) { musicSource.time = 0f; musicSource.Play(); return; } _history.RemoveAt(_history.Count - 1); var prev = _history[_history.Count - 1]; _remaining.Insert(0, _current); _current = prev; var clipIdx = _playlist.Tracks.ToList().IndexOf(prev); musicSource.clip = _playlist.Clips[clipIdx]; musicSource.Play(); OnTrackChanged?.Invoke(prev); PlayerPrefs.SetString(MusicBootstrap.MusicLastTrackIdKey, prev.Id); }`. Fall-through branch when history empty → restart current. |
| T2.1.9 | C# events + PlayerPrefs state writes | 3 | _pending_ | _pending_ | Declare `public event Action<MusicTrack> OnTrackChanged; public event Action<bool> OnPlayStateChanged;`. Invoke in `AdvanceToNext` + `Prev` + `BootstrapAutoplay` (track) and `Play` / `Pause` (state). Wrap invokes w/ null-conditional (`?.Invoke`). PlayerPrefs writes — `MusicLastTrackId` on every track change; `MusicEnabled` on every play-state change. No `PlayerPrefs.Save()` per call (Unity flushes on quit; explicit save adds no value + risks stutter). |
| T2.1.10 | BootstrapAutoplay entry method | 4 | _pending_ | _pending_ | Add public method on `MusicPlayer`: `public void BootstrapAutoplay(string lastTrackId, bool enabled) { if (_playlist == null || _playlist.Tracks.Count == 0) { Debug.LogWarning("[Music] Empty playlist — MusicPlayer disabled"); return; } Reshuffle(); MusicTrack first; var idx = _playlist.Tracks.ToList().FindIndex(t => t.Id == lastTrackId); if (idx >= 0) { first = _playlist.Tracks[idx]; _remaining.Remove(first); } else { if (!string.IsNullOrEmpty(lastTrackId)) Debug.LogWarning($"[Music] last track '{lastTrackId}' not in playlist — starting shuffle-fresh"); first = PickNext(); } _current = first; _history.Add(first); musicSource.clip = _playlist.Clips[_playlist.Tracks.ToList().IndexOf(first)]; if (enabled) { musicSource.Play(); _firstTrackAfterBoot = true; } else { _pauseRequested = true; } OnTrackChanged?.Invoke(first); StartCoroutine(AdvanceOnEndLoop()); if (enabled) StartCoroutine(FirstTrackFadeInCoroutine()); }`. Also add `public void InjectPlaylist(MusicPlaylist p) { _playlist = p; }` setter — called from `MusicBootstrap.Awake` before `BootstrapAutoplay`. |
| T2.1.11 | MusicBootstrap hand-off + smoke verify | 4 | _pending_ | _pending_ | Extend `MusicBootstrap.Awake` tail — after mixer `SetFloat` calls: `var playlist = MusicPlaylistLoader.LoadFromResources(); if (musicPlayer != null) { musicPlayer.InjectPlaylist(playlist); musicPlayer.BootstrapAutoplay(PlayerPrefs.GetString(MusicLastTrackIdKey, ""), PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1); } else { Debug.LogWarning("[Music] MusicPlayer ref missing on MusicBootstrap — autoplay skipped"); }`. Add `[SerializeField] private MusicPlayer musicPlayer;` field on `MusicBootstrap` + Inspector wire to child GO hosting `MusicPlayer`. Also add `public MusicPlaylist Playlist { get; private set; }` accessor on `MusicBootstrap` (needed for Credits panel Stage 3.2) — assign after loader call: `this.Playlist = playlist;`. Manual smoke: drop at least one valid `.ogg` into `Assets/Resources/Music/` + matching JSON row → run MainMenu → confirm music fades in over 2s. `npm run unity:compile-check` green. |

---

#### Stage 2.2 — NowPlayingWidget HUD

**Status:** _pending_

**Objectives:** HUD widget top-right corner. Subscribes `MusicPlayer` events (`OnEnable`/`OnDisable`). Renders `Title • Artist` TMP + EQ-bars icon (4 bars, sine pulse while playing, flat paused) + 3 buttons (prev / play-pause / next). Consumes `UiTheme`. Empty playlist → `SetActive(false)`. No per-frame `FindObjectOfType` (invariant #3 — ref cached in `Awake`). Corner anchor (1,1), fixed 32px height, no full-stretch (ui-design-system §1.3.1). Title click no-op. Text swaps instantly on advance (no animation per doc §6.1).

**Exit:**

- `NowPlayingWidget.cs` at `Assets/Scripts/UI/Hud/NowPlayingWidget.cs`.
- `[SerializeField] private MusicPlayer musicPlayer` + `FindObjectOfType<MusicPlayer>()` fallback in `Awake` (invariant #3 — cached once).
- `OnEnable` subscribes `musicPlayer.OnTrackChanged += HandleTrackChanged` + `OnPlayStateChanged += HandlePlayStateChanged`; `OnDisable` unsubscribes both.
- HUD layout — root `RectTransform` w/ `anchorMin = anchorMax = new Vector2(1f, 1f)` (top-right corner anchor), `sizeDelta = new Vector2(260f, 32f)` (per exploration doc §6.1 ~240–280px wide), `anchoredPosition` offset so widget sits inside HUD safe area. No full-stretch (per ui-design-system §1.3.1 warning).
- Sibling under `Canvas/DataPanelButtons` per ui-design-system §3.1 as-built cluster (or new `HudAudioRow` parent if placement collides — scene review at authoring time).
- `Title • Artist` TMP row — middle zone; ellipsis via `TextMeshProUGUI.overflowMode = TextOverflowModes.Ellipsis`. Bullet separator `•` char (U+2022). Title click no-op.
- EQ-bars icon — 4 stacked `Image` rects (2px wide, 1px spacing) on left edge. While playing (`musicPlayer.IsPlaying`): `bar[i].rectTransform.sizeDelta = new Vector2(w, baseH + amp * Mathf.Abs(Mathf.Sin(Time.time * freq + phase[i])))` per frame (struct assignment, zero alloc). While paused: `sizeDelta.y = baseH` flat. Refs cached in `Awake` — no `GetComponent` / `Find` inside `Update` (invariant #3).
- 3 buttons (prev / play-pause / next) right-aligned — `Button` onClick wires `() => musicPlayer.Prev()`, `() => musicPlayer.TogglePlayPause()`, `() => musicPlayer.Next()`. Play/pause icon swaps on `HandlePlayStateChanged`.
- `UiTheme` consumed per ui-design-system §5.2 — colors + TMP font from theme asset (`Assets/UI/Theme/DefaultUiTheme.asset` or scene-level override). Reference resolved via `FindObjectOfType<UiTheme>()` once in `Awake` (or Inspector ref).
- Empty playlist guard — `Start` body: `if (musicPlayer == null || musicPlayer.CurrentTrack.Id == null) { gameObject.SetActive(false); return; }`.
- `npm run unity:compile-check` green. Manual smoke: `.ogg` + playlist row → widget visible top-right, EQ bars pulse, text swaps on track end, buttons advance/rewind/toggle.

**Phases:**

- [ ] Phase 1 — Widget scaffold + ref caching + event subscribe/unsubscribe.
- [ ] Phase 2 — Layout + TMP text + theme consumption + corner anchor.
- [ ] Phase 3 — EQ-bars icon animation + 3 buttons wired + empty-playlist guard.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | NowPlayingWidget scaffold + ref cache | 1 | _pending_ | _pending_ | Author `NowPlayingWidget : MonoBehaviour` at `Assets/Scripts/UI/Hud/NowPlayingWidget.cs`. Fields: `[SerializeField] private MusicPlayer musicPlayer; [SerializeField] private TextMeshProUGUI titleArtistText; [SerializeField] private Image[] eqBars; [SerializeField] private Image playPauseIcon; [SerializeField] private Sprite playSprite; [SerializeField] private Sprite pauseSprite; [SerializeField] private Button prevButton; [SerializeField] private Button playPauseButton; [SerializeField] private Button nextButton; [SerializeField] private UiTheme uiTheme;`. `Awake` body: `if (musicPlayer == null) musicPlayer = FindObjectOfType<MusicPlayer>();` + same for `uiTheme` (invariant #3 — cache once, no per-frame lookup). |
| T2.2.2 | Event subscribe / unsubscribe | 1 | _pending_ | _pending_ | `OnEnable` body: `if (musicPlayer != null) { musicPlayer.OnTrackChanged += HandleTrackChanged; musicPlayer.OnPlayStateChanged += HandlePlayStateChanged; HandleTrackChanged(musicPlayer.CurrentTrack); HandlePlayStateChanged(musicPlayer.IsPlaying); }`. `OnDisable` body: mirror w/ `-=`. Null-safe — null musicPlayer = no-op. Handlers: `HandleTrackChanged(MusicTrack t)` sets `titleArtistText.text = $"{t.Title} • {t.Artist}"`; `HandlePlayStateChanged(bool playing)` swaps `playPauseIcon.sprite = playing ? pauseSprite : playSprite`. |
| T2.2.3 | Layout + corner anchor + TMP ellipsis | 2 | _pending_ | _pending_ | Build widget prefab `Assets/Prefabs/UI/NowPlayingWidget.prefab`. Root `RectTransform`: `anchorMin = anchorMax = (1, 1)`, `pivot = (1, 1)`, `sizeDelta = (260, 32)`, `anchoredPosition = (-16, -16)` (16px inset from top-right HUD corner). Child TMP `titleArtistText`: `overflowMode = Ellipsis`, horizontal anchor stretched between bars-icon and button-row, font from `UiTheme`. No full-stretch on root (ui-design-system §1.3.1 warning). |
| T2.2.4 | UiTheme consumption | 2 | _pending_ | _pending_ | Apply theme in `Start` (after `UiTheme` resolved in `Awake`): `titleArtistText.color = uiTheme.TextPrimary; titleArtistText.font = uiTheme.PrimaryFont;` (adapt to actual `UiTheme` public API — inspect `Assets/Scripts/Managers/GameManagers/UiTheme.cs` fields; use closest existing color/font tokens for HUD row parity per ui-design-system §5.2). Also theme EQ bars + button icon tints. |
| T2.2.5 | EQ-bars icon animation | 3 | _pending_ | _pending_ | In `Update`: `if (musicPlayer.IsPlaying) { for (int i = 0; i < eqBars.Length; i++) { float amp = Mathf.Abs(Mathf.Sin(Time.time * 6f + i * 0.7f)); float h = 4f + amp * 20f; eqBars[i].rectTransform.sizeDelta = new Vector2(2f, h); } } else { for (int i = 0; i < eqBars.Length; i++) { eqBars[i].rectTransform.sizeDelta = new Vector2(2f, 4f); } }`. Refs `eqBars` cached in Inspector — no `GetComponent` / `Find` inside `Update` (invariant #3). Struct assignment on `sizeDelta` — zero alloc. |
| T2.2.6 | Button wiring + empty-playlist guard | 3 | _pending_ | _pending_ | `Start` body — after theme applied: `if (musicPlayer == null) { gameObject.SetActive(false); return; }` + null-check `CurrentTrack` → `SetActive(false)` if no track (empty playlist state per doc §6.1). Wire buttons: `prevButton.onClick.AddListener(() => musicPlayer.Prev()); playPauseButton.onClick.AddListener(() => musicPlayer.TogglePlayPause()); nextButton.onClick.AddListener(() => musicPlayer.Next());`. Title TMP gets no click handler (doc §6.1 "Text click = no-op"). Place prefab instance under `Canvas/DataPanelButtons` in `MainMenu.unity` + gameplay scene. `npm run unity:compile-check` green. |

---

### Step 3 — Settings sliders + Credits + first-run toast + resume polish

**Status:** _pending_

**Objectives:** Integration polish. Two new sliders in Settings panel — Master + Music — bound to `MasterVolume` + `MusicVolume` mixer params + persisted via `PlayerPrefs` keys on `MusicBootstrap`. SFX slider co-exists (Blip Settings step provides it; Music must not collide on `SfxVolumeDb` key — exploration doc Subsystem impact row clarifies Blip-owned `BlipSfxVolumeDb` key stays verbatim). `MusicCreditsPanel` Settings sub-screen renders scrollable list of `Title — Artist — licenseBlurb`. First-run toast via `GameNotificationManager.Instance.PostNotification("Jazz playing — toggle via top-right", NotificationType.Info, 5f)` on first gameplay scene load + immediate `MusicFirstRunDone = 1` flip. Resume track-id flow end-to-end verified (falls through to shuffle-fresh on miss — already wired in Stage 2.1 `BootstrapAutoplay`; this stage adds explicit smoke + edge coverage).

**Exit criteria:**

- Settings panel UI extended — 3 sliders visible (Master, Music, SFX). Master + Music sliders new (this stage). SFX slider pre-existing (Blip Settings work — do not re-author; verify no collision).
- Master slider binds `MasterVolume` param on `BlipMixer.mixer`; writes `MasterVolumeDb` PlayerPrefs key (constant from `MusicBootstrap`). Music slider binds `MusicVolume` param; writes `MusicVolumeDb` key. Volume range −80 dB … 0 dB (standard mixer scale).
- `MusicCreditsPanel.cs` at `Assets/Scripts/UI/Settings/MusicCreditsPanel.cs` — renders scrollable list from `MusicPlaylist.Tracks`. Row format: `"{Title} — {Artist} — {LicenseBlurb}"`. Reached via Settings nav entry `Settings > Music > Credits`.
- First-run toast fires on first gameplay scene load when `PlayerPrefs.GetInt(MusicFirstRunDoneKey, 0) == 0`; immediately `PlayerPrefs.SetInt(MusicFirstRunDoneKey, 1)`. Uses actual API `GameNotificationManager.Instance.PostNotification(msg, NotificationType.Info, 5f)` — exploration doc writes "ShowMessage" but real method = `PostNotification` (verified against `GameNotificationManager.cs`).
- Resume track-id — explicit smoke test: set `MusicLastTrackId` to non-existent id via editor script or `PlayerPrefs.SetString` in menu; restart scene; verify warn log + shuffle-fresh fallback (already coded Stage 2.1; stage 3.3 locks coverage).
- Sidebar guard — toast duration arg in `PostNotification` may not be overridable on existing API without refactor. Fallback: if refactor out of scope for Music MVP, set `notificationDuration` field via SerializedObject at runtime before call OR accept default 3s + doc update in exploration doc revision. Stage 3.3 task intent locks contingency.
- `npm run unity:compile-check` green.
- Manual smoke chain: fresh PlayerPrefs → launch → toast shows 5s (or 3s fallback) → toast dismisses → `MusicFirstRunDone = 1` persisted → second launch → no toast. Settings sliders move mixer dB in real time + persist across launches. Credits screen lists all 3 placeholder rows.

**Art:** Slider track + handle sprites reuse existing Settings UI theme (ui-design-system §5.2 `DefaultUiTheme.asset`). Scrollable list — reuse `Assets/UI/Prefabs/UI_ScrollListShell.prefab` per ui-design-system §5.2.

**Relevant surfaces (load when step opens):**
- `Assets/Scripts/UI/Settings/` **(new)** — `MusicCreditsPanel.cs`.
- Settings panel GameObject — path TBD at authoring; likely under `Assets/Scenes/MainMenu.unity` or shared Settings prefab. Scene review at Stage 3.1 opening — check for in-flight Blip Settings work and coordinate sibling slider placement.
- `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` — `PostNotification(message, type, duration)` method at L188 (2-arg + 3-arg overloads); `notificationDuration` serialized field at L24. No `ShowMessage` method exists — exploration doc naming drift.
- `Assets/Scripts/Audio/Music/MusicBootstrap.cs` — existing at Step 2 close; extend for first-run toast logic OR host on new helper MB.
- `ia/specs/managers-reference.md` §Game notifications — `GameNotificationManager.Instance` singleton rule.
- `ia/specs/ui-design-system.md` §5.2 — `UI_ScrollListShell.prefab` for Credits list.

#### Stage 3.1 — Settings Master + Music sliders

**Status:** _pending_

**Objectives:** Extend Settings panel w/ two sliders (Master + Music). Bind to mixer params (`MasterVolume` + `MusicVolume`). Persist via PlayerPrefs keys from `MusicBootstrap`. Verify no collision w/ in-flight Blip SFX slider work. Slider range covers −80 dB … 0 dB standard mixer scale.

**Exit:**

- Settings panel UI has 2 new sliders: Master (top) + Music (below Master). SFX slider co-exists (Blip-owned — do not touch).
- Slider handlers — `OnValueChanged` → convert normalized [0,1] to dB (e.g. `db = Mathf.Lerp(-80f, 0f, t)`) → `blipMixer.SetFloat(paramName, db)` → `PlayerPrefs.SetFloat(keyName, db)`. Paramname + keyname sourced from `MusicBootstrap` constants.
- Awake restore — on settings panel open, read PlayerPrefs + set slider values to match persisted dB (reverse `Lerp` via `Mathf.InverseLerp(-80f, 0f, db)`).
- No re-declaration of `SfxVolumeDbKey` or `SfxVolumeParam` — Music panel code references `BlipBootstrap.SfxVolumeDbKey` if interop needed (clean separation).
- `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — Slider placement + layout under Settings panel.
- [ ] Phase 2 — Slider handlers + dB conversion + PlayerPrefs persistence + restore-on-open.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | Settings panel slider rows | 1 | _pending_ | _pending_ | Open existing Settings panel scene / prefab. Add 2 slider rows (Master + Music) above SFX slider (Blip-owned — do not reorder Blip rows). Each row: TMP label ("Master Volume" / "Music Volume") + `Slider` UI component, min=0, max=1, whole-numbers=false. Theme per `UiTheme`. Exact path TBD at authoring — check for in-flight Blip Settings work first + coordinate placement (exploration doc §5.3 — "reuse Blip Settings panel scaffolding when it lands"). |
| T3.1.2 | SFX slider collision check | 1 | _pending_ | _pending_ | Verify SFX slider (if present from Blip Settings step) reads `BlipBootstrap.SfxVolumeDbKey = "BlipSfxVolumeDb"` + writes `SfxVolume` mixer param — both Blip-owned. Music sliders MUST use different keys (`MasterVolumeDb` / `MusicVolumeDb`) + different params (`MasterVolume` / `MusicVolume`) — zero collision. If SFX slider not yet implemented, leave placeholder row + doc dependency in Stage 3.1 exit notes. |
| T3.1.3 | Master slider handler + restore | 2 | _pending_ | _pending_ | Attach `MasterVolumeSliderController : MonoBehaviour` (or inline on Settings panel script). `OnValueChanged(float t)` body: `float db = Mathf.Lerp(-80f, 0f, t); musicBootstrap.BlipMixer.SetFloat(MusicBootstrap.MasterVolumeParam, db); PlayerPrefs.SetFloat(MusicBootstrap.MasterVolumeDbKey, db);`. Restore on `OnEnable`: `float db = PlayerPrefs.GetFloat(MusicBootstrap.MasterVolumeDbKey, 0f); slider.value = Mathf.InverseLerp(-80f, 0f, db);`. Use `FindObjectOfType<MusicBootstrap>()` (cached in Awake; invariant #3) for mixer ref. |
| T3.1.4 | Music slider handler + restore | 2 | _pending_ | _pending_ | Mirror T3.1.3 structure for Music slider — use `MusicBootstrap.MusicVolumeParam` + `MusicBootstrap.MusicVolumeDbKey`. Keep identical dB conversion (−80..0). `npm run unity:compile-check` green. Manual smoke: move Master slider → Master group dB changes in Audio Mixer window; restart scene → slider position restored. |

---

#### Stage 3.2 — Music Credits sub-screen

**Status:** _pending_

**Objectives:** `Settings > Music > Credits` sub-screen scrollable list of track attribution. Rendered from `MusicPlaylist.Tracks` — row format `"{Title} — {Artist} — {LicenseBlurb}"`. Text-only (no audio preview — out of scope per doc §6.3). Settings nav entry wired. Empty playlist → screen shows "No tracks loaded" placeholder row.

**Exit:**

- `MusicCreditsPanel.cs` at `Assets/Scripts/UI/Settings/MusicCreditsPanel.cs` — populates scrollable list from `MusicPlaylist.Tracks`. Rows instantiated from list-item prefab.
- List container uses `Assets/UI/Prefabs/UI_ScrollListShell.prefab` scaffold per ui-design-system §5.2.
- Settings nav entry wired — clicking "Music" group in Settings tree expands to show "Credits" leaf; clicking "Credits" activates `MusicCreditsPanel` + hides other settings panels.
- Empty playlist guard — `if (playlist.Tracks.Count == 0) { AddPlaceholderRow("No tracks loaded"); return; }`.
- `npm run unity:compile-check` green.

**Phases:**

- [ ] Phase 1 — `MusicCreditsPanel` MB + list population logic.
- [ ] Phase 2 — Settings nav entry wiring + `UI_ScrollListShell` scaffold + row prefab.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | MusicCreditsPanel scaffold | 1 | _pending_ | _pending_ | Author `MusicCreditsPanel : MonoBehaviour` at `Assets/Scripts/UI/Settings/MusicCreditsPanel.cs`. Fields: `[SerializeField] private MusicBootstrap musicBootstrap; [SerializeField] private Transform listContainer; [SerializeField] private GameObject rowPrefab;`. `FindObjectOfType<MusicBootstrap>()` fallback in `Awake` (invariant #3). |
| T3.2.2 | Populate list on enable | 1 | _pending_ | _pending_ | `OnEnable` body: clear `listContainer` children (iterate + `Destroy` stale rows); read playlist via `musicBootstrap.Playlist` accessor (add `public MusicPlaylist Playlist => _playlist;` + setter on `MusicBootstrap` — currently passed to `MusicPlayer` via `InjectPlaylist`; `MusicBootstrap` must retain ref). For each `MusicTrack t` in `playlist.Tracks`: `Instantiate(rowPrefab, listContainer)`; populate TMP child w/ `$"{t.Title} — {t.Artist} — {t.LicenseBlurb}"`. Empty guard: `if (playlist.Tracks.Count == 0) { /* instantiate placeholder row "No tracks loaded" */ return; }`. |
| T3.2.3 | Scroll list scaffold + row prefab | 2 | _pending_ | _pending_ | Build Credits panel prefab at `Assets/Prefabs/UI/MusicCreditsPanel.prefab`. Use `Assets/UI/Prefabs/UI_ScrollListShell.prefab` as list frame (ui-design-system §5.2). Row prefab at `Assets/Prefabs/UI/MusicCreditsRow.prefab` — single TMP child w/ `overflowMode = Ellipsis`. Theme via `UiTheme`. |
| T3.2.4 | Settings nav entry | 2 | _pending_ | _pending_ | Wire Settings panel nav: add "Music" group in nav tree w/ "Credits" leaf. Click on "Credits" activates `MusicCreditsPanel` GameObject + deactivates sibling panels. Exact Settings nav implementation TBD — check current Settings panel structure at authoring. If Settings panel lacks nav-tree concept, add simple button + tabbed-panel switcher scoped to this stage. `npm run unity:compile-check` green. Manual smoke: open Settings → Music → Credits → see 3 placeholder rows. |

---

#### Stage 3.3 — First-run toast + resume polish

**Status:** _pending_

**Objectives:** First-run toast fires exactly once on first gameplay scene load via `GameNotificationManager.Instance.PostNotification(...)`. Immediate `MusicFirstRunDone = 1` flip (fire-and-forget — no dismissal callback per doc §6.2). Resume track-id smoke coverage — verify fallback warn + shuffle-fresh on missing id (already coded Stage 2.1; this stage adds explicit test path + closes exploration doc Example 4). Lock correct `PostNotification` API signature — `ShowMessage` does not exist; name drift in exploration doc §6.2 + P6 noted in master plan Step 3 header.

**Exit:**

- First-run toast logic lives on `MusicBootstrap` (per exploration doc Design Expansion Components — `MusicFirstRunToast` not a new MB). Method `TryShowFirstRunToast()` called from `MusicBootstrap.Start` (not `Awake` — `GameNotificationManager.Instance` may not be alive yet in Awake ordering; `Start` runs after all Awakes).
- Toast uses `GameNotificationManager.Instance.PostNotification("Jazz playing — toggle via top-right", GameNotificationManager.NotificationType.Info, 5f)` — real API signature (3-arg overload at L188 duration overload). NOT `ShowMessage` (does not exist).
- Duration override contingency — if 3-arg `PostNotification(msg, type, duration)` routes `duration` through but `GameNotificationManager.notificationDuration` field overrides at queue-pop time (coroutine internals unverified), fall-back acceptance = 3s default + doc note in exploration doc revision. Stage 3.3 task intent locks verification step.
- `PlayerPrefs.SetInt(MusicBootstrap.MusicFirstRunDoneKey, 1)` immediately after call (before toast dismissal — fire-and-forget per doc §6.2).
- Resume track-id smoke — editor script or manual PlayerPrefs poke sets `MusicLastTrackId = "t-does-not-exist"`; reload MainMenu; verify warn log + shuffle-fresh start (`BootstrapAutoplay` Stage 2.1 T2.1.10 already wires this — Stage 3.3 adds explicit manual test).
- `npm run unity:compile-check` green.
- Exploration doc §6.2 + P6 "ShowMessage" naming drift noted in MEMORY.md or exploration-doc revision (optional follow-up; non-blocker for Music MVP).

**Phases:**

- [ ] Phase 1 — `TryShowFirstRunToast` on `MusicBootstrap.Start` + PlayerPrefs flag flip + correct API.
- [ ] Phase 2 — Resume track-id smoke + duration contingency verification.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.3.1 | TryShowFirstRunToast on Start | 1 | _pending_ | _pending_ | Add `private void Start()` + `private void TryShowFirstRunToast()` on `MusicBootstrap`. `Start` body: `TryShowFirstRunToast();`. Method body: `if (PlayerPrefs.GetInt(MusicFirstRunDoneKey, 0) != 0) return; var gnm = GameNotificationManager.Instance; if (gnm == null) { Debug.LogWarning("[Music] GameNotificationManager not alive — first-run toast skipped"); PlayerPrefs.SetInt(MusicFirstRunDoneKey, 1); return; } gnm.PostNotification("Jazz playing — toggle via top-right", GameNotificationManager.NotificationType.Info, 5f); PlayerPrefs.SetInt(MusicFirstRunDoneKey, 1);`. Reason `Start` not `Awake`: `GameNotificationManager.Instance` set in its own `Awake`; execution order uncertain → `Start` is safe. |
| T3.3.2 | Duration contingency verify | 1 | _pending_ | _pending_ | Verify at authoring time — does `PostNotification(msg, type, duration)` (L188 3-arg overload) actually apply per-call `duration` OR does `DisplayNotificationCoroutine` use the serialized `notificationDuration` field? Read `GameNotificationManager.cs` L200-260 during implementation; if duration arg is ignored, either (a) patch `GameNotificationManager` to use arg (out-of-scope Music MVP — split to separate TECH id) OR (b) accept default 3s + update exploration doc §6.2 to state "toast ~3s default". Log outcome in task Verification block + MEMORY.md. |
| T3.3.3 | Resume missing-id smoke | 2 | _pending_ | _pending_ | Manual smoke — create `EditorTools/ForceMissingMusicTrackId.cs` editor menu (Territory Developer → Music → Force missing last-track id). Menu body: `PlayerPrefs.SetString("MusicLastTrackId", "t-does-not-exist"); PlayerPrefs.Save(); Debug.Log("[Music] Forced missing last-track id — reload MainMenu to verify fallback");`. Operator runs menu → reloads MainMenu → observes warn log `"[Music] last track 't-does-not-exist' not in playlist — starting shuffle-fresh"` + music autoplays fresh track. Closes exploration doc §Examples "Missing track on resume". |
| T3.3.4 | Second-launch no-toast smoke | 2 | _pending_ | _pending_ | Manual smoke — `Window → Unity Registry → clear PlayerPrefs` (or editor menu `Edit → Clear All PlayerPrefs`) → launch MainMenu → toast shows (~5s OR 3s per T3.3.2 outcome) → stop play → launch MainMenu again → toast does NOT show (flag `MusicFirstRunDone = 1` persisted). `npm run unity:compile-check` green. Stage 3.3 closes Step 3 — all 6 exploration Implementation Points landed (P1 + P2 Step 1, P3 + P4 Step 2, P5 + P6 Step 3). |

---

## Orchestration guardrails

- **No BACKLOG rows until `stage-file`.** Tasks in this plan stay `_pending_` status. `/stage-file ia/projects/music-player-master-plan.md Stage 1.1` materializes BACKLOG ids + `ia/projects/{ISSUE_ID}.md` specs.
- **No orchestrator close.** This file is permanent per `ia/rules/orchestrator-vs-spec.md`. Stage close is handled by `project-stage-close` skill (stage `Final` rollup only); issue close by `/closeout` (individual TECH-ids + spec files). This orchestrator never appears in BACKLOG.
- **Parallel-work rule.** Do NOT run `/stage-file` or `/closeout` concurrently against this orchestrator + `ia/projects/blip-master-plan.md` — glossary + MCP index regens must sequence on single branch.
- **Blip mixer coordination.** Stage 1.1 mixer edit lands between Blip stages (not during). Check `ia/projects/blip-master-plan.md` status before running `/stage-file` on Music 1.1 — if Blip is mid-stage on mixer work, pause + wait for Blip stage `Final`. Current state at plan landing (2026-04-16): Blip Step 1 mostly done, Step 2 Final, Step 3 Final — mixer asset stable → safe window for Music 1.1.
- **Invariants checklist (re-run at every `/kickoff`):**
  - #3 — `MusicPlayer`/`MusicBootstrap`/`NowPlayingWidget`/`MusicCreditsPanel` cache refs in `Awake` via `[SerializeField] private` + `FindObjectOfType` fallback. No `FindObjectOfType` / `GetComponent` inside `Update` / per-frame. EQ-bars `Update` reads only cached `Image[]` + writes `sizeDelta` (struct — zero alloc).
  - #4 — `MusicBootstrap.Instance` + `MusicPlayer` MB are Inspector-placed. Zero `new MusicBootstrap()` / `new MusicPlayer()`. `DontDestroyOnLoad(transform.root.gameObject)` persists across scene loads.
- **Glossary rows** — deferred to `/stage-file` + `/implement`. Target rows per exploration doc §10 item 27: **Music track**, **Music playlist**, **Music player**, **Now-playing widget**, **Music mixer group**, **Music Credits**. Add on the stage that introduces each term (e.g. Music track + Music playlist land Stage 1.2; Music player lands Stage 2.1; Now-playing widget lands Stage 2.2; Music mixer group lands Stage 1.1; Music Credits lands Stage 3.2).
- **Naming drift noted:** exploration doc §6.2 + P6 write "ShowMessage" but `GameNotificationManager.cs` exposes `PostNotification(message, type, duration)`. Master plan task intents use real API. Optional follow-up — revise exploration doc to match OR add glossary row asserting `PostNotification` as canonical.
- **Spec indexing gap:** `ia/specs/audio-blip.md` present on disk but not indexed by MCP `list_specs` (flagged in exploration doc Review Notes §Gaps). Stage 1.1 task authoring reads spec directly. Registration follow-up = separate TECH-id, out of music-player scope.

---
