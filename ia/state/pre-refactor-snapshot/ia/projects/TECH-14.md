---
purpose: "Project spec for TECH-14 — Remove residual placeholder / test scripts."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-14 — Remove residual placeholder / test scripts

> **Issue:** [TECH-14](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — **TECH-33** prefab/scene scans can validate no dangling references after stub removal.

## 1. Summary

Remove or replace **`CityManager.cs`** (namespace stub) and **`TestScript.cs`** (compile smoke test) if nothing in scenes, prefabs, or other scripts references them. Reduces noise for agents and humans navigating **`Assets/Scripts`**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Confirm zero references to **`CityManager`** and **`TestScript`** (code, scenes, prefabs, **`Resources`**).
2. Delete unused files; if referenced, document why and defer removal.

### 2.2 Non-Goals (Out of Scope)

1. Removing other legacy stubs not listed in **TECH-14**.
2. Changing build / CI smoke-test strategy without replacement.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want the repo free of dead **MonoBehaviour** stubs. | No orphan **`CityManager`** / **`TestScript`** unless explicitly retained. |
| 2 | QA | I want **MainScene** and saves to load after cleanup. | No missing-script components on load. |

## 4. Current State

### 4.1 Domain behavior

N/A (tooling / hygiene).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — TECH-14 |
| Files | `Assets/Scripts/Managers/GameManagers/CityManager.cs`, `TestScript.cs` |

### 4.3 Implementation investigation notes (optional)

- Search: `rg CityManager`, `rg TestScript`, Unity **Find References** if available.
- Check **`.unity`** YAML for **`MonoBehaviour`** script fileID / GUID (match **`.meta`**).

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-facing change.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Full-text and asset search for references.
2. If none: delete both **`.cs`** and **`.meta`** files; commit.
3. If references exist: add **Decision Log** entry and either remove references first or close **TECH-14** sub-scope with backlog note.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Agent-friendly audit task | — |

## 7. Implementation Plan

### Phase 1 — Reference audit

- [ ] `rg -n "CityManager"` and `rg -n "TestScript"` across repo (include **`Assets/`**).
- [ ] Search scene/prefab YAML for script GUIDs from **`.meta`**.

### Phase 2 — Remove or defer

- [ ] If safe: delete files; verify Unity compiles.
- [ ] If not safe: list blocking references in **Issues Found** and backlog **Notes**.

## 8. Acceptance Criteria

- [ ] Either files removed with no broken references, or documented blockers with next steps.
- [ ] Project compiles in Unity after changes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. None — **game logic** not affected.
