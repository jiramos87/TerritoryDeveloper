# Asset tree reorg + rename — exploration seed

**Status:** seed (pre-`/design-explore`)
**Captured:** 2026-05-06
**Owner-on-pickup:** TBD
**Trigger:** post-stub-icon cleanup (commit `fe7df648`); catalog seeds in `game-ui-catalog-bake` master plan are about to scale, so canonical asset paths need to land before they get baked into 50+ rows.

## Problem statement (product-side)

Current `Assets/Sprites/*` and `Assets/Prefabs/*` trees grew organically. Folder split is inconsistent — `Sprites/Forest` is missing while `Prefabs/Forest` exists; `Sprites/Enviromental` is misspelled and mixes forests with slopes; sprite naming switches between mixed-case, kebab, and snake; `Sprites/PowerPlant` vs `Prefabs/Buildings/PowerPlant*` drift across families.

This is fine while the game is small. It becomes painful as soon as we:

1. Bake catalog rows that reference asset paths (sprite_catalog, panel_detail).
2. Onboard another art-pack drop that needs to slot into a known place.
3. Ask an agent to find "the residential heavy prefab" without scanning the tree.

We want an ordered, consistent asset library so future categorization, catalog seeding, and prefab discovery become trivial.

## Goal

Land a clean, canonical asset tree under `Assets/Sprites/*` and `Assets/Prefabs/*` with:

- Consistent family folders (residential, commercial, industrial, power, water, roads, forest, terrain, ui, fx).
- Consistent naming convention (proposal: `{family}-{tier-or-variant}-{descriptor}.{ext}`, kebab-case).
- No broken Unity references — GUIDs preserved across moves/renames via `.meta` co-movement.
- A short canonical-paths doc + a validator that flags drift.

## Non-goals

- Generating new art.
- Changing the catalog/IR/bake pipeline.
- Touching Resources/ tree shape (small, separate concern).
- Changing scene wiring (SerializeField references survive on GUID).

## Known shape (snapshot 2026-05-06)

Sprite folders: `Buttons, Cliff, Commercial, Effects, Enviromental[sic], Generated, Grass, Icons, Industrial, Placeholders, PowerPlant, Residential, Roads, Slopes, State, Water, WaterPlant`.

Prefab folders: `Audio, Buildings, Buttons, Cliff, Commercial, Effects, Forest, Grass, Industrial, Residential, Road, Slopes, Water`.

Drift hotspots:

- `Sprites/Enviromental` (typo) holds forest + slope sprites.
- `Sprites/Forest` missing; `Prefabs/Forest` exists.
- `Sprites/PowerPlant` (singular) vs `Prefabs/Buildings/...` (under Buildings umbrella).
- `Sprites/Roads` plural; `Prefabs/Road` singular.
- Naming styles mixed: `NW-slope-forest.png`, `Forest-sprites.png`, `Forest1-64.png`, `picker-residential-light-icon-72.png` (now removed).

Total file count in scope: ~423 `.png`/`.prefab` pairs (each with a `.meta`).

## Constraints

- Unity tracks references by GUID stored in `.meta`. Moving/renaming preserves refs IF and ONLY IF the `.meta` moves alongside its asset and keeps its `guid:` field intact. Filesystem `git mv` of both files together = safe. `mv` of asset alone = breaks refs.
- Code paths via `Resources.Load<T>("...")` are string-based and DO break on path changes. Audit reveals current Resources/ tree is small (UI, Economy, Construction); sprites/prefabs in scope live OUTSIDE Resources, so all refs are GUID-based.
- Catalog DB rows (`sprite_catalog`, `catalog_entity`) may store asset paths. Needs re-bake after move; one shot, automatic.
- Some prefabs are referenced in scene `.unity` files via fileID + guid pair → safe across moves, breaks only if `.meta` is regenerated (don't delete `.meta`!).

## Approach options (to be expanded by `/design-explore`)

1. **Big-bang reorg.** One stage, one PR, ~423 file moves + renames, single re-bake, single validator gate. Atomic; bisectable; risky if any GUID is lost.
2. **Family-by-family rolling reorg.** One family per stage (residential, commercial, industrial, power, water, roads, forest, terrain). Smaller diffs; longer overall calendar; multiple intermediate states where naming is half-old half-new.
3. **Rename-only first, structure-later.** Pass 1 = canonical names in place. Pass 2 = move into canonical folders. Reduces concurrent risk per pass but doubles total churn.
4. **Hybrid: structure first, names later.** Pass 1 = move into canonical family folders, keep file names. Pass 2 = rename. Lets baking pick up new folders early; rename pass is purely cosmetic.

## Open decisions (poll user during `/design-explore`)

1. Naming convention: `{family}-{tier}-{descriptor}` vs `{family}_{tier}_{descriptor}` vs PascalCase.
2. Family taxonomy: keep `Buildings` umbrella (Prefabs side) or flatten to `residential/commercial/industrial/power/water` siblings on both sides?
3. `Sprites/Generated/` policy — keep separate generated bucket or merge into family tree?
4. `Placeholders/` — promote into a `placeholder/{family}/` mirror or retire entirely once real art lands?
5. Big-bang vs rolling vs hybrid (Approach #1–4).
6. Validator strictness: hard gate in `validate:all` or warn-only?

## Likely subsystems impacted

- Unity scene/prefab references (GUID-safe → likely no impact).
- Catalog bake script(s) — need re-run after reorg lands.
- `sprite_catalog` DB rows — `path_hash` regenerated on re-bake.
- IR JSON authoring — only if any IR file hard-codes a path stem (most read by GUID/slug).
- Validators — new `validate:asset-tree-canonical.mjs` proposed (path/name regex per family).
- Docs: `docs/asset-pipeline-standard.md`, `ia/specs/architecture/data-flows.md` may need a §Canonical asset paths section.

## Implementation points (pre-design)

- Build a manifest CSV: `current_path,target_path,current_name,target_name,family,reason` for every asset in scope.
- Script the moves: one-shot bash script that does `git mv old new && git mv old.meta new.meta` per row.
- Run `npm run validate:all` + `npm run unity:compile-check` after batch.
- Re-bake catalog: trigger AssetPostprocessor + `validate:sprite-catalog-coverage`.
- Add `validate:asset-tree-canonical` regex gate over the new layout.
- Update `docs/asset-pipeline-standard.md` with the canonical path/name spec.

## Risks

- Lost `.meta` → broken scene references. Mitigation: scripted move (asset + meta atomic), pre-flight scan that any `.png` without sibling `.meta` aborts the run.
- Validator over-fits current shape, blocks future families. Mitigation: regex per family is opt-in, unknown families pass-through with WARN.
- Catalog stale row count drift. Mitigation: re-bake is idempotent; one CI run resolves.

## Examples (for `/design-explore` to expand)

- Renames: `Sprites/Enviromental/Forest1-64.png` → `Sprites/Forest/forest-tier-1-64.png`.
- Moves: `Sprites/PowerPlant/coal-stack.png` → `Sprites/Power/coal-stack.png`.
- Folder rename: `Sprites/Enviromental/` → `Sprites/Terrain/` (or split → `Sprites/Forest/` + `Sprites/Slopes/`).

## Handoff prompt — paste into a fresh main session

> /design-explore docs/explorations/asset-tree-reorg-and-rename.md
>
> Take this exploration seed end-to-end. Phases: compare the four approaches under "Approach options", select one (poll me on the Open decisions list — do not pick silently), expand into a detailed plan with concrete examples, scope subsystem impact, list implementation points, and run the subagent review. Persist the final design back to this same file. Do NOT seed a master plan or BACKLOG issue yet — stop after the design is reviewed and persisted, and hand me the next-action prompt for `/master-plan-new` or `/project-new` so I can decide which surface to scale into. Pause after the design is ready.

## Closeout (post-design-explore)

Replace this section with the master-plan slug or BACKLOG id once the design has graduated past `/design-explore`.
