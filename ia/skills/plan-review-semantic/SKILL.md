---
name: plan-review-semantic
purpose: >-
  Run semantic drift scan (checks 1–2) across Stage Task specs. Reads plan-reviewer-mechanical output.
  Emits §Plan Fix — SEMANTIC tuple appendix per plan-apply-pair-contract.
audience: agent
loaded_by: ondemand
slices_via: none
description: >-
  Run semantic drift scan (checks 1–2) across Stage Task specs. Reads plan-reviewer-mechanical output.
  Emits §Plan Fix — SEMANTIC tuple appendix per plan-apply-pair-contract.
phases:
  - read_mechanical_output
  - check_1_goal_intent
  - check_2_completeness
  - emit_tuples
triggers: []
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Mission

Semantic drift scan over Stage Task specs. Two checks (1–2). Reads `plan-reviewer-mechanical` output as input bundle (mechanical context already loaded — do NOT re-call lifecycle_stage_context). Emits `§Plan Fix — SEMANTIC` tuple appendix per `ia/rules/plan-apply-pair-contract.md`.

# Phases

## Phase 1 — read_mechanical_output

Read `plan-reviewer-mechanical` output bundle from invocation context. Extract: Stage id, Task list, mechanical PASS/FAIL verdict. If mechanical is FAIL → note in combined verdict but still run semantic checks (checks are independent).

## Phase 2 — check_1_goal_intent

For each Task spec:

1. Read `§1 Summary` + `§2 Problem/Goal` via `mcp__territory-ia__spec_section({issue_id, section: "1"})` + `section: "2"`.
2. Read master plan Task row description.
3. Call `mcp__territory-ia__backlog_issue({id})` → compare `acceptance_criteria` against spec `§2 Goal`.
4. Flag divergence where spec goal **contradicts** (not merely elaborates) plan intent.
5. Fail record: `{task, spec_section, plan_section, divergence_description}`.

## Phase 3 — check_2_completeness

For each Task spec:

1. Read `§Plan Digest` mechanical steps.
2. Read `backlog_issue.acceptance_criteria` list.
3. For each acceptance criterion, verify at least one mechanical step is traceable (keyword match or explicit reference).
4. Untraced criterion → fail: record `{task, criterion, gap_description}`.

## Phase 4 — emit_tuples

Collect all failures → emit `§Plan Fix — SEMANTIC` tuple list per `ia/rules/plan-apply-pair-contract.md`. Emit combined verdict:

- `PASS` only when mechanical input PASS AND semantic checks PASS.
- Otherwise list all failing checks.

If zero semantic failures → emit `PASS — no semantic drift found (checks 1–2)`.

# Output shape

```markdown
## §Plan Fix — SEMANTIC (Stage {STAGE_ID})

- id: fix-{N}
  check: {1|2}
  task: {ISSUE_ID}
  issue: {description}
  fix: {Edit tuple per plan-apply-pair-contract}
  validator_gate: {gate command}
  invariant_touchpoints: none (utility)

**Combined verdict:** PASS | FAIL (mechanical: {PASS|FAIL}, semantic: {PASS|FAIL})
```

# Hard boundaries

- Do NOT run mechanical checks (3–8).
- Do NOT edit spec files.
- Do NOT commit.
- Flag divergence only — do NOT rewrite spec intent to match plan.
