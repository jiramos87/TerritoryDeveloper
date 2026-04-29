# Mission

Run `ia/skills/stage-decompose/SKILL.md` end-to-end on target Stage. Expand the deferred skeleton Stage into full Task table + 2 pending subsections (§Stage File Plan + §Plan Fix). Do NOT create BACKLOG rows.

# Recipe

1. **Resolve inputs** — Parse from user prompt: **1st token = `SLUG`** (bare master-plan slug, e.g. `blip`); **2nd token = `STAGE_ID`** (`N.M`, e.g. `2.3`). Verify slug exists via `master_plan_state(slug=SLUG)`.
2. **Load + validate (Phase 0)** — Load Stage block via `stage_render(slug, stage_id)`. Confirm skeleton (no Task table). If already decomposed → STOP and report. Extract: Stage Name, Objectives, Exit criteria, Relevant surfaces, Art, Task hints. Scan prior Stage Exit + closeout rollup via `master_plan_render(slug)`.
3. **MCP context (Phase 1)** — Run Tool recipe (SKILL.md §Tool recipe). Greenfield = skip router/spec_sections/invariants_summary. Brownfield = full recipe. Surface-path pre-check via Glob after tool recipe.
4. **Task decomposition (Phase 2)** — 2–6 Tasks. Ordering: scaffolding → data model → runtime → integration+tests. 5-column Task table (no Phase column) with `_pending_` Issue + Status. Task intent must cite concrete types / methods / paths. Apply sizing heuristic (2–5 files = correct; ≤1 file = merge; >3 subsystems = split).
5. **Cardinality gate (Phase 3)** — ≥2 Tasks/Stage (hard), ≤6 soft. 1-Task → warn + pause. 7+ → warn + pause. Single-file Tasks → warn + pause. Proceed only after user confirms.
6. **Sizing-gate eval (Phase 3.5)** — H1–H6 per `ia/rules/stage-sizing-gate.md`. PASS / WARN-gate / FAIL outcomes.
7. **Persist (Phase 4)** — Call `stage_body_write({slug, stage_id, body})` with full Task table + 2 pending subsections (§Stage File Plan / §Plan Fix). Preserve `Status: Draft`. Tasks `_pending_`. Backlog state: 0 filed.
8. **Stage deps (Phase 4a)** — If Stage has `depends_on[]` (sibling/intra-plan stage refs `slug/stage_id`), write to `ia_stages.depends_on text[]` via TECH-3225 col same txn as stage row commit. Cycle-check trigger raises `cycle_detected: <path>` on self-loop / multi-node cycle → STOP + surface error. Empty `[]` (no deps) is valid. Cross-slug refs accepted (resolved at read time by `master_plan_next_actionable` MCP tool).
9. **Progress regen (Phase 5)** — `npm run progress`. Log exit; non-zero does NOT block.
10. **Handoff (Phase 6)** — Report: Stage {STAGE_ID} decomposed (N Tasks). Invariants flagged. Cardinality + sizing gate outcomes. Next: `/stage-file {SLUG} Stage {STAGE_ID}` when prior Stage closes.

# Hard boundaries

- Do NOT decompose Stages beyond target — lazy materialization.
- Do NOT create BACKLOG rows or task spec stubs — `stage-file` does that.
- Do NOT overwrite a decomposed Stage without explicit user confirmation.
- Do NOT persist if Task count <2 without user confirmation.
- Do NOT commit — user decides.

# Output

Single caveman message: Stage {STAGE_ID} decomposed (N Tasks, all `_pending_`), cardinality + sizing gate outcomes, next step.
