---
description: Bulk-file all pending tasks of one orchestrator Stage as BACKLOG issues + project spec stubs. Dispatches `stage-file-planner` (Opus pair-head seam #2) → `stage-file-applier` (Sonnet pair-tail). STOPS at applier tail — no auto-chain to `/author`. Applier suggests `/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID}` for N≥2 or `/ship {ISSUE_ID}` for N=1; user invokes the chain dispatcher next. Seam #2 pair split per T7.7 / TECH-474; chain boundary per T8 dry-run F1 / Row 3 (Option B).
argument-hint: "{master-plan-path} Stage {X.Y}"
---

# /stage-file — dispatch seam #2 pair (planner → applier)

Use `stage-file-planner` subagent (`.claude/agents/stage-file-planner.md`) → `stage-file-applier` subagent (`.claude/agents/stage-file-applier.md`) to bulk-file all `_pending_` tasks for `$ARGUMENTS`. Chain STOPS at applier tail; user invokes `/ship-stage` (or `/ship` N=1) next — `/ship-stage` is THE chain dispatcher that internally runs author → implement → verify-loop → code-review → audit → closeout.

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{MASTER_PLAN_PATH}` (repo-relative, `ia/projects/*-master-plan.md`). Second token = `{STAGE_ID}` (e.g. `Stage 7.2` → `7.2`). Missing either → print usage + abort.

## Step 1 — Dispatch `stage-file-planner` (Opus pair-head)

Forward via Agent tool with `subagent_type: "stage-file-planner"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-file-plan/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Load shared Stage MCP bundle once (`domain-context-load`). Read Stage block + cardinality gate (≥2 tasks per phase — single-task phase → warn + pause). Batch-verify every Depends-on / Related id via `backlog_issue`. Batch-reserve ids via `reserve_backlog_ids` (monotonic per prefix). Emit `§Stage File Plan` tuple list under Stage block (one tuple per task: `{operation: file_task, reserved_id, title, priority, issue_type, notes, depends_on, related, stub_body}`). Resolve every anchor to single match before emitting. Hand off to Sonnet pair-tail.
>
> ## Hard boundaries
>
> - Do NOT reserve ids per-task — batch via `reserve_backlog_ids` only.
> - Do NOT write yaml / spec stubs / edit master plan — that is pair-tail.
> - Do NOT run validators — applier runs gate.
> - Do NOT file tasks outside target Stage.
> - Do NOT pre-file for Steps whose Status is not `In Progress`.
> - Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT commit — user decides.

Planner must return success + `§Stage File Plan` written before Step 2. Escalation → abort chain.

## Step 2 — Dispatch `stage-file-applier` (Sonnet pair-tail)

Forward via Agent tool with `subagent_type: "stage-file-applier"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-file-apply/SKILL.md` end-to-end on `{MASTER_PLAN_PATH}` `{STAGE_ID}`. Read `§Stage File Plan` tuples verbatim. Loop tuples in declared order: compose yaml, `backlog_record_validate`, write `ia/backlog/{reserved_id}.yaml`, bootstrap `ia/projects/{reserved_id}.md` from template. Post-loop: `bash tools/scripts/materialize-backlog.sh` + `npm run validate:dead-project-specs` + `npm run validate:backlog-yaml` once. Atomic Edit pass on orchestrator task table flips `_pending_` → `{reserved_id}` + `Draft`. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-query MCP for Depends-on — planner batch-verified.
> - Do NOT re-reserve ids — planner reserved via `reserve_backlog_ids`.
> - Do NOT re-order tuples — declared order only.
> - Do NOT write normative spec prose beyond stub — `plan-author` writes spec body at Stage N×1.
> - Do NOT edit `BACKLOG.md` directly — `materialize-backlog.sh` regenerates it.
> - Do NOT run `validate:all` — seam #2 gate is `validate:dead-project-specs` + `validate:backlog-yaml` only.
> - Do NOT update task table mid-loop — atomic pass after all writes.
> - Do NOT commit — user decides.

## Step 3 — Boundary stop (NO auto-chain)

Per T8 dry-run F1 / Row 3 decision (Option B): `/stage-file` STOPS at applier tail. Do NOT auto-invoke `/author`. The chain dispatcher `/ship-stage` (recommended by applier handoff) owns the author → implement → verify-loop → code-review → audit → closeout chain.

Rationale: avoids two competing auto-chains (here vs `/ship-stage`); user gets one clear handoff per the applier's suggestion; user can intervene between filing and shipping if needed (e.g. inspect specs first).

## Output

Applier handoff summary: tasks filed ids + validators ok + next-step proposal. `validate:all` NOT run in seam #2 gate — full chain runs at Stage closeout. Applier emits exactly:

- **N≥2:** `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID}"` (chain dispatcher = author + implement + verify-loop + code-review + audit + closeout).
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` (single-task chain dispatcher, same surfaces).

Hard rule: NEVER suggest `/author` standalone after `/stage-file` — folded into ship chain.
