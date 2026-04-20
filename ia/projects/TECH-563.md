---
purpose: "TECH-563 — Play Mode + grid gate errors."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.1.5"
---
# TECH-563 — Play Mode + grid gate errors

> **Issue:** [TECH-563](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Add precondition gates in bridge export path: GridManager.isInitialized
and TerrainManager readiness checks before Play-only exports. Return
failed status + human-readable error string when preconditions not met.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Play-only export kinds check GridManager.isInitialized before execution.
2. TerrainManager readiness verified where documented needs exist.
3. Failed response includes structured error field (human-readable string).
4. No new gridArray / cellArray reads — invariant #5 respected.

### 2.2 Non-Goals (Out of Scope)

1. Full error taxonomy beyond bridge preconditions.
2. Retry logic on caller side (MCP sugar tool scope — Stage 1.2).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want clear failed responses when grid not ready so that I can diagnose without guessing | Failed + error string returned |

## 4. Current State

### 4.1 Domain behavior

Bridge exports may NullRef or produce empty artifacts when Play Mode not active or grid uninitialized. No structured error path.

### 4.2 Systems map

- Assets/Scripts/Editor/AgentBridgeCommandRunner.cs — precondition gate
- Assets/Scripts/Managers/GameManagers/GridManager.cs — isInitialized check
- docs/unity-ide-agent-bridge-analysis.md §8.3 — risk table

## 5. Proposed Design

### 5.1 Target behavior (product)

Bridge exports return failed + human-readable error when Play Mode or grid preconditions fail. Agents parse error field for diagnostic messages.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Precondition check before dispatch in runner. Return failed JSON with error field. Align error strings with analysis §8.3 categories.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Precondition in runner | Central gate; all kinds benefit | Per-kind check in menu methods |

## 7. Implementation Plan

### Phase 1 — Precondition gates

- [ ] Add isInitialized / TerrainManager readiness check before Play-only dispatch
- [ ] Return failed + error JSON on precondition failure
- [ ] Align error strings with analysis §8.3 risk categories

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Precondition gate works | Unity compile | `npm run unity:compile-check` | C# edit |
| Failed shape correct | EditMode test / MCP | TECH-564 contract tests | Cross-task |

## 8. Acceptance Criteria

- [ ] Play-only export kinds check GridManager.isInitialized before execution
- [ ] TerrainManager readiness verified where documented needs exist
- [ ] Failed response includes structured error field (human-readable string)
- [ ] No new gridArray / cellArray reads — invariant #5 respected

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: `GridManager.isInitialized` may be false during Edit Mode or before geography init completes in Play Mode. Bridge exports that require Play Mode + grid must check both states. Mitigation: explicit two-part gate (Play Mode + isInitialized).
- Risk: `TerrainManager` readiness check — `TerrainManager` is accessed via `GridManager.terrainManager` (Inspector wired). If null, export should fail gracefully, not NullRef. Mitigation: null check before terrain access.
- Invariant #5: precondition checks use `GridManager.isInitialized` (a boolean property) — no `gridArray`/`cellArray` access. Safe.
- Invariant #3: precondition check runs in runner dispatch (Editor update path) — no per-frame hot-loop concern since bridge commands are dequeued at `PollEveryNFrames = 30`.
- Analysis §8.3 risk table documents: "grid not ready" as top bridge failure mode. Error strings should map to these risk categories.

### §Examples

| Scenario | kind | Grid state | Expected |
|----------|------|-----------|----------|
| Edit Mode, no grid | `export_cell_chunk` | isInitialized = false | `failed` + `"Grid not initialized — enter Play Mode first"` |
| Play Mode, early (pre-init) | `export_sorting_debug` | isInitialized = false | `failed` + `"Grid not initialized — wait for geography init"` |
| Play Mode, grid ready | `export_cell_chunk` | isInitialized = true | normal dispatch to menu static |
| Play Mode, TerrainManager null | `export_sorting_debug` | isInitialized = true, terrainManager = null | `failed` + `"TerrainManager not available"` |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| gate_rejects_edit_mode | enqueue `export_cell_chunk` in Edit Mode | `failed` + error string | bridge |
| gate_rejects_uninit_grid | enqueue `export_sorting_debug` pre-init | `failed` + error string | bridge |
| gate_passes_initialized | enqueue `export_cell_chunk` with grid ready | `completed` | bridge |
| error_field_human_readable | any failed response | `error` field is non-empty string | bridge |

### §Acceptance

- [ ] Play-only export kinds return `failed` when `GridManager.isInitialized` is false
- [ ] `TerrainManager` null check returns `failed` with clear error
- [ ] Error field is human-readable string (not stack trace)
- [ ] No new `gridArray`/`cellArray` reads — invariant #5
- [ ] Error strings align with analysis §8.3 risk categories

### §Findings

- Runner already has `EnterPlayModeGridWaitMaxSeconds = 24.0` constant and grid-wait logic for `enter_play_mode` kind. Precondition gate should reuse or reference this pattern rather than inventing a new wait loop.
- `InterchangeJsonReportsMenu.ExportCellChunkInterchange()` already checks `EditorApplication.isPlaying` and shows dialog on failure. Bridge path needs equivalent check returning `failed` JSON instead of dialog.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
