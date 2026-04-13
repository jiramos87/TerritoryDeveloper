---
purpose: "TECH-92 — RegionCell placeholder type (coord + parent-region-id; no behavior)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-92 — `RegionCell` placeholder type + glossary row

> **Issue:** [TECH-92](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.2 Phase 2 — add `RegionCell` as thin placeholder type inheriting the abstract cell base (TECH-90/91). Carries coord + parent-region-id reference; no behavior. Adds glossary row for **region cell**. City sim unaffected.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `RegionCell` class exists; inherits abstract base; carries `int x`, `int y`, `string parentRegionId` (or GUID); no behavior.
2. Glossary row added for **region cell** in `ia/specs/glossary.md`.
3. Project compiles clean; city sim untouched.

### 2.2 Non-Goals (Out of Scope)

1. `CountryCell` — that is TECH-93.
2. `GetCell<T>` typed surface — that is TECH-94.
3. Any region-scale simulation behavior.
4. Save/load for region cells (no save integration in this stage).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Region-scale cell type exists as named stub for future region sim code to target | RegionCell compiles; glossary row present |

## 4. Current State

### 4.1 Domain behavior

No region-scale cell type exists. City sim works exclusively with `CityCell` (post-TECH-91). `RegionCell` is inert infrastructure for Step 2 region sim.

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `Assets/Scripts/Managers/UnitManagers/CityCell.cs` | Abstract base lives here (post-TECH-90/91) |
| `Assets/Scripts/Managers/UnitManagers/RegionCell.cs` | New file to create |
| `ia/specs/glossary.md` | Glossary row target |
| `ia/projects/multi-scale-master-plan.md` | Stage 1.2 orchestrator |

### 4.3 Implementation investigation notes (optional)

`RegionCell` does NOT need `MonoBehaviour` — no scene GameObject at region scale in MVP. Plain C# class inheriting abstract base sufficient.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change. Region scale remains dormant.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

```csharp
// Assets/Scripts/Managers/UnitManagers/RegionCell.cs
public class RegionCell : CellBase   // or whatever abstract base name TECH-90 chose
{
    public string ParentRegionId { get; }
    public RegionCell(int x, int y, string parentRegionId) { ... }
}
```

No `MonoBehaviour`. No serialization. No GridManager integration yet.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-13 | Plain C# class, not MonoBehaviour | No scene object at region scale in MVP; avoids coupling to Unity lifecycle | MonoBehaviour (would add GameObject overhead with no payoff) |

## 7. Implementation Plan

### Phase 1 — Add RegionCell type + glossary

- [ ] Create `Assets/Scripts/Managers/UnitManagers/RegionCell.cs` — plain C# class inheriting abstract base; fields: coord + parentRegionId.
- [ ] Add glossary row for **region cell** to `ia/specs/glossary.md` (term, definition, spec reference = multi-scale-master-plan).
- [ ] Run `npm run unity:compile-check` — must pass clean.
- [ ] Run `npm run validate:all` to confirm IA indexes pick up glossary addition.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile-clean | Unity compile | `npm run unity:compile-check` | Mandatory |
| Glossary row indexed | Node | `npm run validate:all` | Catches glossary parse issues |

## 8. Acceptance Criteria

- [ ] `RegionCell` class compiles; inherits abstract base; carries coord + parentRegionId.
- [ ] No behavior added; city sim unaffected.
- [ ] Glossary row for **region cell** present in `ia/specs/glossary.md`.
- [ ] `npm run unity:compile-check` passes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Name of abstract base after TECH-90 resolves — `CellBase`? Implementer coordinates with TECH-90/91 outputs.
