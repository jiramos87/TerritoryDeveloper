# Stage 12 — Content-layer fix (game-ui-design-system)

Branch: `feature/asset-pipeline`
Stage status: trigger paths green; bake-handler patches landed; viewport-blocker triage in progress; human QA gate next.
Author: in-session diagnostic.

## TL;DR

Stage 12 trigger contract (`UIManager.OpenPopup(PopupType)` + LIFO `popupStack`) verified end-to-end. Content-layer was dead in 4 layers (A–D, all landed). Visual fidelity polish iteration uncovered 4 bake-handler-shape findings (D1–D4, all landed). **Current blocker** = panels auto-active on scene load covering viewport (Stage 13 popup-stack gate gap). Mitigated by bridge `set_gameobject_active` + `save_scene`. Toolbar + hud-bar layout fixed by IR `kind` backfill. Next: verify legacy `UI/City/Canvas` GO state post-save, re-activate hud-bar/toolbar, hand off to user for Play Mode QA.

## Architecture-relevant findings (essence)

All four are **bake-handler shape** issues — IR → prefab pipeline emits incomplete component graphs. Each fix patches `Assets/Scripts/Editor/Bridge/UiBakeHandler*.cs` or IR data; runtime code was correct.

### D1 — border tint invisible

`ThemedPanel.ApplyTheme` picked `ramp[2]` for border, only ~9 brightness units lighter than `ramp[1]` panel-fill → invisible hairline. Fix: pick `ramp[4]` (or `ramp.Length-1`) for clearly-distinct border. **Pattern:** when picking palette ramp indices, require minimum brightness delta from the surface's fill index. Landed `f0921088`.

### D2 — info-panel content empty

Bake omitted adapter-attach + slot-wiring pass. Prefab carried 16 anonymous studio-control widgets (`_slug=""`), every TMP `m_text=""`, no `InfoPanelAdapter`/`*DataBinding` on any node. Fix: bake `InfoPanelAdapter` MB at authoring time + write `_slug` per widget from IR + treat IR slot as container (`slot.children[]`). **Pattern:** bake-time adapter attach + deterministic `_slug` ref-writing; runtime `Find` is fragile. Landed.

### D3 — toolbar hover scope partial

`UiBakeHandler` emitted `IlluminatedButton`+`IlluminatedButtonRenderer` clones with no `Selectable`, no `ThemedButton`, no serialized `_halo`/`_body` refs. Hover relied on runtime `transform.Find("halo")` → succeeded on some buttons, failed on others (Awake order / stale child names). Fix: bake-time `_halo`+`_body` field writes + `Selectable` + ColorTint targetGraphic=`body` + `illuminated_button_wiring` conformance check. **Pattern:** bake-time field-ref writes eliminate runtime Find ambiguity. D3.1+D3.2 landed; D3.3 conformance kind pending (post visual QA).

### D4 — IR panel.kind missing → ApplyKindLayout defaults Modal

`ResolvePanelKindIndex(null)` returns `PanelKind.Modal` (center 600×800). 14 panels' IR carried `archetype` only, no `kind`. Fix: backfill IR `kind` per panel (toolbar→toolbar, hud-bar→hud, info-panel→modal, settings/save-load/new-game/onboarding→screen, etc.). **Pattern:** IR `archetype` ≠ `kind`; layout taxonomy is independent and must be authored explicitly. Landed 2026-05-01 (one batch re-bake).

## Current state (detailed)

### Bake-handler patches landed

| Fix | Status | Verified |
|---|---|---|
| D1 — border tint ramp index | landed (`f0921088`) | visible hairline |
| D2.1 — `InfoPanelAdapter` baked at authoring time | landed | adapter MB present on prefab |
| D2.2 — `_slug` per widget from IR | landed | conformance + adapter resolution key |
| D2.3 — placeholder `"--"` for cell-data | landed | live binding deferred Stage 13 |
| D2.4 — IR `slot.children[]` as container | landed | 3 IR slots × widgets |
| D3.1 — `_halo`+`_body` field writes | landed | renderer fields populated |
| D3.2 — `Selectable` + ColorTint targetGraphic=body | landed | reuses `button_state_block` check |
| D4 — IR `kind` backfill (14 panels) | landed | `prefab_inspect` confirms `_kind` flips |

### Scene mutations applied (`MainScene.unity`)

10 panels deactivated via bridge `set_gameobject_active` + `save_scene`:

| Panel | Path | Kind |
|---|---|---|
| overlay-toggle-strip | `UI Canvas/overlay-toggle-strip` | Toolbar |
| city-stats-handoff | `UI Canvas/city-stats-handoff` | Screen |
| onboarding-overlay | `UI Canvas/onboarding-overlay` | Screen |
| splash | `UI Canvas/splash` | Screen |
| glossary-panel | `UI Canvas/glossary-panel` | Modal |
| info-panel | `UI Canvas/info-panel` | Modal |
| pause-menu | `UI Canvas/pause-menu` | Modal |
| settings-screen | `UI Canvas/settings-screen` | Screen |
| save-load-screen | `UI Canvas/save-load-screen` | Screen |
| new-game-screen | `UI Canvas/new-game-screen` | Screen |

3 panels (`BondIssuanceModal`, `BudgetPanel`, `HudEstimatedSurplusHint`) unresolvable by `find_gameobject` — likely already invisible via parent deactivation. Not blocking.

### Pending verification (open)

- **Legacy GameObjects** (`UI/City/Canvas/DataPanelButtons`, `ControlPanel`, `GridCoordinatesText`, `MiniMapPanel`) — earlier hand-edited YAML overrides may have been overwritten by `save_scene`. Need `ui_tree_walk` on `UI/City/Canvas` to read `active_self`.
- **hud-bar + toolbar** — currently SetActive(false) from earlier batch, but they should be persistent (Hud + Toolbar kinds → top strip + left edge respectively). Need to flip back active.

## Next steps (detailed)

### Step 16.A — legacy GO deactivation (DONE 2026-05-01)

`find_gameobject` confirmed all 4 legacy GOs exist under `UI/City/Canvas/`:
- `DataPanelButtons` (Image + 9 children) → `active=false`
- `ControlPanel` (Image + 10 children) → `active=false`
- `GridCoordinatesText` → `active=false`
- `MiniMapPanel` (Image + MiniMapController + 3 children) → `active=false`

Each via bridge `set_gameobject_active`.

### Step 16.B — new panels activated (DONE 2026-05-01)

- `UI Canvas/toolbar` (Themed+ToolbarDataAdapter+VLG, 15 children) → `active=true`
- `UI Canvas/hud-bar` (Themed+HudBarDataAdapter+HLG, 21 children) → `active=true`

Then `save_scene Assets/Scenes/MainScene.unity` ok.

### Step 16.C — Play Mode QA gate (BLOCKED on 16.D)

QA preview surfaced toolbar buttons rendering as flat color tiles — no icons. Investigation findings + decision below (Step 16.D). QA bumped behind icon refactor.

**QA acceptance once 16.D green:**
- Grid clickable from default scene start.
- Toolbar at left edge, 200×stretch, 11 illuminated buttons visible **with human-art icons**.
- Hud-bar at top, 100×stretch.
- Cell click triggers `info-panel` modal at center 600×800 with content (slot widgets visible — placeholder `"--"`).
- Hover on toolbar buttons highlights all 11 uniformly (D3.1 fix).

### Step 16.D — toolbar icon refactor (A2 IR-side, ACTIVE 2026-05-01)

#### Findings

1. **No icon wired in current bake.** `IlluminatedButton` body Image fills full rect with palette color; halo Image sits centered (64×64, alpha=0). No third icon child. Comment at `IlluminatedButton.cs:54` admits *"runtime sprite/halo binding deferred to Stage 5"* — never landed.
2. **IR detail has no icon field.** `IlluminatedButtonDetail` schema = `{illuminationSlug, pulseOnEvent}` only. No sprite slug.
3. **Toolbar IR carries generic slugs.** All 11 buttons in `Assets/UI/Prefabs/Generated/toolbar.prefab` named `illuminated-button (N)`, `illuminationSlug` empty — no per-button identity for sprite mapping.
4. **Human button art exists + unused.** `Assets/Sprites/Buttons/*.png` + `Assets/Sprites/*.png` carry ~30 target/pressed PNG pairs (Bulldoze, Roads, Residential, Commercial, Industrial, Power-buildings, Water-buildings, Forest, state, Pause, Speed-1..4, Stats, Zoom-in/out, Save-game, Load-game, New-game).
5. **claude-design has no toolbar icon set** — no asset to consume from design output.
6. **Adapter slot mismatch (out of icon scope).** `ToolbarDataAdapter` declares 18 Inspector slots (10 zoning + 1 road + 1 terrain + 2 building + 3 forest + 1 bulldoze); current toolbar prefab bakes 11 buttons. Triage deferred to separate task — not blocking icon refactor.

#### Decision: Option A2 (IR-side refactor)

**Authoritative source for button icons = human art under `Assets/Sprites/Buttons/*.png` + `Assets/Sprites/*.png`.** Mandate applies to all current + future panel work in next stages.

A2 wires sprite via IR detail field + bake-time `AssetDatabase` load (Editor-only). Sprite ref serialized into prefab → no runtime `Resources.Load` cost, no PlayMode-only API. Trade-off vs A3 hybrid: simpler IR, single source of truth, but bake re-run required when art swaps.

**Implementation steps:**

1. Add `public string iconSpriteSlug;` field to `IlluminatedButtonDetail` (`Assets/Scripts/UI/StudioControls/IlluminatedButton.cs:60-64`).
2. Spawn `icon` Image child in `SpawnIlluminatedButtonRenderTargets` (`Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs:597-646`) — center 64×64, on top of body, below halo.
3. When `iconSpriteSlug` non-empty, load via `AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Buttons/{slug}-target.png")` with fallback to `Assets/Sprites/{slug}-target.png`. Assign to icon Image `.sprite` at bake.
4. Optional: add `_iconImage` `[SerializeField]` to `IlluminatedButtonRenderer` for press-state pressed-sprite swap (deferred — Stage 5 follow-up).
5. Update DB toolbar IR rows (`catalog_panel` + child `illuminated-button` rows) — assign per-button unique slug + `iconSpriteSlug` for the 11 baked buttons.
6. Re-bake toolbar prefab via bridge `bake_ui` mutation.
7. Visual verify in Editor + Play Mode (Step 16.C QA).

#### Sprite-to-slot mapping (toolbar 11-button current bake)

| Toolbar slot index | Adapter family | `iconSpriteSlug` | Source PNG |
|---|---|---|---|
| 0 | Zoning — Residential L | `Residential-button-64` | `Assets/Sprites/Residential-button-64-target.png` |
| 1 | Zoning — Commercial L | `Commercial-button-64` | `Assets/Sprites/Commercial/Commercial-button-64-target.png` |
| 2 | Zoning — Industrial L | `Industrial-button-64` | `Assets/Sprites/Industrial-button-64-target.png` |
| 3 | Road | `Roads-button-64` | `Assets/Sprites/Roads-button-64-target.png` |
| 4 | Terrain — flatten | `Bulldoze-button-64` | `Assets/Sprites/Bulldoze-button-64-target.png` |
| 5 | Building — Power | `Power-buildings-button-64` | `Assets/Sprites/Power-buildings-button-64-target.png` |
| 6 | Building — Water | `Water-buildings-button-64` | `Assets/Sprites/Water-buildings-button-64-target.png` |
| 7 | Forest — plant | `Forest-button-64` | `Assets/Sprites/Buttons/Forest-button-64-target.png` |
| 8 | Stats | `Stats-button-64` | `Assets/Sprites/Buttons/Stats-button-64-target.png` |
| 9 | Pause | `Pause-button-1-64` | `Assets/Sprites/Buttons/Pause-button-1-64-target.png` |
| 10 | Bulldoze | `Bulldoze-button-64` | `Assets/Sprites/Bulldoze-button-64-target.png` |

Mapping intentionally narrow (no L/M/H zoning split; Forest x3 share one art; bulldoze + terrain reuse). Refine when adapter mismatch (16.D scope-note 6) lands.

**Asset inventory (2026-05-01 verified).** All 11 mapping slugs resolve. Sprites distributed across three folders:
- `Assets/Sprites/Buttons/`: `Forest-button-64-target.png`, `Stats-button-64-target.png`, `Pause-button-1-64-target.png`
- `Assets/Sprites/` (root): `Residential-`, `Industrial-`, `Roads-`, `Bulldoze-`, `Power-buildings-`, `Water-buildings-button-64-target.png`
- `Assets/Sprites/Commercial/`: `Commercial-button-64-target.png`

`ResolveButtonIconSprite` (Archetype.cs) probes three paths in order: `Assets/Sprites/Buttons/{slug}-target.png` → `Assets/Sprites/{slug}-target.png` → `AssetDatabase.FindAssets` recursive scan under `Assets/Sprites` (handles sibling subfolders like `Commercial/`). Re-bake 2026-05-01: 11/11 icons spawned with non-null sprite refs verified via `prefab_inspect Assets/UI/Prefabs/Generated/toolbar.prefab`. Decision: keep folder layout as-is (artist-authored) — resolver tolerates dispersion.

#### Mandate for next stages

All future panel + toolbar + hud-bar work in stages following Stage 12 must consume `Assets/Sprites/Buttons/*.png` + `Assets/Sprites/*.png` via IR `iconSpriteSlug`. claude-design output is **theme + layout only**; iconography stays human-authored. Encode in `ia/specs/ui-design-system.md` at Stage 12 closeout.

### Step 16.E — D3.3 conformance kind (post-QA)

**Action.** Add `illuminated_button_wiring` check in `AgentBridgeCommandRunner.Conformance.cs`. Error when `IlluminatedButtonRenderer._halo == null` or `_body == null`. Defer until QA confirms hover convergence.

### Step 16.G — HUD-bar redesign (ACTIVE 2026-05-01, reordered ahead of 16.F)

#### Trigger

Play Mode QA on toolbar (post 16.D bake) surfaced layout regressions plus HUD/toolbar widget-bucket drift:

- Toolbar slot 9 currently maps to `Pause-button-1-64` — Pause belongs to HUD speed cluster, not toolbar.
- Toolbar slot 8 currently maps to `Stats-button-64` — Stats toggle belongs to HUD right cluster (toggles `city-stats` panel).
- HUD-bar IR carries 11 widgets (3 left + 7 right + 1 readout) but no center label, no AUTO button, no Zoom +/- pair, no Mini-Map toggle, no Budget readout — gap vs reference layout.

Reordering: HUD-bar redesign moves into Stage 12 ahead of `16.F` closeout (originally implicit in Stage 13 popup-stack gate work). Rationale — HUD-bar is the persistent top strip; without it the persistent UI surface is incomplete and 16.F closeout gate is meaningless.

#### Findings

1. **Toolbar misaligned to product domain.** Pause + Stats are global game-state controls, not build-mode tools. They were placed in toolbar IR by claude-design auto-mapping, never reviewed.
2. **HUD-bar IR has 3 slots (left / center / right) but center is empty.** Reference layout puts `City Name` label dead-center.
3. **`HudBarDataAdapter` declares 5-element `_speedButtons[]`** (paused / 0.5x / 1x / 2x / 4x) — IR right-slot only emits 5 illuminated-buttons; the additional toggles (AUTO, Zoom +/-, Stats, Mini-Map) need separate widget bindings, not the speed-array.
4. **Sprite inventory gaps.** `Assets/Sprites/Buttons/` has Save/Load/New/Pause/Speed1-4/Stats/Zoom-in/Zoom-out **target+pressed** PNG pairs. **No** dedicated AUTO sprite (procedural red-circle button — caption-only, palette-tinted). **No** Mini-Map sprite (caption-only on Long-button-256-64 base, or empty-fill).
5. **Budget readout already supported.** `SegmentedReadout` widget bound via `HudBarDataAdapter._moneyReadout`; needs IR slug `budget-readout` in right slot.
6. **City Name label.** New consumer ref needed in `HudBarDataAdapter` (`_cityNameLabel : ThemedLabel`); IR center slot needs `themed-label` child with `text="City Name"` placeholder.

#### Decision

A. **Rebucket toolbar:** drop Pause (slot 9) + Stats (slot 8) from toolbar IR. Toolbar becomes 9-slot (3 zoning + road + terrain + 2 building + forest + bulldoze). 2-col grid → 5 rows (last row half-full) which is acceptable; or backfill with an additional zoning/density variant later. Decision: keep at 9, accept asymmetric last row for Stage 12.

B. **HUD-bar IR redesign — 3-slot layout:**

| Slot | Children | Notes |
|---|---|---|
| left | `New`, `Save`, `Load` (illuminated-button) | iconSpriteSlug = `New-game-button` / `Save-game-button` / `Load-game-button` |
| center | `city-name-label` (themed-label) | text placeholder `"City Name"`, populated by `HudBarDataAdapter._cityNameLabel` |
| right | `AUTO`, `Zoom-in`, `Zoom-out`, `Stats`, `Mini-Map`, `budget-readout`, `Pause`, `Speed-1`, `Speed-2`, `Speed-3`, `Speed-4` | mixed: 6 illuminated + 1 segmented-readout + 4 speed-illuminated; speed buttons bound to `_speedButtons[]` 5-array starting at Pause |

C. **Sprite mapping (HUD slots):**

| HUD slot | iconSpriteSlug | Source PNG |
|---|---|---|
| New | `New-game-button` | `Assets/Sprites/Buttons/New-game-button-target.png` |
| Save | `Save-game-button` | `Assets/Sprites/Buttons/Save-game-button-target.png` |
| Load | `Load-game-button` | `Assets/Sprites/Buttons/Load-game-button-target.png` |
| AUTO | _(empty — procedural red-circle, caption "AUTO")_ | n/a |
| Zoom-in | `Zoom-in-button-1-64` | `Assets/Sprites/Buttons/Zoom-in-button-1-64-target.png` |
| Zoom-out | `Zoom-out-button-1-64` | `Assets/Sprites/Buttons/Zoom-out-button-1-64-target.png` |
| Stats | `Stats-button-64` | `Assets/Sprites/Buttons/Stats-button-64-target.png` |
| Mini-Map | _(empty — caption-only "Mini Map")_ | n/a |
| Pause | `Pause-button-1-64` | `Assets/Sprites/Buttons/Pause-button-1-64-target.png` |
| Speed-1..4 | `Speed-{N}-button-1-64` | `Assets/Sprites/Buttons/Speed-{N}-button-1-64-target.png` |

D. **Adapter wiring updates** (`HudBarDataAdapter.cs`):

- Add `[SerializeField] private ThemedLabel _cityNameLabel;` — populated from `_cityStats.cityName` (read-only — once at Awake or per-Update if rename UI exists).
- Add `[SerializeField] private IlluminatedButton _autoButton;` — toggles auto-budget mode (binding TBD; placeholder click-handler logs).
- Add `[SerializeField] private IlluminatedButton _zoomInButton; _zoomOutButton; _statsButton; _miniMapButton;` — wire onClick to existing UIManager handlers (`ZoomIn`, `ZoomOut`, `OpenPopup(CityStatsScreen)`, `OpenPopup(MiniMapPopup)`).
- Reuse existing `_moneyReadout` for budget channel (already wired).
- Keep `_speedButtons[]` 5-array, but change semantics: index 0 = Pause, 1..4 = Speed-1..4 (was 0.5x/1x/2x/4x; check `TimeManager.CurrentTimeSpeedIndex` enum to confirm mapping — if it doesn't match, refactor `TimeManager` indices in a follow-up).

#### Implementation steps

1. Edit `web/design-refs/step-1-game-ui/ir.json` — `panels[hud-bar]` slot definitions per (B) above; add `detail.iconSpriteSlug` per illuminated-button per (C).
2. Edit `panels[toolbar]` — drop slot 8 (Stats) + slot 9 (Pause); reassign remaining 9 buttons. Update doc table at line 129-141 to match.
3. Add `ThemedLabel`-spawn support to `UiBakeHandler.Archetype.cs` slot composer if not already there (likely landed for info-panel D2.4). Verify.
4. Update `HudBarDataAdapter.cs` per (D).
5. Re-bake both prefabs via bridge `bake_ui` for `hud-bar` + `toolbar`.
6. `MainScene.unity` — refresh prefab instance overrides if any anchor drift.
7. Play Mode QA gate (16.C) — visual parity vs reference image, speed buttons follow `TimeManager.CurrentTimeSpeedIndex`, money readout shows `cityStats.money`, Stats/MiniMap toggles open respective popups.
8. Hand off to 16.E (conformance) + 16.F (closeout).

#### Bake-result observations (2026-05-01 post-bake QA)

Screenshot: `tools/reports/bridge-screenshots/stage12-step16g-hud-bar-redesign-bake-20260501-230156.png`

Visual confirmation:
- Left cluster: 3 illuminated-buttons render with `New-game-button` + `Save-game-button` + `Load-game-button` icon sprites — all GREEN illuminated.
- Center cluster: `themed-label` renders fallback "City Name" placeholder (TMP white, ramp-light fallback). Adapter binds `_cityStats.cityName` post-wire.
- Right cluster: 11 widgets render — Zoom-in (+), Zoom-out (-), Stats (graph), [budget readout dark bar], Pause (||), Speed-1 (▶), Speed-2 (▶▶), Speed-3 (▶▶▶), Speed-4 (▶▶▶▶) icons visible. AUTO + MAP slots render empty bodies (no caption) — known limitation; `IlluminatedButtonDetail` lacks label/caption field.
- Toolbar: 9-cell 2-col grid (8 tool-grid + 1 subtype-row Bulldoze) renders left-docked between HUD top + building-selector reserve.

Post-bake required wire-up (manual Editor session — not attempted via bridge):
1. Attach `HudBarDataAdapter` MonoBehaviour to `hud-bar` prefab root in `MainScene.unity`.
2. Bind 12 SerializeField refs: `_cityStats`, `_economyManager`, `_timeManager`, `_autoZoningManager`, `_autoRoadBuilder`, `_uiManager`, `_cameraController`, `_uiTheme`, plus widget refs `_moneyReadout`, `_cityNameLabel`, `_newButton`/`_saveButton`/`_loadButton`/`_autoButton`/`_zoomInButton`/`_zoomOutButton`/`_statsButton`/`_miniMapButton`, `_speedButtons[5]`.
3. Bind toggle roots: `_cityStatsRoot` → existing `city-stats` panel; `_miniMapRoot` → existing fixed `MiniMapPanel` transform.
4. UIManager Inspector: ensure `saveLoadScreenRoot` SerializeField points at the `save-load.prefab` instance for `OpenPopup(SaveLoadScreen)` to activate.

Follow-ups deferred to Stage 13:
- IlluminatedButton caption/label support — add `labelText` field to `IlluminatedButtonDetail` + render via ThemedLabel sibling (AUTO + MAP currently render as empty body).
- `Stats` button currently SetActive-toggles `_cityStatsRoot` (no PopupStack registration). Promote to `OpenPopup(CityStatsScreen)` once `PopupType.CityStatsScreen` enum + `cityStatsScreenRoot` SerializeField land in `UIManager.PopupStack.cs`.
- Pause button onClick wiring — currently no onClick handler attached (needs `_pauseButton` SerializeField + handler that calls `OpenPopup(PauseMenu)` OR drives `TimeManager.CurrentTimeSpeedIndex = 0`).

#### Resolved decisions (2026-05-01 user poll)

- **AUTO button** = single toggle for **both** auto-zoning + auto-road-building. Click flips `AutoZoningManager.enabled` + `AutoRoadBuilder.enabled` together. Illumination ON when both enabled, OFF otherwise. (Existing managers run unconditionally → patch to `enabled`-gated. Add `HudBarDataAdapter._autoZoningManager` + `_autoRoadBuilder` refs.)
- **Mini-Map button** = opens existing `MiniMapPanel` (fixed bottom-right, current behavior). Movable deferred to a later stage.
- **Speed buttons** = keep TimeManager array `[0, 0.5x, 1x, 2x, 4x]` as-is (5 indices). HUD sprite labels: `Pause / Speed-1 / Speed-2 / Speed-3 / Speed-4` (sprite slugs already align). No 3x slot — reference image labelling was approximate.
- **City Name** = read-only label bound to `CityStats.cityName`. No rename UI for now.
- **Save** = direct call to current save handler (writes current game to existing slot, no dialog).
- **Load** = opens `save-load-screen` popup in load mode.
- **New** = direct call to current new-game handler (creates new game, no dialog).
- **Stats** = `OpenPopup(CityStatsScreen)` — toggles existing `city-stats` modal.

### Step 16.H — HUD + toolbar scene wiring + click-pipeline gap (DONE 2026-05-02)

#### Trigger

Post-16.G bake landed both prefabs; SerializeField slots remained `fileID:0` on `MainScene.unity`. Pre-compaction directive: agent owns end-to-end wiring via Unity bridge MCP — no human checklist handoff. Per `ia/rules/unity-scene-wiring.md` cabinet-gap protocol — propose new bridge kind before escalating.

#### Bridge cabinet audit (vs 22 SerializeFields × 2 adapters)

- 21/22 fields covered by existing `assign_serialized_field` value_kinds (`asset_ref` / `component_ref` / `object_ref`). No new value_kind needed.
- 1 array-shape field `_speedButtons : IlluminatedButton[]` initially flagged as gap — mitigation found: SerializedProperty path syntax `_speedButtons.Array.size` + `_speedButtons.Array.data[i]` reaches array elements via existing `field_name` plumbing. Validated against ToolbarDataAdapter's 4 array fields (`_zoningButtons`, `_roadButtons`, `_buildingButtons`, `_forestButtons`) — all writes returned `ok=true`. **No new bridge kind needed.**
- IR auto-suffix collision: bake handler appends `(N)` to duplicate-name siblings → button slot lookup must use `iconSpriteSlug` to disambiguate, not name.

#### Findings

1. **Click pipeline gap on `IlluminatedButtonRenderer`.** Renderer implemented `IPointerEnter/Exit/Down/Up` for hover + press visuals but **never** invoked `_button.OnClick`. Result: caption (AUTO/MAP) + illumination rendered correctly but no button on either bar dispatched. Fix: added `IPointerClickHandler` + `OnPointerClick(PointerEventData)` that filters left-click + invokes `_button.OnClick.Invoke()`. Single edit unlocks every illuminated-button instance project-wide. **Pattern:** Selectable hover/press handlers do not imply click dispatch — needs explicit `IPointerClickHandler`.
2. **Speed-button clicks unwired in adapter.** `HudBarDataAdapter` had illumination mirror (`TimeManager.CurrentTimeSpeedIndex` → `IlluminationAlpha`) but no inverse channel — clicks never hit `TimeManager.SetTimeSpeedIndex(i)`. Fix: `WireSpeedButtonClicks()` iterates `_speedButtons[]` with captured index closure.
3. **Toolbar slot count mismatch.** `ToolbarDataAdapter` declares 18 button slots (10 zoning + 1 road + 1 terrain + 2 building + 3 forest + 1 bulldoze); current toolbar bake renders only 9 buttons. Resolved by sparse-fill: zoning slots 0/3/6/9 = R/C/I/Zone-S placeholder, road slot 0, building slots 0/1, forest slot 0, bulldoze singleton. Adapter null-tolerance keeps unused slots unwired. Terrain array intentionally `size=0` (no terrain button in current IR).
4. **Slot-4 Zone S placeholder (human decision).** Bake emitted duplicate Bulldoze-button-64 icons at slot (4) and slot (8). User clarification: slot (4) = Zone S (intended icon missing — placeholder caption acceptable, route to `_zoningButtons[9]`), slot (8) = real bulldoze. Future: replace placeholder when Zone S art lands.

#### Bridge writes (14 successful, all `ok=true`)

| Field | Kind | Target |
|---|---|---|
| `HudBarDataAdapter` (12 fields) | mixed | already wired Step 16.G post-bake |
| `ToolbarDataAdapter._uiManager` | component_ref | `Game Managers/UIManager` |
| `ToolbarDataAdapter._uiTheme` | asset_ref | `Assets/UI/Theme/DefaultUiTheme.asset` |
| `_zoningButtons.Array.size` / `_roadButtons` / `_buildingButtons` / `_forestButtons` | int | 10 / 1 / 2 / 1 |
| `_zoningButtons.Array.data[0,3,6,9]` | component_ref | `illuminated-button` / `(1)` / `(2)` / `(4)` |
| `_roadButtons.Array.data[0]` | component_ref | `illuminated-button (3)` |
| `_buildingButtons.Array.data[0,1]` | component_ref | `illuminated-button (5)` / `(6)` |
| `_forestButtons.Array.data[0]` | component_ref | `illuminated-button (7)` |
| `_bulldozeButton` | component_ref | `illuminated-button (8)` |
| `save_scene` + `unity_compile` gate | — | `compilation_failed: false` |

#### QA outcome

User QA: ACCEPTED. HUD-bar + toolbar buttons all dispatch on click. Captions (AUTO/MAP) render correctly. Speed cluster + AUTO toggle illumination mirrors manager state.

#### Carry-forward

- Zone S art replacement — drop placeholder once sprite lands.
- Terrain button — IR addition + bake re-run if/when terrain tools surface.
- Stage 13 popup-stack gate — Stats/MiniMap currently raw `SetActive`; promote to `OpenPopup` registration once enum + roots land in `UIManager.PopupStack.cs` (prior 16.G follow-up; not regressed by 16.H).

### Step 16.I — post-16.H bug triage (DONE 2026-05-02)

#### User-reported bugs after 16.H QA acceptance

1. **MAP button no-op.** Click did not toggle MiniMapPanel.
2. **AUTO button verification blocked.** User couldn't confirm AUTO mode wiring because budget panel inaccessible (bug #3).
3. **Budget toggler missing.** Pre-16.G hud-bar had a button doubling as money readout + budget panel opener; post-16.G IR carries only `segmented-readout` (display-only widget). User unable to assign auto-mode budget %.

#### Findings

- **Two-canvas scene structure.** Bake placed hud-bar under new `UI Canvas` root; legacy MiniMapPanel + ControlPanel live under `UI/City/Canvas`. `find_gameobject UI Canvas/MiniMapPanel` returns `exists:false`. Correct path = `UI/City/Canvas/MiniMapPanel`.
- **MiniMapController location.** Component lives ON the MiniMapPanel GO itself (not a sibling under Game Managers).
- **HudBarDataAdapter `_miniMapRoot` was `fileID:0`.** Step 16.H wired `_cityStatsRoot` + speed buttons but missed mini-map root.
- **No budget toggle in IR.** Restoration requires runtime click handler on existing money readout — IR change deferred (out of scope for triage).
- **Raycast surface gap on `segmented-readout`.** GO has `RectTransform + SegmentedReadout + SegmentedReadoutRenderer` only — no Graphic. IPointerClickHandler on a child without Graphic doesn't catch clicks.

#### Fixes

| Bug | Fix |
|---|---|
| Mini-map | Bridge `assign_serialized_field` → `HudBarDataAdapter._miniMapRoot = UI/City/Canvas/MiniMapPanel` (object_ref). |
| Budget toggle | New `Assets/Scripts/UI/HUD/MoneyReadoutBudgetToggle.cs` — inherits `Graphic` (invisible raycast surface, `OnPopulateMesh` clears verts) + `IPointerClickHandler` → `UIManager.OpenBudgetPanel()`. Bridge `attach_component` to `UI Canvas/hud-bar/segmented-readout`. |
| AUTO verify | Unblocked by budget toggle fix; deferred to user QA. |

#### Bridge writes

| Op | Target | Result |
|---|---|---|
| `assign_serialized_field` | `_miniMapRoot` (object_ref) | ok |
| `attach_component` | `MoneyReadoutBudgetToggle` on segmented-readout | ok |
| `save_scene` | MainScene | ok |
| `unity_compile` | — | `compilation_failed: false` |

#### Carry-forward

- Stage 13 popup-stack gate — MiniMapPanel toggle still raw `SetActive` (HudBarDataAdapter.HandleMiniMapClick); promote to `OpenPopup(PopupType.MiniMap)` once enum + handler lands.
- Money readout click handler is runtime-only (not in IR). If hud-bar IR rebakes, MoneyReadoutBudgetToggle must be re-attached — same applies to mini-map root wire.
- IR tracking: consider adding `clickAction: open-budget` field to readout panel def so bake handler attaches the toggle automatically.

#### Follow-up — CanvasRenderer requirement (DONE same-session)

Play Mode threw `MissingComponentException: CanvasRenderer ... segmented-readout`. `Graphic.RequireComponent(typeof(CanvasRenderer))` only fires at Editor-time attach via menu — bridge `attach_component` skipped the auto-add. Fix: bridge `attach_component CanvasRenderer` on `UI Canvas/hud-bar/segmented-readout` + `save_scene`. **Pattern:** when bridge-attaching a `Graphic` subclass, explicitly attach `CanvasRenderer` first.

#### Follow-up — city-stats panel wiring (DONE 2026-05-02)

`_cityStatsRoot` was `fileID: 0` → STATS button click no-op. Target: `UI Canvas/city-stats-handoff` prefab instance (`a153ec8cf629842019a03b5263e0f11a`). Default `m_IsActive: 0`.

**Bridge cabinet gap surfaced.** `find_gameobject` / `set_gameobject_active` / `assign_serialized_field value_kind=object_ref` all `target_not_found` when GO inactive — handlers use `GameObject.Find()`, which skips inactive. Hidden-by-default popups can't be referenced by path through the bridge.

**Workaround.** Scene yaml text-edit. Located stripped GameObject via `m_CorrespondingSourceObject` source-GUID match → fileID `427734555`. Set `_cityStatsRoot: {fileID: 427734555}` direct. `open_scene` (reload from disk) + `save_scene` round-trip confirmed persistence.

**Carry-forward.** File TECH issue: extend `find_gameobject` (and mutation kinds that resolve `value_object_path` / `target_path`) with `include_inactive` flag using `Resources.FindObjectsOfTypeAll<Transform>()` for inactive subtree walk. Until then, text-edit is only path for hidden-by-default panel wiring.

### Step 16.F — close out Stage 12

**Action.** Once Steps 16.A–E + 16.G green, `/ship-stage` Pass B closeout. Stage 13 popup-stack gate work follows separately.

## Deferred to Stage 13 (popup-stack gate)

- `OpenPopup`-gated activation for Screen/Modal kinds — currently bypassed via SetActive(false) workaround.
- Legacy `DataPanelButtons` / `ControlPanel` / `MiniMapPanel` / `GridCoordinatesText` retirement — replaced by new design system but never deleted from scene.
- Live cell-data binding (Step 16 D2.3 placeholder `"--"` → real population/jobs/demand).

## Learnings — to migrate to /docs IA system at iteration close

When Stage 12 ships, the patterns below are extractable as permanent IA references. **None of them are stage-specific** — they apply to every future bake-handler / IR-driven UI flow.

### L1 — Bake-handler component-graph completeness checklist

Every prefab emitted by `UiBakeHandler*.cs` must satisfy:

1. Adapter MB attached at authoring time — never rely on runtime `transform.Find`.
2. All `[SerializeField]` ref slots populated from IR — never `fileID: 0`.
3. Slug fields (`_slug`) written from IR per widget — name-based lookup is fragile.
4. Layout-shape fields (`_kind`, anchors, pivot) written from IR `kind`, not defaulted.
5. Theme-relevant Image refs (`_borderTop`, `_halo`, `_body`, etc.) written, not Awake-resolved.

**Migration target.** New section in `ia/specs/architecture/interchange.md` — "Bake-handler completeness invariants" — or new spec `ia/specs/ui-bake-handler.md`.

### L2 — IR schema — `archetype` vs `kind` are independent axes

- `archetype` = which component family (e.g. `info-panel`, `toolbar`, `hud-bar`).
- `kind` = layout taxonomy (`modal`, `screen`, `hud`, `toolbar`).
- Default-Modal-when-missing is a footgun: silently breaks layout.
- Authoring tools (`transcribe:cd-game-ui`) must emit both fields.

**Migration target.** Glossary row + `web/design-refs/step-1-game-ui/README.md` IR schema section + bake-handler reference spec.

### L3 — Palette ramp index — minimum brightness delta rule

When picking ramp indices for adjacent surfaces (panel-fill vs border, body vs halo, etc.), enforce minimum brightness delta. `ramp[1]` vs `ramp[2]` was ~9 units → invisible. `ramp[1]` vs `ramp[4]` works. Encode delta requirement in `ThemedPanel`/`ThemedPrimitiveBase` ramp-index helpers.

**Migration target.** `ia/specs/ui-theme-system.md` (or equivalent) — "Ramp adjacency rules" subsection.

### L4 — Scene state vs disk YAML — bridge mutation precedence

Direct YAML edits to `*.unity` while Unity holds the scene in memory **do not take effect**. Must use bridge `set_gameobject_active` / `attach_component` / etc. (operates on in-memory state) + `save_scene` (persists). Hand-editing PrefabInstance `m_Modifications` is acceptable only when the scene is closed. Document in agent verification policy.

**Migration target.** `docs/agent-led-verification-policy.md` — "Scene mutation precedence" callout. Possibly `ia/rules/unity-invariants.md` numbered invariant.

### L5 — Stage 13 popup-stack gate prerequisites

Screen-kind panels auto-anchor full-stretch and stack on scene load → must be SetActive(false) until `OpenPopup` triggers. Current workaround = bridge mutation per panel; permanent solution = bake-handler emits `m_IsActive: 0` for all `Screen`/`Modal` kinds + `UIManager.OpenPopup` flips active.

**Migration target.** Stage 13 master plan + `ia/specs/architecture/data-flows.md` — UI activation lifecycle.

### L6 — Bridge mutation kinds catalog (operational maturity)

Stage 12 surfaced gaps in bridge tooling: `set_panel_visible` is an obvious add (currently only `set_gameobject_active` by full path). `find_gameobject` with PrefabInstance children sometimes fails — needs investigation. `prefab_inspect` returns >250k chars hitting tool-result limit — needs filtered mode.

**Migration target.** `tools/mcp-ia-server/src/index.ts` enhancement backlog + `docs/mcp-ia-server.md` operational notes.

## Prior step history (compact, 2026-04-28 → 2026-05-01)

| Step | Date | Outcome |
|---|---|---|
| 1 | 2026-04-28 | Trigger-path trace instrumentation `[Stage12-trace]` confirmed `OpenPopup` flow green. |
| 2 | 2026-04-28 | Layer A — `ThemedLabel._tmpText` bake-handler wiring patch landed. |
| 3 | 2026-04-28 | Layer B — `ThemedTabBar._tabStripImage` bake-handler wiring patch landed. |
| 4 | 2026-04-28 | Layer D — MainMenu Options button rewired to `UIManager.OpenPopup(SettingsScreen)`. |
| 5 | 2026-04-28 | PlayMode trigger-path verification — Esc/Alt-click/MainMenu/Pause all reach `OpenPopup`. |
| 6 | 2026-04-29 | Trace instrumentation strip — clean diff. |
| 7 | 2026-04-29 | Post-fix runtime regression — Layer C (DataAdapter slots) flagged. |
| 8 | 2026-04-30 | Architecture-alignment audit — 5×`*DataAdapter` × 6 SerializeField slots = 30 `fileID:0` confirmed. |
| 9 | 2026-04-30 | Post-Step-8 regression — Stage 8 prefab carcass identified. |
| 10 | 2026-04-30 | Modal sizing + layout + z-order + ramp index — `SetAsLastSibling` + `ApplyKindLayout` + ramp[1] panel-fill. |
| 11 | 2026-04-30 | Scene-override regression + layout-kind taxonomy `PanelKind` enum landed. |
| 12 | 2026-04-30 | Caption labels for themed-buttons. |
| 13 | 2026-04-30 | Palette contrast + click wiring + sound parity. |
| 14 | 2026-04-30 | Bridge tools planning — `set_gameobject_active`, `find_gameobject`, `prefab_inspect`, `ui_tree_walk`, `bake_ui_from_ir` shipped. |
| 15 | 2026-04-30 | UI fidelity iteration on info-panel + targeted menus — D1 ramp-index border fix `f0921088`. |
| 16 | 2026-05-01 | D2 + D3 + D4 diagnosis + landed; viewport-blocker triage; IR `kind` backfill (14 panels). |
| 17 | 2026-05-02 | Step 16.H — HUD + toolbar scene wiring via bridge (14 writes); IPointerClickHandler gap fix; speed-button click dispatch; Zone S placeholder slot; QA accepted. |

(Detailed step prose is in branch git history — `git log --grep='stage-12'` for verbatim commit messages.)
