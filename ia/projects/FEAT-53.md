# FEAT-53 — District / neighborhood system

> **Issue:** [FEAT-53](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Allow players to define named districts (contiguous cell regions) that each track their own statistics (population, happiness, demand, density, tax policy). Districts enable "downtown vs suburbs" strategic gameplay, per-district tax rates and service priorities, and visual distinction on the minimap. Complements multipolar urban growth (FEAT-47) by giving each urban pole a meaningful identity.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Player-defined districts: contiguous cell regions with a name and optional color
2. Per-district statistics: population, happiness, zone distribution, density, employment, tax revenue
3. Optional per-district tax policy overrides (default: city-wide tax rates)
4. Minimap district overlay with distinct coloring per district
5. District dashboard showing comparison metrics across districts
6. Districts persisted in save data
7. AUTO simulation awareness: respect district boundaries for growth ring behavior when districts overlap with urban poles (FEAT-47 coordination)

### 2.2 Non-Goals (Out of Scope)

1. Automatic district generation (player draws them manually)
2. District-level ordinances or policies beyond tax rates (future work)
3. District-level service coverage management (FEAT-52 operates city-wide)
4. NPC district representatives or political simulation

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | As a player, I want to draw a district boundary around my commercial downtown and name it "Centro" | District creation tool: click cells to define boundary, name it, assign color |
| 2 | Player | As a player, I want to see that "Centro" has higher tax revenue per cell than "Suburbia" | District dashboard shows per-district metrics: population density, tax revenue, happiness |
| 3 | Player | As a player, I want to set higher commercial tax rates in "Centro" without affecting "Suburbia" | Per-district tax override in district settings panel |
| 4 | Player | As a player, I want to see my districts on the minimap as colored regions | Minimap "Districts" layer shows each district with its assigned color |
| 5 | Developer | As a developer implementing FEAT-47 (multipolar growth), I want urban poles to map naturally to districts | DistrictManager exposes district boundaries for UrbanCentroidService ring calculations |
| 6 | AI agent | As an agent analyzing a gameplay issue, I want to query district-level stats | MCP tool or debug_context_bundle includes district membership and stats for seed cell |

## 4. Current State

### 4.1 Domain behavior

There is no district or neighborhood concept. All statistics are city-wide aggregates in CityStats. Tax rates are global (one rate per zone type in EconomyManager). The minimap shows zones, roads, water, and forests but no player-defined regions. Urban growth rings (UrbanCentroidService) operate from a single centroid with no sub-city partitioning.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/CityStats.cs` — city-wide aggregate stats
- `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` — global tax rates
- `Assets/Scripts/Managers/UnitManagers/Cell.cs` — no district field currently
- `MiniMapController.cs` — minimap layers
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — save/load pipeline (CellData, CityStatsData)
- `UrbanCentroidService.cs` — single centroid + growth rings

## 5. Proposed Design

### 5.1 Target behavior (product)

**District creation:**

Player activates "District tool" from toolbar. Click cells to add them to the current district boundary. Cells must form a contiguous region. Name and color picker in a popup. Cells can belong to at most one district; unassigned cells belong to "undistricted" (city-wide defaults apply).

**Per-district statistics (computed each simulation tick):**

| Metric | Computation |
|--------|-------------|
| Population | Sum of `Cell.population` for cells in district |
| Zone distribution | Count of R/C/I cells by density tier |
| Happiness | City-wide happiness score (0–100, shipped); per-district breakdown deferred to this issue |
| Tax revenue | Per-district sum based on district tax rates × building counts |
| Density | Total buildings / total cells in district |
| Employment | District-scoped employment ratio |

**Per-district tax override:**

```
EconomyManager.GetEffectiveTaxRate(cell)
  → if cell.districtId != 0 && district.hasTaxOverride
      → return district.taxRates[cell.zoneType]
    else
      → return global taxRates[cell.zoneType]
```

**Example gameplay:**

1. Player draws "Industrial Park" district around factory zone at the south of the city
2. Sets industrial tax rate to 5% (vs city-wide 10%) to attract more industry
3. Dashboard shows: Industrial Park has 30% higher industrial demand than city average
4. Player draws "Lakeside" district around residential area near water
5. Dashboard shows: Lakeside has 85% happiness (water desirability) vs 65% city average
6. Minimap shows blue (Lakeside), orange (Industrial Park), gray (undistricted)

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: cell→district membership storage (new field on Cell/CellData? separate data structure?), district stats computation frequency (per-tick vs on-demand), save/load format, contiguity validation during drawing, UI for district management.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Player-drawn districts over auto-generated | Player agency is the goal; auto-generation is a separate feature (could derive from FEAT-47 urban poles) | Voronoi from urban centroids; road-network partitioning |
| 2026-04-07 | Optional tax override per district (not mandatory) | Keeps it simple for players who don't want micromanagement; city-wide defaults always work | Mandatory per-district tax; no per-district tax (stats-only) |

## 7. Implementation Plan

### Phase 1 — Data model and drawing

- [ ] District data structure: id, name, color, cell membership set, optional tax overrides
- [ ] Cell→district mapping (new field or lookup)
- [ ] District drawing tool in UI
- [ ] Save/load integration

### Phase 2 — Statistics and economy

- [ ] Per-district stat computation (per tick or on demand)
- [ ] Per-district tax override integration in EconomyManager
- [ ] District dashboard UI

### Phase 3 — Visualization

- [ ] Minimap district layer
- [ ] District boundary overlay on game view

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Districts persist across save/load | Play Mode | Create district, save, load, verify district intact | Manual |
| Per-district tax rates affect economy | Play Mode | Set different rates per district, verify revenue changes | Manual |
| Game compiles | MCP / dev machine | `unity_compile` or `npm run unity:compile-check` | |
| Old saves without districts load correctly | Play Mode | Load pre-district save file, verify no errors | Backward compatibility |

## 8. Acceptance Criteria

- [ ] Player can draw contiguous cell districts with name and color
- [ ] Per-district statistics computed and displayed in dashboard (population, zone distribution, happiness, tax revenue, density)
- [ ] Optional per-district tax rate overrides functional
- [ ] Minimap district layer shows colored regions
- [ ] Districts persisted in save data; backward-compatible with old saves
- [ ] Cells belong to at most one district; undistricted cells use city-wide defaults

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

1. Should **districts** have a maximum or minimum size in **cells**?
2. Can a **district** be split (non-contiguous) after a **bulldozer** removes connecting **cells**, or must it stay contiguous?
3. Should **AUTO** **simulation** respect **district** boundaries (e.g., prefer zoning within a district vs expanding outside)?
4. How should **districts** interact with **urban growth rings** — does each **district** get its own **centroid** for ring calculations?
