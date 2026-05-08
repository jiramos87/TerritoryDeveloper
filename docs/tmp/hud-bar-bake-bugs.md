# hud-bar bake-test bug tracker

Source failure: `docs/hud-bar-bake-test-process.md` test bake. Definitions locked in `docs/ui-element-definitions.md`. Root cause = bake pipeline (handler + adapter), not definitions. Fix order: D → A → C → B → E.

## Bug A — HUD baked at bottom strip instead of top

- **Symptom**: Screenshot 5.06.37 shows HUD strip at screen bottom; ui-element-definitions specifies top strip.
- **Pipeline surface**: `Assets/Scripts/UI/UiBakeHandler.cs` (`ApplyPanelKindRectDefaults` ~line 454).
- **Root cause**: `PanelKind.Hud` arm hardcodes `anchorMin=(0,0), anchorMax=(1,0)` — bottom strip. Snapshot `params_json.position` ignored.
- **Fix**: Read `params_json.position` (`"top"` / `"bottom"`) → set anchors accordingly. Default `top` for hud-bar panel.
- **Touched files**: `UiBakeHandler.cs` only.
- **Risk**: low. Single arm switch.

## Bug B — Right zone renders as 1-column vertical, not 2×4 (or designed grid)

- **Symptom**: Screenshot 5.08.23 — all 8 right-zone buttons stacked vertically, not in the row/column layout from definitions.
- **Pipeline surface**: `UiBakeHandler.Archetype.cs` ~line 1092 (Right wrapper construction).
- **Root cause**: Right wrapper = single flat HLG/VLG. Children carry `layout_json.col` / `row` / `sub_col` / `rowSpan` hints. Bake reads only `zone`; col/row/sub_col ignored → all flatten to one stack.
- **Fix**: Right zone needs nested layout — one `VerticalLayoutGroup` per `col` value, then row stack per col. Walk `layout_json.col` first (group), then `layout_json.row` (order within group).
- **Touched files**: `UiBakeHandler.Archetype.cs`.
- **Risk**: medium. Layout walker rewrite. In-session.

## Bug C — Center labels invisible (city name / date / population)

- **Symptom**: Center div empty. No `cityName`, `date`, `population` shown. Items also stacked column instead of row.
- **Pipeline surface**: `UiBakeHandler.cs` (kind dispatcher ~line 969) + adapter (`HudBarDataAdapter.cs`).
- **Root cause** (cascading):
  1. Bake handler kind dispatcher only branches on `kind=button`. `kind=label` falls through with no TMP child instantiation → labels exist as empty GameObjects.
  2. No adapter binder pushes `bind="cityStats.cityName"` (etc.) onto TMP component at runtime.
  3. Center HLG `childControlWidth=false` + 64×64 RectTransform → empty TMP collapses.
- **Fix**:
  1. Bake handler `kind=label` arm: instantiate TMP child, set placeholder text from `params_json.label`.
  2. Adapter: subscribe label `bind` slugs (`cityStats.cityName` etc.) → push value onto TMP each tick.
- **Touched files**: `UiBakeHandler.cs`, `HudBarDataAdapter.cs`.
- **Risk**: medium. New code path but isolated.

## Bug D — No button works (click handlers null)

- **Symptom**: Every button silent on click. Only AUTO/MAP partial via caption fallback.
- **Pipeline surface**: `Assets/Scripts/UI/HUD/HudBarDataAdapter.cs` switch at ~line 193.
- **Root cause**: Adapter binds buttons via `IlluminatedButtonDetail.iconSpriteSlug` switch. Switch cases use legacy v1 variant slugs (`zoom-in-button-1-64`, `pause-button-1-64`, …). v2 seed (`0107_seed_hud_bar_sprites_v2.sql`) emits bare v2 slugs (`zoom-in`, `zoom-out`, `pause`, `stats`, `new-game`, `save-game`, `load-game`, `budget`, etc.). All v2 slugs hit `default:` → no slot bind → field stays null → click does nothing.
- **Fix**: Replace legacy switch labels with bare v2 slugs.
- **Touched files**: `HudBarDataAdapter.cs` only.
- **Risk**: low. Mechanical string swap. Smallest fix, unblocks click testing.

## Bug E — BUDGET button renders as wide minus bar (same as zoom-out)

- **Symptom**: BUDGET shows long horizontal bar, identical to zoom-out minus glyph.
- **Pipeline surface**: `UiBakeHandler.cs` `WireIlluminatedButtonIcon` + sprite catalog.
- **Root cause**: `params_json.icon="long"` → variant scan picks `long-button-256-64-target.png` (a wide button SHAPE, not a glyph). Bake handler treats `icon` as atomic glyph; squeezed into 64×64 Image slot = thin bar. Pipeline conflates icon-glyph vs button-shape semantics.
- **Fix**: Schema split — `params_json.icon` = glyph slug only; new `params_json.shape` = button background variant (`square`/`long`/etc.). Bake handler reads both; shape feeds button background, icon feeds glyph slot.
- **Touched files**: bake handler + `button_detail` schema (DB migration) + seed migration.
- **Risk**: high. DB schema change. Cross-cuts catalog + bake. In-session.

## Why pipeline, not definitions

A/B/C/E — locked definitions in `docs/ui-element-definitions.md` already carry the right hints (`position`, `col`/`row`, `bind`, kind=label, shape vs icon). Bake handler only consumes a subset (`zone`, `icon`). Adding keys to defs can't fix it — bake handler is the consumption floor.

D — pure C# legacy slug drift. Adapter switch frozen at pre-v2 shape.

## Implementation order

1. **D** — smallest, mechanical, unblocks click test.
2. **A** — top-strip anchor fix.
3. **C** — label arm + adapter binder.
4. **B** — right zone nested col/row layout.
5. **E** — shape vs icon schema split (DB migration).

## Bug F — Snapshot bake never spawns button caption (TMP)

- **Symptom predicted**: AUTO, MAP, BUDGET, speed-cycle render as fully blank squares (no glyph + no caption).
- **Pipeline surface**: `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs:969-1034` (snapshot button arm).
- **Root cause**: Snapshot bake path calls only `WireIlluminatedButtonIcon` + `ApplyDetail`. `params_json.label` ("NEW"/"SAVE"/"LOAD"/"AUTO"/"MAP") is never read; `SpawnIlluminatedButtonCaption` (used by IR/Frame.cs path) never invoked. When `icon` resolves to placeholder `empty` sprite, button has body but no signal of its function.
- **Fix surface**: `UiBakeHandler.cs:1031` — after `ApplyDetail`, extract `label` from `params_json` (mirror `ExtractParamsJsonIconSlug` pattern), call `SpawnIlluminatedButtonCaption(instance, label)` when sprite resolution is the placeholder/empty case.
- **Risk**: low. Adds caption surface; isolated to snapshot path.
- **Why visible next bake**: This also breaks Bug D's caption-fallback recovery — adapter `RebindButtonsByIconSlug` reads `TextMeshProUGUI.text` to bind AUTO/MAP/BUDGET when `iconSpriteSlug=="empty"`. No TMP child exists → null → no rebind → no click handler. Clicks stay broken even after slug-switch fix.

## Bug G — Snapshot bake never adds `UnityEngine.UI.Button` / hover+press wiring

- **Symptom predicted**: Buttons render but emit no `OnClick` events even after Bug D switch is fixed (all paths null-routed).
- **Pipeline surface**: `UiBakeHandler.cs:969-1034` snapshot path; missing call to `WireIlluminatedButtonHoverAndPress` (defined in `UiBakeHandler.Archetype.cs:761`).
- **Root cause**: IR/Frame path explicitly wires `Button` + `IlluminatedButtonRenderer._mainImage`/`_haloImage` + ColorTint at bake. Snapshot path does not. Click delivery depends entirely on `Assets/UI/Prefabs/Generated/illuminated-button.prefab` already carrying a Button component pre-baked. If prefab ships without it (or its `_renderer` refs are null), `IlluminatedButton.OnClick` UnityEvent is never invoked.
- **Fix surface**: `UiBakeHandler.cs:1031` — after icon wire, locate `IlluminatedButtonRenderer` on `instance`, call `WireIlluminatedButtonHoverAndPress(instance, renderer, bodyImage, haloImage, theme)`. Or — if illuminated-button.prefab already carries Button — verify pre-bake and add an assertion/Debug.Log when missing.
- **Risk**: medium. Cross-references IR-path baked components; need to refactor `WireIlluminatedButtonHoverAndPress` to accept already-instantiated renderer.
- **Why visible next bake**: even with Bug D's slug switch corrected, click-test will fail silently on every button. Compounds with Bug F.

## Bug H — `kind=label` / `kind=readout` children render as empty GameObjects (no component)

- **Symptom predicted**: Center zone shows 3 empty 64×64 cells. No city name, no date, no population text — even if Bug C's adapter binder is added.
- **Pipeline surface**: `UiBakeHandler.cs:969` only branches on `child.kind == "button"`. `kind == "label"` (which migration 0108 uses for ALL THREE center children including the two readouts) falls through with no component instantiation.
- **Root cause**: snapshot bake handler has no `kind=label` / `kind=readout` arm. Children get a 64×64 RT with `instance_slug` name + `CatalogPrefabRef` only. No `ThemedLabel`, no `TextMeshProUGUI`, no `SegmentedReadout`. Migration 0108 lines 148/154/160 set all three center kids to `kind='label'` even though `params_json.kind` says `"readout"` for two of them.
- **Fix surface**: `UiBakeHandler.cs` snapshot loop — add arms for `kind=label` (spawn `ThemedLabel` + child TMP) and `kind=readout` (spawn `SegmentedReadout` + `SegmentedReadoutRenderer` + render targets, mirroring `Frame.cs:684-697`). Also reconcile `panel_child.child_kind` vs `params_json.kind` mismatch — migration uses `child_kind='label'` for readouts; consider using `params_json.kind` as authoritative.
- **Risk**: high. Two new arms + readout render-target spawning. Largest fix in tracker.
- **Why visible next bake**: this is the actual root of "Center labels invisible" — Bug C's adapter fix can't bind to TMP that doesn't exist. Until the bake produces `ThemedLabel`/`SegmentedReadout` components, adapter slot fields remain null.

## Bug I — Adapter `_speedButtons[]` array (length 5) doesn't match v2 panel (2 speed buttons)

- **Symptom predicted**: Time speed cycle never illuminates the active button; speed button clicks call the wrong indices.
- **Pipeline surface**: `HudBarDataAdapter.cs:79` — `_speedButtons` declared as 5-element array (paused / 0.5x / 1x / 2x / 4x) and bound via legacy slugs (`pause-button-1-64`, `speed-1-button-1-64`, …, `speed-4-button-1-64`).
- **Root cause**: v2 def replaces 5-button speed cluster with 2 buttons: `hud-bar-play-pause-button` (slug `pause`) + `hud-bar-speed-cycle-button` (slug `empty` + `label_bind` `currentTimeSpeedLabel`). Adapter has no slot for "speed-cycle"; `EnsureSpeedSlot` never fills slots 1–4; `Update` channel mirrors `CurrentTimeSpeedIndex` against a partially-populated array.
- **Fix surface**: `HudBarDataAdapter.cs` — add `_playPauseButton` + `_speedCycleButton` SerializedFields, drop `_speedButtons[]`, wire click handlers `HandlePlayPauseClick` / `HandleSpeedCycleClick` against `TimeManager.TogglePause` / `TimeManager.CycleSpeed`. Update slug switch with v2 keys (`pause` for play-pause, caption-fallback for speed-cycle since icon=empty).
- **Risk**: medium. Touches Update loop + new TimeManager methods (verify `CycleSpeed` exists; if not, file a sub-ticket).
- **Why visible next bake**: speed cluster will appear blank/non-functional regardless of D/F/G fixes.

## Bug J — Adapter button method-target verification gap

- **Symptom predicted**: NEW/SAVE/LOAD click handlers may NRE if `UIManager` API doesn't ship the expected methods.
- **Pipeline surface**: `HudBarDataAdapter.cs:299-312` calls `_uiManager.OnNewGameButtonClicked()`, `OnSaveGameButtonClicked()`, `OpenPopup(PopupType.SaveLoadScreen)`.
- **Root cause**: not verified during this audit. Method names look legacy (`OnNewGameButtonClicked` is a UI-event-handler shape). If renamed in current `UIManager`, click → NRE → silent failure (single-frame log).
- **Fix surface**: spot-check `Assets/Scripts/UI/UIManager.cs` API surface vs adapter call sites.
- **Risk**: low (verification only).
- **Why visible next bake**: would surface only if Bugs D/F/G all resolved + click actually fires.

## Bug K — Right-zone wrapper width insufficient for 8 children at 64×64

- **Symptom predicted**: Right-zone children clip / overlap / squeeze into right edge; appears as visual column-stack at small canvas widths.
- **Pipeline surface**: `UiBakeHandler.cs:1090` `ApplyChildLayoutJsonSize` reads only `layout_json.size.{w,h}`; migration 0108 panel_child rows carry only `col`/`row`/`sub_col`/`rowSpan`, no `size`. Every child wrapper stays 64×64.
- **Root cause**: Right zone wrapper gets ~1/3 of bar width (~330px on 1000px canvas via `flexibleWidth=1`). 8 children × 64px = 512px content. With HLG `childControlWidth=false` + `childForceExpandWidth=false` + `childAlignment=MiddleRight`, layout overflows to the left of the wrapper bounds. Bug B's nested 2-row × multi-col layout would naturally resolve this (2×5 = 10 cells × 64 = 640 logical width, but each col only counts once in the row-major flow).
- **Fix surface**: subsumed by Bug B (col/row nesting). Once Right is split into `col=0` (2 stacked: zoom-in/out), `col=1` (2 stacked: budget / play-pause+speed-cycle), `col=2` (1 spanning: stats), `col=3` (1 spanning: auto), `col=4` (1 spanning: map), wrapper width = 5 × 64 = 320px → fits.
- **Risk**: med (rolled into Bug B's fix).
- **Why visible next bake**: same root as Bug B's column-stacking visual.

## Bug L — Schema mismatch: `child_kind` in DB vs `kind` in `params_json`

- **Symptom predicted**: ambiguous bake routing for readouts; future migration drift could silently mis-spawn components.
- **Pipeline surface**: migration 0108 lines 148/154/160 — `panel_child.child_kind = 'label'` for all 3 center kids, but `params_json.kind` is `"label"` for city-name + `"readout"` for sim-date + population.
- **Root cause**: snapshot exporter passes `child_kind` to bake; bake reads only top-level `child.kind` field. `params_json.kind` is consumed as documentation only. Two sources of truth — ambiguous when they disagree.
- **Fix surface**: pick one canonical source. Recommend: bake handler reads `params_json.kind` as primary, falls back to `child_kind`. Update validator + migration to enforce match.
- **Risk**: low (schema discipline).
- **Why visible next bake**: bug surfaces only if Bug H's fix uses `child_kind` (=`label`) and treats sim-date as a static label — which would make readouts non-tickable. If fix uses `params_json.kind`, silent escape for now but tracked for future migrations.

## Updated implementation order (revised — 11 bugs, 5 priority tiers)

**Tier 1 — blocks any visual recovery** (do first, in this order):
1. **D** — adapter v2 slug switch (mechanical, smallest).
2. **A** — top-strip anchor.
3. **F** — caption spawn on snapshot path (depends D's fallback path).
4. **G** — Button + hover/press wire on snapshot path (verify `illuminated-button.prefab` first; may be a no-op).
5. **H** — `kind=label` + `kind=readout` arms (largest visible fix).

**Tier 2 — functional but layout still wrong**:
6. **C** — adapter label binders (city name / date / population) — depends on H.
7. **I** — speed cluster slot rewrite (5 → 2 buttons).

**Tier 3 — layout polish**:
8. **B** — Right-zone nested col/row layout.
9. **K** — width budget validation (subsumed by B).

**Tier 4 — schema split**:
10. **E** — `shape` vs `icon` `params_json` schema split + DB migration.

**Tier 5 — verification only**:
11. **J** — confirm UIManager method names; **L** — schema-source-of-truth discipline.

## Re-bake checklist

After each fix:
- `npm run unity:compile-check`
- Re-run bake test per `docs/hud-bar-bake-test-process.md`
- Visual check vs `docs/ui-element-definitions.md` hud-bar layout
- Click smoke: zoom-in/zoom-out, pause, stats minimum

---

# Bake-pipeline improvement audit

Bug fixes A–L address the *symptoms* of the failed bake. The improvements below address the *structural fragility* that lets bugs of this shape recur. All in-session.

Files in scope: `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` (1361 lines), `UiBakeHandler.Archetype.cs` (1159), `UiBakeHandler.Frame.cs` (1035), `UiBakeHandler.Button.cs` (547). Plus snapshot exporter (writes `panels.json`) + adapter (`HudBarDataAdapter.cs`).

## Improvement order (8 items, 3 tiers)

**Tier α — must-do, blocks bug fixes from regressing** (do alongside Tier 1 fixes):
1. **Imp-1** — unify kind dispatch (snapshot vs IR/Frame paths)
2. **Imp-2** — typed `params_json` / `layout_json` POCOs (drop regex)
3. **Imp-3** — propagate bake errors to MCP response

**Tier β — should-do, prevents next class of bugs** (do after Tier 1, before re-bake):
4. **Imp-4** — bake validation gate (pre-flight contract checks)
5. **Imp-5** — structured `BakeReport` for diff-able assertions
6. **Imp-6** — placeholder vs missing-sprite distinction

**Tier γ — could-do, hygiene** (do after first green re-bake):
7. **Imp-7** — file/partial-class re-split by responsibility
8. **Imp-8** — idempotency + dry-run mode

## Imp-1 — Unify kind dispatch (snapshot vs IR/Frame paths)

- **Smell**: `UiBakeHandler.cs:917 BakePanelSnapshotChildren` only branches on `kind == "button"`. `UiBakeHandler.Frame.cs:648` (IR path) has full kind dispatch (illuminated-button, segmented-readout, themed-label, …) using shared spawner helpers from `Archetype.cs` (`SpawnIlluminatedButtonRenderTargets`, `WireIlluminatedButtonHoverAndPress`, `SpawnIlluminatedButtonCaption`, `SpawnSegmentedReadoutRenderTargets`).
- **Why bad**: snapshot path duplicates icon resolution (`WireIlluminatedButtonIcon` at `:1051`) with **less coverage** than the IR path version → root cause of Bugs F (no caption), G (no Button wire), H (no label/readout arms). Two code paths, one is silently degraded.
- **Fix**: extract `BakeChildByKind(parent, kind, paramsJson, detailJson, theme)` taking the kind dispatch from Frame.cs:648-770 and re-using shared spawners. Snapshot path calls it; IR path calls it. One arm per kind, one place to add new kinds. Delete `WireIlluminatedButtonIcon` + `ExtractParamsJsonIconSlug` (subsumed by Imp-2 typed reader).
- **Effort**: medium — refactor of two callers around one new dispatcher.

## Imp-2 — Typed `params_json` / `layout_json` POCOs

- **Smell**: `ExtractParamsJsonIconSlug` (`UiBakeHandler.cs:1040`) regex-extracts `"icon":"…"`. `TryReadFloatPath` (`:1101`) regex-extracts `outer.inner` numeric paths. `layout_json.col`/`row`/`sub_col`/`rowSpan` not read at all (Bug B/K root).
- **Why bad**: regex JSON parsing breaks on escaped quotes, nested braces, multi-line bodies, key reorderings. Schema additions silently no-op (Bug B's missing col/row reads). No type system to flag a future migration that emits `params_json.iconSlug` vs `icon`.
- **Fix**: add `[Serializable]` POCOs `PanelChildLayoutJson { string zone; LayoutSize size; int col; int row; int sub_col; int row_span; }` and `PanelChildParamsJson { string icon; string label; string kind; string bind; string label_bind; string shape; }`. Deserialize once via `JsonUtility.FromJson<>` at child-iter top; pass typed struct to dispatcher. Drop both regex helpers.
- **Effort**: low — Unity has built-in `JsonUtility`; ~30 lines new POCO declarations.

## Imp-3 — Propagate bake errors to MCP response

- **Smell**: `BakePanelSnapshotChildren` `Debug.LogWarning` for missing slot wrapper (`:933`), missing icon sprite (`:1057`). Zero warnings reach MCP `unity_bridge_command` response. Zone-fallback-to-center is silent.
- **Why bad**: bake "succeeds" when N children silently degrade. Test harness (manual screenshot) catches symptoms 1 frame after, but agent can't diff-check.
- **Fix**: wrap warnings in `BakeWarning` records appended to `BakeError[]` returned from `SavePanelSnapshotPrefab`. Bridge command surfaces `warnings[]` array in JSON response. Re-bake emits `bake_warnings_count` to validate against (zero on green bake).
- **Effort**: low — list append + JSON shape extension.

## Imp-4 — Bake validation gate (pre-flight)

- **Smell**: handler accepts any `panels.json` shape and writes prefab. No contract check that `kind=button` has resolvable icon, that `kind=label` has `bind`, that `layout_json.zone` matches archetype slot map.
- **Why bad**: Bug L surfaces because schema discipline absent. Future seed migration could ship `kind="readouts"` typo and bake silently emits empty cells.
- **Fix**: before child-iter, validate every child against an archetype contract table. Per panel slug, declare expected child kinds + required keys. Validation failure → `BakeError`, abort prefab write (no half-baked output).
- **Effort**: medium — declare contracts per archetype + early-exit pass before iter loop.

## Imp-5 — Structured `BakeReport` for diff-able assertions

- **Smell**: bake test = manual screenshot inspection (`docs/hud-bar-bake-test-process.md`). No structured output the bridge can assert against.
- **Why bad**: Bugs A–E all show in screenshots; agent regressions detected only when human eyeballs it. Closed-loop verification (`verify-loop` / `testmode-batch`) can't gate.
- **Fix**: handler emits `BakeReport { panel_slug; child_count; per_kind_counts; sprite_resolutions[]; warnings[]; root_anchor; }`. Test harness diffs report against expected fixture. Same shape consumable by `unity_bridge_get` so agent-led bake tests don't need screenshots.
- **Effort**: medium — new POCO + emit at end of `SavePanelSnapshotPrefab` + fixture setup in MCP test bench.

## Imp-6 — Placeholder vs missing-sprite distinction

- **Smell**: `iconSlug == "empty"` resolves to placeholder sprite (or null) → same warning as "real slug, file missing". Adapter caption-fallback path (Bug D context, `HudBarDataAdapter.cs:178`) only fires when `iconSpriteSlug == "empty"`.
- **Why bad**: bake success/failure ambiguous for caption-only buttons (AUTO/MAP/BUDGET in v2). Bug F prediction relies on knowing `empty` is intentional; current code gives no signal.
- **Fix**: add explicit `placeholder` enum value to icon-resolution result. `empty` slug → `Placeholder` (no warning, caption expected); unresolved real slug → `MissingSprite` (warning, fail validation in Imp-4).
- **Effort**: low — one enum + branch in `ResolveButtonIconSprite`.

## Imp-7 — File/partial-class re-split by responsibility

- **Smell**: 4 files, ~4100 lines total. `UiBakeHandler.Archetype.cs` carries: snapshot-archetype dispatch, IR-archetype dispatch, kind helpers (button/readout/label spawners), theme propagation, sprite resolution. `UiBakeHandler.cs` root carries: snapshot path, JSON parsing helpers, panel-kind anchor logic. Cross-file find-the-helper costs.
- **Why bad**: `WireIlluminatedButtonIcon` lives in root (snapshot helper, ~1051) but `SpawnIlluminatedButtonRenderTargets` lives in Archetype.cs (IR helper, ~610). Same job, different files, easy to miss when refactoring. Drove duplicate-impl in Bug F/G.
- **Fix**: re-split:
  - `UiBakeHandler.cs` — entry points (`SavePanelSnapshotPrefab`, `BakePanelIr`), error types
  - `UiBakeHandler.Snapshot.cs` — snapshot path child-iter (current `BakePanelSnapshotChildren` + Snapshot archetype)
  - `UiBakeHandler.Frame.cs` — IR/Frame path (kept)
  - `UiBakeHandler.Components.cs` — shared kind spawners (button/readout/label)
  - `UiBakeHandler.Layout.cs` — anchor/zone/RT helpers + JSON readers
- **Effort**: medium — pure move-by-region; no logic change. Run after green re-bake.

## Imp-8 — Idempotency + dry-run mode

- **Smell**: handler always `PrefabUtility.SaveAsPrefabAsset` overwrites destination. No dry-run. No collision detect when destination has manual edits (e.g. designer wired Button click in Inspector).
- **Why bad**: silent destruction of user work on re-bake. Test loop wants pre-flight bake without mutating disk.
- **Fix**: `SavePanelSnapshotPrefab(item, options { dry_run, force_overwrite })`. Dry-run → skip `SaveAsPrefabAsset`, return `BakeReport` only. Default mode → diff destination components vs expected; warn on unexpected components, fail on `--force-overwrite=false`.
- **Effort**: low — option flag + early-return branch.

## Improvement → Bug interaction

| Imp | Resolves bugs by mechanism |
|---|---|
| Imp-1 unify dispatch | Removes the surface where F/G/H were possible (snapshot path can't silently lack arms when arms live in shared dispatcher) |
| Imp-2 typed JSON | B/K (col/row/sub_col now read), L (typed `kind` field auto-canonical) |
| Imp-3 propagate errors | All bugs gain test-loop visibility |
| Imp-4 validation gate | E (icon/shape contract enforced), L (kind enum validated) |
| Imp-5 BakeReport | All bugs become diff-detectable |
| Imp-6 placeholder enum | F (caption-on-empty becomes contract, not symptom) |

## In-session execution plan (final, supersedes earlier order)

Phase 1 — fast green for re-bake:
1. Imp-2 typed POCOs (drops regex; needed by 1, 4, 6)
2. Imp-1 unify kind dispatch (collapses snapshot/IR divergence)
3. Imp-6 placeholder vs missing-sprite enum (drives caption decision)
4. Tier 1 bugs D, A, F, G, H (now mechanical applications of dispatcher)
5. Imp-3 error propagation
6. **Re-bake** — first green target

Phase 2 — functional + layout:
7. Tier 2 bugs C, I
8. Tier 3 bugs B, K
9. Tier 4 bug E (DB migration)
10. **Re-bake** — full coverage

Phase 3 — hygiene + verification:
11. Imp-4 validation gate
12. Imp-5 BakeReport + fixture
13. Tier 5 bugs J, L
14. Imp-8 dry-run mode
15. Imp-7 file re-split
16. **Re-bake** — green-on-green confirmation
