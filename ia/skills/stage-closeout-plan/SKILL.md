---
purpose: "Opus pair-head (seam #4): runs once per Stage when all Tasks reach Done post-verify; writes §Stage Closeout Plan tuples under Stage block covering shared migration + N per-Task closeout ops in one unified list."
audience: agent
loaded_by: skill:stage-closeout-plan
slices_via: none
name: stage-closeout-plan
description: >
  Opus pair-head (seam #4). Runs once per Stage when every Task reaches
  Done post-verify (replaces per-Task `closeout-apply`). Reads master-plan
  Stage header + N Task `§Audit` paragraphs (from `opus-audit`) + N Task
  §Implementation + §Findings + §Verification + invariants + glossary;
  writes unified `§Stage Closeout Plan` tuple list (shared glossary rows,
  rule section edits, doc paragraph edits, plus N BACKLOG archive ops, N
  id purges, N spec deletes, N master-plan status flips, N digest emissions).
  Resolves every anchor to exact line/heading/row-id before handing to
  `stage-closeout-apply` Sonnet pair-tail. Idempotent on re-run.
  Triggers: "/closeout {MASTER_PLAN_PATH} {STAGE_ID}", "stage closeout plan",
  "bulk close stage", "stage end closeout".
phases:
  - "Load Stage + Task closeout context"
  - "Dedupe shared migration ops"
  - "Resolve anchors"
  - "Write §Stage Closeout Plan tuples"
  - "Hand-off"
---

# Stage-closeout-plan skill (Opus pair-head, seam #4)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Opus pair-head for Stage closeout seam (seam #4 in [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md)). Runs **once per Stage** when every Task row in target Stage reaches `Done` and `/verify-loop` passed. Replaces the retired per-Task `closeout-apply` / `project-stage-close` dual path — one unified tuple list covers all closeout ops (shared + per-Task) in one Opus pass.

Sibling pair-tail: [`stage-closeout-apply/SKILL.md`](../stage-closeout-apply/SKILL.md) (authored in T7.14).

Does **NOT** edit spec files, archive yaml, delete specs, flip status, regenerate BACKLOG, or run validators. All mutation happens in pair-tail.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path (e.g. `ia/projects/lifecycle-refactor-master-plan.md`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.1`). |

Pre-condition: every Task row in target Stage has Status = `Done` (post-verify) AND a populated `§Audit` paragraph from prior `opus-audit` Stage-scoped run.

---

## Phase 1 — Load Stage + Task closeout context

**Composite-first call (MCP available):**

1. Call `mcp__territory-ia__lifecycle_stage_context({ master_plan_path: "{MASTER_PLAN_PATH}", stage_id: "{STAGE_ID}" })` — first MCP call; returns stage header + Task spec bodies + glossary anchors + invariants + pair-contract slice in one bundle. Use as primary payload.
2. Supplement with per-Task §Audit / §Lessons Learned sections read from `ia/projects/{ISSUE_ID}.md` if not fully covered in bundle.
3. Load relevant rule bodies via `list_rules` + `rule_content` when any §Audit paragraph cites a rule section for migration (e.g. invariant rewrite, pair-contract seam update).
4. Emit in-memory payload `{stage_header, task_closeouts[], invariants, glossary_candidates, rule_targets}`.

### Bash fallback (MCP unavailable)

1. Read `MASTER_PLAN_PATH` Stage `STAGE_ID` block: Objectives, Exit criteria, Tasks table. Collect every Task row `{task_key, name, issue_id}` with Status = `Done`.
2. For each Task: read `ia/projects/{ISSUE_ID}.md` sections — §Audit (paragraph), §7 Implementation Plan (completed phases + deliverables), §9 Issues Found, §10 Lessons Learned, Verification block snippet (when present), §Plan Author §Acceptance (checkbox state).
3. Load invariants subset via `invariants_summary` (domain keywords from Stage Objectives).
4. Load glossary table via `glossary_discover` → `glossary_lookup` for every canonical term touched by any Task's §Audit or §Lessons Learned.
5. Load relevant rule bodies via `list_rules` + `rule_content` when any §Audit paragraph cites a rule section for migration (e.g. invariant rewrite, pair-contract seam update).
6. Emit in-memory payload `{stage_header, task_closeouts[], invariants, glossary_candidates, rule_targets}`.

---

## Phase 2 — Dedupe shared migration ops

Aggregate across N Task closeouts. Bucket candidate migrations into **shared** vs **per-Task**.

**Shared** (one tuple total, regardless of N):

- Glossary row additions — when multiple Task §Audit paragraphs cite the same new canonical term.
- Rule section edits — when multiple Task §Audit paragraphs cite the same rule (e.g. invariant #17 rewrite).
- Doc paragraph edits — when multiple Task §Audit paragraphs cite the same `docs/*.md` paragraph.
- CLAUDE.md / AGENTS.md edits — when shared surface map / lifecycle flow changes.

**Per-Task** (N tuples):

- `archive_record` — move `ia/backlog/{id}.yaml` → `ia/backlog-archive/{id}.yaml` (with `status: closed` + `completed: {ISO_DATE}` mutation).
- `delete_file` — `ia/projects/{ISSUE_ID}.md`.
- `replace_section` on `MASTER_PLAN_PATH` — flip Task row Status `Done` → `Done (archived)`.
- `id_purge` — scan durable docs (CLAUDE.md, AGENTS.md, `ia/rules/*.md`, `docs/*.md`) for `{ISSUE_ID}` references and emit `replace_section` tuples for each hit (grep-resolve exact anchor).
- `digest_emit` — write per-Task journal digest via `stage_closeout_digest` MCP tool (renamed from `project_spec_closeout_digest` in T7.14).

Dedupe rule: an op is **shared** only when ≥2 Tasks cite the exact same target anchor (path + heading + row-id). Otherwise it is per-Task.

---

## Phase 3 — Resolve anchors

For every candidate tuple (shared + per-Task):

- `target_path` exists (grep/stat check) OR is a `write_file` creating a new file.
- `target_anchor` resolves to a single match (heading / line / row-id / glossary row). Multiple matches → escalate to user before writing §Stage Closeout Plan.
- `payload` literal final-state content (never a diff).
- For `archive_record` ops: payload = YAML body with `status: closed` + `completed: {ISO_DATE}` merged in.
- For `replace_section` on master plan task row: payload = full updated row text `| {task_key} | {name} | **{ISSUE_ID}** | Done (archived) | {intent} |`.

Ambiguity (zero or >1 match) → return escalation shape `{escalation: true, tuple_index, reason, candidate_matches}` per pair-contract §Escalation rule.

---

## Phase 4 — Write §Stage Closeout Plan tuples

Write `## §Stage Closeout Plan` section under Stage `STAGE_ID` block in `MASTER_PLAN_PATH`. Placement: after `#### §Plan Fix` section, before next `### Stage` heading (or end of Stages block if last).

### Structure

```markdown
#### §Stage Closeout Plan

> stage-closeout-plan — {N} Tasks ({M_shared} shared migration ops + {M_task} per-Task ops = {M_total} tuples total). Spawn `stage-closeout-apply {MASTER_PLAN_PATH} {STAGE_ID}`.

```yaml
# Shared migration ops (apply once)
- operation: insert_after
  target_path: ia/specs/glossary.md
  target_anchor: "| existing-term |"
  payload: |
    | new-canonical-term | definition ... |

- operation: replace_section
  target_path: ia/rules/invariants.md
  target_anchor: "## Invariant #17"
  payload: |
    <corrected body>

# Per-Task ops (apply per Task in declared order)
- operation: archive_record
  target_path: ia/backlog/{ISSUE_ID}.yaml
  target_anchor: "id: {ISSUE_ID}"
  payload:
    status: closed
    completed: "{ISO_DATE}"
    dest: ia/backlog-archive/{ISSUE_ID}.yaml

- operation: delete_file
  target_path: ia/projects/{ISSUE_ID}.md
  target_anchor: "file:{ISSUE_ID}.md"
  payload: null

- operation: replace_section
  target_path: {MASTER_PLAN_PATH}
  target_anchor: "task_key:{task_key}"
  payload: |
    | {task_key} | {name} | **{ISSUE_ID}** | Done (archived) | {intent} |

- operation: digest_emit
  target_path: ia/backlog-archive/{ISSUE_ID}.yaml
  target_anchor: "{ISSUE_ID}"
  payload:
    tool: stage_closeout_digest
    mode: per_task
```
```

Rules:
- Tuples execute in declared order. Applier reads verbatim, never re-orders.
- Shared tuples go first, then per-Task tuples grouped by Task (all ops for Task A, then Task B, …).
- Idempotent: re-running an applied §Stage Closeout Plan → exit 0 + zero diff (applier is safe per pair-contract §Idempotency).
- One tuple per atomic edit. Multiple glossary row additions → one tuple per row.

---

## Phase 5 — Hand-off

Emit structured summary:

```
stage-closeout-plan: Stage {STAGE_ID} — {N} Tasks, {M_shared} shared + {M_task} per-Task = {M_total} tuples written to §Stage Closeout Plan.
  Shared ops: {list of surfaces touched — glossary / rules / docs}.
  Per-Task ops: {ISSUE_ID list, {N} archive + {N} delete + {N} status flip + {n_purge} id purges + {N} digest}.
  Next: /closeout {MASTER_PLAN_PATH} {STAGE_ID}  →  dispatches stage-closeout-apply Sonnet pair-tail.
```

Does NOT run the applier. `/closeout` Stage-scoped dispatcher (rewired in T7.14) chains head → tail.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — seam #4, §Plan tuple shape, §Escalation rule, §Idempotency requirement, §Tier 2 bundle reuse.
- [`ia/skills/stage-closeout-apply/SKILL.md`](../stage-closeout-apply/SKILL.md) — Sonnet pair-tail (T7.14).
- [`ia/skills/opus-audit/SKILL.md`](../opus-audit/SKILL.md) — upstream source of per-Task §Audit paragraphs.
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — Tier 2 per-Stage ephemeral bundle; call once at Stage-start; reuse `cache_block` across all closeout tuple authoring.
- [`ia/templates/master-plan-template.md`](../../templates/master-plan-template.md) — §Stage Closeout Plan section stub.
- Glossary: `ia/specs/glossary.md` — canonical-term source of truth.

## Changelog
