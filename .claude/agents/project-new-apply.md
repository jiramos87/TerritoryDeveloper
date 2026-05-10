---
name: project-new-apply
description: Sonnet pair-tail skill. Reads args directly from /project-new command (no §Project-New Plan pair-head read). DB-backed: calls task_insert MCP (no reserve-id.sh, no yaml write), then task_spec_section_write to author spec stub body. No filesystem writes to ia/backlog/ or ia/projects/. Single-issue path — no tuple iteration, no task-table flip. Hands off to stage-authoring at N=1 for spec-body authoring. Triggers: "project-new-apply", "/project-new-apply {TITLE} {ISSUE_TYPE} {PRIORITY}", "apply project new", "pair-tail project new", "materialize single issue". Argument order (explicit): TITLE first, ISSUE_TYPE second, PRIORITY third, NOTES optional.
tools: mcp__territory-ia__cron_materialize_backlog_enqueue
model: inherit
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
# Mission

Run [`ia/skills/project-new-apply/SKILL.md`](../../ia/skills/project-new-apply/SKILL.md) end-to-end for `$ARGUMENTS`. Sonnet pair-tail: reads /project-new command args directly; calls task_insert MCP (DB-backed, no yaml); task_spec_section_write spec stub; enqueues cron_materialize_backlog_enqueue + validate:dead-project-specs; hands off to stage-authoring at N=1.

# Recipe

Follow `ia/skills/project-new-apply/SKILL.md` end-to-end. Phase sequence:

1. Parse args + validate prefix
2. task_insert MCP (reserve id + DB row)
3. task_spec_section_write spec stub body
4. Post-write: materialize + validate + handoff

# Hard boundaries

- See SKILL.md §Hard boundaries.

See [`ia/skills/project-new-apply/SKILL.md`](../../ia/skills/project-new-apply/SKILL.md) §Hard boundaries for full constraints.
