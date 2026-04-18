---
description: Stage-scoped chain shipper — chains spec-kickoff → spec-implementer → verify-loop → closeout across every non-Done filed task row of one Stage X.Y in a master plan. Args: {MASTER_PLAN_PATH} {STAGE_ID} (e.g. "ia/projects/citystats-overhaul-master-plan.md Stage 1.1").
argument-hint: "{MASTER_PLAN_PATH} {STAGE_ID} (e.g. ia/projects/citystats-overhaul-master-plan.md Stage 1.1)"
---

# /ship-stage — stage-scoped chain dispatcher

Chain kickoff → implement → verify-loop → closeout across every non-Done filed task row of `$ARGUMENTS`.

Follow `caveman:caveman` for all your own output and all dispatched subagents. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Context resolution (before dispatch)

Parse `$ARGUMENTS` as `{MASTER_PLAN_PATH} {STAGE_ID}`:

- `MASTER_PLAN_PATH` = first token (path to master plan, e.g. `ia/projects/citystats-overhaul-master-plan.md`).
- `STAGE_ID` = remainder (e.g. `Stage 1.1`).

Verify `{MASTER_PLAN_PATH}` exists (Glob). Extract plan display name from filename. Print context banner:

```
SHIP-STAGE {STAGE_ID} — {plan display name}
  master plan : {MASTER_PLAN_PATH}
  stage       : {STAGE_ID}
```

---

## Stage 1 — Chain dispatch (`ship-stage`)

Dispatch Agent with `subagent_type: "ship-stage"`:

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
> 3. Phase 2 — Task loop: for each non-Done task: kickoff → implement → verify-loop (`--skip-path-b`) → closeout. Stop on first gate failure.
> 4. Phase 3 — Batched Path B verify on cumulative stage delta.
> 5. Phase 4 — Chain-level stage digest (JSON header + caveman summary + `chain:` block).
> 6. Phase 5 — Next-stage resolver (4 cases: filed / pending / skeleton / umbrella-done).
>
> ## Exit
>
> End with one of:
> - `SHIP_STAGE {STAGE_ID}: PASSED`
> - `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — {gate}: {reason}`
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
