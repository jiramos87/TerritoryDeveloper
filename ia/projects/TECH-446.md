---
purpose: "TECH-446 — Rewrite project-hierarchy rule to 2-level (Stage·Task); cardinality gate ≥2 tasks per Stage."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.2.3"
---
# TECH-446 — Rewrite project-hierarchy rule

> **Issue:** [TECH-446](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Rewrite `ia/rules/project-hierarchy.md` so the canonical hierarchy table collapses from 4 rows (Step·Stage·Phase·Task) to 2 rows (Stage·Task). Restate cardinality gate at Stage granularity (≥2 hard, ≤6 soft). Update lazy-materialization + ephemeral-spec rules to Stage scope.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Hierarchy table = 2 rows (Stage · Task).
2. Cardinality gate: ≥2 tasks per Stage (hard); ≤6 (soft).
3. Lazy-materialization rule at Stage granularity.
4. Ephemeral-spec rule preserved: each Task gets `ia/projects/{ISSUE_ID}.md`.
5. Phase + Gate rows absent.
6. `npm run validate:frontmatter` exit 0.

### 2.2 Non-Goals

1. Edit `orchestrator-vs-spec.md` — TECH-447.
2. Add new glossary rows for **Stage** redefinition — TECH-449.
3. Migrate existing master plans / specs — Step 2.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Lifecycle-skill author | As a skill author reading the hierarchy rule, I want a single 2-row table so I never need to map Phase semantics to anything else. | 2-row table; Phase row absent. |
| 2 | Cardinality validator | As a validator reading the gate, I want ≥2 tasks per Stage cleanly stated so I can enforce on `stage-file`. | Gate text = "≥2 tasks per Stage". |

## 4. Current State

### 4.1 Domain behavior

`ia/rules/project-hierarchy.md` currently encodes 4-level hierarchy (Step→Stage→Phase→Task). Cardinality gate phrased "≥2 tasks per Phase". Lazy-materialization and ephemeral-spec rules reference Phase scope.

### 4.2 Systems map

- `ia/rules/project-hierarchy.md` — rewrite target.
- `ia/templates/master-plan-template.md` — TECH-444; template + rule must agree on 2-level.
- `ia/specs/glossary.md` — TECH-449 redefines **Stage** + **Project hierarchy** + tombstones **Phase** + **Gate**.
- `ia/state/pre-refactor-snapshot/` — original rule available for diff.

## 5. Proposed Design

### 5.1 Target behavior

Reader sees 2-row table:

| Layer | Owner doc | Cardinality | Materialization |
|-------|-----------|-------------|-----------------|
| Stage | Master plan | ≥2 Tasks (hard); ≤6 (soft) | Authored at master-plan-new time |
| Task | Project spec (`ia/projects/{ISSUE_ID}.md`) | 1 Task = 1 Issue | Lazy via `stage-file` |

(Implementer may adjust columns to match existing table conventions.)

### 5.2 Architecture / implementation

Pure markdown edit. Drop Step + Phase rows; restate gate sentence; update lazy-materialization paragraph; preserve ephemeral-spec rule (rephrased Stage→Task scope).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Hard cardinality ≥2 / soft ≤6 per Stage | Per Stage 1.2 Exit; matches existing Phase gate semantics one level up | ≥3 hard (rejected — too restrictive for small Stages) |

## 7. Implementation Plan

### Phase 1 — Rule rewrite

- [ ] Read current `ia/rules/project-hierarchy.md`.
- [ ] Collapse hierarchy table 4→2 rows.
- [ ] Restate cardinality gate at Stage scope.
- [ ] Update lazy-materialization paragraph.
- [ ] Update ephemeral-spec rule (Task = `ia/projects/{ISSUE_ID}.md`).
- [ ] `npm run validate:frontmatter` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Rule parses + 2-row table | Node | `npm run validate:all` | Doc validators only |

## 8. Acceptance Criteria

- [ ] Hierarchy table = 2 rows (Stage · Task).
- [ ] Cardinality gate = ≥2 tasks per Stage (hard).
- [ ] Lazy-materialization at Stage granularity.
- [ ] Ephemeral-spec rule preserved at Task scope.
- [ ] Phase + Gate rows absent.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
