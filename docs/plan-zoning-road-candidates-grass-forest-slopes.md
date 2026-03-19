# Plan: Zoning and Road Candidates = Grass, Forest, and N-S / E-W Slopes

## Objective

Treat as valid candidates for **zone placement** and for **road expansion** not only Grass cells, but also:

- **Forest** (cell with forest)
- **Slopes** of type **north-south** or **east-west** (and Flat), when the terrain allows it.

This avoids over-restricting automatic growth to "Grass only" and aligns the logic with buildable terrain (zones and roads on Flat/N/S/E/W).

---

## 1. Definition of "zoneable/expandable cell"

- **Zoneable (zone candidate):**  
  `zoneType == Grass` **or** `HasForest()` **or** cell whose terrain is placeable for zone (Flat, North, South, East, West).  
  In practice: every cell that is Grass (with or without forest) is already zoneable; if in the future there were cells with another `zoneType` but N-S/E-W slope, include them with a helper that consults `TerrainManager.GetTerrainSlopeTypeAt`.

- **Expandable for road (valid neighbor of a road edge):**  
  Same idea: Grass, Forest, water (`GetCellInstanceHeight() == 0`), or terrain accepted by `TerrainManager.CanPlaceRoad` (already includes Flat, N, S, E, W and diagonals if the previous change is kept).

---

## 2. Changes by file

### 2.1 GridManager.cs

| Location | Change |
|----------|--------|
| **GetRoadEdgePositions** (lines 1863–1866) | Consider neighbor "expandable" if Grass **or** `n.HasForest()` **or** water (`n.GetCellInstanceHeight() == 0`). Replace current condition with something like: `(n.zoneType == Zone.ZoneType.Grass \|\| n.HasForest() \|\| n.GetCellInstanceHeight() == 0)`. |
| **CountGrassNeighbors** (lines 1873–1890) | Option A: Keep name and broaden criteria: count neighbors that are **zoneable** (Grass, HasForest, or slope Flat/N/S/E/W). Option B: Add `CountZoneableNeighbors(int gx, int gy)` that uses helper `IsZoneableNeighbor(Cell c)` (Grass \|\| HasForest \|\| slope Flat/N/S/E/W via `terrainManager.GetTerrainSlopeTypeAt`) and use this method in auto-zoning reserve. If Option B is used, `IsReservedForRoadExpansion` in AutoZoningManager must use `CountZoneableNeighbors` instead of `CountGrassNeighbors`. |

Recommendation: **Option A** to avoid changing the public signature or the name of the method used from AutoZoningManager; only broaden the count condition to "zoneable" (Grass, HasForest, and if `terrainManager != null` and slope is Flat/North/South/East/West, count as well).

---

### 2.2 AutoZoningManager.cs

| Location | Change |
|----------|--------|
| **GetCandidatesAdjacentToRoad** (lines 128–142) | Include cell if it is a zone candidate: **Grass, HasForest(), or zoneable terrain**. Add reference to `TerrainManager` (FindObjectOfType if needed). Local or private helper: `IsZoneableCandidate(Cell c, int x, int y)` → `c != null && (c.zoneType == Zone.ZoneType.Grass \|\| c.HasForest() \|\| IsSlopePlaceableForZone(x, y))`. `IsSlopePlaceableForZone(x, y)` uses `terrainManager.GetTerrainSlopeTypeAt(x, y)` and returns true for Flat, North, South, East, West. |
| **ProcessTick – cell check** (lines 86–91) | Replace current condition with same criteria: `IsZoneableCandidate(cell, p.x, p.y)` (or equivalent: Grass, HasForest, or slope N-S/E-O). |
| **IsReservedForRoadExpansion** | Continues using `CountGrassNeighbors` from GridManager; when GridManager counts "zoneable" (see 2.1), the reserve will remain coherent without changing this function. |

Note: If AutoZoningManager should not depend on TerrainManager, "zoneable slope" can be centralized in GridManager with a public method `IsCellZoneableTerrain(int x, int y)` that consults `terrainManager` and returns true for Flat/N/S/E/W; AutoZoningManager would only call that method in addition to Grass/HasForest.

---

### 2.3 AutoRoadBuilder.cs

| Location | Change |
|----------|--------|
| **IsCellPlaceableForRoad** (lines 434–442) | Already accepts Grass and HasForest(); no change needed. Effective road placement already goes through `TerrainManager.CanPlaceRoad`, which accepts Flat and N/S/E/W (and diagonals if kept). |
| **GetCellPlaceableRejectReason** (lines 446–460) | Optional: in the "zone not grass/water" case, clarify that Grass or Forest is accepted; the message can remain "zone not grass/water" or be "zone not grass/forest/water" as preferred. |
| **CountGrassNeighbors** (AutoRoadBuilder, line 471) | Used to prioritize edges with more "space". Optional: also count Forest for consistency: `(c.zoneType == Zone.ZoneType.Grass \|\| c.HasForest())`. |

Summary: In AutoRoadBuilder the logic for "which cell is placeable for road" is already aligned with Grass/Forest and terrain (via TerrainManager); only **GetRoadEdgePositions** in GridManager needs to consider Forest as an expandable neighbor (and optionally unify the count of "expandable" neighbors with Grass+Forest).

---

### 2.4 ZoneManager / GridManager – zone validation (PlaceZoneAt / canPlaceZone)

- **ZoneManager.PlaceZoneAt** calls `canPlaceZone(..., requireInterstate: false)`.
- **canPlaceZone** uses `gridManager.canPlaceBuilding(gridPosition, 1)`.
- **TryValidateBuildingPlacement** (GridManager) requires `zoneType == Grass` for each cell in the footprint.

If in the game **all** placeable cells are `zoneType == Grass` (with or without forest, with or without slope on terrain), this validation does not need to change. If in the future there were cells with another `zoneType` but N-S/E-W slope that should be zoneable, it would be necessary to:

- Add in GridManager a method like `IsCellValidForZonePlacement(int x, int y)` that returns true if `zoneType == Grass` **or** `HasForest()` **or** (terrain Flat/N/S/E/W per TerrainManager), and
- Make the validation branch for **size 1** (zone only) use that method instead of only `zoneType == Grass`.

Remains **optional / phase 2** in the plan.

---

## 3. Suggested implementation order

1. **GridManager**
   - GetRoadEdgePositions: add `n.HasForest()` to the expandable neighbor condition.
   - CountGrassNeighbors: broaden to "zoneable" (Grass, HasForest, and slope Flat/N/S/E/W via TerrainManager). Keep name or document that it now counts "zoneable".
2. **AutoZoningManager**
   - Add reference to TerrainManager (or use GridManager if `IsCellZoneableTerrain` is added there).
   - GetCandidatesAdjacentToRoad: include only cells that pass `IsZoneableCandidate` (Grass, HasForest, or slope N-S/E-O).
   - ProcessTick: same condition when checking the cell before placing zone.
3. **AutoRoadBuilder** (optional)
   - Internal CountGrassNeighbors: also count HasForest() to prioritize edges.
4. **Zone validation 1x1** (optional/phase 2)
   - Only if non-Grass zoneable cells are introduced; then use `IsCellValidForZonePlacement` in building validation for size 1.

---

## 4. Summary of unified criteria

- **Expandable neighbor for road (edge):** Grass **or** Forest **or** water (height 0). N-S/E-W slopes are already considered in TerrainManager.CanPlaceRoad when placing the tile.
- **Zone candidate:** Grass **or** HasForest() **or** terrain with slope Flat / North / South / East / West.
- **Reserve for road expansion:** Continue using the count of "zoneable" neighbors (after the change in CountGrassNeighbors/CountZoneableNeighbors) so as not to zone where the road should grow.

With this, both roads and zones consider **Grass, Forest, and north-south or east-west slopes** coherently throughout the automatic flow.

---

**Backlog:** FEAT-36 — Expand auto-zoning and auto-road candidates to include forests and slopes.
