---
purpose: "TECH-742 — slots.py resolve_slot: distribute N buildings evenly along axis, integer-pixel anchors."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T9.2.2
---
# TECH-742 — `resolve_slot` distribute N evenly across axis

> **Issue:** [TECH-742](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Implement `resolve_slot(name, footprint, idx, count)` in `tools/sprite-gen/src/slots.py`. Returns `(x_px, y_px)` for the `idx`-th of `count` buildings, equal-spaced along the axis captured by TECH-741's parser. Anchors are integer pixels (no subpixel). Anchors respect footprint so every building stays inside the ground diamond.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `resolve_slot(name, footprint, idx, count)` accepts all 4 args.
2. Anchors equal-spaced along the named axis.
3. Anchors are integer pixels.
4. Anchors stay inside the ground diamond (footprint-aware).

### 2.2 Non-Goals

1. Parsing the slot name — TECH-741.
2. Tests — TECH-743.
3. DAS amendment — TECH-744.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Composer | Resolve anchor for house #2 of 3 | `resolve_slot("tiled-row-3", footprint, 1, 3)` returns middle anchor |
| 2 | Stage 9 consumer | Render 5-house row | Anchors for N=5 equal-spaced + inside diamond |
| 3 | TECH-734 consumer | `row_houses_3x` renders | 3-house row anchors correct |

## 4. Current State

### 4.1 Domain behavior

No slot-resolver exists; TECH-741 just added parser.

### 4.2 Systems map

- `tools/sprite-gen/src/slots.py` — append `resolve_slot`.
- Consumers: composer building-dispatch (future Stage 9 T9.2), TECH-734 preset render path.

### 4.3 Implementation investigation notes

Axis dispatch: `row` → x varies, y fixed; `column` → y varies, x fixed. Centre of mass along axis = `(footprint_axis_px) * (idx + 0.5) / count`. Round to int. Perpendicular axis uses footprint midline.

## 5. Proposed Design

### 5.1 Target behavior

```python
# footprint: (cols, rows) in tiles; tile_size = 32 px (per Stage 9 canvas math)
>>> resolve_slot("tiled-row-3", (2, 2), 0, 3)  # idx=0 of 3
(11, 32)   # leftmost anchor on row
>>> resolve_slot("tiled-row-3", (2, 2), 1, 3)
(32, 32)   # middle anchor
>>> resolve_slot("tiled-row-3", (2, 2), 2, 3)
(53, 32)   # rightmost anchor
```

### 5.2 Architecture / implementation

- `resolve_slot(name, footprint, idx, count)`:
  - `axis, n = parse_slot(name)` (TECH-741).
  - Assert `count == n` (same N).
  - `tile_size = 32` (Stage 9 locked).
  - `cols, rows = footprint`.
  - Compute `(x_px, y_px)`:
    - `row` axis: `x = int((idx + 0.5) * cols * tile_size / count); y = int(rows * tile_size / 2)`.
    - `column` axis: `y = int((idx + 0.5) * rows * tile_size / count); x = int(cols * tile_size / 2)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | `(idx + 0.5) / count` spacing | Symmetric around midline, no edge-hugging | `idx / (count - 1)` — rejected, first/last hit edges |
| 2026-04-23 | Integer-pixel clamp | Isometric pixel art — no subpixel | Float anchors — rejected, primitive dispatch expects int |
| 2026-04-23 | `assert count == n` | Surface mismatch early (author supplied 4 buildings for `tiled-row-3`) | Silent truncate — rejected, debugging nightmare |

## 7. Implementation Plan

### Phase 1 — Axis dispatch skeleton

### Phase 2 — Equal-space math + integer clamp

### Phase 3 — Footprint sanity (assert inside diamond)

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Equal spacing row | Python | `pytest tests/test_parametric_slots.py::test_distribute_row_equal_spacing -q` | TECH-743 |
| Equal spacing column | Python | `pytest tests/test_parametric_slots.py::test_distribute_column_equal_spacing -q` | TECH-743 |
| Integer-pixel anchors | Python | `pytest tests/test_parametric_slots.py::test_integer_pixel_anchors -q` | TECH-743 |
| Count mismatch raises | Python | `pytest tests/test_parametric_slots.py::test_count_mismatch -q` | TECH-743 |

## 8. Acceptance Criteria

- [ ] `resolve_slot(name, footprint, idx, count)` accepts all 4 args.
- [ ] Anchors equal-spaced along named axis.
- [ ] Anchors are integer pixels (no subpixel).
- [ ] Anchors respect footprint (stay inside ground diamond).
- [ ] Unit tests cover distribution correctness (in TECH-743).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Symmetric `(idx + 0.5) / count` spacing beats edge-hugging `idx / (count-1)` — buildings feel planned, not jammed against the ground diamond edge.

## §Plan Digest

### §Goal

Distribute N buildings evenly along the row or column axis with integer-pixel anchors that respect the ground diamond, completing the parametric slot mechanism so `tiled-row-N` / `tiled-column-N` produce renderable compositions.

### §Acceptance

- [ ] `resolve_slot("tiled-row-N", footprint, idx, count)` returns `(x_px, y_px)` for every `idx ∈ [0, N)`
- [ ] Consecutive anchor Δx (row) / Δy (column) equal to within ±1 px
- [ ] All anchors are integers
- [ ] Anchors stay inside footprint tile-bounds (never escape ground diamond)
- [ ] `count != N` (parsed from name) raises `SpecError`

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_distribute_row_equal_spacing | `tiled-row-3`, (2,2), idx=0..2 | Δx between consecutive anchors equal | pytest |
| test_distribute_column_equal_spacing | `tiled-column-4`, (2,2), idx=0..3 | Δy equal | pytest |
| test_integer_pixel_anchors | any N | `type(x_px) == type(y_px) == int` | pytest |
| test_anchors_inside_footprint | any N | `0 ≤ x < cols*32 and 0 ≤ y < rows*32` | pytest |
| test_count_mismatch | `tiled-row-3` with count=4 | SpecError | pytest |

### §Examples

```python
# tools/sprite-gen/src/slots.py (excerpt; builds on TECH-741)
from .spec import SpecError

_TILE = 32

def resolve_slot(name: str, footprint: tuple[int, int], idx: int, count: int) -> tuple[int, int]:
    axis, n = parse_slot(name)
    if count != n:
        raise SpecError(
            f"slot {name!r} expects count={n}; got count={count}"
        )
    cols, rows = footprint
    w_px, h_px = cols * _TILE, rows * _TILE
    if axis == "row":
        x = int((idx + 0.5) * w_px / count)
        y = h_px // 2
    else:  # column
        x = w_px // 2
        y = int((idx + 0.5) * h_px / count)
    return x, y
```

### §Mechanical Steps

#### Step 1 — Axis dispatch + equal-space math

**Edits:**

- `tools/sprite-gen/src/slots.py` — add `resolve_slot(name, footprint, idx, count)`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.slots import resolve_slot
print(resolve_slot('tiled-row-3', (2,2), 1, 3))
"
```

#### Step 2 — Count-mismatch guard

**Edits:**

- Same file — `count != n` → `SpecError`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.slots import resolve_slot, SpecError
try: resolve_slot('tiled-row-3', (2,2), 0, 4)
except SpecError as e: print('OK:', e)
"
```

#### Step 3 — Integer-pixel + footprint sanity

**Edits:**

- Same file — ensure `int(...)`; add footprint assertion.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_parametric_slots.py -q -k "distribute or integer_pixel"
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Is `_TILE = 32` still the Stage 9 canonical size? **Resolution:** yes per Stage 9 master plan T9.1 (`canvas_size(2,2) → (128, 0)` → tile = 32 px).

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
