# Stage 13.x — Bake Handler + Icon Pipeline Findings (live log)

Caveman. Append-only. Newest entry on top. For fast agent resume during ship-stage loop.

Renamed from `stage-13.2-findings.md` → `stage-13.x-findings.md` to span 13.2 closeout + 13.3 onward.

---

## Stage 13.3 — Icon import pipeline

### Operator override — proceed with 22 + reserve 5 hooks (2026-05-03 second run)

Operator unblocked Stage 13.3 by overriding prior STOP. Directive:
- Proceed with 22 symbols in `web/design-refs/step-1-game-ui/cd-bundle/icons.svg`.
- Reserve 5 missing ids as known slugs in code with no-op behavior:
  - `icon-happiness`, `icon-population`, `icon-money`, `icon-bond`, `icon-envelope`
- Missing slug → resolver returns `icon-info` fallback + emits per-slug per-session deduped `Debug.LogWarning`.
- Designer adds 5 SVGs later → split tool picks them up automatically, zero code change.
- Stage exit "Missing-id fallback warns + substitutes `icon-info` placeholder; bake does not crash" already part of acceptance — lean into it for the 5 absent ids.

### Blocker scan — designer SVG handoff (T13.3.0) — SUPERSEDED

Stage opened BLOCKED on T13.3.0 designer SVG handoff for 5 new ids (above). Operator override supersedes the original STOP. 5 ids carry forward as reserved-slugs in code; SVG handoff still owed by designer but no longer gates ship.

### Phase 3 STOP — superseded by operator override

Original STOP (verified 22 of 27 symbols, then halted). Now resumed under operator directive — split tool emits whichever count exists, theme dict covers existing PNGs, ThemedIcon falls back on missing slugs.

### TECH-9862 implementation (2026-05-03)

- `tools/scripts/icons-svg-split.ts` ships. Uses `sharp@0.34.5` for SVG→PNG @ 128x128 transparent.
- `CANONICAL_ICON_SLUGS` constant carries the full 27-slug catalog (22 shipped + 5 reserved).
- Split parses `<symbol id="icon-…">` regex from source SVG, builds standalone `<svg>` per symbol with hoisted presentation attrs (fill/stroke), rasterizes via sharp, emits `{slug}.png` + `{slug}.png.meta`.
- `.meta` derives a deterministic guid from `md5("territory-icon-{slug}")` so re-runs land on the same Unity asset id.
- Wired into `transcribe-cd-game-ui.ts` as a sub-step after IR emit (gated on `icons.svg` existence).
- `package.json` script `icons:split` for ad-hoc reruns.
- First run: 22 PNGs emitted, 5 missing reported as reserved-slug hooks. 2 stale leftovers (`population-sta*`) cleaned from `Assets/Sprites/Icons/` (not in canonical catalog).
- Decision: `sharp` was already a transitive dep at top-level node_modules — no new install needed.
- Decision: deterministic md5 guid (vs random) chosen so importer doesn't churn `.meta` on re-runs.

### TECH-9864 implementation (2026-05-03)

- `Assets/Scripts/UI/Themed/ThemedIcon.cs` rewritten — slug-driven sprite resolution. Priority: Inspector-pinned `_spriteRef` (legacy) → `theme.TryGetIcon(_iconSlug)` → `icon-info` fallback + per-slug per-session deduped `Debug.LogWarning` (`HashSet<string>` keyed by slug, cleared on Unity domain reload).
- `IconSlug` public bake-time setter exposed so `UiBakeHandler` can write the IR `iconSlug` onto the prefab without runtime AddComponent on existing nodes (Invariant #6 honored — bake spawns fresh GameObject per icon).
- `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` — `IrTab` + `IrRow` C# DTOs extended with `public string iconSlug;` mirroring TS `tools/scripts/ir-schema.ts` extension.
- `Assets/Scripts/Editor/Bridge/UiBakeHandler.Frame.cs` — `EmitRowChildren` spawns Icon child before Caption when `row.iconSlug` non-empty; new `SpawnRowIcon(rowRoot, iconSlug, theme, rowHeight)` helper composes RectTransform + Image + ThemedIcon + LayoutElement (square preferredW/H = rowHeight - 4f, flexibleWidth=0). `WireTabBarPages` now persists `iconSlug` field onto `ThemedTabBar.PageBinding[]` (the page descriptor itself extended with optional `iconSlug` field).
- `Assets/Scripts/UI/Themed/ThemedTabBar.cs` — `PageBinding` struct extended with optional `iconSlug` for per-tab icon binding (Stage 13.4 follow-up will spawn the per-tab icon under the tab strip when the IR transcriber starts assigning per-tab icons).
- Decision: `ThemedIcon._iconSlug` written via SerializedObject path so re-bake idempotency holds; `IconSlug` property setter still touches the backing field directly so non-SerializedObject callers (tests / programmatic bakes) work too.
- Decision: row icon size derived from `rowLe.preferredHeight - 4f` so icons sit cleanly inside the row without overflow; falls back to 20px when row height unset.
- Decision: transcribe-side IR emit (assigning concrete `iconSlug` values to rows + tabs) deferred to a follow-up stage. Stage 13.3 scope = bake-side accommodation; designer mapping pass + transcriber update is its own work item.
- Per-tab icon visual emit deferred — `ThemedTabBar.PageBinding.iconSlug` carries the value so the runtime adapter (Stage 13.4) can spawn the icon next to the tab label when the strip layout is finalized.

### TECH-9863 implementation (2026-05-03)

- `UiTheme.cs` extended with `iconEntries: List<IconKv>` field, `TryGetIcon(slug, out Sprite)` lookup, lazy `_iconCache` dict + `EnsureIconCache()` rebuild path mirroring sibling token caches. `InvalidateTokenCaches()` resets the icon cache too.
- New `IconKv` struct (`{ string slug; Sprite sprite }`) — Unity-serializable list-of-pair, no SerializedDictionary dep.
- New `Assets/Scripts/Editor/UiThemeIconPopulator.cs` — `[MenuItem("Tools/Territory/UI/Populate UiTheme Icons")]`. Canonical 27-slug array (mirrors TS `CANONICAL_ICON_SLUGS`) drives population. Each slug binds to `{IconSpriteFolder}/{slug}.png`; missing PNG → null sprite entry (so runtime fallback path takes over).
- Decision: parallel canonical-slug constants in TS + C# rather than a generated file. Trade-off: two surfaces to keep in sync vs avoiding codegen tooling. Stage 13.3 scope is small; manual sync acceptable.
- Decision: `[MenuItem]` populate vs `[ContextMenu]` on a custom editor — `[MenuItem]` chosen so populate works on any UiTheme asset selected in Project pane (no editor class boilerplate per asset).
- Existing `Assets/UI/UiTheme.asset` not present — will be picked up automatically when human/agent creates the SO via `Create > Territory/UI/Ui Theme`. Populate menu falls back to first `t:UiTheme` asset in project when no selection.

§Plan Digest authoring status — **complete** for all 3 tasks (TECH-9862 / TECH-9863 / TECH-9864). Goal / Acceptance / Pending Decisions / Implementer Latitude / Work Items / Test Blueprint / Invariants & Gate populated per `/stage-authoring` template.

What blocks Pass A — implementation, not authoring:
- TECH-9862 §Acceptance #5: "27 PNGs in `Assets/Sprites/Icons/` (existing 22 + 5 new ids per D3)" — cannot satisfy without designer SVG extension. Split tool authoring (work item #1) ships fine; the *invocation* against current SVG would emit 22 PNGs, missing 5.
- TECH-9863 §Acceptance: "all 27 expected ids resolve via `TryGetIcon`" — depends on T9862 producing 27 PNGs.
- TECH-9864 §Acceptance: "16 panels re-bake clean with zero unresolved icon warnings" — depends on T9863 populated dict.

Per TECH-9862 §Pending Decisions: "BLOCKED until designer extends `web/design-refs/step-1-game-ui/cd-bundle/icons.svg` with 5 new symbol ids […] T13.3.0 handoff is out-of-band — implementer pings Javier before starting."

Decision — **STOP ship-stage 13.3 here**. Designer SVG handoff is upstream of Pass A. Resuming `/ship-stage game-ui-design-system 13.3` once SVG extended will succeed (no §Plan Digest re-author needed; readiness gate will pass + Pass A enters with 27-id source).

Unblock action — Javier produces extended icons.svg (22 → 27 symbols) → drop into `web/design-refs/step-1-game-ui/cd-bundle/icons.svg`. Re-run `/ship-stage game-ui-design-system 13.3`.

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

