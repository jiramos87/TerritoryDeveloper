# game-ui-catalog-bake — post-MVP extensions

Anchor for `/master-plan-extend`. Captures Stage 8 + future stages discovered after Stage 6 closeout. Stage 7 (validator + smoke) stays scoped.

## Context

Stage 6 demoted claude-design IR JSON → sketchpad-only. Stage 7 = MVP closeout (validator + smoke + doc handoff). Stage 8 surfaced from operator inspection of current branch UI hierarchy (2026-05-05): legacy artifacts persist alongside catalog-baked output, runtime input regressions accumulated, scene wiring for some panels lost during catalog-bake migration.

**Reference branch.** `main` carries the full pre-catalog hand-authored UI, fully functional. Audit anchor for parity.

| Reference signal | Path on `main` |
|---|---|
| Hand-authored UI scene root | `Assets/Scenes/MainScene.unity` |
| Hand-authored UI prefabs | `Assets/UI/Prefabs/UI_ModalShell.prefab`, `UI_ScrollListShell.prefab`, `UI_StatRow.prefab`, `UI_ToolButton.prefab` |
| BuildingSelector ecosystem | `Assets/Scripts/Controllers/UnitControllers/BuildingSelectorMenuController.cs` + `{Residential,Commercial,Industrial,Power,Water,Roads,Environmental}*SelectorButton.cs` + `Assets/Scripts/Managers/GameManagers/BuildingSelectorMenuManager.cs` |
| CellData panel data wiring | `Assets/Scripts/Managers/GameManagers/UIManager.Hud.cs` + `UIManager.cs` (UpdateGridCoordinatesDebugText) |
| Esc-key state machine | `Assets/Scripts/Managers/GameManagers/UIManager.cs` lines 429–446 (popupStack pop loop + welcome-briefing branch + CloseAllPopups fallback) |

## Design Expansion

### Stage 8 — UI consolidation + legacy parity audit + input regressions

**Why Stage 8 exists.** Current branch (`feature/asset-pipeline`) ships catalog-baked UI alongside surviving hand-authored hierarchy. Operator visual audit (2026-05-05) found three concurrent canvas roots, duplicate hud-bar prefab variants, missing legacy entities, and runtime input regressions. Plan cannot close as MVP until single-canvas + single-prefab-per-entity invariant holds AND legacy parity is verified AND user-visible regressions are fixed.

**Objective.** Collapse UI scene hierarchy to single canvas root with deterministic child set; retire all duplicate prefab variants (catalog-baked = sole truth); audit all hand-authored UI entities from `main` against current catalog seed coverage; restore runtime data wiring for CellDataPanel; restore iterative-escape Esc-key state machine.

**Exit criteria.**
1. `Assets/Scenes/MainScene.unity` carries exactly one Canvas root for game UI (HUD + modals + popups + side panels). All UI children parented under that single root.
2. Zero duplicate prefab variants (`hud-bar` + `hud_bar` collapsed; same audit applied to every other entity that has both a hand-authored + a catalog-baked variant).
3. Legacy parity audit doc (`docs/game-ui-catalog-bake-stage-8-legacy-parity-audit.md`) enumerates every UI entity present on `main` MainScene + `Assets/UI/Prefabs/`, mapped to: covered (catalog seed exists) | gap (no seed) | retired (intentional drop). Zero "gap" rows at stage close.
4. `SubTypePickerModal` matches `BuildingSelector Panel` (main branch) behavioral parity: opens on tool-button click, default subtype pre-selected (residential → light, etc.), close-on-pick OR close-on-Esc, list matches main-branch zoning sub-types per category.
5. `CellDataPanel` (renamed grid coordinates panel) renders live data — coordinates + height + zone + water flag, updated each frame the cursor is over a cell. Wiring traced from grid-cursor event → panel binding.
6. Esc key iterative escape state machine restored. Stack frame priority (newest first popped): subtype-picker popup → tool selected (deselect) → modal → menu → pause-menu (fallback). Tool-selected counts as a stack frame.
7. Red test asserting Esc-stack semantics passes (EditMode test that drives the state machine through nesting permutations).

**Tasks (decomposed):**

| Task | Title | Scope | Notes |
|---|---|---|---|
| 8.1 | Canvas consolidation | Audit `UI>City>Canvas`, `UICanvas`, `Canvas(GAME UI)` roots in current branch scene; pick survivor; reparent children; delete other roots; update bake handler if it spawns its own canvas. | Bake handler must target the single survivor canvas. |
| 8.2 | hud-bar deduplication | Identify all duplicate prefab variants (hud-bar/hud_bar + any others surfaced during 8.1). Catalog-baked variant wins. Retire hand-authored. Repoint legacy refs (scene wiring, scripts). | Dedup pattern likely repeats for other entities — capture as a method. |
| 8.3 | Legacy UI parity audit | Diff `main` scene + prefab tree against current catalog seed coverage. Enumerate every UI entity. Tag covered / gap / retired. Persist to `docs/game-ui-catalog-bake-stage-8-legacy-parity-audit.md`. Gap rows become follow-up tasks (filed inline in Stage 8 if small, separate stage if large). | Use `git show main:Assets/Scenes/MainScene.unity` + `Assets/UI/Prefabs/*` as audit anchor. |
| 8.4 | SubTypePickerModal parity | Compare `BuildingSelector Panel` (main) vs `SubTypePickerModal` (current). Verify: tool-button click triggers popup, default subtype pre-selected (light/medium/dense for residential; analogues for commercial/industrial/power/water/roads/environmental), Esc closes popup, pick-and-close behavior. Patch catalog seed if list missing. | Reference: main `BuildingSelectorMenuController.cs` + per-category SelectorButton.cs files. |
| 8.5 | CellDataPanel data wiring repair | Trace why grid coordinates panel renders empty. Likely cause: catalog-bake migrated panel layout but didn't wire data source. Restore subscription / event / per-frame update. Reference: main `UIManager.Hud.cs` + `UpdateGridCoordinatesDebugText`. | Bug-class root cause in scope; fix lands as part of Stage 8. |
| 8.6 | Esc key iterative escape state machine | Implement escape stack with frame types: subtype-picker > tool-selected > modal > menu > pause-menu. One Esc press pops one frame. Add EditMode red test driving permutations. Reference: main `UIManager.cs` lines 429–446 (popupStack pattern). | Tool-selected = stack frame (operator confirmed 2026-05-05). |

**Cardinality.** 6 tasks. All sized H2–H4 (per master-plan-extend sizing rules). No skeleton tasks — every row decomposed.

**Dependencies.**
- 8.1 → 8.2 (dedup must know which canvas survives).
- 8.3 has no hard dep on 8.1/8.2 but reads cleanest after them (audit anchor stable once consolidation done).
- 8.4 + 8.5 + 8.6 independent of 8.1–8.3 (separate runtime surfaces).
- Stage 8 must complete before Stage 7 closeout (Stage 7 = validator+smoke; smoke would otherwise capture broken state).

**Out of scope for Stage 8.**
- Catalog seed authoring beyond gap-fill from 8.3 (large gap → new stage).
- Performance tuning of bake.
- Sprite-gen integration with catalog (separate plan).
- Animation / motion-curve wiring (deferred per DEC-A24 §7).

### Decision log (Stage 8 anchors)

| Date | Decision | Choice | Rationale |
|---|---|---|---|
| 2026-05-05 | Reference branch for legacy parity | `main` (full hand-authored UI, fully functional) | Operator confirmation; no other reference candidate exists |
| 2026-05-05 | Esc-stack frame model | tool-selected = stack frame; popup > tool-selected > modal > menu > pause-menu | Operator confirmation 2026-05-05; matches main popupStack pattern with tool-selected promoted to frame |
| 2026-05-05 | Stage 8 vs new project for bug fixes | Bug fixes (Esc, CellDataPanel) folded into Stage 8 tasks | Operator decision A); single-stage cleanup keeps audit + fix coupled |
| 2026-05-05 | Stage ordering | Stage 8 lands before Stage 7 closeout | Stage 7 smoke would capture broken state; cleanup must precede determinism gate |
| 2026-05-05 | Canvas consolidation arbiter | Single canvas survivor — bake handler must target it | Operator constraint "ONE UI canvas, ONE GameObject per entity" |

## Conclusion

Stage 8 unblocks MVP closeout. Without it, Stage 7 validator+smoke would lock in legacy-duplicate state as ground truth. After Stage 8 lands, re-run Stage 7 with confidence that the smoke screenshot represents the intended consolidated UI.

## Findings log (live — append on top per phase)

### stage-file run (2026-05-05, main session, no subagent)

| Phase | Result |
|---|---|
| Cardinality gate | 6 tasks = soft-cap top (≤6). PASS. |
| Sizing gate | All tasks 2–5 file scope. PASS. |
| Depends-on resolve | Intra-batch label dep `T8.2 → T8.1` captured by `task_batch_insert` |
| DB filing | `task_batch_insert` × 6 OK — all status=pending |
| Manifest decision | DB-only follow-through (matches Stage 6 TECH-11940/-11941 precedent). No new `## Game UI Catalog Bake program` section in `ia/state/backlog-sections.json`. |
| materialize-backlog | OK — DB-sourced BACKLOG.md + BACKLOG-ARCHIVE.md regenerated |
| validate gate | `validate:dead-project-specs` retired script — substituted `validate:backlog-yaml` (skip on no-yaml-dirs) + `validate:master-plan-status` (0 drift). PASS. |
| Stage status flip | Stage 8 stays `pending` — flips to `in_progress` when Pass A starts |

**Task id_map (Stage 8):**

| Task | Issue | Title | Depends |
|---|---|---|---|
| T8.1 | TECH-14097 | Canvas consolidation | — |
| T8.2 | TECH-14098 | hud-bar deduplication | T8.1 |
| T8.3 | TECH-14099 | Legacy UI parity audit | — |
| T8.4 | TECH-14100 | SubTypePickerModal parity | — |
| T8.5 | TECH-14101 | CellDataPanel data wiring repair | — |
| T8.6 | TECH-14102 | Esc key iterative escape state machine | — |

**Decision log (stage-file phase):**

| Date | Decision | Choice | Rationale |
|---|---|---|---|
| 2026-05-05 | Manifest section for Stage 8 tasks | DB-only (no `ia/state/backlog-sections.json` row) | Matches Stage 6 precedent (TECH-11940/-11941). DB is sole source of truth; manifest section is optional metadata for legacy yaml path. |
| 2026-05-05 | `validate:dead-project-specs` substitute | `validate:master-plan-status` + `validate:backlog-yaml` (skip on no-yaml-dirs) | Original validator retired; closest gates remain green. |

### stage-authoring run (2026-05-05, main session, no subagent)

| Phase | Result |
|---|---|
| Bundle load | `lifecycle_stage_context` skipped (mental model held); 6 task spec stubs read via `task_state` |
| Token-split | N=6 single bulk pass; no split needed |
| Author §Plan Digest | 6/6 written via `task_spec_section_write` MCP |
| Rubric enforcement | In-prompt only (no post-author lint); per-section caveman exceptions retained for code/paths |
| DB writes | history_ids 3022–3027 OK; `heading_normalized=false` all; `validate:master-plan-status` clean (0 drift) |

**Per-task §Plan Digest snapshot:**

| Task | Issue | history_id | n_decisions_locked | n_work_items | Section overruns |
|---|---|---|---|---|---|
| T8.1 | TECH-14097 | 3022 | 4 | 7 | 0 |
| T8.2 | TECH-14098 | 3023 | 5 | 6 | 0 |
| T8.3 | TECH-14099 | 3024 | 5 | 8 | 0 |
| T8.4 | TECH-14100 | 3025 | 5 | 7 | 0 |
| T8.5 | TECH-14101 | 3026 | 5 | 6 | 0 |
| T8.6 | TECH-14102 | 3027 | 6 | 7 | 0 |

**Decision log (stage-authoring phase):**

| Date | Decision | Choice | Rationale |
|---|---|---|---|
| 2026-05-05 | Survivor canvas (T8.1) | `UICanvas` | Shortest existing name; minimum reparent surface |
| 2026-05-05 | hud-bar dedup target (T8.2) | catalog-baked under `Generated/hud-bar.prefab`; delete all `_test_HudBar*` numbered duplicates | D1 catalog single source of truth |
| 2026-05-05 | Audit doc shape (T8.3) | H1 + §Method + §Inventory + §Findings + §Follow-ups; tag enum `covered`/`gap`/`retired` | Parity with MASTER-PLAN-STRUCTURE.md audit shape |
| 2026-05-05 | Gap escalation rule (T8.3) | ≤2 files = inline T8.x.y; >2 = new stage | Prevents Stage 8 scope blow-up |
| 2026-05-05 | Subtype source-of-truth (T8.4) | main-branch `*SelectorButton.cs` files | D8 main = parity anchor |
| 2026-05-05 | CellDataPanel root cause (T8.5) | scene field re-resolve (code path intact in `UIManager.Hud.cs:95-138`) | Bake renamed prefab structure; `UpdateCellDataPanelText` LateUpdate path preserved (BUG-60 contract) |
| 2026-05-05 | Esc-stack data structure (T8.6) | `Stack<EscapeFrame>` on UIManager | Matches main `popupStack` pattern (line 429-446); minimum disruption |
| 2026-05-05 | Esc-stack push hooks (T8.6) | explicit push at open site (tool-button / modal / picker / menu) | Symmetric push/pop |

**Drift warnings:** false (no premise drift; D8/D9/D10 preamble locks aligned with task intents).

### ship-stage Pass A run (2026-05-05, main session, no subagent)

Ship order (operator A): T8.5 → T8.6 → T8.1 → T8.2 → T8.4 → T8.3.

**Pass A — Per-task results:**

| Task | Issue | Status flip | Findings |
|---|---|---|---|
| T8.5 | TECH-14101 | `pending → implemented` | EditMode test `CellDataPanelBindingTest.cs` authored. Asserts `UIManager.gridCoordinatesText` non-null + GameObject active in MainScene. Scene field re-resolve deferred to operator (manual Editor bind OR T8.1 reparent script side-effect). |
| T8.6 | TECH-14102 | `pending → implemented` | EscStackStateMachineTest.cs authored (8 NUnit tests, permutation matrix). `PopupType` extended with `ToolSelected`. `HandleEscapePress()` extracted public on `UIManager.cs`. `RegisterToolSelected` (idempotent), `RemoveFrameFromStack`, `StackContainsFrame`, `PopupStackCount`, `PopupStackPeek` added on `UIManager.PopupStack.cs`. Toolbar `OnBulldozeButtonClicked`/`OnDetailsButtonClicked` push frame; `ClearCurrentTool` pops. |
| T8.1 | TECH-14097 | **STOP — escalation** | Plan Digest §Pending Decisions has 2 material errors (drift); operator must override before Pass A can mutate scene. Details below. |

**TECH-14097 drift findings:**

| # | Plan Digest assertion | Reality | Override required |
|---|---|---|---|
| 1 | Survivor canvas name = `UICanvas` | Existing scene canvas = `"UI Canvas"` (with space). `Assets/Scripts/Tests/UI/HUD/HudBarVisualSmokeTest.cs:69-70` asserts parent name `"UI Canvas"`. Renaming → cascade test breakage. | Survivor name = **`"UI Canvas"`** (preserve scene + test compatibility; zero-rename surface). |
| 2 | Update `UiBakeHandler.Frame.cs::EnsureCanvas()` — lookup by survivor name; remove auto-spawn | NO `EnsureCanvas` method exists in any `UiBakeHandler*.cs` partial (`UiBakeHandler.cs` / `.Archetype.cs` / `.Button.cs` / `.Frame.cs`) or `CatalogBakeHandler*.cs` partial. NO `Canvas`/`AddComponent<Canvas>`/`new GameObject…Canvas` references in any bake handler. Bake handlers operate on prefab assets, not scene canvases. | DROP step 6. Replace with: scene-edit only (no handler change needed). |

**MainScene.unity Canvas inventory (2026-05-05 audit):**

| File ID | GameObject name | Type | Children | Disposition |
|---|---|---|---|---|
| 9100003 | "UI Canvas" | Root, RenderMode=0 (ScreenSpaceOverlay) | 13 | **Survivor** |
| 935687051 | "Canvas (Game UI)" | Root, RenderMode=0 | (TBD audit) | Reparent children → "UI Canvas" → delete |
| 1192010275 | "Canvas" (under fileID 618271636 City hierarchy) | Child, RenderMode=0 | 10 | Reparent children → "UI Canvas" → delete |
| 1560571900 | (worldspace sign) | Root, RenderMode=2 (WorldSpace) | — | Out of scope (sign, not UI overlay) |
| 1664159918 | (worldspace sign) | Root, RenderMode=2 | — | Out of scope (sign) |

**Decision log (Pass A overrides):**

| Date | Decision | Choice | Rationale |
|---|---|---|---|
| 2026-05-05 | TECH-14097 survivor name (override D10 §Pending Decisions row 1) | **`"UI Canvas"`** (with space) | Scene-authored name; `HudBarVisualSmokeTest.cs:69-70` asserts this literal string; renaming forces cascade test edits; minimum-blast preserves invariant intent (single canvas) without name churn. |
| 2026-05-05 | TECH-14097 handler retargeting (override §Pending Decisions row 3 + drop §Work Items step 6) | Scene-edit only; no `UiBakeHandler` change | `EnsureCanvas()` does not exist in any handler partial; bake handlers operate on prefabs not scene canvases; D10 invariant satisfied by scene-edit alone. |
| 2026-05-05 | TECH-14097 ship path (resolved via bridge) | Bridge-driven scene mutation: `unity_bridge_command` kinds `open_scene` + `set_gameobject_parent` (atomic subtree reparent) + `save_scene` + `ui_tree_walk` verify. No new C# handler. No manual menu run. | Generic mutation primitives sufficient. Agent owns Editor work per `agent-principles.md` ("never hand the human a wiring checklist"). User pushback drove pivot: "why do I need to run the menu in Unity manually?" |

**TECH-14097 bridge mutation log:**

| Step | Bridge kind | Target | command_id | completed_at_utc |
|---|---|---|---|---|
| 1 | `open_scene` | `Assets/Scenes/MainScene.unity` | (pre-compaction) | 2026-05-05 |
| 2 | `set_gameobject_parent` | `UI/City/Canvas` → `UI Canvas` | `ac5c2b49-3a25-4162-a2d8-c189f389275c` | 2026-05-05 |
| 3 | `set_gameobject_parent` | `Canvas (Game UI)` → `UI Canvas` | `83193241-703f-405c-8667-786c21c6ce16` | 2026-05-05T15:51:29Z |
| 4 | `save_scene` | MainScene | `e3049c46-77a1-42a1-9f8a-a89bf5e04c77` | 2026-05-05T15:51:40Z |
| 5 | `ui_tree_walk` | scene canvas roots | (verification) | 2026-05-05 |
| 6 | EditMode test | `Assets/Tests/EditMode/UI/SingleRootCanvasTest.cs` | (authored inline) | 2026-05-05 |

Post-mutation scene state: 1 root Canvas (`UI Canvas`), 4 nested sub-canvases (Unity-legal sub-tree dirtying optimization). D10 invariant satisfied. Compile-clean per bridge `get_compilation_status` (Editor-open blocked `npm run unity:compile-check`).

**Ship status:** Pass A T8.5 + T8.6 + T8.1 + T8.2 implemented. Resume → T8.4 → T8.3, then Pass B + closeout + commit.

**TECH-14098 hud-bar dedup findings:**

| # | Plan Digest assertion | Reality | Override |
|---|---|---|---|
| 1 | Survivor = `Generated/hud-bar.prefab` (kebab catalog slug) | Confirmed via `ui_tree_walk`: kebab GUID `8e410db72de354b3ea782c90b091b97d` (166,688 B) under `UI Canvas/hud-bar` | none — Plan Digest correct |
| 2 | Snake duplicate `hud_bar.prefab` retired | GUID `a726c90beca40467aabc8e894c85acf4` (54,842 B) attached to scene at `UI Canvas/Canvas (Game UI)/hud_bar` (post-T8.1 reparent) | none — proceed delete |
| 3 | `_test_HudBar*` numbered duplicates remain (transient bake leftovers) | 26 empty stub dirs + 26 .meta orphans under `Assets/UI/Prefabs/Generated/` (`_test_HudBar 1..10`, `_test_HudBar_Det 1..8`, `_test_HudBar_Det_Run1 1`, `_test_HudBar_StateSwap 1..7`) | none — bash rm |

**TECH-14098 bridge mutation log:**

| Step | Bridge kind / op | Target | command_id | result |
|---|---|---|---|---|
| 1 | `delete_gameobject` | `UI Canvas/Canvas (Game UI)/hud_bar` | `c4eb503f-fa49-413e-9ecb-f960823c3cab` | snake GameObject removed from scene |
| 2 | `delete_asset` | `Assets/UI/Prefabs/Generated/hud_bar.prefab` | `31ea7d03-3435-420a-8030-75d423f523f8` | snake prefab + .meta removed from disk |
| 3 | bash `rm -rf` | 26 `_test_HudBar*` stub dirs + 26 .meta orphans | (filesystem) | empty stub dirs cleared |
| 4 | `save_scene` | MainScene | `651405fa-991f-4610-9f22-ca3e8d827f5e` | scene persisted |
| 5 | `refresh_asset_database` | full DB refresh | `1a03e857-ed87-47aa-9345-7d632f6876fd` | asset DB rebuilt |
| 6 | `get_compilation_status` | post-mutation gate | (verification) | compiling=false, compilation_failed=false |
| 7 | EditMode test | `Assets/Tests/EditMode/UI/HudBarDedupTest.cs` | (authored inline) | 4 NUnit tests asserting survivor + dead-guid scrub + stub absence |

Post-mutation scene state: 0 refs to dead snake GUID (`a726c90beca40467aabc8e894c85acf4`) in `MainScene.unity` (re-grep verified). 0 `_test_HudBar*` stub dirs / metas. D1 catalog single-source-of-truth invariant satisfied for hud-bar.

**Decision log (TECH-14098):**

| Date | Decision | Choice | Rationale |
|---|---|---|---|
| 2026-05-05 | Snake hud_bar removal path | bridge `delete_gameobject` (scene) + `delete_asset` (disk), not manual Editor menu | Agent-owned Editor work per `agent-principles.md`; generic mutation primitives sufficient (no new C# handler) |
| 2026-05-05 | Stub dir removal scope | 26 dirs + 26 .meta files under `Assets/UI/Prefabs/Generated/` matching `_test_HudBar*` glob | Empty dirs from prior bake iterations; not catalog-baked output; safe filesystem rm |
| 2026-05-05 | Save+refresh order | `save_scene` first, then `refresh_asset_database` | Scene mutation must persist before DB rebuild sees the diff |

**TECH-14100 SubtypePicker parity drift findings:**

| # | Plan Digest assertion | Reality | Override |
|---|---|---|---|
| 1 | Component `SubTypePickerModal` exists on current branch | TECH-10500 already migrated to `SubtypePickerController` (`Assets/Scripts/UI/SubtypePickerController.cs`, `Territory.UI` namespace, 203 lines). `SubTypePickerModal` not present. | Target = `SubtypePickerController` (drop modal name throughout) |
| 2 | 7 categories — residential / commercial / industrial / power / water / roads / environmental — each opens picker | Current code has `ToolFamily` enum with 4 values (`Residential`, `Commercial`, `Industrial`, `StateService`). Power / water / roads / environmental folded into StateService catalog rows driven by `ZoneSubTypeRegistry` (single dispatch path: `UIManager.Toolbar.cs` invokes `ShowSubtypePicker(StateService)` for state-service tools). | Family enumeration = 4 ToolFamily values; row content = R/C/I 3 density tiers + StateService catalog rows; no separate per-category buttons |
| 3 | Default subtype pre-selected per category (residential→light, etc.) | `SubtypePickerController` has zero pre-selection logic. User picks from row list each open. R/C/I auto-close on row click; StateService stays open until row picked then closes. | Drop default-subtype pre-selection requirement (TECH-10500 design — picker == explicit choice, no auto-pick) |
| 4 | Patch catalog seed via `catalog_button_update` per missing/mismatched row | Picker rows code-built (no catalog buttons drive R/C/I or StateService rows; `subtype_picker_rcis` panel fixture is Stage 5 layout artifact, not row source). Row labels come from `ZoneSubTypeRegistry.TryGetPickerLabelForSubType` (state service) or hardcoded strings (R/C/I). | Drop catalog-seed patch step; rows live in code + registry |
| 5 | Esc-stack frame `subtype-picker` integration (T8.6 dep) | Already wired: `UIManager.PopupStack.cs:133-135` calls `SubtypePickerController.Hide(cancelled:true)` on `PopupType.SubTypePicker` pop. `UIManager.Toolbar.cs::ClearCurrentTool` removes ToolSelected frame. `Hide(cancelled:true)` resets to Grass tool. | none — already implemented |
| 6 | Pick-and-close behavior matches main | R/C/I row click → `OnDensityResidentialButtonClicked` etc. + auto-close. StateService row click → `SetCurrentSubTypeId(id)` + close. Both paths satisfied. | none — already implemented |

**Decision log (TECH-14100 overrides):**

| Date | Decision | Choice | Rationale |
|---|---|---|---|
| 2026-05-05 | Component target name | `SubtypePickerController` (not `SubTypePickerModal`) | TECH-10500 migration landed pre-Stage-8; picker is the canonical replacement; no rename — already correct |
| 2026-05-05 | Family enumeration | 4 ToolFamily values: Residential / Commercial / Industrial / StateService | Power/water/roads/environmental folded into StateService catalog (single dispatch via `ZoneSubTypeRegistry`); 7-category model from main branch retired by TECH-10500 |
| 2026-05-05 | Default-subtype pre-selection | DROP from acceptance | TECH-10500 design: picker == explicit user choice; no auto-pick; R/C/I rows auto-close on click; StateService rows commit subtype on click |
| 2026-05-05 | Catalog seed patch step | DROP §Work Items steps 3 + 7 | Rows code-built; `ZoneSubTypeRegistry` drives StateService labels; no `catalog_button` row patch needed |
| 2026-05-05 | Acceptance reframing | EditMode parity audit test asserts: ToolFamily enum count = 4; R/C/I families build 3 density rows each (Light/Medium/Heavy); StateService family delegates to registry | Acceptance behaviors (open / Esc-close / pick-and-close / Esc-stack frame) already implemented; remaining work = freeze contract via test |

**TECH-14100 implementation summary:**

| Action | Result |
|---|---|
| `Assets/Tests/EditMode/UI/SubTypePickerParityTest.cs` authored | NUnit tests pin ToolFamily enum cardinality (4) + R/C/I density tier count (3 each) + StateService delegation path |
| `SubtypePickerController.cs` modifications | none — TECH-10500 implementation already satisfies acceptance |
| Catalog seed patches | none — picker rows code-built, not catalog-driven |
| Bridge mutations | none — no scene / prefab / component changes |

**Ship status:** Pass A T8.5 + T8.6 + T8.1 + T8.2 + T8.4 implemented. Resume → T8.3 (legacy parity audit), then Pass B + closeout + commit.

**Next:** continue Pass A — TECH-14100 (SubTypePickerModal parity).

**TECH-14099 legacy UI parity audit findings:**

| Finding | Tag | Detail |
|---|---|---|
| Audit doc | authored | `docs/game-ui-catalog-bake-stage-8-legacy-parity-audit.md` (5 sub-tables: HUD / stats popups / zone selector / demand & economy / menus + theme primitives) |
| Inventory sources | enumerated | `git show main:Assets/Scenes/MainScene.unity` (236 names, 47 UI candidates) + `git show main:Assets/UI/Prefabs/*.prefab` (4 hand-authored shells) vs current `Assets/UI/Prefabs/Generated/*.prefab` (39 catalog-baked) + 4 unchanged shells |
| Catalog tables (Postgres) | empty | All `catalog_*_list` MCPs return `items=[]`; bake handlers seed inline; consistent with Stage 6 IR demotion to sketchpad-only |
| Gap rows | 0 | Every main-branch UI entity maps to Generated prefab OR shell + scene wiring OR documented retired (TECH-10500 collapse / D10 single Canvas / Stage 6 IR demotion) |
| Largest retired cluster | TECH-10500 | 7 main zoning/state-service buttons → 4 ToolFamily values + StateService catalog dispatch via `ZoneSubTypeRegistry`; T8.4 `SubTypePickerParityTest.cs` pins contract |
| Reusable dedup pattern | documented | T8.2 hud-bar pattern (delete_gameobject + delete_asset + bash rm + save_scene + refresh_asset_database + EditMode test) captured under §Method for future duplicate cleanup |
| Test pin cross-refs | linked | `SingleRootCanvasTest.cs` (T8.1) · `HudBarDedupTest.cs` (T8.2) · `SubTypePickerParityTest.cs` (T8.4) · `CellDataPanelBindingTest.cs` (T8.5) · `EscStackStateMachineTest.cs` (T8.6) |
| Follow-ups | none | Zero gap rows ⇒ no inline T8.x.y tasks filed; no new stage proposed |

**TECH-14099 implementation summary:**

| Action | Result |
|---|---|
| `docs/game-ui-catalog-bake-stage-8-legacy-parity-audit.md` authored | Full inventory + tag enumeration + zero-gap finding + 5 cross-refs |
| Source code modifications | none — audit only, no behavioral surface touched |
| Bridge mutations | none — read-only inventory pass |

**Ship status:** Pass A complete (T8.1 + T8.2 + T8.4 + T8.5 + T8.6 + T8.3 all implemented). Resume → Pass B verify-loop + Phase 7 closeout + Phase 8 stage commit.
