---
purpose: "TECH-567 — CityStats envelope + bond fields."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T8.3"
---
# TECH-567 — `CityStats` envelope + bond fields

> **Issue:** [TECH-567](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Surface **BudgetAllocationService** + **BondLedgerService** state on **CityStats** read model for stats UI and downstream HUD hint (TECH-568).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Add `totalEnvelopeCap`, `envelopeRemaining[7]`, `activeBondDebt`, `monthlyBondRepayment` to `CityStats`.
2. Populate from `BudgetAllocationService` + `BondLedgerService` on economy tick.
3. Stats panel labels + values for each field.

### 2.2 Non-Goals (Out of Scope)

1. Historical charts or graphs for envelope.
2. Multi-city stats.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I see envelope and bond figures in city stats | New rows visible |
| 2 | Developer | Read model updated once per tick | No per-frame allocator spam |

## 4. Current State

### 4.1 Domain behavior

**CityStats** holds demand/treasury aggregates; allocator + ledger live on services.

### 4.2 Systems map

- `CityStats.cs`, `CityStatsUIController`, `BudgetAllocationService`, `BondLedgerService`

### 4.3 Implementation investigation notes (optional)

Confirm where monthly economy tick aggregates stats today — hook after allocator reset if needed.

## 5. Proposed Design

### 5.1 Target behavior (product)

Stats panel shows total envelope cap, seven remaining buckets, outstanding bond principal/debt aggregate, monthly bond payment line.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Add fields to `CityStats`; populate from service getters; extend `CityStatsUIController` binding.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | `envelopeRemaining` length 7 | Matches **ZoneSubTypeRegistry** ids 0–6 | List vs array (array locked in Exit) |

## 7. Implementation Plan

### Phase 1 — Fields + tick population

- [ ] Add fields to `CityStats`; populate from services during existing economy/stats refresh.
- [ ] Define `activeBondDebt` semantics (sum outstanding principal vs service API).

### Phase 2 — UI controller

- [ ] Add rows to stats panel prefab / controller for each field.
- [ ] Format integers + labels per UI design system.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Fields populated | EditMode | Unit test or debug assert | Optional if tick hard to isolate |

## 8. Acceptance Criteria

- [ ] `CityStats` exposes `totalEnvelopeCap`, `envelopeRemaining[7]`, `activeBondDebt`, `monthlyBondRepayment`
- [ ] Fields populated from **BudgetAllocationService** + **BondLedgerService**; stats UI shows labels and values

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: Double-counting **monthlyBondRepayment** vs HUD surplus line — coordinate with **TECH-568** so formula does not subtract bond twice. Mitigation: single source of truth comment in **CityStats** population.
- **envelopeRemaining** length 7 must match **ZoneSubTypeRegistry** ids 0–6; invalid index guard on read.
- **activeBondDebt** definition — align with sum of outstanding principal or service helper; document in §5 if product locks.

### §Examples

| Field | Source |
|-------|--------|
| totalEnvelopeCap | **BudgetAllocationService** global monthly cap |
| envelopeRemaining[i] | **currentMonthRemaining** per sub-type |
| monthlyBondRepayment | Sum active bonds’ monthly repayment |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| stats_populated | After tick | **CityStats** fields non-default when services have data | EditMode |
| ui_rows | Open stats panel | Four new rows visible | manual |

### §Acceptance

- [ ] Fields populated from **BudgetAllocationService** + **BondLedgerService**; stats UI shows all four aggregates

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. Exact definition of **activeBondDebt** — outstanding principal sum vs notional; align with **BondData** fields.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
