---
description: No-subagent variant of /ship-stage. Executes the full DB-backed two-pass chain (stage_bundle → §Plan Digest gate → DB resume gate → Pass A per-task implement+compile+task_status_flip(implemented) NO COMMITS → Pass B per-stage verify-loop + code-review (inline fix per E14) + verified→done flips + inline stage_closeout_apply per C10 + single stage commit per E13 + stage_verification_flip) inline. Wraps ia/skills/ship-stage-main-session. Closeout mandatory on green.
argument-hint: "{MASTER_PLAN_RELATIVE_PATH} {STAGE_ID} [--no-resume]"
---

# /ship-stage-main-session — no-subagent `/ship-stage`

Execute the full `/ship-stage` chain for `$ARGUMENTS` **inline in this session**. Do **not** use the Agent/Task tool for any step.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, chain-level digest JSON, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Argument parsing

Split `$ARGUMENTS` on whitespace:

- `MASTER_PLAN_RELATIVE_PATH` = first token (path to master plan `.md`, relative to repo root, e.g. `ia/projects/grid-asset-master-plan.md`).
- `STAGE_ID` = second token (e.g. `7.2`). Strip `Stage ` prefix → `STAGE_ID_DB` for DB calls.
- Optional flag: `--no-resume` only. Append only if user explicitly asked.

Missing either positional → print usage and abort:

```
/ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID} [--no-resume]
```

Verify `{MASTER_PLAN_RELATIVE_PATH}` exists (Glob). Extract plan display name from filename. Print context banner:

```
SHIP-STAGE (main-session) {STAGE_ID} — {plan display name}
  master plan : {MASTER_PLAN_RELATIVE_PATH}
  stage       : {STAGE_ID}
  flags       : {FLAGS or "(none)"}
  mode        : in-session (no subagents)
```

## Step 1 — Load the wrapper skill

Read `ia/skills/ship-stage-main-session/SKILL.md` end-to-end. Then read the canonical sources it references:

- `ia/skills/ship-stage/SKILL.md` (full 11-phase DB-backed pipeline — Pass A no-commit + Pass B inline closeout per E13/E14/C10)
- `.claude/commands/ship-stage.md` (canonical phase list + exit codes)

## Step 2 — Execute the chain inline

Perform every phase from `ia/skills/ship-stage/SKILL.md` **yourself**, in this session, using territory-ia MCP + bash + direct file edits. Do **not** dispatch any subagent (`ship-stage`, `spec-implementer`, `opus-code-reviewer`, `verify-loop`, `plan-applier`).

Phases (matches `ia/skills/ship-stage-main-session/SKILL.md` frontmatter `phases:`):

1. **Phase 0** — Parse stage (derive `SLUG`, `STAGE_ID_DB`, `SESSION_ID`).
2. **Phase 1** — Stage state load via `stage_bundle(slug, stage_id)` → `master_plan_title`, `stage`, `tasks`, `status_counts`, `next_pending`. Stale-DB → `/stage-file` handoff. Idle exit when stage done + tasks all terminal.
3. **Phase 2** — Context load via `domain-context-load` once; cache `CHAIN_CONTEXT`.
4. **Phase 3** — §Plan Digest readiness gate via `task_spec_section(task_id, "Plan Digest")` per pending task. Missing/empty → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff. **No JIT lazy migration** (pre-DB legacy specs already upgraded).
5. **Phase 4** — Resume gate via `task_state` DB query per pending task. `pending` → Pass A required; `implemented` → skip Pass A. All implemented + stage not done → `PASS_B_ONLY` (worktree dirty required; clean → STOPPED). Disabled by `--no-resume`. **No git scan** (pre-DB `feat(id):`/`fix(id):` regex retired).
6. **Phase 5 — Pass A per-task loop** (sequential, fail-fast, **NO commits**):
   - `spec-implementer` work inline — read `§Plan Digest` via `task_spec_section`, apply edits in declared order, resolve anchors via `plan_digest_resolve_anchor`.
   - `npm run unity:compile-check` (~15 s fast-fail) + scene-wiring preflight when §Plan Digest carries Scene Wiring step.
   - `task_status_flip(task_id, "implemented")` + `journal_append(phase: "pass_a.implemented")`.
   - **NO per-task commits** (E13 — single stage commit at Phase 8).
7. **Phase 6 — Pass B per-stage** (runs ONCE):
   - **6.1 verify-loop** — full Path A + Path B on cumulative `git diff HEAD` (Pass A worktree dirty). `verdict == pass` required; fail → `STAGE_VERIFY_FAIL` + chain digest, no rollback, worktree stays dirty.
   - **6.2 code-review** — opus-code-reviewer work inline on Stage diff with shared `CHAIN_CONTEXT`. **On critical: apply fixes inline via direct Edit/Write per design E14** — do NOT write `§Code Fix Plan` tuples; do NOT dispatch retired plan-applier code-fix mode. Re-entry cap=1; second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`.
   - **6.3 per-task verified→done flips** — for each task in `STAGE_TASK_IDS` (skip if already terminal): `task_status_flip(task_id, "verified")` then `task_status_flip(task_id, "done")` (enum walk requires both).
8. **Phase 7 — Inline closeout (DB + filesystem)** — `stage_closeout_apply(slug, stage_id)` (DB-backed per design C10; replaces retired `stage-closeout-plan` → `stage-closeout-apply` skill pair) + guarded `git mv` of `ia/projects/{SLUG}/stage-{STAGE_ID_DB}-*.md` → `ia/projects/{SLUG}/_closed/` (skip silently if no match — pre-Step-9 foldering).
9. **Phase 8 — Stage commit + verification record** — single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` covers ALL changes (Pass A diffs + code-review fixes + closeout mv per E13). Capture `STAGE_COMMIT_SHA`. Per-task `task_commit_record(task_id, commit_sha=STAGE_COMMIT_SHA, "feat", ...)`. `stage_verification_flip(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage-main-session")` (E11 history-preserving INSERT).
10. **Phase 9** — Chain digest (JSON header `chain_stage_digest: true` + caveman summary + `next_handoff` block).
11. **Phase 10** — Next-stage resolver via `master_plan_state(slug)` — 3 cases priority: filed → `/ship-stage`; pending → `/stage-file`; umbrella-done → `/closeout {UMBRELLA_ISSUE_ID}`. Skeleton stages → `STOPPED — skeleton stage encountered`.

## Hard boundaries (critical)

- **Pass A NEVER commits per design E13.** Single stage-end commit at Phase 8 covers everything.
- **Code-reviewer applies critical fixes inline via direct Edit/Write per design E14** — do NOT write `§Code Fix Plan` tuples; do NOT dispatch retired plan-applier code-fix mode.
- **Inline closeout (Phase 7) mandatory on green Pass B per design C10.** `stage-closeout-plan` + `stage-closeout-apply` skill pair retired (`ia/skills/_retired/`). Never defer to separate `/closeout` invocation.
- Resume gate queries DB (`task_state`) only — no git scan.
- `SHIP_STAGE {STAGE_ID}: PASSED` is **invalid** until Phase 7 closeout + Phase 8 commit + verification flip succeed.
- Append `--no-resume` only on explicit user request.

## Step 3 — Output

Emit exactly one of:

- `SHIP_STAGE {STAGE_ID}: PASSED` — **only** after Phase 7 closeout + Phase 8 stage commit + `stage_verification_flip` succeed. Include `Next:` from Phase 10 resolver.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` — include `Next: /stage-authoring {MASTER_PLAN_RELATIVE_PATH} Stage {STAGE_ID}` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` — include `Next: /stage-file ...` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. ...` — manual-repair directive.
- `STOPPED at {ISSUE_ID} — implement: {reason}` / `compile_gate: {reason}` / `scene_wiring: {reason}` — Pass A failure; include `Next: /ship-stage-main-session ...` resume line after fix.
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — `Human review required — worktree stays dirty; do NOT roll back Pass A status flips automatically.`
- `STAGE_CODE_REVIEW_CRITICAL_TWICE` — human review required.
- `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — DB-drift repair directive.
- `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook (do NOT amend or `--no-verify`).

Followed by pipeline summary block:

```
SHIP-STAGE (main-session) {STAGE_ID}: {PASSED|STOPPED|STAGE_VERIFY_FAIL}
  master plan : {plan display name} ({MASTER_PLAN_RELATIVE_PATH})
  tasks shipped : {count} ({ids})
  stage commit  : {short_sha} (when PASSED)
  stage verify  : {passed|failed|skipped}
```
