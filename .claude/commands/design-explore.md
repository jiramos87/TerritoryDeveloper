---
description: Use when an exploration doc (under docs/) needs to move from fuzzy survey to a defined, detailed, reviewed design ready to seed a master plan or BACKLOG issue. Phases: compare approaches → select → expand → architecture → subsystem impact → implementation points → examples → subagent review → persist. Triggers: "/design-explore [path]", "expand exploration", "design review [doc]", "turn this exploration into a design", "compare and select approach", "take this exploration doc to a master plan".
argument-hint: "{DOC_PATH} [APPROACH_HINT] [--against REFERENCE_DOC] [--force-model {model}] [--resume {slug}] (e.g. docs/foo.md C OR docs/foo.md --against docs/full-game-mvp-exploration.md OR --resume ship-protocol)"
---

# /design-explore — Use before a master plan or backlog issue exists: survey approaches in an exploration doc, select one, expand with architecture + subsystem impact + implementation points + examples, review with a subagent, and persist back to the same doc.

Drive `$ARGUMENTS` via the [`design-explore`](../agents/design-explore.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /design-explore [path]
- expand exploration
- design review [doc]
- turn this exploration into a design
- compare and select approach
- take this exploration doc to a master plan
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{DOC_PATH} [APPROACH_HINT] [--against {REFERENCE_DOC}] [--force-model {model}]`. First token = path to exploration `.md`. Optional second token = approach id (e.g. `C`) to skip Phase 2 gate. Optional `--against {REFERENCE_DOC}` = path to an umbrella orchestrator or master plan — activates **gap-analysis mode** when `DOC_PATH` is a locked design with no Approaches list. Optional `--force-model {model}` (valid: `sonnet`, `opus`, `haiku`): store as `FORCE_MODEL` and pass to the Agent dispatch + embed as `FORCE_MODEL_OVERRIDE` in the subagent prompt so Phase 8's Plan subagent inherits it. Absent or invalid → `FORCE_MODEL` unset.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "design-explore"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, Mermaid / diagram blocks persisted to the doc. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `design-explore` skill (`ia/skills/design-explore/SKILL.md`) end-to-end on the exploration doc given in `$ARGUMENTS`. Parse args: first token = `DOC_PATH`, optional second token = `APPROACH_HINT` (if not `--against`), optional `--against {AGAINST_DOC}`. Resolve `DOC_PATH` via Read — if unreadable, stop and report path error.
>
> ## Phase sequence (gated)
> First welcome the user, briefly explain process and mention exact LLM model being used (with version number).
>
> ### Locked-doc detection (end of Phase 0)
>
> After loading `DOC_PATH`, evaluate doc structure:
> - Has Approaches list → **standard mode**, continue Phase 1.
> - No Approaches list + `--against {AGAINST_DOC}` set → **gap-analysis mode** (see below).
> - No Approaches list + no `--against` → STOP, offer: (A) add Approaches section + re-run, (B) re-run with `--against {UMBRELLA_DOC}`, (C) skip to `/master-plan-extend` if no gaps expected.
>
> ### Standard mode (doc has Approaches list)
>
> 0. Load doc — extract problem statement, approaches list, existing recommendation, open questions.
> 0.5. Interview (user gate) — Before Phase 1, run a short interview. Ask **ONE question per turn, stop, wait for the user's answer** before asking the next. Do NOT list questions. Pull from: (1) open questions in the doc, (2) up to 3 inferred questions about scope boundaries, blocking constraints, or priority trade-offs. Max 5 questions; stop early if answers already cover remaining ones. After the last answer emit a one-paragraph summary, then proceed without another confirmation prompt.
> 1. Compare — criteria matrix (constraint fit, effort, output control, maintainability, dependencies/risk) as Markdown table.
> 2. Select — if recommendation unambiguous AND no `APPROACH_HINT` → proceed. Else → present table + leading candidate, PAUSE, ask user confirm/override.
> 2.5. Architecture Decision (DEC-A15 lock) — fires when selected approach touches `arch_surfaces`. Skip-clause: zero arch hits → silent no-op. 4 sequential AskUserQuestion polls (slug → rationale → alternatives → affected `arch_surfaces[]`). MCP writes: `arch_decision_write` (status=active) → `cron_arch_changelog_append_enqueue` (kind=`design_explore_decision`, fire-and-forget; cron drains to `arch_changelog`) → `arch_drift_scan` against open master plans. Drift report appended inline under `### Architecture Decision` block. Stop on any MCP write failure.
> 3. Expand — components (one-line responsibility each), data flow, interfaces/contracts, non-scope.
> 4. Architecture — Mermaid (`flowchart LR` / `graph TD`) + entry/exit points. >20 nodes → ASCII + simplified Mermaid.
> 5. Subsystem impact — Tool recipe below. Per subsystem: dependency nature, invariant risk (`ia/rules/invariants.md` by number), breaking vs additive, mitigation.
> 6. Implementation points — phased checklist ordered by dependency + "Deferred / out of scope".
> 7. Examples — ≥1 input + ≥1 output + ≥1 edge case for most non-obvious piece.
> 8. Subagent review — spawn `Plan` subagent via Agent tool per SKILL.md prompt. Resolve BLOCKING before persist. Copy NON-BLOCKING + SUGGESTIONS verbatim into Review Notes.
> 9. Persist — detect existing `## Design Expansion` in `DOC_PATH` → update in place between header and next `---`. Else → append after `---` following last section. Never overwrite Problem / Approaches surveyed / Recommendation / Open questions.
>
> ### Gap-analysis mode (`--against` set, locked doc)
>
> 0b. Load `AGAINST_DOC` — extract all cross-references to the system in `DOC_PATH`: exit gates, tier conditions, interface contracts, locked decisions. Assign requirement ids (R1, R2, …).
> 1g. Gap inventory — compare requirements vs current design. Gap table: `Req | Source | Current coverage | Gap severity (Blocking/Additive/Deferred)`.
> 2g. Confirm gate — present gap table. PAUSE — ask user to confirm/trim gaps before expanding.
> 3–7. Expand gaps — same as standard Phases 3–7, scoped to confirmed gaps. Skip Phase 4 if no new components introduced.
> 8. Subagent review — same prompt template.
> 9g. Persist — derive context title from `AGAINST_DOC` filename (e.g. `full-game-mvp-exploration.md` → `## Design Expansion — MVP Alignment`; bare master-plan slug `full-game-mvp` → same). Append as new named section after any existing `## Design Expansion` block; never overwrite it or original sections.
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
> ## Model override (Phase 8 propagation)
>
> If `FORCE_MODEL_OVERRIDE={model}` is present in these inputs, pass `model: "{model}"` to the `Agent` tool call in Phase 8 (subagent review). Absent → Phase 8 Agent call uses no model override (subagent frontmatter wins).
>
> ## Hard boundaries
>
> - Do NOT guess approach when Phase 2 gate open — ask user.
> - Do NOT proceed past Phase 2g (gap-analysis) without user confirming gap list.
> - Do NOT persist with unresolved BLOCKING review items — re-run Phase 8.
> - Do NOT overwrite Problem / Approaches surveyed / Recommendation / Open questions / any prior Design Expansion block.
> - Do NOT create master plan, BACKLOG row, or invoke `project-new` — propose as next step only.
> - Do NOT commit — user decides.
> - Do NOT load whole reference specs when slices suffice.
>
> ## Output
>
> Single concise caveman message: doc path + mode (standard: approach id; gap-analysis: gap count confirmed), phases completed (skipped + reason), subsystem impact summary (count + invariants flagged by number), review results (BLOCKING resolved, NON-BLOCKING carried), persist diff summary (sections written / updated), next step (standard: `claude-personal "/ship-plan {SLUG}"` or `"/project-new ..."`; gap-analysis: `claude-personal "/ship-plan --version-bump {SLUG} {DOC_PATH}"`).
