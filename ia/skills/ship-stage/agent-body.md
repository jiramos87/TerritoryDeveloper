# Mission

Drive every non-terminal task of `Stage {STAGE_ID}` for master-plan slug `{SLUG}` through a two-pass DB-backed chain per `ia/skills/ship-stage/SKILL.md` (11-phase pipeline).

**Pass A (per-task):** implement → `unity:compile-check` fast-fail gate → `task_status_flip(task_id, "implemented")`. **NO per-task commits** — single stage-end commit covers everything. Worktree stays dirty across Pass A loop.

**Pass B (per-stage, runs ONCE):** verify-loop on cumulative HEAD diff → code-review (Stage diff; inline fix cap=1 — reviewer applies critical fixes via direct Edit/Write, no `§Code Fix Plan` tuples) → per-task `task_status_flip(verified)` then `task_status_flip(done)` (enum walk requires both) → `stage_closeout_apply` MCP tool (DB-backed inline closeout) → `master_plan_change_log_append` audit row → single stage commit `feat({SLUG}-stage-{STAGE_ID_DB})` → per-task `task_commit_record` → `stage_verification_flip(pass, commit_sha)`.

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

1. **Phase 0 — Parse stage** — derive `SLUG` + `STAGE_ID_DB` + `SESSION_ID` from `$ARGUMENTS`.
2. **Phase 1 — Stage state load** — `stage_bundle(slug, stage_id)` → `master_plan_title`, `stage`, `tasks`, `status_counts`, `next_pending`. Stale-DB → `/stage-file` handoff. Idle exit when stage done + all tasks terminal.
3. **Phase 2 — Context load** — `domain-context-load` once for stage domain; cache as `CHAIN_CONTEXT` (passed to Pass A spec-implementer + Pass B code-reviewer). `tooling_only_flag` heuristic per SKILL Step 2.
4. **Phase 3 — §Plan Digest readiness gate** — `task_spec_section(task_id, "§Plan Digest")` per pending task (literal `§` prefix; bare `"Plan Digest"` returns `section_not_found`). Missing/empty → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff. No JIT lazy migration.
5. **Phase 4 — Resume gate** — DB status scan via `task_state` per pending task. `pending` → Pass A required; `implemented` → skip Pass A. All implemented → `PASS_B_ONLY` (verify worktree dirty; clean → STOPPED with manual-repair directive). Skipped under `--no-resume`.
6. **Phase 5 — Pass A per-task loop** — sequential, fail-fast, NO commits. For each task: implement (`spec-implementer` work inline) → `unity:compile-check` + scene-wiring preflight when §Plan Digest carries Scene Wiring step → `task_status_flip(implemented)` + `journal_append`. Stop on first failure; emit partial chain digest.
7. **Phase 6 — Pass B per-stage** — runs ONCE after Pass A loop. 6.1 verify-loop full Path A+B on cumulative `git diff HEAD` → 6.2 code-review on Stage diff (inline fix; cap=1 re-entry) → 6.3 per-task `task_status_flip(verified)` then `task_status_flip(done)`.
8. **Phase 7 — Inline closeout (DB-only)** — `stage_closeout_apply(slug, stage_id)` (DB-backed atomic) + `master_plan_change_log_append` audit row. No filesystem mv.
9. **Phase 8 — Stage commit + verification record** — single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` covers ALL changes (Pass A diffs + code-review fixes). Capture `STAGE_COMMIT_SHA`. Per-task `task_commit_record(task_id, commit_sha, "feat", ...)`. `stage_verification_flip(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage")` (history-preserving INSERT).
10. **Phase 9 — Chain digest** — JSON header (`chain_stage_digest: true`, slug, stage_id, tasks_shipped, stage_verify, stage_commit_sha, archived_task_count, next_handoff) + caveman summary.
11. **Phase 10 — Next-stage resolver** — re-call `master_plan_state(slug)`. 4 cases priority: filed → `/ship-stage`; pending → `/stage-file`; skeleton → `/stage-decompose`; umbrella-done → `/closeout {UMBRELLA_ISSUE_ID}` (or "umbrella close pending").

# Verification

**Pass A:** Per-task `unity:compile-check` (~15 s fast-fail gate) mandatory. No verify-loop per task.

**Pass B (always):** Single `verify-loop` run — full Path A+B on cumulative HEAD diff (Pass A worktree dirty; equivalent: `git diff HEAD`). Single `code-review` on Stage diff with shared `CHAIN_CONTEXT`. Re-entry cap=1 for critical verdict (reviewer applies inline fixes, then re-enter Step 6.1 verify-loop once + re-run Step 6.2; second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`).

# Exit lines

- `SHIP_STAGE {STAGE_ID}: PASSED` — readiness gate + Pass A complete + Pass B verify pass + code-review non-critical + Step 7 inline closeout succeeded + Step 8 stage commit landed + `stage_verification_flip` recorded; next-stage handoff emitted. **Invalid** if emitted before Step 7 closeout + Step 8 commit.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` — Phase 3 readiness gate failed; `Next: claude-personal "/stage-authoring {SLUG} Stage {STAGE_ID_DB}"` then re-invoke `/ship-stage`.
- `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` — Phase 1 stale-DB; `Next: /stage-file {SLUG} {STAGE_ID}`.
- `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. DB says implemented but disk has nothing.` — Phase 4 inconsistency; manual repair (re-run Pass A or `task_status_flip` back to pending).
- `STOPPED at {ISSUE_ID} — implement: {reason}` / `compile_gate: {reason}` / `scene_wiring: {reason}` — Pass A failure; partial chain digest; `Next: /ship-stage {SLUG} Stage {STAGE_ID_DB}` (resume gate picks up after fix).
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — Pass B verify failed; chain digest with `stage_verify: failed` + escalation taxonomy; no rollback; worktree stays dirty; human review.
- `STAGE_CODE_REVIEW_CRITICAL_TWICE` — code-review critical on initial + post-fix re-entry; structural issue; human review.
- `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — `stage_closeout_apply` rejected; DB drift; human repair.
- `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook (do NOT amend or `--no-verify`).

# Hard boundaries

- Sequential per-task dispatch only — no parallel.
- **Pass A NEVER commits.** Single stage-end commit at Step 8.1 covers all Pass A diffs + code-review fixes. Do NOT emit `feat({ISSUE_ID}):` per task.
- Resume gate (Phase 4) queries DB via `task_state` / `stage_bundle` only. Do NOT git-scan.
- Stop on first Pass A gate failure (implement, compile, scene-wiring); do NOT continue to next task.
- Do NOT rollback Pass A status flips on `STAGE_VERIFY_FAIL` or `STAGE_CODE_REVIEW_CRITICAL_TWICE` — DB stays `implemented`; worktree stays dirty; human repairs via re-run.
- Code-review critical re-entry cap=1; second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`.
- **Code-reviewer applies fixes inline via direct Edit/Write.** Do NOT write `§Code Fix Plan` tuples.
- **Pass B (verify → code-review → closeout → commit → verification record) is MANDATORY. `PASSED` is forbidden until Step 8 commit + verification flip succeed.** Applies on resume path too (PASS_B_ONLY).
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
Phase 6 Pass B: single-line per gate (verify verdict / code-review verdict + inline-fix iteration / per-task verified→done flips).
Phase 7: closeout result (DB archived count).
Phase 8: stage commit sha + per-task `task_commit_record` count + `stage_verification_flip` ack.
Phase 9: chain-level stage digest (JSON header + caveman summary).
Phase 10: `Next:` handoff line.
Final: `SHIP_STAGE {STAGE_ID}: PASSED` | `STOPPED — prerequisite: §Plan Digest not populated for ...` | `STOPPED — stage not found in DB` | `STOPPED at {ISSUE_ID} — {gate}: {reason}` | `STAGE_VERIFY_FAIL` | `STAGE_CODE_REVIEW_CRITICAL_TWICE` | `STOPPED at closeout — {reason}` | `STOPPED at commit — {reason}`.
