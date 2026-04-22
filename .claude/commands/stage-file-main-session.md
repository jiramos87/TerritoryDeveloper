---
description: No-subagent variant of /stage-file. Executes the planner → applier → plan-author → plan-digest → plan-reviewer chain inline in the current Claude Code session (no Agent/Task dispatch). Wraps ia/skills/stage-file-main-session.
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

- `ia/skills/stage-file/SKILL.md`
- `ia/skills/stage-file-plan/SKILL.md`
- `ia/skills/stage-file-apply/SKILL.md`
- `.claude/commands/stage-file.md` (for the canonical chain + hard boundaries)

## Step 2 — Execute the chain inline

Perform every step from `.claude/commands/stage-file.md` **yourself**, in this session, using territory-ia MCP + `bash tools/scripts/reserve-id.sh` + direct file edits. Do **not** dispatch any subagent (`stage-file-planner`, `stage-file-applier`, `plan-author`, `plan-digest`, `plan-reviewer`, `plan-applier`).

Chain:

1. Planner work — `domain-context-load`, cardinality gate, batch-verify Depends-on/Related, batch-reserve ids, emit `§Stage File Plan` tuples.
2. Applier work — loop tuples in declared order, compose yaml, `backlog_record_validate`, write `ia/backlog/{id}.yaml`, bootstrap `ia/projects/{id}.md`. Post-loop: `bash tools/scripts/materialize-backlog.sh` + `npm run validate:dead-project-specs` + `npm run validate:backlog-yaml`. Atomic task-table flip.
3. `plan-author` bulk Stage 1×N.
4. `plan-digest` bulk Stage 1×N + aggregate doc + `plan_digest_lint` (cap=1).
5. `plan-reviewer`: PASS → STOP; critical → `plan-applier` Mode plan-fix → re-review (cap=1); second critical → abort.
6. STOP at plan-review PASS. Do NOT auto-chain to `/ship-stage`.

Apply every hard boundary from `.claude/commands/stage-file.md` (batched id reservation, declared-order, atomic flip, no `validate:all`, no hand-edit `id-counter.json` / `BACKLOG.md`, idempotent, no auto-commit).

## Step 3 — Output

Emit the standard chain completion summary: tasks filed ids, N specs with populated `§Plan Digest`, plan-review PASS, validators ok + next-step handoff:

- **N≥2:** `Next: /ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID}` (main-session chain) or `/ship-stage {MASTER_PLAN_RELATIVE_PATH} Stage {STAGE_ID}` (subagent chain).
- **N=1:** `Next: /ship {ISSUE_ID}`.
