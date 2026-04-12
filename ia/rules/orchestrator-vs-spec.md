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
- Status: `Draft` → `In Review` → `In Progress` → `Final`.
- Step/stage orchestrators (children of global) deleted when parent step/stage closes — only after learnings migrated per `project-hierarchy.md`.

## Project spec

- **Temporary** per-issue doc (`ia/projects/{ISSUE_ID}.md`) tied to 1 BACKLOG row.
- Created from `ia/templates/project-spec-template.md` via `project-new`.
- Deleted on close via `project-spec-close` after IA persistence + journal capture.

## Guards

- `project-spec-close`: **refuse** to delete files matching orchestrator patterns (`*master-plan*`, `step-*-*.md`, `stage-*-*.md` under orchestrator dirs).
- `project-spec-kickoff`: recognize orchestrator docs, route to step/stage review — not issue-level kickoff.
- `project-spec-implement`: navigate orchestrator step/stage structure when target = step/stage, not single issue.
