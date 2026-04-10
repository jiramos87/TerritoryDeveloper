---
description: Review or enrich a project spec before implementation. Dispatches the `spec-kickoff` subagent against `ia/projects/{ID}*.md` in isolated context.
argument-hint: "{ISSUE_ID} (e.g. TECH-85)"
---

# /kickoff — dispatch `spec-kickoff` subagent

Use the **`spec-kickoff`** subagent (defined in `.claude/agents/spec-kickoff.md`) to review and enrich the project spec for `$ARGUMENTS`.

## Subagent prompt (forward verbatim)

Forward the following prompt to the subagent via the Agent tool with `subagent_type: "spec-kickoff"`:

> Follow `caveman:caveman` skill rules for all responses (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). Standard exceptions apply: code, commits, security/auth content, verbatim error/tool output, structured MCP inputs/outputs. Project anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run the `project-spec-kickoff` skill (`ia/skills/project-spec-kickoff/SKILL.md`) end-to-end on the project spec at `ia/projects/$ARGUMENTS*.md`. Resolve the actual filename via Glob — the spec may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md` (per Q8 descriptive naming convention).
>
> ## MCP first
>
> 1. `mcp__territory-ia__backlog_issue` for `$ARGUMENTS` — pull Files / Notes / Spec / Acceptance / depends_on_status.
> 2. `mcp__territory-ia__invariants_summary` once if the spec implies code or game subsystem changes.
> 3. `mcp__territory-ia__router_for_task` per domain (1–3 from Summary / Goals / Files).
> 4. `mcp__territory-ia__spec_section` (or `spec_sections` batch) for routed reference specs — slices, never whole files.
> 5. `mcp__territory-ia__glossary_discover` with English keyword array, then narrow with `glossary_lookup`.
>
> ## Editorial pass
>
> Tighten Open Questions, Implementation Plan phases, Decision Log, sibling spec cross-links. Edit the spec in place. Do **not** execute the Implementation Plan (that is the `spec-implementer` subagent's job). Do **not** close the issue (that is the `closeout` subagent's job).
>
> ## Hard boundaries
>
> - Do NOT load whole reference specs when slices suffice.
> - Do NOT touch `BACKLOG.md` row state, archive, or id purge.
> - Do NOT delete the spec.
> - Do NOT skip `invariants_summary` when runtime C# / subsystem changes are implied.
>
> ## Output
>
> Single concise caveman message: spec edits made, Open Questions resolved/deferred, glossary terms aligned, Implementation Plan phases tightened, Verification readiness, next step.
