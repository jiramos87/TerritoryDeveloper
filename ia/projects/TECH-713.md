---
purpose: "TECH-713 — Tests for placement matrix + variant determinism + split-seed independence."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.3.5
---
# TECH-713 — Tests: placement + variants + split seeds

> **Issue:** [TECH-713](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Author three new test files under `tools/sprite-gen/tests/`:

- `test_building_placement.py` — matrix over `footprint_px` / `footprint_ratio` / `padding` / `align` combinations; asserts resolved building mass bbox per case.
- `test_variants_geometric.py` — same spec + `vary:` produces 4 variants with pairwise-distinct bboxes; identical outputs across runs with the same seeds.
- `test_split_seeds.py` — freezing `palette_seed` varies only geometry when `geometry_seed` advances, and vice versa.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `test_building_placement.py` exercises ≥12 combos.
2. `test_variants_geometric.py` asserts 4 distinct bboxes + reproducibility.
3. `test_split_seeds.py` asserts independence in both seed-freeze directions.
4. Full suite exits 0.

### 2.2 Non-Goals

1. New composer code — TECH-711 owns implementation.
2. CLI tests for bootstrap — TECH-712 owns them.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Regression-guard placement combinatorics | New `align` combo accidentally breaking SW anchor fails at CI |
| 2 | Sprite-gen dev | Reproducibility contract | Same seeds → byte-identical across runs |
| 3 | Sprite-gen dev | Split-seed independence | Test fails if `palette_seed` change leaks into geometry |

## 4. Current State

### 4.1 Domain behavior

`test_render_integration.py` + `test_scale_calibration.py` (latter retiring in TECH-707) cover canonical 1×1 bbox. No placement matrix, no variant determinism, no seed-independence tests.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_building_placement.py` (new).
- `tools/sprite-gen/tests/test_variants_geometric.py` (new).
- `tools/sprite-gen/tests/test_split_seeds.py` (new).
- Deps: `src/spec.py` (TECH-709/710), `src/compose.py::resolve_building_box` + variant loop (TECH-711).

### 4.3 Implementation investigation notes

Placement tests should use inline YAML via tmp_path to keep fixtures versioned in the test file (easier review). Variant tests can reuse live `building_residential_small.yaml` augmented with `variants:` block at test-time.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ cd tools/sprite-gen && python3 -m pytest tests/test_building_placement.py tests/test_variants_geometric.py tests/test_split_seeds.py -q
...
~18 passed in 0.6s
```

### 5.2 Architecture / implementation

- Parametrize `test_building_placement` over `(align, padding)` matrix (4 aligns × 3 padding profiles = 12 cases).
- `test_variants_geometric`: render 4 variants with `vary.roof.h_px`; collect bboxes; assert pairwise-distinct; assert reproducibility by running twice.
- `test_split_seeds`: two pairs of runs — (freeze palette, vary geometry) + (vice versa); assert independence via pixel-diff scoring.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Inline YAML fixtures via tmp_path | Tests are self-contained; no test-data sprawl | External fixture dir — rejected, lifecycle churn |
| 2026-04-23 | "Pairwise-distinct" checked via `set(bboxes)` | Simple; catches all-identical + two-identical failures | Mean-diff — rejected, imprecise |

## 7. Implementation Plan

### Phase 1 — `test_building_placement.py`

### Phase 2 — `test_variants_geometric.py`

### Phase 3 — `test_split_seeds.py`

### Phase 4 — Full-suite regression

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Placement matrix | Python | `pytest tests/test_building_placement.py -q` | ≥12 parametrized cases |
| Variant determinism | Python | `pytest tests/test_variants_geometric.py -q` | 4 distinct + reproducible |
| Split-seed independence | Python | `pytest tests/test_split_seeds.py -q` | Both directions |
| Full suite | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ + new parametrized cases green |

## 8. Acceptance Criteria

- [ ] `test_building_placement.py` ≥12 cases pass.
- [ ] `test_variants_geometric.py` passes.
- [ ] `test_split_seeds.py` passes.
- [ ] Full pytest green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Parametrize every combinatorial test — the matrix grows naturally and each cell is a named failure in CI.

## §Plan Digest

### §Goal

Three new pytest files locking placement combinatorics, variant determinism, and split-seed independence so Stage 6.3 surface changes can't silently drift.

### §Acceptance

- [ ] `tests/test_building_placement.py` — ≥12 parametrized cases (4 aligns × 3 padding profiles) all green
- [ ] `tests/test_variants_geometric.py` — 4 variants produce pairwise-distinct bboxes, byte-identical on re-run with same seeds
- [ ] `tests/test_split_seeds.py` — freezing `palette_seed` varies geometry only; freezing `geometry_seed` varies palette only
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_placement_matrix | 4 aligns × 3 padding profiles | resolved mass bbox matches per-combo expected | parametrized |
| test_variants_pairwise_distinct | `variants: {count: 4, vary: {roof: {h_px: {min: 6, max: 14}}}, seed_scope: geometry}` | `len(set(bboxes)) == 4` | pytest |
| test_variants_reproducible | same spec + same seeds run twice | identical outputs | pytest |
| test_split_seed_palette_freeze | `palette_seed` fixed, `geometry_seed` varies | palette channels identical, geometry differs | pytest |
| test_split_seed_geometry_freeze | `geometry_seed` fixed, `palette_seed` varies | geometry identical, palette differs | pytest |

### §Examples

```python
# tools/sprite-gen/tests/test_building_placement.py
import pytest
from src.spec import load_spec_from_dict
from src.compose import resolve_building_box

_ALIGNS = ["center", "sw", "ne", "nw", "se"]
_PADDINGS = [
    {"n": 0, "e": 0, "s": 0, "w": 0},
    {"n": 4, "e": 0, "s": 0, "w": 0},
    {"n": 0, "e": 0, "s": 10, "w": 0},
]

@pytest.mark.parametrize("align", _ALIGNS)
@pytest.mark.parametrize("padding", _PADDINGS)
def test_placement_matrix(align: str, padding: dict) -> None:
    spec = {
        "class": "residential_small",
        "footprint": [1, 1],
        "canvas": [64, 64],
        "building": {
            "footprint_px": [28, 28],
            "padding": padding,
            "align": align,
        },
        "composition": [],
    }
    bx, by, ox, oy = resolve_building_box(spec)
    assert bx == 28 and by == 28
    # TODO: assert expected (ox, oy) per (align, padding) cell
```

### §Mechanical Steps

#### Step 1 — `test_building_placement.py`

**Edits:**

- `tools/sprite-gen/tests/test_building_placement.py` — new file with parametrized matrix.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_building_placement.py -q
```

#### Step 2 — `test_variants_geometric.py`

**Edits:**

- `tools/sprite-gen/tests/test_variants_geometric.py` — 4-variant render + pairwise distinct + reproducibility.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_variants_geometric.py -q
```

#### Step 3 — `test_split_seeds.py`

**Edits:**

- `tools/sprite-gen/tests/test_split_seeds.py` — freeze-one / vary-other tests.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_split_seeds.py -q
```

#### Step 4 — Full-suite regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none — test-only.

## Open Questions (resolve before / during implementation)

1. Need a `load_spec_from_dict` helper? **Resolution:** yes — add to `src/spec.py` in TECH-709 if not present; tests consume inline dicts to avoid tmp-file churn.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
