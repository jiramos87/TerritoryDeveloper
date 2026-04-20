---
purpose: "Sonnet pair-tail (seam #4): reads §Code Fix Plan tuples; applies verbatim; re-enters /verify-loop; 1-retry bound; escalates to Opus pair-head on second fail."
audience: agent
loaded_by: skill:code-fix-apply
slices_via: none
name: code-fix-apply
description: >
  Sonnet pair-tail skill (seam #4). Reads §Code Fix Plan tuples written by
  `opus-code-review` (Opus pair-head) in ia/projects/{id}.md; applies each tuple
  verbatim in declared order; re-enters /verify-loop; on verify fail — retries once
  (1-retry bound, 2 total attempts); second fail → escalates to Opus pair-head with
  structured return shape. Never reorders, merges, or interprets tuples.
  Triggers: "apply code fix", "/code-fix-apply {ISSUE_ID}", "code fix apply",
  "pair-tail code fix", "apply §Code Fix Plan".
model: inherit
phases:
  - "Read §Code Fix Plan"
  - "Apply tuples"
  - "Re-enter /verify-loop"
  - "1-retry bound"
  - "Escalate"
---

# Code-fix-apply skill (Sonnet pair-tail — seam #4)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-tail. Reads `§Code Fix Plan` tuples emitted by `opus-code-review` (Opus pair-head); applies each edit verbatim in declared order; re-enters `/verify-loop`; bounded 1 retry on verify failure; second fail → escalates to Opus. Never reorders, merges, or interprets.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #4, §Validation gate, §Escalation rule, §Idempotency requirement.
Sibling pair-head: [`opus-code-review/SKILL.md`](../opus-code-review/SKILL.md).

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ISSUE_ID` | 1st arg | Task issue id (e.g. `TECH-471`). |

---

## Phase 1 — Read §Code Fix Plan

1. Open `ia/projects/{ISSUE_ID}.md`. Locate `## §Code Fix Plan` section.
2. If section absent → escalate: `{escalation: true, reason: "code_fix_plan_missing", issue_id}`. Return to Opus pair-head.
3. Parse YAML tuple list inside `§Code Fix Plan`. Load into ordered array `tuples[]`.
4. Validate each tuple has all required keys: `operation`, `target_path`, `target_anchor`, `payload`. Missing key → escalate: `{escalation: true, tuple_index: N, reason: "malformed_tuple", missing_keys: [...]}`.
5. Idempotency pre-check: if all tuples already applied (each `replace_section` matches current content) → log "already applied, skipping" + proceed directly to Phase 3 verify.

---

## Phase 2 — Apply tuples

Resolve anchors per contract §Escalation rule BEFORE applying any tuple:

- Open each `target_path`. Non-`write_file` on missing file → escalate `{escalation: true, tuple_index, reason: "target_path_missing"}`.
- Search for `target_anchor` in file. Zero matches → escalate `{escalation: true, tuple_index, reason: "anchor_not_found"}`. Multiple matches → escalate `{escalation: true, tuple_index, reason: "anchor_ambiguous", candidate_matches: [...]}`.

All anchors resolved → execute tuples in declared order. One atomic edit per tuple. Log `applied tuple {N}: {operation} → {target_path}` after each.

### Operation implementations

| Operation | Behavior |
|-----------|----------|
| `replace_section` | Replace content from anchor heading to next same-or-higher heading with `payload`. Skip if content already matches. |
| `insert_after` | Insert `payload` immediately after anchor line. Skip if `payload` already present. |
| `insert_before` | Insert `payload` immediately before anchor line. Skip if `payload` already present. |
| `append_row` | Append `payload` as new table row after anchor row. Skip if identical row present. |
| `delete_section` | Remove section from anchor to next same-or-higher heading. `payload` must be `null`. No-op if already absent. |
| `set_frontmatter` | Write `payload` key-value into YAML frontmatter. Skip if already matches. |
| `write_file` | Write `payload` to `target_path` (create or overwrite). Verify after write. |

---

## Phase 3 — Re-enter `/verify-loop`

After all tuples applied, run the seam #4 validation gate per contract:

```sh
npm run verify:local
```

For tooling-only Tasks (no C# changes): use `npm run validate:all` instead (per `feedback_refactor_tooling_only_verify` memory).

On clean exit (exit 0): proceed to Phase 5 (success).

On non-zero exit: record `{exit_code, stderr}` and proceed to Phase 4 (retry).

---

## Phase 4 — 1-retry bound

> **Retry bound = 1 (2 total attempts).** After first verify fail, retry once. Second fail → escalate to Opus pair-head.

**Retry attempt (iteration 2):**

1. Re-read `§Code Fix Plan` from `ia/projects/{ISSUE_ID}.md` (pick up any Opus revision).
2. Re-apply tuples from scratch (idempotency clause guarantees safety).
3. Re-run verify (`npm run verify:local` or `npm run validate:all`).

On clean exit (exit 0): proceed to Phase 5 (success).

On second verify fail: proceed to Phase 5 (escalate).

---

## Phase 5 — Escalate / Return

### Success path

Emit apply summary:

```
code-fix-apply: {N} tuples applied to ia/projects/{ISSUE_ID}.md.
Verify gate: PASS ({validator} exit 0).
Returning to caller (opus-code-review PASS; or chain continues).
```

Return: `{success: true, issue_id, tuples_applied: N, verify_iterations: {1|2}}`.

### Escalation path (second verify fail)

STOP. Return to Opus pair-head:

```json
{
  "escalation": true,
  "issue_id": "{ISSUE_ID}",
  "reason": "verify_gate_failed_after_retry",
  "failing_iteration": 2,
  "exit_code": {N},
  "stderr": "..."
}
```

Opus pair-head re-reads changed files, revises `§Code Fix Plan`, re-spawns `code-fix-apply`. Sonnet pair-tail re-applies from scratch (idempotency clause guarantees safety).

---

## Idempotency requirement

Re-running on partially- or fully-applied `§Code Fix Plan` exits 0 with zero diff. Phase 1 idempotency pre-check + per-operation skip guards ensure safe re-runs without rollback machinery.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #4, §Validation gate, §Escalation rule, §Idempotency requirement.
- [`ia/skills/opus-code-review/SKILL.md`](../opus-code-review/SKILL.md) — Opus pair-head.
- [`ia/skills/verify-loop/SKILL.md`](../verify-loop/SKILL.md) — downstream verify consumer re-entered in Phase 3.
- Glossary term **code-fix apply** (`ia/specs/glossary.md`).
