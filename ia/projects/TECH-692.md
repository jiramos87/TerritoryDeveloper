---
purpose: "TECH-692 — Unlock gate stub using catalog unlocks_after; integrate tech stub or document Allowed fallback."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T3.1.5
---
# TECH-692 — Unlock gate stub

> **Issue:** [TECH-692](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

**`PlacementValidator`** reads **`unlocks_after`** from catalog row for **`assetId`**. If tech unlock system exists, map locked assets to **`PlacementFailReason.Locked`**. If not implemented, return **Allowed** (or equivalent) and **document** fallback in **Decision Log** / **Open Questions** per master-plan intent.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **`unlocks_after`** consulted in **`CanPlace`** path.
2. Documented behavior when tech tree missing (explicit default).
3. Test or **§8** manual checklist for locked vs unlocked.

### 2.2 Non-Goals (Out of Scope)

1. Full tech tree UX.
2. Save/load unlock persistence changes unless already present.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Locked catalog assets rejected when unlock system wired | Documented when stub |

## 4. Current State

### 4.1 Domain behavior

Catalog carries **`unlocks_after`** string; tech may be stub.

### 4.2 Systems map

- **`PlacementValidator.cs`**
- **`GridAssetCatalog`** / asset DTO
- Existing tech unlock stub (if any)

### 4.3 Implementation investigation notes (optional)

Search codebase for **`unlocks_after`** consumers.

## 5. Proposed Design

### 5.1 Target behavior (product)

When unlock system exists: respect it. When absent: do not block MVP placement; document.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Prefer single helper **`IsUnlocked(assetId)`** inside validator or small dependency.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Default Allowed if no unlock service | MVP path per master-plan stub language | Hard-fail all locked — rejected |

## 7. Implementation Plan

### Phase 1 — Unlock stub

<!-- TECH-692: consult catalog row unlocks_after inside PlacementValidator.CanPlace per §7 -->

- [ ] Read **`unlocks_after`**; branch **`Locked`** vs allowed per integration.
- [ ] Record integration gap in **Decision Log** if defaulting to allowed.
- [ ] Add minimal test or manual **§8** step.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity | `npm run unity:compile-check` |  |
| Logic / manual | Unity Test or checklist | per §8 | When stub, manual OK |

## 8. Acceptance Criteria

- [ ] Catalog field **`unlocks_after`** consulted in **`CanPlace`** path.
- [ ] Documented fallback when tech tree not implemented.
- [ ] Test or explicit manual checklist for locked vs unlocked asset.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

`CanPlace` reads `unlocks_after` from catalog asset row; when tech unlock subsystem absent, document default-allow in Decision Log; when present, emit `PlacementFailReason.Locked`.

### §Acceptance

- [ ] Catalog row field `unlocks_after` consulted (`GridAssetCatalog.Dto.cs`)
- [ ] Decision Log records default-allow vs integrated behavior
- [ ] `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| unlock_stub | row with non-empty unlocks_after | allowed or locked per integration | EditMode or manual §8 |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| unlocks_after | Tech system | Result |
|---------------|-------------|--------|
| empty | n/a | Allowed |
| "tech_x" | not wired | Allowed + Decision Log note |

### §Mechanical Steps

#### Step 1 — Anchor implementation plan

**Goal:** Tie unlock work to Phase 1 heading for searchability.

**Edits:**

- `ia/projects/TECH-692.md` — **before:** `### Phase 1 — Unlock stub` — **after:** `### Phase 1 — Unlock stub\n\n<!-- TECH-692: consult catalog row unlocks_after inside PlacementValidator.CanPlace per §7 -->`

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Fix `ia/backlog/TECH-692.yaml` `spec:` path if validator errors.

#### Step 2 — Validator unlock branch

**Goal:** Consult `unlocks_after` before economy + zoning outcomes (ordering per §7).

**Edits:**

- `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs` — **before:** `    public string unlocks_after;` — **after:** `    public string unlocks_after; // PlacementValidator TECH-692 reads via catalog indexes`

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** If comment-only change is insufficient, implement real unlock lookup in `PlacementValidator` using public catalog query APIs from §7.

## Open Questions (resolve before / during implementation)

1. Canonical unlock query API name — discover at implement time or add thin adapter.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
