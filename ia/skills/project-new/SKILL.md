---
name: project-new
purpose: >-
  Use when creating a new BACKLOG issue from a user prompt: calls task_insert MCP (DB-backed, no yaml
  write), task_spec_section_write spec stub, materializes backlog async via cron enqueue. Depends on /…
audience: agent
loaded_by: "skill:project-new"
slices_via: none
description: >-
  DB-backed: calls task_insert MCP (no reserve-id.sh, no yaml write) to create new BACKLOG issue;
  writes spec stub via task_spec_section_write. No ia/backlog/*.yaml or ia/projects/*.md writes.
  Triggers: "/project-new", "new backlog issue", "create TECH-xx from prompt", "bootstrap project
  spec", "add issue to backlog from description".
phases:
  - Context load
  - Backlog dep check
  - Spec outline
  - task_insert MCP (reserve id + DB row)
  - task_spec_section_write spec stub body
  - Post-insert validate + handoff
triggers:
  - /project-new
  - new backlog issue
  - create TECH-xx from prompt
  - bootstrap project spec
  - add issue to backlog from description
argument_hint: {free-text intent} [--type BUG|FEAT|TECH|ART|AUDIO] [--priority P1|P2|P3|P4]
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# New backlog issue and project spec bootstrap

No MCP calls from skill body. Follow **Tool recipe** below before editing BACKLOG or creating spec — thin context via `AGENTS.md` step 3 + force-loaded `ia/rules/invariants.md` (MCP-first directive + universal safety).

**vs author:** this skill creates backlog row + spec stub from user prompt. After stub → [`stage-authoring`](../stage-authoring/SKILL.md) (N=1 fills §Plan Digest) → [`project-spec-implement`](../project-spec-implement/SKILL.md) → `verify-loop` → `opus-code-review` → `/ship-stage` (inline closeout). Per canonical flow in [`docs/agent-lifecycle.md`](../../../docs/agent-lifecycle.md).

**Related:** [`project-implementation-validation`](../project-implementation-validation/SKILL.md) · [`BACKLOG.md`](../../../BACKLOG.md) · [`ia/skills/README.md`](../README.md).

## Stage MCP bundle contract

Stage opener calls [`domain-context-load`](../domain-context-load/SKILL.md) once; returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope. All Sonnet pair-tail invocations within the Stage read from that payload — no re-query of `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, or `invariants_summary` inside a Stage. The 5-tool recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) is encapsulated entirely in `domain-context-load`; callers never inline it.

## Seed prompt (parameterize)

Replace placeholders before sending.

```markdown
Create a new backlog issue and initial project spec from this description:

**Title / intent:** {SHORT_TITLE}
**Issue type:** {BUG-|FEAT-|TECH-|ART-|AUDIO-} (or ask me if unsure)
**User / product prompt:**

{USER_PROMPT}

Follow `ia/skills/project-new/SKILL.md`: run the Tool recipe (territory-ia), then call `task_insert` MCP to reserve id + create DB row, write spec stub via `task_spec_section_write`, and link Depends on / Related with verified ids only. Run `npm run validate:dead-project-specs` before finishing the PR.
```

## Stage context injection (called from `stage-file`)

When `stage-file` invokes this skill for a task belonging to a stage, the seed prompt includes two extra blocks:

- `{STAGE_CONTEXT}` — stage Objectives + Exit criteria + Phases list (from orchestrator spec). Use to populate §1 Summary context, §2.1 Goals (task contributes to which exit criterion), and §4.2 Systems map.
- `{TASK_INTENT}` — the task table's Intent cell. Use as the primary source for §1 Summary and §7 Implementation Plan sketch.

**Shared context:** `stage-file` pre-loads glossary/router/invariants ONCE for the whole stage before iterating tasks. Each `project-new` call receives that pre-loaded context in the seed prompt; skip re-running `glossary_discover` / `router_for_task` / `invariants_summary` unless the task intent diverges clearly from the shared domain.

**Orchestrator task table:** `stage-file` handles updating the task row (issue id + `Draft` status) after all issues are created. `project-new` does NOT touch the orchestrator spec.

**Validate:all timing:** `stage-file` runs `npm run validate:all` once after all tasks are filed. Each individual `project-new` call only runs `npm run validate:dead-project-specs`.

## When to use `web_search`

Only for external facts (vendor APIs, third-party packages, standards) not in repo. Never override glossary/specs/invariants. Cite URLs in Decision Log or Notes.

## Tool recipe (territory-ia)

Run in order. Pure meta (no domain terms) → skip steps marked optional.

1. Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from prompt (avoid generic-only arrays); `brownfield_flag = false` for most issues (full recipe); `tooling_only_flag = true` for doc/IA-only issues. Use returned `glossary_anchors`, `router_domains`, `spec_sections`, `invariants` for spec authoring. Editor Reports → include unity-development-context §10 via `spec_section` inside the subskill.
2. **`backlog_issue`** — For each related id in Depends on / Related / Notes. Hard dep unsatisfied → align or wait. Searches BACKLOG then BACKLOG-ARCHIVE.
3. **`list_specs`** / **`spec_outline`** — Only if `spec` key unknown.

### Optional: journal (Postgres)

Only when prompt ambiguous/cross-cutting or user requests exploration context. `project_spec_journal_search` English query, `max_results` ≤ 8. `db_unconfigured` → skip.

### Branching

- **Roads/bridge/wet run** → roads-system + geo via `router_for_task` + `spec_section`.
- **Water/HeightMap/shore** → water-terrain-system + geo.
- **JSON/schema/Save** → persistence-system; no on-disk Save data changes unless user requires.

## File and backlog checklist

1. **Prefix** — `BUG-`/`FEAT-`/`TECH-`/`ART-`/`AUDIO-` per [`AGENTS.md`](../../../AGENTS.md).
2. **Reserve id + create DB row** — Call `mcp__territory-ia__task_insert` with `{slug: null, stage_id: null, title, type: PREFIX, priority, notes, depends_on: [], related: []}`. Response carries reserved `task_id` from DB sequence. **No `reserve-id.sh`, no `ia/backlog/*.yaml` write.** MCP unavailable → escalate.
3. **Priority** — Match severity + existing BACKLOG structure per AGENTS.md.
4. **Spec stub** — Call `mcp__territory-ia__task_spec_section_write({task_id, section: "Goal", content: "## §Goal\n\n{TITLE} — implementation TBD. Spec body authored by stage-authoring at N=1.\n\n**Status:** Draft\n**Created:** {TODAY}"})`. **No `ia/projects/{ISSUE_ID}.md` write.**
5. **Materialize** — `mcp__territory-ia__cron_materialize_backlog_enqueue({triggered_by: "project-new"})`. Fallback: `bash tools/scripts/materialize-backlog.sh`.
6. **Validate** — `npm run validate:dead-project-specs`.
7. **Next** — At N=1: `/stage-authoring --task` to fill §Plan Digest before `/implement`.

## Follow-up

Domain skills (roads, terrain/water, new managers) from [`BACKLOG.md`](../../../BACKLOG.md) when implementing.

## Changelog
