---
description: No-subagent variant of /stage-file. Executes the stage-file (merged DB-backed single-skill) → plan-author → plan-digest → plan-reviewer chain inline in the current Claude Code session (no Agent/Task dispatch). Wraps ia/skills/stage-file-main-session. Step 6 of ia-dev-db-refactor (2026-04-24) retired the -plan/-apply pair into the single stage-file skill.
argument-hint: "{MASTER_PLAN_RELATIVE_PATH} {STAGE_ID}"
---

# /stage-file-main-session — no-subagent `/stage-file`

Execute the full `/stage-file` chain for `$ARGUMENTS` **inline in this session**. Do **not** use the Agent/Task tool for any step.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Argument parsing

Split `$ARGUMENTS` on whitespace:

- `MASTER_PLAN_RELATIVE_PATH` = first token (path to master plan `.md`, relative to repo root, e.g. `ia/projects/web-platform-master-plan.md`).
- `STAGE_ID` = second token (e.g. `7.2`).

Missing either → print usage and abort:

```
/stage-file-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID}
```

Verify `{MASTER_PLAN_RELATIVE_PATH}` exists (Glob). Extract plan display name from filename. Print context banner:

```
STAGE-FILE (main-session) {STAGE_ID} — {plan display name}
  master plan : {MASTER_PLAN_RELATIVE_PATH}
  stage       : {STAGE_ID}
  mode        : in-session (no subagents)
```

## Step 1 — Load the wrapper skill

Read `ia/skills/stage-file-main-session/SKILL.md` end-to-end. Then read the canonical sources it references:

- `ia/skills/stage-file/SKILL.md` (merged DB-backed single-skill — replaces retired `-plan` + `-apply` pair; 8 phases)
- `.claude/commands/stage-file.md` (for the canonical chain + hard boundaries)

Retired pair body archived at `ia/skills/_retired/stage-file-plan/SKILL.md` + `ia/skills/_retired/stage-file-apply/SKILL.md` — do not load unless debugging Step 6 rollback.

## Step 2 — Execute the chain inline

Perform every step from `.claude/commands/stage-file.md` **yourself**, in this session, using territory-ia MCP (`lifecycle_stage_context`, `backlog_list`, `task_insert`, `backlog_record_validate`) + direct file edits (manifest `ia/state/backlog-sections.json` + spec stubs under `ia/projects/` + master-plan task table). Do **not** write yaml under `ia/backlog/`, do **not** call `reserve-id.sh`, do **not** dispatch any subagent (`stage-file`, `plan-author`, `plan-digest`, `plan-reviewer`, `plan-applier`).

Chain:

1. `stage-file` work (8 phases): Mode detection → `lifecycle_stage_context` once → Stage block + cardinality + sizing gates → Batch Depends-on verify via single `backlog_list` → Resolve target BACKLOG manifest section (slug heuristic / user prompt) → Per-task `task_insert` MCP (DB-backed per-prefix id; NO reserve-id.sh; NO yaml) + manifest append + spec stub from template → Post-loop `bash tools/scripts/materialize-backlog.sh` + `npm run validate:dead-project-specs` (NO `validate:backlog-yaml` on DB path) + atomic task-table flip + R2 Stage Status flip + R1 plan-top Status flip.
2. `plan-author` bulk Stage 1×N.
3. `plan-digest` bulk Stage 1×N + aggregate doc + `plan_digest_lint` (cap=1).
4. `plan-reviewer`: PASS → STOP; critical → `plan-applier` Mode plan-fix → re-review (cap=1); second critical → abort.
5. STOP at plan-review PASS. Do NOT auto-chain to `/ship-stage`.
6. **Branch guardrail:** on `feature/ia-dev-db-refactor` the chain stops after Step 1 (Steps 2–4 skipped per `docs/ia-dev-db-refactor-implementation.md §3`).

Apply every hard boundary from `.claude/commands/stage-file.md` (DB-only writes, atomic flip after all writes, no `validate:backlog-yaml`, no `validate:all`, no hand-edit `id-counter.json` / `BACKLOG.md`, idempotent, no auto-commit).

## Step 3 — Output

Emit the standard chain completion summary: tasks filed ids, N specs with populated `§Plan Digest`, plan-review PASS, validators ok + next-step handoff:

- **N≥2:** `Next: /ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID}` (main-session chain) or `/ship-stage {MASTER_PLAN_RELATIVE_PATH} Stage {STAGE_ID}` (subagent chain).
- **N=1:** `Next: /ship {ISSUE_ID}`.
