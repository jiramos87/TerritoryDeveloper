---
purpose: "TECH-562 — Menu regression pass."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.1.4"
---
# TECH-562 — Menu regression pass

> **Issue:** [TECH-562](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Verify all Territory Developer → Reports menu items still work after
parameterized refactor. No duplicate file writes. TryPersistReport
paths unchanged for Postgres registry exports.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Every MenuItem under Reports menu produces expected output.
2. No duplicate file writes from bridge vs menu paths.
3. TryPersistReport paths + Postgres registry export unchanged.

### 2.2 Non-Goals (Out of Scope)

1. New export functionality (covered by TECH-559, TECH-561).
2. MCP-side testing (covered by TECH-564).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want Reports menu items to work unchanged after refactor so that manual diagnostics are unbroken | All menu items succeed |

## 4. Current State

### 4.1 Domain behavior

Reports menu items invoke Export methods that persist artifacts via TryPersistReport + Postgres registry. Current paths work. Refactor in TECH-561 may break if overloads misconfigured.

### 4.2 Systems map

- Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs — Reports menu
- Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs — Reports menu
- Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs — registry

## 5. Proposed Design

### 5.1 Target behavior (product)

All Reports menu items produce same output as before parameterized refactor. No regressions.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Systematic invocation of each MenuItem. Compare output paths and Postgres rows.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Separate regression task | Isolates verification from implementation | Inline in TECH-561 |

## 7. Implementation Plan

### Phase 1 — Regression verification

- [ ] Enumerate all Reports menu items; confirm each invocation succeeds
- [ ] Check file output paths for duplicates
- [ ] Verify Postgres registry export rows match pre-refactor baseline

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Menu items work | Manual / bridge | Unity Editor Reports menu | Play Mode or Edit Mode |
| Registry unchanged | Manual | Postgres editor_export_* rows | Compare counts |

## 8. Acceptance Criteria

- [ ] Every MenuItem under Reports menu produces expected output
- [ ] No duplicate file writes from bridge vs menu paths
- [ ] TryPersistReport paths + Postgres registry export unchanged

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | No regressions found | Clean overload pattern from TECH-561 | 0 code diff; verification-only deliverable |

## 10. Lessons Learned

- ForAgentBridge + outcome struct pattern keeps MenuItem wrappers thin and regression-safe.

## §Plan Author

### §Audit Notes

- Risk: Regression pass depends on Play Mode + Postgres availability. If dev machine lacks `DATABASE_URL`, `TryPersistReport` returns false but menu items don't crash — just skip DB check. Mitigation: document both paths (DB available / unavailable).
- Risk: File output paths may have changed if TECH-561 alters baseName generation. Mitigation: compare file output paths pre/post refactor.
- Low-risk task overall — primarily verification, not code change. May generate 0 code diff if all menu items pass.

### §Examples

| Menu item | Expected output |
|-----------|----------------|
| Reports → Export Agent Context | `agent-context-*.json` in `tools/reports/` + Postgres `editor_export_agent_context` row |
| Reports → Export Sorting Debug (Markdown) | sorting debug markdown in Postgres `editor_export_sorting_debug` row |
| Reports → Export CityCell Chunk (Interchange) | `cell-chunk-interchange-*.json` + Postgres `editor_export_terrain_cell_chunk` row |
| Reports → Export World Snapshot (Dev Interchange) | Postgres `editor_export_world_snapshot_dev` row |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| all_menu_items_run | click each Reports menu item in Play Mode | no errors in Console | manual |
| no_duplicate_files | compare file counts in `tools/reports/` before/after | no new unexpected files | manual |
| postgres_rows_match | query `editor_export_*` tables | row counts match pre-refactor | manual |
| edit_mode_items_safe | click Edit Mode items without Play Mode | dialog or no-op, no crash | manual |

### §Acceptance

- [ ] All Reports menu items produce expected output without errors
- [ ] No duplicate file writes from bridge vs menu paths
- [ ] `TryPersistReport` Postgres rows unchanged for registry exports
- [ ] Edit Mode menu items gracefully handle no-grid state

### §Findings

- Task is verification-only — likely produces 0 code diff unless regressions found. May produce a short report or checklist as deliverable instead of code changes.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
