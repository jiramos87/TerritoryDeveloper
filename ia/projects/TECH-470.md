---
purpose: "TECH-470 — Project-new-apply slim skill + retire project-new-plan + drop §Project-New Plan template section (Stage 7 T7.3)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.3"
---
# TECH-470 — Project-new-apply slim skill + retire project-new-plan + drop §Project-New Plan template section (Stage 7 T7.3)

> **Issue:** [TECH-470](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Collapse single-issue `/project-new` flow. Drop `§Project-New Plan` Opus pair-head — no N=1 pair needed; slim Sonnet `project-new-apply` reads args directly from command + materializes id + yaml + spec stub; hands off to `plan-author` at N=1. Retire legacy `project-new-plan/` skill with tombstone redirect.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/project-new-apply/SKILL.md` — slim Sonnet materialization; no `§Project-New Plan` read.
2. `ia/skills/_retired/project-new-plan/` tombstone present.
3. `§Project-New Plan` section removed from `ia/templates/project-spec-template.md`.

### 2.2 Non-Goals

1. `/project-new` command repoint (handled T7.8 / TECH-475).
2. plan-author N=1 handoff contract (handled T7.11 / TECH-478).

## 4. Current State

### 4.2 Systems map

- `ia/templates/project-spec-template.md` carries `§Project-New Plan` anchor (added in TECH-445). Drop in this task.
- `ia/skills/project-new/SKILL.md` — current monolithic; splits into planner (retired here) + applier (new).
- TECH-478 plan-author handoff = downstream dependency.

## 7. Implementation Plan

### Phase 1 — Author project-new-apply SKILL.md

- Inputs: direct `/project-new` args (title, type, priority).
- Flow: `reserve-id.sh` → yaml → spec stub → `materialize-backlog.sh` → `validate:dead-project-specs` → handoff signal for `plan-author` N=1.

### Phase 2 — Retire project-new-plan + template section drop + validate

## 8. Acceptance Criteria

- [ ] Slim applier SKILL.md present + `phases:` frontmatter.
- [ ] Tombstone file at `ia/skills/_retired/project-new-plan/SKILL.md`.
- [ ] Template no longer contains `§Project-New Plan` heading.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only.
