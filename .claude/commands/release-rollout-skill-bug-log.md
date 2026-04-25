---
description: Use when a lifecycle skill (`design-explore`, `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`) misbehaves during rollout — misses a guardrail, misroutes a Phase, fails a pre-condition check, produces invalid output shape. Dual-writes the bug + fix to the per-skill `## Changelog` section (authoritative source) + appends an aggregator row to the tracker's Skill Iteration Log table (rollup, cross-referenced). Does NOT fix the skill (= hand-edit by user or targeted patch via a fresh subagent). Triggers: "log skill bug", "release-rollout-skill-bug-log", "skill iteration log entry".
argument-hint: ""
---

# /release-rollout-skill-bug-log — Log a skill bug / gap encountered during rollout. Dual-write: per-skill `## Changelog` section (source of truth) + tracker Skill Iteration Log aggregator row (rollup). Used when a lifecycle skill misbehaves mid-rollout.

Drive `$ARGUMENTS` via the [`release-rollout-skill-bug-log`](../agents/release-rollout-skill-bug-log.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- log skill bug
- release-rollout-skill-bug-log
- skill iteration log entry
## Dispatch

Single Agent invocation with `subagent_type: "release-rollout-skill-bug-log"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/release-rollout-skill-bug-log/SKILL.md`](../../ia/skills/release-rollout-skill-bug-log/SKILL.md) §Hard boundaries.
