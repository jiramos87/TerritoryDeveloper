---
description: Expand an exploration doc into a reviewed, persisted design (pre-master-plan). Dispatches the `design-explore` subagent against `{DOC_PATH}` in isolated context.
argument-hint: "{DOC_PATH} [APPROACH_HINT]  (e.g. docs/explorations/foo.md C)"
---

# /design-explore — dispatch `design-explore` subagent

Use `design-explore` subagent (`.claude/agents/design-explore.md`) to run `ia/skills/design-explore/SKILL.md` end-to-end on `$ARGUMENTS`.

`$ARGUMENTS` = `{DOC_PATH} [APPROACH_HINT]`. First token = path to exploration `.md`. Optional second token = approach id (e.g. `C`) to skip Phase 2 gate.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "design-explore"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `design-explore` skill (`ia/skills/design-explore/SKILL.md`) end-to-end on the exploration doc given in `$ARGUMENTS`. Parse args: first token = `DOC_PATH`, optional second token = `APPROACH_HINT`. Resolve `DOC_PATH` via Read — if unreadable, stop and report path error.
>
> ## Phase sequence (gated)
> First welcome the user, briefly explain process and mention exact LLM model being used (with version number)
> 0. Load doc — extract problem statement, approaches list, existing recommendation, open questions.
> 1. Compare — criteria matrix (constraint fit, effort, output control, maintainability, dependencies/risk) as Markdown table.
> 2. Select — if recommendation unambiguous AND no `APPROACH_HINT` → proceed. Else → present table + leading candidate, PAUSE, ask user confirm/override.
> 3. Expand — components (one-line responsibility each), data flow, interfaces/contracts, non-scope.
> 4. Architecture — Mermaid (`flowchart LR` / `graph TD`) + entry/exit points. >20 nodes → ASCII + simplified Mermaid.
> 5. Subsystem impact — Tool recipe below. Per subsystem: dependency nature, invariant risk (`ia/rules/invariants.md` by number), breaking vs additive, mitigation.
> 6. Implementation points — phased checklist ordered by dependency + "Deferred / out of scope".
> 7. Examples — ≥1 input + ≥1 output + ≥1 edge case for most non-obvious piece.
> 8. Subagent review — spawn `Plan` subagent via Agent tool per SKILL.md prompt. Resolve BLOCKING before persist. Copy NON-BLOCKING + SUGGESTIONS verbatim into Review Notes.
> 9. Persist — detect existing `## Design Expansion` in `DOC_PATH` → update in place between header and next `---`. Else → append after `---` following last section. Never overwrite Problem / Approaches surveyed / Recommendation / Open questions.
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
> - Do NOT overwrite Problem / Approaches surveyed / Recommendation / Open questions sections.
> - Do NOT create master plan, BACKLOG row, or invoke `project-new` — propose as next step only.
> - Do NOT commit — user decides.
> - Do NOT load whole reference specs when slices suffice.
>
> ## Output
>
> Single concise caveman message: doc path + approach selected, phases completed (skipped + reason), subsystem impact summary (count + invariants flagged by number), review results (BLOCKING resolved, NON-BLOCKING carried), persist diff summary (sections written / updated), next step (`claude-personal "/master-plan-new {DOC_PATH}"` for multi-stage work, or `claude-personal "/project-new ..."` for single issue).
