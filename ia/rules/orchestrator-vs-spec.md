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

- **Permanent** coordination doc that tracks a multi-step plan (e.g. `ia/projects/multi-scale-master-plan.md`).
- Owns the step/stage skeleton; child specs are created lazily beneath it.
- **NOT closeable** via `project-spec-close` or `closeout`. Never deleted by automation.
- Status transitions: `Draft` -> `In Review` -> `In Progress` -> `Final`.
- Step and stage orchestrators (children of the global orchestrator) **are** deleted when their parent step/stage closes, but only after learnings are migrated per `project-hierarchy.md`.

## Project spec

- **Temporary** per-issue doc (`ia/projects/{ISSUE_ID}.md`) tied to a single BACKLOG row.
- Created from `ia/templates/project-spec-template.md` via `project-new`.
- Deleted on close via `project-spec-close` after IA persistence + journal capture.

## Guards

- `project-spec-close`: **refuse** to delete any file matching orchestrator patterns (e.g. `*master-plan*`, `step-*-*.md`, `stage-*-*.md` under orchestrator directories).
- `project-spec-kickoff`: recognize orchestrator docs and route to step/stage review instead of issue-level kickoff.
- `project-spec-implement`: navigate orchestrator step/stage structure when the target is a step or stage, not a single issue.
