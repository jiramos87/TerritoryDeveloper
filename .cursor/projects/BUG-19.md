# BUG-19 — Load Game scroll wheel also zooms camera

> **Issue:** [BUG-19](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — **TECH-33** (scene **MonoBehaviour** listing for **`MainScene.unity`** / Load Game hierarchy).

## 1. Summary

When the player scrolls the mouse wheel over the **Load Game** save list (**ScrollRect**), the list scrolls **and** the **camera** zooms. **Expected:** UI consumes scroll; **camera** zoom ignores wheel events when the pointer is over interactive UI (same idea as **`GridManager`** gating clicks with **`IsPointerOverGameObject()`**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Load Game** list scroll does not trigger **camera** zoom.
2. Generalize if low-risk: other scrollable popups (e.g. **Building Selector**) behave consistently when backlog **Notes** alignment is desired.

### 2.2 Non-Goals (Out of Scope)

1. Redesigning **camera** zoom controls globally.
2. Changing **ScrollRect** styling.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want to scroll saves without the map zooming. | Wheel over **Load Game** list: list moves, zoom does not. |
| 2 | Player | I want zoom to still work when pointer is not over blocking UI. | Wheel over map: zoom unchanged from baseline. |

## 4. Current State

### 4.1 Domain behavior

**Observed:** Dual action — list scroll + zoom.  
**Expected:** **`EventSystem`** hit test determines whether **camera** handles scroll.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — BUG-19 |
| Code | `CameraController.cs` — **`HandleScrollZoom`** |
| UI | `UIManager.cs` — load game menu, **`savedGamesListContainer`** |
| Scene | **`MainScene.unity`** — **Load Game** panel / **Scroll View** |
| Spec | `.cursor/specs/ui-design-system.md` — foundations, input |

### 4.3 Implementation investigation notes (optional)

- Backlog proposes **`EventSystem.current.IsPointerOverGameObject()`** before zoom.
- **ScrollRect** child graphics need **raycast targets** enabled so hit test succeeds.

## 5. Proposed Design

### 5.1 Target behavior (product)

If the pointer is over UI that should consume scroll (at minimum the **Load Game** scroll area), **camera** zoom does not run for that wheel input.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. In **`HandleScrollZoom`**, early-out when **`EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()`** (optionally filter by layer/panel if false positives appear).
2. Add **`using UnityEngine.EventSystems`** if missing.
3. Verify **Load Game** **ScrollRect** hierarchy has raycastable graphic under pointer.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Follow backlog proposed solution | Low blast radius | Per-panel scroll consumer component |

## 7. Implementation Plan

### Phase 1 — Camera gate

- [ ] Implement **`IsPointerOverGameObject`** guard in **`CameraController.HandleScrollZoom`**.
- [ ] Compile; smoke test zoom on map vs over **Load Game** panel.

### Phase 2 — UI raycast verification

- [ ] In Editor, confirm scroll over list stops zoom; fix missing **raycastTarget** if needed.

## 8. Acceptance Criteria

- [ ] **Unity:** Mouse wheel over **Load Game** list scrolls list only (no zoom).
- [ ] **Unity:** Mouse wheel over world (no UI) still zooms as before.
- [ ] **English** comments / logs only; XML docs updated if public API touched.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. Should **all** scrollable **UI Toolkit** / uGUI panels block zoom globally, or only specific layers? If false positives block legitimate zoom, narrow the guard (e.g. tag or panel root) — **UX** decision, not simulation logic.
