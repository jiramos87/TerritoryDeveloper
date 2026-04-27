---
description: Use when an existing master plan needs new Stages sourced from an exploration doc (with persisted `## Design Expansion`) OR an extensions doc (e.g. `{slug}-post-mvp-extensions.md`). Appends new Stage rows to `ia_stages` — never rewrites existing Stages, never overwrites headers, never inserts BACKLOG rows. Fully decomposes every new Stage (Task table) at author time — no skeletons. 2-level hierarchy `Stage > Task`. Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`. Triggers: "/master-plan-extend {slug} {source}", "extend master plan from exploration", "add new stages to orchestrator", "append from extensions doc", "pull deferred stage into master plan".
argument-hint: "{SLUG} {SOURCE_DOC} [START_STAGE_NUMBER] [SCOPE_BOUNDARY_DOC] (e.g. blip docs/blip-post-mvp-extensions.md)"
---

# /master-plan-extend — Extend an existing master plan (`ia_master_plans` row) with new Stages sourced from an exploration or extensions doc. Appends to `ia_stages` — never rewrites existing Stages. Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`.

Drive `$ARGUMENTS` via the [`master-plan-extend`](../agents/master-plan-extend.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator header block prose (human-consumed cold — may run 2–4 sentences per Objectives field). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /master-plan-extend {plan} {source}
- extend master plan from exploration
- add new stages to orchestrator
- append from extensions doc
- pull deferred stage into master plan
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{SLUG} {SOURCE_DOC} [START_STAGE_NUMBER] [SCOPE_BOUNDARY_DOC]`. First token = bare master plan slug (e.g. `blip`). Second token = path to exploration doc with persisted `## Design Expansion` (or semantic equivalent) OR extensions doc listing deferred Stages. Optional third token = `START_STAGE_NUMBER` `N.M` override (default = next free `N.M`). Optional fourth token = scope-boundary doc path.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "master-plan-extend"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator preamble prose (Objectives fields may run 2–4 sentences — human-consumed cold). Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `master-plan-extend` skill (`ia/skills/master-plan-extend/SKILL.md`) end-to-end on the target master plan + source doc given in `$ARGUMENTS`. Parse args: first token = `SLUG`, second token = `SOURCE_DOC`, optional third token = `START_STAGE_NUMBER` override, optional fourth token = `SCOPE_BOUNDARY_DOC`. Call `master_plan_render({slug: SLUG})` to fetch existing plan; `not_found` → STOP. Read `SOURCE_DOC` (filesystem) — unreadable → STOP and report path error.
>
> ## Phase sequence (gated)
>
> 0. Load + validate — Call `master_plan_render({slug: SLUG})`. `not_found` → STOP, route to `/master-plan-new {SOURCE_DOC}`. Validate rendered preamble shape (Stages + tracking legend + Orchestration guardrails). Read `SOURCE_DOC`. Confirm expansion intent present (literal `## Design Expansion` OR semantic equivalents per Phase 0 mapping table). Missing source expansion intent → STOP, route to `/design-explore {SOURCE_DOC}`.
> 1. Start-number resolution + duplication gate — Compute `START_STAGE_NUMBER` (`N.M`). User override gated against existing `(N, M)` pairs (collision → STOP). Default = next free in last cluster OR `(max_N + 1, 1)` for new cluster. Duplication gate playbook: Draft unpersisted Stage → merge; In Review+ → STOP and ask rename/drop/revision-cycle; near-overlap with distinct scope → proceed with note.
> 2. MCP context + surface-path pre-check — Run **Tool recipe** (below) via `domain-context-load` subskill. Greenfield skips `router_for_task` / `spec_sections` / `invariants_summary`. Tooling-only plans skip `invariants_summary`. Surface-path pre-check via `surface-path-precheck` subskill — mark `(new)` for non-existent paths.
> 3. New-stage proposal + digest — Emit caveman one-liner outline per proposed new Stage (`Stage {N}.{M} — {Name} — {one-line objective} — {est 2–6 tasks}`). Subagent single-shot — emit digest, do NOT pause; proceed to Phase 4.
> 4. Stage decomposition (new Stages only) — Per new Stage, author canonical Stage block per MASTER-PLAN-STRUCTURE.md §3 (Status / Notes / Backlog state / Objectives / Exit criteria / Art / Relevant surfaces / 5-column Task table + 2 pending subsections §Stage File Plan / §Plan Fix). Reuse Phase 2 MCP output. Stage ordering: scaffolding → data model → runtime logic → integration + tests. Task id format `T{N}.{M}.{K}`.
> 5. Cardinality gate (new Stages only) — `cardinality-gate-check` subskill. ≥2 Tasks/Stage hard, ≤6 soft. Violations pause for user. Do NOT re-gate existing Stages.
> 6. Persist (DB-only) — Operations in order:
>    - `master_plan_preamble_write({slug: SLUG, preamble: <merged preamble>})` — header sync.
>    - Per new Stage: `stage_insert({slug: SLUG, stage_id: "{N}.{M}", title: "{Name}", body: <Stage block markdown>, objective: "{Objectives prose}", exit_criteria: "{Exit criteria bullets}"})`.
>    - `master_plan_change_log_append({slug: SLUG, kind: "plan_extended", body: "Extended via {SOURCE_DOC} — +N stages, +M tasks"})`.
> 6b. Regenerate progress dashboard — `npm run progress` (repo root). Failure does NOT block Phase 6c — log exit code and continue.
> 6c. R6 demote (Final → In Progress) — If rendered preamble top Status was `Final` AND new Stages appended ≥1: re-fetch via `master_plan_render`, rewrite Status to `In Progress — Stage {N_first_new}.{M_first_new} pending (extensions appended)`, land via second `master_plan_preamble_write`. Idempotent.
> 7. Handoff + umbrella flip (if applicable) — Single caveman message with delta counts + new-Stage range + header-sync summary + invariants + gate results + umbrella child-row flip note (when applicable) + next-step call (`claude-personal "/stage-file {SLUG} Stage {START}.{M_first}"`).
>
> ## Tool recipe — Phase 2 only
>
> Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:
>
> - `keywords`: English tokens from source-doc Chosen Approach + Subsystem Impact + Architecture component names.
> - `brownfield_flag`: `true` for greenfield (skips router/spec_sections/invariants_summary). `false` for brownfield. Treat as brownfield if `Assets/**` paths referenced.
> - `tooling_only_flag`: `true` for tooling/pipeline-only plans.
>
> Run `list_specs` / `spec_outline` only if a routed domain references a spec whose sections weren't returned. Surface-path pre-check via `surface-path-precheck` subskill.
>
> ## Hard boundaries
>
> - Do NOT author new orchestrator — route to `/master-plan-new` if `master_plan_render` returns `not_found`.
> - Do NOT touch existing Stage rows in `ia_stages` — not even cosmetic edits.
> - Do NOT overwrite top-of-preamble `**Status:**` line — lifecycle skills flip it. (Exception: Phase 6c R6 demote.)
> - Do NOT persist with cardinality violations (<2 or >6 Tasks/Stage) unresolved.
> - Do NOT persist when duplication gate (Phase 1) trips on In Review+ Stage — route to revision cycle.
> - Do NOT persist when source introduces a locked decision that contradicts an existing Locked decision — route to revision cycle.
> - Do NOT insert BACKLOG rows. Do NOT create task spec stubs. Tasks stay `_pending_`.
> - Do NOT delete or rename source doc. Do NOT edit its expansion / extensions block.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message: `{SLUG}` extended with delta counts (`+N stages · +M tasks`); new Stage range `{START}.{M_first}..{END}.{M_last}`; header-sync summary (Exploration source + Locked decisions + invariants merged); cardinality + duplication gate outcomes; umbrella flip note (when applicable); next step `claude-personal "/stage-file {SLUG} Stage {START}.{M_first}"`.
