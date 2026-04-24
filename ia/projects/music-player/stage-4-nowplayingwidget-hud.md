### Stage 4 — MusicPlayer runtime + NowPlayingWidget / NowPlayingWidget HUD

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
- Phase 1 — Widget scaffold + ref caching + event subscribe/unsubscribe.
- Phase 2 — Layout + TMP text + theme consumption + corner anchor.
- Phase 3 — EQ-bars icon animation + 3 buttons wired + empty-playlist guard.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | NowPlayingWidget scaffold + ref cache | _pending_ | _pending_ | Author `NowPlayingWidget : MonoBehaviour` at `Assets/Scripts/UI/Hud/NowPlayingWidget.cs`. Fields: `[SerializeField] private MusicPlayer musicPlayer; [SerializeField] private TextMeshProUGUI titleArtistText; [SerializeField] private Image[] eqBars; [SerializeField] private Image playPauseIcon; [SerializeField] private Sprite playSprite; [SerializeField] private Sprite pauseSprite; [SerializeField] private Button prevButton; [SerializeField] private Button playPauseButton; [SerializeField] private Button nextButton; [SerializeField] private UiTheme uiTheme;`. `Awake` body: `if (musicPlayer == null) musicPlayer = FindObjectOfType<MusicPlayer>();` + same for `uiTheme` (invariant #3 — cache once, no per-frame lookup). |
| T4.2 | Event subscribe / unsubscribe | _pending_ | _pending_ | `OnEnable` body: `if (musicPlayer != null) { musicPlayer.OnTrackChanged += HandleTrackChanged; musicPlayer.OnPlayStateChanged += HandlePlayStateChanged; HandleTrackChanged(musicPlayer.CurrentTrack); HandlePlayStateChanged(musicPlayer.IsPlaying); }`. `OnDisable` body: mirror w/ `-=`. Null-safe — null musicPlayer = no-op. Handlers: `HandleTrackChanged(MusicTrack t)` sets `titleArtistText.text = $"{t.Title} • {t.Artist}"`; `HandlePlayStateChanged(bool playing)` swaps `playPauseIcon.sprite = playing ? pauseSprite : playSprite`. |
| T4.3 | Layout + corner anchor + TMP ellipsis | _pending_ | _pending_ | Build widget prefab `Assets/Prefabs/UI/NowPlayingWidget.prefab`. Root `RectTransform`: `anchorMin = anchorMax = (1, 1)`, `pivot = (1, 1)`, `sizeDelta = (260, 32)`, `anchoredPosition = (-16, -16)` (16px inset from top-right HUD corner). Child TMP `titleArtistText`: `overflowMode = Ellipsis`, horizontal anchor stretched between bars-icon and button-row, font from `UiTheme`. No full-stretch on root (ui-design-system §1.3.1 warning). |
| T4.4 | UiTheme consumption | _pending_ | _pending_ | Apply theme in `Start` (after `UiTheme` resolved in `Awake`): `titleArtistText.color = uiTheme.TextPrimary; titleArtistText.font = uiTheme.PrimaryFont;` (adapt to actual `UiTheme` public API — inspect `Assets/Scripts/Managers/GameManagers/UiTheme.cs` fields; use closest existing color/font tokens for HUD row parity per ui-design-system §5.2). Also theme EQ bars + button icon tints. |
| T4.5 | EQ-bars icon animation | _pending_ | _pending_ | In `Update`: `if (musicPlayer.IsPlaying) { for (int i = 0; i < eqBars.Length; i++) { float amp = Mathf.Abs(Mathf.Sin(Time.time * 6f + i * 0.7f)); float h = 4f + amp * 20f; eqBars[i].rectTransform.sizeDelta = new Vector2(2f, h); } } else { for (int i = 0; i < eqBars.Length; i++) { eqBars[i].rectTransform.sizeDelta = new Vector2(2f, 4f); } }`. Refs `eqBars` cached in Inspector — no `GetComponent` / `Find` inside `Update` (invariant #3). Struct assignment on `sizeDelta` — zero alloc. |
| T4.6 | Button wiring + empty-playlist guard | _pending_ | _pending_ | `Start` body — after theme applied: `if (musicPlayer == null) { gameObject.SetActive(false); return; }` + null-check `CurrentTrack` → `SetActive(false)` if no track (empty playlist state per doc §6.1). Wire buttons: `prevButton.onClick.AddListener(() => musicPlayer.Prev()); playPauseButton.onClick.AddListener(() => musicPlayer.TogglePlayPause()); nextButton.onClick.AddListener(() => musicPlayer.Next());`. Title TMP gets no click handler (doc §6.1 "Text click = no-op"). Place prefab instance under `Canvas/DataPanelButtons` in `MainMenu.unity` + gameplay scene. `npm run unity:compile-check` green. |

---
