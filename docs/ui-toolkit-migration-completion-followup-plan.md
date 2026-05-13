# UI Toolkit Migration — Completion Follow-up Plan

**Status:** Draft (2026-05-13) · **Branch:** `feature/asset-pipeline` (worktree `ui-toolkit-completion-citystats-mainmenu`) · **Prereq plan:** `docs/ui-toolkit-migration-completion-plan.md`

Goal — close the gap between current state (HUD strip + city-stats rendering live values via UIToolkit) and pre-migration parity (toolbar buttons + icons, subtype picker, stats/budget/glossary modal togglers, time-controls, mini-map). Same execution constraint: Unity 2022.3, no runtime `dataSource` API, manual `Q<>` binding pattern.

---

## 1. Inventory — what is missing vs pre-migration

| Surface | Pre-migration (UGUI) | Current (UIToolkit) | Gap |
|---|---|---|---|
| **Toolbar** | `ToolbarDataAdapter` + `IlluminatedButton[]` arrays (zoning ×10, road, terrain, building ×2) with custom sprite icons + tier rows | `toolbar-uidoc` with empty `<ui:ListView name="tool-list">` + `<ui:Label name="active-tool-label">` | No items wired; no icons; no subtype picker |
| **Subtype picker** | `SubtypePickerController` opens contextual picker on parent-tool click | `tool-subtype-picker-uidoc` NOT in CityScene | UIDocument GO absent; Host stub only |
| **Stats / Budget / Glossary togglers** | `ShowStatsButton`, `ShowTaxes`, `OpenBudgetButton`, `GlossaryButton` UGUI buttons | `hud-bar.uxml` has no toggler buttons | UXML missing; Host has no click handlers |
| **Time-controls** | `SpeedButtonsController` + Pause/1x/2x/3x UGUI buttons | `time-controls-uidoc` Buttons via broken `binding-path="PauseCommand"` | Need manual `Q<Button>.clicked` + `TimeManager` wire |
| **Mini-map** | `MiniMapController` + RawImage with RenderTexture | `mini-map-uidoc` with empty `mini-map__surface` Image | No RT camera, no projection |
| **Modal panels (stats/budget/glossary/info/building-info/etc.)** | UGUI panels toggled by UIManager popup stack | UIDocuments SetActive(false), modal Hosts call `RegisterMigratedPanel` | Show triggers not wired (Esc, building-click, key shortcuts, toggler buttons) |
| **Panel chrome (cream/tan vs dark)** | Cream `#f5e6c8` bg + tan border per `ia/specs/ui-design-system.md` § Tokens | Dark `#313244` bg per `Assets/UI/Themes/dark.tss` | Token system mismatch (deferred — out of session scope) |

---

## 2. Architecture decisions

### 2.1 Tools — minimal viable list

Skip ListView complexity. Hard-code 4 top-level tool buttons in `toolbar.uxml` matching pre-migration core actions: **Zone · Road · Demolish · Build**. Each routes click → `ToolbarHost.OnToolSelected(slug)` → opens subtype picker via `ModalCoordinator.Show("tool-subtype-picker")`.

### 2.2 Subtype picker

Add `tool-subtype-picker-uidoc` GO to CityScene (deactivated by default). Host populates ListView on `Show()` based on selected parent tool. Manual `Q<ListView>.itemsSource = new[]{"Residential L/M/H","Commercial L/M/H",...}` per parent tool.

### 2.3 Modal toggler buttons

Add 4 toggler buttons to `hud-bar.uxml` right cluster: **📊 Stats · 💰 Budget · 📖 Glossary · ⏸ Pause** (Pause moves out of time-controls into hud-bar; time-controls keeps speed 1x/2x/3x only).

`HudBarHost` extends with `Q<Button>.clicked += () => _coordinator.Show("stats-panel")` etc.

### 2.4 Time-controls

`TimeControlsHost` rewrite: manual `Q<Button>` lookup for Pause/Speed1/2/3, hook `clicked +=` to `TimeManager.SetTimeSpeedIndex(idx)` + `IsPaused` toggle.

### 2.5 Icons

Skip raster icons (no sprite-atlas mapping for UIToolkit yet). Use Unicode glyphs as placeholders: 🏘 zone · 🛣 road · ⛏ demolish · 🏗 build · 📊 stats · 💰 budget · 📖 glossary · ⏸ pause. Real sprite injection deferred to a separate icon-atlas pass.

### 2.6 Mini-map

Skip in this session. Mini-map needs RenderTexture camera projection + grid sampling — significant work. Leave `mini-map-uidoc` rendering as empty frame; follow-up issue.

### 2.7 Backgrounds / chrome

Keep dark theme tokens (current `dark.tss`). Cream/tan parity is a separate visual-fidelity pass requiring token regeneration through panels.json + bake handler. Out of scope.

### 2.8 ModalCoordinator GameObject SetActive trigger

`ModalCoordinator.Show(slug)` currently does `ve.style.display = DisplayStyle.Flex` (USS-level). But our modal panel GOs are `SetActive(false)`. Need to extend `Show()` to also flip `gameObject.SetActive(true)` on the UIDocument GO.

Patch: in `ModalCoordinator.RegisterMigratedPanel`, store reference to the UIDocument GO. In `Show(slug)`, call `panelGo.SetActive(true)` before `ve.style.display = Flex`. In `HideMigrated(slug)`, call `SetActive(false)` after `ve.style.display = None`.

Hosts pass `_doc.gameObject` to `RegisterMigratedPanel` overload.

---

## 3. Phased execution

Each phase mechanical, ~1 file or 1 patch. Run compile-check between phases.

### Phase A — ModalCoordinator GO-toggle extension

| Step | File | Action |
|---|---|---|
| A1 | `Assets/Scripts/UI/Modals/ModalCoordinator.cs` | Add overload `RegisterMigratedPanel(string slug, VisualElement root, GameObject panelGo)`. Store GO in `Dictionary<string,GameObject> _panelGos`. In `Show(slug)`: if GO known + inactive → `SetActive(true)`. In `HideMigrated(slug)`: `SetActive(false)`. |
| A2 | All Modal Hosts (`AlertsPanelHost`, `BudgetPanelHost`, `BuildingInfoHost`, `GlossaryPanelHost`, `GrowthBudgetPanelHost`, `InfoPanelHost`, `LoadViewHost`, `MapPanelHost`, `StatsPanelHost`, `ToolSubtypePickerHost`) | Update `RegisterMigratedPanel(slug, root)` call → `RegisterMigratedPanel(slug, root, gameObject)`. Sed-bulk. |
| A3 | Compile-check | `npm run unity:compile-check` |

### Phase B — Time-controls Host rewrite (Q&lt;Button&gt; manual wiring)

| Step | File | Action |
|---|---|---|
| B1 | `Assets/Scripts/UI/Hosts/TimeControlsHost.cs` | Rewrite: cache `Button _btnPause/_btnSpeed1/_btnSpeed2/_btnSpeed3` + `Label _speedLabel` in OnEnable. Hook `clicked += () => TimeManager.SetTimeSpeedIndex(idx)`. Pause toggles via `TimeManager.SetModalPauseOwner("ui-time-controls")` / `ClearModalPauseOwner`. Update label in `Update()`. |
| B2 | `Assets/UI/Generated/time-controls.uss` | Increase button visibility — `font-size: 16px`, `min-width: 36px`, `min-height: 32px`, dark bg + light text. Anchor strip top-right (top:8px right:8px). |
| B3 | Compile-check |  |

### Phase C — Hud-bar toggler buttons

| Step | File | Action |
|---|---|---|
| C1 | `Assets/UI/Generated/hud-bar.uxml` | Inject 4 buttons in `hud-bar__right` cluster: `btn-stats` (text="📊"), `btn-budget` (text="💰"), `btn-glossary` (text="📖"), `btn-pause` (text="⏸"). Class `hud-bar__toggler`. |
| C2 | `Assets/UI/Generated/hud-bar.uss` | Add `.hud-bar__toggler { background-color: transparent; border-color: var(--ds-color-accent-2); border-width:1px; padding: 2px 8px; margin-left: 4px; font-size: 14px; }` |
| C3 | `Assets/Scripts/UI/Hosts/HudBarHost.cs` | OnEnable: `Q<Button>("btn-stats").clicked += () => _coord.Show("stats-panel")` (+ "budget-panel", "glossary-panel", and pause toggle). Resolve `_coord = FindObjectOfType<ModalCoordinator>()` in Awake. |
| C4 | Compile-check |  |

### Phase D — Reactivate modal UIDocs (now safely hidden via ModalCoordinator)

| Step | Action |
|---|---|
| D1 | Python YAML edit `Assets/Scenes/CityScene.unity`: flip `m_IsActive: 0 → 1` for `stats-panel-uidoc`, `budget-panel-uidoc`, `glossary-panel-uidoc`. They'll instantiate, Host OnEnable calls `RegisterMigratedPanel` which (Phase A) hides GO via SetActive(false). |
| D2 | Compile-check + open scene to verify no errors |

### Phase E — Toolbar buttons (hard-coded 4 actions, no ListView)

| Step | File | Action |
|---|---|---|
| E1 | `Assets/UI/Generated/toolbar.uxml` | Replace `<ui:ListView>` with 4 explicit `<ui:Button>` children: `btn-zone` (text="🏘 Zone"), `btn-road` (text="🛣 Road"), `btn-demolish` (text="⛏ Demolish"), `btn-build` (text="🏗 Build"). |
| E2 | `Assets/UI/Generated/toolbar.uss` | Style `.toolbar__tool-btn` to vstack each button 64×64, dark bg, light text, hover state. Strip width=80px. |
| E3 | `Assets/Scripts/UI/Hosts/ToolbarHost.cs` | OnEnable: Q each btn, hook `clicked += () => OnToolSelected(slug)`. OnToolSelected logs + opens `tool-subtype-picker` modal (Show("tool-subtype-picker")). |
| E4 | Compile-check |  |

### Phase F — Subtype picker UIDocument

| Step | Action |
|---|---|
| F1 | Inspect `Assets/UI/Generated/tool-subtype-picker.uxml`. Confirm root has `.tool-subtype-picker` class with absolute positioning. If missing, add `position:absolute; top:140px; left:88px; background:#313244; padding:8px; border-radius:8px`. |
| F2 | Add `tool-subtype-picker-uidoc` GameObject to CityScene via `unity_bridge_command set_gameobject_active` (or python YAML insert). Reference: copy structure from existing `*-uidoc` GOs. Set `m_IsActive: 0`. |
| F3 | `Assets/Scripts/UI/Hosts/ToolSubtypePickerHost.cs` — populate `ListView.itemsSource` based on parent tool slug (received via `_coordinator.Show("tool-subtype-picker", parentToolSlug)` extension). |
| F4 | Compile-check |  |

### Phase G — Validation + commit

| Step | Action |
|---|---|
| G1 | `npm run unity:compile-check` green |
| G2 | `npm run validate:all` no delta from baseline |
| G3 | Bridge: open CityScene + enter_play_mode + capture_screenshot → verify HUD has stats/budget/glossary togglers, toolbar 4 buttons, time-controls pause/speed |
| G4 | Bridge: open MainMenu + enter_play_mode + capture_screenshot → verify UIToolkit-only TERRITORY menu still works |
| G5 | Commit: `feat(ui-toolkit-migration-completion): wire modal togglers + time-controls + toolbar via Q<> manual binding` |

---

## 4. Out of scope (separate follow-up issues)

- **Mini-map renderer** — RenderTexture + IsometricCamera projection
- **Cream/tan visual fidelity** — token system rewrite, panels.json schema_version bump, bake handler emits cream USS variants
- **Sprite icon atlas** — replace Unicode glyphs with `<ui:VisualElement style="background-image:url(...)">` per tool
- **ToolService integration** — currently OnToolSelected is stub log; needs to fire scene tool-mode change via existing `UIManager`/`SelectorButton` pipeline
- **ListView-driven dynamic tool list** — keep hard-coded 4 buttons; dynamic list requires registry binding (deferred per current ListView ambiguity)
- **Onboarding-overlay panel** — deactivated, no triggers
- **Notifications-toast queue** — empty queue, no game events feed it

---

## 5. Execution checklist

- [ ] A1 — ModalCoordinator GO-toggle overload
- [ ] A2 — Modal Hosts pass `gameObject` to RegisterMigratedPanel
- [ ] A3 — Compile green
- [ ] B1 — TimeControlsHost manual Q&lt;Button&gt; rewrite
- [ ] B2 — time-controls.uss anchor + size
- [ ] B3 — Compile green
- [ ] C1 — hud-bar.uxml togglers injected
- [ ] C2 — hud-bar.uss toggler styles
- [ ] C3 — HudBarHost click handlers
- [ ] C4 — Compile green
- [ ] D1 — Reactivate stats/budget/glossary uidoc m_IsActive=1
- [ ] D2 — Compile + verify scene loads
- [ ] E1 — toolbar.uxml 4 explicit buttons
- [ ] E2 — toolbar.uss styles
- [ ] E3 — ToolbarHost click handlers
- [ ] E4 — Compile green
- [ ] F1 — tool-subtype-picker.uss anchor
- [ ] F2 — Add GO to scene
- [ ] F3 — ToolSubtypePickerHost ListView wire
- [ ] F4 — Compile green
- [ ] G1 — validate:all
- [ ] G2 — bridge Play Mode capture CityScene
- [ ] G3 — bridge Play Mode capture MainMenu
- [ ] G4 — Commit

## 6. Risk + mitigation

| Risk | Mitigation |
|---|---|
| ModalCoordinator GO-toggle breaks existing UGUI legacy fallback path (`TryOpen`) | Keep `TryOpen` branch unchanged; only mutate `_migratedPanels` + new `_panelGos` dict |
| Hud-bar togglers conflict with happiness/weather labels positioning | Move happiness/weather above togglers (vstack inside right cluster) or shrink font |
| Reactivating stats-panel-uidoc may show panel briefly before Host OnEnable hides it | Set GO inactive in scene YAML AND set USS `display:none` on root element as fallback |
| Unicode emoji glyphs may not render in default Unity font | Use `-unity-font: "LiberationSans"` or fallback ASCII chars |
| ToolbarHost OnToolSelected → ModalCoordinator.Show("tool-subtype-picker") fires before picker UIDoc exists | Guard `if (_coordinator.IsRegistered(slug))` else log + no-op |
