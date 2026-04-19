---
purpose: "TECH-337 — Add LandmarkCatalogRow class."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-337 — Add LandmarkCatalogRow class

> **Issue:** [TECH-337](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add `LandmarkCatalogRow` — serializable class carrying the 9 per-landmark fields authored in `StreamingAssets/landmark-catalog.yaml`. Consumes `LandmarkTier` (TECH-335) + `LandmarkPopGate` (TECH-336). Consumed by `LandmarkCatalogStore` (Stage 1.3) + every service thereafter. Stage 1.1 Phase 2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Add `Assets/Scripts/Data/Landmarks/LandmarkCatalogRow.cs` — `[Serializable]` class w/ 9 fields:
   - `string id`
   - `string displayName`
   - `LandmarkTier tier`
   - `LandmarkPopGate popGate`
   - `string spritePath`
   - `int commissionCost` (placeholder — `// cost-catalog bucket 11` marker)
   - `int buildMonths`
   - `string utilityContributorRef` (nullable)
   - `float contributorScalingFactor` (default 1.0)
2. XML doc per field citing consumer + nullability.
3. Compile clean; `npm run validate:all` green.

### 2.2 Non-Goals

1. YAML parser wiring — Stage 1.2 / 1.3.
2. Validation rules (uniqueness, ref resolution) — Stage 1.2 validator script.
3. Runtime consumer — Stage 1.3 store.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 1.3 Store author | As `LandmarkCatalogStore` author, I want a single row type holding all 9 fields so I can `Dictionary<string, LandmarkCatalogRow> byId`. | Type compiles; fields reachable via property access. |

## 4. Current State

### 4.1 Domain behavior

TECH-335 + TECH-336 land the enum + gate discriminator. Row class consolidates them plus authoring-time metadata (cost, months, super-utility ref).

### 4.2 Systems map

- Target file: `Assets/Scripts/Data/Landmarks/LandmarkCatalogRow.cs` (new).
- Consumes: `LandmarkTier` (TECH-335), `LandmarkPopGate` (TECH-336).
- Consumed by: `LandmarkCatalogStore` (Stage 1.3), `BigProjectService` (Stage 3.2 — reads `commissionCost` + `buildMonths`), `LandmarkPlacementService` (Stage 4.1 — reads `utilityContributorRef` + `contributorScalingFactor`).
- Super-utility bridge fields (`utilityContributorRef`, `contributorScalingFactor`) consumed by sibling Bucket 4-a `UtilityContributorRegistry.Register` at Stage 4.1.

## 5. Proposed Design

### 5.2 Architecture

Plain `[Serializable]` class. Nullable semantics on `utilityContributorRef` = empty string or explicit null per YAML parser convention (Stage 1.2 validator enforces). `contributorScalingFactor` default 1.0 via field initializer.

Placeholder cost convention — XML doc on `commissionCost` explicitly calls out `cost-catalog bucket 11` migration + inline `//` marker. Every other touch site in the codebase carries the same marker per orchestrator guardrail.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | `[Serializable]` class (not `struct`) | Holds reference type `LandmarkPopGate`; polymorphism required | Struct — rejected (no polymorphic gate) |
| 2026-04-17 | Nullable `utilityContributorRef` as `string` | Non-super-utility rows = null/empty | Separate `bool hasContributor` — rejected (redundant) |

## 7. Implementation Plan

### Phase 1 — Create row class

- [ ] Add `LandmarkCatalogRow.cs` under `Assets/Scripts/Data/Landmarks/`.
- [ ] 9 fields + XML doc each.
- [ ] `// cost-catalog bucket 11 placeholder` marker on `commissionCost`.
- [ ] Default value `contributorScalingFactor = 1.0f`.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Class compiles | Unity | `npm run unity:compile-check` | Consumer checks in Stage 1.3 |
| IA clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] `LandmarkCatalogRow.cs` present w/ 9 fields.
- [ ] XML doc per field.
- [ ] Cost placeholder marker present.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; YAML serialization attributes finalized in Stage 1.2 when parser lib picked.
