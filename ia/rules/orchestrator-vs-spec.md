---
purpose: "Distinguishes orchestrator documents (permanent coordination docs) from project specs (temporary per-issue docs)."
audience: agent
loaded_by: always
slices_via: none
description: "Orchestrator vs project spec distinction. Orchestrators are NOT closeable via /ship-stage Pass B closeout."
alwaysApply: true
---

# Orchestrator documents vs project specs

## Orchestrator document

- **Permanent** coordination doc — DB-backed row in `ia_master_plans` (e.g. slug `multi-scale`), rendered via `mcp__territory-ia__master_plan_render({slug})`. No filesystem `.md` master-plan files.
- Owns Stage skeleton; child Tasks materialize lazily beneath via `stage-file` applier pass.
- **NOT closeable** via `/ship-stage` Pass B inline closeout. Never deleted by automation.
- Status enum: `Draft | In Review | In Progress — Stage {N.M} / TECH-XX | Final`.
  - `Draft` — initial pre-filing state; also used post-`master-plan-new` before first Task filed.
  - `In Review` — plan content under review (e.g. `master-plan-extend` mid-pass).
  - `In Progress — Stage {N.M} / TECH-XX` — at least one Task filed; plan actively worked. Flipped from `Draft` by `stage-file` applier pass on first Task ever filed (R1); Stage header flipped `Draft → In Progress` by `stage-file` applier pass on first Task filed in that Stage (R2).
  - `Final` — all Stages read `Final`; flipped by `/ship-stage` Pass B inline closeout via `stage_closeout_apply` MCP (R5; absorbs retired `project-stage-close` + `closeout-apply` path per T7.14 / M6 collapse). `master-plan-extend` demotes back to `In Progress — Stage {N_new}.1 / TECH-XX` when new Stages appended to a Final plan (R6).

## Status flip responsibility matrix (R1, R2, R5, R6)

| Rule | Trigger | Who flips | From → To |
|------|---------|-----------|-----------|
| R1 | First Task ever filed on plan | `stage-file` applier pass | Plan top `Draft` → `In Progress — Stage {N.M} / TECH-XX` |
| R2 | First Task filed in a Stage | `stage-file` applier pass | Stage `Draft` → `In Progress` |
| R5 | All Stages Final | `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP) | Plan top `In Progress` → `Final` |
| R6 | New Stages appended to Final plan | `master-plan-extend` | Plan top `Final` → `In Progress — Stage {N_new}.1 / TECH-XX` |

R3 (Stage rollup `In Progress → Final` on last Task archived) is owned by `/ship-stage` Pass B inline closeout via `stage_closeout_apply` MCP — see `project-hierarchy.md`. Phase-flip rules dropped (no Phase level under 2-level hierarchy). Skeleton-flip rule dropped (`master-plan-new` decomposes ALL Stages at author time; no Stage skeletons).

## Project spec

- **DB-backed** per-issue task body (`ia_tasks.body` column + `ia_task_spec_history` audit) tied to 1 BACKLOG row.
- Created from `ia/templates/project-spec-template.md` via `project-new-apply` (writes to DB).
- Archived on close via `stage_closeout_apply` MCP (`/ship-stage` Pass B) — sets `ia_tasks.archived_at`; no filesystem delete required (DB-primary post Step 9.x refactor).

## Guards

- `stage_closeout_apply` MCP: **refuse** to archive rows whose master-plan path matches orchestrator patterns (`*master-plan*`); orchestrators are permanent.
- `stage-authoring`: recognize orchestrator docs, route to Stage review — not issue-level kickoff (absorbed retired `spec-kickoff` path).
- `spec-implementer`: navigate orchestrator Stage structure when target = Stage, not single issue.
