---
purpose: "TECH-471 — Opus-audit (Stage-scoped bulk) + code-review + code-fix-apply skills (Stage 7 T7.4)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.4"
---
# TECH-471 — Opus-audit (Stage-scoped bulk) + code-review + code-fix-apply skills (Stage 7 T7.4)

> **Issue:** [TECH-471](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Land three audit-chain skills. `opus-audit` = Opus **Stage-scoped bulk** (one pass writes all N `§Audit` paragraphs; feeds `stage-closeout-plan` T7.13). `opus-code-review` = Opus per-Task pair-head (PASS / minor / critical → `§Code Fix Plan`). `code-fix-apply` = Sonnet per-Task pair-tail (applies tuples; re-enters `/verify-loop`; 1 retry bound).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/opus-audit/SKILL.md` — Stage-scoped bulk; Phase 0 guardrail asserts non-empty §Findings per Task else escalate.
2. `ia/skills/opus-code-review/SKILL.md` — Opus pair-head; 3 verdict branches.
3. `ia/skills/code-fix-apply/SKILL.md` — Sonnet pair-tail; 1-retry bound escalation.
4. `code-review → code-fix-apply` seam registered in pair-contract rule.
5. `phases:` frontmatter on all 3.

### 2.2 Non-Goals

1. Per-Task `closeout-apply` (replaced Stage-level in T7.14 / TECH-481 — not authored).
2. `/audit` + `/code-review` commands (T7.8 / TECH-475).
3. `§Closeout Plan` writing (moved Stage-level).

## 4. Current State

### 4.2 Systems map

- `ia/rules/plan-apply-pair-contract.md` — seam registration target.
- Spec template `§Audit` + `§Code Review` + `§Code Fix Plan` anchors (TECH-445).
- `ia/skills/verify-loop/SKILL.md` — downstream consumer of code-fix-apply re-entry.

## 7. Implementation Plan

### Phase 1 — opus-audit bulk skill

- Stage-scoped; reads all N specs + Stage header + invariants + glossary bundle; emits N §Audit paragraphs; Phase 0 §Findings gate.

### Phase 2 — opus-code-review pair-head

- Per-Task; 3 verdicts; critical branch writes `§Code Fix Plan` tuples.

### Phase 3 — code-fix-apply pair-tail + seam registration + validate

## 8. Acceptance Criteria

- [ ] 3 SKILL.md files present + `phases:` frontmatter.
- [ ] opus-audit Phase 0 §Findings gate documented.
- [ ] code-review / code-fix-apply seam in pair-contract.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only. Rev 4 candidate S8 (bulk code-review) deferred; stays per-Task this Stage.
