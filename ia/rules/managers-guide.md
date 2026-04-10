---
purpose: "GameManagers guide — guardrails and spec pointers"
audience: agent
loaded_by: router
slices_via: none
description: GameManagers guide — guardrails and spec pointers
globs: "**/Managers/GameManagers/*.cs"
alwaysApply: false
---

# GameManagers Guide

**Deep spec:** `ia/specs/managers-reference.md`

## Guardrails

- Every manager is a MonoBehaviour on a scene GameObject — NEVER `new`
- GridManager is the central hub — all cell operations go through it; do not add responsibilities
- For notifications use `GameNotificationManager.Instance` (only singleton)
- CityStats is the global data aggregator — read city-wide stats from here
- Road terraform validation: use `TryPrepareRoadPlacementPlan` family, not `ComputePathPlan` alone
- Style reference: `EconomyManager.cs` (XML docs + `#region` + `[Header]`)
