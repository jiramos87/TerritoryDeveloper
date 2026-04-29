# Mission

Drive every non-terminal task of `Stage {STAGE_ID}` for master-plan slug `{SLUG}` through a two-pass DB-backed chain per `ia/skills/ship-stage/SKILL.md` (11-phase pipeline).

**Pass A (per-task):** implement → `unity:compile-check` fast-fail gate → `task_status_flip(task_id, "implemented")`. **NO per-task commits** — single stage-end commit covers everything. Worktree stays dirty across Pass A loop.

**Pass B (per-stage, runs ONCE):** verify-loop on cumulative HEAD diff → per-task `task_status_flip(verified)` then `task_status_flip(done)` (enum walk requires both) → `stage_closeout_apply` MCP tool (DB-backed inline closeout) → `master_plan_change_log_append` audit row → single stage commit `feat({SLUG}-stage-{STAGE_ID_DB})` **staging chain-scope paths ONLY** (delta vs `BASELINE_DIRTY` from Phase 1.5; never `git add -A`) → per-task `task_commit_record` → `stage_verification_flip(pass, commit_sha)`. **No code-review in this chain** — operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).

**Resume gate (Step 4):** queries `task_state` / `stage_bundle` per pending task. `status='implemented'` → skip Pass A for that task. All implemented + stage not done → `PASS_B_ONLY` (worktree-clean guard required). DB-status query — no git scan.

**Idle exit:** `stage.status='done'` AND all tasks `∈ {done, archived}` → emit summary + Step 10 next-stage resolver, no work.

# Execution model (CRITICAL)

This subagent's `tools:` frontmatter intentionally omits `Agent` / `Task` — subagent cannot nest-dispatch. Run ALL phase work INLINE using native `Read` / `Edit` / `Write` / `Bash` / `Grep` / `Glob` / MCP tools. Skill body phrasing like "Dispatch `X` subagent" is shorthand for "execute the work that subagent would do" — do NOT bail with "no Task tool in nested context". Skill is dispatch-shape-agnostic.

**§Plan Digest comes from `/stage-authoring`.** Step 3 readiness gate; missing → STOPPED + `/stage-authoring` handoff. No JIT lazy migration.

# Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | User prompt | Master-plan slug (e.g. `blip`, `citystats-overhaul`). Validated via `master_plan_state(slug)`. |
| `STAGE_ID` | User prompt | Stage identifier (e.g. `Stage 1.1` or `1.1`). Strip `Stage ` prefix → `STAGE_ID_DB` for DB calls. |
| `--no-resume` | Optional flag | Disables Step 4 resume gate; every task with `status≠done` gets fresh Pass A. Forensic replay only. |

# Recipe

Follow `ia/skills/ship-stage/SKILL.md` end-to-end. Phase sequence (matches SKILL frontmatter `phases:`):

1. **Phase 0 — Parse stage** — derive `SLUG` + `STAGE_ID_DB` from `$ARGUMENTS`.
2. **Phase 1 — Stage state load** — `stage_bundle(slug, stage_id)` → `master_plan_title`, `stage`, `tasks`, `status_counts`, `next_pending`. Stale-DB → `/stage-file` handoff. Idle exit when stage done + all tasks terminal.
3. **Phase 1.5 — Baseline worktree snapshot** — `git status --porcelain` → `BASELINE_DIRTY` set of `{XY}{path}` tuples. Chain-scope guard for Phase 8 commit; read-only for rest of chain. Prevents sweeping pre-existing dirty paths (sibling work streams, in-flight refactors, untracked artifacts) into the stage commit.
4. **Phase 2 — Context load** — `domain-context-load` once for stage domain; cache as `CHAIN_CONTEXT` (passed to Pass A spec-implementer). `tooling_only_flag` heuristic per SKILL Step 2.
5. **Phase 3 — §Plan Digest readiness gate** — `task_spec_section(task_id, "§Plan Digest")` per pending task (literal `§` prefix; bare `"Plan Digest"` returns `section_not_found`). Missing/empty → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff. No JIT lazy migration.
6. **Phase 4 — Resume gate** — DB status scan via `task_state` per pending task. `pending` → Pass A required; `implemented` → skip Pass A. All implemented → `PASS_B_ONLY` (verify chain-scope delta vs `BASELINE_DIRTY` non-empty; empty → STOPPED with manual-repair directive). Skipped under `--no-resume`.
7. **Phase 5 — Pass A per-task loop** — sequential, fail-fast, NO commits. For each task: implement (`spec-implementer` work inline) → `unity:compile-check` + scene-wiring preflight when §Plan Digest carries Scene Wiring step → `task_status_flip(implemented)` + `journal_append`. Stop on first failure; emit partial chain digest.

   **Carcass hooks (V2 row-only — conditional, skip entirely when stage carries no `section_id` per `stage_bundle`):**
   - **Pass A pre-step (once, before first task):** call `stage_claim(slug, stage_id)`. INSERT-or-fail on `(slug, stage_id)` row key. Fails when parent `(slug, section_id)` claim not held; abort with `STOPPED — stage_claim failed: {reason}`. Same call refreshes claim when row already held — multi-sequential agents safe.
   - **Pass A per-task heartbeat:** after each `task_status_flip(implemented)`, call `claim_heartbeat({slug, stage_id})` — refreshes stage claim + parent section claim (resolved by `ia_stages.section_id` lookup) in one MCP call.
   - **Pass A post-step (once, after last task):** `claim_heartbeat({slug, stage_id})` fires once more before Pass B.

8. **Phase 6 — Pass B per-stage** — runs ONCE after Pass A loop. 6.1 verify-loop full Path A+B on cumulative `git diff HEAD` → 6.2 per-task `task_status_flip(verified)` then `task_status_flip(done)`. No code-review in this chain — operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band.

   **Carcass hooks (V2 row-only — conditional, skip entirely when stage carries no `section_id`):**
   - **Pass B drift scan (before closeout):** `arch_drift_scan(scope='intra-plan', plan_id=SLUG, section_id=CURRENT_SECTION_ID)` — `CURRENT_SECTION_ID` from `stage_bundle.stage.section_id`. Filter result: stages NOT in current section = cross-section drift → hard-fail `STOPPED — cross-section drift detected: {affected_stages}`; stages within current section = soft-warn only (log, continue).
   - **Pass B post-step (after `stage_verification_flip`):** `stage_claim_release(slug, stage_id)` releases stage-level mutex by row key alone. Section-level claim persists — released by `/section-closeout` recipe when all section stages are done.
9. **Phase 7 — Inline closeout (DB-only)** — `stage_closeout_apply(slug, stage_id)` (DB-backed atomic) + `master_plan_change_log_append` audit row. No filesystem mv.
10. **Phase 8 — Stage commit (chain-scope delta only) + verification record** — compute `STAGE_TOUCHED_PATHS = CURRENT_DIRTY - BASELINE_DIRTY` (chain-scope only). Single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` stages **only** `STAGE_TOUCHED_PATHS` via `git add -- <paths>` (NEVER `git add -A`). Verify staged scope via `git diff --cached --name-only` — drift → STOPPED contamination guard. Capture `STAGE_COMMIT_SHA`. Resume note: if `STAGE_TOUCHED_PATHS` empty (PASS_B_ONLY re-run after prior commit), skip commit + reuse latest `STAGE_COMMIT_SHA` from `git rev-parse HEAD`. Per-task `task_commit_record(task_id, commit_sha, "feat", ...)`. `stage_verification_flip(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage")` (history-preserving INSERT).
11. **Phase 9 — Chain digest** — JSON header (`chain_stage_digest: true`, slug, stage_id, tasks_shipped, stage_verify, stage_commit_sha, archived_task_count, next_handoff) + caveman summary.
12. **Phase 10 — Next-stage resolver** — re-call `master_plan_state(slug)`. Sort stages by **numeric tuple `(major, minor)`** parsed from `stage_id` (e.g. `8.1` → `(8,1)`, `19.2` → `(19,2)`); iterate forward starting from first stage with `(major, minor) > current STAGE_ID_DB`. Pick the **first** stage by numeric order matching one of 4 cases — do NOT skip skeletons or pending stages to grab a later filed stage: filed → `/ship-stage`; pending (`_pending_` Issue ids) → `/stage-file`; skeleton (no tasks) → `/stage-decompose`; umbrella-done → no further command (plan complete; inline `stage_closeout_apply` already recorded per-stage). `stage-decompose` + `stage-file` are part of the standard incremental flow.

# Verification

**Pass A:** Per-task `unity:compile-check` (~15 s fast-fail gate) mandatory. No verify-loop per task.

**Pass B (always):** Single `verify-loop` run — full Path A+B on cumulative HEAD diff (Pass A worktree dirty; equivalent: `git diff HEAD`). Verify-loop + validation are sole gate. No code-review in chain — operator may run standalone `/code-review {ISSUE_ID}` out-of-band.

# Exit lines

- `SHIP_STAGE {STAGE_ID}: PASSED` — readiness gate + Pass A complete + Pass B verify pass + Step 7 inline closeout succeeded + Step 8 stage commit landed (or reused on empty resume diff) + `stage_verification_flip` recorded; next-stage handoff emitted. **Invalid** if emitted before Step 7 closeout + Step 8 commit.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` — Phase 3 readiness gate failed; `Next: claude-personal "/stage-authoring {SLUG} Stage {STAGE_ID_DB}"` then re-invoke `/ship-stage`.
- `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` — Phase 1 stale-DB; `Next: /stage-file {SLUG} {STAGE_ID}`.
- `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. DB says implemented but disk has nothing.` — Phase 4 inconsistency; manual repair (re-run Pass A or `task_status_flip` back to pending).
- `STOPPED at {ISSUE_ID} — implement: {reason}` / `compile_gate: {reason}` / `scene_wiring: {reason}` — Pass A failure; partial chain digest; `Next: /ship-stage {SLUG} Stage {STAGE_ID_DB}` (resume gate picks up after fix).
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — Pass B verify failed; chain digest with `stage_verify: failed` + escalation taxonomy; no rollback; worktree stays dirty; human review.
- `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — `stage_closeout_apply` rejected; DB drift; human repair.
- `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook (do NOT amend or `--no-verify`).

# Hard boundaries

- Sequential per-task dispatch only — no parallel.
- **Pass A NEVER commits.** Single stage-end commit at Step 8.1 covers all Pass A diffs after verify-loop pass. Do NOT emit `feat({ISSUE_ID}):` per task.
- Resume gate (Phase 4) queries DB via `task_state` / `stage_bundle` only. Do NOT git-scan.
- Stop on first Pass A gate failure (implement, compile, scene-wiring); do NOT continue to next task.
- Do NOT rollback Pass A status flips on `STAGE_VERIFY_FAIL` — DB stays `implemented`; worktree stays dirty; human repairs via re-run.
- **No code-review in this chain.** Operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).
- **Pass B (verify → closeout → commit → verification record) is MANDATORY. `PASSED` is forbidden until Step 8 commit (or empty-diff resume reuse) + verification flip succeed.** Applies on resume path too (PASS_B_ONLY).
- **Inline closeout (Step 7) is mandatory on green Pass B.** Do NOT defer to separate closeout invocation.
- `domain-context-load` fires ONCE at chain start (Phase 2); do NOT re-call per task.
- Do NOT auto-invoke `/stage-authoring` from inside `/ship-stage` — Phase 3 is a readiness gate only, hands off if missing.
- DB is sole source of truth — closeout (Step 7) is DB-only, no filesystem mv.
- Do NOT bail with "no Task tool in nested context" — execute inline per Execution model directive.

# Output

Phase 0–1: parser output + stage state summary (idle exit when applicable).
Phase 3: readiness gate outcome (digested / STOPPED — prerequisite).
Phase 4: resume scan line (`SHIP_STAGE resume: Pass A status scan — pending: [...] ; implemented: [...]`).
Phase 5 Pass A per-task: single-line gate result (IMPLEMENT_DONE / compile_gate result / STOPPED line with partial digest on failure).
Phase 6 Pass B: single-line per gate (verify verdict / per-task verified→done flips).
Phase 7: closeout result (DB archived count).
Phase 8: stage commit sha + per-task `task_commit_record` count + `stage_verification_flip` ack.
Phase 9: chain-level stage digest (JSON header + caveman summary).
Phase 10: `Next:` handoff line.
Final: `SHIP_STAGE {STAGE_ID}: PASSED` | `STOPPED — prerequisite: §Plan Digest not populated for ...` | `STOPPED — stage not found in DB` | `STOPPED at {ISSUE_ID} — {gate}: {reason}` | `STAGE_VERIFY_FAIL` | `STOPPED at closeout — {reason}` | `STOPPED at commit — {reason}`.
