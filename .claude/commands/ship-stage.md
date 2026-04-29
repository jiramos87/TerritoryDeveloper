---
description: Opus orchestrator. Drives every non-terminal task of one Stage X.Y through a two-pass DB-backed chain. Pass A (per-task): implement + unity:compile-check fast-fail gate + task_status_flip(implemented). NO per-task commits — Pass A leaves a dirty worktree. Pass B (per-stage): verify-loop on cumulative HEAD diff + per-task task_status_flip(verified→done) + stage_closeout_apply + master_plan_change_log_append (audit row) + single stage commit feat({slug}-stage-X.Y) + per-task task_commit_record + stage_verification_flip(pass, commit_sha). Code-review intentionally NOT part of this chain — verify-loop + validation are the gate; standalone /code-review remains available out-of-band. Resume gate queries task_state per pending task; status='implemented' skips Pass A. PASS_B_ONLY when all tasks implemented but stage not done. On resume with new diff a fresh stage commit is created. Idle exit when all tasks done/archived AND ia_stages.status=done. Triggers: "/ship-stage", "ship stage", "chain stage tasks".
argument-hint: "{SLUG} {STAGE_ID} [--no-resume] [--force-model {model}]"
---

# /ship-stage — Two-pass DB-backed Stage chain. Pass A = per-task implement + unity:compile-check fast-fail gate + task_status_flip(implemented). NO per-task commits. Pass B = per-stage verify-loop + per-task task_status_flip(verified→done) + stage_closeout_apply + single stage-end commit + per-task task_commit_record + stage_verification_flip. Resume gate via task_state status query (no git scan).

Drive `$ARGUMENTS` via the [`ship-stage`](../agents/ship-stage.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, chain-level digest JSON, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /ship-stage
- ship stage
- chain stage tasks
<!-- skill-tools:body-override -->

Two-pass DB-backed chain over every non-terminal task of `$ARGUMENTS`. **Pass A** = per-task implement → `unity:compile-check` → `task_status_flip(implemented)`; **NO per-task commits** (single stage-end commit covers everything). **Pass B** = per-stage verify-loop on cumulative `git diff HEAD` → per-task `verified→done` flips → inline `stage_closeout_apply` → single stage commit `feat({SLUG}-stage-{STAGE_ID_DB})` (or reused sha on empty resume diff) → per-task `task_commit_record` → `stage_verification_flip(pass)`. No code-review in chain (operator may run standalone `/code-review {ISSUE_ID}` out-of-band). Resume gate via `task_state` DB query (no git scan).

Prerequisite: `/stage-authoring` already populated `§Plan Digest` in DB. Missing → readiness gate STOPPED + `/stage-authoring` handoff.

Follow `caveman:caveman` for all your own output and all dispatched subagents. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Context resolution (before dispatch)

Parse `$ARGUMENTS` as `{SLUG} {STAGE_ID}`:

- `SLUG` = first token (e.g. `blip`, `citystats-overhaul`).
- `STAGE_ID` = remainder (excluding flags).
- `--no-resume` flag disables Step 4 resume gate (forensic replay only).
- If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset (subagent uses frontmatter model).

Verify slug exists via `master_plan_state(slug=SLUG)`. Missing → STOPPED + `Next: claude-personal "/master-plan-new ..."` handoff. Capture `master_plan_title` from MCP result. Print context banner:

```
SHIP-STAGE {STAGE_ID} — {master_plan_title}
  slug   : {SLUG}
  stage  : {STAGE_ID}
```

---

## Stage 1 — Chain dispatch (`ship-stage`)

Dispatch Agent with `subagent_type: "ship-stage"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> ## Mission
>
> Run `ia/skills/ship-stage/SKILL.md` end-to-end on slug `{SLUG}` Stage `{STAGE_ID}` (with `--no-resume` if present).
>
> Follow caveman:caveman. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, chain-level digest JSON, destructive-op confirmations.
>
> ## Phase sequence (matches SKILL frontmatter `phases:`)
>
> 1. Phase 0 — Parse stage (derive `SLUG`, `STAGE_ID_DB`, `SESSION_ID`).
> 2. Phase 1 — Stage state load via `stage_bundle(slug, stage_id)`. Stale-DB → `/stage-file` handoff. Idle exit when stage done + tasks all terminal.
> 3. Phase 1.5 — Baseline worktree snapshot: `git status --porcelain` → `BASELINE_DIRTY` set of `{XY}{path}` tuples. Chain-scope guard for Phase 8 commit (read-only after capture). Prevents sweeping pre-existing dirty paths (sibling work streams, in-flight refactors, untracked artifacts) into the stage commit.
> 4. Phase 2 — Context load via `domain-context-load` (once per chain); cache `CHAIN_CONTEXT`.
> 5. Phase 3 — §Plan Digest readiness gate via `task_spec_section(task_id, "§Plan Digest")` per pending task (literal `§` prefix; bare `"Plan Digest"` returns `section_not_found`). Missing → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff. No JIT lazy migration.
> 6. Phase 4 — Resume gate via `task_state` DB query. `pending` → Pass A required; `implemented` → skip Pass A. All implemented + stage not done → `PASS_B_ONLY` (chain-scope delta vs `BASELINE_DIRTY` must be non-empty; empty → STOPPED with manual-repair directive). Disabled by `--no-resume`.
> 7. Phase 5 — Pass A per-task loop: implement (`spec-implementer` work inline) → `unity:compile-check` + scene-wiring preflight → `task_status_flip(implemented)` + `journal_append`. **NO commits.** Stop on first failure.
> 8. Phase 6 — Pass B per-stage (runs ONCE): full `verify-loop` (Path A+B) on `git diff HEAD` → per-task `task_status_flip(verified)` then `task_status_flip(done)`. No code-review — standalone `/code-review {ISSUE_ID}` available out-of-band (lifecycle row 9).
> 9. Phase 7 — Inline closeout: `stage_closeout_apply(slug, stage_id)` (DB-backed atomic) + `master_plan_change_log_append` audit row. DB-only — no filesystem mv.
> 10. Phase 8 — Stage commit (chain-scope delta only) + verification record: compute `STAGE_TOUCHED_PATHS = CURRENT_DIRTY - BASELINE_DIRTY` (chain-scope only). Single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` stages **only** `STAGE_TOUCHED_PATHS` via `git add -- <paths>` (NEVER `git add -A` / `git add .`). Verify staged scope via `git diff --cached --name-only` — drift → STOPPED contamination guard. Capture `STAGE_COMMIT_SHA`. Resume note: if `STAGE_TOUCHED_PATHS` empty (PASS_B_ONLY re-run after prior commit), skip commit + reuse `git rev-parse HEAD` as `STAGE_COMMIT_SHA`. Per-task `task_commit_record(task_id, commit_sha, "feat", ...)` → `stage_verification_flip(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage")`.
> 11. Phase 9 — Chain digest (JSON header `chain_stage_digest: true` + caveman summary + `next_handoff` block).
> 12. Phase 10 — Next-stage resolver via `master_plan_state(slug)`. Sort stages by numeric tuple `(major, minor)` from `stage_id`; iterate forward from first stage > current `STAGE_ID_DB`. Pick **first** stage by numeric order matching one of 4 cases — do NOT skip stages to grab a later one: filed → `/ship-stage`; pending (`_pending_` Issue ids) → `/stage-file`; skeleton (no tasks) → `/stage-decompose`; umbrella-done → no further command (plan complete; inline `stage_closeout_apply` already recorded per-stage). `stage-decompose` is part of the standard incremental flow.
>
> ## Hard boundaries (critical)
>
> - **Pass A NEVER commits.** Single stage-end commit at Step 8.1 covers everything.
> - **Stage commit at Step 8.1 stages ONLY chain-scope paths** (delta vs `BASELINE_DIRTY` snapshot from Phase 1.5). NEVER `git add -A` / `git add .` / blanket-stage. Pre-existing dirty files (sibling work streams, in-flight refactors, untracked artifacts) stay in worktree, untouched by stage commit.
> - **No code-review in this chain.** Operator may run standalone `/code-review {ISSUE_ID}` per Task out-of-band (lifecycle row 9).
> - **Inline closeout (Step 7) mandatory on green Pass B.**
> - Resume gate queries DB (`task_state`), not git log.
> - DB is sole source of truth — no `ia/projects/**` reads or writes.
> - `SHIP_STAGE {STAGE_ID}: PASSED` is **invalid** until Step 7 closeout + Step 8 commit (or empty-diff resume reuse) + verification flip succeed.
>
> ## Exit
>
> End with one of:
> - `SHIP_STAGE {STAGE_ID}: PASSED` (only after Step 7 closeout + Step 8 stage commit + `stage_verification_flip` succeed)
> - `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for ...` (+ `/stage-authoring` Next line)
> - `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` (+ `/stage-file` Next line)
> - `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean (no chain-scope changes vs baseline). ...` (manual-repair directive)
> - `STOPPED at {ISSUE_ID} — {gate}: {reason}` (Pass A failure; partial chain digest)
> - `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL`
> - `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}`
> - `SHIP_STAGE {STAGE_ID}: STOPPED at commit — staged scope drift: expected {N}, got {M}. Refusing contamination.`
> - `SHIP_STAGE {STAGE_ID}: STOPPED at commit — pre-commit hook failed: {reason}`

---

## Pipeline summary output

After dispatch completes (or on stop), emit:

```
SHIP-STAGE {STAGE_ID}: {PASSED|STOPPED|STAGE_VERIFY_FAIL}
  slug          : {SLUG} ({master_plan_title})
  tasks shipped : {count} ({ids})
  stage commit  : {short_sha} (when PASSED)
  stage verify  : {passed|failed|skipped}
```

On `PASSED`: include `Next:` handoff from Step 10 resolver.
On `STOPPED`: include `Next: claude-personal "/ship-stage {SLUG} {STAGE_ID}"` (resume gate picks up after fix).
On `STAGE_VERIFY_FAIL`: include `Human review required — worktree stays dirty; do NOT roll back Pass A status flips automatically.`
