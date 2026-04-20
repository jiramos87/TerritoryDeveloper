---
purpose: "TECH-560 — MCP Zod alignment for new params."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.1.2"
---
# TECH-560 — MCP Zod alignment for new params

> **Issue:** [TECH-560](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Align MCP unity_bridge_command Zod request schema with new Unity DTO
param shapes (export_cell_chunk, export_sorting_debug, export_agent_context).
Add fixture or unit test proving param round-trip fidelity.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Zod request schema accepts params matching Unity DTO structs.
2. Fixture or unit test validates param round-trip (enqueue → dequeue shape match).
3. Existing non-parameterized kinds still pass Zod validation.

### 2.2 Non-Goals (Out of Scope)

1. Unity-side DTO changes (separate task TECH-559).
2. Sugar tool registration (Stage 1.2 scope).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want Zod to validate my bridge params so that invalid requests fail fast | Zod rejects malformed params |

## 4. Current State

### 4.1 Domain behavior

unity_bridge_command Zod schema does not validate export-specific params sub-object. Params pass through unvalidated.

### 4.2 Systems map

- tools/mcp-ia-server/src/ — registerTool, Zod schemas
- tools/mcp-ia-server/tests/ — test surface
- ia/specs/unity-development-context.md §10 — artifact table

## 5. Proposed Design

### 5.1 Target behavior (product)

Zod schema validates params per export kind. Invalid params rejected at enqueue time with clear error.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Extend request Zod with discriminated union or optional params sub-object keyed by kind.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Optional params sub-object | Backward-compatible; kinds without params pass unchanged | Discriminated union per kind |

## 7. Implementation Plan

### Phase 1 — Zod schema + tests

- [ ] Extend unity_bridge_command request Zod with optional params sub-object
- [ ] Add fixture covering each new kind + param shape
- [ ] Confirm existing non-parameterized kinds pass unchanged

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Zod validates params | Node | `npm run validate:all` | MCP tests |
| Round-trip fidelity | Node | test fixture | Enqueue/dequeue shape match |

## 8. Acceptance Criteria

- [ ] Zod request schema accepts params matching Unity DTO structs
- [ ] Fixture or unit test validates param round-trip
- [ ] Existing non-parameterized kinds still pass Zod validation

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: Zod schema change must be backward-compatible — existing callers passing no params for known kinds must still validate. Mitigation: params field optional (`.optional()`) with per-kind refinement.
- Risk: MCP server caches schema at startup. After Zod changes, MCP server must be restarted for tests to pick up new validation. Mitigation: document restart requirement in spec.
- Existing `unity_bridge_command` Zod likely validates `kind` + `request` body but may not have params sub-object at all. Need to read current schema shape before extending.

### §Examples

| kind | params | Zod result |
|------|--------|------------|
| `export_cell_chunk` | `{"origin_x": 0, "origin_y": 0, "width": 8, "height": 8}` | PASS |
| `export_cell_chunk` | `null` / absent | PASS (defaults apply) |
| `export_sorting_debug` | `{"seed_cell": "3,7"}` | PASS |
| `export_sorting_debug` | `{"seed_cell": 999}` | FAIL (string expected) |
| `get_console_logs` | `{"severity_filter": "error"}` | PASS (existing kind, no params change) |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| zod_accepts_cell_chunk_params | `{kind: "export_cell_chunk", params: {origin_x: 0, ...}}` | schema.parse succeeds | node |
| zod_accepts_empty_params | `{kind: "export_cell_chunk"}` | schema.parse succeeds (params optional) | node |
| zod_rejects_bad_seed_cell | `{kind: "export_sorting_debug", params: {seed_cell: 123}}` | schema.parse throws | node |
| existing_kinds_unchanged | `{kind: "get_console_logs", ...}` | schema.parse succeeds | node |

### §Acceptance

- [ ] Zod request schema extends with optional `params` sub-object
- [ ] Per-kind param shapes validated (origin_x/y = number, seed_cell = string)
- [ ] Existing kinds without params still pass validation
- [ ] At least one fixture test per new kind + param shape
- [ ] `npm run validate:all` green

### §Findings

- Need to read current `unity_bridge_command` tool definition in `tools/mcp-ia-server/src/` to understand existing Zod shape before extending. The `request` body's inner structure may vary by kind.
- Fixture round-trip test should verify enqueue → dequeue shape fidelity, not just Zod parse.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
