---
purpose: "Sonnet pair-tail (seam #4): reads §Stage Closeout Plan tuples from master plan Stage block; applies shared migration ops once + per-Task ops in loop (archive yaml / delete spec / flip task-row status / purge id refs / emit digest); runs materialize + validate:all once at end; aggregates N per-Task digests into one Stage-level digest; rolls Stage Status → Final via R5."
audience: agent
loaded_by: skill:stage-closeout-apply
slices_via: none
name: stage-closeout-apply
description: >
  Sonnet pair-tail skill (seam #4). Reads §Stage Closeout Plan tuple list
  emitted by stage-closeout-plan (Opus pair-head) from master plan Stage
  block. Applies bulk:
    (a) shared glossary / rule / doc migration ops once across the Stage;
    (b) loops N Task per-Task ops — archive_record (yaml → ia/backlog-archive/
        with status: closed + completed: {ISO_DATE}), delete_file (spec),
        replace_section (task-row Status → Done (archived)), id_purge
        (durable doc refs), digest_emit (stage_closeout_digest MCP call);
    (c) runs materialize-backlog.sh + validate:all once at end;
    (d) aggregates N per-Task digests into one Stage-level digest (chain-level);
    (e) flips Stage header Status → Final + rolls up to Step / Plan-level
        Final via R5 gate.
  Never reorders tuples. Escalates to Opus on anchor ambiguity, validator
  failure, or transient flock timeout (1 retry). Idempotent: re-run on
  partial or fully-applied state = zero diff.
  Triggers: "stage-closeout-apply", "/closeout {MASTER_PLAN_PATH} {STAGE_ID}",
  "apply stage closeout plan", "pair-tail stage closeout", "bulk close stage apply".
  Argument order (explicit): MASTER_PLAN_PATH first, STAGE_ID second.
model: inherit
phases:
  - "Read §Stage Closeout Plan"
  - "Apply shared migration ops"
  - "Apply per-Task ops (archive + delete + status flip + id purge + digest_emit)"
  - "Post-loop: materialize + validate"
  - "Aggregate digests + Stage-Status rollup + hand-off"
---

# Stage-closeout-apply skill (Sonnet pair-tail, seam #4)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-tail for Stage closeout seam (seam #4 in [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md)). Fires **once per Stage** after `stage-closeout-plan` (Opus pair-head) has written `§Stage Closeout Plan` tuples. Replaces the retired per-Task `project-spec-close` + per-Task `project-stage-close` dual path — one Sonnet pass applies all closeout ops (shared + per-Task) across the full Stage.

Sibling pair-head: [`stage-closeout-plan/SKILL.md`](../stage-closeout-plan/SKILL.md).

Never re-queries MCP for anchor resolution (planner resolved every anchor). Never re-orders, merges, or interprets tuples — applies verbatim. Never authors new normative spec prose — only the mutations dictated by tuple payloads.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path (e.g. `ia/projects/lifecycle-refactor-master-plan.md`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.2` or `Stage 7.2`). |

Pre-conditions:

- `§Stage Closeout Plan` section populated under Stage `STAGE_ID` block in `MASTER_PLAN_PATH`.
- Every Task row in target Stage has Status = `Done` (post-verify + post-audit).
- `/verify-loop` passed for every Task (spec §Verification block = OK).

---

## Phase 0.5 — Shared Stage context (composite-first, MCP available)

Before reading tuples, if any validation gate or anchor re-check requires Stage context:

Call `mcp__territory-ia__lifecycle_stage_context({ master_plan_path: "{MASTER_PLAN_PATH}", stage_id: "{STAGE_ID}" })` — single call; returns stage header + Task spec bodies + glossary anchors + invariants. Use to verify tuple intent alignment only. **Do NOT re-query per-tuple.** Pair-tail applies tuples verbatim — planner is authoritative.

### Bash fallback (MCP unavailable)

Read `MASTER_PLAN_PATH` Stage block directly for context re-check. Tuples still applied verbatim from `§Stage Closeout Plan`.

---

## Phase 1 — Read `§Stage Closeout Plan`

1. Open `MASTER_PLAN_PATH`. Locate `#### Stage {STAGE_ID}` block.
2. Find `#### §Stage Closeout Plan` subsection within Stage block. Absent → escalate: `{escalation: true, reason: "§Stage Closeout Plan section not found in Stage {STAGE_ID}", tuple_index: null}`.
3. Parse YAML tuple list under `#### §Stage Closeout Plan`. Split into two ordered arrays by comment header: `shared_ops[]` (under `# Shared migration ops`) and `per_task_ops[]` (under `# Per-Task ops`).
4. Validate each tuple has required keys per `plan-apply-pair-contract.md` §Plan tuple shape: `operation`, `target_path`, `target_anchor`, `payload`. Missing key → escalate: `{escalation: true, tuple_index: N, reason: "missing key {KEY}"}`.
5. Validate `operation` enum: `replace_section | insert_after | insert_before | append_row | delete_section | set_frontmatter | archive_record | delete_file | write_file | digest_emit | id_purge`. Unknown → escalate.
6. Group `per_task_ops[]` by contiguous per-Task block (archive → delete → status flip → id purge* → digest_emit). Collect `task_issue_ids[]` for post-loop digest aggregation + rollup.

---

## Phase 2 — Apply shared migration ops

Loop `shared_ops[]` in declared order. Process one tuple at a time. For each:

### 2a. Resolve anchor (re-verify)

- `target_path` exists (stat check for non-`write_file` ops).
- `target_anchor` resolves to exactly one heading / line / glossary row / table row on `target_path`. Zero or >1 match → escalate: `{escalation: true, tuple_index: N, reason: "anchor not found" | "anchor ambiguous", candidate_matches: [...]}`.

### 2b. Apply operation

| Operation | Action |
|-----------|--------|
| `replace_section` | Overwrite section at `target_anchor` with `payload` (literal final-state). Idempotent: if existing content matches payload → no-op. |
| `insert_after` / `insert_before` | Insert `payload` at relative position to `target_anchor`. Idempotent: detect payload already at target offset → no-op. |
| `append_row` | Append row `payload` to table identified by `target_anchor`. Idempotent: detect row already present (match on row key) → no-op. |
| `delete_section` | Delete section at `target_anchor`. Idempotent: already absent → no-op. |
| `set_frontmatter` | Merge `payload` object into YAML frontmatter. Idempotent: overwrite to final state. |
| `write_file` | Write `payload` to `target_path` (overwrite). Idempotent. |

### 2c. Failure

Any write error, unexpected file state, or payload/anchor mismatch → escalate immediately with `{escalation: true, tuple_index: N, reason, snapshot: <file state>}`. Never retry (non-transient).

---

## Phase 3 — Apply per-Task ops

Loop `per_task_ops[]` in declared order, grouped per Task. For each tuple:

### 3a. `archive_record`

1. Read `target_path` (e.g. `ia/backlog/{ISSUE_ID}.yaml`).
2. Merge `payload` fields into yaml body: `status: closed` + `completed: {ISO_DATE}` (ISO today — use `TZ=UTC date +%Y-%m-%d` or tuple-provided value).
3. `git mv ia/backlog/{ISSUE_ID}.yaml {payload.dest}` (e.g. `ia/backlog-archive/{ISSUE_ID}.yaml`). Non-`git` env → `mv` fallback.
4. Idempotency: if `payload.dest` already exists AND source absent → no-op skip.
5. Failure (missing source + missing dest) → escalate: `{escalation: true, tuple_index: N, reason: "archive_record source not found"}`.

### 3b. `delete_file`

1. `rm {target_path}` (e.g. `ia/projects/{ISSUE_ID}.md`).
2. Idempotency: file already absent → no-op.
3. Failure (permission / lock) → escalate.

### 3c. `replace_section` (task-row Status flip)

1. `target_anchor` = `task_key:T{STAGE_ID}.{N}` — find the exact task-table row in `MASTER_PLAN_PATH`.
2. Rewrite row with `payload` text (full updated row: `| {task_key} | {name} | **{ISSUE_ID}** | Done (archived) | {intent} |`).
3. Idempotency: row already flipped to `Done (archived)` → no-op.

### 3d. `id_purge`

1. `target_path` = durable doc (e.g. `CLAUDE.md`, `AGENTS.md`, `ia/rules/{rule}.md`, `docs/{doc}.md`).
2. `target_anchor` = exact line or paragraph containing `{ISSUE_ID}` reference. Overwrite with `payload` (scrubbed prose).
3. Idempotency: reference already absent → no-op.
4. Historical surfaces skipped by planner: `ia/backlog-archive/*`, `ia/state/pre-refactor-snapshot/*`, `ia/specs/*` (never scrub historical refs). Applier enforces: if `target_path` matches those patterns → escalate `{escalation: true, tuple_index: N, reason: "id_purge on historical surface forbidden"}`.

### 3e. `digest_emit`

1. Call MCP tool `mcp__territory-ia__stage_closeout_digest` with `{ issue_id: {ISSUE_ID} }` or `{ spec_path: ia/backlog-archive/{ISSUE_ID}.yaml }`.
2. Capture JSON response `{ok: true, payload: {sections, issue_ids, spec_path}}` into `task_digests[ISSUE_ID]`.
3. Failure (`ok: false` / envelope error) → log + continue (digest failure is non-blocking per seam #4 §Validation gate leniency). Record `{ISSUE_ID: "digest_unavailable"}` in `task_digests[]` for Phase 5 aggregation.

### 3f. Transient-failure retry

Single `flock` timeout on yaml mv or materialize helper → sleep 2s + retry once. Second failure → escalate.

---

## Phase 4 — Post-loop: materialize + validate

Run after all shared + per-Task tuples processed (regardless of skip count).

1. **Materialize BACKLOG:**
   ```bash
   bash tools/scripts/materialize-backlog.sh
   ```
   Non-zero exit → escalate: `{escalation: true, reason: "materialize-backlog.sh failed: {stderr}"}`.

2. **Validate (seam #4 gate):**
   ```bash
   npm run validate:all
   ```
   Per seam #4 validation gate in `plan-apply-pair-contract.md`. Non-zero exit → escalate: `{escalation: true, reason: "validator failed: {exit_code} {stderr}", failing_tuple_index: null}`. Return full stderr to Opus pair-head.

No per-tuple validator runs — once at end only (performance + atomicity).

---

## Phase 5 — Aggregate digests + Stage-Status rollup + hand-off

### 5a. Aggregate N per-Task digests → one Stage-level digest

Concatenate `task_digests[]` into single Stage-level digest. Shape:

```json
{
  "stage_id": "{STAGE_ID}",
  "master_plan_path": "{MASTER_PLAN_PATH}",
  "closed_tasks": [
    {
      "issue_id": "{ISSUE_ID_1}",
      "sections": { "summary": "...", "lessons_learned": "...", "audit": "..." }
    },
    ...
  ],
  "shared_migration_ops": {M_shared},
  "per_task_ops": {M_task}
}
```

Emit to stdout (chain-level consumer = `/ship-stage` orchestrator or direct user). Does NOT persist to disk (superseded by durable `§Audit` paragraphs in archived yaml + master-plan Stage block).

### 5b. Stage-Status rollup (R5 gate)

Per [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md) R5:

1. **Stage header:** find `#### Stage {STAGE_ID} — {Title}` block in `MASTER_PLAN_PATH`. Rewrite `**Status:**` line `In Progress` → `Final`. Idempotent if already `Final`.
2. **Step header rollup:** if every Stage of parent Step `N` has `Status: Final` → rewrite Step header `**Status:**` line to `Final`. Idempotent.
3. **Plan top-Status rollup:** if every Step has `Status: Final` → rewrite top-of-file `> **Status:**` line to `Final`. Idempotent. Otherwise rewrite to `In Progress — Step {next_open_step}` (shift cursor to next non-Final step).

### 5c. Hand-off

Emit structured summary:

```
stage-closeout-apply done. STAGE_ID={STAGE_ID} TASKS_CLOSED={N}
Shared ops applied: {M_shared} (surfaces: {glossary/rules/docs list})
Per-Task ops applied: {M_task} ({N} archive + {N} delete + {N} status flip + {n_purge} id purges + {N} digest)
Validators: validate:all exit 0.
Stage Status → Final. Step {STEP_N} rollup: {Final|In Progress}. Plan rollup: {Final|In Progress}.
Next: {next-stage handoff | all stages Final → /closeout umbrella done}
```

Next-stage handoff resolution (4 cases):

| Case | Next step |
|------|-----------|
| Same Step has next Stage `Draft`/`Planned` | Suggest `claude-personal "/ship-stage {MASTER_PLAN_PATH} {next_stage_id}"` |
| Parent Step has no more Stages, sibling Step exists | Suggest begin sibling Step Stage 1 via `/ship-stage {MASTER_PLAN_PATH} {sibling_first_stage}` |
| All Stages of all Steps `Final` | Plan-level close — no action, orchestrator remains permanent per `orchestrator-vs-spec.md` |
| Next stage has no filed tasks | Suggest `claude-personal "/stage-file {MASTER_PLAN_PATH} {next_stage_id}"` |

---

## Escalation rules

Sonnet pair-tail NEVER guesses. Immediate return-to-Opus triggers (per `plan-apply-pair-contract.md` §Escalation rule):

| Trigger | Return shape |
|---------|-------------|
| `§Stage Closeout Plan` section missing | `{escalation: true, reason: "section missing", tuple_index: null}` |
| Missing required tuple key | `{escalation: true, tuple_index: N, reason: "missing key {KEY}"}` |
| Unknown `operation` enum | `{escalation: true, tuple_index: N, reason: "unknown operation {OP}"}` |
| Anchor matches zero rows | `{escalation: true, tuple_index: N, reason: "anchor not found", candidate_matches: []}` |
| Anchor matches multiple rows | `{escalation: true, tuple_index: N, reason: "anchor ambiguous", candidate_matches: [...]}` |
| `archive_record` source + dest both missing | `{escalation: true, tuple_index: N, reason: "archive_record source not found"}` |
| `id_purge` on historical surface | `{escalation: true, tuple_index: N, reason: "id_purge on historical surface forbidden"}` |
| `materialize-backlog.sh` non-zero | `{escalation: true, reason: "materialize failed: {stderr}"}` |
| `validate:all` non-zero exit | `{escalation: true, reason: "validator failed: {exit_code} {stderr}"}` |
| `flock` transient (2nd attempt) | `{escalation: true, tuple_index: N, reason: "flock timeout 2x"}` |

Opus pair-head receives escalation → revises `§Stage Closeout Plan` → applier re-runs from scratch (idempotency guarantees safety).

---

## Idempotency

- `archive_record`: `git mv` source → dest with merged status/completed fields; if source absent + dest present → no-op.
- `delete_file`: `rm` target; absent → no-op.
- `replace_section` task-row: row already `Done (archived)` → no-op.
- `id_purge`: reference absent in target paragraph → no-op.
- `digest_emit`: MCP call idempotent (read-only from applier's perspective; digest regenerated per call — no write to disk).
- Shared `replace_section` / `insert_*` / `append_row` / `set_frontmatter` / `write_file`: per Phase 2b rules.
- `materialize-backlog.sh` + `validate:all`: idempotent by design.
- Stage / Step / Plan Status flips: overwrite to desired final state — no-op if already matches.

Re-running fully-applied state = exit 0 + zero diff.

---

## Hard boundaries

- Do NOT re-query MCP for anchor resolution — planner resolved every anchor; applier reads tuples verbatim.
- Do NOT reorder tuples — apply in declared order only (shared first, then per-Task grouped per Task).
- Do NOT write normative spec prose — only the mutations dictated by tuple payloads.
- Do NOT run validators per-tuple — `materialize-backlog.sh` + `validate:all` at Phase 4 end only.
- Do NOT edit `BACKLOG.md` / `BACKLOG-ARCHIVE.md` directly — `materialize-backlog.sh` regenerates both from yaml state.
- Do NOT guess on ambiguous anchor — escalate immediately.
- Do NOT persist the aggregated digest to disk — emit to stdout only (durable state = archived yaml + Stage block).
- Do NOT flip Stage Status → Final if any Task row is non-`Done (archived)` post-loop — escalate.
- Do NOT touch `ia/backlog-archive/*`, `ia/state/pre-refactor-snapshot/*`, or `ia/specs/*` for `id_purge` — historical surfaces are read-only to the applier.
- Do NOT `git commit` — commit is a separate user-gated step outside pair scope.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — seam #4, §Plan tuple shape, §Validation gate, §Escalation rule, §Idempotency requirement.
- [`ia/skills/stage-closeout-plan/SKILL.md`](../stage-closeout-plan/SKILL.md) — Opus pair-head (seam #4 head).
- [`ia/skills/opus-audit/SKILL.md`](../opus-audit/SKILL.md) — upstream `§Audit` paragraph source.
- [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md) — R5 Stage/Step/Plan Status rollup rule.
- [`ia/templates/master-plan-template.md`](../../templates/master-plan-template.md) — `§Stage Closeout Plan` section stub.
- MCP tool: `stage_closeout_digest` (renamed from `project_spec_closeout_digest` in T7.14).

## Changelog
