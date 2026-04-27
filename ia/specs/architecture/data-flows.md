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

## Interchange JSON (config and tooling, TECH-41)

Data is split into three layers: **runtime** (`MonoBehaviour` managers and live `Cell` on the grid), **interchange** (JSON DTOs with string `artifact` and optional `schema_version` — validated by JSON Schema under `docs/schemas/` and Zod in `tools/mcp-ia-server`), and **persistence** (`CellData` / `GameSaveData` / `WaterMapData` on the save/load path only). Geography initialization may load `geography_init_params` once per pipeline from `StreamingAssets` (`GeographyInitParamsLoader`, `GeographyManager`). Editor exports for diagnostics live under `tools/reports/` (see `unity-development-context.md` §10).

For full JSON schemas, MCP server tool catalog, Postgres bridge contracts (B1/B3/P5), and local verification commands, see [`interchange.md`](interchange.md).

## UI / UX design system

Cross-cutting effort: reference spec [`ia/specs/ui-design-system.md`](../ui-design-system.md) (**as-built** baseline + committed [`docs/reports/ui-inventory-as-built-baseline.json`](../../../docs/reports/ui-inventory-as-built-baseline.json) + **Codebase inventory (uGUI)**). **UI-as-code program** umbrella **§ Completed** — trace [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) **Recent archive**. **Glossary:** **UI design system (reference spec)**, **UI-as-code program**.

## Water

`WaterMap` stores per-cell body ids; `WaterBody` holds surface height. Procedural lakes (depression-fill), procedural rivers (after lakes, before interstate), shore/cliff/cascade visuals. **`TerrainManager`** **`PlaceCliffWalls`** seals **south**/**east** **map border** voids with brown **cliff** stacks to **`MIN_HEIGHT`**, and skips duplicate brown faces toward void when the cell uses **water-shore** primary art. See geography spec §5.7, §11–§12.

## Isometric geography (canonical spec)

[`ia/specs/isometric-geography-system.md`](../isometric-geography-system.md) — single source of truth for grid math, heights, slopes, water/shore/cliffs, sorting, terraform, roads, pathfinding. When another doc disagrees, update the spec or code.
