# Mission

Run `ia/skills/ship-cycle/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{SLUG}`. Stage-atomic full ship — Pass A (bulk emit + per-task compile + `task_status_flip(implemented)`) AND Pass B (verify-loop + verified→done flips + inline closeout + single stage commit + `cron_stage_verification_flip_enqueue`). Sole stage-driver in chain `design-explore → ship-plan → ship-cycle → ship-final`. Token budget hard cap 80k input on Pass A inference — over cap = fallback `/ship-stage-main-session` legacy two-pass adapter.

# Phase sequence (matches SKILL frontmatter `phases:`)

1. Phase 0 — Parse `{SLUG} {STAGE_ID}`; `stage_bundle(slug, stage_id)`; idle exit if stage done.
2. Phase 1 — Token-budget preflight: sum bundle + per-task §Plan Digest bytes; over 80k → STOPPED + fallback handoff to `/ship-stage-main-session`.
3. Phase 2 — Resume gate (DB `task_state` per task): pending → Pass A required; all implemented → PASS_B_ONLY; all terminal → idle exit.
4. Phase 3 — Pass A — single inference body emits all tasks with boundary markers `<!-- TASK:{ISSUE_ID} START/END -->`.
5. Phase 4 — Pass A — per task: `unity:compile-check` (when Assets/**/*.cs touched) → `task_status_flip(implemented)` → `cron_journal_append_enqueue(payload_kind=phase_checkpoint)`. NO per-task commits.
6. Phase 5 — Pass B — verify-loop on cumulative `git diff HEAD`. Verdict pass required; fail → `STAGE_VERIFY_FAIL` (no rollback).
7. Phase 6 — Pass B — per task: `task_status_flip(verified)` then `task_status_flip(done)`.
8. Phase 7 — Pass B — inline closeout: `stage_closeout_apply(slug, stage_id)` + `cron_audit_log_enqueue({audit_kind:'stage_closed'})`.
9. Phase 8 — Pass B — single stage commit `feat({slug}-stage-{stage_id_db}): ...` + per-task `cron_task_commit_record_enqueue(commit_sha)` + `cron_stage_verification_flip_enqueue(verdict='pass', commit_sha)`.
10. Phase 9 — Chain digest + next-stage resolver via `master_plan_state(slug)`: filed Stage → `/ship-cycle Stage N.M`; all done → `/ship-final {SLUG}`.

# Boundary marker contract

```
<!-- TASK:TECH-XXXXX START -->
... implementation body (code edits, file creations) ...
<!-- TASK:TECH-XXXXX END -->
```

HTML comments — invisible in rendered markdown, greppable by code-review / validators. Order = `tasks[]` order. Unbalanced markers → `STOPPED at {ISSUE_ID} — boundary_marker_unbalanced`.

# Hard boundaries

- Do NOT bypass token-budget preflight — over cap → fallback `/ship-stage-main-session`.
- Pass A NEVER commits per Task — single stage commit at Phase 8 covers all Pass A + Pass B diffs.
- Do NOT skip `unity:compile-check` per task on Assets/**/*.cs touched.
- Do NOT cross stage boundary — strictly one Stage per invocation.
- Pass A flips strictly `pending → implemented`; Pass B flips strictly `implemented → verified → done`.
- Inline closeout (Phase 7) MANDATORY on green Pass B — never defer.
- On Pass B verify-loop fail → `STAGE_VERIFY_FAIL` + worktree stays dirty + no rollback of Pass A flips.
- Do NOT chain `/code-review` — operator runs out-of-band per Task (lifecycle row 9).
- Do NOT write task spec bodies to filesystem — DB sole source of truth.

# Escalation shape

```json
{"escalation": true, "phase": <int>, "reason": "token_budget_exceeded | boundary_marker_unbalanced | compile_check_failed | task_status_flip_failed | stage_verify_fail | closeout_apply_failed | commit_failed | verification_flip_failed", "task_id": "<opt>", "stderr": "<opt>"}
```

# Output

Caveman summary: `ship-cycle done. STAGE_ID={S} BATCH_SIZE={N} IMPLEMENTED={K} VERIFIED={V} DONE={D} STAGE_COMMIT={short_sha} VERIFY={pass|fail|skipped}` + per-task rows + `Next:` handoff (`/ship-cycle Stage N.M` next | `/ship-final {SLUG}` | fallback `/ship-stage-main-session`).
