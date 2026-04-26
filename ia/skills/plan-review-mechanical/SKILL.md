---
name: plan-review-mechanical
purpose: >-
  Run mechanical drift scan (checks 3–8) across Stage Task specs. Emits §Plan Fix — MECHANICAL tuple
  list per plan-apply-pair-contract.
audience: agent
loaded_by: ondemand
slices_via: none
description: >-
  Run mechanical drift scan (checks 3–8) across Stage Task specs. Emits §Plan Fix — MECHANICAL tuple
  list per plan-apply-pair-contract.
phases:
  - load_context
  - check_3_anchors
  - check_4_paths
  - check_5_gates
  - check_6_invariants
  - check_7_glossary
  - check_8_schema
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

Mechanical drift scan across all §Plan Digest sections of one Stage. Six checks (3–8), all deterministic — no judgment. Emits `§Plan Fix — MECHANICAL` tuple list per `ia/rules/plan-apply-pair-contract.md`.

# Phases

## Phase 1 — load_context

Call `mcp__territory-ia__lifecycle_stage_context({master_plan_path, stage_id})`. Collect Stage block + Task spec §Plan Digest sections + invariants + glossary terms. Bash fallback: read master plan → collect task ids → read each spec → call `invariants_summary`.

## Phase 2 — check_3_anchors

For each §Plan Digest tuple containing a `before_string` or `anchor` field:

```
Grep(pattern="{before_string}", path="{target_file}")
```

Expect exactly 1 match. 0 or ≥2 → fail: record `{task, step_id, anchor_path, grep_count}`.

## Phase 3 — check_4_paths

For each `file_path` / `target_file` pick in §Plan Digest tuples:

```
test -f {path}  (via Bash or Glob)
```

Missing → fail: record `{task, step_id, path}`.

## Phase 4 — check_5_gates

For each mechanical step in §Plan Digest:

- Verify `validator_gate` field present and non-empty.
- Missing → fail: record `{task, step_id}`.

## Phase 5 — check_6_invariants

For each step touching `Assets/**/*.cs` or runtime identifiers:

- Verify `invariant_touchpoints[]` present OR opt-out marker `invariant_touchpoints: none (utility)`.
- Missing → fail: record `{task, step_id}`.

## Phase 6 — check_7_glossary

For key domain terms in step prose (identified by ALL-CAPS or backtick-quoted identifiers):

```
mcp__territory-ia__glossary_lookup({term})
```

- Term not in glossary AND not in codebase → fail with `{task, step_id, term}`.
- Note: only terms from `ia/specs/glossary.md` authority; invented synonyms = fail.

## Phase 7 — check_8_schema

Verify §Plan Digest field names match schema in `ia/rules/plan-digest-contract.md`:

- Required fields: `operation`, `target_path`, `before_string`, `after_string`, `validator_gate`, `invariant_touchpoints`.
- Extra unknown fields → warn (not fail).
- Missing required field → fail: record `{task, step_id, missing_field}`.

## Phase 8 — emit_tuples

Collect all failures → emit `§Plan Fix — MECHANICAL` tuple list per `ia/rules/plan-apply-pair-contract.md`.

If zero failures → emit `PASS — no mechanical drift found (checks 3–8)`.

# Output shape

```markdown
## §Plan Fix — MECHANICAL (Stage {STAGE_ID})

- id: fix-{N}
  check: {3|4|5|6|7|8}
  task: {ISSUE_ID}
  step: {step_id}
  issue: {description}
  fix: {Edit/Bash tuple per plan-apply-pair-contract}
  validator_gate: {gate command}
  invariant_touchpoints: none (utility)
```

# Hard boundaries

- Do NOT run semantic checks (1, 2).
- Do NOT edit spec files.
- Do NOT commit.
- Do NOT infer glossary terms — only check existence via `glossary_lookup`.
