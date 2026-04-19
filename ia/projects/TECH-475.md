---
purpose: "TECH-475 — Update commands — 4 new + 3 repointed + 1 retired (Stage 7 T7.8)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.8"
---
# TECH-475 — Update commands — 4 new + 3 repointed + 1 retired (Stage 7 T7.8)

> **Issue:** [TECH-475](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Reshape slash-command surface. 4 new (`/plan-review`, `/audit`, `/code-review`, `/author`); 3 repointed (`/stage-file`, `/project-new`, `/closeout`); 1 retired (`/kickoff` → `_retired/`). `/enrich` never authored. `/author` + `/audit` + `/closeout` Stage-scoped (`{MASTER_PLAN_PATH} {STAGE_ID}`); `/author` carries `--task {ISSUE_ID}` escape hatch.

## 2. Goals and Non-Goals

### 2.1 Goals

1. 4 new commands; 3 repointed; 1 retired.
2. Each carries caveman-asserting forwarding prompt citing owning SKILL.md.
3. `/stage-file` chains planner → applier → bulk `/author` Stage-scoped.
4. `/project-new` chains planner → applier → `/author --task` N=1.
5. `/closeout` rewired Stage-scoped per T7.14.

### 2.2 Non-Goals

1. Agent markdowns (T7.7 / TECH-474).
2. SKILL.md authoring (T7.1–T7.4, T7.11, T7.13, T7.14).

## 4. Current State

### 4.2 Systems map

- `.claude/commands/*.md` — current 13 slash dispatchers.
- TECH-474 agents = dispatch targets.
- TECH-478 `/author` + TECH-481 `/closeout` downstream.

## 7. Implementation Plan

### Phase 1 — Author 4 new commands

### Phase 2 — Repoint 3 existing commands

### Phase 3 — Retire `/kickoff` + validate

## 8. Acceptance Criteria

- [ ] 4 new, 3 repointed, 1 retired file set in place.
- [ ] All carry caveman prompt + SKILL citation.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only.
