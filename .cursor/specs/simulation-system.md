# Simulation System — Reference Spec

> Deep reference for the automatic simulation pipeline: tick order, AUTO systems, growth, and dependencies.

## Tick execution order

`TimeManager` → `SimulationManager.ProcessSimulationTick()` runs these in **strict order**:

1. `GrowthBudgetManager.EnsureBudgetValid` (when present)
2. `UrbanCentroidService.RecalculateFromGrid` — urban centroid + ring metrics
3. `AutoRoadBuilder` — extends **street** network
4. `AutoZoningManager` — zones cells adjacent to **streets**/**interstates**
5. `AutoResourcePlanner` — plans resource buildings (water, power)

### Urban centroid and growth rings

> **Glossary index:** `glossary.md` cites this subsection as **sim §Rings**.

Each tick, `UrbanCentroidService.RecalculateFromGrid` updates the **urban centroid** (development-weighted center of the city) and **ring metrics** — distance bands from that center. `AutoRoadBuilder` and `AutoZoningManager` use centroid and rings to bias growth (typically stronger near the core, weaker in outer rings; tuning in backlog e.g. FEAT-43). Ring logic is separate from the obsolete UrbanizationProposal system (see below).

## System dependencies

| System | Dependencies |
|--------|-------------|
| `AutoRoadBuilder` | GridManager, RoadManager, TerrainManager, GrowthBudgetManager, CityStats, InterstateManager |
| `AutoZoningManager` | GridManager, ZoneManager, GrowthBudgetManager, CityStats, DemandManager |
| `AutoResourcePlanner` | CityStats, GridManager, GrowthBudgetManager, UIManager |
| `GrowthManager` | GridManager, DemandManager |
| `GrowthBudgetManager` | CityStats |
| `UrbanCentroidService` | GridManager (reads cell data for centroid computation) |

## Road reservation for AUTO zoning (BUG-47)

Each tick, `AutoZoningManager` builds a set from `GridManager.GetRoadExtensionCells()` and `GetRoadAxialCorridorCells()` and does **not zone** those cells, so axial strips stay clear for `AutoRoadBuilder`. See geography spec §13.9.

## AUTO street placement rules

- AUTO **streets** use the same **road validation pipeline** as manual **street** draw: `PathTerraformPlan` + Phase-1 + `Apply`.
- Water crossings require full segment budget in one tick; `AutoRoadBuilder` reverts if it cannot place every tile.
- After batch placement, junction prefabs are refreshed via `RefreshRoadPrefabsAfterBatchPlacement` (once per tick, deduped).

## Obsolete system — UrbanizationProposal

`UrbanizationProposalManager` and related proposal UI are **obsolete** — intentionally not called from `ProcessSimulationTick()`. **NEVER re-enable.** Full removal tracked as TECH-13 in `BACKLOG.md`.

`UrbanCentroidService` and ring-based AUTO growth **remain supported** — they are NOT part of the obsolete proposal system.

## Key files

| File | Role |
|------|------|
| `SimulationManager.cs` | Tick orchestrator |
| `AutoRoadBuilder.cs` | Automatic **street** network extension |
| `AutoZoningManager.cs` | Automatic zoning adjacent to **streets**/**interstates** |
| `AutoResourcePlanner.cs` | Automatic resource building placement |
| `GrowthManager.cs` | Zone growth logic |
| `GrowthBudgetManager.cs` | Per-category growth budget |
| `UrbanCentroidService.cs` | Urban centroid and ring metrics |
| `TimeManager.cs` | Game speed, tick scheduling |
