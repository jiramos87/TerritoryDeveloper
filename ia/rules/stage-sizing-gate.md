---
purpose: "Stage sizing gate — 6-heuristic analytic check on a drafted Stage task cluster; gates bulk-verify + bulk-code-review feasibility. Layers on top of cardinality gate (project-hierarchy.md)."
audience: agent
loaded_by: skill:stage-decompose, skill:stage-file
slices_via: none
rule_key: stage-sizing-gate
description: >
  Six-heuristic analytic gate applied to a Stage task cluster post task-sizing (in stage-decompose)
  and at Phase 0 of stage-file planner pass. Any FAIL (or ≥2 WARN) triggers split recommendation:
  Stage X.Y → X.Y.A + X.Y.B, reauthor each. Distinct from cardinality gate (task count) and
  lifecycle-refactor F2 sizing gate (cache-block token bytes). LLM-prompt heuristic evaluation —
  no CI validator in v1.
---

# Stage sizing gate

> **Scope note:** "sizing gate" and "cardinality gate" are distinct guard layers on the Stage/Task
> hierarchy (per `ia/rules/project-hierarchy.md`). They are NOT the retired prose-Gate concept
> (folded into Stage Exit criteria pre-refactor). The cardinality gate bounds task count (≥2 hard /
> ≤6 soft); this rule gates task-cluster complexity for bulk-verify + bulk-code-review feasibility.

## §1 Purpose

Stage decomposition and Stage filing agents must verify that a drafted task cluster is feasible to
bulk-verify (Path A + optional Path B) and bulk-code-review in a single Opus pass. This rule
provides 6 analytic heuristics for that check. Failure triggers a recurse-split protocol: the Stage
is partitioned into X.Y.A / X.Y.B sub-stages before filing proceeds.

Applies to: NEW Stage drafts only. Existing filed Stages are grandfathered; do NOT apply
retroactively.

**Layer ordering:**

1. Cardinality gate (`project-hierarchy.md`) — task count check. Runs first.
2. This sizing gate — cluster complexity check. Runs post task-sizing, after cardinality gate PASS.

## §2 Heuristics table

Evaluate each heuristic in sequence. Each returns PASS / WARN / FAIL with a one-line rationale
citing the heuristic id (H1–H6).

| ID | Heuristic | PASS | WARN | FAIL | Estimation source |
|----|-----------|------|------|------|-------------------|
| H1 | **Subsystem cohesion** — count of distinct top-level subsystems touched by the Stage task cluster (per `ia/specs/architecture/layers.md` layer map: `Assets/Scripts/Managers/`, `ia/skills/`, `ia/rules/`, `tools/scripts/`, `web/`, `Assets/Art/`, etc.) | ≤2 subsystems | 3 subsystems | ≥4 subsystems | Union of `files:` fields across task yaml or Stage task Intent column |
| H2 | **Verify-path overlap** — Path A (compile / Node validators) vs Path B (bridge Play Mode) scenarios shared across tasks | All tasks share same verify path (all Path A or all Path B) | Tasks split across Path A + Path B but Path B cluster ≤1 task | Path B cluster ≥2 tasks within Stage | Task Intent + `ia/backlog/{id}.yaml` `files:` subsystem prefix |
| H3 | **Diff LOC budget** — estimated cumulative LOC diff across all Stage tasks | ≤800 LOC / ≤25 files | 801–1200 LOC or 26–40 files | ≥1201 LOC or ≥41 files | Union of `files:` lists across backlog yaml records; NOT live diff |
| H4 | **Depends-on DAG linearity** — task dependency graph shape within the Stage | Linear chain or independent (no declared `depends_on` within Stage) | ≤1 fan-out node (one task depended on by 2) | Fan-out ≥2 or DAG depth ≥3 | `depends_on:` fields in task yaml; empty DAG = PASS by default |
| H5 | **Invariant-hotspot density** — count of Stage tasks touching the same system invariant (`ia/rules/invariants.md` #1–#13) | 0 tasks share an invariant (or all tasks disjoint) | 2 tasks share one invariant | ≥2 tasks share the same invariant OR any invariant touched by 3+ tasks | Task Intent + `files:` subsystem; flag shared `HeightMap`, `RoadCache`, `GridManager` touches |
| H6 | **Compile-break probability** — count of tasks mutating shared C# class members (same class file modified by multiple tasks) | ≤1 task touches any given C# class | 2 tasks touch the same C# class | ≥3 tasks touch same class OR ≥2 tasks write to same method/field | `files:` union; flag repeated `Assets/Scripts/**/*.cs` paths |

**Gate outcome rule:**

- **PASS** — all 6 heuristics PASS or ≤1 WARN: proceed to Stage filing.
- **WARN-gate** — ≥2 WARN (any combination, no FAIL): emit warning block + ask planner to
  accept or split; proceed only after user confirmation.
- **FAIL** — any single FAIL: gate FAIL → invoke fail-recurse protocol (§3).

## §3 Fail-recurse protocol

### 3.1 Split proposal

On gate FAIL, the calling agent emits a split recommendation block:

```
SIZING GATE FAIL — Stage {X.Y}
Failed heuristics: {H-ids with rationale}
Recommended split:
  Stage {X.Y.A} — Tasks: {subset A — grouped by dominant subsystem / DAG cut}
  Stage {X.Y.B} — Tasks: {subset B}
Action required: reauthor each sub-stage; run sizing gate on each before filing.
```

Partition strategy (priority order):

1. **Subsystem cut (H1 fail):** group tasks by their dominant subsystem layer. Assign each
   cohesive cluster to one sub-stage.
2. **Verify-path cut (H2 fail):** group all Path A tasks in one sub-stage; Path B tasks in
   another.
3. **DAG cut (H4 fail):** cut at fan-out node; upstream tasks form sub-stage A; downstream B.
4. **LOC cut (H3 fail):** split largest task group evenly until each sub-stage is under budget.
5. **Invariant cut (H5/H6 fail):** isolate invariant-touching tasks in their own sub-stage; keep
   read-only tasks separate.

Sub-stage naming: `X.Y.A`, `X.Y.B` (append letter suffix to the parent Stage id). If parent
already has a letter suffix (e.g. `X.Y.A`), escalate to user — triple-split signals step-level
scope problem.

### 3.2 Recurse budget

- **First fail:** agent auto-proposes split + runs sizing gate on each proposed sub-stage.
  Proceed if both sub-stages PASS.
- **Second consecutive fail (sub-stage also FAIL after first split):** agent emits user-gate
  prompt — do not auto-split again. Prompt (product-domain phrasing):

  > "This part of the plan covers too many moving pieces to ship cleanly in one batch. You can:
  > (a) Accept the oversized Stage with a documented waiver — the work ships but bulk review
  >     may lose signal quality.
  > (b) Re-scope at Step level — move some goals to a later Step.
  > Context: Stage {X.Y.B} gate FAIL ({heuristic ids}); second consecutive auto-split exhausted."

  Wait for user decision before proceeding. Do NOT auto-split a third time.

- **Waiver path:** if user chooses (a), record waiver in master plan Stage block:
  `<!-- sizing-gate-waiver: {reason}; accepted {YYYY-MM-DD} -->`. Gate not re-evaluated for
  this Stage on subsequent runs.

### 3.3 Grandfathering

Sizing gate applies to Stage drafts only. Already-filed Stages (Status ≠ Draft / _pending_)
are NOT subject to retroactive evaluation. Gate verdict = N/A for filed Stages.

## §4 Cross-references

- `ia/rules/project-hierarchy.md` — cardinality gate (task count ≥2 / ≤6); runs BEFORE this gate.
- `ia/rules/invariants.md` — system invariants #1–#13 referenced in H5 / H6 evaluation.
- `ia/skills/stage-decompose/SKILL.md` — invokes sizing gate at Phase N (post task-sizing).
- `ia/skills/stage-file/SKILL.md` — planner Phase 0 references this gate; halts on FAIL.
- `docs/agent-lifecycle.md` — Hard-rules section cross-ref (single line).
- `ia/specs/architecture/layers.md` — subsystem layer map used in H1 subsystem count.
- `docs/stage-scoped-verify-review-findings.md` — motivating findings; thresholds derived from
  empirical Stage observations logged there.
