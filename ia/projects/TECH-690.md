---
purpose: "TECH-690 — Zone S manual placement consults PlacementValidator before commit; GridManager public API only."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T3.1.3
---
# TECH-690 — Zoning channel match MVP

> **Issue:** [TECH-690](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

**Zone S** manual building placement calls **`PlacementValidator.CanPlace`** before committing spawn/transform. On failure, abort commit and preserve **`PlacementResult`** for future UX (**Stage 3.2**). **`PlacementValidator`** uses **`GridManager`** public API only — no **`grid.cellArray`**; avoid new **`GridManager`** methods unless unavoidable (**document in §Findings**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Manual placement path blocks zoning channel mismatch using validator.
2. Map failure to **`PlacementFailReason`** zoning (or dedicated enum member).
3. Manual Editor smoke: allowed vs disallowed asset.

### 2.2 Non-Goals (Out of Scope)

1. Ghost tint / tooltip (**Stage 3.2**).
2. Economy / unlock (**TECH-691**, **TECH-692**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Illegal zone channel cannot be committed | Validator gate on commit path |

## 4. Current State

### 4.1 Domain behavior

**Grid asset catalog** + **zone subtype** channels must align for MVP legality.

### 4.2 Systems map

- **`PlacementValidator.cs`**
- **`ZoneManager.cs`**, **`CursorManager.cs`** (integration per master plan **Relevant surfaces**)
- **`GridManager`** public surface

### 4.3 Implementation investigation notes (optional)

Trace current **Zone S** commit / click handler; single insertion point preferred.

## 5. Proposed Design

### 5.1 Target behavior (product)

Wrong channel → no building placed; reason available for UI later.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Inject or locate **`PlacementValidator`** same as other managers; no per-frame **`FindObjectOfType`** in hot paths.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
|  |  |  |  |

## 7. Implementation Plan

### Phase 1 — Integration

- [ ] Locate **Zone S** commit point; call **`CanPlace`**; abort on false.
- [ ] Set zoning fail reason in **`PlacementResult`**.
- [ ] Document manual smoke steps in **§8** or **§7b**.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile + manual | Unity | `npm run unity:compile-check` + manual checklist | Path B optional |

## 8. Acceptance Criteria

- [ ] Manual placement path blocks illegal zoning channel mismatches using validator output.
- [ ] No direct **`grid.cellArray`** from validator; **`GridManager`** extraction only.
- [ ] Any new **`GridManager`** API documented in **§Findings**.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Zone S commit path (`PlaceStateServiceZoneAt` or successor) consults `PlacementValidator` before mutating `CityCell`; blocks illegal zoning channel match using `PlacementResult` from **TECH-689**; no `grid.cellArray` access inside validator.

### §Acceptance

- [ ] `ZoneManager` (or documented alternate Zone S commit site) calls validator before state mutation
- [ ] Failure maps to zoning-related `PlacementFailReason`
- [ ] `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| manual_zone_s | disallowed channel | commit blocked | Editor checklist in §8 |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| Site | Hook |
|------|------|
| `PlaceStateServiceZoneAt` | Guard immediately after `GetCell` null check |

### §Mechanical Steps

#### Step 1 — Inject validator dependency

**Goal:** `ZoneManager` holds optional `PlacementValidator` reference resolved once.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/ZoneManager.cs` — **before:**
```
    public InterstateManager interstateManager;
    public SlopePrefabRegistry slopePrefabRegistry;
    #endregion
```
**after:**
```
    public InterstateManager interstateManager;
    public SlopePrefabRegistry slopePrefabRegistry;
    [SerializeField] private PlacementValidator placementValidator;
    #endregion
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** On compile error, add `using` only if needed (types live in `Territory.Core`) then re-run gate.

#### Step 2 — Guard commit path

**Goal:** Abort Zone S placement when validator denies.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/ZoneManager.cs` — **before:**
```
        if (cell == null) return false;

        cell.zoneType = zoneType;
```
**after:**
```
        if (cell == null) return false;

        if (placementValidator != null)
        {
            // TECH-690: call CanPlace with catalog-backed assetId + grid args; return false when PlacementResult denies (see §7)
        }

        cell.zoneType = zoneType;
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** Replace placeholder comment arguments with real `assetId` resolution from `subTypeId` + `GridAssetCatalog` per §7 before merge.

## Open Questions (resolve before / during implementation)

1. Which **`Zone S`** codepath is authoritative for “commit” — confirm with **`ZoneManager`** / **`CursorManager`** ownership.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
