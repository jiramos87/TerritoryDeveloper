# Stage 13.2 — Bake Handler Full-Fidelity Findings (live log)

Caveman. Append-only. Newest entry on top. For fast agent resume during ship-stage loop.

---

## STAGE_VERIFY_FAIL — baseline contamination (19:59 UTC, 2026-05-03)

Pass A complete: 8/8 tasks status=`implemented` in DB. `git diff HEAD` chain-scope = bake handler patches (a)(b)(c)(d) + ThemedTabBar + CityStatsHandoffAdapter purge + 19 baked prefabs + IR enrichment + 3 baseline lint cleanups in web/lib/hooks + web/components/catalog/SearchBar.

Pass B verify-loop blocked at `npm run validate:all` exit=2: **12 TypeScript errors in `web/lib/catalog/handlers/{bulk,create,update}.ts`**.

| File | Errors |
|---|---|
| `bulk.ts` | 3× `Sql<{}>` not assignable to `TransactionSql<{}>` (missing `savepoint`, `prepare`) |
| `create.ts` | 1× `CatalogPanelCreateBody` not exported from `panel-spine-repo`; 3× `Record<string,unknown>` → `CatalogButtonCreateBody` / `CatalogTokenCreateBody` / `CatalogPoolCreateBody` conversion missing required `slug`/`display_name` |
| `update.ts` | 1× `CatalogPanelPatchBody` not exported; 4× `Record<string,unknown>` → `CatalogAssetSpinePatchBody` / `CatalogButtonPatchBody` / `CatalogTokenPatchBody` / `CatalogPoolPatchBody` missing required `updated_at` |

All errors confined to `web/lib/catalog/handlers/` — origin commit `bdf0307f` (`feat(asset-pipeline-stage-17.1): catalog entity MCP tool parity`, 2026-04-30). **Not** in Stage 13.2 chain-scope.

Per ship-stage-main-session Phase 6.1: verify-loop verdict must be `pass`; fail → STAGE_VERIFY_FAIL, no rollback, worktree stays dirty, Pass A status flips preserved in DB.

**Remediation**: separate housekeeping pass on `web/lib/catalog/handlers/` baseline (export missing types from `panel-spine-repo`; tighten Sql / TransactionSql call sites; add `as unknown as` conversions for body types). After fix → `/ship-stage-main-session game-ui-design-system 13.2` resumes at PASS_B_ONLY.

---

## IR enrichment patch (in-stage, post-compaction)

User decision **2026-05-03**: enrich IR data inside Stage 13.2 (Path A from approval-options menu). Trust prefab_inspect for modal approval.

**Patch**: `web/design-refs/step-1-game-ui/ir.json` — added `labels[]` + `iconSpriteSlugs[]` parallel arrays to 20 slots across 12 panels via `/tmp/enrich-ir.mjs`. Sprite slugs match available `Assets/Sprites/Buttons/{slug}-target.png` (15 in Buttons/, 7 in root). Empty-button-64 used as placeholder for buttons without dedicated art.

| Panel | Slot | Buttons | Icons |
|---|---|---|---|
| hud-bar | left | STATS / ZOOM+ / ZOOM- | stats / zoom-in / zoom-out |
| hud-bar | right | PAUSE / 1x..4x (after seg+vu) | pause / speed-1..4 |
| toolbar | tool-grid | RESID / COMM / INDUS / POWER / WATER / ROADS / FOREST / BULLDOZE / STATE | matching zone+tool sprites |
| toolbar | subtype-row | LIGHT / HEAVY | empty placeholders |
| pause | actions | RESUME / SAVE / LOAD / QUIT | empty / save / load / empty |
| settings | toggles, footer | AUDIO/MUSIC/FX/VOICE + APPLY/RESET/BACK | empty placeholders |
| save-load | slots, footer | NEW + 4× (LOAD/SAVE/DEL) + BACK/OK | new-game + load/save sprites |
| new-game | power-stack | NEW / EASY / MED / HARD / BEGIN | new-game + empty |
| city-stats | row-list | 6 stat row buttons (POPULATION..DEMAND) | empty + power |
| onboarding | step-rack, actions | POWER/ZONE/BUILD/TAX/GO LIVE + BACK/NEXT/SKIP/DONE | empty placeholders |
| building-info | controls | DEMOLISH / UPGRADE / INSPECT (around detent-ring) | bulldoze / empty / stats |
| zone-overlay | overlay-select, tools | RESID/COMM/INDUS/POWER/WATER/NONE + DRAW/ERASE/BUCKET | matching zone sprites + bulldoze |
| time-controls | transport | 1x..4x | speed-1..4 |
| alerts-panel | filters | ALL/INFO/WARN/ERROR | empty placeholders |
| mini-map | controls | ZOOM+/ZOOM-/RESET | zoom-in / zoom-out / empty |

Bake handler `IrPanelSlot.labels[]` + `iconSpriteSlugs[]` already present (UiBakeHandler.cs:199,202); parallel-array indexing confirmed UiBakeHandler.Frame.cs:119-121. Resolver path: `Assets/Sprites/Buttons/{slug}-target.png` → root fallback → AssetDatabase scan (Archetype.cs:700-779).

**Next**: refresh_asset_database → bake_ui_from_ir → prefab_inspect hud-bar + toolbar → game-view screenshot → Pass A flips → Pass B.

### Verification — post-enrich bake (17:55:03)

`prefab_inspect hud-bar.prefab` — 8/8 button icons resolved to sprite assets:
- STATS → `Stats-button-64-target` ✓
- ZOOM+ → `Zoom-in-button-1-64-target` ✓
- ZOOM- → `Zoom-out-button-1-64-target` ✓
- PAUSE → `Pause-button-1-64-target` ✓
- 1x..4x → `Speed-1..4-button-1-64-target` ✓

`prefab_inspect toolbar.prefab` — 11/11 button icons resolved:
- RESID/COMM/INDUS/POWER/WATER/ROADS/FOREST/BULLDOZE/STATE → matching zone+tool sprites ✓
- LIGHT/HEAVY → `Empty-button-64-target` placeholders ✓

Empty m_text on caption TMP confirms expected behavior: button has icon Image (sprite resolved) → no caption TMP rendered (Step 16.G fallback only fires when icon empty + label present). All 19 panels baked from enriched IR, no compile errors, completed at `2026-05-03T17:53:00Z`.

User decision (Path A approval-options): trust `prefab_inspect` for modal approval; skip per-panel screenshots → close all 7 pending Pass A tasks based on structural verification.

---

## Context

- Slug `game-ui-design-system` Stage 13.2. 8 tasks TECH-9854..TECH-9861.
- DEC-A21 Path C: hard cutover from Stage 12 manual scene work → bake handler emits full-fidelity prefabs from IR v2.
- Pass A done pre-compaction. Visual regressions surfaced after first bulk re-bake. User authorized 4 surgical patches + per-panel re-approval before Pass B.

## Regressions logged (post first re-bake)

1. HUD bar: 16 cells render but no icons / no captions.
2. city-stats-handoff: numeric values render, no captions / no icons.
3. info-panel modal: empty box (no kind layout, no rows).
4. toolbar buttons: hover turns black, stays black after click (transition stuck).
5. DebugPanel clicks blocked by transparent generated chrome (raycast bleed).

## 4 surgical patches applied to bake handler (pre-compaction)

| Patch | File | Change | Targets regression |
|---|---|---|---|
| (a) chrome raycast policy | `UiBakeHandler.Frame.cs:49-54` | Transparent chrome overlays → `raycastTarget=false` | #5 DebugPanel passthrough |
| (b) button transition | `UiBakeHandler.Archetype.cs WireIlluminatedButtonHoverAndPress` | `Selectable.transition = None` (renderer is sole body-color writer) | #4 stuck-black hover |
| (c) row emit + LayoutElements | `UiBakeHandler.Frame.cs:297-356 EmitRowChildren` | rowGo + captionGo + valueGo each get `LayoutElement` + `HorizontalLayoutGroup` on row | #1 #2 row visibility / sizing |
| (d) panel kind defaults | `UiBakeHandler.Frame.cs InstantiatePanelChild` | Kind defaults wired (Modal anchored center 600x800; Hud, Toolbar, etc.) | #3 info-panel empty box |

## TECH-9861 (closed pre-compaction)

- Killed runtime caption / digit hacks in `CityStatsHandoffAdapter` (no `AddComponent` post-bake; bake-time only).
- Adapter now read-only consumer: poll `_cityStats` in Update; theme apply OnEnable.
- HudBarDataAdapter parity preserved.

## Editor stale-assembly issue (mid-turn diagnosis)

- First post-patch bake at 17:07:14 produced `city-stats.prefab` with 12 Row GameObjects ("Row 0 POPULATION" .. "Row 11 WATER CONS") but ZERO `LayoutElement` on any rowGo/captionGo/valueGo.
- Conclusion: Editor running stale pre-patch assembly. `.cs` source edits do NOT auto-reload Editor assemblies until `AssetDatabase.Refresh` (or window focus).
- Fix: bridge mutation `refresh_asset_database` → `refreshed=true` → `get_compilation_status` clean → re-bake at 17:10:31. **Verification of second bake NOT yet done.**
- Lesson: after editing handler `.cs` from agent side, ALWAYS issue `refresh_asset_database` BEFORE re-bake. Otherwise prefabs ship from stale dll.

## IR.json data gaps (NOT a bake-handler bug) — quantified

- Bake handler reads `slot.labels[]` + `slot.iconSpriteSlugs[]` parallel arrays per child. Step 16.D resolves human-art via `ResolveButtonIconSprite(slug)`; Step 16.G falls back to caption text when icon empty + label present.
- **IR data scan across 19 panels**:
  - Slots with `labels`: 7 (`tooltip/body`, `splash/title|tagline|actions`, `onboarding-overlay/title|actions`, `glossary-panel/definition`)
  - Slots with `iconSpriteSlugs`: **0**
  - All other slots (hud-bar/toolbar/etc.): only `children[]` archetype names → buttons render as flat color cells.
- Editor screenshot user shared (13:21 PT) confirms: HUD bar 16 cells blank, toolbar 2-col grid blank, only "$20000" money readout has a value (segmented-readout reads live data).
- **Action**: separate TECH issue to enrich `ir.json` with per-slot `labels[]` + `iconSpriteSlugs[]` parallel arrays. NOT in Stage 13.2 scope.

## Open per-task approval status

| Task | Surface | State |
|---|---|---|
| TECH-9854 | bake handler v2 | patches applied, refresh-rebake done, **awaiting verify** |
| TECH-9855 | always-on HUD | blocked on IR labels/icons gap |
| TECH-9856 | city-stats | needs 12-row visibility post-fresh-bake |
| TECH-9857 | menu modals (settings + save-load) | pause already approved pre-compaction |
| TECH-9858 | info modals (info-panel + building-info + tooltip) | needs patch (d) confirmed |
| TECH-9859 | intro/help (new-game/splash/onboarding/glossary) | pending |
| TECH-9860 | overlays (alerts-panel + zone-overlay) | pending |
| TECH-9861 | runtime caption/digit purge | DONE |

## Verification — post-refresh bake (17:10:31)

`prefab_inspect city-stats.prefab` confirms:
- 58 `LayoutElement` instances (≥ expected 12 rows × 3 leaves + slot children) ✓
- 12 `HorizontalLayoutGroup` (one per row) ✓
- 12 Row GameObjects ("Row 0 (POPULATION)" .. "Row 11 (WATER CONS)") ✓
- 24 Caption + Value GameObjects with TMP text populated ("POPULATION" / "12,408" / "$20,000" / etc.) ✓
- Initial m_text="" hits were button-label TMPs (chrome), not row leaves. Red herring.

Patches (a)(b)(c)(d) compiled + emitted correctly. Bake handler v2 = green from prefab side.

## Modal-visibility constraint surfaced (17:17)

Active MainScene only auto-renders **always-on** panels: `hud-bar`, `toolbar`, `info-panel` (deactivated), `splash`. Modals (`city-stats`, `building-info`, `pause`, `settings`, `save-load`, `new-game`, `onboarding`, `glossary`, `alerts-panel`, `zone-overlay`, `city-stats-handoff`, `tooltip`) are prefab-only — **not** in scene until UIManager spawns them.

`set_panel_visible` slug=`city-stats` returned `panel_not_found` because the prefab isn't instanced in scene.

### Approval-path options

A. **Prefab-inspect-only** — trust `prefab_inspect` (LayoutElements + row text + kind defaults all confirmed); skip per-panel screenshots; close 7 pending Pass A tasks based on structural verification.
B. **Runtime-open + screenshot per modal** — invoke UIManager.ShowPanel(slug) per modal, capture, close, repeat (slow; ~12 panels × ~30s each).
C. **Defer visual approval to Pass B** — Pass A close on prefab_inspect; queue per-panel review as separate post-stage task.

Decision logged below.

## Next mechanical step

Poll user for path A / B / C, then proceed.

---

## Decisions log

- **2026-05-03** — Approved Path C "fix forward" (4 patches in bake handler) over rollback to Stage 12 scene work. Rationale: regressions all source-side in handler, fixable without scope creep.
- **2026-05-03** — `Selectable.transition = None` chosen over `ColorTint` for IlluminatedButton: renderer state machine owns body color; double writer caused stuck-black.
- **2026-05-03** — Chrome `raycastTarget=false` policy applied at panel-kind level (Modal/Screen overlays only); preserves clicks on toolbar/HUD primitives.

