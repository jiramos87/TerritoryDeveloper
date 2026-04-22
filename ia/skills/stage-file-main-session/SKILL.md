---
purpose: "Main-session adapter for /stage-file: executes the full planner → applier → plan-author → plan-digest → plan-reviewer chain inline (no subagents). Use when the caller agent (Cursor Composer-2 / Claude Code main session) must do the work itself rather than dispatch via Agent/Task tool."
audience: agent
loaded_by: skill:stage-file-main-session
slices_via: none
name: stage-file-main-session
description: >
  In-session (no-subagent) wrapper around the /stage-file chain. Read
  ia/skills/stage-file/SKILL.md (+ stage-file-plan, stage-file-apply) and the
  phase list in .claude/commands/stage-file.md, then execute the chain
  inline: planner → applier → plan-author → plan-digest → plan-reviewer →
  STOP at plan-review PASS. Use MCP, reserve-id, and direct file edits.
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
   - `ia/skills/stage-file/SKILL.md` (dispatcher shim — mode detection + routing)
   - `ia/skills/stage-file-plan/SKILL.md` (planner pair-head)
   - `ia/skills/stage-file-apply/SKILL.md` (applier pair-tail)
   - `.claude/commands/stage-file.md` (canonical chain: planner → applier → plan-author → plan-digest → plan-reviewer → STOP)

2. **Execute the full chain inline** for `{MASTER_PLAN_RELATIVE_PATH}` Stage `{STAGE_ID}`:
   - Step 1 — planner work (Opus pair-head): `domain-context-load` once, cardinality gate, batch-verify Depends-on/Related via `backlog_issue`, batch-reserve ids via `reserve_backlog_ids`, emit `§Stage File Plan` tuples.
   - Step 2 — applier work (pair-tail): loop tuples in declared order, compose yaml, `backlog_record_validate`, write `ia/backlog/{id}.yaml`, bootstrap `ia/projects/{id}.md` stubs. Post-loop: `bash tools/scripts/materialize-backlog.sh` + `npm run validate:dead-project-specs` + `npm run validate:backlog-yaml`. Atomic task-table flip `_pending_` → `{id}` + `Draft`.
   - Step 3 — `plan-author` bulk Stage 1×N (populate `§Plan Author` for all N specs).
   - Step 4 — `plan-digest` bulk Stage 1×N (mechanize `§Plan Digest` + drop `§Plan Author` + compile aggregate doc + `plan_digest_lint` cap=1).
   - Step 5 — `plan-reviewer`: PASS → Step 6; critical → `plan-applier` Mode plan-fix → re-review (cap=1); second critical → abort.
   - Step 6 — STOP at plan-review PASS. Do NOT auto-chain to `/ship-stage`.

3. **Tooling:**
   - territory-ia MCP: `domain-context-load`, `backlog_issue`, `reserve_backlog_ids`, `backlog_record_validate`, `plan_digest_compile_stage_doc`, `plan_digest_lint`, etc.
   - `bash tools/scripts/reserve-id.sh {PREFIX} {count}` when MCP reservation is unavailable.
   - Direct file edits (yaml, spec stubs, master-plan task table).

4. **Hard boundaries (from `.claude/commands/stage-file.md` — apply inline):**
   - No Agent/Task dispatch for any chain step.
   - Batched id reservation only — never per-task.
   - Declared-order tuple loop — never re-order.
   - Atomic task-table flip after all writes — never mid-loop.
   - Seam #2 gate is `validate:dead-project-specs` + `validate:backlog-yaml` only — no `validate:all`.
   - Never hand-edit `ia/state/id-counter.json` or `BACKLOG.md`.
   - Idempotent on re-entry.
   - No auto-commit — user decides.

## Exit

Emit the standard `/stage-file` completion summary (tasks filed ids, N specs with populated `§Plan Digest`, plan-review PASS, validators ok) + next-step proposal:
- **N≥2:** `Next: /ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID}` (main-session chain) or `/ship-stage {MASTER_PLAN_RELATIVE_PATH} Stage {STAGE_ID}` (subagent chain).
- **N=1:** `Next: /ship {ISSUE_ID}`.
