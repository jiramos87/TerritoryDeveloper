---
purpose: "Sonnet pair-tail: reads /project-new command args directly; reserves id + writes yaml + spec stub; runs materialize + validate:dead-project-specs; hands off to plan-author at N=1."
audience: agent
loaded_by: skill:project-new-apply
slices_via: none
name: project-new-apply
description: >
  Sonnet pair-tail skill (seam #3). Reads args directly from /project-new command (no
  §Project-New Plan pair-head read). Runs reserve-id.sh, writes ia/backlog/{id}.yaml,
  writes ia/projects/{id}.md stub from project-spec-template, runs materialize-backlog.sh
  + validate:dead-project-specs. Single-issue path — no tuple iteration, no task-table flip.
  Hands off to plan-author at N=1 for spec-body authoring (TECH-478 / T7.11).
  Triggers: "project-new-apply", "/project-new-apply {TITLE} {ISSUE_TYPE} {PRIORITY}",
  "apply project new", "pair-tail project new", "materialize single issue".
  Argument order (explicit): TITLE first, ISSUE_TYPE second, PRIORITY third, NOTES optional.
phases:
  - "Parse args + validate prefix"
  - "Reserve id"
  - "Write ia/backlog/{ISSUE_ID}.yaml"
  - "Write ia/projects/{ISSUE_ID}.md stub"
  - "Post-write: materialize + validate + handoff"
---

# Project-new-apply skill (Sonnet pair-tail)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-tail (seam #3). Reads `/project-new` command args verbatim (no Opus pair-head, no tuple list); reserves id, writes yaml, writes spec stub, materializes + validates. Never authors §1/§2/§4/§5/§7 beyond skeleton — plan-author writes spec body at N=1.

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

## Phase 2 — Reserve id

```bash
bash tools/scripts/reserve-id.sh {PREFIX}
```

- Capture stdout as `ISSUE_ID` (e.g. `TECH-470`).
- Non-zero exit or `flock` timeout → escalate: `{escalation: true, reason: "reserve-id.sh failed: {stderr}", id_counter_path: "ia/state/id-counter.json"}`.
- Invariant #13: `ia/state/id-counter.json` written exclusively via `reserve-id.sh` under `flock`; never hand-edit the counter or the `id:` field of an existing yaml record.
- Idempotency: if `ia/backlog/{ISSUE_ID}.yaml` already exists with matching `title:` → reuse existing id; skip reserve call.

---

## Phase 3 — Write `ia/backlog/{ISSUE_ID}.yaml`

Author yaml body. Required fields:

```yaml
id: "{ISSUE_ID}"
type: "{PREFIX}"                   # e.g. TECH (no dash)
title: "{TITLE}"
priority: "{PRIORITY}"
status: open
section: "Single-issue"
spec: "ia/projects/{ISSUE_ID}.md"
files: []
notes: |
  {NOTES if provided, else empty string}
acceptance: |
  - [ ] Spec authored and implementation complete.
  - [ ] npm run validate:all exit 0.
depends_on: []
depends_on_raw: ""
related: []
created: "{TODAY}"
raw_markdown: |
  {TITLE}
```

Before writing, call `mcp__territory-ia__backlog_record_validate(record: {yaml body})`. Fix any schema errors before disk write. MCP unavailable → skip validate; Phase 5 `validate:dead-project-specs` catches drift.

Write to `ia/backlog/{ISSUE_ID}.yaml`. **Do NOT** edit `BACKLOG.md` directly.

Idempotency: if file exists and `id:` field matches → overwrite with desired final state.

---

## Phase 4 — Write `ia/projects/{ISSUE_ID}.md` stub

Bootstrap from `ia/templates/project-spec-template.md`. Populate:

- Frontmatter: `purpose` (1-line summary), `audience: both`, `loaded_by: ondemand`, `slices_via: none`.
- `> **Issue:** [{ISSUE_ID}](../../BACKLOG.md)` link.
- `> **Status:** Draft`.
- `> **Created:** {TODAY}`.
- `> **Last updated:** {TODAY}`.
- `## 1. Summary` — single skeleton paragraph: `{TITLE} — implementation TBD. Spec body authored by plan-author at N=1.`
- `## 7. Implementation Plan` — placeholder line: `_pending — plan-author writes phases at N=1._`
- Leave `§Plan Author` subsections empty — plan-author writes at N=1.
- Leave all other template sections at their default placeholder text.

Do NOT run `validate:dead-project-specs` here — runs in Phase 5.
Idempotency: overwrite if file exists.

---

## Phase 5 — Post-write: materialize + validate + handoff

1. **Materialize BACKLOG:**
   ```bash
   bash tools/scripts/materialize-backlog.sh
   ```
   Non-zero exit → escalate: `{escalation: true, reason: "materialize-backlog.sh failed: {stderr}"}`.

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
   Next: claude-personal "/ship {ISSUE_ID}"
   ```

   Hard rule: single-issue path always suggests `/ship {ISSUE_ID}` (chain dispatcher = author → implement → verify-loop → code-review → audit → closeout). NEVER suggest `/author --task` standalone — folded into ship chain. Anchor: `feedback_stage_file_next_step.md` user memory.

---

## Escalation rules

Sonnet pair-tail NEVER guesses. Immediate escalation triggers:

| Trigger | Return shape |
|---------|-------------|
| Unknown prefix | `{escalation: true, reason: "unknown prefix: {PREFIX}"}` |
| Invalid priority | `{escalation: true, reason: "invalid priority: {PRIORITY}"}` |
| `reserve-id.sh` non-zero exit | `{escalation: true, reason: "reserve-id.sh failed: {stderr}"}` |
| `materialize-backlog.sh` non-zero | `{escalation: true, reason: "materialize failed: {stderr}"}` |
| `validate:dead-project-specs` non-zero | `{escalation: true, reason: "validator failed: {exit_code} {stderr}"}` |

---

## Hard boundaries

- Do NOT author §1/§2/§4/§5/§7 beyond skeleton — plan-author (TECH-478) writes spec body.
- Do NOT run `validate:all` — only `validate:dead-project-specs` in Phase 5.
- Do NOT edit `BACKLOG.md` directly — materialize-backlog.sh regenerates it.
- Do NOT chain to plan-author — command dispatcher does that in T7.8 / TECH-475.
- Do NOT read `§Project-New Plan` tuples — this skill has no pair-head; reads args verbatim.
- Do NOT update any orchestrator task table — single-issue path has no master-plan row.

---

## Idempotency

- `reserve-id.sh`: detect existing `ia/backlog/{ISSUE_ID}.yaml` with matching `title:` → reuse id; skip reserve call.
- yaml write: overwrite with desired final state — no-op if content matches.
- spec stub write: overwrite — no-op if content matches.

Re-running fully-applied state = exit 0 + zero diff.

---

## §Changelog emitter

## Changelog

### 2026-04-19 — N=1 hard rule for /ship suggestion (F2 dry-run finding sibling)

**Status:** applied (uncommitted on `feature/master-plans-1` — Row 2)

**Symptom:**
M8 dry-run flagged sibling problem in `stage-file-apply`: post-filing handoff suggested wrong dispatcher. Single-issue path needed equivalent rule lock.

**Root cause:**
Pre-fix Phase 5 emitted `/author --task {ISSUE_ID}` standalone. Now folded into `/ship` chain dispatcher (author → implement → verify-loop → code-review → audit → closeout).

**Fix:**
Phase 5 hand-off: `Next: claude-personal "/ship {ISSUE_ID}"`. Hard rule: single-issue path always `/ship` chain; NEVER `/author --task` standalone. Subagent body `.claude/agents/project-new-applier.md` aligned same.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---
