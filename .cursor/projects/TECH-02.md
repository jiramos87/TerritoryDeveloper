# TECH-02 — Change public fields to [SerializeField] private in managers

> **Issue:** [TECH-02](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **33** (public API / XML doc report) optional follow-up; [unity-development-context.md](../../.cursor/specs/unity-development-context.md) for **SerializeField** policy.

## 1. Summary

Many managers expose dependencies as **`public`** fields, allowing any caller to mutate references. Project direction: **`[SerializeField] private`** with **public read-only accessors** or **`[SerializeField]`** only where Inspector assignment is required, reducing accidental coupling.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Encapsulate dependencies listed in **TECH-02** backlog (**`ZoneManager`**, **`RoadManager`**, **`GridManager`**, **`CityStats`**, **`AutoZoningManager`**, **`AutoRoadBuilder`**, **`UIManager`**, **`WaterManager`**).
2. Preserve **Unity** Inspector assignments (serialized fields keep **`[FormerlySerializedAs]`** if renames break YAML — use only when needed).

### 2.2 Non-Goals (Out of Scope)

1. One giant PR touching every script in **`Assets/Scripts`** beyond backlog list.
2. Changing runtime behavior of **simulation** (refactor only).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want clear boundaries between managers. | External code uses methods/properties, not **`public`** manager fields. |
| 2 | Designer | I want prefabs to still wire in Inspector. | Serialized private fields retain assignments after migration. |

## 4. Current State

### 4.1 Domain behavior

N/A (engineering encapsulation).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — TECH-02 |
| Files | Per backlog **Files** list |
| Risk | External **`SomeManager.otherManager.foo`** compile breaks — grep callers |

### 4.3 Implementation investigation notes (optional)

- **Agent-friendly approach:** one manager per PR or phase; run Unity to fix missing references.
- Add **`public X GetX()`** or properties only where other code legitimately reads the dependency.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible change.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Per file: change **`public ManagerType x`** → **`[SerializeField] private ManagerType x`**.
2. Expose **`public ManagerType X => x;`** or specific methods for cross-manager use.
3. Fix compile errors in dependents.
4. Open **`MainScene`** (and prefabs): confirm no “missing script” / unassigned references.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Phased by manager | Safer than monolith | Single PR |

## 7. Implementation Plan

### Phase 1 — Inventory callers

- [ ] Pick first manager (e.g. **`CityStats`** — smaller surface).
- [ ] `rg` for external reads of its **`public`** fields.

### Phase 2 — Migrate + fix

- [ ] Apply **`SerializeField` private** + accessors.
- [ ] Repeat per backlog manager.

### Phase 3 — Unity validation

- [ ] Load **MainScene**; run smoke **New Game**; watch Console for null refs.

## 8. Acceptance Criteria

- [ ] Backlog-listed managers no longer use **`public`** fields for dependencies that should be encapsulated (per phased completion — note partial completion in backlog if needed).
- [ ] **Unity:** Project compiles; **MainScene** (or agreed scene) loads; Inspector references intact on touched prefabs.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. None for **game logic**. Serialization migration is **engineering** only.
