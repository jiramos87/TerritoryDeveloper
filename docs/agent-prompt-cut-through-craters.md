# Agent Prompt: Cut-Through Craters (BUG-29)

**Status:** Issue **completed** in BACKLOG (2026-03-19). Implementation summary: [plan-cut-through-craters.md](plan-cut-through-craters.md). This prompt is retained for historical context.

## Task

Create a **development plan** for fixing the cut-through crater issue. The plan should be written as a markdown file (e.g. `docs/plan-cut-through-craters.md` or `.cursor/specs/plan-cut-through-craters.md`) that another developer or agent can execute. Before writing the plan, analyze the codebase, clarify any open questions, and propose a concrete, step-by-step approach.

---

## Issue Summary

When an interstate (or road) performs a **cut-through** across a hill (height ≥ 2), black voids ("craters") appear where terrain tiles should be. The hill is partially removed or incorrectly rendered, leaving visible gaps.

**Backlog reference:** BUG-29 — Cut-through: high hills cut through disappear leaving crater

---

## Context from Prior Analysis

### Cut-Through Flow

1. **TerraformingService.ComputePathPlan** — When `pathCrossesHill` is true (path has consecutive height diff > 1 and maxHeight ≥ 2), the plan uses cut-through mode: path cells are flattened to `baseHeight` (min height along path).

2. **PathTerraformPlan.Apply** — Three phases:
   - **Phase 1:** Set heights on heightmap for path and adjacent cells.
   - **Phase 2:** Call `RestoreTerrainForCell` on path and adjacent cells (with `forceFlat` / `forceSlopeType` where applicable).
   - **Phase 3:** Refresh 8 neighbors (cardinal + diagonal) of modified cells so slope/cliff sprites update.

3. **ExpandAdjacentFlattenCellsRecursively** — Adds adjacent cells to flatten only when `|neighborHeight - baseHeight| > 1`. For a hill at h=2 and baseHeight=1, this does **not** expand (diff = 1), so the cut boundary is narrow.

### Suspected Root Causes

1. **DetermineSlopePrefab** (TerrainManager) only places slopes on the **lower** cell of a height pair (`hasNorthSlope = northHeight > currentHeight`). Cells at h=2 with a neighbor at h=1 (the cut) have no higher neighbors → returns null → `PlaceFlatTerrain`. This may not correctly represent the cliff face at the cut boundary.

2. **Phase 3 neighbor refresh** may not cover all cells that need updating, or the order of operations may leave some cells in an inconsistent state.

3. **Corner/edge configurations** at the cut boundary (e.g. cell with two neighbors at h=1, two at h=2) may not have a matching slope prefab, leading to degenerate or missing terrain.

4. **PlaceCliffWalls** — Cliff walls are placed when `currentHeight - neighborHeight > 1`. For a diff of exactly 1 (h=2 vs h=1), no cliff wall is placed. The transition may rely on slope prefabs that are not being selected correctly.

### Relevant Files

| File | Role |
|------|------|
| `TerraformingService.cs` | `ComputePathPlan`, `ExpandAdjacentFlattenCellsRecursively`, cut-through logic |
| `PathTerraformPlan.cs` | `Apply`, `ValidateNoHeightDiffGreaterThanOne`, Phase 1–3 |
| `TerrainManager.cs` | `RestoreTerrainForCell`, `DetermineSlopePrefab`, `RequiresSlope`, `PlaceCliffWalls`, `PlaceFlatTerrain` |
| `InterstateManager.cs` | Pathfinding (avoidHighTerrain, path selection) |
| `RoadManager.cs` | `PlaceInterstateFromPath`, terraform + placement pipeline |

### Existing Spec References

- `.cursor/specs/interstate-prefab-and-pathfinding-fixes.md` — Section 3.4 (Cut-through visual artifacts), Phase 3 (Cut-through robustness)
- BUG-29 rule in TerraformingService: reject cut-through when `maxHeight - baseHeight > 1`

---

## Instructions for the Agent

1. **Read** the files listed above, especially:
   - `PathTerraformPlan.Apply` (Phases 1–3)
   - `TerrainManager.RestoreTerrainForCell` and `DetermineSlopePrefab`
   - `TerraformingService.ExpandAdjacentFlattenCellsRecursively`

2. **Trace** the exact flow when a path cuts through a hill (h=2): which cells get modified, which get refreshed, and what terrain prefab each receives.

3. **Identify** the specific code paths or conditions that lead to missing terrain (black holes). Consider:
   - Cells that are never refreshed
   - Cells where `DetermineSlopePrefab` returns null and `PlaceFlatTerrain` is insufficient
   - Cells at the boundary (h=2 next to h=1) that need a cliff or slope representation

4. **Clarify** before writing the plan:
   - Is the crater on the flattened path cells, the adjacent hill cells, or both?
   - Does `DetermineSlopePrefab` need to handle "neighbor is lower" (cliff-down) cases, or is that handled elsewhere?
   - Should the expansion logic be changed to include more adjacent cells at the cut boundary?

5. **Propose** a development plan with:
   - Concrete tasks (file + method + change description)
   - Order of implementation (dependencies)
   - Acceptance criteria for each phase
   - Any tests or manual verification steps

6. **Output** the plan as a new markdown file. Do not implement the changes—only write the plan.

---

## Out of Scope (per user)

- Bridge prefab issues — user reports no problems with bridge prefabs.
- Focus is exclusively on craters / black holes at cut-through boundaries.
