---
description: Standalone single-task ship pipeline. Four mechanical steps in order: (1) author §Plan Digest via stage-authoring --task, (2) implement via spec-implementer, (3) verify-loop with MAX_ITERATIONS=2, (4) close via DB status walk (pending → implemented → verified → done → archived). Standalone-tasks only — task must have master_plan_id IS NULL. No code review. No audit. No commit. No master-plan handoff. Stage-attached tasks must use /ship-stage instead. Triggers: "/ship {ISSUE_ID}", "ship task", "ship standalone". Argument: {ISSUE_ID} (e.g. TECH-42, BUG-17, FEAT-9).
argument-hint: "{ISSUE_ID} (e.g. TECH-42, BUG-17, FEAT-9)"
---

# /ship — Single-task standalone ship pipeline. Four mechanical steps: author digest → implement → verify-loop → close. No master plan, no code review, no audit, no commit.

Drive `$ARGUMENTS` via the [`ship`](../agents/ship.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /ship {ISSUE_ID}
- ship task
- ship standalone
<!-- skill-tools:body-override -->

<!-- skill-tools:body-override -->

**Scope:** Standalone tasks only (`master_plan_id IS NULL` in `ia_tasks`). Stage-attached tasks STOP at Phase 0 with `/ship-stage` handoff.

**Related:**
- [`/ship-stage`](ship-stage.md) — multi-task Stage chain (different surface; covers stage-attached tasks).
- [`/stage-authoring`](stage-authoring.md) — Stage-scoped digest authoring (`/ship` Phase 1 calls `--task` mode inline).
- [`/verify-loop`](verify-loop.md) — closed-loop verification (`/ship` Phase 3 invokes inline).

**Locked design:**
- Step 1 = author digest (no separate readiness gate).
- Code review: dropped.
- Audit: dropped.
- Step 4 = DB status walk (`pending → implemented → verified → done → archived`).
- No commit. User decides when.
- Verify-loop `MAX_ITERATIONS = 2`.

## Dispatch

Single Agent invocation with `subagent_type: "ship"` carrying `$ARGUMENTS` verbatim. Subagent runs `ia/skills/ship/SKILL.md` end-to-end inline (no nested dispatch).

## Pipeline summary output

After all phases complete (or on stop), the `ship` subagent emits:

```
SHIP {ISSUE_ID}: {PASSED|ALREADY_CLOSED|STOPPED} — {title}
  status walk : pending → implemented → verified → done → archived
  diff        : {git diff --stat HEAD count}
  next        : {commit when ready | /ship-stage handoff | /ship retry after fix}
```

## Hard boundaries

- Sequential phase dispatch only — no parallel.
- Standalone tasks only — stage-attached → STOP + `/ship-stage` handoff.
- No code review / audit / commit / master-plan task-row sync.
- Idempotent on re-entry (Phase 1 readiness skip + Phase 4 status-walk no-ops).
- Do NOT call `stage_closeout_apply` — standalone close is direct `task_status_flip` walk.
- Do NOT touch filesystem in Phase 4 — DB sole source of truth.
