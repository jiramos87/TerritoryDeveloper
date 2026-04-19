---
purpose: "TECH-362 — spec_stage_table MCP slice tool migration for ship-stage parser."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-362 — spec_stage_table MCP slice tool migration for ship-stage parser

> **Issue:** [TECH-362](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Migrate the narrow regex v1 parser in `ship-stage` SKILL.md Phase 0 to a dedicated `spec_stage_table` MCP slice tool. Depends on the MCP lifecycle audit (mcp-lifecycle-tools-opus-4-7-audit) landing first. Filed as follow-up from TECH-322.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `spec_stage_table` MCP tool returns structured `{task_id, status, phase, intent}` rows for a named `{MASTER_PLAN_PATH, STAGE_ID}`.
2. `ship-stage` SKILL.md Phase 0 updated to call the MCP tool instead of inline regex.
3. Regex fallback removed; fail-loud behavior preserved (schema drift → structured error from MCP tool).

### 2.2 Non-Goals

1. Changes to ship-stage chain logic — only parser layer.
2. New MCP server deployment — piggybacks on existing `territory-ia` server.

## 3. Open Questions

- MCP lifecycle audit must ship before this issue starts — monitor `depends_on`.

## 7. Implementation Plan

_Stub — populate after MCP lifecycle audit ships._

- [ ] Design `spec_stage_table` MCP tool schema.
- [ ] Implement tool in `tools/mcp-ia-server/src/tools/`.
- [ ] Update `ship-stage` SKILL.md Phase 0 to use new tool.
- [ ] Remove inline regex parser.
- [ ] Add test fixture.
- [ ] `npm run validate:all` clean.

## 8. Acceptance Criteria

- [ ] `spec_stage_table` MCP tool present + callable.
- [ ] `ship-stage` Phase 0 uses MCP tool, not inline regex.
- [ ] `npm run validate:all` clean.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …
