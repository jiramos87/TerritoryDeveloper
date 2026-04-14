---
description: Author `ia/projects/{slug}-master-plan.md` orchestrator from an exploration doc carrying a persisted `## Design Expansion` (or semantic equivalent). Dispatches the `master-plan-new` subagent against `{DOC_PATH}` in isolated context.
argument-hint: "{DOC_PATH} [SLUG] [SCOPE_BOUNDARY_DOC]  (e.g. docs/foo-exploration.md foo docs/foo-post-mvp-extensions.md)"
---

# /master-plan-new — dispatch `master-plan-new` subagent

Use `master-plan-new` subagent (`.claude/agents/master-plan-new.md`) to run `ia/skills/master-plan-new/SKILL.md` end-to-end on `$ARGUMENTS`.

`$ARGUMENTS` = `{DOC_PATH} [SLUG] [SCOPE_BOUNDARY_DOC]`. First token = path to exploration `.md` with persisted `## Design Expansion` (or semantic equivalent). Optional second token = slug override (kebab-case stem for `ia/projects/{SLUG}-master-plan.md`; defaults to exploration doc filename stem stripped of `-exploration` / `-design` suffix). Optional third token = scope-boundary doc path (e.g. `docs/{SLUG}-post-mvp-extensions.md`).

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
> 1. Slug + overwrite gate — Resolve `SLUG`. `ia/projects/{SLUG}-master-plan.md` exists already → STOP, ask user confirm overwrite OR new slug. Never silently overwrite an orchestrator doc.
> 2. MCP context + surface-path pre-check — Run **Tool recipe** (below). Greenfield (new subsystem, no existing code paths touched) skips `router_for_task` / `spec_sections` / `invariants_summary`. Tooling/pipeline-only plans skip `invariants_summary`. Glob every entry/exit point from Architecture; mark `(new)` for non-existent paths.
> 3. Scope header — Author header block verbatim shape: Status, Scope, Exploration source + sections, Locked decisions, Hierarchy rules pointer, Read-first list (invariants by number from Phase 2, scope-boundary doc if provided).
> 4. Step decomposition — Group Implementation Points phases into 1–4 steps. Step 1 decomposed in full; Steps 2+ stay as skeletons (Objectives + Exit + "decomposition deferred until Step {N-1} closes"). Lazy materialization.
> 5. Stage decomposition — Per Step 1 only, 2–4 stages each landing on a green-bar boundary. Ordering heuristic: scaffolding → data model → runtime logic → integration + tests (unless exploration doc's declared dep chain overrides).
> 6. Cardinality gate — Every phase in a stage task table: **≥2 tasks AND ≤6 tasks**. Phase with 1 → warn + pause for split-or-justify. Phase with 0 → strip OR add tasks. Phase with 7+ → warn + suggest split. Proceed only after user confirms or fixes.
> 7. Tracking legend — Insert standard legend verbatim under `## Steps` (copy from `blip-master-plan.md` line 22). Do NOT paraphrase.
> 8. Persist — Write `ia/projects/{SLUG}-master-plan.md`. Order: header → `---` → `## Steps` + legend → Step 1 (full) → stages → Steps 2+ (skeletons) → `---` → `## Deferred decomposition` → `---` → `## Orchestration guardrails` → final `---`.
> 9. Handoff — Single caveman message with counts + invariants + gate results + next-step call (`/stage-file {SLUG}-master-plan.md Stage 1.1`).
>
> ## Tool recipe — Phase 2 only
>
> Greenfield skips steps 3–5. Tooling/pipeline-only plans skip step 5 regardless.
>
> 1. `mcp__territory-ia__glossary_discover` — English keywords array from Chosen Approach + Subsystem Impact + Architecture component names.
> 2. `mcp__territory-ia__glossary_lookup` — high-confidence terms from discover.
> 3. `mcp__territory-ia__router_for_task` — 1–3 domains from Subsystem Impact entries.
> 4. `mcp__territory-ia__spec_sections` — sections implied by routed subsystems; set `max_chars`. No full spec reads.
> 5. `mcp__territory-ia__invariants_summary` — if Subsystem Impact flags runtime C# / Unity subsystems.
> 6. `mcp__territory-ia__list_specs` / `mcp__territory-ia__spec_outline` — fallback only.
>
> **Surface-path pre-check (Glob, Phase 2 sub-step):** per entry/exit point in Architecture, Glob existing paths. Existing → note line refs. New → mark `(new)`. Ambiguous → Grep for plausible type names.
>
> ## Hard boundaries
>
> - Do NOT author master plan when Phase 0 expansion gate unmet — route to `/design-explore` first.
> - Do NOT silently overwrite existing `ia/projects/{SLUG}-master-plan.md` — orchestrators are permanent.
> - Do NOT persist with cardinality violations (<2 or >6 tasks/phase) unresolved.
> - Do NOT insert BACKLOG rows. Do NOT create `ia/projects/{ISSUE_ID}.md` specs. Tasks stay `_pending_`.
> - Do NOT pre-decompose Steps 2+ — skeletons only until Step 1 closes.
> - Do NOT delete or rename exploration doc. Do NOT edit its expansion block.
> - Do NOT create scope-boundary stub if missing — raise recommendation only.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message: `{SLUG}-master-plan.md` written with counts (`N steps · M stages · P phases · Q tasks`); deferred steps named; invariants flagged by number + gated stages; cardinality splits resolved; scope-boundary-doc outcome; next step `/stage-file {SLUG}-master-plan.md Stage 1.1`.
