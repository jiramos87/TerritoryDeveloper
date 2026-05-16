---
purpose: "Reference spec for Simulation System — Reference Spec."
audience: agent
loaded_by: router
slices_via: spec_section
---
# Simulation System — Reference Spec

> Deep reference for automatic simulation pipeline: tick order, AUTO systems, growth, dependencies.

## Tick execution order

`TimeManager` → `SimulationManager.ProcessSimulationTick()` runs these in **strict order**:

1. `GrowthBudgetManager.EnsureBudgetValid` (when present)
2. `UrbanCentroidService.RecalculateFromGrid` — urban centroid + ring metrics
3. `AutoRoadBuilder` — extends **street** network
4. `AutoZoningManager` — zones cells adjacent to **streets**/**interstates**
5. `AutoResourcePlanner` — plans resource buildings (water, power)

City-sim depth Bucket 2 inserts **signal phase** between steps 2 + 3 (producers → separable Gaussian diffusion → district rollup → consumers, run by `SignalTickScheduler`). Contract + 12-entry inventory + rollup taxonomy → [`simulation-signals.md`](simulation-signals.md).

### Urban centroid + growth rings

> **Glossary index:** `glossary.md` cites subsection as **sim §Rings**.

Each tick, `UrbanCentroidService.RecalculateFromGrid` updates **urban centroid** (development-weighted center of city) + **ring metrics** — distance bands from that center. `AutoRoadBuilder` + `AutoZoningManager` use centroid + rings to bias growth (typically stronger near core, weaker in outer rings; tuning in [`BACKLOG.md`](../../BACKLOG.md)). Ring logic separate from obsolete UrbanizationProposal system (see below).

## System dependencies

| System | Dependencies |
|--------|-------------|
| `AutoRoadBuilder` | GridManager, RoadManager, TerrainManager, GrowthBudgetManager, CityStats, InterstateManager |
| `AutoZoningManager` | GridManager, ZoneManager, GrowthBudgetManager, CityStats, DemandManager |
| `AutoResourcePlanner` | CityStats, GridManager, GrowthBudgetManager, UIManager |
| `GrowthManager` | GridManager, DemandManager |
| `GrowthBudgetManager` | CityStats, EconomyManager |
| `UrbanCentroidService` | GridManager (reads cell data for centroid computation) |

## Road reservation for AUTO zoning

Each tick, `AutoZoningManager` builds set from `GridManager.GetRoadExtensionCells()` + `GetRoadAxialCorridorCells()` + does **not zone** those cells, so axial strips stay clear for `AutoRoadBuilder`. See geography spec §13.9.

Segment-strip zoning covers perp strips at `k = 0` (origin end, true endpoints only — T-joint origins skipped) through `k = L - 1` (far end). Cells at segment endpoints skipped in prior builds, or cells previously inside road reservation that later exit it as city grows, reconsidered by post-tick frontier re-scan: `AutoZoningManager` iterates all road-edge positions + attempts `CanZoneCell` on each cardinal neighbor under remaining tick budget. Reservation cells remain untouchable throughout.

## AUTO street placement rules

- AUTO **streets** use same **road validation pipeline** as manual **street** draw: `PathTerraformPlan` + Phase-1 + `Apply`.
- Water crossings require full segment budget in one tick; `AutoRoadBuilder` reverts if it cannot place every tile.
- After batch placement, junction prefabs refreshed via `RefreshRoadPrefabsAfterBatchPlacement` (once per tick, deduped).

## Obsolete system — UrbanizationProposal

`UrbanizationProposalManager` + related proposal UI = **obsolete** — intentionally not called from `ProcessSimulationTick()`. **NEVER re-enable.** Full removal tracked on [`BACKLOG.md`](../../BACKLOG.md); **glossary** **Urbanization proposal**.

`UrbanCentroidService` + ring-based AUTO growth **remain supported** — NOT part of obsolete proposal system.

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
| `EconomyManager.cs` | Daily/monthly economy: **tax base** and **monthly maintenance** on calendar day 1 (see **glossary** **Monthly maintenance**, **managers-reference** §Demand) |

## Calendar and monthly economy

Outside `ProcessSimulationTick`, `TimeManager` advances **simulation** date each in-game **day** + calls `CityStats.PerformDailyUpdates()` **before** zoning placement + `ProcessSimulationTick()`. Daily pass updates employment, statistics, forest stats, pollution, **happiness** (target + lerp), then refreshes R/C/I **demand** so **tax** + **happiness** targets apply on **same** day to **demand** (see **managers-reference** **Demand (R / C / I)**).

On calendar day 1, after daily pass, `TimeManager` also calls `EconomyManager.ProcessDailyEconomy()`. On first day of each in-game calendar month, `EconomyManager` runs **tax base** collection then **monthly maintenance** (order matters: income before upkeep). Separate from per-tick **AUTO** work but shares **CityStats** treasury + **game notification** feedback.
