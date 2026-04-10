---
description: Roads domain — guardrails and spec pointers
globs:
  - "**/RoadManager*.cs"
  - "**/RoadPrefabResolver*.cs"
  - "**/TerraformingService*.cs"
  - "**/PathTerraformPlan*.cs"
  - "**/GridPathfinder*.cs"
  - "**/RoadPathCostConstants*.cs"
  - "**/InterstateManager*.cs"
  - "**/RoadCacheService*.cs"
  - "**/AutoRoadBuilder*.cs"
alwaysApply: false
---

# Roads Domain

**Deep spec:** `.cursor/specs/roads-system.md`
**Geography spec:** `.cursor/specs/isometric-geography-system.md` §9, §10, §13

## Guardrails

- IF modifying roads → THEN call `InvalidateRoadCache()` afterward
- IF placing a road → THEN use the preparation family (`TryPrepareRoadPlacementPlan` / longest-prefix / locked deck-span), NOT `ComputePathPlan` alone
- IF placing bridge → THEN axis-aligned span only; no elbows on water/water-slope cells
- IF manual draw holds a locked water chord → THEN prefer `TryBuildDeckSpanOnlyWaterBridgePlan`
- IF cut-through → THEN reject when `maxHeight - baseHeight > 1`
- IF slope-climb → THEN use `TerraformAction.None` + `postTerraformSlopeType` — do not flatten terrain
