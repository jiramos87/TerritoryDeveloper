---
purpose: "TECH-691 — PlacementValidator affordability gate via catalog baseCost and EconomyManager APIs."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T3.1.4
---
# TECH-691 — Affordability gate

> **Issue:** [TECH-691](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Extend **`PlacementValidator.CanPlace`** to reject when player cannot afford **`baseCost`** (cents) from **`GridAssetCatalog`** economy snapshot for **`assetId`**. Delegate to existing **`EconomyManager`** (or treasury) try/spend/affordability patterns used elsewhere. Failure → **`PlacementFailReason.Unaffordable`**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Read **`baseCost`** from catalog snapshot for **`assetId`**.
2. Reuse existing economy “can afford” / reserve pattern; no parallel treasury logic.
3. Automated test: affordable vs unaffordable (EditMode or pure logic with mocked economy if pattern exists).

### 2.2 Non-Goals (Out of Scope)

1. Loan/bonds edge cases beyond existing economy spec.
2. Refund on undo — follow existing building spend rules.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Cannot place unaffordable catalog asset | Validator returns unaffordable |

## 4. Current State

### 4.1 Domain behavior

**Economy** uses cents and **`EconomyManager`** as hub per **`ia/specs/economy-system.md`**.

### 4.2 Systems map

- **`PlacementValidator.cs`**
- **`EconomyManager`**, **`GridAssetCatalog`**
- **`ia/specs/economy-system.md`**

### 4.3 Implementation investigation notes (optional)

Find existing “try purchase building” or maintenance afford helper to mirror.

## 5. Proposed Design

### 5.1 Target behavior (product)

Insufficient treasury → placement blocked with unaffordable reason (UX in **Stage 3.2**).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Call economy after zoning checks or before — document order; prefer consistent ordering with **TECH-690**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | Cost field | Use `base_cost_cents` from `GridAssetCatalog` economy row via `TryGetEconomyForAsset`; treasury check uses `base_cost_cents / 100` sim units to match `EconomyManager.CanAfford` | Raw cents vs treasury scale |

## 7. Implementation Plan

### Phase 1 — Economy gate

- [ ] Resolve **`baseCost`** for **`assetId`** from catalog.
- [ ] Integrate affordability probe via **`EconomyManager`**.
- [ ] Add test coverage for can/cannot afford.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Affordability tests | Unity Test | Unity Test Runner | Mock or fixture |
| Compile | Unity | `npm run unity:compile-check` |  |

## 8. Acceptance Criteria

- [ ] Query catalog snapshot for **`baseCost`**; align with economy cents.
- [ ] Use existing spend/affordability check pattern.
- [ ] Test(s) for affordable vs unaffordable paths.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

`PlacementValidator.CanPlace` rejects when catalog `base_cost_cents` for `assetId` exceeds treasury headroom using `EconomyManager.CanAfford` (or treasury helper already used for building spends). Returns `PlacementFailReason` value for unaffordable.

### §Acceptance

- [ ] Cost read from `GridAssetCatalog` snapshot indexes (not ad-hoc JSON)
- [ ] Uses existing economy afford/spend patterns from `EconomyManager` / `TreasuryFloorClampService`
- [ ] EditMode or unit coverage for afford vs deny; `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| afford_gate | treasury high vs low | allow / deny | Unity EditMode |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| Treasury | base_cost_cents | Outcome |
|----------|-----------------|--------|
| 0 | 100 | Unaffordable |

### §Mechanical Steps

#### Step 1 — Document economy field source

**Goal:** Spec references DTO field name for implementer.

**Edits:**

- `ia/projects/TECH-691.md` — **before:** `|  |  |  |  |` — **after:** `| 2026-04-22 | Cost field | Use base_cost_cents from GridAssetCatalog economy row (see GridAssetCatalog.Dto.cs) |  |`

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** On validator failure, fix `spec:` pointer in `ia/backlog/TECH-691.yaml` then re-run gate.

#### Step 2 — Affordability branch in validator

**Goal:** Before returning allowed from `CanPlace`, ensure `economyManager.CanAfford(amount)` for resolved `base_cost_cents`.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` — **before:** `            return PlacementResult.Allowed();` — **after:**
```
            int cents = /* resolve base_cost_cents for assetId via GridAssetCatalog public API per §7 */;
            if (economyManager != null && cents > 0 && !economyManager.CanAfford(cents))
                return PlacementResult.Fail(PlacementFailReason.Unaffordable, "Insufficient treasury.");
            return PlacementResult.Allowed();
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** On compile error, complete TECH-689 `PlacementResult` / `PlacementFailReason` first, then re-open this edit block.

## Open Questions (resolve before / during implementation)

1. Does placement deduct on commit or on preview only — follow existing zone/building spend moment.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
