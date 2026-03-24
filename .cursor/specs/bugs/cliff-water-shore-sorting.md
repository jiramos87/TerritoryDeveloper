# Cliff / water-shore / sorting — findings (SS1–SS5)

> **Status:** Active engineering notes (screenshots + code review, 2026-03-23). **Backlog:** umbrella **[BUG-33](../../BACKLOG.md)**. ~~**[BUG-39](../../BACKLOG.md)** (bay / cliff placement + art)~~ and ~~**[BUG-40](../../BACKLOG.md)** (cliff vs foreground water sorting)~~ **completed** 2026-03-24 — see **Resolved (2026-03-24)** below.

## Glossary (stable vocabulary)

| Term | Meaning |
|------|---------|
| **Water surface / open water** | Registered water cell: tile from `WaterManager` / `WaterMap`, sorted using **visual surface height** (FEAT-37). |
| **Water-shore (ramp)** | Land cell that passes the **surface-height gate** (§4.2 in `isometric-geography-system.md`): gets **water-slope** prefabs (`DetermineWaterShorePrefabs` / `PlaceWaterShore`), not a vertical “cliff” read from height diff alone. |
| **Rim** | Land **above** the shore cap (typically **≥** two steps above adjacent water surface): uses **normal slopes** + **`PlaceCliffWalls`** toward lower neighbors, not water-shore art. |
| **Cliff face / cliff wall stack** | One or more **child** prefabs on the **higher** cell, placed along the **shared cardinal edge** toward a lower neighbor (`PlaceCliffWallStack`). **Δh > 1** produces multiple segments. |
| **Bay (shore prefab)** | Inner-corner / diagonal water pattern: **Bay** NE/NW/SE/SW prefabs (see §5.9 isometric spec). Cardinal cliff stacks use tunable placement on `TerrainManager` (**BUG-39** completed); inner-corner bay art may still need polish under **BUG-33**. |
| **Visible cliff faces (camera)** | Only **south** and **east** cardinal faces get prefab meshes (`IsCliffCardinalFaceVisibleToCamera`); **N/W** skip instantiation but `Cell.cliffFaces` can still record bits. |

## Scope

Observed issues when combining **depression-fill lakes**, **water-shore prefabs**, **cliff wall stacks**, and **pure land** (no water). Related code: `TerrainManager` (`UpdateTileElevation`, `PlaceCliffWalls`, `PlaceCliffWallStack`, `PlaceWaterShore`, shore gate), `WaterManager` / `WaterMap`, `GridManager` sorting.

### Resolved (2026-03-24)

- **BUG-39:** Cliff wall world placement vs sprite art — **`TerrainManager`** exposes serialized **south/east face nudges** (fractions of `tileWidth` / `tileHeight`) and **`cliffWallWaterShoreYOffsetTileHeightFraction`** when the cell uses a water-shore primary prefab, so `GetCliffWallSegmentWorldPositionOnSharedEdge` / `PlaceCliffWallStack` align art with the diamond edge after textures were repositioned. Bay-specific gaps, if any, remain under **BUG-33**.
- **BUG-40:** **`GetMaxCliffSortingOrderFromForegroundWaterNeighbors`** — scans the 8-neighbor ring for registered water with **lower** isometric depth (`nx+ny < highX+highY`) and returns `min(neighbor.sortingOrder - 1)`. Each cliff segment sets `finalSort = min(computedSort, maxCliffSort, maxSortFromForegroundWater - visualIndex)` so stacks stay below foreground water.

### Open defects (tracked in backlog)

- **[BUG-33](../../BACKLOG.md)** — Umbrella shore / lake-edge polish (tiles, gaps, sorting edge cases not covered above).

---

## SS1 — Cliffs on gentle shores / “on water”

**Symptoms (screenshots):** Thin brown cliff sprites on lake borders where the transition is a **slope** (water-shore / grass slope), not a vertical drop; cliffs appearing to sit **on** the water plane or wrong depth.

**Likely causes (code):**

1. **`PlaceCliffWalls` after `PlaceWaterShore`** on the same cell (`UpdateTileElevation` / `RestoreTerrainForCell`): the shore tile already gets water-slope art; **additional** cliff children can duplicate the “wall” read or sit at the wrong Y vs water sorting.
2. **`GetCliffWallDrop*`** can return **> 0** toward a neighbor that is **water or a 1-step shore**, while the **visual** face is already a ramp (`DetermineWaterShorePrefabs`). Cardinal **height diff** does not distinguish “ramp tile” vs “vertical rim.”
3. **Cliffs parented to the land cell** but positioned with `GetWorldPositionVector` toward **water/low floor** can intersect the **water tile** (different sorting layer / order), reading as “under” or “on” water.

**Direction for fix:** `ShouldSuppressCliffFaceTowardLowerCell` suppresses **only one-step** drops toward water / water-shore; **Δh ≥ 2** (riscos) still stack cliffs on **visible** (south/east) faces. Cardinal geometry uses `CliffCardinalFace`; north/west faces skip prefab instantiation. The old `NeedsCliffWallOneStepAboveWaterSlopeNeighbor` path was removed.

---

## SS2 / SS4 — Stacked cliffs not visibly stepped

**Symptoms:** Two `CliffSouthPrefab(Clone)` children (correct count for Δh = 2) but **same apparent height** in the scene / hierarchy confusion.

**Code observation — `PlaceCliffWallStack`:**

- Each segment uses `Vector2.Lerp(topCenter, bottomCenter, edgeBlend)` with **`edgeBlend = 1.0f`** → world position is always **bottomCenter** of the segment, not an intermediate point along the face.
- For multi-step stacks, consecutive segments may end up with **world positions that are too close** or **aligned** if `GetWorldPositionVector` for adjacent height indices on the same/different cells collapses visually (isometric + small `tileHeight` step).
- **Sorting:** `CalculateTerrainSortingOrder(highX, highY, topH) + SLOPE_OFFSET + s` — if sprites overlap in screen space, order alone may not separate them.

**Direction for fix:** Per-segment **explicit Y (and X) offset** for each height band (e.g. step by `tileHeight * 0.25` per level), or lerp with `edgeBlend` **&lt; 1** for inner segments; verify prefab **pivot** (bottom vs center).

---

## SS5 — Sorting, exterior land cliffs, cliffs “under” water

**Symptoms:** Z-fighting / wrong draw order on cliff sprites; missing or inconsistent cliffs on **pure land** escarpments; cliff faces **below** water plane.

**Likely causes:**

1. **Sorting:** Cliff `SpriteRenderer.sortingOrder` uses terrain formula on **high cell** coords + `SLOPE_OFFSET + s`. Water uses **surface − 1** visual height (`WaterManager.PlaceWater`). Same cell edge can interleave incorrectly with **neighbor water** tiles.
2. **Exterior land:** Drops toward **dry** lower land rely on the same `GetCliffWallDrop*` rules; **1-step** drops often return **0** unless cut-through / water-slope special cases — can explain **missing** cliffs on pure hills.
3. **Under water:** If a cliff is parented to a cell that is **above** water in grid space but the **sprite** extends into the water cell’s screen rect, it draws **behind** water or **through** it depending on order.

**Direction for fix:** Dedicated **sorting bucket** for cliff faces vs water vs shore; optional **masking** or **split render** for lake edges; audit **1-step** land–land cliff rules for non–water-slope neighbors.

---

## SS3 — Template height grid (bowl)

The **11×11** excerpt (rings 3 → 1 → 0) encodes **Δh = 2** between **3** and **1** on cardinals. That violates the “soft” |Δh|≤1 guideline in specs but is **intentional** for cliff stacks. **Stacking math** (SS2) and **shore vs cliff** choice (SS1) must be consistent on these borders.

---

## Debugging — instrumented cells

On **`TerrainManager`** enable **`Terrain Debug Log Cells Enabled`**. Default traced coordinates:

| (x, y)   |
|----------|
| (28, 24) |
| (28, 25) |
| (34, 24) |
| (34, 25) |

Logs include: heightmap height, `Cell` prefab names, shore eligibility, `RequiresSlope` / `GetTerrainSlopeTypeAt`, cardinal + diagonal neighbors, water flags + surface heights, **cliff drops** `S_down`, `E_down`, `S_towardHigherSouth`, `E_towardHigherEast` (visible faces only; no N/W prefabs), **per-segment** `PlaceCliffWallStack` world `Vector2` and sorting order, and **child list** under the cell (names + sprite sort).

Filter console: **`[TerrainDebug]`**.

---

## Mouse / “missing flat grass in hierarchy”

If **`PlaceFlatTerrain`** destroys children and re-instantiates grass as a **child** with `Zone`, the **root** `Cell` object may still have an **empty** or **non-grass** root `SpriteRenderer` depending on pipeline. **Picking** uses `GetMouseGridCell` + sorting — mis-clicks on steep stacks are expected until sorting/cliff placement is stable. Use the logs above to compare **heightMap** vs **cell.height** vs **children** for the four coordinates.

---

## References

- `TerrainManager.cs` — `terrainDebugLogCellsEnabled`, `TerrainDebugCellCoordinates`, `BuildTerrainDebugCellReport`, `PlaceCliffWallStack`
- `.cursor/specs/isometric-geography-system.md` — §4.2, §5.6.1, §5.7–5.9 (cliff + shore + bay)
- `.cursor/specs/water-system-refactor.md` — FEAT-37 water visual height
- **[BUG-33](../../BACKLOG.md)** · ~~**[BUG-39](../../BACKLOG.md)**~~ / ~~**[BUG-40](../../BACKLOG.md)**~~ (completed 2026-03-24)
