# FEAT-32 — More Streets and Intersections in Central/Mid-Urban Areas (AUTO mode)

## Implementation Summary

This document summarizes the implementation of FEAT-32 as completed.

### Phase 1: UrbanCentroidService

- **Created** `Assets/Scripts/Managers/GameManagers/UrbanCentroidService.cs` — MonoBehaviour that owns UrbanMetrics, exposes GetCentroid(), GetUrbanRing(), GetStreetParamsForRing(), GetBaseZoneProbabilities(), GetZoningDensityForRing(), GetUrbanRadius(), GetUrbanMetrics()
- **Updated** `SimulationManager.cs` — Added urbanCentroidService reference, calls RecalculateFromGrid() at start of ProcessSimulationTick(), wires service to AutoZoningManager, AutoRoadBuilder, UrbanizationProposalManager in Start()
- **Refactored** `AutoZoningManager.cs` — Uses urbanCentroidService instead of owning UrbanMetrics; GetUrbanMetrics() delegates to service for backward compatibility
- **Refactored** `AutoRoadBuilder.cs` — Uses urbanCentroidService directly; added urbanCentroidService dependency
- **Refactored** `UrbanizationProposalManager.cs` — Uses urbanCentroidService.GetCentroid(); FindRemoteAnchor uses GetUrbanRing (Outer/Rural only) when service available

### Phase 2: Strengthen RingStreetParams

- **Updated** `UrbanMetrics.cs` InitializeStreetParams() — 4 areas (Inner, Mid, Outer, Rural):
  - Inner: minLength 2, maxLength 8, parallelSpacing 1
  - Mid: minLength 4, maxLength 12, parallelSpacing 3
  - Outer: minLength 7, maxLength 25, parallelSpacing 6
  - Rural: minLength 10, maxLength 35, parallelSpacing 6

### Phase 3: Ring-Dependent Budget and Spacing

- **Updated** `AutoRoadBuilder.cs`:
  - Added `coreInnerExtraProjects`, `coreInnerMinEdgeSpacing` (Inspector-tunable)
  - `GetEffectiveMinEdgeSpacing()` — Inner uses reduced minEdgeSpacing for more intersections
  - `CountInnerEdges()` — Used for effectiveMaxProjects
  - effectiveMaxProjects = maxActiveProjects + (innerEdgeCount >= 2 ? coreInnerExtraProjects : 0)

### Phase 4: MiniMapController and Scene

- **Updated** `MiniMapController.cs` — Added urbanCentroidService; centroid layer uses service when available
- **Scene** — Added UrbanCentroidService component to SimulationManager GameObject; wired urbanCentroidService to SimulationManager, MiniMapController

## Scene Setup

1. UrbanCentroidService is on the SimulationManager GameObject.
2. SimulationManager.urbanCentroidService references it.
3. SimulationManager.Start() wires urbanCentroidService to AutoZoningManager, AutoRoadBuilder, UrbanizationProposalManager when their references are null.
4. MiniMapController.urbanCentroidService is wired in the scene.

## 3 Rings / 4 Areas Model (Later Refactor)

Urban ring classification uses 3 concentric boundaries defining 4 areas:
- **Inner**: centroid to Ring 1 (70% of radius) — urban center, dense, no industrial
- **Mid**: Ring 1 to Ring 2 (100% of radius) — residential
- **Outer**: Ring 2 to Ring 3 (180% of radius) — transition, industrial-dominant
- **Rural**: beyond Ring 3 — sparse

Edge was removed; its logic merged into Outer. MiniMap centroid layer draws 3 rings via GetRingBoundaryDistances().

## Testing Checklist

- [ ] New game: roads build; central areas have visibly shorter streets and more intersections
- [ ] Rural zones: longer, straighter roads; fewer branches
- [ ] UrbanizationProposalManager: proposals use centroid; no regression (when BUG-15 is fixed)
- [ ] AutoZoningManager: zoning uses ring probabilities; no regression
- [ ] MiniMapController: centroid layer displays 3 rings correctly
