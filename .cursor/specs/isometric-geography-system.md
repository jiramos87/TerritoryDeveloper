# Isometric Geography System — Technical Specification

> **Canonical spec** for isometric geography (single source of truth). See `ARCHITECTURE.md` for init order and persistence.

## 0. Canonical scope and doc hierarchy

### 0.1 What this spec owns

- Grid ↔ world math, direction naming, neighbor deltas (§1).
- `HeightMap` semantics, `Cell.height` sync, water surface vs visual placement (§2).
- Land slope types and slope determination algorithm (§3–4.1).
- Water-shore eligibility, neighbor tests, shore prefab decision order (§4.2, §5.8–5.9).
- Layered model: open water, water-shore art, cliff stacks, suppression rules (§5.6–5.7).
- Sorting constants and formulas; load/save visual ordering (§7).
- Terraform modes (§8), roads on slopes (§9), pathfinding costs (§10).
- Water map, lake generation, persistence (§11), river junction geometry / brinks (§11.8), procedural rivers (§12), road/interstate/bridge validation (§13), engineering notes (§14).

### 0.2 What lives elsewhere

| Concern | Document |
|---------|----------|
| UI design system | `.cursor/specs/ui-design-system.md` |
| Road placement pipeline | `.cursor/specs/roads-system.md` |
| Simulation / AUTO growth | `.cursor/specs/simulation-system.md` |
| Save / load pipeline | `.cursor/specs/persistence-system.md` |
| Water & terrain overview | `.cursor/specs/water-terrain-system.md` |
| Manager responsibilities | `.cursor/specs/managers-reference.md` |
| Domain glossary | `.cursor/specs/glossary.md` |

### 0.3 Quick reference for AI agents

Read only the sections you need — use this table to navigate:

| Need to understand... | Read sections | Approx. lines |
|---|---|---|
| Grid math, coordinates, directions | §1 | ~45 |
| Height model, water surface vs bed | §2 | ~50 |
| Slope type determination | §3–§4 | ~100 |
| Shore/cliff/water layering | §5.6–§5.9, §14 | ~80 |
| Prefab inventory and naming | §6 | ~40 |
| Sorting order formula | §7 | ~45 |
| Terraform system | §8 | ~35 |
| Road prefab selection on terrain | §9 | ~35 |
| Pathfinding costs | §10 | ~20 |
| Water map, lakes, multi-body junctions | §11 | ~70 |
| Procedural rivers | §12 | ~45 |
| Road/interstate/bridge validation | §13 | ~60 |
| Engineering notes, shore mini-glossary, road/grid vocabulary, debug | §14 (see **§14.5** for stroke, lip, grass, Chebyshev) | ~45 |

---

## 1. Isometric Grid Fundamentals

### 1.1 Coordinate System

Diamond (isometric) projection — logical `(x, y)` → world:

```
worldX = (x - y) * (tileWidth / 2)
worldY = (x + y) * (tileHeight / 2) + heightOffset
```

| Constant | Value | Notes |
|----------|-------|-------|
| `tileWidth` | 1.0 | Full diamond width in world units |
| `tileHeight` | 0.5 | Half of width (isometric squash) |
| `heightOffset` | `(h - 1) * 0.25` | Vertical shift per height level above base (h=1) |

Grid origin `(0, 0)` is the **bottom corner** of the diamond; extends to `(width-1, height-1)`.

### 1.2 Direction Convention

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

> **Mnemonic:** `+x` = toward top-right (North). `+y` = toward top-left (West).

### 1.3 Inverse Conversion (World → Grid)

```
posX = worldPoint.x / (tileWidth / 2)
posY = worldPoint.y / (tileHeight / 2)
gridX = round((posY + posX) / 2)
gridY = round((posY - posX) / 2)
```

Height-aware picking tests a 3×3 candidate area, selecting the cell with the highest sorting order whose sprite bounds contain the cursor.

---

## 2. Height System

### 2.1 HeightMap

`HeightMap` stores `int[width, height]`; each cell has height in `[MIN_HEIGHT=0, MAX_HEIGHT=5]`.

| Height | Semantic |
|--------|----------|
| 0 | **Sea level** — `SEA_LEVEL = 0`. Registered lake/river/sea bodies use **per-body surface height** from `WaterBody`, not a global surface. |
| 1 | **Base land** — default elevation; most flat terrain |
| 2–5 | **Elevated land** — hills, mountains, plateaus |

Height affects: (1) **world Y** — +0.25 per level, (2) **sorting order** — higher renders atop lower at same depth, (3) **slope detection** — any Δh to an 8-neighbor triggers slope prefab selection.

### 2.2 Height Generation

The initial 40×40 map uses a hardcoded template. Grids larger than 40×40 extend with **dual-octave Perlin noise** blended at the 40-cell border. Procedural lakes (circular h=0 patches) and rivers (downhill h=0 paths) are stamped on extended terrain.

### 2.3 Height Constraint

Max **|Δh| = 1** between cardinal neighbors for valid terrain. Greater differences display with cliff walls but may cause visual artifacts. Terraform plans reject violations via height-diff validation.

**Exception:** Intentional lake basins may use |Δh| > 1 across bowl walls; those rely on cliff stacks and shore rules.

### 2.4 Height authority and procedural water

During geography generation, **`HeightMap` is source of truth**. `Cell.height` must stay in sync with `HeightMap[x, y]` whenever either is written. Water surface height (per `WaterBody` / river segment) is the terrain height hosting that water after carving.

**Invariants:** (1) `HeightMap` and `Cell.height` never diverge. (2) Fallback/forced paths update the same fields as depression-fill. (3) Fallback lake border corners have heights consistent with the lake surface.

#### 2.4.1 Shore band height coherence

Land cells bordering water stay on the **same vertical band** as the body's surface so shore art aligns with water visuals (logical surface **S**, visual reference **V = max(MIN_HEIGHT, S − 1)**).

**Invariant:** For any dry cell Moore-adjacent to water, `HeightMap` must not exceed the minimum logical surface among adjacent water cells. In practice, shore refresh clamps: if `h > min(S)`, height is lowered to `min(S)` (never raised).

**Shore prefab gate:** `h ≤ V + MAX_LAND_HEIGHT_ABOVE_ADJACENT_WATER_SURFACE_FOR_SHORE_PREFABS` (default MAX=1). Higher cells use ordinary slopes + cliff walls (rim).

**Rivers:** Shore dry cells share one bank height per run; clamp + promotion (§12.5) keep `HeightMap` consistent.

### 2.5 Minimal neighbor refresh after lake shore

After shore terrain update for a water body, recompute affected shore land cells and audit only the single land neighbor immediately outward from water along each relevant cardinal. Expand refresh radius only if a verified bug requires it.

---

## 3. Terrain Slope Types

### 3.1 The TerrainSlopeType Enum

13 values:

```
Flat,
North, South, East, West,
NorthEast, NorthWest, SouthEast, SouthWest,
NorthEastUp, NorthWestUp, SouthEastUp, SouthWestUp
```

### 3.2 Naming Convention

**Slope names indicate downhill direction:**
- `South` slope → North neighbor is higher → terrain slopes downhill toward South.
- `NorthEast` diagonal → SouthWest diagonal neighbor is higher.
- `SouthEastUp` corner → both West AND North neighbors higher → concave valley opening southeast.

### 3.3 Slope Categories

#### 3.3.1 Flat
- **Condition:** All 8 neighbors same height, OR local maximum (plateau).
- **Visual:** Standard grass tile.

#### 3.3.2 Orthogonal (Cardinal) Slopes — N, S, E, W
- **Condition:** Exactly one cardinal neighbor higher; no two adjacent cardinals both higher.
- **Visual:** Ramp on the **lower cell** transitioning one height level.
- Roads can use directional slope road prefabs.

| Slope | Higher Neighbor | Screen Ramp Direction |
|-------|----------------|----------------------|
| North | South `(x-1, y)` | Down-left → up-right |
| South | North `(x+1, y)` | Up-right → down-left |
| East | West `(x, y+1)` | Up-left → down-right |
| West | East `(x, y-1)` | Down-right → up-left |

#### 3.3.3 Diagonal Slopes — NE, NW, SE, SW
- **Condition:** No cardinal higher, but exactly one diagonal neighbor higher.
- **Visual:** Wedge-shaped diagonal transition.
- **Road strokes:** Land diagonal slopes are **not** valid road cells. `RoadStrokeTerrainRules` / `RoadManager` truncate strokes at the first such cell; A* and interstate generation skip them (see `roads-system.md` and §13.10). Terrain art remains a wedge; roads do not run through these land tiles.

| Slope | Higher Diagonal Neighbor |
|-------|--------------------------|
| NorthEast | SouthWest `(x-1, y+1)` |
| NorthWest | SouthEast `(x-1, y-1)` |
| SouthEast | NorthWest `(x+1, y+1)` |
| SouthWest | NorthEast `(x+1, y-1)` |

#### 3.3.4 Corner / Upslope Types — NEUp, NWUp, SEUp, SWUp
- **Condition:** Two adjacent cardinal neighbors both higher (concave corner).
- **Visual:** L-shaped concavity opening away from the two higher neighbors.
- **Road strokes:** Corner-up land cells are **not** valid road cells (same rule as §3.3.3 — only flat + cardinal ramps for strokes). Resolver/route-first behavior applies only where a road may legally exist.

| Slope | Higher Pair | Valley Opens Toward |
|-------|------------|---------------------|
| SouthEastUp | West + North | Southeast ↘ |
| NorthEastUp | West + South | Northeast ↗ |
| SouthWestUp | East + North | Southwest ↙ |
| NorthWestUp | East + South | Northwest ↖ |

---

## 4. Slope Determination Algorithm

### 4.1 Land Slope Selection (`DetermineSlopePrefab`)

Reads 8-neighbor heights and applies a **priority cascade**:

1. **Corner/upslope (highest priority):** Two adjacent cardinal neighbors higher → corner upslope prefab.
   - West+North → `SouthEastUp`; West+South → `NorthEastUp`
   - East+North → `SouthWestUp`; East+South → `NorthWestUp`
2. **Cardinal slope:** Exactly one cardinal higher → cardinal slope prefab (named by downhill face).
3. **Diagonal slope (lowest):** No cardinal higher but one diagonal higher → diagonal slope prefab.
4. **No match → null** (plateau; uses flat grass).

### 4.2 Water Shore Slope Selection (`DetermineWaterShorePrefabs`)

Water-shore prefabs apply only when the land cell passes the **surface-height gate**: among 8 neighbors, some water cell exists whose logical surface **S** yields visual reference **V = max(MIN_HEIGHT, S − 1)** such that **h ≤ V + MAX** (default 1). Higher rim land uses ordinary terrain + cliff walls instead.

The same gate drives **one-step cliff suppression** toward water: suppression applies only on shore-band cells; rim plateaus keep a visible cliff segment toward lower shore/water.

**Two neighbor-test layers:** (1) Surface-height gate — uses water registration + surface-height lookup; `SEA_LEVEL` cells without registration still yield a surface. (2) Shore pattern detection — uses `WaterOrSeaAt` (true if `HeightMap == SEA_LEVEL` or registered water); bits may differ from "registered only" at generator boundaries.

For eligible cells, **fixed priority** (first match wins):
1. **Map-edge shortcuts** — border cell → cardinal water-slope.
2. **Two wet cardinals** — e.g. S+E. Bay vs corner slope via rectangle-corner test + land-slope signal. Three-cardinal ties break by diagonal wetness and legacy pairwise order.
3. **Single cardinal water** — E, W, N, S branches; cardinal ramps or upslope combinations. N/S with no opposite strip and no E/W water → pure cardinal shore.
4. **Diagonal-only water** — rectangle outer corner → Bay; land-slope → Bay or downslope; flat → Bay; else upslope+downslope pair. **Exception (§11.8):** junction brink forces diagonal slope water (no Bay).

### 4.3 `RequiresSlope` vs Slope Selection

`RequiresSlope(x, y, height)` returns true if any 8-neighbor has different height. But `DetermineSlopePrefab` may still return null for plateaus (all neighbors lower or equal) — cell gets flat grass.

### 4.4 `GetTerrainSlopeTypeAt`

Returns a `TerrainSlopeType` enum using the same logic as `DetermineSlopePrefab`. Used by forest placement, terraforming, road prefab resolution, and road validation.

---

## 5. Geographic Elements

### 5.1 Flat Terrain (Plains)
- **HeightMap:** Uniform height across a region.
- **Visual:** Standard grass tiles. Ideal for building, roads, zoning.

### 5.2 Hills and Mountains
- **HeightMap:** Region at height h surrounded by h-1. Concentric rings form multi-level hills.
- **Structure:** Max |Δh|=1 between cardinals → hills require gradual transitions.

### 5.3 Orthogonal Hillside (Cardinal Slope Line)
- Linear boundary where adjacent row/column differs by 1. Continuous line of cardinal slope tiles.

### 5.4 Diagonal Hillside
- Height increases along a diagonal; only diagonal neighbor higher. Less common; occurs at hill corners.

### 5.5 Concave Corners (Upslope / Valley)
- Two perpendicular cardinal neighbors both higher → L-shaped concavity.

### 5.6 Convex Corners (Diagonal Slope at Hill Corner)
- Only one diagonal neighbor higher, no cardinal higher → outer corner wedge.

### 5.6.1 Lake edges: layered model (water + shore + cliffs)

Three cooperating layers:

| Layer | Lives on | Rule |
|-------|----------|------|
| Open water | Registered water cells | Sorting at body's surface height; visual at surface − 1 |
| Water-shore art | **Land** cells passing surface-height gate (§4.2) | Cardinal ramps, Bay corners, upslope+downslope pairs |
| Cliff wall stacks | Children of **higher** land cell | Along shared cardinal edge toward lower; S and E faces only (§5.7) |

**Rim vs shore:** Land within one height step of adjacent water uses shore art. Higher rim → ordinary slopes + cliff stacks.

**Face ownership:** Do not combine independent cliff stacks with a shore ramp on the same cardinal face — that duplicates the transition. Shore prefabs may embed bank art; cliff stacks attach to other faces.

**One-step suppression:** Applies only when high cell is shore-eligible AND lower neighbor is water or water-shore. Single Δh skips cliff on that face. Non-eligible rim plateaus keep one cliff segment. Δh ≥ 2 always stacks on visible faces.

**Fallback border corners:** Must have `HeightMap`/`Cell.height` consistent with the lake surface.

#### 5.6.2 Water–water cascades (cardinal surface step)

When two cardinally adjacent cells are both registered water and `S_high > S_low`, the transition is a **cascade** (no dry shore between). Cascades exist only between distinct logical surfaces.

| Property | Rule |
|----------|------|
| **Visible faces** | South and East only (`cliffWaterSouth`/`cliffWaterEast` prefabs) |
| **Anchor** | Upper pool's visual surface plane |
| **Segment count** | `S_high − S_low` (logical step) |
| **Mirror placement** | When lower pool is N or W, stacks parent to the lower-pool cell using S/E prefabs |
| **Lake exclusion** | Edges where either body is Lake skip cascades (§11.7 `IsLakeSurfaceStepContactForbidden`) |
| **Stacks** | Same model as cliff stacks; water–water cascades skip underwater segment cull |

### 5.7 Cliffs
- **Pattern:** Cardinal Δh > 1 (e.g. cell h=3, south neighbor h=1).
- **Visible faces:** Only **south** and **east** meshes instantiated (`IsCliffCardinalFaceVisibleToCamera`). N/W are hidden behind the terrain diamond. `Cell.cliffFaces` still records N/S/E/W bits for hydrology.
- **Water classification** uses registered water, not raw `SEA_LEVEL`. One-step drops toward water are suppressed only if the high cell passes the shore eligibility gate; rim plateaus keep one cliff segment. Δh ≥ 2 uses stacked segments on visible faces.
- **Border (exterior void):** S/E border neighbors outside the grid use virtual foot at `SEA_LEVEL` so cliff meshes cover elevated border cells.
- **N/W faces:** Not instantiated for interior cells. Map-border N/W cliff art is a future follow-up.

### 5.8 Coastal Transitions (Water Slopes)
- Land cell with Moore-neighbor water passing the surface-height gate (§4.2).
- Special water-slope prefabs transition from land toward the water surface.
- Normal roads cannot be placed on water-shore tiles (`IsWaterSlopeCell`). Rim cells above the cap follow normal terrain rules.

### 5.9 Bays and shore corners

**Bay prefabs** are one option from shore prefab selection. Describe by **which neighbors are water**, not informal "concave/convex."

| Pattern | Cardinal water? | Diagonal water? | Typical outcome |
|---------|-----------------|-----------------|-----------------|
| **Perpendicular shore corner** | Two adjacent cardinals | Usually yes | Bay if rectangle outer corner or land-slope signal; else corner slope water |
| **Rectangle outer corner** | No | Yes (outer vertex of axis-aligned water block) | Bay |
| **Diagonal lake edge (flat)** | No | Yes | Bay; if missing, upslope+downslope pair |
| **River confluence / mouth** | Varies (often 3 cardinals or T) | Often asymmetric | Same §4.2 priority; refresh land in wider halo after river stamps |

**Multi-surface junction:** Two cardinal water neighbors at different `SurfaceHeight` → prefer diagonal slope water over Bay.

**Rectangle corner test:** Diagonal cell W is water and both outward cardinals from W are not water → W is a convex vertex. L-shapes and notches fail this test → corner slope water.

**Orphan shore sprites** on open water usually indicate `HeightMap`/`Cell.height` vs `WaterMap` mismatch — compare §2.4.

### 5.10 Cut-Through Corridors
- Path flattened to base height through a hill by terraforming. Creates a trench with cliff walls on sides where terrain drops from surrounding hill height.

### 5.11 Sea Level Water
- Cells at h=0. Animated water tile. Bridges allowed; buildings not (except water plants).

---

## 6. Prefab Inventory

### 6.1 Land Slope Prefabs (12)

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
| `southCliffWallPrefab` | South face (visible — instantiated on drop) |
| `eastCliffWallPrefab` | East face (visible — instantiated on drop) |
| `northCliffWallPrefab` | North face (never instantiated — hidden; kept for asset parity) |
| `westCliffWallPrefab` | West face (same as north) |
| `northEastBayPrefab` … `southWestBayPrefab` | Bay shore tiles (4 quadrants) |

### 6.4 Slope Variant Naming Convention

```
{flatPrefabName}_{slopeCode}Slope
```

Where `slopeCode` is: `N`, `S`, `E`, `W`, `NE`, `NW`, `SE`, `SW`, `NEUp`, `NWUp`, `SEUp`, `SWUp`.

Example: `ResidentialLight_NSlope` = north-slope variant of `ResidentialLight`.

`GetSlopeVariant(flatPrefab, slopeType)` looks up the variant by constructed name. Returns null if no variant exists.

---

## 7. Sorting Order System

### 7.1 Formula

```
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
| Land slope | +1 | Slightly in front of terrain |
| Water slope | +1 | Above sea-level water |
| Road | +5 | Above terrain and slopes |
| Utility | +8 | Above roads |
| Building | +10 | Above everything on same cell |
| Effect | +30 | Topmost layer |

### 7.3 Design Rationale

- `DEPTH_MULTIPLIER (100) > HEIGHT_MULTIPLIER (10) * MAX_HEIGHT (5) = 50` → depth dominates over height. Hilltops at back never draw over foreground.
- Negative depth order → higher `x+y` drawn first (behind).

### 7.4 Save / Load — cell visuals and building sorting

Load does **not** run a global sort recalculation. Building and water behavior:

| Mechanism | Role |
|-----------|------|
| `SortCellDataForVisualRestore` | Stable phase order: water → grass/shore/slope → RCI overlays → roads → building pivots → multi-cell non-pivots (tie-break `y`, then `x`). |
| `RestoreGridCellVisuals` | Instantiates saved prefabs; applies saved `sortingOrder`; open water uses terrain sorting formula for visual surface height. |
| Building sort post-pass | Re-runs building sorting on each pivot after full grid restore so max-content-sorting matches runtime; multi-cell passes building size. |
| Grass removal on place/restore | When placing or restoring RCI/utility buildings, destroys flat grass children so cell does not keep grass + building as siblings. |

---

## 8. Terraforming System

### 8.1 Overview

When roads cross sloped terrain, a `PathTerraformPlan` describes height modifications. Two strategies:

### 8.2 Scale-with-Slopes Mode

**Condition:** All consecutive path cells have |Δh| ≤ 1. Road "climbs" using slope road prefabs. No terrain modification; plan records `TerraformAction.None` and sets `postTerraformSlopeType`.

### 8.3 Cut-Through Mode

**Condition:** At least one consecutive pair has |Δh| > 1. Terrain **flattened** along the path to `baseHeight`.

Phases:
1. Write target heights to heightmap for flatten cells.
2. Validate no cardinal neighbors exceed |Δh|=1 after changes. Revert if failed.
3. Refresh terrain visuals with `forceFlat`/`forceSlopeType` flags.
4. Refresh 8-neighbors (2 waves for cut-through, 1 for normal).

### 8.4 Diagonal Step Expansion

Converts diagonal path steps into two orthogonal steps (road prefabs only support cardinal movement).

### 8.5 Terraform Actions

| Action | Description |
|--------|-------------|
| `None` | No terrain change |
| `Flatten` | Set cell height to `plan.baseHeight` |
| `DiagonalToOrthogonal` | (Legacy / obsolete) |

### 8.6 River junction straightening

When multi-surface river contact is not cardinally straight enough for cascade cliffs and shore selection, terraform may cut rectilinear corridors. Re-run water visuals after height writes.

---

## 9. Road Prefab Selection on Terrain

### 9.1 Road Prefab Resolver

Three entry points:
1. **Full path** — uses `postTerraformSlopeType` from terraform plan. Handles elbows, T-intersections, crossings.
2. **Single cell with neighbor connectivity** — used for refresh after road changes.
3. **Ghost preview** — single cell for cursor preview.

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

Named after the **slope face direction** (downhill), matching `TerrainSlopeType`:

| `postTerraformSlopeType` | Road Prefab |
|--------------------------|-------------|
| `North` | `roadTilePrefabNorthSlope` |
| `South` | `roadTilePrefabSouthSlope` |
| `East` | `roadTilePrefabEastSlope` |
| `West` | `roadTilePrefabWestSlope` |
| Corner slopes (`NEUp`, etc.) | Decomposed to orthogonal axis aligned with travel (legacy / plan edge cases only — **new strokes** do not place roads on corner-up or pure diagonal **land**; see §13.10) |

---

## 10. Pathfinding Cost Model

A* with costs:

| Terrain | Base Cost | Notes |
|---------|-----------|-------|
| Flat | 1 | Strongly preferred |
| Diagonal slope | 35 | Moderate penalty |
| Cardinal slope | 60 | High penalty |
| Height diff = 1 | +25 | Climbing penalty |
| Water slope | 500 | Nearly impassable |
| Height diff > 1 | ∞ | Cannot step directly |

Interstate multiplies slope costs by 5 and adds penalties: turns (5), zigzags (500), away-from-goal (18). Straight continuation bonus: 15.

---

## 11. Water map, lakes, and persistence

### 11.1 Mental model

Water is **hosted by terrain**: each body has a **logical surface height** aligned with the carved bowl or channel. Visual placement uses surface − 1 in world space; sorting uses that visual height index (§2, §7).

### 11.2 Data structures

- **`WaterBody`:** `Id`, `SurfaceHeight`, occupied cell indices.
- **`WaterMap`:** `int[,]` body ids (0 = dry); `GetSurfaceHeightAt`. Lake/sea init may merge adjacent cells into one body. Touching bodies at the same surface may remain different ids; rivers do not post-merge adjacent segments (§11.7, §12.3).
- **`Cell` / `CellData`:** `WaterBodyType` (None, Lake, River, Sea); optional `secondaryPrefabName` for two-part shores; `waterBodyId` — same as `WaterBody.Id`. Open water cells match `WaterMap`. Dry water-shore/rim/ShoreBay tiles identify the winning adjacent body (highest adjacent S, then lowest id). Persisted on save.
- **`WaterMapData` (save v2):** body ids + serialized bodies; legacy `bool[]` water load still supported.

**Shore terminology:** **Open water** — registered in `WaterMap`. **Water-shore** — dry cell using transition prefabs. **Rim** — dry land above shore band, cliffs/slopes toward water. **ShoreBay** — axis-aligned inner-corner pattern. **ShoreLine** — contiguous dry cells sharing `waterBodyId` along an edge.

### 11.3 Lake generation

- **Depression-fill** from strict/window local minima, spill height, optional bounded basin pass, artificial axis-aligned fallback if count below target.
- Budget: hard cap or scaled; extra random seed attempts scale with map area.
- `LakeAcceptProbability` applied after spill feasibility.
- Lake feasibility carves minimal cardinal bowls so enough cells pass spill test.
- Extended maps: 40×40 designer template centered; Perlin + smoothing outside.
- Seeded RNG: seed from map generation seed; depression-fill and bowl shuffle use derived `System.Random`.

### 11.4 Valid lake — rules

Strict/window minima as seeds; flood under spill; per-body axis-aligned bbox in `[MinLakeBoundingExtent, MaxLakeBoundingExtent]` per axis (typically 2..10). Merged bodies may exceed one bbox. Sea at reference height 0; sea-level dry cells merged with `WaterMap`.

### 11.5 Save / load

- Save: `GameSaveData.waterMapData` from `WaterMap.GetSerializableData()`.
- Load: restore water map before grid restore; legacy path when `waterMapData` absent.
- Load does **not** run global slope restoration or sorting recalculation; snapshot applies saved prefabs and `sortingOrder` (§7.4).

### 11.6 Shore refresh after lakes

After water visuals update, shore terrain refresh updates land in the Moore neighborhood of new lake water (optionally a second Chebyshev ring for procedural rivers). Shore prefab logic: §4.2, §5.8–5.9; rim cliffs: §5.6–5.7.

### 11.7 Multi-body contact: bed alignment and junction merge

These rules separate **what players read as water level** from **terrain under water**, and define **Pass A** and **Pass B** before water placement when a height map is available.

1. **Several bodies, one plane:** Multiple `WaterBody` instances may share `SurfaceHeight` and sit adjacent with different ids. Water placement uses `SurfaceHeight` for a homogeneous visual plane per body.

2. **Bed vs surface:** Underwater `HeightMap` is the bed — may vary within a body. Logical surface is one value per body/segment. Shores and sorting use surface; cascades appear only where two different surfaces meet cardinally.

   **Lake at a surface step:** When `S_high > S_low` cardinally and either body is Lake, Pass A/B skip that edge and cascades are not placed. Sea is not treated as Lake. The edge is not a junction; perpendicular land uses normal lake shore selection. The upper Lake reads as a closed basin.

   **Lake-high vs river-low fallback:** Post-Pass-B removes those lake cells, sets height to lake's S, restores as dry land / lake-shore art. Shore refresh may clamp; height re-applied afterward.

3. **Pass A — Bed alignment:** Higher-surface water at cardinal contact with lower-surface water: bed lowered to min bed among adjacent lower neighbors (excluding Lake edges). One cell thick on upper side. Sweeps until stable. No `waterBodyIds` change.

4. **Pass B — Junction merge:** Reclassifies lower-side cells (excluding Lake edges). Assigns dry/water-shore to lower surface body. Absorbs upper-bank perpendicular dry cells. **Contact-bed reassignment:** upper-pool water reassigned to lower body when beds align or can be lowered. Sweeps until stable.

5. **Rivers downstream:** Bed does not increase toward exit (§12.4). Pass A/B must not violate upstream geometry beyond the contact strip.

6. **Update order:** Pass A → Pass B → Lake-river fallback → place water → cascade cliffs → shore terrain (Chebyshev-2 halo when Pass B merged or fallback ran).

7. **Shore affiliation at junctions:** Upper-brink → upper pool; lower-brink → lower pool; otherwise lowest S among Moore neighbors. Shore-end closure skipped on upper brinks.

### 11.8 River / multi-surface junction geometry (cascades, brinks)

A **junction** is cardinal contact edges where `S_high > S_low` (subject to Lake exclusion). Junctions decompose into straight strips; water–water cascades use cardinal prefabs only (§5.6.2). Pass A/B align beds and extend the lower surface.

**Brink roles (dry land):** `RiverJunctionBrinkRole` — **UpperBrink**: dry cell Moore-adjacent to the high-surface water of a River–River cardinal step. **LowerBrink**: dry cell cardinally adjacent to the low-surface cell. Lower tested before upper so cascade shore-closure tiles cardinally touching the low pool keep the lower body id.

#### 11.8.1 Shore neighbor topology vs junction diagonal prefabs

**Refresh order:** Water visuals → cascade cliffs → shore refresh (main pass → junction post-pass → upper-brink cliff stacks).

| Step | Rule |
|------|------|
| Registered water | `WaterMap.IsWater` is authoritative |
| Shore masks | Neighbor wet when registered water matches affiliated body; brink dry land excluded |
| Unaffiliated shore | Mask uses `WaterOrSeaAt` only |
| Cascades (main pass) | Default mask ignores brink dry land; forced diagonal slope for closest-to-cascade tile per brink component |
| Junction post-pass | Revisits brink-classified dry cells with extended mask (brink neighbors count as wet when affiliation matches); forces diagonal slope water along full cascade strip |
| Geography corners | Unrelated to junction; two-cardinal wet masks drive Bay vs slope via rectangle tests |
| Upper-brink cliff stacks | Placed on UpperBrink dry cells; same cardinal/mirror rules as cascade cliffs; Y anchor at shore grid cell water surface |

---

## 12. Procedural rivers

Rivers are **static** after geography init — no runtime fluid simulation.

### 12.1 Initialization order

Terrain → water map (lakes/sea) → **river pass** → interstate → forests → desirability, sorting.

### 12.2 Scope

- **In scope:** Pathfinding on generated terrain, shallow carve (default ≤ 2 steps), cardinal corridor, 1–3 rivers per New Game.
- **Out of scope:** Gameplay spill/flood, dynamic volume, drainage networks.

### 12.3 Merge and adjacency to lakes/sea

River merges only with River; Lake with Lake; Sea with Sea; Lake with Sea. After river generation, cardinally adjacent River cells at the same surface share one `WaterBody` id. Where river overlaps existing water, cells may be reassigned only when existing body's surface matches the river segment's surface; Lake at a different surface is not carved or reassigned (§11.7). Spacing from prior river corridors is enforced via Chebyshev dilation.

### 12.4 Geometry (implementation contracts)

| Topic | Rule |
|-------|------|
| **Symmetric banks** | `H_bank = H_bed + 1` (one step above shared bed floor) when possible |
| **Single bed height per section** | All bed cells in a section share one `H_bed` |
| **Surface segments** | `surface = H_bed + 1` (clamped); same surface → same `WaterBody`; new body on surface change |
| **Longitudinal monotonicity** | `H_bed(i+1) ≤ H_bed(i)` — river does not climb toward exit |
| **Entry / exit borders** | Random axis (N–S vs E–W) and flow sign (50/50); four equally likely cases |
| **Border margin** | Default 2: entry/exit in interior band |
| **Width** | Bed 1–3 cells; total = bed + 2 shores → {3,4,5}; |ΔW| ≤ 1; prefer ≥ 4 steps between width changes |
| **Length** | Max 1.5 × map dimension on flow axis |
| **Forced river** | If no viable candidate, carve basin and place forced river |
| **Spacing** | Prior corridors dilated in Chebyshev space; same-border entries ≥ 5 apart |

**Cardinal edges:** N–S or E–W flow between opposite borders; entry vs exit random; high/low from relief; lake/sea as logical exit when present.

### 12.5 Shore band continuity (inner corners)

After cross-section height application, a **corner promotion** pass runs on bed footprint cells. If a cell is at `H_bed` but has two perpendicular cardinal neighbors at `H_bed + 1`, it is raised to `H_bed + 1` so the dry shore strip stays continuous around inner L-corners.

Water assignment uses `HeightMap` after this pass: a bed cell is assigned water only if its height still equals the applied bed height (promoted corners stay dry).

---

## 13. Roads: manual draw, interstate, bridges, shared validation

### 13.1 Shared validation surface; two plan constructors

All persistent road placement must end with a **`PathTerraformPlan`**, Phase-1 checks where the plan applies, and **`Apply`** / prefab resolution — the same validation *surface* as other roads. **Do not** treat `ComputePathPlan` as the only gate: callers must not commit from raw `ComputePathPlan` output without that full path.

**Plan construction** may use either:

1. **`ComputePathPlan`** — default for filtered strokes (slope climb, cut-through, flatten neighbors as designed). Built inside `TryPrepareFromFilteredPathList` after bridge / FEAT-44 stroke checks on the filtered path.
2. **`TryBuildDeckSpanOnlyWaterBridgePlan`** (code name) — for **manual** draw when a **locked lip→exit chord** is active over water/shore (FEAT-44). Produces a plan with **no height mutations** (`TerraformAction.None` on path cells), `waterBridgeTerraformRelaxation`, and `waterBridgeDeckDisplayHeight` from the same assignment rules as the full pipeline. Phase-1 then skips strict cliff/water edges when there are no mutations (see `PathTerraformPlan`). This avoids false failures when the player’s **full polyline** (e.g. tail, round-trip) would otherwise force cut-through or |Δh| checks unrelated to the bridge core.

Both paths converge on **`TryValidatePhase1Heights`**, **`Apply`**, and **`RoadPrefabResolver.ResolveForPath`**.

| Mode | Validation | `forbidCutThrough` |
|------|-----------|-------------------|
| **Manual streets / preview** | Locked deck-span attempt, then longest valid prefix | `false` |
| **Interstate** | Full path | `true` |
| **AUTO streets** | Extend cardinal stroke when the next cell is water/slope; longest prefix plus programmatic deck-span; prefer deck-span when the stroke is wet or longer. Water crossings: all tiles placed in one tick or the plan is reverted (no partial bridges); firm dry exit required. Uniform `waterBridgeDeckDisplayHeight` for the whole deck span. | `false` |

### 13.2 Manual draw pipeline

1. Drag: rebuild stroke (optional **flex** + locked chord over water) → **try locked deck-span plan** → else **longest-valid-prefix** (filtered path → `ComputePathPlan`) → resolve prefabs → preview ghosts (no heightmap commit until release).
2. Release: final prep (same order as preview) → afford → **`Apply`** → place tiles → refresh junctions.

### 13.3 Slope climb vs carve

When no consecutive |Δh| > 1, ascending steps use `None` + `postTerraformSlopeType` so the road rides the slope. Gorge expansion only when not slope-climb. Adjacent-cliff validation with one-ring Phase 1 expansion.

### 13.4 Bridges and water approach

- Bridge span must be axis-aligned; no kinked water crossings.
- No elbows on water/water-slope; straight approach before water.
- Coastal terrain refresh uses terrain-only child destruction so bridge tiles survive.
- **Locked chord (manual):** when the stroke qualifies, `RoadManager` fixes a **straight cardinal chord** from lip through wet cells to far dry land at matching bridge height; preview/commit prefer a **deck-span-only** plan for that merged path so the deck sits at **display height** above water/cliffs without requiring the wet run to pass **cut-through** `ComputePathPlan`. Tail segments still obey stroke rules (e.g. no turn on water); invalid tails may be dropped by prefix search when not using the locked plan.
- **Programmatic chord (AUTO street segment):** `TryExtendCardinalStreetPathWithBridgeChord` appends the same `WalkStraightChordFromLipThroughWetToFarDry` span when the stroke ends on dry land and the next cardinal step is water or water-slope (shore), so planning sees the full crossing (high-deck first deck may sit on shore). `AutoRoadBuilder` then runs longest-prefix and programmatic deck-span; it **prefers** the deck-span result when the stroke contains wet/shore cells or it yields a longer expanded path, so a valid land-only prefix does not block the bridge. Curved A* connectors omit the extension (no fixed segment direction).
- **Cliffs vs deck:** elevated deck placement is driven by **lip / land-before-wet** height and resolver rules; absence of dedicated “cliff bridge” terraform does not block the span if the plan carries no terrain mutations and FEAT-44 height checks pass.

### 13.5 Interstate pathfinding

Border endpoint scoring → sorted candidates → dual A* (avoidHighTerrain true/false), picks cheaper. Penalties from §10.

### 13.6 Cut-through robustness

Reject when `maxHeight - baseHeight > 1`; map-edge margin; Phase 1 validation ring. Interstate always forbids cut-through.

### 13.7 Resolver rules

| ID | Rule |
|----|------|
| **A** | Elbow connectivity matches exactly two path neighbors |
| **B** | Prefab exits align with path in/out |
| **C** | Terraform wins: cut-through → flat prefabs from plan, not live slope misread |
| **D** | Prefer offset paths avoiding hills when costs close |
| **E** | Interstate prefers straight segments |
| **F** | Bridge approach perpendicular to water; no turn on last land cells before water |

On **legal** land cells (§13.10), slope/corner plan outputs use travel-aligned `postTerraformSlopeType` where applicable; terrain restore can force orthogonal ramp when action is None but plan has cardinal slope.

### 13.8 Optional polish (backlog)

- Crossroads: augment path-only neighbor checks with road-presence queries; final refresh over path cells.
- Pass `postTerraformSlopeType` into refresh after cut-through.
- Interstate vs slope sorting; border entry/exit prefabs.

### 13.9 AUTO simulation: pathfinding walkability, reservations, perpendicular growth (BUG-47)

These rules apply to **`AutoRoadBuilder`**, **`AutoZoningManager`**, and **`GridPathfinder`** / **`RoadCacheService`** during simulation ticks — not to manual street draw or generic `FindPath` used elsewhere.

1. **Undeveloped light zoning:** Cells with **R/C/I light zoning only** (`ResidentialLightZoning`, `CommercialLightZoning`, `IndustrialLightZoning`) and **no building** may be treated as land that AUTO roads can plan through and replace, using shared predicates (`AutoSimulationRoadRules`). Medium/heavy zoning and buildings are not expanded for AUTO walkability.
2. **Pathfinding:** Manual and legacy callers keep **`FindPath` / `FindPathWithRoadSpacing`** (grass and road only). AUTO simulation uses **`FindPathForAutoSimulation` / `FindPathWithRoadSpacingForAutoSimulation`**, which allow undeveloped light zoning subject to the same terrain / `CanPlaceRoad` gates as grass. Both paths also enforce **land slope walkability** (§13.10): non-walkable on pure diagonal and corner-up land slopes.
3. **Road frontier:** **`GetRoadEdgePositions`** treats a neighbor as expandable if it is grass, forest, sea-level, or undeveloped light zoning, so road cells remain growth candidates after lateral auto-zoning.
4. **Zoning reservations:** **`GetRoadExtensionCells`** and **`GetRoadAxialCorridorCells`** define cells where **`AutoZoningManager` must not place zones**, preserving axial strips for future street alignment. Extension cells may include the same expanded land types as in (3) when classifying the cell beyond an edge.
5. **Perpendicular vs parallel spacing:** When scoring a growth direction **perpendicular** to the dominant road axis at an edge, **`HasParallelRoadTooClose`** is called with **`excludeAlongDir`** set to that dominant axis so the parent street line is not mistaken for a separate parallel arterial.
6. **Commit path:** Placing AUTO roads still uses the shared validation surface (§13.1): `PathTerraformPlan`, `Apply`, prefab resolution — unchanged.
7. **Junction prefabs after batch `PlaceRoadTileFromResolved`:** `AutoRoadBuilder` accumulates placed cells per tick and calls **`RoadManager.RefreshRoadPrefabsAfterBatchPlacement`** once (deduped set: each new tile plus cardinal road neighbors). Skips bridge deck cells so FEAT-44 deck height is preserved. Single-tile **`PlaceRoadTileAt`** still uses per-placement **`UpdateAdjacentRoadPrefabsAt`**.

### 13.10 Land slope eligibility for road strokes (BUG-51)

**Allowed land** for any road stroke (manual, AUTO, interstate): **`TerrainSlopeType.Flat`** and **cardinal ramps only** (`North`, `South`, `East`, `West`). Pure diagonals and all `*Up` corner types are **disallowed**.

**Pipeline:** `RoadStrokeTerrainRules` truncates the filtered stroke at the first disallowed land slope inside `TryBuildFilteredPathForRoadPlan`; deck-span chord paths use the same truncation in `TryPrepareDeckSpanPlanFromAdjacentStroke`. Empty after truncation → no preview/commit (silent). **`TryPrepareRoadPlacementPlanLongestValidPrefix`** suppresses the generic “cannot extend further” warning when there is **no** slope-valid prefix on the raw stroke.

**Pathfinding:** `GridPathfinder` treats disallowed land slopes as non-walkable for grass/road/AUTO light-zoning cells (with the same water / `IsWaterSlopeCell` exceptions as the truncator — see `roads-system.md`).

**Interstate:** `InterstateManager.IsCellAllowedForInterstate` enforces the same land-slope rule on positive-height cells (water-slope shore cells remain allowed as before).

Canonical procedural detail: **`roads-system.md`** (Land slope stroke policy).

---

## 14. Lake / cliff / shore — engineering notes

### 14.1 Glossary

| Term | Meaning |
|------|---------|
| **Water surface / open water** | Registered water, sorted at visual surface height |
| **Water-shore (ramp)** | Land passing §4.2 gate → shore prefab selection |
| **Rim** | Land above shore cap → normal slopes + cliff walls |
| **Cliff wall stack** | Child prefab(s) on higher cell along shared cardinal edge |
| **Bay** | Shore corner prefab — neighbor patterns §5.9 |
| **Visible cliff faces** | South and east meshes only; N/W bits may still be set on `Cell.cliffFaces` |

### 14.5 Domain vocabulary — roads, grid, and spacing

> **Glossary index:** `glossary.md` cites this subsection as **geo §14.5**.

| Term | Meaning |
|------|---------|
| **Road stroke** | Ordered sequence of cells from a player drag or AUTO pathfinder for a road attempt. Filtered/truncated for land-slope and water rules before `PathTerraformPlan`. Procedural detail: `roads-system.md`. |
| **Bridge lip** | Last **firm dry** land cell before a **wet run** on a straight chord; anchor for locked deck-span preview/commit and deck display height (§13.4). |
| **Wet run** | Contiguous water and/or water-slope cells along a stroke crossed by a bridge plan. |
| **baseHeight** | Cut-through target elevation: path cells flattened to this value when not using scale-with-slopes (§8.3). |
| **Grass cell** | Undeveloped land substrate (typical grass `cellType`) — no road; zoning, forests, and manual A* treat it as buildable/walkable per mode (§13.9). |
| **Street (ordinary road)** | Non-**interstate** road placed by player or AUTO using the shared validation pipeline (§13.1–§13.2); contrasts with border interstate (§13.5). |
| **Map border / grid edge** | Cells on `x=0`, `y=0`, `maxX`, or `maxY`; interstate endpoints, virtual cliff feet, and exit rules reference these edges (§5.7, §13.5). |
| **Chebyshev distance** | `max(|Δx|,|Δy|)` on the grid; used to dilate river corridors and spacing between entries (§12.4). |

### 14.2 Resolved techniques

- S/E cliff face inspector nudges + water-shore Y offset fraction.
- Cliff sorting cap from registered water neighbors at lower isometric depth (prevents cliffs drawing over nearer water).

### 14.3 Symptom → direction

| ID | Symptom | Check |
|----|---------|-------|
| SS1 | Duplicate cliff + shore on same face | One-step suppression logic |
| SS2/SS4 | Stacked segments collapsed | Segment Y offset, prefab pivots |
| SS3 | Template bowls |Δh|>1 | Intentional; must match shore choice |
| SS5 | Z-fighting / cliffs under water | Sorting formula vs water plane |

### 14.4 Debug and picking

Terrain debug: enable `terrainDebugLogCellsEnabled`; default cells (28,24), (28,25), (34,24), (34,25); filter `[TerrainDebug]`. Height-aware picking uses sorting — steep stacks can mis-pick until cliff/shore order is stable.
