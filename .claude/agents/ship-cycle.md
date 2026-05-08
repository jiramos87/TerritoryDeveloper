---
name: ship-cycle
description: Stage-atomic ship-cycle — full Pass A + Pass B. One inference emits all Tasks of one Stage with `<!-- TASK:{ISSUE_ID} START/END -->` boundary markers, flips each `pending → implemented`, then runs verify-loop on cumulative `git diff HEAD`, flips each `implemented → verified → done`, fires inline `stage_closeout_apply` + `cron_audit_log_enqueue({audit_kind:'stage_closed'})` audit row (cron-drained), lands a single stage commit `feat({slug}-stage-{stage_id_db})`, records per-Task commit sha via `cron_task_commit_record_enqueue`, and writes `cron_stage_verification_flip_enqueue(verdict='pass', commit_sha)`. Failure mode = `ia_stages.status='partial'` (mig 0069); resume re-enters at first non-done task (DB `task_state` query, no git scan). Token budget hard cap 80k input on Pass A inference; over cap = fallback `/ship-stage-main-session` legacy two-pass adapter (kept as a separate surface, not part of new chain). Validate gate = `validate:fast` (TECH-12640) on cumulative stage diff. Triggers: "/ship-cycle {SLUG} {STAGE_ID}", "ship cycle stage", "stage-atomic batch ship". Argument order (explicit): SLUG first, STAGE_ID second.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__unity_bridge_command, mcp__territory-ia__stage_bundle, mcp__territory-ia__task_state, mcp__territory-ia__task_spec_body, mcp__territory-ia__task_status_flip, mcp__territory-ia__task_status_flip_batch, mcp__territory-ia__stage_closeout_apply, mcp__territory-ia__master_plan_state, mcp__territory-ia__master_plan_next_pending, mcp__territory-ia__unity_compile, mcp__territory-ia__cron_audit_log_enqueue, mcp__territory-ia__cron_journal_append_enqueue, mcp__territory-ia__cron_task_commit_record_enqueue, mcp__territory-ia__cron_stage_verification_flip_enqueue
model: sonnet
reasoning_effort: low
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

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
9. Phase 8 — Pass B — when stage diff touches `Assets/**`: `unity_bridge_command(kind="refresh_asset_database")` BEFORE `git add -A` so live Editor writes `.meta` siblings synchronously into the stage commit (skip when no `Assets/**` paths). Then single stage commit `feat({slug}-stage-{stage_id_db}): ...` + per-task `cron_task_commit_record_enqueue(commit_sha)` + `cron_stage_verification_flip_enqueue(verdict='pass', commit_sha)`.
10. Phase 9 — Chain digest + next-stage resolver via `master_plan_state(slug)`: capture `STAGE_PROGRESS={STAGE_INDEX}/{TOTAL_STAGES}` from `stages[]` (length = total, 1-based index of `{STAGE_ID}` = position); filed Stage → `/ship-cycle Stage N.M`; all done → `/ship-final {SLUG}`.

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
- Phase 8 step 0: refresh_asset_database bridge call BEFORE git add -A whenever stage diff touches Assets/** — never skip when Assets/** present (orphan .meta drift recurrence).
- On Pass B verify-loop fail → `STAGE_VERIFY_FAIL` + worktree stays dirty + no rollback of Pass A flips.
- Do NOT chain `/code-review` — operator runs out-of-band per Task (lifecycle row 9).
- Do NOT write task spec bodies to filesystem — DB sole source of truth.

# Escalation shape

```json
{"escalation": true, "phase": <int>, "reason": "token_budget_exceeded | boundary_marker_unbalanced | compile_check_failed | task_status_flip_failed | stage_verify_fail | closeout_apply_failed | commit_failed | verification_flip_failed", "task_id": "<opt>", "stderr": "<opt>"}
```

# Output

Caveman summary: `ship-cycle done. STAGE_ID={S} STAGE_PROGRESS={STAGE_INDEX}/{TOTAL_STAGES} BATCH_SIZE={N} IMPLEMENTED={K} VERIFIED={V} DONE={D} STAGE_COMMIT={short_sha} VERIFY={pass|fail|skipped}` + per-task rows + `Next:` handoff (`/ship-cycle Stage N.M` next | `/ship-final {SLUG}` | fallback `/ship-stage-main-session`). Always emit `STAGE_PROGRESS` (every branch) so operator sees plan position.
