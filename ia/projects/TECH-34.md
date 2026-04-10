---
purpose: "Project spec for TECH-34 — Generate gridmanager-regions.json from GridManager #region blocks."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-34 — Generate `gridmanager-regions.json` from GridManager `#region` blocks

> **Issue:** [TECH-34](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **28**. Supports **TECH-01** planning; optional **TECH-18** MCP `gridmanager_region_map`.

## 1. Summary

Implement a **deterministic** extractor that parses `GridManager.cs` **`#region` / `#endregion`** pairs and emits **`gridmanager-regions.json`** mapping region names to **line ranges** (1-based or 0-based — document in schema). Output path target: `tools/mcp-ia-server/data/gridmanager-regions.json` or `tools/data/` per repo layout; register in **TECH-18** if an MCP tool reads it.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Regenerate JSON via `npm run` or `node tools/extract-gridmanager-regions.mjs` after **GridManager** edits.
2. JSON includes `schema_version`, `source_file`, `regions: [{ name, lineStart, lineEnd }]`.
3. CI optional: fail if JSON stale vs `GridManager.cs` (hash check) — **Decision Log**.

### 2.2 Non-Goals (Out of Scope)

1. Refactoring **GridManager** (**TECH-01**).
2. Implementing MCP tool in this issue — note **Depends on** **TECH-18** for registration.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want agents to jump to **Sorting order** region quickly. | JSON line ranges match file. |
| 2 | Maintainer | I want regen in one command. | Documented script. |

## 4. Current State

### 4.1 Domain behavior

N/A.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Code | `Assets/Scripts/Managers/GameManagers/GridManager.cs` (confirm path) |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Line-based parser: track `#region Name` stack; handle nested regions per C# rules.
- Add `npm run generate:gridmanager-regions` at root or under `tools/`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | JSON for MCP | **TECH-18** consumption | Markdown only |

## 7. Implementation Plan

- [ ] Implement extractor + first generated file.
- [ ] Document regen in **TECH-34** backlog **Notes** and **TECH-18** phase.
- [ ] Optional: CI staleness check.

## 8. Acceptance Criteria

- [ ] Generated JSON matches manual spot-check of 3 regions.
- [ ] Script committed; path documented.
- [ ] **GridManager** `#region` edits have a clear “regen JSON” step in **TECH-01** discussion.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only.
