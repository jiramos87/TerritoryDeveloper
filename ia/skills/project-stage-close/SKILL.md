---
purpose: Close a non-final stage of a multi-stage project spec — phase checklists, decision log, lessons, optional journal persist, handoff message — without touching BACKLOG or deleting the spec.
audience: agent
loaded_by: skill:project-stage-close
slices_via: none
name: project-stage-close
description: >
  Use at the end of each non-final stage of a multi-stage project spec (e.g. TECH-85). Marks the
  stage's phase checklists complete, updates Last updated, appends Decision Log / Issues Found /
  Lessons Learned, optionally persists a Postgres journal entry, verifies the spec is in a clean
  handoff state, and emits a paste-ready handoff message for the next stage's fresh agent. Does
  NOT touch BACKLOG.md, BACKLOG-ARCHIVE.md, or delete the spec — that is the umbrella
  `project-spec-close` skill, run only at the end of the very last stage. Triggers: "close
  stage", "stage close", "finish stage X", "handoff to next stage", "project stage close",
  "stage handoff".
---

# Project stage close (per-stage close for multi-stage project specs)

**Output style — caveman default.** Follow `caveman:caveman` skill rules for the chat message output produced while running this skill (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). Standard exceptions apply: code, commits, security/auth content, verbatim error/tool output, structured MCP inputs/outputs, destructive-op confirmations. **The handoff message in step 8 must itself be caveman-shaped** and must include an explicit "follow `caveman:caveman` skill rules" line forwarded verbatim to the next stage's fresh agent — without that directive baked into the pasted prompt, the next stage runs in clean context with no inherited SessionStart hook and falls back to verbose default phrasing. The skill is invoked inline (not via a subagent), so the preamble lives here directly. Project anchor: [`ia/rules/agent-output-caveman.md`](../../rules/agent-output-caveman.md).

This skill is **inline** — invoked directly by the stage-executing agent, **not** dispatched to a subagent. It runs once at the end of every non-final stage of a multi-stage project spec, leaving the spec in a clean handoff state for a fresh agent to pick up the next stage.

**Distinction from [`project-spec-close`](../project-spec-close/SKILL.md):** that skill is the **umbrella close** — it migrates lessons to canonical IA, deletes the project spec, removes the BACKLOG row, appends to BACKLOG-ARCHIVE, and purges the closed id. It runs **once per spec**, at the end of the **very last stage**. This skill (`project-stage-close`) runs **N times per spec**, once per non-final stage, and **never** touches BACKLOG / archive / spec deletion.

**Origin:** introduced by [TECH-85](../../projects/TECH-85-ia-migration.md) §5.3. Stage 1 of that migration is bootstrap-recursive — Phase 1.1 creates this skill, Phase 1.5 invokes it. Going forward, every multi-stage project spec is expected to use this pattern.

## Relationship to other lifecycle skills

- After [`project-spec-implement`](../project-spec-implement/SKILL.md) finishes the **stage**'s implementation phases and verification has passed, run **this** skill to close the stage and emit a handoff for the next stage's agent.
- Run [`project-spec-close`](../project-spec-close/SKILL.md) **only** at the end of the final stage (umbrella close).

## When to use vs when not to use

| Situation | Skill to run |
|---|---|
| Closing a non-final stage of a multi-stage spec | **`project-stage-close`** (this one) |
| Closing the final stage of a multi-stage spec | **`project-spec-close`** (umbrella) |
| Closing a single-stage / flat spec | **`project-spec-close`** |
| Mid-stage checkpoint with no agent handoff needed | Neither — keep working |

## Inputs (gather before running)

- **`{ISSUE_ID}`** — e.g. `TECH-85`
- **`{SPEC_PATH}`** — e.g. `ia/projects/TECH-85-ia-migration.md` (during the migration itself, the path is whatever is current; after Stage 2, it lives under `ia/projects/`)
- **`{STAGE_ID}`** — e.g. `Stage 1`
- **`{STAGE_TITLE}`** — e.g. `Quick wins on Claude Code (no breaking changes)`
- **`{NEXT_STAGE_ID}`** — e.g. `Stage 2` (omit when closing the final stage — but use the umbrella skill in that case)
- **Verification block** — already produced per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) before invoking this skill
- **Stage decisions / issues / lessons** — collected during stage execution

## 8-step procedure

Run **in order**. Skip a step only when it is genuinely **N/A** (state why in chat).

### 1. Mark phase checklists complete in §7

Open `{SPEC_PATH}` and tick every `- [ ]` checkbox under **`### {STAGE_ID} — {STAGE_TITLE}`** that the stage actually completed. Use `Edit` with the surrounding context (phase header) so the change is unambiguous. If a phase task was **not** completed and is intentionally deferred, leave the box unticked and add a one-line note in §9 Issues Found explaining the deferral.

### 2. Update **Last updated** in the spec header

Locate the `> **Last updated:** YYYY-MM-DD` line near the top of the spec and replace the date with **today's date**. Use the date the host injects via the `currentDate` context block (e.g. `2026-04-10`) — do not invent a date.

### 3. Append to §6 Decision Log

For every stage-specific decision that emerged during execution (especially deviations from the spec, resolved ambiguities, or empirically locked options), append a row to the **§6 Decision Log** table:

```
| {today} | {decision in one line} | {rationale} | {alternatives considered} |
```

If no new decisions were made during the stage, add a single row of the form `| {today} | {STAGE_ID} closed with no new decisions | — | — |` so the log records that the stage ran cleanly.

### 4. Append to §9 Issues Found During Development

For every unexpected issue that surfaced during the stage (broken assumption, hidden dependency, deferred task), append a row:

```
| {n} | {description} | {root cause} | {resolution or "deferred to {NEXT_STAGE_ID}"} |
```

If no issues were found, leave §9 untouched (do **not** invent placeholder rows).

### 5. Append to §10 Lessons Learned

For every reusable insight from the stage, add a bullet under **§10 Lessons Learned**. These accumulate across stages — the umbrella `project-spec-close` migrates them to canonical IA at the very end. Phrase each lesson so a future maintainer can act on it without re-reading the spec.

### 6. **Optional:** persist a journal entry to Postgres

Call `mcp__territory-ia__project_spec_journal_persist` with:

- `issue_id` = `{ISSUE_ID}`
- `spec_path` = `{SPEC_PATH}`
- `stage_id` = `{STAGE_ID}` (when the tool supports it; otherwise prefix the journal `kind` / `notes` so the entry is searchable)

Acceptable outcomes:

- **`ok`** — entry persisted; note the row id in chat.
- **`db_unconfigured`** — neither `DATABASE_URL` nor `config/postgres-dev.json` resolves; **graceful skip**, state the skip reason in chat. Do not block stage close on this.
- **`db_error`** — log the error in chat. The stage close may still proceed; consider this a soft warning unless the user explicitly waives DB capture.

### 7. Sanity-check the spec is in clean handoff state

Before emitting the handoff message, verify:

- All `- [ ]` checkboxes for the closing stage are now `- [x]` (or have a documented deferral in §9).
- The **Last updated** date matches today.
- §6 / §9 / §10 edits parse cleanly (no broken table rows or stray markdown).
- Internal links in the spec still resolve (relative paths to `BACKLOG.md`, sibling specs, rules, skills).
- No contradictory open questions remain unresolved for the **next** stage's work — if any do, escalate in the handoff message rather than silently passing them on.
- **`.claude/settings.json` still has `permissions.defaultMode: "acceptEdits"` and the `mcp__territory-ia__*` wildcard in `permissions.allow`** — both are canonical project stances (TECH-85 §6 decision rows from 2026-04-10, §9 issue #4, §10 lessons). If a recent edit stripped either one, restore it before handoff so the next stage's agent does not hit per-call approval friction. Verify with: `python3 -c 'import json; d=json.load(open(".claude/settings.json"))["permissions"]; assert d["defaultMode"]=="acceptEdits", d["defaultMode"]; assert "mcp__territory-ia__*" in d["allow"], "wildcard missing"; print("OK")'`.

If anything fails this check, fix it before emitting the handoff message.

### 8. Emit a handoff message

Produce a fenced markdown code block the user can paste **verbatim** into a fresh agent session to start `{NEXT_STAGE_ID}`. The block must include:

- **Issue id** (`{ISSUE_ID}`) and **stage just closed** (`{STAGE_ID}`).
- **Branch state** — current branch name and a one-line summary of what shipped in this stage.
- **Verification summary** — exit codes / outcomes from the Verification block produced per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md). One row per check.
- **Pointer to the spec** — `{SPEC_PATH}` and the explicit instruction "read §5.3, §6 Decision Log, §9 Issues Found, §10 Lessons Learned, and the §7 phases for the **next** stage before doing anything else".
- **Inherited blockers / decisions** — anything the next stage agent needs to know that is not already obvious from the spec (e.g. resolved-but-deferred questions, empirical findings from the stage just closed, environmental gotchas).
- **Hard boundaries** — explicit "do NOT" list for the next stage if relevant (e.g. "do not touch `tools/mcp-ia-server/src/` until Phase 2.3"; "do not strip `permissions.defaultMode: \"acceptEdits\"` from `.claude/settings.json`").
- **Final instruction** — "execute every phase of `{NEXT_STAGE_ID}` in order, then invoke `project-stage-close` skill to close the stage and produce the next handoff."

### Handoff message template

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

Replace placeholders in `{curly braces}` before pasting.

## Output expected from this skill (single chat message)

When you finish running this skill, your message to the user should contain, in order:

1. A short bullet list of edits made to the spec (which sections, how many lines).
2. The journal persistence outcome (`ok` / `db_unconfigured` / `db_error` + note).
3. The sanity-check summary (one line per item from step 7).
4. The handoff message in a fenced markdown code block, ready for the user to copy.

## Failure modes and how to handle them

| Failure | Handling |
|---|---|
| Cannot edit the spec (file locked, missing) | Report the error; do not invent a handoff. The stage is not closed until the spec is updated. |
| Verification was not produced before invoking this skill | Stop. Run verification first per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md), then re-invoke this skill. |
| Postgres journal returns `db_error` | Report the error; proceed with steps 7–8 unless the user explicitly says otherwise. The stage close is not blocked on optional journal capture. |
| §6 / §9 / §10 tables are malformed after edit | Re-read the file, locate the broken rows, fix them. Do not emit the handoff until the markdown parses cleanly. |
| The closing stage left unticked phases that are **not** intentional deferrals | Stop. Either complete those phases or document them as deferrals in §9 Issues Found before proceeding. |

## Conventions

- **No invented dates.** Use the host-injected `currentDate` exclusively.
- **No BACKLOG / archive edits.** Those are exclusive to the umbrella `project-spec-close` skill.
- **No spec deletion.** Same — exclusive to the umbrella close.
- **English only**, per project rule (`ia/rules/coding-conventions.md`).
- **Thin body.** This skill orchestrates; canonical detail lives in §5.3 of whatever spec is being closed.
