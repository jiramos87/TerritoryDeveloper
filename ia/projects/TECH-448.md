---
purpose: "TECH-448 — Write plan-apply-pair-contract rule; canonical §Plan tuple shape; 5 pair seams; validation/escalation/idempotency."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.2.5"
---
# TECH-448 — Write plan-apply-pair-contract rule

> **Issue:** [TECH-448](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Author new file `ia/rules/plan-apply-pair-contract.md` — canonical contract every Plan-Apply pair seam reads. Defines `§Plan` tuple shape, enumerates 5 pair seams, sets validation gate + escalation rule + idempotency requirement. Downstream pair-head + pair-tail skills (Step 3 onward) all cite this contract.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `ia/rules/plan-apply-pair-contract.md` exists.
2. Canonical `§Plan` tuple shape defined: `{operation, target_path, target_anchor, payload}`; Opus resolves anchors to exact line/heading/glossary-row-id.
3. 5 pair seams enumerated: plan-review → plan-fix-apply, stage-file-plan → stage-file-apply, project-new-plan → project-new-apply, code-review → code-fix-apply, audit → closeout-apply.
4. Validation gate clause: Sonnet runs appropriate validator per pair; on failure returns control to Opus w/ error + failing tuple.
5. Escalation clause: ambiguous anchor → immediate return to Opus; Sonnet never guesses.
6. Idempotency clause: re-running applier on already-applied tuple = no-op + exit 0.
7. `npm run validate:frontmatter` exit 0.

### 2.2 Non-Goals

1. Author the pair-head / pair-tail skills themselves — Step 3.3.
2. Author MCP `plan_apply_validate` tool — Step 3.1.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Pair-head Opus author | As an Opus pair-head, I want one canonical contract so my `§Plan` payload is automatically applier-readable across all 5 seams. | Tuple shape + 5 seams documented. |
| 2 | Pair-tail Sonnet | As a Sonnet pair-tail, I want explicit escalation rules so I never silently guess anchors. | Escalation clause present. |
| 3 | Validator | As `plan_apply_validate` MCP tool (TECH-Step-3.1), I want a stable shape to validate against. | Tuple shape stable. |

## 4. Current State

### 4.1 Domain behavior

No `plan-apply-pair-contract.md` exists. Plan-Apply pair pattern is described only in `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion. Downstream skills cannot cite a stable rule.

### 4.2 Systems map

- `ia/rules/plan-apply-pair-contract.md` — new file (this task).
- `ia/rules/project-hierarchy.md` — TECH-446; sibling rule.
- `ia/rules/orchestrator-vs-spec.md` — TECH-447; sibling rule.
- `ia/templates/project-spec-template.md` — TECH-445; defines section anchors (`§Project-New Plan`, `§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`) the contract references.
- `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion — source of pair seam list + tuple shape rationale.

## 5. Proposed Design

### 5.1 Target behavior

Reader of new rule file sees:

1. Header + scope: applies to all 5 Plan-Apply pair seams in lifecycle.
2. Canonical `§Plan` tuple shape definition — bulleted spec of `{operation, target_path, target_anchor, payload}` keys + value rules.
3. Pair seam table — 5 rows mapping Opus pair-head → Sonnet pair-tail + the spec section the payload lives in.
4. Validation gate paragraph.
5. Escalation rule paragraph.
6. Idempotency requirement paragraph.

### 5.2 Architecture / implementation

Pure markdown authoring. Implementer drafts new file w/ standard rule frontmatter (mirror frontmatter of existing rules under `ia/rules/`).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Tuple keys = operation/target_path/target_anchor/payload | Per Stage 1.2 Exit + exploration doc Implementation Points | Free-form prose plan (rejected — applier cannot parse) |

## 7. Implementation Plan

### Phase 1 — Authoring

- [ ] Read existing rule files under `ia/rules/` for frontmatter convention.
- [ ] Author new `ia/rules/plan-apply-pair-contract.md` w/ all 6 sections above.
- [ ] Cross-link from `ia/rules/project-hierarchy.md` (TECH-446) where pair semantics relevant.
- [ ] `npm run validate:frontmatter` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| New rule parses + sections present | Node | `npm run validate:all` | Doc validators only |

## 8. Acceptance Criteria

- [ ] `ia/rules/plan-apply-pair-contract.md` exists.
- [ ] Canonical tuple shape defined.
- [ ] 5 pair seams enumerated.
- [ ] Validation gate + escalation + idempotency clauses present.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
