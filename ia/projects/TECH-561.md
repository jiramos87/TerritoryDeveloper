---
purpose: "TECH-561 — Menu parameterized entry points."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.1.3"
---
# TECH-561 — Menu parameterized entry points

> **Issue:** [TECH-561](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Refactor AgentDiagnosticsReportsMenu and InterchangeJsonReportsMenu so
bridge runner calls Export* methods with explicit parameter structs.
Existing MenuItem paths remain as zero-param defaults.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Export methods accept parameter structs (chunk bounds, seed_cell).
2. MenuItem wrappers call same methods with default (null/empty) params.
3. Bridge runner routes parameterized requests through these entry points.

### 2.2 Non-Goals (Out of Scope)

1. Runner dispatch changes (separate task TECH-559).
2. New export kinds or sugar tools (Stage 1.2 scope).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want parameterized Export methods so that bridge and menu paths share logic | No duplicate export bodies |

## 4. Current State

### 4.1 Domain behavior

Menu Export methods are parameterless. Bridge calls route through MenuItem-equivalent paths with hardcoded defaults.

### 4.2 Systems map

- Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs — sorting + agent context exports
- Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs — cell chunk + world snapshot exports
- ia/specs/unity-development-context.md §10 — artifact table

## 5. Proposed Design

### 5.1 Target behavior (product)

Export methods accept explicit parameter structs. Menu items call with defaults. Bridge calls pass params from DTO.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Add parameter-accepting overloads. Existing MenuItem wrappers delegate with null/default params.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Overload pattern | Backward-compatible; no MenuItem behavior change | Replace methods entirely |

## 7. Implementation Plan

### Phase 1 — Parameterized entry points

- [ ] Add parameter-accepting overloads to Export methods
- [ ] Wire MenuItem wrappers to overloads with default params
- [ ] Confirm bridge runner dispatch reaches parameterized path

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Overloads compile | Unity compile | `npm run unity:compile-check` | C# edit |
| MenuItem behavior preserved | Manual | Reports menu items | Regression |

## 8. Acceptance Criteria

- [ ] Export methods accept parameter structs (chunk bounds, seed_cell)
- [ ] MenuItem wrappers call same methods with default (null/empty) params
- [ ] Bridge runner routes parameterized requests through these entry points

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: Existing `MenuItem` methods (`ExportAgentContext`, `ExportSortingDebug`, `ExportCellChunkInterchange`) are public static parameterless. Adding overloads preserves ABI but callers must be checked. Mitigation: existing menu items call zero-param version; bridge calls parameterized overload.
- Risk: `InterchangeJsonReportsMenu.BuildCellChunkInterchangeJsonString` already accepts `(x0, y0, w, h)` — method exists but is private. May need to promote to internal or add a public wrapper with param struct. Mitigation: add `ExportCellChunkForAgentBridge(params)` mirroring `ExportAgentContextForAgentBridge` pattern.
- Invariant #5: menu methods use `GridManager.GetCell` exclusively (confirmed in class XML docs). Adding param struct doesn't change cell access pattern.
- `AgentDiagnosticsReportsMenu.ExportAgentContextForAgentBridge` already exists as the bridge-callable overload with `overrideSeedX/Y` params — same pattern applies to sorting debug.

### §Examples

| Method | Before (menu) | After (bridge) |
|--------|--------------|----------------|
| `ExportCellChunkInterchange()` | hardcoded (0,0,8,8) | `ExportCellChunkForAgentBridge(origin_x, origin_y, w, h)` |
| `ExportSortingDebug()` | full grid | `ExportSortingDebugForAgentBridge(seed_cell?)` |
| `ExportAgentContext()` | no seed | `ExportAgentContextForAgentBridge(seedX?, seedY?)` (already exists) |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| menu_item_still_works | click Reports → Export CityCell Chunk | Postgres row created (Play Mode) | manual |
| bridge_overload_accepts_params | call `ExportCellChunkForAgentBridge(5, 5, 4, 4)` | bounded chunk exported | unity-batch |
| default_params_match_menu | call `ExportCellChunkForAgentBridge(0, 0, 8, 8)` | same as MenuItem invocation | unity-batch |
| sorting_debug_with_seed | call `ExportSortingDebugForAgentBridge("3,7")` | seed_cell scoped output | unity-batch |

### §Acceptance

- [ ] `ExportCellChunkForAgentBridge(origin_x, origin_y, w, h)` method exists on `InterchangeJsonReportsMenu`
- [ ] `ExportSortingDebugForAgentBridge(seed_cell?)` method exists on `AgentDiagnosticsReportsMenu`
- [ ] Existing `MenuItem` methods call new overloads with default params
- [ ] No new `gridArray`/`cellArray` reads — invariant #5
- [ ] Runner dispatch (TECH-559) can call these entry points

### §Findings

- `ExportAgentContextForAgentBridge` already follows the `ForAgentBridge` naming pattern with typed outcome struct `AgentBridgeAgentContextOutcome`. New methods should follow same naming + return an outcome struct for bridge completion.
- `BuildCellChunkInterchangeJsonString` is private static — needs to stay callable from new public overload without exposing internals.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
