---
purpose: "Defines the step/stage/phase/task execution hierarchy for multi-scale orchestrator and project spec workflows."
audience: agent
loaded_by: always
slices_via: none
description: "Global project hierarchy: step > stage > phase > task. Lazy materialization, ephemeral specs, learnings flow backward."
alwaysApply: true
---

# Project hierarchy — execution units

Four levels, loosely bound. Applies to **all** orchestrator documents and project specs, not just one plan.

| Level | Definition | Materialization | Lifecycle |
|-------|-----------|-----------------|-----------|
| **Step** | Major product milestone | Stable; defined in the global orchestrator | Permanent in orchestrator |
| **Stage** | Coherent sub-milestone inside a step | Semi-stable; orchestrator doc created lazily when parent step enters `In Progress` | Deleted after step closes |
| **Phase** | Shippable compilable increment (measured in merged PRs) | Rewritable until `In Progress`; frozen while active | Deleted after stage closes |
| **Task** | Atomic unit of work; maps to exactly one BACKLOG row + `ia/projects/{ISSUE_ID}.md` | Fully defined only when parent phase enters `In Progress` | Spec deleted on issue close per `project-spec-close` |

## Learnings flow backward

Task closes -> phase Lessons Learned. Phase closes -> stage rollup. Stage closes -> step Decision Log -> next step's skeleton.

## Lazy materialization

- Step and stage orchestrators materialize **only** when their parent enters `In Progress`.
- Do not pre-create orchestrator docs for future steps.

## Ephemeral spec lifecycle

Specs are **temporary**. After implementation completes:
- Migrate canonical knowledge to glossary, reference specs, `ARCHITECTURE.md`, rules, `docs/`.
- Persist verbose Decision Log + Lessons Learned to Postgres journal (`project_spec_journal_persist`).
- Delete the spec. Only permanent, canonical documentation survives.

## Status enum

All project specs and orchestrator documents use: **`Draft`** | **`In Review`** | **`In Progress`** | **`Final`**.

`In Progress` format includes active context: `In Progress — Step 2 / Stage 3 / TECH-42` (showing the currently active execution unit).
