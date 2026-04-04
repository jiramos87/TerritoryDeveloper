# Territory Developer — Architecture

## Overview

Territory Developer is a 2D isometric city-builder built in Unity with C#. Players place roads, zones (residential/commercial/industrial), buildings, and manage their city's economy, resources, and growth.

All game logic lives in MonoBehaviour classes under `Assets/Scripts/`. No dependency injection — managers reference each other via Inspector fields with `FindObjectOfType<T>()` fallback in Awake/Start.

## System Layers

```
┌─────────────────────────────────────────────────────────┐
│  UI Layer                                               │
│  UIManager, CursorManager, GameNotificationManager,     │
│  all Controllers (buttons, popups, sliders)             │
├─────────────────────────────────────────────────────────┤
│  Simulation Layer                                       │
│  SimulationManager, AutoRoadBuilder, AutoZoningManager, │
│  AutoResourcePlanner, GrowthManager, GrowthBudgetMgr,  │
│  UrbanCentroidService (AUTO roads/zoning rings)         │
├─────────────────────────────────────────────────────────┤
│  Gameplay Layer                                         │
│  ZoneManager, RoadManager, InterstateManager,           │
│  DemandManager, EconomyManager                          │
├─────────────────────────────────────────────────────────┤
│  Stats Layer                                            │
│  CityStats, EmploymentManager, StatisticsManager        │
├─────────────────────────────────────────────────────────┤
│  Terrain Layer                                          │
│  TerrainManager, WaterManager, HeightMap, WaterMap      │
├─────────────────────────────────────────────────────────┤
│  Forests Layer                                          │
│  ForestManager, ForestMap, Forest, IForest              │
├─────────────────────────────────────────────────────────┤
│  Core Layer                                             │
│  GridManager (hub), Cell, CellData                      │
├─────────────────────────────────────────────────────────┤
│  Persistence Layer                                      │
│  GameSaveManager, GameManager                           │
└─────────────────────────────────────────────────────────┘
```

## Helper Services

| Service | Role |
|---------|------|
| GridPathfinder | A* pathfinding for road routes |
| GridSortingOrderService | Sorting order computation |
| BuildingPlacementService | Building placement and load/restore |
| TerraformingService, PathTerraformPlan | Terraform plan computation, apply/revert, cut-through |
| RoadPrefabResolver | Prefab selection for path and single-cell contexts |
| RoadPathCostConstants | Shared cost constants for road pathfinding |
| RoadStrokeTerrainRules | Land slope allowlist and stroke truncation (flat + cardinal ramps only for road cells) |
| UrbanCentroidService | Urban centroid and ring calculation |
| GameBootstrap | Entry point, game loading flow |

## Data Flows

### Initialization

`GeographyManager` orchestrates startup:
1. Regional map with neighboring cities
2. Optional **interchange** load of `geography_init_params` from StreamingAssets (session **MapGenerationSeed** + optional procedural-rivers override); then grid + heightmap (40×40 designer template centered; procedural fill on larger maps)
3. Water map + lake bodies (depression-fill or legacy sea-level mask)
4. Interstate highways (up to 3 random attempts + deterministic fallback)
5. Forests (conditional)
6. Water desirability, sorting order recalculation, border signs
7. Zone manager ready for player zoning

### Simulation (each tick)

SimulationManager executes in order:
1. Growth budget validation
2. Urban centroid / ring recalculation
3. Auto road extension
4. Auto zoning (cells adjacent to roads)
5. Auto resource planning (water, power)

The legacy UrbanizationProposal system is obsolete and not invoked.

### Player Input

GridManager dispatches clicks by active mode → zoning, road drawing, building placement, or bulldozer.

### Persistence

- **Save:** Grid data (`List<CellData>`) + `WaterMapData` serialized on `GameSaveData`.
- **Load:** Restore heightmap → restore water map (or legacy path) → restore grid → sync water body ids with shore membership. Snapshot applies saved prefabs, sorting order, water body type/id. Does **not** run global slope restoration or sorting recalculation (see geography spec §7.4).

### Interchange JSON (config and tooling, TECH-41)

Data is split into three layers: **runtime** (`MonoBehaviour` managers and live `Cell` on the grid), **interchange** (JSON DTOs with string `artifact` and optional `schema_version` — validated by JSON Schema under `docs/schemas/` and Zod in `tools/mcp-ia-server`), and **persistence** (`CellData` / `GameSaveData` / `WaterMapData` on the save/load path only). Geography initialization may load `geography_init_params` once per pipeline from `StreamingAssets` (`GeographyInitParamsLoader`, `GeographyManager`). Editor exports for diagnostics live under `tools/reports/` (see `unity-development-context.md` §10).

### UI / UX design system

Cross-cutting effort: charter [`docs/ui-design-system-project.md`](docs/ui-design-system-project.md), discovery [`docs/ui-design-system-context.md`](docs/ui-design-system-context.md), spec [`.cursor/specs/ui-design-system.md`](.cursor/specs/ui-design-system.md). Work tracked in `BACKLOG.md`.

### Water

`WaterMap` stores per-cell body ids; `WaterBody` holds surface height. Procedural lakes (depression-fill), procedural rivers (after lakes, before interstate), shore/cliff/cascade visuals. See geography spec §11–§12.

### Isometric geography (canonical spec)

[`.cursor/specs/isometric-geography-system.md`](.cursor/specs/isometric-geography-system.md) — single source of truth for grid math, heights, slopes, water/shore/cliffs, sorting, terraform, roads, pathfinding. When another doc disagrees, update the spec or code.

## Agent information architecture and MCP

Authoritative **agent-facing** content lives in `.cursor/specs/`, `.cursor/rules/`, [`AGENTS.md`](AGENTS.md), and this file. [`.cursor/rules/agent-router.mdc`](.cursor/rules/agent-router.mdc) maps tasks to specs.

The **territory-ia** MCP server ([`tools/mcp-ia-server/`](tools/mcp-ia-server/), configured in [`.cursor/mcp.json`](.cursor/mcp.json)) exposes that corpus through tools (`backlog_issue` for [`BACKLOG.md`](BACKLOG.md) by issue id, plus `list_specs`, `spec_outline`, `spec_section`, `spec_sections`, `project_spec_closeout_digest`, `glossary_lookup`, `glossary_discover`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`, `isometric_world_to_grid`) so agents can fetch slices without reading whole files. **Computational** math for **`isometric_world_to_grid`** lives in [`tools/compute-lib/`](tools/compute-lib/) (**npm** **`territory-compute-lib`**, **glossary** **territory-compute-lib (TECH-37)**); gameplay **grid** authority remains **C#**. It does not change Unity runtime architecture. Overview: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md). A **domain-agnostic** description of the same file-backed IA + MCP pattern (reusable in other repos) is in [`docs/mcp-markdown-ia-pattern.md`](docs/mcp-markdown-ia-pattern.md). **Integrated tooling and verification task order** (scripts, CI, MCP, Unity exports): [`docs/agent-tooling-verification-priority-tasks.md`](docs/agent-tooling-verification-priority-tasks.md).

**JSON interchange program (completed):** **TECH-21** **§ Completed** — **glossary** **JSON program (TECH-21)**; phased delivery across **TECH-40** (JSON Schema, **CI** validation, optional **spec**/**glossary** index manifests), **TECH-41** (runtime/Editor **Geography initialization** and tooling payloads), and **TECH-44a** ([`BACKLOG.md`](BACKLOG.md) **§ Completed**): patterns in [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (row+**JSONB**, **B3** patch envelope, **P5** streaming). **Postgres:** **TECH-44** program (completed — [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; extension map [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **Program extension mapping (E1–E3)**) — **TECH-44b** (completed — same section): **`db/migrations/`** (**IA** tables), [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md), **`tools/postgres-ia/`**; **TECH-44c** (completed — same section — **E1** **`dev_repro_bundle`**: migration **`0003_dev_repro_bundle.sql`**, **`register-dev-repro.mjs`**, docs in setup guide; glossary **Dev repro bundle**). **Editor export registry** (**TECH-55** + **TECH-55b** **§ Completed** — [`BACKLOG.md`](BACKLOG.md); glossary): **`editor_export_*`** (**`0004`**, **`0005`** migrations), **`register-editor-export.mjs`** (**`--document-file`**, **`document jsonb`**), **`EditorPostgresExportRegistrar`** **DB-first** + **`tools/reports/`** fallback ([`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md)). Exploration and versioning FAQ: [`projects/TECH-21-json-use-cases-brainstorm.md`](projects/TECH-21-json-use-cases-brainstorm.md). Generated indexes are **supplementary** to Markdown and MCP until explicitly wired; they do not replace **`list_specs`** / **`spec_section`** as authoritative sources (**TECH-18**).

## Full Dependency Map

| Manager | Dependencies |
|---------|-------------|
| GridManager | ZoneManager, UIManager, CityStats, CursorManager, TerrainManager, DemandManager, WaterManager, GameNotificationManager, ForestManager, CameraController, RoadManager, InterstateManager, BuildingSelectorMenuController |
| ZoneManager | GridManager, RoadManager, CityStats, UIManager, GameNotificationManager, DemandManager, WaterManager, InterstateManager |
| RoadManager | TerrainManager, GridManager, CityStats, UIManager, ZoneManager, InterstateManager |
| TerrainManager | GridManager, ZoneManager, WaterManager |
| WaterManager | GridManager, TerrainManager, ZoneManager |
| ForestManager | GridManager, WaterManager, CityStats, EconomyManager, UIManager, GameNotificationManager, TerrainManager |
| GeographyManager | TerrainManager, WaterManager, ForestManager, GridManager, ZoneManager, InterstateManager, RegionalMapManager |
| TimeManager | UIManager, SpeedButtonsController, CityStats, EconomyManager, GridManager, AnimatorManager, ZoneManager, InterstateManager, SimulationManager |
| SimulationManager | CityStats, GrowthBudgetManager, UrbanCentroidService, AutoRoadBuilder, AutoZoningManager, AutoResourcePlanner |
| EconomyManager | CityStats, TimeManager, GameNotificationManager |
| DemandManager | EmploymentManager, CityStats, ForestManager, GridManager |
| InterstateManager | GridManager, TerrainManager, RoadManager |
| CityStats | TimeManager, WaterManager, ForestManager |
| AutoRoadBuilder | GridManager, RoadManager, GrowthBudgetManager, CityStats, InterstateManager, TerrainManager |
| AutoZoningManager | GridManager, ZoneManager, GrowthBudgetManager, CityStats, DemandManager |
| AutoResourcePlanner | CityStats, GridManager, GrowthBudgetManager, UIManager |
| GrowthManager | GridManager, DemandManager |
| GrowthBudgetManager | CityStats |
| EmploymentManager | CityStats, DemandManager |
| StatisticsManager | EmploymentManager, DemandManager, EconomyManager, CityStats |
| UIManager | ZoneManager, CursorManager, GridManager, TimeManager, EconomyManager, GameManager, TerrainManager, CityStats, various Controllers |
| GameSaveManager | GridManager, CityStats, TimeManager, InterstateManager, MiniMapController |
| GameManager | GridManager, GameSaveManager |
| RegionalMapManager | InterstateManager, CityStats, GridManager |
| CursorManager | GridManager |

## Road and interstate routing

Manual streets use longest-valid-prefix terraform validation; interstate uses full-path with `forbidCutThrough`. See geography spec §10, §13.

## Architectural Decisions

- **GridManager as hub:** Central coordinator for cell operations. Keeps access consistent but makes it large.
- **FindObjectOfType pattern:** Inspector wiring + null-check fallback in Awake/Start.
- **Namespaces:** Most scripts under `Territory.*` (`Core`, `Terrain`, `Roads`, `Zones`, `Forests`, `Buildings`, `Economy`, `UI`, `Geography`, `Timing`, `Utilities`, `Simulation`, `Persistence`). A few legacy scripts in global namespace.
- **Spec policy:** See `AGENTS.md`. Full spec inventory in `.cursor/specs/`; agent routing in `.cursor/rules/agent-router.mdc`. Optional MCP access to the same files: `docs/mcp-ia-server.md`; generic pattern notes: `docs/mcp-markdown-ia-pattern.md`.
- **Editor agent diagnostics (IA for agents):** `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs` emits JSON/Markdown under `tools/reports/` (gitignored outputs). Expected menus, prerequisites, and field vocabulary are documented in `.cursor/specs/unity-development-context.md` §10; **BUG-53** tracks gaps if the **Unity Editor** does not match that contract.

## Known Trade-offs

- **High coupling:** Managers reference each other directly.
- **GridManager size:** ~2070 lines; decomposition tracked in `BACKLOG.md`.
- **No event system:** Direct method calls, not events.
