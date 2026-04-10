---
purpose: "Project spec for TECH-05 — Extract duplicated dependency resolution pattern."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-05 — Extract duplicated dependency resolution pattern

> **Issue:** [TECH-05](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — [`.cursor/specs/unity-development-context.md`](../../.cursor/specs/unity-development-context.md) (**`FindObjectOfType`** policy); **TECH-26** (scanner) after changes.

## 1. Summary

~25+ managers duplicate the same **`Awake`** / **`Start`** pattern: **`if (x == null) x = FindObjectOfType<X>();`**. Consolidate via a small **helper**, **base class**, or **extension** to reduce copy-paste while keeping **Inspector-first** assignment and project **FindObjectOfType** policy.

## 2. Goals and Non-Goals

### 2.1 Goals

1. DRY for dependency resolution without violating **invariants** (still no **`FindObjectOfType`** in **`Update`**).
2. Preserve **MonoBehaviour** scene component pattern — no new **singletons**.

### 2.2 Non-Goals (Out of Scope)

1. Introducing a full DI framework.
2. Migrating every manager in one PR without validation.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want one idiom for wiring managers. | Helper used in pilot managers; docs in **unity-development-context** or **coding-conventions**. |
| 2 | Player | I want no behavior change. | Same init order outcomes for pilot scenes. |

## 4. Current State

### 4.1 Domain behavior

N/A.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — TECH-05 |
| Candidates | `rg "FindObjectOfType"` in **`Managers`** |

### 4.3 Implementation investigation notes (optional)

- Options: **`Component.Resolve<T>(ref field)`** extension in **`Territory.Core`** or **`Utilities`**; or **`ManagerDependencyHelper`** static methods; or lightweight **`MonoBehaviour`** base (risk: inheritance coupling).
- **SerializeField private** alignment with **TECH-02** — coordinate order of work.

## 5. Proposed Design

### 5.1 Target behavior (product)

No change.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Prototype helper API (name, namespace) in **`Utilities`** or agreed folder.
2. Migrate 2–3 pilot managers; measure readability.
3. Document pattern in **`coding-conventions.mdc`** or **unity-development-context.md**.
4. Optionally roll out incrementally (follow-up PRs).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Pilot-first | Limits blast radius | Repo-wide automated replace |

## 7. Implementation Plan

### Phase 1 — API sketch

- [ ] Add helper (e.g. **`void EnsureRef<T>(ref T field) where T : Object`** with **`FindObjectOfType`**).
- [ ] Unit-style compile check; XML doc on public API.

### Phase 2 — Pilot migrations

- [ ] Replace duplicate blocks in 2–3 small managers.
- [ ] Unity: smoke **New Game**.

### Phase 3 — Documentation

- [ ] Update **coding conventions** or **AGENTS** pointer.

## 8. Acceptance Criteria

- [ ] Helper exists and is used in at least two managers without compile regressions.
- [ ] Documented pattern for future PRs.
- [ ] No **`FindObjectOfType`** added to per-frame paths.
- [ ] **Unity:** Pilot scenes load; **Awake**/**Start** order unchanged for tested managers.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. None for **game logic**.
