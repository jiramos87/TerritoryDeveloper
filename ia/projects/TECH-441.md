---
purpose: "TECH-441 — Update `CLAUDE.md` §2 MCP-first ordering w/ 3 new reverse-lookup tools."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T4.2.4"
---
# TECH-441 — Update `CLAUDE.md` §2 MCP-first ordering

> **Issue:** [TECH-441](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-21

## 1. Summary

Append three additive callouts to `CLAUDE.md` §2 "MCP first" — `master_plan_locate` (issue→plan reverse), `master_plan_next_pending` (`/ship` next-task), `parent_plan_validate` (advisory in `validate:all` until Step 6 strict flip). Additive only — existing ordering block stays. Satisfies Stage 4.2 Phase 2 exit of `backlog-yaml-mcp-alignment-master-plan.md`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Three tool callouts appended to `CLAUDE.md` §2.
2. Existing ordering prose untouched.
3. Caveman style; tool names backticked; only existing paths linked.

### 2.2 Non-Goals

1. Catalog doc updates — TECH-440.
2. Tool impl / tests — earlier stages.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent author | As an agent reading `CLAUDE.md` top-matter first, I want new reverse-lookup tools mentioned so I reach for them instead of bash scans. | `master_plan_locate` + `master_plan_next_pending` + `parent_plan_validate` mentioned in §2. |

## 4. Current State

### 4.1 Domain behavior

`CLAUDE.md` §2 "MCP first" carries an ordering block ending w/ tools added in earlier stages (`reserve_backlog_ids`, `backlog_record_validate`, `backlog_list`). No callout for Step 3–4 reverse-lookup tools.

### 4.2 Systems map

- `CLAUDE.md` §2 — target section.
- References the three new tools registered by Stage 3.3 (TECH-408) + Stage 4.1 (TECH-413, TECH-415).

## 5. Proposed Design

### 5.1 Target behavior

Reader scanning §2 sees brief caveman callouts for the three new tools alongside existing ones. No rewrite.

### 5.2 Architecture / implementation

Implementer owns exact insertion point (likely at tail of §2 MCP-first list). Additive only.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Additive only, no rewrite | Existing ordering block battle-tested by many agents; rewrite risks breaking conventions | Full §2 rewrite (rejected — scope creep) |

## 7. Implementation Plan

### Phase 1 — §2 additive edit

- [ ] Read `CLAUDE.md` §2 current structure.
- [ ] Append 3 callouts — caveman, backticked tool names.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc chain green | Node | `npm run validate:all` | Doc validators only |

## 8. Acceptance Criteria

- [ ] Three callouts appended to `CLAUDE.md` §2.
- [ ] Existing ordering untouched.
- [ ] Caveman prose; tool names backticked.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: rewriting `CLAUDE.md` §2 voids `@`-import line budget or Cursor validate. Mitigation: **append-only** bullets per Stage Exit; run `npm run validate:claude-imports` if touched.
- Risk: stale tool ordering confuses MCP-first recipe. Mitigation: place `master_plan_locate` near `backlog_issue` flow description; do not reorder existing bullets wholesale.
- Ambiguity: `master_plan_next_pending` vs `/ship` — clarify suggested next-task flow without claiming automation.
- Invariant touch: `CLAUDE.md` is Claude-delta file; keep additions English + concise.

### §Examples

| Addition | Wording intent |
|----------|----------------|
| Reverse lookup | “After `backlog_issue`, use `master_plan_locate` when parent plan unknown” |
| Advisory | “`parent_plan_validate` runs advisory during `validate:all`” |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_claude_imports | edited CLAUDE.md | `npm run validate:claude-imports` exit 0 | node |
| validate_all | repo | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] §2 gains additive mentions: `master_plan_locate`, `master_plan_next_pending`, `parent_plan_validate` advisory note.
- [ ] No full rewrite of §2 ordering.
- [ ] Validators green.

### §Findings

- Cross-link **TECH-440** catalog entries so humans can jump docs ↔ CLAUDE.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
