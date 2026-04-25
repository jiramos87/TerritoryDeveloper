---
purpose: Authoring guidance for placing caveman directives into subagents, skills, slash commands, and handoff messages
audience: agent
loaded_by: ondemand
slices_via: none
description: On-demand authoring companion to agent-output-caveman — where the rule applies + how to author the directive into each surface
alwaysApply: false
---

# Agent output style — caveman authoring companion

Runtime stub: [`ia/rules/agent-output-caveman.md`](agent-output-caveman.md). Fetch via `rule_content agent-output-caveman-authoring` when editing subagent bodies, skill recipes, slash commands, handoff messages, memory entries.

## Where the rule applies

- **Agent responses** in this working dir — main session + any subagent via Agent tool.
- **Subagent bodies** `.claude/agents/{name}.md` — must contain explicit `caveman:caveman` directive so fresh-context window inherits style.
- **Skill recipes** `ia/skills/{name}/SKILL.md` — preamble directive + body prose (phase descriptions, framing sentences, guardrail bullets) in caveman fragments. Preamble alone insufficient; compress article-heavy framing on every edit.
- **Slash command bodies** `.claude/commands/*.md` — reassert caveman in forwarded prompt (belt-and-suspenders vs drift if subagent file loses directive).
- **Handoff messages** from `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP) + any future stage-handoff surfaces — message itself caveman-shaped; include explicit "follow `caveman:caveman` skill rules" line forwarded verbatim to next stage's fresh agent.
- **Project memory entries** (`MEMORY.md` or `.claude/memory/{slug}.md`) — caveman body.
- **New project specs** (`ia/projects/{ISSUE_ID}*.md`) — §1–§5, §6–§10 prose born caveman. Section headers, tables, code blocks, seed prompts, HTML-comment guidance stay normal. Existing specs NOT retroactively compressed.
- **BACKLOG.md rows** (new or modified) — **Notes** / **Acceptance** prose born caveman. Row structure (Type / Files / Spec / Notes / Acceptance / Depends on), bolded glossary terms, issue id cross-refs, path links stay verbatim. Section intro paragraphs caveman. Lane headers + tables normal. Same rule for `BACKLOG-ARCHIVE.md` archived row bodies.
- **Test mode batch output, verifier summaries, closeout digests** — caveman applies to prose around structured content, not structured content itself (see runtime stub exceptions).
- **C# XML doc comments** (`/// <summary>` / `<param>` / `<returns>` / `<exception>` / `<remarks>` / `<value>`) — Full level per [`xml-doc-caveman.md`](xml-doc-caveman.md). Tags + identifiers + `<see cref>` verbatim. Single-sentence ≤~8-word docs skipped.

## How to apply when authoring

Drop one-line directive near top of body, above task instructions:

> Follow `caveman:caveman` skill rules for all responses (drop articles/filler/pleasantries/hedging; fragments OK). Standard exceptions: code, commits, security/auth, verbatim errors, structured output, destructive-op confirmations.

## Human-polling surfaces — refined voice

When the skill / subagent pauses to poll the human (approach select, ambiguity gate, disagreement prompt, scope confirmation, `AskUserQuestion` call), caveman terseness applies but question + option text must use product/domain wording — not IA/tooling jargon. Ids, paths, cell coords, skeleton-step numbers move to a trailing `Context:` line, never inside the question stem. Full rule + good/bad examples: [`ia/rules/agent-human-polling.md`](agent-human-polling.md) — fetch via `rule_content agent-human-polling` when authoring any user-gate step.

## Soft-lint

Opt-in caveman drift detector. Runs `tools/scripts/caveman-lint.sh` against in-scope authoring surfaces.

### What it checks

Three indicator categories:

- **Sentence length** — lines with > 12 words trigger a hit.
- **Articles** — standalone `the`, `a`, `an` in non-code prose.
- **Hedging verbs** — `should`, `might`, `could`, `would` in prose.

Fenced code blocks (` ``` ` delimiters) always skipped — no false positives from code examples.

### Enabling

```bash
export SKILL_TRAIN_LINT=1
```

Set before starting a Claude Code session. Hook wired in `.claude/settings.json` fires on Write/Edit/MultiEdit tool calls while var is set.

### Reading output

Each hit: `{file}:{line}:{indicator}` — e.g. `ia/skills/foo/SKILL.md:42:article(the)`.

Footer always present: `N indicators found in M files (warn-only).`

### Rationale

Warn-only — exits 0 on any finding count; never blocks tool execution. Surfaces drift signal for human review, not enforcement.
