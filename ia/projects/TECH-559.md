---
purpose: "TECH-559 — Runner params DTO + dispatch."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.1.1"
---
# TECH-559 — Runner params DTO + dispatch

> **Issue:** [TECH-559](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Extend AgentBridgeCommandRunner dispatch to deserialize request.params
for export_cell_chunk, export_sorting_debug, export_agent_context kinds.
New DTO structs carry bounded parameters per unity-development-context §10.

## 2. Goals and Non-Goals

### 2.1 Goals

1. BridgeCommand / DTO path parses params for export_cell_chunk (origin, width, height).
2. export_sorting_debug + export_agent_context accept optional seed_cell param.
3. Runner switch-dispatch routes parameterized requests to menu statics.
4. No new gridArray / cellArray reads — invariant #5 respected.

### 2.2 Non-Goals (Out of Scope)

1. MCP Zod schema changes (separate task TECH-560).
2. Menu method refactoring (separate task TECH-561).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want to pass bounded params when enqueuing bridge exports so that I get focused data slices | Params parsed and routed correctly |

## 4. Current State

### 4.1 Domain behavior

AgentBridgeCommandRunner dispatches bridge commands by kind but does not parse request.params. All exports run with hardcoded defaults or full-grid scope.

### 4.2 Systems map

- Assets/Scripts/Editor/AgentBridgeCommandRunner.cs — dispatch extension
- Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs — mutation helpers
- ia/specs/unity-development-context.md §10 — Reports + bridge artifacts

### 4.3 Implementation investigation notes (optional)

Runner already has switch-case by kind. Extension adds param deserialization before dispatch.

## 5. Proposed Design

### 5.1 Target behavior (product)

Bridge exports accept bounded params (cell chunk bounds, seed_cell) controlling output scope.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Define param structs per export kind. Deserialize from request JSON params field. Pass to menu static methods.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Param structs per kind | Typed deserialization; Zod alignment downstream | Generic dictionary |

## 7. Implementation Plan

### Phase 1 — DTO + dispatch

- [ ] Define param structs (ExportCellChunkParams, ExportSortingDebugParams, ExportAgentContextParams)
- [ ] Extend BridgeCommand deserialization to populate params from request JSON
- [ ] Route parameterized requests through runner switch to menu statics

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Params deserialized correctly | Unity compile | `npm run unity:compile-check` | C# edit |
| No invariant #5 violation | Agent review | Code review against invariants | Manual |

## 8. Acceptance Criteria

- [ ] BridgeCommand / DTO path parses params for export_cell_chunk (origin, width, height)
- [ ] export_sorting_debug + export_agent_context accept optional seed_cell param
- [ ] Runner switch-dispatch routes parameterized requests to menu statics
- [ ] No new gridArray / cellArray reads — invariant #5 respected

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: `export_cell_chunk` and `export_sorting_debug` are new bridge kinds — no existing runner case arms. Must add switch cases + `RunExport*` methods mirroring `RunExportAgentContext` pattern. Mitigation: follow existing dispatch shape exactly.
- Risk: Runner touches `request_json` deserialization — malformed params could NullRef downstream. Mitigation: defensive null checks before param access; return `failed` on parse error.
- Invariant #5: runner must NOT add new `gridArray`/`cellArray` reads. All cell access stays in menu static methods via `GridManager.GetCell`. Runner only deserializes params and delegates.
- Invariant #6: no new responsibilities added to `GridManager`. Param parsing stays in runner/DTO layer.
- Existing `export_agent_context` already parses `bridge_params.seed_cell` via `TryParseRequestEnvelope` — new param DTOs should follow same envelope pattern.

### §Examples

| Kind | params JSON | Expected behavior |
|------|------------|-------------------|
| `export_cell_chunk` | `{"origin_x": 5, "origin_y": 5, "width": 4, "height": 4}` | Routes to `InterchangeJsonReportsMenu` with bounded chunk |
| `export_cell_chunk` | `{}` (empty) | Uses defaults: origin (0,0), width 8, height 8 |
| `export_sorting_debug` | `{"seed_cell": "3,7"}` | Routes to `AgentDiagnosticsReportsMenu` with seed |
| `export_sorting_debug` | `{}` | Full grid sorting debug (existing behavior) |
| `export_agent_context` | `{"seed_cell": "10,10"}` | Already works — verify backward compat |
| `export_cell_chunk` | `{"origin_x": -1}` | Clamp to 0; no crash |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| runner_dispatches_cell_chunk | enqueue `export_cell_chunk` with params | `RunExportCellChunk` called | unity-batch or manual |
| runner_dispatches_sorting_debug | enqueue `export_sorting_debug` with seed_cell | `RunExportSortingDebug` called | unity-batch or manual |
| param_deserialization_null_safety | malformed request_json | `failed` + error string, no NullRef | unity-batch |
| backward_compat_agent_context | existing `export_agent_context` request | unchanged behavior | unity-batch |

### §Acceptance

- [ ] Runner switch has `case "export_cell_chunk"` and `case "export_sorting_debug"` arms
- [ ] Param structs (ExportCellChunkParams, ExportSortingDebugParams) defined with nullable/defaulted fields
- [ ] `RunExportCellChunk` delegates to `InterchangeJsonReportsMenu` with params
- [ ] `RunExportSortingDebug` delegates to `AgentDiagnosticsReportsMenu` with params
- [ ] No new `gridArray`/`cellArray` access in runner — invariant #5
- [ ] Existing `export_agent_context` with `seed_cell` still works unchanged

### §Findings

- `export_cell_chunk` and `export_sorting_debug` are brand new bridge kinds — runner has 0 case arms for them today. `InterchangeJsonReportsMenu.BuildCellChunkInterchangeJsonString(x0, y0, w, h)` already accepts bounds internally but is only called from `ExportCellChunkInterchange()` with hardcoded defaults.
- `AgentDiagnosticsReportsMenu.BuildSortingDebugMarkdownString()` is parameterless. Need to add seed_cell parameter support (or leave as full-grid default for v1).
- Existing `AgentBridgeRequestEnvelopeDto.bridge_params` carries `seed_cell` for `export_agent_context`. New param shapes may need a richer DTO or per-kind param classes.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
