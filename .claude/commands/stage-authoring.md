---
description: DB-backed single-skill stage-authoring. One Opus bulk pass authors §Plan Digest direct per filed Task spec stub of one Stage (RELAXED shape: §Goal / §Acceptance / §Pending Decisions / §Implementer Latitude / §Work Items / §Test Blueprint / §Invariants & Gate — intent over verbatim code). Stub → digest direct, no intermediate surface. Persists each per-Task §Plan Digest body to DB via `task_spec_section_write` MCP. Glossary alignment + retired-surface scan narrowed to §Plan Digest body only. Self-lints via `plan_digest_lint` (cap=1 retry). Mechanicalization preflight via `mechanicalization_preflight_lint`. No aggregate doc compile. Triggers: "/stage-authoring {SLUG} {STAGE_ID}", "stage authoring", "stage-scoped digest", "author stage tasks". Argument order (explicit): SLUG first, STAGE_ID second.
argument-hint: "{slug} Stage {X.Y} [--task {ISSUE_ID}] [--force-model {model}]"
---

# /stage-authoring — DB-backed single-skill stage-authoring: one Opus bulk pass writes §Plan Digest direct per task via task_spec_section_write MCP. No aggregate doc.

Drive `$ARGUMENTS` via the [`stage-authoring`](../agents/stage-authoring.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per agent-output-caveman-authoring). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /stage-authoring {SLUG} {STAGE_ID}
- stage authoring
- stage-scoped digest
- author stage tasks
<!-- skill-tools:body-override -->

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{SLUG}` (bare master-plan slug, e.g. `blip`). Second token = `{STAGE_ID}` (e.g. `Stage 7.2` → `7.2`). Missing either → print usage + abort. If `--task {ISSUE_ID}` present: extract `{ISSUE_ID}` for single-spec re-author (bulk pass of N=1). If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

Verify slug exists via `master_plan_state(slug=SLUG)`. Missing → STOPPED + `Next: claude-personal "/master-plan-new ..."` handoff.

## Subagent dispatch

Forward via Agent tool with `subagent_type: "stage-authoring"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-authoring/SKILL.md` end-to-end on Stage `{STAGE_ID}` of slug `{SLUG}` (single-spec re-author when `--task {ISSUE_ID}` present). 9 phases: Sequential-dispatch guardrail → Load shared Stage MCP bundle (`lifecycle_stage_context`) → Read filed Task spec stubs (DB via `task_spec_body`) → Token-split guardrail → Bulk author §Plan Digest direct (RELAXED shape, single Opus pass; §Goal / §Acceptance / §Pending Decisions / §Implementer Latitude / §Work Items / §Test Blueprint / §Invariants & Gate — flat work-item rows with 1-line intent, ONE invariants & gate block, optional Scene Wiring row when triggered, canonical-term fold on digest body only) → Self-lint via `plan_digest_lint` (cap=1 retry per Task) → Mechanicalization preflight via `mechanicalization_preflight_lint` → Per-task `task_spec_section_write` to DB (DB sole persistence — no filesystem mirror) → Hand-off.
>
> ## Hard boundaries
>
> - Do NOT write `## §Plan Author` section.
> - Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md`.
> - Do NOT write code, run verify, or flip Task status.
> - Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ bulk sub-passes.
> - Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff.
> - Do NOT call `lifecycle_stage_context` per Task — once per Stage.
> - Do NOT skip the Scene Wiring step when triggered — per `ia/rules/unity-scene-wiring.md`.
> - Do NOT write task spec bodies to filesystem — DB only via `task_spec_section_write`.
> - Do NOT fall back to filesystem-only write on `db_unavailable` — escalate; DB is source of truth.
> - Do NOT edit `ia/specs/glossary.md` — propose candidates in §Open Questions only.
> - Do NOT commit — user decides.

`stage-authoring` must return success + N specs with §Plan Digest written to DB + lint PASS + preflight PASS + `validate:all` exit 0 before chain success. Escalation → abort with handoff `/stage-authoring {SLUG} {STAGE_ID}` for re-run after manual fix.

## Output

Single caveman block from subagent. Shape:

```
stage-authoring done. STAGE_ID={STAGE_ID} AUTHORED={N} SKIPPED={K} (split: {sub_pass_count} sub-pass(es))
Per-Task:
  {ISSUE_ID_1}: §Plan Digest written ({n_work_items} work items, {n_decisions} pending decisions, {n_latitude} latitude rows, {n_acceptance} acceptance, {n_tests} test rows); fold: {n_term_replacements}/{n_retired_refs_replaced}; lint=PASS; preflight=PASS.
  {ISSUE_ID_2}: ...
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged.
next=stage-authoring-chain-continue
```

Then dispatcher emits next-step handoff:

- **N≥2:** `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"` — runs implement + verify + code-review + inline closeout.
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` — single-task path.
