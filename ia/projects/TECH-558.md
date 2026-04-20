---
purpose: "TECH-558 — Overspend-blocked notification wiring."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T7.6"
---
# TECH-558 — Overspend-blocked notification wiring

> **Issue:** [TECH-558](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

When **`BudgetAllocationService.TryDraw`** returns **false**, surface **player-visible** feedback via **`GameNotificationManager`** (or existing notification channel): transient HUD badge ~**3s**, text like **`{displayName} envelope exhausted`**, aligned with **Example 2** in exploration.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Hook failure path of `TryDraw` — event, callback, or wrapper approved by existing patterns.
2. Message includes sub-type **displayName** from registry (id known at failure site).
3. ~3s transient presentation; styling consistent with existing HUD notifications.

### 2.2 Non-Goals (Out of Scope)

1. Full modal error — badge only MVP.
2. Sound effect — optional, not required.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Know why place blocked | Clear envelope message |
| 2 | Developer | No duplicate singletons | Use `GameNotificationManager.Instance` pattern |

## 4. Current State

### 4.1 Domain behavior

**Example 2** — insufficient envelope leaves grid and balances unchanged; user must see reason.

### 4.2 Systems map

- `BudgetAllocationService` — `TryDraw` call sites / return false path
- `GameNotificationManager`
- `ZoneSubTypeRegistry` — resolve display name from id
- HUD badge presenter (if separate from notification manager)

### 4.3 Implementation investigation notes (optional)

Grep `GameNotificationManager` usage for existing transient messages.

## 5. Proposed Design

### 5.1 Target behavior (product)

Non-blocking, dismisses automatically; does not steal focus from placement tool.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Prefer raising one notification type with payload `{ message, duration }` to keep service UI-agnostic if pattern exists.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-20 | 3s duration | Matches Stage Exit bullet | 5s (rejected — longer annoyance) |

## 7. Implementation Plan

### Phase 1 — Failure signal

- [ ] Extend `TryDraw` failure to emit event or call notification helper with sub-type id.

### Phase 2 — HUD

- [ ] Subscribe / display badge; timer hide 3s.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Node | `npm run unity:compile-check` | |
| Example 2 manual | Human | Force TryDraw false | Play Mode |

## 8. Acceptance Criteria

- [ ] TryDraw false → visible message with sub-type name.
- [ ] ~3s transient; style matches existing notifications.
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: spam on repeated failures — mitigation: throttle or coalesce if same id within 1s (optional polish).
- **GameNotificationManager** is allowed singleton — use documented API only.
- Coupling: failure site must have **subTypeId** — already on **ZoneSService** path.

### §Examples

| TryDraw | id | Message |
|---------|-----|---------|
| false | 3 (Fire) | “Fire envelope exhausted” |
| true | — | no badge |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| overspend_shows_notice | Mock TryDraw false | notification fired | EditMode / integration |
| duration_3s | — | hidden after ~3s | Manual |

### §Acceptance

- [ ] Failure path wired.
- [ ] Message + duration per spec.
- [ ] Compile green.

## Open Questions (resolve before / during implementation)

1. Whether notification API supports duration parameter — if not, HUD coroutine on listener.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
