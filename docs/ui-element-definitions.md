# UI element definitions

> **Role.** Annotation + JSON staging surface for the game UI. **DB is bake source of truth** (Unity reads DB rows). This doc is the human-readable layer + the JSON-as-text the seed migration consumes. See `docs/ideas/ui-elements-grilling.md` for the process spec.

> **Status.** Authoring in progress — Phase 0 baseline annotation done; Phase 1 panel definition in progress (`hud-bar` + `toolbar` + `tool-subtype-picker` + `budget-panel` + `stats-panel` + `map-panel` + `info-panel` + `pause-menu` + `notifications-toast` locked 2026-05-07).

---

## Tokens

Named values referenced by panels + components. Seeded from `docs/ideas/ui-elements-grilling.md §4.1` at process lock 2026-05-07.

### Prose summary

| Group | Tokens |
| --- | --- |
| Color — backgrounds | `color.bg.cream` `#f5e6c8` (button body), `color.bg.cream-pressed` `#d9c79c` |
| Color — borders | `color.border.tan` `#a37b3a` |
| Color — icons / text | `color.icon.indigo` `#4a3aff`, `color.text.dark` `#1a1a1a` |
| Color — alerts | `color.alert.red` `#c53030` |
| Sizing | `size.icon` 64, `size.button.tall` 72, `size.button.short` 48, `size.strip.h` 80, `size.panel.card` 320 |
| Spacing | `gap.tight` 4, `gap.default` 8, `gap.loose` 16, `pad.button` `[4,8,4,8]` (TRBL) |
| Layers (z-index) | `z.world` 0, `z.hud` 10, `z.toast` 20, `z.modal` 30, `z.overlay` 40 |

### JSON (seed source)

```json
{
  "tokens": {
    "color.bg.cream":         "#f5e6c8",
    "color.bg.cream-pressed": "#d9c79c",
    "color.border.tan":       "#a37b3a",
    "color.icon.indigo":      "#4a3aff",
    "color.text.dark":        "#1a1a1a",
    "color.alert.red":        "#c53030",

    "size.icon":              64,
    "size.button.tall":       72,
    "size.button.short":      48,
    "size.strip.h":           80,
    "size.panel.card":        320,

    "gap.tight":              4,
    "gap.default":            8,
    "gap.loose":              16,
    "pad.button":             [4, 8, 4, 8],

    "z.world":                0,
    "z.hud":                  10,
    "z.toast":                20,
    "z.modal":                30,
    "z.overlay":              40
  }
}
```

---

## Components

Reusable atoms + molecules. Seeded from `docs/ideas/ui-elements-grilling.md §4.2`.

### Prose summary

| Component | Role | Default props |
| --- | --- | --- |
| `<HudStrip>` | anchored full-width strip with named zones | `side`, `h`, `bg`, zones=`{left,center,right}` |
| `<IconButton>` | icon-only button, optional label | `slug`, `icon`, `size`, `variant`, `hotkey`, `action`, `tooltip` |
| `<Label>` | static or bound text | `slug`, `bind?`, `font`, `align` |
| `<Readout>` | live data text with format + cadence | `slug`, `bind`, `format`, `cadence` |
| `<Toggle>` | on/off bound to setting | `slug`, `bind` |
| `<Modal>` | overlay panel with focus-trap + esc-close | `slug`, `trapFocus`, `closeOnEsc` |

### Variants (state machine per component)

```
IconButton.variants = { default | hover | pressed | disabled | active }
HudStrip.variants   = { idle | dimmed }   // dimmed when modal on top
```

### JSON (seed source)

```json
{
  "components": {
    "HudStrip": {
      "props": {
        "side":  { "type": "enum", "values": ["top","bottom","left","right"], "default": "bottom" },
        "h":     { "type": "size",  "default": "size.strip.h" },
        "bg":    { "type": "color", "default": "color.bg.cream" },
        "zones": { "type": "array", "default": ["left","center","right"] }
      },
      "variants": ["idle", "dimmed"]
    },
    "IconButton": {
      "props": {
        "slug":    { "type": "string", "required": true },
        "icon":    { "type": "string", "required": true },
        "size":    { "type": "enum", "values": ["icon","tall","short"], "default": "icon" },
        "variant": { "type": "string", "default": "amber" },
        "hotkey":  { "type": "string", "default": null },
        "action":  { "type": "string", "required": true },
        "tooltip": { "type": "string", "default": null }
      },
      "variants": ["default","hover","pressed","disabled","active"]
    },
    "Label": {
      "props": {
        "slug":  { "type": "string", "required": true },
        "bind":  { "type": "string", "default": null },
        "font":  { "type": "enum", "values": ["display","body","mono"], "default": "body" },
        "align": { "type": "enum", "values": ["start","center","end"], "default": "center" }
      },
      "variants": []
    },
    "Readout": {
      "props": {
        "slug":    { "type": "string", "required": true },
        "bind":    { "type": "string", "required": true },
        "format":  { "type": "enum", "values": ["text","currency","percent","integer"], "default": "text" },
        "cadence": { "type": "enum", "values": ["frame","tick","event"], "default": "tick" }
      },
      "variants": []
    },
    "Toggle": {
      "props": {
        "slug": { "type": "string", "required": true },
        "bind": { "type": "string", "required": true }
      },
      "variants": ["default","hover","on","disabled"]
    },
    "Modal": {
      "props": {
        "slug":       { "type": "string", "required": true },
        "trapFocus":  { "type": "bool", "default": true },
        "closeOnEsc": { "type": "bool", "default": true }
      },
      "variants": ["closed","opening","open","closing"]
    }
  }
}
```

---

## Primitives

> Component-level surfaces (not panels). Cross-cut all panels. Same lock format as panels (prose meta + JSON definition + wiring contract).

### tooltip

**Role.** Hover-driven hint primitive. Cross-cut concern — every interactive element (button / card / toggle / readout-button) opts in by declaring a `tooltip` field. Sole discoverability channel post-D36 (glossary-panel dropped). Per D28 lock.

**Trigger.** Pointer-rest on element for 500 ms. Pointer-fly-by under threshold = no tooltip. Pointer-leave at any point during the dwell timer cancels. Touch / long-press fallback **deferred post-MVP** (desktop-first).

**Content shape.**
- **Default.** Single line — element name string (e.g. `"Recenter camera"`, `"Bulldoze"`, `"Toggle minimap"`). Sourced from per-element `tooltip` field on the catalog row (button / card / toggle params_json).
- **Override.** When element is in `disabled` variant (or otherwise opted in via `tooltip_override`), the override **replaces** the default tooltip — never appended. Used for "Cannot afford — Need $X / cell", "Coming soon", per-family blocked-state copy.
- **Wrap.** Max width 240 px, wraps to a 2nd line. No 3rd line. Strings exceeding 2 lines are an authoring bug — fail catalog validation.

**Position.** Auto-anchor — default above element, flips below when element is near top edge of viewport. Horizontal align centered on element; clamp left / right to keep tooltip on-screen with 8 px viewport margin.

**Visual.** Cream / paper background (`color.bg.cream`) + dark indigo text (`color.text.indigo`) + 1 px tan border (`color.border.tan`) + 4 px corner radius. Padding 8 px horizontal × 6 px vertical. Body font (not display). 12 px gap between tooltip edge and source element.

**Z-order.** Topmost UI layer (`z.tooltip` — above modals, info-panel, notifications-toast, hud-bar). Nothing should ever obscure a tooltip.

**Animation.** Fade-in ~120 ms on dwell-threshold reach; fade-out ~120 ms on pointer-leave or click. No slide.

**Dismiss paths.**
- Pointer leaves source element → fade out.
- Pointer-down (click) on source element → instant hide (action takes priority).
- Source element variant transitions to `disabled` while hovered → tooltip swaps to override copy without re-fade.

**Pause-time behavior.** Tooltips fire in all sim-states — running, paused, modal-open. Pure UI primitive; sim-state-agnostic.

**Source field.** Each interactive element declares its tooltip explicitly in its catalog row (`params_json.tooltip` / `params_json.tooltip_override`). No auto-generation from slug. No glossary fallback. Missing `tooltip` field on a tooltip-eligible element = **authoring bug** (catalog validator surfaces it).

#### JSON (DB shape — not a panel row, but tooltip field consumers reference this contract)

```json
{
  "primitives": {
    "tooltip": {
      "props": {
        "trigger":      { "type": "enum",   "values": ["hover-dwell"], "default": "hover-dwell" },
        "dwell_ms":     { "type": "number", "default": 500 },
        "position":     { "type": "enum",   "values": ["auto-flip"], "default": "auto-flip" },
        "max_width_px": { "type": "number", "default": 240 },
        "max_lines":    { "type": "number", "default": 2 },
        "fade_ms":      { "type": "number", "default": 120 },
        "z_layer":      { "type": "string", "default": "z.tooltip" }
      },
      "variants": ["hidden", "fading-in", "visible", "fading-out"]
    }
  }
}
```

**Field on consumer rows** (button / card / toggle / readout-button):

```json
{
  "params_json": {
    "tooltip":          "Recenter camera",
    "tooltip_override": null
  }
}
```

#### Wiring contract

```json
{
  "wiring": {
    "bake_requirements": {
      "sprites":    [],
      "tokens":     ["color.bg.cream", "color.text.indigo", "color.border.tan", "z.tooltip", "size.tooltip.padding-x", "size.tooltip.padding-y", "size.tooltip.gap", "size.tooltip.maxwidth"],
      "archetypes": ["tooltip-card"]
    },
    "actions_referenced":  [],
    "binds_referenced":    ["tooltip.text", "tooltip.target", "tooltip.visible"],
    "hotkeys":             [],
    "verification_hooks":  ["bridge.tooltip-state-get"],
    "variant_transitions": [
      {"from": "hidden",      "to": "fading-in",  "trigger": "pointer.rest.500ms"},
      {"from": "fading-in",   "to": "visible",    "trigger": "fade.complete"},
      {"from": "visible",     "to": "fading-out", "trigger": "pointer.leave OR pointer.down"},
      {"from": "fading-out",  "to": "hidden",     "trigger": "fade.complete"},
      {"from": "fading-in",   "to": "fading-out", "trigger": "pointer.leave"},
      {"from": "visible",     "to": "visible",    "trigger": "source.variant.disabled (text swap)"}
    ]
  }
}
```

#### Drift flagged

- **NEW archetype `tooltip-card`** — cream-paper card with tan border + corner radius. Used only by tooltip primitive.
- **NEW tokens** — `z.tooltip` (above all UI), `size.tooltip.padding-x` (8 px), `size.tooltip.padding-y` (6 px), `size.tooltip.gap` (12 px source-to-tooltip), `size.tooltip.maxwidth` (240 px).
- **Bind registry** — 3 new bind paths: `tooltip.text` (string), `tooltip.target` (element ref / null), `tooltip.visible` (bool). Singleton tooltip controller; one tooltip rendered at a time.
- **EXTEND existing `TooltipController`** (`Assets/Scripts/UI/Tooltips/TooltipController.cs`) — already implemented + working. **Preserve as-is:** `Instance` static singleton resolved in `Awake`; `_themeRef` + `_canvasRect` cached per invariants #3 / #4; `HandleEnter(TooltipText, PointerEventData)` spawns prefab as `_canvasRect` child at pointer screen-point (via `RectTransformUtility.ScreenPointToLocalPointInRectangle`); `HandleExit` destroys when current trigger matches; single-instance lifecycle (new enter destroys prior). `TooltipText` marker component on tooltip-eligible elements survives unchanged. **Add (extension):** 500 ms hover-dwell timer (currently spawns instantly on enter), fade 120 ms in / out (currently instant Instantiate / Destroy), position auto-flip (above default → below near top edge), disabled-variant `tooltip_override` text swap path. **Do NOT:** create a second singleton, switch to `IPointerEnterHandler` per-element listener (already routed through `TooltipText`), reparent away from `_canvasRect`.
- **Catalog validator extension** — new rule: every catalog row with `tooltip-eligible: true` (buttons / cards / toggles / readout-buttons) MUST declare `tooltip` field non-empty. Override-only (no default) = bake error.
- **Disabled-variant tooltip swap** — source element transitioning between `default ↔ disabled` while tooltip visible = swap text in place, no fade. Affordability tier on subtype-picker cards re-uses this path.
- **Modal-coexistence** — tooltip layer above modals means modal-internal hover (e.g. budget-panel slider thumbs) can show tooltips. Confirm with budget-panel + stats-panel + pause-menu locks (slider thumbs / save-row buttons may want tooltips).
- **i18n** — tooltip strings are user-facing copy. Per-element `tooltip` field will route through string-table at localization pass. Defer to localization workstream.
- **Motion canon** — 120 ms fade matches notifications-toast (200 ms in / 300 ms out) but is shorter. Tooltips need to feel snappy; toasts feel arrival-y. Keep distinct.
- **Cross-cut audit** — every locked panel declared a tooltip field on at least one child. Catalog audit pass after primitive locks: enumerate every interactive child across hud-bar / toolbar / tool-subtype-picker / budget-panel / stats-panel / map-panel / info-panel / pause-menu / notifications-toast → confirm `tooltip` field populated. Surface gaps as authoring tasks.
- **Touch fallback (post-MVP)** — long-press 500 ms dwell → tooltip show; touch-up → fade out. Same primitive shell, alternate trigger. Track as follow-up.

---

### audio-cues

**Role.** Behavior-freeze registry for UI/UX audio. Cross-cuts every panel. Authoritative source of truth — bake pipeline + UI rebake MUST preserve every emit-site listed below. Missing emit on rebake = regression.

**Two engines, one registry:**

- **`BlipEngine` + `BlipId` enum** (`Assets/Scripts/Audio/Blip/`) — procedural / sampled UI cues, master + SFX bus volumes, PlayerPrefs persistence (`BlipBootstrap.SfxVolumeDbKey` + `BlipMutedKey`). 10 cues registered.
- **`UiSfxPlayer.Play(clip, volume?)`** (`Assets/Scripts/UI/UiSfxPlayer.cs`) — stateless `AudioSource.PlayClipAtPoint` fallback for AudioClip refs that aren't in the Blip catalogue. Used by `SubtypePickerController` (3 clips). No-op on null.

**Cue table (locked).**

| Cue | Engine | Trigger source class | Fire event | Panel host | Preserve verdict |
| --- | --- | --- | --- | --- | --- |
| `BlipId.UiButtonHover` | BlipEngine | `ThemedButton` (auto via PointerEnter) + `MainMenuController` (entry callback) | Pointer enters any `ThemedButton` | every panel with buttons | KEEP — ThemedButton owns auto-emit; rebake MUST route every interactive button through `ThemedButton` to inherit |
| `BlipId.UiButtonClick` | BlipEngine | `ThemedButton` (auto on click) + `MainMenuController` (explicit on Continue / NewGame / Load / Settings / Quit / Back / scenario-toggle) | Click commit | every panel with buttons | KEEP — ThemedButton auto + MainMenuController explicit emits both survive |
| `BlipId.ToolRoadTick` | BlipEngine | `RoadManager` | Per-cell tick during road stroke paint | toolbar (Road tool active) | KEEP — sim-side, not UI; rebake of toolbar MUST NOT bypass `RoadManager` |
| `BlipId.ToolRoadComplete` | BlipEngine | `RoadManager` | Road stroke commit | toolbar (Road tool) | KEEP — same |
| `BlipId.ToolBuildingPlace` | BlipEngine | `BuildingPlacementService` | Successful building placement | toolbar / tool-subtype-picker (any build family) | KEEP — placement-service-owned |
| `BlipId.ToolBuildingDenied` | BlipEngine | `BuildingPlacementService` | Placement rejected (insufficient funds / invalid cell / blocked) | toolbar / tool-subtype-picker | KEEP — denied-feedback critical UX |
| `BlipId.WorldCellSelected` | BlipEngine | `GridManager` | World click resolves to a cell selection | info-panel (auto-open trigger) | KEEP — selection-resolver emit must survive `WorldSelectionResolver` extraction (info-panel drift item) |
| `BlipId.EcoMoneyEarned` | BlipEngine | `EconomyManager` | Positive treasury delta (income) | hud-bar money readout / budget-panel | KEEP |
| `BlipId.EcoMoneySpent` | BlipEngine | `EconomyManager` | Negative treasury delta with `notifyInsufficientFunds` flag | hud-bar money readout / toolbar (denied buy) | KEEP |
| `BlipId.SysSaveGame` | BlipEngine | `GameSaveManager` (2 emit-sites: `SaveGame()` + `SaveGame(string)`) | Save file commit (autosave + named-save) | save-load-view (Save button) | KEEP — both emit-sites must survive `GameSaveManager` API additions (`DeleteSave` / `GetSaveFiles` / `HasAnySave`) |
| `sfxPanelOpen` (AudioClip) | UiSfxPlayer | `SubtypePickerController` | Picker becomes visible | tool-subtype-picker | KEEP — SubtypePickerController-owned; rebake of picker MUST preserve open emit |
| `sfxPanelClose` (AudioClip) | UiSfxPlayer | `SubtypePickerController` | Picker dismisses (ESC / same-tool re-click) | tool-subtype-picker | KEEP |
| `sfxPickerConfirm` (AudioClip) | UiSfxPlayer | `SubtypePickerController` | Subtype card click commits selection | tool-subtype-picker | KEEP |
| `sfxNotificationShow` (AudioClip) | direct AudioSource | `GameNotificationManager` | Non-error toast posted (Info / Success / Warning) | notifications-toast | KEEP — already in toast lock, anchored here for cross-ref |
| `sfxErrorFeedback` (AudioClip) | direct AudioSource | `GameNotificationManager` | Error toast posted | notifications-toast | KEEP — already in toast lock |

**Behavior-freeze rules.**

1. **Auto-emit invariance.** `ThemedButton` owns hover + click Blips for every button across every panel. Rebake MUST route all baked buttons through `ThemedButton` (not raw `UnityEngine.UI.Button`). Drift = silent loss of every UI Blip cue.
2. **Sim-side emits never reroute through UI layer.** `RoadManager` / `BuildingPlacementService` / `GridManager` / `EconomyManager` / `GameSaveManager` own their own emits. UI panels receive consequences via binds; they do NOT replay these cues.
3. **Volume + mute persistence.** `BlipBootstrap.SfxVolumeDbKey` + `BlipBootstrap.SfxMutedKey` PlayerPrefs are read on Awake and applied to BlipMixer SfxVolume param. `settings-view` SFX-volume slider (locked) drives these keys via dB↔linear util in `SettingsScreenDataAdapter`. Master + Music volume keys distinct. Rebake of settings-view MUST preserve the dB↔linear mapping.
4. **No silent muting.** `BlipEngine.Play(BlipId.None)` is a no-op by design (None = 0). Other emits assume valid registered patches.

**Pending follow-ups.**

- **Toast-tier expansion (already locked in `notifications-toast`).** Adds 3 new clips (`sfxSuccess` chime, `sfxWarning` low-pulse, `sfxMilestone` gold-flourish) — not yet authored. Authoring task tracked separately.
- **Pause-menu + modals (budget / stats / pause-menu / save-load-view).** No open / close cue today. Decision deferred — candidates: reuse `sfxPanelOpen` / `sfxPanelClose`, or add `BlipId.UiModalOpen` / `UiModalClose`. Lock during interactions phase.
- **Tooltip primitive.** No audio (intentional — 500 ms dwell + visual fade is the channel). Lock = silent.
- **Subtype picker confirm vs button click overlap.** When user clicks a subtype card, `sfxPickerConfirm` fires AND the underlying button auto-emits `UiButtonClick`. Audit if double-emit is intentional — flag for interactions phase.

---

### Per-panel wiring contract — template

Every `### {slug}` block below emits this sub-section alongside prose + children + JSON. MCP bake / bridge / validation tools read these fields directly. No prose-only panel definitions.

| Field | Type | Purpose |
| --- | --- | --- |
| `bake_requirements.sprites[]` | string[] | Sprite slugs that must exist in `catalog_sprite` rows before bake |
| `bake_requirements.tokens[]` | string[] | Token slugs referenced (validated against §Tokens) |
| `bake_requirements.archetypes[]` | string[] | Archetype slugs needed (e.g. `illuminated-button`) |
| `actions_referenced[]` | string[] | Action strings used by buttons / toggles — validated against C# action registry |
| `binds_referenced[]` | string[] | Bind paths used by labels / readouts / toggles — validated against runtime bind registry |
| `hotkeys[]` | `{key, action}[]` | Hotkey bindings — checked against global conflict registry |
| `verification_hooks[]` | string[] | Bridge tool slugs that introspect panel state for closed-loop verify |
| `variant_transitions[]` | `{from, to, trigger}[]` | State machine edges — trigger = input event / state flag / time |

JSON shape per panel:

```json
{
  "wiring": {
    "bake_requirements": {
      "sprites":    ["sprite-a", "sprite-b"],
      "tokens":     ["color.bg.cream", "size.icon"],
      "archetypes": ["illuminated-button"]
    },
    "actions_referenced":  ["action.zoom-in"],
    "binds_referenced":    ["cityStats.population"],
    "hotkeys":             [{"key": "Space", "action": "action.pause-toggle"}],
    "verification_hooks":  ["bridge.panel-state-get"],
    "variant_transitions": [
      {"from": "default", "to": "hover",   "trigger": "pointer.enter"},
      {"from": "hover",   "to": "pressed", "trigger": "pointer.down"}
    ]
  }
}
```

Drift rule: any `actions_referenced` / `binds_referenced` / sprite slug not resolvable at bake time = bake error, not warning.

---

Locked panels listed below. Each `### {slug}` block = prose meta + children tree + DB-shape JSON + wiring contract. Seed migration scans these per-panel JSON blocks to assemble the consolidated `panels[]` array — no separate consolidated source needed.

### hud-bar

**Role.** Single always-visible top strip. Owns file ops (new/save/load) + city status readouts (name, sim-date, population) + global game controls (camera zoom, budget, time, stats panel toggler, map panel toggler). Replaces the prior 19-cell bottom hud-bar (baseline).

**Position.** Anchored top edge of viewport, full width.
**Layout.** `hstack` with 3 named zones (`left`, `center`, `right`). Right zone uses internal column layout (4 cols, mixed row spans).
**Height.** Driven by tallest right-zone column (2 rows of `illuminated-button`).
**Theme.** `illuminated-button` archetype across all buttons; cream body + tan border + indigo icon.
**Layer.** `z.hud`.

#### Children tree

```
hud-bar  (hstack, full-width, top-anchored)
├─ left zone   (hstack)  — new-save-load-div
│   ├─ new-button       (illuminated-button)
│   ├─ save-button      (illuminated-button)
│   └─ load-button      (illuminated-button)
├─ center zone (vstack)  — city-readout-div
│   ├─ city-name-label  (label)
│   ├─ sim-date-readout (readout)
│   └─ population-readout (readout)
└─ right zone  (hstack of 4 cols) — game-controls-div
    ├─ col 0: zoom-cluster (vstack, 2 rows)
    │   ├─ zoom-in-button   (illuminated-button)
    │   └─ zoom-out-button  (illuminated-button)
    ├─ col 1: time-control-stack (vstack, 2 rows)
    │   ├─ row 0: budget-button (illuminated-button, full col width, dual readout — total + delta)
    │   └─ row 1: time-controls-row (hstack, 2 buttons)
    │       ├─ play-pause-button   (illuminated-button, icon swap on bind)
    │       └─ speed-cycle-button  (illuminated-button, label cycles 1×→2×→3×→4×)
    ├─ col 2: stats-button (illuminated-button, tall, spans 2 rows)
    ├─ col 3: auto-button  (illuminated-button, tall, spans 2 rows) — auto-mode toggler (caption-only "AUTO" until icon authored)
    └─ col 4: map-button   (illuminated-button, tall, spans 2 rows)
```

12 leaf elements + 6 grouping containers.

#### JSON (seed source — DB shape)

```json
{
  "slug": "hud-bar",
  "fields": {
    "anchor": "top",
    "layout_template": "hstack",
    "layout": "hstack",
    "gap_px": 8,
    "padding_json": "{\"top\":4,\"left\":8,\"right\":8,\"bottom\":4}",
    "params_json": "{\"side\":\"top\",\"width\":\"full\"}"
  },
  "children": [
    { "ord": 1,  "kind": "button",  "instance_slug": "hud-bar-new-button",         "params_json": "{\"icon\":\"icon-new\",\"kind\":\"illuminated-button\",\"label\":\"NEW\",\"action\":\"action.game-new\"}",                                                                                                                              "layout_json": "{\"zone\":\"left\",\"ord\":0}" },
    { "ord": 2,  "kind": "button",  "instance_slug": "hud-bar-save-button",        "params_json": "{\"icon\":\"icon-save\",\"kind\":\"illuminated-button\",\"label\":\"SAVE\",\"action\":\"action.game-save\"}",                                                                                                                            "layout_json": "{\"zone\":\"left\",\"ord\":1}" },
    { "ord": 3,  "kind": "button",  "instance_slug": "hud-bar-load-button",        "params_json": "{\"icon\":\"icon-load\",\"kind\":\"illuminated-button\",\"label\":\"LOAD\",\"action\":\"action.game-load\"}",                                                                                                                            "layout_json": "{\"zone\":\"left\",\"ord\":2}" },
    { "ord": 4,  "kind": "label",   "instance_slug": "hud-bar-city-name-label",    "params_json": "{\"kind\":\"label\",\"bind\":\"cityStats.cityName\",\"font\":\"display\",\"align\":\"center\"}",                                                                                                                                          "layout_json": "{\"zone\":\"center\",\"row\":0}" },
    { "ord": 5,  "kind": "readout", "instance_slug": "hud-bar-sim-date-readout",   "params_json": "{\"kind\":\"readout\",\"bind\":\"timeManager.currentDate\",\"format\":\"text\",\"cadence\":\"tick\"}",                                                                                                                                    "layout_json": "{\"zone\":\"center\",\"row\":1}" },
    { "ord": 6,  "kind": "readout", "instance_slug": "hud-bar-population-readout", "params_json": "{\"kind\":\"readout\",\"bind\":\"cityStats.population\",\"format\":\"integer\",\"cadence\":\"tick\"}",                                                                                                                                    "layout_json": "{\"zone\":\"center\",\"row\":2}" },
    { "ord": 7,  "kind": "button",  "instance_slug": "hud-bar-zoom-in-button",     "params_json": "{\"icon\":\"icon-zoom-in\",\"kind\":\"illuminated-button\",\"action\":\"action.camera-zoom-in\"}",                                                                                                                                        "layout_json": "{\"zone\":\"right\",\"col\":0,\"row\":0}" },
    { "ord": 8,  "kind": "button",  "instance_slug": "hud-bar-zoom-out-button",    "params_json": "{\"icon\":\"icon-zoom-out\",\"kind\":\"illuminated-button\",\"action\":\"action.camera-zoom-out\"}",                                                                                                                                      "layout_json": "{\"zone\":\"right\",\"col\":0,\"row\":1}" },
    { "ord": 9,  "kind": "readout-button", "instance_slug": "hud-bar-budget-button", "params_json": "{\"icon\":\"icon-budget\",\"kind\":\"illuminated-button\",\"bind\":\"economyManager.totalBudget\",\"sub_bind\":\"economyManager.budgetDelta\",\"format\":\"currency\",\"sub_format\":\"currency-delta\",\"action\":\"action.budget-panel-toggle\"}", "layout_json": "{\"zone\":\"right\",\"col\":1,\"row\":0}" },
    { "ord": 10, "kind": "button",  "instance_slug": "hud-bar-play-pause-button",  "params_json": "{\"icon\":\"icon-play\",\"alt_icon\":\"icon-pause\",\"kind\":\"illuminated-button\",\"bind_state\":\"timeManager.isPaused\",\"action\":\"action.time-play-pause-toggle\"}",                                                                "layout_json": "{\"zone\":\"right\",\"col\":1,\"row\":1,\"sub_col\":0}" },
    { "ord": 11, "kind": "button",  "instance_slug": "hud-bar-speed-cycle-button", "params_json": "{\"kind\":\"illuminated-button\",\"label_bind\":\"timeManager.currentTimeSpeedLabel\",\"action\":\"action.time-speed-cycle\"}",                                                                                                            "layout_json": "{\"zone\":\"right\",\"col\":1,\"row\":1,\"sub_col\":1}" },
    { "ord": 12, "kind": "button",  "instance_slug": "hud-bar-stats-button",       "params_json": "{\"icon\":\"icon-stats\",\"kind\":\"illuminated-button\",\"action\":\"action.stats-panel-toggle\"}",                                                                                                                                       "layout_json": "{\"zone\":\"right\",\"col\":2,\"row\":0,\"rowSpan\":2}" },
    { "ord": 13, "kind": "button",  "instance_slug": "hud-bar-auto-button",        "params_json": "{\"kind\":\"illuminated-button\",\"label\":\"AUTO\",\"bind_state\":\"uiManager.isAutoMode\",\"action\":\"action.auto-mode-toggle\"}",                                                                                                       "layout_json": "{\"zone\":\"right\",\"col\":3,\"row\":0,\"rowSpan\":2}" },
    { "ord": 14, "kind": "button",  "instance_slug": "hud-bar-map-button",         "params_json": "{\"icon\":\"icon-map\",\"kind\":\"illuminated-button\",\"action\":\"action.map-panel-toggle\"}",                                                                                                                                          "layout_json": "{\"zone\":\"right\",\"col\":4,\"row\":0,\"rowSpan\":2}" }
  ]
}
```

#### Wiring contract

```json
{
  "wiring": {
    "bake_requirements": {
      "sprites": [
        "icon-new", "icon-save", "icon-load",
        "icon-zoom-in", "icon-zoom-out",
        "icon-budget", "icon-play", "icon-pause",
        "icon-stats", "icon-map"
      ],
      "tokens": [
        "color.bg.cream", "color.bg.cream-pressed",
        "color.border.tan", "color.icon.indigo", "color.text.dark",
        "size.icon", "size.button.tall", "size.button.short", "size.strip.h",
        "gap.tight", "gap.default", "gap.loose", "pad.button",
        "z.hud"
      ],
      "archetypes": ["illuminated-button", "label", "readout", "readout-button"]
    },
    "actions_referenced": [
      "action.game-new", "action.game-save", "action.game-load",
      "action.camera-zoom-in", "action.camera-zoom-out",
      "action.budget-panel-toggle",
      "action.time-play-pause-toggle", "action.time-speed-cycle",
      "action.time-speed-set-1", "action.time-speed-set-2", "action.time-speed-set-3", "action.time-speed-set-4",
      "action.stats-panel-toggle", "action.map-panel-toggle",
      "action.auto-mode-toggle"
    ],
    "binds_referenced": [
      "cityStats.cityName",
      "cityStats.population",
      "timeManager.currentDate",
      "timeManager.isPaused",
      "timeManager.currentTimeSpeedLabel",
      "economyManager.totalBudget",
      "economyManager.budgetDelta",
      "uiManager.isAutoMode"
    ],
    "hotkeys": [
      { "key": "Space",  "action": "action.time-play-pause-toggle" },
      { "key": "Alpha1", "action": "action.time-speed-set-1" },
      { "key": "Alpha2", "action": "action.time-speed-set-2" },
      { "key": "Alpha3", "action": "action.time-speed-set-3" },
      { "key": "Alpha4", "action": "action.time-speed-set-4" }
    ],
    "verification_hooks": [
      "bridge.panel-state-get",
      "bridge.button-click",
      "bridge.label-text-get",
      "bridge.readout-value-get"
    ],
    "variant_transitions": [
      { "from": "default", "to": "hover",    "trigger": "pointer.enter" },
      { "from": "hover",   "to": "pressed",  "trigger": "pointer.down" },
      { "from": "pressed", "to": "active",   "trigger": "pointer.up + action.fire" },
      { "from": "hover",   "to": "default",  "trigger": "pointer.exit" },
      { "from": "default", "to": "disabled", "trigger": "state.flag.disabled" }
    ]
  }
}
```

#### Drift / open questions (post-lock code tasks)

- **Bake schema nesting.** Current `panels.json` snapshot (schema_v4) carries flat zone-tagged children only. Right-zone column + row + rowSpan + sub_col layout requires `layout_json` schema extension OR sub-panel decomposition. Flag → bake-pipeline code task.
- **`<readout-button>` kind missing.** `budget-button` mixes dual readouts (total + delta) with click action (panel toggle). Either extend `<IconButton>` to accept secondary bind + format, OR introduce new `<ReadoutButton>` component. Flag → §Components reconciliation.
- **Speed model drift (spec D5/D33 vs code).** MVP scope: 1× = 1 real-sec per game-day, cycle 1→2→3→4, Pause separate. `TimeManager.timeSpeeds = [0, 0.5, 1, 2, 4]` in code. Action `action.time-speed-cycle` cycles over [1,2,3,4]; pause = `action.time-play-pause-toggle`. Code reconciliation task.
- **Action registry source-of-truth.** None of the `action.*` strings exist in C# yet. Need `UiActionRegistry` static class + bake-time validator + MCP `action_registry_list` slice. Flag → code task.
- **Bind registry source-of-truth.** `cityStats.*` / `timeManager.*` / `economyManager.*` paths need a runtime bind dispatcher. Flag → code task.
- **Sprite catalog gaps.** 10 icon slugs listed; verify each exists in `catalog_sprite` rows pre-bake. Flag → catalog audit task.

#### Existing Implementation (preserve)

> Behavior-freeze inventory. Every entry below is **working production code** that the rebake MUST extend, not replace. Drift list above describes ADDITIONS / EXTENSIONS layered on top of these.

**1. `HudBarDataAdapter` (`Assets/Scripts/UI/HUD/HudBarDataAdapter.cs`)** — bake-to-sim bridge. PRESERVE:

- **Producer cache in `Awake`** (invariants #3 + #4) — `CityStats` SO + `EconomyManager` + `TimeManager` + `UIManager` + `CameraController` + `UiAssetCatalog` + `MiniMapController` + `GrowthBudgetPanelController`. Inspector first, `FindObjectOfType` fallback. Rebake MUST keep this caching contract.
- **`RebindButtonsByIconSlug()`** — hard-resets all `IlluminatedButton` Inspector slots (`_newButton` / `_saveButton` / `_loadButton` / `_autoButton` / `_budgetButton` / `_zoomInButton` / `_zoomOutButton` / `_statsButton` / `_miniMapButton` / `_speedButtons[5]`) then walks child `IlluminatedButton` components matching `IlluminatedButtonDetail.iconSpriteSlug`. Lowercase normalization for BUG-62. Caption-text fallback for sprite-less buttons (MAP / AUTO / BUDGET) via `TextMeshProUGUI`. Catalog-resolved display names for AUTO + MAP toggles. **CRITICAL:** drop this method = every click handler fires stale action against re-baked button. Rebake MUST keep slug-walk + caption-fallback.
- **`WireClickHandlers()`** — binds every `IlluminatedButton.OnClicked` to its action handler (zoom / minimap-toggle / stats-toggle / budget-toggle / speed slot / play-pause). Rebake MUST preserve these bindings.
- **`EnsureSpeedSlot(index, button)`** — populates `_speedButtons` array by canonical slug (`pause-button-1-64` / `speed-1..4-button-1-64`), index 0..4 maps to pause / 0.5× / 1× / 2× / 4×. PRESERVE.
- **Stale-button cleanup** — `RuntimeMiniMapButton` GameObject destroyed on Awake (legacy corner button retired). PRESERVE.
- **Lazy-spawn `GrowthBudgetPanelController`** when Inspector slot empty (FEAT-59). PRESERVE.

**2. `MiniMapController` (`Assets/Scripts/Controllers/GameControllers/MiniMapController.cs`)** — minimap canvas + click/drag camera nav. PRESERVE every existing behavior; lock at `map-panel` says "EXTEND for layer toggles + drag-pan". Rebake of hud-bar's `hud-bar-map-button` MUST call existing `MiniMapController` API (toggle / SetVisible) — NOT replace it.

**3. `SpeedButtonsController` (`Assets/Scripts/Controllers/UnitControllers/SpeedButtonsController.cs`)** — 5 speed buttons + `OnSpeedChangedExternally(int)` callback wired by `TimeManager`. PRESERVE. Rebake of hud-bar's speed cluster MUST keep `TimeManager.SetTimeSpeedIndex(int)` as the sole mutation path (HUD click + keyboard `Space` / `Alpha1..4` both route here per `TimeManager.HandleOnKeyInput`).

**4. `TimeManager` (`Assets/Scripts/Managers/GameManagers/TimeManager.cs`)** — sim-tick driver + speed state owner. PRESERVE: `currentTimeSpeedIndex` / `timeSpeeds[5]` / `SetTimeSpeedIndex(int)` / `CurrentTimeSpeedIndex` getter / `HandleOnKeyInput()` / `GetCurrentDate()` / `GetCurrentTimeMultiplier()`. Hud-bar speed-cycle button binds against `currentTimeSpeedLabel` (locked above). Geography-init gate (`geographyManager.IsInitialized`) survives — UI must remain responsive during init even when sim-tick blocked.

**5. `CityStats` SO** — `cityName` + `population` producer. Rebake of city-name label + population readout MUST bind to existing fields (no shadow producer). Drift list above already calls out `CityStats.SetCityName` API addition for new-game-form.

**6. `EconomyManager`** — `totalBudget` + `budgetDelta` producer. Hud-bar budget-button readout-button binds here. PRESERVE. `BlipId.EcoMoneyEarned` / `EcoMoneySpent` emit-sites preserved (see audio-cues registry).

**7. `IlluminatedButton` + `IlluminatedButtonDetail`** — bake-time button archetype carrying `iconSpriteSlug` field used by slug-walk above. PRESERVE both, including `OnClicked` UnityEvent surface.

**8. `UiAssetCatalog`** — slug → display-name resolution (Stage 9.13). PRESERVE `TryGetButtonEntry(slug, out entry)` API.

**Behavior-freeze rules (hud-bar specific).**

1. **Slug-walk owns slot binding.** Inspector array slots are scratch space; runtime authority = `RebindButtonsByIconSlug` matching baked `iconSpriteSlug`. Rebake MUST maintain slug stability — renaming a baked icon = breaks slot resolution silently.
2. **Speed mutation single-path.** `TimeManager.SetTimeSpeedIndex` is the ONLY write path. HUD click + keyboard hotkey both route here. Rebake MUST NOT add a parallel mutator.
3. **Geography-init gate non-bypass.** Sim-state reads gated by `geographyManager.IsInitialized`. HUD time accumulator + key input remain active during init for responsiveness. Rebake MUST keep this split.
4. **Caption-text fallback survives.** MAP / AUTO / BUDGET bake without sprite art today; caption-text fallback in slug-walk is the resolution path. If rebake adds sprite art, slug-walk MUST still match by slug FIRST + fall back to caption — both paths coexist.

#### DB shape achieved

> Migration `0108_seed_hud_bar_panel_v2` (entity_id=41, slug=`hud-bar`). DB row 1:1 with locked definition.

| Field | Value |
| --- | --- |
| migration | `0108_seed_hud_bar_panel_v2` |
| entity_id | 41 |
| slug | `hud-bar` |
| layout_template | `hstack` |
| layout | `hstack` |
| rect_json | `{"pivot":[0.5,1],"anchor_min":[0,1],"anchor_max":[1,1],"size_delta":[-16,144],"anchored_position":[0,-8]}` |

**schema_v4 children list** (14 rows, `panel_child` table, all slot_name=`main`):

| order_idx | instance_slug | child_kind |
| --- | --- | --- |
| 1 | `hud-bar-new-button` | button |
| 2 | `hud-bar-save-button` | button |
| 3 | `hud-bar-load-button` | button |
| 4 | `hud-bar-city-name-label` | label |
| 5 | `hud-bar-sim-date-readout` | label |
| 6 | `hud-bar-population-readout` | label |
| 7 | `hud-bar-zoom-in-button` | button |
| 8 | `hud-bar-zoom-out-button` | button |
| 9 | `hud-bar-budget-button` | button |
| 10 | `hud-bar-play-pause-button` | button |
| 11 | `hud-bar-speed-cycle-button` | button |
| 12 | `hud-bar-stats-button` | button |
| 13 | `hud-bar-auto-button` | button |
| 14 | `hud-bar-map-button` | button |

---

### toolbar

**Role.** Build / paint tool launcher pinned to viewport left edge. Player taps a tool → subtype picker opens at fixed bottom-left strip. Owns 11 tool slots in 4 logical groups + small separators between groups. Single always-visible.

**Position.** Anchored left edge, top-aligned (sits below `hud-bar`).
**Layout.** 2-column grid, `vstack` of group blocks separated by thin separator bars. Each group = small `vstack` of 2-col rows.
**Width.** Driven by `2 × size.icon + gap.tight + pad.button-h × 2`. Narrow + tall.
**Theme.** `illuminated-button` archetype across all tool buttons; cream body + tan border + indigo icon.
**Layer.** `z.hud`.

**Selection state.** Active tool = pressed-look (cream-pressed fill, reuses existing pressed sprite). One tool active at a time; clicking another swaps. Click active tool again or ESC → deselect (no tool active, picker closes).

**Tooltips.** Icon-only buttons; hover tooltip = full tool name (no hotkey hint, no labels). Touch fallback = long-press.

**Hotkeys.** None. Click only.

**Subtype picker.** Opening anchor = fixed bottom-left horizontal strip (separate panel `tool-subtype-picker`, not nested under toolbar). Toolbar tool click → emits `action.tool-select` with tool family payload; picker panel listens + opens.

#### Children tree

```
toolbar  (vstack, left-anchored, top-aligned)
├─ group 0: zoning  (2x2 grid)
│   ├─ row 0: [residential-tool | commercial-tool]
│   └─ row 1: [industrial-tool  | state-zoning-tool]
├─ separator-0  (1px tan bar, gap.tight above + below)
├─ group 1: infrastructure  (2x2 grid)
│   ├─ row 0: [road-tool  | power-tool]
│   └─ row 1: [water-tool | sewage-tool]
├─ separator-1
├─ group 2: civic + nature  (1x2 row)
│   └─ row 0: [landmark-tool | forests-tool]
├─ separator-2
└─ group 3: destroy  (1x2 row)
    └─ row 0: [demolish-cell-tool | demolish-area-tool (DISABLED placeholder)]
```

11 tool slots (10 active + 1 disabled placeholder) + 4 group containers + 3 separators.

#### JSON (seed source — DB shape)

```json
{
  "slug": "toolbar",
  "fields": {
    "anchor": "left",
    "layout_template": "vstack",
    "layout": "vstack",
    "gap_px": 4,
    "padding_json": "{\"top\":8,\"left\":4,\"right\":4,\"bottom\":8}",
    "params_json": "{\"side\":\"left\",\"align\":\"top\",\"cols\":2}"
  },
  "children": [
    { "ord": 1,  "kind": "button",    "instance_slug": "toolbar-residential-tool",     "params_json": "{\"icon\":\"icon-zone-residential\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"ResidentialZoning\"}",                                "layout_json": "{\"group\":0,\"row\":0,\"col\":0}" },
    { "ord": 2,  "kind": "button",    "instance_slug": "toolbar-commercial-tool",      "params_json": "{\"icon\":\"icon-zone-commercial\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"CommercialZoning\"}",                                  "layout_json": "{\"group\":0,\"row\":0,\"col\":1}" },
    { "ord": 3,  "kind": "button",    "instance_slug": "toolbar-industrial-tool",      "params_json": "{\"icon\":\"icon-zone-industrial\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"IndustrialZoning\"}",                                  "layout_json": "{\"group\":0,\"row\":1,\"col\":0}" },
    { "ord": 4,  "kind": "button",    "instance_slug": "toolbar-state-zoning-tool",    "params_json": "{\"icon\":\"icon-zone-state\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"StateZoning\"}",                                            "layout_json": "{\"group\":0,\"row\":1,\"col\":1}" },
    { "ord": 5,  "kind": "separator", "instance_slug": "toolbar-separator-0",          "params_json": "{\"orientation\":\"horizontal\",\"thickness\":1,\"color_token\":\"color.border.tan\"}",                                                                                       "layout_json": "{\"group\":\"sep\",\"after_group\":0}" },
    { "ord": 6,  "kind": "button",    "instance_slug": "toolbar-road-tool",            "params_json": "{\"icon\":\"icon-infra-road\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"Road\"}",                                                    "layout_json": "{\"group\":1,\"row\":0,\"col\":0}" },
    { "ord": 7,  "kind": "button",    "instance_slug": "toolbar-power-tool",           "params_json": "{\"icon\":\"icon-infra-power\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"Power\"}",                                                  "layout_json": "{\"group\":1,\"row\":0,\"col\":1}" },
    { "ord": 8,  "kind": "button",    "instance_slug": "toolbar-water-tool",           "params_json": "{\"icon\":\"icon-infra-water\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"Water\"}",                                                  "layout_json": "{\"group\":1,\"row\":1,\"col\":0}" },
    { "ord": 9,  "kind": "button",    "instance_slug": "toolbar-sewage-tool",          "params_json": "{\"icon\":\"icon-infra-sewage\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"Sewage\"}",                                                "layout_json": "{\"group\":1,\"row\":1,\"col\":1}" },
    { "ord": 10, "kind": "separator", "instance_slug": "toolbar-separator-1",          "params_json": "{\"orientation\":\"horizontal\",\"thickness\":1,\"color_token\":\"color.border.tan\"}",                                                                                       "layout_json": "{\"group\":\"sep\",\"after_group\":1}" },
    { "ord": 11, "kind": "button",    "instance_slug": "toolbar-landmark-tool",        "params_json": "{\"icon\":\"icon-civic-landmark\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"Landmark\"}",                                            "layout_json": "{\"group\":2,\"row\":0,\"col\":0}" },
    { "ord": 12, "kind": "button",    "instance_slug": "toolbar-forests-tool",         "params_json": "{\"icon\":\"icon-nature-forests\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"Forests\"}",                                             "layout_json": "{\"group\":2,\"row\":0,\"col\":1}" },
    { "ord": 13, "kind": "separator", "instance_slug": "toolbar-separator-2",          "params_json": "{\"orientation\":\"horizontal\",\"thickness\":1,\"color_token\":\"color.border.tan\"}",                                                                                       "layout_json": "{\"group\":\"sep\",\"after_group\":2}" },
    { "ord": 14, "kind": "button",    "instance_slug": "toolbar-demolish-cell-tool",   "params_json": "{\"icon\":\"icon-destroy-cell\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"DemolishCell\"}",                                          "layout_json": "{\"group\":3,\"row\":0,\"col\":0}" },
    { "ord": 15, "kind": "button",    "instance_slug": "toolbar-demolish-area-tool",   "params_json": "{\"icon\":\"icon-destroy-area\",\"kind\":\"illuminated-button\",\"action\":\"action.tool-select\",\"tool_family\":\"DemolishArea\",\"disabled\":true,\"tooltip_override\":\"Area demolish (coming soon)\"}", "layout_json": "{\"group\":3,\"row\":0,\"col\":1}" }
  ]
}
```

#### Wiring contract

```json
{
  "wiring": {
    "bake_requirements": {
      "sprites": [
        "icon-zone-residential", "icon-zone-commercial", "icon-zone-industrial", "icon-zone-state",
        "icon-infra-road", "icon-infra-power", "icon-infra-water", "icon-infra-sewage",
        "icon-civic-landmark", "icon-nature-forests",
        "icon-destroy-cell", "icon-destroy-area"
      ],
      "tokens": [
        "color.bg.cream", "color.bg.cream-pressed",
        "color.border.tan", "color.icon.indigo",
        "size.icon", "size.button.short",
        "gap.tight", "gap.default", "pad.button",
        "z.hud"
      ],
      "archetypes": ["illuminated-button", "separator"]
    },
    "actions_referenced": [
      "action.tool-select",
      "action.tool-deselect"
    ],
    "binds_referenced": [
      "toolSelection.activeFamily",
      "toolSelection.disabledTools"
    ],
    "hotkeys": [],
    "verification_hooks": [
      "bridge.panel-state-get",
      "bridge.button-click",
      "bridge.tool-active-family-get"
    ],
    "variant_transitions": [
      { "from": "default",  "to": "hover",    "trigger": "pointer.enter" },
      { "from": "hover",    "to": "pressed",  "trigger": "pointer.down" },
      { "from": "pressed",  "to": "active",   "trigger": "pointer.up + action.fire" },
      { "from": "active",   "to": "default",  "trigger": "pointer.down (same button) | ESC | other-tool.active" },
      { "from": "hover",    "to": "default",  "trigger": "pointer.exit" },
      { "from": "default",  "to": "disabled", "trigger": "state.flag.disabled (e.g. demolish-area)" }
    ]
  }
}
```

#### Drift / open questions (post-lock code tasks)

- **`SubtypePickerController.ToolFamily` enum gaps.** Current C# enum has 8 entries (incl. `StateService`). Spec needs: rename `StateService` → `StateZoning`, add `Sewage`, `Landmark`, `DemolishCell`, `DemolishArea`. Flag → C# enum reconciliation.
- **`<separator>` archetype missing.** No separator component yet in §Components. Need thin horizontal bar element with token-driven thickness + color. Flag → §Components addition.
- **Disabled-state visual.** `illuminated-button` archetype lacks documented `disabled` variant (greyed sprite + tooltip override). Flag → component variant table extension.
- **`action.tool-select` payload schema.** Action signature must accept `tool_family` payload. Action registry needs typed payload support, not flat string. Flag → action registry design task.
- **Toolbar height / overflow.** 11 active + 1 disabled = 6 rows × 2 cols + 3 separators. Need to verify viewport-height fit at minimum supported resolution. Flag → layout responsive audit.
- **StateZoning subtype mechanism.** Each `StateZoning` subtype (park, plaza, civic, etc.) paints area + spawns subtype-pool buildings + uses subtype-specific grey-shade tile variant. Subtype pool + tile variants not yet implemented. Flag → simulation + sprite-catalog tasks.
- **Sprite catalog gaps.** 12 icon slugs listed; verify each exists in `catalog_sprite` rows pre-bake. Flag → catalog audit task.

#### DB shape achieved

> Migration `0110_seed_toolbar_panel` (entity_id=100, slug=`toolbar`). Root rect seeded. Children deferred per Track A scope (hand-authored on prefab until later track).

| Field | Value |
| --- | --- |
| migration | `0110_seed_toolbar_panel` |
| entity_id | 100 |
| slug | `toolbar` |
| layout_template | `vstack` |
| layout | `vstack` |
| rect_json | `{"pivot":[0,0.5],"anchor_min":[0,0],"anchor_max":[0,1],"size_delta":[220,-352],"anchored_position":[12,24]}` |

**schema_v4 children note:** `panel_child` rows deferred — Track A only seeds root rect. Children stay hand-authored on `Assets/UI/Prefabs/Generated/toolbar.prefab` until a later track migrates them to DB.

---

### tool-subtype-picker

- **Role.** Subtype-selection strip companion to `toolbar`. Opens when a toolbar tool that has a `picker_variant` activates; lists subtype cards for that family + lets player arm a specific subtype before painting/placing. Replaces the 4 separate per-category palette panels per D23.
- **Position.** Fixed bottom-left horizontal strip; anchored bottom-left of the viewport, just inboard of `toolbar`'s right edge. Single shared screen position regardless of which family is active (variant content swaps in place).
- **Layout.** Horizontal scrollable card row inside a bordered translucent panel. Wheel + drag scroll; left/right arrow buttons at edges as overflow affordance.
- **Strip dimensions.** Strip height ≈ 96 px; cards 80 × 80; gap 8 px; 1 px border; 8 px corner-radius; dark translucent background. Width hugs the cards up to a viewport cap; beyond cap = horizontal scroll with arrow buttons surfaced.
- **Theme.** Dark translucent panel + cream/tan card bodies (matches `illuminated-button` body) + indigo icons.
- **Layer.** `z.hud` (10) — same plane as `hud-bar` + `toolbar`. Below modals + toasts.
- **Visibility / lifecycle.**
  - **Open trigger.** Toolbar tool selection where the family declares a `picker_variant ≠ none` → strip mounts with that variant + auto-arms the family's default subtype card.
  - **Close trigger.** ESC OR re-clicking the same active toolbar tool → strip unmounts + tool deselects. **No other dismissal.** World clicks paint freely; HUD clicks do not dismiss; clicking another card swaps subtype + keeps strip open; clicking another toolbar tool swaps strip variant in place.
  - **No-picker mode.** Toolbar tools with `picker_variant: none` (only `DemolishCell` in MVP) → strip stays unmounted; tool acts standalone.
- **Card content (3-line).** Icon + name + cost. **No capacity line** on the card; capacity surfaces in the post-placement info-panel only.
- **Sprite source policy (preview reuse).** Each card's icon = the same in-world isometric diamond tile sprite the placed cell will render with after commit. Picker = visual preview of the placed result; no separate picker-only artwork. `catalog_sprite` rows for picker subtype slugs (e.g. `r-light`, `c-medium`, `forest-sparse`) point at the same `Assets/Sprites/{Family}/{kind}-zoning-64.png` files used by the in-world tile renderer.
- **Cost label per family.** Single-click families (Power / Water / Sewage / Landmark) → flat $. Drag-paint + stroke + mode-driven families (R / C / I / StateZoning / Road / Forests) → $/cell.
- **Affordability state.** Live-bound to budget. Unaffordable cards render greyed + non-interactive + tooltip override `Cannot afford` / `Need $X / cell`. Affordable cards render normal cream body.
- **Active-card visual.** Armed card = pressed-cream body + 2 px indigo highlight ring. Other cards = default cream body. Hover = cream-pressed body.
- **Tooltips.** Hover ~500 ms → tooltip with name + cost + family-specific one-line hint (e.g. "drag to paint", "click 2 cells to place a stroke", "click to place"). No hotkeys.
- **Hotkeys.** None (mouse-only, matches toolbar D7 onboarding-dropped + toolbar lock).
- **Paint-mode policy per family.** Drives behavior on world clicks while card is armed. Card variant DOES NOT change paint mode — paint mode is a property of the family, not the subtype. Mode-driven families (Forests) place 2 secondary mode-buttons inside the strip alongside the cards (single-cell vs spray); the secondary buttons mutate paint mode within the family.

#### Variants per family

| Family         | Cards    | `picker_variant` | Subtype slugs                                                                              | Paint mode    | Cost label |
| -------------- | -------- | ---------------- | ------------------------------------------------------------------------------------------ | ------------- | ---------- |
| Residential    | 3        | `cards-density`  | `r-light`, `r-medium`, `r-heavy`                                                           | drag-paint    | $/cell     |
| Commercial     | 3        | `cards-density`  | `c-light`, `c-medium`, `c-heavy`                                                           | drag-paint    | $/cell     |
| Industrial     | 3        | `cards-density`  | `i-light`, `i-medium`, `i-heavy`                                                           | drag-paint    | $/cell     |
| StateZoning    | 7        | `cards-kind`     | `s-police`, `s-fire`, `s-edu`, `s-health`, `s-parks`, `s-public-housing`, `s-public-offices` | drag-paint    | $/cell     |
| Road           | 1        | `cards-kind`     | `road-highway` (elbows / bridges / T-intersections auto-derived from placement context — not exposed as picker subtypes) | stroke        | $/cell     |
| Power          | 2        | `cards-kind`     | `power-coal`, `power-solar`                                                                | single-click  | flat $     |
| Water          | 2        | `cards-kind`     | `water-reservoir`, `water-desal`                                                           | single-click  | flat $     |
| Sewage         | 2        | `cards-kind`     | `sewage-basic`, `sewage-treated`                                                           | single-click  | flat $     |
| Landmark       | 4        | `cards-kind`     | `lmk-city-1`, `lmk-city-2`, `lmk-region-1`, `lmk-region-2`                                 | single-click  | flat $     |
| Forests        | 3 + 2    | `cards-mode`     | `forest-sparse`, `forest-medium`, `forest-dense` + mode buttons `mode-single`, `mode-spray` | mode-driven   | $/cell     |
| DemolishCell   | 0        | `none`           | —                                                                                          | click-each    | —          |

#### Density-evolution semantics (R/C/I)

R/C/I cards lock density tier on placement. Sim evolves the placed cell *within* the selected density tier (richer building OR merge with same-density-same-subtype neighbors into a larger footprint), but never crosses density tier. Industrial agri/manuf/tech subtype assignment post-placement is `TBD` — separate sim concern, not a picker dimension. StateZoning has no density; cards are kinds.

#### JSON (DB shape per panel row)

```json
{
  "slug": "tool-subtype-picker",
  "fields": {
    "layout_template": "hstack",
    "layout": "hstack",
    "gap_px": 8,
    "padding_json": "{\"top\":8,\"left\":8,\"right\":8,\"bottom\":8}",
    "params_json": "{\"anchor\":\"bottom-left\",\"strip_h_px\":96,\"card_w_px\":80,\"card_h_px\":80,\"max_strip_w_px\":1200,\"hidden_default\":true,\"open_trigger\":\"toolbar.tool-select\",\"close_triggers\":[\"key.escape\",\"toolbar.tool-deselect\"]}"
  },
  "panel": {
    "slug": "tool-subtype-picker",
    "layout_template": "hstack",
    "layout": "hstack",
    "gap_px": 8,
    "padding_json": "{\"top\":8,\"left\":8,\"right\":8,\"bottom\":8}",
    "params_json": "{\"anchor\":\"bottom-left\",\"strip_h_px\":96,\"card_w_px\":80,\"card_h_px\":80,\"max_strip_w_px\":1200,\"hidden_default\":true,\"open_trigger\":\"toolbar.tool-select\",\"close_triggers\":[\"key.escape\",\"toolbar.tool-deselect\"]}"
  },
  "children": [
    { "ord":  1, "kind": "button", "params_json": "{\"icon\":\"scroll-left\",\"kind\":\"illuminated-button\",\"role\":\"strip-arrow-left\",\"hidden_unless_overflow\":true}",            "sprite_ref": "", "layout_json": "{\"zone\":\"left-edge\"}",  "instance_slug": "tool-subtype-picker-arrow-left" },
    { "ord":  2, "kind": "button", "params_json": "{\"icon\":\"scroll-right\",\"kind\":\"illuminated-button\",\"role\":\"strip-arrow-right\",\"hidden_unless_overflow\":true}",          "sprite_ref": "", "layout_json": "{\"zone\":\"right-edge\"}", "instance_slug": "tool-subtype-picker-arrow-right" },
    { "ord": 10, "kind": "card",   "params_json": "{\"kind\":\"subtype-card\",\"family\":\"*\",\"subtype\":\"*\",\"icon\":\"*\",\"name\":\"*\",\"cost_text\":\"*\",\"affordable_bind\":\"toolSelection.affordable.*\"}", "sprite_ref": "", "layout_json": "{\"zone\":\"cards\"}", "instance_slug": "tool-subtype-picker-card-template" }
  ],
  "notes": "Children flatten to N cards per active picker_variant at runtime — 'card-template' row is the schema example, not a literal child. Bake plan: emit one child per resolved subtype keyed by `{family}-{subtype}` instance_slug. Variant slug pattern → `tool-subtype-picker-<family>-<subtype>`."
}
```

#### Wiring contract

| Surface                | Slug / id                                                                                                                                                | Source of truth                                            |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| Sprites — strip arrows | `scroll-left`, `scroll-right`                                                                                                                            | `catalog_sprite`                                           |
| Sprites — R/C/I cards  | `r-light`, `r-medium`, `r-heavy`, `c-light`, `c-medium`, `c-heavy`, `i-light`, `i-medium`, `i-heavy`                                                     | `catalog_sprite`                                           |
| Sprites — StateZoning  | `s-police`, `s-fire`, `s-edu`, `s-health`, `s-parks`, `s-public-housing`, `s-public-offices`                                                             | `catalog_sprite`                                           |
| Sprites — Road         | `road-highway`                                                                                                                                           | `catalog_sprite`                                           |
| Sprites — Utility      | `power-coal`, `power-solar`, `water-reservoir`, `water-desal`, `sewage-basic`, `sewage-treated`                                                          | `catalog_sprite`                                           |
| Sprites — Landmark     | `lmk-city-1`, `lmk-city-2`, `lmk-region-1`, `lmk-region-2`                                                                                               | `catalog_sprite`                                           |
| Sprites — Forests      | `forest-sparse`, `forest-medium`, `forest-dense`, `mode-single`, `mode-spray`                                                                            | `catalog_sprite`                                           |
| Tokens                 | `color.bg.cream`, `color.bg.cream-pressed`, `color.border.tan`, `color.icon.indigo`, `color.text.dark`, `color.alert.red`, `size.strip.h`, `gap.default`, `pad.button`, `z.hud` | `tokens` block §1                                          |
| Archetypes             | `subtype-card` (new — icon + name + cost, 80×80, 3-line), `illuminated-button` (re-used for arrows + Forests mode buttons)                               | `catalog_archetype`                                        |
| Actions                | `action.subtype-arm` (payload `{family, subtype}`), `action.subtype-disarm`, `action.forests-set-mode` (payload `{mode: 'single' \| 'spray'}`), `action.strip-scroll` (payload `{dir: 'left' \| 'right'}`) | `UiActionRegistry` (TBD)                                   |
| Binds                  | `toolSelection.activeFamily` (string), `toolSelection.activeSubtype` (string), `toolSelection.forestsMode` (enum), `toolSelection.affordable.*` (per-subtype bool keyed by subtype-slug), `toolSelection.stripVisible` (bool) | runtime bind dispatcher (TBD)                              |
| Hotkeys                | None                                                                                                                                                     | —                                                          |
| Verification hooks     | After bake: `findobjectoftype_scan` → `tool-subtype-picker` exists, hidden by default. Test-mode: tap toolbar `r-zoning` → strip mounts + 3 R cards + first armed. Tap ESC → strip unmounts. Tap `s-zoning` → strip remounts with 7 StateZoning cards. | `unity:testmode-batch` scenarios `picker-open` + `picker-swap` + `picker-close` (TBD) |
| Variant transitions    | `default → arming` on tool-select. `arming → armed` on card-click. `armed → painting` on world-click. `painting → armed` on world mouse-up. `armed → unmount` on ESC or same-toolbar-tool re-click. `armed → arming` on different-tool-click (variant swap). | runtime dispatcher                                         |

#### Drift / open questions (post-lock code tasks)

- **`subtype-card` archetype missing.** No 3-line card archetype yet (icon + name + cost). Needs new entry in `catalog_archetype` + bake handler + ThemedPanel-equivalent layout. Flag → archetype + bake task.
- **Action registry source-of-truth.** 4 new actions (`subtype-arm`, `subtype-disarm`, `forests-set-mode`, `strip-scroll`) — depends on action-registry design task already flagged in toolbar lock. Consolidate.
- **Bind registry source-of-truth.** 5 new binds; same dependency on bind-dispatcher design task. `toolSelection.affordable.*` keyed by subtype-slug = wildcard pattern → registry must support pattern subscriptions.
- **Industrial subtype assignment.** Picker exposes 3 density cards (light/medium/heavy). Agri/Manuf/Tech subtype determination post-placement is undefined in MVP. Flag → sim spec task (likely deferred to post-MVP).
- **StateZoning subtype pool + tile variants.** Each of 7 kinds needs a building spawn pool + a grey-shade tile variant for paint preview. Inherits drift flag from toolbar block.
- **Sprite catalog gaps.** ~34 picker-card sprite slugs listed (9 R/C/I + 7 StateZoning + 1 Road + 6 Utility + 4 Landmark + 5 Forests + 2 arrows). Catalog audit must verify each row exists pre-bake or scaffold a placeholder family. Flag → catalog audit (consolidate with toolbar audit).
- **Children flattening at bake time.** JSON above shows a single `card-template` row, not literal cards. Bake handler must expand `children` per active variant into N actual rows. Specify expansion rule as part of bake schema. Flag → bake handler task.
- **Forests mode-button placement.** Mode buttons (`mode-single`, `mode-spray`) sit alongside cards or in a separate sub-zone? Need UX answer + visual mock. Flag → follow-up design poll.
- **Affordability tooltip copy localization.** "Cannot afford" + "Need $X / cell" need i18n surface — defer to localization pass. Flag.
- **Card swap animation.** When swapping tool families, do cards crossfade or hard-cut? Defer to motion pass.

---

### budget-panel

- **Role.** Full budget editor surfaced from HUD `budget-readout` click (D24). Single place where the player adjusts tax rates, scales service / utility / road funding, audits last monthly close + 3-month forecast, and views balance trend over time. Replaces the separate `budget-panel` historical concept (D24 collapsed graphs/demographics elsewhere; this panel = budget only).
- **Position.** Center modal. Centered horizontally + vertically; backdrop dim across entire viewport. HUD + toolbar + tool-subtype-picker remain visible behind dim but click-through blocked.
- **Modal size.** ~960 × 720 px (fixed). Internal layout = 2 × 2 grid of quadrants, ~440 × 280 each, with header strip (~40 px) + outer padding (24 px).
- **Sim policy.** Sim **pauses on open**, **resumes on close** (matches D25 pause-menu family but budget-panel is HUD-triggered, NOT a pause-menu sub-modal). Time-multiplier preserved; close restores to whatever speed was active before open.
- **Layout.** Header strip with `Budget · {month-year}` title (left) + `[X]` close icon (right). Below, 2 × 2 grid: `taxes` (top-left), `expenses` (top-right), `monthly-close` (bottom-left), `trend` (bottom-right). No tabs. No scroll inside the modal.
- **Quadrant — taxes.** 4 horizontal sliders (R / C / I / StateZoning). Range 0–20 %, step 0.5 %, live commit on drag (no save button). Numeric readout on the left of each slider. Edits feed sim immediately and reflect in the `monthly-close` forecast preview.
- **Quadrant — expenses.** 11 funding rows (police / fire / edu / health / parks / public-housing / public-offices from D9 + power / water / sewage from D15 + roads). Each row = funding slider 0–100 % (5 % step) + last-month $ spent readout. Sim consumes funding; lowering = reduced effectiveness + reduced cost. Edits commit live.
- **Quadrant — monthly-close.** Last-closed-month block (in / out / net / balance) + 3-month forecast preview. Forecast computed live from current slider state — recomputes as the player drags taxes / expenses sliders. Forecast horizon fixed at 3 months. Numbers + delta arrows; no chart inside this quadrant.
- **Quadrant — trend.** Stacked-area chart of expense categories (services bundle / utilities bundle / roads) with revenue line on top, time on x-axis. 3-position range selector (3mo / 12mo / all-time) — default 12mo, matches D13 stats-panel canon. No pan / zoom. Tooltip on hover shows month + per-category $ + revenue.
- **Theme.** Cream body background, tan border, indigo accents. Dim backdrop = `color.bg.cream` at low alpha (TBD token). Section frames inside use 1 px tan border + soft inner padding.
- **Layer.** `z.modal` (30) — above `z.hud` (10) + `z.toast` (20). Below `z.overlay` (40).
- **Header strip.** Title text `Budget · {month}` left-aligned (binds to current sim month). `[X]` close button right-aligned (40 × 40 px hit target).
- **Close triggers.** `[X]` click + ESC key + click on dimmed backdrop. All three commit pending edits live (no separate save) and resume sim. Inside-modal clicks are absorbed (no close).
- **Open trigger.** Click on HUD `budget-readout` cell. Open animation = 150 ms fade-in + 0.95 → 1.0 scale.
- **Affordance / read-only state.** No write access while sim is replaying a save (TBD post-MVP). MVP = always editable when open.
- **No hotkeys.** Mouse + ESC only.
- **No nested modals.** Budget-panel does NOT spawn sub-modals. All edits inline.

#### JSON (DB shape)

```json
{
  "slug": "budget-panel",
  "fields": {
    "layout_template": "modal-grid-2x2",
    "layout": "modal",
    "gap_px": 24,
    "padding_json": "{\"top\":24,\"left\":24,\"right\":24,\"bottom\":24}",
    "params_json": "{\"anchor\":\"center\",\"modal_w_px\":960,\"modal_h_px\":720,\"backdrop_dim\":true,\"backdrop_click_closes\":true,\"sim_pause_on_open\":true,\"hidden_default\":true,\"open_trigger\":\"hud-bar.budget-readout-click\",\"close_triggers\":[\"key.escape\",\"close-button-click\",\"backdrop-click\"]}"
  },
  "panel": {
    "slug": "budget-panel",
    "layout_template": "modal-grid-2x2",
    "layout": "modal",
    "gap_px": 24,
    "padding_json": "{\"top\":24,\"left\":24,\"right\":24,\"bottom\":24}",
    "params_json": "{\"anchor\":\"center\",\"modal_w_px\":960,\"modal_h_px\":720,\"backdrop_dim\":true,\"backdrop_click_closes\":true,\"sim_pause_on_open\":true,\"hidden_default\":true,\"open_trigger\":\"hud-bar.budget-readout-click\",\"close_triggers\":[\"key.escape\",\"close-button-click\",\"backdrop-click\"]}"
  },
  "children": [
    { "ord":  1, "kind": "label",      "params_json": "{\"kind\":\"label\",\"bind\":\"budget.headerTitle\",\"format\":\"Budget · {month-year}\"}",                                                                       "sprite_ref": "", "layout_json": "{\"zone\":\"header\",\"col\":\"left\"}",  "instance_slug": "budget-panel-header-title" },
    { "ord":  2, "kind": "button",     "params_json": "{\"icon\":\"close\",\"kind\":\"icon-button\",\"role\":\"modal-close\",\"action\":\"action.budget-close\"}",                                                          "sprite_ref": "", "layout_json": "{\"zone\":\"header\",\"col\":\"right\"}", "instance_slug": "budget-panel-close-button" },

    { "ord": 10, "kind": "section",    "params_json": "{\"kind\":\"quadrant\",\"title\":\"Taxes\"}",                                                                                                                       "sprite_ref": "", "layout_json": "{\"zone\":\"grid\",\"row\":1,\"col\":1}", "instance_slug": "budget-panel-taxes" },
    { "ord": 11, "kind": "slider-row", "params_json": "{\"label\":\"R\",\"bind\":\"budget.taxRate.r\",\"min\":0,\"max\":20,\"step\":0.5,\"format\":\"{v}%\",\"action\":\"action.budget-set-tax\",\"action_payload\":\"r\"}", "sprite_ref": "", "layout_json": "{\"zone\":\"taxes\",\"ord\":1}",         "instance_slug": "budget-panel-taxes-r" },
    { "ord": 12, "kind": "slider-row", "params_json": "{\"label\":\"C\",\"bind\":\"budget.taxRate.c\",\"min\":0,\"max\":20,\"step\":0.5,\"format\":\"{v}%\",\"action\":\"action.budget-set-tax\",\"action_payload\":\"c\"}", "sprite_ref": "", "layout_json": "{\"zone\":\"taxes\",\"ord\":2}",         "instance_slug": "budget-panel-taxes-c" },
    { "ord": 13, "kind": "slider-row", "params_json": "{\"label\":\"I\",\"bind\":\"budget.taxRate.i\",\"min\":0,\"max\":20,\"step\":0.5,\"format\":\"{v}%\",\"action\":\"action.budget-set-tax\",\"action_payload\":\"i\"}", "sprite_ref": "", "layout_json": "{\"zone\":\"taxes\",\"ord\":3}",         "instance_slug": "budget-panel-taxes-i" },
    { "ord": 14, "kind": "slider-row", "params_json": "{\"label\":\"S\",\"bind\":\"budget.taxRate.s\",\"min\":0,\"max\":20,\"step\":0.5,\"format\":\"{v}%\",\"action\":\"action.budget-set-tax\",\"action_payload\":\"s\"}", "sprite_ref": "", "layout_json": "{\"zone\":\"taxes\",\"ord\":4}",         "instance_slug": "budget-panel-taxes-s" },

    { "ord": 20, "kind": "section",    "params_json": "{\"kind\":\"quadrant\",\"title\":\"Expenses\"}",                                                                                                                    "sprite_ref": "", "layout_json": "{\"zone\":\"grid\",\"row\":1,\"col\":2}", "instance_slug": "budget-panel-expenses" },
    { "ord": 21, "kind": "expense-row","params_json": "{\"category\":\"police\",\"bind_funding\":\"budget.funding.police\",\"bind_spent\":\"budget.spent.police\",\"min\":0,\"max\":100,\"step\":5}",                       "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":1}",      "instance_slug": "budget-panel-expenses-police" },
    { "ord": 22, "kind": "expense-row","params_json": "{\"category\":\"fire\",\"bind_funding\":\"budget.funding.fire\",\"bind_spent\":\"budget.spent.fire\",\"min\":0,\"max\":100,\"step\":5}",                              "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":2}",      "instance_slug": "budget-panel-expenses-fire" },
    { "ord": 23, "kind": "expense-row","params_json": "{\"category\":\"edu\",\"bind_funding\":\"budget.funding.edu\",\"bind_spent\":\"budget.spent.edu\",\"min\":0,\"max\":100,\"step\":5}",                                  "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":3}",      "instance_slug": "budget-panel-expenses-edu" },
    { "ord": 24, "kind": "expense-row","params_json": "{\"category\":\"health\",\"bind_funding\":\"budget.funding.health\",\"bind_spent\":\"budget.spent.health\",\"min\":0,\"max\":100,\"step\":5}",                        "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":4}",      "instance_slug": "budget-panel-expenses-health" },
    { "ord": 25, "kind": "expense-row","params_json": "{\"category\":\"parks\",\"bind_funding\":\"budget.funding.parks\",\"bind_spent\":\"budget.spent.parks\",\"min\":0,\"max\":100,\"step\":5}",                            "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":5}",      "instance_slug": "budget-panel-expenses-parks" },
    { "ord": 26, "kind": "expense-row","params_json": "{\"category\":\"public-housing\",\"bind_funding\":\"budget.funding.public-housing\",\"bind_spent\":\"budget.spent.public-housing\",\"min\":0,\"max\":100,\"step\":5}", "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":6}",      "instance_slug": "budget-panel-expenses-public-housing" },
    { "ord": 27, "kind": "expense-row","params_json": "{\"category\":\"public-offices\",\"bind_funding\":\"budget.funding.public-offices\",\"bind_spent\":\"budget.spent.public-offices\",\"min\":0,\"max\":100,\"step\":5}", "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":7}",      "instance_slug": "budget-panel-expenses-public-offices" },
    { "ord": 28, "kind": "expense-row","params_json": "{\"category\":\"power\",\"bind_funding\":\"budget.funding.power\",\"bind_spent\":\"budget.spent.power\",\"min\":0,\"max\":100,\"step\":5}",                            "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":8}",      "instance_slug": "budget-panel-expenses-power" },
    { "ord": 29, "kind": "expense-row","params_json": "{\"category\":\"water\",\"bind_funding\":\"budget.funding.water\",\"bind_spent\":\"budget.spent.water\",\"min\":0,\"max\":100,\"step\":5}",                            "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":9}",      "instance_slug": "budget-panel-expenses-water" },
    { "ord": 30, "kind": "expense-row","params_json": "{\"category\":\"sewage\",\"bind_funding\":\"budget.funding.sewage\",\"bind_spent\":\"budget.spent.sewage\",\"min\":0,\"max\":100,\"step\":5}",                          "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":10}",     "instance_slug": "budget-panel-expenses-sewage" },
    { "ord": 31, "kind": "expense-row","params_json": "{\"category\":\"roads\",\"bind_funding\":\"budget.funding.roads\",\"bind_spent\":\"budget.spent.roads\",\"min\":0,\"max\":100,\"step\":5}",                            "sprite_ref": "", "layout_json": "{\"zone\":\"expenses\",\"ord\":11}",     "instance_slug": "budget-panel-expenses-roads" },

    { "ord": 40, "kind": "section",    "params_json": "{\"kind\":\"quadrant\",\"title\":\"Monthly close\"}",                                                                                                                "sprite_ref": "", "layout_json": "{\"zone\":\"grid\",\"row\":2,\"col\":1}", "instance_slug": "budget-panel-monthly" },
    { "ord": 41, "kind": "readout-block","params_json": "{\"role\":\"last-month\",\"binds\":[\"budget.lastMonth.in\",\"budget.lastMonth.out\",\"budget.lastMonth.net\",\"budget.lastMonth.balance\"]}",                       "sprite_ref": "", "layout_json": "{\"zone\":\"monthly\",\"row\":1}",        "instance_slug": "budget-panel-monthly-last" },
    { "ord": 42, "kind": "readout-block","params_json": "{\"role\":\"forecast\",\"horizon_months\":3,\"binds\":[\"budget.forecast.month1\",\"budget.forecast.month2\",\"budget.forecast.month3\"],\"recompute_on\":\"slider-edit\"}", "sprite_ref": "", "layout_json": "{\"zone\":\"monthly\",\"row\":2}",        "instance_slug": "budget-panel-monthly-forecast" },

    { "ord": 50, "kind": "section",    "params_json": "{\"kind\":\"quadrant\",\"title\":\"Trend\"}",                                                                                                                        "sprite_ref": "", "layout_json": "{\"zone\":\"grid\",\"row\":2,\"col\":2}", "instance_slug": "budget-panel-trend" },
    { "ord": 51, "kind": "chart",      "params_json": "{\"chart_kind\":\"stacked-area\",\"series\":[\"services\",\"utilities\",\"roads\"],\"overlay_line\":\"revenue\",\"x_bind\":\"budget.history.months\",\"y_bind\":\"budget.history.byCategory\",\"range_default\":\"12mo\"}", "sprite_ref": "", "layout_json": "{\"zone\":\"trend\",\"row\":1}",         "instance_slug": "budget-panel-trend-chart" },
    { "ord": 52, "kind": "range-tabs", "params_json": "{\"options\":[\"3mo\",\"12mo\",\"all-time\"],\"default\":\"12mo\",\"bind\":\"budget.trend.range\",\"action\":\"action.budget-set-trend-range\"}",                       "sprite_ref": "", "layout_json": "{\"zone\":\"trend\",\"row\":2}",         "instance_slug": "budget-panel-trend-range" }
  ],
  "notes": "Children flatten 1 header + 1 close-btn + 4 sections + 4 tax slider-rows + 11 expense-rows + 2 readout-blocks + 1 chart + 1 range-tabs = 25 child rows. Bake handler must support new kinds: section, slider-row, expense-row, readout-block, chart, range-tabs."
}
```

#### Wiring contract

| Surface                | Slug / id                                                                                                                                                                                                            | Source of truth                                                              |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Sprites                | `close` (header X icon)                                                                                                                                                                                              | `catalog_sprite`                                                             |
| Tokens                 | `color.bg.cream`, `color.border.tan`, `color.icon.indigo`, `color.text.dark`, `gap.default`, `gap.loose`, `pad.button`, `z.modal`                                                                                    | `tokens` block §1                                                            |
| Archetypes             | `slider-row` (label + slider + numeric readout, NEW), `expense-row` (label + slider + spent-readout, NEW), `readout-block` (multi-line label/value pairs, NEW), `chart` (stacked-area + line overlay + range-tabs, NEW), `section` (titled quadrant frame, NEW), `range-tabs` (pill-segmented control, NEW), `icon-button` (close button, EXISTING) | `catalog_archetype`                                                          |
| Actions                | `action.budget-open` (HUD trigger), `action.budget-close`, `action.budget-set-tax` (payload `{family: 'r' \| 'c' \| 'i' \| 's', value: number}`), `action.budget-set-funding` (payload `{category: string, value: number}`), `action.budget-set-trend-range` (payload `{range: '3mo' \| '12mo' \| 'all-time'}`) | `UiActionRegistry` (TBD)                                                     |
| Binds — taxes          | `budget.taxRate.r`, `budget.taxRate.c`, `budget.taxRate.i`, `budget.taxRate.s` (number, %)                                                                                                                            | runtime bind dispatcher (TBD)                                                |
| Binds — funding        | `budget.funding.{police,fire,edu,health,parks,public-housing,public-offices,power,water,sewage,roads}` (number, %)                                                                                                   | bind dispatcher                                                              |
| Binds — last-month     | `budget.lastMonth.{in,out,net,balance}` (number, $)                                                                                                                                                                  | bind dispatcher                                                              |
| Binds — spent          | `budget.spent.{police,fire,edu,health,parks,public-housing,public-offices,power,water,sewage,roads}` (number, $)                                                                                                     | bind dispatcher                                                              |
| Binds — forecast       | `budget.forecast.month1`, `budget.forecast.month2`, `budget.forecast.month3` (number, $)                                                                                                                              | bind dispatcher (recomputed on slider edit)                                  |
| Binds — trend          | `budget.history.months` (array of months), `budget.history.byCategory` (matrix), `budget.trend.range` (enum)                                                                                                          | bind dispatcher                                                              |
| Binds — header         | `budget.headerTitle` (formatted string from sim month)                                                                                                                                                                | bind dispatcher                                                              |
| Hotkeys                | None inside the panel; ESC closes (handled by modal close-trigger, not a hotkey binding).                                                                                                                            | —                                                                            |
| Verification hooks     | After bake: `findobjectoftype_scan` → `budget-panel` exists, hidden by default. Test-mode `budget-panel-open`: click HUD `budget-readout` → modal mounts + sim pauses + 4 quadrants render + default tax sliders match seed. `budget-panel-edit`: drag R-tax slider → forecast updates within frame. `budget-panel-close`: ESC → modal unmounts + sim resumes at prior speed. | `unity:testmode-batch` scenarios `budget-panel-open` + `budget-panel-edit` + `budget-panel-close` (TBD) |
| Variant transitions    | `hidden → mounting` on HUD click. `mounting → idle` on open animation end. `idle → editing-tax` on tax-slider drag-start. `editing-tax → idle` on slider-mouse-up + forecast recompute. Mirrored for funding sliders + range-tabs. `idle → unmounting` on any close trigger. `unmounting → hidden` after fade-out, sim resume committed. | runtime dispatcher                                                           |

#### Drift / open questions (post-lock code tasks)

- **6 new archetypes.** `slider-row`, `expense-row`, `readout-block`, `chart`, `section`, `range-tabs` all missing from `catalog_archetype`. Need archetype rows + bake-handler kind cases. Flag → archetype + bake task (largest single-panel archetype debt so far).
- **Action registry expansion.** 5 new actions with typed payloads (`set-tax`, `set-funding`, `set-trend-range` need scoped payloads). Reuses action-registry design task already flagged in toolbar + picker locks. Consolidate into single design task.
- **Bind dispatcher pattern subscriptions.** `budget.funding.*` + `budget.spent.*` are wildcard families (11 keys each). Same pattern as `toolSelection.affordable.*`. Bind dispatcher must support wildcard subscriptions.
- **Forecast service.** `budget.forecast.month{1,2,3}` recompute on slider edit requires a forecast simulator separate from the main monthly-tick simulator. New service: `BudgetForecaster` or similar. Flag → sim spec task.
- **Stacked-area chart primitive.** D34 already flagged 3 chart primitives for stats-panel demographics tab (histogram + age-pyramid + bar-chart). Trend quadrant adds a 4th: stacked-area + overlay line. Flag → chart primitive expansion.
- **Trend bind shape.** `budget.history.byCategory` is a matrix (months × categories). Bind dispatcher must support array / matrix payloads, not just scalars. Flag → bind-payload-shape spec.
- **Pause-on-open mechanism.** D33 / `TimeManager` does not yet expose a "modal pauses sim, restore on close" idiom. New: `TimeManager.SetModalPauseOwner(string)` / `ClearModalPauseOwner(string)` so multiple modals stack pause requests. Flag → TimeManager API extension.
- **Backdrop dim token.** No `color.bg.dim` token yet. Add a backdrop-overlay token (e.g. `color.bg.dim` `#0008`) to `tokens` block §1.
- **Sim-month string format.** Header `Budget · {month-year}` requires a `MonthFormatter` reading `TimeManager.GetCurrentDate()`. Flag → small util.
- **Modal stacking with pause-menu.** D25 pause-menu is the in-game hub modal. If player Pause → opens budget elsewhere → opens settings sub-modal: budget cannot stack with pause-menu in MVP. Decision: budget-panel is HUD-triggered, NOT a pause-menu sub-modal — closes itself before pause-menu opens. Document as Interactions-section rule when that section is grilled.
- **Industrial sub-tax.** Tax slider for `i` is a single rate. D4 splits Industrial into Agri / Manuf / Tech subtypes. Should each have its own tax rate, or share? MVP = share (one slider). Flag → post-MVP tax granularity.
- **Save-on-exit behavior.** Live-commit means autosave snapshot on every drag is too noisy. Autosave cadence governed by D14 settings; budget edits do NOT force a save. Flag → confirm with autosave spec.
- **Read-only state during replay / cutscenes.** Future post-MVP. No surface in MVP.

---

### stats-panel

**Role.** HUD-triggered center modal that absorbs the legacy Graphs / Demographics / CityStats tabs (D24). Read-only window into trends, population composition, and service coverage. Opens from a (yet-to-be-added) `hud-bar-stats-button`.

**Anchor + sim policy.** Center-anchored modal, 720 × 520 px, dark backdrop overlay (`color.bg.dim`), tan-bordered card. Sim pauses on open via `TimeManager.SetModalPauseOwner('stats-panel')`; restores on close. Mutually exclusive with `budget-panel` and `pause-menu`.

**Header strip.** 32 px tall. Title `Stats · {month-year}` (MonthFormatter) on the left. Close X (24 × 24 px) on the right. Tab strip sits directly under the header.

**Tab strip.** 3 tabs, equal width, top of body: `Graphs` / `Demographics` / `Services`. Active tab = pressed-cream + bottom highlight. Default = `Graphs` on first open per session; sticks to last selection within session. No keyboard tab cycling in MVP.

**Tab body — Graphs.** Single 3-position range selector at top (`3mo` / `12mo` / `all-time`). Body = 3 stacked line-chart rows top-to-bottom: Population, Money (net cash), Employment-rate. Each chart row ≈ 130 px tall, full body width. Reuses chart primitive shared with `budget-panel`. Range selector value persists across tab switches within session.

**Tab body — Demographics.** 3 stacked-bar rows top-to-bottom: R/C/I composition (R / C / I %), Density tiers (Light / Medium / Heavy %), Wealth tiers (Low / Mid / High %). Each row = label-left + numeric breakdown + horizontal stacked bar. Numeric labels right-aligned per segment. New archetype `stacked-bar-row`.

**Tab body — Services.** 10 rows, one per service: Police / Fire / Edu / Health / Parks / PublicHousing / PublicOffices / Power / Water / Sewage / Roads (Roads counted as a service for parity with budget funding sliders — final list TBD post-MVP). Each row = service icon + name + coverage % + horizontal progress bar (color-coded green ≥ 70 / yellow 40–69 / red < 40). New archetype `service-row` (or extend `expense-row` semantically).

**Close triggers.** X click + ESC + backdrop click. No nested modals. Closing restores sim resume via `TimeManager.ClearModalPauseOwner('stats-panel')`.

**Open trigger.** `hud-bar-stats-button` (NOT yet in baked hud-bar snapshot — flag below). Bound to action `stats.open`. Closing emits `stats.close`.

**No hotkeys.** No global hotkey opens stats-panel in MVP.

#### JSON DB shape — stats-panel

```jsonc
{
  "slug": "stats-panel",
  "fields": {
    "layout_template": "modal",
    "layout": "vstack",
    "params_json": "{\"width\":720,\"height\":520,\"backdrop\":\"color.bg.dim\",\"modalPauseOwner\":\"stats-panel\"}"
  },
  "children": [
    { "ord":  1, "kind": "label",         "instance_slug": "stats-panel-header-title",        "params_json": "{\"kind\":\"label\",\"binds\":\"stats.headerTitle\"}",                          "layout_json": "{\"zone\":\"header-left\"}" },
    { "ord":  2, "kind": "button",        "instance_slug": "stats-panel-close-button",        "params_json": "{\"icon\":\"close\",\"kind\":\"icon-button\",\"action\":\"modal.close\"}",        "layout_json": "{\"zone\":\"header-right\"}" },
    { "ord":  3, "kind": "tab-strip",     "instance_slug": "stats-panel-tabs",                "params_json": "{\"tabs\":[\"graphs\",\"demographics\",\"services\"],\"default\":\"graphs\",\"binds\":\"stats.activeTab\",\"action\":\"stats.tabSet\"}", "layout_json": "{\"zone\":\"tabbar\"}" },

    { "ord": 10, "kind": "range-tabs",    "instance_slug": "stats-panel-graphs-range",        "params_json": "{\"tabs\":[\"3mo\",\"12mo\",\"all\"],\"default\":\"12mo\",\"binds\":\"stats.graphs.range\",\"action\":\"stats.graphs.rangeSet\"}", "layout_json": "{\"zone\":\"graphs-header\"}" },
    { "ord": 11, "kind": "chart",         "instance_slug": "stats-panel-graphs-population",   "params_json": "{\"type\":\"line\",\"label\":\"Population\",\"binds\":\"stats.graphs.population\"}",  "layout_json": "{\"zone\":\"graphs-body\"}" },
    { "ord": 12, "kind": "chart",         "instance_slug": "stats-panel-graphs-money",        "params_json": "{\"type\":\"line\",\"label\":\"Money\",\"binds\":\"stats.graphs.money\"}",            "layout_json": "{\"zone\":\"graphs-body\"}" },
    { "ord": 13, "kind": "chart",         "instance_slug": "stats-panel-graphs-employment",   "params_json": "{\"type\":\"line\",\"label\":\"Employment\",\"binds\":\"stats.graphs.employment\"}",  "layout_json": "{\"zone\":\"graphs-body\"}" },

    { "ord": 20, "kind": "stacked-bar-row","instance_slug": "stats-panel-demog-composition",  "params_json": "{\"label\":\"Composition\",\"segments\":[\"r\",\"c\",\"i\"],\"binds\":\"stats.demog.composition\"}", "layout_json": "{\"zone\":\"demog-body\"}" },
    { "ord": 21, "kind": "stacked-bar-row","instance_slug": "stats-panel-demog-density",      "params_json": "{\"label\":\"Density\",\"segments\":[\"light\",\"medium\",\"heavy\"],\"binds\":\"stats.demog.density\"}", "layout_json": "{\"zone\":\"demog-body\"}" },
    { "ord": 22, "kind": "stacked-bar-row","instance_slug": "stats-panel-demog-wealth",       "params_json": "{\"label\":\"Wealth\",\"segments\":[\"low\",\"mid\",\"high\"],\"binds\":\"stats.demog.wealth\"}", "layout_json": "{\"zone\":\"demog-body\"}" },

    { "ord": 30, "kind": "service-row",   "instance_slug": "stats-panel-svc-police",          "params_json": "{\"icon\":\"police\",\"label\":\"Police\",\"binds\":\"stats.services.police\"}",       "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 31, "kind": "service-row",   "instance_slug": "stats-panel-svc-fire",            "params_json": "{\"icon\":\"fire\",\"label\":\"Fire\",\"binds\":\"stats.services.fire\"}",            "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 32, "kind": "service-row",   "instance_slug": "stats-panel-svc-edu",             "params_json": "{\"icon\":\"edu\",\"label\":\"Education\",\"binds\":\"stats.services.edu\"}",       "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 33, "kind": "service-row",   "instance_slug": "stats-panel-svc-health",          "params_json": "{\"icon\":\"health\",\"label\":\"Health\",\"binds\":\"stats.services.health\"}",   "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 34, "kind": "service-row",   "instance_slug": "stats-panel-svc-parks",           "params_json": "{\"icon\":\"parks\",\"label\":\"Parks\",\"binds\":\"stats.services.parks\"}",     "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 35, "kind": "service-row",   "instance_slug": "stats-panel-svc-public-housing",  "params_json": "{\"icon\":\"public-housing\",\"label\":\"Public Housing\",\"binds\":\"stats.services.public-housing\"}", "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 36, "kind": "service-row",   "instance_slug": "stats-panel-svc-public-offices",  "params_json": "{\"icon\":\"public-offices\",\"label\":\"Public Offices\",\"binds\":\"stats.services.public-offices\"}", "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 37, "kind": "service-row",   "instance_slug": "stats-panel-svc-power",           "params_json": "{\"icon\":\"power\",\"label\":\"Power\",\"binds\":\"stats.services.power\"}",     "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 38, "kind": "service-row",   "instance_slug": "stats-panel-svc-water",           "params_json": "{\"icon\":\"water\",\"label\":\"Water\",\"binds\":\"stats.services.water\"}",     "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 39, "kind": "service-row",   "instance_slug": "stats-panel-svc-sewage",          "params_json": "{\"icon\":\"sewage\",\"label\":\"Sewage\",\"binds\":\"stats.services.sewage\"}",   "layout_json": "{\"zone\":\"services-body\"}" },
    { "ord": 40, "kind": "service-row",   "instance_slug": "stats-panel-svc-roads",           "params_json": "{\"icon\":\"roads\",\"label\":\"Roads\",\"binds\":\"stats.services.roads\"}",     "layout_json": "{\"zone\":\"services-body\"}" }
  ]
}
```

#### Wiring contract — stats-panel

| Channel | Surface | Notes |
| --- | --- | --- |
| `bake_requirements` | new archetypes: `tab-strip`, `chart` (shared with budget), `range-tabs` (shared), `stacked-bar-row`, `service-row` | `tab-strip` switches body container visibility per active tab; archetype carries 3 child slots one per tab. `service-row` could be an extension of `expense-row` — bake-time decision. |
| `actions_referenced` | `stats.open` · `stats.close` · `stats.tabSet` · `stats.graphs.rangeSet` · `modal.close` | `modal.close` is shared with budget-panel. `stats.open` fired by `hud-bar-stats-button`. |
| `binds_referenced` | `stats.headerTitle` · `stats.activeTab` · `stats.graphs.range` · `stats.graphs.population` · `stats.graphs.money` · `stats.graphs.employment` · `stats.demog.composition` · `stats.demog.density` · `stats.demog.wealth` · `stats.services.{police,fire,edu,health,parks,public-housing,public-offices,power,water,sewage,roads}` | Charts are series + range; demog rows are 3-segment percent vectors; service rows are `{coverage, status}` records. |
| `hotkeys` | none | No global hotkey opens stats-panel in MVP. ESC closes when open. |
| `verification_hooks` | open via `stats.open` → modal-pause owner asserted; close via X/ESC/backdrop → owner cleared; tab switch repaints body without remount; range switch on graphs swaps series window in place; service-row coverage threshold drives bar color | Bridge hook needed for asserting modal-pause owner stack state. |
| `variant_transitions` | `tab=graphs` ⇄ `tab=demographics` ⇄ `tab=services` (active-tab rotation); per-row coverage tier `green` / `yellow` / `red` (threshold-driven recolor) | No subtype hierarchy — flat rotation. |

#### Drift items + open questions — stats-panel

- **HUD trigger missing.** Locked hud-bar snapshot has no `hud-bar-stats-button`. Adding it requires re-opening the hud-bar lock or appending to its center / right zone. Flag → hud-bar amendment (post-Phase 1) to add stats trigger between `budget-graph-button` and `map-button`, or after `map-button`.
- **Tab-strip archetype.** New archetype. Active state = pressed-cream + bottom highlight. Bake template should support N tabs with one default. Shared candidate for future panels (info-panel).
- **stacked-bar-row archetype.** New archetype. Layout = label + numeric segments + horizontal bar made of N segments with per-segment color (token `color.demog.{r,c,i}` / `color.demog.{light,medium,heavy}` / `color.demog.{low,mid,high}`). Tokens missing — flag → token catalog addition.
- **service-row archetype.** New archetype OR semantic extension of `expense-row`. Bake decision: keep distinct (`service-row` reads coverage %, `expense-row` reads $ spent vs budgeted) for clarity. Coverage-tier thresholds (≥ 70 / 40–69 / < 40) live as tokens or component constants — flag → threshold-token decision.
- **Chart primitive shared with budget-panel.** Chart catalog now: budget stacked-area + 3 stats line-charts. Confirm bake-time chart kind enum: `line` / `stacked-area` / future. Flag → chart enum lock.
- **Range-tabs reuse.** `range-tabs` archetype now used by both budget-panel (trend quadrant) and stats-panel (graphs tab). Shared. Single bake template.
- **Action registry expansion.** New actions: `stats.open` · `stats.close` · `stats.tabSet` · `stats.graphs.rangeSet`. Plus `modal.close` is now shared. Flag → action registry.
- **Bind registry expansion.** New bind families: `stats.headerTitle` · `stats.activeTab` · `stats.graphs.*` · `stats.demog.*` · `stats.services.*`. Many are series — bind dispatcher must support array / time-series payloads. Flag → bind dispatcher pattern.
- **Service list final shape.** Services tab lists 11 entries (Police / Fire / Edu / Health / Parks / PublicHousing / PublicOffices / Power / Water / Sewage / Roads). Roads-as-service is debatable. Flag → confirm Roads inclusion + ordering with sim-services spec when written.
- **CityStats data source.** Stats values come from `CityStats` (existing) + `EconomyManager` history. New: `StatsHistoryRecorder` service that snapshots monthly aggregates into a ring buffer (3mo / 12mo / all-time). Flag → new sim service.
- **Modal stacking exclusion.** stats-panel + budget-panel + pause-menu mutually exclusive. Opening any closes the others. Document in `## Interactions` when grilled.
- **No keyboard tab cycling.** Confirmed MVP. Future: Tab / Shift-Tab to rotate active tab.
- **Default tab persistence.** Per-session memory only (RAM). Save-game does not persist `stats.activeTab`. Flag → confirm with save-game spec.
- **Empty-state rendering.** Brand-new city has < 1 month of data. Charts render flat / empty grid until first month tick. Flag → empty-state visual spec.
- **Backdrop dim token.** `color.bg.dim` token first introduced by budget-panel. stats-panel reuses. No new token here.
- **TimeManager modal-pause stack.** Reuses `TimeManager.SetModalPauseOwner` API flagged in budget-panel drift. Owners stack: opening stats while budget is open is forbidden by exclusion rule, so single-owner stack is sufficient.
- **i18n.** Tab labels + service names + chart axis labels are user-facing strings. Flag → string-table.
- **Motion.** Modal slide-fade-in same as budget. Tab switch = body cross-fade 80 ms. Flag → motion spec.

---

### map-panel

**Role.** Always-on persistent HUD minimap. Top-down render of the playable city tile region with toggleable overlay layers + click-to-jump main-camera control. NOT a modal — sim runs while it is visible. `hud-bar-map-button` toggles its visibility (open ⇄ collapsed).

**Anchor + sim policy.** Bottom-right corner of viewport, anchored with 24 px right + bottom margins. 360 × 360 px square. Sim continues running. NO modal-pause owner. Mutually compatible with all modals — minimap is a HUD widget that sits behind any active modal backdrop.

**Default state.** Open by default (matches existing `MiniMapController.Awake` `m_IsActive = 1`). Player toggles via `hud-bar-map-button` → action `minimap.toggle` → `SetActive(!isActive)`. Hidden state = no minimap, no header, no render. Re-open restores last-used layer mix + viewport rect.

**Content scope.** City-only top-down render — playable tile region rendered at fixed scale to fit 360 × 360. Water always rendered as base. NO region / neighbor cities (RegionScene is a separate scene).

**Layers.** 5 multi-select layers — `Streets` · `Zones` · `Forests` · `Desirability` · `Centroid`. Defaults active = `Streets` + `Zones`. Layers composite on top of the always-on water base. Implementation = `MiniMapLayer` enum in `MiniMapController.cs` lines 18–26 + render switch in `GetCellColor` lines 304–354.

**Layer-toggle UI.** Header strip top of minimap, ~ 36 px tall. Row of 5 icon-only toggle buttons (one per layer) reusing `illuminated-button` archetype. Active = pressed-cream + outline. Multi-select. Tooltip on hover shows layer name. Header takes 36 px → effective render area 360 × 324 px.

**Click-to-jump.** Click anywhere inside the render area → `cameraController.MoveCameraToMapCenter(grid)` recenters main camera on the clicked grid position. Black viewport rectangle drawn live on the minimap showing main-camera frustum (`UpdateViewportRect` lines 444–484). Cyan ring overlay drawn at urban-centroid ring boundaries when `Centroid` layer is active.

**Drag-rect to pan (NEW).** Pointer-down on the viewport rectangle + drag → continuously pans the main camera so the rect tracks the pointer. Pointer-down outside the rect = jump-then-no-drag (existing behavior). Adds `OnDrag` handler in `MiniMapController` + new `cameraController.PanCameraTo(grid)` method (or reuse `MoveCameraToMapCenter` per drag tick).

**No close button.** Visibility owned by `hud-bar-map-button` only. No X inside the minimap header — preserves the 5 layer-toggle slots.

**Hotkeys.** None in MVP.

#### JSON DB shape — map-panel

```jsonc
{
  "slug": "map-panel",
  "fields": {
    "layout_template": "minimap",
    "layout": "vstack",
    "params_json": "{\"width\":360,\"height\":360,\"anchor\":\"bottom-right\",\"marginRight\":24,\"marginBottom\":24,\"defaultActive\":true}"
  },
  "children": [
    { "ord":  1, "kind": "button", "instance_slug": "map-panel-layer-streets",      "params_json": "{\"icon\":\"layer-streets\",\"kind\":\"illuminated-button\",\"toggle\":true,\"binds\":\"minimap.layer.streets\",\"action\":\"minimap.layer.set\",\"actionPayload\":\"streets\"}",         "layout_json": "{\"zone\":\"header\"}" },
    { "ord":  2, "kind": "button", "instance_slug": "map-panel-layer-zones",        "params_json": "{\"icon\":\"layer-zones\",\"kind\":\"illuminated-button\",\"toggle\":true,\"binds\":\"minimap.layer.zones\",\"action\":\"minimap.layer.set\",\"actionPayload\":\"zones\"}",             "layout_json": "{\"zone\":\"header\"}" },
    { "ord":  3, "kind": "button", "instance_slug": "map-panel-layer-forests",      "params_json": "{\"icon\":\"layer-forests\",\"kind\":\"illuminated-button\",\"toggle\":true,\"binds\":\"minimap.layer.forests\",\"action\":\"minimap.layer.set\",\"actionPayload\":\"forests\"}",         "layout_json": "{\"zone\":\"header\"}" },
    { "ord":  4, "kind": "button", "instance_slug": "map-panel-layer-desirability", "params_json": "{\"icon\":\"layer-desirability\",\"kind\":\"illuminated-button\",\"toggle\":true,\"binds\":\"minimap.layer.desirability\",\"action\":\"minimap.layer.set\",\"actionPayload\":\"desirability\"}", "layout_json": "{\"zone\":\"header\"}" },
    { "ord":  5, "kind": "button", "instance_slug": "map-panel-layer-centroid",     "params_json": "{\"icon\":\"layer-centroid\",\"kind\":\"illuminated-button\",\"toggle\":true,\"binds\":\"minimap.layer.centroid\",\"action\":\"minimap.layer.set\",\"actionPayload\":\"centroid\"}",     "layout_json": "{\"zone\":\"header\"}" },
    { "ord": 10, "kind": "minimap-canvas", "instance_slug": "map-panel-render",     "params_json": "{\"binds\":\"minimap.render\",\"viewportBind\":\"minimap.viewport.rect\",\"action\":\"minimap.click\",\"dragAction\":\"minimap.drag\"}",                                              "layout_json": "{\"zone\":\"body\"}" }
  ]
}
```

#### Wiring contract — map-panel

| Channel | Surface | Notes |
| --- | --- | --- |
| `bake_requirements` | new archetype `minimap-canvas` (texture target + viewport-rect overlay + pointer-event passthrough); `illuminated-button` reused for layer toggles with new `toggle: true` payload semantic | `minimap-canvas` is a Unity-side `RawImage` driven by `MiniMapController` — bake template only needs to wire the GameObject + RectTransform + click/drag forwarders. |
| `actions_referenced` | `minimap.toggle` (fired by `hud-bar-map-button`) · `minimap.layer.set` (fired by header buttons, payload = layer slug) · `minimap.click` (fired by canvas pointer-up, payload = grid coord) · `minimap.drag` (fired by canvas pointer-drag on rect, payload = grid coord stream) | `hud-bar-map-button` action assignment is currently undefined — flag below. |
| `binds_referenced` | `minimap.layer.streets` · `minimap.layer.zones` · `minimap.layer.forests` · `minimap.layer.desirability` · `minimap.layer.centroid` (each = bool, drives toggle visual) · `minimap.render` (texture pixels driven by `MiniMapController.RebuildTexture`) · `minimap.viewport.rect` (rect screen-space coords for the viewport overlay) · `minimap.visible` (bool, drives root SetActive) | Bind dispatcher must support `RectTransform` rect payloads + texture-data refresh signals. |
| `hotkeys` | none | No MVP hotkeys. Future: L1–L5 to toggle layers. |
| `verification_hooks` | hud MAP click → `minimap.visible` flips; layer toggle click → `minimap.layer.{slug}` flips → render area rebuilds; canvas click → main-camera transform jumps to clicked grid; drag-on-rect → main-camera pans continuously; centroid layer active → cyan ring renders at ring boundaries; viewport rect reflects current main-camera frustum | Bridge tool stub needed: `unity_minimap_state_get` returns `{visible, layersActive[], viewportRectGrid, lastClickedGrid}`. |
| `variant_transitions` | minimap `visible=true` ⇄ `visible=false`; per-layer `active=true` ⇄ `active=false` (multi-select rotation); centroid layer ON adds cyan ring overlay; click vs drag mode dispatch per pointer event | No subtype hierarchy. |

#### Drift items + open questions — map-panel

- **`hud-bar-map-button` rewiring.** Button exists in baked hud-bar snapshot (ord 9, center zone) but has no documented action. Lock: assign action `minimap.toggle`. Flag → hud-bar action-payload registry update + `MiniMapController.SetVisible(bool)` API addition (currently only Awake-time activation in `MiniMapController.Awake` lines 134–145).
- **`minimap-canvas` archetype.** New archetype for the texture surface. Existing `MiniMapController` already owns the `RawImage` + click handler; bake template just needs to wire the GameObject + forward pointer events to the controller. Consider whether to expose as a generic `texture-canvas` archetype reusable for future inset views.
- **Header strip layout.** Existing prefab is body-only (`mini-map.prefab` 600 × 800 normalized to 360 × 360 in Awake). Adding a 36 px header strip on top means the render area shrinks from 360 × 360 to 360 × 324 OR the total widget grows to 360 × 396. Decision: keep total widget at 360 × 360, render area shrinks to 324 px tall. Flag → `MiniMapController.Awake` size enforcement update + texture aspect renormalization.
- **Layer-toggle multi-select state machine.** Existing `MiniMapController` already has `MiniMapLayer` enum + per-layer flags. Wiring header buttons to those flags requires exposing `MiniMapController.SetLayerActive(MiniMapLayer, bool)` (or equivalent) as the action handler. Flag → controller API addition.
- **Default-active layers persistence.** First open of new game = Streets + Zones active. Subsequent toggles persist for the session (RAM). Save-game does not persist layer mix in MVP. Flag → save-game spec confirmation.
- **Drag-rect to pan (NEW code).** Existing `OnPointerClick` jumps camera. New: `OnDrag` handler that pans camera continuously while pointer-down inside viewport rect. Flag → `MiniMapController.OnDrag` implementation + new `CameraController.PanCameraTo(grid)` (or per-tick `MoveCameraToMapCenter` calls).
- **Drag start zone.** Drag starts INSIDE the viewport rect → pan; OUTSIDE → existing jump-on-pointer-up. Need a hit-test against `minimap.viewport.rect` on pointer-down. Flag → input routing update.
- **Toggling minimap with drag-in-flight.** If player presses `hud-bar-map-button` mid-drag → cancel drag + hide. Flag → drag-state cleanup on visibility toggle.
- **Layer icon sprites.** 5 NEW sprite slugs needed: `layer-streets` · `layer-zones` · `layer-forests` · `layer-desirability` · `layer-centroid`. Flag → sprite catalog audit.
- **Action registry expansion.** New actions: `minimap.toggle` · `minimap.layer.set` · `minimap.click` · `minimap.drag`. Flag → action registry.
- **Bind registry expansion.** New bind families: `minimap.layer.{streets,zones,forests,desirability,centroid}` · `minimap.render` · `minimap.viewport.rect` · `minimap.visible`. Flag → bind dispatcher pattern.
- **Tooltip dispatch on layer buttons.** Hover shows layer name. Reuses `illuminated-button` tooltip mechanism if/when defined; otherwise adds tooltip primitive. Flag → tooltip primitive lock (cross-cutting with toolbar).
- **Centroid cyan-ring overlay.** Tied to `Centroid` layer being active. Drawn by `MiniMapController` lines TBD. No new wiring — already implemented. Document only.
- **Modal coexistence.** Minimap remains visible BEHIND the dim backdrop when budget-panel / stats-panel / pause-menu are open. Pointer events should be blocked by the modal backdrop (clicks on the minimap area while a modal is open dismiss the modal per backdrop-click rule). Flag → confirm in `## Interactions` grilling.
- **Pause-mode rendering.** Sim is paused while a modal is open → minimap should still render the last frame. No extra logic — minimap re-renders on tick anyway. Document only.
- **Region map (post-MVP).** Full RegionScene minimap variant for the region screen is out of scope for CityScene. Flag → post-MVP region-minimap variant.
- **i18n.** Layer-button tooltips are user-facing strings. Flag → string-table.
- **Motion.** Toggle hide / show = instant SetActive in MVP (no fade). Flag → motion spec confirmation.

---

### info-panel

**Role.** Right-edge inspect dock for the currently-selected world thing (zoned-building / road / utility-tile / forest / bare-cell / landmark). Auto-opens on non-empty world click; renders type-specific big card content + inline Demolish action for demolish-able selections. Sim runs while it is visible — NOT a modal.

**Anchor + sim policy.** Right-edge dock anchored top-right under hud-bar, full remaining viewport height, fixed 320 px wide. Sim continues running. NO modal-pause owner. Mutually compatible with HUD widgets (minimap stays visible to its left); any centered modal (budget-panel / stats-panel / pause-menu) renders on top with backdrop dimming the info dock.

**Selection-type catalog.** 6 detection types driven by `CityCell.zoneType` + tile classification:

| Type | Detection | Demolish-able |
| --- | --- | --- |
| `zoned-building` | `zoneType` ∈ R/C/I light/medium/heavy + cell has placed building | Yes |
| `road` | `zoneType == Road` | Yes |
| `utility-tile` | `zoneType` ∈ StateService variants (power / water plants + lines) | Yes |
| `forest` | `zoneType == Forest` | No |
| `bare-cell` | `zoneType == Grass` (or unzoned) — only when no tool active | No |
| `landmark` | landmark flag on cell (post-MVP placeholder; rare in MVP) | No |

**Card content per type (big card).** Each type renders its own field set in a single vertical scroll column. Template = header (icon + name + type tag) → field list → action zone:

```
┌──────────────────────────────┐
│ [icon]  Residential Heavy    │   ← header (instance name + type tag)
│         R3 · cell (12, 7)    │
├──────────────────────────────┤
│ Population      120 / 150   │
│ Jobs            —            │
│ Power           +0 / -8      │
│ Water           -4           │
│ Happiness       72 %         │
│ Desirability    58           │
│ Land value      $1.2k        │
├──────────────────────────────┤
│ [   Demolish   ]             │   ← inline confirm slot
└──────────────────────────────┘
```

Field sets per type:

- **`zoned-building`** — population (cur / cap), jobs (filled / available), power (+gen / −use), water (−use), happiness %, desirability score, land value.
- **`road`** — segment length, condition %, traffic load (0–1), connected-to (count of buildings reachable).
- **`utility-tile`** — capacity (units produced / consumed), coverage radius (cells), connection state (online / offline / overloaded).
- **`forest`** — cluster size (cells), biome tag, desirability bonus contribution.
- **`bare-cell`** — current zone label (Grass / unzoned), terrain tag (flat / slope / coast), zoneability (yes / no + reason).
- **`landmark`** — landmark name, effect summary (1 line), persistent-or-event flag.

**Demolish action (inline confirm).** Shown only for demolish-able types (`zoned-building` / `road` / `utility-tile`). First click swaps button to red `Confirm demolish` for 3 s; second click within 3 s fires `world.demolish` action wired to `GridManager.HandleBulldozerMode` (line 519). Outside 3 s window, button reverts. Hidden for `forest` / `bare-cell` / `landmark`.

**Open / close trigger.**
- **Auto-open paths.** Plain click on any non-empty tile when no tool active → opens with that tile's content. `Alt + click` on any tile when a tool IS active → opens (without firing the tool). All other tool-active plain clicks fire the tool, info card stays as-is (no open, no close).
- **Selection swap.** Click a different selectable thing while card is open → content re-renders in-place, no animation, no slide. Card stays docked.
- **Close paths (4).** (1) Explicit `X` button in header. (2) `ESC` key (only when no centered modal is open — modals own ESC first). (3) Click an empty / non-selectable tile (terrain water etc.). (4) Selection swap to another selectable thing replaces content (functionally a content close).

**Hotkeys.** `ESC` closes when no modal active. `Alt+Click` is the inspect modifier when any tool is active.

#### JSON DB shape — info-panel

```jsonc
{
  "slug": "info-panel",
  "fields": {
    "layout_template": "info-dock",
    "layout": "vstack",
    "params_json": "{\"width\":320,\"anchor\":\"top-right\",\"underHudBar\":true,\"verticalScroll\":true,\"defaultActive\":false}"
  },
  "children": [
    { "ord":  1, "kind": "icon",        "instance_slug": "info-panel-type-icon",       "params_json": "{\"binds\":\"info.selection.icon\"}",                                                    "layout_json": "{\"zone\":\"header\"}" },
    { "ord":  2, "kind": "label",       "instance_slug": "info-panel-name-label",      "params_json": "{\"kind\":\"label\",\"binds\":\"info.selection.name\"}",                                  "layout_json": "{\"zone\":\"header\"}" },
    { "ord":  3, "kind": "label",       "instance_slug": "info-panel-type-tag-label",  "params_json": "{\"kind\":\"label\",\"variant\":\"caption\",\"binds\":\"info.selection.typeTag\"}",       "layout_json": "{\"zone\":\"header\"}" },
    { "ord":  4, "kind": "button",      "instance_slug": "info-panel-close-button",    "params_json": "{\"icon\":\"close\",\"kind\":\"illuminated-button\",\"action\":\"info.close\"}",          "layout_json": "{\"zone\":\"header\"}" },
    { "ord": 10, "kind": "field-list",  "instance_slug": "info-panel-field-list",      "params_json": "{\"binds\":\"info.selection.fields\"}",                                                  "layout_json": "{\"zone\":\"body\"}" },
    { "ord": 20, "kind": "button",      "instance_slug": "info-panel-demolish-button", "params_json": "{\"label\":\"Demolish\",\"kind\":\"illuminated-button\",\"variant\":\"danger\",\"action\":\"info.demolish.confirm\",\"visibilityBind\":\"info.selection.demolishable\",\"confirmTimeoutMs\":3000}", "layout_json": "{\"zone\":\"footer\"}" }
  ]
}
```

#### Wiring contract — info-panel

| Channel | Surface | Notes |
| --- | --- | --- |
| `bake_requirements` | new archetypes `info-dock` (root container with right-edge anchor + scroll) + `field-list` (renders array of `{label, value}` rows from a single bind); reuses `illuminated-button` (header close + footer Demolish) + `icon` + `label` | `field-list` archetype is the per-type content engine — adapter rebuilds rows when `info.selection.fields` changes. |
| `actions_referenced` | `info.close` (footer X click) · `info.demolish.confirm` (Demolish click — first click stages confirm, second click within 3 s fires `world.demolish`) · `world.demolish` (terminal action wrapping `GridManager.HandleBulldozerMode`) · `world.select` (fired by world-click; payload = `{gridX, gridY, modifierAlt}`) · `world.deselect` (fired by empty-tile click or ESC) | `world.select` + `world.deselect` are scene-level actions emitted by `GridManager.Update` selection logic, not info-panel buttons. |
| `binds_referenced` | `info.selection.icon` · `info.selection.name` · `info.selection.typeTag` · `info.selection.fields` (array of `{label, value}` per type) · `info.selection.demolishable` (bool, drives Demolish visibility) · `info.visible` (bool, drives root SetActive) | Bind dispatcher must support array payloads for `info.selection.fields` driving `field-list` row count + content. |
| `hotkeys` | `ESC` → `info.close` (only when no centered modal active) · `Alt+Click` → inspect modifier when tool active | Hotkey priority: centered-modal ESC > info-panel ESC. |
| `verification_hooks` | non-empty world click → `info.visible` flips true + `info.selection.*` binds populate; selection swap → `info.selection.*` binds re-populate without flipping `info.visible`; `Alt+Click` while tool active → opens info card AND tool does NOT fire; empty-tile click → `info.visible` flips false; demolish first click → button re-renders red confirm state + 3 s timer; demolish second click within 3 s → `world.demolish` fires + cell clears + `info.visible` flips false; ESC with modal active → modal closes, info card stays | Bridge tool stub needed: `unity_info_panel_state_get` returns `{visible, selectionType, selectionGrid, demolishable, demolishConfirming}`. |
| `variant_transitions` | info-panel `visible=true` ⇄ `visible=false`; per-selection-type field-list re-render (6 type variants); demolish button `idle` ⇄ `confirming` (3 s) ⇄ fired-or-reverted; close button hover / pressed states | No subtype hierarchy — selection type drives field-list content variant. |

#### Drift items + open questions — info-panel

- **Existing thin readout deprecation.** `DetailsPopupController` + `OnCellInfoShown` event + 5-tuple (cellType / zoneType / population / landValue / pollution) is the current implementation; new info-panel REPLACES it with type-aware big card + inline Demolish. Flag → migrate `InfoPanelDataAdapter` from event-listener to bind-dispatch model + retire `DetailsPopupController`.
- **`info-dock` archetype.** New archetype: right-edge dock with top-right anchor under hud-bar, fixed 320 px width, full remaining height, vertical scroll on overflow. Flag → archetype catalog addition + bake template.
- **`field-list` archetype.** New archetype: bind-driven row repeater rendering `{label, value}` array. Each row uses two label slots (left = label, right = value). Flag → archetype catalog addition + adapter row pooling pattern.
- **Selection-type detection logic.** Six-way type detection from `CityCell.zoneType` + building presence + landmark flag. Currently inline in `GridManager.HandleShowTileDetails` (lines 384–409) returns a partial 5-tuple. Flag → extract `WorldSelectionResolver` returning `{type, fields[]}` per click; consumed by info-panel adapter.
- **Per-type field set adapters.** 6 field-set builders (one per type) reading from `CityCell` + manager queries (`EmploymentManager.JobData`, power/water managers, etc.). Flag → adapter wiring per type + manager API audit.
- **Inline demolish confirm pattern.** New 3 s confirm-on-double-click pattern not used elsewhere. Flag → confirm-button primitive (could be reused for retire / delete actions later) + animation spec.
- **Demolish wiring.** `info.demolish.confirm` second-click → `GridManager.HandleBulldozerMode(selectedGrid)`. Currently bulldozer is a tool-active mode toggled from toolbar. Need a tool-mode-independent demolish entry point. Flag → `GridManager.DemolishAt(grid)` direct API or programmatic mode-set + click.
- **Alt+Click inspect modifier.** Plain click fires tool when tool active; `Alt+Click` opens info card without firing tool. Flag → input routing in `GridManager.Update` adds modifier check + emits `world.select` instead of tool fire.
- **ESC priority.** Centered modal owns ESC first; info-panel ESC only when no modal active. Flag → hotkey-stack pattern (already exists for budget / stats / pause modals).
- **Selection swap behavior.** Click new selectable while card open → content re-renders, no animation, no fade. Flag → adapter `OnSelectionChanged` re-populates binds; root SetActive stays true.
- **Empty-tile close.** Click on water / non-selectable terrain → `info.visible` flips false. Flag → world-click router must classify empty-tile vs selectable.
- **Modal backdrop coexistence.** Centered modal opens → backdrop dims info dock + blocks pointer; info card content stays rendered behind. Flag → confirm in `## Interactions` grilling (mirrors map-panel).
- **Auto-open vs no-tool-active interaction.** Bare-cell click with no tool active → opens info card with bare-cell content. Bare-cell click with tool active → tool fires, no info card. Flag → world-click router decision tree.
- **Building action stubs (post-MVP).** Future: Upgrade button + transit-to-this button + production graphs inline. Currently only Demolish. Flag → action zone extensibility in `field-list` footer.
- **`info-panel-type-icon` sprite catalog.** 6 type icons needed: `info-icon-building` · `info-icon-road` · `info-icon-utility` · `info-icon-forest` · `info-icon-bare-cell` · `info-icon-landmark`. Flag → sprite catalog audit.
- **Action registry expansion.** New actions: `info.close` · `info.demolish.confirm` · `world.select` · `world.deselect` · `world.demolish`. Flag → action registry.
- **Bind registry expansion.** New bind families: `info.selection.icon` · `info.selection.name` · `info.selection.typeTag` · `info.selection.fields` (array) · `info.selection.demolishable` · `info.visible`. Flag → bind dispatcher pattern + array-bind support.
- **Field-list row scrollability.** When per-type field count exceeds dock height, vertical scroll engages on the body. Header + footer (Demolish action zone) stay pinned. Flag → scroll component + sticky header / footer.
- **i18n.** Type tags + field labels + Demolish button text are user-facing. Flag → string-table.
- **Motion.** Open / close = instant SetActive in MVP (no slide). Selection swap = instant re-render. Demolish confirm = button color tween + 3 s countdown bar. Flag → motion spec confirmation.

---

### main-menu

**Role.** Pre-game title-screen surface. Single panel with 5 root buttons (Continue / New Game / Load / Settings / Quit) that swaps its center content between root / new-game-form / load-list / settings sub-views. Entry point into CityScene; pause-menu's "Main menu" returns here. Lives in its own scene (`MainMenu.unity`, build index 0); CityScene = build index 1.

**Anchor + sim policy.** Full-screen panel; no sim running yet (title screen, no game state). Plain themed cream/sand background — full-screen color fill from `color.bg.menu` token. No hero art, no audio, no live preview in MVP. Static layout, no animation on bg.

**Layout (locked).**
- Title strip top-center: game name large (`size.text.title-display`).
- Branding strip bottom-left + bottom-right corners: studio name `Bacayo Studio` (left) + version string `v0.X.Y` (right). Both small, muted (`color.text.muted`).
- Center area: vertical button stack centered horizontally + vertically. Buttons full-width within 320 px column. 12 px gap between buttons. 5 buttons in fixed order top-to-bottom: Continue / New Game / Load / Settings / Quit.
- Sub-view: same center area swaps in sub-view content (form fields / list / settings rows). Title + branding strips stay constant across all views.

**Navigation model (locked).** Single panel, content-swap navigation. `main-menu.contentScreen` enum bind drives which view fills center: `root` / `new-game-form` / `load-list` / `settings`. Click a root button → bind flips → center re-renders. No prefab swap, no modal stack. Title + branding never re-render.

**Back navigation (locked).** Persistent back-arrow button top-left of every sub-view (NOT visible on root). Click back → `contentScreen` flips to `root`. ESC key bound to same action when on sub-view; ESC on root = no-op (not a modal — nothing to close).

**Button behaviors (locked).**
| Button | Action | Notes |
| --- | --- | --- |
| Continue | `mainmenu.continue` → auto-load most-recent save → fade out → CityScene | Disabled (greyed) when no save exists; tooltip override `No save found`. No intermediate splash — instant transition. |
| New Game | `mainmenu.openNewGame` → `contentScreen = new-game-form` | In-place swap. Form Submit → load CityScene with new-game params. |
| Load | `mainmenu.openLoad` → `contentScreen = load-list` | In-place swap. List item click → load that save → CityScene. |
| Settings | `mainmenu.openSettings` → `contentScreen = settings` | In-place swap. Same settings-view shared with pause-menu. |
| Quit | `mainmenu.quit.confirm` (1st click) → `mainmenu.quit` (2nd within 3 s) → `Application.Quit` | Inline 3 s confirm; reuses `ConfirmButton` primitive shared with info-panel demolish + pause-menu Quit / Main-menu. Editor branch: `EditorApplication.ExitPlaymode`. |

**View reuse with pause-menu.** `settings-view` + `load-list-view` are reusable subpanels. From main-menu they swap into the menu surface. From pause-menu they open as nested modal content-replacement. Single source of truth for fields + layout. Bake emits one shared `settings-view` + `load-list-view` archetype; host (main-menu OR pause-menu) determines mount point + decoration.

**New-game-form view.** Locked separately under `### new-game-form` (next phase 1 surface). Hosted by main-menu in MVP; no other host.

**Continue-disabled detection.** On main-menu open: scan `Application.persistentDataPath` for `*.json` save files (existing `GameSaveManager.GetSaveFiles()` API). Empty list → `mainmenu.continue.enabled = false` bind → Continue button greys + tooltip override engages. Non-empty → enabled, click loads `GameSaveManager.GetMostRecentSave()`.

**Quit confirm.** Identical pattern to info-panel demolish + pause-menu Quit. 1st click: button morphs to red bg + countdown bar (3 s) + label `Click again to confirm`. 2nd click within 3 s → `Application.Quit`. Timeout → reverts to default.

**Pause-menu return path.** When CityScene's pause-menu fires `pause.toMainMenu` (after its own 3 s confirm), runtime calls `SceneManager.LoadScene(0)` → main-menu loads fresh. Player must Save before returning or unsaved progress is lost (matches pause-menu confirm copy).

**Existing implementation reused.**
- Adapter: `Assets/Scripts/UI/Modals/MainMenuController.cs` (existing — drives 5 buttons, scene index 0/1 logic).
- Sub-adapters: `NewGameScreenDataAdapter.cs` · `SaveLoadScreenDataAdapter.cs` (mode = Load) · `SettingsScreenDataAdapter.cs` (all reusable across main-menu + pause-menu hosts).
- Save layer: `GameSaveManager.GetSaveFiles()` + `GameSaveManager.GetMostRecentSave()` + `GameSaveManager.LoadGame()`.
- Quit confirm primitive: shared `ConfirmButton` (info-panel demolish + pause-menu).

#### JSON DB shape — main-menu

```jsonc
{
  "panels": {
    "main-menu": {
      "layout_template": "fullscreen-stack",
      "layout": "fullscreen-stack",
      "params_json": "{\"bg_color_token\":\"color.bg.menu\"}",
      "children": [
        { "ord": 1, "kind": "label",          "instance_slug": "main-menu-title-label",       "params_json": "{\"size_token\":\"size.text.title-display\",\"zone\":\"top\"}" },
        { "ord": 2, "kind": "label",          "instance_slug": "main-menu-studio-label",      "params_json": "{\"size_token\":\"size.text.caption\",\"color_token\":\"color.text.muted\",\"zone\":\"bottom-left\"}" },
        { "ord": 3, "kind": "label",          "instance_slug": "main-menu-version-label",     "params_json": "{\"size_token\":\"size.text.caption\",\"color_token\":\"color.text.muted\",\"zone\":\"bottom-right\"}" },
        { "ord": 4, "kind": "button",         "instance_slug": "main-menu-continue-button",   "params_json": "{\"kind\":\"primary-button\",\"label\":\"Continue\",\"action\":\"mainmenu.continue\",\"disabled_bind\":\"mainmenu.continue.disabled\",\"tooltip\":\"Resume your most recent city.\",\"tooltip_override_when_disabled\":\"No save found.\",\"zone\":\"center\"}" },
        { "ord": 5, "kind": "button",         "instance_slug": "main-menu-new-game-button",   "params_json": "{\"kind\":\"primary-button\",\"label\":\"New Game\",\"action\":\"mainmenu.openNewGame\",\"tooltip\":\"Start a new city.\",\"zone\":\"center\"}" },
        { "ord": 6, "kind": "button",         "instance_slug": "main-menu-load-button",       "params_json": "{\"kind\":\"primary-button\",\"label\":\"Load\",\"action\":\"mainmenu.openLoad\",\"tooltip\":\"Open the load list.\",\"zone\":\"center\"}" },
        { "ord": 7, "kind": "button",         "instance_slug": "main-menu-settings-button",   "params_json": "{\"kind\":\"primary-button\",\"label\":\"Settings\",\"action\":\"mainmenu.openSettings\",\"tooltip\":\"Open settings.\",\"zone\":\"center\"}" },
        { "ord": 8, "kind": "confirm-button", "instance_slug": "main-menu-quit-button",       "params_json": "{\"kind\":\"destructive-confirm-button\",\"label\":\"Quit\",\"confirm_label\":\"Click again to confirm\",\"confirm_window_ms\":3000,\"action_confirm\":\"mainmenu.quit.confirm\",\"action\":\"mainmenu.quit\",\"tooltip\":\"Exit to desktop.\",\"zone\":\"center\"}" },
        { "ord": 9, "kind": "button",         "instance_slug": "main-menu-back-button",       "params_json": "{\"kind\":\"icon-button\",\"icon\":\"back-arrow\",\"action\":\"mainmenu.back\",\"visible_bind\":\"mainmenu.back.visible\",\"tooltip\":\"Back to menu.\",\"zone\":\"top-left\"}" },
        { "ord": 10, "kind": "view-slot",     "instance_slug": "main-menu-content-slot",      "params_json": "{\"slot_bind\":\"mainmenu.contentScreen\",\"views\":[\"root\",\"new-game-form\",\"load-list\",\"settings\"],\"default\":\"root\",\"zone\":\"center\"}" }
      ]
    }
  }
}
```

#### Wiring contract — main-menu

```yaml
bake_requirements:
  - layout_template: fullscreen-stack
  - children: [title-label, studio-label, version-label, continue-button, new-game-button, load-button, settings-button, quit-button (confirm), back-button (sub-view only), content-slot]
  - shared_views: [new-game-form, load-list-view, settings-view]
  - reuses: ConfirmButton primitive (info-panel demolish, pause-menu)

actions_referenced:
  - mainmenu.continue        # auto-load most recent save → CityScene
  - mainmenu.openNewGame     # contentScreen = new-game-form
  - mainmenu.openLoad        # contentScreen = load-list
  - mainmenu.openSettings    # contentScreen = settings
  - mainmenu.back            # contentScreen = root
  - mainmenu.quit.confirm    # 1st click — stage 3s confirm
  - mainmenu.quit            # 2nd click within 3s — Application.Quit

binds_referenced:
  - mainmenu.contentScreen           # enum: root | new-game-form | load-list | settings
  - mainmenu.continue.disabled       # bool — true when no saves exist
  - mainmenu.back.visible            # bool — true when contentScreen != root
  - mainmenu.title.text              # string — game name
  - mainmenu.version.text            # string — v0.X.Y
  - mainmenu.studio.text             # string — Bacayo Studio

hotkeys:
  - ESC on sub-view → mainmenu.back
  - ESC on root → no-op

verification_hooks:
  - test:mainmenu.continue.disabled-when-no-saves
  - test:mainmenu.continue.enabled-when-saves-exist
  - test:mainmenu.contentSwap.new-game-form
  - test:mainmenu.contentSwap.load-list
  - test:mainmenu.contentSwap.settings
  - test:mainmenu.contentSwap.back-returns-to-root
  - test:mainmenu.quit.requires-2-clicks-within-3s
  - test:mainmenu.quit.confirm-times-out-after-3s
  - test:mainmenu.continue.loads-most-recent-save

variant_transitions:
  - root ⇄ new-game-form (via mainmenu.openNewGame / mainmenu.back)
  - root ⇄ load-list      (via mainmenu.openLoad / mainmenu.back)
  - root ⇄ settings       (via mainmenu.openSettings / mainmenu.back)
  - quit-button: idle → armed (3s countdown) → idle (timeout) | quit (2nd click)
```

**Drift flagged.**
- **Shared `settings-view` + `load-list-view` archetypes.** New archetype kind `view-slot` for content-swap mounts; `settings-view` + `load-list-view` + `new-game-form` reusable across main-menu + pause-menu hosts. Flag → archetype catalog audit + lock with `### settings-modal` / `### load-modal` / `### new-game-form`.
- **`mainmenu.contentScreen` enum-bind.** Mirrors `pause.contentScreen` from pause-menu. Reuse same enum-bind dispatcher pattern. Flag → bind dispatcher pattern.
- **`view-slot` primitive.** New archetype kind not yet in component catalog. Renders one of N declared sub-views based on enum bind value. Flag → component archetype audit.
- **`mainmenu.continue.disabled` detection.** Need lightweight `GameSaveManager.HasAnySave()` API (avoid scanning files on every bind read). Flag → `GameSaveManager` API audit.
- **`GameSaveManager.GetMostRecentSave()`.** Need API returning newest save by `File.GetLastWriteTime`. Flag → `GameSaveManager` API audit.
- **Pre-game scene split.** `MainMenu.unity` (build index 0) vs `CityScene.unity` (build index 1). Confirm `MainMenuController` does NOT load any sim managers (no GridManager / GeographyManager / etc. in MainMenu scene). Flag → MainMenu scene composition audit.
- **Background color token.** New token `color.bg.menu` (cream / sand). Flag → token catalog audit.
- **Title typography token.** New `size.text.title-display` (large display weight for game name). Flag → token catalog audit.
- **Version string source.** Reads `Application.version` (Unity Player Settings); studio name = const. Flag → version pipeline.
- **No music / SFX (MVP).** Audio system not wired in main-menu. Title-screen music deferred post-MVP. Flag → audio MVP scope.
- **No hero art (MVP).** Plain `color.bg.menu` fill only; static illustration / live CityScene preview / animated gradient deferred post-MVP. Flag → art MVP scope.
- **Action registry expansion.** New actions: `mainmenu.continue` · `mainmenu.openNewGame` · `mainmenu.openLoad` · `mainmenu.openSettings` · `mainmenu.back` · `mainmenu.quit.confirm` · `mainmenu.quit`. Flag → action registry.
- **Bind registry expansion.** New binds: `mainmenu.contentScreen` (enum) · `mainmenu.continue.disabled` (bool) · `mainmenu.back.visible` (bool) · `mainmenu.title.text` · `mainmenu.version.text` · `mainmenu.studio.text`. Flag → bind dispatcher pattern.
- **`MainMenuController` retrofit.** Existing controller already wires 5 buttons + scene-load logic. Needs refactor: drop direct `Application.Quit` call → route through `ConfirmButton` primitive; drop direct sub-screen prefab activate → drive via `mainmenu.contentScreen` bind. Flag → controller refactor audit.
- **Tooltip-override on Continue.** Per locked tooltip primitive: disabled state replaces default tooltip. Confirm tooltip bake includes both default + override strings. Flag → tooltip catalog audit.
- **i18n.** All button labels + title + studio + version-prefix + tooltip strings are user-facing. Flag → string-table.
- **Motion.** Open / close = instant scene transition. Sub-view swap = instant. Continue → CityScene = fade out current scene + fade in CityScene (TBD duration; reuse scene transition primitive if exists). Flag → motion spec confirmation + scene transition primitive audit.

---

### new-game-form

**Role.** Pre-game character-sheet for a new city. Hosted as a sub-view of `main-menu` via `view-slot` (NOT a top-level panel; NOT a modal; NOT reachable from in-game pause-menu). Player picks 3 params (map size · starting budget · city name), clicks Start, CityScene loads with those params. Locks D18 + D30.

**Anchor + sim policy.** Mounted into `main-menu-content-slot` when `mainmenu.contentScreen = new-game-form`. Inherits main-menu's full-screen frame: title strip + branding strips stay constant; back-arrow top-left visible. No sim running yet (pre-game).

**Fields (locked — D18).**
| Field | Control | Values | Default |
| --- | --- | --- | --- |
| Map size | 3 preset cards | Small 64×64 (4 096 cells) / Medium 128×128 (16 384 cells) / Large 256×256 (65 536 cells) | Medium |
| Starting budget | 3 preset chips | Tight $10 000 / Standard $50 000 / Generous $200 000 | Standard |
| City name | Single-line text input | 1–32 chars; allowlist `[A-Za-z0-9 \-]`; emoji + control chars stripped | Random pick from `city-name-pool-es` (100 fictional Spanish names) |

Seed is NOT exposed in UI (player choice — round 1). Generated server-side per new game; recorded in save metadata for reproducibility / debugging.

Difficulty is NOT exposed (D18 lock — single sim tuning across all new games).

**Layout (locked).**
- Top: back-arrow (inherited from main-menu host).
- Center column, 480 px wide, vertical flow:
  1. Section header `Map size` → row of 3 cards (160 × 120 px each, 12 px gap). Each card = icon + size label (Small/Medium/Large) + cell-count subtitle (e.g. `64×64 — 4 096 cells`).
  2. Section header `Starting budget` → row of 3 chips (140 × 56 px, 12 px gap). Each chip = label (Tight/Standard/Generous) + $ amount.
  3. Section header `City name` → text input field (full column width, 48 px tall) with placeholder = pre-rolled name + small `↻ Reroll name` icon-button on right edge.
  4. Start button (full column width, 56 px tall, primary-button kind, label `Start`).
- 24 px gap between sections.
- All sections always visible (no progressive disclosure).

**Selected state (locked).** Selected map-card + selected budget-chip render with cream-highlight bg (`color.bg.selected`) + 2 px tan border (`color.border.selected`) + dark text (`color.text.dark`). Unselected = default panel-card surface. Reuses toolbar pressed-active idiom.

**City-name input.**
- Pre-filled on form open with random pick from `city-name-pool-es` (100 names, see drift).
- Validation: live trim + char-allowlist filter; shows live char count `N/32` on right edge inside input.
- Empty input on Start click → re-roll auto-name silently (Start never blocked by empty name).
- `↻ Reroll name` icon-button → picks a different random name from pool (different from current; never same twice in a row).

**Start button (locked).**
- Always enabled (defaults pre-selected; name always non-empty after re-roll fallback).
- Click → `mainmenu.startNewGame` action with payload `{mapSize, budget, cityName, seed: <random>}` → fade out main-menu scene → CityScene loads → sim spins up with params.
- Instant load (no confirm). Non-destructive at title screen — no save to overwrite, no progress to lose.

**Back navigation.** Inherits main-menu host: top-left back arrow + ESC → `mainmenu.back` → `mainmenu.contentScreen = root`. Form state (selections + name) discarded on back.

**Existing implementation reused / refactored.**
- Adapter: `Assets/Scripts/UI/Modals/NewGameScreenDataAdapter.cs` exists. Current shape uses 2 sliders (map / seed) + scenario toggles — DOES NOT match locked design. Needs refactor: replace sliders + scenario toggles with 3-card map picker + 3-chip budget picker + city-name input; drop seed slider entirely; drop scenario toggles entirely.
- Prefab: `Assets/UI/Prefabs/Generated/new-game-screen.prefab` + `new-game.prefab` (placeholders; need full bake from new catalog row).
- Producer call: `MainMenuController.StartNewGame(mapSize, seed, scenarioIndex)` — refactor signature to `StartNewGame(mapSize, startingBudget, cityName, seed)`; drop scenarioIndex.

#### JSON DB shape — new-game-form

```jsonc
{
  "panels": {
    "new-game-form": {
      "layout_template": "vertical-form",
      "layout": "vertical-form",
      "host": "main-menu",
      "host_slot": "main-menu-content-slot",
      "params_json": "{\"width_px\":480,\"section_gap_px\":24}",
      "children": [
        { "ord": 1,  "kind": "label",            "instance_slug": "new-game-form-map-size-header",       "params_json": "{\"text\":\"Map size\",\"size_token\":\"size.text.section-header\"}" },
        { "ord": 2,  "kind": "card-picker",      "instance_slug": "new-game-form-map-size-picker",       "params_json": "{\"bind\":\"newgame.mapSize\",\"default\":\"medium\",\"layout\":\"hstack\",\"gap_px\":12,\"options\":[{\"value\":\"small\",\"label\":\"Small\",\"subtitle\":\"64\u00d764 \u2014 4 096 cells\",\"icon\":\"map-size-small\"},{\"value\":\"medium\",\"label\":\"Medium\",\"subtitle\":\"128\u00d7128 \u2014 16 384 cells\",\"icon\":\"map-size-medium\"},{\"value\":\"large\",\"label\":\"Large\",\"subtitle\":\"256\u00d7256 \u2014 65 536 cells\",\"icon\":\"map-size-large\"}]}" },
        { "ord": 3,  "kind": "label",            "instance_slug": "new-game-form-budget-header",         "params_json": "{\"text\":\"Starting budget\",\"size_token\":\"size.text.section-header\"}" },
        { "ord": 4,  "kind": "chip-picker",      "instance_slug": "new-game-form-budget-picker",         "params_json": "{\"bind\":\"newgame.budget\",\"default\":\"standard\",\"layout\":\"hstack\",\"gap_px\":12,\"options\":[{\"value\":\"tight\",\"label\":\"Tight\",\"subtitle\":\"$10 000\",\"amount\":10000},{\"value\":\"standard\",\"label\":\"Standard\",\"subtitle\":\"$50 000\",\"amount\":50000},{\"value\":\"generous\",\"label\":\"Generous\",\"subtitle\":\"$200 000\",\"amount\":200000}]}" },
        { "ord": 5,  "kind": "label",            "instance_slug": "new-game-form-city-name-header",      "params_json": "{\"text\":\"City name\",\"size_token\":\"size.text.section-header\"}" },
        { "ord": 6,  "kind": "text-input",       "instance_slug": "new-game-form-city-name-input",       "params_json": "{\"bind\":\"newgame.cityName\",\"max_length\":32,\"allowlist_regex\":\"[A-Za-z0-9 \\\\-]\",\"placeholder_pool\":\"city-name-pool-es\",\"show_char_count\":true,\"trailing_action\":{\"icon\":\"reroll\",\"action\":\"newgame.cityName.reroll\",\"tooltip\":\"Pick a new random name.\"}}" },
        { "ord": 7,  "kind": "button",           "instance_slug": "new-game-form-start-button",          "params_json": "{\"kind\":\"primary-button\",\"label\":\"Start\",\"action\":\"mainmenu.startNewGame\",\"tooltip\":\"Start a new city with these settings.\"}" }
      ]
    }
  },
  "pools": {
    "city-name-pool-es": {
      "kind": "string-pool",
      "lang": "es",
      "values_count": 100,
      "values_inline_sample": ["Bahía Bacayo", "Nueva Castilla del Mar", "San Lorenzo de los Robles", "Puerto Cendrales", "Villa Atalaya"]
    }
  }
}
```

#### Wiring contract — new-game-form

```yaml
bake_requirements:
  - layout_template: vertical-form
  - host: main-menu (mounted via main-menu-content-slot when mainmenu.contentScreen = new-game-form)
  - children: [map-size-header, map-size-picker (3 cards), budget-header, budget-picker (3 chips), city-name-header, city-name-input (with reroll trailing action), start-button]
  - new archetypes: card-picker, chip-picker, text-input
  - new pools: city-name-pool-es (100 fictional Spanish city names)
  - new sprite slugs: map-size-small, map-size-medium, map-size-large, reroll
  - new tokens: color.bg.selected, color.border.selected, color.text.dark, size.text.section-header

actions_referenced:
  - newgame.mapSize.set        # card click → bind.set
  - newgame.budget.set          # chip click → bind.set
  - newgame.cityName.reroll     # trailing-action click → re-roll from pool
  - mainmenu.startNewGame       # Start button click → load CityScene with payload
  - mainmenu.back               # ESC / back arrow (inherited from host)

binds_referenced:
  - newgame.mapSize             # enum: small | medium | large
  - newgame.budget              # enum: tight | standard | generous
  - newgame.cityName            # string

hotkeys:
  - ESC → mainmenu.back (inherited from host)

verification_hooks:
  - test:newgame.defaults-on-open (medium + standard + non-empty name)
  - test:newgame.mapSize.card-click-flips-bind
  - test:newgame.budget.chip-click-flips-bind
  - test:newgame.cityName.reroll-picks-different-value
  - test:newgame.cityName.allowlist-strips-emoji
  - test:newgame.cityName.max-length-32
  - test:newgame.cityName.empty-on-start-rerolls-silently
  - test:newgame.start.payload-shape (mapSize + budget + cityName + seed)
  - test:newgame.start.seed-is-random-per-game
  - test:newgame.back-discards-form-state

variant_transitions:
  - map-card: idle ⇄ selected (one selected at a time across the row)
  - budget-chip: idle ⇄ selected (one selected at a time across the row)
  - city-name-input: empty / typing / valid (validation reflected in char-count color)
```

**Drift flagged.**
- **NEW `card-picker` archetype.** Row of N cards, single-select, drives one enum bind. Used by map-size; reusable for any single-pick row. Flag → archetype catalog audit.
- **NEW `chip-picker` archetype.** Row of N chips, single-select, drives one enum bind. Like card-picker but smaller geometry. Reusable for budget + future short-list pickers. Flag → archetype catalog audit.
- **NEW `text-input` archetype.** Single-line input with bind, max-length, allowlist regex, placeholder pool, optional trailing action button. Currently no other panel needs text input — this is the first. Flag → archetype catalog audit + form primitive lock.
- **`placeholder_pool` mechanism.** `text-input` reads a string-pool ref on mount + picks random value. New pool kind `string-pool` (lang-tagged). Flag → catalog kind extension + pool resolver.
- **`city-name-pool-es` content authoring.** 100 fictional Spanish city names — needs creative authoring pass. Catalog row owns the full list; placeholder seed values in JSON above. Flag → name-pool authoring.
- **`MainMenuController.StartNewGame` signature refactor.** Current: `(mapSize, seed, scenarioIndex)`. Target: `(mapSize, startingBudget, cityName, seed)`. Drop `scenarioIndex`. Add `startingBudget` int + `cityName` string params. Wire to `EconomyManager.SetStartingFunds(int)` + `CityStats.SetCityName(string)`. Flag → controller refactor audit.
- **`NewGameScreenDataAdapter` refactor.** Current: 2 sliders + scenario toggles. Target: 3 enum binds (mapSize / budget / cityName) read from card-picker / chip-picker / text-input + writes payload via `MainMenuController.StartNewGame`. Replace inspector slots: drop `_mapSizeSlider` + `_seedSlider` + `_scenarioToggles`; add bind-driven dispatch. Flag → adapter refactor audit.
- **Cell-count → world-grid mapping.** 64 / 128 / 256 chosen as locked counts. Confirm `GridManager.SetGridSize(int width, int height)` accepts these AND that geography pipeline + chunk loader perf-test cleanly at 256×256. Flag → grid + geography perf audit at Large.
- **Starting budget wiring.** `EconomyManager` needs `SetStartingFunds(int)` API (or equivalent). Confirm existing `EconomyManager.startingTreasury` field is mutable pre-game-start. Flag → economy API audit.
- **City name persistence.** Confirm `CityStats.cityName` (or equivalent) accepts string + persists to save. Existing `hud-bar-city-name-label` reads from this surface. Flag → city-name surface trace.
- **Seed generation.** `mainmenu.startNewGame` payload includes `seed: <random>`. Source = `System.Random` at action-fire moment. Persist into save metadata for replay / debug. Flag → save metadata schema audit.
- **Pre-game vs post-game state separation.** Form binds (`newgame.*`) live in pre-game scene (`MainMenu.unity`); discarded on scene transition. Confirm bind dispatcher supports scene-scoped binds (no leak to CityScene). Flag → bind lifecycle audit.
- **Reroll never-twice-in-a-row policy.** `newgame.cityName.reroll` action must avoid picking same name as current. Simple: pick from pool minus current. Flag → reroll util.
- **Action registry expansion.** New actions: `newgame.mapSize.set` · `newgame.budget.set` · `newgame.cityName.reroll` · `mainmenu.startNewGame`. Flag → action registry.
- **Bind registry expansion.** New binds: `newgame.mapSize` (enum) · `newgame.budget` (enum) · `newgame.cityName` (string). Flag → bind dispatcher pattern.
- **Tooltip strings.** Start button + reroll button need tooltip copy. Flag → tooltip catalog.
- **i18n.** All section headers + card / chip labels + subtitles + Start label + tooltip strings + name pool are user-facing. Pool is Spanish-only by design (developer flavour) — does NOT translate. Other strings need string-table. Flag → string-table + i18n policy for in-game proper nouns.
- **Motion.** Section appearance = instant on view-mount. Card / chip selection = instant bg+border swap. Start click → main-menu fade out → CityScene fade in (shared scene-transition primitive flagged on main-menu lock).

---

### settings-view

**Role.** Shared sub-view archetype for player preferences. Mounted by main-menu (when `mainmenu.contentScreen = settings`) AND by pause-menu (when `pause.contentScreen = settings`). 9 controls across 3 sections: Gameplay (3) / Audio (3) / Display (3). Single source of truth for fields + layout — host wires mount point + back-destination only.

**Anchor + sim policy.** Inherits host frame. From main-menu: full-screen content slot, no sim. From pause-menu: nested inside pause-menu modal, sim paused (TimeManager modal-pause owner = `pause-menu`). Both hosts: back-arrow top-left visible.

**Sections + controls (locked).**

| Section | Control | Kind | Range / values | Default | PlayerPrefs key |
| --- | --- | --- | --- | --- | --- |
| Gameplay | Scroll-edge-pan | toggle | on / off | on | `ScrollEdgePanKey` (existing) |
| Gameplay | Monthly-budget notifications | toggle | on / off | on | `MonthlyBudgetNotificationsKey` (NEW) |
| Gameplay | Auto-save | toggle | on / off | on | `AutoSaveKey` (NEW) |
| Audio | Master volume | slider | 0–100 % | 80 | `MasterVolumeKey` (existing) |
| Audio | Music volume | slider | 0–100 % | 60 | `MusicVolumeKey` (existing) |
| Audio | SFX volume | slider | 0–100 % | 80 | `SfxVolumeDbKey` (existing in `BlipBootstrap` — needs surface) |
| Display | Resolution | dropdown | 1280×720 / 1920×1080 / 2560×1440 / 3840×2160 (hardcoded 4) | match closest current `Screen.currentResolution` | `ResolutionIndexKey` (existing) |
| Display | Fullscreen | toggle | on / off | on | `FullscreenKey` (existing) |
| Display | VSync | toggle | on / off | on | `VSyncKey` (existing) |

**Layout (locked).**
- Top: back-arrow (host-wired destination).
- Center column, 480 px wide, vertical flow:
  - Section header `Gameplay` → 3 control rows.
  - 24 px gap.
  - Section header `Audio` → 3 control rows.
  - 24 px gap.
  - Section header `Display` → 3 control rows.
  - 32 px gap.
  - Footer: `Reset to defaults` button (full column width, 48 px tall, secondary-button kind, inline 1 s confirm primitive).
- Each control row: 56 px tall = label (left) + control (right). Volume sliders show live `NN %` readout on right edge inside the slider.

**Apply mode (locked).** Instant apply. Each control change writes to PlayerPrefs immediately + dispatches the matching action (e.g. `settings.master.set` → `AudioListener.volume` + PlayerPrefs). No staging buffer, no Apply button, no Cancel — back arrow simply navigates away (changes already persisted).

**Reset to defaults (locked).** Footer button. 1 s inline confirm (lighter than 3 s destructive — settings reset is recoverable). On confirm: every control reverts to factory default + PlayerPrefs writes flush + every dependent system applies (audio mixer / Screen.SetResolution / `CameraController.scrollEdgePanEnabled`).

**Volume mapping (locked).** Slider value 0–100 % → internal dB curve via `LinearToDecibel(percent / 100f)` on each set. UI shows `NN %`; mixer / `AudioListener.volume` receives mapped value. Existing `SettingsScreenDataAdapter` already does this for Master / Music; SFX needs surface from `BlipBootstrap.SfxVolumeDbKey`.

**Resolution dropdown (locked).** Hardcoded 4 entries: `1280×720` · `1920×1080` · `2560×1440` · `3840×2160`. On open: pick closest match to `Screen.currentResolution.width` as default selection. On change: `Screen.SetResolution(w, h, fullscreen, refreshRate=Screen.currentResolution.refreshRate)`. Display refresh rate NOT user-controlled in MVP.

**Back navigation (locked, host-aware).**
- From main-menu host: back / ESC → `mainmenu.back` → `mainmenu.contentScreen = root`.
- From pause-menu host: back / ESC → `pause.back` → `pause.contentScreen = root`.
- Same primitive (settings-view emits abstract `settings.back` event); host-side adapter wires the destination.

**Existing implementation reused / refactored.**
- Adapter: `Assets/Scripts/UI/Modals/SettingsScreenDataAdapter.cs` (existing — wires Master / Music / Resolution / Fullscreen / VSync / Scroll-edge-pan to PlayerPrefs lines 11–101). Needs additions: SFX slider surface, monthly-budget-notifications toggle, auto-save toggle, Reset-to-defaults button. Existing inspector slots for the 6 existing controls preserved.
- PlayerPrefs keys: existing 6 (`MasterVolumeKey` · `MusicVolumeKey` · `ResolutionIndexKey` · `FullscreenKey` · `VSyncKey` · `ScrollEdgePanKey`) + 3 NEW (`SfxVolumeKey` reflowing existing dB key · `MonthlyBudgetNotificationsKey` · `AutoSaveKey`).
- Prefab: `Assets/UI/Prefabs/Generated/settings-screen.prefab` (placeholder; needs full bake from new catalog row).
- Volume mapping: `BlipBootstrap.SfxVolumeDbKey` already uses dB; reuse mapping helper for all 3 sliders.

#### JSON DB shape — settings-view

```jsonc
{
  "panels": {
    "settings-view": {
      "layout_template": "vertical-form",
      "layout": "vertical-form",
      "host_slots": ["main-menu-content-slot", "pause-menu-content-slot"],
      "params_json": "{\"width_px\":480,\"section_gap_px\":24}",
      "children": [
        { "ord": 1,  "kind": "label",         "instance_slug": "settings-view-gameplay-header",          "params_json": "{\"text\":\"Gameplay\",\"size_token\":\"size.text.section-header\"}" },
        { "ord": 2,  "kind": "toggle-row",    "instance_slug": "settings-view-scroll-edge-pan-toggle",   "params_json": "{\"label\":\"Scroll-edge-pan\",\"bind\":\"settings.scrollEdgePan\",\"action\":\"settings.scrollEdgePan.set\",\"prefs_key\":\"ScrollEdgePanKey\",\"default\":true}" },
        { "ord": 3,  "kind": "toggle-row",    "instance_slug": "settings-view-monthly-notif-toggle",     "params_json": "{\"label\":\"Monthly-budget notifications\",\"bind\":\"settings.monthlyBudgetNotifications\",\"action\":\"settings.monthlyBudgetNotifications.set\",\"prefs_key\":\"MonthlyBudgetNotificationsKey\",\"default\":true}" },
        { "ord": 4,  "kind": "toggle-row",    "instance_slug": "settings-view-auto-save-toggle",         "params_json": "{\"label\":\"Auto-save\",\"bind\":\"settings.autoSave\",\"action\":\"settings.autoSave.set\",\"prefs_key\":\"AutoSaveKey\",\"default\":true}" },
        { "ord": 5,  "kind": "label",         "instance_slug": "settings-view-audio-header",             "params_json": "{\"text\":\"Audio\",\"size_token\":\"size.text.section-header\"}" },
        { "ord": 6,  "kind": "slider-row",    "instance_slug": "settings-view-master-slider",            "params_json": "{\"label\":\"Master volume\",\"bind\":\"settings.masterVolume\",\"action\":\"settings.masterVolume.set\",\"prefs_key\":\"MasterVolumeKey\",\"min\":0,\"max\":100,\"step\":1,\"default\":80,\"readout_suffix\":\"%\",\"db_mapping\":true}" },
        { "ord": 7,  "kind": "slider-row",    "instance_slug": "settings-view-music-slider",             "params_json": "{\"label\":\"Music volume\",\"bind\":\"settings.musicVolume\",\"action\":\"settings.musicVolume.set\",\"prefs_key\":\"MusicVolumeKey\",\"min\":0,\"max\":100,\"step\":1,\"default\":60,\"readout_suffix\":\"%\",\"db_mapping\":true}" },
        { "ord": 8,  "kind": "slider-row",    "instance_slug": "settings-view-sfx-slider",               "params_json": "{\"label\":\"SFX volume\",\"bind\":\"settings.sfxVolume\",\"action\":\"settings.sfxVolume.set\",\"prefs_key\":\"SfxVolumeKey\",\"min\":0,\"max\":100,\"step\":1,\"default\":80,\"readout_suffix\":\"%\",\"db_mapping\":true}" },
        { "ord": 9,  "kind": "label",         "instance_slug": "settings-view-display-header",           "params_json": "{\"text\":\"Display\",\"size_token\":\"size.text.section-header\"}" },
        { "ord": 10, "kind": "dropdown-row",  "instance_slug": "settings-view-resolution-dropdown",      "params_json": "{\"label\":\"Resolution\",\"bind\":\"settings.resolutionIndex\",\"action\":\"settings.resolutionIndex.set\",\"prefs_key\":\"ResolutionIndexKey\",\"options\":[{\"value\":0,\"label\":\"1280\u00d7720\"},{\"value\":1,\"label\":\"1920\u00d71080\"},{\"value\":2,\"label\":\"2560\u00d71440\"},{\"value\":3,\"label\":\"3840\u00d72160\"}],\"default\":\"closest-to-screen-current\"}" },
        { "ord": 11, "kind": "toggle-row",    "instance_slug": "settings-view-fullscreen-toggle",        "params_json": "{\"label\":\"Fullscreen\",\"bind\":\"settings.fullscreen\",\"action\":\"settings.fullscreen.set\",\"prefs_key\":\"FullscreenKey\",\"default\":true}" },
        { "ord": 12, "kind": "toggle-row",    "instance_slug": "settings-view-vsync-toggle",             "params_json": "{\"label\":\"VSync\",\"bind\":\"settings.vsync\",\"action\":\"settings.vsync.set\",\"prefs_key\":\"VSyncKey\",\"default\":true}" },
        { "ord": 13, "kind": "confirm-button","instance_slug": "settings-view-reset-button",             "params_json": "{\"kind\":\"secondary-confirm-button\",\"label\":\"Reset to defaults\",\"confirm_label\":\"Click again to reset\",\"confirm_window_ms\":1000,\"action_confirm\":\"settings.reset.confirm\",\"action\":\"settings.reset\",\"tooltip\":\"Restore all settings to factory defaults.\"}" }
      ]
    }
  }
}
```

#### Wiring contract — settings-view

```yaml
bake_requirements:
  - layout_template: vertical-form
  - hosts: [main-menu (via main-menu-content-slot), pause-menu (via pause-menu-content-slot)]
  - children: 3 section headers + 3 toggle-rows (gameplay) + 3 slider-rows (audio) + 1 dropdown-row + 2 toggle-rows (display) + 1 confirm-button (reset)
  - new archetypes: toggle-row, slider-row, dropdown-row (form-row family)
  - reuses: confirm-button (with 1 s window for non-destructive reset)

actions_referenced:
  - settings.scrollEdgePan.set
  - settings.monthlyBudgetNotifications.set
  - settings.autoSave.set
  - settings.masterVolume.set
  - settings.musicVolume.set
  - settings.sfxVolume.set
  - settings.resolutionIndex.set
  - settings.fullscreen.set
  - settings.vsync.set
  - settings.reset.confirm
  - settings.reset
  - settings.back   # abstract — host adapter wires to mainmenu.back OR pause.back

binds_referenced:
  - settings.scrollEdgePan (bool)
  - settings.monthlyBudgetNotifications (bool)
  - settings.autoSave (bool)
  - settings.masterVolume (int 0–100)
  - settings.musicVolume (int 0–100)
  - settings.sfxVolume (int 0–100)
  - settings.resolutionIndex (int 0–3)
  - settings.fullscreen (bool)
  - settings.vsync (bool)

hotkeys:
  - ESC → settings.back (host-wired)

verification_hooks:
  - test:settings.instant-apply (each control writes prefs + applies on change)
  - test:settings.reset.restores-all-9-defaults
  - test:settings.reset.requires-confirm-within-1s
  - test:settings.volume.percent-to-db-mapping
  - test:settings.resolution.closest-default-on-open
  - test:settings.back.host-aware (main-menu vs pause-menu)
  - test:settings.host-mount.pause-menu-pauses-sim

variant_transitions:
  - reset-button: idle → armed (1s countdown) → idle (timeout) | reset (2nd click)
  - resolution-dropdown: closed ⇄ open (4 options visible)
  - sliders + toggles: bind value reflects live state
```

**Drift flagged.**
- **3 NEW form-row archetypes.** `toggle-row` (label + toggle, 56 px) · `slider-row` (label + slider + readout, 56 px) · `dropdown-row` (label + dropdown, 56 px). Reusable across settings-view + future form panels. Flag → archetype catalog audit + form-row family lock.
- **3 NEW PlayerPrefs keys.** `MonthlyBudgetNotificationsKey` (bool) · `AutoSaveKey` (bool) · `SfxVolumeKey` (int 0–100; reflows existing dB-only key). Flag → PlayerPrefs schema migration.
- **`SettingsScreenDataAdapter` extension.** Add: SFX slider wiring, monthly-budget-notifications toggle, auto-save toggle, Reset-to-defaults button + confirm primitive, dB↔percent mapping helper exposed to all 3 volume sliders. Drop direct prefab serialized refs in favor of bind dispatcher reads. Flag → adapter refactor audit.
- **Auto-save sim hook.** `AutoSaveKey = true` → save every N in-game minutes (or N real seconds). Need scheduler in CityScene tick loop calling `GameSaveManager.SaveGame(autoSaveName)`. Auto-save name = `<city>-autosave-N` rotation (3-slot ring buffer recommended). Flag → auto-save scheduler design.
- **Monthly-budget-notifications routing.** When `false`, suppress the monthly-close toast event surface from `notifications-toast`. Need filter in `notifications-toast` event-emitter chain. Flag → toast filter wiring.
- **Reset-to-defaults factory values.** Locked defaults table above is the factory source. Action `settings.reset` writes all 9 defaults + flushes prefs + dispatches each `settings.*.set` to apply. Flag → reset action body + factory-defaults const table.
- **Host-aware `settings.back`.** Same primitive emits abstract `settings.back`; host adapter listens + dispatches `mainmenu.back` OR `pause.back`. Flag → bind dispatcher pattern (event routing).
- **Resolution closest-match heuristic.** On view-open, find dropdown index whose w×h is closest to `Screen.currentResolution.width × height`. Tie-break: pick higher resolution. Flag → resolution match util.
- **`secondary-confirm-button` variant.** Existing `confirm-button` primitive locked at 3 s window for destructive actions. Settings reset uses 1 s window (recoverable). Need variant in primitive. Flag → confirm-button primitive variants.
- **Volume slider during pause-menu mount.** From pause-menu host, sim paused but audio continues. Sliders apply live during pause (mixer reflects changes). Confirm AudioListener / mixer not also paused. Flag → audio + pause coupling audit.
- **Action registry expansion.** New actions: 9 `settings.*.set` + `settings.reset.confirm` + `settings.reset` + `settings.back` (abstract). Flag → action registry.
- **Bind registry expansion.** 9 settings.* binds (3 bool + 3 int volume + 1 int dropdown + 2 bool display). Flag → bind dispatcher pattern.
- **Tooltip strings.** Reset button needs tooltip; toggles + sliders + dropdown rely on row labels (no per-control tooltip in MVP). Flag → tooltip catalog.
- **i18n.** All section headers + control labels + Reset label + tooltip strings + dropdown options are user-facing. Resolution labels (`1280×720` etc.) are technical strings — likely safe to leave untranslated. Flag → string-table.
- **Motion.** Section appearance = instant on view-mount. Slider drag = live readout update. Dropdown open = instant. Toggle flip = instant bg swap. Reset confirm = 1 s countdown bar (smaller variant of pause-menu Quit primitive).

---

### save-load-view

**Role.** Shared sub-view archetype for saving + loading game state. Mode-driven (`saveload.mode` enum: `save` | `load`). Mounted by main-menu (load-only — no game running to save) AND by pause-menu (both modes via separate Save game / Load game buttons in pause-menu root). Same screen, mode bind hides save controls in load-only host. Wraps existing `GameSaveManager` (file I/O at `Application.persistentDataPath`).

**Anchor + sim policy.** Inherits host frame. From main-menu (load-only): full-screen content slot, no sim. From pause-menu: nested inside pause-menu modal, sim paused. Both hosts: back-arrow top-left visible.

**Mode policy (locked).**
- `mode = save` → save-controls strip visible (top); save-list visible (below); per-row click = stage overwrite confirm; per-row trash = delete confirm. Pause-menu host only.
- `mode = load` → save-controls strip HIDDEN; save-list visible (full height); per-row click = highlight + footer Load button enables; per-row trash = delete confirm. Both hosts.
- Mode is set by host on mount: main-menu → `load`. Pause-menu → set by which root button was clicked (Save game → `save`, Load game → `load`).

**Layout (locked).**
- Top: back-arrow (host-wired destination).
- Center column, 560 px wide, vertical flow:
  - **Save-controls strip** (visible only when `mode = save`) — 64 px tall: name input (full width, 48 px tall, placeholder = pre-rolled `<city>-YYYY-MM-DD-HHmm`) + Save button on right (96 px wide, primary-button kind).
  - **Save-list** — flex height (fills remaining space up to ~480 px), vertical scroll. Each row 56 px tall = save name (left, primary text) + date+time (right, muted text, format `2026-05-07 14:23`) + per-row trash icon-button (right edge, hover-revealed).
  - **Footer** — visible only when `mode = load` AND a row is selected: Load button (full column width, 48 px tall, primary-button kind, disabled until selection).
- 12 px gap between controls. Save-list scrolls; sticky save-controls + footer outside scroll viewport.

**Save mode interactions (locked).**
- Name input pre-fills with `<cityName>-YYYY-MM-DD-HHmm` on view-mount + every Save click (re-rolls timestamp). Player can override.
- Validation: same allowlist as new-game-form city-name (`[A-Za-z0-9 \-]`, 1–32 chars). Save button disabled when name invalid.
- Save click: file does NOT exist → write directly. File exists → stage 3 s overwrite confirm on the Save button.
- List row click (save mode): stage 3 s overwrite confirm on that row's name (treat row name as target file name).
- Trash icon click: stage 3 s delete confirm on that row.

**Load mode interactions (locked).**
- List row click: highlights row (cream-highlight bg + 2 px tan border, same idiom as map-card / budget-chip selected state). Footer Load button enables.
- Footer Load click: loads selected file → close save-load-view → resume / load CityScene with that save.
- Trash icon click: stage 3 s delete confirm on that row.

**Save list rendering (locked).**
- Source: `GameSaveManager.GetSaveFiles()` (existing API) — list of save metadata {name, mtime}.
- Sort: newest first (by `mtime` descending).
- Cap: unlimited; vertical scroll engages when list exceeds container height.
- Empty state: full-list area shows centered muted text `No saves yet.` (load mode from main-menu when player has never saved).

**Auto-name format (locked).** `<cityName>-YYYY-MM-DD-HHmm` — example: `Bahía Bacayo-2026-05-07-1423`. Spaces in city name preserved (existing save-name allowlist accepts).

**Back navigation (locked, host-aware).** Same primitive as settings-view: emits abstract `saveload.back`; host wires to `mainmenu.back` OR `pause.back`.

**Existing implementation reused / refactored.**
- Adapter: `Assets/Scripts/UI/Modals/SaveLoadScreenDataAdapter.cs` (existing — drives existing save-load screen). Needs additions: mode bind, name input, per-row trash + 3 s confirm primitives, footer Load button, list selection highlight, sort newest-first.
- Save layer: `GameSaveManager.SaveGame(string customSaveName)` (existing lines 69–82) + `GameSaveManager.LoadGame()` + new `GameSaveManager.GetSaveFiles()` API (returns sorted metadata) + new `GameSaveManager.DeleteSave(string fileName)` API.
- Prefab: `Assets/UI/Prefabs/Generated/save-load-screen.prefab` (placeholder; needs full bake from new catalog row).

#### JSON DB shape — save-load-view

```jsonc
{
  "panels": {
    "save-load-view": {
      "layout_template": "vertical-form",
      "layout": "vertical-form",
      "host_slots": ["main-menu-content-slot", "pause-menu-content-slot"],
      "params_json": "{\"width_px\":560,\"row_gap_px\":12}",
      "children": [
        { "ord": 1, "kind": "save-controls-strip", "instance_slug": "save-load-view-controls-strip",  "params_json": "{\"visible_bind\":\"saveload.mode.is.save\",\"name_input_bind\":\"saveload.nameInput\",\"name_input_placeholder_pattern\":\"<cityName>-YYYY-MM-DD-HHmm\",\"name_input_max_length\":32,\"name_input_allowlist_regex\":\"[A-Za-z0-9 \\\\-]\",\"save_button_action\":\"saveload.save\",\"save_button_action_confirm\":\"saveload.save.confirm\",\"save_button_confirm_window_ms\":3000}" },
        { "ord": 2, "kind": "save-list",            "instance_slug": "save-load-view-list",            "params_json": "{\"items_bind\":\"saveload.list\",\"selection_bind\":\"saveload.selectedSlot\",\"empty_text\":\"No saves yet.\",\"sort\":\"mtime-desc\",\"row_height_px\":56,\"row_actions\":{\"click_save_mode\":{\"action_confirm\":\"saveload.overwrite.confirm\",\"action\":\"saveload.overwrite\",\"confirm_window_ms\":3000},\"click_load_mode\":{\"action\":\"saveload.selectSlot\"},\"trash\":{\"action_confirm\":\"saveload.delete.confirm\",\"action\":\"saveload.delete\",\"confirm_window_ms\":3000}}}" },
        { "ord": 3, "kind": "button",               "instance_slug": "save-load-view-load-button",     "params_json": "{\"kind\":\"primary-button\",\"label\":\"Load\",\"action\":\"saveload.load\",\"visible_bind\":\"saveload.mode.is.load\",\"enabled_bind\":\"saveload.selectedSlot.exists\",\"tooltip\":\"Load the selected save.\"}" }
      ]
    }
  }
}
```

#### Wiring contract — save-load-view

```yaml
bake_requirements:
  - layout_template: vertical-form
  - hosts: [main-menu (load-only mode), pause-menu (both modes)]
  - children: save-controls-strip (mode-gated visible) + save-list (scrollable, sort newest-first) + Load button (mode-gated visible + selection-gated enabled)
  - new archetypes: save-controls-strip, save-list (with row-action map)
  - reuses: confirm-button (3s destructive window for overwrite + delete)

actions_referenced:
  - saveload.save.confirm        # save mode: save button 1st click (only if name collides existing file)
  - saveload.save                # save mode: save button confirmed click → GameSaveManager.SaveGame(name)
  - saveload.overwrite.confirm   # save mode: existing-row click 1st
  - saveload.overwrite           # save mode: existing-row click 2nd within 3s → GameSaveManager.SaveGame(rowName)
  - saveload.selectSlot          # load mode: row click → bind selectedSlot
  - saveload.load                # load mode: footer Load button click → GameSaveManager.LoadGame(selectedSlot)
  - saveload.delete.confirm      # any mode: trash icon 1st click
  - saveload.delete              # any mode: trash icon 2nd click within 3s → GameSaveManager.DeleteSave(name)
  - saveload.back                # abstract — host wires

binds_referenced:
  - saveload.mode               # enum: save | load (set by host on mount)
  - saveload.mode.is.save       # derived bool — true when mode == save
  - saveload.mode.is.load       # derived bool — true when mode == load
  - saveload.list               # array of {name, mtime} sorted newest-first
  - saveload.selectedSlot       # string | null — currently highlighted slot
  - saveload.selectedSlot.exists # derived bool — true when selectedSlot != null
  - saveload.nameInput          # string — save-mode name input value

hotkeys:
  - ESC → saveload.back (host-wired)

verification_hooks:
  - test:saveload.list.sort-newest-first
  - test:saveload.list.empty-state
  - test:saveload.save.new-name-saves-directly
  - test:saveload.save.existing-name-stages-3s-confirm
  - test:saveload.save.confirm-times-out-after-3s
  - test:saveload.overwrite.row-click-stages-3s-confirm
  - test:saveload.delete.trash-stages-3s-confirm-then-removes
  - test:saveload.load.footer-button-disabled-without-selection
  - test:saveload.load.loads-selected-then-closes-view
  - test:saveload.mode.save-controls-hidden-in-load-mode
  - test:saveload.host-mount.main-menu-forces-load-mode
  - test:saveload.auto-name.format-is-cityName-YYYY-MM-DD-HHmm
  - test:saveload.back.host-aware

variant_transitions:
  - mode: save ⇄ load (set by host, bind toggles strip + footer visibility)
  - row: idle → highlighted (load mode) | idle → armed-overwrite (save mode 1st click) | idle → armed-delete (trash 1st click)
  - save-button: idle → armed (3s on existing-name collision) → idle (timeout) | save (2nd click)
```

**Drift flagged.**
- **2 NEW archetypes.** `save-controls-strip` (name input + save button, mode-gated visible) · `save-list` (scrollable list with per-row action map). Flag → archetype catalog audit.
- **`GameSaveManager.GetSaveFiles()` API.** Returns sorted metadata `{name, mtime}[]` newest-first. Existing manager only exposes save / load — needs metadata enumeration. Flag → save manager API audit.
- **`GameSaveManager.DeleteSave(name)` API.** New. Deletes file at `Application.persistentDataPath/<name>.json` (or whatever extension). Flag → save manager API audit.
- **`GameSaveManager.HasAnySave()` API.** New (cross-cut with main-menu lock). Returns bool — drives `mainmenu.continue.disabled`. Flag → save manager API audit.
- **Save-name auto-format helper.** `SaveNameFormatter.AutoName(cityName, dateTime)` → `<cityName>-YYYY-MM-DD-HHmm`. Reuse existing `cityName` allowlist for whole-string validation. Flag → save name util.
- **Save-name collision detection.** On Save click: check if file exists at target name → if yes, route through 3 s confirm; if no, write directly. Flag → save action body.
- **Mode-gated visibility binds.** `saveload.mode.is.save` + `saveload.mode.is.load` — derived bools (computed from `saveload.mode` enum). Reusable derivation pattern for any enum-driven visibility. Flag → bind derivation pattern.
- **Host forces mode on mount.** Main-menu host: `saveload.mode = load` (locked, never `save` from main-menu — no game running). Pause-menu host: sets per-button (`pause.openSave` → `save`; `pause.openLoad` → `load`). Mode change WHILE mounted is not supported in MVP (player navigates back + reopens to switch). Flag → mode-set-on-mount semantics.
- **Selection state lifecycle.** `saveload.selectedSlot` reset to null on view-mount + on view-unmount + on delete (when selected). Flag → selection lifecycle.
- **Trash icon hover-reveal.** Per-row trash icon shown only on row hover (or always on touch / non-hover platforms). Flag → row-hover state + accessibility.
- **Row action priority.** Save mode + click on row body → overwrite-confirm. Save mode + click on trash icon → delete-confirm. Click target dispatcher must route correctly (icon takes precedence over row body). Flag → click-routing.
- **Auto-save filename rotation.** `<city>-autosave-1` / `<city>-autosave-2` / `<city>-autosave-3` ring buffer. Auto-saves appear in same list as manual saves but visually marked (small auto-save icon). Flag → auto-save row rendering + cross-cut with settings-view.auto-save toggle.
- **Empty-state copy.** `No saves yet.` shown when list empty. Localized. Flag → string-table.
- **Action registry expansion.** New actions: `saveload.save.confirm` · `saveload.save` · `saveload.overwrite.confirm` · `saveload.overwrite` · `saveload.selectSlot` · `saveload.load` · `saveload.delete.confirm` · `saveload.delete` · `saveload.back`. Flag → action registry.
- **Bind registry expansion.** 5 binds (mode enum + 2 derived bools + list array + selectedSlot string + nameInput string + selectedSlot.exists derived bool). Flag → bind dispatcher pattern.
- **Tooltip strings.** Save / Load / trash icons need tooltip copy. Flag → tooltip catalog.
- **i18n.** Save / Load button labels + auto-name template + empty-state copy + tooltip strings are user-facing. Save names themselves (player input + city names) are NOT translated — they're proper nouns / user content. Flag → string-table + i18n policy for user-content strings.
- **Motion.** View mount = instant. Row hover = trash fade-in (120 ms). Selection highlight = instant bg+border swap. Save / overwrite / delete confirms = 3 s countdown bar (shared with info-panel demolish + main-menu Quit + pause-menu Main-menu / Quit primitive).

---

### pause-menu

**Role.** ESC-triggered center modal hub that pauses the sim and exposes 6 game-state actions (Resume / Settings / Save / Load / Main menu / Quit). Hosts 3 sub-screens (Settings / Save-Load / pause-menu root) inside a single modal root via content-replacement navigation. Mutually exclusive with `budget-panel` + `stats-panel`.

**Anchor + sim policy.** Center modal with backdrop dim, geometry inherited from existing `pause-menu.prefab`. Sim pauses on open: TimeManager modal-pause owner = `pause-menu`. Mutually exclusive with `budget-panel` + `stats-panel` (open one → others auto-close OR open is blocked — see drift). Backdrop click resumes sim.

**Existing implementation reused.**
- Adapter: `Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs:12–62` (wires 6 ThemedButtons).
- Sub-adapters: `SettingsScreenDataAdapter.cs:11–101` · `SaveLoadScreenDataAdapter.cs` · `NewGameScreenDataAdapter.cs`.
- Prefabs: `Assets/UI/Prefabs/Generated/pause-menu.prefab` + `pause.prefab` + `settings-screen.prefab` + `save-load-screen.prefab` + `new-game-screen.prefab`.
- ESC stack: `UIManager.HandleEscapePress` lines 383–415 (TECH-14102 LIFO discipline). Pause-menu sits at the BOTTOM of the stack (fallback when no other modal/picker active).
- Save layer: `GameSaveManager.SaveGame(string customSaveName)` lines 69–82 + `LoadGame()`. Writes to `Application.persistentDataPath`.
- Settings layer: PlayerPrefs keys `MasterVolumeKey` · `MusicVolumeKey` · `ResolutionIndexKey` · `FullscreenKey` · `VSyncKey` · `ScrollEdgePanKey` (+ `SfxVolumeDbKey` from `BlipBootstrap:73` — currently unsurfaced).
- Quit / scene swap: `MainMenuController.QuitGame` line 752 (`Application.Quit`); `SceneManager.LoadScene(0)` line 750 (main menu).

**Button list (6, locked).** Order matches existing prefab:

| # | Button | Action | Sub-screen / terminal |
| --- | --- | --- | --- |
| 1 | Resume | `pause.resume` | Closes modal, resumes sim |
| 2 | Settings | `pause.openSettings` | Replaces content with `settings-screen` |
| 3 | Save game | `pause.openSave` | Replaces content with `save-load-screen` (mode = save) |
| 4 | Load game | `pause.openLoad` | Replaces content with `save-load-screen` (mode = load) |
| 5 | Main menu | `pause.toMainMenu.confirm` | Inline 3 s confirm → `SceneManager.LoadScene(0)` |
| 6 | Quit to desktop | `pause.quit.confirm` | Inline 3 s confirm → `Application.Quit` |

**Sub-screen navigation (replace + back).** Single modal root; clicking Settings / Save / Load swaps modal content to the corresponding sub-screen. ESC at sub-screen → returns to pause-menu root (back-one-level). ESC at pause-menu root → closes modal + resumes sim. Backdrop click at any level → fully closes modal + resumes sim. Resume button at root → closes modal + resumes sim. Sub-screens own their own internal Back affordance (top-left arrow) for mouse users.

**Settings sub-screen scope (7 controls).** Existing 6 PlayerPrefs settings + NEW SFX volume slider:

1. Master volume (slider 0–1) → `AudioListener.volume`
2. Music volume (slider 0–1) → music mixer channel
3. **NEW** SFX volume (slider 0–1, dB-mapped) → BlipBootstrap `SfxVolumeDbKey`
4. Resolution (dropdown) → `Screen.SetResolution`
5. Fullscreen (toggle) → `Screen.fullScreen`
6. VSync (toggle) → `QualitySettings.vSyncCount`
7. Scroll-edge-pan (toggle) → camera input gate

**Save-Load sub-screen (two modes).** Same screen, mode driven by which pause-menu button opened it.
- **Save mode** — text input for save name (default = ISO timestamp) + scrollable existing-saves list + Save button. Click on existing slot → overwrite confirm (inline 3 s). Empty list state shows "no saves yet".
- **Load mode** — scrollable saves list + Load button (disabled until selection). Click on slot → highlight; double-click → Load directly.
- Existing `GameSaveManager.SaveGame(string customSaveName)` accepts free-text names. Existing `PlayerPrefs.SetString("LastSavePath")` (`MainMenuController:19`) tracks continue-button target.

**Destructive confirm pattern (inline 3 s).** Reuses the info-panel demolish pattern. Main menu + Quit buttons each implement: first click → button swaps to red `Confirm — quit?` / `Confirm — main menu?` for 3 s; second click within 3 s fires the terminal action; outside 3 s, button reverts to default state.

**Open trigger.** ESC key only when no other modal / picker active. No HUD button. Existing TECH-14102 stack priority (newest-first):

```
SubTypePicker  >  ToolSelected  >  {budget-panel, stats-panel, info-panel}  >  pause-menu (fallback)
```

When pause-menu is the active layer, ESC closes it (resumes sim). When something else is active, ESC dismisses that layer first.

**Close paths (4).**
1. Resume button → `pause.resume`.
2. ESC at root → close + resume sim.
3. Backdrop click at any level → close + resume sim.
4. Terminal action (Main menu / Quit second-click) → fires action; modal closes implicitly via scene swap / app quit.

**Mutual exclusion rule.** Only one of `{budget-panel, stats-panel, pause-menu}` open at a time. When pause-menu open trigger fires while another is open: drift below — recommend auto-close of the other. (Other modals route ESC away from pause-menu via the stack, so this race is rare.)

**Hotkeys.** `ESC` toggles pause-menu when stack is empty. No other hotkeys in MVP.

#### JSON DB shape — pause-menu

```jsonc
{
  "slug": "pause-menu",
  "fields": {
    "layout_template": "modal-card",
    "layout": "vstack",
    "params_json": "{\"width\":\"prefab\",\"height\":\"prefab\",\"anchor\":\"center\",\"backdropDim\":true,\"backdropDismiss\":true,\"defaultActive\":false,\"contentMode\":\"replaceable\"}"
  },
  "children": [
    { "ord":  1, "kind": "label",  "instance_slug": "pause-menu-title-label",       "params_json": "{\"kind\":\"label\",\"variant\":\"title\",\"text\":\"Paused\"}",                                                          "layout_json": "{\"zone\":\"header\"}" },
    { "ord": 10, "kind": "button", "instance_slug": "pause-menu-resume-button",     "params_json": "{\"label\":\"Resume\",\"kind\":\"illuminated-button\",\"action\":\"pause.resume\"}",                                       "layout_json": "{\"zone\":\"body\"}" },
    { "ord": 11, "kind": "button", "instance_slug": "pause-menu-settings-button",   "params_json": "{\"label\":\"Settings\",\"kind\":\"illuminated-button\",\"action\":\"pause.openSettings\"}",                                "layout_json": "{\"zone\":\"body\"}" },
    { "ord": 12, "kind": "button", "instance_slug": "pause-menu-save-button",       "params_json": "{\"label\":\"Save game\",\"kind\":\"illuminated-button\",\"action\":\"pause.openSave\"}",                                  "layout_json": "{\"zone\":\"body\"}" },
    { "ord": 13, "kind": "button", "instance_slug": "pause-menu-load-button",       "params_json": "{\"label\":\"Load game\",\"kind\":\"illuminated-button\",\"action\":\"pause.openLoad\"}",                                  "layout_json": "{\"zone\":\"body\"}" },
    { "ord": 14, "kind": "button", "instance_slug": "pause-menu-mainmenu-button",   "params_json": "{\"label\":\"Main menu\",\"kind\":\"illuminated-button\",\"action\":\"pause.toMainMenu.confirm\",\"variant\":\"danger\",\"confirmTimeoutMs\":3000}", "layout_json": "{\"zone\":\"body\"}" },
    { "ord": 15, "kind": "button", "instance_slug": "pause-menu-quit-button",       "params_json": "{\"label\":\"Quit to desktop\",\"kind\":\"illuminated-button\",\"action\":\"pause.quit.confirm\",\"variant\":\"danger\",\"confirmTimeoutMs\":3000}", "layout_json": "{\"zone\":\"body\"}" }
  ]
}
```

Sub-screens (`settings-screen`, `save-load-screen`, `new-game-screen`) each get their own panel rows in `panels.json` and are rendered into the pause-menu modal root via the replace-content flow.

#### Wiring contract — pause-menu

| Channel | Surface | Notes |
| --- | --- | --- |
| `bake_requirements` | new archetype `modal-card` (root container with backdrop dim + center anchor + content-replace slot); reuses `illuminated-button` + `label`. Sub-screens own their own archetypes (sliders / dropdowns / toggles in `settings-screen`; text input + slot list in `save-load-screen`). | `modal-card` is shared with budget / stats — promote to a common archetype. Pause-menu adds `contentMode: "replaceable"` semantic (sub-screen swap-in target). |
| `actions_referenced` | `pause.resume` · `pause.openSettings` · `pause.openSave` · `pause.openLoad` · `pause.toMainMenu.confirm` · `pause.quit.confirm` (terminal: `pause.toMainMenu` + `pause.quit`); plus sub-screen actions: `settings.master.set` · `settings.music.set` · `settings.sfx.set` · `settings.resolution.set` · `settings.fullscreen.set` · `settings.vsync.set` · `settings.scrollEdgePan.set` · `save.save` · `save.delete` · `save.load` · `pause.back` (sub-screen → root) | All actions emit through the existing `PauseMenuDataAdapter` / sub-adapter wiring. New: `confirmTimeoutMs` payload semantic for the inline-confirm pattern (also used by info-panel). |
| `binds_referenced` | `pause.visible` · `pause.contentScreen` (enum: `root` / `settings` / `save-load`) · `settings.master.value` · `settings.music.value` · `settings.sfx.value` · `settings.resolution.value` · `settings.fullscreen.value` · `settings.vsync.value` · `settings.scrollEdgePan.value` · `save.list` (array of `{name, timestamp, path}`) · `save.selectedSlot` · `save.mode` (enum: `save` / `load`) | Pause root binds + 7 settings binds + 3 save-load binds. Bind dispatcher must support enum payloads (`pause.contentScreen`, `save.mode`). |
| `hotkeys` | `ESC` (when stack empty) → pause toggle; `ESC` at sub-screen → `pause.back`; `ESC` at root → `pause.resume` | All ESC routing already in `UIManager.HandleEscapePress`. Confirm sub-screen back-one-level wiring in adapter. |
| `verification_hooks` | ESC with empty stack → `pause.visible=true` + sim pauses; Settings click → `pause.contentScreen=settings` + content swaps; Resume click → `pause.visible=false` + sim resumes; backdrop click → same as Resume; Main menu first click → red confirm state for 3 s; Main menu second click within 3 s → `SceneManager.LoadScene(0)` fires; Quit second click within 3 s → `Application.Quit`; Settings slider drag → PlayerPrefs key writes + audio/display effect applies; Save mode + name + Save click → `GameSaveManager.SaveGame(name)` writes file; Load mode + slot select + Load click → `GameSaveManager.LoadGame()` restores | Bridge tool stub needed: `unity_pause_menu_state_get` returns `{visible, contentScreen, simPaused, confirmingButton, save{listCount, selectedSlot, mode}}`. |
| `variant_transitions` | `pause.visible=true` ⇄ `false`; `pause.contentScreen` ∈ `{root, settings, save-load}` (3 sub-screens swap inside same modal root); each destructive button `idle` ⇄ `confirming` (3 s) ⇄ fired-or-reverted; sub-screen back transitions; backdrop dim fade (instant in MVP) | No subtype hierarchy — sub-screens are content variants. |

#### Drift items + open questions — pause-menu

- **TimeManager modal-pause owner not implemented.** `TimeManager.SetModalPauseOwner(string)` / `ClearModalPauseOwner(string)` API is referenced by budget-panel + stats-panel + pause-menu specs but does not exist yet (audit confirms only `timeMultiplier` + `SetTimeSpeedIndex(int)`). Flag → single TimeManager API addition that all 3 modals share.
- **Mutual exclusion enforcement.** Spec says budget / stats / pause-menu mutually exclusive but no enforcement layer today. Decision: when one opens, the others auto-close (call `Close()` on each before opening). Flag → add `ModalCoordinator` (or similar) singleton OR push exclusion logic into each modal's adapter.
- **Confirm-button primitive.** Inline 3 s confirm pattern shared with info-panel demolish + pause-menu Main menu / Quit. Flag → extract `ConfirmButton` component (button variant) reusable across panels.
- **Sub-screen content-replace mechanism.** Single modal root with swappable content payload. Existing adapters (`SettingsScreenDataAdapter` / `SaveLoadScreenDataAdapter`) currently render into separate prefab roots. Decision: keep separate prefab roots; pause-menu modal root just calls `SetActive` on the right sub-prefab. Flag → confirm prefab-swap vs single-root-with-content-slot.
- **`save-load-screen` shape.** Existing `SaveLoadScreenDataAdapter` shape unknown without deeper audit. Locked spec: save-mode = name input + existing-saves list + Save button + per-slot overwrite-confirm; load-mode = saves list + Load button (disabled until selection). Flag → audit current adapter + reconcile.
- **Save name default.** Default name = ISO timestamp `YYYY-MM-DD HH:mm`. Flag → `SaveTimestampFormatter` util.
- **Save slot delete affordance.** Each slot row gets a small × delete button + inline 3 s confirm. Currently no delete API in `GameSaveManager`. Flag → `GameSaveManager.DeleteSave(string path)` addition.
- **Save dirty-tracking.** Knowing when current game has unsaved changes (to gate destructive actions or warn) requires a dirty flag. Out of MVP scope per "no conditional confirm" decision but flag as future work.
- **SFX volume slider — NEW UI surface.** `BlipBootstrap.SfxVolumeDbKey` exists in PlayerPrefs but has no UI. Add slider to settings sub-screen. Flag → `SettingsScreenDataAdapter` addition + dB ↔ linear mapping.
- **Settings reset to defaults — deferred.** Not in MVP scope per poll. Flag → post-MVP add.
- **Backdrop click vs settings unsaved values.** Settings writes are immediate (PlayerPrefs.Save on slider release), so backdrop click is safe. Confirm: no in-flight buffer. Flag → audit `SettingsScreenDataAdapter` write semantics.
- **Resolution dropdown population.** Existing adapter populates from `Screen.resolutions[]`. No change. Document only.
- **VSync + scroll-edge-pan toggles.** Existing wiring is correct. Document only.
- **Quit-confirm + Application.Quit in editor.** `Application.Quit` is no-op in editor; existing code handles via `#if UNITY_EDITOR EditorApplication.ExitPlaymode`. Confirm. Flag → audit `MainMenuController.QuitGame` for editor branch.
- **Main menu scene index.** `SceneManager.LoadScene(0)` per `MainMenuController:750`. Build index 0 = main menu, 1 = CityScene (per `CitySceneBuildIndex` const). Locked.
- **CityScene → CityScene rename.** Open task #18 — pause-menu spec references `CityScene` build index but post-rename will be `CityScene`. Flag → rename audit covers `CitySceneBuildIndex` constant.
- **Action registry expansion.** New actions: `pause.resume` · `pause.openSettings` · `pause.openSave` · `pause.openLoad` · `pause.toMainMenu.confirm` · `pause.toMainMenu` · `pause.quit.confirm` · `pause.quit` · `pause.back` · `settings.*.set` (7) · `save.save` · `save.delete` · `save.load`. Flag → action registry.
- **Bind registry expansion.** New bind families: `pause.visible` · `pause.contentScreen` (enum) · `settings.*.value` (7) · `save.list` (array) · `save.selectedSlot` · `save.mode` (enum). Flag → bind dispatcher pattern + enum-bind support.
- **i18n.** All button labels + sub-screen labels are user-facing. Flag → string-table.
- **Motion.** Open / close = instant SetActive. Sub-screen swap = instant. Confirm-button = color tween + 3 s countdown bar (shared with info-panel demolish). Flag → motion spec confirmation.

---

### notifications-toast

**Role.** Always-on transient feedback channel. Stacks brief cards in the top-right corner under the hud-bar to surface event signals from sim + player actions. Reuses the existing production-ready `GameNotificationManager` queue (4-tier today; spec extends to 5 tiers + new event surfaces). Sim runs (NOT a modal); never blocks input.

**Anchor + sim policy.** Top-right corner stack, growing downward, sits under hud-bar. 320 px wide cards, 8 px gap, 12 px padding. Highest z-order — overlays info-panel + map-panel when both share the right edge. Sim runs in all states; toasts never pause time.

**Existing implementation reused.**
- Manager: `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` — queue (max 5 visible), 4-tier enum (`Info` / `Success` / `Warning` / `Error`), lazy-create UI, fade in / out coroutines, convenience `PostInfo` / `PostSuccess` / `PostWarning` / `PostError` methods.
- SFX hooks: `sfxNotificationShow` (lines 29–31) + `sfxErrorFeedback`; played via `UiSfxPlayer.Play` (line 296). Spec extends with 3 new clips.
- Existing emitters: `GridManager:769` (PostWarning interstate demolition), `GridManager:52` (`onUrbanCellsBulldozed` Action), `BuildingPlacementService:250,258` (success / error), `ZoneManager:523,742` (warnings), `TreasuryFloorClampService:67` (PostError insufficient funds).
- Placeholder prefab: `Assets/UI/Prefabs/Generated/alerts-panel.prefab` (bare; needs full bake).

**Geometry (locked).** 320 px wide cards. Stack origin = below hud-bar, right-edge aligned with 24 px right margin. Each card: 12 px padding, 8 px gap to next card, ~64 px tall (icon 32 + text body). Overlay info-panel when both visible — toasts render at highest z; info-panel keeps its 320 px right-edge dock geometry.

**Severity tiers (5).** Locked.

| Tier | Color (token) | Sticky? | SFX | Use cases |
| --- | --- | --- | --- | --- |
| Info | `color.toast.info` (blue) | No | `sfxNotificationShow` (existing) | Generic neutral signals |
| Success | `color.toast.success` (green) | No | `sfxSuccess` (NEW) | Successful build / save / connect |
| Warning | `color.toast.warning` (amber) | No | `sfxWarning` (NEW) | Service-coverage drops, soft cautions |
| Error | `color.toast.error` (red) | No | `sfxErrorFeedback` (existing) | Insufficient funds, blocked actions |
| Milestone | `color.toast.milestone` (gold-pulse) | **Yes** (until clicked) | `sfxMilestone` (NEW) | City milestones (population thresholds) |

Milestone tier renders with a gold pulse animation + crown icon variant; sticks until player clicks (no auto-dismiss timer).

**Dismiss policy (locked).**
- Info: 4 s auto-fade.
- Success: 4 s auto-fade.
- Warning: 6 s auto-fade.
- Error: 8 s auto-fade.
- Milestone: sticky-until-clicked.
- All: click-to-dismiss any time.

**Queue policy.** Max 5 cards visible. 6th post → oldest non-sticky card ages out (fade-quick) + new card pushes onto stack. Queued FIFO when 5 sticky milestones occupy slots (rare). Existing `GameNotificationManager` queue logic confirms shape.

**Click action (locked).** Each toast's `cellRef` payload (when present) → click jumps camera to cell + dismisses toast (`cameraController.MoveCameraToCell(grid)`). Toasts without `cellRef` (e.g. milestone "Population 10 000") → click only dismisses.

**Event surfaces (locked, multi-select).**
1. **City milestones — sticky Milestone tier.** Population thresholds: 1 000 / 5 000 / 10 000 / 25 000 / 50 000 / 100 000. Fires once per threshold cross; persists until clicked.
2. **Service-coverage drops — Warning tier.** Per-service threshold cross (below 40 % coverage). Debounced: one toast per service per 30 in-game days. 11 services covered (Police / Fire / Edu / Health / Parks / PublicHousing / PublicOffices / Power / Water / Sewage / Roads).

NOT in MVP: treasury balance crossings (already covered via Error on insufficient funds), disaster events (no disasters in MVP scope).

**Z-order vs info-panel.** Toasts render at highest z (above info-panel + map-panel + hud-bar). Player sees toast briefly + dismisses or it ages out; info-panel stays put underneath.

#### JSON DB shape — notifications-toast

```jsonc
{
  "slug": "notifications-toast",
  "fields": {
    "layout_template": "toast-stack",
    "layout": "vstack",
    "params_json": "{\"width\":320,\"anchor\":\"top-right\",\"marginTop\":\"hud-bar.bottom\",\"marginRight\":24,\"gapPx\":8,\"paddingPx\":12,\"maxVisible\":5,\"zOrder\":\"highest\",\"defaultActive\":true}"
  },
  "children": []
}
```

Toast cards are runtime-instantiated by `GameNotificationManager`; no static children in the bake. Card prefab variant per tier (Info / Success / Warning / Error / Milestone) lives under `Assets/UI/Prefabs/Generated/`.

#### Wiring contract — notifications-toast

| Channel | Surface | Notes |
| --- | --- | --- |
| `bake_requirements` | NEW archetype `toast-stack` (top-right vstack with z-order=highest, runtime-managed children); NEW archetype `toast-card` (icon + body label + optional close affordance, 5 tier variants); reuses `label` + sprite slugs for tier icons. | `toast-stack` is unique to this surface. `toast-card` tier variants drive color tokens + sticky semantics + SFX clip. |
| `actions_referenced` | `notification.dismiss` (click → fade out + queue advance); `notification.click` (jumps camera + dismiss when `cellRef` present); `notification.post` (emit-side, internal — adapters call `GameNotificationManager.PostInfo/Success/Warning/Error` + new `PostMilestone`) | New: `PostMilestone(string title, string subtitle = null)` method on `GameNotificationManager` (sticky variant). New: `notification.click` semantic for camera-jump on `cellRef` payload. |
| `binds_referenced` | `notification.queue` (array of `{tier, title, body, cellRef?, postedAt, expiresAt?}`); `notification.visible` (bool — false when queue empty) | Queue shape matches existing internal `GameNotificationManager` data. Bind dispatcher must support array-bind + nullable `cellRef`. |
| `hotkeys` | None | No hotkeys in MVP. |
| `verification_hooks` | `PostInfo("test")` → toast appears + fades after 4 s; `PostError(...)` → red card + `sfxErrorFeedback` plays + fades after 8 s; `PostMilestone("Pop 10 000")` → gold-pulse card + `sfxMilestone` plays + sticks until clicked; service-coverage drop below 40 % → debounced Warning toast (max one per service per 30 days); 6th post → oldest non-sticky ages out; click on toast with `cellRef` → camera jumps to cell + toast dismissed | Bridge tool stub needed: `unity_notifications_state_get` returns `{queueLength, visibleCount, byTier{...}, oldestPostedAt}`. |
| `variant_transitions` | Card lifecycle: `entering` (fade-in 200 ms) → `visible` → `exiting` (fade-out 300 ms) → destroyed; tier variant fixed at post-time (no in-flight tier swap); milestone gold-pulse loop runs while `visible` | Existing fade coroutines (lines 289–357) implement entering / exiting; sticky semantics + pulse loop are new. |

#### Drift items + open questions — notifications-toast

- **Z-order vs info-panel collision.** Toasts overlay info-panel at right edge. Player loses bottom of info-panel content briefly while toasts are visible. Acceptable per lock; flag if QA finds blocking content (e.g. demolish button hidden by toast). Mitigation post-MVP: shift-left when info-panel open.
- **`PostMilestone` API addition.** `GameNotificationManager` today exposes 4 `Post*` methods. Add 5th: `PostMilestone(string title, string subtitle = null, Vector2Int? cellRef = null)` → sets sticky + gold-pulse variant. Flag → API addition + tier enum extension.
- **Tier enum extension.** Existing `NotificationType` enum has 4 values (Info / Success / Warning / Error). Add `Milestone`. Flag → enum + switch-statement audits across emitters.
- **Sticky-until-clicked queue semantics.** Today's queue ages oldest out at 5+. Sticky milestones must skip age-out + queue overflow in front. Flag → queue logic update: count non-sticky against max-visible, sticky cards always render in front.
- **3 new SFX clips.** `sfxSuccess` (chime, ~200 ms) · `sfxWarning` (low pulse, ~300 ms) · `sfxMilestone` (gold flourish, ~600 ms). Flag → audio asset authoring + serialized field additions on `GameNotificationManager`.
- **City milestone emitter.** No `CityStats` event today fires on population threshold cross. Flag → `CityStats.OnPopulationMilestone` Action<int> (fires once per threshold) + emitter wiring in monthly update path. Threshold list: `[1000, 5000, 10000, 25000, 50000, 100000]`.
- **Service-coverage threshold-crossing emitter.** No service-coverage event surface today. Flag → per-service threshold crosser util + 30-day debounce (`lastWarnTimestamp` per service, gates emit if `currentDate - lastWarn >= 30 days`).
- **Camera-jump on click.** Toasts with `cellRef` payload jump camera. Existing `cameraController.MoveCameraToCell(Vector2Int)` confirmed. Flag → audit `MoveCameraToMapCenter` vs `MoveCameraToCell` API surface; if missing, add.
- **Click-anywhere-on-toast vs explicit close affordance.** MVP: entire card is click target. No close X. Flag → confirm UX; consider × for sticky milestones to disambiguate from camera-jump.
- **Toast width vs hud-bar truncation.** 320 px width assumes hud-bar's right edge has 24 px margin. If hud-bar geometry changes, toast anchor must update. Flag → tie `marginTop` token to `hud-bar.bottom` glossary slug.
- **Existing emitter audit.** `BuildingPlacementService:250,258` + `ZoneManager:523,742` + `TreasuryFloorClampService:67` + `GridManager:769` already post — no change. Document only.
- **`alerts-panel.prefab` placeholder fate.** Today's bare prefab is unused (lazy-create in `GameNotificationManager` builds UI at runtime). Decision: deprecate prefab + bake `notifications-toast` panel row instead. Flag → prefab cleanup.
- **5 tier-color tokens.** `color.toast.{info,success,warning,error,milestone}` — milestone is a 2-color pulse loop (gold-bright ⇄ gold-dim). Flag → token additions + pulse animation curve.
- **5 tier-icon sprite slugs.** `toast-icon-{info,success,warning,error,milestone}` (milestone = crown variant). Flag → sprite catalog audit + asset authoring.
- **Queue pause on modal open.** Open question: when budget / stats / pause-menu modal opens (sim pauses), does toast queue freeze (no fade-out) or continue? Lock: queue continues, fade timers run on real time not sim time. Player sees toasts age out while modal open. Flag → confirm via QA; alternative is freeze-during-modal.
- **Notification persistence across save/load.** Out of MVP scope — queue is in-memory only. Save/load resets queue. Flag as future work.
- **Replay / spectator mode rendering.** Out of MVP scope. Flag → future work.
- **Action registry expansion.** New actions: `notification.dismiss` · `notification.click`. New emit-side method: `PostMilestone`. Flag → action registry.
- **Bind registry expansion.** New bind: `notification.queue` (array) + `notification.visible` (bool). Flag → bind dispatcher pattern + array-bind support.
- **i18n.** All toast titles + bodies are user-facing. Existing emitters pass literal English strings. Flag → string-table integration for all `Post*` call sites + milestone copy.
- **Motion.** Fade-in 200 ms ease-out · fade-out 300 ms ease-in (existing curves). Milestone pulse: 1.2 s sinusoidal gold-bright ⇄ gold-dim loop. Flag → animation curve confirmation + token names.

---

## Calibration ledger

> Schema-lock section. Locked BEFORE seed tasks (Stage 2.0.3 / 2.0.4). These shapes are the contract; seed rows MUST conform. State file paths: `ia/state/ui-calibration-corpus.jsonl` + `ia/state/ui-calibration-verdicts.jsonl`.

### Corpus row schema

One row per grilling decision. Append-only. Consumed by Stage 3 MCP slice `ui_calibration_corpus_query`.

```json
{
  "ts":                "ISO-8601",
  "panel_slug":        "string",
  "decision_id":       "D001",
  "prompt":            "product-language question asked",
  "resolution":        "chosen option or typed value",
  "rationale":         "why this option was picked",
  "agent|human":       "agent | human",
  "parent_decision_id": "D0XX | null"
}
```

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `ts` | ISO-8601 string | yes | UTC timestamp of decision |
| `panel_slug` | string | yes | e.g. `hud-bar` |
| `decision_id` | string | yes | monotonic `D001`..`D999` per panel |
| `prompt` | string | yes | exact product-language question text |
| `resolution` | string | yes | chosen option (product terms) |
| `rationale` | string | yes | agent-side translation + why |
| `agent\|human` | enum | yes | who authored: `agent` or `human` |
| `parent_decision_id` | string | no | when decision refines another |

### Verdict row schema

One row per rebake iteration verdict. Append-only. Consumed by Stage 3 MCP slice `ui_calibration_verdict_record`.

```json
{
  "ts":              "ISO-8601",
  "panel_slug":      "string",
  "rebake_n":        1,
  "bug_ids":         ["A", "B"],
  "improvement_ids": ["Imp-3"],
  "resolution_path": "prose describing what was changed",
  "outcome":         "pass | partial | fail"
}
```

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `ts` | ISO-8601 string | yes | UTC timestamp |
| `panel_slug` | string | yes | e.g. `hud-bar` |
| `rebake_n` | integer | yes | 1-based rebake sequence number |
| `bug_ids` | string[] | yes | bugs fixed this rebake (e.g. `["A","B"]`) — empty array `[]` if none |
| `improvement_ids` | string[] | yes | improvements landed (e.g. `["Imp-3"]`) — empty array `[]` if none |
| `resolution_path` | string | yes | prose: what changed (files, logic) |
| `outcome` | enum | yes | `pass` / `partial` / `fail` |

### State file paths

| File | Role |
| --- | --- |
| `ia/state/ui-calibration-corpus.jsonl` | One row per grilling decision; append-only |
| `ia/state/ui-calibration-verdicts.jsonl` | One row per rebake verdict; append-only |

---

## Interactions

> Cross-panel rules: modal stacking, input routing priority, focus trapping, cross-panel triggers, hotkey conflicts.

_(Phase 4 grilling will fill this section.)_

```json
{
  "interactions": []
}
```

---

## Baseline reference

> **Read-only annotation** of the current scene look at process-lock baseline (2026-05-07). NOT a definition source — purely visual context for the agent during grilling. Per Q1 decision (`docs/ideas/ui-elements-grilling.md §8`), user defines all elements from scratch; this section only documents what exists today.

### Scene state observed

Single panel currently lives in the bottom of the game view: `hud-bar`.

### Panel: `hud-bar` (current look)

- **Position** — bottom strip across full width. Strip extends past the visible viewport on the right side (clipping issue observed in last QA pass).
- **Height** — ~80px.
- **Layout** — horizontal flex with three zones: left, center, right.
- **Children** — 19 cells total:
  - **Left zone (3):** zoom-in, zoom-out, recenter — camera controls.
  - **Center zone (8):** city-name label, AUTO toggle, budget +/- buttons, budget-graph button, MAP button, budget-readout label, pause button — game state + economy.
  - **Right zone (8):** speed-1x through speed-5x buttons, play button, build-residential button, build-commercial button — time controls + build entry-points.
- **Theme** — illuminated-button kind (cream body + tan border + indigo icon).
- **Rendering** — 7/17 button icons render correctly post bake-pipeline fix (F1–F12). Remaining 10 cells empty due to missing sprite assets in catalogue (separate issue, out of UI-definition scope).
- **Reference screenshot** — `tools/reports/bridge-screenshots/hud-bar-pass4-F12-icons-20260507-20260507-105159.png`.
- **Reference snapshot** — `Assets/UI/Snapshots/panels.json` (schema_v4, 1 panel, 19 children).

### Annotated drift / pain points (from QA observations)

| # | Observation | Implication for new design |
| --- | --- | --- |
| B1 | hud-bar overflows viewport right edge | new `<HudStrip>` must constrain to viewport width OR push overflow to a secondary panel |
| B2 | 19 cells in one panel = visual noise; user couldn't identify several functions | reduce per-panel button count; split build / camera / speed / economy across separate panels or sub-zones |
| B3 | Several buttons (build-residential, budget-graph) lack clear UX function from the icon alone | every button needs a tooltip + a clear action binding documented in Phase 2 |
| B4 | Speed cluster (1x..5x + play + pause) = 7 controls for one concept | candidate to collapse into single time-control sub-component |
| B5 | Sprite catalogue gap = 10 missing icons | sprite-author work, tracked separately; not blocking definition phase |

---

## References

| Doc | Role |
| --- | --- |
| `docs/ui-bake-pipeline-rollout-plan.md` | Process plan — Tracks A–E, bake iteration log, status tracker |
| `docs/ideas/ui-elements-grilling.md` | Process spec — grilling protocol, polling templates, calibration design vision |
| `ia/state/ui-calibration-corpus.jsonl` | Grilling decision ledger — append-only corpus rows (schema: §Calibration ledger) |
| `ia/state/ui-calibration-verdicts.jsonl` | Rebake verdict ledger — append-only verdict rows (schema: §Calibration ledger) |

---

## [Stage-2.AUDIT]

> Stage 2 in-stage shape additions confirmed. Dual audit checkpoint (agent + human) per Q7 process lock.

| Check | Status |
| --- | --- |
| `§ hud-bar` carries `#### DB shape achieved` sub-block | ✅ mig id `0108_seed_hud_bar_panel_v2`, entity_id=41, rect_json + 14 children listed |
| `§ toolbar` carries `#### DB shape achieved` sub-block | ✅ mig id `0110_seed_toolbar_panel`, entity_id=100, rect_json; children deferred per Track A scope |
| `§ Calibration ledger` schema section added | ✅ corpus row + verdict row schemas locked; state file paths cross-linked |
| `ia/state/ui-calibration-corpus.jsonl` exists | ✅ 14 rows, all panel_slug=hud-bar, D001..D014 |
| `ia/state/ui-calibration-verdicts.jsonl` exists | ✅ 7 rows, rebake_n 1..7 |
| Three-doc cross-link triple wired | ✅ definitions ↔ rollout-plan ↔ grilling-ideas §References in each doc |
| State file paths cited in all three docs | ✅ corpus.jsonl + verdicts.jsonl named in §References of each doc |

---

## Changelog

| Date | Change | Notes |
| --- | --- | --- |
| 2026-05-07 | Doc created | Skeleton + tokens + components seeded from `docs/ideas/ui-elements-grilling.md §4`. Phase 0 baseline annotated. Phase 1 polling starting. |
| 2026-05-07 | Wiring-contract template added | New per-panel sub-section captures `bake_requirements` / `actions_referenced` / `binds_referenced` / `hotkeys` / `verification_hooks` / `variant_transitions` for MCP-tool calibration. |
| 2026-05-07 | `hud-bar` locked | Top-anchored full-width strip. 3 zones — `left` (new/save/load), `center` (city-name/sim-date/pop readouts), `right` (4 cols: zoom, money+time stack, stats, map). Replaces prior 19-cell bottom hud-bar. Drift flagged: schema nesting, `readout-button` archetype, speed-model code drift, action+bind registry, sprite catalog audit. |
| 2026-05-07 | `toolbar` locked | Left-edge top-anchored 2-col grid. 11 active tools + 1 disabled placeholder across 4 groups (zoning RCIS / infra Road-Power-Water-Sewage / civic Landmark-Forests / destroy DemolishCell+DemolishArea-disabled), separated by 3 thin tan bars. Icon-only + hover tooltip; no hotkeys; pressed-cream active state. Subtype picker = separate panel `tool-subtype-picker` at fixed bottom-left strip. Drift flagged: ToolFamily enum gaps (StateService→StateZoning rename + Sewage/Landmark/DemolishCell/DemolishArea adds), `<separator>` archetype, disabled variant, action payload schema, viewport-height audit, StateZoning subtype mechanism, sprite catalog audit (12 slugs). |
| 2026-05-07 | `tool-subtype-picker` locked | Fixed bottom-left horizontal strip, 96 px tall, 80 × 80 cards, 8 px gap, dark translucent panel + 1 px tan border + scroll arrows. Sticky open during paint sessions: ESC + same-toolbar-tool re-click only dismiss (world / HUD clicks never dismiss); other-tool clicks swap variant in place. 3-line cards (icon + name + cost); capacity moves to info-panel. Per-family policy: cost = flat $ for single-click families (Power/Water/Sewage/Landmark), $/cell otherwise; paint mode declared per family (drag-paint / stroke / single-click / mode-driven / click-each); `picker_variant` ∈ `cards-density` (R/C/I) / `cards-kind` (StateZoning / Road / Utility / Landmark) / `cards-mode` (Forests + 2 mode buttons) / `none` (DemolishCell). Card counts: R=3 · C=3 · I=3 · StateZoning=7 · Road=4 · Power=2 · Water=2 · Sewage=2 · Forests=3+2 · Landmark=4. Affordability: live greyed + click blocked + tooltip override. R/C/I density-evolution stays WITHIN density tier. Drift flagged: `subtype-card` archetype, action+bind registry consolidation with toolbar, Industrial agri/manuf/tech post-placement assignment TBD, StateZoning spawn pool + grey-shade tile variants, ~36 sprite slugs catalog audit, bake-time children flattening rule, Forests mode-button placement, i18n + motion follow-ups. |
| 2026-05-07 | `budget-panel` locked | HUD-triggered center modal, 720 × 520 px, dark backdrop + tan-bordered card. Sim pauses on open (TimeManager modal-pause owner = `budget-panel`). 2 × 2 quadrant grid all visible: TL taxes (4 sliders R/C/I/S, 0–20 % step 0.5), TR funding (11 sliders Police/Fire/Edu/Health/Parks/PublicHousing/PublicOffices/Power/Water/Sewage/Roads, 0–100 % step 5 + spent-readout row per service), BL monthly close (last-month in/out/net/balance + 3-month forecast preview), BR trend (stacked-area chart of expense breakdown by category, 3-position range tabs: 3mo / 12mo / all-time). Header strip: title + close X. Close: X click + ESC + backdrop click. No hotkeys. Cannot stack with pause-menu. Drift flagged: 6 NEW archetypes (`slider-row` · `expense-row` · `readout-block` · `chart` · `section` · `range-tabs`), action registry expansion (`budget.taxRate.set` · `budget.funding.set` · `budget.trend.rangeSet` · `modal.close`), bind dispatcher pattern subscriptions (`budget.taxRate.*` · `budget.funding.*` · `budget.spent.*` · `budget.lastMonth.*` · `budget.forecast.month{1,2,3}` · `budget.history.*` · `budget.trend.range`), BudgetForecaster sim service (recompute on slider edit), stacked-area chart primitive, TimeManager.SetModalPauseOwner API, color.bg.dim token, MonthFormatter util, Industrial sub-tax sharing policy, autosave-during-modal behavior, replay read-only state. |
| 2026-05-07 | `stats-panel` locked | HUD-triggered center modal, 720 × 520 px, sim pauses (TimeManager modal-pause owner = `stats-panel`), mutually exclusive with budget-panel + pause-menu. 3 tabs: Graphs / Demographics / Services. Graphs = 3 line charts (Population / Money / Employment) + 3-pos range tabs (3mo / 12mo / all-time, range shared with budget). Demographics = 3 stacked-bar rows (R/C/I composition · density tiers · wealth tiers). Services = 11 service rows (Police / Fire / Edu / Health / Parks / PublicHousing / PublicOffices / Power / Water / Sewage / Roads), each = icon + name + coverage % + color-coded bar (green ≥ 70 / yellow 40–69 / red < 40). Close: X + ESC + backdrop. Open: `hud-bar-stats-button` (NOT in current hud-bar snapshot — flagged). No hotkeys. Drift flagged: hud-bar amendment for stats trigger, NEW archetypes (`tab-strip` · `stacked-bar-row` · `service-row`), shared archetypes with budget (`chart` · `range-tabs` · backdrop dim token · TimeManager modal-pause API), action registry (`stats.open` · `stats.close` · `stats.tabSet` · `stats.graphs.rangeSet`), bind dispatcher pattern (series + percent vector + record families), StatsHistoryRecorder sim service (monthly ring buffer), service-row coverage-tier thresholds as tokens, demog segment-color tokens, chart kind enum lock, Roads-as-service confirmation, empty-state rendering for new cities, tab persistence in save-game, i18n + motion. |
| 2026-05-07 | `map-panel` locked | Always-on persistent HUD minimap (NOT a modal — sim runs). Bottom-right corner, 360 × 360 px, 24 px right + bottom margins. `hud-bar-map-button` toggles visibility (open ⇄ collapsed). City-only top-down render at fixed scale; water always rendered as base. 5 multi-select layers: Streets / Zones / Forests / Desirability / Centroid (defaults Streets + Zones). Layer-toggle UI = row of 5 icon-only buttons in 36 px header strip on top (render area = 360 × 324). Click anywhere on render → `cameraController.MoveCameraToMapCenter`; black viewport rect overlay shows main-camera frustum (cyan ring when Centroid layer active). NEW behavior: drag-on-rect to pan continuously. No close button (visibility owned by hud button). No hotkeys. Drift flagged: `hud-bar-map-button` action assignment + `MiniMapController.SetVisible` API, NEW `minimap-canvas` archetype, header strip layout retrofit (existing prefab body-only), `MiniMapController.SetLayerActive` API, `OnDrag` handler + `CameraController.PanCameraTo`, drag-state cleanup on toggle, 5 NEW sprite slugs (`layer-streets/zones/forests/desirability/centroid`), action registry (`minimap.toggle` · `minimap.layer.set` · `minimap.click` · `minimap.drag`), bind dispatcher (per-layer bools + render texture + viewport-rect Rect + visible bool), tooltip primitive cross-cut, modal-coexistence pointer-event routing, region-minimap post-MVP, i18n + motion. |
| 2026-05-07 | `info-panel` locked | Right-edge inspect dock, 320 px wide, top-anchored under hud-bar, full remaining height, vertical scroll on overflow. Sim runs (NOT a modal). 6 selection types: zoned-building / road / utility-tile / forest / bare-cell / landmark. Big card per type — header (icon + name + type tag + close X) → bind-driven `field-list` body (per-type field set) → footer action zone (inline Demolish for demolish-able types). Auto-open on plain world click when no tool active; `Alt+Click` opens without firing tool when tool active. Selection swap re-renders content in-place (no animation). 4 close paths: X / ESC (modal-priority guarded) / empty-tile click / selection swap. Inline Demolish = first click stages red 3 s confirm, second click within 3 s fires `world.demolish` (wraps `GridManager.HandleBulldozerMode`). Drift flagged: deprecate `DetailsPopupController` + 5-tuple `OnCellInfoShown`, NEW archetypes (`info-dock` · `field-list`), `WorldSelectionResolver` extraction from `GridManager`, 6 per-type field-set adapters, demolish-without-tool API (`GridManager.DemolishAt(grid)` or programmatic mode-set), `Alt+Click` modifier in input routing, ESC hotkey-stack priority (modals first), action registry (`info.close` · `info.demolish.confirm` · `world.select` · `world.deselect` · `world.demolish`), bind registry with array-bind support for `info.selection.fields`, 6 NEW type-icon sprite slugs, post-MVP Upgrade / production / transit-to-this action stubs, sticky header / footer scroll behavior, i18n + motion (instant SetActive + 3 s confirm tween). |
| 2026-05-07 | `pause-menu` locked | ESC-triggered center modal hub. Geometry inherited from existing `pause-menu.prefab`. Sim pauses (TimeManager modal-pause owner = `pause-menu`), mutually exclusive with budget-panel + stats-panel. 6 buttons (existing `PauseMenuDataAdapter`): Resume / Settings / Save game / Load game / Main menu / Quit to desktop. Sub-screens (Settings / Save-Load) replace pause-menu content via single modal root; ESC at sub-screen returns to root, ESC at root closes + resumes sim. Settings sub-screen = 7 controls (Master / Music / NEW SFX / Resolution / Fullscreen / VSync / Scroll-edge-pan; existing `SettingsScreenDataAdapter` PlayerPrefs keys + new SFX from `BlipBootstrap.SfxVolumeDbKey`). Save-Load sub-screen = same screen two modes (Save = name input + saves list + Save button + per-slot overwrite confirm; Load = list + Load button); free-text save name via existing `GameSaveManager.SaveGame(string)`. Inline 3 s destructive confirm on Main menu + Quit (reuses info-panel demolish primitive). Open trigger: ESC fallback in TECH-14102 LIFO stack only (no HUD button). Close paths: Resume / ESC at root / backdrop click / terminal action. Drift flagged: `TimeManager.SetModalPauseOwner` API NOT YET IMPLEMENTED (needed for budget + stats + pause-menu), `ModalCoordinator` mutual-exclusion enforcement, shared `ConfirmButton` primitive across panels, sub-screen content-replace mechanism (prefab swap vs single-root-with-slot), `GameSaveManager.DeleteSave(path)` addition, save-name `SaveTimestampFormatter`, SFX volume dB↔linear mapping in `SettingsScreenDataAdapter`, CityScene→CityScene rename audit (`CitySceneBuildIndex`), `Application.Quit` editor-branch confirmation, action registry (~13 new actions inc. `pause.resume` · `pause.openSettings` · `pause.openSave` · `pause.openLoad` · `pause.toMainMenu.confirm` · `pause.quit.confirm` · `pause.back` · `settings.*.set` × 7 · `save.save/delete/load`), bind registry with enum-bind support (`pause.contentScreen` · `save.mode`) + array-bind (`save.list`), `modal-card` shared archetype with budget/stats, i18n + motion (instant + 3 s confirm tween). |
| 2026-05-07 | `tooltip` primitive locked | Hover-dwell 500 ms hint. Single line element name; max 240 px wrap to 2 lines. Position auto-flip (above default, below near top edge). Cream/paper bg + dark indigo text + tan 1 px border, fade 120 ms in/out. Z-layer above modals. Disabled variant → `tooltip_override` REPLACES default (never appended). Source = per-element `params_json.tooltip` field — no slug auto-gen, no glossary fallback. Pause-time agnostic. Pointer-leave + click both dismiss. Touch / long-press deferred post-MVP. Drift flagged: NEW `tooltip-card` archetype, 5 NEW tokens (z.tooltip + 4 size.tooltip.*), 3 NEW binds (tooltip.text / target / visible), EXTEND existing `TooltipController` (preserve singleton + canvas-reparent + TMP injection + TooltipText marker; add 500ms dwell + 120ms fade + auto-flip + disabled-variant override), catalog validator rule (every tooltip-eligible row must declare non-empty `tooltip`), modal-coexistence tooltips inside budget/stats/pause sub-controls, cross-cut audit pass to enumerate every interactive child across 9 locked panels for missing tooltip strings, distinct motion from toast (snappy 120 ms vs arrival 200/300 ms), i18n via string-table. |
| 2026-05-07 | `main-menu` locked | Pre-game title-screen panel, full-screen plain themed bg (`color.bg.menu` cream/sand, no hero art / no audio / no live preview MVP). Title top-center + version bottom-right + studio bottom-left strips constant across all views. Center area = vertical button stack (Continue / New Game / Load / Settings / Quit) on root view, swaps to new-game-form / load-list / settings sub-views via `mainmenu.contentScreen` enum-bind. Single panel, content-swap navigation (no prefab swap, no modal stack). Back button top-left + ESC bound on sub-views; ESC no-op on root. Continue: auto-loads most recent save, instant scene fade to CityScene; greyed + tooltip-override `No save found` when `GameSaveManager.HasAnySave() = false`. New Game / Load / Settings: in-place center swap. Settings + Load views = SHARED archetypes with pause-menu (single source of truth, host determines mount point). Quit: inline 3 s confirm reusing `ConfirmButton` primitive (info-panel demolish + pause-menu); editor branch via `EditorApplication.ExitPlaymode`. Pause-menu's `pause.toMainMenu` returns to MainMenu.unity (build index 0) after its own 3 s confirm. Drift flagged: NEW `view-slot` archetype + shared `settings-view` / `load-list-view` / `new-game-form` archetypes, `GameSaveManager.HasAnySave()` + `GetMostRecentSave()` API additions, `MainMenu.unity` scene composition audit (no sim managers), `color.bg.menu` + `size.text.title-display` token adds, `MainMenuController` refactor (drop direct `Application.Quit` + direct sub-screen activate; route via `ConfirmButton` + `mainmenu.contentScreen` bind), action registry (`mainmenu.continue` · `openNewGame` · `openLoad` · `openSettings` · `back` · `quit.confirm` · `quit`), bind registry (`mainmenu.contentScreen` enum · `continue.disabled` bool · `back.visible` bool + 3 string binds), tooltip-override on Continue, scene-transition fade primitive, audio + hero art MVP scope, version-string from `Application.version`, i18n string-table. |
| 2026-05-07 | `new-game-form` locked | Sub-view of main-menu (mounted via `view-slot` when `mainmenu.contentScreen = new-game-form`; NOT a top-level panel; NOT reachable from in-game pause-menu). 3 visible fields locked from D18: map size = 3 preset cards (Small 64×64 / Medium 128×128 / Large 256×256, default Medium); starting budget = 3 preset chips (Tight $10k / Standard $50k / Generous $200k, default Standard); city name = single-line text input (1–32 chars, allowlist `[A-Za-z0-9 \-]`, pre-rolled from new `city-name-pool-es` 100 fictional Spanish names — developer flavour; never translates) + trailing reroll icon-button. Seed HIDDEN — random per game, recorded in save metadata. Difficulty DROPPED. Layout: vertical 480 px column — section headers + map cards row → budget chips row → city name input → Start button. Selected state = cream-highlight bg + 2 px tan border + dark text (reuses toolbar pressed-active idiom). Start = instant CityScene load (no confirm; non-destructive at title screen). Empty city-name on Start → silent reroll (Start never blocked). Back = inherited from main-menu host (top-left arrow + ESC; form state discarded). Drift flagged: 3 NEW archetypes (`card-picker` · `chip-picker` · `text-input`) + new `string-pool` catalog kind + `placeholder_pool` mechanism, `city-name-pool-es` 100-name authoring pass, `MainMenuController.StartNewGame` signature refactor (drop scenarioIndex, add startingBudget + cityName), `NewGameScreenDataAdapter` refactor (drop seed slider + scenario toggles, add 3 bind-driven pickers), GridManager + geography perf audit at 256×256, `EconomyManager.SetStartingFunds` + `CityStats.SetCityName` API surface trace, seed generation + save-metadata schema audit, scene-scoped bind lifecycle (`newgame.*` discarded on scene transition), reroll never-twice-in-a-row util, action registry (`newgame.mapSize.set` · `newgame.budget.set` · `newgame.cityName.reroll` · `mainmenu.startNewGame`), bind registry (`newgame.mapSize` enum · `newgame.budget` enum · `newgame.cityName` string), tooltip catalog adds, i18n string-table for headers + labels + tooltips (pool itself is Spanish-only by design), motion shared with main-menu scene-transition primitive. |
| 2026-05-07 | `settings-view` locked | Shared sub-view archetype mounted by main-menu (`mainmenu.contentScreen = settings`) + pause-menu (`pause.contentScreen = settings`); single source of truth across both hosts. 480 px vertical column, 3 grouped sections separated by thin tan bars: Gameplay (3 toggles — Scroll-edge-pan / Monthly-budget notifications / Auto-save) + Audio (3 sliders 0–100% with live readout — Master / Music / SFX, dB↔linear mapping in adapter) + Display (Resolution dropdown 4 hardcoded entries 1280×720 / 1920×1080 / 2560×1440 / 3840×2160 default = closest-to-current; Fullscreen toggle; VSync toggle). Instant apply on every change (no commit button); footer Reset = 1 s confirm primitive (resets all 9 controls to defaults). Back = host-aware: main-menu host returns to root view, pause-menu host returns to pause root. ESC mirrors Back. Drift flagged: 3 NEW PlayerPrefs keys (SfxVolumeKey reflowed from BlipBootstrap.SfxVolumeDbKey, MonthlyBudgetNotificationsKey, AutoSaveKey), `SettingsScreenDataAdapter` extension (3 new toggles + SFX slider + dB↔linear util + Reset method), 3 NEW archetypes (`form-row-toggle` · `form-row-slider` · `form-row-dropdown`), AutoSaveScheduler service design (cadence + filename pattern + replace-vs-rotate policy), MonthlyBudgetNotifier event subscriber (hooks `EconomyManager.ProcessDailyEconomy` month-rollover), Resolution dropdown index-↔-Resolution-struct util, hardcoded resolution list constant + closest-default util, 3-section thin-bar separator reuse from toolbar, ConfirmButton primitive 1s variant (recoverable vs 3s destructive), action registry (`settings.scrollEdgePan.set` · `settings.monthlyBudgetNotifs.set` · `settings.autoSave.set` · `settings.master.set` · `settings.music.set` · `settings.sfx.set` · `settings.resolution.set` · `settings.fullscreen.set` · `settings.vsync.set` · `settings.reset.confirm`), bind registry (9 setting binds — 3 bool + 3 float + 1 int + 2 bool), i18n string-table for section headers + labels + tooltips, motion (instant + 1s confirm tween). |
| 2026-05-07 | `save-load-view` locked | Shared sub-view archetype mounted by main-menu (load-only; `mainmenu.contentScreen = load`) + pause-menu (both modes; `pause.contentScreen = saveload`); host forces `saveload.mode` ∈ `save` \| `load` on mount. 560 px vertical column. 3 zones: save-controls strip (mode-gated visible only in `save` mode — name input single-line + Save button; auto-name format `<cityName>-YYYY-MM-DD-HHmm`) + scrollable save-list (56 px compact rows — name + date only, no thumbnails / no city stats; sort newest-first; unlimited scroll) + footer Load button (mode-gated visible in `load` mode + selection-gated). Click row = highlight (cream bg + tan border, reuses toolbar pressed-active idiom). Save mode interactions: type or accept auto-name → click Save → if name collides existing slot → 3 s overwrite confirm; else instant write. Trash icon per row = 3 s destructive confirm primitive (deletes save file). Load mode interactions: click row to highlight → click footer Load → instant scene transition to CityScene with restored state. Back = host-aware (main-menu / pause-menu root). ESC mirrors Back. Drift flagged: 2 NEW archetypes (`save-row` 56 px compact · `save-controls-strip` mode-gated), `GameSaveManager` API additions (`GetSaveFiles()` returns sorted metadata list · `DeleteSave(name)` · `HasAnySave()`), save-metadata schema (cityName + timestamp + seed + mapSize + budget snapshot for list-row rendering), `SaveLoadScreenDataAdapter` refactor (drop hardcoded slot count, drive list from `save.list` array-bind), `saveload.mode` enum-bind + mode-gated visibility binds (`save.controls.visible` · `load.button.visible`), auto-name SaveTimestampFormatter util + cityName fallback, ConfirmButton 3 s destructive variant reuse (overwrite + delete), filename collision detector, list-row click selection state (`save.selectedName` string bind), action registry (`save.save` · `save.overwrite.confirm` · `save.delete.confirm` · `load.load` · `save.list.select`), bind registry (`save.list` array · `save.mode` enum · `save.selectedName` string · `save.controls.visible` bool · `load.button.visible` bool · `save.nameInput` string), i18n string-table for placeholder + labels + confirm prompts + tooltips, motion (instant + 3 s confirm tween). |
| 2026-05-07 | `audio-cues` registry locked | Cross-cutting behavior-freeze table covering all 10 `BlipId` cues + 3 `UiSfxPlayer` AudioClips + 2 `GameNotificationManager` clips. Names trigger source class + fire event + panel host + KEEP verdict per cue. Locks 4 invariance rules: ThemedButton owns auto hover/click emits (rebake MUST route every button through ThemedButton), sim-side emits never reroute through UI layer (RoadManager / BuildingPlacementService / GridManager / EconomyManager / GameSaveManager own their emits), BlipBootstrap PlayerPrefs persistence (SfxVolumeDbKey + SfxMutedKey via dB↔linear util in SettingsScreenDataAdapter), BlipId.None no-op invariance. Pending follow-ups: 3 unauthored toast clips, modal open/close cue lock, tooltip silent confirmed, picker double-emit audit. |
| 2026-05-07 | `notifications-toast` locked | Always-on transient feedback channel. Top-right corner stack under hud-bar, 320 px wide cards, 8 px gap, max 5 visible, growing downward, highest z-order (overlays info-panel). Reuses existing production-ready `GameNotificationManager` (queue + lazy UI + fade coroutines + 2 existing SFX). 5 severity tiers — Info(blue,4s) / Success(green,4s) / Warning(amber,6s) / Error(red,8s) / Milestone(gold-pulse,sticky-until-clicked). Click on toast with `cellRef` jumps camera + dismisses. Event surfaces (MVP, multi-select): city-population milestones (1k/5k/10k/25k/50k/100k → sticky Milestone) + service-coverage drops (below 40 %, debounced one-per-service-per-30-days → Warning). NOT in scope: treasury balance, disasters. SFX mapping: reuse 2 existing (sfxNotificationShow, sfxErrorFeedback) + add 3 new (sfxSuccess chime, sfxWarning low-pulse, sfxMilestone gold-flourish). Drift flagged: `PostMilestone` API + `NotificationType.Milestone` enum extension + sticky-queue semantics, NEW archetypes (`toast-stack` · `toast-card` 5-tier variants), `CityStats.OnPopulationMilestone` Action<int> emitter + threshold const, per-service coverage threshold-crosser util + 30-day debounce field, `cameraController.MoveCameraToCell(Vector2Int)` audit, deprecate `alerts-panel.prefab` placeholder, 5 tier-color tokens (milestone = pulse), 5 tier-icon sprite slugs (milestone = crown), action registry (`notification.dismiss` · `notification.click`), bind registry with array-bind (`notification.queue`) + bool (`notification.visible`), queue real-time vs sim-time fade decision (lock = real-time, fades during modal pause), no save/load persistence (in-memory only), i18n string-table integration, motion (200ms fade-in / 300ms fade-out / 1.2s pulse loop). |
