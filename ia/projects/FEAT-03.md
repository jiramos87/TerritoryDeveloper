---
purpose: "Project spec for FEAT-03 — Forest (coverage) mode hold-to-place."
audience: both
loaded_by: ondemand
slices_via: none
---
# FEAT-03 — Forest (coverage) mode hold-to-place

> **Issue:** [FEAT-03](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — **TECH-26** (no **`FindObjectOfType`** in per-frame paths) applies to input/drag work.

## 1. Summary

**Forest (coverage)** painting today requires **one click per cell**. Players expect **hold and drag** continuous placement, similar to **zoning** drag behavior.

## 2. Goals and Non-Goals

### 2.1 Goals

1. While **forest** tool is active and mouse button held, **forest (coverage)** applies to cells under the cursor path (with sensible rate limiting if needed to avoid per-frame spam).
2. Respect existing validation (eligible **cells**, costs, UI-over-grid gating).

### 2.2 Non-Goals (Out of Scope)

1. **FEAT-04** random spray tool.
2. Changing **forest** maturation over **simulation ticks** (**FEAT-06**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want to paint **forest (coverage)** by dragging. | Hold + drag places **forest** on valid **grass** / eligible tiles without per-cell click. |
| 2 | Player | I want the same rules as single-click placement. | Invalid **cells** still reject; no **forest** through UI. |

## 4. Current State

### 4.1 Domain behavior

**Observed:** Discrete click per **cell**.  
**Expected:** Continuous stroke while pointer moves with button held.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — FEAT-03 |
| Code | `ForestManager.cs`, `GridManager.cs` (input / tool mode routing) |
| Spec | `.cursor/specs/managers-reference.md` — **World features** / **forest** |
| Parallel pattern | **Zoning** drag: **`GridManager`** start/end grid positions |

### 4.3 Implementation investigation notes (optional)

- Reuse **`IsPointerOverGameObject()`** gating if **zoning** uses it.
- Avoid **`FindObjectOfType`** in **`Update`** per **invariants**; cache managers.

## 5. Proposed Design

### 5.1 Target behavior (product)

With **forest** tool selected, holding the placement button and dragging across the **grid** applies **forest (coverage)** to each newly entered valid **cell** (or at a throttled interval along the path — implementation choice that does not change which **cells** are eligible).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Mirror **zoning** mouse-down / mouse-drag / mouse-up on **grid** for **forest** mode.
2. Call existing **`ForestManager`** (or **`GridManager`**) placement API used by single click.
3. Optional: skip repeat application on same **cell** until pointer leaves and re-enters (prevents redundant work).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Agent-friendly feature (localized input) | — |

## 7. Implementation Plan

### Phase 1 — Input wiring

- [ ] Identify single-click **forest** path in **`GridManager`** / **`ForestManager`**.
- [ ] Add drag state: last painted **cell**, button held flag.

### Phase 2 — Apply on drag

- [ ] On drag while held, invoke placement for new **cells** only (or throttled).
- [ ] Respect UI blocking and tool mode.

### Phase 3 — Verify

- [ ] Unity: drag across a **grass** region; **forest** fills path.
- [ ] No per-frame **`FindObjectOfType`**.

## 8. Acceptance Criteria

- [ ] Hold-to-paint **forest (coverage)** works on a flat **grass** test area.
- [ ] Single-click still works.
- [ ] No new singletons; **Inspector** + cache pattern preserved.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. Should drag paint **every frame** on the current **cell**, or only on **cell** entry (crossing **grid** boundaries)? Default **cell-entry** matches many city builders and reduces cost; confirm **product** preference.
2. Does **forest** placement cost **money** per **cell**? If yes, drag must apply same economic rules as click (no free batch).
