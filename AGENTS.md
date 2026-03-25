# AI Agent Guide — Territory Developer

## Before You Start

1. Read `ARCHITECTURE.md` to understand the project structure, data flows, and dependency map
2. Read `.cursor/rules/` for coding conventions (including **prefab naming for new assets** in `coding-conventions.mdc`) and manager responsibilities
3. Check the `/// <summary>` on the class you are about to modify — it describes its role and dependencies
4. Read `BACKLOG.md` for the current list of issues, priorities, and what's in progress

## Archived documentation (do not read by default)

Completed implementation summaries, historical agent prompts, and old scene-setup notes live under **`.cursor/specs/archive/`**. See **`.cursor/specs/archive/README.md`**. **Do not** treat those files as active work — use them only for historical context. Active specs are **`.cursor/specs/*.md`** outside `archive/`, plus `ARCHITECTURE.md` and `docs/` (e.g. active plans such as `docs/plan-zoning-road-candidates-grass-forest-slopes.md`).

## Language
All code, comments, XML docs, annotations, Debug.Log messages, and repository content must be in **English**. Chat with the user may be in any language.

## Backlog: Next Issue and AI Agent Prompts
When the user asks which is the next issue to work on (or similar), respond with the issue and **ask if they want you to create an AI agent prompt** — a prompt for another agent to analyze, evaluate, and propose a development plan in Cursor for the changes needed to resolve the issue, clarifying all questions before writing the plan file.

## Backlog: After Implementing a Plan
After executing a development plan for an issue, **keep the issue in "In progress"**. Do NOT move it to "Completed". Only move to "Completed" when the user explicitly confirms the fix has been verified (e.g. after testing in Unity).

## What to Read by Task Type

| Task | Primary File(s) | Then Also Read |
|------|-----------------|----------------|
| Backlog issue | `BACKLOG.md` | Files listed in the issue's "Files" field |
| Road logic | `RoadManager.cs` | `GridManager.cs` (coordinate conversion), `TerrainManager.cs` (slopes). Use **`TryPrepareRoadPlacementPlan`** or **`TryPrepareRoadPlacementPlanLongestValidPrefix`** (streets) with **`RoadPathValidationContext`** for terraform routes; interstate uses full-path prepare with `forbidCutThrough: true`. Spec: `.cursor/specs/road-drawing-fixes.md` (BACKLOG **BUG-25**). |
| Zoning logic | `ZoneManager.cs` | `GridManager.cs`, `DemandManager.cs` |
| UI changes | `UIManager.cs` | The specific Controller in `UnitControllers/` or `GameControllers/`. Design system program: `docs/ui-design-system-project.md`, context `docs/ui-design-system-context.md`, spec `.cursor/specs/ui-design-system.md` (toolbar **§3.3**). **ControlPanel** layout: **TECH-07**, `MainScene.unity`. |
| UI / UX design system (meta) | `docs/ui-design-system-project.md` | `docs/ui-design-system-context.md`, `.cursor/specs/ui-design-system.md`, `UIManager.cs`; ticket work in `BACKLOG.md` |
| Simulation / AUTO growth | `SimulationManager.cs` | `AutoRoadBuilder.cs`, `AutoZoningManager.cs`, `AutoResourcePlanner.cs`, `UrbanCentroidService.cs`, `GrowthBudgetManager.cs`. Legacy **UrbanizationProposal** is obsolete (**TECH-13**); do not re-enable. |
| Economy | `EconomyManager.cs` | `CityStats.cs` |
| Isometric geography / slopes / heightmap | `.cursor/specs/isometric-geography-system.md` | `TerrainManager.cs`, `HeightMap.cs`, `TerraformingService.cs`, `RoadPrefabResolver.cs`, `SlopePrefabRegistry.cs`, `GridPathfinder.cs` |
| Terrain/heightmap | `TerrainManager.cs` | `HeightMap.cs`, `GeographyManager.cs` |
| Water bodies | `WaterManager.cs` | `WaterMap.cs`, `WaterBody.cs`, `WaterBodyType.cs`, `GeographyManager.cs`. **FEAT-37a** / **FEAT-37b** / **FEAT-37c** completed; spec `.cursor/specs/water-system-refactor.md`. Lake + river shore + cliff stacks + sorting: `.cursor/specs/bugs/cliff-water-shore-sorting.md`; shore / cliff / waterfall follow-up **BUG-42** (merged **BUG-33** + **BUG-41**); **BUG-39** / **BUG-40** completed 2026-03-24 (cliff placement tunables + foreground-water sort cap on `TerrainManager`). Minimap water **BUG-32** completed; load building sort **BUG-34** + grass under buildings **BUG-35** (both completed 2026-03-22) |
| Minimap height / relief (optional layer) | `MiniMapController.cs` | `HeightMap`, `GridManager`; **FEAT-42** in `BACKLOG.md` |
| Forests | `ForestManager.cs` | `ForestMap.cs`, `GeographyManager.cs` |
| New building type | `IBuilding.cs` (interface) | `ZoneManager.cs`, `GridManager.cs` (placement) |
| New prefab variants / slope asset names | `.cursor/rules/coding-conventions.mdc` (Prefabs and asset naming) | `SlopePrefabRegistry.cs`, `.cursor/specs/isometric-geography-system.md` §6.4 |
| Sorting/render bug | `GridManager.cs` region "Sorting Order" | `TerrainManager.cs` |
| Interstate highways | `InterstateManager.cs` | `GridManager.cs`, `TerrainManager.cs`, `RoadManager.cs` (`TryPrepareRoadPlacementPlan`). Spec: `.cursor/specs/interstate-prefab-and-pathfinding-fixes.md`. Cut-through (historical): `.cursor/specs/archive/plan-cut-through-craters.md`. |
| Save/load | `GameSaveManager.cs` | `GridManager.cs` (GetGridData/RestoreGrid), `CellData.cs`, `WaterManager.cs` (`GetSerializableData` / `RestoreWaterMapFromSaveData`), `WaterMapData` on `GameSaveData`. Load sorting **BUG-34** + **BUG-35** (completed 2026-03-22). Archived reference: `.cursor/specs/archive/agent-prompt-load-game-building-sorting-order.md`. |
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

## Pre-commit Checklist

- [ ] Code compiles (Build in Unity)
- [ ] Class-level `/// <summary>` exists and is accurate
- [ ] New public methods have XML documentation
- [ ] Debug.Log messages and comments are in English
- [ ] If GridManager was touched, verify sorting order works with different height levels
- [ ] If roads were modified, verify `InvalidateRoadCache()` is called where needed
- [ ] If a new manager was added, it follows the Inspector + FindObjectOfType dependency pattern
- [ ] New prefabs / asset names follow `.cursor/rules/coding-conventions.mdc` (do not rename existing assets; use conventions for new variants)
