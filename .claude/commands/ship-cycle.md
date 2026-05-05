---
description: Stage-atomic batch ship-cycle. One Sonnet 4.6 inference body emits ALL tasks of one Stage with `<!-- TASK:{ISSUE_ID} START/END -->` boundary markers. Replaces `/ship-stage` Pass A per-task loop when stage fits one window. Per-task `unity:compile-check` gate + `task_status_flip(implemented)` after batch lands. Pass B (`verify-loop` + closeout) reuses `/ship-stage` machinery. Failure mode = `ia_stages.status='partial'` (mig 0069); resume re-enters at first non-done task. Token budget hard cap 80k input. Validate gate = `validate:fast` (TECH-12640) on cumulative stage diff. Triggers: "/ship-cycle {SLUG} {STAGE_ID}", "ship cycle stage", "stage-atomic batch ship". Argument order (explicit): SLUG first, STAGE_ID second.
argument-hint: "{slug} Stage {X.Y} [--force-model {model}]"
---

# /ship-cycle — Stage-atomic batch ship: one Sonnet 4.6 inference body emits ALL tasks of one Stage with structured boundary markers. Drop-in replacement for `/ship-stage` Pass A two-pass loop when stage size fits one inference window. Falls back to ship-stage two-pass when batch exceeds token cap.

Drive `$ARGUMENTS` via the [`ship-cycle`](../agents/ship-cycle.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /ship-cycle {SLUG} {STAGE_ID}
- ship cycle stage
- stage-atomic batch ship
## Dispatch

Single Agent invocation with `subagent_type: "ship-cycle"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/ship-cycle/SKILL.md`](../../ia/skills/ship-cycle/SKILL.md) §Hard boundaries.
