### Stage 5 — ThemedPrimitive ring / Primitives batch B (Tab / Slider / Toggle / List / OverlayToggleRow) + broadcaster wiring

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship remaining 5 structural primitives + promote `ThemeBroadcaster` stub to full impl + land `UIManager.ThemeBroadcast.cs` partial. Token-swap end-to-end test lives here.

**Exit:**

- `ThemedTabBar`, `ThemedSlider`, `ThemedToggle`, `ThemedList`, `ThemedOverlayToggleRow` shipped under `Assets/Scripts/UI/Primitives/`.
- `ThemeBroadcaster.cs` full impl — `Awake` scans `FindObjectsOfType<MonoBehaviour>()`, filters `IThemed`, caches list; `BroadcastAll()` iterates cached list; additional subscribers registered via `Register(IThemed)` for runtime spawns.
- `UIManager.ThemeBroadcast.cs` partial — `[SerializeField] private ThemeBroadcaster themeBroadcaster` + `partial void Start_ThemeBroadcast()` hook + single `themeBroadcaster.BroadcastAll()` call. Invoked from existing `UIManager.Start` via partial method pattern; no body changes to other partials.
- EditMode test: scene w/ 10 primitives + `ThemeBroadcaster`; token field edit → all 10 repaint (assert `ApplyTheme` called on each).
- Phase 1 — Batch B primitives.
- Phase 2 — Broadcaster full impl + UIManager partial.
- Phase 3 — Integration test + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | ThemedTabBar + ThemedList | _pending_ | _pending_ | `ThemedTabBar.cs` — horizontal tab row; each tab is a `ThemedButton` variant with selected/unselected color states. `ThemedList.cs` — vertical list container; row spacing from `theme.spacing.listRow`; alternate row bg from `theme.surfaceAlt`. Both `ApplyTheme` cascade to children. |
| T5.2 | ThemedSlider + ThemedToggle | _pending_ | _pending_ | `ThemedSlider.cs` — wraps uGUI `Slider`; track / fill / handle colors from accent ladder. `ThemedToggle.cs` — wraps `Toggle`; on/off colors + checkmark icon tint from token accent. Both `ApplyTheme` writes to child `Image.color` fields cached `Awake`. |
| T5.3 | ThemedOverlayToggleRow | _pending_ | _pending_ | `ThemedOverlayToggleRow.cs` — composite for Bucket 2 signal overlays (pollution / crime / traffic / etc.). Holds `ThemedIcon` + `ThemedLabel` + a small active-state indicator slot (filled by `LED` in Step 3). `ApplyTheme` cascades to named children. One instance per signal. |
| T5.4 | ThemeBroadcaster full impl | _pending_ | _pending_ | Promote Stage 1.2 stub `ThemeBroadcaster.cs` to full runtime impl. `Awake`: single scan `FindObjectsOfType<MonoBehaviour>()` → filter `IThemed` → cache `List<IThemed>`. Expose `Register(IThemed)` / `Unregister(IThemed)` for runtime spawns. `BroadcastAll()` foreach cached list → `ApplyTheme(theme)`. Invariant #3 compliant — scan only in `Awake` + explicit `Register`. |
| T5.5 | UIManager.ThemeBroadcast partial | _pending_ | _pending_ | New file `Assets/Scripts/Managers/GameManagers/UIManager.ThemeBroadcast.cs`. `partial class UIManager` with `[SerializeField] private ThemeBroadcaster themeBroadcaster;` + `private void Start_ThemeBroadcast() { themeBroadcaster?.BroadcastAll(); }`. Wire the call from existing `UIManager.cs` `Start` via a single added line (the only edit to an existing partial; document the one-line exemption in commit). |
| T5.6 | Token-swap integration test + glossary | _pending_ | _pending_ | EditMode / PlayMode test scene with all 10 primitives + `ThemeBroadcaster`. Token edit → `BroadcastAll()` → assert `ApplyTheme` called once per primitive. Assert allocation-free repaint via `GC.GetTotalMemory` delta check. Glossary row: `ThemeBroadcaster` (scene MonoBehaviour, `Awake`-cached `IThemed` list, invariant #3 + #4 compliant). |

---
