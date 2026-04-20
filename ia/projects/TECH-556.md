---
purpose: "TECH-556 — Picker cancel UX (N3)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T7.4"
---
# TECH-556 — Picker cancel UX (N3)

> **Issue:** [TECH-556](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

**Cancel** sub-type picker via **ESC** or **outside-click**: no charge, no placement, **`currentSubTypeId = -1`**, exit **S placement mode**. Matches **`docs/zone-s-economy-exploration.md`** Review Note **N3**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. ESC closes modal and clears selection + exits S placement.
2. Backdrop / outside click same as ESC.
3. XML summary on `SubTypePickerModal` references N3.

### 2.2 Non-Goals (Out of Scope)

1. Animated cancel FX.
2. Undo after successful place.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Abort S flow without cost | Cancel returns to prior mode |
| 2 | Developer | Documented contract | N3 cited in XML |

## 4. Current State

### 4.1 Domain behavior

Exploration **N3** locks: dismiss picker must not mutate treasury or grid.

### 4.2 Systems map

- `SubTypePickerModal`
- Input — `Update` ESC check or new Input System action
- `PopupStack` pop
- Placement / `ZoneManager` — exit S mode

### 4.3 Implementation investigation notes (optional)

Reuse backdrop button pattern from other popups if present.

## 5. Proposed Design

### 5.1 Target behavior (product)

Indistinguishable from “never started S place” financially.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Central `OnCancel()` used by ESC, backdrop, and optional Cancel button if any.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-20 | -1 reset | Aligns TECH-554 guard | Separate “cancelled” bool (rejected — extra state) |

## 7. Implementation Plan

### Phase 1 — Input + backdrop

- [ ] Wire ESC and transparent backdrop to `OnCancel`.

### Phase 2 — State reset

- [ ] Pop stack; set id -1; `ZoneManager` exits S placement / neutral tool.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Node | `npm run unity:compile-check` | |
| No spend on cancel | Manual | Treasury unchanged | Play Mode |

## 8. Acceptance Criteria

- [ ] ESC + outside-click: no placement, no spend, id -1, exit S placement.
- [ ] XML docs reference N3.
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: ESC also closes other UI — mitigation: modal consumes event when top of stack only.
- Coupling: must call same reset as failed place path if any — avoid duplicate logic.
- Invariant: spend only inside approved services — cancel must not touch **TryDraw**.

### §Examples

| Action | Treasury | Grid | activeType |
|--------|----------|------|------------|
| Open picker then ESC | unchanged | unchanged | non-S or tool cleared |
| Pick then place | per service | zone added | — |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| cancel_resets_id | ESC in picker | id -1 | Manual |
| cancel_no_try_draw | cancel | no TryDraw | Code review / log |

### §Acceptance

- [ ] Both cancel paths behave per N3.
- [ ] XML cites N3.
- [ ] Compile green.

## Open Questions (resolve before / during implementation)

1. Neutral tool state after cancel — last RCI type vs “no zoning” — product default: exit S mode to no active place tool unless design says otherwise; document in Decision Log if changed.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
