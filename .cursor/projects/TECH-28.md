# TECH-28 — Unity Editor agent diagnostics (context JSON + sorting debug)

> **Issue:** [TECH-28](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — tasks **2**, **23**. Supports **TECH-20** / **BUG-16**–**BUG-17** debugging narratives.

## 1. Summary

Add **Unity Editor** utilities that write **machine-readable** artifacts for Cursor agents: (1) **`agent-context-{timestamp}.json`** with `schema_version`, active scene, selection, and a **small sample** of grid state (e.g. focused **cell** coordinates, **height**, **water** flags) without dumping the full grid; (2) optional **`sorting-debug.md`** for selected cells documenting inputs relevant to **Sorting order** (see **isometric-geography-system** §7) — formula-related fields as exposed by existing code, not re-deriving spec math in the exporter.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Editor menu under agreed path (e.g. **Territory Developer → Reports**).
2. Writes only under **`tools/reports/`** or user-approved folder; **never** silent writes in player builds.
3. Truncation limits documented (max cells, max string length).

### 2.2 Non-Goals (Out of Scope)

1. Replacing Unity Profiler or scene serialization.
2. Auto-uploading data off-machine.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want one click to snapshot context for an agent. | JSON file appears with required fields. |
| 2 | Developer | I want sorting investigations without hand-copying numbers. | Optional markdown lists per-cell debug fields. |

## 4. Current State

### 4.1 Domain behavior

Exports must use **glossary** terms in keys/labels (**cell**, **HeightMap**, **Sorting order**, **water map**, etc.).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Spec | `.cursor/specs/isometric-geography-system.md` §7 |
| Unity | `GridManager`, selection / debug tooling if present |

## 5. Proposed Design

### 5.1 Target behavior (product)

No change when menu not invoked.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- `Editor` assembly script; use `JsonUtility` or `Newtonsoft` if already in project — Decision Log.
- **sorting-debug:** call into existing **GridSortingOrderService** or debug hooks if available; otherwise document “not available until …” in Phase 1.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Editor-only | Safety | Runtime menu |

## 7. Implementation Plan

### Phase 1 — Context JSON

- [ ] Menu item + JSON v1 schema.
- [ ] `.gitignore` entry for `tools/reports/*.json` if policy is local-only — or commit samples — **Decision Log**.

### Phase 2 — Sorting debug export

- [ ] Implement for Moore neighborhood or selection only.

## 8. Acceptance Criteria

- [ ] **Unity:** Export runs in Edit Mode / Play Mode without exception on default scene.
- [ ] Output includes `schema_version` and UTC timestamp.
- [ ] Document in **TECH-28** backlog **Notes** or this spec how agents should reference files in prompts.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only.
