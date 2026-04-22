---
description: No-subagent variant of /ship-stage. Executes the full stage-scoped chain (domain-context-load → §Plan Digest gate → resume gate → Pass 1 implement+compile+commit → Pass 2 verify-loop + code-review + audit + closeout) inline in the current Claude Code session (no Agent/Task dispatch). Wraps ia/skills/ship-stage-main-session. Closeout is mandatory on green.
argument-hint: "{MASTER_PLAN_RELATIVE_PATH} {STAGE_ID} [--per-task-verify] [--no-resume]"
---

# /ship-stage-main-session — no-subagent `/ship-stage`

Execute the full `/ship-stage` chain for `$ARGUMENTS` **inline in this session**. Do **not** use the Agent/Task tool for any step.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, chain-level digest JSON, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Argument parsing

Split `$ARGUMENTS` on whitespace:

- `MASTER_PLAN_RELATIVE_PATH` = first token (path to master plan `.md`, relative to repo root, e.g. `ia/projects/grid-asset-master-plan.md`).
- `STAGE_ID` = second token (e.g. `7.2`).
- Optional flags: `--no-resume`, `--per-task-verify`. Append only if the user explicitly asked.

Missing either positional → print usage and abort:

```
/ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID} [--no-resume | --per-task-verify]
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

- `ia/skills/ship-stage/SKILL.md` (full phase policy — Step 1.5, Step 1.6, Pass 1, Pass 2, closeout contract)
- `.claude/commands/ship-stage.md` (canonical phase list + exit codes)

## Step 2 — Execute the chain inline

Perform every phase from `.claude/commands/ship-stage.md` **yourself**, in this session, using territory-ia MCP + bash + direct file edits. Do **not** dispatch any subagent (`ship-stage`, `spec-implementer`, `opus-code-review`, `opus-audit`, `stage-closeout-planner`, `plan-applier`, `plan-digest`).

Phases:

1. **Phase 0** — Parse stage task table (narrow regex; fail loud on schema mismatch).
2. **Phase 1** — `domain-context-load` once per chain.
3. **Phase 1.5** — §Plan Digest readiness gate on every non-Done Task:
   - Populated → proceed.
   - Missing but `§Plan Author` populated → JIT `plan-digest` inline (lazy migration) + one-time session warning; resume Pass 1 after.
   - Both missing → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/plan-digest` handoff. No Pass 1.
4. **Phase 1.6** — Resume gate: `git log --first-parent -400` for `feat(TECH-xxx):` / `fix(TECH-xxx):` per pending Task; skip Pass 1 for satisfied ids; all satisfied → skip Pass 1 → Pass 2 only. Disabled by `--no-resume` or `--per-task-verify`.
5. **Phase 2 — Pass 1 per-Task loop** (for each pending, non-resumed Task):
   - `spec-implementer` work inline (read `§Plan Digest`, apply edits in declared order, resolve anchors).
   - `npm run unity:compile-check` **when `Assets/**/*.cs` changed**.
   - One atomic commit per task.
   - If `--per-task-verify`: also run per-Task verify-loop + code-review in Pass 1.
6. **Phase 3 — Pass 2 Stage-end** (once, after all Pass 1 tasks):
   - Full `verify-loop` (Path A + Path B per policy) on cumulative delta.
   - Stage-level `opus-code-review`.
   - `opus-audit`.
   - **Closeout (mandatory on green):** `stage-closeout-planner` → `plan-applier` Mode `stage-closeout`.
   - **Do NOT emit `PASSED` or defer closeout** after successful verify + review + audit. Closeout is part of `PASSED`.
7. **Phase 4** — Chain-level stage digest (JSON header + caveman summary + `chain:` block).
8. **Phase 5** — Next-stage resolver (4 cases: filed / pending / skeleton / umbrella-done).

## Step 3 — Output

Emit exactly one of:

- `SHIP_STAGE {STAGE_ID}: PASSED` — **only** after closeout + validators succeed. Include `Next:` from Phase 5 resolver.
- `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — {gate}: {reason}` — include `Next: /ship {FAILED_ISSUE_ID}` after fix.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for …` — include `/plan-digest` next line.
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — `Human review required — do NOT resume tasks automatically.`
- `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch`.

Followed by pipeline summary block:

```
SHIP-STAGE (main-session) {STAGE_ID}: {PASSED|STOPPED|STAGE_VERIFY_FAIL}
  master plan : {plan display name} ({MASTER_PLAN_RELATIVE_PATH})
  tasks shipped : {count} ({ids})
  stage verify  : {passed|failed|skipped}
```
