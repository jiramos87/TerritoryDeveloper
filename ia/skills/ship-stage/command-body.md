Two-pass DB-backed chain over every non-terminal task of `$ARGUMENTS`. **Pass A** = per-task implement → `unity:compile-check` → `task_status_flip(implemented)`; **NO per-task commits** (single stage-end commit covers everything). **Pass B** = per-stage verify-loop on cumulative `git diff HEAD` → code-review (inline fix cap=1) → per-task `verified→done` flips → inline `stage_closeout_apply` → single stage commit `feat({SLUG}-stage-{STAGE_ID_DB})` → per-task `task_commit_record` → `stage_verification_flip(pass)`. Resume gate via `task_state` DB query (no git scan).

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
> 3. Phase 2 — Context load via `domain-context-load` (once per chain); cache `CHAIN_CONTEXT`.
> 4. Phase 3 — §Plan Digest readiness gate via `task_spec_section(task_id, "Plan Digest")` per pending task. Missing → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/stage-authoring` handoff. No JIT lazy migration.
> 5. Phase 4 — Resume gate via `task_state` DB query. `pending` → Pass A required; `implemented` → skip Pass A. All implemented + stage not done → `PASS_B_ONLY` (worktree-clean guard). Disabled by `--no-resume`.
> 6. Phase 5 — Pass A per-task loop: implement (`spec-implementer` work inline) → `unity:compile-check` + scene-wiring preflight → `task_status_flip(implemented)` + `journal_append`. **NO commits.** Stop on first failure.
> 7. Phase 6 — Pass B per-stage (runs ONCE): full `verify-loop` (Path A+B) on `git diff HEAD` → code-review on Stage diff (inline fix; cap=1) → per-task `task_status_flip(verified)` then `task_status_flip(done)`.
> 8. Phase 7 — Inline closeout: `stage_closeout_apply(slug, stage_id)` (DB-backed atomic) + `master_plan_change_log_append` audit row. DB-only — no filesystem mv.
> 9. Phase 8 — Stage commit + verification record: single commit `feat({SLUG}-stage-{STAGE_ID_DB}): ...` (covers all Pass A + code-review fixes) → capture `STAGE_COMMIT_SHA` → per-task `task_commit_record(task_id, commit_sha, "feat", ...)` → `stage_verification_flip(verdict="pass", commit_sha=STAGE_COMMIT_SHA, actor="ship-stage")`.
> 10. Phase 9 — Chain digest (JSON header `chain_stage_digest: true` + caveman summary + `next_handoff` block).
> 11. Phase 10 — Next-stage resolver via `master_plan_state(slug)` — 3 cases priority: filed → `/ship-stage`; pending → `/stage-file`; umbrella-done → `/closeout {UMBRELLA_ISSUE_ID}`. Skeleton stages → `STOPPED — skeleton stage encountered`.
>
> ## Hard boundaries (critical)
>
> - **Pass A NEVER commits.** Single stage-end commit at Step 8.1 covers everything.
> - **Code-reviewer applies critical fixes inline via direct Edit/Write** — do NOT write `§Code Fix Plan` tuples.
> - **Inline closeout (Step 7) mandatory on green Pass B.**
> - Resume gate queries DB (`task_state`), not git log.
> - DB is sole source of truth — no `ia/projects/**` reads or writes.
> - `SHIP_STAGE {STAGE_ID}: PASSED` is **invalid** until Step 7 closeout + Step 8 commit + verification flip succeed.
>
> ## Exit
>
> End with one of:
> - `SHIP_STAGE {STAGE_ID}: PASSED` (only after Step 7 closeout + Step 8 stage commit + `stage_verification_flip` succeed)
> - `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for ...` (+ `/stage-authoring` Next line)
> - `SHIP_STAGE {STAGE_ID}: STOPPED — stage not found in DB` (+ `/stage-file` Next line)
> - `SHIP_STAGE {STAGE_ID}: STOPPED — PASS_B_ONLY but worktree clean. ...` (manual-repair directive)
> - `STOPPED at {ISSUE_ID} — {gate}: {reason}` (Pass A failure; partial chain digest)
> - `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL`
> - `STAGE_CODE_REVIEW_CRITICAL_TWICE`
> - `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — non-terminal tasks present: {ids}`
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
