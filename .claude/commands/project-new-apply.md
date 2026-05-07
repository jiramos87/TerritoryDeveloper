---
description: Sonnet pair-tail skill. Reads args directly from /project-new command (no §Project-New Plan pair-head read). Runs reserve-id.sh, writes ia/backlog/{id}.yaml, writes ia/projects/{id}.md stub from project-spec-template, enqueues cron_materialize_backlog_enqueue + validate:dead-project-specs. Single-issue path — no tuple iteration, no task-table flip. Hands off to stage-authoring at N=1 for spec-body authoring. Triggers: "project-new-apply", "/project-new-apply {TITLE} {ISSUE_TYPE} {PRIORITY}", "apply project new", "pair-tail project new", "materialize single issue". Argument order (explicit): TITLE first, ISSUE_TYPE second, PRIORITY third, NOTES optional.
argument-hint: ""
---

# /project-new-apply — Sonnet pair-tail: reads /project-new command args directly; reserves id + writes yaml + spec stub; enqueues cron_materialize_backlog_enqueue + validate:dead-project-specs; hands off to stage-authoring at N=1.

Drive `$ARGUMENTS` via the [`project-new-apply`](../agents/project-new-apply.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- project-new-apply
- /project-new-apply {TITLE} {ISSUE_TYPE} {PRIORITY}
- apply project new
- pair-tail project new
- materialize single issue
## Dispatch

Single Agent invocation with `subagent_type: "project-new-apply"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/project-new-apply/SKILL.md`](../../ia/skills/project-new-apply/SKILL.md) §Hard boundaries.
