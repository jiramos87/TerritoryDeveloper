---
purpose: "Opus pair-head: reads orchestrator Stage block + shared MCP bundle; emits §Stage File Plan tuple list (one per _pending_ Task) under the Stage block in the master plan."
audience: agent
loaded_by: skill:stage-file-plan
slices_via: none
name: stage-file-plan
description: >
  Opus pair-head skill. Loads shared Stage MCP bundle once via domain-context-load;
  reads the target Stage block from the master plan; gates cardinality; batch-verifies
  all Depends-on ids via a single backlog_list filter call; emits a structured
  §Stage File Plan tuple list under the Stage block — one tuple per _pending_ Task.
  Each tuple carries {reserved_id, title, priority, notes, depends_on, related, stub_body}.
  Pair-tail stage-file-apply reads and materializes tuples without re-querying MCP.
  Triggers: "stage-file-plan", "/stage-file-plan {ORCHESTRATOR_SPEC} {STAGE_ID}",
  "file stage plan", "stage plan planner", "emit stage file tuples".
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
model: inherit
phases:
  - "Load shared Stage MCP bundle"
  - "Read Stage block + cardinality gate"
  - "Batch Depends-on verification"
  - "Emit §Stage File Plan tuples"
  - "Anchor resolution + handoff"
---

# Stage-file-plan skill (Opus pair-head)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Opus pair-head (seam #2). Loads shared Stage context once; reads orchestrator Stage block; gates cardinality; batch-verifies Depends-on ids; emits `§Stage File Plan` tuple list under Stage block in master plan. Pair-tail [`stage-file-apply`](../stage-file-apply/SKILL.md) materializes tuples without re-querying MCP.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #2, §Escalation rule, §Tier 2 bundle reuse.
Sibling pair-tail: [`stage-file-apply/SKILL.md`](../stage-file-apply/SKILL.md).
Mode-routing: [`stage-file/SKILL.md`](../stage-file/SKILL.md) — File mode routes here; Compress mode routes to [`stage-compress`](../stage-compress/SKILL.md).

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ORCHESTRATOR_SPEC` | 1st arg (explicit path preferred) | Repo-relative path to `ia/projects/{master-plan}.md`. Glob fallback only when exactly one `*-master-plan.md` exists. |
| `STAGE_ID` | 2nd arg | e.g. `7.2` or `Stage 7.2`. |
| `ISSUE_PREFIX` | 3rd arg or default | `TECH-` / `FEAT-` / `BUG-` / `ART-` / `AUDIO-` — default `TECH-`. |

---

## Phase 0 — Load shared Stage MCP bundle

**Composite-first call (MCP available):**

1. Call `mcp__territory-ia__lifecycle_stage_context({ master_plan_path: "{ORCHESTRATOR_SPEC}", stage_id: "{STAGE_ID}" })` — first MCP call; returns stage header + Task spec bodies + glossary anchors + invariants + pair-contract slice in one bundle. Store as **shared context block** seed.
2. Proceed to `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)) for any domain enrichment not covered by the bundle:
   - `keywords` = English tokens extracted from Stage Objectives + Exit criteria text (translate if non-English).
   - `brownfield_flag = false` for stages touching existing subsystems.
   - `tooling_only_flag = true` for doc/IA-only stages (no runtime C#).
   - `context_label` = `"stage-file-plan Stage {STAGE_ID}"`.

Store returned payload as **shared context block**: `{glossary_anchors, router_domains, spec_sections, invariants, cache_block}`. Used in Phase 4 `stub_body` authoring for all Tasks. **Call exactly once** — do NOT re-run per Task. `cache_block` is the Tier 2 per-Stage ephemeral bundle (see `ia/rules/plan-apply-pair-contract.md` §Tier 2 bundle reuse); all Tasks within the Stage reuse it without re-fetching.

### Bash fallback (MCP unavailable)

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)):

- `keywords` = English tokens extracted from Stage Objectives + Exit criteria text (translate if non-English).
- `brownfield_flag = false` for stages touching existing subsystems.
- `tooling_only_flag = true` for doc/IA-only stages (no runtime C#).
- `context_label` = `"stage-file-plan Stage {STAGE_ID}"`.

Store returned payload as **shared context block**: `{glossary_anchors, router_domains, spec_sections, invariants, cache_block}`. Used in Phase 4 `stub_body` authoring for all Tasks. **Call exactly once** — do NOT re-run per Task. `cache_block` is the Tier 2 per-Stage ephemeral bundle; reuse across all Tasks of this Stage.

---

## Phase 1 — Read Stage block + cardinality gate

1. Read `ORCHESTRATOR_SPEC`. Locate `#### Stage {STAGE_ID}` block. Extract: Objectives, Exit criteria, Task table rows.
2. Classify mode from Task status counts (same logic as `stage-file` mode detection):
   - **File mode** (≥1 `_pending_` task, 0 `Draft` tasks) → continue.
   - **Compress mode** (0 `_pending_`, ≥1 `Draft`) → STOP; instruct caller to route to [`stage-compress`](../stage-compress/SKILL.md).
   - **No-op** (0 `_pending_`, 0 `Draft`) → report stage state + exit.
3. Collect all `_pending_` Task rows into `pending_tasks[]` (ordered by task-table row order).
4. Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass phase → tasks map for `pending_tasks`.
   - `verdict = pause` → surface violations to user (product/designer phrasing per `agent-human-polling.md`); wait for confirmation before continuing.
   - `verdict = proceed` → continue to sizing-gate check below.
5. **Sizing-gate check** — After cardinality gate PASS, evaluate `ia/rules/stage-sizing-gate.md`
   heuristics H1–H6 on the `pending_tasks` cluster. This gate checks bulk-verify + bulk-code-review
   feasibility of the task cluster (distinct from the task-count cardinality gate):
   - **PASS** (all H1–H6 PASS or ≤1 WARN) → continue to Phase 2.
   - **WARN-gate** (≥2 WARN, no FAIL) → emit warning block; ask user to confirm or split.
     Do NOT proceed to Phase 2 without user confirmation.
   - **FAIL** (any heuristic FAIL) → **HALT**. Do NOT write any yaml files. Emit:
     ```
     SIZING GATE FAIL — Stage {X.Y}
     Failed heuristics: {H-ids with rationale citing stage-sizing-gate.md}
     Action: re-route to /stage-decompose to split Stage {X.Y} → {X.Y.A} / {X.Y.B}.
     No yaml written. Halt.
     ```
     Route back: `claude-personal "/stage-decompose {ORCHESTRATOR_SPEC} Stage {X.Y}"`. Stop here.
   - **Waiver present** (sizing-gate-waiver comment in Stage block) → skip evaluation; proceed.

---

## Phase 2 — Batch Depends-on verification

1. Collect **union** of all Depends-on ids across every `pending_task` entry (stage-level deps + task-level deps; deduplicate).
2. If union is non-empty: call `mcp__territory-ia__backlog_list` **once** with filter `ids: [union_ids]`. Verify each id appears in the response (open or archived). Record unresolvable ids.
3. Unresolvable dep id → escalate immediately: `{escalation: true, reason: "dep id {ID} not found in backlog or archive", pending_tasks}`. Do NOT proceed to tuple authoring.
4. Store `verified_deps: Map<task_key, string[]>` — maps each task to its verified subset of dep ids. Applier reads this from tuple body; no re-query.

**Hard rule:** exactly one `backlog_list` call per Stage. Zero `backlog_issue` calls inside this skill.

---

## Phase 3 — Emit `§Stage File Plan` tuples

For each task in `pending_tasks[]` (in task-table order), author one tuple. Tuple shape (per `plan-apply-pair-contract.md` §Plan tuple shape, seam #2):

```yaml
- reserved_id: ""           # left blank — stage-file-apply fills via reserve-id.sh
  title: "{TASK_INTENT}"   # verbatim from task Intent column
  priority: "{PRIORITY}"   # from task Priority column; default "medium"
  notes: |
    {Concise 1–3 sentence scope note derived from Stage context + task intent.
     Include key files/subsystems from shared context block router_domains.
     Caveman prose.}
  depends_on:
    - "{DEP_ID}"            # from verified_deps[task_key]; empty list if none
  related:
    - "{REL_ID}"            # sibling task ids within same Stage (from task table)
  stub_body:
    summary: |
      {§1 Summary — 1–3 sentences from task intent + stage context.}
    goals: |
      {§2.1 Goals bullet list — 2–4 items derived from task intent.}
    systems_map: |
      {§4.2 Systems map — files/classes from shared context router_domains + spec_sections.}
    impl_plan_sketch: |
      {§7 Implementation Plan — single Phase stub from task intent. One phase is fine at stub level.}
```

Fields:
- `reserved_id`: always blank here — applier calls `reserve-id.sh` atomically during apply.
- `stub_body` sub-fields populate their matching project-spec template sections.
- `depends_on`: verified dep ids from Phase 2 `verified_deps`; applier writes these verbatim to yaml; no re-check.
- `related`: sibling Task issue ids in same Stage (non-self); leave empty if no sibling ids known yet.

---

## Phase 4 — Write `§Stage File Plan` to master plan

Write the tuple list under the target Stage block in `ORCHESTRATOR_SPEC`. Operation = `insert_after` targeting anchor `task_key:T{STAGE_ID}` (last task row in stage) or `#### Stage {STAGE_ID}` heading if no tasks exist yet.

Section format:

```markdown
### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "..."
  ...
- reserved_id: ""
  title: "..."
  ...
```
```

After writing, scan `ORCHESTRATOR_SPEC` to confirm:
- `### §Stage File Plan` heading exists under Stage block.
- Count of tuples = count of `pending_tasks[]`.
- Each tuple has all 7 required keys (`reserved_id`, `title`, `priority`, `notes`, `depends_on`, `related`, `stub_body`).

---

## Phase 5 — Anchor resolution + handoff

1. For each tuple, verify `target_anchor` in applier context resolves to exactly one match:
   - Task-table row anchor = `task_key:T{STAGE_ID}.{N}` (N = task row number within stage).
   - Confirm each anchor matches exactly one row in the master plan task table.
2. Ambiguous or missing anchor → revise before handoff (do NOT pass ambiguous tuples downstream).
3. Emit handoff message to pair-tail:

```
§Stage File Plan written. STAGE_ID={STAGE_ID} TUPLE_COUNT={N} ORCHESTRATOR_SPEC={PATH}
Next: claude-personal "/stage-file-apply {ORCHESTRATOR_SPEC} {STAGE_ID}"
```

---

## Hard boundaries

- Do NOT call `reserve-id.sh` or write yaml files — applier's territory.
- Do NOT reorder tuples after authoring — tuple order = task-table order; applier applies in declared order.
- Do NOT call `backlog_issue` per-task for dep verification — one `backlog_list` call per Stage only.
- Do NOT skip Phase 0 context load — even for tooling-only stages; use `tooling_only_flag = true` in that case.
- Do NOT leave `reserved_id` non-blank — applier fills this atomically; planner leaving a value causes id-collision risk.
- Do NOT proceed past Phase 1 cardinality gate if `verdict = pause` without user confirmation.

---

## §Changelog emitter

## Changelog
