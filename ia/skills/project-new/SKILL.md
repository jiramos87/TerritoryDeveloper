---
purpose: "Use when creating a new BACKLOG.md issue from a user prompt: next BUG-/FEAT-/TECH-/ART-/AUDIO- id, row in the correct priority section, bootstrap ia/projects/{ISSUE_ID}.md from the template, and Depends on /…"
audience: agent
loaded_by: skill:project-new
slices_via: none
name: project-new
description: >
  Use when creating a new BACKLOG.md issue from a user prompt: next BUG-/FEAT-/TECH-/ART-/AUDIO- id,
  row in the correct priority section, bootstrap ia/projects/{ISSUE_ID}.md from the template,
  and Depends on / Related with verified ids (territory-ia MCP + optional web_search). Triggers:
  "/project-new", "new backlog issue", "create TECH-xx from prompt", "bootstrap project spec",
  "add issue to backlog from description".
model: inherit
phases:
  - "Context load"
  - "Backlog dep check"
  - "Spec outline"
  - "Reserve id"
  - "Write yaml + spec"
  - "Materialize backlog"
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

Follow `ia/skills/project-new/SKILL.md`: run the Tool recipe (territory-ia), then write `ia/backlog/{ISSUE_ID}.yaml`, create `ia/projects/{ISSUE_ID}.md` from `ia/templates/project-spec-template.md`, run `bash tools/scripts/materialize-backlog.sh`, and link Depends on / Related with verified ids only. Run `npm run validate:dead-project-specs` before finishing the PR.
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
2. **Next id** — Three paths (never hand-edit the counter):
   - **Normal path (MCP available):** Call `mcp__territory-ia__reserve_backlog_ids(prefix: "{PREFIX}", count: 1)` to get the next id. Use the returned id.
   - **Normal path (MCP unavailable):** Run `bash tools/scripts/reserve-id.sh {PREFIX}` (atomic flock on `ia/state/id-counter.json`). Use the returned id.
   - **`--reserved-id {ID}` path (called from `stage-file`):** When the seed prompt carries `--reserved-id {ID}`, use that id verbatim. Skip both `reserve_backlog_ids` and `reserve-id.sh` entirely — `stage-file` already reserved the id via a batch call. Invariant #13 preserved (one writer per call chain).
3. **Priority section** — Match severity + existing BACKLOG structure. Follow Priority order in AGENTS.md.
4. **Backlog record** — Author the yaml body (id, type, title, priority, status: open, section, spec, files, notes, acceptance, depends_on, depends_on_raw, related, created, raw_markdown). Every cited id in Depends on must exist in `ia/backlog/` or `ia/backlog-archive/`. Before writing to disk, call `mcp__territory-ia__backlog_record_validate(record: {yaml body})` and fix any reported schema errors. **MCP unavailable fallback:** skip the validate call; `validate:all` at end catches schema drift. Write the validated yaml to `ia/backlog/{ISSUE_ID}.yaml`. Post-hook: `bash tools/scripts/materialize-backlog.sh` to regenerate `BACKLOG.md`.
5. **Project spec** — Copy [`project-spec-template.md`](../../templates/project-spec-template.md) → `ia/projects/{ISSUE_ID}.md`. Fill header, Summary, Goals, stub Implementation Plan, Open Questions per [`PROJECT-SPEC-STRUCTURE.md`](../../../docs/PROJECT-SPEC-STRUCTURE.md).
6. **Validate** — `npm run validate:dead-project-specs`.
7. **Next** — At N=1: `/stage-authoring --task` to fill §Plan Digest before `/implement`.

## Follow-up

Domain skills (roads, terrain/water, new managers) from [`BACKLOG.md`](../../../BACKLOG.md) when implementing.

## Changelog
