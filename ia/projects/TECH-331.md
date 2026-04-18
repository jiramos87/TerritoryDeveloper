---
purpose: "TECH-331 — Utility enums (UtilityKind / ScaleTag / PoolStatus)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-331 — Utility enums (UtilityKind / ScaleTag / PoolStatus)

> **Issue:** [TECH-331](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Typed scaffolding for utilities v1 (Bucket 4a). Three plain enums — `UtilityKind` (Water / Power / Sewage), `ScaleTag` (City / Region / Country), `PoolStatus` (Healthy / Warning / Deficit) — under new `Assets/Scripts/Data/Utilities/`. No behavior, no runtime refs. Consumed by Stage 1.2+ services + Stage 1.1 Phase 2 interfaces.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Create `Assets/Scripts/Data/Utilities/UtilityKind.cs` — enum `Water`, `Power`, `Sewage`.
2. Create `Assets/Scripts/Data/Utilities/ScaleTag.cs` — enum `City`, `Region`, `Country`.
3. Create `Assets/Scripts/Data/Utilities/PoolStatus.cs` — enum `Healthy`, `Warning`, `Deficit`.
4. XML doc on every value citing canonical meaning (e.g. `Water` → "potable supply pool").
5. `npm run unity:compile-check` green.

### 2.2 Non-Goals

1. Runtime references (Stage 1.2+).
2. Serialization wiring (Stage 4.1).
3. Glossary rows (Step 1 exit criteria — separate task).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 1.2 implementer | As author of `UtilityPoolService`, I want typed `UtilityKind` keys so my dictionary signatures compile. | Enum exists, compiles, documented. |

## 4. Current State

### 4.1 Domain behavior

No utilities types exist yet. Bucket 4a starts here.

### 4.2 Systems map

- New folder `Assets/Scripts/Data/Utilities/`.
- Consumers (future stages): `UtilityPoolService`, `UtilityContributorRegistry`, `DeficitResponseService`, infrastructure contributor components.
- Router domain: Forests, regional map, utilities → `ia/specs/managers-reference.md` World features.

## 5. Proposed Design

### 5.1 Target behavior

Plain C# enums, default int backing. XML doc per value.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | One file per enum | Matches repo convention for data enums | Single `UtilityTypes.cs` — rejected, harder to grep |

## 7. Implementation Plan

### Phase 1 — Enum scaffolding

- [ ] Create folder `Assets/Scripts/Data/Utilities/`.
- [ ] `UtilityKind.cs` — `Water`, `Power`, `Sewage` w/ XML doc.
- [ ] `ScaleTag.cs` — `City`, `Region`, `Country` w/ XML doc.
- [ ] `PoolStatus.cs` — `Healthy`, `Warning`, `Deficit` w/ XML doc.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Enums compile | Unity | `npm run unity:compile-check` | No runtime refs expected yet |
| IA indexes clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] Three enum files exist under `Assets/Scripts/Data/Utilities/`.
- [ ] Every value carries XML doc.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling / data-model scaffolding only; no gameplay rules.
