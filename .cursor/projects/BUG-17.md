# BUG-17 — cachedCamera null when creating ChunkCullingSystem

> **Issue:** [BUG-17](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — **TECH-20** (Unity lifecycle / execution order); **TECH-28** (Editor context export for repro snapshots).

## 1. Summary

**`GridManager.InitializeGrid()`** may construct **`ChunkCullingSystem`** while **`cachedCamera`** is still null because **`cachedCamera`** is assigned in **`Update()`**. That risks **NullReferenceException** or broken culling during early grid setup.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **`ChunkCullingSystem`** always receives a valid **`Camera`** reference when the grid initializes, or culling defers safely until the camera exists.
2. No regression to chunk visibility or **sorting** behavior after play.

### 2.2 Non-Goals (Out of Scope)

1. Rewriting the entire **culling** algorithm.
2. **`TECH-01`**-scale **`GridManager`** decomposition (stay minimal).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want a stable game start without exceptions. | No NRE from **culling** init on **New Game** / load. |
| 2 | Developer | I want camera resolution consistent with project patterns. | **`Camera.main`** or serialized reference resolved before **culling** uses it. |

## 4. Current State

### 4.1 Domain behavior

**Observed:** **`cachedCamera`** populated in **`Update`**; **`InitializeGrid`** runs earlier.  
**Expected:** Camera available before **culling** depends on it.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — BUG-17 |
| Code | `GridManager.cs` — **`InitializeGrid`**, **`ChunkCullingSystem`**, **`Update`** |
| Related | **BUG-16** (init ordering), **TECH-20** (execution order doc) |

### 4.3 Implementation investigation notes (optional)

- Options: assign **`cachedCamera = Camera.main`** (or serialized field) in **`Awake`** / **`Start`** before **`InitializeGrid`**; or lazy-init **culling** after first camera resolve; or pass camera into **`ChunkCullingSystem`** constructor from a known-good path.

## 5. Proposed Design

### 5.1 Target behavior (product)

Grid initialization does not throw and **culling** attaches to the game **camera** used for the isometric view.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Trace call order: **`InitializeGrid`** vs **`Awake`/`Start`/`Update`**.
2. Ensure **`cachedCamera`** (or equivalent) is set before **`ChunkCullingSystem`** needs it.
3. If **`Camera.main`** is null in early frame, document fallback (defer culling init to first **`Update`** where camera exists) in **Decision Log**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | From agent-friendly §6 | — |

## 7. Implementation Plan

### Phase 1 — Trace and fix ordering

- [ ] Read **`GridManager`** regions for **culling** and **camera** cache.
- [ ] Implement earliest safe assignment of **camera** reference.

### Phase 2 — Verify

- [ ] **New Game** and **Load Game**: no NRE; chunks cull as before.
- [ ] If project uses multiple cameras, confirm correct reference (main isometric camera).

## 8. Acceptance Criteria

- [ ] No **NullReferenceException** from **culling** creation during **`InitializeGrid`**.
- [ ] **Unity:** Visual chunk behavior unchanged in normal play (spot-check camera pan).
- [ ] **`/// <summary>`** on **`GridManager`** accurate if lifecycle wording changes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. None for **simulation** rules — **camera** identity is engine/setup. If **multi-camera** setups exist in some scenes, confirm which camera **culling** must follow (product/scene owner).
