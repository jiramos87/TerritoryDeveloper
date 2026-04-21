---
purpose: "Distinguishes orchestrator documents (permanent coordination docs) from project specs (temporary per-issue docs)."
audience: agent
loaded_by: always
slices_via: none
description: "Orchestrator vs project spec distinction. Orchestrators are NOT closeable via closeout-apply."
alwaysApply: true
---

# Orchestrator documents vs project specs

## Orchestrator document

- **Permanent** coordination doc tracking multi-stage plan (e.g. `ia/projects/multi-scale-master-plan.md`).
- Owns Stage skeleton; child Tasks materialize lazily beneath via `stage-file-apply`.
- **NOT closeable** via `closeout-apply` / `closeout`. Never deleted by automation.
- Status enum: `Draft | In Review | In Progress — Stage {N.M} / TECH-XX | Final`.
  - `Draft` — initial pre-filing state; also used post-`master-plan-new` before first Task filed.
  - `In Review` — plan content under review (e.g. `master-plan-extend` mid-pass).
  - `In Progress — Stage {N.M} / TECH-XX` — at least one Task filed; plan actively worked. Flipped from `Draft` by `stage-file-apply` on first Task ever filed (R1); Stage header flipped `Draft → In Progress` by `stage-file-apply` on first Task filed in that Stage (R2).
  - `Final` — all Stages read `Final`; flipped by `plan-applier` Mode stage-closeout (R5; absorbs retired `project-stage-close` + `closeout-apply` path per T7.14 / M6 collapse). `master-plan-extend` demotes back to `In Progress — Stage {N_new}.1 / TECH-XX` when new Stages appended to a Final plan (R6).

## Status flip responsibility matrix (R1, R2, R5, R6)

| Rule | Trigger | Who flips | From → To |
|------|---------|-----------|-----------|
| R1 | First Task ever filed on plan | `stage-file-apply` | Plan top `Draft` → `In Progress — Stage {N.M} / TECH-XX` |
| R2 | First Task filed in a Stage | `stage-file-apply` | Stage `Draft` → `In Progress` |
| R5 | All Stages Final | `plan-applier` Mode stage-closeout | Plan top `In Progress` → `Final` |
| R6 | New Stages appended to Final plan | `master-plan-extend` | Plan top `Final` → `In Progress — Stage {N_new}.1 / TECH-XX` |

R3 (Stage rollup `In Progress → Final` on last Task archived) is owned by the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode stage-closeout) — see `project-hierarchy.md`. Phase-flip rules dropped (no Phase level under 2-level hierarchy). Skeleton-flip rule dropped (`master-plan-new` decomposes ALL Stages at author time; no Stage skeletons).

## Project spec

- **Temporary** per-issue doc (`ia/projects/{ISSUE_ID}.md`) tied to 1 BACKLOG row.
- Created from `ia/templates/project-spec-template.md` via `project-new-apply`.
- Deleted on close via `closeout-apply` after IA persistence + journal capture.

## Guards

- `closeout-apply`: **refuse** to delete files matching orchestrator patterns (`*master-plan*` under orchestrator dirs).
- `spec-enricher` (kickoff): recognize orchestrator docs, route to Stage review — not issue-level kickoff.
- `spec-implementer`: navigate orchestrator Stage structure when target = Stage, not single issue.
