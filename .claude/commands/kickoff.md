---
description: Review or enrich a project spec before implementation. Dispatches the `spec-kickoff` subagent against `ia/projects/{ID}*.md` in isolated context.
argument-hint: "{ISSUE_ID} (e.g. TECH-11)"
---

# /kickoff — dispatch `spec-kickoff` subagent

Use `spec-kickoff` subagent (`.claude/agents/spec-kickoff.md`) to review + enrich project spec for `$ARGUMENTS`.

## Step 0 — Context banner (before dispatch)

Before dispatching the subagent, resolve and print for the human developer:

1. Glob `ia/projects/$ARGUMENTS*.md` → confirm spec file + extract short description from filename.
2. Glob `ia/projects/*-master-plan.md` → grep each for `$ARGUMENTS` → identify owning master plan.
3. Print:
   ```
   KICKOFF $ARGUMENTS — {issue title from BACKLOG.md}
     master plan : {Plan Name} (ia/projects/{master-plan-filename})
     spec        : ia/projects/{spec-filename}
   ```
   If no master plan found: `master plan: (none — standalone issue)`.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "spec-kickoff"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `project-spec-kickoff` skill (`ia/skills/project-spec-kickoff/SKILL.md`) end-to-end on `ia/projects/$ARGUMENTS*.md`. Resolve filename via Glob — may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md`.
>
> ## MCP first
>
> 1. `mcp__territory-ia__backlog_issue` for `$ARGUMENTS` — Files / Notes / Spec / Acceptance / depends_on_status.
> 2. `mcp__territory-ia__invariants_summary` once if code/subsystem changes implied.
> 3. `mcp__territory-ia__router_for_task` per domain (1–3 from Summary/Goals/Files).
> 4. `mcp__territory-ia__spec_section` or `spec_sections` batch for routed specs — slices, never whole files.
> 5. `mcp__territory-ia__glossary_discover` with English keyword array → narrow via `glossary_lookup`.
>
> ## Editorial pass
>
> Tighten Open Questions, Implementation Plan phases, Decision Log, sibling cross-links. Edit spec in place. Do NOT execute Implementation Plan (= `spec-implementer`). Do NOT close issue (= `closeout`).
>
> ## Hard boundaries
>
> - Do NOT load whole reference specs when slices suffice.
> - Do NOT touch BACKLOG row state, archive, id purge.
> - Do NOT delete spec.
> - Do NOT skip `invariants_summary` when runtime C#/subsystem changes implied.
>
> ## Output
>
> Single concise caveman message: spec edits made, Open Questions resolved/deferred, glossary terms aligned, Implementation Plan phases tightened, Verification readiness, next step.
