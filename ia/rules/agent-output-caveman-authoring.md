---
purpose: Authoring guidance for placing caveman directives into subagents, skills, slash commands, and handoff messages
audience: agent
loaded_by: ondemand
slices_via: none
description: On-demand authoring companion to agent-output-caveman — where the rule applies + how to author the directive into each surface
alwaysApply: false
---

# Agent output style — caveman authoring companion

Runtime stub: [`ia/rules/agent-output-caveman.md`](agent-output-caveman.md). Fetch this companion via `rule_content agent-output-caveman-authoring` when editing subagent bodies, skill recipes, slash commands, handoff messages, or memory entries.

## Where the rule applies

- **Agent responses** in this working directory — main session and any subagent invoked via the Agent tool.
- **Subagent bodies** under `.claude/agents/{name}.md` — every subagent file must contain an explicit `caveman:caveman` directive in its body so its fresh-context window inherits the style.
- **Skill recipes** under `ia/skills/{name}/SKILL.md` — every recipe must carry a one-paragraph "Output style: caveman" preamble so direct skill invocations (not via a subagent) inherit the style.
- **Slash command bodies** under `.claude/commands/*.md` — each command must reassert the caveman directive in the prompt it forwards to its subagent (belt-and-suspenders against drift if a future edit strips the directive from the subagent file).
- **Handoff messages** emitted by `project-stage-close` (and any future stage-handoff surface) — the message must itself be caveman-shaped and must include an explicit "follow `caveman:caveman` skill rules" line forwarded verbatim to the next stage's fresh agent.
- **Project memory entries** added to `MEMORY.md` or `.claude/memory/{slug}.md` — write the entry body in caveman style.
- **Test mode batch output, verifier human-readable summaries, closeout digests** — caveman applies to the prose surrounding the structured content, not to the structured content itself (see runtime stub exceptions).

## How to apply when authoring a subagent / skill / handoff message

Drop a one-line directive near the top of the body, above any task instructions. Example wording:

> Follow `caveman:caveman` skill rules for all responses (drop articles/filler/pleasantries/hedging; fragments OK). Standard exceptions apply: code, commits, security/auth, verbatim errors, structured output, destructive-op confirmations.

Anchored history: TECH-85 §6 Decision Log row from 2026-04-10 captures the original rationale and rejected alternatives.
