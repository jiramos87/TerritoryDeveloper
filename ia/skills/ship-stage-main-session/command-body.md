Execute the full `/ship-stage` chain for `$ARGUMENTS` **inline in this session**. Do **not** use the Agent/Task tool for any step.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, chain-level digest JSON, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Argument parsing

Split `$ARGUMENTS` on whitespace:

- `SLUG` = first token (master-plan slug, e.g. `blip`, `grid-asset`).
- `STAGE_ID` = second token (e.g. `7.2`). Strip `Stage ` prefix → `STAGE_ID_DB` for DB calls.
- Optional flag: `--no-resume` only. Append only if user explicitly asked.

Missing either positional → print usage and abort:

```
/ship-stage-main-session {SLUG} {STAGE_ID} [--no-resume]
```

Verify slug exists via `master_plan_state(slug=SLUG)`. Missing → STOPPED + `Next: claude-personal "/master-plan-new ..."` handoff. Capture `master_plan_title` from MCP result. Print context banner:

```
SHIP-STAGE (main-session) {STAGE_ID} — {master_plan_title}
  slug   : {SLUG}
  stage  : {STAGE_ID}
  flags  : {FLAGS or "(none)"}
  mode   : in-session (no subagents)
```

## Step 1 — Load the wrapper skill

Read `ia/skills/ship-stage-main-session/SKILL.md` end-to-end. Then read the canonical sources it references:

- `ia/skills/ship-stage/SKILL.md` (full DB-backed pipeline — Pass A no-commit + Pass B inline closeout)
- `.claude/commands/ship-stage.md` (canonical phase list + exit codes)

## Step 2 — Execute the chain inline

Perform every phase from `ia/skills/ship-stage/SKILL.md` **yourself**, in this session, using territory-ia MCP + bash + direct file edits. Do **not** dispatch any subagent (`ship-stage`, `spec-implementer`, `verify-loop`, `plan-applier`).

Phases (matches `ia/skills/ship-stage-main-session/SKILL.md` frontmatter `phases:`):

1. **Phase 0** — Parse stage (derive `SLUG`, `STAGE_ID_DB`, `SESSION_ID`).
2. **Phase 1** — Stage state load via `stage_bundle(slug, stage_id)` → `master_plan_title`, `stage`, `tasks`, `status_counts`, `next_pending`. Stale-DB → `/stage-file` handoff. Idle exit when stage done + tasks all terminal.
3. **Phase 2** — Context load via `domain-context-load` once; cache `CHAIN_CONTEXT`.
4. **Phase 3** — §Plan Digest readiness gate via `task_spec_section(task_id, "§Plan Digest")` per pending task (literal `§` prefix; bare `"Plan Digest"` returns `section_not_found`). Missing/empty → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff. No JIT lazy migration.
5. **Phase 4** — Resume gate via `task_state` DB query per pending task. `pending` → Pass A required; `implemented` → skip Pass A. All implemented + stage not done → `PASS_B_ONLY` (worktree dirty required; clean → STOPPED). Disabled by `--no-resume`.
6. **Phase 5 — Pass A per-task loop** (sequential, fail-fast, **NO commits**):
   - `spec-implementer` work inline — read `§Plan Digest` via `task_spec_section`, apply edits in declared order, resolve anchors via `plan_digest_resolve_anchor`.
   - `npm run unity:compile-check` (~15 s fast-fail) + scene-wiring preflight when §Plan Digest carries Scene Wiring step.
   - `task_status_flip(task_id, "implemented")` + `cron_journal_append_enqueue(phase: "pass_a.implemented")`.
   - **NO per-task commits** (single stage commit at Phase 8).
7. **Phase 6 — Pass B per-stage** (runs ONCE):
   - **6.1 verify-loop** — full Path A + Path B on cumulative `git diff HEAD` (Pass A worktree dirty). `verdict == pass` required; fail → `STAGE_VERIFY_FAIL` + chain digest, no rollback, worktree stays dirty.
   - **6.2 per-task verified→done flips** — for each task in `STAGE_TASK_IDS` (skip if already terminal): `task_status_flip(task_id, "verified")` then `task_status_flip(task_id, "done")` (enum walk requires both).

   No code-review in chain — operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).
8. **Phase 7 — Inline closeout (DB-only)** — `stage_closeout_apply(slug, stage_id)` (DB-backed atomic) + `cron_audit_log_enqueue({slug, audit_kind:"stage_closed", body, stage_id, commit_sha})` audit row (fire-and-forget; cron drains to `ia_master_plan_change_log` within 90 s). No filesystem mv.
9. **Phase 8 — Stage commit + verification record** — single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` covers ALL Pass A diffs after verify-loop pass. Resume note: if `git diff HEAD` empty (PASS_B_ONLY re-run after prior commit), skip commit + reuse `git rev-parse HEAD` as `STAGE_COMMIT_SHA`. Capture `STAGE_COMMIT_SHA`. Per-task `cron_task_commit_record_enqueue(task_id, commit_sha=STAGE_COMMIT_SHA, commit_kind="feat", ...)` — fire-and-forget < 100 ms. `cron_stage_verification_flip_enqueue(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage-main-session")` — fire-and-forget < 100 ms; cron drains to `ia_stage_verifications` within 90 s.
10. **Phase 9** — Chain digest (JSON header `chain_stage_digest: true` + caveman summary + `next_handoff` block).
11. **Phase 10** — Next-stage resolver via `master_plan_state(slug)` — 3 cases priority: filed → `/ship-stage`; pending → `/stage-file`; umbrella-done → no further command (plan complete; inline `stage_closeout_apply` already recorded per-stage). Skeleton stages → `STOPPED — skeleton stage encountered`.

## Hard boundaries (critical)

- **Pass A NEVER commits.** Single stage-end commit at Phase 8 covers everything.
- **No code-review in this chain.** Operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).
- **Inline closeout (Phase 7) mandatory on green Pass B.** Never defer to separate closeout invocation.
- DB is sole source of truth — no `ia/projects/**` reads or writes.
- Resume gate queries DB (`task_state`) only — no git scan.
- `SHIP_STAGE {STAGE_ID}: PASSED` is **invalid** until Phase 7 closeout + Phase 8 commit (or empty-diff resume reuse) + verification flip succeed.
- Append `--no-resume` only on explicit user request.

## Step 3 — Output

Emit exactly one of:

- `SHIP_STAGE {STAGE_ID}: PASSED` — **only** after Phase 7 closeout + Phase 8 stage commit + `cron_stage_verification_flip_enqueue` succeed. Include `Next:` from Phase 10 resolver.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` — include `Next: /stage-authoring {SLUG} Stage {STAGE_ID}` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` — include `Next: /stage-file ...` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. ...` — manual-repair directive.
- `STOPPED at {ISSUE_ID} — implement: {reason}` / `compile_gate: {reason}` / `scene_wiring: {reason}` — Pass A failure; include `Next: /ship-stage-main-session {SLUG} {STAGE_ID}` resume line after fix.
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — `Human review required — worktree stays dirty; do NOT roll back Pass A status flips automatically.`
- `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — DB-drift repair directive.
- `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook (do NOT amend or `--no-verify`).

Followed by pipeline summary block:

```
SHIP-STAGE (main-session) {STAGE_ID}: {PASSED|STOPPED|STAGE_VERIFY_FAIL}
  slug          : {SLUG} ({master_plan_title})
  tasks shipped : {count} ({ids})
  stage commit  : {short_sha} (when PASSED)
  stage verify  : {passed|failed|skipped}
```
