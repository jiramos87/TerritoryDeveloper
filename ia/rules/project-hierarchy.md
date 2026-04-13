---
purpose: "Defines the step/stage/phase/task execution hierarchy for multi-scale orchestrator and project spec workflows."
audience: agent
loaded_by: always
slices_via: none
description: "Global project hierarchy: step > stage > phase > task. Lazy materialization, ephemeral specs, learnings flow backward."
alwaysApply: true
---

# Project hierarchy — execution units

Four levels, loosely bound. All orchestrator docs + project specs.

| Level | Definition | Materialization | Lifecycle |
|-------|-----------|-----------------|-----------|
| **Step** | Major product milestone | Stable; in global orchestrator | Permanent in orchestrator |
| **Stage** | Sub-milestone inside step | Semi-stable; orchestrator created lazily when parent step → `In Progress` | Deleted after step closes |
| **Phase** | Shippable compilable increment (merged PRs) | Rewritable until `In Progress`; frozen while active | Deleted after stage closes |
| **Task** | Atomic unit; 1 BACKLOG row + `ia/projects/{ISSUE_ID}.md` | Defined when parent phase → `In Progress` | Spec deleted on issue close per `project-spec-close` |

## Phase/Task cardinality

Phase must contain ≥2 tasks. Single-task phase requires explicit justification in the parent spec's Decision Log.

**Why:** 1:1 phase→task mapping conflates logical grouping with atomic unit. Phase = shippable increment with verifiable exit criterion; task = one BACKLOG row + one project spec, independently implementable and closeable. Decomposing each phase into ≥2 tasks surfaces hidden coupling early and keeps individual PRs small.

**Enforcement:** `stage-file` warns when a phase in the task table has only 1 task. Orchestrator author must either split the task or log the justification before filing.

## Learnings flow backward

Task close → phase Lessons Learned. Phase close → stage rollup. Stage close → step Decision Log → next step skeleton.

## Lazy materialization

- Step/stage orchestrators materialize only when parent → `In Progress`.
- Do NOT pre-create orchestrator docs for future steps.

## Ephemeral spec lifecycle

Specs temporary. After implementation:
- Migrate canonical knowledge → glossary, reference specs, `ARCHITECTURE.md`, rules, `docs/`.
- Persist verbose Decision Log + Lessons Learned → Postgres journal (`project_spec_journal_persist`).
- Delete spec. Only canonical docs survive.

## Status enum

All specs + orchestrators use: **`Draft`** | **`In Review`** | **`In Progress`** | **`Final`**.

`In Progress` format includes active context: `In Progress — Step 2 / Stage 3 / TECH-42`.
