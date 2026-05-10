# Mission

Run `ia/skills/ship-cycle/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{SLUG}`. Stage-atomic full ship — Pass A (Sonnet inference: bulk emit + ONE compile-check + `task_status_flip_batch(implemented)`) + Pass B (verify-loop + recipe `ship-cycle-pass-b` mechanical tail). Sole stage-driver in chain `design-explore → ship-plan → ship-cycle → ship-final`. Token budget hard cap 80k input on Pass A inference — over cap = fallback `/ship-stage-main-session` legacy two-pass adapter.

# Phase sequence (matches SKILL frontmatter `phases:`)

1. Phase 0 — `tools/scripts/recipe-engine/ship-cycle/preflight.sh --slug {SLUG} --stage-id {STAGE_ID}` — parse + canonical `STAGE_ID_DB` resolve + token-budget preflight (over 80k → fallback `/ship-stage-main-session`) + resume bucket via DB `task_state`. Emits JSON consumed by Pass A.
2. Phase 1 (Pass A) — single Sonnet inference emits all tasks with boundary markers `<!-- TASK:{ISSUE_ID} START/END -->`. After return: aggregate ALL `Assets/**/*.cs` paths across markers → ONE `unity:compile-check` (single batchmode invocation; replaces per-task loop) → `task_status_flip_batch(slug, stage_id, from='pending', to='implemented')` (single MCP call). NO per-task commits. `cron_journal_append_enqueue(payload_kind=phase_checkpoint)`.
3. Phase 2 (Pass B step 1) — verify-loop on cumulative `git diff HEAD`. Write verdict file `/tmp/ship-cycle-verify-{slug}-{stage_id}.json`. Fail → `STAGE_VERIFY_FAIL` (no rollback).
4. Phase 3 (Pass B steps 2–12) — `npm run recipe:run -- ship-cycle-pass-b --input slug={SLUG} stage_id={STAGE_ID_DB}`. Recipe owns: verify-loop check + 2× `task_status_flip_batch` (verified, done) + `stage_closeout_apply` (DB-only) + `materialize-backlog` + conditional `unity_refresh_asset_database` (fire-and-forget; verdict drains async via `cron_unity_compile_verify_jobs`) + per-file `git add` + stage commit `feat({slug}-stage-{stage_id_db}): ship-cycle Pass B verify + closeout` + 4 cron enqueues (`audit_log[stage_closed]`, `task_commit_record` foreach, `stage_verification_flip[verdict=pass]`, NEW `validate_post_close`, NEW `unity_compile_verify`).
5. Phase 4 — Chain digest + next-stage resolver via `master_plan_state(slug)`: capture `STAGE_PROGRESS={STAGE_INDEX}/{TOTAL_STAGES}` from `stages[]` (length = total, 1-based index of `{STAGE_ID}` = position); filed Stage → `/ship-cycle Stage N.M`; all done → `/ship-final {SLUG}`.

# Boundary marker contract

```
<!-- TASK:TECH-XXXXX START -->
... implementation body (code edits, file creations) ...
<!-- TASK:TECH-XXXXX END -->
```

HTML comments — invisible in rendered markdown, greppable by code-review / validators. Order = `tasks[]` order. Unbalanced markers → `STOPPED at {ISSUE_ID} — boundary_marker_unbalanced`.

# Hard boundaries

- Do NOT bypass token-budget preflight — over cap → fallback `/ship-stage-main-session`.
- Pass A NEVER commits per Task — single stage commit at Pass B recipe step 7 covers all Pass A + Pass B diffs.
- Do NOT skip aggregated `unity:compile-check` when Pass A touched `Assets/**/*.cs`.
- Do NOT cross stage boundary — strictly one Stage per invocation.
- Pass A flips strictly `pending → implemented` (single batch); Pass B flips strictly `implemented → verified → done` (two batches).
- Inline closeout (Pass B recipe step 4) MANDATORY on green verify-loop — never defer.
- On Pass B verify-loop fail → `STAGE_VERIFY_FAIL` + worktree stays dirty + no rollback of Pass A flips.
- Pass B recipe step 6 (`unity_refresh_asset_database`) fires-and-forgets — verdict drains async; sync `get_compilation_status` poll REMOVED.
- Do NOT chain `/code-review` — operator runs out-of-band per Task (lifecycle row 9).
- Do NOT write task spec bodies to filesystem — DB sole source of truth.

# Escalation shape

```json
{"escalation": true, "phase": <int>, "reason": "token_budget_exceeded | boundary_marker_unbalanced | compile_check_failed | task_status_flip_failed | stage_verify_fail | closeout_apply_failed | commit_failed | verification_flip_failed", "task_id": "<opt>", "stderr": "<opt>"}
```

# Output

Caveman summary: `ship-cycle done. STAGE_ID={S} STAGE_PROGRESS={STAGE_INDEX}/{TOTAL_STAGES} BATCH_SIZE={N} IMPLEMENTED={K} VERIFIED={V} DONE={D} STAGE_COMMIT={short_sha} VERIFY={pass|fail|skipped}` + per-task rows + `Next:` handoff (`/ship-cycle Stage N.M` next | `/ship-final {SLUG}` | fallback `/ship-stage-main-session`). Always emit `STAGE_PROGRESS` (every branch) so operator sees plan position.
