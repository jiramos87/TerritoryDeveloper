# Phase B fixes — 2026-05-05

Ad-hoc sweep — 8 UI/runtime issues, single session, branch `feature/asset-pipeline`. No master-plan stage.

## Issues + fixes

| # | Issue | Fix | Files |
|---|---|---|---|
| 1 | Picker tile icons text-only, no in-game sprite | Bake-time slug walk via `ToolbarDataAdapter.RebindButtonsByIconSlug` reads `_detail.iconSpriteSlug` → resolved against grid prefab `SpriteRenderer` extraction | `Assets/Scripts/UI/Toolbar/SubtypePickerController.cs`, prefab bake |
| 2 | Family button click → no auto-pick + no picker default highlight | New `ShowSubtypePicker(family, defaultKey)` overload; family handlers pass `Light` (or canonical default) | `SubtypePickerController.cs` |
| 3 | Picker single-tier rows for Road/Forest/Power/Water | **Deferred** — single-tier picker low value vs. cost |
| 4 | S zoning button missing in vertical toolbar | Slot-7 baked `iconSpriteSlug` was wrong (`Bulldoze-button-64`); flipped to `State-button-64` + repointed `m_Sprite` GUID `d93dc313…` → `18ca6daa…` | `Assets/UI/Prefabs/Generated/toolbar.prefab` lines 788, 2940 |
| 5 | Two bulldozer buttons in vertical toolbar | First bulldoze was the mis-baked slot-7 (now S); slot-8 Bulldoze preserved | `toolbar.prefab` |
| 6 | `GameNotificationManager.notificationPanel` SerializeField unwired → `LogError` on Awake | **Code-side lazy-init** — `LazyCreateNotificationUi()` builds hidden panel + `TextMeshProUGUI` child under first active screen-space Canvas. Survives domain reloads + reroot. | `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` |
| 7 | `[ZoneSubTypeRegistry] GridAssetCatalog not found in scene` | Two-part: (a) moved `: MonoBehaviour` from `GridAssetCatalog.Dto.cs` → canonical `GridAssetCatalog.cs` (Unity partial-class file-name binding rule), (b) repointed scene `m_Script` GUID `…01` → canonical `…12` matching `GridAssetCatalog.cs.meta` | `GridAssetCatalog.cs`, `GridAssetCatalog.Dto.cs`, `MainScene.unity` line 12910 |
| 8 | Picker panel too wide | Fit-content width via existing layout pipeline | `SubtypePickerController.cs` |

Bonus — silenced benign empty-`_streamingRelativePath` LogErrors in `GridAssetCatalog` + `TokenCatalog` (v1 path superseded by per-kind exports + `CatalogLoader`, Stage 13.1 / TECH-2675; empty = expected production state).

## Learnings (worth carrying forward)

1. **Unity partial-class MonoBehaviour binding** — declaration `: MonoBehaviour` MUST live in the file whose stem matches the class name. Otherwise Unity fails to resolve the script reference at scene load → component shows "missing script" + scene `m_Script` GUID may even point to the right `.meta` but bind fails. Fix: keep base + `: MonoBehaviour` in canonical file; secondary partial files declare `partial class X` (no base spec).

2. **Bridge mutations during Edit Mode are ephemeral** — `create_gameobject` / `attach_component` / `assign_serialized_field` apply to in-memory scene only. ANY C# edit triggers domain reload + scene reload, discarding unsaved bridge mutations. Mitigation order:
   - Prefer code-side init (lazy-create / fallback construction) over scene authoring.
   - When scene authoring required: chain `save_scene` immediately after each mutation batch, BEFORE any C# edit.
   - `find_gameobject` returning `exists=false` after a fresh `create_gameobject` = clear domain-reload signal.

3. **Bake output is the source of truth for prefab slot ordering** — when the wrong icon shows in a toolbar slot, look at the bake output (`iconSpriteSlug` in `_detail`) NOT the bake source. In-place YAML edits of `toolbar.prefab` are valid for one-off slot fixes; bake source touched only if pattern recurs.

4. **Empty `_streamingRelativePath` is benign** — v1 catalog snapshot path superseded by per-kind exports + `CatalogLoader` (Stage 13.1, TECH-2675). `Debug.LogError` on empty = stale dev noise. Pattern: silent early return + comment referencing the supersession ticket.

## Verification

- `unity_compile` clean (no compile errors, post-edit DLLs).
- Scene reopen + `prefab_manifest` walk: `GridAssetCatalog`, `GameNotificationManager` both `is_missing_script: false`.
- Play Mode entered (128×128 grid ready). Console errors after fix sweep: 0.
- Screenshot `tools/reports/bridge-screenshots/phase-b-final-postcompile-20260505-194346.png` confirms vertical toolbar = 9 family rows (R / C / I / road / S / power / water / forest / bulldoze), single bulldoze at bottom, welcome notification overlay rendering.

## Phase B follow-up — 2026-05-06 (regression sweep before Stage 9.5)

Two regressions surfaced via player walk on `feature/asset-pipeline` HEAD. Neither was on a master-plan stage. Root causes + permanent guards below.

### Reg 1 — SubtypePicker invisible from R/C/I toolbar buttons

**Symptom.** Clicking Residential / Commercial / Industrial family icons selected Light density immediately and skipped the picker. Only Zone S opened the picker.

**Root cause (two-layer).**
1. `SubtypePickerController.EnsureRuntimePanelRootIfNeeded()` parented the runtime `SubtypePickerRoot` under the controller GO's plain `Transform` instead of `canvas.transform`. RectTransform descendants of plain Transform render off-canvas → invisible even when activated.
2. `ToolbarDataAdapter.OnZoningClick(0|3|6)` routed slots to density handlers (`OnLightResidentialButtonClicked` etc.). After Stage 9 IR was rebaked to single Family icons (one slug per family — `residential-button-64`, `commercial-button-64`, `industrial-button-64`), `RebindButtonsByIconSlug` mapped each to its slot, but the dispatch switch was never updated. Picker entry points (`OnResidentialFamilyButtonClicked` / `OnCommercialFamilyButtonClicked` / `OnIndustrialFamilyButtonClicked` → `ShowSubtypePicker`) had zero callers from the toolbar surface.

**Fix.**
- `SubtypePickerController.cs` — parent the runtime root under `canvas.transform`, not `transform`.
- `ToolbarDataAdapter.OnZoningClick` — slots 0/3/6 now invoke the family entry points; slot 9 unchanged; defensive density routes preserved on slots 1/2/4/5/7/8 for legacy multi-tier rebake compatibility.

**Permanent guard.** New PlayMode test block in `MvpUiCloseoutSmokeTest` (`AssertToolbarSlotOpensPicker`) drives the actual private dispatch path via reflection and asserts the picker becomes active for slots 0/3/6. Catches the dispatch-routing class of regression at CI time. Also tightened the existing StateService picker assertion from silent-skip to fail-loud — controller MUST be non-null after `ShowSubtypePicker`.

### Reg 2 — Esc opens PauseMenu while a tool is selected

**Symptom.** Pressing Esc with a toolbar tool active simultaneously cleared the tool AND opened the pause menu. Stage 8 D9 (TECH-14102) `PopupStack` design intended single-frame pop semantics (Esc clears top frame only).

**Root cause.** `GridManager.Update()` carried a stale `Input.GetKeyDown(KeyCode.Escape)` handler from before the popup-stack refactor. Two handlers raced per frame — `UIManager.HandleEscapePress()` popped the `ToolSelected` frame, `GridManager` independently called `ClearCurrentTool()` and let the keypress fall through, after which the next-frame Esc check (or co-routed input) opened pause.

**Fix.** Removed the `GridManager` Esc block entirely. Migrated its toolbar-button-deselect side effect (`buildingSelectorMenuController.DeselectAndUnpressAllButtons()`) into `UIManager.ClearCurrentTool` so every tool-clear path keeps the toolbar visuals in sync. Added idempotent `RemoveFrameFromStack(PopupType.ToolSelected)` inside `ClearCurrentTool` so external clear paths don't strand orphan stack frames.

**Permanent guard.** New `validate:single-esc-handler` Node lint (`tools/scripts/validate-single-esc-handler.mjs`) greps `Assets/Scripts/**/*.cs` (excluding `Tests/`) for `Input.GetKeyDown(KeyCode.Escape)`. Allowlist = `UIManager.cs` only. Wired into `validate:all:readonly` chain. Any future stray Esc handler fails CI before merge.

### Why these were not caught earlier

Stage 9 IR rebake was driven by a deterministic prefab bake pipeline that validates IR shape (slug/sprite/slot integrity) but does not exercise the `ToolbarDataAdapter` dispatch wire — the bake validates "the icon is in the slot" but not "clicking the slot opens the right surface." The MVP closeout smoke test asserted `ShowSubtypePicker(StateService)` directly, not the click-to-picker path.

The Stage 8 D9 popup-stack refactor introduced `popupStack` + `HandleEscapePress` but never deleted the legacy `GridManager` Esc handler. `validate:all` never had a constraint pinning Esc handlers to a single owner.

Both regressions trace to the same anti-pattern: surface refactors that introduced a new canonical handler without retiring the predecessor and without a CI assertion that locked the new ownership. Forward fix: every "single owner" claim in design docs ships with a validator on the same PR.

## Out of scope / deferred

- Issue 3 (picker for Road/Forest/Power/Water) — single-tier rows, low value. Re-evaluate when families gain sub-types.
- Toolbar bake-source review (whether slot-7 mis-bake recurs from upstream config) — not investigated; if recurrence observed, audit bake config for `Bulldoze-button-64` slot-7 binding.
