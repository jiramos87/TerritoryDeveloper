---
purpose: "Single-task standalone ship pipeline. Four mechanical steps: author digest → implement → verify-loop → close. No master plan, no code review, no audit, no commit."
audience: agent
loaded_by: skill:ship
slices_via: none
name: ship
description: >
  Standalone single-task ship pipeline. Four mechanical steps in order:
  (1) author §Plan Digest via stage-authoring --task, (2) implement via
  spec-implementer, (3) verify-loop with MAX_ITERATIONS=2, (4) close via
  DB status walk (pending → implemented → verified → done → archived).
  Standalone-tasks only — task must have master_plan_id IS NULL. No code
  review. No audit. No commit. No master-plan handoff. Stage-attached
  tasks must use /ship-stage instead.
  Triggers: "/ship {ISSUE_ID}", "ship task", "ship standalone".
  Argument: {ISSUE_ID} (e.g. TECH-42, BUG-17, FEAT-9).
phases:
  - "Resolve task + standalone gate"
  - "Author §Plan Digest"
  - "Implement"
  - "Verify-loop"
  - "Close (DB status walk)"
  - "Hand-off"
---

# Ship skill — single-task standalone pipeline

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Drive one standalone Task through the four mechanical steps that turn a `pending` row into an `archived` row. No master plan involvement, no review/audit gates, no git commit. Resumable: every step is idempotent on re-entry.

**Scope guard:** Task must be standalone (`master_plan_id IS NULL` in `ia_tasks`). Stage-attached tasks ship via `/ship-stage`. Mismatch → STOP with handoff line.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ISSUE_ID` | 1st arg | `{PREFIX}-{N}`. `PREFIX ∈ {TECH, FEAT, BUG, ART, AUDIO}`. Resolved via `task_state` MCP. |

---

## Phase 0 — Resolve task + standalone gate

1. Call `task_state({task_id: ISSUE_ID})`. Missing → STOP `STOPPED — task not found in DB: {ISSUE_ID}`.
2. Read `slug` + `stage_id` + `status` + `title` from response.
3. Standalone gate: `slug == null AND stage_id == null` → continue. Stage-attached → STOP:
   ```
   SHIP {ISSUE_ID}: STOPPED — task is stage-attached (slug={slug}, stage={stage_id}).
   Next: /ship-stage ia/projects/{slug}-master-plan.md Stage {stage_id}
   ```
4. Terminal-status idle exit: `status ∈ {done, archived}` → emit summary `SHIP {ISSUE_ID}: ALREADY_CLOSED ({status})` + Phase 5 handoff (no work).
5. Print banner:
   ```
   SHIP {ISSUE_ID} — {title}
     standalone : true
     status     : {status}
     pipeline   : author → implement → verify → close
   ```

---

## Phase 1 — Author §Plan Digest

Idempotent readiness check first: `task_spec_section({task_id, section: "§Plan Digest"})`. Body present + non-empty + sub-headings (`### §Goal`, `### §Acceptance`, `### §Mechanical Steps`) populated → skip authoring.

Otherwise execute `ia/skills/stage-authoring/SKILL.md` end-to-end with `--task {ISSUE_ID}` flag (bulk pass of N=1). Steps reused verbatim:

- Phase 0–8 of stage-authoring run inline (sequential-dispatch guardrail trivially satisfied; load shared MCP bundle for the single task; bulk author §Plan Digest direct; self-lint via `plan_digest_lint` cap=1; mechanicalization preflight; `task_spec_section_write({task_id, section: "§Plan Digest", body})`).
- Aggregate-doc compile is OUT OF SCOPE (retired). DB sole source.

**Gate:** §Plan Digest written to DB, `plan_digest_lint` PASS, `mechanicalization_preflight_lint` PASS (or TECH-776 advisory hatch on `picks`-only failure). Failure → STOP:

```
SHIP {ISSUE_ID}: STOPPED at author — {reason}
Next: /ship {ISSUE_ID} after fix
```

---

## Phase 2 — Implement

Execute `ia/skills/project-spec-implement/SKILL.md` end-to-end on `ISSUE_ID`. DB-first reads:

- `task_spec_body({task_id})` for spec body (§7 / §Plan Digest).
- `backlog_issue({issue_id})` for Notes / Acceptance.
- `router_for_task({task_id})` for invariants/glossary slice hints.
- Per-phase `invariants_summary` only when runtime C# / subsystem changes implicated.

Minimal diffs. `Edit` for existing files; `Write` only for new files. Verify per phase per `docs/agent-led-verification-policy.md`. Stop on first failure; root-cause.

**Status flip on completion:** `task_status_flip({task_id, new_status: "implemented"})`. NO commit.

**Gate:** all §Plan Digest mechanical steps applied + per-step verification passed + status flipped to `implemented`. Failure → STOP:

```
SHIP {ISSUE_ID}: STOPPED at implement — {reason}
Next: /ship {ISSUE_ID} after fix
```

---

## Phase 3 — Verify-loop

Execute `ia/skills/verify-loop/SKILL.md` end-to-end on current branch state. `MAX_ITERATIONS=2` (locked).

- Bridge preflight → compile gate → `validate:all` → Path A (`unity:testmode-batch`) → Path B (IDE bridge hybrid) → bounded fix iteration (cap=2) → JSON Verification block.
- `--tooling-only` flag: pass through when `git diff HEAD` shows zero `Assets|Packages|ProjectSettings` paths dirty. Skips Path A/B; runs `validate:all` only.

**Gate:** JSON `verdict == "pass"`. `fail` / `escalated` → STOP:

```
SHIP {ISSUE_ID}: STOPPED at verify — verdict: {verdict}
Human review required.
Next: /ship {ISSUE_ID} after fix
```

---

## Phase 4 — Close (DB status walk)

Walk the `ia_tasks.status` enum to terminal in three calls:

1. `task_status_flip({task_id, new_status: "verified"})` — verify-loop passed.
2. `task_status_flip({task_id, new_status: "done"})` — sets `completed_at = now()`.
3. `task_status_flip({task_id, new_status: "archived"})` — sets `archived_at = now()`.

Each call is idempotent (re-entering on already-final state is a no-op transition guarded by enum walk). Failure on any flip → STOP `SHIP {ISSUE_ID}: STOPPED at close — {reason}`.

**No filesystem ops.** Backlog yaml + spec markdown deleted in Step 9.6 / 9.5; DB is sole source of truth.

**No commit.** User decides when to commit (locked answer 5).

**No master-plan task-row sync.** Standalone task by definition has no master plan row to flip.

---

## Phase 5 — Hand-off

Single summary line:

```
SHIP {ISSUE_ID}: PASSED — {title}
  status walk : pending → implemented → verified → done → archived
  diff        : {git diff --stat HEAD count}
  next        : commit when ready (caveman not advisable for code; user decides)
```

On any STOP path, emit the structured stop line + `Next:` directive (specific to the failing step).

---

## Hard boundaries

- **Standalone-tasks only.** Stage-attached → handoff to `/ship-stage`.
- **No code review.** Locked B3 #2.
- **No audit.** Locked B3 #3.
- **No commit.** Locked B3 #5. User commits manually after PASSED.
- **`MAX_ITERATIONS=2`** for verify-loop (locked B3 #6).
- **Sequential steps only.** Each gate inputs the previous step's output.
- **Idempotent on re-entry.** Phase 1 readiness skip + Phase 4 status-walk no-ops handle resume.
- **No filesystem ops.** No yaml archive, no spec delete — both already gone post Step 9.x.
- Do NOT auto-invoke `/stage-authoring` Stage-scope mode — only `--task` mode.
- Do NOT call `stage_closeout_apply` — that MCP is stage-scoped only; standalone close is direct status walk.
- Do NOT touch BACKLOG.md / BACKLOG-ARCHIVE.md — generator views; archived task drops from open list automatically on next regen.

---

## Cross-references

- `ia/skills/stage-authoring/SKILL.md` — Phase 1 author digest (delegated, `--task` mode).
- `ia/skills/project-spec-implement/SKILL.md` — Phase 2 implement.
- `ia/skills/verify-loop/SKILL.md` — Phase 3 verify-loop.
- `ia/skills/ship-stage/SKILL.md` — multi-task stage chain (different surface).
- `ia/rules/plan-apply-pair-contract.md` — `§Plan Digest` shape contract.
- `ia/rules/plan-digest-contract.md` — 9-point lint rubric enforced in Phase 1.
- `docs/agent-led-verification-policy.md` — Path A / Path B / verdict semantics.

## Changelog

- Initial single-skill replacement of legacy 5-stage `/ship`. Standalone-only scope. Drops code-review + audit per locked answers.
