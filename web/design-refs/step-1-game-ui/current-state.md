# Current state — Step 1 Game UI distillation

Co-authored doc per peer-loop §Phase 6. Wraps T1.1 inventory + T1.2 screenshots into per-element rows for T1.4 CD context bundle prep.

- §Inventory — agent-seeded structural data from `UiTheme.cs` SO + `Assets/UI/Prefabs/` + HUD scripts (TECH-2285).
- §Per-element rows — co-authored per-element table targeting CD partner (TECH-2287; awaits user `tag` column signoff).
- §Tag summary — keep / evolve / drop counts (TECH-2287).

> **Status:** §Inventory landed. §Per-element rows + §Tag summary signed off (27 rows: 0 keep / 25 evolve / 2 drop). Locked direction: audio-rack / studio-console aesthetic. Ready for T1.4 CD context bundle prep.

---

## §Inventory

Agent-extracted structural data — read-only pass over `UiTheme.cs` SO fields, `Assets/UI/Prefabs/`, and HUD wiring scripts. Cross-referenced with `docs/ui-polish-exploration.md` Bucket 6 polish notes (RETIRED — superseded by `docs/game-ui-design-system-exploration.md`).

### UiTheme SO fields

Source: `Assets/Scripts/Managers/GameManagers/UiTheme.cs`. Single asset instance: `Assets/UI/Theme/DefaultUiTheme.asset` (consumed by `UIManager.hudUiTheme` + `MainMenuController.menuTheme` per polish-exploration §Architecture).

| Field | Type | Default | Consumer | Notes |
|---|---|---|---|---|
| `primaryButtonColor` | Color | `(0.157, 0.173, 0.208, 1)` navy | `UIManager.Theme` button repaint path | dark-first; same hex as `menuButtonColor` + `surfaceElevated` — drift risk flagged by polish-exploration §Problem |
| `primaryButtonTextColor` | Color | `(0.91, 0.918, 0.941, 1)` near-white | `UIManager.Theme` | reused as `textPrimary` — single bright text token |
| `primaryButtonFontSize` | int | `18` | `UIManager.Theme` | matches `menuButtonFontSize` + `fontSizeHeading` — duplicate scale |
| `menuButtonColor` | Color | `(0.157, 0.173, 0.208, 1)` navy | `MainMenuController.menuTheme` | duplicate of `primaryButtonColor` |
| `menuButtonTextColor` | Color | `(0.91, 0.918, 0.941, 1)` near-white | `MainMenuController.menuTheme` | duplicate of `primaryButtonTextColor` |
| `menuButtonFontSize` | int | `18` | `MainMenuController.menuTheme` | duplicate of `primaryButtonFontSize` |
| `surfaceBase` | Color | `(0.0667, 0.0745, 0.0941, 1)` deep navy | `UIManager.Hud` chrome paint | fullscreen tint base |
| `surfaceCardHud` | Color | `(0.11, 0.122, 0.149, 0.88)` | `UIManager.Hud` HUD card paint | alpha 0.88 lets map bleed-through |
| `surfaceToolbar` | Color | `(0.0667, 0.0745, 0.0941, 0.94)` | `UIManager.ToolbarChrome` | slightly more opaque than HUD cards |
| `surfaceElevated` | Color | `(0.157, 0.173, 0.208, 1)` navy | `UIManager.Theme` (active tool / tooltip / dropdown) | duplicate of `primaryButtonColor` — token aliasing missing |
| `borderSubtle` | Color | `(0.18, 0.2, 0.251, 1)` | `UIManager.Theme` divider paint | 1 px panel edges |
| `textPrimary` | Color | `(0.91, 0.918, 0.941, 1)` | `UIManager.Theme` text paint | duplicate of `primaryButtonTextColor` |
| `textSecondary` | Color | `(0.545, 0.561, 0.643, 1)` muted slate | `UIManager.Theme` secondary text | demoted readout text |
| `accentPrimary` | Color | `(0.29, 0.62, 1, 1)` cyan-blue | `UIManager.Hud` highlight | demand bar fill, hover state |
| `accentPositive` | Color | `(0.204, 0.78, 0.349, 1)` green | toolbar building selector outline (per `toolbar-building-selector.png`) | also CityStats label icon silhouette |
| `accentNegative` | Color | `(1, 0.271, 0.227, 1)` red | top-bar AUTO toggle, deselect X (per `full-scene.png`) | warning state |
| `modalDimmerColor` | Color | `(0, 0, 0, 0.667)` | `UIManager.WelcomeBriefing` (Options / save dialogs) | fullscreen dim behind modals |
| `fontSizeDisplay` | int | `28` | `UIManager.Hud` (city name / large readouts) | display tier |
| `fontSizeHeading` | int | `18` | section titles (CITY STATISTICS, Options) | duplicate of `primaryButtonFontSize` |
| `fontSizeBody` | int | `14` | row labels, info-panel body | body tier |
| `fontSizeCaption` | int | `11` | secondary readouts, cursor-cell debug overlay | caption tier |
| `spacingUnit` | int | `4` | layout reference (px multiples) | base spacing atom |
| `panelPadding` | int | `16` | inner panel inset | 4× spacing unit |

### UI prefabs in scope

Source: `Assets/UI/Prefabs/*.prefab`. 10-surface scope = `HUD / info-panel / pause / settings / save-load / new-game / tooltip / toolbar / city-stats / onboarding`. **Most surfaces are scene-baked into `MainScene.unity` / `MainMenu.unity`, not prefab-instantiated** — only 4 generic shells under `Assets/UI/Prefabs/`. Scene-baked surfaces flagged below.

| Prefab / surface | Chrome | Slot count | Control archetype hint | Consumer | Notes |
|---|---|---|---|---|---|
| `UI_ModalShell.prefab` | rounded panel + Title slot anchored top + body slot fill | 2 (Title + body) | container | reused by Options modal (`settings.png`), pause (not present), save-load (not present) | thin generic shell; styling lives in scene-baked Options instance |
| `UI_ScrollListShell.prefab` | rectangular panel + viewport + content frame | 1 (scrolled list body) | scrollable list container | reused by `city-stats.png` row list, `info-panel-subtype.png` subtype tile row | 320×400 default size; vertical scroll |
| `UI_ToolButton.prefab` | square icon tile with text label below | 1 (text + icon) | button | reused by toolbar `BuildingSelector` (per `toolbar-building-selector.png`) | green-outline accent on selected state |
| `UI_StatRow.prefab` | horizontal row: icon + key label + value + (optional) demand bar | 4 (Key + value + bar + icon) | readout row | reused by `city-stats.png` 16-row list | Key + Value + horizontal bar slots; demand rows enable inline bar |
| HUD top-bar (scene-baked) | flat bar across top of `MainScene.unity`; left tabs + center city name + right cluster | 8+ (tabs / name / AUTO / zoom / graph / minimap / money / speed) | container + readouts + steppers | `UIManager.Hud` | not a prefab; scene-rooted Canvas tree |
| HUD top-right cluster (scene-baked) | sub-bar inside top-bar | 6 (AUTO toggle / +/− zoom / graph / minimap / money / speed) | toggle + steppers + readouts | `UIManager.Hud` | per `hud-top-right.png` |
| Minimap floating panel (scene-baked) | rectangular panel + top toggle row + minimap render | 6 (5 layer toggles + map view) | toggles + image readout | `UIManager.Hud` | per `minimap.png` (St / Zn / Fr / De / Ct toggles) |
| BuildingSelector floating panel (scene-baked) | dark navy panel + 3×3 icon grid + spillover row + subtype info row | 10+ (per icon tile + subtype tiles) | button grid + container | `UIManager.ToolbarChrome` | per `toolbar-building-selector.png` + `info-panel-subtype.png` |
| CityStats floating panel (scene-baked) | rectangular panel + tab strip + 16-row stat list | 16 stat rows (uses `UI_StatRow.prefab` instances) | readout list + tab bar | `UIManager.Hud` | per `city-stats.png` |
| Budget floating panel (scene-baked) | panel + Growth Budget slider + 4 sub-allocation sliders + 3 tax stepper rows | 8 (1 master slider + 4 sub-sliders + 3 tax steppers) | slider + stepper | tax/budget controller (TBD inventory) | per `budget.png`; only slider+stepper-heavy surface |
| Cursor-cell debug overlay (scene-baked) | thin caption text panel | 1 (text readout) | readout | `UIManager.Hud` debug path | per `full-scene.png` (`Cursor: x: 67 y: 127 chunk:(4,7) ...`) |
| MainMenu screen (scene-baked, separate scene) | flat navy panel + 4-button vertical stack | 4 (Continue / New Game / Load City / Options) | button stack | `MainMenuController` | per `main-menu.png`; lives in `MainMenu.unity` |
| Options modal (scene-baked, MainMenu) | rounded dark panel + Title + SFX volume slider + Mute SFX toggle + Back button | 4 (Title + slider + toggle + button) | slider + toggle + button | `MainMenuController` Options path | per `settings.png`; reuses `UI_ModalShell.prefab` chrome |
| Pause modal | _(not present in build)_ | — | — | — | flagged as scope gap |
| Tooltip primitive | _(not present in build)_ | — | — | — | flagged as scope gap |
| Save / load picker modal | _(not present in build)_ | — | — | — | save routes through MainMenu Continue + top-bar floppy/folder tabs (per `city-scene-overview.png`); no dedicated picker |
| Onboarding flow | _(not present in build)_ | — | — | — | flagged as scope gap |

### Polish-exploration notes folded in

Source: `docs/ui-polish-exploration.md` (RETIRED). Relevant rows folded into surface inventory above:

- §Problem flagged "different panels use different paddings, fonts, palette variants, corner radii" — confirmed by `UiTheme` token duplicates table above (`primaryButtonColor` ≡ `menuButtonColor` ≡ `surfaceElevated`; `primaryButtonFontSize` ≡ `menuButtonFontSize` ≡ `fontSizeHeading`).
- §Problem "no unified HUD contract" — confirmed by HUD scene-baked split: top-bar + minimap + BuildingSelector + CityStats are independent floating panels with no shared anchor or overflow rule (`city-scene-overview.png`).
- §Problem "no notifications / event feed" — confirmed; no notification surface present in build captures.
- §Problem "info panels thin" — partial; budget panel rich (`budget.png`) but tile/building click panel not in capture set (info-panel scope shows only budget + subtype detail row).
- BUG-14 (per-frame `FindObjectOfType`) + BUG-48 (minimap stale after load) + TECH-72 (HUD/uGUI scene hygiene) — backlog refs; NOT touched by Stage 1 (doc-only; deferred to later stages).

---

## §Per-element rows

> **Status — SIGNED OFF.** All `evolve-toward` + `tag` columns filled per user round-trip (chrome + controls + feedback signed off A). Locked direction: audio-rack / studio-console vocabulary feeds T1.4 CD context bundle.

Table groups: chrome (HUD / panels / modals) → controls (buttons / toggles / sliders / inputs / steppers) → feedback (readouts / progress / status indicators).

### Chrome group

| element | current style | structural data | screenshot ref | evolve-toward | tag |
|---|---|---|---|---|---|
| Top HUD bar | flat dark navy bar; thin border; `surfaceCardHud` paint | scene-baked Canvas root; left tabs + center city name + right cluster (`hud-top-right.png` shape) | `city-scene-overview.png`, `hud-top-right.png`, `full-scene.png` | studio-rack faceplate chassis: tighter top inset, matte panel paint, thin accent strip under bar, corner rivets/screws marking module boundary | evolve |
| BuildingSelector toolbar panel | floating dark navy panel + 3×3 icon grid + spillover row; green outline on selected; `surfaceToolbar` paint | scene-baked floating Canvas; `UI_ToolButton.prefab` instances per cell | `toolbar-building-selector.png`, `info-panel-subtype.png` | patchbay-module rack: 3×3 cells = rack slots, green outline → lit rack-LED indicator, icons sit in recessed module windows | evolve |
| Minimap floating panel | rectangular panel + 5 layer-toggle row + minimap render; `surfaceCardHud` paint | scene-baked; `St / Zn / Fr / De / Ct` toggles → minimap layer mask | `minimap.png` | CRT/scope readout in rack bezel: layer toggles = depressed rack buttons with lit-state ring, map view inside dark scope window | evolve |
| CityStats floating panel | rectangular panel + tab strip + 16-row stat list; `surfaceCardHud` paint | uses `UI_ScrollListShell.prefab` + `UI_StatRow.prefab` × 16 | `city-stats.png` | channel-strip rack: each row = channel with icon-jack + label + value/meter; demand rows render as inline VU meter strips | evolve |
| Budget floating panel | panel + master Growth Budget slider + 4 sub-allocation sliders + 3 tax stepper rows; `surfaceCardHud` paint | scene-baked; disc-thumb sliders + ◀ ▶ steppers | `budget.png` | mixing-console strip: master Growth Budget = main fader, 4 sub-allocations = bus faders, 3 tax rows = trim-knob steppers below fader bay | evolve |
| Cursor-cell debug readout | thin caption-tier text overlay; `textSecondary` paint at `fontSizeCaption` | scene-baked debug Canvas | `full-scene.png` | debug-only overlay; not part of MVP UI vocabulary; gate behind dev flag | drop |
| Options modal | rounded dark panel + Title + body + Back button; `modalDimmerColor` backdrop | uses `UI_ModalShell.prefab` chrome | `settings.png` | rack-front maintenance panel: bezel border + corner screws, Back button = pull-tab handle, slider in console-fader visual | evolve |
| MainMenu vertical stack | flat navy panel + 4-button stack (Continue / New Game / Load City / Options); `menuButtonColor` paint | scene-baked in `MainMenu.unity` | `main-menu.png` | studio standby/power-on screen: 4 buttons = embossed console power switches with backlit labels; flat navy → matte rack chassis | evolve |

### Controls group

| element | current style | structural data | screenshot ref | evolve-toward | tag |
|---|---|---|---|---|---|
| Tool button (BuildingSelector cell) | square icon tile + text label; green outline on selected | `UI_ToolButton.prefab` (icon + caption-tier label) | `toolbar-building-selector.png` | rack-module slot: recessed icon window, label silkscreened below, lit-LED ring on select state | evolve |
| MainMenu button | full-width rounded rectangle; `menuButtonColor` fill + `menuButtonTextColor` text at `menuButtonFontSize` | scene-baked button row | `main-menu.png` | embossed console power switch: rectangular hard-edge cap, backlit label, depressed-state shadow | evolve |
| AUTO toggle (top-bar right) | red square toggle button; `accentNegative` fill in active state | scene-baked; bool toggle | `hud-top-right.png`, `full-scene.png` | latching rack toggle: red lens cover when active, dark unlit when off, screw-mount surround | evolve |
| +/− zoom steppers | square icon buttons; `surfaceElevated` paint | scene-baked stepper pair | `hud-top-right.png` | rack rocker buttons: square caps with embossed +/− glyph, click-down haptic shadow | evolve |
| Play-speed control row | row of speed icons (pause / play / fast-forward); `surfaceElevated` paint | scene-baked stepper-style row | `hud-top-right.png` | transport-control bay: pause/play/fast-forward = tape-deck transport buttons in shared rack strip | evolve |
| SFX volume slider (Options) | horizontal slider with disc thumb; `accentPrimary` fill | scene-baked slider | `settings.png` | console fader: vertical or horizontal track with hard-edge cap thumb, tick marks on rail | evolve |
| Mute SFX toggle (Options) | checkbox-style toggle | scene-baked toggle | `settings.png` | rack mute switch: square button with lit-LED state ring (lit = muted) | evolve |
| Growth Budget master slider | horizontal slider with disc thumb + right-aligned % readout; `accentPrimary` fill | scene-baked slider with bound readout | `budget.png` | console master fader: thicker rail than sub-faders, prominent cap thumb, % readout in segmented-display style | evolve |
| Sub-allocation slider (Road / Energy / Water / Zoning) | horizontal slider with disc thumb + right-aligned % readout | scene-baked slider × 4 | `budget.png` | bus-channel fader strip: 4 parallel faders sharing rail metric, label silkscreen above | evolve |
| Tax stepper row (R / C / I) | label + ◀ ▶ stepper buttons + value readout | scene-baked stepper row × 3 | `budget.png` | trim-knob row: ◀ ▶ become detent-step rocker, value readout in segmented-display style | evolve |
| Minimap layer toggle (St / Zn / Fr / De / Ct) | small text-label toggle pill row | scene-baked toggle row × 5 | `minimap.png` | rack-pushbutton row: depressed-when-active, lit ring marks layer, label silkscreened on cap | evolve |
| Subtype chip (Medium / Heavy) | diamond chip + label | scene-baked; appears in BuildingSelector subtype row | `info-panel-subtype.png` | mode-select rotary tab: angled chip with detent-click feel, lit when active | evolve |
| City-name selection X | small `accentNegative` X button on selected city tab | scene-baked top-bar control | `full-scene.png` | rack-eject latch: small `accentNegative` lit micro-button, click = eject city tab | evolve |

### Feedback group

| element | current style | structural data | screenshot ref | evolve-toward | tag |
|---|---|---|---|---|---|
| Money readout (`$20,000 (+$0)`) | large heading-tier numeric + delta caption; `textPrimary` + `accentPositive`/`accentNegative` for delta sign | scene-baked top-bar text + delta sub-text | `hud-top-right.png` | segmented LED display panel: large primary digits + small delta digits, green/red lit segments per delta sign | evolve |
| CityStats row (Population / Money / Happiness / Power output / Power consumption / Unemployment / Total jobs / Residential demand / Commercial demand / Industrial demand / Demand feedback / Total jobs created / Available jobs / Jobs taken / Water output / Water consumption) | green silhouette icon + `textPrimary` label + right-aligned `textPrimary` value | `UI_StatRow.prefab`: Key (icon+label) + Value slot | `city-stats.png` | channel-strip row: silkscreened label-jack + value in segmented mini-display, icon = jack-port symbol | evolve |
| Demand inline bar (R / C / I demand rows) | horizontal `accentPrimary` fill bar inside CityStats demand row | `UI_StatRow.prefab` bar slot enabled | `city-stats.png` | inline VU meter: horizontal LED-segment ladder, fills with `accentPrimary` per demand level | evolve |
| Demand feedback row | full-width text row inside CityStats list | `UI_StatRow.prefab` text-only mode | `city-stats.png` | console annunciator strip: dim caption text inside dedicated rack lane below meters | evolve |
| City name display | display-tier text (`fontSizeDisplay`) at top-bar center; `textPrimary` paint | scene-baked top-bar text | `city-scene-overview.png`, `full-scene.png` | nameplate engraving: display-tier text with subtle bevel/etch on rack faceplate band | evolve |
| Cursor-cell readout (`Cursor: x: y: chunk: S: body: CityCell:`) | caption-tier debug text; `textSecondary` paint | scene-baked debug overlay | `full-scene.png` | debug-only overlay; gate behind dev flag (matches chrome row 6 decision) | drop |

---

## §Tag summary

> **Status — SIGNED OFF.** User signed off chrome (8 rows, A) + controls (13 rows, A) + feedback (6 rows, A). Per peer-loop §Phase 6 step 3, tag set = `keep` / `evolve` / `drop`.

| tag | count | rows |
|---|---|---|
| keep | 0 | — |
| evolve | 25 | chrome × 7 (Top HUD bar / BuildingSelector / Minimap / CityStats / Budget / Options modal / MainMenu) + controls × 13 (Tool button / MainMenu button / AUTO toggle / +/− zoom / Play-speed / SFX slider / Mute toggle / Growth Budget master / Sub-allocation × 4 / Tax stepper × 3 / Minimap layer toggle × 5 / Subtype chip / City-name X) + feedback × 5 (Money readout / CityStats row / Demand inline bar / Demand feedback / City name) |
| drop | 2 | Cursor-cell debug readout (chrome) + Cursor-cell readout (feedback) — both debug-only overlay; gate behind dev flag |
| **total** | 27 | 8 chrome + 13 controls + 6 feedback |

**Locked direction:** audio-rack / studio-console aesthetic — patchbay/rack faceplate chrome, console-fader + rack-button controls, segmented-LED + VU-meter feedback. CD partner consumes this vocabulary in TECH-2288 context bundle + TECH-2290 bundle generation.

Test blueprint per TECH-2287 §Test Blueprint:
- `tag_summary_counts`: assert §Tag summary count totals equal §Per-element rows count once user fills tags.
- `per_element_screenshot_coverage`: every PNG under `screenshots/` is referenced by ≥ 1 row in §Per-element rows. **PASS** at draft time — all 10 captures referenced.
- `per_element_uitheme_coverage`: every `UiTheme.cs` field listed in §Inventory is referenced by ≥ 1 row in §Per-element rows. **PASS** at draft time — palette tokens, font-size tiers, spacing tokens all surface in row `current style` columns.

---

## Handoff → T1.4 CD context bundle prep

Once user round-trip on `tag` column closes, this doc feeds:

- **T1.4 — `web/design-refs/step-1-game-ui/cd-context-bundle.md`** §Current-state distillation handoff section: link this doc + `screenshots/README.md` so CD partner has the structural inventory + capture set + keep/evolve/drop triage.
- **T1.5 — `tools/scripts/transcribe-cd-game-ui.ts`** input contract: CD partner output under `web/design-refs/step-1-game-ui/cd-bundle/` references these surface tags + element rows when generating tokens / panels / interactives.

Cross-link: see [`screenshots/README.md`](screenshots/README.md) for capture index, [`docs/game-ui-mvp-authoring-approach-exploration.md`](../../../docs/game-ui-mvp-authoring-approach-exploration.md) §Phase 3 (locked IR shape) and §Phase 6 (peer-loop steps).
