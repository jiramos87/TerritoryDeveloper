---
description: Sonnet literal-applier for §Plan Fix tuples emitted by the plan-review pair-head. Validation gate: validate:master-plan-status + validate:backlog-yaml. Single mode — plan-fix only. Triggers: "/plan-fix-apply", "plan-applier", "apply §Plan Fix tuples".
argument-hint: ""
---

# /plan-applier — Sonnet pair-tail: applies §Plan Fix tuples verbatim per plan-apply-pair-contract.

Drive `$ARGUMENTS` via the [`plan-applier`](../agents/plan-applier.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /plan-fix-apply
- plan-applier
- apply §Plan Fix tuples
## Dispatch

Single Agent invocation with `subagent_type: "plan-applier"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/plan-applier/SKILL.md`](../../ia/skills/plan-applier/SKILL.md) §Hard boundaries.
