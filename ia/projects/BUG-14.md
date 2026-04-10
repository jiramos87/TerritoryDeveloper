---
purpose: "Project spec for BUG-14 — FindObjectOfType in Update / per-frame paths."
audience: both
loaded_by: ondemand
slices_via: none
---
# BUG-14 — FindObjectOfType in Update / per-frame paths

> **Issue:** [BUG-14](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-02
> **Last updated:** 2026-04-08 (implementation pass)

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **1** / **TECH-26** (CI scanner for **`FindObjectOfType`** in hot paths) prevents regressions after this fix.

## 1. Summary

Project **invariants** forbid **`FindObjectOfType`** inside **`Update`**, **`LateUpdate`**, **`FixedUpdate`**, or other per-frame hot paths — cache references in **`Awake`** / **`Start`** (see **`unity-development-context`** §7 — anti-patterns table). HUD logic lives on a **partial** of **`UIManager`** (`UIManager.Hud.cs`): **`UpdateUI`** still resolves **`EmploymentManager`**, **`DemandManager`**, and **`StatisticsManager`** every frame; **`UpdateGridCoordinatesDebugText`** (invoked from **`LateUpdate`**) still resolves **`GameDebugInfoBuilder`** and **`WaterManager`** when fields are null. **`WaterManager`** is already resolved once in **`UIManager.Start`**, but the debug path must not repeat scene queries per frame. **`CursorManager`** resolves **`UIManager`** once in **`Start`** only — no per-frame lookup there (backlog **Files** may say “Update” for historical reasons; treat **`Start`** as the actual site).

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

**Observed (verified 2026-04-08):** When **`cityStats`** is non-null, **`Update`** calls **`UpdateUI()`** every frame. Inside **`UIManager.Hud.cs`**, **`UpdateUI`** performs **`FindObjectOfType<EmploymentManager>`**, **`FindObjectOfType<DemandManager>`**, and **`FindObjectOfType<StatisticsManager>`** on every invocation (lines 58–60). **`LateUpdate`** calls **`UpdateGridCoordinatesDebugText`**, which may call **`FindObjectOfType<GameDebugInfoBuilder>`** (lines 101–102) and **`FindObjectOfType<WaterManager>`** when **`waterManager`** is null (lines 116–117).  
**Expected:** No scene-wide type queries in those hot paths; resolve once at initialization per **invariants** and **`IF adding a manager reference → THEN [SerializeField] private + FindObjectOfType fallback in Awake`** (guardrail — use **`Awake`** / **`Start`** consistently with sibling **`MonoBehaviour`** order; see **`unity-development-context`** §2).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — BUG-14 |
| Code | `UIManager.cs` — **`Update`**, **`LateUpdate`**, **`Start`** ( **`cityStats`**, **`waterManager`** one-shot resolution; **`EnsureConstructionCostTextExists`** ) |
| Code | `UIManager.Hud.cs` — **`UpdateUI`**, **`UpdateGridCoordinatesDebugText`** (hot-path violations) |
| Code | `CursorManager.cs` — **`Start`** resolves **`UIManager`** once; **`Update`** uses cached reference |
| Reference | `.cursor/specs/ui-design-system.md` (UI foundations), `.cursor/specs/managers-reference.md` (**Demand**, economy-adjacent managers) |
| Rules | `.cursor/rules/invariants.mdc`, **`unity-development-context`** §2, §7 |

### 4.3 Implementation investigation notes (optional)

- **`CloseAllPopups`** / popup stack: **`FindObjectOfType<DataPopupController>`** in `UIManager.PopupStack.cs` — event-driven, not per-frame; out of scope unless profiling shows spikes.
- **`EnsureConstructionCostTextExists`** — **`FindObjectOfType<Canvas>`** in **`Start`** only; out of scope.
- **`UIManager.WelcomeBriefing.cs`** — **`FindObjectOfType<Canvas>`** on welcome path; not per-frame; out of scope.
- **`GridManager`** already exposes **`demandManager`** in places (e.g. demand feedback / warnings in **`UIManager.Hud.cs`**). Prefer **one** cached reference on **`UIManager`** for **`UpdateUI`** (Inspector + fallback) rather than mixing **`gridManager.demandManager`** and **`FindObjectOfType`** — agent-owned, but avoids split brain if **`GridManager`** reference is null in a test scene.
- **`StatisticsManager`**: **`UpdateUI`** assigns **`stats`** via **`FindObjectOfType<StatisticsManager>()`** but never reads **`stats`** (dead per-frame query as of 2026-04-08). Implementation may drop the lookup or wire the intended HUD field — either satisfies the invariant.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change; smoother CPU profile on UI-heavy frames.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. On **`UIManager`** (main partial — keep **`[SerializeField]`** fields on the type for Inspector wiring): add **`[SerializeField] private EmploymentManager employmentManager`** and **`demandManager`**; omit **`StatisticsManager`** unless a HUD consumer exists (it was previously resolved every frame but never read). **`GameDebugInfoBuilder`** is already **`[SerializeField] private`** — populate in **`Start`** via Inspector or single fallback, not inside **`UpdateGridCoordinatesDebugText`**.
2. In **`Awake`** or **`Start`**, assign each via Inspector or **`FindObjectOfType<T>()`** once when null (same pattern as **`cityStats`** / **`waterManager`** in **`UIManager.Start`**).
3. **`UIManager.Hud.cs` — `UpdateUI`**: use cached **`employmentManager`** / **`demandManager`** (no per-frame **`FindObjectOfType`**); preserve existing null-gated text updates (empty or skip when manager missing).
4. **`UIManager.Hud.cs` — `UpdateGridCoordinatesDebugText`**: remove lazy **`FindObjectOfType`** branches; rely on **`waterManager`** from **`Start`** and serialized / **`Start`**-resolved **`gameDebugInfoBuilder`**. If **`useFullDebugText`** is true but the builder is absent, keep the shorter coordinate line without querying the scene every frame.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec scoped to per-frame paths | Matches invariant wording | Auditing entire `UIManager` |
| 2026-04-08 | Kickoff pass vs current repo | Violations still in `UIManager.Hud.cs`; goals and acceptance unchanged | Closing issue as obsolete without code change (rejected) |
| 2026-04-08 | Implementation | Cache in **`Start`** + drop dead **`StatisticsManager`** query | Adding unused **`statisticsManager`** field |

## 7. Implementation Plan

### Phase 1 — Cache managers used in `UpdateUI`

- [x] Add **`EmploymentManager`**, **`DemandManager`** fields on **`UIManager`** with **`[SerializeField] private`** + null-safe **`Start`** fallback **`FindObjectOfType`** once each. (**`StatisticsManager`** was unused in **`UpdateUI`** — removed the dead per-frame lookup; no serialized field added.)
- [x] Edit **`UIManager.Hud.cs`** — **`UpdateUI`**: use cached fields; no **`FindObjectOfType`** in this method.

### Phase 2 — `LateUpdate` debug path

- [x] Edit **`UIManager.Hud.cs`** — **`UpdateGridCoordinatesDebugText`**: no lazy **`FindObjectOfType`**. **`gameDebugInfoBuilder`** is resolved once in **`UIManager.Start`** when null; **`waterManager`** remains one-shot in **`Start`** (debug path only reads it).

### Phase 3 — Verification

- [x] Repository search: **`UIManager.Hud.cs`** contains no **`FindObjectOfType`**; remaining **`UIManager*.cs`** uses are **`Start`** / event paths only.
- [ ] Unity: compile; Play Mode — HUD money, demand strings, unemployment / jobs lines, and optional grid debug line (with and without assigned **`GameDebugInfoBuilder`**) behave as before (**owner confirmation**).
- [ ] When **TECH-26** lands, confirm the per-frame **`FindObjectOfType`** scanner stays green for touched paths.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| No **`FindObjectOfType`** in **`UpdateUI`** or **`UpdateGridCoordinatesDebugText`** | Manual / repo | `rg -n "FindObjectOfType" Assets/Scripts/Managers/GameManagers/UIManager.Hud.cs` | After fix: matches only if moved outside hot paths (should be none in those methods). |
| HUD labels unchanged | Manual / Unity | Play Mode smoke | Unemployment %, job counts, R/C/I demand text, demand bar fills. |
| Grid coordinate debug line | Manual / Unity | Play Mode with **`gridCoordinatesText`** assigned | Toggle **`useFullDebugText`**; with builder unassigned, expect coordinate-only line without per-frame **`FindObjectOfType`**. |
| **TECH-26** scanner (when available) | CI / Node | Per **TECH-26** tooling | **MCP / dev machine** optional row — not in **`ia-tools.yml`** until shipped. |

## 8. Acceptance Criteria

- [x] No **`FindObjectOfType`** in **`UIManager.UpdateUI`**.
- [x] No **`FindObjectOfType`** in **`UIManager.UpdateGridCoordinatesDebugText`**.
- [x] **`CursorManager`**: **`Update`** does not call **`FindObjectOfType`** (unchanged; verified in repo).
- [ ] **Unity:** compile; smoke test HUD and debug coordinates line (**pending owner**).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | **`StatisticsManager`** queried every frame but never used | Legacy / dead code | Removed lookup from **`UpdateUI`**; no HUD wiring added |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. None for **game logic** — this is a performance/engineering invariant. If a manager is intentionally absent in a test scene, null-safe UI (empty strings) is acceptable.
