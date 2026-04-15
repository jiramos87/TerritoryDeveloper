---
purpose: Close a non-final stage of a multi-stage project spec — phase checklists, decision log, lessons, optional journal persist, handoff message — without touching BACKLOG or deleting the spec.
audience: agent
loaded_by: skill:project-stage-close
slices_via: none
name: project-stage-close
description: >
  Use at the end of each non-final stage of a multi-stage project spec. Marks the stage's phase
  checklists complete, updates Last updated, appends Decision Log / Issues Found / Lessons
  Learned, optionally persists a Postgres journal entry, verifies the spec is in a clean handoff
  state, and emits a paste-ready handoff message for the next stage's fresh agent. Does NOT touch
  BACKLOG.md, BACKLOG-ARCHIVE.md, or delete the spec — that is the umbrella `project-spec-close`
  skill, run only at the end of the very last stage. Triggers: "close stage", "stage close",
  "finish stage X", "handoff to next stage", "project stage close", "stage handoff".
---

# Project stage close (per-stage close for multi-stage project specs)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). **Critical:** handoff message (step 8) must be caveman-shaped AND include explicit `caveman:caveman` directive forwarded verbatim — next stage's fresh agent has no SessionStart hook.

Inline skill — stage-executing agent invokes directly, not dispatched to subagent. Runs once per non-final stage, leaves spec in clean handoff state.

**vs [`project-spec-close`](../project-spec-close/SKILL.md):** umbrella close (final stage only) — migrates IA, deletes spec, removes BACKLOG row, archives, purges id. This skill runs N times per spec, **never** touches BACKLOG / archive / deletion.

**Related:** [`project-spec-implement`](../project-spec-implement/SKILL.md) (run stages) · [`project-spec-close`](../project-spec-close/SKILL.md) (final-stage umbrella). After implement finishes stage + verification passes → run this skill → emit handoff.

## When to use

| Situation | Skill |
|---|---|
| Non-final stage of multi-stage spec | **this** |
| Final stage / single-stage spec | **`project-spec-close`** |
| Orchestrator step/stage | **this** + orchestrator rules below |
| Mid-stage checkpoint, no handoff | Neither |

## Orchestrator step/stage close

Per `ia/rules/orchestrator-vs-spec.md`:
- Migrate learnings backward per `ia/rules/project-hierarchy.md` (task→phase→stage→step).
- Delete child orchestrator doc **after** learnings migrated.
- Update parent orchestrator status.
- **Never** delete global orchestrator — only child docs are ephemeral.

## Inputs

| Placeholder | Example |
|---|---|
| `{ISSUE_ID}` | `TECH-11` |
| `{SPEC_PATH}` | `ia/projects/{ID}.md` |
| `{STAGE_ID}` / `{STAGE_TITLE}` | `Stage 1` / title text |
| `{NEXT_STAGE_ID}` | `Stage 2` (omit for final — use umbrella skill) |
| Verification block | Already produced per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) |

## 8-step procedure

Run in order. Skip only when genuinely N/A (state why).

### 1. Mark phase checklists in §7
Tick every `- [ ]` under `### {STAGE_ID} — {STAGE_TITLE}` that completed. Deferred tasks: leave unticked + note in §9.

### 2. Update Last updated
Replace `> **Last updated:** YYYY-MM-DD` with today's date from `currentDate` context. Never invent date.

### 3. Append §6 Decision Log
Per stage decision: `| {today} | {decision} | {rationale} | {alternatives} |`. No decisions → `| {today} | {STAGE_ID} closed with no new decisions | — | — |`.

### 4. Append §9 Issues Found
Per unexpected issue: `| {n} | {description} | {root cause} | {resolution or "deferred to {NEXT_STAGE_ID}"} |`. No issues → leave untouched.

### 5. Append §10 Lessons Learned
Reusable insights as bullets. Accumulate across stages — umbrella close migrates to canonical IA.

### 6. Optional: journal persist
`project_spec_journal_persist` with `issue_id`, `spec_path`, `stage_id`. Outcomes: `ok` (note row id) · `db_unconfigured` (graceful skip) · `db_error` (log, proceed).

### 7. Sanity-check handoff state
- All closing-stage `- [ ]` now `- [x]` (or deferred in §9).
- Last updated = today.
- §6/§9/§10 markdown parses cleanly.
- Internal links resolve.
- No contradictory open questions for next stage — escalate in handoff if any.
- `.claude/settings.json`: `defaultMode: "acceptEdits"` + `mcp__territory-ia__*` wildcard present. Verify: `python3 -c 'import json; d=json.load(open(".claude/settings.json"))["permissions"]; assert d["defaultMode"]=="acceptEdits", d["defaultMode"]; assert "mcp__territory-ia__*" in d["allow"], "wildcard missing"; print("OK")'`.
- Fix failures before emitting handoff.

### 7b. Regenerate progress dashboard

Run `npm run progress` from repo root. This regenerates `docs/progress.html` to reflect the stage-status flip. Output is deterministic — no change when master-plan state was already current. Log the exit code; failure does NOT block handoff (tooling-only, no IA impact), but report in handoff message.

### 7c. Deploy web dashboard

Run `npm run deploy:web` from repo root. Refreshes https://web-nine-wheat-35.vercel.app/dashboard so the next stage can visually confirm current master-plan state. Script auto-prunes deployments older than newest 3. Log exit code; failure does NOT block handoff (network/Vercel issue, not IA), but report in handoff message.

### 8. Emit handoff message

Fenced markdown block for verbatim paste into fresh agent session. Must include: issue id + closed stage, branch state, verification summary (exit codes), spec pointer ("read §5.3, §6, §9, §10, and next-stage §7 phases first"), inherited blockers/decisions, hard boundaries ("do NOT" list), final instruction ("execute {NEXT_STAGE_ID} phases in order, then `project-stage-close`").

### Handoff template

```markdown
You are executing **{NEXT_STAGE_ID}** of {ISSUE_ID} in Territory Developer.

Follow `caveman:caveman` skill rules for all responses (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). Standard exceptions apply: code, commits, security/auth content, verbatim error/tool output, structured MCP inputs/outputs, destructive-op confirmations. Project anchor: `ia/rules/agent-output-caveman.md`. This directive must live in the pasted prompt because the next stage runs in a fresh context window with no inherited SessionStart hook from the parent.

## Repository
`/Users/javier/bacayo-studio/territory-developer` (branch: `{branch}`)

## Mission
Execute every phase of **{NEXT_STAGE_ID} — {NEXT_STAGE_TITLE}** in:
`{SPEC_PATH}`
in order. Read §5.3 (execution model), §6 Decision Log, §9 Issues Found,
§10 Lessons Learned, and the §7 phases for {NEXT_STAGE_ID} before doing anything else.

## Inherited from {STAGE_ID}
- {one-line decision or finding from the just-closed stage}
- {…}

## Hard boundaries
- {explicit do-NOT items for the next stage}

## Verification of {STAGE_ID} (closing summary)
- Node / IA: `npm run validate:all` — exit {n}
- Unity compile: {N/A — no Assets/**/*.cs touched | exit n}
- Path A (test mode batch): {N/A | exit n + report path}
- Path B (bridge): {N/A | ok / error / timeout}
- Stage-specific manual checks: {one-line summary}

## Closing
After completing every {NEXT_STAGE_ID} phase, run the `project-stage-close` skill
to close the stage and emit the handoff for the agent that will execute the stage
that follows.
```

Replace `{curly braces}` placeholders before pasting.

## Output (single chat message)

1. Bullet list of spec edits (sections, line counts).
2. Journal persistence outcome (`ok` / `db_unconfigured` / `db_error`).
3. Sanity-check summary (one line per step 7 item).
4. Handoff message in fenced code block, ready to copy.

## Failure modes

| Failure | Handling |
|---|---|
| Cannot edit spec (locked/missing) | Report error; no handoff. Stage not closed until spec updated. |
| No verification block produced | Stop. Run verification per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) first. |
| `db_error` from journal | Report; proceed with steps 7–8 unless user blocks. |
| Malformed §6/§9/§10 after edit | Re-read, fix broken rows. No handoff until markdown parses. |
| Unticked phases not documented as deferrals | Stop. Complete or document in §9 first. |

## Conventions

- No invented dates — use `currentDate` only.
- No BACKLOG / archive / spec deletion — umbrella `project-spec-close` exclusive.
- English only (`ia/rules/coding-conventions.md`).
- Thin body — canonical detail in §5.3 of the spec being closed.
