---
description: Use when an exploration doc under `docs/` carries a persisted `## Design Expansion` block and the work needs a multi-stage plan rather than a single BACKLOG issue. Produces `ia_master_plans` row + `ia_stages` rows (orchestrator is permanent — never closeable, never deleted by automation) with ALL Stages fully decomposed into Tasks (2-level hierarchy: `Stage > Task`). Tasks seeded `_pending_` for later `stage-file`. Canonical shape authority: `docs/MASTER-PLAN-STRUCTURE.md` — file shape, Stage block shape, 5-column Task table schema, Status enums, flip matrix. Triggers: "/master-plan-new {path}", "turn expanded design into master plan", "create orchestrator from exploration", "author master plan from design expansion".
argument-hint: "{DOC_PATH} [SLUG] [SCOPE_BOUNDARY_DOC] (e.g. docs/foo-exploration.md foo docs/foo-post-mvp-extensions.md)"
---

# /master-plan-new — Use after design-explore has persisted `## Design Expansion` in an exploration doc: decompose Implementation Points into stage/task (2-level hierarchy) and author `ia_master_plans` + `ia_stages` rows as a permanent orchestrator. Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`.

Drive `$ARGUMENTS` via the [`master-plan-new`](../agents/master-plan-new.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator header block prose (human-consumed cold — may run 2–4 sentences per Objectives field). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /master-plan-new {path}
- turn expanded design into master plan
- create orchestrator from exploration
- author master plan from design expansion
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{DOC_PATH} [SLUG] [SCOPE_BOUNDARY_DOC]`. First token = path to exploration `.md` with persisted `## Design Expansion` (or semantic equivalent). Optional second token = slug override (kebab-case, e.g. `blip`; defaults to exploration doc filename stem stripped of `-exploration` / `-design` suffix). Optional third token = scope-boundary doc path (e.g. `docs/{SLUG}-post-mvp-extensions.md`).

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "master-plan-new"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator header prose (Objectives fields may run 2–4 sentences — human-consumed cold). Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `master-plan-new` skill (`ia/skills/master-plan-new/SKILL.md`) end-to-end on the exploration doc given in `$ARGUMENTS`. Parse args: first token = `DOC_PATH`, optional second token = `SLUG` override, optional third token = `SCOPE_BOUNDARY_DOC`. Resolve `DOC_PATH` via Read — if unreadable, stop and report path error.
>
> ## Phase sequence (gated)
>
> 0. Load + validate — Read `DOC_PATH`. Confirm expansion intent present (literal `## Design Expansion` OR semantic equivalents per Phase 0 mapping table in SKILL.md). Missing any intent → STOP, route user to `/design-explore {DOC_PATH}` first.
> 1. Slug + overwrite gate — Resolve `SLUG`. Probe via `master_plan_render({slug: SLUG})`. Plan payload returned → STOP, ask confirm overwrite OR new slug. `not_found` → continue.
> 2. MCP context + surface-path pre-check — Run **Tool recipe** (below). Greenfield (new subsystem, no existing code paths touched) skips `router_for_task` / `spec_sections` / `invariants_summary`. Tooling/pipeline-only plans skip `invariants_summary`. Surface-path pre-check via `surface-path-precheck` subskill.
> 3. Scope header — Author header block verbatim shape: Status, Scope, Exploration source + sections, Locked decisions, Hierarchy rules pointer, Read-first list (invariants by number from Phase 2, scope-boundary doc if provided).
> 4. Stage decomposition — Map Implementation Points directly to Stages (2-level: no Step grouping, no Phase layer). 2–6 Stages typical; each = shippable compilable increment landing on green-bar boundary. Reuse Phase 2 MCP output. Ordering heuristic: scaffolding → data model → runtime logic → integration + tests (unless exploration's declared dep chain overrides). Per Stage: full 5-column Task table (`Task | Name | Issue | Status | Intent`), all Tasks `_pending_`.
> 5. Cardinality gate — Each Stage Task table: **≥2 Tasks AND ≤6 Tasks** (per `ia/rules/project-hierarchy.md`). <2 → STOP, split-or-justify. 7+ → STOP, suggest split. Single-file Tasks → STOP, merge candidate. Proceed only after user confirms.
> 6. Tracking legend — Insert standard legend verbatim under `## Stages` per `docs/MASTER-PLAN-STRUCTURE.md` §3. Do NOT paraphrase.
> 7. Persist (DB MCP) — `master_plan_insert({slug, title, preamble})` → seeds row + preamble. Per Stage authored: `stage_insert({slug, stage_id, title, body, objective, exit_criteria})`. `master_plan_change_log_append({slug, kind: "plan_authored", body})` → audit row.
> 7b. Regenerate progress dashboard — `npm run progress` (repo root). Failure does NOT block Phase 8 — log exit code and continue.
> 8. Handoff — Single caveman message with counts (`N stages · M tasks`) + invariants + gate results + next-step call (`claude-personal "/stage-file {SLUG} Stage 1.1"`).
>
> ## Tool recipe — Phase 2 only
>
> Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:
>
> - `keywords`: English tokens from Chosen Approach + Subsystem Impact + Architecture component names.
> - `brownfield_flag`: `true` for greenfield (skips router/spec_sections/invariants_summary). `false` for brownfield.
> - `tooling_only_flag`: `true` for tooling/pipeline-only plans.
>
> Run `list_specs` / `spec_outline` only if a routed domain references a spec whose sections weren't returned. Surface-path pre-check via `surface-path-precheck` subskill.
>
> ## Hard boundaries
>
> - Do NOT author master plan when Phase 0 expansion gate unmet — route to `/design-explore` first.
> - Do NOT silently overwrite existing `ia_master_plans` row — orchestrators are permanent.
> - Do NOT persist with cardinality violations (<2 or >6 Tasks/Stage) unresolved.
> - Do NOT insert BACKLOG rows. Do NOT create task spec stubs. Tasks stay `_pending_`.
> - Do NOT delete or rename exploration doc. Do NOT edit its expansion block.
> - Do NOT create scope-boundary stub if missing — raise recommendation only.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message: `{SLUG}` master plan written with counts (`N stages · M tasks`); invariants flagged by number + gated stages; cardinality splits resolved; scope-boundary-doc outcome; next step `claude-personal "/stage-file {SLUG} Stage 1.1"`.
