# Data flows

## Initialization

`GeographyManager` startup:
1. Regional map + neighboring cities
2. Optional **interchange** load of `geography_init_params` from StreamingAssets (session **MapGenerationSeed** + optional procedural-rivers override); grid + heightmap (40×40 designer template centered; procedural fill on larger maps)
3. Water map + lake bodies (depression-fill or legacy sea-level mask)
4. Interstate highways (up to 3 random attempts + deterministic fallback)
5. Forests (conditional)
6. Water desirability, sorting order recalc, border signs
7. Zone manager ready

## Simulation (per tick)

SimulationManager order:
1. Growth budget validation
2. Urban centroid / ring recalc
3. Auto road extension
4. Auto zoning (cells adjacent to roads)
5. Auto resource planning (water, power)

Legacy UrbanizationProposal obsolete; not invoked.

## Player Input

GridManager dispatches clicks by active mode → zoning, road drawing, building placement, bulldozer.

## Persistence

- **Save:** Grid data (`List<CellData>`) + `WaterMapData` on `GameSaveData`.
- **Load:** Restore heightmap → water map (or legacy path) → grid → sync water body ids w/ shore membership. Snapshot applies saved prefabs, sorting order, water body type/id. Does NOT run global slope restoration / sorting recalc (geography spec §7.4).

## Master-plan health rollup (DB MV)

`ia_master_plan_health` materialized view — rollup metrics per master-plan slug. Powers `master_plan_health` MCP tool (TECH-3227) + `master_plan_cross_impact_scan` MCP tool (TECH-3229). Migration `0045_ia_master_plan_health_mv.sql` (db-lifecycle-extensions Stage 2 / TECH-3226).

**Schema:**

| Column | Type | Source |
|--------|------|--------|
| `slug` | text PK | `ia_master_plans.slug` |
| `n_stages` | int | `COUNT(ia_stages)` per slug |
| `n_done` | int | `COUNT(ia_stages WHERE status='done')` |
| `n_in_progress` | int | `COUNT(ia_stages WHERE status='in_progress')` |
| `n_pending` | int | `COUNT(ia_stages WHERE status='pending')` |
| `oldest_in_progress_age_days` | int | days since oldest in-progress stage `updated_at` |
| `missing_arch_surfaces` | text[] | stage_ids with zero `stage_arch_surfaces` rows |
| `drift_events_open` | int | `COUNT(arch_changelog WHERE commit_sha IS NULL AND kind IN ('edit','spec_edit_commit'))` joined via `stage_arch_surfaces` |
| `sibling_collisions` | text[] | other slugs sharing surface_slug whose stages are `in_progress` |
| `refreshed_at` | timestamptz | last refresh ts |

**Refresh contract (sync):** `fn_ia_master_plan_health_refresh` runs `REFRESH MATERIALIZED VIEW CONCURRENTLY` post-statement on `ia_stages`, `ia_tasks`, `stage_arch_surfaces` DML. UNIQUE INDEX on `slug` is the precondition. Smoke gate (`validate:master-plan-health-rollup`) asserts p50 refresh latency <150ms on N=10 successive refreshes.

**Async escape hatch:** staleness-tolerant consumers may switch to `LISTEN ia_master_plan_health_dirty` + debounced background refresh (not yet wired — escape hatch documented for future regression if sync triggers regress beyond budget). Conversion path: drop the 3 statement-level triggers, add `NOTIFY ia_master_plan_health_dirty` from a lighter trigger, run a daemon that issues CONCURRENTLY refresh on debounce. Staleness window: <1s typical with sub-second debounce.

## Interchange JSON (config and tooling, TECH-41)

Data is split into three layers: **runtime** (`MonoBehaviour` managers and live `Cell` on the grid), **interchange** (JSON DTOs with string `artifact` and optional `schema_version` — validated by JSON Schema under `docs/schemas/` and Zod in `tools/mcp-ia-server`), and **persistence** (`CellData` / `GameSaveData` / `WaterMapData` on the save/load path only). Geography initialization may load `geography_init_params` once per pipeline from `StreamingAssets` (`GeographyInitParamsLoader`, `GeographyManager`). Editor exports for diagnostics live under `tools/reports/` (see `unity-development-context.md` §10).

For full JSON schemas, MCP server tool catalog, Postgres bridge contracts (B1/B3/P5), and local verification commands, see [`interchange.md`](interchange.md).

## UI / UX design system

Cross-cutting effort: reference spec [`ia/specs/ui-design-system.md`](../ui-design-system.md) (**as-built** baseline + committed [`docs/reports/ui-inventory-as-built-baseline.json`](../../../docs/reports/ui-inventory-as-built-baseline.json) + **Codebase inventory (uGUI)**). **UI-as-code program** umbrella **§ Completed** — trace [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) **Recent archive**. **Glossary:** **UI design system (reference spec)**, **UI-as-code program**.

**UI Toolkit overlay flow (current UI baseline / DEC-A28 strangler).** The current shipping in-game UI runs on UI Toolkit alongside the legacy uGUI prefab pipeline. Per-panel `*Host` MonoBehaviours (e.g. `HudBarHost`, `BudgetPanelHost`, `MapPanelHost`, `StatsPanelHost`, `PauseMenuHost`, `MainMenuHost`, `NotificationsToastHost`, `HoverInfoHost`, `InfoPanelHost`) live in `Assets/Scripts/UI/Hosts/` and own a `UIDocument` reference whose `sourceAsset` is a `*.uxml` file under `Assets/UI/Generated/`. On `OnEnable` each Host Q-lookups its `VisualElement` children by name (`root.Q<Button>("hud-pause")`), wires `Button.clicked` / `Slider.RegisterValueChangedCallback` / `Toggle.RegisterValueChangedCallback` to gameplay managers via `FindObjectOfType` fallbacks (EconomyManager / GrowthBudgetManager / MiniMapController / CityStats / DemandManager), and registers the panel root with `ModalCoordinator.RegisterMigratedPanel(slug, root)` so HUD button clicks (`HudBarHost.OnBudget`, `OnMap`, `OnStats`) toggle modal display via `_modalCoordinator.Show(slug)` / `HideMigrated(slug)`. Two surfaces are runtime-VE only — `HoverInfoHost` and `MapPanelHost.BuildRuntimePanel` (with `MiniMapController-Runtime` spawner) — both construct their VisualElement tree programmatically in C# and attach to the proven full-viewport `notifications-toast` UIDocument root (pickingMode=Ignore so the overlay never blocks world clicks). Visual contract: pixel goldens under `tools/visual-baseline/golden/`. Path B reverse-capture explored at [`docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md`](../../../docs/explorations/ui-toolkit-emitter-parity-and-db-reverse-capture.md).

## Water

`WaterMap` stores per-cell body ids; `WaterBody` holds surface height. Procedural lakes (depression-fill), procedural rivers (after lakes, before interstate), shore/cliff/cascade visuals. **`TerrainManager`** **`PlaceCliffWalls`** seals **south**/**east** **map border** voids with brown **cliff** stacks to **`MIN_HEIGHT`**, and skips duplicate brown faces toward void when the cell uses **water-shore** primary art. See geography spec §5.7, §11–§12.

## Canonical asset paths

Where every new sprite, prefab, or audio asset lives in the Unity project. Governs file placement + naming across all 10 asset families. Validated by `validate:asset-tree-canonical` (integrated in `validate:all`).

### Tree skeleton

```
Assets/
 Art/
 Sprites/
 UI/
 Icons/ ← family: ui-icon
 Terrain/ ← family: terrain-sprite
 Roads/ ← family: road-sprite
 Buildings/ ← family: building-sprite
 Water/ ← family: water-sprite
 VFX/ ← family: vfx-particle
 Audio/
 SFX/ ← family: audio-sfx
 Music/ ← family: audio-music
 UI/
 Prefabs/
 Generated/ ← family: ui-panel
 Buttons/ ← family: ui-button
```

All file stems: `^[a-z0-9]+(-[a-z0-9]+)*$` (kebab-case). Enforced by `validate:asset-tree-canonical`. Full family table + naming regex: [`asset-pipeline-standard.md §Canonical asset paths`](asset-pipeline-standard.md).

## Isometric geography (canonical spec)

[`ia/specs/isometric-geography-system.md`](../isometric-geography-system.md) — single source of truth for grid math, heights, slopes, water/shore/cliffs, sorting, terraform, roads, pathfinding. When another doc disagrees, update the spec or code.
