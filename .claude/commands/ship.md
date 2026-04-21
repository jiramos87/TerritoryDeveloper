---
description: Single-task pipeline — readiness → implement → verify-loop → code-review (+ fix loop cap=1) → audit — sequentially for one ISSUE_ID. Each stage gates on the previous succeeding; stops on failure and reports which stage failed. For standalone issues (no master plan) or the N=1 tail of a `/stage-file` chain.
argument-hint: "{ISSUE_ID} (e.g. TECH-42)"
---

# /ship — sequential readiness → implement → verify-loop → code-review → audit

Orchestrate all five per-Task lifecycle stages for `$ARGUMENTS` in order. Run each stage by dispatching the matching subagent via the Agent tool. **Do NOT run stages in parallel — each gate must pass before the next starts.**

Follow `caveman:caveman` for all your own output and all dispatched subagents below. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

**Related:** [`/ship-stage`](ship-stage.md) (multi-task Stage chain — implement / verify / code-review / audit / closeout) · [`/stage-file`](stage-file.md) (seam #2 chain; N=1 handoff → `/ship`) · [`/author`](author.md) (writes `§Plan Author`; prerequisite for this command) · [`/closeout`](closeout.md) (Stage-scoped bulk closeout — NOT per-Task).

Subagents reused from the `/ship-stage` chain: [`spec-implementer`](../agents/spec-implementer.md), [`verify-loop`](../agents/verify-loop.md), [`opus-code-reviewer`](../agents/opus-code-reviewer.md), [`plan-applier`](../agents/plan-applier.md) Mode code-fix, [`opus-auditor`](../agents/opus-auditor.md).

**Scope note (no closeout stage):** Umbrella / archival closeout is Stage-scoped only post-T7.14 (see [`/closeout`](closeout.md)). `/ship` ends at audit + PASSED; archival of `ia/backlog/{id}.yaml` + deletion of `ia/projects/{id}.md` happens when the owning Stage closes via `/ship-stage` Step 3.5 or `/closeout {MASTER_PLAN_PATH} {STAGE_ID}`. Standalone issues (no master plan) ship + audit here; archival remains manual until a bulk Stage gathers them.

## Step 0 — Context resolution (before any dispatch)

Before dispatching any subagent, resolve and display context for the human developer:

1. Glob `ia/projects/$ARGUMENTS*.md` → confirm spec file exists; extract short description from filename. Missing → abort with `SHIP STOPPED at context — spec missing: ia/projects/$ARGUMENTS*.md`.
2. Glob `ia/projects/*-master-plan.md` → for each file, grep for `$ARGUMENTS`. Identify which master plan owns this issue (task row reference). Extract plan display name from filename (e.g. `blip-master-plan.md` → `Blip`).
3. Grep `BACKLOG.md` for `$ARGUMENTS` → extract the one-line issue title.
4. Print the context banner **before Stage 1 starts**:

```
SHIP $ARGUMENTS — {issue title}
  master plan : {Plan Name} (ia/projects/{master-plan-filename})
  spec        : ia/projects/{spec-filename}
  stages      : readiness → implement → verify-loop → code-review → audit
```

If no master plan references the issue, print `master plan: (none — standalone issue)`.

---

## Stage sequence

### Stage 1 — Readiness gate (`§Plan Author` populated)

`/ship` does NOT run `/author` or `/plan-review` internally — both fold into `/stage-file` chain (F6 re-fold 2026-04-20). Specs arriving at `/ship` must already carry populated `## §Plan Author` from `/stage-file` chain tail OR from manual `/author --task $ARGUMENTS`.

**Idempotent readiness check:** read `ia/projects/$ARGUMENTS*.md` and locate `## §Plan Author`. Treat spec as **populated** when ALL of these hold:

1. `## §Plan Author` heading exists.
2. No line inside the block (until next `## ` heading at same/higher level) matches `_pending` case-insensitively.
3. All four sub-headings (`### §Audit Notes`, `### §Examples`, `### §Test Blueprint`, `### §Acceptance`) exist with non-whitespace body content.

**Gate:** spec populated → continue to Stage 2. Otherwise STOP:

```
SHIP $ARGUMENTS: STOPPED — prerequisite: §Plan Author not populated
Next: claude-personal "/author --task $ARGUMENTS"
```

Gate is idempotent — safe to re-enter after `/author --task` completes.

---

### Stage 2 — Implement (`spec-implementer`)

Dispatch Agent with `subagent_type: "spec-implementer"`:

> ## Mission
>
> Run `project-spec-implement` skill (`ia/skills/project-spec-implement/SKILL.md`) end-to-end on `ia/projects/$ARGUMENTS*.md`. Resolve filename via Glob — may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md`.
>
> ## Phase loop
>
> 1. Read spec (focus §5 Proposed Design, §6 Decision Log, §7 Implementation Plan, §Plan Author, §9 Issues Found, §10 Lessons Learned). Start at first unticked phase.
> 2. MCP context per phase — `backlog_issue` + `router_for_task` + targeted `spec_section` / `spec_sections`. `invariants_summary` once when runtime C#/subsystem changes involved.
> 3. Implement with minimal diffs. `Edit` for existing files, `Write` only for new files.
> 4. Verify after each phase per `docs/agent-led-verification-policy.md`. Stop on failure; root-cause.
> 5. Tick phase checklist.
>
> ## Hard boundaries
>
> - Do NOT skip phases. Execute in spec order.
> - Do NOT bypass failing verification with `--no-verify`.
> - Do NOT add features/refactors/improvements beyond phase scope.
> - Do NOT introduce new singletons or `FindObjectOfType` in `Update` (per `ia/rules/invariants.md`).
> - Do NOT load whole reference specs — slice via MCP.
> - Do NOT edit BACKLOG row state, archive, or delete spec — closeout territory.
>
> ## Output
>
> Single concise caveman message per phase: phase id closed, files touched, verification run, issues + resolution. Final message must end with `IMPLEMENT_DONE` if all phases pass, or `IMPLEMENT_FAILED: {reason}` on unrecoverable error.

**Gate:** final output must contain `IMPLEMENT_DONE`. `IMPLEMENT_FAILED` → STOP:

```
SHIP $ARGUMENTS: STOPPED at implement — {reason}
Next: claude-personal "/ship $ARGUMENTS" after fix
```

---

### Stage 3 — Verify-loop (`verify-loop`)

Dispatch Agent with `subagent_type: "verify-loop"`:

> ## Mission
>
> Run integrated closed-loop verification on current branch + bounded fix iteration. Follow `ia/skills/verify-loop/SKILL.md` end-to-end. Issue id: `$ARGUMENTS`. Max iterations: 2.
>
> ## Execution sequence
>
> 1. Bridge preflight — `npm run db:bridge-preflight`.
> 2. Compile gate — `unity_bridge_command get_compilation_status` → `npm run unity:compile-check` → `get_console_logs` scan.
> 3. Node CI-parity — `npm run validate:all`.
> 4. Path A — `npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`.
> 5. Path B — queue `.queued-test-scenario-id` → `enter_play_mode` → `debug_context_bundle` → `exit_play_mode`.
> 6. Fix iteration (bounded 2) — minimal code edit → rerun compile + Path B.
> 7. Emit JSON Verification block + caveman summary. `verdict` field must be `pass`, `fail`, or `escalated`.
>
> ## Hard boundaries
>
> - Do NOT restate verification policy — defer to `docs/agent-led-verification-policy.md`.
> - Do NOT modify code outside Step 6 fix-iteration scope.
> - Do NOT exceed MAX_ITERATIONS (2). Escalate to human after cap.
> - Do NOT touch BACKLOG row state, archive, spec deletion — closeout territory.
>
> ## Output
>
> JSON Verification block + caveman summary. JSON `verdict` field determines pipeline gate.

**Gate:** `verdict` in JSON header must be `"pass"`. `"fail"` or `"escalated"` → STOP:

```
SHIP $ARGUMENTS: STOPPED at verify-loop — verdict: {verdict}
Human review required before code-review.
```

---

### Stage 4 — Code-review (`opus-code-reviewer` → `plan-applier` Mode code-fix on critical)

Dispatch Agent with `subagent_type: "opus-code-reviewer"`:

> ## Mission
>
> Run `ia/skills/opus-code-review/SKILL.md` end-to-end for `$ARGUMENTS`. Phase 1 Load diff (`git diff main...HEAD` across `ia/**/*.md` + `Assets/Scripts/**/*.cs`; fallback staged + recent-commit diff) + `ia/projects/$ARGUMENTS*.md` §7 Implementation Plan / §Plan Author §Acceptance / §Findings / §Verification. Run `domain-context-load` subskill for shared MCP bundle (keywords from spec title + domain terms). Load `invariants_summary` domain subset for changed files. Phase 2 Run 8-check review matrix → verdict (PASS / minor / critical). Phase 2a PASS → write `## §Code Review` mini-report. Phase 2b minor → mini-report + suggestions (fix-in-place or defer). Phase 3 critical → write `## §Code Fix Plan` tuples (contract 4-key shape — `operation`, `target_path`, `target_anchor`, `payload`) + `## §Code Review` mini-report. Phase 4 Hand-off `{verdict, issue_id}`.
>
> ## Hard boundaries
>
> - Do NOT mutate source code (C# / TS / skill bodies / commands / agents) — only spec `§Code Review` + `§Code Fix Plan` writes. Source fixes happen in pair-tail **`plan-applier`** Mode code-fix.
> - Do NOT re-run `/verify-loop` — pair-tail re-enters on critical verdict.
> - Do NOT run validators — pair-tail runs gate.
> - Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT emit `§Code Fix Plan` on PASS or minor verdict.
> - Do NOT commit — user decides.

**Verdict PASS / minor:** continue to Stage 5 (audit).

**Verdict critical (first time):** dispatch Agent with `subagent_type: "plan-applier"`:

> ## Mission
>
> Run `ia/skills/plan-applier/SKILL.md` — **Mode: code-fix** for `$ARGUMENTS`. Read `## §Code Fix Plan` tuples verbatim from `ia/projects/$ARGUMENTS*.md`. Resolve every `target_anchor` to single match before applying. Apply tuples in declared order (one atomic edit per tuple). Re-enter verify gate = `npm run verify:local` for C# edits OR `npm run validate:all` for tooling-only. 1-retry bound on verify fail (2 total attempts). Second fail → escalate. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-review drift — read tuples verbatim.
> - Do NOT reorder tuples — declared order only.
> - Do NOT interpret ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT skip verify re-entry on tuple application.
> - Do NOT commit — user decides.

After `plan-applier` success → **re-dispatch `opus-code-reviewer`** (cap = 1). Second critical verdict → STOP:

```
SHIP $ARGUMENTS: STOPPED at code-review — CRITICAL_TWICE
Human review required (structural issue).
```

On `plan-applier` escalation (second verify fail) → STOP:

```
SHIP $ARGUMENTS: STOPPED at code-review — code_fix verify_gate_failed_after_retry
Human review required.
```

Clean re-review PASS / minor → continue to Stage 5.

---

### Stage 5 — Audit (`opus-auditor`, N=1 degenerate)

Dispatch Agent with `subagent_type: "opus-auditor"`:

> ## Mission
>
> Run `ia/skills/opus-audit/SKILL.md` — **N=1 single-Task degenerate case** for `$ARGUMENTS`. No master plan / no Stage block: treat the one spec as Stage of N=1. Phase 0 sequential-dispatch guardrail trivially satisfied (single Task). Phase 1 load shared MCP bundle via `domain-context-load` (keywords: spec title + `audit` + domain terms). Phase 2 read `ia/projects/$ARGUMENTS*.md` §7 Implementation Plan / §Plan Author / §Findings / §Verification / §Code Review. Phase 3 synthesize ONE `§Audit` paragraph (consistent voice with stage-scoped audits). Phase 4 write via `replace_section` on `## §Audit` (or `insert_after ## §Verification` if absent). Phase 5 caveman hand-off summary.
>
> ## Hard boundaries
>
> - Do NOT proceed if §Findings empty — verify-loop should have populated. Missing → STOP + direct user to re-run `/verify-loop`.
> - Do NOT edit other spec sections (§1 / §2 / §7 / §8 / §Code Review / §Findings / §Verification) — audit touches `§Audit` only.
> - Do NOT write `§Closeout Plan` / `§Stage Closeout Plan` — out of scope for `/ship`.
> - Do NOT run validators — seam-scoped write only.
> - Do NOT commit — user decides.

**Gate:** audit paragraph written. Failure → STOP:

```
SHIP $ARGUMENTS: STOPPED at audit — {reason}
```

Success → emit PASSED summary (next section).

---

## Pipeline summary output

After all five stages complete (or on stop), emit a single summary. Include the **issue title** (from Step 0 / `BACKLOG.md`) and **one line** describing what shipped or where it stopped — not only the id + stage ticks.

```
SHIP $ARGUMENTS: {PASSED|STOPPED} — {issue title from BACKLOG}
  implemented : {one-line summary of what landed, or STOPPED reason}
  master plan : {Plan Name} (ia/projects/{master-plan-filename})
  Stage 1 readiness:   {done|failed}
  Stage 2 implement:   {done|failed|skipped}
  Stage 3 verify:      {done|failed|skipped} [verdict: {pass|fail|escalated}]
  Stage 4 code-review: {done|failed|skipped} [verdict: {PASS|minor|critical|critical_twice}]
  Stage 5 audit:       {done|failed|skipped}
```

**Closeout reminder on PASSED:** `/ship` does NOT archive the spec or flip BACKLOG status. Commit manually; archival happens later via the owning Stage's `/ship-stage` or `/closeout {MASTER_PLAN_PATH} {STAGE_ID}`. Standalone issues remain open until batched into a Stage close.

---

## Next-handoff resolver (on PASSED only)

If a master plan owns this issue (Step 0): open that master plan file and find the next task row whose status is **not** `Done` / `archived` / `skipped` — reading task rows in document order after the closed issue's row.

**Before emitting the handoff:** count all non-Done filed task rows in the same Stage X.Y as the closed issue. If ≥2 remain unfiled or non-Done in that stage, prefer the stage chain:

```
Next: claude-personal "/ship-stage ia/projects/{master-plan-filename} Stage {X.Y}"
```

Otherwise (single remaining task, or all remaining tasks belong to a different stage), emit the single-issue handoff:

```
Next: claude-personal "/ship {NEXT_ISSUE_ID}"
```

If no filed task row exists but the master plan has unstarted Steps (status `Draft`, `_pending_`, or skeleton — tasks not yet decomposed/filed), identify the next such Step and append:

```
Next: claude-personal "/stage-decompose ia/projects/{master-plan-filename} Step {N}"
```

If the issue is standalone (no master plan), or the master plan has no remaining steps at all, omit the line. Do NOT scan `BACKLOG.md` by numeric adjacency — next task must come from the owning master plan.

---

## Hard boundaries

- Sequential stage dispatch only — no parallel (each gate inputs the previous stage's outputs).
- Readiness gate (Stage 1) is idempotent on populated `§Plan Author` — safe to re-enter after `/author --task` completes.
- Code-review critical re-entry cap = **1**; second critical → `CRITICAL_TWICE` human-review directive. Do NOT re-enter a third time.
- Do NOT dispatch `plan-author`, `plan-reviewer`, or `stage-closeout-planner` from `/ship` — all out of scope per post-T7.14 / F6 re-fold surface split.
- Do NOT archive `ia/backlog/{id}.yaml`, delete `ia/projects/{id}.md`, or flip master-plan task row Status from this command. Archival territory belongs to `/ship-stage` Step 3.5 / `/closeout`.
- Do NOT skip stages on green path. PASSED is emitted **only** after Stage 5 audit succeeds.
- Retired surfaces `spec-kickoff` and single-issue `closeout` are tombstoned under `.claude/agents/_retired/` — do NOT invoke; kickoff work is absorbed into `/author` (via `/stage-file` or manual `--task`), closeout into Stage-scoped `/closeout`.
