---
description: Execute a project spec's Implementation Plan phase by phase. Dispatches the `spec-implementer` subagent against `ia/projects/{ID}*.md` in isolated context.
argument-hint: "{ISSUE_ID} (e.g. TECH-85)"
---

# /implement — dispatch `spec-implementer` subagent

Use the **`spec-implementer`** subagent (defined in `.claude/agents/spec-implementer.md`) to execute the Implementation Plan for `$ARGUMENTS`.

## Subagent prompt (forward verbatim)

Forward the following prompt to the subagent via the Agent tool with `subagent_type: "spec-implementer"`:

> Follow `caveman:caveman` skill rules for all responses (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). Standard exceptions apply: code, commits, security/auth content, verbatim error/tool output, structured MCP inputs/outputs, destructive-op confirmations. Project anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run the `project-spec-implement` skill (`ia/skills/project-spec-implement/SKILL.md`) end-to-end on the project spec at `ia/projects/$ARGUMENTS*.md`. Resolve the actual filename via Glob — the spec may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md` (per Q8 descriptive naming convention).
>
> ## Phase loop
>
> 1. Read the spec (focus on §5 Proposed Design, §6 Decision Log, §7 Implementation Plan, §9 Issues Found, §10 Lessons Learned). Start at the first unticked phase.
> 2. Pull MCP context per phase — `backlog_issue` + `router_for_task` + targeted `spec_section` / `spec_sections` slices. Call `invariants_summary` once when runtime C# / subsystem changes are involved.
> 3. Implement the phase with minimal diffs. Use `Edit` for existing files, `Write` only for genuinely new files.
> 4. Verify after each phase per `docs/agent-led-verification-policy.md`. Stop on failure; diagnose root cause.
> 5. Tick the phase checklist in the spec.
>
> If the spec is multi-stage, invoke the `project-stage-close` skill **inline** at the end of each non-final stage. The umbrella close is the `closeout` subagent's job (run via `/closeout`), not yours.
>
> ## Hard boundaries
>
> - Do NOT skip phases. Execute in spec order.
> - Do NOT bypass failing verification with `--no-verify`.
> - Do NOT use `git push --force`. Do NOT touch `.claude/settings.json` `permissions.defaultMode` or the `mcp__territory-ia__*` wildcard.
> - Do NOT add features, refactors, or improvements beyond the phase scope.
> - Do NOT introduce new singletons or `FindObjectOfType` in `Update` (per `ia/rules/invariants.md`).
> - Do NOT load whole reference specs. Slice via MCP.
> - Do NOT edit `BACKLOG.md` row state, archive, or delete the spec — closeout territory.
>
> ## Output
>
> Single concise caveman message per phase: phase id closed, files touched, MCP slices loaded, verification run (commands + exit codes), issues + resolution, next step.
