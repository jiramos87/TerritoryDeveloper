---
purpose: "TECH-741 — slots.py parser: tiled-(row|column)-N regex, N>=2 validation, legacy alias."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T9.2.1
---
# TECH-741 — Slot name grammar — `tiled-(row|column)-N` parser

> **Issue:** [TECH-741](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Parse the parametric slot name `tiled-(row|column)-N` via regex in `tools/sprite-gen/src/slots.py`. Capture axis ∈ {row, column} and integer `N`; validate `N ≥ 2`; otherwise raise `SpecError` naming the offending slot string. Hard-coded legacy names (`tiled-row-3`, `tiled-row-4`, `tiled-column-3`) from Stage 9 T9.2 alias through this parser for back-compat.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Regex `^tiled-(row|column)-(\d+)$` captures axis + `N`.
2. `N < 2` or non-int `N` raises `SpecError` with offending name.
3. Hard-coded legacy names still parse without error.

### 2.2 Non-Goals

1. Slot resolution / pixel math — TECH-742.
2. Tests — TECH-743.
3. Doc amendment — TECH-744.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Write `tiled-row-5` for a 5-house row | Parser accepts without special case |
| 2 | Spec author | Typo `tiled-row-1` | `SpecError` with the bad name + hint |
| 3 | Stage 9 consumer | Legacy `tiled-row-3` still works | Alias through parser unchanged |

## 4. Current State

### 4.1 Domain behavior

`slots.py` (scaffold under Stage 9 T9.2, not yet filed as its own tasks) expects a fixed enum of slot names; no regex parse.

### 4.2 Systems map

- `tools/sprite-gen/src/slots.py` — parser entry here.
- Consumer: `resolve_slot` (TECH-742).

### 4.3 Implementation investigation notes

Keep regex module-level constant so TECH-742 imports directly. Return a small parsed shape — `(axis: str, N: int)` — not a heavier dataclass.

## 5. Proposed Design

### 5.1 Target behavior

```python
>>> parse_slot("tiled-row-5")
("row", 5)
>>> parse_slot("tiled-column-3")
("column", 3)
>>> parse_slot("tiled-row-1")
SpecError: slot 'tiled-row-1' has N<2; parametric slots require N≥2
>>> parse_slot("tiled-foo-3")
SpecError: slot 'tiled-foo-3' does not match tiled-(row|column)-N grammar
```

### 5.2 Architecture / implementation

- `_SLOT_RE = re.compile(r"^tiled-(row|column)-(\d+)$")`.
- `parse_slot(name) → (axis, N)` — raise on no match or `N < 2`.
- Legacy accept path: hard-coded slot names from T9.2 always match; no special case needed.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Regex parse not enum | Parametric grammar = infinite enum | Enumerate — rejected, doesn't scale |
| 2026-04-23 | Return `(axis, N)` tuple | Minimal surface; no import cost | Dataclass — rejected, overhead |

## 7. Implementation Plan

### Phase 1 — Regex + capture

### Phase 2 — `N ≥ 2` validation

### Phase 3 — Legacy alias verification

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Accept row/col for N=2..5 | Python | `pytest tests/test_parametric_slots.py -q -k parse_accept` | TECH-743 |
| Reject `N<2` / malformed | Python | `pytest tests/test_parametric_slots.py -q -k parse_reject` | TECH-743 |
| Legacy aliases pass | Python | `pytest tests/test_parametric_slots.py -q -k legacy` | TECH-743 |

## 8. Acceptance Criteria

- [ ] Regex parse captures axis ∈ {row, column} and `N`.
- [ ] `N < 2` or non-int raises `SpecError` with offending name.
- [ ] Hard-coded legacy names (`tiled-row-3/4`, `tiled-column-3`) still accepted.
- [ ] Unit tests cover accept + reject paths (in TECH-743).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Parametric grammars via regex scale to unbounded N without touching enum tables — one-time upfront cost; compounds forever.

## §Plan Digest

### §Goal

Replace fixed slot-name enum with a parametric parser so `tiled-row-N` / `tiled-column-N` work for any `N ≥ 2`, unblocking larger row compositions and the `row_houses_3x` preset.

### §Acceptance

- [ ] `parse_slot("tiled-row-5")` returns `("row", 5)`
- [ ] `parse_slot("tiled-column-3")` returns `("column", 3)`
- [ ] `parse_slot("tiled-row-1")` raises `SpecError`
- [ ] `parse_slot("tiled-foo-3")` raises `SpecError`
- [ ] Legacy names (`tiled-row-3`, `tiled-row-4`, `tiled-column-3`) all parse successfully

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_parse_row_variants | `tiled-row-2..5` | `("row", N)` for each | pytest |
| test_parse_column_variants | `tiled-column-2..5` | `("column", N)` for each | pytest |
| test_parse_rejects_n_lt_2 | `tiled-row-1` | SpecError | pytest |
| test_parse_rejects_malformed | `tiled-foo-3`, `tiled-row-`, `row-3` | SpecError | pytest |
| test_legacy_aliases | existing Stage 9 names | all parse | pytest |

### §Examples

```python
# tools/sprite-gen/src/slots.py (excerpt)
import re

_SLOT_RE = re.compile(r"^tiled-(row|column)-(\d+)$")

class SpecError(Exception): ...

def parse_slot(name: str) -> tuple[str, int]:
    m = _SLOT_RE.match(name)
    if not m:
        raise SpecError(
            f"slot {name!r} does not match tiled-(row|column)-N grammar"
        )
    axis, n_str = m.group(1), m.group(2)
    n = int(n_str)
    if n < 2:
        raise SpecError(
            f"slot {name!r} has N<2; parametric slots require N≥2"
        )
    return axis, n
```

### §Mechanical Steps

#### Step 1 — Regex + `parse_slot` helper

**Edits:**

- `tools/sprite-gen/src/slots.py` — add `_SLOT_RE` + `parse_slot(name)`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.slots import parse_slot; print(parse_slot('tiled-row-5'))"
```

#### Step 2 — `N ≥ 2` validation

**Edits:**

- Same file — raise on `N < 2`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.slots import parse_slot, SpecError
try: parse_slot('tiled-row-1')
except SpecError as e: print('OK:', e)
"
```

#### Step 3 — Legacy alias verification

**Edits:** none — regex already covers legacy names; add sanity check only.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_parametric_slots.py -q -k legacy
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does `slots.py` exist yet at Stage 6.6/9-addendum filing time? **Resolution:** scaffold the file if missing (Stage 9 T9.2 will later consolidate); TECH-741 owns the first landing.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
