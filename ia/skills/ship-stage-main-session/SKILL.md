---
purpose: "Main-session adapter for /ship-stage: executes the full stage-scoped chain (domain-context-load → §Plan Digest gate → resume gate → Pass 1 implement+compile → Pass 2 verify-loop + code-review + audit + closeout) inline (no subagents). Use when the caller agent (Cursor Composer-2 / Claude Code main session) must do the work itself rather than dispatch via Agent/Task tool."
audience: agent
loaded_by: skill:ship-stage-main-session
slices_via: none
name: ship-stage-main-session
description: >
  In-session (no-subagent) wrapper around the /ship-stage chain. Read
  ia/skills/ship-stage/SKILL.md end-to-end and .claude/commands/ship-stage.md
  for the phase list, then execute inline: domain-context-load → Step 1.5
  §Plan Digest readiness gate (JIT plan-digest if Author exists but Digest
  missing; stop if both missing) → Step 1.6 resume gate (git feat/fix)
  unless --no-resume or --per-task-verify → Pass 1 per Task (spec-implementer
  work in-repo → npm run unity:compile-check when Assets/**/*.cs changed →
  one atomic commit per task) → Stage end: verify-loop (Path A+B) →
  opus-code-review → opus-audit → stage-closeout-planner → plan-applier
  Mode stage-closeout. Closeout is MANDATORY on green — do not emit PASSED
  or defer closeout. Use territory-ia MCP and bash per the skill; never
  dispatch via Agent/Task tool.
  Triggers: "/ship-stage-main-session {master-plan-path} {stage}",
  "execute ship-stage in this session", "no-subagent ship-stage".
  Argument order (explicit): MASTER_PLAN_RELATIVE_PATH first, STAGE_ID
  second, optional flags (--no-resume | --per-task-verify) third.
model: inherit
phases:
  - "Load canonical skill + command"
  - "Step 1.5 §Plan Digest readiness gate"
  - "Step 1.6 resume gate"
  - "Pass 1 per-Task implement + compile + commit"
  - "Pass 2 Stage-end verify + review + audit + closeout"
---

# ship-stage-main-session — no-subagent `/ship-stage`

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** same outcomes as `/ship-stage`, executed inline by the current session. No Agent/Task dispatch.

## Arguments

- `$1` / `{MASTER_PLAN_RELATIVE_PATH}` — path to the master plan `.md`, relative to the territory-developer repo root (e.g. `ia/projects/grid-asset-master-plan.md`).
- `$2` / `{STAGE_ID}` — stage identifier (e.g. `7.2`).
- `$3…` / `{FLAGS}` — optional: `--no-resume` and/or `--per-task-verify`. Append only if the user explicitly asked; never on your own initiative.

Missing either positional → print usage + abort: `/ship-stage-main-session {MASTER_PLAN_RELATIVE_PATH} {STAGE_ID} [--no-resume | --per-task-verify]`.

## Instructions

1. **Load canonical sources end-to-end:**
   - `ia/skills/ship-stage/SKILL.md` (full phase policy — Step 1.5, Step 1.6, Pass 1, Pass 2, closeout contract)
   - `.claude/commands/ship-stage.md` (canonical phase list + exit codes)

2. **Execute the full chain inline** for `{MASTER_PLAN_RELATIVE_PATH}` Stage `{STAGE_ID}`:

   - **Phase 0** — Parse stage task table (narrow regex; fail loud on schema mismatch).
   - **Phase 1** — `domain-context-load` **once** per chain.
   - **Phase 1.5** — §Plan Digest readiness gate on every non-Done Task:
     - `§Plan Digest` populated → proceed.
     - `§Plan Digest` missing but `§Plan Author` populated → JIT `plan-digest` (lazy migration) inline + one-time session warning; resume Pass 1 after.
     - Both missing → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/plan-digest` handoff. No Pass 1.
   - **Phase 1.6** — Resume gate: `git log --first-parent -400` for `feat(TECH-xxx):` / `fix(TECH-xxx):` per pending Task; skip Pass 1 for satisfied ids; all satisfied → skip Pass 1 → Pass 2 only. **Disabled** by `--no-resume` or `--per-task-verify`.
   - **Phase 2 — Pass 1 per-Task loop** (for each pending, non-resumed Task):
     1. `spec-implementer` work inline — read `§Plan Digest` edits, apply in declared order, resolve anchors via `plan_digest_resolve_anchor`.
     2. `npm run unity:compile-check` **when `Assets/**/*.cs` changed**.
     3. One atomic commit per task (`feat(TECH-xxx): …` or `fix(TECH-xxx): …`).
     4. If `--per-task-verify`: also run per-Task verify-loop + code-review in Pass 1.
   - **Phase 3 — Pass 2 Stage-end** (once, after all Pass 1 tasks):
     1. Full `verify-loop` (Path A + Path B per policy) on cumulative delta.
     2. Stage-level `opus-code-review`.
     3. `opus-audit`.
     4. **Closeout (mandatory on green):** `stage-closeout-planner` → `plan-applier` Mode `stage-closeout`.
     5. **Do NOT emit `PASSED` or defer closeout** after successful verify + review + audit. Closeout is part of `PASSED` (see `ia/skills/ship-stage/SKILL.md` — **Normative — closeout is part of `PASSED`**).
   - **Phase 4** — Chain-level stage digest (JSON header + caveman summary + `chain:` block).
   - **Phase 5** — Next-stage resolver (4 cases: filed / pending / skeleton / umbrella-done).

3. **Tooling:**
   - territory-ia MCP: `backlog_issue`, `router`, `spec_section`, `plan_digest_resolve_anchor`, etc.
   - `bash` / repo scripts per the skill (`npm run unity:compile-check`, `npm run validate:*`, `tools/scripts/materialize-backlog.sh`, etc.).
   - Direct file edits (Unity sources under `Assets/`, spec closeout updates, master-plan status flips).

4. **Hard boundaries:**
   - **Never** dispatch via Agent/Task tool for any step in this chain (no `ship-stage` subagent, no `spec-implementer` subagent, no `opus-code-review` subagent, no `opus-audit` subagent, no `stage-closeout-planner` subagent, no `plan-applier` subagent).
   - Closeout is mandatory on green — never defer.
   - Append `--no-resume` / `--per-task-verify` only on explicit user request.

## Exit

End with one of:

- `SHIP_STAGE {STAGE_ID}: PASSED` — **only** after closeout + validators succeed. Include `Next:` from Phase 5 resolver.
- `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — {gate}: {reason}` — include `Next: /ship {FAILED_ISSUE_ID}` after fix.
- `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for …` — include `/plan-digest` next line.
- `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` — `Human review required — do NOT resume tasks automatically.`
- `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch`.
