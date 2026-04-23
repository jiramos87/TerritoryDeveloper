---
purpose: "TECH-743 — tests/test_parametric_slots.py: parser accept/reject + distribute correctness for N in {2,3,4,5} + column mirror."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T9.2.3
---
# TECH-743 — Tests — `test_parametric_slots.py`

> **Issue:** [TECH-743](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

One test file locking the parametric slot grammar end-to-end: parser accept/reject for row + column with `N ∈ {2,3,4,5}`; distribute correctness (equal spacing, integer pixels, footprint respect) per axis. Serves as regression net for TECH-741 + TECH-742.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Parser accept tests cover `N ∈ {2,3,4,5}` for row + column.
2. Parser reject tests cover `tiled-row-1`, malformed names.
3. Distribute tests assert equal spacing + integer pixels.
4. Column axis behaves as mirror of row.
5. `pytest tools/sprite-gen/tests/ -q` green.

### 2.2 Non-Goals

1. Implementing parser / resolver — TECH-741/742.
2. DAS amendment — TECH-744.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Repo guardian | Prevent parser regression | Accept + reject paths green |
| 2 | Repo guardian | Prevent distribute regression | Spacing + integer asserted |
| 3 | Stage 9 reviewer | Trust N-generalisation | Tests cover N=2..5 not only N=3 |

## 4. Current State

### 4.1 Domain behavior

No parametric-slot test file yet.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_parametric_slots.py` — new file.
- Exercises: `src/slots.py` (TECH-741 parse + TECH-742 resolve).

### 4.3 Implementation investigation notes

Parametrize over N to avoid repetitive test bodies. Column tests mirror row with axes swapped. Assert `type(x) is int` (not `isinstance(x, int)` — rules out bools).

## 5. Proposed Design

### 5.1 Target behavior

Roughly 10 tests (with parametrize) covering:

1. `test_parse_row_variants[N]` — N ∈ {2,3,4,5}.
2. `test_parse_column_variants[N]` — N ∈ {2,3,4,5}.
3. `test_parse_rejects_n_lt_2` — `tiled-row-1`.
4. `test_parse_rejects_malformed` — `tiled-foo-3`, bare `row-3`, empty N.
5. `test_legacy_aliases` — legacy names parse.
6. `test_distribute_row_equal_spacing[N]` — N ∈ {2,3,4,5}.
7. `test_distribute_column_equal_spacing[N]` — N ∈ {2,3,4,5}.
8. `test_integer_pixel_anchors` — parametrized over several (name, count).
9. `test_anchors_inside_footprint` — anchors within tile bounds.
10. `test_count_mismatch_raises` — `count != N`.

### 5.2 Architecture / implementation

- pytest with `@pytest.mark.parametrize("n", [2, 3, 4, 5])`.
- Small helpers: `deltas = [anchors[i+1] - anchors[i] for i in ...]`; assert all equal ±1.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Parametrize over N | Single test body covers 4 Ns | 4 hand-rolled tests — rejected, duplication |
| 2026-04-23 | `±1` tolerance on spacing deltas | Integer-pixel rounding can shift one pixel | Exact equality — rejected, brittle |

## 7. Implementation Plan

### Phase 1 — Parser accept + reject tests

### Phase 2 — Distribute (row) tests

### Phase 3 — Column mirror + misc edge tests

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| All tests green | Python | `pytest tools/sprite-gen/tests/test_parametric_slots.py -q` | — |
| Suite green | Python | `pytest tools/sprite-gen/tests/ -q` | No regressions |

## 8. Acceptance Criteria

- [ ] Parser accept tests cover N ∈ {2,3,4,5} for row + column.
- [ ] Parser reject tests cover `tiled-row-1`, malformed names.
- [ ] Distribute tests assert equal spacing + integer pixels.
- [ ] Column axis is mirror of row axis.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Parametrized tests make N-generalisation cheap — the parser can scale to arbitrary N because the test matrix verifies it.

## §Plan Digest

### §Goal

Lock the parser + resolver contract under a single test file so the parametric slot grammar has a dense regression net from day one.

### §Acceptance

- [ ] `tools/sprite-gen/tests/test_parametric_slots.py` exists
- [ ] All tests exit green under `pytest -q`
- [ ] Parametrize covers N ∈ {2,3,4,5} for row + column
- [ ] Distribute tests assert integer-pixel anchors + equal spacing ±1
- [ ] Reject tests cover `tiled-row-1` + malformed names
- [ ] No test-file growth > 150 lines

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_parse_row_variants | `tiled-row-N`, N∈{2..5} | `("row", N)` | pytest parametrize |
| test_parse_column_variants | `tiled-column-N`, N∈{2..5} | `("column", N)` | pytest parametrize |
| test_parse_rejects_n_lt_2 | `tiled-row-1` | SpecError | pytest |
| test_parse_rejects_malformed | `tiled-foo-3`, `row-3`, `tiled-row-` | SpecError each | pytest parametrize |
| test_legacy_aliases | `tiled-row-3`, `tiled-row-4`, `tiled-column-3` | all parse | pytest |
| test_distribute_row_equal_spacing | N∈{2..5}, (2,2) | Δx equal ±1 | pytest parametrize |
| test_distribute_column_equal_spacing | N∈{2..5}, (2,2) | Δy equal ±1 | pytest parametrize |
| test_integer_pixel_anchors | various | `type(x) is int and type(y) is int` | pytest |
| test_anchors_inside_footprint | various | `0 ≤ x < cols*32` and `0 ≤ y < rows*32` | pytest |
| test_count_mismatch_raises | `tiled-row-3`, count=4 | SpecError | pytest |

### §Examples

```python
# tools/sprite-gen/tests/test_parametric_slots.py
import pytest
from src.slots import parse_slot, resolve_slot, SpecError

@pytest.mark.parametrize("n", [2, 3, 4, 5])
def test_parse_row_variants(n):
    assert parse_slot(f"tiled-row-{n}") == ("row", n)

@pytest.mark.parametrize("n", [2, 3, 4, 5])
def test_parse_column_variants(n):
    assert parse_slot(f"tiled-column-{n}") == ("column", n)

def test_parse_rejects_n_lt_2():
    with pytest.raises(SpecError):
        parse_slot("tiled-row-1")

@pytest.mark.parametrize("bad", ["tiled-foo-3", "row-3", "tiled-row-"])
def test_parse_rejects_malformed(bad):
    with pytest.raises(SpecError):
        parse_slot(bad)

@pytest.mark.parametrize("n", [2, 3, 4, 5])
def test_distribute_row_equal_spacing(n):
    anchors = [resolve_slot(f"tiled-row-{n}", (2, 2), i, n) for i in range(n)]
    deltas = [anchors[i+1][0] - anchors[i][0] for i in range(n - 1)]
    assert max(deltas) - min(deltas) <= 1

def test_count_mismatch_raises():
    with pytest.raises(SpecError):
        resolve_slot("tiled-row-3", (2, 2), 0, 4)
```

### §Mechanical Steps

#### Step 1 — Parser accept + reject tests

**Edits:**

- `tools/sprite-gen/tests/test_parametric_slots.py` — new file; parser variants.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_parametric_slots.py -q -k parse
```

#### Step 2 — Distribute row tests

**Edits:**

- Same file — row equal-spacing tests.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_parametric_slots.py -q -k distribute_row
```

#### Step 3 — Column mirror + misc tests

**Edits:**

- Same file — column tests + integer-pixel + count-mismatch.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_parametric_slots.py -q
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. What tile size should we use in tests? **Resolution:** import the `_TILE` constant from `src.slots` to avoid drift.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
