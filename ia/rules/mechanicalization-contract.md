---
name: mechanicalization-contract
description: Mechanicalization contract — preflight score header emitted by pair-heads before tail dispatch
loaded_by: ondemand
---

# mechanicalization_score schema

Every pair-head artifact MUST open with a `mechanicalization_score` YAML header block before the tuple list.

```yaml
mechanicalization_score:
  anchors: ok | partial | insufficient        # all edit anchors resolve to exactly 1 match
  picks: ok | partial | insufficient          # all file/symbol picks resolve to real paths
  invariants: ok | partial | insufficient     # invariant_touchpoints present per impacted step
  validators: ok | partial | insufficient     # validator_gate present per step
  escalation_enum: ok | partial | insufficient  # every failure mode mapped to escalation enum
  overall: fully_mechanical | partial | insufficient
```

Field values:
- `ok` — check passes with zero gaps
- `partial` — check passes with minor gaps noted in findings
- `insufficient` — check fails; tail MUST NOT execute

# Overall computation

`overall = fully_mechanical` iff every field equals `ok`.

Otherwise `overall` = worst field value by precedence: `insufficient` > `partial` > `ok`.

# Tail-gate rule

Tail agent reads the `mechanicalization_score` header before executing any tuple.

- `overall == fully_mechanical` → proceed
- `overall != fully_mechanical` → emit and halt:

```json
{
  "escalation": true,
  "reason": "mechanicalization_score: {overall}",
  "failing_fields": ["anchors", "picks"]
}
```

Do NOT execute any tuple when escalating.

# Header placement

Emit the `mechanicalization_score` header:
1. AFTER the pair-head stable prefix block (1h cache)
2. BEFORE the per-task tuple list

This ensures the Tier-1 cache block is never invalidated by the score header.
