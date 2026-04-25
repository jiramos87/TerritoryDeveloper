## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{SLUG}` (bare master-plan slug, e.g. `blip`). Second token = `{STAGE_ID}` (e.g. `Stage 7.2` → `7.2`). Missing either → print usage + abort. If `--task {ISSUE_ID}` present: extract `{ISSUE_ID}` for single-spec re-author (bulk pass of N=1). If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

Verify slug exists via `master_plan_state(slug=SLUG)`. Missing → STOPPED + `Next: claude-personal "/master-plan-new ..."` handoff.

## Subagent dispatch

Forward via Agent tool with `subagent_type: "stage-authoring"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-authoring/SKILL.md` end-to-end on Stage `{STAGE_ID}` of slug `{SLUG}` (single-spec re-author when `--task {ISSUE_ID}` present). 9 phases: Sequential-dispatch guardrail → Load shared Stage MCP bundle (`lifecycle_stage_context`) → Read filed Task spec stubs (DB via `task_spec_body`) → Token-split guardrail → Bulk author §Plan Digest direct (single Opus pass; §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps with Edits + Gate + STOP + MCP hints + invariant_touchpoints + validator_gate + optional Scene Wiring step + canonical-term fold) → Self-lint via `plan_digest_lint` (cap=1 retry per Task) → Mechanicalization preflight via `mechanicalization_preflight_lint` → Per-task `task_spec_section_write` to DB (DB sole persistence — no filesystem mirror) → Hand-off.
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
  {ISSUE_ID_1}: §Plan Digest written ({n_steps} mechanical steps, {n_acceptance} acceptance criteria, {n_tests} test rows); fold: {n_term_replacements}/{n_retired_refs_replaced}/{n_section_drift_fixed}; lint=PASS; preflight=PASS.
  {ISSUE_ID_2}: ...
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged.
next=stage-authoring-chain-continue
```

Then dispatcher emits next-step handoff:

- **N≥2:** `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"` — runs implement + verify + code-review + inline closeout.
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` — single-task path.
