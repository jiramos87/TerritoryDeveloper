---
purpose: "TECH-566 — Bond-active HUD flag + entry point."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T8.2"
---
# TECH-566 — Bond-active HUD flag + entry point

> **Issue:** [TECH-566](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Persistent HUD indicator for active bond; drill-down reuses issuance modal in read-only mode with issue disabled.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Badge text: active bond summary + arrears flag when ledger marks arrears.
2. Click opens `BondIssuanceModal` read-only path (no issue) showing current **BondData**.
3. Wire HUD / budget entry points without breaking Stage 7 layout.

### 2.2 Non-Goals (Out of Scope)

1. Multiple city tiers in HUD (MVP single tier).
2. Bond refinancing UI.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I see when a bond is active and can open details | Badge + modal read-only |
| 2 | Developer | HUD uses same modal class as TECH-565 with mode flag | Shared component |

## 4. Current State

### 4.1 Domain behavior

Ledger exposes active bond + arrears; HUD strip exists from prior UI work.

### 4.2 Systems map

- `UIManager.Hud`, `BondLedgerService`, `BondIssuanceModal`

### 4.3 Implementation investigation notes (optional)

Prefer event/callback from ledger over per-frame polling if API exists.

## 5. Proposed Design

### 5.1 Target behavior (product)

When bond active: show months remaining + monthly repayment; arrears → red badge. Click opens modal in detail mode.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

`BondIssuanceModal` gains `ShowReadOnly(BondData)` or equivalent; HUD subscribes to ledger changes.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Reuse issuance modal for detail | Single prefab; less art | Separate detail panel (rejected) |

## 7. Implementation Plan

### Phase 1 — Badge presenter

- [ ] Add HUD badge row; bind to `BondLedgerService.GetActiveBond` / arrears flag.
- [ ] Entry from HUD strip + optional budget button hook.

### Phase 2 — Modal dual mode

- [ ] Extend `BondIssuanceModal` for read-only: hide issue, show bond fields.
- [ ] Arrears styling on badge.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Badge visibility | Manual / bridge | `unity_bridge_command` screenshot optional | After TECH-565 modal exists |

## 8. Acceptance Criteria

- [ ] HUD badge shows active bond summary + arrears styling when ledger flags arrears
- [ ] Click opens `BondIssuanceModal` read-only; issue path disabled

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: HUD polling **BondLedgerService** every frame — prefer event or refresh on economy tick. Mitigation: align with **CityStats** refresh cadence if shared.
- **Arrears** display must read **BondData.arrears** from ledger; red badge only — no new notification spam (N6 review note).
- **Read-only** modal mode must not call **TryIssueBond**; gate issue button + input interactable.

### §Examples

| Bond state | Badge text | Click |
|------------|------------|-------|
| Active, 23 mo left, 233/mo | "Active bond: 23 mo, 233/mo" | Opens modal detail |
| **arrears** true | Red badge + arrears copy | Same drill-down |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| badge_visible_when_active | Ledger has tier bond | Badge active | EditMode / scene |
| modal_readonly | Click badge | **BondIssuanceModal** issue disabled | manual |

### §Acceptance

- [ ] Badge shows months + repayment; arrears red styling
- [ ] Click opens **BondIssuanceModal** read-only; issue path disabled

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. None — behavior locked in orchestrator Stage 8 Exit unless tier selection multiplies later.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
