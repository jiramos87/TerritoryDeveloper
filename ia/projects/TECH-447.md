---
purpose: "TECH-447 — Update orchestrator-vs-spec rule R1–R7 matrix; drop Phase-flip rows; rewrite Step/Stage/Phase prose to Stage/Task."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.2.4"
---
# TECH-447 — Update orchestrator-vs-spec rule

> **Issue:** [TECH-447](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Edit `ia/rules/orchestrator-vs-spec.md` R1–R7 status flip matrix to drop Phase-flip rows and rewrite "Step/Stage/Phase" prose → "Stage/Task". Keep R2 (Stage In Progress flip via `stage-file`), R5 (Final rollup via `project-stage-close`), R6 (Final → In Progress via `master-plan-extend`), R7 (Skeleton → Draft via `stage-decompose`). Verify orchestrator vs project-spec distinction prose still accurate against rewritten hierarchy rule (TECH-446).

## 2. Goals and Non-Goals

### 2.1 Goals

1. R1–R7 matrix carries no Phase-level flip rows.
2. R2 + R5 + R6 + R7 retained verbatim or w/ minimal prose update.
3. Prose mentions of "Step/Stage/Phase" rewritten to "Stage/Task".
4. Orchestrator vs project-spec distinction prose consistent w/ TECH-446 rewrite.
5. `npm run validate:frontmatter` exit 0.

### 2.2 Non-Goals

1. Author Plan-Apply pair contract — TECH-448.
2. Update glossary — TECH-449.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Lifecycle skill | As a closeout skill checking which rule governs status flips, I want R1–R7 matrix Phase-free so I never look for a Phase-flip rule that no longer exists. | Matrix Phase-free; R2/R5/R6/R7 retained. |

## 4. Current State

### 4.1 Domain behavior

`ia/rules/orchestrator-vs-spec.md` carries R1–R7 status-flip matrix. Some rows reference Phase-level flips (e.g. Phase completion → stage rollup). Prose paragraphs reference "Step/Stage/Phase" naming.

### 4.2 Systems map

- `ia/rules/orchestrator-vs-spec.md` — edit target.
- `ia/rules/project-hierarchy.md` — TECH-446; this rule references hierarchy rule.
- `ia/state/pre-refactor-snapshot/` — original rule available.

## 5. Proposed Design

### 5.1 Target behavior

Reader sees R1–R7 matrix w/ no Phase-flip entries. R2 still describes `stage-file` Stage In Progress flip. R5 still describes `project-stage-close` Final rollup. R6 still describes `master-plan-extend` Final → In Progress. R7 still describes `stage-decompose` Skeleton → Draft. All "Phase" tokens in prose replaced by "Stage" or "Task".

### 5.2 Architecture / implementation

Pure markdown edit. Implementer reads current matrix, drops Phase rows, sweeps prose for "Phase" + "Step/Stage/Phase" + adjusts.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Keep R2/R5/R6/R7; drop Phase-flip rows | Stage 1.2 Exit explicitly enumerates retained rules | Renumber to R1–R4 (rejected — breaks downstream cross-refs) |

## 7. Implementation Plan

### Phase 1 — Matrix + prose update

- [ ] Read current `ia/rules/orchestrator-vs-spec.md`.
- [ ] Drop Phase-flip rows from R1–R7 matrix.
- [ ] Verify R2 + R5 + R6 + R7 retained.
- [ ] Sweep prose for "Step/Stage/Phase" → "Stage/Task".
- [ ] Verify orchestrator vs project-spec distinction prose still accurate.
- [ ] `npm run validate:frontmatter` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Rule parses + Phase-free | Node | `npm run validate:all` | Doc validators only |

## 8. Acceptance Criteria

- [ ] R1–R7 matrix Phase-flip rows absent.
- [ ] R2 + R5 + R6 + R7 retained.
- [ ] Prose "Step/Stage/Phase" rewritten "Stage/Task".
- [ ] Orchestrator vs project-spec distinction prose still accurate.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
