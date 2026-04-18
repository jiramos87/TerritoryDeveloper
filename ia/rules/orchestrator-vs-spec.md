---
purpose: "Distinguishes orchestrator documents (permanent coordination docs) from project specs (temporary per-issue docs)."
audience: agent
loaded_by: always
slices_via: none
description: "Orchestrator vs project spec distinction. Orchestrators are NOT closeable via project-spec-close."
alwaysApply: true
---

# Orchestrator documents vs project specs

## Orchestrator document

- **Permanent** coordination doc tracking multi-step plan (e.g. `ia/projects/multi-scale-master-plan.md`).
- Owns step/stage skeleton; child specs created lazily beneath.
- **NOT closeable** via `project-spec-close` / `closeout`. Never deleted by automation.
- Status enum (full): `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final`.
  - `Skeleton` — step authored by `master-plan-new` but not yet decomposed into stages (deferred materialization). Flipped `Skeleton → Draft (tasks _pending_)` by `stage-decompose` (R7).
  - `Planned` — step decomposed into stages/tasks (all `_pending_`), not yet filed by `stage-file`.
  - `Draft` — initial pre-filing state; also used post-decompose before first task filed.
  - `In Progress — Step {N} / Stage {N.M}` — at least one task filed; plan actively worked. Flipped from `Draft` by `stage-file` on first task ever filed (R1); Stage header flipped `Draft/Planned → In Progress` by `stage-file` on first task filed in that stage (R2).
  - `Final` — all Steps read `Final`; flipped by `project-stage-close` / `project-spec-close` (R5). `master-plan-extend` demotes back to `In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).
- Step/stage orchestrators (children of global) deleted when parent step/stage closes — only after learnings migrated per `project-hierarchy.md`.

## Status flip responsibility matrix (R1–R7)

| Rule | Trigger | Who flips | From → To |
|------|---------|-----------|-----------|
| R1 | First task ever filed on plan | `stage-file` | Plan top `Draft` → `In Progress — Step {N} / Stage {N.M}` |
| R2 | First task filed in a stage | `stage-file` | Stage `Draft/Planned` → `In Progress` |
| R3 | All tasks in stage archived | `project-stage-close` | Stage `In Progress` → `Final` (existing) |
| R4 | All stages in step Final | `project-stage-close` / `project-spec-close` | Step `In Progress` → `Final` (existing) |
| R5 | All Steps Final | `project-stage-close` / `project-spec-close` | Plan top `In Progress` → `Final` |
| R6 | New Steps appended to Final plan | `master-plan-extend` | Plan top `Final` → `In Progress — Step {N_new} / Stage {N_new}.1` |
| R7 | Step decomposed from skeleton | `stage-decompose` | Step `Skeleton` → `Draft (tasks _pending_)` |

## Project spec

- **Temporary** per-issue doc (`ia/projects/{ISSUE_ID}.md`) tied to 1 BACKLOG row.
- Created from `ia/templates/project-spec-template.md` via `project-new`.
- Deleted on close via `project-spec-close` after IA persistence + journal capture.

## Guards

- `project-spec-close`: **refuse** to delete files matching orchestrator patterns (`*master-plan*`, `step-*-*.md`, `stage-*-*.md` under orchestrator dirs).
- `project-spec-kickoff`: recognize orchestrator docs, route to step/stage review — not issue-level kickoff.
- `project-spec-implement`: navigate orchestrator step/stage structure when target = step/stage, not single issue.
