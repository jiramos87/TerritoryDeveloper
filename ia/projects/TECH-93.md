---
purpose: "TECH-93 — CountryCell placeholder type + glossary rows for all 3 cell types."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-93 — `CountryCell` placeholder type + complete cell-type glossary

> **Issue:** [TECH-93](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.2 Phase 2 — add `CountryCell` as thin placeholder type; carries coord + parent-country-id; no behavior. Completes glossary rows for all three cell types (city cell, region cell, country cell). City sim unaffected.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `CountryCell` class exists; inherits abstract base; carries `int x`, `int y`, `string parentCountryId`; no behavior.
2. Glossary row added for **country cell**; glossary rows for **city cell** and **region cell** confirmed complete (may be co-authored with TECH-92 or completed here).
3. Project compiles clean; city sim untouched.

### 2.2 Non-Goals (Out of Scope)

1. Any country-scale simulation behavior.
2. `GetCell<T>` typed surface — that is TECH-94.
3. Save/load for country cells.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | All three scale-specific cell types exist as named stubs; glossary complete | CountryCell compiles; all 3 glossary rows present |

## 4. Current State

### 4.1 Domain behavior

`CityCell` exists (TECH-91); `RegionCell` exists (TECH-92). `CountryCell` missing. Glossary entry "City cell / Region cell / Country cell" exists as combined row — individual rows may need splitting/expansion per TECH-92/93.

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `Assets/Scripts/Managers/UnitManagers/CityCell.cs` | Reference — abstract base pattern |
| `Assets/Scripts/Managers/UnitManagers/RegionCell.cs` | Reference — TECH-92 output |
| `Assets/Scripts/Managers/UnitManagers/CountryCell.cs` | New file to create |
| `ia/specs/glossary.md` | Glossary target — confirm all 3 rows |

### 4.3 Implementation investigation notes (optional)

Identical pattern to RegionCell — plain C# class, no MonoBehaviour. Coordinate glossary completion with TECH-92 to avoid duplicate row edits.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change. Country scale remains dormant.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

```csharp
// Assets/Scripts/Managers/UnitManagers/CountryCell.cs
public class CountryCell : CellBase
{
    public string ParentCountryId { get; }
    public CountryCell(int x, int y, string parentCountryId) { ... }
}
```

Glossary: confirm or add individual rows for city cell (→ `CityCell`), region cell (→ `RegionCell`), country cell (→ `CountryCell`). Update combined entry or split per glossary authoring conventions.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Plain C# class, not MonoBehaviour | No scene object at country scale in MVP | MonoBehaviour (no payoff) |

## 7. Implementation Plan

### Phase 1 — Add CountryCell type + complete glossary

- [ ] Create `Assets/Scripts/Managers/UnitManagers/CountryCell.cs` — plain C# class inheriting abstract base; fields: coord + parentCountryId.
- [ ] Confirm/add glossary rows for **city cell**, **region cell**, **country cell** in `ia/specs/glossary.md`.
- [ ] Run `npm run unity:compile-check` — must pass clean.
- [ ] Run `npm run validate:all` to confirm IA indexes.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile-clean | Unity compile | `npm run unity:compile-check` | Mandatory |
| All 3 glossary rows indexed | Node | `npm run validate:all` | Catches glossary parse issues |

## 8. Acceptance Criteria

- [ ] `CountryCell` class compiles; inherits abstract base; carries coord + parentCountryId.
- [ ] Glossary rows present for all 3 cell types (city cell, region cell, country cell).
- [ ] No behavior added; city sim unaffected.
- [ ] `npm run unity:compile-check` passes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Does glossary need separate rows per type or is a combined row OK? Coordinate with TECH-92 to avoid duplicate edits.
