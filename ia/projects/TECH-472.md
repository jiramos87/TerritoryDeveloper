---
purpose: "TECH-472 — Retire legacy kickoff + close skills; spec-enrich folded into bulk plan-author (Stage 7 T7.5)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.5"
---
# TECH-472 — Retire legacy kickoff + close skills; spec-enrich folded into bulk plan-author (Stage 7 T7.5)

> **Issue:** [TECH-472](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Retirement-only task. Move `ia/skills/project-spec-kickoff/` + `ia/skills/project-spec-close/` under `ia/skills/_retired/` with tombstone redirect headers. Spec-enrich is never authored — canonical-term enforcement absorbed into bulk `plan-author` (T7.11 / TECH-478). No new skill authoring here.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/_retired/project-spec-kickoff/SKILL.md` — tombstone pointing to `plan-author` + `/author`.
2. `ia/skills/_retired/project-spec-close/SKILL.md` — tombstone pointing to `stage-closeout-apply` + Stage-scoped `/closeout`.
3. Original paths no longer present under active `ia/skills/`.

### 2.2 Non-Goals

1. Authoring `ia/skills/spec-enrich/` — never exists.
2. Command retirements (handled T7.8 / TECH-475).
3. Agent retirements (handled T7.7 / TECH-474).

## 4. Current State

### 4.2 Systems map

- `ia/skills/project-spec-kickoff/SKILL.md` — legacy Opus kickoff.
- `ia/skills/project-spec-close/SKILL.md` — legacy per-Task closeout skill (replaced Stage-level).
- TECH-478 plan-author + TECH-481 stage-closeout-apply = redirect targets.

## 7. Implementation Plan

### Phase 1 — Move + tombstone

- `git mv` both skill dirs under `_retired/`; overwrite body w/ tombstone header + redirect link.

### Phase 2 — Validate

## 8. Acceptance Criteria

- [ ] Retired paths present with tombstone redirect headers.
- [ ] Originals absent from active skill tree.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only.
