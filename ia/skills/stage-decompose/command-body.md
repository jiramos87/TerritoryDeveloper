## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{SLUG}` (bare master-plan slug, e.g. `blip`). Second token = `{STAGE_ID}` (e.g. `Stage 2.3` → `2.3`). Missing either → print usage + abort.

Verify slug exists via `master_plan_state(slug=SLUG)`. Missing → STOPPED + `Next: claude-personal "/master-plan-new ..."` handoff.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "stage-decompose"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-decompose/SKILL.md` end-to-end for slug `{SLUG}` Stage `{STAGE_ID}`.
>
> ## Phase loop
>
> 1. **Phase 0** — Load Stage block via `stage_render(slug, stage_id)`; confirm skeleton (no task table). Extract: Stage Name, Objectives, Exit criteria, Relevant surfaces, Art, Task hints. If Stage already has a complete task table → STOP and report; ask user to confirm overwrite.
> 2. **Phase 1** — MCP Tool recipe: `glossary_discover` → `glossary_lookup` → `router_for_task` (brownfield) → `spec_sections` (brownfield) → `invariants_summary` (C# only). Surface-path pre-check via Glob.
> 3. **Phase 2** — Decompose into 2–6 Tasks (ordering: scaffolding → data model → runtime → integration+tests). Per Task: 5-column row with `_pending_` Issue + Status. Task intent cites concrete types/methods/paths. Sizing: 2–5 files = correct; ≤1 file = merge; >3 subsystems = split.
> 4. **Phase 3** — Cardinality gate: ≥2 Tasks/Stage (1 → warn + pause); ≤6 soft (7+ → warn + pause). Single-file/function tasks → warn + pause. Proceed only after user confirms.
> 5. **Phase 3.5** — Sizing-gate eval (H1–H6 per `ia/rules/stage-sizing-gate.md`).
> 6. **Phase 4** — Call `stage_body_write({slug, stage_id, body})` with full Task table + 4 pending subsections (§Stage File Plan · §Plan Fix · §Stage Audit · §Stage Closeout Plan); preserve Status `Draft`.
> 7. **Phase 5** — `npm run progress`. Log exit; non-zero does NOT block.
>
> ## Hard boundaries
>
> - Do NOT decompose Stages beyond target.
> - Do NOT create BACKLOG rows or task spec stubs — `stage-file` does that.
> - Do NOT overwrite a decomposed Stage without user confirmation.
> - Do NOT persist with <2 Tasks without confirmation.
> - Do NOT commit.
>
> ## Output
>
> Single caveman message: Stage {STAGE_ID} decomposed (N Tasks, all `_pending_`), cardinality + sizing gate outcomes, next step (`claude-personal "/stage-file {SLUG} Stage {STAGE_ID}"` when prior Stage closes).
