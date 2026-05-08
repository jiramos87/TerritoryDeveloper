---
description: Sequential design-explore → ship-plan pipeline. Expands an exploration doc into a persisted Design Expansion + lean YAML frontmatter, then bulk-authors the master plan into the DB. Stops between stages for review if design-explore surfaces a blocking item.
argument-hint: "{DOC_PATH} [APPROACH_HINT] [SLUG]  (e.g. docs/explorations/foo.md C foo)"
---

# /explore-plan — sequential design-explore → ship-plan

Orchestrate both pre-implementation authoring stages for an exploration doc. Run each stage by dispatching the matching subagent via the Agent tool. **Do NOT run stages in parallel — ship-plan requires the lean YAML frontmatter persisted by design-explore.**

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

## Stage 2 — Ship-plan (`ship-plan`)

Dispatch Agent with `subagent_type: "ship-plan"`:

> ## Mission
>
> Run `ship-plan` skill (`ia/skills/ship-plan/SKILL.md`) end-to-end. Input: SLUG={SLUG} (derived from DOC_PATH filename stem if not provided explicitly).
>
> Read `docs/explorations/{SLUG}.md`. Lean YAML frontmatter must be present (emitted by design-explore). Run all 8 phases per SKILL.md.
>
> ## Hard boundaries
>
> - Do NOT start if lean YAML frontmatter missing from exploration doc — STOP + report.
> - Do NOT write task spec bodies to filesystem — DB sole source of truth.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman summary per ship-plan Phase 8 shape. End with `PLAN_DONE: {SLUG}` on success.

---

## Pipeline summary output

After both stages complete (or on stop), emit a single summary:

```
EXPLORE-PLAN {DOC_PATH}: {PASSED|STOPPED}
  Stage 1 design-explore:  {done|failed}
  Stage 2 master-plan-new: {done|failed|skipped}
  Next: claude-personal "/stage-file {SLUG}-master-plan.md Stage 1.1"
```
