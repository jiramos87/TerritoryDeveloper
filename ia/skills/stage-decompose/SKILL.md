---
purpose: "Expand a deferred skeleton Stage in an existing orchestrator master plan into a full Task table (5-column canonical). Edits the master plan in-place. Does NOT create BACKLOG rows — that is stage-file. Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`."
audience: agent
loaded_by: skill:stage-decompose
slices_via: glossary_discover, glossary_lookup, router_for_task, invariants_summary, spec_sections
name: stage-decompose
description: >
  Expand one skeleton Stage (Stages that carry Objectives + Exit but no Task table) in an existing
  2-level master plan into its Task table + 4 canonical subsections (§Stage File Plan · §Plan Fix ·
  §Stage Audit · §Stage Closeout Plan). Source material: Stage's Exit criteria + Deferred
  decomposition hints + Relevant surfaces. MCP context: glossary, router, invariants, spec_sections.
  Applies the same cardinality + task-sizing rules as master-plan-new. Persists the decomposed Stage
  into the existing orchestrator doc in-place. Does NOT create BACKLOG rows (stage-file does that).
  2-level hierarchy Stage > Task (Step + Phase layers removed per lifecycle-refactor). Canonical
  shape authority: `docs/MASTER-PLAN-STRUCTURE.md`.
  Triggers: "/stage-decompose {path} Stage 2.3", "decompose stage 2.3", "expand stage skeleton",
  "materialize deferred stage", "decompose before stage-file".
model: inherit
phases:
  - "Load + validate"
  - "MCP context"
  - "Task decomposition"
  - "Cardinality gate"
  - "Sizing-gate evaluation"
  - "Persist"
  - "Progress regen"
  - "Handoff"
---

# Stage decompose — expand skeleton Stage in existing master plan

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Exit criteria carry type/method/file precision; Relevant surfaces stay explicit (paths + line refs).

**Canonical master-plan shape:** [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) — authoritative source for Stage block shape, 5-column Task table schema, required subsections. This skill fills in TO that shape; drift → MASTER-PLAN-STRUCTURE.md wins.

**Lifecycle:** AFTER [`master-plan-new`](../master-plan-new/SKILL.md) (or [`master-plan-extend`](../master-plan-extend/SKILL.md)) authors the orchestrator with a skeleton Stage, BEFORE [`stage-file`](../stage-file/SKILL.md) files the tasks. `stage-file` requires `_pending_` tasks — this skill materializes them.

```
master-plan-new → [stage-decompose (deferred Stages only)] → stage-file → plan-author → ...
```

**Note:** Default path is `master-plan-new` fully decomposes ALL Stages at author time (no skeletons). `stage-decompose` is reserved for:
- Rare cases where a Stage was intentionally left as skeleton (Objectives + Exit only) pending downstream design clarity.
- Old pre-lifecycle-refactor plans whose Stages carry `**Stages:** _TBD_` or `decomposition deferred` markers that need forward migration.

**Related:** [`master-plan-new`](../master-plan-new/SKILL.md) · [`master-plan-extend`](../master-plan-extend/SKILL.md) · [`stage-file`](../stage-file/SKILL.md) · [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) · [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ORCHESTRATOR_SPEC` | **1st arg** (explicit path) or Glob resolve | Path to `ia/projects/{slug}-master-plan.md`. Glob fallback only when exactly one `*-master-plan.md` exists — otherwise ask user. |
| `STAGE_ID` | **2nd arg** | `N.M` format (e.g. `2.3` or `Stage 2.3`). Points at an existing `### Stage {STAGE_ID}` block that lacks a Task table. |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Load + validate

Read `ORCHESTRATOR_SPEC`. Find the `### Stage {STAGE_ID}` block (H3 canonical; accept `#### Stage` H4 as legacy drift but flag for re-author).

**Confirm it is a skeleton** — look for any of:

- `Status: Draft (decomposition deferred …)` in the Stage block.
- `**Tasks:** _TBD_` placeholder line.
- No task table present under the Stage.
- Stage carries Objectives + Exit criteria but its body ends before a Tasks table.

If Stage already has a complete Task table → **STOP**. Report current decomposition state; ask user to confirm intentional overwrite before continuing.

If `STAGE_ID` does not match any `### Stage` heading in orchestrator → **STOP**. Report available stage IDs.

Hold in working memory:

- **Stage Name + Objectives** — from Stage block header.
- **Exit criteria** — full list; these are the deliverable contract.
- **Relevant surfaces** — code paths / spec refs cited in the Stage block (may be brief in skeletons).
- **Art** — if declared; else `None`.
- **Task hints** — from Stage block's own hint lines OR from a `## Deferred decomposition` section entry if present (e.g. "Candidate tasks: …"). These are inputs, not constraints — override if implementation logic demands different breakdown.
- **Prior Stage outputs** — scan `### Stage {prior}` Exit criteria + any `§Stage Closeout Plan` rollup rows. Captures what exists on disk when this Stage opens; feeds "Relevant surfaces".

### Phase 1 — MCP context (Tool recipe)

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from Stage Exit criteria + Relevant surfaces + Task hints (translate any non-English before passing); `brownfield_flag = true` for greenfield Stages (no existing C# paths); `brownfield_flag = false` for brownfield; `tooling_only_flag = true` for doc/IA/tooling-only Stages.

Use returned `glossary_anchors` for canonical names in Task intent prose; `router_domains` + `spec_sections` for Relevant surfaces augmentation; `invariants` for guardrail flags.

**Surface-path pre-check** (after tool recipe, both greenfield + brownfield): run `surface-path-precheck` subskill ([`ia/skills/surface-path-precheck/SKILL.md`](../surface-path-precheck/SKILL.md)) on paths from Exit criteria + Task hints. Use returned `line_hint` in surfaces; mark `(new)` for `exists: false`. Skip → ghost line numbers downstream.

### Phase 2 — Task decomposition

Break down the Stage's Exit criteria into 2–6 Tasks (cardinality gate Phase 3 hard ≥2 / soft ≤6). Apply same ordering heuristic as `master-plan-new` (earliest first):

1. **Scaffolding / infrastructure** — persistent setup, new file skeletons, config, new bindings. No logic yet.
2. **Data model** — ScriptableObjects, structs, serialized fields. Typed but inert.
3. **Runtime logic** — algorithms, DSP kernels, update loops, compute. Consumes data model.
4. **Integration + tests** — call sites, EditMode/PlayMode tests, golden fixtures.

Deviate only when Exit criteria force a different dep chain — note deviation in the Decision Log block authored in Phase 4.

**Task hints** from the Stage block (or matching `## Deferred decomposition` entry) are candidate task names. Use them as starting scaffolding; reshape to fit the ordering heuristic + actual Exit criteria decomposition.

Author the Task table (5 columns canonical per MASTER-PLAN-STRUCTURE.md §3 — **NO Phase column**):

```markdown
**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T{STAGE_N}.{STAGE_M}.1 | {short name ≤6 words} | _pending_ | _pending_ | {≤2 sentences — concrete deliverable: types, methods, file paths. Reference existing patterns where applicable.} |
| T{STAGE_N}.{STAGE_M}.2 | {short name} | _pending_ | _pending_ | {...} |
| T{STAGE_N}.{STAGE_M}.3 | {short name} | _pending_ | _pending_ | {...} |
```

Also author the 4 pending subsections (per MASTER-PLAN-STRUCTURE.md §3):

```markdown
#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit {this-doc} Stage {STAGE_ID}` when all Tasks reach Done post-verify._

#### §Stage Closeout Plan

_pending — populated by `/closeout {this-doc} Stage {STAGE_ID}` planner pass when all Tasks reach `Done`._
```

**Task intent concreteness bar:** cite the thing being shipped — type names, method signatures, file paths, field names. Vague verbs ("add support for X") degrade into useless `stage-file` stubs. Match the bar set in `master-plan-new` Phase 4.

**Task sizing heuristic:**

- **Correct scope:** 2–5 files forming one algorithm layer. Target ≤2 `spec_section` reloads and meaningful per-phase deltas.
- **Too small (merge):** single file, single function, single struct with no logic → merge with adjacent same-domain task in the same Stage.
- **Too large (split):** touches >3 unrelated subsystems → split at subsystem-layer seam.

### Phase 3 — Cardinality gate

Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass `{stage_id, task_count}` single-entry map. Cardinality rule (`ia/rules/project-hierarchy.md`): ≥2 Tasks/Stage (hard), ≤6 soft.

Subskill returns `{stages_lt_2, stages_gt_6, single_file_tasks, oversized_tasks, verdict}`:

- `verdict = pause` → surface violations to user; ask split, merge, or justify in Decision Log. Proceed only after user confirms or fixes. Phrase the split/merge question in terms of what the player/designer sees change (user-visible checkpoints, releasable slices) — not stage numbers or task-count math. Ids / stage numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- `verdict = proceed` → continue to Phase 3.5.

Covers task sizing: single-file/function/struct tasks → `single_file_tasks`; >3 unrelated subsystems → `oversized_tasks`.

### Phase 3.5 — Sizing-gate evaluation

Run `ia/rules/stage-sizing-gate.md` heuristics on the Task cluster produced in Phase 2, AFTER Phase 3 cardinality gate PASS. Evaluate all 6 heuristics (H1–H6) in order:

1. **H1 Subsystem cohesion** — count distinct top-level subsystems in task `files:` union.
2. **H2 Verify-path overlap** — classify each task as Path A or Path B; check Path B cluster size.
3. **H3 Diff LOC budget** — estimate cumulative LOC / file count from `files:` union.
4. **H4 DAG linearity** — inspect `depends_on:` edges within Stage; flag fan-out ≥2.
5. **H5 Invariant-hotspot density** — flag shared invariant (#1–#13) touches across tasks.
6. **H6 Compile-break probability** — flag same `*.cs` file mutated by multiple tasks.

Each heuristic returns PASS / WARN / FAIL. Emit verdict block:

```
SIZING GATE — Stage {X.Y}
  H1 cohesion:      {PASS|WARN|FAIL} — {rationale}
  H2 verify-path:   {PASS|WARN|FAIL} — {rationale}
  H3 LOC budget:    {PASS|WARN|FAIL} — {rationale}
  H4 DAG linearity: {PASS|WARN|FAIL} — {rationale}
  H5 invariant:     {PASS|WARN|FAIL} — {rationale}
  H6 compile-break: {PASS|WARN|FAIL} — {rationale}
Outcome: {PASS | WARN-gate | FAIL}
```

**Gate outcomes:**

- **PASS** (all PASS or ≤1 WARN) → continue to Phase 4.
- **WARN-gate** (≥2 WARN, no FAIL) → emit warning block; ask planner to confirm or split. Proceed only on user confirmation.
- **FAIL** (any FAIL) → invoke fail-recurse protocol per `ia/rules/stage-sizing-gate.md` §3: emit split recommendation (X.Y.A / X.Y.B); run sizing gate on each proposed sub-stage. On second consecutive FAIL: user-gate prompt per §3.2 — do NOT auto-split a third time. DO NOT proceed to Phase 4 until gate PASS (or user-accepted waiver).

**Note:** gate evaluates NEW Task drafts only. Already-filed Stages are grandfathered (§3.3).

---

### Phase 4 — Persist (in-place edit)

Edit `ORCHESTRATOR_SPEC` in atomic operations:

**4a — Fill skeleton Stage body:**

Find the `### Stage {STAGE_ID}` block. Preserve existing header fields (Status / Notes / Backlog state / Objectives / Exit criteria / Art / Relevant surfaces — only augment Relevant surfaces with MCP-routed refs from Phase 1 if useful). Append the Task table + 4 pending subsections authored in Phase 2.

Expected final block shape (per MASTER-PLAN-STRUCTURE.md §3):

```markdown
### Stage {STAGE_ID} — {Name}

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage {STAGE_ID}):** 0 filed

**Objectives:** {preserved from skeleton}

**Exit criteria:**

{preserved from skeleton}

**Art:** {preserved from skeleton}

**Relevant surfaces (load when stage opens):**

{preserved from skeleton, augmented with Phase 1 MCP routed refs + Phase 1 pre-check `(new)` marks}

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T{STAGE_N}.{STAGE_M}.1 | ... | _pending_ | _pending_ | ... |
| T{STAGE_N}.{STAGE_M}.2 | ... | _pending_ | _pending_ | ... |

#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit {this-doc} Stage {STAGE_ID}` when all Tasks reach Done post-verify._

#### §Stage Closeout Plan

_pending — populated by `/closeout {this-doc} Stage {STAGE_ID}` planner pass when all Tasks reach `Done`._
```

If the existing skeleton uses retired `#### Stage` H4 → rewrite heading to `### Stage` H3 during fill.

**4b — Update `## Deferred decomposition` section (if present):**

If orchestrator carries a `## Deferred decomposition` section (legacy), find the bullet for `Stage {STAGE_ID}`. Replace with:

```markdown
- **Stage {STAGE_ID} — {Name}:** decomposed {YYYY-MM-DD}. Tasks: {N} (`_pending_`).
```

If no such section exists (canonical new plans don't emit it), skip 4b.

**4c — Status line (idempotent):**

Ensure the Stage's `**Status:**` line reads `Draft` (not `Skeleton` / `Draft (decomposition deferred…)` — those are retired markers). Rewrite if needed. Do NOT flip to `In Progress` — that is `stage-file-apply`'s responsibility (R2). Do NOT touch plan top-of-file `> **Status:**` — `stage-file-apply` owns that flip (R1).

### Phase 5 — Progress regen

Run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block Phase 6; log exit code and continue.

### Phase 6 — Handoff

Single concise caveman message:

- `{ORCHESTRATOR_SPEC}` edited — Stage {STAGE_ID} decomposed: {N} tasks (all `_pending_`).
- Invariant numbers flagged (if any).
- Cardinality + sizing gates: violations resolved / justified.
- Deferred decomposition entry updated (if section existed).
- Next step: `claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"` when prior Stage closes.

---

## Tool recipe (territory-ia) — Phase 1 only

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:

- `keywords`: English tokens from Stage Exit criteria + Task hints + Relevant surfaces text.
- `brownfield_flag`: `true` for greenfield Stages (no existing C# paths in Exit criteria / Relevant surfaces); `false` for brownfield.
- `tooling_only_flag`: `true` for doc/IA/tooling-only Stages.

Use returned `glossary_anchors` for canonical names; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for guardrail flags.

Also run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned by `domain-context-load`. **Brownfield fallback.**

---

## Guardrails

- IF `STAGE_ID` does not match any `### Stage` heading → STOP. Report available stage IDs.
- IF target Stage already has a complete Task table → STOP, ask user to confirm overwrite before proceeding.
- IF target Stage is `In Review`, `In Progress`, or `Final` → STOP. Mutating an advanced Stage requires revision cycle, not this skill.
- IF cardinality gate finds <2 tasks → STOP, pause for user confirmation or fix.
- IF cardinality gate finds 7+ tasks → STOP, suggest split; persist only after user confirms.
- IF any task covers only 1 file / 1 function with no logic → warn + pause.
- IF any task spans >3 unrelated subsystems → warn + pause.
- IF authored output carries 6-column Task table with `Phase` column, `**Phases:**` checkbox list, or `#### Stage` H4 heading → STOP, re-author per canonical 5-column / H3 shape.
- Do NOT create `BACKLOG.md` rows. Do NOT create `ia/projects/{ISSUE_ID}.md` stubs. Tasks stay `_pending_` — `stage-file` materializes them.
- Do NOT decompose other Stages beyond `STAGE_ID` — only the target Stage is expanded.
- Do NOT delete or rename the orchestrator doc.
- Do NOT commit — user decides when to commit.

---

## Seed prompt

```markdown
Run the stage-decompose workflow.

Follow ia/skills/stage-decompose/SKILL.md end-to-end. Inputs:
  ORCHESTRATOR_SPEC: {path to *-master-plan.md}
  STAGE_ID: {N.M, e.g. 2.3}

Canonical master-plan shape: docs/MASTER-PLAN-STRUCTURE.md (Stage block, 5-col Task table, 4 pending subsections). 2-level hierarchy Stage > Task (no Steps, no Phases).
Phase 1 Tool recipe uses territory-ia MCP slices (greenfield skips router / spec_sections / invariants_summary).
Cardinality gate requires ≥2 tasks per Stage AND ≤6 soft — pause for user confirmation on either violation.
Only the target STAGE_ID is decomposed; all other deferred Stages remain as skeletons.
```

---

## Next step

After persist: `claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"` — but ONLY after the prior Stage reaches `Final`.

## Changelog

### 2026-04-24 — lifecycle-refactor alignment

**source:** canonical-structure consolidation (MASTER-PLAN-STRUCTURE.md authored)

**deviation:** skill described 3-level Step > Stage > Phase > Task hierarchy. Decomposed deferred `### Step N` blocks into H4 Stages with `**Phases:**` checkbox list + 6-column Task table carrying `Phase` column; also flipped Step header `Skeleton → Planned` (R7 — now retired per orchestrator-vs-spec.md). Per post-lifecycle-refactor 2-level hierarchy, this skill now operates on deferred `### Stage N.M` skeletons (H3, no Step wrapper), fills in the 5-column Task table + 4 pending subsections (§Stage File Plan / §Plan Fix / §Stage Audit / §Stage Closeout Plan), and leaves Status flipping to downstream skills (`stage-file-apply` owns R1 + R2). Cite `docs/MASTER-PLAN-STRUCTURE.md` as authoritative shape source. Input rename `STEP_ID → STAGE_ID` (N.M format).
