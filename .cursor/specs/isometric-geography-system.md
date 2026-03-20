# Isometric Geography System — Technical Specification

> **Status:** Reference documentation
> **Audience:** AI agents and developers working on terrain, roads, water, sorting order, or any system that interacts with the isometric grid.
> **Related:** `ARCHITECTURE.md`, `.cursor/specs/water-system-refactor.md`, `.cursor/specs/road-drawing-fixes.md`

---

## 1. Isometric Grid Fundamentals

### 1.1 Coordinate System

The game uses a **diamond (isometric) projection** where logical grid coordinates `(x, y)` map to screen-space (world) positions:

```
worldX = (x - y) * (tileWidth / 2)
worldY = (x + y) * (tileHeight / 2) + heightOffset
```

| Constant | Value | Notes |
|----------|-------|-------|
| `tileWidth` | 1.0 | Full diamond width in world units |
| `tileHeight` | 0.5 | Diamond height (half of width due to isometric squash) |
| `heightOffset` | `(h - 1) * 0.25` | Vertical shift per height level above base (h=1) |

Grid origin `(0, 0)` is at the **bottom corner** of the diamond. The grid extends to `(width-1, height-1)`.

### 1.2 Direction Convention

Cardinal and diagonal directions map to grid deltas as follows. This convention is used consistently across `TerrainManager`, `TerraformingService`, `RoadPrefabResolver`, and `GridPathfinder`.

| Direction | Grid Delta (Δx, Δy) | Screen Appearance |
|-----------|---------------------|-------------------|
| **North** | `(+1, 0)` | Up-right ↗ |
| **South** | `(-1, 0)` | Down-left ↙ |
| **East** | `(0, -1)` | Down-right ↘ |
| **West** | `(0, +1)` | Up-left ↖ |
| **NorthEast** | `(+1, -1)` | Right → |
| **NorthWest** | `(+1, +1)` | Up ↑ |
| **SouthEast** | `(-1, -1)` | Down ↓ |
| **SouthWest** | `(-1, +1)` | Left ← |

> **Mnemonic:** `+x` = toward top-right of screen (North). `+y` = toward top-left (West). Grid x increases "into the screen" to the right; grid y increases "into the screen" to the left.

### 1.3 Inverse Conversion (World → Grid)

`GridManager.GetGridPosition(worldPoint)` converts screen clicks to grid coordinates:

```
posX = worldPoint.x / (tileWidth / 2)
posY = worldPoint.y / (tileHeight / 2)
gridX = round((posY + posX) / 2)
gridY = round((posY - posX) / 2)
```

For height-aware picking, `GetMouseGridCell` performs screen-space hit testing against neighboring cells (3×3 candidates) using sprite bounds, selecting the cell with the highest sorting order whose screen rect contains the cursor.

---

## 2. Height System

### 2.1 HeightMap

`HeightMap` is a plain C# class (not MonoBehaviour) storing an `int[width, height]` array. Each cell has an integer height in `[MIN_HEIGHT=0, MAX_HEIGHT=5]`.

| Height | Semantic |
|--------|----------|
| 0 | **Sea level** — water surface. `SEA_LEVEL = 0` |
| 1 | **Base land** — default land elevation. Most flat terrain is h=1 |
| 2–5 | **Elevated land** — hills, mountains, plateaus |

Height affects three things:
1. **World Y position** — each level adds `tileHeight / 2 = 0.25` world units upward
2. **Sorting order** — higher terrain renders on top of lower terrain at the same depth
3. **Slope detection** — any height difference to an 8-neighbor triggers slope prefab selection

### 2.2 Height Generation

The initial 40×40 map uses a **hardcoded template** (`GetOriginal40x40Heights()` in `TerrainManager`). For grids larger than 40×40, extended cells use **dual-octave Perlin noise** blended smoothly at the 40-cell border (`FillExtendedTerrainProcedural`). Procedural lakes (circular `h=0` patches) and rivers (downhill-flowing `h=0` paths) are stamped on the extended terrain.

### 2.3 Height Constraint

The terrain system enforces a **maximum height difference of 1** between any two cardinal neighbors for valid terrain. Greater differences are displayed with cliff walls but can cause visual artifacts (black voids). The `PathTerraformPlan.ValidateNoHeightDiffGreaterThanOne()` validation rejects terraform plans that would violate this constraint.

---

## 3. Terrain Slope Types

### 3.1 The TerrainSlopeType Enum

`TerrainSlopeType` (defined in `TerrainManager.cs`) has 13 values:

```
Flat,
North, South, East, West,                                   // 4 orthogonal (cardinal) slopes
NorthEast, NorthWest, SouthEast, SouthWest,                 // 4 diagonal slopes
NorthEastUp, NorthWestUp, SouthEastUp, SouthWestUp          // 4 corner (upslope) slopes
```

### 3.2 Naming Convention

**Slope names indicate the direction the slope faces (downhill)**:
- A `South` slope means the **North neighbor is higher** → the terrain slopes downhill toward the South.
- A `NorthEast` diagonal slope means the **SouthWest diagonal neighbor is higher**.
- A `SouthEastUp` corner slope means **both West AND North neighbors are higher** → the cell sits in a concave valley corner opening toward the southeast.

### 3.3 Slope Categories

#### 3.3.1 Flat
- **Condition:** All 8 neighbors have the same height as this cell, OR this cell is a local maximum (plateau — no neighbor is higher).
- **Visual:** Standard grass tile, no elevation change visible.
- **Prefab:** One of the `grassPrefabs` from `ZoneManager`.

#### 3.3.2 Orthogonal (Cardinal) Slopes — N, S, E, W
- **Condition:** Exactly one cardinal neighbor is higher; no two adjacent cardinals are both higher.
- **Visual:** A ramp connecting the lower cell to the higher neighbor along one axis. Occupies the full diamond tile but visually transitions one height level.
- **Placement:** The slope prefab is placed on the **lower cell** (the cell whose height is less than the neighbor's).
- **Road compatibility:** Roads can be placed on cardinal slopes using directional slope road prefabs.

| Slope | Higher Neighbor | Screen Ramp Direction |
|-------|----------------|----------------------|
| North | South `(x-1, y)` | Ramp goes down-left to up-right |
| South | North `(x+1, y)` | Ramp goes up-right to down-left |
| East | West `(x, y+1)` | Ramp goes up-left to down-right |
| West | East `(x, y-1)` | Ramp goes down-right to up-left |

#### 3.3.3 Diagonal Slopes — NE, NW, SE, SW
- **Condition:** No cardinal neighbor is higher, but exactly one diagonal neighbor is higher.
- **Visual:** An angled ramp connecting the cell diagonally to one corner. Visually a wedge-shaped terrain transition.
- **Road compatibility:** Roads can traverse diagonal slopes; `RoadPrefabResolver` selects the best orthogonal road prefab based on the road's travel axis relative to the diagonal.

| Slope | Higher Diagonal Neighbor |
|-------|--------------------------|
| NorthEast | SouthWest `(x-1, y+1)` |
| NorthWest | SouthEast `(x-1, y-1)` |
| SouthEast | NorthWest `(x+1, y+1)` |
| SouthWest | NorthEast `(x+1, y-1)` |

#### 3.3.4 Corner / Upslope Types — NEUp, NWUp, SEUp, SWUp
- **Condition:** Two **adjacent** cardinal neighbors are both higher (forming a concave corner).
- **Visual:** The cell sits in a valley between two ascending ridges. Visually a concave corner piece.
- **Road compatibility:** Roads can traverse corner slopes; `TerraformingService` derives the best orthogonal slope type based on the road's exit direction.

| Slope | Higher Pair | Valley Opens Toward |
|-------|------------|---------------------|
| SouthEastUp | West `(x, y+1)` AND North `(x+1, y)` | Southeast ↘ |
| NorthEastUp | West `(x, y+1)` AND South `(x-1, y)` | Northeast ↗ |
| SouthWestUp | East `(x, y-1)` AND North `(x+1, y)` | Southwest ↙ |
| NorthWestUp | East `(x, y-1)` AND South `(x-1, y)` | Northwest ↖ |

---

## 4. Slope Determination Algorithm

### 4.1 Land Slope Selection (`DetermineSlopePrefab`)

`TerrainManager.DetermineSlopePrefab(x, y)` determines which slope prefab to use for a land cell. It reads the 8-neighbor heights and applies a **priority cascade**:

1. **Corner/upslope check (highest priority):** If two adjacent cardinal neighbors are higher → return corner upslope prefab.
   - West+North higher → `SouthEastUp` prefab
   - West+South higher → `NorthEastUp` prefab
   - East+North higher → `SouthWestUp` prefab
   - East+South higher → `NorthWestUp` prefab

2. **Cardinal slope check:** If exactly one cardinal neighbor is higher → return cardinal slope prefab.
   - North higher → `South` slope (faces south = downhill)
   - South higher → `North` slope
   - East higher → `West` slope
   - West higher → `East` slope

3. **Diagonal slope check (lowest priority):** If no cardinal is higher but one diagonal is → return diagonal slope prefab.
   - NW higher → `SouthEast` slope
   - NE higher → `SouthWest` slope
   - SW higher → `NorthEast` slope
   - SE higher → `NorthWest` slope

4. **No match → returns null** (cell is a local maximum/plateau; uses flat grass).

### 4.2 Water Slope Selection (`DetermineWaterSlopePrefab`)

For land cells (h ≥ 1) adjacent to water (h = 0), `DetermineWaterSlopePrefab(x, y)` uses a similar but distinct decision tree. It checks which cardinal and diagonal neighbors are at sea level and selects the appropriate water-slope variant. Priority: border cases → cardinal water neighbors → combined cardinal patterns → diagonal-only water (upslope water variants).

### 4.3 `RequiresSlope` vs Slope Selection

`RequiresSlope(x, y, height)` returns true if **any** of the 8 neighbors has a different height. This determines whether the cell needs slope processing. However, `DetermineSlopePrefab` may still return null if the cell is a **plateau** (all neighbors are lower or equal) — in that case the cell gets flat grass even though `RequiresSlope` was true.

### 4.4 `GetTerrainSlopeTypeAt` (Public API)

Returns a `TerrainSlopeType` enum value using the same logic as `DetermineSlopePrefab`. Used by `ForestManager` (slope-aware tree placement), `TerraformingService`, `RoadPrefabResolver`, and road placement validation.

---

## 5. Geographic Elements

### 5.1 Flat Terrain (Plains)
- **HeightMap pattern:** Uniform height across a region (e.g., all cells at h=1).
- **Visual:** Standard grass tiles, no elevation transitions visible.
- **Gameplay:** Ideal for building placement (no constraints), road placement, and zoning.
- **Code:** `PlaceFlatTerrain(x, y)` instantiates a random grass prefab from `ZoneManager.grassPrefabs`.

### 5.2 Hills and Mountains
- **HeightMap pattern:** A region of cells at height h surrounded by cells at h-1. Concentric rings of decreasing height form multi-level hills (e.g., center at h=5, ring at h=4, ring at h=3, etc.).
- **Visual:** Elevated mass with slope transitions on all perimeter cells. The top is flat (plateau) if multiple cells share the peak height.
- **Structure:** A hill of height 3 on base terrain h=1 requires: center cells at h=3, a ring of h=2 around them, and a ring of h=1 that receives the slope prefabs facing the h=2 cells. Constraint: max |Δh|=1 between cardinal neighbors means hills must have gradual transitions.
- **Example from the hardcoded 40×40 map:** Rows 0–6 around column 22–28 form a hill peaking at h=5.

### 5.3 Orthogonal Hillside (Cardinal Slope Line)
- **HeightMap pattern:** A linear boundary where one row/column is at h and the adjacent row/column is at h+1.
- **Visual:** A continuous line of cardinal slope tiles (e.g., all `South` slopes) forming a ridge or escarpment along one axis.
- **Example:** A wall of `South` slope prefabs along x=10 where cells at x=10 are h=1 and cells at x=11 are h=2.

### 5.4 Diagonal Hillside
- **HeightMap pattern:** Height increases along a diagonal direction. Only the diagonal neighbor is higher; all cardinal neighbors are at the same height as the cell.
- **Visual:** A diagonal wedge tile that connects two height levels at a 45° angle to the grid axes.
- **Rarity:** Less common than cardinal slopes. Occurs at hill corners where the slope transitions from one axis to another.

### 5.5 Concave Corners (Upslope / Valley)
- **HeightMap pattern:** A cell where two perpendicular cardinal neighbors are both higher. This creates a "valley corner" — the cell is at the inner junction of two ascending ridges.
- **Visual:** An L-shaped concavity opening away from the two higher neighbors.
- **Example:** Cell at h=1 with North neighbor at h=2 and West neighbor at h=2 → `SouthEastUp` prefab.

### 5.6 Convex Corners (Diagonal Slope at Hill Corner)
- **HeightMap pattern:** A cell where only one diagonal neighbor is higher, and no cardinal neighbor is higher. This is the outer corner of a hill.
- **Visual:** A small wedge connecting the diagonal height transition.
- **Example:** Cell at h=1, only NW diagonal at h=2 → `SouthEast` diagonal slope prefab.

### 5.7 Cliffs
- **HeightMap pattern:** Cardinal neighbor height difference > 1 (e.g., cell at h=3, south neighbor at h=1).
- **Visual:** Vertical cliff wall prefabs rendered on the cell's edge facing the lower neighbor. Multiple cliff directions can stack on one cell.
- **Code:** `PlaceCliffWalls(x, y)` checks each cardinal direction. `NeedsCliffWallSouth/East/North/West` returns true when the drop exceeds 1 height level.
- **Special case:** Land-to-water transitions at h=1 also get cliff walls when a higher neighbor (h=2) is behind them.

### 5.8 Coastal Transitions (Water Slopes)
- **HeightMap pattern:** Land cell (h ≥ 1) adjacent to water cell (h = 0).
- **Visual:** Special water-slope prefabs that visually transition from land elevation down to sea level. The cell's logical height stays at 1 (so game systems treat it as land) but the slope sprite is rendered at sea-level world position.
- **Constraint:** Normal roads cannot be placed on water-slope cells (`IsWaterSlopeCell` returns true). This enforces a 1-cell buffer between roads and coastlines. Water plants can be placed on coastal slopes.

### 5.9 Bays
- **HeightMap pattern:** Concave water corners where water surrounds a land cell diagonally.
- **Visual:** NE/NW/SE/SW bay prefabs that render a rounded coastal indent.

### 5.10 Cut-Through Corridors
- **HeightMap pattern:** A path of cells flattened to base height through a hill by the terraforming system.
- **Visual:** A trench with cliff walls on the sides where the terrain drops from the surrounding hill height to the flattened path height. Created when roads are placed through terrain with consecutive height differences > 1.
- **Code:** `PathTerraformPlan.isCutThrough = true` when path has consecutive |Δh| > 1. `BuildTerraformCutCorridorSet()` tracks lowered cells for cliff wall generation.

### 5.11 Sea Level Water
- **HeightMap pattern:** Cells at h=0.
- **Visual:** Animated water tile (`seaLevelWaterPrefab`).
- **Gameplay:** Water cells can have bridges placed over them. Buildings cannot be placed directly on water (except water plants, which allow water in their footprint).

---

## 6. Prefab Inventory

### 6.1 Land Slope Prefabs (12)

Assigned in `TerrainManager` Inspector fields:
- `northSlopePrefab`, `southSlopePrefab`, `eastSlopePrefab`, `westSlopePrefab`
- `northEastSlopePrefab`, `northWestSlopePrefab`, `southEastSlopePrefab`, `southWestSlopePrefab`
- `northEastUpslopePrefab`, `northWestUpslopePrefab`, `southEastUpslopePrefab`, `southWestUpslopePrefab`

### 6.2 Water Slope Prefabs (12)

Same 12 patterns with water visual treatment:
- `{direction}SlopeWaterPrefab` for each direction
- `{direction}UpslopeWaterPrefab` for each corner

### 6.3 Infrastructure Prefabs

| Prefab | Purpose |
|--------|---------|
| `seaLevelWaterPrefab` | Animated water tile at h=0 |
| `southCliffWallPrefab` | Cliff face on south edge |
| `eastCliffWallPrefab` | Cliff face on east edge |
| `northCliffWallPrefab` | Cliff face on north edge |
| `westCliffWallPrefab` | Cliff face on west edge |
| `northEastBayPrefab` | Concave coastal corner (NE) |
| `northWestBayPrefab` | Concave coastal corner (NW) |
| `southEastBayPrefab` | Concave coastal corner (SE) |
| `southWestBayPrefab` | Concave coastal corner (SW) |

### 6.4 Slope Variant Naming Convention (SlopePrefabRegistry)

For zoning overlays and building sprites that need slope-aware variants:

```
{flatPrefabName}_{slopeCode}Slope
```

Where `slopeCode` is: `N`, `S`, `E`, `W`, `NE`, `NW`, `SE`, `SW`, `NEUp`, `NWUp`, `SEUp`, `SWUp`.

Example: `ResidentialLight_NSlope` is the north-slope variant of the `ResidentialLight` zoning prefab.

`SlopePrefabRegistry.GetSlopeVariant(flatPrefab, slopeType)` looks up the variant by constructed name. Returns `null` if no variant exists, in which case the flat prefab is used or placement is rejected.

---

## 7. Sorting Order System

### 7.1 Formula

```csharp
sortingOrder = TERRAIN_BASE_ORDER + depthOrder + heightOrder + typeOffset
```

Where:
- `TERRAIN_BASE_ORDER = 0`
- `depthOrder = -(x + y) * DEPTH_MULTIPLIER` (`DEPTH_MULTIPLIER = 100`)
- `heightOrder = height * HEIGHT_MULTIPLIER` (`HEIGHT_MULTIPLIER = 10`)
- `typeOffset` varies by object type

### 7.2 Type Offsets

| Object Type | Offset | Notes |
|-------------|--------|-------|
| Terrain (flat grass) | 0 | Base |
| Land slope | +1 (`SLOPE_OFFSET`) | Slightly in front of terrain |
| Water slope | +1 | Above sea-level water |
| Road | +5 | Above terrain and slopes |
| Utility | +8 | Above roads |
| Building | +10 | Above everything on same cell |
| Effect | +30 | Topmost layer |

### 7.3 Design Rationale

- `DEPTH_MULTIPLIER (100) > HEIGHT_MULTIPLIER (10) * MAX_HEIGHT (5) = 50` ensures that **depth (distance from camera)** dominates over **height**. A hilltop at the back of the map never draws over foreground forest/terrain.
- `HEIGHT_MULTIPLIER = 10` is chosen to be less than `DEPTH_MULTIPLIER / MAX_HEIGHT` so depth always wins.
- Negative depth order means cells farther from camera (higher `x+y`) have lower sorting order (drawn first / behind).

---

## 8. Terraforming System

### 8.1 Overview

When roads cross sloped terrain, the `TerraformingService` computes a `PathTerraformPlan` that describes how to modify terrain so the road can be placed. Two strategies exist:

### 8.2 Scale-with-Slopes Mode

**Condition:** All consecutive path cells have `|Δh| ≤ 1`.

The road "climbs" the terrain using slope road prefabs. No terrain modification is needed; the terraform plan records `TerraformAction.None` for most cells and sets `postTerraformSlopeType` to guide `RoadPrefabResolver` in selecting the correct slope road prefab.

### 8.3 Cut-Through Mode

**Condition:** At least one pair of consecutive path cells has `|Δh| > 1`.

The road cannot climb gradually, so the terrain is **flattened** along the path to `baseHeight` (minimum height on the path). This creates a corridor with cliff walls on the sides.

Phases:
1. **Phase 1:** Write target heights to the heightmap for all flatten cells.
2. **Validation:** `ValidateNoHeightDiffGreaterThanOne()` checks no cardinal neighbors exceed |Δh|=1 after the planned changes. If failed, the plan reverts.
3. **Phase 2:** `RestoreTerrainForCell` with `forceFlat`/`forceSlopeType` flags refreshes terrain visuals.
4. **Phase 3:** Refresh 8-neighbors (2 waves for cut-through, 1 wave for normal) so adjacent slopes/cliffs update.

### 8.4 Diagonal Step Expansion

`ExpandDiagonalStepsToCardinal()` converts diagonal path steps into two orthogonal steps, since road prefabs only support cardinal movement. This ensures the terraform plan and road prefab resolution receive pure orthogonal segments.

### 8.5 Terraform Actions

| Action | Description |
|--------|-------------|
| `None` | No terrain change; cell keeps its current height |
| `Flatten` | Set cell height to `plan.baseHeight`; terrain becomes flat |
| `DiagonalToOrthogonal` | (Legacy / obsolete) Convert diagonal slope to orthogonal |

---

## 9. Road Prefab Selection on Terrain

### 9.1 RoadPrefabResolver

Centralized road prefab selection. Three entry points:

1. **`ResolveForPath(path, plan)`** — Full path context. Uses `postTerraformSlopeType` from the terraform plan to select slope road prefabs. Handles elbows at turns, T-intersections, and crossings.
2. **`ResolveForCell(curr, prev)`** — Single cell with neighbor connectivity. Used for `RefreshRoadPrefabAt` after demolition or road changes.
3. **`ResolveForGhostPreview(gridPos)`** — Single cell for cursor preview.

### 9.2 Road Prefab Types

| Prefab | When Used |
|--------|-----------|
| `roadTilePrefab1` | Vertical straight (grid-y axis) |
| `roadTilePrefab2` | Horizontal straight (grid-x axis) |
| `roadTilePrefabNorthSlope` | Road on North-facing slope |
| `roadTilePrefabSouthSlope` | Road on South-facing slope |
| `roadTilePrefabEastSlope` | Road on East-facing slope |
| `roadTilePrefabWestSlope` | Road on West-facing slope |
| `roadTilePrefabElbow*` | 4 elbow variants (turns) |
| `roadTilePrefabTIntersection*` | 4 T-intersection variants |
| `roadTilePrefabCrossing` | 4-way crossing |
| `roadTileBridgeHorizontal` | Bridge over water (horizontal) |
| `roadTileBridgeVertical` | Bridge over water (vertical) |

### 9.3 Slope Road Prefab Mapping

The slope prefab is named after the **slope face direction** (downhill), matching `TerrainSlopeType`:

| `postTerraformSlopeType` | Road Prefab |
|--------------------------|-------------|
| `North` | `roadTilePrefabNorthSlope` |
| `South` | `roadTilePrefabSouthSlope` |
| `East` | `roadTilePrefabEastSlope` |
| `West` | `roadTilePrefabWestSlope` |
| Corner slopes (`NEUp`, etc.) | Decomposed to the orthogonal axis aligned with travel direction |

---

## 10. Pathfinding Cost Model

`GridPathfinder` uses A* with costs from `RoadPathCostConstants`:

| Terrain | Base Cost | Notes |
|---------|-----------|-------|
| Flat | 1 | Strongly preferred |
| Diagonal slope | 35 | Moderate penalty |
| Cardinal slope | 60 | High penalty |
| Height diff = 1 | +25 | Additional penalty for climbing |
| Water slope | 500 | Nearly impassable (road buffer) |
| Height diff > 1 | ∞ (impassable) | Cannot step directly |

Interstate pathfinding multiplies slope costs by `InterstateSlopeMultiplier = 5` and adds penalties for turns (`InterstateTurnPenalty = 5`), zigzags (`InterstateZigzagPenalty = 500`), and moving away from goal (`InterstateAwayFromGoalPenalty = 18`). Straight continuation gets a bonus (`InterstateStraightnessBonus = 15`).

---

## 11. Code Reference Map

| Concept | Primary File(s) | Key Methods |
|---------|----------------|-------------|
| Height data | `HeightMap.cs` | `GetHeight`, `SetHeight`, `IsValidPosition` |
| Slope type determination | `TerrainManager.cs` | `DetermineSlopePrefab`, `GetTerrainSlopeTypeAt` |
| Water slope determination | `TerrainManager.cs` | `DetermineWaterSlopePrefab`, `IsWaterSlopeCell` |
| Cliff walls | `TerrainManager.cs` | `PlaceCliffWalls`, `NeedsCliffWall{N,S,E,W}` |
| Terrain tile placement | `TerrainManager.cs` | `PlaceFlatTerrain`, `PlaceSlopeFromPrefab`, `PlaceWaterSlope` |
| Sorting order | `TerrainManager.cs` | `CalculateTerrainSortingOrder`, `CalculateSlopeSortingOrder` |
| Full sort recalculation | `GeographyManager.cs` | `ReCalculateSortingOrderBasedOnHeight` |
| Initialization orchestration | `GeographyManager.cs` | `InitializeGeography` |
| Grid ↔ world conversion | `GridManager.cs` | `GetGridPosition`, `GetWorldPositionVector`, `GetMouseGridCell` |
| Terraform planning | `TerraformingService.cs` | `ComputePathPlan`, `ExpandDiagonalStepsToCardinal` |
| Terraform apply/revert | `PathTerraformPlan.cs` | `Apply`, `Revert`, `TryValidatePhase1Heights` |
| Road prefab selection | `RoadPrefabResolver.cs` | `ResolveForPath`, `ResolveForCell` |
| Slope overlay naming | `SlopePrefabRegistry.cs` | `GetSlopeVariant`, `GetSlopeSuffix` |
| Pathfinding costs | `RoadPathCostConstants.cs` | `GetStepCost`, `GetStepCostForInterstate` |
| A* pathfinding | `GridPathfinder.cs` | `FindPath`, `FindPathWithRoadSpacing` |
