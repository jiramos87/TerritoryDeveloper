---
purpose: "Defines the 2-level Stage/Task execution hierarchy for orchestrator and project spec workflows."
audience: agent
loaded_by: always
slices_via: none
description: "Global project hierarchy: stage > task. Lazy materialization, ephemeral specs, learnings flow backward."
alwaysApply: true
---

# Project hierarchy — execution units

Two levels, loosely bound. All orchestrator docs + project specs use this shape.

| Level | Definition | Materialization | Lifecycle |
|-------|-----------|-----------------|-----------|
| **Stage** | Shippable compilable increment (merged PRs) authored as a `### Stage N.M` block in a master plan; carries Exit + Tasks subsections | Authored at `master-plan-new` time; refined later by `master-plan-extend` / `stage-decompose` | Permanent in orchestrator; flips `Draft → In Review → In Progress → Final` over its lifetime |
| **Task** | Atomic unit; 1 BACKLOG row + 1 `ia/projects/{ISSUE_ID}.md` spec | Defined as `_pending_` row in Stage Tasks table; lazy-materialized to BACKLOG row + spec stub by `stage-file-apply` when parent Stage opens | Spec deleted on issue close per `closeout-apply`; BACKLOG row archived |

## Stage/Task cardinality gate

- **Hard:** ≥2 Tasks per Stage. Single-task Stage requires explicit justification in the parent master plan's Stage Decision Log (or umbrella Decision Log when the Stage has no spec).
- **Soft:** ≤6 Tasks per Stage. Split at ≥7 — large Stages indicate hidden grouping.

**Why:** Stage = the unit verified end-to-end (Path A + batched Path B in `/ship-stage`); Task = one BACKLOG row + one project spec, independently implementable and closeable. Decomposing each Stage into ≥2 Tasks surfaces hidden coupling early and keeps individual PRs small. Single-Task Stages collapse Stage and Task semantics — they exist only when no further atomic split makes sense.

**Enforcement:** `master-plan-new` Phase 6 + `master-plan-extend` Phase 5 + `stage-decompose` Phase 3 fail when a Stage carries <2 Tasks without a Decision Log waiver. `stage-file-plan` re-checks before authoring `§Stage File Plan`.

## Learnings flow backward

Task close → Stage `§Audit` migration anchors → glossary / reference spec / docs (per `closeout-apply`). Stage close → master-plan rollup + cross-Stage decision log entries via `project-stage-close` skill.

## Lazy materialization

- Stage blocks are authored ahead of time in master plans (no skeleton lazy-creation; `master-plan-new` decomposes ALL Stages at author time).
- Tasks materialize lazily — the `_pending_` row stays in the master plan until `stage-file-plan` (Opus) writes the `§Stage File Plan` tuples and `stage-file-apply` (Sonnet) reserves ids + writes BACKLOG rows + spec stubs.
- Do NOT pre-create `ia/projects/{ISSUE_ID}.md` stubs for future Tasks — only `stage-file-apply` materializes them.

## Ephemeral spec lifecycle

Project specs are temporary scaffolding for one Issue lifecycle. After implementation:

- Migrate canonical knowledge → `ia/specs/glossary.md`, reference specs under `ia/specs/`, `ARCHITECTURE.md`, rules under `ia/rules/`, `docs/` (per `§Closeout Plan` tuples authored by `opus-audit`).
- Persist verbose Decision Log + Lessons Learned → Postgres journal (`project_spec_journal_persist`).
- Delete `ia/projects/{ISSUE_ID}.md`. Only canonical docs survive.

Each Task = exactly one `ia/projects/{ISSUE_ID}.md` spec. Multi-task umbrella specs are not allowed — file separate Tasks.

## Status enum

All specs + orchestrators use: **`Draft`** | **`In Review`** | **`In Progress`** | **`Final`**.

`In Progress` format includes active context: `In Progress — Stage 2.3 / TECH-42`.
