---
purpose: "Sonnet pair-tail: reads §Plan Fix tuples from master plan Stage block; applies verbatim; runs validation gate; escalates on anchor ambiguity."
audience: agent
loaded_by: skill:plan-fix-apply
slices_via: none
name: plan-fix-apply
description: >
  Sonnet pair-tail skill. Reads §Plan Fix tuples written by `plan-review` (Opus pair-head)
  under a master-plan Stage block; applies each tuple verbatim in declared order;
  runs validate:master-plan-status + validate:backlog-yaml after all edits;
  escalates immediately on any anchor ambiguity; idempotent re-runs.
  Triggers: "apply plan fix", "/plan-fix-apply {MASTER_PLAN_PATH} {STAGE_ID}",
  "plan fix apply", "pair-tail plan fix".
phases:
  - "Read §Plan Fix"
  - "Resolve anchors"
  - "Apply tuples"
  - "Validate"
  - "Return"
---

# Plan-fix-apply skill (Sonnet pair-tail)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-tail. Reads `§Plan Fix` tuples emitted by `plan-review` (Opus pair-head); applies each edit verbatim in declared order; runs validation gate; escalates on anchor ambiguity. Never reorders, merges, or interprets tuples.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #1, §Validation gate, §Escalation rule, §Idempotency requirement.
Sibling pair-head: [`plan-review/SKILL.md`](../plan-review/SKILL.md).

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path to master plan (e.g. `ia/projects/lifecycle-refactor-master-plan.md`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.1`). |

---

## Phase 1 — Read §Plan Fix

1. Open `MASTER_PLAN_PATH`. Locate Stage `STAGE_ID` block.
2. Find `### §Plan Fix` subsection within Stage block.
3. Check for PASS sentinel line: `plan-review exit 0 — all Task specs aligned`. If present → exit 0 immediately (nothing to apply).
4. Parse YAML tuple list under `### §Plan Fix`. Load into ordered array `tuples[]`.
5. Validate each tuple has all required keys: `operation`, `target_path`, `target_anchor`, `payload`. Missing key → escalate (see §Escalation rules).

---

## Phase 2 — Resolve anchors

For each tuple in `tuples[]`:

1. Open `target_path`. Verify file exists. Non-`write_file` operation on missing file → escalate.
2. Search file for `target_anchor`:
   - Exact heading text (`## §Audit`) → find matching heading line.
   - Exact line number (`L42`) → verify line count.
   - Glossary row id (`glossary:HeightMap`) → find row in `ia/specs/glossary.md`.
   - Task key (`task_key:T1.2.5`) → find matching row in master-plan Tasks table.
3. **Zero matches** → escalate with `{escalation: true, tuple_index: N, reason: "anchor_not_found", candidate_matches: []}`.
4. **Multiple matches** → escalate with `{escalation: true, tuple_index: N, reason: "anchor_ambiguous", candidate_matches: ["line X: ...", "line Y: ..."]}`.
5. `payload` references glossary term → verify term exists in `ia/specs/glossary.md`. Unknown term → escalate with `{escalation: true, tuple_index: N, reason: "unknown_glossary_term", candidate_matches: []}`.
6. `operation` enum not recognized → escalate with `{escalation: true, tuple_index: N, reason: "unknown_operation"}`.

All tuples resolved cleanly → proceed to Phase 3. Any escalation → STOP, return escalation payload to Opus pair-head.

---

## Phase 3 — Apply tuples

Execute tuples in declared order. One atomic edit per tuple.

### Operation implementations

| Operation | Behavior |
|-----------|----------|
| `replace_section` | Replace content from resolved anchor heading to next same-or-higher heading with `payload`. Idempotency: skip if content already matches `payload`. |
| `insert_after` | Insert `payload` immediately after resolved anchor line. Idempotency: skip if `payload` already present at that location. |
| `insert_before` | Insert `payload` immediately before resolved anchor line. Idempotency: skip if `payload` already present at that location. |
| `append_row` | Append `payload` as new table row after resolved anchor row. Idempotency: skip if identical row already present. |
| `delete_section` | Remove section from anchor heading to next same-or-higher heading. `payload` must be `null`. Idempotency: no-op if section already absent. |
| `set_frontmatter` | Write `payload` key-value into YAML frontmatter block. Idempotency: skip if key already matches value. |
| `archive_record` | Move `target_path` per `payload.source` → `payload.destination`. Idempotency: no-op if already at destination. |
| `delete_file` | Delete `target_path`. `payload` must be `null`. Idempotency: no-op if file already missing. |
| `write_file` | Write `payload` content to `target_path` (create or overwrite). Idempotency: verify content matches `payload` after write. |

After each tuple: log `applied tuple {N}: {operation} → {target_path}`.

---

## Phase 4 — Validate

After all tuples applied, run the seam #1 validation gate (contract §Validation gate):

```sh
npm run validate:master-plan-status
npm run validate:backlog-yaml
```

On non-zero exit from either command:
- STOP immediately.
- Return to Opus pair-head: `{exit_code, stderr, failing_tuple_index: <last applied>}`.
- Opus revises `§Plan Fix`; Sonnet re-applies from scratch (idempotency clause guarantees safety).

On clean exit (both exit 0): proceed to Phase 5.

---

## Phase 5 — Return

Emit apply summary:

```
plan-fix-apply: {N} tuples applied to Stage {STAGE_ID} in {MASTER_PLAN_PATH}.
Validation gate: PASS (validate:master-plan-status + validate:backlog-yaml exit 0).
Returning to plan-review for re-check (optional) or downstream continue.
```

Return control to caller. Caller (agent or dispatcher) routes:
- Tuples applied + gate PASS → proceed to stage Task kickoff.
- Validation failure → already returned `{exit_code, stderr}` to Opus above.

---

## Escalation rules

Sonnet pair-tail **NEVER guesses** an ambiguous anchor. Immediate return to Opus on any of:

| Trigger | Return shape |
|---------|-------------|
| `target_anchor` matches zero locations | `{escalation: true, tuple_index, reason: "anchor_not_found", candidate_matches: []}` |
| `target_anchor` matches multiple locations | `{escalation: true, tuple_index, reason: "anchor_ambiguous", candidate_matches: [...]}` |
| `target_path` missing (non-`write_file`) | `{escalation: true, tuple_index, reason: "target_path_missing"}` |
| `payload` references unknown glossary term | `{escalation: true, tuple_index, reason: "unknown_glossary_term"}` |
| `operation` not in enum | `{escalation: true, tuple_index, reason: "unknown_operation"}` |
| Required tuple key missing | `{escalation: true, tuple_index, reason: "malformed_tuple", missing_keys: [...]}` |

Opus re-resolves; never the applier.

---

## Idempotency requirement

Re-running this skill on partially- or fully-applied `§Plan Fix` exits 0 with zero diff. All operation implementations above include idempotency guards. This unblocks Opus revision loops without rollback machinery.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, §Validation gate, §Escalation rule, §Idempotency requirement.
- [`ia/skills/plan-review/SKILL.md`](../plan-review/SKILL.md) — Opus pair-head.
- Glossary term **plan-fix apply** (`ia/specs/glossary.md`).
