---
description: Execute a project spec's Implementation Plan phase by phase. Dispatches the `spec-implementer` subagent against `ia/projects/{ID}*.md` in isolated context.
argument-hint: "{ISSUE_ID} (e.g. TECH-11)"
---

# /implement — dispatch `spec-implementer` subagent

Use `spec-implementer` subagent (`.claude/agents/spec-implementer.md`) to execute Implementation Plan for `$ARGUMENTS`.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "spec-implementer"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `project-spec-implement` skill (`ia/skills/project-spec-implement/SKILL.md`) end-to-end on `ia/projects/$ARGUMENTS*.md`. Resolve filename via Glob — may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md`.
>
> ## Phase loop
>
> 1. Read spec (focus §5 Proposed Design, §6 Decision Log, §7 Implementation Plan, §9 Issues Found, §10 Lessons Learned). Start at first unticked phase.
> 2. MCP context per phase — `backlog_issue` + `router_for_task` + targeted `spec_section` / `spec_sections`. `invariants_summary` once when runtime C#/subsystem changes involved.
> 3. Implement with minimal diffs. `Edit` for existing files, `Write` only for new files.
> 4. Verify after each phase per `docs/agent-led-verification-policy.md`. Stop on failure; root-cause.
> 5. Tick phase checklist.
>
> Multi-stage → invoke `project-stage-close` skill **inline** at end of each non-final stage. Umbrella close = `closeout` subagent (`/closeout`), not yours.
>
> ## Hard boundaries
>
> - Do NOT skip phases. Execute in spec order.
> - Do NOT bypass failing verification with `--no-verify`.
> - Do NOT `git push --force`. Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
> - Do NOT add features/refactors/improvements beyond phase scope.
> - Do NOT introduce new singletons or `FindObjectOfType` in `Update` (per `ia/rules/invariants.md`).
> - Do NOT load whole reference specs. Slice via MCP.
> - Do NOT edit BACKLOG row state, archive, or delete spec — closeout territory.
>
> ## Output
>
> Single concise caveman message per phase: phase id closed, files touched, MCP slices loaded, verification run (commands + exit codes), issues + resolution, next step.
