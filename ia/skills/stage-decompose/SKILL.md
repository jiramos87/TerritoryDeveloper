---
purpose: "Expand a deferred skeleton step in an existing orchestrator master plan into a full stages → phases → tasks decomposition. Edits the master plan in-place. Does NOT create BACKLOG rows — that is stage-file."
audience: agent
loaded_by: skill:stage-decompose
slices_via: glossary_discover, glossary_lookup, router_for_task, invariants_summary, spec_sections
name: stage-decompose
description: >
  Expand one skeleton step (Steps 2+ in an existing master plan) into stages → phases → tasks.
  Source material: step's Exit criteria + Deferred decomposition hints + Relevant surfaces.
  MCP context: glossary, router, invariants, spec_sections. Applies the same cardinality +
  task-sizing rules as master-plan-new. Persists the decomposed step into the existing
  orchestrator doc in-place. Does NOT create BACKLOG rows (stage-file does that).
  Triggers: "/stage-decompose {path} Step 2", "decompose step 2", "expand step skeleton",
  "materialize deferred step", "decompose before stage-file", "expand Step N in master plan".
model: inherit
phases:
  - "Load + validate"
  - "MCP context"
  - "Stage decomposition"
  - "Cardinality gate"
  - "Sizing-gate evaluation"
  - "Persist"
  - "Progress regen"
  - "Handoff"
---

# Stage decompose — expand skeleton step in existing master plan

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Exit criteria carry type/method/file precision; Relevant surfaces stay explicit (paths + line refs).

**Lifecycle:** AFTER [`master-plan-new`](../master-plan-new/SKILL.md) authors the orchestrator (skeleton steps 2+), BEFORE [`stage-file`](../stage-file/SKILL.md) files the tasks. `stage-file` requires `_pending_` tasks — this skill materializes them.

```
master-plan-new → [stage-decompose (Steps 2+)] → stage-file → project-new → ...
```

**Related:** [`master-plan-new`](../master-plan-new/SKILL.md) · [`stage-file`](../stage-file/SKILL.md) · [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ORCHESTRATOR_SPEC` | **1st arg** (explicit path) or Glob resolve | Path to `ia/projects/{slug}-master-plan.md`. Glob fallback only when exactly one `*-master-plan.md` exists — otherwise ask user. |
| `STEP_ID` | **2nd arg** | Integer or `Step N` (e.g. `2` or `Step 2`). Must be ≥ 2 (Step 1 is already decomposed by master-plan-new). |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Load + validate

Read `ORCHESTRATOR_SPEC`. Find the `### Step {STEP_ID}` block.

**Confirm it is a skeleton** — look for any of:
- `Status: Draft (decomposition deferred …)`
- `**Stages:** _TBD_`
- No task table present under the step

If step already has a task table (previously decomposed) → **STOP**. Report current decomposition state; ask user to confirm intentional overwrite before continuing.

If `STEP_ID` == 1 → **STOP**. Step 1 is decomposed by `master-plan-new`; this skill only handles deferred steps.

Hold in working memory:

- **Step Name + Objectives** — from step block header.
- **Exit criteria** — full list; these are the deliverable contract.
- **Relevant surfaces** — code paths / spec refs cited in the step block (may be brief in skeletons).
- **Art** — if declared; else `None`.
- **Stage hints** — from `## Deferred decomposition` section for this step (e.g. "Candidate stages: …"). These are inputs, not constraints — override if implementation logic demands different ordering.
- **Prior step Exit** — scan `### Step {STEP_ID - 1}` Exit criteria + any Stage-scoped `/closeout` pair (`plan-applier` Mode stage-closeout) rollup rows. Captures what exists on disk when this step opens; feeds "Relevant surfaces" for new stages.

### Phase 1 — MCP context (Tool recipe)

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from step Exit criteria + Relevant surfaces + Stage hints (translate any non-English before passing); `brownfield_flag = true` for greenfield steps (no existing C# paths); `brownfield_flag = false` for brownfield; `tooling_only_flag = true` for doc/IA/tooling-only steps.

Use returned `glossary_anchors` for canonical names in phase/task prose; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for guardrail flags.

**Surface-path pre-check** (after tool recipe, both greenfield + brownfield): run `surface-path-precheck` subskill ([`ia/skills/surface-path-precheck/SKILL.md`](../surface-path-precheck/SKILL.md)) on paths from Exit criteria + Stage hints. Use returned `line_hint` in stage surfaces; mark `(new)` for `exists: false`. Skip → ghost line numbers downstream.

### Phase 2 — Stage decomposition

Expand the step into 2–4 stages. Apply the same ordering heuristic as `master-plan-new` Phase 5 (earliest first):

1. **Scaffolding / infrastructure** — persistent setup, new file skeletons, config, new bindings. No logic yet.
2. **Data model** — ScriptableObjects, structs, serialized fields. Typed but inert.
3. **Runtime logic** — algorithms, DSP kernels, update loops, compute. Consumes data model.
4. **Integration + tests** — call sites, EditMode/PlayMode tests, golden fixtures.

Deviate from this order only when prior step output forces a different dep chain — note deviation in the Decision Log block authored in Phase 4.

**Stage hints from `## Deferred decomposition`** are candidate stage names. Use them as starting scaffolding; reshape to fit the ordering heuristic + actual Exit criteria decomposition.

Per stage, author the block (verbatim shape):

```markdown
#### Stage {STEP_ID}.{M} — {Name}

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Objectives:** {1–3 sentences — what this stage lands}.

**Exit:**

- {observable outcome 1 — cites type / method / file path}
- {outcome 2}
- {glossary row additions, if canonical terms introduced}

**Phases:**

- [ ] Phase 1 — {shippable increment description}.
- [ ] Phase 2 — {...}.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T{STEP_ID}.{M}.1 | {short name ≤6 words} | 1 | _pending_ | _pending_ | {≤2 sentences — concrete deliverable: types, methods, file paths. Reference existing patterns where applicable.} |
| T{STEP_ID}.{M}.2 | {short name} | 1 | _pending_ | _pending_ | {...} |
| T{STEP_ID}.{M}.3 | {short name} | 2 | _pending_ | _pending_ | {...} |
```

**Task intent concreteness bar:** cite the thing being shipped — type names, method signatures, file paths, field names. Vague verbs ("add support for X") degrade into useless `stage-file` stubs. Match the bar set in `master-plan-new` Phase 5.

**Task sizing heuristic:**
- **Correct scope:** 2–5 files forming one algorithm layer. Target ≤2 `spec_section` reloads and meaningful per-phase deltas.
- **Too small (merge):** single file, single function, single struct with no logic → merge with adjacent same-domain task in the same phase.
- **Too large (split):** touches >3 unrelated subsystems or needs >6 phases → split at subsystem-layer seam.

### Phase 3 — Cardinality gate

Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass phase → tasks map from Phase 2 stage decomposition. Cardinality rule (`ia/rules/project-hierarchy.md`): ≥2 tasks/phase (hard), ≤6 soft.

Subskill returns `{phases_lt_2, phases_gt_6, single_file_tasks, oversized_tasks, verdict}`:
- `verdict = pause` → surface violations to user; ask split, merge, or justify in Decision Log. Proceed only after user confirms or fixes. Phrase the split/merge question in terms of what the player/designer sees change (user-visible checkpoints, releasable slices) — not stage numbers or task-count math. Ids / stage numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- `verdict = proceed` → continue to Phase 4.

Covers task sizing: single-file/function/struct tasks → `single_file_tasks`; >3 unrelated subsystems → `oversized_tasks`.

### Phase 3.5 — Sizing-gate evaluation

Run `ia/rules/stage-sizing-gate.md` heuristics on the Stage task cluster produced in Phase 2,
AFTER Phase 3 cardinality gate PASS. Evaluate all 6 heuristics (H1–H6) in order:

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
- **WARN-gate** (≥2 WARN, no FAIL) → emit warning block; ask planner to confirm or split.
  Proceed only on user confirmation.
- **FAIL** (any FAIL) → invoke fail-recurse protocol per `ia/rules/stage-sizing-gate.md` §3:
  emit split recommendation (X.Y.A / X.Y.B); run sizing gate on each proposed sub-stage.
  On second consecutive FAIL: user-gate prompt per §3.2 — do NOT auto-split a third time.
  DO NOT proceed to Phase 4 until gate PASS (or user-accepted waiver).

**Note:** gate evaluates NEW Stage drafts only. Already-filed Stages are grandfathered (§3.3).

---

### Phase 4 — Persist (in-place edit)

Edit `ORCHESTRATOR_SPEC` in three atomic operations:

**4a — Replace skeleton step block:**
Find the `### Step {STEP_ID}` block (from the skeleton `Status: Draft (decomposition deferred…)` line to the next `### Step` or `---` separator). Replace with the full decomposition authored in Phase 2:

```markdown
### Step {STEP_ID} — {Name}

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Step {STEP_ID}):** 0 filed

**Objectives:** {from skeleton — unchanged}

**Exit criteria:**

{from skeleton — unchanged}

**Art:** {from skeleton — unchanged}

**Relevant surfaces (load when step opens):**
- {prior step outputs — from Phase 0 working memory}
- {MCP-routed spec section refs (via Phase 1)}
- {invariant numbers from Phase 1}
- {code paths from Exit criteria — mark `(new)` per surface-path pre-check}

#### Stage {STEP_ID}.1 — {Name}
{...stage block...}

#### Stage {STEP_ID}.2 — {Name}
{...stage block...}
```

**4b — Update `## Deferred decomposition` section:**
Find the bullet for `Step {STEP_ID}` under `## Deferred decomposition`. Replace it with a single line:

```markdown
- **Step {STEP_ID} — {Name}:** decomposed {YYYY-MM-DD}. Stages: {comma-separated stage names}.
```

If the section has no remaining `_TBD_` bullets (all steps decomposed), add a note line: `All steps decomposed.`

**4c — Flip Step header Skeleton → Planned (R7):**
Find the `### Step {STEP_ID}` block's `**Status:**` line. If it reads `Skeleton` (or `Draft (decomposition deferred…)` or any Skeleton variant), rewrite it to:

```markdown
**Status:** Draft

**Notes:** tasks _pending_ — not yet filed
```

This flip signals that the step is now decomposed and ready for `stage-file` but no tasks are filed yet (`_pending_`). Do NOT flip to `In Progress` — that is `stage-file`'s responsibility (R2). Do NOT touch the plan top-of-file `> **Status:**` from here — `stage-file` owns that flip (R1). Flip is idempotent: if Status is already `Draft (tasks _pending_…)`, no change needed.

### Phase 5 — Progress regen

Run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block Phase 6; log exit code and continue.

### Phase 6 — Handoff

Single concise caveman message:

- `{ORCHESTRATOR_SPEC}` edited — Step {STEP_ID} decomposed: N stages · M phases · K tasks (all `_pending_`).
- Invariant numbers flagged (if any).
- Cardinality gate: violations resolved / justified.
- Deferred decomposition section updated.
- Next step: `claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {STEP_ID}.1"` when Step {STEP_ID-1} closes.

---

## Tool recipe (territory-ia) — Phase 1 only

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:

- `keywords`: English tokens from step Exit criteria + Stage hints + Relevant surfaces text.
- `brownfield_flag`: `true` for greenfield steps (no existing C# paths in Exit criteria / Relevant surfaces); `false` for brownfield (full recipe).
- `tooling_only_flag`: `true` for doc/IA/tooling-only steps.

Use returned `glossary_anchors` for canonical names; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for guardrail flags.

Also run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned by `domain-context-load`. **Brownfield fallback.**

---

## Guardrails

- IF `STEP_ID == 1` → STOP. Step 1 decomposed by `master-plan-new`; this skill handles Steps 2+ only.
- IF target step already has a task table → STOP, ask user to confirm overwrite before proceeding.
- IF cardinality gate finds any phase with <2 tasks → STOP, pause for user confirmation or fix.
- IF cardinality gate finds any phase with 7+ tasks → STOP, suggest split; persist only after user confirms.
- IF any task covers only 1 file / 1 function with no logic → warn + pause.
- IF any task spans >3 unrelated subsystems → warn + pause.
- Do NOT create `BACKLOG.md` rows. Do NOT create `ia/projects/{ISSUE_ID}.md` stubs. Tasks stay `_pending_` — `stage-file` materializes them.
- Do NOT decompose other steps beyond `STEP_ID` — lazy materialization applies; only the target step is expanded.
- Do NOT delete or rename the orchestrator doc.
- Do NOT commit — user decides when to commit.
- Do NOT skip `## Deferred decomposition` update — stale hints mislead future agents landing cold.

---

## Seed prompt

```markdown
Run the stage-decompose workflow.

Follow ia/skills/stage-decompose/SKILL.md end-to-end. Inputs:
  ORCHESTRATOR_SPEC: {path to *-master-plan.md}
  STEP_ID: {integer, e.g. 2}

Phase 1 Tool recipe uses territory-ia MCP slices (greenfield skips router / spec_sections / invariants_summary).
Cardinality gate requires ≥2 tasks per phase AND ≤6 soft — pause for user confirmation on either violation.
Only the target STEP_ID is decomposed; all other deferred steps remain as skeletons.
```

---

## Next step

After persist: `claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {STEP_ID}.1"` — but ONLY after Step {STEP_ID-1} reaches `Final`. Do NOT stage-file against a step whose predecessor is still open.

## Changelog
