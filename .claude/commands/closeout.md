---
description: Close a Stage end-to-end — Stage-scoped bulk closeout (NOT per-Task). Dispatches `stage-closeout-planner` Opus pair-head then `plan-applier` Sonnet pair-tail Mode stage-closeout. Fires once per Stage when all Task rows reach Done post-verify.
argument-hint: "{MASTER_PLAN_PATH} {STAGE_ID} (e.g. ia/projects/lifecycle-refactor-master-plan.md 7.2)"
---

# /closeout — dispatch Stage-scoped closeout pair (seam #4)

Use `stage-closeout-planner` subagent (`.claude/agents/stage-closeout-planner.md`) → **`plan-applier`** subagent (`.claude/agents/plan-applier.md`, Mode stage-closeout) for bulk closeout on `$ARGUMENTS`. All ops run without human confirmation. Replaces retired per-Task `/closeout {ISSUE_ID}` flow (T7.14 / TECH-481 — lifecycle-refactor seam #4 collapse).

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{MASTER_PLAN_PATH}` (repo-relative, `ia/projects/*-master-plan.md`). Second token = `{STAGE_ID}` (e.g. `7.2` or `Stage 7.2`). Missing either → print usage + abort.

Any other flag (e.g. legacy `--refactor`) → reject with message: `/closeout is Stage-scoped post-T7.14. Legacy per-Task flag {flag} not supported — use Stage-scoped invocation.`

## Step 0 — Pre-dispatch banner

Resolve and print for the human developer:

1. Read `MASTER_PLAN_PATH` Stage `{STAGE_ID}` block. Extract Stage Title + list of Task rows with Status = `Done`.
2. Verify every Task row has Status = `Done`. Any row non-`Done` → abort before planner dispatch with: `Stage {STAGE_ID} not ready: {N_not_done} task(s) non-Done. Run /ship-stage or /verify-loop first.`
3. Print:
   ```
   CLOSEOUT Stage {STAGE_ID} — {Stage Title}
     master plan   : {Plan Name} ({MASTER_PLAN_PATH})
     tasks to close: {N} ({comma-separated ISSUE_IDs})
     seam          : #4 (stage-closeout-plan → plan-applier Mode stage-closeout)
   ```

## Step 1 — Dispatch `stage-closeout-planner` (Opus pair-head)

Forward to planner subagent via Agent tool with `subagent_type: "stage-closeout-planner"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `stage-closeout-plan` skill (`ia/skills/stage-closeout-plan/SKILL.md`) end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Read master-plan Stage block + all Task §Audit / §Implementation / §Findings / §Verification / §Lessons Learned + invariants + glossary. Write unified `§Stage Closeout Plan` tuple list (shared migration ops deduped + N per-Task archive / delete / status-flip / id-purge / digest_emit ops). Hand off to **`plan-applier`** Sonnet pair-tail Mode stage-closeout. Does NOT mutate target files — plan only.
>
> ## Hard boundaries
>
> - Do NOT edit spec files, archive yaml, delete specs, flip status, regenerate BACKLOG, or run validators.
> - Do NOT re-order / merge / interpret tuples — applier reads verbatim.
> - Do NOT write `§Stage Closeout Plan` if any Task row Status ≠ `Done`.
> - Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT commit — user decides.

Planner must return success + `§Stage Closeout Plan` written before Step 2. Escalation shape → abort chain, surface to user.

## Step 2 — Dispatch `plan-applier` (Sonnet pair-tail, Mode stage-closeout)

Forward to applier subagent via Agent tool with `subagent_type: "plan-applier"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-applier/SKILL.md` — **Mode: stage-closeout** on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Read `§Stage Closeout Plan` tuples verbatim; apply shared migration ops once + per-Task ops in loop (archive yaml + delete spec + flip task-row Status + id purge + digest_emit); run `materialize-backlog.sh` + `npm run validate:all` once at end; aggregate N per-Task digests into one Stage-level digest emitted to stdout; flip Stage header Status → Final + roll up to Step / Plan-level Final per R5. Idempotent on re-run.
>
> ## Hard boundaries
>
> - Do NOT re-query MCP for anchor resolution — planner resolved every anchor.
> - Do NOT re-order tuples — apply in declared order (shared first, then per-Task grouped).
> - Do NOT write normative prose — only mutations from tuple payloads.
> - Do NOT edit `BACKLOG.md` / `BACKLOG-ARCHIVE.md` directly — `materialize-backlog.sh` regenerates both.
> - Do NOT touch `ia/backlog-archive/*`, `ia/state/pre-refactor-snapshot/*`, `ia/specs/*` for `id_purge` — historical surfaces read-only.
> - Do NOT flip Stage Status → Final if any Task row non-`Done (archived)` post-loop.
> - On `validate:all` non-zero exit: print full stdout/stderr before diagnosing. Never attribute failure to guessed id.
> - Do NOT `git commit` — commit is user-gated.

## Output

Chain emits single closeout digest per applier Phase 5c: caveman hand-off block + JSON Stage-level digest. Legacy per-Task closeout digest output style (`.claude/output-styles/closeout-digest.md`) is retired for Stage-scoped `/closeout` — per-Task digest retained only as MCP tool response consumed by applier internally.
