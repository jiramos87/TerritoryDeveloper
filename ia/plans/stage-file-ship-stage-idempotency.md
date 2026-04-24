---
title: Idempotency plan — /stage-file + /ship-stage
author: agent
created: 2026-04-23
status: draft
scope: ia/skills/stage-file, ia/skills/stage-file-plan, ia/skills/stage-file-apply, ia/skills/ship-stage
---

# Idempotency plan — `/stage-file` + `/ship-stage`

## Goal

Re-running `/stage-file` or `/ship-stage` on partial or fully-applied state = **zero diff + exit 0 + fills gaps forward only**. Never overwrites enriched specs. Never duplicates ids. Never re-runs expensive Pass 2 steps already green.

## Summary — what's broken today

| # | Chain | Symptom | Root cause |
|---|-------|---------|------------|
| G1 | stage-file-plan | Re-run regenerates §Stage File Plan with different tuples if `_pending_` rows changed between runs. | Phase 4 blindly writes; no existing-plan detect. |
| G2 | stage-file-apply 3a | Mid-loop crash after `reserve-id.sh` but before yaml write → id orphaned in `id-counter.json`; re-run reserves fresh id → ghost row. | Reserve happens before write; existing-yaml idempotency check runs AFTER reserve (checks `{NEW_ID}.yaml`, always absent). |
| G3 | stage-file-apply 3b/3c | Re-run on spec enriched by plan-author + plan-digest + plan-review **overwrites** §Plan Author / §Plan Digest / §Code Review back to stub. Destroys authored content. | "Overwrite if file exists (write idempotent)" assumes stub-only content. Stub does not equal enriched spec. |
| G4 | stage-file-apply Phase 5 | Atomic task-table flip only at Phase 5 end. Mid-loop crash leaves row `_pending_` even though yaml + spec exist on disk. Next run re-enumerates as pending. | Atomicity chosen for validator red-state avoidance; broke resume. |
| G5 | stage-file dispatcher | No pre-scan of current state before invoking planner. Re-run always routes File mode even when all tuples already applied (= no-op but wastes a planner pass). | Mode detect only counts task statuses; does not detect partial apply state. |
| G6 | ship-stage 2.1 | No-resume or git-scan-miss → dispatches `spec-implementer` on already-implemented code. May re-edit or no-op. | Step 1.6 resume is only hedge; guard absent when scan misses. |
| G7 | ship-stage 2.3 | Commit with no staged diff = non-zero exit; chain stops. | No empty-diff guard. |
| G8 | ship-stage Step 3 | Pass 2 sub-step resume not implemented (skill Open Questions confirms). Re-run pays full verify-loop + code-review + audit cost even when already green. | No persisted per-sub-step marker. `CHAIN_JOURNAL` is in-process only. |
| G9 | ship-stage 3.2 | Re-run appends §Code Review again → spec pollution. | opus-code-reviewer does not detect existing §Code Review. |
| G10 | ship-stage 3.4 | Re-run duplicates §Audit paragraphs. | opus-auditor skips per R11 gate but section duplication not handled. |
| G11 | ship-stage 3.5 | Plan-applier Mode stage-closeout tuple-level idempotent (confirmed). Chain-level re-entry after successful closeout = Step 0 idle exit (all yaml archived). Edge case: closeout partial (half yaml archived) → Step 0 PASS2_ONLY true → re-runs entire Pass 2. | Per-task resume missing across Pass 2. |

---

## Design decisions (fixed choices — no options)

**D1 — Ids reserved at plan time, not apply time.** Planner Phase 3 calls `reserve-id.sh` once per tuple; writes concrete id into `reserved_id` field. Applier never reserves. On applier crash + re-run, id is stable. Eliminates G2 ghost-id class entirely.

**D2 — Planner skips re-planning when §Stage File Plan already present AND task-table matches tuples.** Re-parse existing tuples; verify each `reserved_id` maps 1:1 to task-table row (either `_pending_` or `**{reserved_id}**`); if all match → emit `STAGE_FILE_PLAN_REUSED` and hand off to applier unchanged. Eliminates G1.

**D3 — Applier enrichment guard on `ia/projects/{ID}.md` write.** Before §3c overwrite, scan existing file for ANY of: `## §Plan Author`, `## §Plan Digest`, `## §Code Review`, `## §Audit`, `## §Stage Closeout Plan`, or `Status:` line not containing `Draft`. If any present → skip stub write (file is enriched). Yaml overwrite remains safe (schema-fixed, stable). Eliminates G3.

**D4 — Applier per-tuple task-table flip, not atomic post-loop.** After §3c success for tuple N, immediately edit row `T{STAGE}.{N}`: `_pending_` → `**{reserved_id}**` + `Draft`. Phase 5 becomes pure header-flip reconcile (Stage header `Status`, plan-top `Status`). Validator red-state concern resolved because yaml + spec + row flip happen in same iteration; materialize-backlog runs after loop regardless. Eliminates G4.

**D5 — Dispatcher pre-scan.** `stage-file` Step 1 adds pre-File-mode check: count tuples in existing §Stage File Plan (if any); count rows with filed ids (non-`_pending_`); if tuples count equal rows count AND every tuple.reserved_id appears in table → dispatch applier only (skip planner). Eliminates G5 waste.

**D6 — Disk-backed chain journal.** `ia/state/ship-stage-journal-{slug}-{stage}.json` written incrementally by ship-stage. Schema below (D6.1). Enables Pass 2 sub-step resume. Closes G8.

**D7 — Empty-diff commit guard.** Step 2.3 pre-checks `git diff --cached --quiet && git diff --quiet`; if clean → skip commit, write journal entry `pass1_commit_skipped: true, reason: "no diff (idempotent re-implement)"`. Eliminates G7.

**D8 — Pass 2 sub-step presence check.** Before dispatching verify-loop / code-reviewer / auditor, read D6 journal + spec sections. Skip dispatch when journal records green verdict for same `FIRST_TASK_COMMIT_PARENT..HEAD` cumulative SHA range. Eliminates G8 / G9 / G10.

**D9 — PASS2_ONLY per-task archive detect.** Step 3.5 planner already idempotent; ship-stage Step 0 additionally computes `STAGE_PARTIAL_ARCHIVED = (0 < archived_count < filed_count)`. If true → skip Step 3.1 (verify ran at full-filed state; re-run harmless but redundant) AND Step 3.2 (same) AND Step 3.4 (auditor skipped per R11 when §Audit present); go straight to Step 3.5 to finish closeout. Eliminates G11.

### D6.1 — Chain journal schema

Path: `ia/state/ship-stage-journal-{plan-slug}-stage-{X.Y}.json`. Written via `flock .ship-stage-journal.lock`. Schema:

```json
{
  "schema": 1,
  "master_plan": "ia/projects/{slug}-master-plan.md",
  "stage_id": "1.2",
  "filed_ids": ["TECH-501","TECH-502"],
  "first_task_commit_parent": "<sha>|null",
  "pass1": {
    "tasks": {
      "TECH-501": {
        "commit_sha": "<sha>|null",
        "commit_skipped": false,
        "committed_at": "2026-04-23T..."
      }
    }
  },
  "pass2": {
    "cumulative_range": "<parent-sha>..<head-sha>|null",
    "verify_loop": { "verdict": "pass|fail|null", "at": "..."|null },
    "code_review": { "verdict": "PASS|minor|critical|null", "at": "..."|null },
    "audit": { "completed_at": "..."|null, "tasks_covered": ["..."] },
    "closeout": {
      "planner_emitted_at": "..."|null,
      "applier_tasks_done": ["TECH-501"],
      "applier_completed_at": "..."|null
    }
  },
  "last_updated": "..."
}
```

**Journal invariants:**
- Written AFTER each successful sub-step (verify pass, code-review PASS/minor, audit done, closeout-applier tuple batch done).
- `cumulative_range` invalidates journal sections if HEAD differs from recorded (new commits since last run) — treat as fresh Pass 2.
- Delete journal on successful chain close (Step 3.5 full completion) OR leave for audit trail and gate on `closeout.applier_completed_at != null`.

---

## Mechanical change list (one anchor per entry)

### M1 — stage-file-plan Phase 3: pre-reserve ids

**File:** `ia/skills/stage-file-plan/SKILL.md`
**Anchor:** `## Phase 3 — Emit §Stage File Plan tuples` heading.
**Operation:** replace_section body.
**Payload (delta):**
- Insert new bullet before tuple shape: "Call `bash tools/scripts/reserve-id.sh {ISSUE_PREFIX}` exactly `len(pending_tasks)` times; collect into `reserved_ids[]` in task-table order. Each tuple `reserved_id` field populated with `reserved_ids[N]` verbatim."
- Update tuple shape comment: `reserved_id: "{TECH-XYZ}"           # concrete id — planner reserved at Phase 3`.
- Add bullet after tuple list: "Planner holds lock (`flock .id-counter.lock`) over the entire reserve batch — atomic."

### M2 — stage-file-plan Phase 4: idempotent write + reuse detect

**File:** `ia/skills/stage-file-plan/SKILL.md`
**Anchor:** `## Phase 4 — Write §Stage File Plan to master plan` heading.
**Operation:** prepend pre-write branch.
**Payload (delta):**
- New sub-phase "Phase 4.0 — Detect existing §Stage File Plan":
  1. Search `ORCHESTRATOR_SPEC` for `### §Stage File Plan` heading under target Stage block.
  2. If absent → proceed to Phase 4.1 (current body = new 4.1).
  3. If present → parse YAML; if `len(existing_tuples) == len(pending_tasks)` AND every `existing_tuple.reserved_id` resolves to a task-table row for this stage (either `_pending_` still or `**{id}**` filed) AND every `existing_tuple.title` matches Phase 1 enumerated title → emit `STAGE_FILE_PLAN_REUSED: {N} tuples valid` and SKIP Phase 4.1 write.
  4. If mismatch → STOP. Return escalation `{escalation: true, reason: "existing §Stage File Plan mismatch (ids/titles drift)", existing_count: X, expected_count: Y}` — human resolves. DO NOT overwrite silently.

### M3 — stage-file-plan Phase 1 mode detect: handle fully-applied Stage

**File:** `ia/skills/stage-file-plan/SKILL.md`
**Anchor:** `## Phase 1 — Read Stage block + cardinality gate` heading, step 2.
**Operation:** insert case in mode classification table.
**Payload (delta):**
- Add row: "**Fully applied** (0 `_pending_` tasks AND §Stage File Plan present AND every tuple.reserved_id filed in table) → emit `STAGE_FILE_APPLIED_NOOP` + exit. Applier not invoked."

### M4 — stage-file-apply Phase 3a: remove reserve-id call

**File:** `ia/skills/stage-file-apply/SKILL.md`
**Anchor:** `### 3a. Reserve id` heading.
**Operation:** replace_section.
**Payload (delta):**
- Rename heading: `### 3a. Read reserved id`.
- Body: "Read `ISSUE_ID = tuple.reserved_id`. Planner reserved during Phase 3 — applier never reserves. Empty `reserved_id` → escalate `{escalation: true, tuple_index: N, reason: "tuple.reserved_id empty (planner contract violation)"}`. Remove all `reserve-id.sh` references."
- Delete escalation row "reserve-id.sh non-zero exit" from Escalation rules table (Phase 5 of same file).

### M5 — stage-file-apply Phase 3b: yaml idempotent guard

**File:** `ia/skills/stage-file-apply/SKILL.md`
**Anchor:** `### 3b. Write ia/backlog/{ISSUE_ID}.yaml` heading.
**Operation:** append bullet before "Write to `ia/backlog/{ISSUE_ID}.yaml`".
**Payload (delta):**
- "If `ia/backlog/{ISSUE_ID}.yaml` exists AND its `title:` + `priority:` + `depends_on:` + `related:` + `notes:` match the tuple-derived values → skip write (zero-diff). Mismatch → overwrite only when tuple is the source of truth (planner-verified)."

### M6 — stage-file-apply Phase 3c: spec enrichment guard

**File:** `ia/skills/stage-file-apply/SKILL.md`
**Anchor:** `### 3c. Write ia/projects/{ISSUE_ID}.md stub` heading.
**Operation:** append bullet before "Do NOT run `validate:dead-project-specs` per-tuple".
**Payload (delta):**
- "**Enrichment guard:** before write, grep spec for headings `## §Plan Author`, `## §Plan Digest`, `## §Code Review`, `## §Audit`, `## §Stage Closeout Plan` AND for top `> **Status:**` line. If ANY enrichment heading present OR Status line contains `In Progress` / `Done` / `In Review` / `Blocked` → SKIP stub write; emit log `stage-file-apply.3c: {ISSUE_ID} enriched — skip stub (preserves authored content)`. Only write stub when file missing OR file contains only template stub markers AND Status is `Draft`."

### M7 — stage-file-apply Phase 3d: per-tuple task-table flip

**File:** `ia/skills/stage-file-apply/SKILL.md`
**Anchor:** `### 3d. Record for post-loop task-table update` heading.
**Operation:** replace_section.
**Payload (delta):**
- Rename heading: `### 3d. Flip task-table row for this tuple (durable progress marker)`.
- Body: "After §3b + §3c succeed for this tuple, edit `ORCHESTRATOR_SPEC` task-table row matching anchor `task_key:T{STAGE_ID}.{N}`: Issue column `_pending_` → `**{ISSUE_ID}**`; Status column `_pending_` → `Draft`. Skip if already flipped (idempotency). Append `{tuple_index, ISSUE_ID, title}` to `filed_tasks[]`. This makes each tuple's commit-boundary durable — applier crash after any tuple leaves the stage in a half-filed but consistent state; re-run reads updated table, plan re-parse sees non-`_pending_` rows, enumeration skips them."

### M8 — stage-file-apply Phase 5: demote to header-flip reconcile

**File:** `ia/skills/stage-file-apply/SKILL.md`
**Anchor:** `## Phase 5 — Update task table + status flips` heading.
**Operation:** replace_section body.
**Payload (delta):**
- Body: "Phase 5 runs AFTER Phase 4 validators exit 0. Task-table rows already flipped per-tuple in §3d — do NOT re-flip. Phase 5 now owns only: (1) Stage header `**Status:**` line flip (R2) — `Draft` or `Planned` → `In Progress`; idempotent on match. (2) Plan-top `> **Status:**` flip (R1) — `Draft` → `In Progress — Step {STEP_N} / Stage {STAGE_ID}`; idempotent if already `In Progress`. (3) `npm run progress` non-blocking."
- Remove "Atomic: update all rows in one edit" clause (obsolete).
- Remove hard boundary "Do NOT update orchestrator task table mid-loop — atomic update after Phase 4 exits 0 only" from § Hard boundaries (contradicts M7).

### M9 — stage-file-apply §Idempotency section refresh

**File:** `ia/skills/stage-file-apply/SKILL.md`
**Anchor:** `## Idempotency` heading.
**Operation:** replace_section.
**Payload (delta):**
- New list:
  - `reserved_id` read-only (planner-reserved); applier never calls `reserve-id.sh`.
  - yaml write: skip when tuple-derived fields match existing.
  - spec stub write: skip when enrichment headings present OR Status != `Draft`.
  - task-table row flip: per-tuple (§3d); idempotent — no-op when row already filed.
  - Stage header + plan-top Status flips: overwrite to desired; no-op if match.
  - `materialize-backlog.sh` + validators: idempotent by design.
- Explicit: "Applier crashed partway + re-run = resume from first `_pending_` row; completed tuples no-op; enriched specs preserved; id counter untouched."

### M10 — stage-file dispatcher pre-scan

**File:** `ia/skills/stage-file/SKILL.md`
**Anchor:** `## Step 1 — Mode detection` heading, mode table.
**Operation:** insert new mode row + pre-check sub-step.
**Payload (delta):**
- Insert row between **File mode** and **No-op**: "**Resume mode** | ≥1 `_pending_` OR §Stage File Plan exists with ≥1 tuple whose `reserved_id` not yet filed | → invoke planner (M2 detects reuse) → applier. Safe default — idempotent re-entry."
- Insert row before **No-op**: "**Applied** | 0 `_pending_` AND §Stage File Plan exists AND all tuples filed | → emit `STAGE_FILE_APPLIED_NOOP`; DO NOT dispatch planner or applier. Route forward to §Plan Author / §Plan Digest / §Plan Review readiness check (existing dispatcher Steps 3–5)."

### M11 — ship-stage Step 0: add STAGE_PARTIAL_ARCHIVED signal

**File:** `ia/skills/ship-stage/SKILL.md`
**Anchor:** `## Step 0 — Parse stage task table` heading, step 9 `Stage tail signal`.
**Operation:** insert after step 9.
**Payload (delta):**
- New step 9b: "`STAGE_PARTIAL_ARCHIVED = (0 < count(id in STAGE_FILED_IDS where ia/backlog/{id}.yaml missing but ia/projects/{id}.md missing OR row Status == Done) < len(STAGE_FILED_IDS))`. When true AND `STAGE_TAIL_INCOMPLETE` true → set `PASS2_CLOSEOUT_ONLY=1`. Step 1.6 § A still fires; Step 3 branch (M14) goes straight to 3.5."

### M12 — ship-stage Step 2.1: idempotent implement guard

**File:** `ia/skills/ship-stage/SKILL.md`
**Anchor:** `### Step 2.1 — Implement` heading.
**Operation:** prepend bullet.
**Payload (delta):**
- Insert before "Dispatch `spec-implementer`": "**Pre-dispatch guard:** read chain journal (D6) for this stage; if `pass1.tasks[{ISSUE_ID}].commit_sha != null` AND that SHA present on first-parent of HEAD → skip dispatch (resume hit; same semantics as Step 1.6 match). Journal-first keeps the gate durable across git-subject drift (squash / amend / reword). Step 1.6 git-scan stays as secondary fallback."

### M13 — ship-stage Step 2.3: empty-diff guard

**File:** `ia/skills/ship-stage/SKILL.md`
**Anchor:** `### Step 2.3 — Atomic Task-level commit` heading.
**Operation:** prepend bullet.
**Payload (delta):**
- Insert before commit message template: "**Empty-diff guard:** run `git diff --cached --quiet && git diff --quiet`; exit 0 → NO commit. Write journal `pass1.tasks[{ISSUE_ID}] = {commit_sha: <HEAD>, commit_skipped: true, committed_at: <now>, reason: 'spec-implementer idempotent no-op'}`. Emit log `PASS1_COMMIT_SKIPPED {ISSUE_ID}`. Continue to Step 2.5. Exit non-zero → proceed to commit as normal (new diff present)."
- Insert after commit message template: "**Post-commit journal write:** capture `git rev-parse HEAD`; write `pass1.tasks[{ISSUE_ID}] = {commit_sha: <sha>, commit_skipped: false, committed_at: <now>}`."

### M14 — ship-stage Step 3: journal-driven sub-step skip

**File:** `ia/skills/ship-stage/SKILL.md`
**Anchor:** `## Step 3 — Pass 2: Stage-end bulk` heading.
**Operation:** prepend new sub-phase + insert skip guards in 3.1 / 3.2 / 3.4.
**Payload (delta):**
- New Step 3.0 before 3.1: "**Journal reconcile.** Read `ia/state/ship-stage-journal-{slug}-stage-{X.Y}.json`. Compute `CURRENT_RANGE = {FIRST_TASK_COMMIT_PARENT}..HEAD`. If `journal.pass2.cumulative_range != CURRENT_RANGE` → clear `pass2.{verify_loop, code_review, audit}` (stale). Writes freshened journal back. Closeout state retained across range changes (per-task archive is durable regardless of range)."
- Step 3.1 prepend: "**Skip when** `journal.pass2.verify_loop.verdict == 'pass'` AND `journal.pass2.cumulative_range == CURRENT_RANGE` → emit `PASS2_VERIFY_RESUMED` + skip dispatch. Post-pass → write `journal.pass2.verify_loop = {verdict: 'pass', at: <now>}`."
- Step 3.2 prepend: "**Skip when** `journal.pass2.code_review.verdict in ('PASS','minor')` AND range matches → skip. Post-verdict → write `journal.pass2.code_review = {verdict, at}`. Critical verdict clears this field on next re-run (per contract)."
- Step 3.4 prepend: "**Skip when** `journal.pass2.audit.completed_at != null` AND `journal.pass2.audit.tasks_covered == STAGE_FILED_IDS` AND every spec has populated `## §Audit` heading → skip. Post-completion → write journal."
- Step 3.5 prepend: "**Skip when** `journal.pass2.closeout.applier_completed_at != null` AND all `STAGE_FILED_IDS` archived (yaml absent AND spec absent). Partial closeout (`applier_tasks_done != STAGE_FILED_IDS`) → re-dispatch `stage-closeout-planner` (idempotent) → `plan-applier` Mode stage-closeout (per-tuple idempotent; already documented). Update `closeout.applier_tasks_done` after each archive success; final `closeout.applier_completed_at` on validator exit 0."

### M15 — ship-stage Step 5: journal cleanup

**File:** `ia/skills/ship-stage/SKILL.md`
**Anchor:** `## Step 5 — Next-stage resolver` heading.
**Operation:** append bullet.
**Payload (delta):**
- "After `SHIP_STAGE {STAGE_ID}: PASSED` emitted (Step 3.5 applier completed): delete `ia/state/ship-stage-journal-{slug}-stage-{X.Y}.json`. Failure to delete is non-fatal — next run sees stale journal but M14 Step 3.0 range check invalidates stale sub-steps."

### M16 — ship-stage Hard boundaries

**File:** `ia/skills/ship-stage/SKILL.md`
**Anchor:** `## Hard boundaries` heading.
**Operation:** append bullets.
**Payload (delta):**
- "**Journal-first resume (D6):** Step 1.6 git-scan is a fallback; journal `pass1.tasks[{id}].commit_sha` is authoritative when present. Do NOT re-dispatch `spec-implementer` when journal shows commit present on HEAD."
- "**Pass 2 sub-step skip:** when journal records green verdict for current cumulative range, SKIP dispatch — do NOT re-run verify-loop / code-review / audit for cost reasons alone. Re-run only when range changed or verdict missing."
- "**Closeout is per-task resumable:** `plan-applier` Mode stage-closeout already tuple-idempotent; journal `pass2.closeout.applier_tasks_done` tracks archived set across retries."

### M17 — ship-stage §Open Questions: mark TECH-493 subsumed

**File:** `ia/skills/ship-stage/SKILL.md`
**Anchor:** `## Open Questions` heading.
**Operation:** replace_section.
**Payload (delta):**
- First bullet rewritten: "~~Crash-survivable `{CHAIN_JOURNAL}`~~ — implemented via D6 disk journal (this plan). [TECH-493](../../projects/TECH-493.md) retained for any future multi-chain orchestration; single-stage crash-survive closed."
- Second bullet rewritten: "~~Pass 2 sub-step resume~~ — implemented via D6 + M14. Bridge-heavy Stages no longer pay full Pass 2 re-cost on re-entry."

### M18 — Changelog entries

**Files:** both `ia/skills/stage-file-apply/SKILL.md` + `ia/skills/ship-stage/SKILL.md`.
**Anchor:** `## Changelog` heading.
**Operation:** insert new dated entry at top.
**Payload (delta):**
- stage-file-apply: "### 2026-04-23 — Idempotent re-entry hardening (plan A); Status: applied; Symptom: mid-loop crash or enriched-spec re-run → id orphaning + stub overwrite destroying authored content. Fix: planner pre-reserves ids (M1); applier enrichment guard (M6); per-tuple task-table flip (M7); §Idempotency refresh (M9)."
- ship-stage: "### 2026-04-23 — Disk journal + Pass 2 sub-step resume; Status: applied; Symptom: re-run after partial Pass 2 re-paid verify + code-review + audit cost; commit failed on empty diff. Fix: `ia/state/ship-stage-journal-*.json` (D6); empty-diff guard (M13); journal-driven skip (M14); partial closeout resume (M11 + M14 Step 3.5)."

---

## Out-of-scope (tracked, not in this plan)

- **Plan-author / plan-digest / plan-review idempotency.** Chain tail of `/stage-file`; deserve separate plan. Current behavior: write-section; re-run may overwrite. Enrichment guard M6 protects against stub reverts during stage-file re-run, not against plan-author re-dispatch.
- **opus-code-reviewer / opus-auditor section-dedup.** Side-stepped by M14 skip — re-run gated at orchestrator. If operator invokes `/code-review` or `/audit` standalone, duplicate sections possible.
- **spec-implementer own idempotency.** M12 journal guard sidesteps at orchestrator level; spec-implementer internal idempotency untouched.

---

## Verification plan (post-apply)

1. `npm run validate:all` green.
2. Dry-run `/stage-file` twice on same Stage with 3 `_pending_` rows — second run emits `STAGE_FILE_PLAN_REUSED` then `STAGE_FILE_APPLIED_NOOP`; zero diff beyond log lines.
3. Dry-run `/stage-file` → kill applier after tuple 2 → re-run. Expect: tuples 1+2 no-op (rows flipped); tuple 3 fires; id counter unchanged from Run 1 reserve batch.
4. Dry-run `/stage-file` → let complete → manually author spec for TECH-xxx → re-run `/stage-file` on same Stage. Expect: `STAGE_FILE_APPLIED_NOOP`; enriched spec intact.
5. Dry-run `/ship-stage` Pass 1 + Pass 2 verify green → kill mid-audit → re-run. Expect: `PASS2_VERIFY_RESUMED` + `PASS2_CODE_REVIEW_RESUMED` log lines; audit re-dispatched; closeout fires.
6. Dry-run `/ship-stage` full green → re-run immediately. Expect: Step 0 idle exit (yaml archived) OR Step 1.6 resume + journal reports everything done + Step 5 cleanup reports journal absent (already deleted).

---

## Next action

Review plan → greenlight M1..M18 → apply in order (M1→M10 under `/stage-file` scope; M11→M18 under `/ship-stage` scope). Each M is a single anchored edit; no cross-file coupling beyond D6 journal schema. Suggest filing as `TECH-xxx` project spec with this plan as Implementation Plan body, then `/implement`.
