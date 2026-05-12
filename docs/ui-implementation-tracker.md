# UI Implementation Tracker

**Source of truth:** Postgres `territory_ia_dev` — `catalog_entity` + `panel_detail` + `panel_child` + `token_detail` + `button_detail`. Bake pipeline reads from this DB.

**Status legend:**
- ✅ **complete** — published in DB + baked prefab green + visual verdict accepted
- 🟡 **partial** — published but visual polish or feature gaps remain (uses pilot primitives but not iterated for visual quality)
- ⚪ **unpublished** — `catalog_entity.current_published_version_id IS NULL`, no current version
- 🔵 **pilot reference** — primitives surfaced here become shared factories/tokens for parallel work

**Pilot baseline (this session, 2026-05-12):** `stats-panel` + `budget-panel` reached visual baseline. 8 cross-panel primitives promoted to shared surface (see [`ia/specs/ui-design-system.md` §8.3](../ia/specs/ui-design-system.md)).

## Panels (13 total)

| status | slug | id | display_name | layout_template | dims (w×h) | primitives used | parallel-agent notes |
|---|---|---|---|---|---|---|---|
| ✅🔵 | **stats-panel** | 220 | City Stats | modal-card | 720×560 | NavBackButton · RoundedBorder · header-strip HLG · row_columns=2 · type-scale 24/20/18 · ApplyCornerOverlay (deprecated by header-strip) | Pilot — reference impl. iters in `docs/stats-panel-design-iteration.md` |
| ✅🔵 | **budget-panel** | 221 | Budget | modal-card | 760×1100 | Same + section-header divs · slider-row-numeric · expense-row · readout-block · themed-label `variant:"section-header"` | Pilot — reference impl. iters in `docs/budget-panel-design-iteration.md` |
| ✅ | **hud-bar** | 41 | HUD Bar | hstack | top dock | size-text-value (readouts) + size-text-body-row (city-name) | Whole-game rollout 2026-05-12 — size_token slugs on population + sim-date + city-name labels; v=613. |
| ✅ | **main-menu** | 175 | Main Menu | fullscreen-stack | full screen | size-text-title-display (title) + size-text-body-row (studio/version) | Whole-game rollout 2026-05-12 — legacy size_token slugs migrated to published type-scale tokens; v=614. |
| ✅ | **new-game-form** | 199 | New Game Form | modal-card | inherits modal-card | header-strip back-button + themed-label modal-title; card-picker + chip-picker renderers landed | Whole-game rollout 2026-05-12 — token padding + header-strip ordering + Bucket C renderers; v=604. |
| ✅ | **pause-menu** | 222 | Pause Menu | modal-card | 480×480 | header-strip back-button (visible_bind: pause.contentBack.visible) + themed-label modal-title | Whole-game rollout 2026-05-12 — runtime InjectNavHeader stripped, DB-driven header-strip; v=608. |
| ✅ | **save-load-view** | 213 | Save / Load View | modal-card | inherits modal-card | header-strip back-button + themed-label modal-title | Whole-game rollout 2026-05-12 — token padding + header-strip ordering; v=606. |
| ✅ | **settings-view** | 200 | Settings View | modal-card | inherits modal-card | header-strip back-button + themed-label modal-title | Whole-game rollout 2026-05-12 — token padding + header-strip ordering; v=605. |
| ✅ | **tool-subtype-picker** | 216 | Tool Subtype Picker | hstack | bottom-left dock | subtype-card renderer landed | Whole-game rollout 2026-05-12 — tokenized padding (gap-tight) + Bucket C3 subtype-card renderer; v=607. |
| 🟡 | **toolbar** | 100 | Toolbar | (vstack/hstack) | left dock | tool select buttons | Whole-game rollout 2026-05-12 — no panel_child label rows in DB; type-scale pass deferred until row authoring; entity_version v=612 published placeholder. |
| ✅ | **info-panel** | 224 | Info Panel | modal-card | 480×320 | header-strip + name (section-header) + body + cell-coord labels | Whole-game rollout 2026-05-12 — DB-authored, action.info.close; v=609. |
| ✅ | **map-panel** | 225 | Map Panel | modal-card | 420×480 | header-strip + minimap-canvas + 3 layer-toggle illuminated-buttons | Whole-game rollout 2026-05-12 — DB-authored, action.map-panel.close + toggleLayer (terrain/roads/zones); v=610. |
| ✅ | **notifications-toast** | 226 | Notifications Toast | top-right-toast | 320×0 | toast-stack + toast-card | Whole-game rollout 2026-05-12 — DB-authored with toast-stack + toast-card template (bindId: notifications.toastList); v=611. |

## Buttons (12 total)

All `button` kind entities materialize via `button_detail` + sprite refs. Bake renders `IlluminatedButton` with halo/body/icon spawned from `SpawnIlluminatedButtonRenderTargets`.

| status | slug | id | display_name | bake-time renderer | notes |
|---|---|---|---|---|---|
| ✅ | hud-bar-new-button | 90 | New Game | illuminated-button | HUD always-on |
| ✅ | hud-bar-save-button | 91 | Save Game | illuminated-button | HUD |
| ✅ | hud-bar-load-button | 92 | Load Game | illuminated-button | HUD |
| ✅ | hud-bar-budget-button | 95 | Budget | illuminated-button | Opens budget-panel |
| ✅ | hud-bar-play-pause-button | 96 | Play / Pause | illuminated-button | HUD |
| ✅ | hud-bar-speed-cycle-button | 97 | Speed Cycle | illuminated-button | HUD |
| ✅ | hud-bar-stats-button | 98 | Stats Panel Toggle | illuminated-button | Opens stats-panel |
| ✅ | hud-bar-auto-button | 99 | AUTO Mode Toggle | illuminated-button | HUD toggle |
| ⚪ | hud-bar-map-button | 50 | Map | — | unpublished; `action.map-panel-toggle` exists |
| ⚪ | hud-bar-zoom-in-button | 42 | Zoom In | — | unpublished |
| ⚪ | hud-bar-zoom-out-button | 43 | Zoom Out | — | unpublished |
| ✅ | mainmenu-{new-game,load,continue,settings,back,quit,quit-confirm}-button | 164–170 | (various) | illuminated-button / confirm-button | Main menu flow |

## Components (7 total — primitive widgets)

| status | slug | id | display_name | bake-time renderer | notes |
|---|---|---|---|---|---|
| 🟡 | ui-label | 140 | Label | themed-label | Used implicitly via `kind:"themed-label"` panel_child. Pilot variant-sizing applies. |
| 🟡 | ui-modal | 143 | Modal | modal-card layout-template | Pilot frame baseline. |
| 🟡 | ui-readout | 141 | Readout | segmented-readout | Digital LCD digit display. Pilot wraps with `themed-label section-header` for label-above-digit. |
| 🟡 | ui-toggle | 142 | Toggle | toggle-row | Used by settings-view. |
| 🟡 | icon-button | 139 | IconButton | illuminated-button | Generic icon button shape. |
| 🟡 | hud-strip | 138 | HudStrip | (hud-bar layout) | Top HUD strip pattern. |
| ✅ | mainmenu-content-slot | 174 | Main Menu Content Slot | view-slot | Main menu content area. |

## Archetypes (12 total — bake-time renderer pairs)

| status | slug | id | child_kind alias | renderer | notes |
|---|---|---|---|---|---|
| ✅ | view-slot | 176 | view-slot | (slot wrapper) | Mounts sub-views. |
| ✅ | confirm-button | 177 | confirm-button | IlluminatedButton + ConfirmButton | Destructive action button. |
| ✅ | card-picker | 201 | card-picker | Grid 3-col + ToggleGroup | Whole-game rollout 2026-05-12 — Bucket C1 renderer landed in UiBakeHandler `case "card-picker"`. |
| ✅ | chip-picker | 202 | chip-picker | HLG + ToggleGroup chips | Whole-game rollout 2026-05-12 — Bucket C2 renderer landed; color-bg-selected token applied. |
| ✅ | text-input | 203 | text-input | TMP_InputField | Used by new-game-form. |
| ✅ | toggle-row | 204 | toggle-row | HLG label + Toggle | Used by settings-view. |
| ✅ | slider-row | 205 | slider-row | HLG label + Slider | Used by settings-view + budget-panel (via `slider-row-numeric` alias). |
| ✅ | dropdown-row | 206 | dropdown-row | HLG label + TMP_Dropdown | Used by settings-view. |
| ✅ | section-header | 207 | section-header | TMP label (20 pt bold, pilot iter 8) | Used by budget-panel. |
| ✅ | save-controls-strip | 214 | save-controls-strip | (custom) | Used by save-load-view. |
| ✅ | save-list | 215 | save-list | (custom) | Used by save-load-view. |
| ✅ | subtype-picker-strip | 217 | subtype-picker-strip | (custom) | Used by tool-subtype-picker. |

## Tokens (28 total)

### Type-scale tokens (5 — pilot promoted 3 new this session)

| status | slug | id | value_json | usage |
|---|---|---|---|---|
| ✅ | size-text-title-display | 179 | `{pt:48, weight:"bold"}` | Branding splash |
| ✅ | size-text-modal-title | 227 | `{pt:24, weight:"bold"}` | Modal header — `themed-label modal-title` (pilot iter 8) |
| ✅ | size-text-section-header | 211 | `{pt:20, weight:"bold"}` | Section dividers (pilot iter 8 — bumped 16→20) |
| ✅ | size-text-body-row | 229 | `{pt:18, weight:"regular"}` | Row primary label (pilot iter 8, new) |
| ✅ | size-text-value | 230 | `{pt:18, weight:"bold"}` | Row secondary value (pilot iter 8, new) |

### Color tokens

| status | slug | id | value_json | usage |
|---|---|---|---|---|
| ✅ | color-bg-menu | 178 | (color) | Menu background |
| ✅ | color-bg-selected | 208 | (color) | Selected state |
| ✅ | color-border-selected | 209 | (color) | Selected border |
| ✅ | color-text-dark | 122 | (color) | Dark text |
| ✅ | color-text-muted | 212 | (color) | Muted text |
| ✅ | color-border-accent | 231 | `{hex:"#ffb020", reference_slug:"led-amber"}` | Modal-card border (pilot iter 10, new) |
| ⚪ | color-border-tan / color-bg-cream / color-bg-cream-pressed / color-alert-red / color-icon-indigo | 118–123 | (unpublished) | Pre-pilot legacy |

### Spacing tokens (unpublished, pre-pilot)

| status | slug | id | usage |
|---|---|---|---|
| ⚪ | size-icon, size-button-short/tall, size-strip-h, size-panel-card | 124–128 | Pre-pilot — not used by current bake handler |
| ⚪ | gap-tight/default/loose | 129–131 | Pre-pilot |
| ⚪ | pad-button | 132 | Pre-pilot |
| ⚪ | layer-world/hud/toast/modal/overlay | 133–137 | Z-layer tokens (pre-pilot) |

### Typography family

| status | slug | id | value_json | usage |
|---|---|---|---|---|
| ✅ | font-family-ui | 232 | `{family:"LiberationSans", weight_default:"regular"}` | TMP default formalized (pilot iter 10, new) |

### Misc tokens (HUD-specific, pre-pilot)

| status | slug | id | usage |
|---|---|---|---|
| ✅ | mainmenu-{title-label,studio-label,version-label} | 171–173 | Main menu labels |
| ✅ | hud-bar-{population-readout,sim-date-readout} | 93–94 | HUD readouts |
| ⚪ | hud-bar-city-name-label | 45 | unpublished |

---

## Parallel-agent task buckets

Each bucket = independent body of work that can be assigned to a parallel agent. All inherit the pilot primitives (NavBackButton, RoundedBorder, header-strip HLG, type-scale tokens, row_columns, modal-card layout-template). Each bucket should follow the same iterative cycle: DB-publish → bake → Play-Mode screenshot → verdict → promote any new cross-panel surface.

### Bucket A — Modal panels needing pilot primitive application

1. **pause-menu** — currently uses runtime nav-header injection (`PauseMenuDataAdapter.InjectNavHeader`). Port to DB-driven header-strip via panel_child ordering. Verify pause flow + sub-view (settings/save/load) still wires correctly.
2. **settings-view** — apply header-strip + type-scale tokens. Verify toggle-row / slider-row / dropdown-row renderers still work with bumped font sizes.
3. **save-load-view** — apply pilot primitives. Verify save-list row rendering at new type-scale.
4. **new-game-form** — apply pilot primitives + define **`card-picker`** + **`chip-picker`** bake renderers (currently fail with `unhandled_inner_kind` warnings).
5. **tool-subtype-picker** — apply pilot primitives + define **`subtype-card`** bake renderer (currently fails with `unhandled_inner_kind`).

### Bucket B — Unpublished panels needing full authoring

6. **info-panel** — author DB rows (panel_detail + panel_child) using pilot baseline. Trigger via cell-click event.
7. **map-panel** — author DB rows. Bind to `action.map-panel-toggle` action.
8. **notifications-toast** — author DB rows + define **`toast-card`** + **`toast-stack`** bake renderers. Top-right-toast layout-template needs verification.

### Bucket C — Missing bake renderers (blockers for buckets A/B)

9. `card-picker` archetype — bake renderer. Affects map-small/medium/large-card under new-game-form.
10. `chip-picker` archetype — bake renderer. Affects budget-low/mid/high-chip under new-game-form.
11. `subtype-card` — bake renderer. Affects tool-subtype-picker-card-template.
12. `toast-card` — bake renderer. Required for notifications-toast.
13. `chart` advanced rendering — currently `chart-stub`. Define axis labels + series rendering for budget-forecast-chart.

### Bucket D — Always-on UI passes (lower priority)

14. **hud-bar** — apply type-scale tokens to readouts (population, sim-date, city-name).
15. **toolbar** — type-scale pass on category labels.
16. **main-menu** — type-scale pass + verify mainmenu labels still render correctly.

### Bucket E — Token gaps (one-off DB authoring)

17. Publish remaining pre-pilot color tokens (color-border-tan, color-bg-cream, color-alert-red, color-icon-indigo) IF needed by buckets A/B; otherwise leave unpublished.
18. Publish spacing tokens (gap-* / pad-button / size-*) IF needed.

## Workflow per parallel agent

1. Read this tracker + `ia/specs/ui-design-system.md` §8 (DB-sourced definitions, source of truth).
2. Read pilot iteration trackers (`docs/{stats,budget}-panel-design-iteration.md`) for mechanical playbook.
3. Mutate DB (UPDATE / INSERT panel_detail + panel_child rows). Never hand-edit prefabs.
4. `node tools/scripts/snapshot-export-game-ui.mjs` → snapshot regenerate.
5. `npm run unity:bake-ui` → bake. Editor must be in Edit Mode (exit Play Mode via bridge if needed).
6. `node tools/mcp-ia-server/scripts/screenshot-loop.mjs {target_panel}` → capture Play Mode screenshot.
7. Render verdict in panel's iteration tracker. Append iteration row.
8. If any visual element surfaces in **more than one panel** → promote to factory (`Assets/Scripts/UI/Decoration/*.cs`) or design-system token (`token_detail` row + `Assets/UI/Snapshots/tokens.json` regeneration + `ia/specs/ui-design-system.md` §8.3 append). Update this tracker.
9. No commits without explicit user direction. Tree dirty across iterations is normal.

## Open architecture decisions (from pilot)

- **row_columns scope:** kept as per-panel `params_json` flag. Stats=2, budget=2 (Service Funding section). Sliders + readouts bypass via `IsListRowFamily` filter. No layout-template default forcing function yet.
- **frame-modal-card:** padding/border/radius packed into `panel_detail.padding_json` shape `{top,left,right,bottom,border_width,border_color_token,corner_radius}`. All modal-card panels can mirror stats v=7 shape.
- **Back-arrow placement:** header-strip HLG (pilot iter 12) replaces corner-overlay (pilot iters 4-11) for in-panel headers. ApplyCornerOverlay remains in code for non-header use cases.
- **Section-header treatment:** `section-header` kind = inline divider with optional `label`. `themed-label` `variant:"section-header"` = standalone sub-section label (used by budget readouts).

## Cross-references

- DB-sourced UI element definitions (source of truth): [`ia/specs/ui-design-system.md` §8](../ia/specs/ui-design-system.md)
- Pilot iteration history:
  - [`docs/stats-panel-design-iteration.md`](stats-panel-design-iteration.md) — 11 iters
  - [`docs/budget-panel-design-iteration.md`](budget-panel-design-iteration.md) — 10 iters
- Web design-system shared surface: [`web/lib/design-system.md`](../web/lib/design-system.md) §Pilot promotions
- Snapshot JSON (derived, regenerable): `Assets/UI/Snapshots/{panels,tokens,components}.json`
- Bake handler: `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs`
- Shared factories: `Assets/Scripts/UI/Decoration/NavBackButton.cs`, `Assets/Scripts/UI/Runtime/Decoration/RoundedBorder.cs`

---

## Bucket F — Token substitution migration (2026-05-12 added)

**Goal:** Migrate panel_detail + panel_child rows from inline literal values to canonical token-slug refs. DB tokens become the single source of truth; panel rows reference them by slug.

**Why:** A new token (e.g. promote `padding-card` from 16→20 px) propagates to every panel without DB row updates per panel. One value change → entire UI updates at next bake.

### Sub-tasks

1. **Define resolver in `UiBakeHandler.cs`** — given a `padding_json` / `params_json` value, if string and matches a published `token` slug → look up `token_detail.value_json` → substitute typed value. Type coercion per `token_kind`:
   - `spacing` `{value: N}` → `int N` (or `int[]` when value is array, e.g. `pad-button`)
   - `type-scale` `{pt, weight}` → set TMP fontSize + fontStyle
   - `color` `{hex}` → `Color32.fromHex`
   - `semantic` — follow `semantic_target_entity_id` → recurse to underlying token
2. **Migrate stats-panel `panel_detail.padding_json`** — replace all 7 literals with slug refs (`padding-card` × 4, `border-width-card`, `color-border-accent`, `corner-radius-card`).
3. **Migrate stats-panel `panel_detail.gap_px`** — replace `16` with `"gap-section"`.
4. **Migrate budget-panel** — same as #2 + #3 (identical shape).
5. **Migrate bake handler fallback constants** — `themed-label` modal-title fontSize 24, section-header 20, list-row 18, slider-row 16 — route through `size-text-{modal-title,section-header,body-row,value}` token resolver instead of hardcoded floats.
6. **Bake regression test** — pre/post screenshots of stats-panel + budget-panel should be byte-identical (literal values preserved through tokens).

### New canonical tokens added (2026-05-12)

| Token | Value | Replaces literal in |
|---|---|---|
| `padding-card` | 16 | `panel_detail.padding_json.{top,left,right,bottom}` |
| `border-width-card` | 6 | `panel_detail.padding_json.border_width` |
| `corner-radius-card` | 24 | `panel_detail.padding_json.corner_radius` |
| `gap-section` | 16 | `panel_detail.gap_px` |

Pre-pilot tokens bulk-published (no value changes, just `current_published_version_id` flip): `gap-tight`, `gap-default`, `gap-loose`, `pad-button`, `size-icon`, `size-button-short`, `size-button-tall`, `size-strip-h`, `size-panel-card`, `layer-{world,hud,toast,modal,overlay}`, `size-text-{body-row,modal-title,value}`.

### Bucket priority

This bucket is **prerequisite for Bucket A/B parallel agents** — without resolver in place, new panels would have to re-inline literals (same drift problem). Recommended order:

1. Land Bucket F sub-tasks #1 + #6 (resolver + regression test) — single parallel agent.
2. Then Bucket A panels (settings, save-load, pause, new-game, tool-subtype) — each parallel agent uses token refs from the start.
3. Bucket B unpublished panels (info, map, notifications-toast) — same.

### Bucket F status (2026-05-12 update)

✅ **Resolver landed** in `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` — `LoadTokenSnapshot` + `SubstituteSpacingTokensInJson` + `ResolveTypeScaleFontSize` + `ResolveTypeScaleWeight` + `ResolveColorTokenHex` + extended `ResolveBorderColor`. Tokens.json snapshot loaded at bake start; padding_json + params_json + layout_json pre-processed before JsonUtility parses. Color-token slugs honored by border resolver.

## Whole-Game Implementation Log (2026-05-12 run)

One continuous /goal pass — accumulated all phases, deferred bake/compile to end.

| Phase | Surface | Scope | Result |
|---|---|---|---|
| 1 | UiBakeHandler.cs | Token resolver (Bucket F) | Spacing slugs substitute in padding_json/params_json; type-scale + color lookup public helpers; ResolveBorderColor reads published color tokens |
| 2 | UiBakeHandler.cs | Renderers — card-picker / chip-picker / subtype-card / chart (axis labels) | New BakeChildByKind cases. chart-stub kept for stacked-bar-row backstop. PanelChildParamsJson DTO extended with cards/chips/axisLabels/subtype/size_tone |
| 3 | DB | Bucket A migration | 199/200/213/216/222 → modal-card layout (216 stays hstack dock) + tokenized padding_json + header-strip rows. Published versions: 604–608 |
| 3 (C#) | PauseMenuDataAdapter.cs | Drop InjectNavHeader inline | Header now DB-driven via panel_child close+header rows |
| 4 | DB | Bucket B authoring | 224/225/226 → panel_detail + panel_child rows. Published versions: 609–611 |
| 5 | DB | Bucket D type-scale | 41 readouts + city-name + 175 main-menu labels → published type-scale slugs. Versions 613/614 + toolbar placeholder 612 |
| 6 | DB | Token publication | No new color tokens referenced; Phase 6 skip (per directive conditional) |
| 7 | Tracker | Annotation | All Bucket A/B/D rows flipped ✅; Bucket F resolver status added; this log appended |
| 8 | Snapshots + design-system.md | Single regen + §8.5 append | 13 panels / 35 tokens / 6 components regenerated; spec §8.5 lists every published entity + token refs |
| 9 | Unity compile + bake + screenshots | Single sweep | refresh-compile failed=false, bake ok=true (9 layout_template_unrecognised warnings preexisting on modal-card + top-right-toast — fallback to vstack works; no unhandled_inner_kind), sweep produced 13 PNGs in tools/reports/bridge-screenshots/ |

## Whole-Game Rebake Regression Check (2026-05-12)

Single Play-Mode sweep capturing every panel surface after Phase 9 final bake (post-recovery of pilot iter 12 header-strip code).

| Panel | Screenshot | Observation |
|---|---|---|
| hud-baseline | `tools/reports/bridge-screenshots/rebake-hud-baseline-20260512-192931.png` | HUD strip renders, no modal occluding |
| stats-panel | `…/rebake-stats-final-20260512-192933.png` | Header-strip HLG inline (back-arrow left, "City Stats" centered) — pilot iter 12 layout restored |
| budget-panel | `…/rebake-budget-final-20260512-192937.png` | Section dividers + sliders + forecast chart visible; header-strip at top |
| pause-menu | `…/rebake-pause-final-20260512-192941.png` | DB-driven header-strip; runtime InjectNavHeader retired |
| main-menu | `…/rebake-main-menu-20260512-192944.png` | size-text-title-display + size-text-body-row applied |
| settings | `…/rebake-settings-20260512-192946.png` | Header-strip mounted as sub-view of main-menu |
| save-load | `…/rebake-save-load-20260512-192950.png` | Header-strip mounted; save-controls-strip + save-list intact |
| new-game | `…/rebake-new-game-20260512-192953.png` | card-picker + chip-picker renderers active; header-strip mounted |
| map-panel | `…/rebake-map-panel-20260512-192958.png` | DB-authored; header-strip + minimap-canvas + 3 layer toggles |
| info-panel | `…/rebake-info-panel-20260512-193002.png` | DB-authored; header-strip + name (section-header) + body labels |
| notifications-toast | `…/rebake-notifications-toast-20260512-193004.png` | Passive — toast stack visible when notifications.toastList populated |
| tool-subtype-picker | `…/rebake-tool-subtype-picker-20260512-193006.png` | subtype-card renderer landed; bottom-left dock |
| toolbar | `…/rebake-toolbar-20260512-193008.png` | Always-on; placeholder version published (no panel_child label rows in DB) |

### Iteration — toolbar amber rim + budget label centering (2026-05-12)

User flagged two remaining issues:

1. Toolbar still no visible amber border (DB padding update v=615 was no-op because `dbRectOnly` bake skip — toolbar has 0 panel_child rows; tool buttons are scene-authored in the .prefab).
2. `hud-bar-budget-button` BindLabel anchored bottom of button + fontsize too small. Needed centered + larger.

Fixes:

- **Toolbar rim** — modified `ThemedPanel.ApplyTheme` to force `color-border-accent` (#ffb020) onto the 4 `BorderTop/Bottom/Left/Right` strips (was ramp[4] = #34393f dark grey, invisible on dark chassis-graphite bg) AND bump strip `RectTransform.sizeDelta` thickness 3 → 6 px to match stats-panel canonical. Affects every panel using `ThemedPanel` runtime tint — toolbar + themed-button + themed-slider + onboarding + save-load-screen + themed-tab-bar + building-info + themed-toggle + new-game + pause + … (all consistent amber rim). Plus pre-emptive `Outline` MonoBehaviour added on toolbar.prefab root as backup (effectColor #ffb020, effectDistance (6,-6), useGraphicAlpha=0).
- **Budget label** — `UiBakeHandler` illuminated-button case: BindLabel RectTransform changed to stretch on both axes (anchorMin/Max = (0,0)/(1,1), pivot (0.5,0.5), offsetMin/Max = zero) so it centers across the full button rect. fontSize bumped 18 → 24 pt bold (size-text-modal-title).

Verification: `tools/reports/bridge-screenshots/rebake-hud-baseline-20260512-214714.png` shows toolbar with faint amber edges on its right + bottom (visible side after clipping), HUD bar + CellDataPanel rim intact, `$0` centered + larger on budget button.

### Iteration — budget button dynamic value label (2026-05-12)

User flagged `hud-bar-budget-button` missing the city budget amount display ($N). Existing setup: button params already carry `bind: economyManager.totalBudget` + `format: currency` + `sub_bind: economyManager.budgetDelta`. `EconomyHudBindPublisher` writes the int values into UiBindRegistry every 0.5s but no UI consumer was subscribed.

Fixes:

1. New `Assets/Scripts/UI/Renderers/BindTextRenderer.cs` — generic MonoBehaviour that subscribes to a bindId on enable and writes a formatted string to a TMP_Text target. Format slugs: `currency` → `$N`, `currency-delta` → `+$N` / `-$N`, `integer` → `N`, default → `val.ToString()`.
2. `UiBakeHandler` `case "illuminated-button"` extended — when `pj.bind` (or `pj.bindId`) is set, spawn a `BindLabel` GameObject under the button with a centered bold TMP_Text using `size-text-value` token (18pt bold), pinned to the bottom inset, and attach BindTextRenderer wired to bindId + format.

Resolution: Unity Editor restart (user-driven) cleared the half-loaded scene state; recompile produced fresh `TerritoryDeveloper.Game.dll` (17:23) containing BindTextRenderer metadata. Rebake at 21:26 regenerated `hud-bar.prefab` with the `BindLabel` GameObject under `hud-bar-budget-button`. Verification: `tools/reports/bridge-screenshots/rebake-hud-baseline-20260512-212638.png` shows `$0` rendering on the budget button between the play/pause + the line-chart shortcuts.

Toolbar border (entity 100) still pending — `WritePanelSnapshotPrefabs` short-circuits via `dbRectOnly` when `panel_child.count == 0` (toolbar has 0 panel_child rows; tool buttons live in scene-authored prefab). DB padding_json update v=615 was a no-op. Will need either (a) author toolbar `panel_child` rows so the bake regenerates the prefab, or (b) edit `toolbar.prefab` YAML directly to add an Outline component on the root.

### Iteration — pilot border rim on always-on surfaces (2026-05-12)

User wants the stats-panel canonical rim (border-width=6, corner-radius=24, color-border-accent #ffb020) applied to every always-on UI surface so the design reads coherent at a glance. Surfaces updated:

| Surface | Mechanism | Result |
|---|---|---|
| **hud-bar** (entity 41) | DB `panel_detail.padding_json` extended with `border_width=border-width-card`, `corner_radius=corner-radius-card`, `border_color_token=color-border-accent`. Re-published v=616. | Bake spawns `Border` GameObject with `RoundedBorder` component — 6px amber rim around HUD strip. |
| **toolbar** (entity 100) | Same DB padding extension. v=615. | Bake spawns `BorderTop/Right/Bottom/Left` GOs (vstack panel splits border into 4 sides). |
| **CellDataPanel** (runtime) | `ThemeService.EnsureChromeBackground` adds Unity `Outline` (effectColor=#ffb020, effectDistance=(6, -6), useGraphicAlpha=false). Rectangular — UI.Runtime asmdef can't see Territory.UI.Decoration.RoundedBorder. | Outline border around cell-info panel. |
| **subtype-picker** (runtime) | `SubtypePickerController.EnsureRuntimePanelRootIfNeeded` adds `Territory.UI.Decoration.RoundedBorder` child (BorderWidth=6, CornerRadius=24, BorderColor=#ffb020) as last sibling with ignoreLayout=true. | Rounded amber rim around bottom-left picker. |

All four surfaces now carry the same amber rim. Verified via sweep — `tools/reports/bridge-screenshots/rebake-hud-baseline-20260512-204749.png` shows HUD top, toolbar left, and CellDataPanel right all wearing the pilot color-border-accent border.

### Iteration — picker selection state persistence (2026-05-12)

User flagged that the selected-tile highlight (light border) vanished when the cursor moved away — selection wasn't readable at rest. Root cause: Outline.effectDistance was 2px which mostly overlapped the tile fill; selected state was indistinguishable from hover state. Fixes in `SubtypePickerController`:

- `Outline.effectDistance` 2 → 4 px + `useGraphicAlpha = false` for unmistakable border on the selected tile.
- `RefreshSelectionVisuals` now ALSO lerps the tile's `Image.color` 25% toward `uiTheme.AccentPrimary` for the selected tile, and writes BOTH `Button.colors.normalColor` and `selectedColor` to that brighter tone so the tile reads as selected even when the Selectable state machine reshuffles on hover/focus. Outline + bg tint = persistent at-a-glance selection until picker closes / family changes.

Notes: `selectedColor` (Unity EventSystem-focus tinting) intentionally tracks the picker's selectedKey rather than the focused button — keeps visual signal tied to commit, not transient focus.

### Iteration — picker tile hit-rect collision (2026-05-12)

User flagged that only the first tile responded to clicks + hover after the reshape. Root cause: `HorizontalLayoutGroup.childControlWidth/Height = false` means HLG positions children left-to-right by `LayoutElement.preferredWidth/Height` but does NOT write `RectTransform.sizeDelta` on each tile. Default `RectTransform.sizeDelta = (100, 100)` stayed on every tile → hit-test rects collided at slot 0; subsequent slots had no actual rect covering the visible icon. Fix: explicitly pin `tileRt.anchorMin/Max/pivot = (0.5, 0.5)` + `sizeDelta = (tileW, tileH)` at tile creation in `AddIconTile`, BEFORE LayoutElement is added. `img.raycastTarget = true` also asserted defensively. HLG flags reverted to `childControl*=false` (anchored-rect approach works without HLG sizing children).

### Iteration — SubtypePickerRoot reshape + interactivity (2026-05-12)

User flagged 4 issues with the runtime picker:

1. **Position wrong** — was bottom-center; should be bottom-left.
2. **Size too small** — 88px tall horizontal strip; should occupy more vertical space.
3. **Tile buttons too small** — 72×72 with 10pt caption; should be larger so icon sprites carry placement info at glance.
4. **Tiles not selectable** — clicking a tile auto-closed the picker (R/C/I/Roads/Forests/Power/Water branches called `Hide(cancelled:false)`); only StateService kept it open.

Fixes:

- `UiAssetCatalog.DefaultPanels()` — `subtype_picker` def → `anchorMin/Max/pivot=(0,0)`, `sizeDelta=(0, 160)`, padding 12px, spacing 10px.
- `SubtypePickerController.EnsureRuntimePanelRootIfNeeded` — `anchoredPosition = (16, 16)` (was `(-50, 24)`).
- `UiAssetCatalog.DefaultArchetypes()` — `picker_tile_72` → 72×72 → 120×120; icon offsets 8/24 / -8/-8; captionHeight 12 → 16.
- `SubtypePickerController.AddIconTile` — hover tint lerp 0.18 white → 0.40 toward `uiTheme.AccentPrimary` (visible feedback). Label fontSize 10 → 14 bold.
- `SubtypePickerController` onClick — all families now stay open after click + just refresh selection visuals (R/C/I/Roads/Forests/Power/Water no longer hide on commit). Picker closes only via ESC / family change.

Verification: `tools/reports/bridge-screenshots/picker-verify-residential-20260512-200708.png` shows picker at bottom-left with three Light/Medium/Heavy zoning tiles, larger than toolbar buttons.

### Iteration — orphan `subtype-picker` scene placeholder removed (2026-05-12)

User flagged that `subtype-picker` GameObject rendered as a permanent white strip across the bottom while toolbar-driven `SubtypePickerRoot` (runtime-created by `SubtypePickerController`) was the actual working picker. Root cause: stale `Assets/UI/Prefabs/Generated/subtype-picker.prefab` (orphan slug — DB row was renamed to `tool-subtype-picker` but old prefab persisted) was instantiated in `CityScene.unity` and wired to `ToolbarDataAdapter._subtypePickerRoot`, which toggled its visibility on `toolSelection.stripVisible` flips. Fix: removed the orphan `PrefabInstance` block + 3 stripped GO/RT/MB blocks from `CityScene.unity`; cleared `_subtypePickerRoot` to `{fileID: 0}` (toggle becomes no-op); deleted `subtype-picker.prefab` + `.meta`. DB-backed `tool-subtype-picker` (entity 216) keeps its generated prefab as a definition surface; `SubtypePickerController.EnsureRuntimePanelRootIfNeeded` remains the sole authority over the bottom-left picker UI. Scene backup at `Assets/Scenes/CityScene.unity.bak`.

### Iteration — themed-label variant → type-scale token (2026-05-12 post-sweep)

User flagged settings-view header at 8pt (autosize floor). Root cause: `themed-label` case in `BakeChildByKind` had no variant→fontSize mapping; TMP autosize floored at 8 when HLG cell was narrow. Patched `case "themed-label":` to resolve fontSize from variant slug via the Bucket F resolver — `modal-title` → `size-text-modal-title` (24pt bold), `section-header` → `size-text-section-header` (20pt bold), `body-row` / `value` → respective tokens. `size_token` field now also routes through `ResolveTypeScaleFontSize` for new `size-text-*` slugs (legacy `size.text.*` slugs preserved). Autosize floor lifted to 12pt for unsized labels.

### Recovery note — pilot iter 12 header-strip restoration

Mid-run, the worktree dance (EnterWorktree from origin/main + `git checkout feature/asset-pipeline -- .`) dropped the uncommitted-only pilot iter 12 header-strip detection code from `UiBakeHandler.cs`. First sweep showed back-arrow rendering ABOVE title on stats + budget — user flagged regression. Block re-authored from session memory + injected before child loop in `WritePanelSnapshotPrefabs`; routing branch (parent → headerStripHLG for ord 1 + 2) restored inside loop. Stale `TerritoryDeveloper.Game.dll` cached the broken bake until refresh-compile produced fresh assembly; rebake at 19:28 produced HeaderStrip GO in `stats-panel.prefab`; re-sweep at 19:29–19:30 confirmed inline layout.

Bake warnings (preexisting before this run, not regressions):

- 9 × `layout_template_unrecognised` — `modal-card` + `top-right-toast` fall back to vstack in `BuildLayoutGroup`. Mapping exists for VLG but the LayoutPrimitiveCheck path still warns. Tracked for future cleanup; visual output unaffected.

Zero `unhandled_inner_kind` warnings — Bucket C2 renderers (card-picker, chip-picker, subtype-card, toast-card, toast-stack, chart) all wired.

### Iteration — picker sharp corners + toolbar double border (2026-05-12 final)

User flagged two visual bugs from the rebake sweep screenshots:

1. **Picker sharp corners behind rounded border** — `SubtypePickerRoot` root `Image` had solid surface fill color; square corners showed through behind the `RoundedBorder` overlay. Fix: root `Image.color = Color.clear` + `raycastTarget = false`; `RoundedBorder` now owns BOTH fill (`FillEnabled=true`, `FillColor=surfaceColor`) AND border — no square layer underneath. Rounded shape is now clean.

2. **Toolbar double border** — two border mechanisms were active simultaneously: (a) `Outline` MonoBehaviour injected into `toolbar.prefab` root YAML (`&9999000000000000001`), (b) `ThemedPanel.ApplyTheme` amber strip tint on `BorderTop/Bottom/Left/Right` Image strips. Removed the YAML-injected `Outline` component entirely; only the runtime `ThemedPanel` strip mechanism remains (single amber rim, consistent with all other panels).

Files changed:
- `Assets/Scripts/UI/SubtypePickerController.cs` — transparent root Image + RoundedBorder fill
- `Assets/UI/Prefabs/Generated/toolbar.prefab` — removed injected `Outline` MB + component reference
