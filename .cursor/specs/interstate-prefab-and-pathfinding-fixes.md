# Interstate Prefab Selection & Pathfinding — Spec & Development Plan

## Overview

This spec addresses multiple issues observed in interstate route generation: incorrect road prefab selection (especially elbows), zigzag paths that could be simplified, poor "environmental" path choices (hugging hills instead of offsetting to avoid them), and cut-through terrain interactions that produce visual artifacts. It also clarifies how the road subsystems share logic and proposes a unified approach.

**Status (2026-03-20):** Bridge approach (Rule F) and shared terraform validation are in place. **BUG-27**, **BUG-29**, and **BUG-30** (interstate/manual slope prefabs on hills, corner cells aligned with travel) are **completed** in [BACKLOG.md](../../BACKLOG.md). **Remaining:** sorting order (**BUG-28**), prefabs at border entry/exit (**BUG-31** — not BUG-30).

## Related Documents

- [bridge-and-junction-fixes.md](bridge-and-junction-fixes.md) — Bridge disappearing, junction refresh
- [road-drawing-fixes.md](road-drawing-fixes.md) — Manual draw pipeline, preview consistency, Phase 1–4 (**BUG-25**)
- [BACKLOG.md](../../BACKLOG.md) — BUG-25 (completed), BUG-23 (interstate flaky, completed)
- [archive/plan-cut-through-craters.md](archive/plan-cut-through-craters.md) — BUG-29 implementation notes (archived; historical reference)

## Related Files

| File | Role |
|------|------|
| `RoadPrefabResolver.cs` | Centralized prefab selection for path and single-cell contexts |
| `RoadManager.cs` | Manual draw, interstate placement, `PlaceInterstateFromPath` |
| `InterstateManager.cs` | Interstate A* pathfinding, `FindInterstatePathAStar`, `BiasedWalkPath` |
| `GridPathfinder.cs` | Manual draw A* (via `GridManager.FindPath`) |
| `TerraformingService.cs` | `ComputePathPlan`, `ExpandDiagonalStepsToCardinal` |
| `PathTerraformPlan.cs` | Terraform Apply/Revert, cut-through mode |
| `TerrainManager.cs` | Terrain visuals, `RestoreTerrainForCell`, `PlaceWaterShore` |
| `AutoRoadBuilder.cs` | Auto road mode, `TryGetStreetPlacementPlan` → `TryPrepareRoadPlacementPlan` when available |
| `RoadPathCostConstants.cs` | Shared cost model for slopes |

---

## 1. Subsystem Logic Sharing

### What is shared

| Component | Manual Draw | Interstate | Auto Road |
|-----------|-------------|------------|-----------|
| **Prefab resolution** | `RoadPrefabResolver.ResolveForPath` | Same | Same |
| **Terraforming** | `TerraformingService.ComputePathPlan` + `PathTerraformPlan.Apply` | Same | Same |
| **Cost model** | `RoadPathCostConstants` (via GridPathfinder) | Same | Same |
| **Bridge validation** | `StraightenBridgeSegments`, `IsBridgePathValid`, `HasElbowTooCloseToWater` | Same | Same |
| **Path expansion** | `ExpandDiagonalStepsToCardinal` | Same | Same |
| **Terraform validation entry** | `RoadManager.TryPrepareRoadPlacementPlanLongestValidPrefix` (`forbidCutThrough: false`) for streets | `TryPrepareRoadPlacementPlan` (`forbidCutThrough: true`, full path) | `AutoRoadBuilder.TryGetStreetPlacementPlan` → longest-prefix API when `RoadManager` present |

### What is different

| Component | Manual Draw | Interstate | Auto Road |
|-----------|-------------|------------|-----------|
| **Pathfinding** | `GridPathfinder.FindPath` (A* + SmoothPath) | `InterstateManager.FindInterstatePathAStar` + `BiasedWalkPath` | `GridManager.FindPath` / `FindPathWithRoadSpacing` |
| **Path source** | User drag (start → end) | Border-to-border (entry → exit) | Edge → target (interstate, cluster, or straight segment) |

**Interstate endpoints:** `InterstateManager` ranks border cells with `ComputeInterstateBorderEndpointScore` (flat neighbors, flat mandatory first step inland, low border height). Lists are sorted best-first; random generation picks from the top third of candidates; `TryGenerateInterstateDeterministic` breaks Manhattan-distance ties by combined entry+exit score.

**Interstate A\*:** `PickLowerCostInterstateAStarPath` runs `RunInterstateAStar` with `avoidHighTerrain` true and false; if both reach the exit, the cheaper path by `ComputePathCost` wins (so a shorter hill-crossing route can beat a long flat U). `InterstateAwayFromGoalPenalty` penalizes steps that increase Manhattan distance to the goal, reducing aimless detours.

**Conclusion:** Prefab selection, bridge checks, and terraform plan validation are fully shared via **`TryPrepareRoadPlacementPlan`**. Interstate adds **`RoadPathValidationContext.forbidCutThrough`** so routes that would flatten a hill in cut-through mode are rejected; manual streets and auto roads allow cut-through when `ComputePathPlan` marks it valid. Pathfinding is **not** shared: Interstate uses its own A*, which has no straightness preference and can produce zigzag routes that then get incorrect prefabs when the resolver misinterprets connectivity.

---

## 2. Simple Rules (Postulated from Analysis)

1. **Rule A — Elbow connectivity:** For a cell with exactly two path neighbors (L+R, U+D, or two diagonals), the prefab must match the actual path directions. No T-junction or crossing prefab when only two neighbors exist.

2. **Rule B — Path continuity:** The prefab’s exits must align with the path’s in/out directions. If the path goes East→North, the elbow must connect East and North.

3. **Rule C — Terraform wins:** When the plan says `postTerraformSlopeType = Flat` (cut-through), use flat road prefabs. Do not fall back to slope prefabs based on live terrain.

4. **Rule D — Environmental preference:** When multiple paths have similar cost, prefer the one that avoids hills (offset by 1 cell) over cutting through them, to reduce terraforming and visual artifacts.

5. **Rule E — Straightness for interstate:** Interstate paths should prefer straight segments. Zigzags that could be replaced by a single turn (e.g. East→North elbow) should be avoided by pathfinding or corrected post-path.

6. **Rule F — Bridge approach:** The road must approach the water edge perpendicular to the bridge axis. No turn on the last land cell before water.

---

## 3. Identified Issues

### 3.1 Prefab selection bug — Wrong elbow for Right+Down — FIXED

**Location:** `RoadPrefabResolver.cs` line 218

**Symptom:** When the path has neighbors to the Right and Down, the resolver returns `ElbowUpRight` instead of `ElbowUpLeft`.

**Root cause:** Copy-paste error. `SelectFromConnectivity` (line 289) correctly uses `ElbowUpLeft` for `hasRight && hasDown`. `ResolvePrefabForPathCell` (line 218) incorrectly uses `ElbowUpRight`.

**Fix:**
```csharp
// Before
if (pathRight && pathDown && !pathLeft && !pathUp)
    return roadManager.roadTilePrefabElbowUpRight;

// After
if (pathRight && pathDown && !pathLeft && !pathUp)
    return roadManager.roadTilePrefabElbowUpLeft;
```

### 3.2 Zigzag path instead of simple elbow (Screenshot 2)

**Symptom:** A simple East-to-North turn is rendered as multiple curves/zigzags instead of one elbow.

**Possible causes:**
1. **Pathfinding:** Interstate A* produces a zigzag (e.g. East, North, East, North) instead of a direct turn.
2. **Path expansion:** `ExpandDiagonalStepsToCardinal` can add intermediate cells for diagonal steps; if the original path is already cardinal, no change. If the path has micro-zigzags, expansion may not help.
3. **Prefab bug (3.1):** Wrong elbow for Right+Down causes visual mismatch; the correct elbow for East+North may never be reached if the path structure is wrong.

**Mitigation:** Fix 3.1 first. Then add straightness preference to interstate pathfinding (Rule E).

### 3.3 Environmental strategy — Prefer offset to avoid hill (Screenshots 1 & 3)

**Symptom:** The interstate hugs a hill, causing cut-through terraforming and visual clipping (black holes). Moving the straight segment one coordinate West would avoid the hill.

**Root cause:** A* minimizes cost but does not compare parallel offset paths. Slope cost (60 cardinal, 35 diagonal) may still favor a path through the hill if the offset path is longer in Manhattan distance.

**Proposed approach:**
- Add a **straightness bonus** (negative cost) when the path continues in the same direction as the previous step.
- Add **parallel path sampling** for interstate: when the primary path would cross a hill (height > 1), try offset paths at ±1 perpendicular to the dominant direction and pick the one with lower total cost (including terraform cost).
- Alternatively, increase slope/cut-through cost for interstate so the pathfinder naturally prefers going around.

### 3.4 Cut-through visual artifacts (black holes)

**Symptom:** When the road cuts through a hill, triangular black voids appear where terrain and road geometry misalign.

**Root cause (from road-drawing-fixes.md):**
- Phase 3 of `PathTerraformPlan.Apply` refreshes cardinal neighbors of modified cells.
- `RestoreTerrainForCell` may not correctly handle the transition between flattened path cells and adjacent non-flattened cells.
- Terrain prefab selection at height boundaries can produce degenerate slopes or missing faces.

**Mitigation:**
- Ensure Phase 3 refreshes all 8 neighbors (including diagonals) at cut-through boundaries.
- Verify `RestoreTerrainForCell` and slope prefab logic for cells that have both flattened and non-flattened neighbors.
- If cut-through remains problematic, strengthen Rule D so pathfinding prefers avoiding hills.

### 3.5 Bridge approach — Turn at water edge (Screenshot 1 & 3)

**Symptom:** The road tile immediately before the bridge is a curve, so the approach is not perpendicular to the water.

**Root cause:** Pathfinding or bridge straightening places a turn on the last land cell. `StraightenBridgeSegments` aligns the water run but does not guarantee the approach segment is straight.

**Fix:** Enforce that the last N land cells before water (e.g. N=2) form a straight line aligned with the bridge axis. If the path has a turn within 2 cells of water, reject or adjust the path. **Implemented** — `HasTurnOnLastLandCellsBeforeWater` in RoadManager.

---

## 3.6 Issue tracker (post BUG-26)

| ID | Issue | Backlog | Status |
|----|-------|---------|--------|
| 3.6a | Interstate pathfinding — endpoint scoring, dual A*, penalties | BUG-27 | **Completed** (2026-03-19) |
| 3.6b | Sorting order between slope cell and interstate cell | BUG-28 | Open |
| 3.6c | Cut-through: tall-hill reject, cliff corridor, map-edge margin, validation ring | BUG-29 | **Completed** (2026-03-19) |
| 3.6d | Prefab selection wrong at interstate input/output points (border) | BUG-31 | Open |
| 3.6e | Slope / ramp prefabs when road climbs hills (incl. corner upslope) | BUG-30 | **Completed** (2026-03-20) |

---

## 4. Development Plan

### Phase 1 — Prefab and connectivity fixes (P0)

| Task | File | Description |
|------|------|-------------|
| 1.1 | `RoadPrefabResolver.cs` | Fix line 218: `pathRight && pathDown` → `ElbowUpLeft` — **FIXED** |
| 1.2 | `RoadPrefabResolver.cs` | Audit all elbow mappings in `ResolvePrefabForPathCell` against `SelectFromConnectivity`; ensure consistency |
| 1.3 | `RoadPrefabResolver.cs` | Add unit tests or debug validation: for each cell, verify resolved prefab exits match path neighbors |

**Acceptance:** East-to-North and similar turns produce a single correct elbow prefab.

---

### Phase 2 — Interstate pathfinding improvements (P1) — **complete (BUG-27, 2026-03-19)**

| Task | File | Description |
|------|------|-------------|
| 2.1 | `InterstateManager.cs` | Straightness / continuity in step costs — **done** (aligned with BUG-27 tuning) |
| 2.2 | `RoadPathCostConstants.cs` | Interstate slope / away-from-goal penalties — **done** |
| 2.3 | `InterstateManager.cs` | Dual-run A* (`avoidHighTerrain` true vs false), pick lower `ComputePathCost` — **done** (`PickLowerCostInterstateAStarPath`) |
| 2.4 | `InterstateManager.cs` / `RoadManager.cs` | Bridge approach — **done** (Rule F / shared bridge validation) |

**Acceptance:** Interstate paths are straighter and tend to avoid hills when a cheaper offset path exists (per cost model).

---

### Phase 3 — Cut-through robustness (P1) — **complete (BUG-29, 2026-03-19)**

| Task | File | Description |
|------|------|-------------|
| 3.1 | `PathTerraformPlan.cs` | Neighbor refresh waves for cut-through — **done** (Phase 3 second ring / waves) |
| 3.2 | `TerrainManager.cs` | One-step cliff walls toward cut corridor — **done** (`terraformCutCorridorCells`, `NeedsCutThroughOneStepCliffToCorridor`) |
| 3.3 | `TerraformingService.cs` | Tall-hill cut reject; map-edge margin; optional widen — **done** (see [archive/plan-cut-through-craters.md](archive/plan-cut-through-craters.md)) |

**Acceptance:** Cut-through no longer leaves voids at typical boundaries; invalid tall cuts rejected; interstate forbids cut-through trench.

---

### Phase 4 — Unify pathfinding (optional, P2)

| Task | File | Description |
|------|------|-------------|
| 4.1 | `GridPathfinder.cs` | Extract shared A* logic; add optional `straightnessBonus` and `avoidSlopes` parameters |
| 4.2 | `InterstateManager.cs` | Replace `RunInterstateAStar` with call to shared pathfinder (or a thin wrapper) with interstate-specific params |
| 4.3 | `RoadPathCostConstants.cs` | Add `GetStepCostForInterstate` or mode parameter to support different cost profiles |

**Acceptance:** Single pathfinding implementation with configurable behavior; less duplication.

---

### Phase 5 — Validation and testing (ongoing)

| Task | Description |
|------|-------------|
| 5.1 | Manual test: New Game → verify interstate has correct elbows, no zigzags at simple turns |
| 5.2 | Manual test: Map with hill between entry and bridge → verify path offsets to avoid hill when possible |
| 5.3 | Manual test: Cut-through path → verify no black holes |
| 5.4 | Add debug overlay (optional): draw resolved prefab type per cell during placement to catch mismatches early |

---

## 5. Dependency Order

```
Phase 1 (prefab fix) — no dependencies, do first
    ↓
Phase 2 (pathfinding) — independent of Phase 3
Phase 3 (cut-through) — independent of Phase 2
    ↓
Phase 4 (unify) — optional, after 2 and 3
```

---

## 6. Summary of Rules (Reference)

| ID | Rule |
|----|------|
| A | Elbow connectivity: prefab must match path neighbor directions |
| B | Path continuity: prefab exits = path in/out |
| C | Terraform wins: use plan’s `postTerraformSlopeType`, not live terrain |
| D | Environmental preference: prefer offset path to avoid hills |
| E | Straightness: interstate should prefer straight segments |
| F | Bridge approach: approach perpendicular to water, no turn at edge |

---

## 7. Backlog Integration

- **BUG-25** (manual street drawing): Phase 1 prefab fixes apply to manual draw as well.
- **BUG-23** (interstate flaky): New Game flow problem is a separate initialization/flow issue in GeographyManager/GameBootstrap.
- **BUG-26** (completed 2026-03-19): Phases 1–3 started; bridge approach (Rule F) and shared validation landed.
- **BUG-27** (completed 2026-03-19): Interstate pathfinding — endpoint ranking, dual A*, penalties (`RoadPathCostConstants`).
- **BUG-29** (completed 2026-03-19): Cut-through craters — reject tall cut, cliff corridor, map-edge guard, validation ring; see [archive/plan-cut-through-craters.md](archive/plan-cut-through-craters.md).
- **BUG-25** (completed): Manual street drawing — see `road-drawing-fixes.md` for optional polish items.
- **BUG-28**: Sorting order between slope cell and interstate cell — open.
- **BUG-30** (completed 2026-03-20): Slope/ramp prefabs when interstate (or manual road) climbs hills — corner and diagonal aligned with `GetPostTerraformSlopeTypeAlongExit`; see [archive/agent-prompt-interstate-slope-prefabs.md](archive/agent-prompt-interstate-slope-prefabs.md) (archived).
- **BUG-31**: Prefab selection at interstate border entry/exit — open.
