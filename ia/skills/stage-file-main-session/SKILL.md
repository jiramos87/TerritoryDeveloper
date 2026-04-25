---
purpose: "Main-session adapter for /stage-file: executes the full planner → applier → plan-author → plan-digest → plan-reviewer chain inline (no subagents). Use when the caller agent (Cursor Composer-2 / Claude Code main session) must do the work itself rather than dispatch via Agent/Task tool."
audience: agent
loaded_by: skill:stage-file-main-session
slices_via: none
name: stage-file-main-session
description: >
  In-session (no-subagent) wrapper around the /stage-file chain. Read
  ia/skills/stage-file/SKILL.md (merged DB-backed single-skill — replaces
  retired stage-file-plan + stage-file-apply pair since Step 6 of
  docs/ia-dev-db-refactor-implementation.md, 2026-04-24) and the phase list
  in .claude/commands/stage-file.md, then execute the chain inline:
  stage-file → plan-author → plan-digest → plan-reviewer → STOP at
  plan-review PASS. Use MCP `task_insert` (DB-backed per-prefix id; NO
  reserve-id.sh / NO yaml writes), manifest append, and direct file edits.
  Never dispatch via Agent/Task tool.
  Triggers: "/stage-file-main-session {master-plan-path} {stage}",
  "execute stage-file in this session", "no-subagent stage-file".
  Argument order (explicit): MASTER_PLAN_RELATIVE_PATH first, STAGE_ID second.
model: inherit
phases:
  - "Load canonical skill + command"
  - "Execute chain inline"
  - "STOP at plan-review PASS"
---

# stage-file-main-session — no-subagent `/stage-file`

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** same outcomes as `/stage-file`, executed inline by the current session. No Agent/Task dispatch.

## Arguments

- `$1` / `{MASTER_PLAN_RELATIVE_PATH}` — path to the master plan `.md`, relative to the territory-developer repo root (e.g. `ia/projects/web-platform-master-plan.md`).
- `$2` / `{STAGE_ID}` — stage identifier (e.g. `7.2`).

Missing either → print usage + abort: `/stage-file-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID}`.

## Instructions

1. **Load canonical sources end-to-end:**
   - `ia/skills/stage-file/SKILL.md` (merged DB-backed single-skill — 8 phases, replaces retired `-plan` + `-apply` pair)
   - `.claude/commands/stage-file.md` (canonical chain: stage-file → plan-author → plan-digest → plan-reviewer → STOP)

   Retired pair body archived at `ia/skills/_retired/stage-file-plan/SKILL.md` + `ia/skills/_retired/stage-file-apply/SKILL.md` — do not load unless debugging Step 6 rollback.

2. **Execute the full chain inline** for `{MASTER_PLAN_RELATIVE_PATH}` Stage `{STAGE_ID}`:
   - Step 1 — `stage-file` work (8 phases): Mode detection → `lifecycle_stage_context` once → Stage block + cardinality + sizing gates → Batch Depends-on verify via single `backlog_list` → Resolve target BACKLOG manifest section → Per-task `task_insert` MCP (DB-backed per-prefix id; NO reserve-id.sh; NO yaml) + manifest append (`ia/state/backlog-sections.json`) + `ia/projects/{ISSUE_ID}.md` spec stub from template → Post-loop `bash tools/scripts/materialize-backlog.sh` + `npm run validate:dead-project-specs` (NO `validate:backlog-yaml` on DB path) + atomic task-table flip + R2 Stage Status flip + R1 plan-top Status flip.
   - Step 2 — `plan-author` bulk Stage 1×N (populate `§Plan Author` for all N specs).
   - Step 3 — `plan-digest` bulk Stage 1×N (mechanize `§Plan Digest` + drop `§Plan Author` + compile aggregate doc + `plan_digest_lint` cap=1).
   - Step 4 — `plan-reviewer`: PASS → Step 5; critical → `plan-applier` Mode plan-fix → re-review (cap=1); second critical → abort.
   - Step 5 — STOP at plan-review PASS. Do NOT auto-chain to `/ship-stage`.
   - **Branch guardrail:** on `feature/ia-dev-db-refactor` the chain stops after Step 1 (Steps 2–4 skipped per `docs/ia-dev-db-refactor-implementation.md §3`).

3. **Tooling:**
   - territory-ia MCP: `lifecycle_stage_context`, `backlog_list`, `task_insert`, `backlog_record_validate`, `plan_digest_compile_stage_doc`, `plan_digest_lint`, etc.
   - `task_insert` MCP owns id assignment (per-prefix DB sequence). Do NOT call `reserve-id.sh` or `reserve_backlog_ids` on DB path.
   - Direct file edits (manifest `ia/state/backlog-sections.json`, spec stubs under `ia/projects/`, master-plan task table). NO yaml under `ia/backlog/`.

4. **Hard boundaries (from `.claude/commands/stage-file.md` — apply inline):**
   - No Agent/Task dispatch for any chain step.
   - No yaml writes under `ia/backlog/` — DB is source of truth (Step 6 of ia-dev-db-refactor).
   - No `reserve-id.sh` invocations — `task_insert` MCP assigns ids.
   - Declared task-table order iterator — never re-order.
   - Atomic task-table flip after all writes — never mid-loop.
   - Seam #2 gate is `validate:dead-project-specs` only on DB path — no `validate:backlog-yaml`, no `validate:all`.
   - Never hand-edit `ia/state/id-counter.json` or `BACKLOG.md`.
   - Idempotent on re-entry.
   - No auto-commit — user decides.

## Exit

Emit the standard `/stage-file` completion summary (tasks filed ids, N specs with populated `§Plan Digest`, plan-review PASS, validators ok) + next-step proposal:
- **N≥2:** `Next: /ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID}` (main-session chain) or `/ship-stage {MASTER_PLAN_RELATIVE_PATH} Stage {STAGE_ID}` (subagent chain).
- **N=1:** `Next: /ship {ISSUE_ID}`.
