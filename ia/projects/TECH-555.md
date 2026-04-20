---
purpose: "TECH-555 — Sub-type picker modal UI."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T7.3"
---
# TECH-555 — Sub-type picker modal UI

> **Issue:** [TECH-555](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

**SubTypePickerModal** lists seven **Zone S** entries from **`ZoneSubTypeRegistry`** (icon, display name, base cost). Player picks one; modal commits **`currentSubTypeId`**, closes, resumes placement via **`UIManager.PopupStack`**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New `SubTypePickerModal` under `Assets/Scripts/Managers/GameManagers/UI/` (or existing UI dir per repo).
2. Seven buttons bound from registry; show `displayName`, cost, icon when path non-empty.
3. Click: set id, pop stack, notify placement coordinator (TECH-554).

### 2.2 Non-Goals (Out of Scope)

1. Per-sub-type art polish — empty icon paths OK per Stage 1 scaffolding.
2. Bond issuance UI — Stage 8.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | See all seven services before place | Scroll/grid readable |
| 2 | Developer | Stack discipline | Uses `PopupStack` like other modals |

## 4. Current State

### 4.1 Domain behavior

Registry JSON seeds seven ids; **Zone S** sub-types are data-driven.

### 4.2 Systems map

- `ZoneSubTypeRegistry` — `GetById`, iteration
- `UIManager.PopupStack`
- New `SubTypePickerModal.cs`
- Prefab reference (if pattern uses prefab)

### 4.3 Implementation investigation notes (optional)

Copy modal shell from existing HUD popup (e.g. bond or settings) for stack API.

## 5. Proposed Design

### 5.1 Target behavior (product)

Modal blocks placement until selection or cancel (TECH-556).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Build button row in `Awake`/`Start` from registry list; callback sets static or injected context id.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-20 | Data-driven buttons | Matches registry MVP | Hard-coded seven buttons (rejected) |

## 7. Implementation Plan

### Phase 1 — Modal shell

- [ ] Class + prefab; push on `PopupStack`; backdrop for outside-click (TECH-556).

### Phase 2 — Registry bind

- [ ] Instantiate seven entries; wire selection callback to placement state.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Node | `npm run unity:compile-check` | |
| Registry count | EditMode | Assert 7 buttons | Optional |

## 8. Acceptance Criteria

- [ ] Seven sub-types shown from registry.
- [ ] Selection commits id and closes modal.
- [ ] Integrates with S toolbar entry (TECH-553).
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: **PopupStack** ordering with other modals — mitigation: document push order; S picker should be top when entering S mode.
- **GameNotificationManager** is only singleton allowed — do not add new singleton for picker.
- Registry null / empty — guard with clear log; Stage 1 guarantees seven rows.

### §Examples

| Registry id | Button label | On click |
|-------------|--------------|----------|
| 0 | Police | id=0 committed |
| 6 | Public offices | id=6 committed |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| picker_lists_seven | Load scene w/ registry | 7 active buttons | Play Mode / EditMode |
| pick_commits_id | Click id 3 | context id 3 | Manual |

### §Acceptance

- [ ] Modal + stack integration.
- [ ] Seven entries + selection path.
- [ ] Compile green.

## Open Questions (resolve before / during implementation)

1. Callback shape — `Action<int>`, `UnityEvent`, or direct `ZoneManager` setter — pick consistent with TECH-554.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
