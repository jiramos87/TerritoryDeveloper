# Exploration — hud-bar panel catalog registration + orphan-button validator

**Date:** 2026-05-06
**Parent plan:** `game-ui-catalog-bake` (extend with Stage 9.12)

## Trigger

After `/ship-cycle game-ui-catalog-bake 9.10` (DB-driven hud-bar layout) + `9.11` (slug rename), Play Mode HUD bar still rendered scene-baked GameObjects, not catalog-driven. Diagnosis:

- `hud-bar-budget-button` (kind=button) registered in catalog.
- **No `hud-bar` panel entity exists** (kind=panel) — only `growth-budget-panel`, `subtype-picker`, `demo-panel`.
- Migration `0089_seed_hud_bar_child_layout_zones.sql` `WHERE slug='hud_bar'` matched 0 rows. Silent no-op.
- Orphan button shipped + survived 9.10/9.11 verify gates.

False-pass root cause: snapshot exporter dumps only `kind=panel` rows. Missing parent panel → 0 hud-bar items in `panels.json` → bake hook sees nothing to bake → "green" pass.

## Stage 9.12 scope

### Task A — hud-bar panel registration + scene retire

- Insert `catalog_entity (kind='panel', slug='hud-bar')` row.
- Insert 10 `panel_child` rows mirroring scene-baked hud-bar children (3× left zone, 3× center, 4× right) with `layout_json.zone` populated.
- Re-export `panels.json` snapshot (schema_version 3) — hud-bar appears in items.
- Run unity-bake-ui → bakes hud-bar prefab from catalog.
- Retire scene-baked `hud-bar` GameObject from MainScene; replace with prefab-instance reference.
- Visible delta: HUD bar visually identical pre/post; click `hud-bar-budget-button` (graph icon) → opens growth-budget-panel via DB-driven wiring (no scene dep).

### Task B — `validate:catalog-panel-coverage` orphan-button detector

- New `tools/scripts/validate-catalog-panel-coverage.mjs`.
- Query: every `catalog_entity WHERE kind='button'` must have ≥1 `panel_child` row referencing it (via `params_json` button_ref or asset link table).
- Hard-fail (exit 1) on offenders. List orphan slugs in error output.
- Wire into `package.json` `validate:all` chain.
- Unit test: seed orphan button → expect non-zero exit; seed parented button → expect zero exit.

## Why catch-up Stage, not Stage 7 amendment

Stage 7 = MVP closeout (bake determinism + vibe-loop smoke). 9.12 = bug-fix catch-up for 9.10 false-pass. Different concern; mixing dilutes Stage 7 exit criteria. Numbering 9.12 keeps it adjacent to 9.10/9.11 chronologically.

## §Visibility Delta

- Pre-9.12: HUD bar = scene-baked GameObject. Catalog edits to hud-bar buttons have zero visual effect.
- Post-9.12: HUD bar = DB-driven prefab instance. Edit `hud-bar-budget-button.display_name` in catalog → re-bake → HUD bar reflects.

## §Red-Stage Proof

Task A — `Assets/Tests/EditMode/UI/HudBarCatalogBakedTest.cs::HudBar_ScenePrefab_RootSlugMatchesCatalog` — assert MainScene `hud-bar` GameObject root has component referencing catalog slug `hud-bar`, not legacy hardcoded layout.

Task B — `tools/scripts/__tests__/validate-catalog-panel-coverage.test.mjs::OrphanButton_ExitsNonZero` — seed orphan, run validator, expect exit 1.

## Surfaces

- `db/migrations/` — next id (likely 0093)
- `Assets/Editor/Bake/CatalogBakeHandler.cs`
- `Assets/Editor/Bridge/UiBakeHandler.Archetype.cs`
- `Assets/UI/Snapshots/panels.json`
- `tools/scripts/snapshot-export-game-ui.mjs`
- `tools/scripts/validate-catalog-naming.mjs` (sibling shape reference)
- `package.json` `validate:all`

## Out of scope

- New panel kinds beyond hud-bar.
- HUD bar interactivity changes (click handlers stay as-is, just plumbing swap).
- Stage 7 work (bake determinism + vibe-loop smoke remain in flight).
