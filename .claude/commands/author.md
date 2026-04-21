---
description: Bulk-author §Plan Author section (4 sub-sections) across all N Task specs of one Stage in a single Opus pass. Dispatches the `plan-author` subagent. Stage-scoped bulk non-pair — absorbs retired spec-enrich canonical-term fold.
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]   |   --task {ISSUE_ID} [--force-model {model}]"
---

# /author — dispatch `plan-author` subagent

Use `plan-author` subagent (`.claude/agents/plan-author.md`) to bulk-author `§Plan Author` sections for `$ARGUMENTS`.

## Argument parsing

Split `$ARGUMENTS`. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "plan-author"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-author/SKILL.md` end-to-end for `$ARGUMENTS`. Default invocation: Stage-scoped bulk pass across ALL N Task specs of target Stage (1st arg = `MASTER_PLAN_PATH`; 2nd arg = `STAGE_ID`). Escape hatch: `--task {ISSUE_ID}` = single-spec re-author, bulk pass of N=1.
>
> Auto-invoked inside `/stage-file` chain tail (F6 re-fold 2026-04-20 — bulk Stage 1×N after stage-file-applier, before plan-reviewer). Manual invocation: `--task {ISSUE_ID}` re-author (single-issue path after `/project-new`), re-run after `plan-review` finds drift, or standalone recovery.
>
> ## Phase loop
>
> 1. Phase 1 — Load Stage context: master-plan Stage block + N spec stubs (§1, §2, §4, §5, §7, §8) + `domain-context-load` bundle + pair-contract + glossary table.
> 2. Phase 2 — Token-split guardrail: sum input tokens vs Opus threshold; single bulk pass OR ⌈N/2⌉ sub-passes. NEVER per-Task.
> 3. Phase 3 — Bulk author §Plan Author: single Opus call → map `{ISSUE_ID → {audit_notes, examples, test_blueprint, acceptance}}` → edit each spec in-place (idempotent replace). Section placement: between §10 Lessons Learned and §Open Questions.
> 4. Phase 4 — Canonical-term fold: enforce glossary terms across §1 / §4.1 / §5.1 / §7. Ad-hoc synonyms → canonical. Missing terms → §Open Questions candidates (do NOT edit glossary).
> 5. Phase 5 — Validate + hand-off: `npm run validate:dead-project-specs`; emit per-Task summary; propose next-stage handoff.
>
> ## Hard boundaries
>
> - Do NOT write code — §Plan Author + canonical-term fold only.
> - Do NOT run `/verify-loop` or `/implement`.
> - Do NOT flip Task Status — downstream owns.
> - Do NOT edit `ia/specs/glossary.md` — propose candidates in §Open Questions.
> - Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ sub-passes.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single caveman message: Stage {STAGE_ID} — N specs authored ({split_count} bulk pass(es)). Per-Task §Plan Author sub-section counts + canonical-term replacement counts. Next: `claude-personal "/plan-review {MASTER_PLAN_PATH} Stage {STAGE_ID}"` (multi-task) OR `claude-personal "/implement {ISSUE_ID}"` (N=1).
