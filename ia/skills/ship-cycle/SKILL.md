---
name: ship-cycle
purpose: >-
  Stage-atomic full ship: one inference body emits ALL tasks of one Stage with
  boundary markers (Pass A) AND drives verify-loop + verified→done flips +
  inline closeout + single stage commit + cron_stage_verification_flip_enqueue (Pass B).
  Sole stage-driver in the new chain (design-explore → ship-plan → ship-cycle
  → ship-final). Falls back to /ship-stage-main-session legacy adapter only
  when batch exceeds token cap.
audience: agent
loaded_by: "skill:ship-cycle"
slices_via: none
description: >-
  Stage-atomic ship-cycle — full Pass A + Pass B. One inference emits all Tasks
  of one Stage with `<!-- TASK:{ISSUE_ID} START/END -->` boundary markers,
  flips each `pending → implemented`, then runs verify-loop on cumulative
  `git diff HEAD`, flips each `implemented → verified → done`, fires inline
  `stage_closeout_apply` + `cron_audit_log_enqueue({audit_kind:'stage_closed'})`
  audit row (cron-drained), lands a single stage commit
  `feat({slug}-stage-{stage_id_db})`, records per-Task commit sha via
  `cron_task_commit_record_enqueue`, and writes
  `cron_stage_verification_flip_enqueue(verdict='pass', commit_sha)`.
  Failure mode =
  `ia_stages.status='partial'` (mig 0069); resume re-enters at first non-done
  task (DB `task_state` query, no git scan). Token budget hard cap 80k input
  on Pass A inference; over cap = fallback `/ship-stage-main-session` legacy
  two-pass adapter (kept as a separate surface, not part of new chain).
  Validate gate = `validate:fast` (TECH-12640) on cumulative stage diff.
  Triggers: "/ship-cycle {SLUG} {STAGE_ID}", "ship cycle stage",
  "stage-atomic batch ship". Argument order (explicit): SLUG first,
  STAGE_ID second.
phases:
  - Parse args + load stage bundle
  - Token-budget preflight (hard cap 80k input → fallback ship-stage-main-session)
  - Resume gate (DB task_state — pending→Pass A, implemented→skip Pass A)
  - Pass A — bulk emit task-batch body with boundary markers
  - Pass A — per-task unity:compile-check gate + task_status_flip(implemented) + phase_checkpoint journal
  - Pass B — verify-loop on cumulative git diff HEAD (verdict==pass required)
  - Pass B — per-task verified→done flips
  - Pass B — inline closeout (stage_closeout_apply + cron_audit_log_enqueue)
  - Pass B — single stage commit + per-task cron_task_commit_record_enqueue + cron_stage_verification_flip_enqueue
  - Chain digest + next-stage resolver
triggers:
  - /ship-cycle {SLUG} {STAGE_ID}
  - ship cycle stage
  - stage-atomic batch ship
argument_hint: "{slug} Stage {X.Y} [--force-model {model}] [--no-resume]"
model: sonnet
reasoning_effort: low
input_token_budget: 80000
pre_split_threshold: 70000
tools_role: pair-head
tools_extra:
  - mcp__territory-ia__unity_bridge_command
  - mcp__territory-ia__stage_bundle
  - mcp__territory-ia__task_state
  - mcp__territory-ia__task_spec_body
  - mcp__territory-ia__task_status_flip
  - mcp__territory-ia__task_status_flip_batch
  - mcp__territory-ia__stage_closeout_apply
  - mcp__territory-ia__master_plan_state
  - mcp__territory-ia__master_plan_next_pending
  - mcp__territory-ia__unity_compile
  - mcp__territory-ia__cron_audit_log_enqueue
  - mcp__territory-ia__cron_journal_append_enqueue
  - mcp__territory-ia__cron_task_commit_record_enqueue
  - mcp__territory-ia__cron_stage_verification_flip_enqueue
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - Do NOT bypass token-budget preflight — over cap → fallback /ship-stage-main-session.
  - Pass A NEVER commits per Task — single stage commit at Pass B end covers all Pass A diffs.
  - Do NOT skip `unity:compile-check` per task on Assets/**/*.cs touched.
  - Do NOT cross stage boundary — strictly one Stage per invocation.
  - Pass A flips strictly `pending → implemented`; Pass B flips strictly `implemented → verified → done`.
  - Inline closeout (Pass B) is MANDATORY on green verify-loop — never defer to a separate closeout invocation.
  - Do NOT write task spec bodies to filesystem — DB sole source of truth.
  - Do NOT chain `/code-review` — operator runs out-of-band per Task (lifecycle row 9).
  - On Pass B verify-loop fail → `STAGE_VERIFY_FAIL` + worktree stays dirty + no rollback of Pass A flips.
  - Phase 8 step 0a (live-Editor compile poll) MANDATORY when step 0 ran — never fire-and-forget `refresh_asset_database` without polling `get_compilation_status` to terminal state before commit.
caller_agent: ship-cycle
---

# Ship-cycle skill — stage-atomic full ship (Pass A + Pass B)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role.** Stage-atomic full ship — owns BOTH Pass A (implement+compile+`task_status_flip(implemented)` for all Tasks of one Stage in a single inference with boundary markers) AND Pass B (verify-loop + verified→done flips + inline closeout + single stage commit + `cron_stage_verification_flip_enqueue`). Sole stage-driver in the new chain `design-explore → ship-plan → ship-cycle → ship-final`.

**Upstream:** `ship-plan` (populates §Plan Digest in DB). **Downstream:** `/ship-final {SLUG}` (when Stage was last filed Stage of plan) OR next `/ship-cycle {SLUG} Stage {N+1}` invocation.

**Legacy fallback:** `/ship-stage-main-session {SLUG} {STAGE_ID}` remains a separate surface (Cursor / Claude main-session no-subagent adapter). NOT chained from `/ship-cycle` — only invoked manually when token budget exceeded or operator wants the legacy two-pass shape.

---

## Inputs

| Param | Source | Notes |
|---|---|---|
| `SLUG` | first positional arg | Bare master-plan slug (e.g. `ship-protocol`). Verified via `master_plan_state(slug)`. |
| `STAGE_ID` | second positional arg | e.g. `Stage 3` → `3`. |
| `--force-model {model}` | optional flag | Override frontmatter `model`. Valid: `sonnet`, `opus`, `haiku`. |
| `--no-resume` | optional flag | Force Pass A execution even on `implemented` tasks (rare; debug only). |

---

## Phase sequence

### Phase 0 — Parse args + load stage bundle

`stage_bundle(slug, stage_id)` once. Capture `tasks[]` (filed, non-terminal). Idle exit when stage `done` + tasks all terminal.

### Phase 1 — Token-budget preflight

Sum input bytes: stage bundle + per-task §Plan Digest body (DB read via `task_spec_body`). Hard cap 80k input → over cap = `STOPPED — token_budget_exceeded`; emit `Next: /ship-stage-main-session {SLUG} {STAGE_ID}` (legacy two-pass adapter).

### Phase 2 — Resume gate (DB-only)

`task_state(task_id)` per task in stage. Bucket:

- All `pending` → run Pass A + Pass B (full chain).
- Mixed `pending` + `implemented` → run Pass A on remaining `pending`, then Pass B on full set.
- All `implemented` → skip Pass A entirely; run `PASS_B_ONLY` (worktree dirty required; clean → `STOPPED — pass_b_only_clean_worktree`).
- All terminal (`done`/`archived`) → idle exit, `Next:` next-stage resolver.

Disabled by `--no-resume`.

### Phase 3 — Pass A — bulk emit task-batch body

Single inference. Boundary markers per task: `<!-- TASK:{ISSUE_ID} START -->` ... `<!-- TASK:{ISSUE_ID} END -->`. Inside markers: full implementation diff body for that task. Greppable by validators / code-review subagents. Skip tasks already `implemented` (resume).

### Phase 4 — Pass A — per-task gate + flip + checkpoint

For each task in batch (skip if already `implemented`):

1. `unity:compile-check` if `Assets/**/*.cs` touched in this task's marker block.
2. `task_status_flip(task_id, implemented)`.
3. `cron_journal_append_enqueue` with `payload_kind=phase_checkpoint` (fire-and-forget; enqueue < 100 ms; cron supervisor drains to `ia_ship_stage_journal`):

```json
{
  "session_id": "{SESSION_ID}",
  "task_id": "{TASK_ID}",
  "slug": "{SLUG}",
  "stage_id": "{STAGE_ID}",
  "phase": "ship-cycle.4.per_task",
  "payload_kind": "phase_checkpoint",
  "payload": {
    "phase_id": "ship-cycle.4.{TASK_ID}",
    "decisions_resolved": ["{TASK_ID}:implemented", "{TASK_ID}:compile_check_pass"],
    "pending_decisions": [],
    "next_phase": "ship-cycle.4.{NEXT_TASK_ID or ship-cycle.5.verify_loop}",
    "ctx_drop_hint": ["task_spec_body:{TASK_ID}", "compile_log:{TASK_ID}"]
  }
}
```

Payload schema: `ia/rules/ship-stage-journal-schema.md §phase_checkpoint`.

Stop on first failure. Surviving tasks remain `implemented`; failed task → `STOPPED at {ISSUE_ID}` + `Next: /ship-cycle {SLUG} {STAGE_ID}` resume.

**NO per-task commits** — single stage commit at Phase 8 covers all Pass A + Pass B diffs.

### Phase 5 — Pass B — verify-loop on cumulative diff (runs ONCE per stage)

Full `verify-loop` Path A + Path B on cumulative `git diff HEAD` (worktree dirty from Pass A edits). Verdict shape per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).

- `verdict == pass` required → continue Phase 6.
- `verdict == fail` → `STAGE_VERIFY_FAIL` + chain digest + worktree stays dirty + no rollback of Pass A flips. Operator fixes manually then re-invokes `/ship-cycle {SLUG} {STAGE_ID}` (resume gate sees `implemented` → re-runs Phase 5).

No code-review in chain — operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).

### Phase 6 — Pass B — verified→done flips

Per task in `STAGE_TASK_IDS` (skip if already terminal):

1. `task_status_flip(task_id, "verified")`
2. `task_status_flip(task_id, "done")`

Enum walk requires both — DB CHECK refuses `implemented → done` direct.

### Phase 7 — Pass B — inline closeout (DB-only)

1. `stage_closeout_apply(slug, stage_id)` — atomic: shared migration ops deduped + N per-Task `archived_at` set + Stage / Plan Status rolled up per R3 / R5 + `materialize-backlog.sh` + `validate:all` run once at end.
2. `cron_audit_log_enqueue({slug, audit_kind:'stage_closed', body, stage_id, commit_sha})` — fire-and-forget audit row (enqueue < 100 ms; cron supervisor drains to `ia_master_plan_change_log` within 90 s).

No filesystem mv. Closeout MANDATORY on green Pass B — never defer.

### Phase 8 — Pass B — stage commit + per-task commit record + verification flip

0. **AssetDatabase refresh pre-commit gate** — when stage diff touched `Assets/**` (any file under Unity asset roots): call `unity_bridge_command(kind="refresh_asset_database")` BEFORE `git add -A`. Live Editor writes `.meta` GUID siblings synchronously for any new `.cs` / asset files Pass A created — without this gate, `unity:compile-check` (batchmode + second-instance when project lock held) skips AssetDatabase writes, leaving orphan untracked `.meta` files outside the stage commit. Skip this step when `git diff HEAD --name-only` shows zero `Assets/**` paths. On bridge failure (Editor not running / lease unavailable) → fall back to `unity:compile-check` (which now hits batchmode AssetDatabase since user can quit Editor) OR `STOPPED at refresh — bridge_unavailable` so operator can resume after starting Editor.

0a. **Live-Editor compile poll (post-refresh gate)** — immediately after step 0 `refresh_asset_database` succeeds, poll `unity_bridge_command(kind="get_compilation_status")` until `compilation_status ∈ {idle, compilation_succeeded}` OR ceiling reached. Refresh triggers live Editor recompile asynchronously; without this poll, ship-cycle proceeds to commit while live Editor compile may still fail — Pass A `unity:compile-check` (batchmode 2nd-instance under project lock) can pass with stale assembly cache while live Editor surfaces real errors only after refresh.

   Poll loop:
   - Initial wait 2 s (refresh → recompile latency); then poll every 2 s.
   - Ceiling: 60 s (30 polls). Configurable via `UNITY_COMPILE_POLL_CEILING_S`.
   - Terminal states:
     - `compilation_status == "idle"` OR `"compilation_succeeded"` → continue Phase 8 step 1 (`git add -A`).
     - `compilation_status == "compilation_failed"` → `STOPPED at refresh — live_editor_compile_failed` + emit `last_error_excerpt` + `recent_error_messages` from response. Worktree stays dirty. No commit. Operator fixes errors in Editor → re-runs `/ship-cycle {SLUG} {STAGE_ID}` (resume gate sees `implemented` → re-enters Phase 5 verify-loop).
     - Ceiling exhausted with `compilation_status == "compiling"` → `STOPPED at refresh — compile_poll_ceiling_exceeded` (rare; manual investigation).
   - Bridge unavailable for poll → same fallback as step 0 (`unity:compile-check` batchmode OR `STOPPED at refresh — bridge_unavailable`).

   This gate is MANDATORY when step 0 ran. Skipping it = same blind spot BUG-63 closed for `.meta` drift, but for compile state — fire-and-forget refresh leaks compile errors past the stage boundary into the user's next Editor session.

1. `git add -A` + single commit:

   ```
   feat({SLUG}-stage-{STAGE_ID_DB}): <one-line summary derived from task titles>

   Tasks: {TASK_ID_LIST}.

   Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
   ```

   Resume note: if `git diff HEAD` empty (PASS_B_ONLY re-run after prior commit), skip commit + reuse `git rev-parse HEAD` as `STAGE_COMMIT_SHA`.

2. Capture `STAGE_COMMIT_SHA = git rev-parse HEAD`.
3. Per task: `cron_task_commit_record_enqueue(task_id, commit_sha=STAGE_COMMIT_SHA, commit_kind="feat", ...)` — fire-and-forget; returns `{job_id, status:'queued'}` < 100 ms; cron drains to `ia_task_commits` within 90 s.
4. `cron_stage_verification_flip_enqueue(slug, stage_id, verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-cycle")` — fire-and-forget; history-preserving INSERT via cron drain.

Pre-commit hook fail → `STOPPED at commit — pre-commit hook failed: {reason}` (investigate; do NOT amend or `--no-verify`).

### Phase 9 — Chain digest + next-stage resolver

`master_plan_state(slug)` — capture `stages[]` for both progress counter + next-stage resolution:

- `TOTAL_STAGES = stages.length`
- `STAGE_INDEX = 1-based index of {STAGE_ID} in stages[]` (use `stage_id` match)
- `STAGE_PROGRESS = "{STAGE_INDEX}/{TOTAL_STAGES}"` — emitted in chain digest summary.

3 next-handoff cases:

- Filed Stage with `pending` Tasks remaining → `Next: /ship-cycle {SLUG} Stage {N.M}`.
- All Stages `done` → `Next: /ship-final {SLUG}`.
- Skeleton Stage encountered → `STOPPED — skeleton stage encountered` + `Next: /design-explore --resume {SLUG}`.

Emit chain digest JSON header `chain_stage_digest: true` + caveman summary + `next_handoff` block.

---

## Boundary marker contract

```
<!-- TASK:TECH-12345 START -->
... full implementation body for TECH-12345 ...
<!-- TASK:TECH-12345 END -->

<!-- TASK:TECH-12346 START -->
... full implementation body for TECH-12346 ...
<!-- TASK:TECH-12346 END -->
```

- Markers are HTML comments — invisible in rendered markdown, greppable by tools.
- Each task block is self-contained — no cross-task references.
- Order = `tasks[]` order from `stage_bundle`.
- Mismatched / missing END marker → `STOPPED at {ISSUE_ID} — boundary_marker_unbalanced`.

---

## Escalation shape

```json
{
  "escalation": true,
  "phase": <int>,
  "reason": "token_budget_exceeded | boundary_marker_unbalanced | compile_check_failed | task_status_flip_failed | stage_verify_fail | closeout_apply_failed | commit_failed | verification_flip_failed | live_editor_compile_failed | compile_poll_ceiling_exceeded | bridge_unavailable",
  "task_id": "<optional>",
  "stderr": "<optional>"
}
```

---

## Output

Emit exactly one of:

- `SHIP_CYCLE {STAGE_ID}: PASSED` — only after Phase 7 closeout + Phase 8 commit + verification flip succeed. Include `Next:` from Phase 9.
- `SHIP_CYCLE {STAGE_ID}: STOPPED — token_budget_exceeded` — `Next: /ship-stage-main-session {SLUG} {STAGE_ID}`.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at {ISSUE_ID} — {reason}` — Pass A failure; `Next: /ship-cycle {SLUG} {STAGE_ID}` resume.
- `SHIP_CYCLE {STAGE_ID}: STAGE_VERIFY_FAIL` — Pass B verify-loop failed; worktree stays dirty; manual fix then re-run.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — DB-drift repair directive.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at refresh — live_editor_compile_failed` — Phase 8 step 0a poll surfaced live Editor compile errors after `refresh_asset_database`. Worktree dirty; no commit. Operator fixes errors → resume `/ship-cycle {SLUG} {STAGE_ID}`.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at refresh — compile_poll_ceiling_exceeded` — Phase 8 step 0a poll exhausted ceiling (60 s default) with `compilation_status == "compiling"`. Manual investigation.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at refresh — bridge_unavailable` — bridge / Editor unavailable for refresh or poll. Operator starts Editor + re-runs.

Followed by caveman summary block: `ship-cycle done. STAGE_ID={S} STAGE_PROGRESS={STAGE_INDEX}/{TOTAL_STAGES} BATCH_SIZE={N} IMPLEMENTED={K} VERIFIED={V} DONE={D} STAGE_COMMIT={short_sha} VERIFY={pass|fail|skipped}` + per-task rows + `Next:` handoff.

`STAGE_PROGRESS` derived from `master_plan_state(slug).stages[]` — length = `TOTAL_STAGES`, 1-based index of `{STAGE_ID}` = `STAGE_INDEX`. Always emit (idle exit + STOPPED branches included) so operator sees plan position at every handoff.

---

## Guardrails

### DB read batching guardrail

Before issuing the first DB read, list every question needed for this phase. Batch into one `db_read_batch` MCP call OR one typed MCP slice (`catalog_panel_get`, `catalog_archetype_get`, `master_plan_state`, `task_bundle_batch`, `spec_section`). Sequential reads only when query N depends on result of N-1.

### Pass A MCP slice banner

When Pass A inference body needs DB context for multiple tables or queries, use the following typed MCP alternatives before falling back to ad-hoc `db_read_batch`:

- `master_plan_state` — plan + stage rollup
- `task_bundle_batch` — all task contexts for a stage in one call
- `spec_section` — single spec slice
- `catalog_panel_get` / `catalog_archetype_get` — catalog lookups

For ad-hoc multi-query DB state (anything not covered by the above): one `db_read_batch` call covers all questions. Do NOT issue sequential `psql` shell calls or N sequential MCP reads when a single batch covers it.

### Stage_id literal match guardrail

`stage_id` propagated to `stage_closeout_apply`, `cron_stage_verification_flip_enqueue`, `cron_task_commit_record_enqueue`, `cron_audit_log_enqueue`, and the stage commit subject MUST be the literal value stored in `ia_stages.stage_id` for the slug — NOT a free-typed approximation.

Resolve once at Phase 0 / Phase 1 entry via `master_plan_state(slug).stages[]`: pick the row matching the user-supplied `{STAGE_ID}` arg (lookup must be tolerant — accept `3`, `3.0`, `stage-3-foo` and resolve to canonical) then propagate the canonical literal verbatim through all downstream phases. Per-plan formats vary (`N.M` for `ui-implementation-mvp-rest`, bare-int + slug-style mix for `large-file-atomization-refactor`, slug-style for `large-file-atomization-cutover-refactor`). No global format invariant.

Why. Cron drainer enforces strict FK `ia_stage_verifications_stage_fk(slug, stage_id) → ia_stages(slug, stage_id)`. MCP enqueue tool inserts verbatim — no pre-check, no normalization. Mismatch surfaces only after async drain: row stuck `status=done` with `error` populated, no retry. Recurrence evidence: 2026-05-08 `cron_stage_verification_flip_jobs.job_id=6436292f`, slug `ui-implementation-mvp-rest`, agent passed `"3"` when canonical was `"3.0"`; manually replayed.

How to apply. (a) Phase 0 — capture `STAGE_ID_DB = master_plan_state(slug).stages[i].stage_id` (canonical literal) before any other DB write. (b) Phases 7–9 — pass `STAGE_ID_DB` verbatim to closeout / cron-flip / commit subject. (c) NEVER concatenate stage_id from chat or BACKLOG row text — resolve from DB.

---

## Changelog

- 2026-05-05 — Pass B absorbed (verify-loop + verified→done flips + closeout + stage commit + verification flip). Chain prose updated: `design-explore → ship-plan → ship-cycle → ship-final`. `/ship-stage-main-session` retained as legacy fallback for token-budget-exceeded path; not chained.
- 2026-05-08 — `STAGE_PROGRESS={STAGE_INDEX}/{TOTAL_STAGES}` added to Phase 9 chain digest summary. Derived from `master_plan_state(slug).stages[]` — operator sees plan position at every handoff (e.g. `12/19`).
- 2026-05-08 (BUG-63) — Phase 8 step 0 added: `unity_bridge_command(kind="refresh_asset_database")` runs before `git add -A` when stage diff touches `Assets/**`. Live Editor writes `.meta` siblings synchronously into stage commit; eliminates orphan `.meta` drift accumulated when batchmode `unity:compile-check` runs in second-instance mode (project lock held by user's Editor → AssetDatabase writes skipped). Recurrence evidence: large-file-atomization-refactor stages 2–15, 65 orphan `.meta` swept in chore commit `bd153cc3`.
- 2026-05-08 — Stage_id literal match guardrail added (Guardrails §). `stage_id` propagated to closeout / cron flip / cron commit-record / audit / stage commit MUST be canonical literal from `ia_stages.stage_id` (resolved via `master_plan_state(slug).stages[]`). Recurrence evidence: `cron_stage_verification_flip_jobs.job_id=6436292f` 2026-05-08, slug `ui-implementation-mvp-rest`, agent emitted `"3"` vs canonical `"3.0"`; FK `ia_stage_verifications_stage_fk` violation surfaced only at async cron drain (row stuck `done` with `error`); manually replayed. Per-plan format inconsistency observed (`N.M` vs bare-int vs `stage-N-...`) — no global invariant; agent must resolve per slug. Server-side FK pre-check tracked separately (TECH issue).
- 2026-05-08 — Phase 8 step 0a added: post-refresh live-Editor compile poll. `refresh_asset_database` (step 0) was fire-and-forget — refresh kicks off async live-Editor recompile, but ship-cycle proceeded to commit + closeout while compile may still fail. Pass A `unity:compile-check` (batchmode 2nd-instance under project lock) passed with stale assembly cache while live Editor surfaced real errors only after refresh. Step 0a polls `get_compilation_status` every 2 s (initial wait 2 s, ceiling 60 s, configurable via `UNITY_COMPILE_POLL_CEILING_S`) until terminal state. New failure modes: `live_editor_compile_failed`, `compile_poll_ceiling_exceeded`, `bridge_unavailable`. Recurrence evidence: large-file-atomization-cutover-refactor stage-6-bridge-mutations shipped clean per ship-cycle but live Editor blocked on compile errors at next session start.
