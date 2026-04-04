# Computational utilities — inventory (phase 1)

High-level map of **RNG**, **pathfinding**, **desirability**, **centroid** / **ring**, and **terrain** keywords under `Assets/Scripts/Managers/` and `Assets/Scripts/Utilities/`. Refine per wave; not every symbol is listed.

| File | Symbol / area | Spec term (glossary / spec) | Pure? | Risk |
|------|----------------|----------------------------|-------|------|
| `MapGenerationSeed.cs` | Seed plumbing | **Geography initialization** | Maybe | Tied to **New Game** flow |
| `GeographyManager.cs` | Init orchestration | **Geography initialization** | N | **MonoBehaviour** state |
| `TerrainManager.cs` | Terraform / heights | **HeightMap**, **Cell.height** | N | **HeightMap** sync, **shore** |
| `ProceduralRiverGenerator.cs` | River profile / **H_bed** | **geo** §12, **H_bed** monotonicity | Maybe | **RNG** + **GridManager** reads |
| `GridPathfinder.cs` | **A***, costs | **Pathfinding cost model** **geo** §10 | Maybe | Second-authority risk (**Wave C** extraction) |
| `GridManager.cs` | **GetGridPosition** / **GetWorldPositionVector** | **World ↔ Grid conversion** **geo** §1 | Y (delegates **`IsometricGridMath`**) | Height-aware picking stays here |
| `UrbanMetrics.cs` / `UrbanCentroidService.cs` | **Centroid**, **urban growth rings** | **sim** §Rings | Partial (**UrbanGrowthRingMath**) | **AUTO** parity |
| `DemandManager.cs` / `CityStats.cs` | **Desirability** | **managers-reference** | N | ≠ **geo** §10 costs |
| `WaterManager.cs` / `WaterMap.cs` | **Open water**, **S** | **water-terrain**, **geo** §11 | N | **RefreshShoreTerrainAfterWaterUpdate** |
| `LakeFeasibility.cs` | Depression / fill | **water body** | Maybe | Init-time only |
| `ForestManager.cs` | Placement | **Forest** | N | **RNG** |
| `AutoRoadBuilder.cs` | **AUTO** roads | **road preparation family** | N | Must stay on validation pipeline |
| `Utilities/Compute/IsometricGridMath.cs` | Planar ↔ grid | **World ↔ Grid conversion** | **Y** | Golden **`world-to-grid.json`** |
| `Utilities/Compute/UrbanGrowthRingMath.cs` | Ring bands | **Urban growth rings** | **Y** | Multipolar = min-distance to poles |
| `Utilities/Compute/GridDistanceMath.cs` | **Chebyshev** / Manhattan | **grid** metrics (not path commit) | **Y** | Do not conflate with **A*** costs |

## Blockers / notes

- **Pure** extraction must not introduce a second **road** / **pathfinding** authority; **Wave C** keeps **GridPathfinder** as owner of committed search.
- **RNG** derivation for **geography init** is **TBD** in **`compute-utilities-rng-derivation.md`** (**Wave D**).
- Review **sim** §Rings + **geo** §10 before **Wave C** / **D** refactors.
