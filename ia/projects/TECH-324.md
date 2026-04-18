---
purpose: "TECH-324 — Implement backlog_record_validate MCP tool."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-324 — Implement `backlog_record_validate` MCP tool

> **Issue:** [TECH-324](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Ship `backlog_record_validate` MCP tool (IP5). Agents call it before disk-writing a yaml backlog record to catch schema defects pre-commit. Depends on TECH-323 shared lint core. Second task of Stage 1.2 Phase 1.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` implementing tool handler.
2. Input schema: `{ yaml_body: string }`.
3. Output: `{ ok: boolean, errors: string[], warnings: string[] }` — exact shape from shared core.
4. Register in `tools/mcp-ia-server/src/index.ts` tool registry w/ descriptor matching `mcp__territory-ia__*` convention.
5. Tool descriptor prose (caveman) — single sentence + input/output schemas.

### 2.2 Non-Goals

1. Write yaml to disk — separate tool (`backlog_record_create`, IP6 / Stage 2.3).
2. Reserve ids — `reserve_backlog_ids` (TECH-326).
3. Cross-record checks — IP8.
4. Fixture tests — TECH-325.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Filing agent (stage-file / project-new) | As MCP client, want pre-write lint so bad yaml never hits disk. | Tool returns structured errors; skill aborts filing on `ok: false`. |

## 4. Current State

### 4.1 Domain behavior

No MCP surface for yaml validation today. Agents hand-author yaml + rely on end-of-flow `validate:all` — errors caught late.

### 4.2 Systems map

- TECH-323 shared core — direct dependency.
- `tools/mcp-ia-server/src/index.ts` — tool registry.
- `tools/mcp-ia-server/src/tools/*.ts` — pattern reference (e.g. `backlog-issue.ts`).

## 5. Proposed Design

### 5.2 Architecture

- Handler: `async function backlogRecordValidate({ yaml_body }) → { ok, errors, warnings }`.
- Delegates straight to `validateBacklogRecord` from TECH-323.
- Input-schema guard: non-empty `yaml_body` string (tool-level error if empty).
- Registered under tool id `backlog_record_validate`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Tool accepts raw yaml body (not path) | Callers author in-memory before disk write | Path-based — rejected, defeats pre-write use case |

## 7. Implementation Plan

### Phase 1 — Tool + registry wire

- [ ] Author `backlog-record-validate.ts` handler.
- [ ] Register in tool registry w/ caveman descriptor.
- [ ] Verify MCP server boots clean (`npm run test:ia` or equivalent).
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tool handler + registry | Node | `npm run validate:all` | Covers MCP server boot + registry drift |
| Fixture coverage | Follow-up | TECH-325 | Dedicated test file |

## 8. Acceptance Criteria

- [ ] Tool file authored + registered.
- [ ] Input/output schemas match design.
- [ ] MCP server boots; schema cache regen clean.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only.
