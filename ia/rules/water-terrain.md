---
description: Water and terrain domain — guardrails and spec pointers
globs:
  - "**/WaterManager*.cs"
  - "**/WaterMap*.cs"
  - "**/TerrainManager*.cs"
  - "**/HeightMap*.cs"
  - "**/ProceduralRiver*.cs"
  - "**/TestRiverGenerator*.cs"
  - "**/WaterBody*.cs"
  - "**/LakeFeasibility*.cs"
  - "**/CliffFace.cs"
  - "**/GeographyManager*.cs"
alwaysApply: false
---

# Water & Terrain Domain

**Deep spec:** `.cursor/specs/water-terrain-system.md`
**Geography spec:** `.cursor/specs/isometric-geography-system.md` §2–§5, §11–§12

## Guardrails

- IF modifying `HeightMap` → THEN also write `Cell.height` (and vice versa)
- IF placing or removing water → THEN call `RefreshShoreTerrainAfterWaterUpdate`
- IF water–water cascade → THEN check `IsLakeSurfaceStepContactForbidden` before placing stacks
- IF multi-body contact → THEN follow Pass A → Pass B → lake-river fallback → place water → cascade cliffs → shore terrain (§11.7)
- IF shore prefab selection → THEN verify surface-height gate: `h ≤ V + MAX` where `V = max(MIN_HEIGHT, S−1)`
- IF procedural river → THEN enforce symmetric banks `H_bank = H_bed + 1`, corner promotion for shore continuity
