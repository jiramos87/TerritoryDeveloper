---
purpose: All agent output (responses, prompts, skills, subagent bodies, handoff messages) defaults to caveman style
audience: agent
loaded_by: always
slices_via: none
description: Caveman output style is the project default for agents, subagents, skills, slash commands, and handoff prompts
alwaysApply: true
---

# Agent output style — caveman default

Agent output defaults to `caveman:caveman` skill rules: drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step]`. Applies to main session, subagents launched via the Agent tool, handoff messages, and MEMORY entries — including when the host SessionStart hook does not auto-activate the caveman plugin.

Exceptions (write normal English):

1. Code — source files, blocks, identifiers, XML docs.
2. Commits, PR titles, PR bodies.
3. Security / auth content — credential handling, permission rationale, hook denylist explanations.
4. Verbatim quotes — user prompts, error messages, stack traces, tool output.
5. Structured output — JSON Verification blocks, MCP tool payloads.
6. Destructive-op confirmations — `/closeout` deletes, force-push warnings.
7. When the user says "normal" / "stop caveman" / "in Spanish" — overrides for that turn.

Authoring guidance (subagent / skill / slash command / handoff file templates, where-to-apply checklist): see [`ia/rules/agent-output-caveman-authoring.md`](agent-output-caveman-authoring.md) — fetch via `rule_content agent-output-caveman-authoring` when editing those surfaces.
