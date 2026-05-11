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
  - Phase 0 — recipe ship-cycle-preflight (parse + token-budget + resume gate via DB)
  - Pass A — bulk emit task-batch body with boundary markers (Sonnet inference)
  - Pass A — aggregate Assets/**/*.cs across tasks → ONE unity:compile-check + task_status_flip_batch(implemented)
  - Pass B step 1 — verify-loop on cumulative git diff HEAD (verdict==pass required, verdict file written)
  - Pass B steps 2–12 — recipe ship-cycle-pass-b (verified→done batch flips + closeout + materialize-backlog + asset-db refresh + stage commit + 4 cron enqueues)
  - Phase 9 — chain digest + next-stage resolver
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
  - Pass B recipe step B.7 (`unity_refresh_asset_database`) enqueues bridge refresh. Step B.7b (`wait_asset_recompile`) SYNCHRONOUSLY blocks until Editor finishes recompile AND `compilation_failed=false`. Hard fail before stage commit (B.8). Async cron `cron_unity_compile_verify_jobs` is belt-and-braces only — sync gate is authoritative.
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

### Phase 0 — Preflight (recipe / bash)

Single bash helper resolves args + token budget + resume bucket:

```bash
tools/scripts/recipe-engine/ship-cycle/preflight.sh --slug {SLUG} --stage-id {STAGE_ID}
```

Steps inside the helper (DB-only, no agent inference):

1. Parse args; resolve canonical `STAGE_ID_DB` from `master_plan_state(slug).stages[]` (mig 0132 enforces `N.M` form at insert; no per-plan format hunting).
2. `stage_bundle(slug, stage_id)` — capture filed non-terminal `tasks[]`. Idle exit when stage `done` + tasks all terminal.
3. Token-budget — sum stage bundle + per-task §Plan Digest body bytes (DB read). Hard cap 80k → exit code 78 = `STOPPED — token_budget_exceeded`; emit `Next: /ship-stage-main-session {SLUG} {STAGE_ID}`.
4. Resume gate — `task_state(task_id)` per task. Bucket as JSON: `pending_only` / `mixed` / `implemented_only` / `all_terminal`. `--no-resume` flag forces `pending_only` regardless.

Output JSON consumed by Pass A inference body: `{stage_id_db, tasks[], resume_bucket, digest_bodies}`.

### Phase 1 — Pass A — bulk emit task-batch body (Sonnet inference)

Single Sonnet inference. Boundary markers per task: `<!-- TASK:{ISSUE_ID} START -->` ... `<!-- TASK:{ISSUE_ID} END -->`. Inside markers: full implementation diff body for that task. Greppable by validators / code-review subagents. Skip tasks already `implemented` (resume).

After inference returns, BEFORE flipping any task status:

0. **Asmdef pre-flight (TECH-30633)** — when `git diff --name-only HEAD -- 'Assets/**/*.asmdef'` is non-empty OR Pass A inference emitted new `.asmdef` files, run `node tools/scripts/ship-cycle-pass-a-preflight.mjs`. Validates forward GUID refs (every `GUID:xxx` in touched asmdef resolves to existing `*.asmdef.meta` in repo OR was external at HEAD) and reverse-edge GUID consistency. Sub-second. On failure → `STOPPED at preflight — asmdef-diff: {reason}` + `Next: /ship-cycle {SLUG} {STAGE_ID}` after manual fix. When this step runs, the subsequent step 2 `unity:compile-check` MUST pass `--cold` (wipes `Library/ScriptAssemblies/` + `Library/Bee/`) to defeat stale-DLL false-greens; warm compile can silently reuse cached assemblies even when asmdef graph just shifted.
1. **Aggregate touched paths** — collect ALL `Assets/**/*.cs` paths across every task marker block.
2. **One unity:compile-check** — single batchmode invocation on the union diff. Pass `--cold` when step 0 ran (asmdef diff present). Replaces legacy per-task compile-check loop. On failure → `STOPPED at compile — {first_error}` + `Next: /ship-cycle {SLUG} {STAGE_ID}` resume.
3. **Batch flip** — `task_status_flip_batch(slug, stage_id, from='pending', to='implemented')` — single MCP call covers all tasks in this stage (DB CHECK enforces enum walk). Replaces legacy N×`task_status_flip` calls.
4. **Phase checkpoint** — `cron_journal_append_enqueue` ONCE per Pass A pass with `payload_kind=phase_checkpoint`, `phase=ship-cycle.1.pass_a_complete`, `decisions_resolved=[task_ids implemented + compile pass]`.

**NO per-task commits** — single stage commit at Pass B recipe step 7 covers all Pass A + Pass B diffs.

### Phase 2 — Pass B step 1 — verify-loop on cumulative diff (agent-owned)

Full `verify-loop` Path A + Path B on cumulative `git diff HEAD` (worktree dirty from Pass A edits). Verdict shape per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).

Write verdict file `/tmp/ship-cycle-verify-{slug}-{stage_id}.json`:

```json
{ "verdict": "pass" | "fail", "reason": "...", "duration_ms": <int> }
```

- `verdict == pass` → continue Phase 3 (Pass B recipe consumes verdict file).
- `verdict == fail` → `STAGE_VERIFY_FAIL` + chain digest + worktree stays dirty + no rollback of Pass A flips. Operator fixes manually then re-invokes `/ship-cycle {SLUG} {STAGE_ID}` (resume gate sees `implemented` → re-runs Phase 2).

No code-review in chain — operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).

### Phase 3 — Pass B steps 2–12 (recipe ship-cycle-pass-b)

Single recipe call drives the full mechanical Pass B tail:

```bash
npm run recipe:run -- ship-cycle-pass-b --input slug={SLUG} stage_id={STAGE_ID_DB}
```

Recipe steps (`tools/recipes/ship-cycle-pass-b.yaml`):

1. `verify_loop_check` — bash; reads `/tmp/ship-cycle-verify-{slug}-{stage_id}.json`. Non-`pass` → STOP.
2. `task_status_flip_batch` (verified) — MCP; single call for all tasks (`from='implemented'`, `to='verified'`). Enum walk requires intermediate state.
3. `task_status_flip_batch` (done) — MCP; single call (`from='verified'`, `to='done'`).
4. `stage_closeout_apply` — MCP; **DB-only** (per `mutations/stage.ts:105–195`). Atomic: shared migration ops deduped + per-Task `archived_at` set + Stage / Plan Status rolled up per R3 / R5. **NO inline `validate:all` / `materialize-backlog` — those are now separate recipe steps below.**
5. `materialize_backlog` — bash; `tools/scripts/materialize-backlog.sh` (lifted out of skill prose; flock'd inside).
6. `maybe_refresh_asset_db` — bash (B.7); conditional on `git diff HEAD` showing `Assets/**` paths. Enqueues `agent_bridge_job(kind=refresh_asset_database)`; emits `touched_assets={true|false}` + `bridge_job_id={uuid}` for downstream sync gate.
6b. `wait_asset_recompile` — bash (B.7b, **SYNC gate**); when `touched_assets=true`, poll `agent_bridge_job` until refresh `status=completed`, then enqueue fresh `get_compilation_status` job(s) until `compiling=false AND compilation_failed=false`. Hard fail (exit 1) aborts the recipe BEFORE stage commit. No-op when no Assets/** touched. Closes the false-green window where commits used to land before live Editor reparsed new source.
7. `git_commit_stage` — bash (B.8); per-file `git add` + single commit `feat({slug}-stage-{stage_id_db}): ship-cycle Pass B verify + closeout`. Captures `STAGE_COMMIT_SHA`.
8. `cron_audit_log_enqueue` — MCP; `audit_kind=stage_closed`.
9. `cron_task_commit_record_enqueue` — MCP foreach over tasks; records per-task commit sha.
10. `cron_stage_verification_flip_enqueue` — MCP; `verdict=pass`, `commit_sha=STAGE_COMMIT_SHA`.
11. `cron_validate_post_close_enqueue` — MCP (NEW, mig 0133); non-blocking `validate:fast --diff-paths` scoped to stage commit. Drainer writes verdict; `/ship-final` Phase 4.5 gates close on this queue draining for slug.
12. `cron_unity_compile_verify_enqueue` — MCP (NEW, mig 0134); belt-and-braces async compile-verify audit row. Sync B.7b gate is authoritative; this row exists for cross-stage telemetry + idle-state replay.

Recipe outputs: `{stage_commit_sha, verification_job_id, validate_job_id, compile_verify_job_id}`.

Pre-commit hook fail at step 7 → `STOPPED at commit — pre-commit hook failed: {reason}` (investigate; do NOT amend or `--no-verify`).

### Phase 4 — Chain digest + next-stage resolver

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
  "reason": "token_budget_exceeded | boundary_marker_unbalanced | compile_check_failed | task_status_flip_failed | stage_verify_fail | closeout_apply_failed | commit_failed | verification_flip_failed",
  "task_id": "<optional>",
  "stderr": "<optional>"
}
```

---

## Output

Emit exactly one of:

- `SHIP_CYCLE {STAGE_ID}: PASSED` — only after Phase 3 recipe completes (closeout + commit + 4 cron enqueues). Include `Next:` from Phase 4.
- `SHIP_CYCLE {STAGE_ID}: STOPPED — token_budget_exceeded` — `Next: /ship-stage-main-session {SLUG} {STAGE_ID}`.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at {ISSUE_ID} — {reason}` — Pass A failure; `Next: /ship-cycle {SLUG} {STAGE_ID}` resume.
- `SHIP_CYCLE {STAGE_ID}: STAGE_VERIFY_FAIL` — Pass B verify-loop failed; worktree stays dirty; manual fix then re-run.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — DB-drift repair directive.
- `SHIP_CYCLE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook.

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

### UI bake — force recompile before `bake_ui_from_ir` (stale-DLL gate)

When a Stage edits `Assets/Scripts/Editor/Bridge/UiBakeHandler.*.cs` OR `Assets/Scripts/Editor/UiBake/**`, an `unity_compile` MUST run BEFORE any `unity_bridge_command(kind="bake_ui_from_ir")` issued within the same Pass A iteration. Otherwise the bake executes against the prior Editor DLL — source edits silently invisible, false-green prefab. Pass A's per-stage aggregated `unity:compile-check` already covers stage close; this guardrail covers iterative bake-edit-rebake loops INSIDE Pass A.

Full contract — `ia/rules/ui-bake-pipeline.md` §2 (stale-DLL gate) + §1 (production dispatch path via `NormalizeChildKind → BakeChildByKind` switch) + §3 (dead-code map: KindRendererMatrix + KindRenderers/* + RowBakeHandler are non-production).

Recurrence evidence — `cityscene-mainmenu-panel-rollout` Stage 13: settings widgets baked with stale slider/toggle/dropdown shape despite repeated source edits; hours lost chasing dispatch mystery before identifying stale DLL as root cause.

### Stage_id canonical form (post mig 0132)

Mig 0132 adds `CHECK (stage_id ~ '^\d+(\.\d+)?$')` constraint at insert + generated column `stage_id_canonical` (auto-suffixes `.0` to bare-int rows). Per-plan format inconsistency now historical — all new stages are `N.M` form at DB level.

Phase 0 preflight resolves `STAGE_ID_DB` via `master_plan_state(slug).stages[]` regardless; the canonical literal propagates verbatim through Pass B recipe. Drainer FK fallback in `stage-verification-flip-cron-handler.ts:30` becomes belt-and-braces (post-mig 0132 it can never fire on new rows; retained for legacy slug data).

---

## Changelog

- 2026-05-05 — Pass B absorbed (verify-loop + verified→done flips + closeout + stage commit + verification flip). Chain prose updated: `design-explore → ship-plan → ship-cycle → ship-final`. `/ship-stage-main-session` retained as legacy fallback for token-budget-exceeded path; not chained.
- 2026-05-08 — `STAGE_PROGRESS={STAGE_INDEX}/{TOTAL_STAGES}` added to Phase 9 chain digest summary. Derived from `master_plan_state(slug).stages[]` — operator sees plan position at every handoff (e.g. `12/19`).
- 2026-05-08 (BUG-63) — Phase 8 step 0 added: `unity_bridge_command(kind="refresh_asset_database")` runs before `git add -A` when stage diff touches `Assets/**`. Live Editor writes `.meta` siblings synchronously into stage commit; eliminates orphan `.meta` drift accumulated when batchmode `unity:compile-check` runs in second-instance mode (project lock held by user's Editor → AssetDatabase writes skipped). Recurrence evidence: large-file-atomization-refactor stages 2–15, 65 orphan `.meta` swept in chore commit `bd153cc3`.
- 2026-05-08 — Stage_id literal match guardrail added (Guardrails §). `stage_id` propagated to closeout / cron flip / cron commit-record / audit / stage commit MUST be canonical literal from `ia_stages.stage_id` (resolved via `master_plan_state(slug).stages[]`). Recurrence evidence: `cron_stage_verification_flip_jobs.job_id=6436292f` 2026-05-08, slug `ui-implementation-mvp-rest`, agent emitted `"3"` vs canonical `"3.0"`; FK `ia_stage_verifications_stage_fk` violation surfaced only at async cron drain (row stuck `done` with `error`); manually replayed. Per-plan format inconsistency observed (`N.M` vs bare-int vs `stage-N-...`) — no global invariant; agent must resolve per slug. Server-side FK pre-check tracked separately (TECH issue).
- 2026-05-08 — Phase 8 step 0a added: post-refresh live-Editor compile poll. `refresh_asset_database` (step 0) was fire-and-forget — refresh kicks off async live-Editor recompile, but ship-cycle proceeded to commit + closeout while compile may still fail. Pass A `unity:compile-check` (batchmode 2nd-instance under project lock) passed with stale assembly cache while live Editor surfaced real errors only after refresh. Step 0a polls `get_compilation_status` every 2 s (initial wait 2 s, ceiling 60 s, configurable via `UNITY_COMPILE_POLL_CEILING_S`) until terminal state. New failure modes: `live_editor_compile_failed`, `compile_poll_ceiling_exceeded`, `bridge_unavailable`. Recurrence evidence: large-file-atomization-cutover-refactor stage-6-bridge-mutations shipped clean per ship-cycle but live Editor blocked on compile errors at next session start.
- 2026-05-10 — B.7b SYNC compile gate added (`wait_asset_recompile.sh`). Pass B recipe step inserted between B.7 (`maybe_refresh_asset_db`) and B.8 (`stage_commit`). When B.7 reports `touched_assets=true`, B.7b blocks on bridge round-trip: phase 1 waits for refresh job `status=completed`; phase 2 enqueues fresh `get_compilation_status` jobs until `compiling=false AND compilation_failed=false`. Hard fail aborts before any commit / closeout cron enqueue. Recurrence evidence: `cityscene-mainmenu-panel-rollout-stage-9.0` shipped `cf665d8b` with CS0051 + CS1061 errors — Pass A `unity:compile-check` batchmode passed (stale assembly cache under project-lock 2nd-instance), Pass B verify-loop read stale `compilation_failed` flag without forcing refresh, false-green verdict landed broken stage commit. B.7b closes the window. Also fixed latent recipe-engine bash bug: `${step.key}` references for kv emitted on stdout resolved to undefined → empty string downstream (e.g. `commit_sha`); `parseStdoutKv()` helper added to `tools/recipe-engine/src/steps/bash.ts` merges parsed `key=value` tokens into step value object. Cron `cron_unity_compile_verify_enqueue` retained as belt-and-braces audit row (not authoritative).
- 2026-05-10 — Lifecycle skills mechanical-work move-out (Phase 5 of cheeky-growing-panda plan). Phases 0–2 collapsed to `tools/scripts/recipe-engine/ship-cycle/preflight.sh` (parse + token-budget + resume gate via DB). Phases 6–8 collapsed to recipe `ship-cycle-pass-b.yaml` (12 mechanical steps: verify-loop check + 2× `task_status_flip_batch` + `stage_closeout_apply` + `materialize-backlog` + conditional asset-db refresh + stage commit + 4 cron enqueues incl. NEW `cron_validate_post_close_enqueue` (mig 0133) + `cron_unity_compile_verify_enqueue` (mig 0134)). Phase 9 renamed Phase 4 (chain digest). Phase 7 prose-fix: `stage_closeout_apply` documented as DB-only (per `mutations/stage.ts:105–195`); legacy claim of inline `validate:all` + `materialize-backlog` was prose drift — those are now separate recipe steps. Phase 8 step 0a sync `get_compilation_status` poll REMOVED — verdict drains async; resume gate of next `/ship-cycle` reads `master_plan_state(slug).stages[].compile_verify_verdict`. Stage_id literal match guardrail simplified (mig 0132 enforces canonical N.M form at insert). Pass A change: aggregate `Assets/**/*.cs` paths across tasks → ONE `unity:compile-check` (replaces per-task loop). Per-task `task_status_flip` calls replaced with single `task_status_flip_batch` (already exists, `task.ts:461–560`). Net: agent prompt body shrinks ~40 %; only Pass A inference + verify-loop coordination remain LLM-owned.
- 2026-05-11 (TECH-30633) — Three-layer asmdef gate added. Layer 1 = baseline graph validator (`tools/scripts/validate-asmdef-graph.mjs`, wired into `validate:all:readonly` + `validate:fast` baseline): Tarjan SCC over `Assets/**/*.asmdef` GUID/name refs + per-`.cs` `using` namespace closure against `rootNamespace` longest-prefix match (skips stdlib/engine: System, UnityEngine, UnityEditor, Unity., TMPro, NUnit, Newtonsoft). Layer 2 = `unity-compile-check.sh --cold` flag wipes `Library/ScriptAssemblies/` + `Library/Bee/` before batchmode invoke; defeats stale-DLL false-greens when asmdef graph shifts mid-stage. `--cold-dry-run` echoes flag-parse sanity without Unity invoke. Unknown flag → exit 2. Layer 3 = `tools/scripts/ship-cycle-pass-a-preflight.mjs` — sub-second structural pre-flight invoked from ship-cycle Phase 1 step 0 when `git diff --name-only HEAD -- 'Assets/**/*.asmdef'` is non-empty. Forward audit: every `GUID:xxx` ref in touched asmdef must resolve OR have been external-at-HEAD (PackageCache discrimination via `git ls-tree HEAD` snapshot). Reverse-edge audit: any other asmdef referring touched asmdef must still match sibling `.meta` GUID. When step 0 runs, step 2 `unity:compile-check` mandatorily passes `--cold`. Recurrence motivation: warm compile reuses cached assemblies → silent greens even when asmdef references rewritten.
