---
purpose: "TECH-589 — ide-bridge-evidence diff."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.3.3"
phases:
  - "Read SKILL + §10 + MCP catalog"
  - "Patch SKILL or no-change note"
---
# TECH-589 — ide-bridge-evidence diff

> **Issue:** [TECH-589](../../BACKLOG.md)
> **Status:** Done
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Ensure ide-bridge-evidence skill text matches shipped bridge responses and MCP tool names after
Stage 1.1–1.2 parameter work.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Read ide-bridge-evidence SKILL end-to-end.
2. If export kinds or response DTOs changed, update skill prose and examples.
3. If no delta, capture explicit no DTO change note for audit trail.

### 2.2 Non-Goals (Out of Scope)

1. Changing Unity Editor bridge implementation (prior stages own C#).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want ide-bridge-evidence to name current tools so that I enqueue correct MCP calls | SKILL matches registerTool + §10 |

## 4. Current State

### 4.1 Domain behavior

Stage 1.1 added parameterized export kinds; SKILL may predate param shapes.

### 4.2 Systems map

- ia/skills/ide-bridge-evidence/SKILL.md
- docs/mcp-ia-server.md — tool catalog
- ia/specs/unity-development-context.md §10

## 5. Proposed Design

### 5.1 Target behavior (product)

Skill examples and tool list match production MCP + Unity DTOs.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Diff-only edit; no runtime code.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Diff-only | Stage 1.3 scope | Rewrite bridge |

## 7. Implementation Plan

### Phase 1 — Diff pass

- [x] Compare SKILL tool names vs MCP registerTool + §10 table
- [x] Edit SKILL or add no-change sentence to §Findings / report
- [x] npm run validate:all if MCP descriptors touched

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc drift | Manual | Diff SKILL vs mcp-ia-server | |
| validate:all | Node | `npm run validate:all` | If descriptors touched |

## 8. Acceptance Criteria

- [x] ide-bridge-evidence SKILL read end-to-end against MCP + §10
- [x] If export kinds or response DTOs changed, skill prose and examples updated
- [x] If no delta, explicit no DTO change note captured for audit trail

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- Documented **`unity_export_cell_chunk`** / **`unity_export_sorting_debug`** in **`ide-bridge-evidence`**; bridge job response envelope unchanged — no DTO migration.

## §Plan Author

### §Audit Notes

- Risk: Parameterized `export_*` kinds from Stage 1.1 may not match SKILL examples — scan for hardcoded pre-param strings.
- `debug_context_bundle` response shape: align with `unity-development-context` §10 JSON contract table if examples cite fields.
- If zero DTO delta: single explicit sentence in §Findings or §Verification — satisfies audit trail without noisy edits.

### §Examples

| SKILL section | Check against | Pass |
|---------------|---------------|------|
| Tool list | `docs/mcp-ia-server.md` bridge tools | names match |
| `unity_bridge_command` | Zod / descriptor | kind enum matches |
| Response examples | §10 artifact rows | field names stable |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| skill_diff | TECH-559 merge base vs HEAD | line diff or no-op note | manual |
| mcp_parity | grep `registerTool` in mcp-ia-server | kinds referenced in SKILL | manual |

### §Acceptance

- [ ] Either SKILL updated for drift OR explicit “no bridge DTO change” line in §Findings
- [ ] No stale `kind` strings left in examples

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
