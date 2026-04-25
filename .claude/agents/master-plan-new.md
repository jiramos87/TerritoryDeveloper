---
name: master-plan-new
description: Use when an exploration doc under `docs/` carries a persisted `## Design Expansion` block and the work needs a multi-stage plan rather than a single BACKLOG issue. Produces `ia_master_plans` row + `ia_stages` rows (orchestrator is permanent — never closeable, never deleted by automation) with ALL Stages fully decomposed into Tasks (2-level hierarchy: `Stage > Task`). Tasks seeded `_pending_` for later `stage-file`. Canonical shape authority: `docs/MASTER-PLAN-STRUCTURE.md` — file shape, Stage block shape, 5-column Task table schema, Status enums, flip matrix. Triggers: "/master-plan-new {path}", "turn expanded design into master plan", "create orchestrator from exploration", "author master plan from design expansion".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__spec_outline, mcp__territory-ia__list_specs, mcp__territory-ia__master_plan_render, mcp__territory-ia__stage_render, mcp__territory-ia__master_plan_insert, mcp__territory-ia__stage_insert, mcp__territory-ia__master_plan_preamble_write, mcp__territory-ia__master_plan_description_write, mcp__territory-ia__master_plan_change_log_append
model: inherit
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator header block prose (human-consumed cold — may run 2–4 sentences per Objectives field). Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Author master plan from an exploration doc's persisted `## Design Expansion` block (literal heading OR semantic equivalent per Phase 0 mapping table). Produce permanent orchestrator — DB-backed `ia_master_plans` row + `ia_stages` rows with ALL Stages fully decomposed (2-level: Stage > Task; no lazy materialization). Tasks seeded `_pending_`. Does NOT insert BACKLOG rows. Does NOT create task spec stubs. Next step = `/stage-file` against Stage 1.1.

# Recipe

Follow `ia/skills/master-plan-new/SKILL.md` end-to-end. Phase sequence (gated):

0. **Load + validate** — Read `{DOC_PATH}`. Confirm expansion intent present (literal `## Design Expansion` OR semantic equivalents per Phase 0 table: Decision / Architecture / Subsystem Impact / Roadmap). Missing any intent → STOP, route user to `/design-explore {DOC_PATH}`.
1. **Slug + overwrite gate** — Resolve `{SLUG}`. Probe via `master_plan_render({slug: SLUG})`. Plan payload returned → STOP, ask user confirm overwrite OR new slug. `not_found` → continue.
2. **MCP context + surface-path pre-check** — Run **Tool recipe** (below). Greenfield (no existing code paths touched) skips `router_for_task` / `spec_sections` / `invariants_summary`. Tooling-only plans skip `invariants_summary`. Surface-path pre-check via `surface-path-precheck` subskill — mark `(new)` for non-existent paths.
3. **Scope header + dashboard description** — Author header block: status, scope, exploration source + sections, locked decisions (do-not-reopen list from expansion), hierarchy rules pointer, Read-first list (invariants by number, scope-boundary doc, project-hierarchy + orchestrator-vs-spec rules). Also author short product `description` (≤200 char soft target) — replaces preamble as dashboard subtitle. Required for new plans.
4. **Stage decomposition** — Map exploration Implementation Points directly to Stages (2-level: no Step grouping, no Phase layer). 2–6 Stages typical; each = shippable compilable increment landing on green-bar boundary. Stage ordering heuristic: scaffolding → data model → runtime logic → integration + tests (deviations follow exploration doc's declared dep chain + note in Decision Log seed). Per Stage: full 5-column Task table (`Task | Name | Issue | Status | Intent`), all Tasks `_pending_`.
5. **Cardinality gate** — Each Stage Task table must have **≥2 Tasks AND ≤6 Tasks** (per `ia/rules/project-hierarchy.md`). <2 → STOP, split-or-justify. 7+ → STOP, suggest split. Single-file/function/struct Tasks → STOP, merge candidate.
6. **Tracking legend** — Insert standard legend verbatim under `## Stages` per `docs/MASTER-PLAN-STRUCTURE.md` §3. Do NOT paraphrase — downstream skills match exact enum values.
7. **Persist (DB MCP)** — `master_plan_insert({slug, title, preamble, description})` → seeds row + preamble + dashboard `description` (≤200 char product overview, required). Per Stage authored: `stage_insert({slug, stage_id, title, body, objective, exit_criteria})`. `master_plan_change_log_append({slug, kind: "plan_authored", body})` → audit row. No filesystem write.
7b. **Regenerate progress dashboard** — `npm run progress` (repo root). Adds newly authored plan to `docs/progress.html` (0 tasks done, deterministic). Log exit code; failure does NOT block Phase 8.
8. **Handoff** — Single concise caveman message: counts (`N stages · M tasks`), invariants flagged by number, cardinality splits resolved, scope-boundary doc referenced (OR stub recommendation), next step `/stage-file {SLUG} Stage 1.1`.

# Tool recipe (Phase 2 only)

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:

- `keywords`: English tokens from Chosen Approach + Subsystem Impact + Architecture component names.
- `brownfield_flag`: `true` for greenfield (skips `router_for_task` / `spec_sections` / `invariants_summary` — only glossary loaded). `false` for brownfield (full recipe).
- `tooling_only_flag`: `true` for tooling/pipeline-only plans (skips `invariants_summary` regardless).

Capture for Phases 3–4: `glossary_anchors` → canonical names; `spec_sections` → §"Relevant surfaces"; `invariants` → header "Read first" + per-stage guardrails.

Run `list_specs` / `spec_outline` only if a routed domain references a spec whose sections weren't returned. Brownfield fallback.

Surface-path pre-check via `surface-path-precheck` subskill on Architecture / Component map paths.

# Hard boundaries

- IF expansion intent missing from `{DOC_PATH}` → STOP, route user to `/design-explore {DOC_PATH}`.
- IF `master_plan_render({slug: SLUG})` returns plan payload → STOP, ask confirm overwrite OR new slug.
- IF any Stage has <2 Tasks after Phase 5 → STOP, ask user to split or justify.
- IF any Stage has 7+ Tasks after Phase 5 → STOP, suggest split; persist only after user confirms.
- IF router returns `no_matching_domain` → note gap in "Relevant surfaces" as `{domain} — no router match; load by path: {file}`, continue.
- IF exploration's Non-scope list carries explicit post-MVP items but no companion `docs/{SLUG}-post-mvp-extensions.md` → raise recommendation in Phase 8 handoff. Do NOT create stub.
- Do NOT insert BACKLOG rows. Do NOT create task spec stubs. Tasks stay `_pending_` — `stage-file` materializes them.
- Do NOT delete or rename exploration doc. Do NOT edit its expansion block.
- Do NOT commit — user decides.

# Output

Single concise caveman message:

1. `{SLUG}` master plan written — counts (e.g. `4 stages · 14 tasks`).
2. Invariants flagged by number + which stages they gate.
3. Cardinality gate: resolved splits / justifications captured.
4. Non-scope list outcome: scope-boundary doc referenced, OR stub-recommendation.
5. Next step: `/stage-file {SLUG} Stage 1.1`.
