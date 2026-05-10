---
name: stage-file-main-session
purpose: >-
  Main-session adapter for /stage-file: executes the full stage-file → stage-authoring → plan-review
  chain inline (no subagents). Use when the caller agent (Cursor Composer-2 / Claude Code main
  session) must do the work itself rather than dispatch via Agent/Task tool.
audience: agent
loaded_by: "skill:stage-file-main-session"
slices_via: none
description: >-
  In-session (no-subagent) wrapper around the /stage-file chain. Read ia/skills/stage-file/SKILL.md
  (DB-backed single-skill) and the phase list in .claude/commands/stage-file.md, then execute the
  chain inline: stage-file → stage-authoring → plan-review → STOP at plan-review PASS. Use MCP
  `task_insert` (DB-backed per-prefix id; NO reserve-id.sh / NO yaml writes), manifest append, and
  direct file edits. Never dispatch via Agent/Task tool. Triggers: "/stage-file-main-session
  {master-plan-path} {stage}", "execute stage-file in this session", "no-subagent stage-file".
  Argument order (explicit): MASTER_PLAN_SLUG first, STAGE_ID second.
  Triggers replaced master-plan path arg with bare slug — DB-primary (master plans live in
  `ia_master_plans`; no filesystem `.md`).
phases:
  - Load canonical skill + command
  - Execute chain inline
  - STOP at plan-review PASS
triggers:
  - /stage-file-main-session {master-plan-path} {stage}
  - execute stage-file in this session
  - no-subagent stage-file
argument_hint: {MASTER_PLAN_SLUG} {STAGE_ID}
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# stage-file-main-session — no-subagent `/stage-file`

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** same outcomes as `/stage-file`, executed inline by the current session. No Agent/Task dispatch.

## Arguments

- `$1` / `{MASTER_PLAN_SLUG}` — bare slug of the DB-backed master plan (e.g. `web-platform`). Resolves via `mcp__territory-ia__master_plan_render({slug})`. No filesystem `.md` master-plan file.
- `$2` / `{STAGE_ID}` — stage identifier (e.g. `7.2`).

Missing either → print usage + abort: `/stage-file-main-session {MASTER_PLAN_SLUG} {STAGE_ID}`.

## Instructions

1. **Load canonical sources end-to-end:**
   - `ia/skills/stage-file/SKILL.md` (DB-backed single-skill — 8 phases)
   - `.claude/commands/stage-file.md` (canonical chain: stage-file → stage-authoring → plan-review → STOP)

2. **Execute the full chain inline** for `{MASTER_PLAN_SLUG}` Stage `{STAGE_ID}`:
   - Step 1 — `stage-file` work (8 phases): Mode detection → `lifecycle_stage_context` once → Stage block + cardinality + sizing gates → Batch Depends-on verify via single `backlog_list` → Resolve target BACKLOG manifest section → Per-task `task_insert` MCP (DB-backed per-prefix id; NO reserve-id.sh; NO yaml) + `task_spec_section_write` spec stub (DB-backed; NO `ia/projects/` write) + manifest append (`ia/state/backlog-sections.json`) → Post-loop `cron_materialize_backlog_enqueue` + `npm run validate:dead-project-specs` (NO `validate:backlog-yaml` on DB path) + atomic task-table flip + R2 Stage Status flip + R1 plan-top Status flip.
   - Step 2 — `stage-authoring` bulk Stage 1×N (one Opus pass writes §Plan Digest direct per task via `task_spec_section_write` MCP; self-lints via `plan_digest_lint` cap=1).
   - Step 3 — `plan-review`: PASS → Step 4; critical → `plan-applier` Mode plan-fix → re-review (cap=1); second critical → abort.
   - Step 4 — STOP at plan-review PASS. Do NOT auto-chain to `/ship-stage`.

3. **Tooling:**
   - territory-ia MCP: `lifecycle_stage_context`, `backlog_list`, `task_insert`, `backlog_record_validate`, `plan_digest_compile_stage_doc`, `plan_digest_lint`, etc.
   - `task_insert` MCP owns id assignment (per-prefix DB sequence). Do NOT call `reserve-id.sh` or `reserve_backlog_ids` on DB path.
   - Direct file edits: manifest `ia/state/backlog-sections.json` only. Spec stubs via `task_spec_section_write` MCP (DB; NO `ia/projects/` writes). Master-plan task table updates flow through `task_insert` + `master_plan_render` (DB rows), NOT a filesystem `.md`. NO yaml under `ia/backlog/`.

4. **Hard boundaries (from `.claude/commands/stage-file.md` — apply inline):**
   - No Agent/Task dispatch for any chain step.
   - No yaml writes under `ia/backlog/` — DB is source of truth.
   - No `reserve-id.sh` invocations — `task_insert` MCP assigns ids.
   - Declared task-table order iterator — never re-order.
   - Atomic task-table flip after all writes — never mid-loop.
   - Filing gate is `validate:dead-project-specs` only on DB path — no `validate:backlog-yaml`, no `validate:all`.
   - Never hand-edit `ia/state/id-counter.json` or `BACKLOG.md`.
   - Idempotent on re-entry.
   - No auto-commit — user decides.

## Exit

Emit the standard `/stage-file` completion summary (tasks filed ids, N specs with populated `§Plan Digest`, plan-review PASS, validators ok) + next-step proposal:
- **N≥2:** `Next: /ship-stage-main-session {MASTER_PLAN_SLUG} {STAGE_ID}` (main-session chain) or `/ship-stage {MASTER_PLAN_SLUG} Stage {STAGE_ID}` (subagent chain).
- **N=1:** `Next: /ship {ISSUE_ID}`.
