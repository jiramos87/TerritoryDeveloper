# Water System Refactor — Technical Overview

> **Backlog:** [FEAT-37](../../BACKLOG.md) (Medium priority) · **Planning pass:** [TECH-12](../../BACKLOG.md) (define objectives, rules, bugs, scope, child issues before implementation)  
> **Status:** **FEAT-37a** / **FEAT-37b** / **FEAT-37c** **completed** (water save/load snapshot per [BACKLOG](../../BACKLOG.md)). Shore defects: [BUG-33](../../BACKLOG.md); cliff placement + foreground-water sorting: [BUG-39](../../BACKLOG.md) / [BUG-40](../../BACKLOG.md) **completed** 2026-03-24 — [bugs/cliff-water-shore-sorting.md](bugs/cliff-water-shore-sorting.md). Load building sort: [BUG-34](../../BACKLOG.md) and [BUG-35](../../BACKLOG.md) **completed** (2026-03-22).  
> **Related:** [BUG-08](../../BACKLOG.md) (generation polish), [FEAT-15](../../BACKLOG.md) (ports / sea), bridge specs (e.g. `.cursor/specs/bridge-and-junction-fixes.md`)

## 1. Problem synthesis

Today, water is effectively tied to a **single global water level** (conceptually “height 0”): procedural bodies and the drawing tool place water as a **flat surface** at that level. Terrain around it uses the heightmap and slopes, so **lakes often read as deep pits** with the water plane at the bottom, dark vertical gaps, and weak embankment visuals. Raising the apparent water surface by even **one height step** would already improve readability; the real goal is larger: **water should be modeled as water masses hosted by terrain**—i.e. persistence of water where the terrain allows—**not** as a universal Z-plane independent of local elevation.

This is a **major feature epic** with a **large refactor** of `WaterManager`, `WaterMap`, cell/water data, rendering/sorting, and downstream systems (roads/bridges, zoning/buildings near water, forests, demand/services, save/load).

## 2. Goals

1. **Per-body or per-cell water surface elevation** — Water bodies can exist at **multiple height levels**, aligned with local terrain so lakes sit naturally in bowls or on plateaus without mandatory “bottomless pit” framing.
2. **Unify the mental model** — All water sources (procedural seas/lakes/rivers, player-painted water) are **the same abstraction**: stored water in cells that the terrain can **hold** (volume + surface height), not a special global layer only at y=0.
3. **Geological variety (conceptual, can be phased)** — Support distinguishing, in data and eventually gameplay/visuals:
   - **Cliffs and deep sinks** vs **shallow basins**
   - **High-altitude lakes** (mountain / mesa)
   - **Directed flow** — Rivers from higher to lower terrain (gradient-based paths)
   - **Coastal / sea** regions with **tide direction** (or dominant swell), distinct from inland still water
4. **Reduce coupling** — Keep `GridManager` thin; extend or add helpers (`WaterMap`, future `WaterSurfaceService` or similar) per project rules.

## 3. Current architecture touchpoints

| Area | Role today |
|------|------------|
| `GeographyManager.InitializeGeography()` | Order: terrain → **water** → forests → grid … |
| `WaterManager` / `WaterMap` | `WaterMap` body ids + `WaterBody` surface heights; depression-fill lakes; sea-level merge; paint uses `LegacyPaintWaterBodyId`. Lake **shore tiles** in **FEAT-37a**; sorting/roads/bridges/`SEA_LEVEL` generalization **FEAT-37b** (done); remaining shore defects [BUG-33](../../BACKLOG.md). |
| `TerrainManager` / `HeightMap` | Elevations and slopes; water must **agree** with these per cell |
| `GridManager` | Cell visuals, sorting; water cells interact with terrain and overlays |
| `Cell` / `CellData` | Serialization; any new water fields must round-trip in save/load |
| `ZoneManager`, `ForestManager`, `RoadManager` | Adjacency rules, bridges, future slope water |

Initialization and dependency overview: see root `ARCHITECTURE.md` (Terrain layer, Geography flow).

## 4. Non-goals (initial draft)

- Final art for every water type (may reuse flat tiles at first).
- Full fluid simulation (Navier–Stokes); **direction** and **height** can be **data-driven** first.
- Shipping all sub-features in one PR; this should be **incremental**.

## 5. Suggested phases (for planning only)

Phases are **not** committed ordering until a dedicated design pass; they split risk.

| Phase | Theme | Outcomes (examples) |
|-------|--------|---------------------|
| **A** | Data model | Water surface height per body or per cell in `WaterMap` / cell flags; migration path for saves |
| **B** | Visual / sorting | Correct draw order vs terrain at same cell height; fewer “void” artifacts at lake edges |
| **C** | Tools & generation | Procedural + paint tool respect new model; optional default level above legacy “0” for readability |
| **D** | Flow & coast | River graph or flow field; sea edge + tide **direction** as data (animation later) |
| **E** | Gameplay integration | Bridges, buildings on/adjacent water, forests — align rules with new model |

**Slope water** (water following or crossing sloped terrain) is **high complexity**—likely late phase, may share logic with road/terrain slope handling and sorting.

## 6. Risks and open questions

- **Save compatibility:** Version `CellData` or water chunk format when adding height/body IDs.
- **Performance:** Larger water graphs (flow) may need caching; avoid per-frame `FindObjectOfType`.
- **Sorting:** Water at arbitrary heights must stay consistent with `GridSortingOrderService` and multi-cell buildings.
- **Bridges / interstate:** Overlap with road prefab and terraform validation — coordinate with `RoadManager` / bridge specs.
- **Lake shore visuals:** Incorrect tiles, gaps, or sorting at lake edges — [BUG-33](../../BACKLOG.md) (outside **FEAT-37b** scope; 37b completed without new shore work).
- **Minimap water layer:** [BUG-32](../../BACKLOG.md) **completed** (2026-03-23); optional height shading remains **FEAT-42**.
- **Console noise:** Startup logs `[LakeGeneration]` (`WaterMap` lake fill) and `[LakeBasins]` (`TerrainManager` spill-passing target) are for diagnostics; strip or gate behind a debug flag for release if needed.

## 7. Documentation maintenance

**Before** large implementation (see **TECH-12** in [BACKLOG.md](../../BACKLOG.md)): lock **objectives**, **rules**, **in-scope bugs**, **non-goals**, and **phased child issues** here and in **FEAT-37**.

When implementation starts:

- Update this spec with **decided** data structures and **public APIs** (FEAT-37a summary: §9 below).
- Update `ARCHITECTURE.md` (Terrain layer / Geography bullets) if initialization order or dependencies change.
- Add a row under `AGENTS.md` “What to Read” if the primary entry point shifts from “only `WaterManager`” to a new helper.

## 8. References

- `ARCHITECTURE.md` — Initialization, `WaterManager` dependencies  
- `BACKLOG.md` — **FEAT-37**, **FEAT-37a–c**, **FEAT-38–41**, **BUG-08**, **BUG-33**, **FEAT-15**  
- **FEAT-38 (rivers):** `.cursor/specs/rivers.md` — definitions, scope, progress (living spec; Phase D alignment)  
- `.cursor/rules/managers-guide.mdc` — Manager responsibilities  
- Bridge / junction context: `.cursor/specs/bridge-and-junction-fixes.md`

## 9. Implementation notes (FEAT-37a)

- **`WaterBody`:** `Id`, `SurfaceHeight`, set of flattened cell indices.
- **`WaterMap`:** `int[,]` body ids (0 = dry); `InitializeLakesFromDepressionFill(HeightMap, LakeFillSettings, seaLevelForArtificialFallback)`; `GetSurfaceHeightAt`; merge adjacent bodies with same surface; `WaterMapData` format v2 (`waterBodyIds` + `WaterBodySerialized[]`); legacy `bool[]` load supported.
- **Procedural lake budget (hard cap vs area-scaled):** **`UseScaledProceduralLakeBudget`** (default **`false`**) controls whether the target body count depends on map area. When **`false`**, `GetEffectiveMaxLakeBodies` returns **`Clamp(ProceduralLakeBudgetHardCap, 1, MaxLakeBodies)`** — same target at any map size (until **FEAT-18** exposes this in UI). When **`true`**, the target is **`min(ProceduralLakeBudgetHardCap, GetAreaScaledLakeBudgetDiagnostic(...))`**, where the diagnostic scales `ProceduralLakeBudgetAtReference` by area vs `ReferenceMapSide²` and caps at `MaxLakeBodies`. **`GetAreaScaledLakeBudgetDiagnostic`** remains useful in logs even when scaling is off. Extra random seed attempts scale with map area (`GetScaledRandomExtraSeedAttempts`). Seeds are ordered by spill headroom (`spill − floor`). If procedural passes still yield fewer bodies than the target, **artificial fallback** carves axis-aligned rectangles (extent in `[MinLakeBoundingExtent, MaxLakeBoundingExtent]`), registers a `WaterBody`, and exposes an **inclusive dirty rect** so `TerrainManager.ApplyHeightMapToRegion` can refresh only touched cells. **Source defaults** for `LakeFillSettings` (not Inspector-serialized): e.g. **`MinLakeCells` = 4**, **`MinLakeBoundingExtent` / `MaxLakeBoundingExtent` = 2..10**, **`ProceduralLakeBudgetHardCap` = 4** — verify in `WaterMap.cs` if tuning.
- **`LakeAcceptProbability`:** Applied **after** the spill check (`spill` > terrain height at seed). Applying random rejection **before** spill would discard rare valid minima (e.g. only two strict minima on a 128×128 map) with no hydrological benefit. Default **1**; lower to thin seeds when many feasible minima exist.
- **Extended terrain:** After Perlin fill and 3×3 smoothing, `TerrainManager` applies sparse fine-scale height dips outside the centered 40×40 template so depression-fill can find valid seeds on large maps.
- **Guaranteed lake capacity (terrain):** `LakeFeasibility` mirrors `WaterMap` plateau spill (same-height 4-connected component; rim minimum; `OutsideMapSpillHeight` = 6 for off-map). After `LoadInitialHeightMap`, `TerrainManager.EnsureGuaranteedLakeDepressions` raises the count of spill-passing cells until **`passing >= min(2 × ProceduralLakeBudgetHardCap + LakeFeasibilityExtraBowls, w × h)`** (at least one). It **shuffles** the full interior cell list each round and carves **`CarveMinimalCardinalBowl`** wherever `PassesSpillTest` is false, recounting after each carve, until the target is met or no progress (if **w** or **h** is below 3 there is no interior; carving is skipped). Actual **water bodies** still come from `WaterMap.InitializeLakesFromDepressionFill` + artificial fallback, not from this pass alone.
- **Shore / slopes:** After `WaterManager.UpdateWaterVisuals`, `TerrainManager.RefreshLakeShoreAfterLakePlacement` updates land cells in the Moore neighborhood of lake water. `DetermineWaterShorePrefabs` treats **water** as cardinal/diagonal neighbors where `WaterMap.IsWater` **or** terrain height is sea level. **Perpendicular shore corners:** whenever **both** cardinals of a quadrant have water (NE, NW, SE, SW), pick **Bay** then diagonal **SlopeWater** — order **SE, SW, NE, NW** so ambiguous triples (e.g. N+E+S) resolve consistently. **Diagonal-only water:** **Priority:** (1) **Bay** when the diagonal water cell `W` is the **outer corner** of an axis-aligned rectangle (`IsAxisAlignedRectangleCornerWater*` — no water on the two cardinals **beyond** `W` away from the shore, i.e. the lake does not continue along that diagonal); (2) else **Upslope + SlopeWater** if `HasLandSlopeIgnoringWater`; (3) else **single Bay** (flat terrain along a diagonal lake edge). Rectangle corner is evaluated **before** land-slope so straight corners are not overridden by a higher land neighbor. `GetNeighborWaterVisualHeightForShore` uses cardinals first, then diagonal water neighbors (external corners). `PlaceWaterShore` uses neighbor water visual height (surface − 1). **Cardinal** water slopes (N/E/S/W) use that position with no extra Y. **Bay** extra Y is **0** (tune in code if art needs nudge). **Diagonal** Upslope/SlopeWater shore tiles add extra Y `(landHeight − waterVisualHeight) × tileHeight × 0.25` so alignment matches per-level terrain steps when the shore cell is above the water visual plane. Bay tiles use `CalculateBayShoreSortingOrder` in `GeographyManager` / `GridSortingOrderService`.
- **Lake rim cliffs:** `GetCliffWallDropNorth/South/East/West` / `PlaceCliffWallStack`; water classification uses **`WaterManager.IsWaterAt`**, not `SEA_LEVEL` alone. **One-step** faces toward **water / `IsWaterSlopeCell`** are suppressed via `ShouldSuppressCliffFaceTowardLowerCell`; **multi-step (Δh ≥ 2)** rim drops still stack on **visible** (south/east) faces. Segments fully below the adjacent water surface (`GetWaterSurfaceHeight` at the cliff foot) are culled. `Cell.cliffFaces` stores logical `CliffFaceFlags` (N/S/E/W) for hydrology; **north/west** faces skip prefab meshes. Narrow strip + one-step toward open water still uses `ShouldSuppressCliffTowardCardinalLower`; ramp vs rim via `IsWaterShoreRampTerrainCell`; island skip via `IsCellSurroundedByCardinalWaterOnly` (cardinal `IsWaterAt` only). Shore corners: convex (rectangle-outer) vs concave/perpendicular paths in `DetermineWaterShorePrefabs` (see shore bullet above).
- **Diagnostics:** `WaterMap.InitializeLakesFromDepressionFill` emits **`[LakeGeneration]`** `Debug.Log` / `LogWarning` (target bodies, procedural/bounded/artificial passes, final count). `EnsureGuaranteedLakeDepressions` emits **`[LakeBasins]`** lines for spill target vs initial/final passing count.
- **`WaterManager`:** `useLakeDepressionFill`, code-only `LakeFillSettings` (via `LakeFillSettings` property / internal instance), `GetWaterSurfaceHeight`; `PlaceWater` keeps **logical** `SurfaceHeight` = spill in `WaterMap` while positioning the water tile at **surface − 1** in world space (Option A); sorting uses that visual height. Paint uses **`LegacyPaintWaterBodyId`** (10001). Lake fill tuning lives in **`LakeFillSettings`** defaults in source — not serialized on the component until terrain generator UI. After lake init, **`[WaterManager]`** logs a one-line summary (`LastLakeGeneration*` fields).
- **FEAT-37c (save / load):** `GameSaveData` includes **`WaterMapData`** from `WaterMap.GetSerializableData()`. **`WaterManager.RestoreWaterMapFromSaveData`** hydrates `WaterMap` before **`GridManager.RestoreGrid`**; legacy saves without `waterMapData` use **`RestoreFromLegacyCellData`**. **`Cell` / `CellData`** add **`WaterBodyType`** (None, Lake, River, Sea) and optional **`secondaryPrefabName`** for two-part lake shores. **Load** does not call **`RestoreWaterSlopesFromHeightMap`**, **`RestoreTerrainSlopesFromHeightMap`**, or **`ReCalculateSortingOrderBasedOnHeight`**; **`RestoreGridCellVisuals`** applies saved **`sortingOrder`** and prefab names. **Non-water follow-ups (completed 2026-03-22):** **[BUG-34](../../BACKLOG.md)** (deterministic cell restore order, building sort post-pass, water/shore sorting alignment); **[BUG-35](../../BACKLOG.md)** (`GridManager.DestroyCellChildren(..., destroyFlatGrass: true)` when **`ZoneManager`** / **`BuildingPlacementService`** place or restore RCI and utility buildings so a flat grass child is not left under the building; **`GridSortingOrderService.SetZoneBuildingSortingOrder`** still runs per-footprint **`SyncCellTerrainLayersBelowBuilding`** for grass-only children). Archived agent prompt: [`archive/agent-prompt-load-game-building-sorting-order.md`](archive/agent-prompt-load-game-building-sorting-order.md) (historical reference only).
- **Artificial lakes on small maps:** Edge margin from the map border for rectangle placement shrinks on tiny grids (0–2 cells). Random/deterministic fallback carves rectangles with **the same per-axis bbox limits as procedural fill** (`MinLakeBoundingExtent`–`MaxLakeBoundingExtent`, default max **10** per axis). A **last-resort** pass tries a bounded square at the four corners (extent capped by `MaxLakeBoundingExtent`).
- **`TerrainManager`:** Original 40×40 template no longer uses `0` as carved water; procedural lake/river **height carve** removed. On grids **larger than 40×40**, the template is **centered**; procedural terrain fills the **surrounding** region (not locked to the top-left corner). Small 3×3 bowl in template data remains only as legacy **height** variation; **lake validity** is procedural (see §10), not template-dependent.

## 10. Lake validity and sea rules (agreed)

- **Valid lake (procedural):** A `WaterBody` created by depression-fill must satisfy **`LakeFillSettings`**: strict and **window** local minima as seeds (window minima require **some** higher terrain in the window—flat plateaus are not seeds); flood-fill basin under **spill** height; optional **bounded local depression** pass (larger window, max basin cell count); **axis-aligned bounding box** of occupied cells must have **width and height in [`MinLakeBoundingExtent`, `MaxLakeBoundingExtent`] grid cells per axis** (defaults in source, typically **2..10** per axis). Bodies that merge afterward may exceed a single bbox—acceptable until merge rules are tightened.
- **Shore prefab polish (known follow-up):** Visual defects at lake edges (wrong tile, gaps, sorting) — [BUG-33](../../BACKLOG.md); deeper notes [bugs/cliff-water-shore-sorting.md](bugs/cliff-water-shore-sorting.md). Cliff wall alignment and cliff-vs-foreground-water sorting: [BUG-39](../../BACKLOG.md), [BUG-40](../../BACKLOG.md) **completed** 2026-03-24 (**FEAT-37b** did not include shore prefab scope).
- **Sea:** Reference surface at **height 0** (`seaLevel`); `MergeSeaLevelDryCellsFromHeightMap` aligns terrain sea cells with `WaterMap`. **Future:** player terraform may leave dry land below sea reference; **not** required for MVP.
- **Lakes at height 0:** Allowed if hydrologically disconnected from the **sea** body (same rules as any other lake: containment + successful fill).
- **FEAT-37 epic (MVP children):** **FEAT-37a**, **FEAT-37b**, and **FEAT-37c** are **completed** (see `BACKLOG.md`). Follow-up work is not part of that closure: shore polish **[BUG-33](../../BACKLOG.md)**; cliff placement / foreground-water sorting **[BUG-39](../../BACKLOG.md)** / **[BUG-40](../../BACKLOG.md)** completed 2026-03-24. Load-time building / grass under buildings: **[BUG-34](../../BACKLOG.md)** and **[BUG-35](../../BACKLOG.md)** completed (2026-03-22).
- **Minimap relief:** Out of scope for water MVP; tracked as **FEAT-42** in `BACKLOG.md`.
- **Minimap water layer:** Logical water alignment with the main map was **[BUG-32](../../BACKLOG.md)** (completed 2026-03-23). Optional **height / relief** on the minimap is **FEAT-42**.
