---
purpose: "Sonnet pair-tail: applies §Plan Fix tuples verbatim per plan-apply-pair-contract."
audience: agent
loaded_by: skill:plan-applier
slices_via: none
name: plan-applier
description: >
  Sonnet literal-applier for §Plan Fix tuples emitted by the plan-review pair-head.
  Validation gate: validate:master-plan-status + validate:backlog-yaml.
  Single mode — plan-fix only.
  Triggers: "/plan-fix-apply", "plan-applier", "apply §Plan Fix tuples".
model: inherit
phases:
  - "Read §Plan Fix"
  - "Resolve anchors"
  - "Apply tuples"
  - "Validate"
  - "Return"
---

# Plan-applier — Sonnet pair-tail

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-tail for the Plan-Apply pair. Reads `§Plan Fix` tuples emitted by the plan-review pair-head; applies each edit verbatim in declared order; runs validation gate; escalates on anchor ambiguity. Never reorders, merges, or interprets.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, §Validation gate, §Escalation rule, §Idempotency requirement.
Sibling pair-heads: [`plan-review-mechanical/SKILL.md`](../plan-review-mechanical/SKILL.md) + [`plan-review-semantic/SKILL.md`](../plan-review-semantic/SKILL.md).

**Progress stderr:** use skill name `plan-applier` and phase labels from frontmatter `phases:` array.

**MCP `caller_agent`:** pass `plan-applier` when mutating backlog or MCP tools — see pair-contract + `tools/mcp-ia-server` caller allowlist.

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

All tuples resolved cleanly → proceed to Phase 3. Any escalation → STOP, return escalation payload to pair-head.

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

After all tuples applied, run the validation gate (contract §Validation gate):

```sh
npm run validate:master-plan-status
npm run validate:backlog-yaml
```

On non-zero exit from either command:
- STOP immediately.
- Return to pair-head: `{exit_code, stderr, failing_tuple_index: <last applied>}`.
- Pair-head revises `§Plan Fix`; pair-tail re-applies from scratch (idempotency clause guarantees safety).

On clean exit (both exit 0): proceed to Phase 5.

---

## Phase 5 — Return

Emit apply summary:

```
plan-applier (plan-fix): {N} tuples applied to Stage {STAGE_ID} in {MASTER_PLAN_PATH}.
Validation gate: PASS (validate:master-plan-status + validate:backlog-yaml exit 0).
Returning to caller for re-check (optional) or downstream continue.
```

Return control to caller. Caller (agent or dispatcher) routes:
- Tuples applied + gate PASS → proceed to stage Task kickoff.
- Validation failure → already returned `{exit_code, stderr}` to Opus above.

---

## Escalation rules

Sonnet pair-tail **NEVER guesses** an ambiguous anchor. Immediate return to pair-head on any of:

| Trigger | Return shape |
|---------|-------------|
| `target_anchor` matches zero locations | `{escalation: true, tuple_index, reason: "anchor_not_found", candidate_matches: []}` |
| `target_anchor` matches multiple locations | `{escalation: true, tuple_index, reason: "anchor_ambiguous", candidate_matches: [...]}` |
| `target_path` missing (non-`write_file`) | `{escalation: true, tuple_index, reason: "target_path_missing"}` |
| `payload` references unknown glossary term | `{escalation: true, tuple_index, reason: "unknown_glossary_term"}` |
| `operation` not in enum | `{escalation: true, tuple_index, reason: "unknown_operation"}` |
| Required tuple key missing | `{escalation: true, tuple_index, reason: "malformed_tuple", missing_keys: [...]}` |

Pair-head re-resolves; never the applier.

---

## Idempotency requirement

Re-running this skill on partially- or fully-applied `§Plan Fix` exits 0 with zero diff. All operation implementations above include idempotency guards. This unblocks pair-head revision loops without rollback machinery.

---

## Hard boundaries

- Do NOT re-query MCP for anchor resolution — planner resolved every anchor; tuples authoritative.
- Do NOT reorder tuples — declared order only.
- Do NOT interpret / merge / collapse tuples.
- Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
- Do NOT write normative spec prose — only mutations from tuple payloads.
- Do NOT re-introduce `code-fix` or `stage-closeout` modes. `opus-code-reviewer` applies fixes inline via direct Edit/Write; `ship-stage` runs closeout inline via `stage_closeout_apply` MCP tool.
- Do NOT `git commit` — user decides.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, §Validation gate, §Escalation rule, §Idempotency requirement.
- [`ia/skills/plan-review-mechanical/SKILL.md`](../plan-review-mechanical/SKILL.md) + [`ia/skills/plan-review-semantic/SKILL.md`](../plan-review-semantic/SKILL.md) — pair-heads.
- Glossary term **plan-fix apply** (`ia/specs/glossary.md`).

---

## Changelog

- **2026-04-24** — Stripped `code-fix` + `stage-closeout` modes. Single-mode pair-tail for seam #1 (`§Plan Fix`) only. Reasons: E14 — `opus-code-reviewer` applies critical fixes inline via direct Edit/Write tools instead of writing `§Code Fix Plan` tuples; C10 — `ship-stage` SKILL Step 4 runs closeout inline via `stage_closeout_apply` MCP tool (DB-backed) instead of dispatching `stage-closeout-planner` → `plan-applier` Mode stage-closeout pair. Retired skill dirs: `ia/skills/_retired/stage-closeout-plan/` + `ia/skills/_retired/stage-closeout-apply/` (already retired) + `ia/skills/_retired/code-fix-apply/` (already retired). Authority: Step 8 of `docs/ia-dev-db-refactor-implementation.md` §2.1 + §3.
