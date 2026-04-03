# BUG-14 — FindObjectOfType in Update / per-frame paths

> **Issue:** [BUG-14](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **1** / **TECH-26** (CI scanner for **`FindObjectOfType`** in hot paths) prevents regressions after this fix.

## 1. Summary

Project **invariants** forbid **`FindObjectOfType`** inside **`Update`**, **`LateUpdate`**, **`FixedUpdate`**, or other per-frame hot paths — cache references in **`Awake`** / **`Start`**. **`UIManager.UpdateUI`** still resolves **`EmploymentManager`**, **`DemandManager`**, and **`StatisticsManager`** every frame. **`UpdateGridCoordinatesDebugText`** may call **`FindObjectOfType`** for **`GameDebugInfoBuilder`** and **`WaterManager`**. **`CursorManager`** already caches **`UIManager`** in **`Start`**; verify no remaining per-frame lookups there.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Eliminate per-frame **`FindObjectOfType`** from **`UIManager`** paths invoked from **`Update`** / **`LateUpdate`**.
2. Preserve existing gameplay and UI output; no behavior change beyond performance and null-safety equivalence.

### 2.2 Non-Goals (Out of Scope)

1. Broad refactor of all **`FindObjectOfType`** in the project (e.g. one-off menu code) unless they run every frame.
2. Replacing Inspector wiring with a new DI pattern (**TECH-05** scope).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want stable frame times during normal play. | No **`FindObjectOfType`** inside **`UIManager.UpdateUI`** or per-frame debug text path. |
| 2 | Developer | I want code to respect **invariants**. | **`invariants_summary`** “No FindObjectOfType in Update” satisfied for targeted methods. |

## 4. Current State

### 4.1 Domain behavior

**Observed:** **`UpdateUI()`** calls **`FindObjectOfType`** for three managers each frame when **`cityStats`** is non-null.  
**Expected:** Cached references, per project guardrails.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — BUG-14 |
| Code | `UIManager.cs` — **`Update`**, **`UpdateUI`**, **`UpdateGridCoordinatesDebugText`** |
| Code | `CursorManager.cs` — **`Start`** caches **`UIManager`**; **`Update`** uses **`cachedUIManager`** |
| Rules | `.cursor/rules/invariants.mdc` |

### 4.3 Implementation investigation notes (optional)

- **`CloseAllPopups`** uses **`FindObjectOfType<DataPopupController>`** — not per-frame; lower priority unless profiling shows spikes.
- **`EnsureConstructionCostTextExists`** uses **`FindObjectOfType<Canvas>`** — startup path.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change; smoother CPU profile on UI-heavy frames.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Add **`[SerializeField] private`** or private fields: **`EmploymentManager`**, **`DemandManager`**, **`StatisticsManager`**, and optionally **`GameDebugInfoBuilder`** (already partially serialized).
2. In **`Awake`** or **`Start`**, assign via Inspector or **`FindObjectOfType`** once (match project **`FindObjectOfType`** fallback pattern).
3. **`UpdateUI`**: use cached fields; null-check before use.
4. **`UpdateGridCoordinatesDebugText`**: use cached **`waterManager`** (already lazy in **`Start`** for one path) and cached **`gameDebugInfoBuilder`**; avoid **`FindObjectOfType`** in **`LateUpdate`**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec scoped to per-frame paths | Matches invariant wording | Auditing entire `UIManager` |

## 7. Implementation Plan

### Phase 1 — Cache managers used in UpdateUI

- [ ] Add fields and resolve once in **`Start`** / **`Awake`**.
- [ ] Replace **`FindObjectOfType`** calls inside **`UpdateUI`**.

### Phase 2 — LateUpdate debug path

- [ ] Remove **`FindObjectOfType`** from **`UpdateGridCoordinatesDebugText`** for **`gameDebugInfoBuilder`** and **`waterManager`** (rely on cache set in **`Start`** / **`Awake`**).

### Phase 3 — Verification

- [ ] Grep **`UIManager`** for **`FindObjectOfType`** inside **`Update`**, **`LateUpdate`**, **`UpdateUI`**, **`UpdateGridCoordinatesDebugText`** — expect none.
- [ ] Unity play mode: demand / unemployment / stats labels still update.
- [ ] When **TECH-26** lands, confirm scanner stays green for **`UIManager`** / **`CursorManager`** paths touched here.

## 8. Acceptance Criteria

- [ ] No **`FindObjectOfType`** in **`UIManager.UpdateUI`**.
- [ ] No **`FindObjectOfType`** in **`UIManager.UpdateGridCoordinatesDebugText`**.
- [ ] **`CursorManager`**: confirm **`Update`** does not call **`FindObjectOfType`** (already uses cache).
- [ ] **Unity:** compile; smoke test HUD and debug coordinates line.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. None for **game logic** — this is a performance/engineering invariant. If a manager is intentionally absent in a test scene, null-safe UI (empty strings) is acceptable.
