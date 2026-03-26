# AI Agent Guide — Territory Developer

## Before You Start

1. Read `ARCHITECTURE.md` to understand the project structure, data flows, and dependency map
2. Read `.cursor/rules/` for coding conventions (including **prefab naming for new assets** in `coding-conventions.mdc`) and manager responsibilities
3. Check the `/// <summary>` on the class you are about to modify — it describes its role and dependencies
4. Read `BACKLOG.md` for the current list of issues, priorities, and what's in progress

### Canonical geography specification

For **grid coordinates, height model, terrain slopes, water vs land, shore/cliff layering, sorting, procedural lakes/rivers, water persistence, roads/terraform/bridges/interstate, and pathfinding costs**, treat **`.cursor/specs/isometric-geography-system.md`** as the **single canonical reference**. **`ARCHITECTURE.md`** summarizes init order and persistence; it defers geography **mechanisms** to that spec.

Do **not** create a parallel “master” geography doc; extend or fix **`isometric-geography-system.md`** and reference **`BACKLOG.md`** for open issues.

### `.cursor/specs/` policy (avoid spec clutter)

**`.cursor/specs/`** holds only **durable system specifications**:

| File | Scope |
|------|--------|
| `isometric-geography-system.md` | Terrain, water, cliffs, shores, sorting, terraform, roads, rivers, pathfinding |
| `ui-design-system.md` | UI foundations, components, patterns |

Do **not** add bug write-ups, agent prompts, one-off “fix” specs, or archived prompts under `.cursor/specs/`. While work is open, document details in **`BACKLOG.md`** (issue **Notes** / **Files**). When an issue is **completed and verified**, **delete** any temporary markdown artifacts — stale specs pollute agent context.

## Project docs outside `.cursor/specs/`

Charters and discovery for cross-cutting programs (e.g. UI design) live under **`docs/`** as listed in `ARCHITECTURE.md`. There is **no** `specs/archive` or `specs/bugs` folder; do not recreate them for completed work.

## Language
All code, comments, XML docs, annotations, Debug.Log messages, and repository content must be in **English**. Chat with the user may be in any language.

## Backlog: Next Issue and AI Agent Prompts

When the user asks which is the next issue to work on (or similar), respond with the issue and **ask if they want you to create an AI agent prompt** — a prompt for another agent to analyze, evaluate, and propose a development plan in Cursor for the changes needed to resolve the issue, clarifying all questions before writing the plan file.

### Format when delivering an AI agent prompt

Whenever the user asks for an **AI agent prompt** (including after they accept your offer to create one), respond with the prompt as **Markdown**, not unstructured plain text, so it can be copied in one step and saved as a `.md` file without losing headings, lists, or tables.

- **Preferred:** Put the **full** prompt document inside one **fenced code block** with the language tag `markdown`: start with a line containing only three backticks plus `markdown`, end with a line containing only three backticks. The user copies the inner Markdown into a `.md` file (optionally stripping those two fence lines). Inside the block use normal Markdown: `#` / `##` headings, lists, tables, `` `paths` ``, and links such as `[BUG-42](BACKLOG.md)`.
- **Short prompts:** If the prompt fits in a few lines, you may emit the same Markdown **without** a fence in the chat — but for anything longer than a short paragraph, **use the `markdown` fence** so nested lists and code paths do not flatten when copied.
- Prompt **body** for another agent should be in **English** (repository language rule), unless the user explicitly asks for another language for that prompt.

This applies to **any** request to “write a prompt for another agent / subagent,” not only backlog-driven prompts.

## Backlog: After Implementing a Plan
After executing a development plan for an issue, **keep the issue in "In progress"**. Do NOT move it to "Completed". Only move to "Completed" when the user explicitly confirms the fix has been verified (e.g. after testing in Unity).

## What to Read by Task Type

| Task | Primary File(s) | Then Also Read |
|------|-----------------|----------------|
| Backlog issue | `BACKLOG.md` | Files listed in the issue's "Files" field |
| Road logic | `RoadManager.cs` | `GridManager.cs` (coordinate conversion), `TerrainManager.cs` (slopes). Use **`TryPrepareRoadPlacementPlan`** or **`TryPrepareRoadPlacementPlanLongestValidPrefix`** (streets) with **`RoadPathValidationContext`**; interstate uses full-path prepare with `forbidCutThrough: true`. Spec: `.cursor/specs/isometric-geography-system.md` §14 (BACKLOG **BUG-25** completed; regressions **BUG-37**). |
| Zoning logic | `ZoneManager.cs` | `GridManager.cs`, `DemandManager.cs` |
| UI changes | `UIManager.cs` | The specific Controller in `UnitControllers/` or `GameControllers/`. Design system program: `docs/ui-design-system-project.md`, context `docs/ui-design-system-context.md`, spec `.cursor/specs/ui-design-system.md` (toolbar **§3.3**). **ControlPanel** layout: **TECH-07**, `MainScene.unity`. |
| UI / UX design system (meta) | `docs/ui-design-system-project.md` | `docs/ui-design-system-context.md`, `.cursor/specs/ui-design-system.md`, `UIManager.cs`; ticket work in `BACKLOG.md` |
| Simulation / AUTO growth | `SimulationManager.cs` | `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `AutoResourcePlanner.cs`, `UrbanCentroidService.cs`, `GrowthBudgetManager.cs`. Legacy **UrbanizationProposal** is obsolete (**TECH-13**); do not re-enable. |
| Economy | `EconomyManager.cs` | `CityStats.cs` |
| Isometric geography / slopes / heightmap | `.cursor/specs/isometric-geography-system.md` | `TerrainManager.cs`, `HeightMap.cs`, `TerraformingService.cs`, `RoadPrefabResolver.cs`, `SlopePrefabRegistry.cs`, `GridPathfinder.cs` |
| Terrain/heightmap | `TerrainManager.cs` | `HeightMap.cs`, `GeographyManager.cs` |
| Water bodies | `WaterManager.cs` | `.cursor/specs/isometric-geography-system.md` (§2, §4.2, §5.6–5.9, §7, §12–§13, §15). `WaterMap.cs`, `WaterBody.cs`, `WaterBodyType.cs`, `GeographyManager.cs`. **BUG-42** completed 2026-03-26 (shores + water–water cascades); **BUG-45** multi-body junctions (in progress). **BUG-39** / **BUG-40** completed 2026-03-24. **BUG-32** minimap; **BUG-34** / **BUG-35** load (2026-03-22) |
| Minimap height / relief (optional layer) | `MiniMapController.cs` | `HeightMap`, `GridManager`; **FEAT-42** in `BACKLOG.md` |
| Forests | `ForestManager.cs` | `ForestMap.cs`, `GeographyManager.cs` |
| New building type | `IBuilding.cs` (interface) | `ZoneManager.cs`, `GridManager.cs` (placement) |
| New prefab variants / slope asset names | `.cursor/rules/coding-conventions.mdc` (Prefabs and asset naming) | `SlopePrefabRegistry.cs`, `.cursor/specs/isometric-geography-system.md` §6.4 |
| Sorting/render bug | `GridManager.cs` region "Sorting Order" | `TerrainManager.cs`, `GridSortingOrderService.cs`; formulas §7 in `.cursor/specs/isometric-geography-system.md` |
| Interstate highways | `InterstateManager.cs` | `GridManager.cs`, `TerrainManager.cs`, `RoadManager.cs` (`TryPrepareRoadPlacementPlan`). Spec: `.cursor/specs/isometric-geography-system.md` §10, §14.5–§14.6 (BACKLOG **BUG-27** / **BUG-29** completed). |
| Save/load | `GameSaveManager.cs` | `GridManager.cs` (GetGridData/RestoreGrid), `CellData.cs`, `WaterManager.cs` (`GetSerializableData` / `RestoreWaterMapFromSaveData`), `WaterMapData` on `GameSaveData`. Load sorting **BUG-34** + **BUG-35** (completed 2026-03-22). Spec: `.cursor/specs/isometric-geography-system.md` §7.4, §12.5. |
| GridManager decomposition | `BACKLOG.md` **TECH-01** | `GridManager.cs`, helpers (`ChunkCullingSystem`, `RoadCacheService`, …). Next extractions: BulldozeHandler, GridInputHandler, CoordinateConversionService. |
| Demand/growth | `DemandManager.cs` | `GrowthManager.cs`, `EmploymentManager.cs`, `CityStats.cs` |
| Statistics display | `StatisticsManager.cs` | `CityStatsUIController.cs`, `CityStats.cs` |
| Camera/viewport | `CameraController.cs` | `GridManager.cs` (chunk culling) |

## Anti-patterns to Avoid

- **Do NOT create new singletons** — use the Inspector + FindObjectOfType pattern
- **Do NOT access `gridArray` or `cellArray` directly** from outside GridManager — use `GetCell(x, y)`
- **Do NOT add more responsibilities to GridManager** — extract to helper classes instead
- **Do NOT use `FindObjectOfType` in Update or loops** — only in Awake/Start
- **Do NOT forget `InvalidateRoadCache()`** after modifying roads
- **Do NOT instantiate managers with `new`** — they are scene components
- **Do NOT re-enable** the obsolete **UrbanizationProposal** flow — removal is **TECH-13** in `BACKLOG.md`
- **Do NOT add** bug reports, agent prompts, or one-off fix specs under **`.cursor/specs/`** — only `isometric-geography-system.md` and `ui-design-system.md` (see **`.cursor/specs/` policy** above)

## Pre-commit Checklist

- [ ] Code compiles (Build in Unity)
- [ ] Class-level `/// <summary>` exists and is accurate
- [ ] New public methods have XML documentation
- [ ] Debug.Log messages and comments are in English
- [ ] If GridManager was touched, verify sorting order works with different height levels
- [ ] If roads were modified, verify `InvalidateRoadCache()` is called where needed
- [ ] If a new manager was added, it follows the Inspector + FindObjectOfType dependency pattern
- [ ] New prefabs / asset names follow `.cursor/rules/coding-conventions.mdc` (do not rename existing assets; use conventions for new variants)
