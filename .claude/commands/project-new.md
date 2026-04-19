---
description: Create one BACKLOG issue + bootstrap `ia/projects/{ISSUE_ID}.md` stub from user prompt. Dispatches `project-new-planner` (Opus pair-head seam #3) → `project-new-applier` (Sonnet pair-tail) → chains `/author --task {ISSUE_ID}` at N=1. Args-only pair (no tuple list). NOT for bulk stage filing (= `/stage-file`).
argument-hint: "{free-text intent} [--type BUG|FEAT|TECH|ART|AUDIO] [--priority P1|P2|P3|P4]"
---

# /project-new — dispatch seam #3 pair then chain `/author --task`

Use `project-new-planner` subagent (`.claude/agents/project-new-planner.md`) → `project-new-applier` subagent (`.claude/agents/project-new-applier.md`) to create one BACKLOG row + project spec stub from `$ARGUMENTS`, then chain `/author --task {ISSUE_ID}` (N=1) to fill `§Plan Author` + canonical-term fold.

`$ARGUMENTS` carries free-text intent (title + product prompt). Optional `--type {prefix}` overrides prefix inference (`BUG` / `FEAT` / `TECH` / `ART` / `AUDIO`); planner asks when ambiguous. Optional `--priority {P1|P2|P3|P4}` overrides inference.

## Step 1 — Dispatch `project-new-planner` (Opus pair-head)

Forward via Agent tool with `subagent_type: "project-new-planner"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/project-new/SKILL.md` research + arg-resolution phases against the user prompt:
>
> ```
> $ARGUMENTS
> ```
>
> Phase 1 Context load: `glossary_discover` / `glossary_lookup` (English tokens) + `router_for_task` (1–3 domains) + `invariants_summary` (if runtime C# touched) + `spec_section` (only sections prompt implies).
> Phase 2 Backlog dep check: `backlog_issue` for every Depends-on / Related id (fabricated ids silently break `validate:dead-project-specs`).
> Phase 3 Spec outline: `list_specs` / `spec_outline` only if `spec:` key unknown.
> Phase 4 Resolve args: extract `TITLE`, `ISSUE_TYPE`, `PRIORITY`, optional `NOTES` + compose stub-body hints.
> Phase 5 Hand-off: emit resolved args payload for pair-tail (args-only seam #3 — no tuple list).
>
> ## Hard boundaries
>
> - Do NOT reserve id — applier reserves via `reserve-id.sh`.
> - Do NOT write yaml / spec stubs — applier writes.
> - Do NOT run `materialize-backlog.sh` / validators — applier runs gate.
> - Do NOT bulk-file multiple issues — that is `stage-file-planner`.
> - Do NOT enrich spec body beyond stub seeds — `plan-author` writes spec body at N=1 post-apply.
> - Do NOT fabricate Depends-on / Related ids — `backlog_issue` must verify.
> - Do NOT commit — user decides.

Planner must return resolved args before Step 2. Escalation → abort chain.

## Step 2 — Dispatch `project-new-applier` (Sonnet pair-tail)

Forward via Agent tool with `subagent_type: "project-new-applier"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/project-new-apply/SKILL.md` end-to-end. Reads planner-resolved args verbatim (`TITLE`, `ISSUE_TYPE`, `PRIORITY`, optional `NOTES`, `depends_on`, `related`). Phase 1 normalize prefix + validate enum. Phase 2 reserve id via `bash tools/scripts/reserve-id.sh {PREFIX}`. Phase 3 compose yaml body, `backlog_record_validate`, write `ia/backlog/{ISSUE_ID}.yaml`. Phase 4 bootstrap `ia/projects/{ISSUE_ID}.md` stub from `ia/templates/project-spec-template.md`. Phase 5 `bash tools/scripts/materialize-backlog.sh` + `npm run validate:dead-project-specs` once. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT author §1/§2/§4/§5/§7 beyond skeleton — `plan-author` writes spec body at N=1.
> - Do NOT run `validate:all` — only `validate:dead-project-specs` in Phase 5.
> - Do NOT edit `BACKLOG.md` directly — `materialize-backlog.sh` regenerates it.
> - Do NOT chain to `plan-author` — command dispatcher (Step 3 below) does that.
> - Do NOT reuse retired ids.
> - Do NOT commit — user decides.

## Step 3 — Auto-chain `/author --task {ISSUE_ID}` (N=1 bulk)

On applier success: auto-invoke `/author --task {ISSUE_ID}` (Stage-scoped bulk `plan-author` at N=1 per T7.11 / TECH-478) to fill `§Plan Author` + canonical-term fold on the one filed spec. Rev 3 single-task path skips `plan-review` at N=1 — next step is `/implement {ISSUE_ID}` directly.

## Output

Chain summary: ISSUE_ID + priority + validators exit + bulk `/author` summary. Next step: `claude-personal "/ship {ISSUE_ID}"` (implement → verify-loop → code-review → audit → closeout at N=1).
