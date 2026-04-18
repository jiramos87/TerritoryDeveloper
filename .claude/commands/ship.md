---
description: Full issue pipeline — kickoff → implement → verify-loop → closeout — sequentially for one ISSUE_ID. Each stage gates on the previous succeeding; stops on failure and reports which stage failed.
argument-hint: "{ISSUE_ID} (e.g. TECH-42)"
---

# /ship — sequential kickoff → implement → verify-loop → closeout

Orchestrate all four lifecycle stages for `$ARGUMENTS` in order. Run each stage by dispatching the matching subagent via the Agent tool. **Do NOT run stages in parallel — each gate must pass before the next starts.**

Follow `caveman:caveman` for all your own output and all dispatched subagents below. Standard exceptions: code, commits, security/auth, verbatim tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Context resolution (before any dispatch)

Before dispatching any subagent, resolve and display context for the human developer:

1. Glob `ia/projects/$ARGUMENTS*.md` → confirm spec file exists; extract short description from filename.
2. Glob `ia/projects/*-master-plan.md` → for each file, grep for `$ARGUMENTS`. Identify which master plan owns this issue (task row reference). Extract plan display name from filename (e.g. `blip-master-plan.md` → `Blip`).
3. Grep `BACKLOG.md` for `$ARGUMENTS` → extract the one-line issue title.
4. Print the context banner **before Stage 1 starts**:

```
SHIP $ARGUMENTS — {issue title}
  master plan : {Plan Name} (ia/projects/{master-plan-filename})
  spec        : ia/projects/{spec-filename}
```

If no master plan references the issue, print `master plan: (none — standalone issue)`.

---

## Stage sequence

### Stage 1 — Kickoff (`spec-kickoff`)

Dispatch Agent with `subagent_type: "spec-kickoff"`:

> ## Mission
>
> Run `project-spec-kickoff` skill (`ia/skills/project-spec-kickoff/SKILL.md`) end-to-end on `ia/projects/$ARGUMENTS*.md`. Resolve filename via Glob — may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md`.
>
> ## MCP first
>
> 1. `mcp__territory-ia__backlog_issue` for `$ARGUMENTS` — Files / Notes / Spec / Acceptance / depends_on_status.
> 2. `mcp__territory-ia__invariants_summary` once if code/subsystem changes implied.
> 3. `mcp__territory-ia__router_for_task` per domain (1–3 from Summary/Goals/Files).
> 4. `mcp__territory-ia__spec_section` or `spec_sections` batch for routed specs — slices, never whole files.
> 5. `mcp__territory-ia__glossary_discover` with English keyword array → narrow via `glossary_lookup`.
>
> ## Editorial pass
>
> Tighten Open Questions, Implementation Plan phases, Decision Log, sibling cross-links. Edit spec in place. Do NOT execute Implementation Plan. Do NOT close issue.
>
> ## Output
>
> Single concise caveman message: spec edits made, Open Questions resolved/deferred, glossary terms aligned, Implementation Plan phases tightened, Verification readiness. End with "KICKOFF_DONE" so the orchestrating pipeline can gate.

**Gate:** kickoff subagent output must contain `KICKOFF_DONE`. If it reports a blocker or error instead, STOP the pipeline and report: `SHIP STOPPED at kickoff — {reason}`. Do not proceed to Stage 2.

---

### Stage 2 — Implement (`spec-implementer`)

Dispatch Agent with `subagent_type: "spec-implementer"`:

> ## Mission
>
> Run `project-spec-implement` skill (`ia/skills/project-spec-implement/SKILL.md`) end-to-end on `ia/projects/$ARGUMENTS*.md`. Resolve filename via Glob — may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md`.
>
> ## Phase loop
>
> 1. Read spec (focus §5 Proposed Design, §6 Decision Log, §7 Implementation Plan, §9 Issues Found, §10 Lessons Learned). Start at first unticked phase.
> 2. MCP context per phase — `backlog_issue` + `router_for_task` + targeted `spec_section` / `spec_sections`. `invariants_summary` once when runtime C#/subsystem changes involved.
> 3. Implement with minimal diffs. `Edit` for existing files, `Write` only for new files.
> 4. Verify after each phase per `docs/agent-led-verification-policy.md`. Stop on failure; root-cause.
> 5. Tick phase checklist.
>
> Multi-stage → invoke `project-stage-close` skill **inline** at end of each non-final stage.
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
> Single concise caveman message per phase: phase id closed, files touched, verification run, issues + resolution. Final message must end with "IMPLEMENT_DONE" if all phases pass, or "IMPLEMENT_FAILED: {reason}" on unrecoverable error.

**Gate:** final output must contain `IMPLEMENT_DONE`. If `IMPLEMENT_FAILED`, STOP and report: `SHIP STOPPED at implement — {reason}`. Do not proceed to Stage 3.

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

**Gate:** `verdict` in JSON header must be `"pass"`. `"fail"` or `"escalated"` → STOP and report: `SHIP STOPPED at verify-loop — verdict: {verdict}. Human review required before closeout.`. Do not proceed to Stage 4.

---

### Stage 4 — Closeout (`closeout`)

Dispatch Agent with `subagent_type: "closeout"`:

> ## Mission
>
> Run `project-spec-close` skill (`ia/skills/project-spec-close/SKILL.md`) — umbrella close (not per-stage) — on verified issue `$ARGUMENTS`. Migrate lessons → canonical IA, persist journal, validate dead spec paths, then delete spec, remove BACKLOG row, append to `BACKLOG-ARCHIVE.md`, purge id from durable docs/code. No confirmation gate — execute all ops in sequence.
>
> ## Sequence
>
> 1. `mcp__territory-ia__backlog_issue` for `$ARGUMENTS`.
> 2. `mcp__territory-ia__project_spec_closeout_digest` — extract H2s from `ia/projects/$ARGUMENTS*.md` (resolve via Glob).
> 3. **Migrate lessons** (non-destructive) — each Lessons Learned bullet → `docs/information-architecture-overview.md`, `AGENTS.md`, `ia/specs/glossary.md`, `ARCHITECTURE.md`, `ia/rules/*.md`, or `.claude/memory/{slug}.md`.
> 4. **Persist journal** — `mcp__territory-ia__project_spec_journal_persist` with `issue_id`. `db_unconfigured` → skip; `db_error` → log + continue.
> 5. **Validate** — `npm run validate:dead-project-specs` + `npm run validate:all`. Stop on failure.
> 6. **Destructive ops** — delete spec (`rm <single-file>`), remove BACKLOG row, append `[x] **$ARGUMENTS**` to `BACKLOG-ARCHIVE.md`, purge id from durable docs/code.
> 7. **Re-validate** — `npm run validate:dead-project-specs`.
>
> ## Hard boundaries
>
> - Do NOT `rm -rf`. Spec deletion = `rm <single-file>`.
> - Do NOT delete spec before lessons migrated.
> - Do NOT skip post-delete `validate:dead-project-specs`.
>
> ## Output
>
> Single closeout digest per `.claude/output-styles/closeout-digest.md`: fenced JSON header + caveman markdown summary.

---

## Pipeline summary output

After all four stages complete (or on stop), emit a single summary:

```
SHIP {ISSUE_ID}: {PASSED|STOPPED}
  master plan : {Plan Name} (ia/projects/{master-plan-filename})
  Stage 1 kickoff:    {done|failed}
  Stage 2 implement:  {done|failed|skipped}
  Stage 3 verify:     {done|failed|skipped} [verdict: {pass|fail|escalated}]
  Stage 4 closeout:   {done|failed|skipped}
```

If `PASSED` and a master plan owns this issue: open that master plan file (resolved in Step 0) and find the next task row whose status is **not** `Done` / `archived` / `skipped` — reading task rows in document order after the closed issue's row.

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
