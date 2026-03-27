# Isometric Geography System — Technical Specification

> **Status:** **Canonical specification** for isometric geography in this project (single source of truth for definitions and mechanisms listed in §0.1). Keep it aligned with `TerrainManager`, `WaterManager`, `GridManager`, and related helpers; prefer updating this file over scattering duplicate rules.
> **Audience:** AI agents and developers working on terrain, roads, water, sorting order, or any system that interacts with the isometric grid.
> **Related:** `ARCHITECTURE.md` (persistence pipeline summary, init order). **Shore / cliff / water–water cascades:** **[BUG-42](../../BACKLOG.md)** completed 2026-03-26 (merged **BUG-33** + **BUG-41**). **Multi-body junction merge, bed alignment, cascades:** **[BUG-45](../../BACKLOG.md)** — rules in **§12.7** / **§5.6.2**; implementation plan **[`docs/water-junction-merge-implementation-plan.md`](../../docs/water-junction-merge-implementation-plan.md)**. **Save/load building sorting:** §7.4; **[BUG-34](../../BACKLOG.md)** / **[BUG-35](../../BACKLOG.md)** completed 2026-03-22. **Cliff art alignment + foreground-water sort cap:** **[BUG-39](../../BACKLOG.md)** / **[BUG-40](../../BACKLOG.md)** completed 2026-03-24.

## 0. Canonical scope and doc hierarchy

### 0.1 What this spec owns

- Grid ↔ world math, direction naming, and neighbor deltas (§1).
- `HeightMap` semantics, sync with `Cell.height`, water **surface** height vs **visual** placement (§2).
- Land slope types and `DetermineSlopePrefab` / `GetTerrainSlopeTypeAt` (§3–4.1).
- Water-shore eligibility, neighbor tests used in code, and `DetermineWaterShorePrefabs` decision order (§4.2, §5.8–5.9).
- Layered model: open water, water-shore art, cliff stacks, suppression rules (§5.6–5.7).
- Terrain-related **sorting** constants and formulas; load/save visual ordering summary (§7).
- Terraform modes affecting height (§8), roads on slopes (§9), pathfinding costs (§10).
- Code → concept map (§11).
- Water map, lake generation, persistence (§12), procedural rivers (§13), road/interstate/bridge validation (§14), lake-edge engineering notes (§15).

### 0.2 What lives elsewhere

| Concern | Document |
|---------|----------|
| UI design system (HUD, toolbar, components) | `docs/ui-design-system-project.md`, `docs/ui-design-system-context.md`, `.cursor/specs/ui-design-system.md` |

**`.cursor/specs/` policy:** Only **long-lived system specs** live here (`isometric-geography-system.md`, `ui-design-system.md`). Do not add bug write-ups, agent prompts, or one-off fix plans — those belong in `BACKLOG.md` while open and are **deleted after completion** to avoid stale context (see `AGENTS.md`).

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
| 0 | **Sea level** — legacy label for water at lowest index; `SEA_LEVEL = 0`. Registered **lake/river/sea** bodies use **per-body surface height** from `WaterMap` / `WaterBody` (not a single global surface). |
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

**Depression-fill bowls and procedural lakes:** Intentional **lake basins** may use **|Δh| > 1** across the bowl wall in some templates; those cases rely on **cliff stacks** (`PlaceCliffWallStack`) and shore rules rather than rejecting the height grid. Do not assume every map satisfies the soft guideline everywhere.

### 2.4 Height authority and procedural water (rivers / lakes)

During **initial geography generation** (rivers and lakes), **`HeightMap` is the source of truth** for integer terrain height at the point each procedural step runs. **`Cell.height`** at `(x, y)` **must stay in sync** with `HeightMap[x, y]` whenever either is written by terraform or water-related carving.

**Water surface height** (per `WaterBody` / river segment) is the **terrain height that hosts that water** at that location after coherent carving and terraform: bowl or channel floor and body registration must agree with **FEAT-37** visuals (`WaterManager`, `WaterMap`; see §12). When a **lake fallback** or similar path terraform raises or lowers terrain so a body sits on a consistent surface, **both** `HeightMap` and **`Cell`** data **must** be updated together — no divergent “display only” height on the cell.

**Fallback coherence:** Artificial lake fallback and forced river paths must update the **same height fields** as depression-fill paths so downstream logic (shore selection, `GetCliffWallDrop*`, `PlaceCliffWalls`) sees one neighborhood.

**Known pitfall ([BUG-42](../../BACKLOG.md)):** **Border corner cells** (concave/convex) of a fallback lake can incorrectly remain at an **elevated neighbor** height instead of the **lake surface** height, breaking shore and cliff prefab selection. Terraform must leave **`HeightMap` and `Cell.height`** consistent with the intended **surface** at those cells.

#### 2.4.1 Shore band height coherence (water-adjacent land)

Land cells that border water must stay on the **same vertical band** as that body’s surface so `PlaceWaterShore` can align the cell transform with the water visual (FEAT-37: logical surface **`S`**, water art uses **`V = max(MIN_HEIGHT, S − 1)`**). **Plateau** heights inherited from pre-carve terrain must **not** remain on those cells when they sit in the **Moore shore ring** (dry cells with at least one water neighbor).

**Invariant (lakes / sea / rivers):** For any dry cell **Moore-adjacent** to water, **`HeightMap` must not exceed** the **minimum logical surface** among its adjacent water cells unless intentional rim design elsewhere requires it. In practice, **`TerrainManager.RefreshLakeShoreAfterLakePlacement`** runs **`ClampShoreLandHeightsToAdjacentWaterSurface`**: if **`h > min(S)`** over adjacent water, **`HeightMap`** is **lowered** to **`min(S)`** (never raised). That matches artificial-lake diagonal rim coercion (`WaterMap.CoerceDiagonalCornerRimForArtificialLake`) and generalizes refresh after procedural rivers.

**Prefab gate:** **`IsLandEligibleForWaterShorePrefabs`** requires **`h ≤ V + MAX`** (default **`MAX = 1`**, **`TerrainManager.MAX_LAND_HEIGHT_ABOVE_ADJACENT_WATER_SURFACE_FOR_SHORE_PREFABS`**) so water-shore art is not selected for land **two or more** steps above the water **visual** reference. Higher cells use **ordinary slopes + `PlaceCliffWalls`** (rim).

**Rivers:** Along a run with one carved bed height and one logical surface, shore dry cells at that run should share one **bank** height (see §13.4–13.5); the clamp plus §13.5 promotion keep **`HeightMap`** consistent with **`PlaceWaterShore`** / body assignment.

### 2.5 Minimal neighbor refresh after lake shore

After **`RefreshLakeShore`** (or equivalent) updates shore tiles for a water body, **recompute** the affected **shore land cell(s)** and, for each, **audit only the single land neighbor** immediately **outward** from the water along the relevant cardinal (away from the water — **not** a full Moore-neighborhood ring). Expand the refresh radius only if a verified bug requires it.

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
- **Road compatibility:** Roads can traverse corner slopes; `TerraformingService` maps the cardinal ramp type from **path travel and segment height** (`GetPostTerraformSlopeTypeAlongExit`), same as diagonal slopes — not a separate corner-only heuristic (**BUG-30**, scale-with-slopes).

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

### 4.2 Water Slope Selection (`DetermineWaterShorePrefabs`)

Water-shore prefabs are used only when the land cell passes the **surface-height gate** in `TerrainManager` (`IsLandEligibleForWaterShorePrefabs`): among 8 neighbors, some water/sea cell exists whose logical surface **`S`** yields a **visual reference** **`V = max(MIN_HEIGHT, S − 1)`** (same as `WaterManager.PlaceWater`) such that **`h ≤ V + MAX_LAND_HEIGHT_ABOVE_ADJACENT_WATER_SURFACE_FOR_SHORE_PREFABS`** (default **1**). This is stricter than comparing only to **`S + 1`** on logical surface and matches **§2.4.1** (shore band height coherence). **Higher rim** land (e.g. flat grass one step above a shore tile, not Moore-adjacent to open water) **does not** pass that gate: it uses **ordinary terrain + `PlaceCliffWalls`** instead of water-shore art. The **same gate** drives **one-step cliff suppression** toward water / `IsWaterSlopeCell` (see §5.6.1 / §5.7): suppression applies only on **shore-band** cells; **rim plateaus** keep a **visible** cliff segment (south/east) toward the lower shore/water cell so the vertical face is not left empty (BUG-42).

**Neighbor tests (two layers):**

1. **Surface-height gate** (`IsLandEligibleForWaterShorePrefabs`, `TryGetSurfaceHeightForWaterNeighbor`): uses `WaterManager.IsWaterAt` / `GetWaterSurfaceHeight` where registered; cells at `HeightMap == SEA_LEVEL` without registration still yield a surface for eligibility. This gate decides **shore-band vs rim** (whether water-shore prefabs may run at all).
2. **Shore pattern detection** inside `DetermineWaterShorePrefabs` uses **`WaterOrSeaAt`**: true if `HeightMap` is `SEA_LEVEL` **or** `WaterManager.IsWaterAt`. Pattern bits can therefore differ from “registered water only” when terrain is `h=0` but not yet in `WaterMap` — a known integration edge case for generators and refresh order.

For eligible land cells, `DetermineWaterShorePrefabs(x, y)` walks a **fixed priority list** (first match wins):

1. **Map-edge shortcuts** — if the cell is on the grid border, pick a cardinal water-slope prefab from whichever border branch applies (avoids missing neighbors).
2. **Perpendicular cardinal corners (two wet cardinals)** — e.g. South + East both water. Chooses **Bay** vs corner `*SlopeWaterPrefab` using `IsAxisAlignedRectangleCornerWater*` on the diagonal water cell (true = **axis-aligned rectangle outer corner**: no water “beyond” that diagonal along the two outward cardinals) and `HasLandSlopeIgnoringWater` (any **non-water** cardinal higher than this cell → prefer Bay when assigned, else corner slope). If not a rectangle corner and no land-slope signal, prefers **corner `*SlopeWaterPrefab`** over Bay (peninsula / non-rectangular water). When **exactly three** cardinals are water, **two** perpendicular pairs both match: the implementation picks the inner corner using **diagonal water** — if only one of the two candidate diagonals is wet, that quadrant wins; if both or neither, tie-break matches the legacy pairwise order (missing North → try SE then SW; missing South → NE then NW; missing East → SW then NW; missing West → SE then NE).
3. **Single cardinal water** — branches for **East, then West, then North, then South**, each returning cardinal ramps or `*UpslopeWaterPrefab` combinations when the opposite cardinals are also water in specific patterns. For **North** (resp. **South**) with **no** South (resp. North) opposite strip and **no** East/West cardinal water, the choice is always **pure** `northSlopeWaterPrefab` / `southSlopeWaterPrefab` — including beside rectangular lake corners, where only one of the two water-side diagonals is wet. **`BuildDiagonalOnlyShorePrefabs`** for NE/NW/SE/SW is **not** used in that branch; it remains for step **4** when **no** cardinal water applies and only a diagonal is wet (with the usual `!hasWaterAtSouth` / `!hasWaterAtNorth` guards). The South branch mirrors the East branch for E/W upslopes and E+W both wet → pure south ramp like North with E+W.
4. **Diagonal-only water** — no cardinal water; only a diagonal is water/sea (with guards such as `!hasWaterAtSouth` / `!hasWaterAtNorth` per direction). Delegates to `BuildDiagonalOnlyShorePrefabs`: rectangle outer corner → **Bay**; else `HasLandSlopeIgnoringWater` → Bay or downslope; else Bay on flat; else **upslope + downslope** pair fallback.

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

### 5.6.1 Lake edges: layered model (water + shore + cliffs)

Treat lake/coast borders as **three cooperating layers**, not one prefab:

1. **Open water** lives on **registered water cells** (`WaterManager` / `WaterMap`). Sorting uses the body’s **surface height** (visual placement uses surface − 1 in world space; see FEAT-37 / `WaterManager.PlaceWater`).
2. **Water-shore art** (cardinal ramps, **Bay** corners, upslope+downslope pairs) is chosen on **land** cells that pass the **surface-height gate** (§4.2): `DetermineWaterShorePrefabs` → `PlaceWaterShore`. Parent is the **land** cell.
3. **Cliff wall stacks** (`PlaceCliffWalls` / `PlaceCliffWallStack`) are **children of the higher land cell**, along the **shared cardinal edge** toward a lower neighbor, when **rim** escarpments or **Δh > 1** require vertical faces. **Do not** assume four symmetric cliff meshes: only **south** and **east** faces instantiate visible prefabs (§5.7).

**Rim vs shore:** Land within **one height step** of an adjacent water body’s **surface** uses water-shore prefabs where eligible. **Higher rim** cells (e.g. bowl walls above the lake) use **ordinary slopes + cliff stacks** toward lower cells, not water-shore tiles.

**Face ownership (one logical system per cardinal face):** On a **single cardinal face** of a land cell, **do not** combine an independent **`PlaceCliffWallStack`** segment with a **water-shore ramp** that already expresses the **same** vertical drop — that duplicates the cliff read. **Shore prefabs** may **embed** bank or brown cliff art on **one or more** faces as part of the asset; **`PlaceCliffWallStack`** may attach to **other** faces of the same cell.

**One-step suppression (`ShouldSuppressCliffFaceTowardLowerCell`):** Applies only when the **high** cell **would** use water-shore prefabs (`IsLandEligibleForWaterShorePrefabs` is **true**) **and** the lower neighbor is registered **water** or an **`IsWaterSlopeCell`** shore tile — then a **single** height step toward that neighbor does **not** add an extra `PlaceCliffWallStack` on that face (ramp/water art carries the transition). If the **high** cell is **not** shore-eligible (typical **rim plateau** above the shore strip, e.g. land not in the Moore neighborhood of water but one cardinal step above a shore cell), suppression does **not** apply; `ResolveCliffWallDropAfterSuppression` may assign **one** segment toward that water or water-slope neighbor so visible **south/east** cliff prefabs fill the vertical gap. **`Δh ≥ 2`** still uses stacked segments on visible faces regardless. Other rules: `ShouldSuppressCliffTowardCardinalLower` (narrow shore strip), cut-through corridors, underwater segment cull — unchanged.

**Lake fallback — border corner heights:** When an **artificial lake fallback** terraform runs, **corner** border cells (concave and convex) must have **`HeightMap` / `Cell.height`** consistent with the **lake surface** at that shoreline, not left at an unrelated **elevated dry neighbor** height when continuity with the lake is required.

**Geometric decisions worth remembering:** Cardinal **Δh** drives drop tests. **One-step** duplicate-cliff suppression is **conditional** on shore eligibility (see above). **Δh ≥ 2** stacks segments on **visible** faces. Cliff **sorting** vs **foreground** water neighbors is capped in **`TerrainManager.PlaceCliffWallStack`** (**[BUG-40](../../BACKLOG.md)** completed 2026-03-24). Shore / cascade / multi-body edge cases: completed **[BUG-42](../../BACKLOG.md)** (2026-03-26); **[BUG-45](../../BACKLOG.md)** (adjacent bodies at different heights). Historical SS notes: §15.

#### 5.6.2 Water–water cascades (cardinal surface step)

When two **cardinally adjacent** cells are **both** registered water and the **logical surface** of the **higher** cell exceeds the neighbor’s (`S_high > S_low` from `WaterManager.GetWaterSurfaceHeight`), the transition is a **cascade** (no dry shore between). **Cascades exist only on that contact line** between two **distinct** logical surfaces — not from bed roughness alone. **`HeightMap` under water is the bed**; it does **not** define the water **surface**. The surface is **homogeneous per body or river segment** (`SurfaceHeight` + **`PlaceWater`** at one visual plane); beds may vary freely underneath.

**Placement:** **`PlaceCliffWalls` does not run on water cells**; **`TerrainManager.RefreshWaterCascadeCliffs`** (after **`WaterManager.UpdateWaterVisuals`**) places **`cliffWaterSouthPrefab` / `cliffWaterEastPrefab`** on the **higher** cell’s **south** or **east** visible face for **every** cardinal edge where the neighbor is registered water at a **strictly lower** logical surface (`S_high > S_low`), **except** edges excluded by **§12.7** (Lake involved — **`WaterMap.IsLakeSurfaceStepContactForbidden`**), including along **long** contacts parallel to the high pool (BUG-45). Same-surface neighbors in the **+x** / **+y** directions do **not** suppress cascades when the shared edge is a **surface step** to lower water. Stack model matches **`PlaceCliffWallStack`** (segment loop, underwater cull toward the **low** cell’s surface, sorting cap vs foreground water).

**Anchor and visible faces:** Cascades are parented to **water cells of the upper pool** (`S_high`) along the **cardinal contact** with the lower pool; the instantiated meshes use the **visible** faces of the isometric system (§5.7) — i.e. the faces that correspond to the **edge toward the lower pool** on the grid. **Only `CliffCardinalFace.South` and `CliffCardinalFace.East`** are used for water–water cascades (`cliffWaterSouth` / `cliffWaterEast`). **West and north** cliff meshes for this feature are **out of scope** (same camera rule as §5.7: N/W are not instantiated for interior cells).

**Segment count:** **`segmentCount = S_high − S_low`** (logical step). For **world/stack geometry**, the code uses the two cells’ bed heights; if the high bed is not above the low bed, it **synthesizes** a foot height so the stack still spans **`ΔS`** steps (bed alignment at the contact strip is handled separately — §12.7).

**World Y anchor** for the stack matches **`WaterManager.PlaceWater`**: `GetWorldPositionVector` at `visualSurfaceHeight = max(MIN_HEIGHT, S_high − 1)` **plus** `(0, tileHeight × 0.25)`. Assign **`cliffWater*`** prefabs in the **`TerrainManager`** inspector (art matches **`southCliffWallPrefab` / `eastCliffWallPrefab`** geometry).

### 5.7 Cliffs
- **HeightMap pattern:** Cardinal neighbor height difference > 1 (e.g., cell at h=3, south neighbor at h=1).
- **Visual (fixed isometric camera):** Each cardinal drop uses **`CliffCardinalFace`** (North/South/East/West) and the matching **prefab** (`GetCliffPrefabForCardinalFace`). **Prefabs are not instantiated** on **north** or **west** faces (`IsCliffCardinalFaceVisibleToCamera`) — those are hidden behind the terrain diamond; **south** and **east** faces (↙ ↘) get sprites. **`Cell.cliffFaces`** still records **N/S/E/W** bits for any cardinal risco (hydrology), even when **N/W** skip meshes.
- **Code:** `PlaceCliffWalls` evaluates `GetCliffWallDropNorth` / `South` / `East` / `West` from the **high** cell toward lower neighbors, then `ResolveCliffWallDropAfterSuppression` for the non-suppressed path (rim plateau rule, narrow shore, cut-through). `PlaceCliffWallStack` parents segments to that cell; world position uses `GetCliffWallSegmentWorldPositionOnSharedEdge` with inspector **face nudges** and optional **water-shore Y** fraction; underwater segment cull unchanged.
- **Water / shore:** Water classification uses **`WaterManager.IsWaterAt`**, not raw `SEA_LEVEL` height. For **one-step** drops toward **registered water** or **`IsWaterSlopeCell`**, cliff prefabs are **suppressed only if** the **high** cell passes **`IsLandEligibleForWaterShorePrefabs`** (same gate as `DetermineWaterShorePrefabs`); otherwise the **rim plateau** keeps **one** cliff segment toward that lower cell where visible (see §5.6.1). **Escarpments (Δh ≥ 2)** toward the same neighbors still get stacked segments on **visible** faces only. **Underwater cull:** at the cliff **foot** (low cell of the drop), if that cell is water, segments whose **entire height band** lies strictly below `GetWaterSurfaceHeight` are not instantiated (`ShouldSkipCliffSegmentFullyUnderwater`). **Cut-through** corridors may still get a **1-step** cliff into a **non–water-slope** lowered cell.
- **South / east map border (exterior void):** When the **south** neighbor `(x−1, y)` or **east** neighbor `(x, y−1)` is **outside the grid**, `GetCliffWallDropSouth` / `GetCliffWallDropEast` still compute a drop using a **virtual foot** at **`SEA_LEVEL`** (same as open sea) so **`PlaceCliffWallStack`** can instantiate visible **south** / **east** cliff meshes toward the map edge — avoiding black voids on elevated border cells. `ResolveCliffWallDropAfterSuppression` handles the invalid lower coordinate without probing `WaterMap` at non-cells.
- **North / west faces (deferred):** With the fixed camera, **north** and **west** cliff **meshes** are not instantiated for typical **interior** cells (`IsCliffCardinalFaceVisibleToCamera`). **Map border** situations can make the absence of N/W prefabs obvious; visible N/W cliff art for edges remains a **future follow-up** (not part of completed **[BUG-42](../../BACKLOG.md)**).

### 5.8 Coastal Transitions (Water Slopes)
- **HeightMap pattern:** Land cell (h ≥ 1) with Moore-neighbor water/sea per **`WaterOrSeaAt`** for **pattern** selection, **and** passing the **surface-height gate** in §4.2 (`IsLandEligibleForWaterShorePrefabs` / `TryGetSurfaceHeightForWaterNeighbor`).
- **Visual:** Special water-slope prefabs that visually transition from land elevation toward the water surface. World placement uses the water visual height (see `WaterManager.PlaceWater` / FEAT-37).
- **Constraint:** Normal roads cannot be placed on water-shore tiles (`IsWaterSlopeCell` returns true). Rim cells above the surface cap are **not** water-slope; roads may use normal terrain rules there. Water plants can be placed on coastal slopes.

### 5.9 Bays and shore corners (neighbor patterns, not “concave” alone)

**Bay prefabs** (`northEastBayPrefab`, …) are **one** shore-art option selected by `DetermineWaterShorePrefabs`. Prefer describing **which neighbors are water** rather than informal “concave/convex” alone (those words flip between land view and water-polygon view).

| Pattern | Cardinal water? | Diagonal water? | Typical outcome |
|---------|-----------------|-----------------|-----------------|
| **Perpendicular shore corner** | Two adjacent cardinals (e.g. S + E) | Usually yes (SE diagonal) | Bay if `IsAxisAlignedRectangleCornerWater*` **or** `HasLandSlopeIgnoringWater`; else corner `*SlopeWaterPrefab` for non-rectangle water on flat land |
| **Rectangle outer corner** | No | Yes; diagonal cell is the **outer** vertex of an axis-aligned water block | Often **Bay** (single tile), including via `BuildDiagonalOnlyShorePrefabs` |
| **Diagonal lake edge (flat land)** | No | Yes | Often **Bay**; if Bay missing, **upslope + downslope** pair |
| **River confluence / mouth** | Varies (often 3 cardinals or T) | Often asymmetric | Same §4.2 priority list; **not** always an axis-aligned rectangle — refresh land in a **wider halo** after river stamps (`RefreshLakeShoreAfterLakePlacement` with second Chebyshev ring from the procedural river path). |

**Multi-surface perpendicular junction (BUG-45):** When two **registered** cardinal neighbors are both water but at **different** logical `SurfaceHeight`, **`DetermineWaterShorePrefabs`** prefers the diagonal **`*SlopeWaterPrefab`** for that quadrant over **Bay** so convex contacts at a surface step do not pick concave bay art.

**`IsAxisAlignedRectangleCornerWater*`** (per diagonal): diagonal cell `W` is water and **both** “outward” cardinals from `W` (away from the shore land cell) are **not** water — i.e. `W` is a **convex vertex** of the water set in grid steps (the tip of a rectangle’s corner). L-shapes, notches, and diagonal coastlines may fail this test and correctly take **corner slope water** instead of Bay.

**River confluence / desembocadura:** When one **River** corridor meets another or widens, neighbor patterns may differ from rectangular lakes; **multiple river `WaterBody` ids** at the **same** surface may remain side by side (§12.7). After **`WaterManager.UpdateWaterVisuals`**, **`TerrainManager.RefreshLakeShoreAfterLakePlacement`** should cover all affected land (procedural river path passes **`expandSecondChebyshevRing: true`** for a Chebyshev-2 land halo). **Orphan** shore sprites or triangles on open water usually indicate **`HeightMap` / `Cell.height` vs `WaterMap`** mismatch or a missed refresh — compare §2.4 lake corner pitfall.

**Visual:** NE/NW/SE/SW bay prefabs round the shoreline art. Cardinal cliff stacks use **`TerrainManager`** inspector nudges vs the shared edge (**[BUG-39](../../BACKLOG.md)** completed 2026-03-24). Residual corner / multi-body issues: **[BUG-45](../../BACKLOG.md)** where applicable. Vocabulary for debugging: §15.1.

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
| `southCliffWallPrefab` | South cardinal face (visible — instantiated when drop exists) |
| `eastCliffWallPrefab` | East cardinal face (visible — instantiated when drop exists) |
| `northCliffWallPrefab` | North cardinal face (selected by geometry; **never** instantiated — hidden face; kept for asset parity / `RemoveExistingCliffWalls`) |
| `westCliffWallPrefab` | West cardinal face (same as north) |
| `northEastBayPrefab` | Bay shore tile (NE quadrant selection in `DetermineWaterShorePrefabs`) |
| `northWestBayPrefab` | Bay shore tile (NW quadrant) |
| `southEastBayPrefab` | Bay shore tile (SE quadrant) |
| `southWestBayPrefab` | Bay shore tile (SW quadrant) |

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

### 7.4 Save / Load Game — cell visuals and building sorting

Load does **not** run a global `ReCalculateSortingOrderBasedOnHeight` (see **FEAT-37c** / `GridManager.RestoreGrid`). Building and water behavior is aligned with runtime as follows:

| Mechanism | Role |
|-----------|------|
| **`SortCellDataForVisualRestore`** | Stable phase order: water → grass/shore/slope → RCI zoning overlays → roads → building pivots → multi-cell non-pivots (tie-break `y`, then `x`). |
| **`RestoreGridCellVisuals`** | Instantiates saved prefabs; applies **`CellData.sortingOrder`** where appropriate; open water uses **`TerrainManager.CalculateTerrainSortingOrder`** for the visual surface height. |
| **BUG-34 (completed)** | **`RecalculateBuildingSortingAfterLoad`** re-runs **`GridSortingOrderService.SetZoneBuildingSortingOrder`** on each pivot building so **`GetCellMaxContentSortingOrder`** matches a fully restored grid; multi-cell RCI passes **`buildingSize`**. |
| **BUG-35 (completed)** | Default **`GridManager.DestroyCellChildren`** skips flat **Grass** (so bulldozer/demolish can leave terrain). When placing or restoring **RCI and utility** buildings, **`DestroyCellChildren(..., destroyFlatGrass: true)`** removes that grass so the cell does not keep **grass + building** as sibling sprites. Call sites: **`ZoneManager.PlaceZoneBuilding`** (section loop), **`ZoneManager.PlaceZoneBuildingTile`**, **`BuildingPlacementService.UpdateBuildingTilesAttributes`**. Multi-cell buildings still use **`SetZoneBuildingSortingOrder`** with a per-footprint pass of **`SyncCellTerrainLayersBelowBuilding`** (grass-only children, if any remain). |

Road restore destroys all **`Zone`** children before placing the road; zoning restore uses **`DestroyCellChildrenExceptForest`**. Those paths already cleared flat grass or replaced overlays without relying on **`destroyFlatGrass`**.

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
| Water / sea neighbor test (pattern) | `TerrainManager.cs` | `WaterOrSeaAt` |
| Water slope determination | `TerrainManager.cs` | `DetermineWaterShorePrefabs`, `BuildDiagonalOnlyShorePrefabs`, `IsAxisAlignedRectangleCornerWaterNorthEast` (and NW/SE/SW), `HasLandSlopeIgnoringWater`, `IsWaterSlopeCell`, `IsLandEligibleForWaterShorePrefabs`, `TryGetSurfaceHeightForWaterNeighbor` |
| Cliff walls | `TerrainManager.cs`, `CliffFace.cs`, `Cell` | `PlaceCliffWalls`, `CliffCardinalFace` / `CliffFaceFlags`, `GetCliffWallDropNorth/South/East/West`, `ResolveCliffWallDropAfterSuppression`, `PlaceCliffWallStack`, `GetCliffPrefabForCardinalFace`, `IsCliffCardinalFaceVisibleToCamera`, `GetWaterSurfaceHeightForCliffProbe`, `ShouldSuppressCliffFaceTowardLowerCell` (with `IsLandEligibleForWaterShorePrefabs` on the **high** cell), `ShouldSuppressCliffTowardCardinalLower`, `IsLandEligibleForWaterShorePrefabs`, `IsWaterShoreRampTerrainCell` |
| Terrain tile placement | `TerrainManager.cs` | `PlaceFlatTerrain`, `PlaceSlopeFromPrefab`, `PlaceWaterShore` |
| Sorting order (terrain formula) | `TerrainManager.cs` | `CalculateTerrainSortingOrder`, `CalculateSlopeSortingOrder`, `CalculateWaterSlopeSortingOrder`, `CalculateBayShoreSortingOrder` |
| Sorting order (cell content / buildings) | `GridManager.cs`, `GridSortingOrderService.cs` | Zone/road/building ordering, load restore helpers |
| Full sort recalculation | `GeographyManager.cs` | `ReCalculateSortingOrderBasedOnHeight` |
| Initialization orchestration | `GeographyManager.cs` | `InitializeGeography` |
| Grid ↔ world conversion | `GridManager.cs` | `GetGridPosition`, `GetWorldPositionVector`, `GetMouseGridCell` |
| Terraform planning | `TerraformingService.cs` | `ComputePathPlan`, `ExpandDiagonalStepsToCardinal` |
| Terraform apply/revert | `PathTerraformPlan.cs` | `Apply`, `Revert`, `TryValidatePhase1Heights` |
| Road prefab selection | `RoadPrefabResolver.cs` | `ResolveForPath`, `ResolveForCell` |
| Slope overlay naming | `SlopePrefabRegistry.cs` | `GetSlopeVariant`, `GetSlopeSuffix` |
| Pathfinding costs | `RoadPathCostConstants.cs` | `GetStepCost`, `GetStepCostForInterstate` |
| A* pathfinding | `GridPathfinder.cs` | `FindPath`, `FindPathWithRoadSpacing` |
| Water map / lakes | `WaterMap.cs`, `WaterManager.cs` | `InitializeLakesFromDepressionFill`, `GetSerializableData`, `RestoreWaterMapFromSaveData`, `PlaceWater` |
| Procedural rivers | `ProceduralRiverGenerator.cs`, `GeographyManager.cs` | `GenerateProceduralRiversForNewGame` (after lakes, before interstate) |
| Road terraform validation | `RoadManager.cs` | `TryPrepareRoadPlacementPlan`, `TryPrepareRoadPlacementPlanLongestValidPrefix` |
| Interstate routing | `InterstateManager.cs` | `FindInterstatePathAStar`, `PickLowerCostInterstateAStarPath`, `ComputeInterstateBorderEndpointScore` |

---

## 12. Water map, lakes, and persistence (FEAT-37)

### 12.1 Mental model

Water is **hosted by terrain**: each body has a **logical surface height** aligned with the carved bowl or channel. **`WaterManager.PlaceWater`** keeps logical `SurfaceHeight` in `WaterMap` while positioning the animated tile at **surface − 1** in world space; **sorting** uses that visual height index (see §2, §7).

### 12.2 Data structures

- **`WaterBody`:** `Id`, `SurfaceHeight`, occupied cell indices.
- **`WaterMap`:** `int[,]` body ids (0 = dry); `GetSurfaceHeightAt`. **Lake / sea init** may merge adjacent cells into one body when rules allow (`MergeAdjacentBodiesWithSameSurface`). **Touching bodies at the same logical surface may remain different ids** (aligned water plane via `PlaceWater`); procedural rivers **do not** run a post-pass merge of adjacent river segments (§12.7, §13.3).
- **`Cell` / `CellData`:** `WaterBodyType` (None, Lake, River, Sea); optional `secondaryPrefabName` for two-part shores.
- **`WaterMapData` (save v2):** `waterBodyIds` + serialized bodies; legacy `bool[]` water load still supported.

### 12.3 Lake generation (summary)

- **`InitializeLakesFromDepressionFill(HeightMap, LakeFillSettings, seaLevelForArtificialFallback)`** — depression-fill from strict/window local minima, spill height, optional bounded basin pass, **artificial axis-aligned fallback** if procedural count is below target.
- **Budget:** `UseScaledProceduralLakeBudget` (default false) vs hard cap; `GetAreaScaledLakeBudgetDiagnostic` for logs; extra random seed attempts scale with map area.
- **`LakeAcceptProbability`:** applied **after** spill feasibility (not before), so valid rare minima are not discarded.
- **`LakeFeasibility` / `TerrainManager.EnsureGuaranteedLakeDepressions`:** carves minimal cardinal bowls so enough cells pass the spill test; **does not** replace `WaterMap` placement.
- **Extended maps:** 40×40 designer template **centered**; Perlin + smoothing outside; sparse dips help depression-fill on large grids.
- **Diagnostics:** `[LakeGeneration]` from `WaterMap`, `[LakeBasins]` from bowl pass; `[WaterManager]` one-line summary after init.
- **Seeded RNG ([BUG-36](../../BACKLOG.md)):** `LakeFillSettings.RandomSeed` from map generation seed; depression-fill and bowl shuffle use derived `System.Random` — reproducible when seed fixed, varied across New Games.

### 12.4 Valid lake (procedural) — agreed rules

Strict/window minima as seeds; flood under spill; per-body axis-aligned bbox of occupied cells in **[`MinLakeBoundingExtent`, `MaxLakeBoundingExtent`]** per axis (defaults in `WaterMap.cs` source, typically **2..10**). Merged bodies may exceed one bbox. **Sea** at reference height 0 (`seaLevel`); `MergeSeaLevelDryCellsFromHeightMap` aligns terrain sea cells with `WaterMap`.

### 12.5 Save / load ([FEAT-37c](../../BACKLOG.md))

- **`GameSaveData.waterMapData`** from `WaterMap.GetSerializableData()`.
- **`WaterManager.RestoreWaterMapFromSaveData`** before **`GridManager.RestoreGrid`**; legacy path **`RestoreFromLegacyCellData`** when `waterMapData` absent.
- **Load** does **not** run global `RestoreWaterSlopesFromHeightMap`, `RestoreTerrainSlopesFromHeightMap`, or `ReCalculateSortingOrderBasedOnHeight`; **`RestoreGridCellVisuals`** applies saved `sortingOrder` and prefabs (see §7.4).

### 12.6 Shore refresh after lakes

After `WaterManager.UpdateWaterVisuals`, **`TerrainManager.RefreshLakeShoreAfterLakePlacement`** updates land in the Moore neighborhood of new lake water (and optionally a **second Chebyshev ring** when called from **procedural river** generation — confluence mouths). Shore prefab logic: §4.2, §5.8–5.9; rim cliffs: §5.6–5.7.

### 12.7 Multi-body contact: bed alignment and junction merge (BUG-45)

These rules separate **what players read as water level** from **terrain under the water**, and define **Pass A** and **Pass B** before **`PlaceWater`** when a height map is available. **Pass B** has two stages: **(i)** lower-side extension (dry / shore absorption) and **(ii)** **contact-bed reassignment** of **upper-pool water** when appropriate (item 4).

1. **Several bodies, one aligned plane:** Two or more **`WaterBody`** instances may share the **same** logical **`SurfaceHeight`** and sit **next to or touching** each other. They remain **different ids** when the design chooses not to merge. **`PlaceWater`** uses **`SurfaceHeight`** so the **animated water tile** stays on one **homogeneous** visual plane per body/segment; that is the operational meaning of “same surface,” **not** identical **`HeightMap`** or **`GetHeight`** on every underwater cell.

2. **Bed vs surface:** Underwater **`HeightMap`** is the **bed** (lake bottom, river trench). It **may vary** within a lake or along a river. The **logical water surface** is **one value per body or per river surface segment** (`SurfaceHeight`). **Shores** and sorting use that surface; **cascades** (§5.6.2) appear only where **two different logical surfaces** meet cardinally.

   **Lake at a surface step:** When **`S_high > S_low`** cardinally between two registered water cells, if **either** body is classified **`Lake`**, **Pass A** does not aggregate that lower neighbor for bed alignment, **Pass B** does not run junction-merge absorption for that edge, and **`RefreshWaterCascadeCliffs`** does **not** place water–water cascade stacks on that edge. **`Sea`** is **not** treated as **`Lake`**. River–river steps and contacts where **neither** side is a lake use the normal Pass A/B + cascade behavior. **`WaterMap.IsLakeSurfaceStepContactForbidden`** implements the predicate.

3. **Pass A — Upper contact bed alignment (no `waterBodyIds` change):** Where a higher-surface **water** cell cardinally touches water at a **strictly lower** logical surface, **only** those **higher-surface** cells on that contact may have their **bed** (`HeightMap`) lowered to match the **minimum bed height** among adjacent **lower-surface** water neighbors **that are not excluded by the Lake rule (item 2)**. **Do not** flatten the entire upper pool. **Do not** change **`waterBodyIds`** or turn dry land into water **in this pass**. Width is **one cell thick** on the upper side of the cardinal contact. **`WaterMap.ApplyMultiBodySurfaceBoundaryNormalization`** implements Pass A by **repeated sweeps** (until stable) so the **minimum** lower-pool bed **propagates** along multi-cell-wide junction strips; **idempotent**; runs **first** in **`WaterManager.UpdateWaterVisuals`**.

4. **Pass B — Junction merge (lower-side extension + contact-bed reassignment):** After Pass A, a **junction merge** pass may **reclassify** cells on the **lower-surface** side of cardinal edges where **`S_high > S_low`** (registered water on both sides — rivers, sea, or mixed — **excluding** edges where **either** body is **`Lake`**, per the Lake rule in **item 2** above). It may assign **dry** land and (until refined) **water-shore** cells to **registered water** at the **lower** logical surface, update **`HeightMap`** and **`Cell.height`** to match that body’s conventions, and set **`waterBodyId`** to a body with **`SurfaceHeight == S_low`**. It also absorbs **dry** cells on the **upper-bank** perpendicular to the contact (beside each **high** cell at the step), so shore wedges between two surface levels are replaced by the **lower** body instead of blocking the junction. Strip extent is **parametric** along the **full** contact length; width follows the **upper** body’s cross-section where terrain allows; if beds cannot align, **narrow** the strip. **Diagonal** water-shore prefabs on the **upper** side are chosen relative to the **contact direction** (cardinal step from high to low), using **existing** assets; **`cliffWaterSouth` / `cliffWaterEast`** (and stack height for **`ΔS > 1`**) on **upper-pool** water cells along the contact as needed, including **upper** diagonal shore cells where applicable (§5.6.2 — **south** and **east** faces only; **west/north** water cascade meshes not in scope). **Same `S_low`, different ids:** when several lower bodies share the same surface height, new cells may attach to **any** of those ids (deterministic choice); **do not** merge those lower bodies into one id.

   **Contact-bed reassignment (upper-pool water):** Pass A may leave an **upper-surface** water cell’s **bed** (`HeightMap`) **above** a **cardinal** lower-surface neighbor’s bed (e.g. propagation not finished on wide channels) **or** **equal** without changing **`waterBodyIds`** — either way **`SurfaceHeight`** can remain **stale** on the **upper** body while the lecho already belongs with the **lower** pool. After the **dry** absorption stage, **Pass B** **reassigns** such **water** cells to the **lower** neighbor’s **`WaterBody`** when **`S_high > S_low`**, the Lake edge rule does **not** apply, and this cell’s bed is **not** **below** the chosen neighbor’s bed (`HeightMap`(high) ≥ `HeightMap`(low)); if **`HeightMap`(high) > `HeightMap`(low)**, the implementation **writes** the upper cell’s bed **down** to the neighbor’s bed **before** changing **`waterBodyId`**. If several cardinal lower neighbors qualify, prefer the **lowest** **`S_low`**, then the **lowest** body id. The sweep may **repeat** until stable. **`WaterMap`** implements this after **`TryAbsorbDryCellIntoLowerBody`** processing.

5. **Rivers downstream:** Along a river path, **bed height** does not increase toward the exit (**§13.4** longitudinal rule). **Surface height** stays constant for a **segment** until generator logic starts a **new** segment. Pass A + B must **not** violate upstream pool geometry beyond the **contact strip** and the **lower-side** extension.

6. **`UpdateWaterVisuals` order:** Pass A → Pass B → **`PlaceWater`** (all water cells) → **`RefreshWaterCascadeCliffs`** → **`RefreshLakeShoreAfterLakePlacement`** when lake depression-fill is enabled **or** Pass B changed any cell (Chebyshev-2 halo when Pass B merged, so river junctions get land shore refresh without requiring depression-fill only).

7. **“Correct surface” in tools and debug:** Means **`PlaceWater` / world water plane** consistency for a body, **not** that every underwater cell shares one **`HeightMap`** value.

**Implementation plan:** [`docs/water-junction-merge-implementation-plan.md`](../../docs/water-junction-merge-implementation-plan.md).

---

## 13. Procedural rivers (FEAT-38)

Rivers are **static** after geography init: **no** runtime fluid simulation, discharge, or type recomputation on merge.

### 13.1 Initialization order

**`GeographyManager`:** terrain → **`WaterManager.InitializeWaterMap()`** (lakes/sea) → **dedicated river pass** → **interstate** → forests (if enabled) → desirability, sorting, etc.

### 13.2 Scope and out of scope

- **In scope:** pathfinding on generated terrain, shallow carve (default **≤ 2** steps, documented exceptions at relief), cardinal corridor, **1–3** rivers per New Game (code defaults; not exposed in UI in FEAT-38 pass).
- **Out of scope:** gameplay spill/flood, dynamic volume, hydraulic reinterpretation after merge, full drainage networks.

### 13.3 Merge and adjacency to lakes/sea

**`MergeAdjacentBodiesWithSameSurface`** (used during **lake / sea** placement and similar init passes): **River** merges **only** with **River**; **Lake** with **Lake**; **Sea** with **Sea**; **Lake** with **Sea** (FEAT-37 compatibility). The **procedural river pass** does **not** call a final adjacency merge: touching river segments at the **same** surface may keep **separate ids** so confluences and wide channels stay explicit (§12.7). Where the river corridor overlaps existing water, **`ProceduralRiverGenerator`** may carve the bed and **`WaterMap.TryReassignCellFromAnyWaterToRiverBody`** may move **Sea** or **Lake** cells into the river body **only** when the existing body’s logical **`SurfaceHeight`** matches the river segment’s surface; **Lake** at a **different** logical surface is **not** carved to the river bed and **not** reassigned (§12.7). Logical exit = river ends in **River** cells **adjacent** to that body’s perimeter.

### 13.4 Geometry (implementation contracts)

Cross-sections perpendicular to local flow (see **`ProceduralRiverGenerator`** XML docs):

| Topic | Rule |
|-------|------|
| **Symmetric banks** | Dry shore cells on both sides: `H_bank = H_bed + 1` (one step above shared bed floor `H_bed`) when possible |
| **Single bed height per section** | All bed cells in a section share one carved `H_bed` |
| **Surface segments** | Logical `surface = H_bed + 1` (clamped); consecutive sections with same `surface` share one `WaterBody` id; **new** body when `surface` changes along the path |
| **Longitudinal monotonicity** | Along centerline from entry: **`H_bed(i+1) ≤ H_bed(i)`** after forward clamp `min(candidate, H_bed(i−1))` — river does not climb toward exit |
| **Border margin** | `RiverBorderMargin` (default **2**): entry/exit in interior band; avoid lateral “moats” along non-flow edges |
| **Width** | Bed width **1–3** cells; total cross-stream **Wₙ = bed + 2** shores → **{3,4,5}**; **|Wₙ₊₁ − Wₙ| ≤ 1**; prefer **≥ 4** steps between width changes unless terrain forces |
| **Length L** | Sum of steps along channel; **max L** = **1.5 ×** map dimension on relevant axis (square: width or height) |
| **Forced river** | If no viable candidate, carve basin and place forced river (spirit: artificial-lake fallback) |

**Cardinal edges:** **N–S** or **E–W** opposite border pairs; high/low from relief; lake/sea as logical exit when present.

Shore / cascade polish: **[BUG-42](../../BACKLOG.md)** completed 2026-03-26; residual multi-body / intersection cases: **[BUG-45](../../BACKLOG.md)**.

### 13.5 Shore band continuity (inner corners)

After **`ApplyCrossSectionHeights`**, **`ProceduralRiverGenerator`** runs a **corner promotion** pass on **bed footprint** cells that actually received a river bed carve (skipped **Lake** cells at a different logical surface are excluded). If a cell is still at carved **`H_bed`** but has **two perpendicular cardinal neighbors** both at **`H_bed + 1`** (the symmetric bank height from §13.4), that cell is **raised** to **`H_bed + 1`** so the dry shore strip stays **continuous** around inner L-corners instead of leaving a bed-height “notch” that would read as water or break shore prefab continuity.

**Water assignment** to the river body uses **`HeightMap`** after this pass: a cell listed in **`sec.Bed`** is assigned water **only** if its height still equals **`sec.AppliedBedHeight`** (promoted corners stay **dry** shore).

**Example (heights, smaller numbers = lower):** plateau **4**, target shore band **2**, bed **1**. Without promotion, row near the inner corner can incorrectly show **`… 2 2 1 1`** (bed notch beside two shore cells). After promotion the inner corner matches the band: **`… 2 2 2 1`**.

---

## 14. Roads: manual draw, interstate, bridges, shared validation

### 14.1 Single entry point for terraform + validation

All persistent road placement that uses terraforming should go through **`RoadManager.TryPrepareRoadPlacementPlan`** (includes **`PathTerraformPlan.TryValidatePhase1Heights`**) and **`RoadPathValidationContext`**. Do **not** use **`TerraformingService.ComputePathPlan`** alone for placement decisions.

| Mode | API | `forbidCutThrough` |
|------|-----|--------------------|
| **Manual streets / preview** | `TryPrepareRoadPlacementPlanLongestValidPrefix` | `false` — partial path when full A* would need invalid terraform |
| **Interstate** | `TryPrepareRoadPlacementPlan` (full path) | `true` — no cut-through trenches |
| **AUTO streets** | `AutoRoadBuilder.TryGetStreetPlacementPlan` → longest-prefix when `RoadManager` present | `false` |

### 14.2 Manual draw pipeline (reference)

1. Drag: **`ClearPreview(false)`** (revert terraform) → **`GetLine()`** (A* on **original** heightmap) → **`DrawPreviewLineCore`**: longest-valid-prefix prep → **`plan.Apply()`** → **`ResolveForPath`** → preview tiles.
2. Release: revert → **`TryFinalizeManualRoadPlacement`** (same prefix hint) → afford → **`Apply`** → place tiles; **`RefreshRoadPrefabAt`** pass on placed cells for junction correctness.

### 14.3 Slope climb vs carve

When no consecutive **`|Δh| > 1`**, **`preferSlopeClimb`**: ascending land steps use terraform **`None`** and **`postTerraformSlopeType`** from travel so the road **rides the slope** instead of flattening a trench.

**Gorge beside corridor:** `ExpandAdjacentFlattenCellsRecursively` only when not (slope-climb with no Flatten and no pre-expansion adjacent cells); **`InvalidatePlanIfPathBesideSteepLandCliff`**; **`ValidateNoHeightDiffGreaterThanOne`** one-ring expansion for Phase 1 validation.

### 14.4 Bridges and water approach

- **`StraightenBridgeSegments`**, **`IsBridgePathValid`**: bridge span must be **axis-aligned** (same X or same Y); no kinked water crossings.
- **`HasTurnOnWaterOrCoast`**, **`HasElbowTooCloseToWater`**, **`HasTurnOnLastLandCellsBeforeWater`**: elbows not on water/water-slope; approach straight before water (**Rule F**).
- **`TerrainManager.PlaceWaterSlope`:** use **`DestroyTerrainChildrenOnly`** (not `DestroyCellChildren`) so **bridge** tiles on coastal cells are not destroyed when refreshing water-slope terrain; bays included in terrain-only destroy where applicable.

### 14.5 Interstate pathfinding (summary)

**`InterstateManager`:** **`ComputeInterstateBorderEndpointScore`** (flat neighbors, first inland step, border height) → sorted candidates → **`PickLowerCostInterstateAStarPath`** runs A* with `avoidHighTerrain` true and false, picks cheaper **`ComputePathCost`** if both succeed. Penalties: **`InterstateAwayFromGoalPenalty`**, **`InterstateZigzagPenalty`**, slope multiplier — see **`RoadPathCostConstants`**.

### 14.6 Cut-through robustness ([BUG-29](../../BACKLOG.md))

Reject cut-through when **`maxHeight - baseHeight > 1`**; **`terraformCutCorridorCells`** / cliff corridor context; map-edge margin **`cutThroughMinCellsFromMapEdge`**; Phase 1 validation ring in **`PathTerraformPlan`**. Interstate always **`forbidCutThrough: true`**.

### 14.7 Resolver rules (post-BUG-26 / BUG-30)

| ID | Rule |
|----|------|
| **A** | Elbow connectivity matches exactly two path neighbors |
| **B** | Prefab exits align with path in/out |
| **C** | **Terraform wins:** cut-through → flat road prefabs from plan, not live slope misread |
| **D** | Prefer offset paths that avoid hills when costs are close |
| **E** | Interstate prefers straight segments (cost tuning) |
| **F** | Bridge approach perpendicular to water; no turn on last land cells before water |

**Slope / corner roads:** segment **`postTerraformSlopeType`** via **`GetPostTerraformSlopeTypeAlongExit`** (travel-aligned); **`RestoreTerrainForCell`** can force orthogonal ramp when action `None` but plan has cardinal slope ([BUG-30](../../BACKLOG.md)).

### 14.8 Optional polish (backlog)

- Crossroads: augment path-only neighbor checks with **`IsRoadAt`** in **`ResolvePrefabForPathCell`**; final **`RefreshRoadPrefabAt`** over path cells if needed (**BUG-25** follow-up).
- Pass **`postTerraformSlopeType`** into refresh after cut-through (**BUG-25** §2.1 style).
- **BUG-28** / **BUG-31:** interstate vs slope sorting; border entry/exit prefabs.

---

## 15. Lake / cliff / shore — engineering notes (historical)

Condensed from prior bug write-ups. **Mechanisms** are normative in §4.2, §5.6–5.9, §7; this section is for **symptoms**, **debugging**, and **vocabulary**.

### 15.1 Glossary

| Term | Meaning |
|------|---------|
| **Water surface / open water** | Registered water: `WaterManager` / `WaterMap`, sorted at visual surface height |
| **Water-shore (ramp)** | Land passing §4.2 gate → `DetermineWaterShorePrefabs` / `PlaceWaterShore` |
| **Rim** | Land above shore cap → normal slopes + `PlaceCliffWalls`, not water-shore art |
| **Cliff wall stack** | Child prefab(s) on **higher** cell along shared cardinal edge toward lower cell |
| **Bay** | Shore corner prefab set — neighbor patterns §5.9 |
| **Visible cliff faces** | **South** and **east** meshes only (`IsCliffCardinalFaceVisibleToCamera`); N/W bits may still be set on `Cell.cliffFaces` |

### 15.2 Resolved techniques ([BUG-39](../../BACKLOG.md), [BUG-40](../../BACKLOG.md))

- **BUG-39:** Inspector nudges for S/E cliff faces + **`cliffWallWaterShoreYOffsetTileHeightFraction`** when primary terrain is water-shore.
- **BUG-40:** **`GetMaxCliffSortingOrderFromForegroundWaterNeighbors`** — cap cliff `sortingOrder` using registered water neighbors at lower isometric depth (`nx+ny < highX+highY`) so cliffs do not draw over nearer water.

### 15.3 Symptom → direction (SS1–SS5)

- **SS1:** Duplicate cliff + shore ramp on same face → **`ShouldSuppressCliffFaceTowardLowerCell`** (one-step toward water / water-shore when high cell is shore-eligible).
- **SS2 / SS4:** Stacked cliff segments visually collapsed → `PlaceCliffWallStack` uses **`edgeBlend = 1`** at bottom of segment; consider per-step Y offset or lerp &lt; 1 for inner segments; check prefab pivots.
- **SS3:** Template bowls with **|Δh| &gt; 1** to neighbors — intentional for cliff stacks; must stay consistent with SS1 shore choice.
- **SS5:** Z-fighting / cliffs under water → sorting formula vs water plane; possible future dedicated buckets or masks.

### 15.4 Terrain debug logging

Enable **`TerrainManager.terrainDebugLogCellsEnabled`**; default cells **(28,24), (28,25), (34,24), (34,25)**. Console filter **`[TerrainDebug]`** — heights, shore eligibility, neighbors, cliff drops, stack positions, child sort orders.

### 15.5 Picking / grass under cells

**`PlaceFlatTerrain`** may leave root `SpriteRenderer` state dependent on pipeline; **`GetMouseGridCell`** uses sorting — steep stacks can mis-pick until cliff/shore order is stable. Compare `HeightMap`, `Cell.height`, and hierarchy when debugging.
