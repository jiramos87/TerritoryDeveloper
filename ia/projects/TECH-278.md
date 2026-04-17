---
purpose: "TECH-278 — Extend ZoneType enum + predicates for Zone S sub-types."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-278 — Extend `ZoneType` enum + predicates for Zone S

> **Issue:** [TECH-278](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add 6 new `ZoneType` enum values for **Zone S** (3 density tiers × Building/Zoning) in `Zone.cs`. Extend `EconomyManager.IsBuildingZone` + `IsZoningType`; add new `IsStateServiceZone` predicate. Scaffolding only — no caller consumes new values yet. Foundation task for zone-s-economy Stage 1.1 Phase 1.

## 2. Goals and Non-Goals

### 2.1 Goals

1. 6 new enum values land in `Zone.ZoneType`: `StateServiceLightBuilding`, `StateServiceMediumBuilding`, `StateServiceHeavyBuilding`, `StateServiceLightZoning`, `StateServiceMediumZoning`, `StateServiceHeavyZoning`.
2. `EconomyManager.IsBuildingZone` returns true for 3 new Building values; `IsZoningType` returns true for 3 new Zoning values.
3. New predicate `EconomyManager.IsStateServiceZone(Zone.ZoneType) → bool` — true for all 6 S values, false for R/C/I + legacy.
4. `unity:compile-check` green. Existing RCI callers unchanged.

### 2.2 Non-Goals

1. No runtime consumer wiring — that lands in Stage 2.3 (`ZoneSService`).
2. No `subTypeId` sidecar on `Zone` — lands in TECH-279.
3. No `ZoneSubTypeRegistry` — lands in TECH-280.
4. No glossary row authoring — lands in TECH-282.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Enumerate S zone types in downstream services | 6 new `ZoneType` values compile + discoverable via reflection |
| 2 | Developer | Branch on S vs RCI via predicate | `IsStateServiceZone` returns correct bool for all 10 enum values |

## 4. Current State

### 4.1 Domain behavior

`Zone.ZoneType` currently enumerates RCI only. `EconomyManager.IsBuildingZone` + `IsZoningType` gate RCI variants. No Zone S branch exists.

### 4.2 Systems map

- `Assets/Scripts/Managers/UnitManagers/Zone.cs` — `ZoneType` enum declaration site.
- `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` — `IsBuildingZone` + `IsZoningType` predicate bodies; new `IsStateServiceZone` lands here.
- Router domain: Zones, buildings, RCI.
- Relevant invariant: #4 (no singletons — predicates stay static on `EconomyManager`).

## 5. Proposed Design

### 5.1 Target behavior

6 new enum values follow existing density-tier naming convention (Light/Medium/Heavy × Building/Zoning). Predicates branch S via single-list check.

### 5.2 Architecture / implementation

Append 6 values to end of `ZoneType` enum (stable ordering — append only, never insert, to preserve save-file int compatibility). Extend two existing predicates with `||` chains or switch-expression. New predicate follows same pattern.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Append 6 values end-of-enum, not grouped | Preserves existing enum int values = legacy save compat | Reorder enum (breaks v3 saves) |

## 7. Implementation Plan

### Phase 1 — Enum + predicates

- [ ] Add 6 new `ZoneType` values at end of enum in `Zone.cs`.
- [ ] Extend `EconomyManager.IsBuildingZone` to include 3 new Building values.
- [ ] Extend `EconomyManager.IsZoningType` to include 3 new Zoning values.
- [ ] Add `IsStateServiceZone(Zone.ZoneType) → bool` static method on `EconomyManager`.
- [ ] Run `npm run unity:compile-check`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| 6 new values compile + predicates correct | Unity compile | `npm run unity:compile-check` | Bridge preflight not required |
| Enum + predicate coverage | EditMode test | `npm run unity:testmode-batch` (covered by TECH-283) | Test harness lands in sibling task |

## 8. Acceptance Criteria

- [ ] 6 new `ZoneType` values declared at end of enum.
- [ ] `IsBuildingZone` + `IsZoningType` extended to cover new values.
- [ ] `IsStateServiceZone` predicate lands on `EconomyManager`.
- [ ] `unity:compile-check` green.
- [ ] No caller behavior change for RCI.

## Open Questions

1. None — scaffolding task, no gameplay change.
