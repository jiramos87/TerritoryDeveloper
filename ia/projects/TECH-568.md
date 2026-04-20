---
purpose: "TECH-568 — HUD income-minus-maintenance hint update."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T8.4"
---
# TECH-568 — HUD income-minus-maintenance hint update

> **Issue:** [TECH-568](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Align projected surplus hint with **totalEnvelopeCap** on **CityStats** so player sees monthly picture after **Zone S** envelope (and label reflects bond repayment wording per Stage 8 Exit).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Locate existing projected income-minus-maintenance HUD formula site.
2. Incorporate `cityStats.totalEnvelopeCap` (and bond repayment line from read model if separate from surplus).
3. Update label to Stage 8 Exit string: estimated monthly surplus after S envelope + bond repayment.

### 2.2 Non-Goals (Out of Scope)

1. Rewriting unrelated HUD rows.
2. Per-sub-type envelope breakdown in this line.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Surplus hint accounts for S envelope ceiling | Number changes when cap changes |
| 2 | Developer | Single formula site | Grep-confirmed |

## 4. Current State

### 4.1 Domain behavior

HUD shows maintenance vs income hint; **CityStats** gains envelope fields in TECH-567.

### 4.2 Systems map

- `UIManager.Hud` (or dedicated HUD presenter), `CityStats`

### 4.3 Implementation investigation notes (optional)

Read `docs/zone-s-economy-exploration.md` §Subsystem Impact for wording parity.

## 5. Proposed Design

### 5.1 Target behavior (product)

Subtract `totalEnvelopeCap` from projected monthly balance line; label documents envelope + bond repayment.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

After TECH-567 lands, read `CityStats` from HUD binding layer; avoid duplicate economy queries.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Subtract cap not per-draw | Matches orchestrator Exit | Per-draw sum (rejected) |

## 7. Implementation Plan

### Phase 1 — Trace formula

- [ ] Find string + calculation for projected income minus maintenance.
- [ ] Document current operands.

### Phase 2 — Apply cap + copy

- [ ] Subtract `totalEnvelopeCap`; incorporate `monthlyBondRepayment` if not already in line.
- [ ] Update localized / literal label per Exit.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity | `npm run unity:compile-check` | |

## 8. Acceptance Criteria

- [ ] Projected surplus formula incorporates `cityStats.totalEnvelopeCap` per Stage 8 Exit
- [ ] Label text matches Stage 8 orchestrator Exit string for estimated monthly surplus

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: Subtracting **totalEnvelopeCap** while maintenance aggregate already includes S spend — verify exploration §Subsystem Impact vs actual formula; avoid triple-count. Mitigation: trace existing operands before edit.
- Label string must match orchestrator Stage 8 Exit verbatim for QA.
- Depends on **TECH-567** for `totalEnvelopeCap` availability on read model.

### §Examples

| treasury hint line | After change |
|--------------------|--------------|
| Old: income − maintenance | New: same operands − **totalEnvelopeCap** (and bond line per Exit) |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| hint_updates | Cap changes | HUD number decreases by cap delta | manual |
| label | N/A | String contains S envelope + bond repayment wording | manual |

### §Acceptance

- [ ] Formula uses **cityStats.totalEnvelopeCap**; label matches Stage 8 Exit

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. Whether bond repayment is already inside existing “maintenance” aggregate — avoid double-subtract; resolve during impl.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
