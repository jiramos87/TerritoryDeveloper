---
purpose: "TECH-731 — spec.py vary-block merge: preset axes preserved, author union, wipe raises."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.6.2
---
# TECH-731 — `vary:` block merge rule (union + non-wipe)

> **Issue:** [TECH-731](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend the preset loader (TECH-730) with a dedicated rule for the `vary:` block: preset axes survive by default, the author may add new axes or override individual axis values, but attempting to wipe the whole block (`vary: {}` or `vary: null`) raises `SpecError`. Guarantees preset-driven variation can't be silently disabled.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Preset `vary.*` axes survive unless explicitly overridden per axis.
2. Author-supplied new axes merge in via set-union.
3. `vary: {}` / `vary: null` from author → `SpecError`.

### 2.2 Non-Goals

1. Base preset loader — TECH-730.
2. Preset content / seed files — TECH-732/733/734.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Add `vary.padding` without losing preset `vary.roof` | Both axes live after merge |
| 2 | Spec author | Override preset `vary.roof.values` | Author values win on that axis |
| 3 | Repo guardian | Prevent variation wipe | Author `vary: null` raises |

## 4. Current State

### 4.1 Domain behavior

TECH-730's deep-merge treats `vary:` like any other dict — author wiping / emptying it would take effect silently.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py` — merge path; extend here.
- Consumer: `tests/test_preset_system.py` (TECH-735).

### 4.3 Implementation investigation notes

Detect wipe intent both at parse time (`vary: null` → `None` in Python) and structural (`vary: {}` → empty dict). Per-axis override is a shallow dict merge, not deep — axis values are lists or scalars.

## 5. Proposed Design

### 5.1 Target behavior

| Preset `vary:` | Author `vary:` | Result |
|----------------|----------------|--------|
| `{roof: [A, B]}` | absent | `{roof: [A, B]}` |
| `{roof: [A, B]}` | `{padding: [0, 1]}` | `{roof: [A, B], padding: [0, 1]}` |
| `{roof: [A, B]}` | `{roof: [C, D]}` | `{roof: [C, D]}` (author wins axis) |
| `{roof: [A, B]}` | `{}` | **SpecError** |
| `{roof: [A, B]}` | `null` | **SpecError** |

### 5.2 Architecture / implementation

- Branch out of `_deep_merge` for `vary:` key: detect empty / null → raise.
- Axis-level overlay via shallow dict merge.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Wipe raises instead of silently dropping the block | Presets promise variation; silent disable is a correctness bug | Warn + keep preset — rejected, author intent is ambiguous |
| 2026-04-23 | Per-axis override, not per-value | Keeps author spec readable | Recursive list-merge — rejected, lists of scalars are the typical shape |

## 7. Implementation Plan

### Phase 1 — Detect wipe intent + raise

### Phase 2 — Union merge for preserved axes

### Phase 3 — Per-axis override for overridden axes

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Author `padding` doesn't erase preset `roof` | Python | `pytest tests/test_preset_system.py::test_vary_union -q` | TECH-735 |
| Author `vary.roof` replaces preset | Python | `pytest tests/test_preset_system.py::test_vary_axis_override -q` | TECH-735 |
| `vary: null` raises | Python | `pytest tests/test_preset_system.py::test_vary_wipe_raises -q` | TECH-735 |

## 8. Acceptance Criteria

- [ ] Preset axes preserved unless explicitly overridden per axis.
- [ ] Author new axes merge in (union).
- [ ] `vary: {}` or `vary: null` → `SpecError`.
- [ ] Unit tests cover all three cases (in TECH-735).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Silent-disable paths are the sharpest edge in layered-config systems — always prefer an early raise over a soft default.

## §Plan Digest

### §Goal

Guarantee preset-supplied `vary:` axes survive author overrides unless the author explicitly replaces them per-axis; refuse whole-block wipes.

### §Acceptance

- [ ] Merge preserves preset `vary.*` axes when author has no `vary:` key
- [ ] Author `vary.padding` merges with preset `vary.roof` (union)
- [ ] Author `vary.roof` replaces preset `vary.roof` (per-axis override)
- [ ] Author `vary: {}` raises `SpecError`
- [ ] Author `vary: null` raises `SpecError`
- [ ] `pytest tools/sprite-gen/tests/test_preset_system.py -q -k vary` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_vary_union | preset `{roof}` + author `{padding}` | both axes present | pytest |
| test_vary_axis_override | preset `{roof:[A,B]}` + author `{roof:[C,D]}` | `[C,D]` | pytest |
| test_vary_wipe_empty_raises | author `vary: {}` | SpecError | pytest |
| test_vary_wipe_null_raises | author `vary: null` | SpecError | pytest |
| test_vary_absent_preserved | no author `vary` | preset `vary` survives intact | pytest |

### §Examples

```python
# tools/sprite-gen/src/spec.py (excerpt — builds on TECH-730's _deep_merge)

def _merge_vary(base_vary: dict, overlay_vary) -> dict:
    if overlay_vary is None or overlay_vary == {}:
        raise SpecError(
            "author spec may not wipe preset `vary:` block — "
            "override individual axes instead"
        )
    merged = dict(base_vary or {})
    for axis, values in overlay_vary.items():
        merged[axis] = values  # per-axis replace
    return merged

# hook into _deep_merge:
#   if key == "vary": out[k] = _merge_vary(out.get(k, {}), v)
```

### §Mechanical Steps

#### Step 1 — Wipe-guard

**Edits:**

- `tools/sprite-gen/src/spec.py` — `_merge_vary` raises on empty dict / None.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py -q -k wipe
```

#### Step 2 — Union + per-axis override

**Edits:**

- Same file — `_merge_vary` union / axis-replace.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py -q -k "union or axis_override"
```

#### Step 3 — Wire into `_deep_merge`

**Edits:**

- Same file — branch on `key == "vary"` inside `_deep_merge`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py -q -k vary
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Should an explicit `vary.<axis>: null` from the author disable that one axis? **Resolution:** yes — per-axis null is a targeted disable (documented in TECH-736 DAS addendum); whole-block wipe is what we refuse.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
