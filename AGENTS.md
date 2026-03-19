# AI Agent Guide — Territory Developer

## Before You Start

1. Read `ARCHITECTURE.md` to understand the project structure, data flows, and dependency map
2. Read `.cursor/rules/` for coding conventions and manager responsibilities
3. Check the `/// <summary>` on the class you are about to modify — it describes its role and dependencies
4. Read `BACKLOG.md` for the current list of issues, priorities, and what's in progress

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
| UI changes | `UIManager.cs` | The specific Controller in `UnitControllers/` |
| Simulation | `SimulationManager.cs` | The relevant `Auto*Manager` |
| Economy | `EconomyManager.cs` | `CityStats.cs` |
| Terrain/heightmap | `TerrainManager.cs` | `HeightMap.cs`, `GeographyManager.cs` |
| Water bodies | `WaterManager.cs` | `WaterMap.cs`, `GeographyManager.cs` |
| Forests | `ForestManager.cs` | `ForestMap.cs`, `GeographyManager.cs` |
| New building type | `IBuilding.cs` (interface) | `ZoneManager.cs`, `GridManager.cs` (placement) |
| Sorting/render bug | `GridManager.cs` region "Sorting Order" | `TerrainManager.cs` |
| Interstate highways | `InterstateManager.cs` | `GridManager.cs`, `TerrainManager.cs`, `RoadManager.cs` (`TryPrepareRoadPlacementPlan`). Spec: `.cursor/specs/interstate-prefab-and-pathfinding-fixes.md`. Cut-through notes: `docs/plan-cut-through-craters.md`. |
| Save/load | `GameSaveManager.cs` | `GridManager.cs` (GetGridData/RestoreGrid), `CellData.cs` |
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

## Pre-commit Checklist

- [ ] Code compiles (Build in Unity)
- [ ] Class-level `/// <summary>` exists and is accurate
- [ ] New public methods have XML documentation
- [ ] Debug.Log messages and comments are in English
- [ ] If GridManager was touched, verify sorting order works with different height levels
- [ ] If roads were modified, verify `InvalidateRoadCache()` is called where needed
- [ ] If a new manager was added, it follows the Inspector + FindObjectOfType dependency pattern
