---
purpose: "TECH-438 — Extend `backlog_list` inputs w/ locator filters (`parent_plan` / `stage` / `task_key`)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T4.2.1"
---
# TECH-438 — Extend `backlog_list` inputs w/ locator filters

> **Issue:** [TECH-438](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-21

## 1. Summary

Add three optional inputs (`parent_plan?`, `stage?`, `task_key?`) to `backlog_list` MCP tool. Lowercase substring compare per N3; applied after existing filters. Satisfies Stage 4.2 Phase 1 exit of `backlog-yaml-mcp-alignment-master-plan.md`. Schema-v2 yaml (Step 3) already carries these fields on `ParsedBacklogIssue` — this task wires them into the `backlog_list` query surface.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `backlog_list` accepts `parent_plan?` / `stage?` / `task_key?` as optional string inputs.
2. Filters applied in-memory AFTER existing `section` / `priority` / `type` / `status` / `scope` filters.
3. Lowercase substring compare — mixed-case inputs still match (matches existing `backlog_list` pattern per N3).
4. id-desc ordering preserved after filtering.
5. Tool descriptor JSON schema + use-case prose updated.
6. Zero behavior change for callers omitting the new inputs.

### 2.2 Non-Goals

1. Tests for new filters — lives in sibling TECH-439.
2. Catalog doc updates in `docs/mcp-ia-server.md` — lives in TECH-440.
3. New MCP tools — reverse-lookup tools shipped in Stage 4.1.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Skill author | As `release-rollout-enumerate` consumer, I want `backlog_list parent_plan=...` to return all issues under one plan so I avoid manual scan. | MCP call w/ `parent_plan` filter returns matching open+archive issues, id-desc. |
| 2 | Skill author | As `/ship` dispatcher, I want `backlog_list stage=4.2 parent_plan=...` to narrow to one stage. | Multi-filter intersection returns only rows matching both. |

## 4. Current State

### 4.1 Domain behavior

`backlog_list` currently filters on `section` / `priority` / `type` / `status` / `scope`. Schema-v2 fields (`parent_plan`, `stage`, `task_key`, etc.) parsed + exposed on `ParsedBacklogIssue` since TECH-363/364 but not queryable via `backlog_list`.

### 4.2 Systems map

- `tools/mcp-ia-server/src/tools/backlog-list.ts` — target file; add inputs + filter predicates.
- `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts` — source of `parent_plan` / `stage` / `task_key` fields on `ParsedBacklogIssue` (read-only here).
- `tools/mcp-ia-server/src/index.ts` — tool registry; descriptor update lives here.
- N3 reference: `docs/parent-plan-locator-fields-exploration.md` §Phase 8 (lowercase substring compare rationale).

## 5. Proposed Design

### 5.1 Target behavior

Caller passes any subset of `{ parent_plan, stage, task_key }`. Each is lowercase-substring-compared against the corresponding `ParsedBacklogIssue` field. Missing input = filter skipped. Multi-filter = AND-intersection. Output shape unchanged (`{ issues, total }`, id-desc).

### 5.2 Architecture / implementation

Implementer owns exact filter-chain insertion point inside `backlog-list.ts`. Match existing filter-pattern style (lowercase on input + field, `.includes`). Null / empty field on an issue = no match for that filter.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Lowercase substring compare (N3) | Matches existing `backlog_list` filter pattern; callers rarely know exact case of plan paths | Exact-match compare (rejected — fragile for path-arg callers) |

## 7. Implementation Plan

### Phase 1 — Filter wiring

- [ ] Read existing filter chain in `backlog-list.ts`.
- [ ] Add optional `parent_plan?` / `stage?` / `task_key?` to input schema.
- [ ] Append filter predicates after existing chain; lowercase-substring style.
- [ ] Preserve id-desc ordering.
- [ ] Update tool descriptor + use-case prose.
- [ ] Restart MCP schema cache (N4) noted to caller via descriptor.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| New inputs wired | Node | `npm run validate:all` | Schema typecheck catches bad descriptor |
| Filter behavior | Deferred | → TECH-439 | Fixture-driven test lives in sibling |

## 8. Acceptance Criteria

- [ ] `parent_plan?` / `stage?` / `task_key?` added to `backlog_list` input schema.
- [ ] Filter predicates lowercase substring compare.
- [ ] Applied after existing filters; id-desc preserved.
- [ ] Tool descriptor use-case prose updated.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: substring filter on `parent_plan` matches unintended records (partial path collision). Mitigation: document “substring” semantics in tool descriptor; tests cover multi-match + empty.
- Risk: `stage` filter collides w/ backlog `section` field naming — clarify input is orchestrator **Stage** id string (e.g. `4.1`), not yaml `section`. Mitigation: descriptor + examples.
- Risk: filter order drift — must stay “after existing filters” per `backlog-yaml-mcp-alignment-master-plan` Stage block Exit. Mitigation: unit test asserts call order or snapshot filter pipeline.
- Invariant touch: MCP tool schema + descriptor must stay synced; run `validate:all` for mcp-ia-server tests.

### §Examples

| Input | Records returned |
|-------|-------------------|
| `parent_plan: "backlog-yaml"` | Any issue whose parent_plan path contains substring |
| `task_key: "T4.2"` | Rows w/ matching task_key substring |
| Combined + `priority: high` | Intersection of predicates |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| backlog_list_filters | fixture yaml set | each filter + intersection | node (mcp tests) |
| validate_all | repo | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] Optional inputs on `backlog_list` tool + TypeScript types.
- [ ] Lowercase substring compare; applied after existing filters; id-desc preserved.
- [ ] Descriptor updated; `validate:all` green.

### §Findings

- **TECH-439** owns fixture breadth; keep filter logic minimal here.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
