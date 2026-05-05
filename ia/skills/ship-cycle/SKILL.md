---
name: ship-cycle
purpose: >-
  Stage-atomic batch ship: one Sonnet 4.6 inference body emits ALL tasks of one
  Stage with structured boundary markers. Drop-in replacement for `/ship-stage`
  Pass A two-pass loop when stage size fits one inference window. Falls back to
  ship-stage two-pass when batch exceeds token cap.
audience: agent
loaded_by: "skill:ship-cycle"
slices_via: none
description: >-
  Stage-atomic batch ship-cycle. One Sonnet 4.6 inference body emits ALL tasks
  of one Stage with `<!-- TASK:{ISSUE_ID} START/END -->` boundary markers.
  Replaces `/ship-stage` Pass A per-task loop when stage fits one window.
  Per-task `unity:compile-check` gate + `task_status_flip(implemented)` after
  batch lands. Pass B (`verify-loop` + closeout) reuses `/ship-stage` machinery.
  Failure mode = `ia_stages.status='partial'` (mig 0069); resume re-enters at
  first non-done task. Token budget hard cap 80k input. Validate gate =
  `validate:fast` (TECH-12640) on cumulative stage diff.
  Triggers: "/ship-cycle {SLUG} {STAGE_ID}", "ship cycle stage", "stage-atomic
  batch ship". Argument order (explicit): SLUG first, STAGE_ID second.
phases:
  - Parse args + load stage bundle
  - Token-budget preflight (hard cap 80k input → fallback ship-stage two-pass)
  - Bulk emit task-batch body with boundary markers
  - Per-task unity:compile-check gate + task_status_flip(implemented)
  - Hand off to ship-stage Pass B (verify-loop + closeout + commit)
triggers:
  - /ship-cycle {SLUG} {STAGE_ID}
  - ship cycle stage
  - stage-atomic batch ship
argument_hint: "{slug} Stage {X.Y} [--force-model {model}]"
model: sonnet
reasoning_effort: low
tools_role: pair-head
tools_extra:
  - mcp__territory-ia__stage_bundle
  - mcp__territory-ia__task_spec_body
  - mcp__territory-ia__task_status_flip
  - mcp__territory-ia__task_status_flip_batch
  - mcp__territory-ia__unity_compile
  - mcp__territory-ia__journal_append
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - Do NOT bypass token-budget preflight — over cap → fallback ship-stage two-pass.
  - Do NOT commit per task — Pass B owns single stage commit.
  - Do NOT skip `unity:compile-check` per task on Assets/**/*.cs touched.
  - Do NOT cross stage boundary — strictly one Stage per invocation.
  - Do NOT flip status outside `pending → implemented` in Pass-A-equivalent.
  - Do NOT run `verify-loop` here — handed to ship-stage Pass B.
  - Do NOT write task spec bodies to filesystem — DB sole source of truth.
caller_agent: ship-cycle
---

# Ship-cycle skill — stage-atomic batch ship (Pass-A-equivalent)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role.** Stage-atomic batch implement. One Sonnet 4.6 inference body emits all tasks of one Stage with `<!-- TASK:{ISSUE_ID} START/END -->` boundary markers. Drop-in for `/ship-stage` Pass A loop when token budget fits.

**Upstream:** `ship-plan` (populates §Plan Digest in DB). **Downstream:** `/ship-stage` Pass B (verify-loop + closeout + single commit). Pass B reuses ship-stage machinery — ship-cycle stops after `task_status_flip(implemented)` batch.

---

## Inputs

| Param | Source | Notes |
|---|---|---|
| `SLUG` | first positional arg | Bare master-plan slug (e.g. `ship-protocol`). Verified via `master_plan_state(slug)`. |
| `STAGE_ID` | second positional arg | e.g. `Stage 3` → `3`. |
| `--force-model {model}` | optional flag | Override frontmatter `model`. Valid: `sonnet`, `opus`, `haiku`. |

---

## Phase sequence

### Phase 0 — Parse args + load stage bundle

`stage_bundle(slug, stage_id)` once. Capture `tasks[]` (filed, non-terminal). Idle exit when stage `done` + tasks all terminal.

### Phase 1 — Token-budget preflight

Sum input bytes: stage bundle + per-task §Plan Digest body (DB read via `task_spec_body`). Hard cap 80k input → over cap = `STOPPED — token_budget_exceeded`; emit `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"` (fallback two-pass).

### Phase 2 — Bulk emit task-batch body

Single inference. Boundary markers per task: `<!-- TASK:{ISSUE_ID} START -->` ... `<!-- TASK:{ISSUE_ID} END -->`. Inside markers: full implementation diff body for that task. Greppable by validators / code-review subagents.

### Phase 3 — Per-task gate + flip

For each task in batch:

1. `unity:compile-check` if `Assets/**/*.cs` touched in this task's marker block.
2. `task_status_flip(task_id, implemented)`.
3. `journal_append({task_id, phase: "ship-cycle-pass-a", status: "implemented"})`.

Stop on first failure. Surviving tasks remain `implemented`; failed task → `STOPPED at {ISSUE_ID}`.

### Phase 4 — Hand off to ship-stage Pass B

Emit `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"` — Pass B resume gate (DB `task_state` query) sees all `implemented`, runs `PASS_B_ONLY` (verify-loop + closeout + commit).

---

## Boundary marker contract

```
<!-- TASK:TECH-12345 START -->
... full implementation body for TECH-12345 ...
<!-- TASK:TECH-12345 END -->

<!-- TASK:TECH-12346 START -->
... full implementation body for TECH-12346 ...
<!-- TASK:TECH-12346 END -->
```

- Markers are HTML comments — invisible in rendered markdown, greppable by tools.
- Each task block is self-contained — no cross-task references.
- Order = `tasks[]` order from `stage_bundle`.
- Mismatched / missing END marker → `STOPPED at {ISSUE_ID} — boundary_marker_unbalanced`.

---

## Escalation shape

```json
{
  "escalation": true,
  "phase": <int>,
  "reason": "token_budget_exceeded | boundary_marker_unbalanced | compile_check_failed | task_status_flip_failed",
  "task_id": "<optional>",
  "stderr": "<optional>"
}
```

---

## Output

Caveman summary: `ship-cycle done. STAGE_ID={S} BATCH_SIZE={N} IMPLEMENTED={K} SKIPPED={M}` + per-task `<TASK:ID> [implemented|skipped|failed]` rows + token usage + `Next:` handoff (Pass B or fallback).

---

## Changelog

(empty — initial author)
