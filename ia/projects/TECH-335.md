---
purpose: "TECH-335 — Add LandmarkTier enum."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-335 — Add LandmarkTier enum

> **Issue:** [TECH-335](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Seed `LandmarkTier` enum under `Assets/Scripts/Data/Landmarks/`. Three values — `City`, `Region`, `Country` — mirror Bucket 1 scale tiers. XML doc per value cites scale coupling (region unlocked on city→region transition; country on region→country). No runtime refs; types-only Stage 1.1 Phase 1 of landmarks-master-plan.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Add `Assets/Scripts/Data/Landmarks/LandmarkTier.cs` — `enum LandmarkTier { City, Region, Country }`.
2. XML doc per value — document scale-transition coupling.
3. File compiles clean (`npm run unity:compile-check`).
4. `npm run validate:all` green.

### 2.2 Non-Goals

1. Runtime references (consumed from TECH-336 onward).
2. `LandmarkPopGate` / `LandmarkCatalogRow` — sibling tasks (TECH-336 / TECH-337).
3. Asmdef wiring — sibling task TECH-338.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 1.1 Phase 2 author | As TECH-336 / TECH-337 author, I want `LandmarkTier` resolvable so `ScaleTransitionGate.fromTier` + `LandmarkCatalogRow.tier` compile. | `using` path resolves; compile clean. |

## 4. Current State

### 4.1 Domain behavior

No landmark data types exist yet. Stage 1.1 seeds the type scaffolding that Steps 2–4 consume.

### 4.2 Systems map

- Target file: `Assets/Scripts/Data/Landmarks/LandmarkTier.cs` (new).
- Sibling files landed same stage: `LandmarkPopGate.cs` (TECH-336), `LandmarkCatalogRow.cs` (TECH-337), `Landmarks.asmdef` (TECH-338).
- Consumers (later stages): `LandmarkProgressionService` (Stage 2.1), `BigProjectService` (Stage 3.2), `LandmarkCatalogStore` (Stage 1.3).

## 5. Proposed Design

### 5.2 Architecture

Plain C# enum in new folder `Assets/Scripts/Data/Landmarks/`. Values ordered by tier progression — `City = 0, Region = 1, Country = 2` — so `>` comparisons work for `ScaleTransitionGate` eval in Stage 2.2.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Explicit int backing (0/1/2) for ordinal `>` comparison | Stage 2.2 gate eval does `scaleTier.CurrentTier > gate.fromTier` | String tags — rejected (no ordinal) |

## 7. Implementation Plan

### Phase 1 — Create enum file

- [ ] Create dir `Assets/Scripts/Data/Landmarks/` if missing.
- [ ] Add `LandmarkTier.cs` — enum w/ 3 values + XML doc per value.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Enum compiles | Unity | `npm run unity:compile-check` | No runtime refs yet |
| IA clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] `LandmarkTier.cs` present under `Assets/Scripts/Data/Landmarks/`.
- [ ] 3 values w/ XML doc each.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; type scaffold for Stage 1.1.
