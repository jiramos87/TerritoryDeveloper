# Stats-Panel Pilot — Handoff for Next Main Session

> Paste this whole file into the next agent's first turn. Self-contained. No prior conversation context needed.

## Intent + spirit

**Stats-panel = pilot for whole-game UI redesign.** We iterate on one panel's DB definition until visually approved, then promote learnings (tokens, factories, layout templates) to the shared design system and rebake every panel.

Each iteration = one tiny visual delta → publish DB version → bake prefab → user opens Play Mode → screenshot → verdict (accept/reject) → next round. Tracker is `docs/stats-panel-design-iteration.md` — append a row per round.

**Centralization > one-off fixes.** Anything visual that appears in more than one place (back-arrow, close button, frame border, row layout) becomes a shared factory or design-system token at first sighting, so future visual changes propagate everywhere.

**DB is source of truth.** Panel rows live in `panel_detail` + `panel_child` (Postgres). `panels.json` snapshot is derived. Prefab is baked output. Never hand-edit prefabs.

**Stack:**
- DB: Postgres `localhost:5434` postgres/postgres `territory_ia_dev`. Catalog tables: `catalog_entity`, `panel_detail`, `panel_child`, `entity_version`. jsonb columns (`padding_json`, `params_json`, `rect_json`, `layout_json`) have no shape constraint — extend with new keys freely.
- Snapshot: `node tools/scripts/snapshot-export-game-ui.mjs` writes `Assets/UI/Snapshots/{panels,tokens,components}.json`.
- Bake: `npm run unity:bake-ui` (calls Editor bridge `bake_ui_from_ir` kind) → reads panels.json → writes `Assets/UI/Prefabs/Generated/{slug}.prefab` via `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs`.
- Runtime sub-views: `Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs` (`Territory.UI.Modals`) injects nav-header into settings/save/load sub-view prefabs at runtime.
- Editor open → bridge `unity_compile` MCP, not `npm run unity:compile-check` (batchmode blocked by Editor lock).

## Where we are (state after iter 7)

stats-panel v=7 (entity_id=220, version_id=562) published + baked. Awaiting Play Mode screenshot + verdict.

### Iter 7 changes (just landed)

**New official back-arrow factory** — `Assets/Scripts/UI/Decoration/NavBackButton.cs`:

```csharp
public static class NavBackButton {
  public const float DefaultSize = 40f;
  public static GameObject Spawn(GameObject parent, float size = DefaultSize) { ... }
}
```

40×40 dark chip (RGB 0.18 alpha 0.9) + `<` TMP glyph (24pt bold white, raycastTarget=false). No onClick wired — caller wires it.

**Two callers wired:**
1. `PauseMenuDataAdapter.InjectNavHeader` (runtime) — 27-line back-button construction replaced with `NavBackButton.Spawn(headerGo)` + `backGo.GetComponent<Button>().onClick.AddListener(OnBack)`.
2. `UiBakeHandler.BakeChildByKind` (bake-time) — new `case "back-button":` inserted after `case "view-slot":`. Calls `NavBackButton.Spawn(childGo)`, collapses spawned chip Image+Button onto `childGo` (so corner-overlay + LayoutElement still target one wrapper), moves `<` label up, attaches `UiActionTrigger(pj.action)`, sizes via `pj.corner_size` or `NavBackButton.DefaultSize`.

**stats-close DB row flipped:**
```sql
UPDATE panel_child SET params_json = jsonb_build_object(
  'kind','back-button',
  'action','stats.close',
  'corner','top-left',
  'corner_offset','22,22'
) WHERE panel_entity_id = 220 AND instance_slug = 'stats-close';
```

Dropped `icon`, `corner_size`, `tooltip`. Kept `corner:"top-left"` + `corner_offset:"22,22"` so `ApplyCornerOverlay` escapes panel VLG flow and pins close-button to top-left of stats-header band.

### Iteration history (full log in `docs/stats-panel-design-iteration.md`)

| # | Verdict | Note |
|---|---------|------|
| 1 | reject | initial border (2px / 4px radius) invisible |
| 2 | (rebake fixes, no DB change) | RoundedBorder mesh wiring |
| 3 | reject | items collide with thick border; close still illuminated-button |
| 4 | reject | back-arrow sprite = white square (missing); overlay too big |
| 5 | reject | sprite `left-arrow.png` not matched by resolver |
| 6 | reject | sprite resolver fixed but user pivoted — wants Settings `<` glyph promoted as official back-arrow |
| 7 | **pending** | NavBackButton factory + bake-time `back-button` kind + DB flip |

## Next action

1. **Confirm Editor is open** on `/Users/javier/bacayo-studio/territory-developer` (Play Mode entry will fail otherwise).
2. **Ask user to enter Play Mode → open stats-panel → screenshot → paste absolute path.**
3. **Read screenshot** via Read tool.
4. **Verdict:**
   - Accept → propose next iteration target (likely: type-scale tokens for row body/value text, or extend `NavBackButton` to other panels).
   - Reject → identify visual delta, plan one mechanical fix (DB patch OR bake code OR factory tweak), publish v=8, rebake.
5. **Append row to `docs/stats-panel-design-iteration.md` §Iteration Log.**

## Mechanical playbook (per iteration round)

```
1. Read user verdict + screenshot path → Read screenshot.
2. Identify single smallest delta (DB patch ∪ bake-code change ∪ factory tweak).
3. If C# edit: bridge unity_compile MCP (Editor open). Editor closed → npm run unity:compile-check.
4. If DB edit: PGPASSWORD=postgres psql -h localhost -p 5434 -U postgres -d territory_ia_dev -c "UPDATE panel_child SET params_json = params_json || jsonb_build_object(...) WHERE ..."
5. ui_panel_publish MCP slug=stats-panel → record new version_number.
6. node tools/scripts/snapshot-export-game-ui.mjs
7. npm run unity:bake-ui (timeout 120s)
8. Append iter row to docs/stats-panel-design-iteration.md
9. Hand back to user for Play Mode screenshot.
```

## Key files (memorize)

- `docs/stats-panel-design-iteration.md` — tracker (authoritative iteration log)
- `Assets/Scripts/UI/Decoration/NavBackButton.cs` — shared back-arrow factory (NEW iter 7)
- `Assets/Scripts/UI/Decoration/RoundedBorder.cs` — shared panel border mesh (iter 1)
- `Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs` — runtime nav-header injection (`InjectNavHeader` lines ~240-280)
- `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` — `BakeChildByKind` switch (line 527), `NormalizeChildKind` (line 488), `ApplyCornerOverlay` (line ~2650), `ApplyRoundedBorder` (line ~1864)
- `Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs` — `ResolveButtonIconSprite` (line ~820, sprite resolver with fallback paths)
- `tools/scripts/snapshot-export-game-ui.mjs` — panels.json generator

## Open decisions (pending promotion to design system)

- **Border color token** — keep raw `led-amber` light-stop, or seed dedicated `color-border-accent`?
- **Type scale** — current 3 tokens insufficient. Need `size-text-body-row`, `size-text-value`, possibly `size-text-section-header`.
- **Font-family token** — currently default TMP `LiberationSans`. Promote before whole-game rebake?
- **`frame-modal-card`** — generalize padding/border/radius across all `layout_template='modal-card'` panels.
- **`row_columns`** — per-panel param now. Promote to layout-template default?

## Important behavioral rules

- **Caveman output style** — all agent prose (chat, docs, commits) drops articles/hedging. Fragments OK. User-facing chat = product/game language, not tech jargon. See `ia/rules/agent-output-caveman.md`.
- **Editor-open compile gate** — never dismiss `unity:compile-check` Editor-lock abort as green. Use bridge `unity_compile` MCP or poll user to close Editor.
- **UI bake edits need explicit rebake** — `UiBakeHandler.cs` writes prefabs to disk. Play Mode loads cached prefab. After every bake-pipeline edit: `npm run unity:bake-ui`.
- **macOS case-insensitive FS** — POCO file collisions (e.g. `ZoneSService.cs` vs `ZonesService.cs`) silently collapse on APFS. Place new shared POCOs in subfolders, never adjacent to existing namespace files.
- **No commits without explicit instruction.** This pilot has NOT been committed yet — all iter 1-7 work lives in dirty tree on `feature/asset-pipeline`. Do not commit unless user says so.

## Verification (after iter accept)

```
npm run validate:all
npm run unity:compile-check  (Editor closed) OR unity_compile MCP (Editor open)
```

Then propose token promotion deltas per "Open decisions" above. Stage commit only when user approves bundle.
