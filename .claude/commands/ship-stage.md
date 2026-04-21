---
description: Stage-scoped chain shipper — Pass 1 implement/compile + Pass 2 verify-loop + code-review + audit + closeout. Gates on §Plan Author readiness (specs must arrive pre-authored + pre-reviewed from `/stage-file` chain). Args: {MASTER_PLAN_PATH} {STAGE_ID}.
argument-hint: "{MASTER_PLAN_PATH} {STAGE_ID} [--per-task-verify] [--no-resume] [--force-model {model}]"
---

# /ship-stage — stage-scoped chain dispatcher

Chain **per-Task implement+compile → Pass 2 verify-loop → code-review → audit → closeout** across every non-Done filed task row of `$ARGUMENTS`. Prerequisite: `/stage-file` chain already populated `§Plan Author` + passed `/plan-review` (else readiness gate stops with handoff).

Follow `caveman:caveman` for all your own output and all dispatched subagents. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Context resolution (before dispatch)

Parse `$ARGUMENTS` as `{MASTER_PLAN_PATH} {STAGE_ID}`:

- `MASTER_PLAN_PATH` = first token (path to master plan, e.g. `ia/projects/citystats-overhaul-master-plan.md`).
- `STAGE_ID` = remainder (excluding any flags).
- If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset (ship-stage subagent uses its frontmatter model).

Verify `{MASTER_PLAN_PATH}` exists (Glob). Extract plan display name from filename. Print context banner:

```
SHIP-STAGE {STAGE_ID} — {plan display name}
  master plan : {MASTER_PLAN_PATH}
  stage       : {STAGE_ID}
```

---

## Stage 1 — Chain dispatch (`ship-stage`)

Dispatch Agent with `subagent_type: "ship-stage"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> ## Mission
>
> Run `ia/skills/ship-stage/SKILL.md` end-to-end on `{MASTER_PLAN_PATH}` Stage `{STAGE_ID}`.
>
> Follow caveman:caveman. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, chain-level digest JSON, destructive-op confirmations.
>
> ## Phase sequence
>
> 1. Phase 0 — Parse stage task table (narrow regex; fail loud on schema mismatch).
> 2. Phase 1 — Context load via `domain-context-load` subskill (once per chain).
> 3. Phase 1.5 — §Plan Author readiness gate (`ia/skills/ship-stage/SKILL.md` Step 1.5): for each pending spec verify `## §Plan Author` populated. Non-populated → `STOPPED — prerequisite: §Plan Author not populated for {ISSUE_ID_LIST}` + `/author` handoff; no Pass 1.
> 4. Phase 1.6 — Resume gate (SKILL Step 1.6): `git log --first-parent -400` for `feat(TECH-xxx):` / `fix(TECH-xxx):` per pending Task; skip Pass 1 for satisfied ids; all satisfied → skip Pass 1 → Pass 2 only. Disabled by `--no-resume` or `--per-task-verify`.
> 5. Phase 2 — Pass 1 per-Task loop: implement (`spec-implementer` work inline) → `unity:compile-check` → atomic commit (resume skips satisfied tasks) (unless `--per-task-verify`, which also runs per-Task verify-loop + code-review in Pass 1).
> 6. Phase 3 — Pass 2 Stage-end (once after all Pass 1 tasks): full `verify-loop` (Path A+B) on cumulative delta → Stage-level code-review → audit → **closeout** (`stage-closeout-planner` → `plan-applier` Mode stage-closeout). **Closeout is mandatory** when upstream Pass 2 gates pass — do not emit `PASSED` or defer `/closeout` after successful verify + review + audit (see `ia/skills/ship-stage/SKILL.md` **Normative — closeout is part of `PASSED`**).
> 7. Phase 4 — Chain-level stage digest (JSON header + caveman summary + `chain:` block).
> 8. Phase 5 — Next-stage resolver (4 cases: filed / pending / skeleton / umbrella-done).
>
> ## Exit
>
> End with one of:
> - `SHIP_STAGE {STAGE_ID}: PASSED` (**only** after Step 3.5 closeout + validators succeed — not after verify/audit alone)
> - `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — {gate}: {reason}`
> - `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Author not populated for …` (+ `/author` Next line)
> - `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL`
> - `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch`

---

## Pipeline summary output

After dispatch completes (or on stop), emit:

```
SHIP-STAGE {STAGE_ID}: {PASSED|STOPPED|STAGE_VERIFY_FAIL}
  master plan : {plan display name} ({MASTER_PLAN_PATH})
  tasks shipped : {count} ({ids})
  stage verify  : {passed|failed|skipped}
```

On `PASSED`: include `Next:` handoff from resolver.
On `STOPPED`: include `Next: claude-personal "/ship {FAILED_ISSUE_ID}"` after fix.
On `STAGE_VERIFY_FAIL`: include `Human review required — do NOT resume tasks automatically.`
