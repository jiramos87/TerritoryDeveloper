---
purpose: "TECH-745 — spec.py accepts ground.passthrough: bool (default false); non-bool raises SpecError."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T7.10.1
---
# TECH-745 — Spec schema: `ground.passthrough` flag

> **Issue:** [TECH-745](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/spec.py` to accept a new `ground.passthrough: bool` flag sibling of `ground.material`. Default value `false`. Non-bool raises `SpecError`. Consumes lock **L17** (cross-tile passthrough pattern).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ground.passthrough: true|false` parses clean.
2. Default value `false` when key absent.
3. Non-bool value raises `SpecError`.

### 2.2 Non-Goals

1. Composer render behaviour — TECH-746.
2. Tests — TECH-747.
3. DAS amendment — TECH-748.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Mark a tile as a neighbor-blending bridge | `ground.passthrough: true` parses |
| 2 | Spec author | Omit the flag | Default `false` assumed |
| 3 | Repo guardian | Prevent typo like `passthrough: 1` | `SpecError` on non-bool |

## 4. Current State

### 4.1 Domain behavior

Stage 6.4 added `ground` as an object form with `material`, `materials`, `hue_jitter`, `value_jitter`, `texture`. No `passthrough` key yet.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py` — loader entry.
- Consumer: `compose.py` ground render path (TECH-746).

### 4.3 Implementation investigation notes

Keep the default (`false`) explicit in the resolved spec — composer branches on `spec.ground.get("passthrough", False)` already, but an explicit default helps the resolver test coverage.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
ground:
  material: grass
  passthrough: true   # new flag
```

Absent → `false`. Non-bool (e.g. `passthrough: 1` or `passthrough: "yes"`) → `SpecError`.

### 5.2 Architecture / implementation

- `_validate_ground(ground)` helper — type-check `passthrough` if present.
- Default materialisation: set `resolved_ground.passthrough = False` when absent.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Reject non-bool at load time | Catches typos early | Coerce truthy — rejected, masks intent |
| 2026-04-23 | Default `false` explicit in resolved spec | Composer can trust the key exists | Default implicit — rejected, test churn |

## 7. Implementation Plan

### Phase 1 — Type guard on ground block

### Phase 2 — Default propagation

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Flag parses | Python | `pytest tests/test_ground_passthrough.py::test_passthrough_flag_parses -q` | TECH-747 |
| Default false | Python | `pytest tests/test_ground_passthrough.py::test_passthrough_default_false -q` | TECH-747 |
| Non-bool raises | Python | `pytest tests/test_ground_passthrough.py::test_passthrough_non_bool_raises -q` | TECH-747 |

## 8. Acceptance Criteria

- [ ] `ground.passthrough: true|false` parses clean.
- [ ] Default value `false` when key absent.
- [ ] Non-bool value raises `SpecError`.
- [ ] Unit tests cover accept + default + raise paths (in TECH-747).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Bool flags with strict type-check beat truthy-accept in spec grammars — silent type coercion hides author intent.

## §Plan Digest

### §Goal

Add a strict `ground.passthrough: bool` flag to the sprite-gen spec grammar so future passthrough-rendering behavior (TECH-746) has a stable switch to branch on.

### §Acceptance

- [ ] Spec with `ground.passthrough: true` loads clean; resolved spec has `ground.passthrough == True`
- [ ] Spec with no `passthrough` key loads clean; resolved spec has `ground.passthrough == False`
- [ ] Spec with `ground.passthrough: "yes"` or `1` or `null` raises `SpecError`
- [ ] Resolved ground dict always has `passthrough` key (default materialised)
- [ ] `pytest tools/sprite-gen/tests/test_ground_passthrough.py -q -k flag_or_default_or_non_bool` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_passthrough_flag_parses | `ground.passthrough: true` | resolved `passthrough == True` | pytest |
| test_passthrough_default_false | no `passthrough` key | resolved `passthrough == False` | pytest |
| test_passthrough_non_bool_raises | `passthrough: "yes"` | SpecError | pytest |
| test_passthrough_int_raises | `passthrough: 1` | SpecError | pytest |
| test_passthrough_null_raises | `passthrough: null` | SpecError | pytest |

### §Examples

```python
# tools/sprite-gen/src/spec.py (excerpt)
def _validate_ground(ground: dict) -> dict:
    pt = ground.get("passthrough", False)
    if not isinstance(pt, bool):
        raise SpecError(
            f"ground.passthrough must be bool; got {type(pt).__name__}={pt!r}"
        )
    ground["passthrough"] = pt
    return ground
```

### §Mechanical Steps

#### Step 1 — Type guard

**Edits:**

- `tools/sprite-gen/src/spec.py` — add `_validate_ground(ground)` with `passthrough` type check.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.spec import _validate_ground, SpecError
try: _validate_ground({'material':'grass','passthrough':'yes'})
except SpecError as e: print('OK:', e)
"
```

#### Step 2 — Default propagation

**Edits:**

- Same file — set default `False` in resolved ground dict.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.spec import _validate_ground
g = _validate_ground({'material':'grass'})
assert g['passthrough'] is False
print('default OK')
"
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does `ground` ever legally arrive as a scalar (pre-Stage-6.4 form)? **Resolution:** Stage 6.4 (TECH-715) formalised object form; scalar path is the pre-migration compat shim — type-check should only run on dict shape.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
