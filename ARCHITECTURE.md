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
| **Compute** utilities (`Utilities/Compute/`) | **Pure** **C#** **World ↔ Grid**, **growth ring**, **grid distance**, **`PathfindingCostKernel`**, **WaterAdjacency**, **DesirabilityFieldSampler**, etc. **Edit Mode** **UTF** parity-checks **`tools/compute-lib`**. **Program** charter: **glossary** **Compute-lib program** ([`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)); shipped **Node** + **MCP** surfaces archived there; ongoing **C#** / **tooling** rows — [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program**. Further **path preview**, **`batchmode`**, **Play** **Mode** parity: open [`BACKLOG.md`](BACKLOG.md) same section. |

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

Cross-cutting effort: reference spec [`ia/specs/ui-design-system.md`](ia/specs/ui-design-system.md) (**as-built** baseline + committed [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json) + **Codebase inventory (uGUI)**). **UI-as-code program** umbrella **§ Completed** — trace [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**. **Glossary:** **UI design system (reference spec)**, **UI-as-code program**.

### Water

`WaterMap` stores per-cell body ids; `WaterBody` holds surface height. Procedural lakes (depression-fill), procedural rivers (after lakes, before interstate), shore/cliff/cascade visuals. **`TerrainManager`** **`PlaceCliffWalls`** seals **south**/**east** **map border** voids with brown **cliff** stacks to **`MIN_HEIGHT`**, and skips duplicate brown faces toward void when the cell uses **water-shore** primary art. See geography spec §5.7, §11–§12.

### Isometric geography (canonical spec)

[`ia/specs/isometric-geography-system.md`](ia/specs/isometric-geography-system.md) — single source of truth for grid math, heights, slopes, water/shore/cliffs, sorting, terraform, roads, pathfinding. When another doc disagrees, update the spec or code.

## Agent information architecture and MCP

Authoritative **agent-facing** content lives in `ia/specs/`, `ia/rules/`, [`AGENTS.md`](AGENTS.md), and this file. [`ia/rules/agent-router.md`](ia/rules/agent-router.md) maps tasks to specs. For a holistic overview of the IA system — philosophy, layers, knowledge lifecycle, extension checklists, and **autoreference** of the stack — see [`docs/information-architecture-overview.md`](docs/information-architecture-overview.md). **Agent-led verification** (Unity batch + IDE bridge, **Verification** block in agent messages): [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md).

The **territory-ia** MCP server ([`tools/mcp-ia-server/`](tools/mcp-ia-server/), configured in [`.mcp.json`](.mcp.json)) exposes that corpus through tools (`backlog_issue` for [`BACKLOG.md`](BACKLOG.md) by issue id, plus `list_specs`, `spec_outline`, `spec_section`, `spec_sections`, `project_spec_closeout_digest`, `project_spec_journal_persist` / `project_spec_journal_search` / `project_spec_journal_get` / `project_spec_journal_update` when a dev DB URL resolves — **glossary** **IA project spec journal**, `glossary_lookup`, `glossary_discover`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`, `isometric_world_to_grid`) so agents can fetch slices without reading whole files. **Computational** math for **`isometric_world_to_grid`** lives in [`tools/compute-lib/`](tools/compute-lib/) (**npm** **`territory-compute-lib`**, **glossary** **territory-compute-lib**); gameplay **grid** authority remains **C#**. It does not change Unity runtime architecture. Overview: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md). A **domain-agnostic** description of the same file-backed IA + MCP pattern (reusable in other repos) is in [`docs/mcp-markdown-ia-pattern.md`](docs/mcp-markdown-ia-pattern.md). **Integrated tooling and verification task order** (scripts, CI, MCP, Unity exports): [`docs/agent-tooling-verification-priority-tasks.md`](docs/agent-tooling-verification-priority-tasks.md).

**JSON interchange program (completed):** **glossary** **JSON interchange program** — JSON Schema + **CI** **`validate:fixtures`**, **IA index manifest**, **Geography initialization** / Editor tooling payloads, **Postgres interchange patterns** (**B1**/**B3**/**P5**) in [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (**Program extension mapping (E1–E3)**). **Postgres** dev surfaces: **`db/migrations/`**, [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md), **`tools/postgres-ia/`**, [`config/postgres-dev.json`](config/postgres-dev.json) (optional committed local default URI; **CI** skips file fallback), **glossary** **Dev repro bundle**, **Editor export registry**, **IA project spec journal**. **Charter trace:** [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). Exploration / versioning FAQ: [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md). Generated indexes are **supplementary** to Markdown and MCP until explicitly wired; they do not replace **`list_specs`** / **`spec_section`** as authoritative sources.

### Local verification (post-implementation)

| Command | Role |
|---------|------|
| **`npm run verify:local`** | **Canonical** dev-machine chain: **`validate:all`** (dead project-spec paths, **`npm run compute-lib:build`**, **`test:ia`**, **`validate:fixtures`**, **`generate:ia-indexes --check`**) then [`tools/scripts/post-implementation-verify.sh`](tools/scripts/post-implementation-verify.sh) with **`--skip-node-checks`** (**`unity:compile-check`**, **`db:migrate`**, **`db:bridge-preflight`**, **macOS** Editor save/quit + relaunch + **`db:bridge-playmode-smoke`**; **non-macOS** stops after **`db:bridge-preflight`** — see script). Implemented by [`tools/scripts/verify-local.sh`](tools/scripts/verify-local.sh). Optional seed: **`npm run verify:local -- "x,y"`**. |
| **`npm run verify:post-implementation`** | Alias for **`verify:local`**. |
| **`npm run validate:all`** | **IA tools** subset only (no Unity / Postgres bridge). **`compute-lib:build`** matches **CI** ordering before **`test:ia`** ([`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml)). |
| **`npm run unity:testmode-batch`** | **Agent test mode batch** (glossary): headless **Editor** load smoke on **committed scenarios** — **`tools/scripts/unity-testmode-batch.sh`**, **`AgentTestModeBatchRunner.Run`**, report under **`tools/reports/`** (optional **`--golden-path`** / integer **CityStats** assert, exit **8** on mismatch). Not the **Postgres** **IDE agent bridge** queue. Matrix and flags: [`tools/fixtures/scenarios/README.md`](tools/fixtures/scenarios/README.md). |
| **`npm run unity:build-scenario-from-descriptor`** | **Scenario descriptor** batch (glossary **scenario_descriptor_v1**): headless **Editor** applies a committed **`scenario_descriptor_v1`** JSON then writes **`GameSaveData`** — **`tools/scripts/unity-build-scenario-from-descriptor.sh`**, **`ScenarioDescriptorBatchBuilder.Run`**. See [`tools/fixtures/scenarios/BUILDER.md`](tools/fixtures/scenarios/BUILDER.md). |

**Agent test-mode verification (Cursor skill):** gate, **Path A** (**Agent test mode batch**) vs **Path B** (**IDE agent bridge**), **`validate:all`** / compile gates, bounded iterate, handoff for human **normal-game** **QA** — [`ia/skills/agent-test-mode-verify/SKILL.md`](ia/skills/agent-test-mode-verify/SKILL.md).

**Not for CI.** Workflow notes: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`ia/skills/project-implementation-validation/SKILL.md`](ia/skills/project-implementation-validation/SKILL.md).

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
| CityStats | TimeManager, WaterManager, ForestManager, EconomyManager, EmploymentManager |
| AutoRoadBuilder | GridManager, RoadManager, GrowthBudgetManager, CityStats, InterstateManager, TerrainManager |
| AutoZoningManager | GridManager, ZoneManager, GrowthBudgetManager, CityStats, DemandManager |
| AutoResourcePlanner | CityStats, GridManager, GrowthBudgetManager, UIManager |
| GrowthManager | GridManager, DemandManager |
| GrowthBudgetManager | CityStats, EconomyManager |
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
- **Spec policy:** See `AGENTS.md`. Full spec inventory in `ia/specs/`; agent routing in `ia/rules/agent-router.md`. Optional MCP access to the same files: `docs/mcp-ia-server.md`; generic pattern notes: `docs/mcp-markdown-ia-pattern.md`.
- **Editor agent diagnostics (IA for agents):** `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs` emits JSON/Markdown under `tools/reports/` (gitignored outputs). Expected menus, prerequisites, and field vocabulary are documented in `ia/specs/unity-development-context.md` §10; regressions belong in a new **BACKLOG** row with **Console** output and sample exports.
- **IDE agent bridge (Postgres queue):** **`unity_bridge_command`**, **`unity_bridge_get`**, and **`unity_compile`** (alias for **`get_compilation_status`**) enqueue work for **`AgentBridgeCommandRunner`** via **`agent_bridge_job`** — see **`docs/mcp-ia-server.md`** and **unity-development-context** §10. Bridge **`kind`** values include **`get_compilation_status`** (compile snapshot in **`response.compilation_status`**) for agents when the **Editor** holds the project open. When **MCP** + **Unity** are available on the dev machine, **AI agents** should run the **Play Mode** smoke sequence documented there (**`get_play_mode_status`** → **`enter_play_mode`** → **`get_play_mode_status`** → **`exit_play_mode`**) to reduce manual **Play**/**Stop** clicks; full before/after **`debug_context_bundle`** workflow: **`ia/skills/close-dev-loop/SKILL.md`**.

## Known Trade-offs

- **High coupling:** Managers reference each other directly.
- **GridManager size:** ~2070 lines; decomposition tracked in `BACKLOG.md`.
- **No event system:** Direct method calls, not events.
