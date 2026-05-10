# cityscene-mainmenu-panel-rollout × bake-pipeline-hardening — impact cross-walk

Cross-walk pending Stage 6.0/7.0/8.0/9.0 tasks vs deliverables shipped by `bake-pipeline-hardening` plan (5 stages closed). Goal: flag §Plan Digest bodies that need edits before next ship-cycle so tasks pick up new blueprint, validators, KindRenderer matrix, and apply-time render-check pattern instead of inventing parallel surfaces.

## Bake-pipeline-hardening deliverables (recap)

| Stage | Deliverable | Surface impact |
|---|---|---|
| 1 | `bridge.console-log` + `validate_panel_blueprint` MCP for ship-plan + bridge command allowlist | Ship-plan branch must call validator pre-bake when `task_kind: ui_from_db` |
| 2 | `KindRendererMatrix Dictionary<string,IKindRenderer>` + `SlotAnchorResolver` (suffix BFS) + `validate:ui-id-consistency` + `validate:bake-handler-kind-coverage` | Every new kind in `panels.json params_json.kind` must register in matrix or BakeChildByKind switch — drift gate RED otherwise |
| 3 | `task_kind` yaml enum + canonical blueprint `ia/templates/blueprints/ui-from-db.md` (5 H2: Schema-Probe / Bake-Apply / Render-Check / Console-Sweep / Tracer) + ship-plan branch | DB-seed/panel-publish tasks must adopt blueprint; ship-plan loads it when `task_kind=ui_from_db` |
| 4 | Apply-time render-check pattern in `SettingsViewController` (12 widgets + bind-counts non-zero) + PlayMode tracer test | Adapter wiring tasks must use this pattern, not bespoke validation |
| 5 fix-up | Real `Slider`/`Toggle`/`TMP_Dropdown` components in `SliderRowRenderer`/`ToggleRowRenderer`/`DropdownRowRenderer` (under `Assets/Scripts/Editor/UiBake/KindRenderers/`); `SlotAnchorResolver` moved to `Territory.UI` runtime (`Assets/Scripts/UI/SlotAnchorResolver.cs`); `validate_panel_blueprint` rewritten behavioral; `console:scan` chained into `verify:local`; `ListRowRenderer` + `SectionHeaderRenderer` shipped | TECH-27335 scope already covered — see redundancy callout |

## Summary table

| task_id | stage | current_kind | needs_edit | new_kind | impact |
|---|---|---|---|---|---|
| TECH-27082 | 6.0 | (none) | Y | `ui_from_db` | DB seed → blueprint mandatory |
| TECH-27083 | 6.0 | (none) | Y | `ui_from_db` | 5 new archetype kinds → matrix + drift-validator allowlist |
| TECH-27084 | 6.0 | (none) | N | (keep design_only) | Pure C# manager — no panel surface |
| TECH-27085 | 6.0 | (none) | Y | `ui_from_db` | Adapter wiring → render-check pattern |
| TECH-27086 | 6.0 | (none) | Y | `ui_from_db` | Scene-wire → console:scan-clean expectation |
| TECH-27087 | 7.0 | (none) | Y | `ui_from_db` | DB seed → blueprint mandatory |
| TECH-27088 | 7.0 | (none) | Y | `ui_from_db` | 3 new archetype kinds → matrix + drift-validator |
| TECH-27089 | 7.0 | (none) | Y | `ui_from_db` | Adapter wiring → render-check pattern |
| TECH-27090 | 7.0 | (none) | Y | `ui_from_db` | DB action wire → blueprint Schema-Probe + Console-Sweep |
| TECH-27091 | 7.0 | (none) | Y | `ui_from_db` | Scene-wire → console:scan-clean expectation |
| TECH-27092 | 8.0 | (none) | Y | `ui_from_db` | DB seed → blueprint mandatory |
| TECH-27093 | 8.0 | (none) | Y | `ui_from_db` | `modal-card` archetype → matrix + drift-validator |
| TECH-27094 | 8.0 | (none) | Y | `ui_from_db` | Host-adapter slot-mount → SlotAnchorResolver `Territory.UI` |
| TECH-27095 | 8.0 | (none) | N | (keep design_only) | Pure C# UIManager + ModalCoordinator wiring |
| TECH-27096 | 8.0 | (none) | Y | `ui_from_db` | Scene-wire → console:scan-clean expectation |
| TECH-27335 | 8.0 | (none) | CANCEL/SHRINK | n/a | Stage 5 fix-up overlap — see callout |
| TECH-27097 | 9.0 | (none) | Y | `ui_from_db` | DB seed → blueprint mandatory |
| TECH-27098 | 9.0 | (none) | Y | `ui_from_db` | 5 new HUD archetype kinds → matrix + drift-validator |
| TECH-27099 | 9.0 | (none) | Y | `ui_from_db` | Adapter wiring → render-check pattern (info-panel) |
| TECH-27100 | 9.0 | (none) | Y | `ui_from_db` | Adapter wiring → render-check pattern (map-panel) |
| TECH-27101 | 9.0 | (none) | N | (keep design_only) | Pure C# notification manager + CityStats |

Rollup: **17/21 need edit**, **3 keep design_only** (managers-only), **1 cancel/shrink** (TECH-27335).

---

## TECH-27335 — redundancy callout (CANCEL/SHRINK)

Stage 5 fix-up shipped real `Slider`/`Toggle`/`TMP_Dropdown` components inside `Assets/Scripts/Editor/UiBake/KindRenderers/{SliderRowRenderer,ToggleRowRenderer,DropdownRowRenderer}.cs` plus `ListRowRenderer.cs` + `SectionHeaderRenderer.cs`. `BakeChildByKind` switch in `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` already has case arms for `slider-row` (L591), `toggle-row` (L616), `dropdown-row` (L635), `section-header` (L659), `list-row` (L671). Drift gate `validate:bake-handler-kind-coverage` already shipped.

Work-items 1, 3, 4, 5 of TECH-27335 = **already done**. Only residual: Work-item 2 — bind-binder MonoBehaviour subscribing `bindRegistry.Subscribe<T>(bindId, applyToWidget)` + dispatching widget→bind on user edit. Verify whether `UiBindBinder` emerged in Stage 4 SettingsViewController render-check pattern; if yes → cancel TECH-27335 outright. If no → shrink TECH-27335 to single work-item: "ship `UiBindBinder` MonoBehaviour wired by KindRenderers". Recommended: spawn verify pass against `SettingsViewController` Stage 4 commit before deciding cancel-vs-shrink.

Action: caveman flip TECH-27335 to `cancelled` with rationale "Stage 5 fix-up overlap" OR rewrite as 1-work-item bind-binder shell task. Keeps Stage 8.0 unblocked either way.

---

## Stage 6.0 — stats-panel

### TECH-27082 — DB seed migration stats-panel + 21 children

**Current digest summary.** Migration `0126_seed_stats_panel.sql` seeds `catalog_entity(slug=stats-panel, kind=panel)` + `panel_detail(layout_template=modal-card)` + 21 panel_child rows (header, close, tab-strip, range-tabs, 3 line-charts, 3 stacked-bar-rows, 11 service-rows). No blueprint reference, no validator call.

**Required edits.**
- Set yaml `task_kind: ui_from_db` so ship-plan loads `ia/templates/blueprints/ui-from-db.md` 5-section template.
- §Plan Digest must contain the 5 H2: `Schema-Probe` (assert seed inserts visible via `catalog_panel_get`), `Bake-Apply` (no bake here — passthrough), `Render-Check` (deferred to T6.0.5), `Console-Sweep` (db migration must not log warn), `Tracer` (migration up + down).
- Add explicit step: `mcp__territory-ia__validate_panel_blueprint {panel_id: stats-panel}` returns `{ok:true, missing:[]}` after migration applies. Validator is keyed by `params_json.kind` so the migration must populate that field on every panel_child.
- Mention `modal-card` layout template depends on T8.0.2 archetype publish (or accept legacy root pattern per T8.0.2 NOTE).

**Body delta.** Medium — body grows from ~10 lines to ~30 lines (5 H2 stubs + validator step).

### TECH-27083 — 5 archetype catalog rows + 5 case arms

**Current digest summary.** Migration `0127_seed_stats_archetypes.sql` + 5 case arms in `Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs` for `tab-strip`, `chart`, `range-tabs`, `stacked-bar-row`, `service-row`. IR DTO + JsonUtility round-trip tests.

**Required edits.**
- Set `task_kind: ui_from_db`.
- Each new kind (`tab-strip` / `chart` / `range-tabs` / `stacked-bar-row` / `service-row`) must register in **EITHER**:
  - `KindRendererMatrix` Dictionary at `Assets/Scripts/Editor/UiBake/KindRendererMatrix.cs` with concrete `IKindRenderer` impls under `Assets/Scripts/Editor/UiBake/KindRenderers/`, **OR**
  - `BakeChildByKind` switch in `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` + add to `_knownKinds` HashSet in `UiBakeHandler.Archetype.cs`.
- Add §Plan Digest §Console-Sweep step: `validate:bake-handler-kind-coverage` exit 0 (coverage gate — fast-path-map registered).
- Add NormalizeChildKind alias entry only if a panels.json producer emits a synonym kind name.
- Render-Check step: invoke `unity_bridge_command bake_panel {panel_id: stats-panel}` then `prefab_inspect` confirms each new kind ships its target component (chart → LineRenderer or RawImage, range-tabs → 3 Toggle children with group, stacked-bar-row → segmented Image stack, service-row → icon Image + 2 TMP labels).

**Body delta.** Large — body grows from ~10 lines to ~40 lines (per-kind component-conformance assertions + drift-validator call).

### TECH-27084 — ModalCoordinator + TimeManager.SetModalPauseOwner

**Current digest summary.** Author `Assets/Scripts/UI/Modals/ModalCoordinator.cs` + extend `TimeManager` with `SetModalPauseOwner` / `ClearModalPauseOwner`. Pure manager + scene-wire defer to T6.0.5.

**Required edits.** None. Pure C# manager surface — no DB seed, no bake, no panels.json kind. Keep `design_only`.

**Body delta.** Zero.

### TECH-27085 — StatsHistoryRecorder + StatsPanelAdapter + Editor menu stub

**Current digest summary.** Author `StatsHistoryRecorder` (monthly tick subscribe + ring buffer) + `StatsPanelAdapter` (~25 binds + range-tabs + register `stats.open` action). Stub Editor menu.

**Required edits.**
- Set `task_kind: ui_from_db` — adapter is the apply-time render-check site.
- §Plan Digest §Render-Check must mirror `SettingsViewController` Stage 4 pattern: after panel instantiated, adapter asserts (a) widget count ≥ 25, (b) `bindRegistry.Subscribe` call count > 0 per series, (c) `KindRendererMatrix` resolved every child kind (no null-renderer). Failure → throw at apply-time, surfaced via `bridge.console-log`.
- Use `SlotAnchorResolver.ResolveByPanel(panelRoot, panelSlug)` from `Territory.UI` runtime namespace (NOT Editor namespace) for child lookups in adapter.
- §Console-Sweep clean (no warn lines) is now CI-gated (`console:scan` chained into `verify:local`).
- §Tracer: PlayMode test asserting all 25 binds non-zero count after `stats.open`.

**Body delta.** Medium — body grows from ~12 lines to ~30 lines (render-check assertions + 5 H2 stubs).

### TECH-27086 — Scene-wire stats-panel under Canvas + Editor menu

**Current digest summary.** Mount baked stats-panel prefab under `CityScene.unity` Canvas via `unity_bridge_command instantiate_prefab` + author `Territory > UI > Open Stats` Editor menu firing `stats.open`.

**Required edits.**
- Set `task_kind: ui_from_db`.
- §Render-Check: after instantiate, `findobjectoftype_scan` confirms ModalCoordinator MonoBehaviour mounted + stats-panel root inactive by default.
- §Console-Sweep: closed-loop scenario — Editor menu fires action → `console:scan` clean → modal renders → close → console clean. Bridge command `bridge.console-log` capture proves it.
- §Tracer: closed-loop test scenario via `unity:testmode-batch --filter StatsPanelOpen.*` (PlayMode) — Editor menu trigger reachable from batch.

**Body delta.** Small — body grows from ~7 lines to ~18 lines.

---

## Stage 7.0 — budget-panel

### TECH-27087 — DB seed budget-panel + 25 children

**Current digest summary.** Migration `0128_seed_budget_panel.sql` seeds budget-panel + 25 panel_child (header + close + 4 sections + 4 tax slider-rows + 11 expense-rows + 2 readout-blocks + chart + range-tabs).

**Required edits.**
- Set `task_kind: ui_from_db`.
- 5 H2 stub blueprint identical to TECH-27082.
- `validate_panel_blueprint {panel_id: budget-panel}` returns `missing:[]`.
- Note that `chart` + `range-tabs` archetypes inherit from Wave B2 (T6.0.2) — Schema-Probe must verify those rows already published before this migration runs.

**Body delta.** Medium — ~10 → ~30 lines.

### TECH-27088 — 3 archetype rows + 3 case arms (slider-row-numeric, expense-row, readout-block)

**Current digest summary.** Migration `0129_seed_budget_archetypes.sql` + 3 case arms (`slider-row-numeric`, `expense-row`, `readout-block`). Chart + range-tabs reused from T6.0.2.

**Required edits.**
- Set `task_kind: ui_from_db`.
- `slider-row-numeric` is a **new kind** distinct from existing `slider-row` (Stage 5 fix-up). Two paths:
  1. Register dedicated `SliderRowNumericRenderer` under `Assets/Scripts/Editor/UiBake/KindRenderers/` + add to `KindRendererMatrix`.
  2. Or extend `SliderRowRenderer` to honor a `params_json.numeric=true` flag and alias `slider-row-numeric → slider-row` in `NormalizeChildKind` + `aliases` map of `tools/scripts/validate-bake-handler-kind-coverage.mjs`.
  Recommend path 2 (alias) — token-cheap, no new renderer class.
- `expense-row` + `readout-block` → new renderers OR `BakeChildByKind` switch arms.
- §Console-Sweep: `validate:bake-handler-kind-coverage` exit 0; alias map updated if path 2 taken.
- IR DTO round-trip + per-kind component-conformance Render-Check.

**Body delta.** Medium — ~10 → ~28 lines.

### TECH-27089 — BudgetForecaster + BudgetPanelAdapter ~40 binds

**Current digest summary.** Author `BudgetForecaster.Recompute` (3-month projection) + `BudgetPanelAdapter` (~40 binds + register `budget.open` + dispatch `taxRate.set` on close).

**Required edits.**
- Set `task_kind: ui_from_db`.
- Apply-time render-check identical to TECH-27085: ≥40 binds subscribed, all kinds resolved by `KindRendererMatrix`, slider→bind→forecaster chain reachable.
- Use `SlotAnchorResolver.ResolveByPanel` from `Territory.UI` for child lookups.
- §Tracer PlayMode: drag tax-R slider → forecast bind delta within next frame — verifiable via `unity_bridge_get` polling on bind value.

**Body delta.** Medium — ~12 → ~30 lines.

### TECH-27090 — wire `budget.open` action onto `hud-bar-budget-readout` child

**Current digest summary.** Migration `0130_wire_hud_bar_budget_readout_action.sql` updates `panel_child.layout_json` to set `params_json.action=budget.open` on existing `hud-bar-budget-readout` row. BLOCK-CALL-OUT escalates to game-ui-catalog-bake plan if absent.

**Required edits.**
- Set `task_kind: ui_from_db`.
- §Schema-Probe must `catalog_panel_get hud-bar` then assert `hud-bar-budget-readout` child exists with current `layout_json.params_json.action` field empty/null.
- §Render-Check: re-bake hud-bar via `unity_bridge_command bake_panel`; `prefab_inspect` confirms `UiActionTrigger` MonoBehaviour wired with `actionId=budget.open` on the readout child (parallels Stage 4.5 fix-up commit `fb858200`).
- §Console-Sweep: click readout → `console:scan` clean (no "action drop on the floor" warn).

**Body delta.** Small-Medium — ~10 → ~22 lines.

### TECH-27091 — scene-wire budget-panel under Canvas

**Current digest summary.** Mount baked budget-panel prefab under `CityScene.unity` Canvas via `unity_bridge_command instantiate_prefab`.

**Required edits.** Same shape as TECH-27086 — `ui_from_db` blueprint + 5 H2 stubs + `console:scan`-clean closed-loop scenario `unity:testmode-batch --filter BudgetPanelOpen.*`.

**Body delta.** Small — ~6 → ~16 lines.

---

## Stage 8.0 — pause-menu

### TECH-27092 — DB seed pause-menu + 7 children

**Current digest summary.** Migration `0131_seed_pause_menu.sql` seeds pause-menu + `panel_detail(layout_template=modal-card)` + 7 panel_child (title + 6 buttons). Sub-views mounted via `pause.contentScreen` enum bind reusing settings-view + save-load-view.

**Required edits.**
- Set `task_kind: ui_from_db`.
- 5 H2 blueprint stubs.
- `validate_panel_blueprint {panel_id: pause-menu}` returns `missing:[]`.
- §Schema-Probe must also assert `modal-card` archetype rows already published (depends on T8.0.2).
- §Render-Check covers the slot mount: 3 sub-view slots discoverable via `SlotAnchorResolver.ResolveByPanel(pause-menu, slotSuffix)` (suffix-match BFS — Stage 2 deliverable).

**Body delta.** Medium — ~10 → ~28 lines.

### TECH-27093 — modal-card archetype + 1 case arm

**Current digest summary.** Migration `0132_seed_modal_card_archetype.sql` + 1 case arm in `UiBakeHandler.Archetype.cs` for `modal-card` (root container with backdrop + center-anchor + content-replace slot). NOTE: Wave B2/B3 panels may need re-bake.

**Required edits.**
- Set `task_kind: ui_from_db`.
- Register `modal-card` in `KindRendererMatrix` (treat as outer-container kind — likely covered by `OUTER_KIND_EXCLUSIONS` set in `validate-bake-handler-kind-coverage.mjs` IF used as outer `child.kind`; otherwise add `_knownKinds` entry).
- §Render-Check: `prefab_inspect` confirms baked `modal-card` produces backdrop child (full-screen Image with raycast-target) + center-anchored card RectTransform + content-replace slot named per `SlotAnchorResolver` suffix convention.
- §Console-Sweep: `validate:bake-handler-kind-coverage` exit 0.
- Resolve NOTE (Wave B2/B3 re-bake or legacy root) by Schema-Probe step before close.

**Body delta.** Medium — ~10 → ~25 lines.

### TECH-27094 — PauseMenuDataAdapter slot-mount refactor

**Current digest summary.** Refactor `PauseMenuDataAdapter` to subscribe `pause.contentScreen` enum bind (root/settings/save/load) and route Settings/Save/Load buttons to mount sub-views via slot. Main-menu + Quit through confirm-button (3s countdown).

**Required edits.**
- Set `task_kind: ui_from_db`.
- §Render-Check: use `SlotAnchorResolver.ResolveByPanel(pauseMenuRoot, "settings-slot")` (and "save-slot" / "load-slot") from `Territory.UI` runtime — apply-time render-check pattern asserts each slot resolved + sub-view instantiated under it.
- Slot suffix-match BFS already deals with deep-anchor nesting (Stage 2 deliverable).
- Confirm-button countdown wiring already shipped Stage 4.5 commit `fb858200` — adapter just dispatches `pause.confirmMainMenu` / `pause.confirmQuit` actions.

**Body delta.** Medium — ~12 → ~28 lines.

### TECH-27095 — Register pause-menu in UIManager.HandleEscapePress LIFO + ModalCoordinator group

**Current digest summary.** Extend `UIManager.HandleEscapePress` LIFO stack (TECH-14102 ordering preserved); pause-menu joins ModalCoordinator exclusive group with budget-panel + stats-panel.

**Required edits.** None. Pure C# UIManager + ModalCoordinator wiring — no panel surface, no bake. Keep `design_only`.

**Body delta.** Zero.

### TECH-27096 — scene-wire pause-menu under Canvas + cutover legacy

**Current digest summary.** Mount baked pause-menu prefab under `CityScene.unity` Canvas + coexist with legacy until cutover.

**Required edits.** Same shape as TECH-27086 / TECH-27091 — `ui_from_db` blueprint + 5 H2 stubs + ESC → modal closed-loop test + cutover delete legacy after Wave B4 verification.

**Body delta.** Small — ~7 → ~18 lines.

### TECH-27335 — non-button kind renderers (CANCEL/SHRINK)

See top-level callout. Either flip `cancelled` (preferred — full overlap with Stage 5 fix-up) or rewrite to single work-item `UiBindBinder` MonoBehaviour shell. Verify against `Assets/Scripts/UI/Settings/SettingsViewController.cs` Stage 4 commit before deciding.

---

## Stage 9.0 — HUD widgets bundle

### TECH-27097 — DB seed 3 panels + ~23 children (info + map + notifications)

**Current digest summary.** Migration `0133_seed_hud_widgets_bundle.sql` seeds info-panel (right-edge) + map-panel (bottom-right minimap) + notifications-toast (top-right transient stack) + ~23 panel_child rows.

**Required edits.**
- Set `task_kind: ui_from_db`.
- 5 H2 blueprint stubs.
- Three separate `validate_panel_blueprint` invocations (one per panel_id) — all must return `missing:[]`.
- §Schema-Probe must catalog new layout templates: right-edge dock + bottom-right anchor + top-right toast-stack.

**Body delta.** Medium-Large — ~10 → ~35 lines (3 panels × validator).

### TECH-27098 — 5 HUD archetype rows + 5 case arms

**Current digest summary.** Migration `0134_seed_hud_archetypes.sql` + 5 case arms (`info-dock`, `field-list`, `minimap-canvas`, `toast-stack`, `toast-card`).

**Required edits.**
- Set `task_kind: ui_from_db`.
- 5 new kinds → register in `KindRendererMatrix` with concrete `IKindRenderer` impls (RawImage component for minimap-canvas, vertical layout for toast-stack, IDragHandler scaffold for minimap-canvas) OR `BakeChildByKind` switch + `_knownKinds` HashSet entries.
- `validate:bake-handler-kind-coverage` exit 0.
- Per-kind component-conformance Render-Check: `prefab_inspect` confirms `RawImage` on minimap-canvas, `IDragHandler`-marked component, etc.
- IR DTO + JsonUtility round-trip tests.

**Body delta.** Large — ~10 → ~38 lines.

### TECH-27099 — WorldSelectionResolver + InfoPanelAdapter + GridManager.DemolishAt

**Current digest summary.** Author `WorldSelectionResolver` (6 per-type field-set builders) + `InfoPanelAdapter` (subscribe `world.select` + Demolish confirm-button) + extend `GridManager.DemolishAt` direct API + Alt+Click inspect.

**Required edits.**
- Set `task_kind: ui_from_db` — info-panel render-check site.
- Apply-time render-check identical to TECH-27085: all field-list rows resolve via KindRendererMatrix; bind-counts > 0 per type; Demolish confirm-button wired via `UiActionRegistry`.
- §Tracer: world-click → info dock renders + Demolish flow → `onUrbanCellsBulldozed` Action fires (verifiable via `unity_bridge_get`).
- §Console-Sweep: replace-DetailsPopupController commit must zero `OnCellInfoShown` event — `console:scan` flags any leftover subscriber warn.
- Use `SlotAnchorResolver.ResolveByPanel(infoPanelRoot, "field-list-slot")` from `Territory.UI` for field-list child lookups.

**Body delta.** Medium-Large — ~14 → ~32 lines.

### TECH-27100 — MapPanelAdapter + MiniMapController extensions + CameraController.PanCameraTo

**Current digest summary.** Author `MapPanelAdapter` (subscribe `minimap.toggle`/`minimap.layer.set`/`minimap.drag`) + extend `MiniMapController` (`SetVisible`, `OnDrag` IDragHandler, header-strip layer-toggle, 360×324 size) + extend `CameraController.PanCameraTo`.

**Required edits.**
- Set `task_kind: ui_from_db`.
- Apply-time render-check: minimap-canvas RawImage resolves, IDragHandler component reachable, header layer-toggle Toggle group has 3+ entries.
- §Tracer PlayMode: HUD map button toggles minimap + drag pans camera.
- Use `SlotAnchorResolver.ResolveByPanel(mapPanelRoot, "header-slot")` from `Territory.UI`.

**Body delta.** Medium — ~12 → ~28 lines.

### TECH-27101 — GameNotificationManager Milestone tier + CityStats.OnPopulationMilestone

**Current digest summary.** Extend `GameNotificationManager` in place (Q5 lock): add `Milestone` to `NotificationType` enum + `PostMilestone` sticky variant + sticky-queue semantics + camera-jump on cellRef click + 3 SFX serialized fields. Extend `CityStats.OnPopulationMilestone` Action<int> + per-service threshold-crosser.

**Required edits.** None on bake-pipeline-hardening axis. Pure manager surface — `notifications-toast` panel itself seeded by TECH-27097 + baked by TECH-27098. This task only authors the manager that posts into the existing toast-stack. Keep `design_only`.

**Body delta.** Zero.

---

## Final rollup + recommended action

| Bucket | Count | Tasks |
|---|---|---|
| Needs `ui_from_db` re-author | 17 | 27082, 27083, 27085, 27086, 27087, 27088, 27089, 27090, 27091, 27092, 27093, 27094, 27096, 27097, 27098, 27099, 27100 |
| Keep `design_only` (managers-only) | 3 | 27084, 27095, 27101 |
| Cancel/shrink (Stage 5 overlap) | 1 | 27335 |

**Recommended action: spawn dedicated re-author stage (Stage 5.5 or pre-6.0 prologue), NOT in-place edit before next ship-cycle.**

Reasons:
- 17 of 21 pending tasks need `task_kind` flip + 5 H2 blueprint sections + validator-call insertion. In-place per-stage drift over Stage 6/7/8/9 = 4× re-author work scattered across 4 ship-cycles → review thrash.
- Single stage authoring all 17 §Plan Digest rewrites in one ship-plan invocation = one inference, one validator pass, one §Plan Digest gate.
- Stage 5.5 also handles TECH-27335 cancel/shrink decision + alias-map updates to `tools/scripts/validate-bake-handler-kind-coverage.mjs` + `_knownKinds` HashSet additions for new kinds (`tab-strip`, `chart`, `range-tabs`, `stacked-bar-row`, `service-row`, `slider-row-numeric`, `expense-row`, `readout-block`, `modal-card`, `info-dock`, `field-list`, `minimap-canvas`, `toast-stack`, `toast-card`) so validators stay GREEN even before each panel migration runs.
- Total ~17 task re-authors + 1 cancel/shrink + ~14 alias/known-kind entries — single inference scope.

**Next step.** Author Stage 5.5 master-plan insert (slug: `cityscene-mainmenu-panel-rollout`) "task_kind cutover + drift-validator pre-population" — single Pass A authoring → flip 17 task bodies + cancel TECH-27335 + populate validator allowlists. Run `validate:all` + `master-plan-state` post-author. Then resume Stage 6.0 ship-cycle on clean substrate.

## Validator + surface reference paths (for re-author stage)

- Blueprint: `ia/templates/blueprints/ui-from-db.md` (5 H2)
- Bake handler: `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` (`BakeChildByKind` L461) + `UiBakeHandler.Archetype.cs` (`_knownKinds` HashSet)
- Kind matrix: `Assets/Scripts/Editor/UiBake/KindRendererMatrix.cs` + `Assets/Scripts/Editor/UiBake/KindRenderers/` (per-kind impls — `IKindRenderer.cs` interface)
- Slot resolver: `Assets/Scripts/UI/SlotAnchorResolver.cs` (runtime `Territory.UI` namespace — suffix-match BFS)
- Drift validator: `tools/scripts/validate-bake-handler-kind-coverage.mjs` (alias map L132-140, `_knownKinds` parse L100, switch parse L116, `OUTER_KIND_EXCLUSIONS` L128)
- Panel-blueprint MCP: `mcp__territory-ia__validate_panel_blueprint` (returns `{ok, panel_id, kindsChecked, missing[]}`)
- Render-check pattern reference: `Assets/Scripts/UI/Settings/SettingsViewController.cs` (Stage 4 apply-time pattern — 12 widgets + non-zero bind counts)
- Console capture: `mcp__territory-ia-bridge__unity_bridge_command bridge.console-log` + `console:scan` chained into `npm run verify:local`
