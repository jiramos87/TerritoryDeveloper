### Stage 6 — Settings sliders + Credits + first-run toast + resume polish / Music Credits sub-screen

**Status:** _pending_

**Objectives:** `Settings > Music > Credits` sub-screen scrollable list of track attribution. Rendered from `MusicPlaylist.Tracks` — row format `"{Title} — {Artist} — {LicenseBlurb}"`. Text-only (no audio preview — out of scope per doc §6.3). Settings nav entry wired. Empty playlist → screen shows "No tracks loaded" placeholder row.

**Exit:**

- `MusicCreditsPanel.cs` at `Assets/Scripts/UI/Settings/MusicCreditsPanel.cs` — populates scrollable list from `MusicPlaylist.Tracks`. Rows instantiated from list-item prefab.
- List container uses `Assets/UI/Prefabs/UI_ScrollListShell.prefab` scaffold per ui-design-system §5.2.
- Settings nav entry wired — clicking "Music" group in Settings tree expands to show "Credits" leaf; clicking "Credits" activates `MusicCreditsPanel` + hides other settings panels.
- Empty playlist guard — `if (playlist.Tracks.Count == 0) { AddPlaceholderRow("No tracks loaded"); return; }`.
- `npm run unity:compile-check` green.
- Phase 1 — `MusicCreditsPanel` MB + list population logic.
- Phase 2 — Settings nav entry wiring + `UI_ScrollListShell` scaffold + row prefab.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | MusicCreditsPanel scaffold | _pending_ | _pending_ | Author `MusicCreditsPanel : MonoBehaviour` at `Assets/Scripts/UI/Settings/MusicCreditsPanel.cs`. Fields: `[SerializeField] private MusicBootstrap musicBootstrap; [SerializeField] private Transform listContainer; [SerializeField] private GameObject rowPrefab;`. `FindObjectOfType<MusicBootstrap>()` fallback in `Awake` (invariant #3). |
| T6.2 | Populate list on enable | _pending_ | _pending_ | `OnEnable` body: clear `listContainer` children (iterate + `Destroy` stale rows); read playlist via `musicBootstrap.Playlist` accessor (add `public MusicPlaylist Playlist => _playlist;` + setter on `MusicBootstrap` — currently passed to `MusicPlayer` via `InjectPlaylist`; `MusicBootstrap` must retain ref). For each `MusicTrack t` in `playlist.Tracks`: `Instantiate(rowPrefab, listContainer)`; populate TMP child w/ `$"{t.Title} — {t.Artist} — {t.LicenseBlurb}"`. Empty guard: `if (playlist.Tracks.Count == 0) { /* instantiate placeholder row "No tracks loaded" */ return; }`. |
| T6.3 | Scroll list scaffold + row prefab | _pending_ | _pending_ | Build Credits panel prefab at `Assets/Prefabs/UI/MusicCreditsPanel.prefab`. Use `Assets/UI/Prefabs/UI_ScrollListShell.prefab` as list frame (ui-design-system §5.2). Row prefab at `Assets/Prefabs/UI/MusicCreditsRow.prefab` — single TMP child w/ `overflowMode = Ellipsis`. Theme via `UiTheme`. |
| T6.4 | Settings nav entry | _pending_ | _pending_ | Wire Settings panel nav: add "Music" group in nav tree w/ "Credits" leaf. Click on "Credits" activates `MusicCreditsPanel` GameObject + deactivates sibling panels. Exact Settings nav implementation TBD — check current Settings panel structure at authoring. If Settings panel lacks nav-tree concept, add simple button + tabbed-panel switcher scoped to this stage. `npm run unity:compile-check` green. Manual smoke: open Settings → Music → Credits → see 3 placeholder rows. |

---
