## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "stage-decompose"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-decompose/SKILL.md` end-to-end for `$ARGUMENTS`. **Argument order:** first token is the orchestrator spec path (`ORCHESTRATOR_SPEC`, e.g. `ia/projects/blip-master-plan.md`); second token is the step id (`STEP_ID`, e.g. `Step 2` or `2`). Glob-resolve as fallback only when path is omitted and exactly one `*-master-plan.md` exists. `STEP_ID` must be ≥ 2.
>
> ## Phase loop
>
> 1. **Phase 0** — Read orchestrator; locate `### Step {STEP_ID}` block; confirm skeleton (no task table). Extract: Step Name, Objectives, Exit criteria, Relevant surfaces, Art, Stage hints from `## Deferred decomposition`. If step already has a task table → STOP and report; ask user to confirm overwrite.
> 2. **Phase 1** — MCP Tool recipe: `glossary_discover` → `glossary_lookup` → `router_for_task` (brownfield) → `spec_sections` (brownfield) → `invariants_summary` (C# only). Surface-path pre-check via Glob.
> 3. **Phase 2** — Decompose into 2–4 stages (ordering: scaffolding → data model → runtime → integration+tests). Per stage: Objectives + Exit + Phases + Tasks table. Task intent cites concrete types/methods/paths. Sizing: 2–5 files = correct; ≤1 file = merge; >3 subsystems = split.
> 4. **Phase 3** — Cardinality gate: ≥2 tasks per phase (1 → warn + pause); ≤6 soft (7+ → warn + pause). Single-file/function tasks → warn + pause. Proceed only after user confirms.
> 5. **Phase 4** — Edit orchestrator in-place (three ops): (a) replace skeleton block with full decomposition; (b) update `## Deferred decomposition` bullet; (c) update master plan header Status if needed.
> 6. **Phase 5** — `npm run progress`. Log exit; non-zero does NOT block.
>
> ## Hard boundaries
>
> - Do NOT decompose Step 1.
> - Do NOT create BACKLOG rows or `ia/projects/{ISSUE_ID}.md` stubs.
> - Do NOT decompose steps beyond `STEP_ID`.
> - Do NOT overwrite an already-decomposed step without user confirmation.
> - Do NOT persist with any phase having <2 tasks without confirmation.
> - Do NOT commit.
>
> ## Output
>
> Single caveman message: orchestrator edited, step N decomposed (N stages · M phases · K tasks, all `_pending_`), cardinality gate outcome, deferred section updated, next step (`claude-personal "/stage-file {spec} Stage {STEP_ID}.1"` when Step {STEP_ID-1} closes).
