---
purpose: All agent output (responses, prompts, skills, subagent bodies, handoff messages) defaults to caveman style
audience: agent
loaded_by: always
slices_via: none
description: Caveman output style is the project default for agents, subagents, skills, slash commands, and handoff prompts
alwaysApply: true
---

# Agent output style — caveman default

## Audience split (read this first)

Pick voice by **who reads the surface**, not by file extension.

| Register | Audience | Where | Voice |
|---|---|---|---|
| **Caveman-tech (default)** | Agents | `ia/**`, `docs/**`, master plans, BACKLOG row prose, code XML docs, commits, PR bodies, MEMORY, agent-to-agent handoffs | Drop articles/filler/hedging. Fragments OK. Jargon welcome (class names, tool names, paths, glossary slugs). Maximum density = max agent perf + token economy. Pattern `[thing] [action] [reason]. [next step]`. |
| **Human-product** | Javier (chat only) | Main-session replies, subagent return prose bubbling up, status updates between tool calls, end-of-turn summaries, `AskUserQuestion` polls | Simple product / game / feature language. Drop class names, tool names, paths, stage numbers, yaml fields, anchor refs, glossary slugs from prose. Translate to what player/designer sees. Caveman terseness still applies — jargon drops, not brevity. Trailing `Context:` line OK for ids when audit needed. |

**Why.** Javier reads chat, not the docs agents edit. Mixing registers forces him to translate every reply → decision latency. Agents read docs at max jargon density.

**Length identical** in both registers; vocabulary differs.

Exceptions (write normal English regardless of register):

1. Code source files, blocks, identifiers, XML docs.
2. Commits, PR titles, PR bodies.
3. Security / auth content — credential handling, hook denylist rationale.
4. Verbatim quotes — user prompts, error messages, stack traces, tool output.
5. Structured payloads — JSON Verification blocks, MCP tool args.
6. Destructive-op confirmations — Pass B spec deletes, force-push warnings.
7. User says "normal" / "stop caveman" / "in Spanish" → overrides for that turn.
8. Human polls — see [`ia/rules/agent-human-polling.md`](agent-human-polling.md). Product wording mandatory.

Authoring surfaces checklist + edge cases: [`ia/rules/agent-output-caveman-authoring.md`](agent-output-caveman-authoring.md) — fetch via `rule_content agent-output-caveman-authoring`.
