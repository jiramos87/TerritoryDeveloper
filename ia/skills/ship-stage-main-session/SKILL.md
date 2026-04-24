---
purpose: "Main-session adapter for /ship-stage: executes the full DB-backed two-pass chain (stage_bundle → §Plan Digest gate → resume gate via task_state → Pass A per-task implement+compile+task_status_flip(implemented) NO COMMITS → Pass B per-stage verify-loop + code-review (inline fix per E14) + verified→done flips + inline stage_closeout_apply per C10 + single stage commit per E13 + stage_verification_flip) inline (no subagents). Use when caller agent (Cursor Composer-2 / Claude Code main session) must do the work itself rather than dispatch via Agent/Task tool."
audience: agent
loaded_by: skill:ship-stage-main-session
slices_via: stage_bundle, task_state, task_spec_section, glossary_lookup, invariants_summary
name: ship-stage-main-session
description: >
  In-session (no-subagent) wrapper around the /ship-stage chain. Read
  ia/skills/ship-stage/SKILL.md end-to-end and .claude/commands/ship-stage.md
  for the canonical 11-phase pipeline, then execute inline:
  stage_bundle load → domain-context-load → §Plan Digest readiness gate
  (task_spec_section per task; missing → /stage-authoring handoff) →
  resume gate via task_state DB query (no git scan) → Pass A per task
  (spec-implementer work in-repo → npm run unity:compile-check + scene-wiring
  preflight → task_status_flip(implemented); NO commits — single stage
  commit at Phase 8 per design E13) → Pass B per stage (verify-loop on
  git diff HEAD → code-review with inline fix per E14 cap=1 →
  per-task task_status_flip(verified) then task_status_flip(done)) →
  inline stage_closeout_apply per C10 + guarded git mv → single stage
  commit feat({SLUG}-stage-{STAGE_ID_DB}) → per-task task_commit_record →
  stage_verification_flip(pass, commit_sha). Closeout is MANDATORY on
  green — do not emit PASSED or defer closeout. Use territory-ia MCP and
  bash per the skill; never dispatch via Agent/Task tool.
  Triggers: "/ship-stage-main-session {master-plan-path} {stage}",
  "execute ship-stage in this session", "no-subagent ship-stage".
  Argument order (explicit): MASTER_PLAN_RELATIVE_PATH first, STAGE_ID
  second, optional flag --no-resume third.
model: inherit
phases:
  - "Load canonical skill + command"
  - "Stage state load + context load"
  - "§Plan Digest readiness gate"
  - "Resume gate (DB task_state)"
  - "Pass A per-task (implement + compile + task_status_flip; NO commits)"
  - "Pass B per-stage (verify + code-review + verified→done flips)"
  - "Inline closeout (stage_closeout_apply + guarded git mv)"
  - "Stage commit + per-task commit record + stage_verification_flip"
---

# ship-stage-main-session — no-subagent `/ship-stage`

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** same outcomes as `/ship-stage`, executed inline by the current session. No Agent/Task dispatch.

## Arguments

- `$1` / `{MASTER_PLAN_RELATIVE_PATH}` — path to the master plan `.md`, relative to repo root (e.g. `ia/projects/grid-asset-master-plan.md`).
- `$2` / `{STAGE_ID}` — stage identifier (e.g. `7.2` or `Stage 7.2`). Strip `Stage ` prefix → `STAGE_ID_DB` for DB calls.
- `$3` / `{FLAGS}` — optional `--no-resume` only. Append only if user explicitly asked.

Missing either positional → print usage + abort: `/ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID} [--no-resume]`.

## Instructions

1. **Load canonical sources end-to-end:**
   - `ia/skills/ship-stage/SKILL.md` (full 11-phase DB-backed pipeline — Pass A no-commit + Pass B inline closeout per E13/E14/C10)
   - `.claude/commands/ship-stage.md` (canonical phase list + exit codes)

2. **Execute the full chain inline** for `{MASTER_PLAN_RELATIVE_PATH}` Stage `{STAGE_ID}` per `ia/skills/ship-stage/SKILL.md` Steps 0–10:

   - **Phase 0 — Parse stage** — derive `SLUG`, `STAGE_ID_DB`, `SESSION_ID`.
   - **Phase 1 — Stage state load** — `stage_bundle(slug, stage_id)` → `master_plan_title`, `stage`, `tasks`, `status_counts`, `next_pending`. Stale-DB → `/stage-file` handoff. Idle exit when stage done + tasks all terminal.
   - **Phase 2 — Context load** — `domain-context-load` once; cache `CHAIN_CONTEXT`.
   - **Phase 3 — §Plan Digest readiness gate** — `task_spec_section(task_id, "Plan Digest")` per pending task. Missing/empty → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff. **No JIT lazy migration** (pre-DB legacy specs already upgraded).
   - **Phase 4 — Resume gate** — `task_state` DB query per pending task. `pending` → Pass A required; `implemented` → skip Pass A. All implemented + stage not done → `PASS_B_ONLY` (verify worktree dirty; clean → STOPPED). Disabled by `--no-resume`. **No git scan** (pre-DB `feat(id):`/`fix(id):` regex retired).
   - **Phase 5 — Pass A per-task loop** (sequential, fail-fast, NO commits):
     1. spec-implementer work inline — read `§Plan Digest` via `task_spec_section`, apply edits in declared order, resolve anchors via `plan_digest_resolve_anchor`.
     2. `npm run unity:compile-check` (~15 s fast-fail) + scene-wiring preflight when §Plan Digest carries Scene Wiring step (verify worktree diff includes `Assets/Scenes/*.unity` edit).
     3. `task_status_flip(task_id, "implemented")` + `journal_append(phase: "pass_a.implemented")`.
     4. **NO per-task commits** (E13 — single stage commit at Phase 8).
   - **Phase 6 — Pass B per-stage** (runs ONCE):
     1. **6.1 verify-loop** — full Path A+B on cumulative `git diff HEAD` (Pass A worktree dirty). `verdict == pass` required; fail → `STAGE_VERIFY_FAIL` + chain digest, no rollback, worktree stays dirty.
     2. **6.2 code-review** — opus-code-reviewer on Stage diff with shared `CHAIN_CONTEXT`. **On critical: apply fixes inline via direct Edit/Write per design E14** — do NOT write `§Code Fix Plan` tuples; do NOT dispatch retired plan-applier code-fix mode. Re-entry cap=1; second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`.
     3. **6.3 per-task verified→done flips** — for each task in `STAGE_TASK_IDS` (skip if already terminal): `task_status_flip(task_id, "verified")` then `task_status_flip(task_id, "done")` (enum walk requires both).
   - **Phase 7 — Inline closeout (DB + filesystem)** — `stage_closeout_apply(slug, stage_id)` (DB-backed per design C10; replaces retired `stage-closeout-plan` → `stage-closeout-apply` skill pair) + guarded `git mv` of `ia/projects/{SLUG}/stage-{STAGE_ID_DB}-*.md` → `ia/projects/{SLUG}/_closed/` (skip silently if no match — pre-Step-9 foldering).
   - **Phase 8 — Stage commit + verification record** — single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` covers ALL changes (Pass A diffs + code-review fixes + closeout mv per E13). Capture `STAGE_COMMIT_SHA`. Per-task `task_commit_record(task_id, commit_sha=STAGE_COMMIT_SHA, "feat", ...)`. `stage_verification_flip(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage-main-session")` (E11 history-preserving INSERT).
   - **Phase 9 — Chain digest** — JSON header `chain_stage_digest: true` + caveman summary + `next_handoff` block.
   - **Phase 10 — Next-stage resolver** — `master_plan_state(slug)`; 3 cases priority: filed → `/ship-stage`; pending → `/stage-file`; umbrella-done → `/closeout {UMBRELLA_ISSUE_ID}`. Skeleton stages (no tasks) → `STOPPED — skeleton stage encountered`.

3. **Tooling:**
   - territory-ia MCP: `stage_bundle`, `task_state`, `task_bundle`, `task_spec_section`, `task_status_flip`, `stage_closeout_apply`, `task_commit_record`, `stage_verification_flip`, `journal_append`, `master_plan_state`, `glossary_lookup`, `invariants_summary`, `plan_digest_resolve_anchor` (Pass A spec-implementer work), `backlog_issue`, `router_for_task`.
   - `bash` / repo scripts per the skill (`npm run unity:compile-check`, `npm run validate:*`).
   - Direct file edits (Unity sources under `Assets/`, IA edits, spec changes); `git add -A` + `git commit` only at Phase 8.

4. **Hard boundaries:**
   - **Never** dispatch via Agent/Task tool for any step in this chain (no `ship-stage` subagent, no `spec-implementer` subagent, no `opus-code-reviewer` subagent, no `verify-loop` subagent).
   - **Pass A NEVER commits per design E13.** Single stage commit at Phase 8 covers everything.
   - **Code-reviewer applies critical fixes inline per design E14** — no `§Code Fix Plan` tuples.
   - **Inline closeout (Phase 7) is mandatory on green Pass B per design C10.** Never defer to separate `/closeout` invocation. `stage-closeout-plan` + `stage-closeout-apply` skill pair retired.
   - Resume gate queries DB only — no git scan.
   - Append `--no-resume` only on explicit user request.

## Exit

End with one of:

- `SHIP_STAGE {STAGE_ID}: PASSED` — **only** after Phase 7 closeout + Phase 8 stage commit + `stage_verification_flip` succeed. Include `Next:` from Phase 10 resolver.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` — include `Next: /stage-authoring {MASTER_PLAN_RELATIVE_PATH} Stage {STAGE_ID}` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` — include `Next: /stage-file ...` line.
- `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. ...` — manual-repair directive.
- `STOPPED at {ISSUE_ID} — implement: {reason}` / `compile_gate: {reason}` / `scene_wiring: {reason}` — Pass A failure; include `Next: /ship-stage-main-session ...` resume line after fix.
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — `Human review required — worktree stays dirty; do NOT roll back Pass A status flips automatically.`
- `STAGE_CODE_REVIEW_CRITICAL_TWICE` — human review required.
- `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}` — DB-drift repair directive.
- `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}` — investigate hook (do NOT amend or `--no-verify`).
