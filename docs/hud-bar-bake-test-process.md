# hud-bar bake-test process

**Date:** 2026-05-07
**Scope:** single-element bake-test pipeline. Target = `hud-bar` panel in `CityScene`.
**Goal:** validate `ui-element-definitions.md` → DB catalog → snapshot → bake → prefab → CityScene runtime path end-to-end on ONE locked panel before extending to remaining 12.
**Authority docs:** `docs/ui-element-definitions.md` (locked def + preserve list) + `ia/specs/architecture/data-flows.md` (UI bake flow).

---

## Why this doc exists

13 panels locked + audio-cues registry locked. Rebake risk = regress working code (MiniMapController, SpeedButtonsController, BlipEngine wiring, slug-walk binding). This doc = mechanical step list to bake hud-bar WITHOUT losing those.

Process here = **template for the other 12 panels** once hud-bar passes. Each panel later cribs this doc shape.

---

## Out of scope

- New panels beyond hud-bar.
- HUD-bar interactivity changes (click handlers stay; only presentation/layout layer rebakes).
- Audio cue additions (registry frozen — net new cues need explicit follow-up).
- Catalog schema migration. Use existing `catalog_panel` + `panel_child` tables.
- Stage decomposition. This is a smoke pass, not a full master-plan stage.

---

## Pre-flight gates (run before bake)

| # | Gate | Command / source | Pass criteria |
|---|---|---|---|
| 1 | Locked def freshness | `git diff docs/ui-element-definitions.md` | Edits A+B+C committed. No pending hud-bar edits. |
| 2 | Catalog row exists | `mcp__territory-ia__catalog_panel_get slug=hud-bar` | Returns row. `layout_template=hstack`, `gap_px=8`, padding `{4,8,8,4}`. |
| 3 | Children registered | `mcp__territory-ia__catalog_panel_refs slug=hud-bar` | 19 children. Slugs match `Assets/UI/Snapshots/panels.json` items[0].children[].instance_slug. |
| 4 | Existing prefab snapshot | `git log --oneline Assets/UI/Prefabs/Generated/hud-bar.prefab \| head -3` | Note last bake sha. Used for diff after rebake. |
| 5 | Working features baseline | Boot CityScene in editor, capture: minimap renders + speed-1..5 buttons toggle + zoom-in/out moves camera + AUTO toggles auto-mode + audio cues fire on click/hover. | Manual eyeball + console log. |
| 6 | C# compiles clean | `npm run unity:compile-check` | Exit 0. |
| 7 | DB bridge alive | `npm run db:bridge-preflight` | Exit 0. |

**If any gate fails → fix before bake. Do NOT bake on red.**

---

## Bake invocation

### Step 1 — refresh snapshot from DB

```bash
npm run snapshot:export-game-ui
```

- Reads `catalog_panel` + `panel_child` rows.
- Writes `Assets/UI/Snapshots/panels.json` (schema_version 4).
- Diff: only `snapshot_id` timestamp + drift if catalog edited since last export. If drift in hud-bar children → STOP. Either revert catalog edit or re-author locked def first.

### Step 2 — bake hud-bar prefab

```bash
npm run unity:bake-ui
```

- `tools/scripts/unity-bake-ui.ts` → `unity_bridge_command kind=bake_ui_panels` → `UiBakeHandler.BakeFromPanelsSnapshot`.
- Writes/updates `Assets/UI/Prefabs/Generated/hud-bar.prefab`.
- Editor stays open (bridge command, not batchmode).

### Step 3 — diff prefab

```bash
git diff Assets/UI/Prefabs/Generated/hud-bar.prefab | head -200
```

- Expected diff: layout positions / gap / padding / icon sprite refs ONLY.
- **Forbidden diff:** removal of MonoBehaviour script refs (HudBarDataAdapter, MiniMapController, SpeedButtonsController, IlluminatedButton, ThemedButton). If those disappear → rebake REGRESSES preserve list. Revert + diagnose.

---

## Runtime verification in CityScene

Target = re-prove every preserve item from `docs/ui-element-definitions.md` §hud-bar §Existing Implementation (preserve) still works after rebake.

### A. Compile + boot

```bash
npm run unity:compile-check
```

Open `Assets/Scenes/CityScene.unity` in Editor, enter Play Mode.

### B. Visual smoke checklist

| Check | Method | Pass |
|---|---|---|
| HUD bar renders all 19 children | Eyeball | 3 left zone (zoom-in/out/recenter) + 8 center (label/auto/+/-/graph/map/readout/pause) + 8 right (speed-1..5/play/build-res/build-com). |
| MiniMap button → opens minimap | Click `hud-bar-map-button` | MiniMapController `Open` fires. Minimap visible. |
| Speed buttons drive `TimeManager` | Click 1x..5x | `TimeManager.SetTimeSpeedIndex` fires. Sim speed changes. |
| Zoom in/out moves camera | Click zoom-in/out | `CameraController` zoom step fires. |
| Pause toggles | Click pause | `TimeManager` pause toggles. |
| AUTO toggles auto-mode | Click AUTO | UiManager auto-mode flip. |
| BUDGET +/- adjusts city budget | Click `+` / `-` | EconomyManager budget delta. Readout updates. |
| BUDGET graph opens panel | Click graph | `GrowthBudgetPanelController` lazy-spawn fires. |
| Build buttons activate placement | Click build-res / build-com | Placement mode activates. |
| Audio cues fire | Hover any button → click | `UiButtonHover` blip on enter, `UiButtonClick` blip on click. Audible + console log if instrumented. |
| Caption fallback intact | Inspect AUTO / MAP / +/- / 1x..5x buttons | TextMeshProUGUI caption shows when icon sprite missing. |
| Geography-init gate intact | Boot before geography ready (timing-sensitive — re-enter Play Mode) | UI responsive even if `cityStats` reads return defaults. |

### C. Headless run (optional)

```bash
npm run unity:testmode-batch -- --quit-editor-first --scenario-id city-hud-smoke
```

If a `city-hud-smoke` scenario exists. If not — defer; manual smoke covers.

### D. Bridge introspection

```bash
mcp__territory-ia__findobjectoftype_scan type=HudBarDataAdapter
mcp__territory-ia__findobjectoftype_scan type=MiniMapController
mcp__territory-ia__findobjectoftype_scan type=SpeedButtonsController
```

Each returns ≥1 hit in CityScene. Zero hits → preserve list violated.

---

## Drift watch — preserve list cross-check

After Play Mode smoke, cross-reference each preserve item from `docs/ui-element-definitions.md`:

- [ ] HudBarDataAdapter — producer cache in Awake, `RebindButtonsByIconSlug`, `WireClickHandlers`, `EnsureSpeedSlot`, stale-button cleanup, lazy-spawn GrowthBudgetPanelController.
- [ ] MiniMapController — open/close + render path unchanged.
- [ ] SpeedButtonsController — 1x..5x toggle pattern unchanged.
- [ ] TimeManager — `SetTimeSpeedIndex` single mutation path unchanged.
- [ ] CityStats — UI reads gated on `geographyManager.IsInitialized`.
- [ ] EconomyManager — budget +/- emits `EcoMoneySpent` / `EcoMoneyEarned` blips.
- [ ] IlluminatedButton + IlluminatedButtonDetail — `iconSpriteSlug` runtime authority over Inspector slot binding.
- [ ] UiAssetCatalog (Stage 9.13) — sprite lookup by slug succeeds.

**Audio preserve cross-check (registry):**

- [ ] ThemedButton auto-emits `UiButtonHover` on pointer enter + `UiButtonClick` on click for every hud-bar button.
- [ ] BlipBootstrap singleton survives scene load.
- [ ] PlayerPrefs volume / mute persistence intact (toggle in settings, restart, recheck).

Any unchecked → **STOP**. Fix or revert prefab.

---

## Pass criteria

All of:

1. Pre-flight gates 1–7 = green.
2. Bake step 1 + 2 = exit 0.
3. Prefab diff = layout/icon-only. No script-ref removal.
4. Runtime smoke A + B = all rows pass.
5. Drift watch = all checkboxes ticked.
6. `npm run validate:all` = green post-rebake.

---

## Rollback

If any pass criterion fails:

```bash
git checkout HEAD -- Assets/UI/Prefabs/Generated/hud-bar.prefab Assets/UI/Snapshots/panels.json
```

Diagnose root cause:

- **Catalog drift?** → reconcile `catalog_panel` row vs locked def; re-export snapshot.
- **Bake handler bug?** → add Stage to `game-ui-catalog-bake` plan with §Red-Stage Proof anchored at the failing assertion.
- **Preserve violation?** → bake handler dropped a script ref. File a TECH issue + re-author preserve list deeper.

No silent partial-fixes. Rollback first, fix in a tracked stage second.

---

## Pass → next step

When hud-bar passes:

1. Commit: `feat(ui-bake): hud-bar single-element bake-test pass`. Include prefab diff + snapshot diff.
2. Open master-plan extension in `game-ui-catalog-bake` (or new `ui-element-rollout` plan) → Stage 1 = hud-bar (closed by this doc), Stage 2..N = remaining 12 panels using THIS doc as template.
3. Update `docs/ui-element-definitions.md` hud-bar row in changelog with bake-pass sha.

---

## Process template reuse

For each subsequent panel, copy this doc to `docs/{panel-slug}-bake-test-process.md`. Edits per panel:

- Pre-flight gate 3: child count + slug list.
- Runtime smoke section B: panel-specific behavior checklist.
- Drift watch: preserve list from that panel's `#### Existing Implementation (preserve)` sub-section in `ui-element-definitions.md`.

If a panel has NO existing implementation (e.g. notifications-toast = new), drop drift watch + add §New Wiring section instead.

---

## Open follow-ups (track during run, file post-pass)

- ThemedButton hover/click auto-emit telemetry — confirm via console log line per click. If silent → instrument before passing.
- `validate:catalog-panel-coverage` re-run. Expected zero orphans.
- Audio-cues registry — any cue fired during test that's NOT in the locked 15-row table → drift. File for registry update.

---

## Iteration log (rebake series 2026-05-08)

| Rebake | Symptom | Root cause | Fix | Source touched |
|---|---|---|---|---|
| 1 | Right zone buttons misplaced; Center labels invisible; root white | Right zone Col/Row wrappers missing childControl + ContentSizeFitter; root Image color = white default | Col VLG + Row HLG `childControlWidth/Height=true, childForceExpand=false`; ContentSizeFitter PreferredSize/PreferredSize on Cols/Rows; root `bgImage.color = ui-surface-dark (0.196, 0.196, 0.196, 1)` + `raycastTarget=false` | `UiBakeHandler.Archetype.cs`, `UiBakeHandler.cs` |
| 2 | All 16 LayoutGroups serialized `m_ChildControlWidth=0` despite source `=true` | Stale-DLL bake — bake-ui ran before Editor recompiled .cs edits | Force compile gate via `unity_compile` (or batchmode `unity:compile-check` when Editor closed) BEFORE every `unity:bake-ui` | (process gate) |
| 3 | Hud-bar root Y=80 too short → Right Col stacked (zoom-in 64 + spacing 4 + zoom-out 64 = 132) overflows below dark strip; Center labels invisible | `ApplyPanelKindRectDefaults` PanelKind.Hud `sizeDelta.y = 80` | `sizeDelta.y = 144` (fits 132 stack + 4+4 padding + headroom; Center 3-row 32-px label stack also fits) | `UiBakeHandler.cs:666` |
| 4 | ThemedLabel TMP placeholder "--" hard to see; readout TMPs render empty pre-runtime | `SpawnThemedLabelChild` spawns TMP without explicit color/autosize; `SpawnSegmentedReadoutRenderTargets` text=string.Empty | Label TMP: `color=Color.white`, `enableAutoSizing=true`, range 8..18. Readout TMP: placeholder text=`"0"`, `color=Color.white` so visual smoke verifies pre-bind | `UiBakeHandler.Button.cs:SpawnThemedLabelChild`, `UiBakeHandler.Archetype.cs:SpawnSegmentedReadoutRenderTargets` |
| 5 | Bake-5 mid-loop blocker — Bee build IPC FD exhaustion | Unity Bee async-pipe FD pool degraded mid-session; cached compilation_failed=true persists across `refresh_asset_database` | Restart Unity Editor (close + reopen project). After restart, compile gate clean → bake-5 prefab matches expected layout. Codified: §"Bee build IPC failure mode" below. | (Editor restart, no source touched) |
| 6 | Hud-bar root rect hardcoded in C# (`ApplyPanelKindRectDefaults` PanelKind.Hud) — DB had no authority over panel-root sizing | Pre-DB-rect: kind defaults applied unconditionally; rebake-3/4 fixes lived in C# only | Migration `0109_panel_detail_rect_json` adds `panel_detail.rect_json jsonb`. Snapshot exporter emits `fields.rect_json`. `UiBakeHandler.ApplyPanelRectJsonOverlay` overlays DB rect on top of kind defaults (per-axis last-write-wins). Hud-bar seeded with rebake-6 values: `{pivot:[0.5,1], anchor_min:[0,1], anchor_max:[1,1], size_delta:[-16,144], anchored_position:[0,-8]}`. Code defaults preserved as fallback. | `db/migrations/0109_panel_detail_rect_json.sql`, `tools/scripts/snapshot-export-game-ui.mjs`, `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` |
| 7 | Toolbar overlapped hud-bar bottom edge (top inset 100 px vs hud-bar footprint 152 px) | Toolbar PrefabInstance scene override `m_AnchoredPosition.y=50`, `m_SizeDelta.y=-300` resolved to top inset 100 | CityScene.unity toolbar overrides → `m_SizeDelta.y=-352`, `m_AnchoredPosition.y=24` → top inset 152 (no overlap), bottom inset 200 preserved. Scene-only fix; toolbar prefab untouched. | `Assets/Scenes/CityScene.unity` (toolbar PrefabInstance overrides) |

### Compile gate guidance (lesson from rebake-2 + rebake-5)

- Editor open → use `mcp__territory-ia-bridge__unity_compile` (synchronous Bee snapshot via bridge job).
- Editor closed → use `npm run unity:compile-check` (batchmode, requires Editor lock free).
- **Do NOT run `unity:bake-ui` until compile-status `compiling=false, compilation_failed=false`.** Stale-DLL bake silently produces incorrect prefab from old assembly.

### Bee build IPC failure mode (rebake-5 blocker)

Symptom — `compilation_status.compilation_failed=true` with `last_error_excerpt` carrying:
```
System.NotSupportedException: Could not register to wait for file descriptor 1439
  at Bee.BeeDriver.BeeDriver_RunBackend.ReadEntireBinlogFromIpcAsync ...
```
Root cause — Unity Bee build process exhausted async-pipe file descriptors. Mid-session degradation; not a code error.

Recovery — `refresh_asset_database` + `execute_menu_item Assets/Refresh` do NOT clear it (cached failure persists across refresh cycles). Must restart Unity Editor (close + reopen project). After restart, recompile gate reverts to clean.

When this hits, agent must STOP the bake loop and surface "restart Editor" to user — cannot recover via bridge.


