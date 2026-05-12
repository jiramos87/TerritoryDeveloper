# UI Continuation — Single /goal Run Directive

## Mission

Drive whole-game UI implementation forward from pilot baseline (stats-panel + budget-panel done) through every remaining entity in `docs/ui-implementation-tracker.md`, in **one continuous run**, until all rows flip to ✅ status. No mid-run compile-check, no mid-run bake, no mid-run user confirmation, no mid-run screenshots, no mid-run verdict prompts. ALL bakes + compile-checks + screenshots happen **once at the end** as the final regression sweep.

## Hard rules (binding for the entire run)

1. **One compile-check at the end.** No `unity:compile-check`, no `bridge-preflight`, no `refresh-compile.mjs` mid-run. Code edits accumulate dirty in repo; resolver test happens after every entity authored.
2. **One bake at the end.** No `npm run unity:bake-ui` mid-run. The final bake covers all panels in a single Editor pass.
3. **One snapshot regen at the end.** No `node tools/scripts/snapshot-export-game-ui.mjs` mid-run; snapshot is regenerated once after all DB mutations land.
4. **No user-confirmation pauses.** No AskUserQuestion. No "awaiting verdict". No "ready for review?". Autonomous progress only. Iteration trackers append rows without verdict gates.
5. **No commits.** Tree stays dirty until user explicitly says commit. Do NOT attempt `git commit`.
6. **No prefab hand-edits.** All visual definitions live in DB rows. Bake handler is the only mutation path.
7. **Token slugs not literals.** Every new `padding_json` / `params_json` / `panel_child.params_json` value uses a published `token` slug. Literals are forbidden in new rows. The resolver (Bucket F #1 below) substitutes literals at bake time.
8. **Annotate tracker inline.** After each entity finishes, flip its row status in `docs/ui-implementation-tracker.md` from ⚪/🟡 → ✅ and append a one-line summary in that row's "parallel-agent notes" column.

## Common context

- **Tracker (authoritative work map):** `docs/ui-implementation-tracker.md` — read once at run start.
- **Design source of truth:** `ia/specs/ui-design-system.md` §1.6 (tokens) + §8 (DB panel defs) — read once.
- **Pilot playbook:** `docs/stats-panel-pilot-handoff.md` + `docs/{stats,budget}-panel-design-iteration.md` — reference for shape of DB rows.
- **DB:** Postgres `localhost:5434` `postgres`/`postgres` `territory_ia_dev`. Use `PGPASSWORD=postgres psql -h localhost -p 5434 -U postgres -d territory_ia_dev` for all DB ops.
- **Bake handler:** `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` — extend `BakeChildByKind` switch for missing renderers.
- **Bridge scripts (used only at end):** `tools/mcp-ia-server/scripts/{screenshot-loop,exit-pm,refresh-compile}.mjs`

## Execution order — single continuous pass

Run sequentially through phases. Each phase is fully landed before the next starts. NO pauses, NO checks between phases — accumulate all changes until phase 9.

### Phase 1 — Bucket F (token resolver, prerequisite for everything)

Edit `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs`:

- Add a resolver helper. When parsing `padding_json` / `params_json` / `panel_child.params_json`, if value is a string AND matches a published `catalog_entity.slug` where `kind='token' AND current_published_version_id IS NOT NULL` → look up `token_detail.value_json` → substitute typed value:
  - `token_kind='spacing'` `{value: N}` → `int N` (or `int[]` when value is array, e.g. `pad-button`)
  - `token_kind='type-scale'` `{pt, weight, family?}` → set TMP fontSize + fontStyle (+ fontAsset family when present)
  - `token_kind='color'` `{hex}` → `Color32` via hex parse
  - `token_kind='semantic'` → recurse via `semantic_target_entity_id`
- Resolver should be called at all places `padding_json.top`, `padding_json.border_width`, `padding_json.corner_radius`, `padding_json.border_color_token`, `panel_detail.gap_px`, `panel_child.params_json.label` etc. are read.
- Existing literal values continue to work (resolver short-circuits on numeric types).

### Phase 2 — Bucket C (missing bake renderers, blockers for A4/A5/B3)

Edit `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` `BakeChildByKind` switch:

- **C1 — `case "card-picker":`** — 3-col GridLayoutGroup of labeled card buttons (used by new-game-form map-small/medium/large-card).
- **C2 — `case "chip-picker":`** — HLG with toggle-chip cells (label + `color-bg-selected` token bg) (used by new-game-form budget-low/mid/high-chip).
- **C3 — `case "subtype-card":`** — IlluminatedButton variant with subtype slug binding (used by tool-subtype-picker).
- **C4 — `case "toast-card":` + `case "toast-stack":`** — toast-card: HLG icon + label + close. toast-stack: VLG container with auto-dismiss runtime hook. Used by notifications-toast.
- **C5 — extend `chart` case** — read `pj.axisLabels[]` from params_json + render TMP labels at chart edges.

### Phase 3 — Bucket A panels (apply pilot primitives via DB mutations)

For each entity, run a SQL UPDATE that mutates `panel_detail.padding_json` to use canonical token slugs (`padding-card`, `border-width-card`, `corner-radius-card`, `color-border-accent`), `gap_px` to `"gap-section"`, and reorders `panel_child` rows so close-button=order_idx 1 + themed-label modal-title=order_idx 2 (triggers header-strip HLG auto-wrap from pilot iter 12).

- **A1 — pause-menu (entity_id=222):** Apply tokens + header-strip ordering. Also patch `Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs` to drop runtime `InjectNavHeader` (now DB-driven via header-strip).
- **A2 — settings-view (entity_id=200):** Apply tokens + header-strip ordering. Verify toggle-row/slider-row/dropdown-row params unchanged.
- **A3 — save-load-view (entity_id=213):** Apply tokens + header-strip ordering.
- **A4 — new-game-form (entity_id=199):** Apply tokens + header-strip ordering. (C1+C2 from Phase 2 cover the card-picker/chip-picker children.)
- **A5 — tool-subtype-picker (entity_id=216):** Apply tokens + header-strip ordering. (C3 covers subtype-card.)

For each, publish a new `entity_version` row + flip `catalog_entity.current_published_version_id` (same pattern as session 2026-05-12 publishes).

### Phase 4 — Bucket B panels (full DB authoring for unpublished entities)

For each entity, INSERT `panel_detail` + `panel_child` rows matching the pilot baseline shape (modal-card layout, tokens, header-strip first 2 children), then publish via `entity_version` INSERT + `catalog_entity.current_published_version_id` flip.

- **B1 — info-panel (entity_id=224):** Author panel for cell-click info display. Header label + close + info content rows (bind to cell metadata). Also author a new `InfoPanelAdapter` under `Assets/Scripts/UI/Modals/` with `info.open` / `info.close` action registration.
- **B2 — map-panel (entity_id=225):** Author panel for minimap overlay. Header + close + minimap-canvas component + layer toggle buttons. Wire to existing `action.map-panel-toggle` from `Assets/Scripts/UI/HUD/MapPanelAdapter.cs`.
- **B3 — notifications-toast (entity_id=226):** Author panel with `layout_template:"top-right-toast"`. toast-stack + toast-card children. (C4 covers renderers.)

### Phase 5 — Bucket D (always-on UI type-scale passes via DB updates)

- **D1 — hud-bar (entity_id=41):** Update `hud-bar-population-readout` + `hud-bar-sim-date-readout` panel_child rows to reference `size-text-value` token. Publish new version.
- **D2 — toolbar (entity_id=100):** Update category labels to reference `size-text-body-row`. Publish.
- **D3 — main-menu (entity_id=175):** Update `mainmenu-title-label` / `mainmenu-studio-label` / `mainmenu-version-label` to reference appropriate type-scale tokens (title-display / body-row / body-row). Publish.

### Phase 6 — Bucket E (token publication gaps, only if Phase 2-5 flagged need)

If any Phase 2-5 code/DB work referenced an unpublished color token (`color-border-tan`, `color-bg-cream`, `color-bg-cream-pressed`, `color-alert-red`, `color-icon-indigo`) → publish via `entity_version` INSERT + `catalog_entity.current_published_version_id` flip. Otherwise skip Phase 6.

### Phase 7 — Tracker annotation

Edit `docs/ui-implementation-tracker.md`:

- Flip every panel/button/component/archetype/token row that was authored in Phases 1-6 from ⚪/🟡 → ✅.
- Append a one-line summary in the "parallel-agent notes" column for each flipped row stating what landed.
- Update the Bucket A/B/C/D/E sections to mark sub-tasks as completed.
- Update Bucket F resolver status to ✅.
- Append a §Whole-Game Implementation Log section listing every entity touched in this run.

### Phase 8 — Single snapshot regen + design-system.md sync

- Run `node tools/scripts/snapshot-export-game-ui.mjs` ONCE to regenerate `Assets/UI/Snapshots/panels.json` + `tokens.json` + `components.json` from current DB state.
- Append a `§8.5 New panel definitions (whole-game rollout 2026-05-12)` block to `ia/specs/ui-design-system.md` with summary rows for each new/updated panel (entity_id, slug, layout_template, token refs).

### Phase 9 — Single compile-check + single bake + single screenshot sweep (the only verification)

This is the ONLY place compile-check + bake + screenshots run. Sequence:

1. Ensure Editor is in Edit Mode:
   ```bash
   node tools/mcp-ia-server/scripts/exit-pm.mjs
   ```
2. Trigger asset refresh + compile (single pass, captures all C# edits from Phases 1-2):
   ```bash
   node tools/mcp-ia-server/scripts/refresh-compile.mjs
   ```
   If compile fails: fix the compile error inline, re-run refresh-compile. Repeat until clean. This is the ONE allowed iteration loop.
3. Single whole-game bake:
   ```bash
   npm run unity:bake-ui
   ```
   Confirm bake mutation_result reports no `unhandled_inner_kind` warnings (Phase 2 should have eliminated all).
4. Single whole-game screenshot sweep — capture every panel in Play Mode. Extend `tools/mcp-ia-server/scripts/screenshot-sweep.mjs` if needed to cover: hud-baseline, stats, budget, pause, settings, save-load, new-game, map, info, notifications-toast, tool-subtype, main-menu, toolbar. Run sweep ONCE.

### Phase 10 — Final tracker update + handoff

- Append `§Whole-Game Rebake Regression Check` to `docs/ui-implementation-tracker.md` (or new `docs/whole-game-rebake-tracker.md`) with one row per panel: screenshot path + agent-side observation note ("renders / has X gap / etc.").
- Flip remaining ⚪ rows to ✅ where bake succeeded.
- Write a final 5-line summary message ending with `result:` line stating: "whole-game UI implementation landed — N panels published, M renderers added, K tokens referenced. Tree dirty. Awaiting user accept + commit instruction."

## Frozen baseline (session 2026-05-12 end state — entry conditions for this run)

- Pilots: stats-panel (10 iters, v=8, id=220), budget-panel (10 iters, v=9, id=221) — both at visual baseline.
- Promoted primitives: `NavBackButton`, `RoundedBorder`, header-strip HLG, modal-card layout-template recognition, themed-label variant-sizing, RowGrid wrapper.
- Canonical tokens published: `padding-card` (16), `border-width-card` (6), `corner-radius-card` (24), `gap-section` (16), `color-border-accent` (#ffb020), `font-family-ui` (LiberationSans), 5 type-scale, 17 bulk-published pre-pilot tokens.
- Bake handler: variant-based fontSize for themed-label, header-strip auto-wrap, RowGrid wrapper, font min 12 floor, modal-card layout-template case.
- Tree: dirty (16 modified + 14 untracked tracker scripts/docs). No commits.

## What the agent must NOT do

- Do NOT run `npm run unity:bake-ui` more than once (at Phase 9).
- Do NOT run `npm run unity:compile-check` or `refresh-compile.mjs` more than once (at Phase 9 — except for repair iterations on compile failure).
- Do NOT run `node tools/scripts/snapshot-export-game-ui.mjs` more than once (at Phase 8).
- Do NOT capture screenshots mid-run.
- Do NOT prompt user via AskUserQuestion.
- Do NOT write "verdict pending" or "awaiting accept" prose in iteration trackers.
- Do NOT commit, push, or stage files.
- Do NOT hand-edit prefabs under `Assets/UI/Prefabs/Generated/`.
- Do NOT inline literal values in new DB rows — always use published token slugs.

## What the agent must do

- Read tracker once at start, then iterate through phases 1-10 sequentially without pausing.
- Accumulate all DB mutations + C# edits across phases 1-6.
- Land tracker annotations (Phase 7) BEFORE the single bake (Phase 9) so progress is visible even if Phase 9 fails.
- Use existing pilot DB shapes as templates — copy stats-panel / budget-panel `panel_detail.padding_json` shape for every new modal-card panel.
- Trust the resolver from Phase 1 to handle token → literal substitution at bake time.

## Result format

End the run with a single message:

```
result: whole-game UI implementation landed.
- N panels published (list)
- M renderers added (list)
- K tokens published (list)
- 1 bake + 1 compile-check executed at end
- {bake_warnings_count} bake warnings
- Tree dirty, no commits.
- Tracker fully annotated. Awaiting user accept + commit instruction.
```

No further work after that result line. Wait silently for user direction.
