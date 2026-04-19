---
purpose: "Sonnet pair-tail: reads §Stage File Plan tuples from master plan Stage block; reserves ids + writes yaml + spec stubs + task-table rows; runs materialize + validators; escalates on error."
audience: agent
loaded_by: skill:stage-file-apply
slices_via: none
name: stage-file-apply
description: >
  Sonnet pair-tail skill (seam #2). Reads §Stage File Plan tuple list emitted by
  stage-file-plan (Opus pair-head) from the master plan Stage block. For each tuple:
  runs reserve-id.sh, writes ia/backlog/{id}.yaml, writes ia/projects/{id}.md stub
  from project-spec-template, updates master-plan task-table row. After loop: runs
  materialize-backlog.sh once + validate:dead-project-specs + validate:backlog-yaml.
  Skips all Depends-on dep re-query (planner verified). Escalates on flock failure,
  anchor miss, or non-zero validator exit. Idempotent: re-run on partial state = zero diff.
  Triggers: "stage-file-apply", "/stage-file-apply {ORCHESTRATOR_SPEC} {STAGE_ID}",
  "apply stage file plan", "pair-tail stage file", "materialize stage tuples".
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
phases:
  - "Read §Stage File Plan"
  - "Resolve anchors"
  - "Apply tuples (iterator)"
  - "Post-loop: materialize + validate"
  - "Update task table + status flips"
  - "Return"
---

# Stage-file-apply skill (Sonnet pair-tail)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-tail (seam #2). Reads `§Stage File Plan` tuples written by `stage-file-plan` (Opus pair-head); for each tuple: reserves id, writes yaml, writes spec stub, appends task-table update; after loop: materializes + validates. Never re-queries MCP for Depends-on (planner verified). Never reorders, merges, or interprets tuples.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #2, §Validation gate, §Escalation rule, §Idempotency requirement.
Sibling pair-head: [`stage-file-plan/SKILL.md`](../stage-file-plan/SKILL.md).

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ORCHESTRATOR_SPEC` | 1st arg | Repo-relative path to master plan. |
| `STAGE_ID` | 2nd arg | e.g. `7.2` or `Stage 7.2`. |
| `ISSUE_PREFIX` | 3rd arg or default | `TECH-` / `FEAT-` / `BUG-` / `ART-` / `AUDIO-` — default `TECH-`. |

---

## Phase 1 — Read `§Stage File Plan`

1. Open `ORCHESTRATOR_SPEC`. Locate `#### Stage {STAGE_ID}` block.
2. Find `### §Stage File Plan` subsection within Stage block. If absent → escalate: `{escalation: true, reason: "§Stage File Plan section not found in Stage {STAGE_ID}", tuple_index: null}`.
3. Parse YAML tuple list under `### §Stage File Plan`. Load into ordered array `tuples[]`.
4. Validate each tuple has all required keys: `reserved_id`, `title`, `priority`, `notes`, `depends_on`, `related`, `stub_body`. Missing key → escalate: `{escalation: true, tuple_index: N, reason: "missing key {KEY}"}`.
5. Verify `stub_body` has sub-fields: `summary`, `goals`, `systems_map`, `impl_plan_sketch`. Missing sub-field → escalate.

---

## Phase 2 — Resolve anchors

For each tuple in `tuples[]`:

1. Compute task-table anchor: `task_key:T{STAGE_ID}.{N}` (N = 1-based tuple index = task row position in stage).
2. Open `ORCHESTRATOR_SPEC`. Confirm anchor resolves to exactly one task-table row.
   - Zero matches → escalate: `{escalation: true, tuple_index: N, reason: "anchor task_key:T{STAGE_ID}.{N} not found", candidate_matches: []}`.
   - Multiple matches → escalate: `{escalation: true, tuple_index: N, reason: "anchor task_key:T{STAGE_ID}.{N} matches {K} rows", candidate_matches: [...]}`.
3. Confirm task row Status = `_pending_`. Status other than `_pending_` → skip tuple (idempotency: already filed or active).

---

## Phase 3 — Apply tuples (iterator)

Process tuples in declared order (T{STAGE}.1, T{STAGE}.2, …). For each non-skipped tuple:

### 3a. Reserve id

```bash
bash tools/scripts/reserve-id.sh {ISSUE_PREFIX}
```

- Capture stdout as `ISSUE_ID` (e.g. `TECH-469`).
- Non-zero exit or `flock` timeout → escalate: `{escalation: true, tuple_index: N, reason: "reserve-id.sh failed: {stderr}", id_counter_path: "ia/state/id-counter.json"}`.
- Idempotency: if `ia/backlog/{ISSUE_ID}.yaml` already exists with matching title → skip reserve + reuse existing id (zero-diff re-run path).

### 3b. Write `ia/backlog/{ISSUE_ID}.yaml`

Author yaml body. Required fields:

```yaml
id: "{ISSUE_ID}"
type: "{ISSUE_PREFIX stripped of dash}"   # e.g. TECH
title: "{tuple.title}"
priority: "{tuple.priority}"
status: open
section: "{Stage Objectives short label}"
spec: "ia/projects/{ISSUE_ID}.md"
files: []
notes: |
  {tuple.notes}
acceptance: |
  - [ ] {derived from tuple.stub_body.goals — one item per goal bullet}
depends_on: {tuple.depends_on}           # list; empty [] if none; planner-verified — no re-query
depends_on_raw: "{raw dep string from master plan task Intent column, if any}"
related: {tuple.related}                 # list; may include sibling ids once all reserved
created: "{YYYY-MM-DD}"
raw_markdown: |
  {tuple.title} — {tuple.notes first line}
```

Before writing, call `mcp__territory-ia__backlog_record_validate(record: {yaml body})`. Fix any schema errors before disk write. MCP unavailable → skip validate; end-of-stage `validate:backlog-yaml` catches drift.

Write to `ia/backlog/{ISSUE_ID}.yaml`. **Do NOT** edit `BACKLOG.md` directly.

Idempotency: if file exists and `id:` field matches → overwrite with desired final state (write idempotent).

### 3c. Write `ia/projects/{ISSUE_ID}.md` stub

Bootstrap from `ia/templates/project-spec-template.md`. Populate:

- Frontmatter: `purpose`, `parent_plan`, `task_key` (= `T{STAGE_ID}.{N}`).
- `## 1. Summary` — from `tuple.stub_body.summary`.
- `## 2. Goals / 2.1 Goals` — from `tuple.stub_body.goals`.
- `## 4. Current State / 4.2 Systems map` — from `tuple.stub_body.systems_map`.
- `## 7. Implementation Plan` — from `tuple.stub_body.impl_plan_sketch`.
- `> **Status:** Draft` header line.
- `> **Issue:** [{ISSUE_ID}](../../BACKLOG.md)` link.
- `> **Created:** {YYYY-MM-DD}` / `> **Last updated:** {YYYY-MM-DD}`.

Do NOT run `validate:dead-project-specs` per-tuple — runs once in Phase 4.
Idempotency: overwrite if file exists (write idempotent).

### 3d. Record for post-loop task-table update

Append `{tuple_index, ISSUE_ID, title}` to `filed_tasks[]`. Used in Phase 5.

---

## Phase 4 — Post-loop: materialize + validate

Run after all tuples processed (regardless of skip count).

1. **Materialize BACKLOG:**
   ```bash
   bash tools/scripts/materialize-backlog.sh
   ```
   Non-zero exit → escalate: `{escalation: true, reason: "materialize-backlog.sh failed: {stderr}"}`.

2. **Validate:**
   ```bash
   npm run validate:dead-project-specs
   npm run validate:backlog-yaml
   ```
   Per seam #2 validation gate in `plan-apply-pair-contract.md`.
   Non-zero exit → escalate: `{escalation: true, reason: "validator failed: {exit_code} {stderr}", failing_tuple_index: null}`. Return full stderr to Opus pair-head.

---

## Phase 5 — Update task table + status flips

After Phase 4 exits 0:

1. **Update orchestrator task table** — for each entry in `filed_tasks[]`: replace `_pending_` in Issue column with `**{ISSUE_ID}**`; replace `_pending_` in Status column with `Draft`. Atomic: update all rows in one edit (do NOT update row-by-row mid-loop).

2. **Flip header Status lines** (R1 + R2):
   - **R2 — Stage header:** find `#### Stage {STAGE_ID} — {Title}` block; rewrite `**Status:**` line from `Draft` or `Planned` → `In Progress`. Idempotent if already `In Progress`.
   - **R1 — Plan top Status:** read top-of-file `> **Status:**` line. If equals `Draft` (any variant) → rewrite to `In Progress — Step {STEP_N} / Stage {STAGE_ID}` where `STEP_N` = parent step number. If already contains `In Progress` → leave unchanged.

3. **Regenerate progress dashboard** (non-blocking):
   ```bash
   npm run progress
   ```
   Failure does NOT block Phase 6 — log exit code and continue.

---

## Phase 6 — Return

Emit final report:

```
stage-file-apply done. STAGE_ID={STAGE_ID} FILED={N} SKIPPED={K}
Filed: {ISSUE_ID_1} — {title_1}
       {ISSUE_ID_2} — {title_2}
       ...
Validators: exit 0.
Next: claude-personal "/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"
```

Single-task stage (N=1): suggest `claude-personal "/ship {ISSUE_ID}"` instead.

**Hard rule (T8 Row 2 / dry-run findings):** N≥2 → ALWAYS `/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}`; NEVER `/ship {ISSUE_ID}` for multi-task stages (chain dispatcher = `/ship-stage`). N=1 → ALWAYS `/ship {ISSUE_ID}`. NEVER `/author` standalone — folded into ship chain. Anchor: `feedback_stage_file_next_step.md` user memory.

---

## Escalation rules

Sonnet pair-tail NEVER guesses. Immediate return-to-Opus triggers (per `plan-apply-pair-contract.md`):

| Trigger | Return shape |
|---------|-------------|
| `§Stage File Plan` section missing | `{escalation: true, reason: "section missing", tuple_index: null}` |
| Missing required tuple key | `{escalation: true, tuple_index: N, reason: "missing key {KEY}"}` |
| Anchor matches zero rows | `{escalation: true, tuple_index: N, reason: "anchor not found", candidate_matches: []}` |
| Anchor matches multiple rows | `{escalation: true, tuple_index: N, reason: "anchor ambiguous", candidate_matches: [...]}` |
| `reserve-id.sh` non-zero exit | `{escalation: true, tuple_index: N, reason: "reserve-id.sh failed: {stderr}"}` |
| `materialize-backlog.sh` non-zero | `{escalation: true, reason: "materialize failed: {stderr}"}` |
| Validator non-zero exit | `{escalation: true, reason: "validator failed: {exit_code} {stderr}", failing_tuple_index: null}` |

Opus pair-head receives escalation → revises `§Stage File Plan` → applier re-runs from scratch (idempotency guarantees safety).

---

## Idempotency

- `reserve-id.sh`: detect existing `ia/backlog/{ISSUE_ID}.yaml` with matching `title:` → reuse id; skip reserve call.
- yaml write: overwrite with desired final state — no-op if content matches.
- spec stub write: overwrite — no-op if content matches.
- task-table update: detect row already updated (`Draft` in Status column) → skip.
- Status flips: detect already `In Progress` → no-op.

Re-running fully-applied state = exit 0 + zero diff.

---

## Hard boundaries

- Do NOT re-query `backlog_issue` per Task for Depends-on — planner verified; applier reads from tuple.
- Do NOT reorder tuples — apply in declared order only.
- Do NOT update orchestrator task table mid-loop — atomic update after Phase 4 exits 0 only.
- Do NOT run `validate:all` per tuple — once in Phase 4 only.
- Do NOT edit `BACKLOG.md` directly — materialize-backlog.sh regenerates it.
- Do NOT guess on ambiguous anchor — escalate immediately.
- Do NOT call `domain-context-load` — planner already loaded; applier reads `stub_body` from tuple verbatim.

---

## §Changelog emitter

## Changelog

### 2026-04-19 — Auto-chain boundary locked at applier tail (F1 dry-run finding)

**Status:** applied (uncommitted on `feature/master-plans-1` — Row 3 Option B)

**Symptom:**
M8 dry-run (Stage 8 lifecycle-refactor) — `/stage-file` auto-chained through `/author` then stopped. User opened fresh CLI to run `/plan-review` separately. Half-chained UX = user cannot predict where chain stops; extra context-setup cost on re-entry.

**Root cause:**
Pre-fix `/stage-file` dispatcher invoked `plan-author` after applier tail but did NOT continue to `plan-review`. Two competing auto-chain semantics (here vs `/ship-stage`) created divergent behaviour.

**Fix:**
`/stage-file` STOPS at applier tail. Does NOT auto-chain `/author`. Applier handoff suggests `/ship-stage {plan} Stage {ID}` (N≥2) or `/ship {ID}` (N=1) — chain dispatcher owns author → implement → verify-loop → code-review → audit → closeout. Documented in `ia/rules/agent-lifecycle.md` + `CLAUDE.md` §3 + `.claude/commands/stage-file.md` Step 3.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — N≥2 hard rule for /ship-stage suggestion (F2 dry-run finding)

**Status:** applied (uncommitted on `feature/master-plans-1` — Row 2)

**Symptom:**
M8 dry-run sessions emitted `/ship TECH-485` after filing 4 tasks in Stage 8. Multi-task Stage requires `/ship-stage {plan} {STAGE_ID}`. Wrong suggestion = user has to catch every multi-task Stage; silent miss = single-issue flow runs on Stage-scope work → per-Task Path B thrash + duplicate closeout attempts.

**Root cause:**
Subagent exit hand-off prose did not branch on filed-task count. User-memory `feedback_stage_file_next_step.md` flagged the rule; implementation lagged in skill body + applier subagent prose.

**Fix:**
Phase 6 + Output line N-conditional handoff: N≥2 → `/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}`; N=1 → `/ship {ISSUE_ID}`. Hard rule paragraph added: NEVER `/ship` for N≥2, NEVER `/author` standalone. Subagent body `.claude/agents/stage-file-applier.md` aligned same.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Stage-entry friction: 3 commands across 2 CLI sessions (F6 dry-run finding)

**Status:** pending (deferred — Fix #6 scope discussion required)

**Symptom:**
M8 dry-run user typed: (1) `/stage-file ... Stage 8` (auto-chain to /author); (2) fresh CLI `claude-personal "/plan-review ... Stage 8"`; (3) corrected `claude-personal "/ship-stage ... 8"`. Three commands across 2 CLI sessions for Stage entry.

**Root cause:**
No single Stage-entry surface. `/stage-file` ends at filing; `/plan-review` separate; `/ship-stage` runs per-Task chain after entry.

**Fix:**
pending — candidate `/stage-start {plan} {stage}` orchestrator OR `/ship-stage` front-end extension covering `stage-file → author → plan-review` before per-Task chain. Keeps human gates at author PASS + plan-review PASS. Scope discussion first.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---
