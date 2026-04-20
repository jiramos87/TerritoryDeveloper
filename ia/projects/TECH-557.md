---
purpose: "TECH-557 — Budget panel UI with sliders."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T7.5"
---
# TECH-557 — Budget panel UI with sliders

> **Issue:** [TECH-557](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

**BudgetPanel** exposes seven **envelope** percentage sliders (one per **Zone S** sub-type), one **global cap** slider, and **remaining-this-month** readouts. Opens from **HUD**; commits call **`SetEnvelopePct`** / allocator API so stored model **sum-locks**; UI **re-reads** normalized values after commit.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `BudgetPanel.cs` + prefab; entry from `UIManager.Hud` (or equivalent HUD strip).
2. Sliders labeled from registry; global cap slider; seven “remaining” labels.
3. On end-drag / commit: call allocator normalize path; refresh UI from service read-model.

### 2.2 Non-Goals (Out of Scope)

1. Bond issuance — Stage 8.
2. Historical spend charts.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Tune monthly envelope split | Sliders + live remainders |
| 2 | Developer | Allocator single source of truth | UI reflects post-normalize state |

## 4. Current State

### 4.1 Domain behavior

**BudgetAllocationService** owns percentages × global cap; **TryDraw** consumes envelope remainder.

### 4.2 Systems map

- `BudgetAllocationService` / `IBudgetAllocator` — `SetEnvelopePct`, getters for pct + remainder
- `ZoneSubTypeRegistry` — labels
- `UIManager.Hud`
- New `BudgetPanel.cs` + prefab

### 4.3 Implementation investigation notes (optional)

Confirm public API names on allocator from prior stages (grep `SetEnvelopePct`).

## 5. Proposed Design

### 5.1 Target behavior (product)

Player sees consistent numbers after commit even when normalize adjusts siblings.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Either listener on allocator changed event or explicit refresh after each commit; avoid fighting slider `SetValueWithoutNotify` loops.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-20 | Commit on end-drag | Reduces normalize churn | Per-tick commit (rejected — noisy) |

## 7. Implementation Plan

### Phase 1 — Layout

- [ ] Prefab: 7 rows + cap + readouts; bind to registry names.

### Phase 2 — Allocator wire

- [ ] Read initial state; on commit call `SetEnvelopePct`; refresh all sliders + labels from model.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Node | `npm run unity:compile-check` | |
| Normalize round-trip | Manual | Drag one slider; others adjust | Play Mode |

## 8. Acceptance Criteria

- [ ] HUD opens panel; seven + cap sliders functional.
- [ ] Readouts match allocator remainder fields.
- [ ] Post-commit UI matches normalized state.
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: slider feedback loop — mitigation: `SetValueWithoutNotify` when pushing model → UI.
- H6 overlap: **UIManager** also touched by TECH-553 — sequence commits or merge conflict watch.
- Invariant: envelope math stays in allocator — panel is view + command only.

### §Examples

| User action | Model | UI |
|-------------|-------|-----|
| Raise slider 1 | normalize redistributes | all seven reflect new pct |
| Lower global cap | cap clamps | readouts update |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| panel_reflects_normalize | commit uneven pct | sum = 100% after normalize | Manual |
| remainder_labels | after TryDraw in test scene | labels match service | Play/Edit |

### §Acceptance

- [ ] Panel + HUD entry.
- [ ] Sliders + cap + readouts wired.
- [ ] Compile green.

## Open Questions (resolve before / during implementation)

1. Exact remainder field names on read-model — grep allocator; mirror in UI binding.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
