# Glossary — Territory Developer

> Quick-reference definitions for domain terms used across specs, rules, and code.
> Canonical specs are linked where applicable — always defer to the spec for full detail.

## Grid & Coordinates

| Term | Definition |
|------|-----------|
| **Cell** | MonoBehaviour on each grid tile GameObject. Holds `height`, `terrainSlopeType`, `cellType`, water body id, zone, building ref. |
| **CellData** | Plain serializable class mirroring Cell fields for save/load. |
| **cellArray** | `Cell[,]` — the logical grid. Access only via `GridManager.GetCell(x, y)`. |
| **gridArray** | `GameObject[,]` — the visual grid. Access only via GridManager. |
| **HeightMap** | `int[,]` terrain elevation. Must always match `Cell.height` (spec §2.4). |
| **sortingOrder** | Integer controlling 2D render depth. Formula in spec §7. |
| **tileWidth / tileHeight** | 1.0 / 0.5 world units. Isometric diamond projection (spec §1). |

## Terrain & Slopes

| Term | Definition |
|------|-----------|
| **TerrainSlopeType** | Enum: Flat, N, S, E, W, NE, NW, SE, SW + Up variants. Determined by neighbor heights (spec §3–§4). |
| **Cliff** | Vertical wall stack placed on south/east faces when `Δh ≥ 1` between neighbors (spec §5.7). |
| **Shore band** | Land cells Moore-adjacent to water; constrained: `height ≤ min(S)` of adjacent water (spec §2.4.1). |
| **Cascade** | Waterfall visual between distinct water surfaces at different heights (spec §5.6). |

## Water

| Term | Definition |
|------|-----------|
| **WaterMap** | Per-cell water body id storage + `WaterBody` list. Spec §11. |
| **WaterBody** | Data class: surface height `S`, body type (lake/river/sea), cell set. |
| **Surface height (S)** | The visual water level of a body. Water cells have `Cell.height = S` for rendering. |
| **H_bed** | River bed elevation — monotonically non-increasing toward exit (spec §12.4). |
| **Depression-fill** | Lake generation algorithm: fills terrain depressions up to spill height (spec §11). |

## Roads & Pathfinding

| Term | Definition |
|------|-----------|
| **PathTerraformPlan** | Plan object for a road path: per-cell terraform actions, heights, slope types. Supports `Apply` / `Revert`. |
| **Phase-1 validation** | Height consistency check on a `PathTerraformPlan` before commit (`TryValidatePhase1Heights`). |
| **Road preparation family** | `TryPrepareRoadPlacementPlan`, longest-valid-prefix, locked deck-span prep. The required pipeline for all committed road placement (spec §13.1). |
| **Cut-through** | Terraform mode that flattens terrain for a road. Forbidden when `maxHeight - baseHeight > 1` (spec §13.6). |
| **RoadPrefabResolver** | Selects correct road prefab based on neighbors, slope type, and path context (spec §9, §13.7). |
| **Deck span** | Bridge segment over water — axis-aligned, no elbows, uniform display height. |
| **RoadCacheService** | Cached road queries. Must call `InvalidateRoadCache()` after any road modification. |

## Zones & Buildings

| Term | Definition |
|------|-----------|
| **RCI** | Residential / Commercial / Industrial — the three zone types. |
| **Zone** | MonoBehaviour on zoned cells. Tracks zone type, level, building reference. |
| **Pivot cell** | The anchor cell of a multi-cell building. Non-pivot cells reference the pivot. |

## Simulation & Growth

| Term | Definition |
|------|-----------|
| **Simulation tick** | Periodic cycle driven by `TimeManager` → `SimulationManager`. Executes AUTO systems in strict order. |
| **AUTO systems** | `AutoRoadBuilder` → `AutoZoningManager` → `AutoResourcePlanner`. Extend city automatically each tick. |
| **UrbanCentroidService** | Computes urban centroid + ring metrics for AUTO road/zoning targeting. Active system (not obsolete). |
| **GrowthBudgetManager** | Caps growth per category per tick to prevent runaway expansion. |
| **UrbanizationProposal** | **OBSOLETE** — never re-enable (TECH-13). |

## Persistence

| Term | Definition |
|------|-----------|
| **GameSaveData** | Root serialization class: `List<CellData>` + `WaterMapData`. |
| **WaterMapData** | Nested type in `WaterMap.cs` for water body serialization. |
| **Visual restore** | Load applies saved prefabs and `sortingOrder` directly — no global slope/sort recalc (spec §7.4). |

## Architecture Patterns

| Term | Definition |
|------|-----------|
| **Manager** | MonoBehaviour scene component. Never `new`. Wired via Inspector + `FindObjectOfType` fallback. |
| **Helper service** | Extracted logic class (e.g., `GridPathfinder`, `TerraformingService`). Keeps managers lean. |
| **GridManager** | Central hub for all cell operations. Do not add responsibilities — extract to helpers. |
| **GameNotificationManager** | The only singleton in the project. Access via `.Instance`. |

## Backlog Conventions

| Term | Definition |
|------|-----------|
| **BUG-XX** | Bug / broken functionality |
| **FEAT-XX** | Feature / enhancement |
| **TECH-XX** | Technical debt / refactor |
| **ART-XX** | Art assets / prefabs / sprites |
| **AUDIO-XX** | Audio assets / audio system |
