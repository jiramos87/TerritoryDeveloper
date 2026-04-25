---
description: Use when an existing master plan orchestrator needs new Stages sourced from an exploration doc (with persisted `## Design Expansion`) OR an extensions doc (e.g. `{slug}-post-mvp-extensions.md`) that was deferred at original author time. Appends new Stage blocks in place — never rewrites existing Stages, never overwrites headers, never inserts BACKLOG rows. Fully decomposes every new Stage (Task table) at author time — no skeletons. 2-level hierarchy `Stage > Task` (Step + Phase layers removed per lifecycle-refactor). Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`. Triggers: "/master-plan-extend {plan} {source}", "extend master plan from exploration", "add new stages to orchestrator", "append from extensions doc", "pull deferred stage into master plan".
argument-hint: "{ORCHESTRATOR_SPEC} {SOURCE_DOC} [START_STEP_NUMBER] [SCOPE_BOUNDARY_DOC] (e.g. ia/projects/blip-master-plan.md docs/blip-post-mvp-extensions.md)"
---

# /master-plan-extend — Extend an existing `ia/projects/{slug}-master-plan.md` with new Stages sourced from an exploration or extensions doc. Appends — never rewrites existing Stages. Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`.

Drive `$ARGUMENTS` via the [`master-plan-extend`](../agents/master-plan-extend.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator header block prose (human-consumed cold — may run 2–4 sentences per Objectives field). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /master-plan-extend {plan} {source}
- extend master plan from exploration
- add new stages to orchestrator
- append from extensions doc
- pull deferred stage into master plan
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{ORCHESTRATOR_SPEC} {SOURCE_DOC} [START_STEP_NUMBER] [SCOPE_BOUNDARY_DOC]`. First token = path to existing master plan (must exist; must match orchestrator shape). Second token = path to exploration doc with persisted `## Design Expansion` (or semantic equivalent) OR extensions doc listing deferred Steps. Optional third token = `START_STEP_NUMBER` integer override (gated `>` last existing step; default = last + 1). Optional fourth token = scope-boundary doc path.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "master-plan-extend"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc, orchestrator header prose (Objectives fields may run 2–4 sentences — human-consumed cold). Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `master-plan-extend` skill (`ia/skills/master-plan-extend/SKILL.md`) end-to-end on the target master plan + source doc given in `$ARGUMENTS`. Parse args: first token = `ORCHESTRATOR_SPEC`, second token = `SOURCE_DOC`, optional third token = `START_STEP_NUMBER` override, optional fourth token = `SCOPE_BOUNDARY_DOC`. Resolve both paths via Read — any unreadable → STOP and report path error.
>
> ## Phase sequence (gated)
>
> 0. Load + validate — Read `ORCHESTRATOR_SPEC`. Confirm orchestrator shape (header + `## Steps` + tracking legend + `## Orchestration guardrails`). Read `SOURCE_DOC`. Confirm expansion intent present (literal `## Design Expansion` OR semantic equivalents per Phase 0 table). Missing orchestrator shape → STOP, route to `/master-plan-new {SOURCE_DOC}`. Missing source expansion intent → STOP, route to `/design-explore {SOURCE_DOC}`.
> 1. Start-number resolution + duplication gate — Compute `START_STEP_NUMBER`. Scan proposed new step names against existing `### Step {N} — {Name}` blocks. Collision OR >50% objective token overlap → STOP, ask rename / drop / confirm.
> 2. MCP context + surface-path pre-check — Run **Tool recipe** (below). Greenfield skips `router_for_task` / `spec_sections` / `invariants_summary`. Tooling-only plans skip `invariants_summary`. Glob every entry/exit point from source-doc Architecture; mark `(new)` for non-existent paths.
> 3. New-step proposal + user confirm — Emit caveman one-liner outline per proposed new step. Pause for user confirm on ordering / names / scope boundary BEFORE full decomposition. Re-emit until confirmed.
> 4. Step decomposition (new steps only) — Per confirmed new step, author Step block shape matching `ia/templates/master-plan-template.md` (Status / Backlog state / Objectives / Exit criteria / Art / Relevant surfaces). All new steps decomposed in full — no skeletons.
> 5. Stage decomposition (new steps only) — Per step, 2–4 stages each landing on green-bar boundary. Reuse Phase 2 MCP output. Apply `ia/skills/stage-decompose/SKILL.md` Phase 2 rules. Ordering: scaffolding → data model → runtime logic → integration + tests. 6-column task table (`Task | Name | Phase | Issue | Status | Intent`).
> 6. Cardinality gate (new stages only) — ≥2 tasks AND ≤6 tasks per phase. Violations pause for user. Do NOT re-gate existing stages.
> 7. Persist in place — Edit `ORCHESTRATOR_SPEC`. (a) Header sync — update `**Last updated:**`, append `SOURCE_DOC` to `**Exploration source:**` + Read-first if absent, merge new Locked decisions, merge new invariant numbers. (b) Insert new Step blocks immediately before the `---` that precedes `## Orchestration guardrails`. (c) Leave Orchestration guardrails intact unless source doc introduces new guardrail.
> 7b. Regenerate progress dashboard — `npm run progress` (repo root). Deterministic; failure does NOT block Phase 8 — log exit code and continue.
> 8. Handoff — Single caveman message with delta counts + new-step range + header-sync summary + invariants + gate results + next-step call (`claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1"`).
>
> ## Tool recipe — Phase 2 only
>
> Greenfield skips steps 3–5. Tooling/pipeline-only plans skip step 5 regardless.
>
> 1. `mcp__territory-ia__glossary_discover` — English keywords array from source-doc Chosen Approach + Subsystem Impact + Architecture component names.
> 2. `mcp__territory-ia__glossary_lookup` — high-confidence terms from discover.
> 3. `mcp__territory-ia__router_for_task` — 1–3 domains from source-doc Subsystem Impact entries.
> 4. `mcp__territory-ia__spec_sections` — sections implied by routed subsystems; set `max_chars`. No full spec reads.
> 5. `mcp__territory-ia__invariants_summary` — if source-doc Subsystem Impact flags runtime C# / Unity subsystems.
> 6. `mcp__territory-ia__list_specs` / `mcp__territory-ia__spec_outline` — fallback only.
>
> **Surface-path pre-check (Glob, Phase 2 sub-step):** per entry/exit point in source-doc Architecture, Glob existing paths. Existing → note line refs. New → mark `(new)`. Ambiguous → Grep for plausible type names.
>
> ## Hard boundaries
>
> - Do NOT author new orchestrator — route to `/master-plan-new` if target doesn't exist.
> - Do NOT touch existing `### Step 1..(START-1)` blocks — not even cosmetic edits.
> - Do NOT overwrite orchestrator `**Status:**` line — lifecycle skills flip it.
> - Do NOT persist with cardinality violations (<2 or >6 tasks/phase) unresolved.
> - Do NOT persist when duplication gate (Phase 1) trips — ask user to rename / drop / confirm.
> - Do NOT persist when source introduces a locked decision that contradicts an existing Locked decision — route to revision cycle.
> - Do NOT insert BACKLOG rows. Do NOT create `ia/projects/{ISSUE_ID}.md` specs. Tasks stay `_pending_`.
> - Do NOT delete or rename source doc. Do NOT edit its expansion / extensions block.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message: `{ORCHESTRATOR_SPEC}` extended with delta counts (`+N steps · +M stages · +P phases · +Q tasks`); new Step range `{START}..{END}`; header-sync summary (Exploration source + Locked decisions + invariants merged); cardinality + duplication gate outcomes; next step `claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1"`.
