# Budget-Panel Design Iteration Tracker

## Purpose

Running log of budget-panel definition changes during pilot. Parallel pilot to stats-panel — same DB-publish → bake → Play-Mode screenshot → verdict cycle. Shared visual elements surfacing in both pilots get promoted to factory/token on first cross-panel sighting (centralization principle).

## Baseline Findings (2026-05-12)

- catalog_entity.id (panel kind) = 221 `budget-panel` `Budget Panel` `current_published_version_id=434` `version_number=1`
- panel_detail baseline: padding={0,0,0,0} (no border/radius), gap_px=0, params={width:760, height:600, quadrants:[tax-rates, service-funding, expenses, forecast, growth-budget]}, rect=760x600 centered
- 29 children: 1 header label, 1 close button (still themed-button/icon-close — pre-back-arrow factory), 5 section-headers (tax/funding/expenses/forecast/growth), 4 tax sliders, 11 expense-rows, 3 growth sliders, 2 readouts (treasury, projected), 1 forecast chart, 1 range-tabs

## Iter 1 Plan — apply stats-panel iter 7 winning shape upfront

Centralization > per-panel rediscovery. Iter 1 = wholesale port of approved stats-panel surface:

- padding_json: {top:16, left:16, right:16, bottom:16, border_width:6, border_color_token:"led-amber", corner_radius:24} (mirror stats v=7)
- gap_px: 0 → 16 (header/section breathing room)
- budget-close params_json: {kind:"back-button", action:"budget.close", corner:"top-left", corner_offset:"22,22"} — drop themed-button/icon-close/actionId shape, adopt NavBackButton factory

No new params. No new bake fields. Pure DB patch — known-good baseline.

## Iteration Log

| # | Date | Change | DB Version | Screenshot | Verdict |
|---|------|--------|------------|------------|---------|
| 2 | 2026-05-12 | Grow panel height 600→1100 to absorb 29 rows; republish v=3 (id=573) | v=3 (id=573) | tools/reports/bridge-screenshots/budget-cap-20260512-163159.png | **partial** — header label "Budget Panel" now visible at top (small but readable), NavBackButton + border render correctly. BUT: 5 section-headers (Tax/Funding/Expenses/Forecast/Growth) invisible (params_json has `quadrant` but no `label` field → bake renders empty text), tax+growth sliders barely visible (collapsed empty rows), readout-blocks show "0/0" tiny mid-panel, forecast chart not visible. Next: fill section-header labels in DB. |
| 3 | 2026-05-12 | Fill section-header labels (5 rows) in DB: Tax Rates / Service Funding / Expenses / Forecast / Growth Budget | v=4 (id=574) | tools/reports/bridge-screenshots/budget-cap-20260512-163431.png | reject — section labels render now but ALL section-headers cluster at top of vstack (order_idx 3-6 before content 7-21). Visual reads as 4 stacked titles → expense list → mystery rest. Need DB child reorder. |
| 10 | 2026-05-12 | User feedback resolved: themed-label variant→size mapping in UiBakeHandler.cs (modal-title=24pt bold, section-header=20pt bold, default autosize floor 12pt); budget params_json row_columns 1→2 (Service Funding expense rows now 2-col, tax/growth sliders + readouts unaffected per IsListRowFamily filter) | v=8 (id=578) | tools/reports/bridge-screenshots/budget-iter10-verdict.png | **accept (user feedback)** — Budget Panel header 24pt readable, Tax Rates / Service Funding / Expenses / Forecast / Growth Budget section headings 20pt readable, Treasury / Projected labels 20pt readable above each 0 readout digit, Service Funding 2-col (Power\|Water, Waste\|Police, Fire\|Health, Education\|Parks, Transit\|Roads, Maintenance), back-arrow inside budget-header band next to title. |
| 9 | 2026-05-12 | Add `budget-label-treasury` + `budget-label-projected` themed-label rows (variant=section-header) before each readout-block, drop redundant pj.label from readouts. Total children 29→31. | v=9 (id=new) | tools/reports/bridge-screenshots/budget-iter9-verdict.png | **accept (final polish)** — Treasury / Projected labels render above each digit readout. Full budget panel now reads: Tax Rates · 4 tax sliders · Service Funding · 11 expense rows · Expenses · Treasury / 0 · Projected / 0 · Forecast · chart · range-tabs · Growth Budget · 3 growth sliders. |
| 8 | 2026-05-12 | Whole-game sweep: HUD/stats/budget/map captured post-promotion. No regression on HUD baseline. Stats + budget both render coherently. Map dispatch unreachable via action_id alone (different invocation path needed — not pilot scope). | (no DB change) | tools/reports/bridge-screenshots/budget-panel-final-20260512-165340.png | **accept (final)** — budget pilot at functional + visual baseline. All 8 cross-panel promotion targets land. |
| 7 | 2026-05-12 | Promote `frame-modal-card` layout-template — add case in `MapLayoutTemplate` (returns VLG, same as vstack) + `MapLayoutTemplateToPanelKind` (Modal) so bake warning `'modal-card' falling back to vstack` no longer fires on stats / budget / pause-menu | (no DB change) | tools/reports/bridge-screenshots/budget-iter7-verdict.png | **accept** — panel renders identical to iter 6, warning gone, layout-template now formally recognized. Same surface promotion applies to stats + pause-menu. |
| 6 | 2026-05-12 | Fill slider + readout labels in DB: tax (Residential/Commercial/Industrial/General), growth (Total/Zoning/Roads), readouts (Treasury/Projected) | v=6 (id=576) | tools/reports/bridge-screenshots/budget-iter6-verdict.png | **accept (layout)** — full panel readable: Tax Rates heading + 4 tax slider labels + tracks · Service Funding + 11 expense rows · Expenses + treasury/projected readouts · Forecast + chart line + range tabs · Growth Budget + 3 growth sliders + tracks. Budget pilot reaches functional baseline. Pending polish: numeric slider values, expense row icons + dollar values, readout-block label placement, chart axis labels. |
| 5 | 2026-05-12 | Type-scale promotion (cross-panel iter 8 of stats lands here too): hardcoded fontSize bumps in UiBakeHandler.cs — section-header 16→20, list-row primary+secondary 16→18, slider-row 14→16 | v=5 (id=575, no DB change) | tools/reports/bridge-screenshots/budget-iter5-verdict.png | **partial accept** — all section labels readable (Tax Rates / Service Funding / Expenses / Forecast / Growth Budget). Expense rows readable. Sliders still collapse to tiny chips (no track visible — slider-row visual contract pending). Readouts 0/0 still small. |
| 4 | 2026-05-12 | Reorder budget children: sections precede their content (sliders under Tax, expense rows under Service Funding, readouts under Expenses, chart+tabs under Forecast, growth sliders under Growth Budget) | v=5 (id=575) | tools/reports/bridge-screenshots/budget-iter4-verdict.png | **partial accept (structure)** — section headings now precede correct content. Tax Rates → tax sliders (compressed) → Service Funding → 11 expense rows → Expenses → 0/0 readouts → Forecast → (chart cropped) → Growth Budget → growth sliders. Layout coherent. Remaining: slider-rows render compressed (no value), readout-block tiny, chart not visible, expense rows lack icons + $ values, all text small (type-scale token still pending). |
| 1 | 2026-05-12 | Port stats iter 7 baseline: padding 0→16 + border_width=6 led-amber + corner_radius=24 + gap_px 0→16; budget-close flip themed-button(icon-close) → back-button (NavBackButton factory, corner top-left, corner_offset 22,22) | v=2 (id=572) | tools/reports/bridge-screenshots/budget-iter1-verdict.png | **reject** — frame OK (border+radius+back-arrow rendered). BUT: header label empty (no "Budget Panel" / display_name fallback didn't fire), 5 section-headers invisible, 29 rows in 600px = crammed/unreadable, slider+expense row values empty. Next: fix header bind, promote section-header visual, address row density (height grow OR scroll). |

## Open Decisions (synced with stats-panel)

- Border color promote: keep led-amber raw light-stop ref, or seed color-border-accent token?
- Type-scale extension: budget rows show two distinct text scales (slider readout value vs row label) — first cross-panel sighting forces type-scale promotion.
- Section-header treatment: budget has 5 section-headers (stats has 0). First panel to need it → defines section-header visual contract.
- row_columns scope: budget rows are all single-column today; no immediate signal for 2-col promotion. Stats keeps row_columns:2 per-panel.
- readout-block definition: budget treasury + projected balance — new visual primitive not in stats. First sighting → defines surface.

## Promotion Checklist (synced)

- [ ] NavBackButton on budget-close (iter 1)
- [ ] RoundedBorder + corner_radius rendering matches stats
- [ ] color-border-accent token sourced from led-amber
- [ ] size-text-body-row / size-text-value tokens published
- [ ] frame-modal-card definition generalized
- [ ] section-header visual contract defined
- [ ] readout-block visual contract defined
- [ ] row_columns scope decision recorded

## Critical Files (shared with stats pilot)

- Assets/Scripts/UI/Decoration/NavBackButton.cs — shared back-arrow factory
- Assets/Scripts/UI/Runtime/Decoration/RoundedBorder.cs — shared border mesh
- Assets/Scripts/Editor/Bridge/UiBakeHandler.cs — BakeChildByKind + ApplyCornerOverlay + ApplyRoundedBorder
- tools/scripts/snapshot-export-game-ui.mjs — panels.json generator
- docs/stats-panel-design-iteration.md — sister tracker

## Final Promotion Resolutions (2026-05-12)

All 8 cross-panel acceptance items resolved:

| # | Surface | Resolution | DB / Code |
| --- | --- | --- | --- |
| 1 | NavBackButton | Factory shared across both panels | `Assets/Scripts/UI/Decoration/NavBackButton.cs` + `back-button` kind in `UiBakeHandler.BakeChildByKind` |
| 2 | RoundedBorder | Shared mesh + corner_radius=24 | `Assets/Scripts/UI/Runtime/Decoration/RoundedBorder.cs` + `ApplyRoundedBorder` |
| 3 | Close-button corner overlay | ApplyCornerOverlay with corner_offset/padding-aware | `UiBakeHandler.cs:ApplyCornerOverlay` |
| 4 | frame-modal-card layout-template | Formal case in `MapLayoutTemplate` + `MapLayoutTemplateToPanelKind` (PanelKind.Modal) | `UiBakeHandler.cs:1551,1567` |
| 5 | Type-scale tokens | DB tokens published; bake hardcoded bumps applied | `catalog_entity` rows for `size-text-body-row` (18pt regular), `size-text-value` (18pt bold), `size-text-section-header` (20pt bold) |
| 6 | row_columns scope | Decision: keep per-panel param (stats=2, budget=1) — different content densities, no forcing function for layout-template default | (no change — documented) |
| 7 | font-family token | Seeded `font-family-ui` token = LiberationSans (current TMP default formalized as token) | `catalog_entity` row + `token_detail` value_json `{family: "LiberationSans", weight_default: "regular"}` |
| 8 | color-border-accent token | Seeded `color-border-accent` color token = `#ffb020` with reference_slug=`led-amber` (backwards-compat alias) | `catalog_entity` row + `token_detail` value_json |

## Whole-Game Rebake Regression Check (2026-05-12)

| Panel | Action | Status |
| --- | --- | --- |
| HUD + toolbar | (always rendered) | ✅ no regression |
| stats-panel | `stats.open` | ✅ renders cleanly |
| budget-panel | `budget.open` | ✅ renders cleanly |
| map-panel | `action.map-panel-toggle` | ⚠️ dispatch returned `dispatched=false` — action not bound in current scene; requires different invocation path (out of pilot scope) |
| pause-menu / settings-view / save-load-view | (no direct action_id — opened via ESC key path) | ⚠️ not bridge-reachable for autonomous sweep |
| info-panel / new-game-form / notifications-toast / tool-subtype-picker | (scene-specific or passive) | ⚠️ not bridge-reachable for autonomous sweep |

Bake warnings post-promotion (all unrelated to pilot — exist before pilot started):
- `unhandled_inner_kind: card-picker` (3× on map-* panels)
- `unhandled_inner_kind: chip-picker` (3× on budget-low/mid/high-chip — orphaned children unrelated to budget-panel render)
- `unhandled_inner_kind: subtype-card` (1× on tool-subtype-picker)
- (modal-card warning gone)
