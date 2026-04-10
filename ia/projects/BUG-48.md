---
purpose: "Project spec for BUG-48 — Minimap staleness + RebuildTexture measurement acceptance."
audience: both
loaded_by: ondemand
slices_via: none
---
# BUG-48 — Minimap staleness + RebuildTexture measurement acceptance

> **Issue:** [BUG-48](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **8** (cost metric for throttle vs full rebuild design).

## 1. Summary

**Player-visible:** The procedural **minimap** does not refresh as the city changes until the player toggles a **minimap** layer or other actions call **`RebuildTexture`**. **Expected:** The minimap tracks **zones**, **streets**, **open water**, **forests**, etc. without requiring layer toggles — via **simulation tick** refresh, **throttled** rebuild, **dirty** / incremental update, or **event-driven** invalidation. **Tooling:** Before choosing strategy, capture **`RebuildTexture`** cost (ms, texture dimensions, optional GC) as JSON under `tools/reports/` per roadmap task **8**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Functional:** Minimap updates when underlying grid/**zone**/**street**/**water** data changes while visible (exact strategy agent/product choice).
2. **Performance:** No sustained frame-time regression; use measurement artifact to justify throttle vs per-tick full rebuild.
3. **Measurement (can land first):** One-shot or throttled profiler hook documented in **§7**.

### 2.2 Non-Goals (Out of Scope)

1. **FEAT-42** optional **HeightMap** layer (separate issue).
2. Redesigning entire **minimap** UX.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I see the minimap match the city without toggling layers. | Repro steps pass on build. |
| 2 | Developer | I know **RebuildTexture** cost before picking cadence. | JSON report checked in or documented run. |

## 4. Current State

### 4.1 Domain behavior

**Observed:** Stale minimap until layer toggle. **Expected:** Live consistency with **simulation** / grid mutations.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Code | `MiniMapController.cs` — `RebuildTexture`, `Update` |
| Wiring | `TimeManager` / `SimulationManager` for **simulation tick** hooks |

### 4.3 Implementation investigation notes (optional)

- Profile **RebuildTexture** on reference save / city size; attach to **TECH-15**/**TECH-16** report style if shared JSON schema adopted.

## 5. Proposed Design

### 5.1 Target behavior (product)

Refresh policy documented (per tick vs throttled vs event); matches **BACKLOG** **Expected** paragraph.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Subscribe to grid change events or poll dirty flag from **simulation tick**.
- Avoid full rebuild every frame if metrics require — use **Decision Log** to record chosen threshold.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Backlog integration | — |

## 7. Implementation Plan

### Phase 0 — Measurement (task 8)

- [ ] Editor or Play Mode one-shot: log ms + width/height of minimap texture to `tools/reports/minimap-rebuild-{timestamp}.json`.

### Phase 1 — Wire refresh

- [ ] Implement invalidation + rebuild policy.
- [ ] Verify with **simulation** running and minimap open.

### Phase 2 — Performance pass

- [ ] Compare before/after **Profiler** or harness JSON.

## 8. Acceptance Criteria

- [ ] **Unity:** Repro from **BACKLOG** no longer reproduces after fix.
- [ ] **Performance:** Documented evidence (task **8** JSON or profiler screenshot summary) supports chosen cadence.
- [ ] No new per-frame **`FindObjectOfType`** (**invariants**).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

Refresh cadence (every **simulation tick** vs throttled) is **product/performance** trade-off — document chosen rule in **Decision Log** after **Phase 0** metrics; if it changes **player-visible** timing vs backlog **Expected**, confirm with product owner.
