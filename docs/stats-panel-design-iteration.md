# Stats-Panel Design Iteration Tracker

## Purpose

Running log of stats-panel definition changes during pilot. Captures DB version after each publish + bake screenshot path + visual verdict. Source for promoting tokens to whole-game design system after pilot approval.

## Phase 2A Audit Findings (2026-05-12)

- `panel_detail.padding_json` + `params_json` = `jsonb` no shape constraint → safe to extend with new keys
- `panel_detail.rect_json` = `jsonb` no shape constraint → safe
- Children live in `panel_child` table (21 rows for stats-panel — see migration 0137)
- `panel_child.params_json` requires `kind` discriminator (trigger from migration 0063)
- **No MCP update tool exists** for `panel_detail` / `panel_child` field writes — only `catalog_panel_publish` (version bump + 5 gates) and `ui_panel_get/list`. Phase 2B adds `panel_detail_update`.
- Bake flow: DB → `tools/scripts/snapshot-export-game-ui.mjs` → `Assets/UI/Snapshots/panels.json` → `UiBakeHandler.cs` → prefab
- Published-row gate: `current_published_version_id IS NOT NULL AND retired_at IS NULL`
- Stats-panel current state: entity_id=220, latest version_number=435 (pre-iteration)

## Current Definition

(rendered JSON dump from `catalog_panel_get stats-panel` — kept in sync after each publish)

### panel_detail (latest — version_id=559, version_number=4)

```json
{
  "layout_template": "modal-card",
  "layout": "vstack",
  "modal": true,
  "gap_px": 0,
  "padding_json": {
    "top": 16, "left": 16, "right": 16, "bottom": 16,
    "border_width": 6, "border_color_token": "led-amber", "corner_radius": 24
  },
  "params_json": {"width": 720, "height": 560, "defaultTab": "population", "row_columns": 2},
  "rect_json": {
    "anchor_min": [0.5, 0.5],
    "anchor_max": [0.5, 0.5],
    "pivot": [0.5, 0.5],
    "size_delta": [720, 560],
    "anchored_position": [0, 0]
  }
}
```

### panel_child override — stats-close (corner-pinned overlay)

`params_json`: `{"icon":"back-arrow","kind":"icon-button","action":"stats.close","corner":"top-left","tooltip":"Back","corner_size":40}`

`corner` + `corner_size` fields drive `ApplyCornerOverlay` in `UiBakeHandler.cs` — escapes panel VLG flow via `LayoutElement.ignoreLayout=true`, pins anchor/pivot to top-left, offsets by `padding.left + border_width = 22px`, shrinks rect to 40×40.

catalog_entity: `display_name = "City Stats"` (iter 3 — drives stats-header label via UiBakeHandler themed-label modal-title fallback).

### panel_child summary

21 rows: 1 header label, 1 close button, 1 tab-strip, 1 range-tabs, 3 charts (population/services/economy), 3 stacked-bar-rows, 11 service-rows (power/water/waste/police/fire/health/education/parks/transit/roads/happiness).

All service-rows tagged `tabGroup:"services"`. Charts + bars partitioned by `tabGroup` matching the active tab.

## Iteration Log

| # | Date       | Change                                                              | DB Version | Screenshot | Verdict |
| - | ---------- | ------------------------------------------------------------------- | ---------- | ---------- | ------- |
| 1 | 2026-05-12 | Initial border (2px led-amber, 4px radius) + 4px padding + 2-col rows | v=2 (id=557) | failed     | reject — no border, no radius, no 2-col visible |
| 2 | 2026-05-12 | Bake fixes: Border LayoutElement.ignoreLayout=true, RoundedBorder fill=on (panel-face dark), root bg Image alpha→0, RowGrid LayoutElement.preferredHeight finalized post-loop | v=2 (id=557, no DB change) | pending    | pending |
| 3 | 2026-05-12 | display_name="City Stats" wired into stats-header (themed-label modal-title fallback); stats-close swapped themed-button(icon-close) → icon-button(icon=back-arrow); border_width 2→6; corner_radius 4→24 | v=3 (id=558)  | reject     | Header + border OK, but items collide with thick border (padding too small) + stats-close still renders as full-width illuminated-button via VLG flow |
| 4 | 2026-05-12 | Padding 4→16 (clear border+radius collision); add `corner` + `corner_size` fields to PanelChildParamsJson DTO + `ApplyCornerOverlay` helper in UiBakeHandler.cs (escapes VLG flow via ignoreLayout=1, pins anchor/pivot, padding-aware offset); stats-close params_json adds `corner:"top-left", corner_size:40` | v=4 (id=559)  | reject     | back-arrow icon = white square (sprite `back-arrow` missing); overlay too big; header strip needs more breathing room below; stats-close + display_name should visually share header-strip band |
| 5 | 2026-05-12 | icon `back-arrow`→`left-arrow` (sprite exists in `Assets/Sprites/Buttons/`); corner_size 40→32; added `corner_offset` string field to PanelChildParamsJson + InvariantCulture "x,y" parser in ApplyCornerOverlay → stats-close `corner_offset:"22,38"` aligns icon center vertically with header-label center; panel gap_px 0→16 for header/tab-strip breathing room | v=5 (id=560)  | reject     | white square persists (resolver hardcoded `-target.png` suffix; `left-arrow.png` not matched); close-button sits below header band, not inside |
| 6 | 2026-05-12 | `ResolveButtonIconSprite` adds plain `Assets/Sprites/Buttons/{slug}.png` + `Assets/Sprites/{slug}.png` fallback (icon-only sprites without pressed-state pair); stats-close `corner_offset:"22,38"→"22,22"` → top edge y=22 matches themed-label modal-title header band (y=22..54, label height 32 from line 622) so close button center aligns with label center | v=6 (id=561)  | reject     | white square persists, sprite approach abandoned — user wants Settings-screen `<` TMP glyph promoted to official back-arrow, centralized for whole-game reuse |
| 11 | 2026-05-12 | Cross-panel side-effect of budget iter 10 variant-size mapping: stats modal-title header now 24pt bold (was autosize), service rows still 2-col, back-arrow still in header band. No regression. | (no DB change) | tools/reports/bridge-screenshots/stats-iter11-verdict.png | **accept (regression check)** — City Stats header readable + 2-col service rows + back-arrow + tabs + range-tabs all stable. |
| 10 | 2026-05-12 | Whole-game sweep: stats panel post-promotion. NavBackButton + RoundedBorder + corner-overlay + tabs all stable. 2-col service rows readable. | (no DB change) | tools/reports/bridge-screenshots/stats-panel-final-20260512-165336.png | **accept (final)** — stats pilot delivered. All 8 cross-panel promotion targets land. |
| 9 | 2026-05-12 | Promote `frame-modal-card` layout-template (cross-panel iter 7 of budget lands here too): `MapLayoutTemplate` + `MapLayoutTemplateToPanelKind` recognize `modal-card` formally | (no DB change) | tools/reports/bridge-screenshots/stats-iter9-verdict.png | **accept** — stats renders identical to iter 8, bake warning gone. modal-card layout-template stable across stats / budget / pause-menu. |
| 8 | 2026-05-12 | Type-scale promotion (cross-panel): hardcoded fontSize bumps in UiBakeHandler.cs — section-header 16→20, list-row primary+secondary 16→18, slider-row 14→16. Both panels rebake. | (no DB change) | tools/reports/bridge-screenshots/stats-iter8-verdict.png | **accept** — header + tabs + service rows clearly readable. Service Funding section labels now legible. Promotion lands. |
| 7 | 2026-05-12 | New shared factory `Assets/Scripts/UI/Decoration/NavBackButton.cs` (`Spawn(parent, size=40)` static — 40×40 dark chip 0.18/0.18/0.18/0.9 + `<` TMP glyph 24pt bold white, raycastTarget=false on label). `PauseMenuDataAdapter.InjectNavHeader` refactored to call factory + wire OnBack externally (27 lines → 2). New bake-time kind `back-button` in `UiBakeHandler.BakeChildByKind` — instantiates NavBackButton onto childGo (chip + label) + AttachUiActionTrigger(pj.action) + EnsureChildLayoutElement(size). DB: stats-close `kind:"icon-button" → "back-button"`, dropped `icon`/`corner_size`/`tooltip`, kept `corner:"top-left"`/`corner_offset:"22,22"`/`action:"stats.close"` | v=7 (id=562)  | tools/reports/bridge-screenshots/stats-iter7-verdict.png | **accept (frame)** — RoundedBorder + corner_radius + NavBackButton + 2-col grid + City Stats header all render. Caveat: row text too small (type-scale token deferred to next iter). |

## Open Decisions

- **Border color promote**: keep raw `led-amber` light stop, or seed dedicated `color-border-accent` token referencing the same hex?
- **Type-scale extension**: which body/row/value tokens to add to design system? Current 3 tokens insufficient — need `size-text-body-row`, `size-text-value`, possibly `size-text-section-header`.
- **Font-family token**: currently TMP `LiberationSans` default — promote to DB token before whole-game rebake?
- **Padding split**: keep border fields packed into `padding_json`, or migrate to separate `frame_json` column in a later round?
- **row_columns scope**: panel-level flag means every list-row family run gets 2-col. If a future panel needs mixed (some single, some double), promote to per-slot annotation.

## Promotion Checklist (to whole-game)

- [ ] `color-border-accent` token sourced from final yellow choice (or keep `led-amber` ref)
- [ ] `size-text-body-row` / `size-text-value` tokens published
- [ ] `frame-modal-card` definition (padding/border/radius) generalized across all `layout_template='modal-card'` panels
- [ ] `UiBakeHandler` reads frame fields through `pj.size_token` / `pj.color_token` (no hardcoded floats)
- [ ] `row_columns` semantic promoted from per-panel param to layout-template default
- [ ] Whole-game rebake passes via `npm run unity:bake-ui` + smoke verify

## Critical Files (this round)

- `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` — `PanelPaddingJson` DTO + border wiring + row-grouping pass
- `Assets/Scripts/UI/Runtime/Decoration/RoundedBorder.cs` — custom `MaskableGraphic` mesh
- `tools/mcp-ia-server/src/db/ui-catalog.ts` — shared DAL (new)
- `tools/mcp-ia-server/src/tools/ui-panel.ts` — adds `panel_detail_update`
- `tools/scripts/snapshot-export-game-ui.mjs` — must propagate new `padding_json` keys to `panels.json`

## Final Promotion Resolutions (2026-05-12)

See `docs/budget-panel-design-iteration.md` §Final Promotion Resolutions — all 8 cross-panel items resolved. Stats pilot inherits same surface.
