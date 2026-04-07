# FEAT-52 — City services coverage model (fire, police, education, health)

> **Issue:** [FEAT-52](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Implement a generic service coverage system where each service building (fire station, police station, school, hospital) has a coverage radius computed from the road network. Cells within coverage receive happiness and desirability bonuses; cells outside suffer penalties. Coverage gaps are visible on the minimap as "danger zones." This is the foundational framework that FEAT-11 (education), FEAT-12 (police), and FEAT-13 (fire) will build upon.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Generic `ServiceCoverageManager` that computes coverage maps for any service type
2. Coverage radius based on road network distance (not Euclidean) from service buildings
3. Per-cell coverage score (0.0 = no coverage, 1.0 = full coverage) with distance-based decay
4. Coverage affects happiness (positive in covered areas, penalty in uncovered populated areas) and desirability (bonus for proximity to services)
5. Minimap layer showing coverage heatmap per service type
6. Integration with demand calculation — areas with poor coverage generate less residential demand
7. At least one concrete service type shipped (e.g., fire station) as proof of concept

### 2.2 Non-Goals (Out of Scope)

1. Specific service gameplay mechanics (fire spread, crime events, education levels) — those belong to FEAT-11/12/13
2. Service building construction UI (reuses existing building placement)
3. Service upgrade tiers (future work)
4. Dynamic coverage that changes mid-tick (recalculate once per simulation tick or on building placement)

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | As a player, I want to see which areas of my city lack fire coverage so I know where to place fire stations | Minimap shows red (uncovered) and green (covered) overlay when fire coverage layer is active |
| 2 | Player | As a player, I want citizens to be unhappier when they live far from services | Happiness calculation includes coverage factor; areas without services show lower happiness |
| 3 | Player | As a player, I want to see the coverage radius of a service building before placing it | Preview overlay shows coverage radius during building placement |
| 4 | Developer | As a developer adding a new service type, I want to register it with the coverage system using minimal code | `ServiceCoverageManager.RegisterServiceType("hospital", radius: 8, happinessBonus: 5.0f)` |
| 5 | AI agent | As an agent diagnosing happiness issues, I want to query coverage data | MCP tool or debug_context_bundle includes coverage scores for the seed cell neighborhood |

## 4. Current State

### 4.1 Domain behavior

**Desirability** exists as a per-cell float computed once during geography initialization based on terrain context (water proximity, forest proximity). There is no dynamic service-based desirability. **Happiness** is a single aggregate number in CityStats (currently hardcoded to 50% — BUG-12). There are no service buildings beyond power plants and water plants, which affect resource supply/consumption but not coverage or happiness.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/CityStats.cs` — happiness aggregate
- `Assets/Scripts/Managers/GameManagers/DemandManager.cs` — demand calculation with desirability factor
- `Assets/Scripts/Managers/UnitManagers/Cell.cs` — desirability field, closeWaterCount, closeForestCount
- `Assets/Scripts/Managers/UnitManagers/Zone.cs` — GetEnvironmentalImpact() (water: +3.0, forest: +2.0, grass: +0.5)
- `Assets/Scripts/Managers/GameManagers/GridManager.cs` — cell access and pathfinding dispatch
- `Assets/Scripts/Utilities/GridPathfinder.cs` — A* pathfinding (road network distance)
- `MiniMapController.cs` — minimap layer rendering

## 5. Proposed Design

### 5.1 Target behavior (product)

**Coverage computation:**

For each service building, compute road-network distance to all reachable cells using BFS/Dijkstra over the road graph. Cells within the service radius receive a coverage score that decays linearly with distance:

```
coverage = max(0, 1.0 - (road_distance / service_radius))
```

If a cell is covered by multiple buildings of the same type, take the maximum coverage score.

**Impact on happiness:**

```
service_happiness_bonus = sum(coverage_score * service_happiness_weight) for each service type
service_happiness_penalty = count(populated_cells_without_any_service_coverage) * penalty_per_cell
net_happiness_delta = bonus - penalty
```

**Impact on desirability:**

```
cell.desirability += service_coverage_bonus * coverage_score
```

Desirability recalculated when service buildings are placed/removed, not every tick.

**Minimap overlay:**

Each service type adds a toggleable minimap layer. Covered cells colored by coverage intensity (green gradient). Uncovered populated cells colored red. Unpopulated uncovered cells are neutral.

**Example gameplay:**

1. Player places a fire station at cell (10, 10)
2. Coverage map computes: cells within road-distance 6 get coverage 1.0→0.0
3. Minimap "Fire coverage" layer shows green around the station, red in populated zones outside range
4. Happiness recalculates: covered residential cells get +3 happiness; uncovered residential cells get -2 happiness
5. Player places another fire station at (20, 15); coverage maps merge (max per cell)

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: coverage recalculation frequency (on placement + per-tick vs only on placement/removal), road network BFS performance for large maps, service type registration pattern (ScriptableObject? enum? config?), integration with existing desirability and happiness systems.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Road-network distance over Euclidean | City-builders use road distance — a fire station can't serve an area with no road access, even if it's geographically close | Euclidean radius (simpler); Chebyshev distance |
| 2026-04-07 | Generic framework first, one concrete service type | Shipping the framework with one proof-of-concept validates the architecture; other services (FEAT-11/12/13) plug in easily | Ship all service types at once; ship framework only with no concrete type |

## 7. Implementation Plan

### Phase 1 — Coverage computation

- [ ] ServiceCoverageManager with BFS-based coverage map computation
- [ ] Service type registration (type name, radius, happiness weight, desirability weight)
- [ ] Integration with at least one service building type (fire station)

### Phase 2 — Gameplay integration

- [ ] Happiness impact from coverage scores
- [ ] Desirability impact from coverage proximity
- [ ] Demand modulation (less residential demand in uncovered areas)

### Phase 3 — Visualization

- [ ] Minimap coverage layer per service type
- [ ] Placement preview overlay showing coverage radius

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Coverage computes correctly for known grid | Play Mode / Unit test | Place fire station, verify coverage scores at known distances | Manual or automated |
| Happiness changes with coverage | Play Mode | Compare happiness before/after placing service building | BUG-12 must be fixed first |
| Game compiles | MCP / dev machine | `unity_compile` or `npm run unity:compile-check` | |
| No per-frame performance regression | Manual | Profile with/without coverage recalculation | |

## 8. Acceptance Criteria

- [ ] `ServiceCoverageManager` computes road-network-based coverage maps for registered service types
- [ ] At least one service type (fire station) shipped with functional coverage
- [ ] Coverage affects happiness: bonus in covered areas, penalty in uncovered populated areas
- [ ] Coverage affects desirability: per-cell bonus based on coverage score
- [ ] Minimap shows coverage heatmap layer
- [ ] Game fully functional without any service buildings placed (coverage = empty, no penalties)
- [ ] New service types can be registered with minimal code

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

1. Should **coverage** decay linearly with **road network** distance, or follow a different curve (e.g., quadratic, step function at threshold)?
2. What is the default coverage **radius** for the first service type (fire station)? Should it scale with **map** size?
3. Should uncovered **cells** suffer a **happiness** penalty even if they have no **population** (empty **zones**)?
4. How does **coverage** interact with **zone density** — do higher-density **zones** require more coverage (multiple overlapping service buildings)?
