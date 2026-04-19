---
purpose: "TECH-86 — Lifecycle skill refactor: project hierarchy rules + orchestrator-vs-spec distinction."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-86 — Lifecycle skill refactor: project hierarchy + orchestrator distinction

> **Issue:** [TECH-86](../../BACKLOG.md)
> **Status:** Final
> **Created:** 2026-04-12
> **Last updated:** 2026-04-12

## 1. Summary

Extract the step/stage/phase/task execution hierarchy from the multi-scale master plan into global always-loaded rules. Teach lifecycle skills the orchestrator-vs-spec distinction so they refuse to close orchestrator documents. Expand the project spec status enum to include `In Progress`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Global `ia/rules/project-hierarchy.md` defining step > stage > phase > task semantics.
2. Global `ia/rules/orchestrator-vs-spec.md` defining the permanent vs temporary doc distinction.
3. All 4 lifecycle skills updated with orchestrator awareness.
4. Template status enum expanded to `Draft | In Review | In Progress | Final`.
5. Glossary Documentation block includes `orchestrator document` and `project hierarchy`.
6. AGENTS.md §4 cross-references the new rules.

### 2.2 Non-Goals (Out of Scope)

1. Creating orchestrator-specific skills (e.g. `orchestrator-close`).
2. Modifying the `project-new` skill (creates project specs, not orchestrators).
3. Process-engineering gaps (#2, #3, #5, #10) from the brainstorm.

## 7. Implementation Plan

### Phase 1 — Rules + template + skills + glossary

- [x] Create `ia/rules/project-hierarchy.md`
- [x] Create `ia/rules/orchestrator-vs-spec.md`
- [x] Update `ia/templates/project-spec-template.md` status enum
- [x] Update `ia/skills/project-spec-close/SKILL.md` — orchestrator guard
- [x] Update `ia/skills/project-spec-kickoff/SKILL.md` — orchestrator routing
- [x] Update `ia/skills/project-spec-implement/SKILL.md` — orchestrator navigation
- [x] Update `ia/skills/project-stage-close/SKILL.md` — orchestrator step/stage close
- [x] Update `AGENTS.md` §4 — cross-ref new rules
- [x] Add glossary process terms (orchestrator document, project hierarchy)
- [x] Create BACKLOG row + this project spec

## 8. Acceptance Criteria

- [x] `ia/rules/project-hierarchy.md` exists, `loaded_by: always`
- [x] `ia/rules/orchestrator-vs-spec.md` exists, `loaded_by: always`
- [x] `project-spec-close` has orchestrator guard section
- [x] Template status enum includes `In Progress`
- [x] Glossary has `orchestrator document` and `project hierarchy` rows
- [x] AGENTS.md §4 references new rule files

## 10. Lessons Learned

- The step/stage/phase/task hierarchy was buried inside a project-specific master plan doc. Extracting to global rules makes it available to all agents without loading the master plan.
- `In Progress` status was used in practice but missing from the template enum — always formalize observed usage.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
