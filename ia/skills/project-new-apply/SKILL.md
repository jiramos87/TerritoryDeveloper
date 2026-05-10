---
name: project-new-apply
purpose: >-
  Sonnet pair-tail: reads /project-new command args directly; calls task_insert MCP (DB-backed, no yaml);
  task_spec_section_write spec stub; enqueues cron_materialize_backlog_enqueue + validate:dead-project-specs; hands off to stage-authoring at N=1.
audience: agent
loaded_by: "skill:project-new-apply"
slices_via: none
description: >-
  Sonnet pair-tail skill. Reads args directly from /project-new command (no §Project-New Plan
  pair-head read). DB-backed: calls task_insert MCP (no reserve-id.sh, no yaml write), then
  task_spec_section_write to author spec stub body. No filesystem writes to ia/backlog/ or
  ia/projects/. Single-issue path — no tuple iteration, no task-table flip. Hands off to
  stage-authoring at N=1 for spec-body authoring. Triggers: "project-new-apply",
  "/project-new-apply {TITLE} {ISSUE_TYPE} {PRIORITY}", "apply project new",
  "pair-tail project new", "materialize single issue". Argument order (explicit):
  TITLE first, ISSUE_TYPE second, PRIORITY third, NOTES optional.
phases:
  - Parse args + validate prefix
  - task_insert MCP (reserve id + DB row)
  - task_spec_section_write spec stub body
  - "Post-write: materialize + validate + handoff"
triggers:
  - project-new-apply
  - /project-new-apply {TITLE} {ISSUE_TYPE} {PRIORITY}
  - apply project new
  - pair-tail project new
  - materialize single issue
model: inherit
tools_role: custom
tools_extra:
  - mcp__territory-ia__cron_materialize_backlog_enqueue
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Project-new-apply skill (Sonnet pair-tail)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-tail. Reads `/project-new` command args verbatim (no Opus pair-head, no tuple list); calls `task_insert` MCP (no yaml, no filesystem spec stub), writes initial spec stub body via `task_spec_section_write`, validates. Never authors §1/§2/§4/§5/§7 beyond skeleton — stage-authoring writes spec body at N=1. DB-backed: no `reserve-id.sh`, no `ia/backlog/*.yaml`, no `ia/projects/*.md` writes.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Validation gate, §Escalation rule, §Idempotency requirement.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `TITLE` | 1st arg | Issue title string. |
| `ISSUE_TYPE` | 2nd arg | Prefix with or without dash: `BUG-` / `FEAT-` / `TECH-` / `ART-` / `AUDIO-`. |
| `PRIORITY` | 3rd arg | `P1` / `P2` / `P3` / `P4`. |
| `NOTES` | Optional free-text | Extra context for yaml `notes:` field. |

---

## Phase 1 — Parse args + validate prefix

1. Extract `TITLE`, `ISSUE_TYPE`, `PRIORITY`, `NOTES` from command args.
2. Normalize `ISSUE_TYPE`: strip trailing dash if present → `PREFIX` (e.g. `TECH-` → `TECH`); add dash for id format → `PREFIX-`.
3. Validate `PREFIX` ∈ `{BUG, FEAT, TECH, ART, AUDIO}`. Invalid → escalate: `{escalation: true, reason: "unknown prefix: {PREFIX}", valid_prefixes: ["BUG","FEAT","TECH","ART","AUDIO"]}`.
4. Validate `PRIORITY` ∈ `{P1, P2, P3, P4}`. Invalid → escalate: `{escalation: true, reason: "invalid priority: {PRIORITY}"}`.
5. Set `TODAY` = current date `YYYY-MM-DD`.

---

## Phase 2 — task_insert MCP (reserve id + DB row)

Call `mcp__territory-ia__task_insert`:

```json
{
  "slug": null,
  "stage_id": null,
  "title": "{TITLE}",
  "type": "{PREFIX}",
  "priority": "{PRIORITY}",
  "notes": "{NOTES if provided, else empty string}",
  "depends_on": [],
  "related": []
}
```

- Response carries reserved `task_id` (e.g. `TECH-27601`) from DB sequence.
- MCP error → escalate: `{escalation: true, reason: "task_insert failed: {error}"}`.
- Idempotency: if task with matching title already exists in DB → use existing `task_id`; skip insert.
- **No `reserve-id.sh`, no yaml write, no filesystem ops.**

---

## Phase 3 — Write spec stub body via task_spec_section_write

Call `mcp__territory-ia__task_spec_section_write`:

```json
{
  "task_id": "{ISSUE_ID}",
  "section": "Goal",
  "content": "## §Goal\n\n{TITLE} — implementation TBD. Spec body authored by stage-authoring at N=1.\n\n**Status:** Draft\n**Created:** {TODAY}"
}
```

- Writes `§Goal` section to `ia_tasks.body` in DB (no filesystem file).
- MCP error → escalate: `{escalation: true, reason: "task_spec_section_write failed: {error}"}`.
- **No `ia/projects/{ISSUE_ID}.md` write.**
- `## 7. Implementation Plan` — placeholder line: `_pending — stage-authoring writes phases at N=1._`
- Leave `§Plan Digest` at template default — `stage-authoring` fills executable digest at N=1.
- Leave all other template sections at their default placeholder text.

Do NOT run `validate:dead-project-specs` here — runs in Phase 5.
Idempotency: overwrite if file exists.

---

## Phase 4 — Post-write: materialize + validate + handoff

1. **Materialize BACKLOG (async enqueue):**
   Call `mcp__territory-ia__cron_materialize_backlog_enqueue({triggered_by: "project-new-apply"})`.
   Returns `{job_id, status:'queued'}` in <100ms — cron supervisor drains within ~2 min.
   MCP unavailable → fallback: `bash tools/scripts/materialize-backlog.sh` (sync).
   Non-zero exit on fallback → escalate: `{escalation: true, reason: "materialize-backlog.sh failed: {stderr}"}`.

2. **Validate:**
   ```bash
   npm run validate:dead-project-specs
   ```
   Non-zero exit → escalate: `{escalation: true, reason: "validate:dead-project-specs failed: {exit_code} {stderr}"}`.

3. **Emit handoff line:**
   ```
   project-new-apply done. ISSUE_ID={ISSUE_ID}
   Filed: {ISSUE_ID} — {TITLE}
   Validators: exit 0.
   Next: claude-personal "/stage-authoring --task {ISSUE_ID}"
   Then: claude-personal "/ship {ISSUE_ID}"
   ```

   Hard rule: `stage-authoring` **before** `/ship` (populate `§Plan Digest`); `/ship` does not run authoring. Anchor: `docs/agent-lifecycle.md` (single-task path after `/project-new`).

---

## Escalation rules

Sonnet pair-tail NEVER guesses. Immediate escalation triggers:

| Trigger | Return shape |
|---------|-------------|
| Unknown prefix | `{escalation: true, reason: "unknown prefix: {PREFIX}"}` |
| Invalid priority | `{escalation: true, reason: "invalid priority: {PRIORITY}"}` |
| `reserve-id.sh` non-zero exit | `{escalation: true, reason: "reserve-id.sh failed: {stderr}"}` |
| `cron_materialize_backlog_enqueue` MCP error | `{escalation: true, reason: "enqueue failed: {stderr}"}` |
| `materialize-backlog.sh` non-zero (fallback) | `{escalation: true, reason: "materialize failed: {stderr}"}` |
| `validate:dead-project-specs` non-zero | `{escalation: true, reason: "validator failed: {exit_code} {stderr}"}` |

---

## Hard boundaries

- Do NOT author §1/§2/§4/§5/§7 beyond skeleton — stage-authoring writes spec body.
- Do NOT run `validate:all` — only `validate:dead-project-specs` in Phase 5.
- Do NOT edit `BACKLOG.md` directly — materialize-backlog.sh regenerates it.
- Do NOT auto-invoke stage-authoring — applier stops at tail; handoff points user to `/stage-authoring --task` then `/ship`.
- Do NOT read `§Project-New Plan` tuples — this skill has no pair-head; reads args verbatim.
- Do NOT update any orchestrator task table — single-issue path has no master-plan row.

---

## Idempotency

- `reserve-id.sh`: detect existing `ia/backlog/{ISSUE_ID}.yaml` with matching `title:` → reuse id; skip reserve call.
- yaml write: overwrite with desired final state — no-op if content matches.
- spec stub write: overwrite — no-op if content matches.

Re-running fully-applied state = exit 0 + zero diff.

---

