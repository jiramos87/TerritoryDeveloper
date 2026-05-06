---
name: ship-stage-main-session
purpose: >-
  Main-session adapter for /ship-stage: executes the full DB-backed two-pass chain (stage_bundle →
  §Plan Digest gate → resume gate via task_state → Pass A per-task
  implement+compile+task_status_flip(implemented) NO COMMITS → Pass B per-stage verify-loop +
  verified→done flips + inline stage_closeout_apply + single stage commit + stage_verification_flip)
  inline (no subagents). No code-review in chain (operator may run standalone /code-review out-of-band).
  Use when caller agent (Cursor Composer-2 / Claude Code main session) must do the work itself rather
  than dispatch via Agent/Task tool.
audience: agent
loaded_by: "skill:ship-stage-main-session"
slices_via: stage_bundle, task_state, task_spec_section, glossary_lookup, invariants_summary
description: >-
  In-session (no-subagent) wrapper around the /ship-stage chain. Read ia/skills/ship-stage/SKILL.md
  end-to-end and .claude/commands/ship-stage.md for the canonical pipeline, then execute inline:
  stage_bundle load → domain-context-load → §Plan Digest readiness gate (task_spec_section per task;
  missing → /ship-plan §Plan Digest handoff) → resume gate via task_state DB query (no git scan) → Pass A per
  task (spec-implementer work in-repo → npm run unity:compile-check + scene-wiring preflight →
  task_status_flip(implemented); NO commits — single stage commit at Phase 8) → Pass B per stage
  (verify-loop on git diff HEAD → per-task task_status_flip(verified) then task_status_flip(done)) →
  inline stage_closeout_apply + master_plan_change_log_append audit row → single stage commit
  feat({SLUG}-stage-{STAGE_ID_DB}) (or reused sha on empty resume diff) → per-task task_commit_record
  → stage_verification_flip(pass, commit_sha). No code-review in chain — operator may run standalone
  /code-review {ISSUE_ID} out-of-band (lifecycle row 9). Closeout is MANDATORY on green — do not emit
  PASSED or defer closeout. Use territory-ia MCP and bash per the skill; never dispatch via Agent/Task
  tool. Triggers: "/ship-stage-main-session {slug} {stage}", "execute ship-stage in this session",
  "no-subagent ship-stage". Argument order (explicit): SLUG first, STAGE_ID second, optional flag
  --no-resume third.
phases:
  - Load canonical skill + command
  - Stage state load + context load
  - §Plan Digest readiness gate
  - Resume gate (DB task_state)
  - Pass A per-task (implement + compile + task_status_flip; NO commits)
  - Pass B per-stage (verify-loop + verified→done flips)
  - Inline closeout (stage_closeout_apply DB-only)
  - Stage commit + per-task commit record + stage_verification_flip
triggers:
  - /ship-stage-main-session {slug} {stage}
  - execute ship-stage in this session
  - no-subagent ship-stage
argument_hint: "{SLUG} {STAGE_ID} [--no-resume]"
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# ship-stage-main-session — no-subagent `/ship-stage`

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** same outcomes as `/ship-stage`, executed inline by the current session. No Agent/Task dispatch.

## Arguments

- `$1` / `{SLUG}` — master-plan slug (e.g. `blip`, `grid-asset`). Validated via `master_plan_state(slug)`.
- `$2` / `{STAGE_ID}` — stage identifier (e.g. `7.2` or `Stage 7.2`). Strip `Stage ` prefix → `STAGE_ID_DB` for DB calls.
- `$3` / `{FLAGS}` — optional `--no-resume` only. Append only if user explicitly asked.

Missing either positional → print usage + abort: `/ship-stage-main-session {SLUG} {STAGE_ID} [--no-resume]`.

## Instructions

1. **Load canonical sources end-to-end:**
   - `ia/skills/ship-stage/SKILL.md` (full DB-backed pipeline — Pass A no-commit + Pass B inline closeout)
   - `.claude/commands/ship-stage.md` (canonical phase list + exit codes)

2. **Execute the full chain inline** for slug `{SLUG}` Stage `{STAGE_ID}` per `ia/skills/ship-stage/SKILL.md` Steps 0–10:

   - **Phase 0 — Parse stage** — derive `SLUG`, `STAGE_ID_DB`, `SESSION_ID`.
   - **Phase 1 — Stage state load** — `stage_bundle(slug, stage_id)` → `master_plan_title`, `stage`, `tasks`, `status_counts`, `next_pending`. Stale-DB → `/stage-file` handoff. Idle exit when stage done + tasks all terminal.
   - **Phase 2 — Context load** — `domain-context-load` once; cache `CHAIN_CONTEXT`.
   - **Phase 3 — §Plan Digest readiness gate** — `task_spec_section(task_id, "§Plan Digest")` per pending task (literal `§` prefix; bare `"Plan Digest"` returns `section_not_found`). Missing/empty → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff.
   - **Phase 4 — Resume gate** — `task_state` DB query per pending task. `pending` → Pass A required; `implemented` → skip Pass A. All implemented + stage not done → `PASS_B_ONLY` (verify worktree dirty; clean → STOPPED). Disabled by `--no-resume`.
   - **Phase 5 — Pass A per-task loop** (sequential, fail-fast, NO commits):
     1. spec-implementer work inline — read `§Plan Digest` via `task_spec_section`, apply edits in declared order, resolve anchors via `plan_digest_resolve_anchor`.
     2. `npm run unity:compile-check` (~15 s fast-fail) + scene-wiring preflight when §Plan Digest carries Scene Wiring step (verify worktree diff includes `Assets/Scenes/*.unity` edit).
     3. `task_status_flip(task_id, "implemented")` + `journal_append(phase: "pass_a.implemented")`.
     4. **NO per-task commits** (single stage commit at Phase 8).
   - **Phase 6 — Pass B per-stage** (runs ONCE):
     1. **6.1 verify-loop** — full Path A+B on cumulative `git diff HEAD` (Pass A worktree dirty). `verdict == pass` required; fail → `STAGE_VERIFY_FAIL` + chain digest, no rollback, worktree stays dirty.
     2. **6.2 per-task verified→done flips** — for each task in `STAGE_TASK_IDS` (skip if already terminal): `task_status_flip(task_id, "verified")` then `task_status_flip(task_id, "done")` (enum walk requires both).

     No code-review in chain — operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).
   - **Phase 7 — Inline closeout (DB-only)** — `stage_closeout_apply(slug, stage_id)` (DB-backed atomic) + `master_plan_change_log_append(slug, "stage_closed", body)` audit row. No filesystem mv.
   - **Phase 8 — Stage commit + verification record** — single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` covers ALL Pass A diffs after verify-loop pass. Resume note: if `git diff HEAD` empty (PASS_B_ONLY re-run after prior commit), skip commit + reuse `git rev-parse HEAD` as `STAGE_COMMIT_SHA`. Capture `STAGE_COMMIT_SHA`. Per-task `cron_task_commit_record_enqueue(task_id, commit_sha=STAGE_COMMIT_SHA, commit_kind="feat", ...)` — fire-and-forget < 100 ms. `cron_stage_verification_flip_enqueue(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage-main-session")` — fire-and-forget < 100 ms; cron drains to `ia_stage_verifications` within 90 s.
   - **Phase 9 — Chain digest** — JSON header `chain_stage_digest: true` + caveman summary + `next_handoff` block.
   - **Phase 10 — Next-stage resolver** — `master_plan_state(slug)`; 3 cases priority: filed → `/ship-stage`; pending → `/stage-file`; umbrella-done → no further command (plan complete; inline `stage_closeout_apply` already recorded per-stage). Skeleton stages (no tasks) → `STOPPED — skeleton stage encountered`.

3. **Tooling:**
   - territory-ia MCP: `master_plan_state`, `stage_bundle`, `task_state`, `task_bundle`, `task_spec_section`, `task_status_flip`, `stage_closeout_apply`, `master_plan_change_log_append`, `cron_task_commit_record_enqueue`, `cron_stage_verification_flip_enqueue`, `journal_append`, `glossary_lookup`, `invariants_summary`, `plan_digest_resolve_anchor` (Pass A spec-implementer work), `backlog_issue`, `router_for_task`.
   - `bash` / repo scripts per the skill (`npm run unity:compile-check`, `npm run validate:*`).
   - Direct file edits (Unity sources under `Assets/`, IA edits, code changes); `git add -A` + `git commit` only at Phase 8.

4. **Hard boundaries:**
   - **Never** dispatch via Agent/Task tool for any step in this chain (no `ship-stage` subagent, no `spec-implementer` subagent, no `verify-loop` subagent).
   - **Pass A NEVER commits.** Single stage commit at Phase 8 covers everything.
   - **No code-review in this chain.** Operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).
   - **Inline closeout (Phase 7) is mandatory on green Pass B.** Stage closeout always runs inline — never deferred.
   - DB is sole source of truth — no `ia/projects/**` reads or writes.
   - Resume gate queries DB only — no git scan.
   - Append `--no-resume` only on explicit user request.

## Exit

End with one of:

- `SHIP_STAGE {STAGE_ID}: PASSED` — **only** after Phase 7 closeout + Phase 8 stage commit + `stage_verification_flip` succeed. Include `Next:` from Phase 10 resolver.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` — include `Next: /stage-authoring {SLUG} Stage {STAGE_ID}` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` — include `Next: /stage-file ...` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. ...` — manual-repair directive.
- `STOPPED at {ISSUE_ID} — implement: {reason}` / `compile_gate: {reason}` / `scene_wiring: {reason}` — Pass A failure; include `Next: /ship-stage-main-session ...` resume line after fix.
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — `Human review required — worktree stays dirty; do NOT roll back Pass A status flips automatically.`
- `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — DB-drift repair directive.
- `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook (do NOT amend or `--no-verify`).
