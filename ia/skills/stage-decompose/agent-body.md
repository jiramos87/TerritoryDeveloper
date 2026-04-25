# Mission

Run `ia/skills/stage-decompose/SKILL.md` end-to-end for the target step. Expand the deferred skeleton step into full stages → phases → tasks. Edit the orchestrator master plan in-place. Do NOT create BACKLOG rows.

# Recipe

1. **Resolve inputs** — Parse from user prompt: **1st token = `ORCHESTRATOR_SPEC`** (explicit path, e.g. `ia/projects/blip-master-plan.md`); **2nd token = `STEP_ID`** (integer or `Step N`, e.g. `2`). Glob-resolve only when path is omitted AND exactly one `*-master-plan.md` exists — otherwise ask. STEP_ID must be ≥ 2.
2. **Load + validate (Phase 0)** — Read orchestrator; find `### Step {STEP_ID}` block. Confirm skeleton (no task table). If already decomposed → STOP and report. Extract: Step Name, Objectives, Exit criteria, Relevant surfaces, Art, Stage hints from `## Deferred decomposition`. Hold prior step Exit in working memory.
3. **MCP context (Phase 1)** — Run Tool recipe (SKILL.md §Tool recipe). Greenfield = skip router/spec_sections/invariants_summary. Brownfield = full recipe. Surface-path pre-check via Glob after tool recipe.
4. **Stage decomposition (Phase 2)** — 2–4 stages. Ordering: scaffolding → data model → runtime → integration+tests. Per stage: Objectives + Exit + Phases + Tasks table. Task intent must cite concrete types / methods / paths. Apply task sizing heuristic (2–5 files = correct; ≤1 file = merge; >3 subsystems = split).
5. **Cardinality gate (Phase 3)** — ≥2 tasks per phase, ≤6 soft. 1-task phase → warn + pause. 7+ → warn + pause. Single-file tasks → warn + pause. Proceed only after user confirms.
6. **Persist (Phase 4)** — Three edits in one pass:
   a. Replace skeleton step block with full decomposition (Status: Draft, tasks `_pending_`, Backlog state: 0 filed, Relevant surfaces expanded).
   b. Update `## Deferred decomposition` bullet for this step: `decomposed {date}. Stages: {names}`.
   c. Update master plan header Status line if it references this step's state.
7. **Progress regen (Phase 5)** — `npm run progress`. Log exit; non-zero does NOT block.
8. **Handoff (Phase 6)** — Report: N stages · M phases · K tasks decomposed. Invariants flagged. Deferred section updated. Next: `/stage-file {spec} Stage {STEP_ID}.1` when Step {STEP_ID-1} closes.

# Hard boundaries

- Do NOT decompose Step 1 — master-plan-new owns that.
- Do NOT create BACKLOG rows or `ia/projects/{ISSUE_ID}.md` stubs — stage-file does that.
- Do NOT decompose steps beyond `STEP_ID` — lazy materialization.
- Do NOT overwrite a step with an existing task table without explicit user confirmation.
- Do NOT persist if any phase has <2 tasks without user confirmation.
- Do NOT commit — user decides.

# Output

Single caveman message: orchestrator edited, step N decomposed (N stages · M phases · K tasks, all `_pending_`), cardinality gate outcome, deferred section updated, next step.
