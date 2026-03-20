# Agent Prompt: Interstate Slope Prefabs (BUG-30)

## Task

Create a **development plan** for fixing incorrect road prefab selection when the interstate climbs slopes. The plan should be written as a markdown file (e.g. `docs/plan-interstate-slope-prefabs.md` or `.cursor/specs/plan-interstate-slope-prefabs.md`) that another developer or agent can execute. Before writing the plan, analyze the codebase, clarify any open questions, and propose a concrete, step-by-step approach.

---

## Issue Summary

When the interstate road **climbs a hill** (path goes over slopes, not cut-through), the wrong road prefabs are used:

- **Flat/junction prefabs** are used instead of **ramp/slope prefabs** at slope transitions
- **Wrong junction** at the top of the slope
- **Visual gaps and disconnection** between road segments

**Backlog reference:** BUG-30 — Incorrect road prefabs when interstate climbs slopes

---

## Context from Prior Analysis

### Prefab Resolution Flow

1. **RoadManager.PlaceInterstateFromPath** — Calls `TerraformingService.ComputePathPlan(expandedPath)` then `RoadPrefabResolver.ResolveForPath(expandedPath, plan)`.

2. **RoadPrefabResolver.ResolveForPath** — For each path cell, gets `postTerraformSlopeType` from `plan.pathCells[i]`, then calls `ResolvePrefabForPathCell(..., height, postSlope)`.

3. **ResolvePrefabForPathCell** — Resolution order:
   - height == 0 → bridge
   - IsWaterSlopeCell → bridge
   - Path neighbors → elbow, or `TrySlopeForStraight(postSlope, isHorizontal)` for straight segments
   - `TrySlopeFromPostTerraform(postSlope, isHorizontal)` as fallback
   - Default: flat straight (prefab1 or prefab2)

4. **TrySlopeForStraight / TrySlopeFromPostTerraform** — Return slope prefab (North/South/East/West) when `postSlope` is orthogonal or corner slope. Return null when `postSlope == Flat`.

### Terraform Plan for Slopes (Non Cut-Through)

When the path crosses slopes without cut-through, **TerraformingService.ComputePathPlan** sets `postTerraformSlopeType` per cell:

- **Flat cell:** `postTerraformSlopeType = Flat`
- **Orthogonal slope, road parallel:** Flatten → `Flat`
- **Orthogonal slope, road perpendicular:** `postTerraformSlopeType = GetSlopeTypeFromRoadDirection(dxOut, dyOut)` (N/S/E/W)
- **Corner slope, road orthogonal:** `postTerraformSlopeType = GetPostTerraformSlopeTypeAlongExit(...)` (aligned with diagonal ramps; BUG-30)
- **Diagonal slope:** Various (flatten or orthogonal)

### Suspected Root Causes

1. **Index mismatch:** `plan.pathCells[i]` assumes path and plan have same length and order. If `ExpandDiagonalStepsToCardinal` or `ComputePathPlan` alters the path, indices may not align.

2. **postSlope = Flat when it should be slope:** Cells at slope transitions (flat→slope or slope→flat) might get `Flat` from the plan, so `TrySlopeForStraight` returns null and flat prefab is used.

3. **Top-of-slope junction:** The cell at the crest (where slope meets flat plateau) may have path neighbors in multiple directions. The resolver might pick elbow/T-junction instead of a slope-end or ramp-top prefab.

4. **Live terrain vs plan:** `ResolvePrefabForPathCell` uses `postSlope` from the plan. If the plan is wrong or missing for some cells, there is no fallback to live terrain slope (`terrainManager.GetTerrainSlopeTypeAt`).

5. **Corner slopes:** cardinal type comes from travel + segment Δh (`GetPostTerraformSlopeTypeAlongExit`). Road fallback uses `TryRampRoadPrefabFromPrevTravel` in `RoadPrefabResolver`.

### Relevant Files

| File | Role |
|------|------|
| `RoadPrefabResolver.cs` | `ResolveForPath`, `ResolvePrefabForPathCell`, `TrySlopeForStraight`, `TrySlopeFromPostTerraform`, `GetWorldPositionForPrefab` |
| `TerraformingService.cs` | `ComputePathPlan`, slope/corner handling, `postTerraformSlopeType` assignment |
| `PathTerraformPlan.cs` | `CellPlan.postTerraformSlopeType`, plan structure |
| `RoadManager.cs` | `PlaceInterstateFromPath`, path expansion, terraform + resolve pipeline |
| `TerrainManager.cs` | `GetTerrainSlopeTypeAt`, slope types |

### Existing Spec References

- `.cursor/specs/interstate-prefab-and-pathfinding-fixes.md` — Rule C (Terraform wins), Phase 1 (prefab fixes), BUG-30
- BUG-31 is separate: wrong prefabs at entry/exit (border) — out of scope for this prompt

---

## Instructions for the Agent

1. **Read** the files listed above, especially:
   - `RoadPrefabResolver.ResolvePrefabForPathCell` and `TrySlopeForStraight`
   - `TerraformingService.ComputePathPlan` (non cut-through branch, slope handling)
   - The path expansion flow: `ExpandDiagonalStepsToCardinal` → `ComputePathPlan` → `ResolveForPath`

2. **Trace** the exact flow when a path climbs a hill (flat → slope → flat): which cells get which `postTerraformSlopeType`, and which prefab each receives.

3. **Identify** the specific code paths or conditions that lead to wrong prefabs. Consider:
   - Cells where `postSlope` is Flat but the cell is on a slope
   - Cells at slope transitions (bottom of ramp, top of ramp)
   - Index alignment between `path` and `plan.pathCells`

4. **Clarify** before writing the plan:
   - Does the plan always have one entry per path cell, or can counts differ?
   - Should there be a fallback to live terrain slope when `postSlope == Flat` but the cell has a slope?
   - Are there slope prefabs for all transition cases (flat→slope, slope→flat, slope→slope)?

5. **Propose** a development plan with:
   - Concrete tasks (file + method + change description)
   - Order of implementation (dependencies)
   - Acceptance criteria for each phase
   - Manual verification steps (e.g. generate interstate that climbs a hill, inspect prefabs)

6. **Output** the plan as a new markdown file. Do not implement the changes—only write the plan.

---

## Out of Scope

- BUG-31 — Wrong prefabs at interstate entry/exit (border). Isolated for separate work.
- Cut-through craters (BUG-29).
- Bridge prefabs — user reports no issues with bridges.
