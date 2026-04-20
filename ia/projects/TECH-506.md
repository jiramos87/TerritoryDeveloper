---
purpose: "TECH-506 — B4 unified plan-applier consolidation (retire 3 per-pair appliers)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T10.5"
---
# TECH-506 — B4 unified plan-applier consolidation (retire 3 per-pair appliers)

> **Issue:** [TECH-506](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Author `ia/skills/plan-applier/SKILL.md` as a unified Sonnet literal-applier reading any `§*Fix Plan` / `§Stage Closeout Plan` tuple shape. Retire 3 legacy per-pair applier skills (`plan-fix-apply`, `code-fix-apply`, `stage-closeout-apply`) and their agents. Update all pair-head skills + commands to dispatch `plan-applier`. Resolves legacy Open Q11.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/plan-applier/SKILL.md` present with dispatch table + escalation contract.
2. `.claude/agents/plan-applier.md` present (Sonnet, caveman, uniform tools frontmatter).
3. 3 retired skills + 3 retired agents moved to `_retired/` with tombstone headers.
4. `/plan-review`, `/code-review`, `/closeout` command dispatcher files point to `plan-applier`.
5. `ia/rules/plan-apply-pair-contract.md` references unified applier.
6. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. F5 tools uniformity validator (T10.4 scope — prerequisite).
2. Changing plan-head Opus skills (only tail changed here).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Single applier for all Plan-Apply pair seams | plan-applier SKILL present with dispatch table |

## 4. Current State

### 4.1 Domain behavior

3 separate Sonnet appliers: `plan-fix-apply`, `code-fix-apply`, `stage-closeout-apply`. Divergent logic; maintenance overhead. Open Q11 unresolved.

### 4.2 Systems map

Creates: ia/skills/plan-applier/SKILL.md, .claude/agents/plan-applier.md.
Retires: ia/skills/{plan-fix-apply,code-fix-apply,stage-closeout-apply}/ → ia/skills/_retired/.
Retires: .claude/agents/{plan-fix-applier,code-fix-applier,stage-closeout-applier}.md → .claude/agents/_retired/.
Edits: .claude/commands/{plan-review,code-review,closeout}.md.
Edits: ia/rules/plan-apply-pair-contract.md.

### 4.3 Implementation investigation notes (optional)

Dispatch table keyed on operation type: fs_edit, glossary_row, backlog_archive, id_purge, spec_delete, status_flip, digest_emit. Escalate to Opus on anchor ambiguity. Bounded 1 retry on transient write failure.

## 5. Proposed Design

### 5.1 Target behavior (product)

Unified literal-applier reads any tuple shape with `{operation, target_path, target_anchor, payload}`. Dispatches per operation type. Single escalation contract. Resolves Open Q11.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Author `ia/skills/plan-applier/SKILL.md` — Phase 1 (parse tuples), Phase 2 (dispatch), Phase 3 (validate), Phase 4 (return).
2. Author `.claude/agents/plan-applier.md` — Sonnet, caveman, uniform tools list.
3. Move 3 skills to `_retired/`; add tombstone header.
4. Move 3 agents to `_retired/`; add tombstone header.
5. Edit `/plan-review`, `/code-review`, `/closeout` commands to dispatch plan-applier.
6. Edit `ia/rules/plan-apply-pair-contract.md`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Unified applier | Reduces drift; resolves Q11 | Keep 3 separate appliers |

## 7. Implementation Plan

### Phase 1 — Author unified skill + agent

- [ ] Author `ia/skills/plan-applier/SKILL.md` with dispatch table.
- [ ] Author `.claude/agents/plan-applier.md`.

### Phase 2 — Retire legacy appliers

- [ ] Move `plan-fix-apply`, `code-fix-apply`, `stage-closeout-apply` skills to `_retired/` with tombstones.
- [ ] Move `plan-fix-applier`, `code-fix-applier`, `stage-closeout-applier` agents to `_retired/` with tombstones.

### Phase 3 — Update commands + contract

- [ ] Edit `/plan-review`, `/code-review`, `/closeout` dispatchers.
- [ ] Edit `ia/rules/plan-apply-pair-contract.md`.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| validate:all | Node | `npm run validate:all` | Tooling only |

## 8. Acceptance Criteria

- [ ] `ia/skills/plan-applier/SKILL.md` present with dispatch table + escalation contract.
- [ ] `.claude/agents/plan-applier.md` present (Sonnet, caveman, uniform tools frontmatter).
- [ ] 3 retired skills + 3 retired agents moved to `_retired/` with tombstone headers.
- [ ] `/plan-review`, `/code-review`, `/closeout` command dispatcher files point to `plan-applier`.
- [ ] `ia/rules/plan-apply-pair-contract.md` references unified applier.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/lifecycle-refactor-master-plan.md Stage 10`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

### §Audit Notes

### §Examples

### §Test Blueprint

### §Acceptance

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
