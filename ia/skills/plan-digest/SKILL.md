---
purpose: "Mechanizes §Plan Author into §Plan Digest per-Task and compiles an aggregate Stage doc. 9-point rubric enforced externally via plan_digest_lint MCP tool."
audience: agent
loaded_by: skill:plan-digest
slices_via: none
name: plan-digest
description: >
  Opus Stage-scoped bulk non-pair stage. Runs AFTER plan-author (populated
  §Plan Author per spec) and BEFORE plan-review. Reads all N §Plan Author
  sections + current repo state via MCP; writes per-spec §Plan Digest
  (rich format: mechanical steps + gates + acceptance + test blueprint +
  glossary refs + examples + STOP + implementer MCP-tool hints) that
  SURVIVES in the final spec (§Plan Author is ephemeral and dropped).
  Compiles aggregate doc at docs/implementation/{slug}-stage-{STAGE_ID}-plan.md
  via plan_digest_compile_stage_doc. Self-lints via plan_digest_lint (cap=1 retry;
  second fail escalates to user). Two modes: `stage` (live) + `audit` (scaffold,
  flag-gated). Always-on: every executor class benefits from the mechanical form.
  Triggers: "/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}", "digest stage plan",
  "compile stage implementation doc", auto-dispatched from /stage-file Step 4.
model: inherit
phases:
  - "Load Stage + §Plan Author slices"
  - "Mechanize per-Task §Plan Digest"
  - "Compile aggregate stage doc"
  - "Self-lint via plan_digest_lint"
  - "Hand-off"
---

# Plan-digest skill (Opus Stage-scoped bulk, non-pair)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Mechanize §Plan Author into §Plan Digest across N Task specs of one Stage in one Opus pass. §Plan Author is ephemeral — this skill transforms + replaces it with §Plan Digest, which is the canonical section surviving in the committed spec (Q5).

**Contract:** [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 9-point rubric, enforced by `plan_digest_lint`.

**Upstream:** `plan-author` (writes §Plan Author — ephemeral intermediate). **Downstream:** `plan-review` (scans final §Plan Digest for semantic drift). Chain anchor: `.claude/commands/stage-file.md` Step 4 (between plan-author and plan-reviewer).

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path. |
| `STAGE_ID` | 2nd arg | e.g. `7.1`. |
| `--mode` | optional | `stage` (default) or `audit` (requires `PLAN_DIGEST_AUDIT_MODE=1`). |
| `--task {ISSUE_ID}` | optional | Single-spec re-digest. |

## Phase 1 — Load Stage + §Plan Author slices

1. Read master-plan Stage block; collect Task ids (Status ∈ {Draft, In Review, In Progress}).
2. For each Task: read `ia/projects/{ISSUE_ID}.md`. Require `## §Plan Author` populated with all 4 sub-headings (§Audit Notes / §Examples / §Test Blueprint / §Acceptance). Missing → abort chain with `STOPPED — plan-author not populated for {ISSUE_ID}`.
3. Load glossary terms + invariants + router domains via shared Stage MCP bundle (do NOT re-call `domain-context-load` — orchestrator provided it).

## Phase 2 — Mechanize per-Task §Plan Digest

For each Task:

1. Translate §Plan Author narrative into a sequential checklist of **Edit** tuples, each with `(operation, target_path, before_string, after_string)`. Use `plan_digest_verify_paths` to confirm every target exists; use `plan_digest_resolve_anchor` to confirm every `before_string` is unique.
2. Render exact literals for code blocks via `plan_digest_render_literal` when the digest must quote a file literally.
3. For each step, ask `plan_digest_gate_author_helper({operation, file, before, after})` for the canonical gate command + expectation; embed verbatim.
4. Author STOP clause per step (what edit to re-open, or which upstream surface to escalate to).
5. Author Implementer MCP-tool hints per step (subset of `backlog_issue`, `glossary_lookup`, `invariant_preflight`, `plan_digest_resolve_anchor`, `unity_bridge_command`, etc.) — mechanical list, not narrative.
6. **Scene Wiring step (mandatory when §Plan Author §Scene Wiring populated):** if §Plan Author carries a `§Scene Wiring` sub-section (trigger fired per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md)), emit a dedicated **Scene Wiring** mechanical step in §Plan Digest §Mechanical Steps. Shape:
   - **Goal:** wire `{ComponentName}` into `Assets/Scenes/{SCENE}.unity` under `{parent_object}` with all `[SerializeField]` fields populated per §Scene Wiring.
   - **Edits:** prefer `unity_bridge_command` kinds in sequence `open_scene → create_gameobject → set_gameobject_parent → attach_component → assign_serialized_field → save_scene`. Text-edit fallback only when bridge unavailable — include verbatim YAML before/after blocks for the `.unity` hunk.
   - **Gate:** `npm run unity:compile-check` exits 0.
   - **STOP:** scene file edit must appear in `git diff`; if absent after gate → re-open the Scene Wiring step, do NOT close the Task.
   - **MCP hints:** `unity_bridge_command` (preferred), `find_gameobject` to confirm parent, `get_compilation_status` as compile gate when the Editor holds the project lock.
   - **Evidence (required verbatim in `after:` literal or §Acceptance):** scene/parent/component/serialized_fields/unity_events/compile_check block per the scene-wiring rule.
   Place this step LAST in §Mechanical Steps (after all script + test edits, before closeout) so the gate runs against the final runtime surface.
7. Write one `## §Plan Digest` section per spec under anchor **between §10 Lessons Learned and §Open Questions** (replaces §Plan Author — delete the §Plan Author block in the same write pass; Q5). Shape mirrors the template `ia/templates/plan-digest-section.md`.

## Phase 3 — Compile aggregate stage doc

Call `plan_digest_compile_stage_doc({master_plan_path, stage_id, task_spec_paths, mode})`. Output written to `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md`. Path A (ship-stage) does not consume this doc; Path B (external / composer-2 / cursor) does.

## Phase 4 — Self-lint via plan_digest_lint

For each per-Task §Plan Digest slice AND the aggregate stage doc:

1. Call `plan_digest_lint({content})`. `pass: true` → continue.
2. `pass: false` → revise failing tuples in-place; re-run lint once. Second failure → abort chain with `STOPPED — plan-digest lint critical twice`; surface first 5 failures verbatim.

Retry cap = 1.

## Phase 5 — Hand-off

Emit caveman summary: N specs digested; aggregate doc path; lint pass status. Next: `/plan-review {MASTER_PLAN_PATH} {STAGE_ID}` (multi-task) OR `claude-personal "/ship {ISSUE_ID}"` (N=1 — same digest gate as `ship` Step 1.5; `/ship` includes implement. Skip standalone `/implement` unless explicitly desired.)

## Hard boundaries

- Do NOT write code. Do NOT flip Task Status. Do NOT commit.
- Do NOT author §Plan Author (upstream). Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + `/author` handoff.
- Do NOT regress to per-Task mode if tokens exceed threshold — split into ⌈N/2⌉ bulk sub-passes.
- Mode `audit` stays flag-gated; no chain dispatches it.
- Do NOT skip the Scene Wiring mechanical step when §Plan Author carries `§Scene Wiring` — wiring is a Stage deliverable per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md); dropping the step lets Stages ship dead runtime paths (grid-asset-visual-registry 2.2 canonical incident).
