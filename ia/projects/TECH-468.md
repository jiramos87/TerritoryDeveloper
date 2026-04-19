---
purpose: "TECH-468 — Plan-review + plan-fix-apply pair skills (Stage 7 T7.1)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.1"
---
# TECH-468 — Plan-review + plan-fix-apply pair skills (Stage 7 T7.1)

> **Issue:** [TECH-468](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Land first Plan-Apply pair of lifecycle refactor. Opus `plan-review` pair-head runs once per Stage before first Task kickoff — reads all filed Task specs + master-plan Stage header + invariants; emits structured `§Plan Fix` tuples when drift found. Sonnet `plan-fix-apply` pair-tail reads tuples + applies literal edits. Satisfies Stage 7 Exit — "plan-review + plan-fix-apply SKILL.md files present; seam registered in pair-contract rule".

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/plan-review/SKILL.md` authored w/ Opus pair-head contract per `ia/rules/plan-apply-pair-contract.md`.
2. `ia/skills/plan-fix-apply/SKILL.md` authored w/ Sonnet pair-tail — reads tuples; applies; escalates on anchor ambiguity.
3. `plan-review → plan-fix-apply` seam registered in `ia/rules/plan-apply-pair-contract.md` (first of 4 pair seams).
4. Both skills carry `phases:` frontmatter (consumed by T7.12 validator).

### 2.2 Non-Goals (Out of Scope)

1. Agent markdown files (handled T7.7 / TECH-474).
2. Command dispatchers (handled T7.8 / TECH-475).
3. Non-pair Stage-scoped bulk stages (plan-author / opus-audit — separate tasks).

## 4. Current State

### 4.2 Systems map

- `ia/skills/plan-review/`, `ia/skills/plan-fix-apply/` — new skill dirs.
- `ia/rules/plan-apply-pair-contract.md` — authored in Stage 2 (TECH-448); extend seam list.
- Glossary rows (TECH-449): **plan review**, **plan-fix apply**, **Plan-Apply pair**.
- Master-plan template (TECH-444): `§Plan Fix` stub already present on Stage blocks.

## 7. Implementation Plan

### Phase 1 — Author plan-review SKILL.md

- Opus pair-head contract; inputs (Stage header + N Task specs + invariants + glossary); outputs (`§Plan Fix` tuple list in master plan).
- Phase sequence + `phases:` frontmatter.

### Phase 2 — Author plan-fix-apply SKILL.md

- Sonnet pair-tail contract; reads `§Plan Fix`; applies literal edits; escalation rules.
- `phases:` frontmatter.

### Phase 3 — Register seam in pair-contract rule + validate

## 8. Acceptance Criteria

- [ ] Both SKILL.md files exist.
- [ ] Pair seam row added to `plan-apply-pair-contract.md`.
- [ ] `phases:` frontmatter present + validates against Phase N headings.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
