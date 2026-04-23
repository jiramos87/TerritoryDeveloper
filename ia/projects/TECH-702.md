---
purpose: "TECH-702 — Tighten test_scale_calibration.py regression bounds to DAS §2.3 envelope."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.1.2
---
# TECH-702 — Tighten test_scale_calibration.py regression bounds

> **Issue:** [TECH-702](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Replace the loose `10 <= y0 <= 16` bound in `test_scale_calibration.py` with the tight DAS §2.3 House1-64 envelope: `y1 == 48`, `content_h ∈ [32, 36]`. Closes the regression hole that let the original pivot bug ship — the old range was wide enough to swallow the 3× scale drift.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Assert `y1 == 48` on rendered bbox of `building_residential_small`.
2. Assert `32 <= content_h <= 36` on rendered bbox.
3. Remove or replace the loose `10 <= y0 <= 16` check.

### 2.2 Non-Goals (Out of Scope)

1. Adding tests for other specs — belongs to TECH-703.
2. Touching reference sprite loading / HSV color match tests (separate assertions; unchanged).
3. Changing the spec itself or the composer.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Any future pivot regression surfaces immediately in CI | Tightened bounds fail fast if y1 ≠ 48 or content_h drifts |

## 4. Current State

### 4.1 Domain behavior

Render of `building_residential_small` currently produces bbox `(0, 15, 64, 48)` — matches DAS §2.3 House1-64 envelope exactly. Existing test only loose-checks y0.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_scale_calibration.py` — `test_residential_small_bbox_y0_in_envelope` + neighbouring functions.
- DAS §2.3 — anchor sprite metrics table (House1-64 = 64×35, y0=13 ⇒ y1=48, content_h=35).

### 4.3 Implementation investigation notes (optional)

DAS §2.3 lists House1-64 content bbox as 64×35 with ~2 px above diamond top (y=15). Derived target range: content_h ∈ [32, 36] (±2 px tolerance, matches DAS §2.3 "±3 px" elsewhere but kept tight to catch drift). `y1 = 48` is exact — the diamond bottom is invariant.

## 5. Proposed Design

### 5.1 Target behavior (product)

Running `pytest tools/sprite-gen/tests/test_scale_calibration.py -q` must fail if the rendered bbox's bottom row is not exactly 48 or the content height falls outside 32..36 inclusive.

### 5.2 Architecture / implementation

Rewrite the bbox-check test functions with tight assertions. Keep the HSV-color match assertions untouched. Update docstrings / comments to cite DAS §2.3.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | `content_h ∈ [32, 36]` (±2 of House1-64's 35) | Catches any drift ≥3 px while tolerating variant permutation | Exact match `content_h == 35` — rejected, variant permutation alters h slightly |
| 2026-04-23 | `y1 == 48` exact, no tolerance | Diamond bottom is invariant; variant cannot shift it | `46 <= y1 <= 50` — rejected, masks pivot regressions |

## 7. Implementation Plan

### Phase 1 — Rewrite bbox assertions

- [ ] Read `tools/sprite-gen/tests/test_scale_calibration.py` current `test_residential_small_bbox_*` functions.
- [ ] Replace loose y0 bound with `assert y1 == 48` and `32 <= content_h <= 36`.
- [ ] Cite DAS §2.3 in test docstring.
- [ ] Run `pytest tools/sprite-gen/tests/test_scale_calibration.py -q` — expect pass.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tightened bounds | Python | `cd tools/sprite-gen && python3 -m pytest tests/test_scale_calibration.py -q` | Expect pass with new `y1 == 48` + `content_h` check |
| Full suite still green | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | Expect 218+ passed |

## 8. Acceptance Criteria

- [ ] `y1 == 48` asserted on rendered bbox.
- [ ] `32 <= content_h <= 36` asserted.
- [ ] Loose `10 <= y0 <= 16` check removed or replaced.
- [ ] Full pytest run green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Regression bounds should be as tight as the reference envelope; +3/−3 tolerance on an invariant (diamond bottom) lets bugs ride for weeks.

## §Plan Digest

### §Goal

Lock the DAS §2.3 House1-64 envelope into `test_scale_calibration.py` as exact (`y1 == 48`) + narrow (`content_h ∈ [32, 36]`) assertions so any pivot drift ≥3 px fails CI on the next run.

### §Acceptance

- [ ] `y1 == 48` assert present (exact, not ranged)
- [ ] `32 <= content_h <= 36` assert present
- [ ] `10 <= y0 <= 16` loose bound replaced (or made redundant by new checks)
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/test_scale_calibration.py -q` green
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 218+ passed

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| scale_calib_y1_exact | rendered `building_residential_small` variant | `bbox[3] == 48` | `pytest tests/test_scale_calibration.py::test_residential_small_bbox_y1_diamond_bottom` |
| scale_calib_content_h | rendered bbox | `bbox[3] - bbox[1] ∈ [32, 36]` | `pytest tests/test_scale_calibration.py::test_residential_small_bbox_content_h_envelope` |
| full_suite | all tests | 218+ passed | `cd tools/sprite-gen && python3 -m pytest tests/ -q` |

### §Examples

Target assertion block:

```python
def test_residential_small_bbox_y1_diamond_bottom(rendered: Image.Image) -> None:
    """DAS §2.3: House1-64 content bbox bottom = 48 (diamond bottom invariant)."""
    box = rendered.getbbox()
    assert box is not None
    _x0, _y0, _x1, y1 = box
    assert y1 == 48, f"y1={y1} != 48 (DAS §2.3 diamond-bottom invariant)"


def test_residential_small_bbox_content_h_envelope(rendered: Image.Image) -> None:
    """DAS §2.3: House1-64 content_h = 35 ± 2 (covers variant permutation)."""
    box = rendered.getbbox()
    assert box is not None
    _x0, y0, _x1, y1 = box
    content_h = y1 - y0
    assert 32 <= content_h <= 36, f"content_h={content_h} outside [32, 36]"
```

### §Mechanical Steps

#### Step 1 — Rewrite bbox test functions

**Goal:** Replace `test_residential_small_bbox_y0_in_envelope` logic with tight `y1`+`content_h` assertions per §Examples.

**Edits:**

- `tools/sprite-gen/tests/test_scale_calibration.py` — **before:**

```python
def test_residential_small_bbox_y0_in_envelope(rendered: Image.Image) -> None:
    box = rendered.getbbox()
    assert box is not None
    _x0, y0, _x1, y1 = box
    # DAS §2.3: House1-64 y0 ≈ 13 ± 3; Stage-6 output targets same band
    assert 10 <= y0 <= 16, f"y0={y0} outside [10, 16]"
```

**after:**

```python
def test_residential_small_bbox_y1_diamond_bottom(rendered: Image.Image) -> None:
    """DAS §2.3: House1-64 content bbox bottom = 48 (diamond bottom invariant)."""
    box = rendered.getbbox()
    assert box is not None
    _x0, _y0, _x1, y1 = box
    assert y1 == 48, f"y1={y1} != 48 (DAS §2.3 diamond-bottom invariant)"


def test_residential_small_bbox_content_h_envelope(rendered: Image.Image) -> None:
    """DAS §2.3: House1-64 content_h = 35 ± 2 (covers variant permutation)."""
    box = rendered.getbbox()
    assert box is not None
    _x0, y0, _x1, y1 = box
    content_h = y1 - y0
    assert 32 <= content_h <= 36, f"content_h={content_h} outside [32, 36]"
```

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_scale_calibration.py -q
```

**STOP:** If `y1 != 48` or `content_h` outside range → do NOT loosen the bound; investigate whether TECH-701 pivot comment drifted or the spec changed. Report back via this §Plan Digest (append to §9 Issues Found).

#### Step 2 — Full suite regression

**Goal:** Confirm no unrelated fallout.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Expect 218+ passed. Any new red → investigate before merge.

**MCP hints:** none — pure test edit.

## Open Questions (resolve before / during implementation)

1. None — tooling only; DAS §2.3 envelope is authoritative.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
