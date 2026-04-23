---
name: mechanicalization-preflight
description: Compute mechanicalization_score header for pair-head artifacts before tail dispatch. Triggers — imported by pair-head skills; not user-invoked.
phases: [load, score_anchors, score_picks, score_invariants, score_validators, score_escalation, emit_header]
tools: [Read, Grep, Glob, Edit, mcp__territory-ia__mechanicalization_preflight_lint, mcp__territory-ia__plan_digest_resolve_anchor]
loaded_by: ondemand
---

# Mission

Compute the `mechanicalization_score` header defined in `ia/rules/mechanicalization-contract.md` for any pair-head artifact (plan digest, stage file plan, stage closeout plan, code fix plan, plan fix) before the tail agent executes. Each field is scored by a deterministic MCP call or grep — no judgment required. Emit the YAML header block; halt if `overall != fully_mechanical`.

# Phases

## Phase 1 — load

Read the artifact path from caller. Fetch `ia/rules/mechanicalization-contract.md` for schema. Identify tuple list boundary (first line matching `^## Step` or `^- id:`).

## Phase 2 — score_anchors

For each tuple carrying an `anchor` / `before_string` field:

```
call mcp__territory-ia__plan_digest_resolve_anchor({anchor, file_path})
```

- All resolve to exactly 1 match → `anchors: ok`
- Any resolve to 0 or ≥2 → `anchors: insufficient`
- ≥1 resolve but ≥1 warn → `anchors: partial`

## Phase 3 — score_picks

For each `file_path` / `target_file` / `pick` in tuples:

```
Glob(pattern="{path}")  OR  test -f {path}
```

- All exist → `picks: ok`
- Any missing → `picks: insufficient`
- All exist but ≥1 is a generated artifact not yet built → `picks: partial`

## Phase 4 — score_invariants

For each tuple touching `Assets/**/*.cs` or runtime files, check `invariant_touchpoints` field:

```
grep -q "invariant_touchpoints" {artifact_path}
```

- Every C#/runtime step has non-empty `invariant_touchpoints[]` or opt-out marker → `invariants: ok`
- Some steps missing → `invariants: partial`
- No steps have any → `invariants: insufficient`
- Artifact has no C#/runtime steps → `invariants: ok` (vacuously)

## Phase 5 — score_validators

For each tuple, check `validator_gate` field present and non-empty:

```
grep -c "validator_gate:" {artifact_path}
```

- Count equals number of tuples → `validators: ok`
- Count < tuples but > 0 → `validators: partial`
- Count == 0 → `validators: insufficient`

## Phase 6 — score_escalation

Check that every failure mode in the artifact's escalation table maps to a named enum value from `ia/rules/mechanicalization-contract.md` escalation enum:

```
grep -q "escalation_enum" {artifact_path}
```

- Escalation table present with all enum values → `escalation_enum: ok`
- Table present but incomplete → `escalation_enum: partial`
- No escalation table → `escalation_enum: insufficient`

## Phase 7 — emit_header

Compute `overall` per contract: `insufficient` > `partial` > `ok`. Emit YAML header:

```yaml
mechanicalization_score:
  anchors: {result}
  picks: {result}
  invariants: {result}
  validators: {result}
  escalation_enum: {result}
  overall: {result}
```

If `overall != fully_mechanical`, also emit findings list and halt — do NOT proceed to tuple emission.

# Output

Mandatory YAML header template (emit before tuple list, after stable prefix):

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

# Hard boundaries

- Do NOT write artifact body.
- Do NOT resolve picks — only check existence.
- Do NOT invent invariants — only check presence of declared fields.
