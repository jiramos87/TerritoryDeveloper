### Stage 2 — Audio infra + playlist pipeline / Playlist data model + loader + import postprocessor

**Status:** _pending_

**Objectives:** Authoring data model — `MusicTrack` record struct + `MusicPlaylist` class. `MusicPlaylistLoader` static parses `Assets/Resources/Music/playlist.json` → `List<MusicTrack>` + resolves `AudioClip` handles via `Resources.Load<AudioClip>`. Fail-soft on malformed JSON (empty list + warn) + missing clips (drop row + warn). Seed `playlist.json` w/ 3 placeholder rows. Editor-only `MusicOggImportPostprocessor` auto-stamps `LoadType=Streaming` / `PreloadAudioData=false` / `CompressionFormat=Vorbis` on `.ogg` drops under `Assets/Resources/Music/`. Answers exploration doc open question Q7.

**Exit:**

- `MusicTrack` record struct at `Assets/Scripts/Audio/Music/MusicTrack.cs` — `{ string id, string filename, string title, string artist, string licenseBlurb }`.
- `MusicPlaylist` class at `Assets/Scripts/Audio/Music/MusicPlaylist.cs` — `List<MusicTrack> Tracks` + `AudioClip[] Clips` cache (parallel index).
- `MusicPlaylistLoader.LoadFromResources()` at `Assets/Scripts/Audio/Music/MusicPlaylistLoader.cs` — `Resources.Load<TextAsset>("Music/playlist")`, `JsonUtility.FromJson<MusicPlaylistJson>`, try/catch on parse. Per row: `Resources.Load<AudioClip>("Music/" + Path.GetFileNameWithoutExtension(filename))`. Null clip → log warn + skip row. Empty list on parse failure (warn).
- `Assets/Resources/Music/playlist.json` seeded w/ 3 placeholder rows (ids `placeholder-01..03`, filenames `placeholder-a.ogg` etc., dummy titles / artists / blurbs). No actual `.ogg` files yet — loader will warn + return empty list at runtime until Step 2 seed OR real tracks arrive.
- `MusicOggImportPostprocessor` at `Assets/Editor/MusicOggImportPostprocessor.cs` — `AssetPostprocessor.OnPreprocessAudio` guards `assetPath.StartsWith("Assets/Resources/Music/")` + `.ogg` suffix; sets `AudioImporter.defaultSampleSettings` to `{ loadType = AudioClipLoadType.Streaming, preloadAudioData = false, compressionFormat = AudioCompressionFormat.Vorbis, quality = 0.5f }`.
- `npm run unity:compile-check` green.
- Phase 1 — `MusicTrack` record + `MusicPlaylist` class.
- Phase 2 — `MusicPlaylistLoader` static + JSON parse + clip resolution + fail-soft paths.
- Phase 3 — `playlist.json` seed + `MusicOggImportPostprocessor` editor-only stamp.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | MusicTrack record struct | _pending_ | _pending_ | Author `public readonly record struct MusicTrack(string Id, string Filename, string Title, string Artist, string LicenseBlurb)` at `Assets/Scripts/Audio/Music/MusicTrack.cs`. Also author `[Serializable] class MusicTrackJson` + `MusicPlaylistJson` DTOs for `JsonUtility.FromJson` (record struct not Unity-serializable — DTO pair needed). Map `MusicTrackJson → MusicTrack` in loader. |
| T2.2 | MusicPlaylist class | _pending_ | _pending_ | Author `MusicPlaylist` at `Assets/Scripts/Audio/Music/MusicPlaylist.cs` — `public IReadOnlyList<MusicTrack> Tracks`, `public IReadOnlyList<AudioClip> Clips` (parallel index). Constructor takes both lists (loader-produced). Static `Empty` factory for fail-soft paths. |
| T2.3 | MusicPlaylistLoader parse + fail-soft | _pending_ | _pending_ | Author `MusicPlaylistLoader.LoadFromResources()` static at `Assets/Scripts/Audio/Music/MusicPlaylistLoader.cs`. Steps: (1) `TextAsset json = Resources.Load<TextAsset>("Music/playlist")`; null → log warn `"[Music] playlist.json missing — player disabled"` + return `MusicPlaylist.Empty`. (2) `try { var dto = JsonUtility.FromJson<MusicPlaylistJson>(json.text); } catch (Exception e) { Debug.LogWarning($"[Music] playlist.json malformed — player disabled: {e.Message}"); return MusicPlaylist.Empty; }`. |
| T2.4 | MusicPlaylistLoader clip resolution | _pending_ | _pending_ | Per-row clip resolution. `var stem = Path.GetFileNameWithoutExtension(row.filename); var clip = Resources.Load<AudioClip>("Music/" + stem);`. If `clip == null` → `Debug.LogWarning($"[Music] Track '{stem}' clip missing — skipping row")` + continue. Else append `MusicTrack` + `AudioClip` to parallel accumulator lists. Final: construct `MusicPlaylist(tracks, clips)`. Empty inputs allowed (returns empty playlist silently). |
| T2.5 | Seed playlist.json placeholders | _pending_ | _pending_ | Create `Assets/Resources/Music/playlist.json` w/ 3 placeholder rows: `{"tracks": [{"id": "placeholder-01", "filename": "placeholder-a.ogg", "title": "Placeholder A", "artist": "Dev Stub", "licenseBlurb": "Placeholder — replace at Step 3"}, ...]}`. No `.ogg` files — Resources.Load will null-return + loader warns + returns empty playlist at runtime. Real tracks dropped in at dev-time or Step 3. Commit JSON + .meta. |
| T2.6 | MusicOggImportPostprocessor | _pending_ | _pending_ | Author `MusicOggImportPostprocessor : AssetPostprocessor` at `Assets/Editor/MusicOggImportPostprocessor.cs`. Override `void OnPreprocessAudio()`. Guards: `if (!assetPath.StartsWith("Assets/Resources/Music/")) return; if (!assetPath.EndsWith(".ogg")) return;`. Body: `var importer = (AudioImporter)assetImporter; var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.Streaming; settings.preloadAudioData = false; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = 0.5f; importer.defaultSampleSettings = settings;`. Wrap in `#if UNITY_EDITOR`. Verify `npm run unity:compile-check` green. Answers exploration doc open question Q7. |

---
