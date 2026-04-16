---
description: Sequential design-explore → master-plan-new pipeline. Expands an exploration doc into a persisted Design Expansion, then authors the orchestrator master plan. Stops between stages for review if design-explore surfaces a blocking item.
argument-hint: "{DOC_PATH} [APPROACH_HINT] [SLUG] [SCOPE_BOUNDARY_DOC]  (e.g. docs/explorations/foo.md C foo docs/foo-post-mvp-extensions.md)"
---

# /explore-plan — sequential design-explore → master-plan-new

Orchestrate both pre-implementation authoring stages for an exploration doc. Run each stage by dispatching the matching subagent via the Agent tool. **Do NOT run stages in parallel — master-plan-new requires the persisted Design Expansion from design-explore.**

Follow `caveman:caveman` for all your own output and all dispatched subagents below. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, Mermaid / diagram blocks. Anchor: `ia/rules/agent-output-caveman.md`.

## Argument parsing

`$ARGUMENTS` = `{DOC_PATH} [APPROACH_HINT] [SLUG] [SCOPE_BOUNDARY_DOC]`

- `DOC_PATH` (required) — first token; path to exploration `.md` under `docs/`.
- `APPROACH_HINT` (optional) — second token if it looks like a single letter (e.g. `A`, `B`, `C`) or an approach id. Pass to design-explore to skip Phase 2 gate.
- `SLUG` (optional) — third token (kebab-case); passed to master-plan-new as slug override. Defaults to exploration doc filename stem stripped of `-exploration` / `-design` suffix.
- `SCOPE_BOUNDARY_DOC` (optional) — fourth token; path to scope-boundary doc, forwarded to master-plan-new.

Parse these from `$ARGUMENTS` before dispatching either subagent.

---

## Stage 1 — Design-explore (`design-explore`)

Dispatch Agent with `subagent_type: "design-explore"`:

> ## Mission
>
> Run `design-explore` skill (`ia/skills/design-explore/SKILL.md`) end-to-end on the exploration doc `{DOC_PATH}`. Approach hint (if provided): `{APPROACH_HINT}`.
>
> ## Phase sequence (gated)
>
> 0. Load doc — extract problem statement, approaches list, existing recommendation, open questions.
> 1. Compare — criteria matrix (constraint fit, effort, output control, maintainability, dependencies/risk) as Markdown table.
> 2. Select — if recommendation unambiguous AND no approach hint → proceed. Else → present table + leading candidate, PAUSE, ask user confirm/override.
> 3. Expand — components (one-line responsibility each), data flow, interfaces/contracts, non-scope.
> 4. Architecture — Mermaid (`flowchart LR` / `graph TD`) + entry/exit points. >20 nodes → ASCII + simplified Mermaid.
> 5. Subsystem impact — Tool recipe below. Per subsystem: dependency nature, invariant risk (`ia/rules/invariants.md` by number), breaking vs additive, mitigation.
> 6. Implementation points — phased checklist ordered by dependency + "Deferred / out of scope".
> 7. Examples — ≥1 input + ≥1 output + ≥1 edge case for most non-obvious piece.
> 8. Subagent review — spawn `Plan` subagent via Agent tool. Resolve BLOCKING before persist. Copy NON-BLOCKING + SUGGESTIONS verbatim into Review Notes.
> 9. Persist — detect existing `## Design Expansion` in `DOC_PATH` → update in place. Else → append after last `---`. Never overwrite Problem / Approaches surveyed / Recommendation / Open questions.
>
> ## Tool recipe — Phase 5 only
>
> Skip `invariants_summary` for tooling/pipeline-only designs touching no runtime C#.
>
> 1. `mcp__territory-ia__glossary_discover` — English keywords array from approach components + interface names.
> 2. `mcp__territory-ia__glossary_lookup` — high-confidence terms.
> 3. `mcp__territory-ia__router_for_task` — 1–3 domains from component responsibilities.
> 4. `mcp__territory-ia__spec_sections` — implied subsystem sections; set `max_chars`. No full spec reads.
> 5. `mcp__territory-ia__invariants_summary` — if runtime C# / Unity touched.
>
> ## Hard boundaries
>
> - Do NOT guess approach when Phase 2 gate open — ask user.
> - Do NOT persist with unresolved BLOCKING review items — re-run Phase 8.
> - Do NOT overwrite Problem / Approaches surveyed / Recommendation / Open questions.
> - Do NOT create master plan, BACKLOG row, or invoke `project-new`.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message: doc path, approach selected, phases completed (skipped + reason), subsystem impact summary, review results (BLOCKING resolved, NON-BLOCKING carried), persist diff summary. End with "EXPLORE_DONE: {DOC_PATH}" if Design Expansion persisted successfully, or "EXPLORE_BLOCKED: {reason}" if a blocker remains.

**Gate:** output must contain `EXPLORE_DONE`. If `EXPLORE_BLOCKED`, STOP the pipeline and report: `EXPLORE-PLAN STOPPED at design-explore — {reason}. Resolve blocker then re-run /explore-plan.`. Do not proceed to Stage 2.

---

## Stage 2 — Master-plan-new (`master-plan-new`)

Dispatch Agent with `subagent_type: "master-plan-new"`:

> ## Mission
>
> Run `master-plan-new` skill (`ia/skills/master-plan-new/SKILL.md`) end-to-end. Inputs: `DOC_PATH={DOC_PATH}`, `SLUG={SLUG}` (if provided), `SCOPE_BOUNDARY_DOC={SCOPE_BOUNDARY_DOC}` (if provided).
>
> ## Phase sequence (gated)
>
> 0. Load + validate — Read `DOC_PATH`. Confirm `## Design Expansion` present (literal or semantic equivalent per Phase 0 mapping table in SKILL.md). Missing → STOP, report path error.
> 1. Slug + overwrite gate — Resolve `SLUG`. `ia/projects/{SLUG}-master-plan.md` exists → STOP, ask user confirm overwrite OR new slug.
> 2. MCP context + surface-path pre-check — Tool recipe below. Greenfield skips `router_for_task` / `spec_sections` / `invariants_summary`. Tooling/pipeline-only plans skip `invariants_summary`. Glob every entry/exit point from Architecture; mark `(new)` for non-existent paths.
> 3. Scope header — author header block: Status, Scope, Exploration source + sections, Locked decisions, Hierarchy rules pointer, Read-first list.
> 4. Step decomposition — group Implementation Points into 1–4 steps. Step 1 full; Steps 2+ skeletons only.
> 5. Stage decomposition — per Step 1 only, 2–4 stages each landing on a green-bar boundary.
> 6. Cardinality gate — every phase: ≥2 tasks AND ≤6 tasks. Violations → warn + pause.
> 7. Tracking legend — insert standard legend verbatim under `## Steps` (copy from `blip-master-plan.md` line 22).
> 8. Persist — write `ia/projects/{SLUG}-master-plan.md`.
> 8b. Regenerate progress dashboard — `npm run progress` (repo root). Reflects new plan in `docs/progress.html`. Deterministic; failure does NOT block Phase 9 — log exit code and continue.
> 9. Handoff — single caveman message with counts + invariants + gate results + next-step call.
>
> ## Tool recipe — Phase 2 only
>
> Greenfield skips steps 3–5. Tooling/pipeline-only plans skip step 5.
>
> 1. `mcp__territory-ia__glossary_discover` — English keywords from Chosen Approach + Subsystem Impact.
> 2. `mcp__territory-ia__glossary_lookup` — high-confidence terms.
> 3. `mcp__territory-ia__router_for_task` — 1–3 domains.
> 4. `mcp__territory-ia__spec_sections` — implied subsystem sections; set `max_chars`. No full spec reads.
> 5. `mcp__territory-ia__invariants_summary` — if Subsystem Impact flags runtime C# / Unity.
> 6. `mcp__territory-ia__list_specs` / `mcp__territory-ia__spec_outline` — fallback only.
>
> ## Hard boundaries
>
> - Do NOT author master plan when Phase 0 expansion gate unmet.
> - Do NOT silently overwrite existing `ia/projects/{SLUG}-master-plan.md`.
> - Do NOT persist with cardinality violations unresolved.
> - Do NOT insert BACKLOG rows or create `ia/projects/{ISSUE_ID}.md` specs.
> - Do NOT pre-decompose Steps 2+ — skeletons only.
> - Do NOT delete or rename exploration doc.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message: `{SLUG}-master-plan.md` written, counts (N steps · M stages · P phases · Q tasks), deferred steps named, invariants flagged, cardinality splits resolved, next step `claude-personal "/stage-file {SLUG}-master-plan.md Stage 1.1"`.

---

## Pipeline summary output

After both stages complete (or on stop), emit a single summary:

```
EXPLORE-PLAN {DOC_PATH}: {PASSED|STOPPED}
  Stage 1 design-explore:  {done|failed}
  Stage 2 master-plan-new: {done|failed|skipped}
  Next: claude-personal "/stage-file {SLUG}-master-plan.md Stage 1.1"
```
