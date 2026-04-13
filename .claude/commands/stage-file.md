---
description: Bulk-file all pending tasks of an orchestrator stage as BACKLOG issues + project spec stubs. Dispatches the `stage-file` subagent with shared stage context and phase/task cardinality enforcement.
argument-hint: "Stage {X.Y} [in {orchestrator-spec-path}] [prefix TECH-|FEAT-|BUG-]"
---

# /stage-file — dispatch `stage-file` subagent

Use `stage-file` subagent (`.claude/agents/stage-file.md`) to bulk-file all `_pending_` tasks for `$ARGUMENTS`.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "stage-file"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-file/SKILL.md` end-to-end for `$ARGUMENTS`. Resolve orchestrator spec via Glob under `ia/projects/` (typically `multi-scale-master-plan.md` unless user specifies). Parse stage id from arguments (e.g. `Stage 1.2` → `1.2`). Default issue prefix `TECH-` unless user specifies.
>
> ## Phase loop
>
> 1. Read orchestrator spec → extract target stage (Objectives, Exit, Phases, task table).
> 2. Cardinality gate — phase with 1 task → warn + pause for user confirmation before proceeding.
> 3. Load shared MCP context ONCE: `glossary_discover` → `glossary_lookup` → `router_for_task` → `invariants_summary` (if C# stage) → `spec_section` → `backlog_issue` (stage-level deps).
> 4. Filing loop (task-table order): next id → BACKLOG row → spec stub from template → `validate:dead-project-specs`. Abort task on non-zero.
> 5. Atomic update: after ALL tasks filed, one Edit pass updates orchestrator task table (issue ids + `Draft` status).
> 6. `npm run validate:all` — stop on failure, root-cause.
>
> ## Hard boundaries
>
> - Do NOT update orchestrator task table mid-loop.
> - Do NOT run `validate:all` per task — once at end.
> - Do NOT file tasks outside target stage.
> - Do NOT pre-file for stages whose parent step is not `In Progress`.
> - Do NOT kickoff or implement any filed issue.
> - Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
>
> ## Output
>
> Single caveman message: tasks filed (id + one-line intent each), cardinality warnings resolved, MCP slices loaded, validate:all exit code, orchestrator table updated, next step (`/kickoff {first_id}`).
