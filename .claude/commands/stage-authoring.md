---
description: Bulk-author §Plan Digest direct (no §Plan Author intermediate) across ALL N filed Task spec stubs of one Stage in a single Opus pass + persist per-Task body to DB via task_spec_section_write MCP. Dispatches the `stage-authoring` subagent (single-skill, DB-backed). Replaces retired `/author` + `/plan-digest` two-step chain — collapsed 2026-04-24 in Step 7 of `docs/ia-dev-db-refactor-implementation.md` per design B2 / B6 / C7 / C8 / D8. Standalone re-author path; auto-invoked inline by `/stage-file` chain Step 2 (post-Step-7).
argument-hint: "{master-plan-path} Stage {X.Y} [--task {ISSUE_ID}] [--force-model {model}]"
---

# /stage-authoring — dispatch `stage-authoring` subagent

Use `stage-authoring` (DB-backed single-skill; replaces retired `plan-author` + `plan-digest` pair) to bulk-author §Plan Digest direct + persist per-Task body to DB via `task_spec_section_write` MCP for `$ARGUMENTS`. Standalone re-author entry point; `/stage-file` chain calls this skill inline post-Step-7 per design C8.

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{ORCHESTRATOR_SPEC}` (repo-relative, `ia/projects/*-master-plan.md`). Second token = `{STAGE_ID}` (e.g. `Stage 7.2` → `7.2`). Missing either → print usage + abort. If `--task {ISSUE_ID}` present: extract `{ISSUE_ID}` for single-spec re-author (bulk pass of N=1). If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

## Subagent dispatch

Forward via Agent tool with `subagent_type: "stage-authoring"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-authoring/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{ORCHESTRATOR_SPEC}` (single-spec re-author when `--task {ISSUE_ID}` present). 9 phases: Sequential-dispatch guardrail → Load shared Stage MCP bundle (`lifecycle_stage_context`) → Read filed Task spec stubs (DB-first via `task_spec_body`, fs fallback) → Token-split guardrail → Bulk author §Plan Digest direct (single Opus pass; §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps with Edits + Gate + STOP + MCP hints + invariant_touchpoints + validator_gate + optional Scene Wiring step + canonical-term fold 4.5a–4.5d) → Self-lint via `plan_digest_lint` (cap=1 retry per Task) → Mechanicalization preflight via `mechanicalization_preflight_lint` (TECH-776 advisory hatch) → Per-task `task_spec_section_write` to DB + transitional filesystem mirror to `ia/projects/{ISSUE_ID}.md` → Hand-off.
>
> ## Hard boundaries
>
> - Do NOT write `## §Plan Author` section — surface retired per design B6.
> - Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` — retired per design D8.
> - Do NOT write code, run verify, or flip Task status.
> - Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ bulk sub-passes.
> - Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff.
> - Do NOT call `lifecycle_stage_context` per Task — once per Stage.
> - Do NOT skip the Scene Wiring step when triggered — per `ia/rules/unity-scene-wiring.md`.
> - Do NOT fall back to filesystem-only write on `db_unavailable` — escalate; DB is source of truth post-Step-6.
> - Do NOT edit `ia/specs/glossary.md` — propose candidates in §Open Questions only.
> - Do NOT commit — user decides.

`stage-authoring` must return success + N specs with §Plan Digest written to DB + transitional filesystem mirrors updated + lint PASS + preflight PASS + `validate:all` exit 0 before chain success. Escalation → abort with handoff `/stage-authoring {ORCHESTRATOR_SPEC} {STAGE_ID}` for re-run after manual fix.

### Branch guardrail (feature/ia-dev-db-refactor)

Per `docs/ia-dev-db-refactor-implementation.md §3`: "No §Plan Digest ceremony. Do not invoke /author, /plan-digest, /plan-review on this branch." Smoke testing of `/stage-authoring` on TECH-858 (Step 6 sentinel filed task) is the Step 7 acceptance gate; broader chain dispatches resume on `main` post-merge.

## Output

Single caveman block from subagent. Shape:

```
stage-authoring done. STAGE_ID={STAGE_ID} AUTHORED={N} SKIPPED={K} (split: {sub_pass_count} sub-pass(es))
Per-Task:
  {ISSUE_ID_1}: §Plan Digest written ({n_steps} mechanical steps, {n_acceptance} acceptance criteria, {n_tests} test rows); fold: {n_term_replacements}/{n_retired_refs_replaced}/{n_section_drift_fixed}; lint=PASS; preflight=PASS.
  {ISSUE_ID_2}: ...
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged.
Filesystem mirrors: {N} updated.
next=stage-authoring-chain-continue
```

Then dispatcher emits next-step handoff:

- **N≥2:** `Next: claude-personal "/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"` — runs implement + verify + code-review + closeout.
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` — single-task path.

Post-Step-9 (filesystem specs deleted): drop the filesystem-mirror line from output; DB write is the only persistence.
