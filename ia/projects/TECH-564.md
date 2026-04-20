---
purpose: "TECH-564 — Bridge response contract tests."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.1.6"
---
# TECH-564 — Bridge response contract tests

> **Issue:** [TECH-564](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Add EditMode or MCP-side tests asserting completed / failed response
shapes for export_cell_chunk + export_sorting_debug when grid absent.
Snapshot keys only — not full JSON body comparison.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Test asserts completed response shape has expected top-level keys.
2. Test asserts failed response shape has error field when grid uninitialized.
3. Key-only snapshots (no full body) for stability across content changes.

### 2.2 Non-Goals (Out of Scope)

1. Full JSON body assertions (brittle across content changes).
2. Play Mode integration tests (manual verify in TECH-562).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want contract tests so that bridge response shape regressions are caught early | Tests fail on shape break |

## 4. Current State

### 4.1 Domain behavior

No automated tests assert bridge response shapes. Regressions discovered manually.

### 4.2 Systems map

- Assets/Tests/EditMode/ — Unity EditMode test surface
- tools/mcp-ia-server/tests/ — MCP-side test surface
- ia/specs/unity-development-context.md §10 — artifact table

## 5. Proposed Design

### 5.1 Target behavior (product)

Automated tests catch response shape regressions for export kinds. Tests run in EditMode (no Play Mode dependency) or MCP-side Node.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

EditMode tests mock or stub grid state. Assert top-level keys of completed / failed responses. MCP-side tests if tooling coverage gaps exist.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Key-only snapshots | Stable across content changes; catches shape breaks | Full body snapshot |

## 7. Implementation Plan

### Phase 1 — Contract tests

- [ ] Add EditMode test: enqueue export_cell_chunk → assert completed keys
- [ ] Add EditMode test: export when grid absent → assert failed + error key
- [ ] Add MCP-side test if tooling coverage needed

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tests pass | Unity compile | `npm run unity:compile-check` | EditMode tests |
| MCP tests pass | Node | `npm run validate:all` | If MCP-side tests added |

## 8. Acceptance Criteria

- [ ] Test asserts completed response shape has expected top-level keys
- [ ] Test asserts failed response shape has error field when grid uninitialized
- [ ] Key-only snapshots (no full body) for stability across content changes

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: EditMode tests for bridge responses require mocking or stubbing `GridManager` initialization state. Unity EditMode test framework allows `FindObjectOfType` but grid won't be initialized. Mitigation: test the response shape assertion, not the full export pipeline — mock or skip grid dependency.
- Risk: MCP-side tests may need to mock Postgres `agent_bridge_job` table. If tests run without DB, use fixture JSON. Mitigation: align with existing test patterns in `tools/mcp-ia-server/tests/`.
- Key-only snapshot approach avoids brittleness: assert `completed.document` has keys `artifact`, `schema_version`, `origin_x` etc. — not values.

### §Examples

| Response type | Top-level keys expected |
|--------------|------------------------|
| `completed` (export_cell_chunk) | `status: "completed"`, `document: {artifact, schema_version, origin_x, origin_y, width, height, cells}` |
| `failed` (grid absent) | `status: "failed"`, `error: <string>` |
| `completed` (export_sorting_debug) | `status: "completed"`, `document: <markdown string>` |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| completed_cell_chunk_shape | fixture completed response JSON | has keys: status, document.artifact, document.schema_version | node |
| failed_grid_absent_shape | fixture failed response JSON | has keys: status, error; error is non-empty string | node |
| completed_sorting_debug_shape | fixture completed response JSON | has keys: status, document (string) | node |
| mcp_tool_handler_error_path | unity_bridge_command with mock failure | tool returns error structure | node |

### §Acceptance

- [ ] At least 2 tests asserting `completed` response key shapes
- [ ] At least 1 test asserting `failed` response shape with `error` field
- [ ] Tests are key-only (no full JSON body comparison)
- [ ] Tests pass in `npm run validate:all`

### §Findings

- Existing `tools/mcp-ia-server/tests/` may already have bridge-related test patterns to follow. Need to check before inventing new test infrastructure.
- EditMode tests under `Assets/Tests/EditMode/` would require Unity test runner. MCP-side Node tests are simpler for response shape assertions if fixture JSON is available.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
