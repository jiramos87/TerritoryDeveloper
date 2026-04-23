---
purpose: "TECH-721 — Single pytest file locking Stage 6.4 surfaces: legacy, object form, pool, jitter, noise mask."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.7
---
# TECH-721 — Tests — test_ground_variation.py

> **Issue:** [TECH-721](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

One new test file `tools/sprite-gen/tests/test_ground_variation.py` covers every surface touched by Stage 6.4 — legacy string form byte-identical, object form round-trip, `materials: [...]` pool, non-zero jitter divergence, zero jitter byte-identity, noise primitive mask + density monotonicity.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Six named cases (legacy, object, pool, non-zero jitter, zero jitter, noise mask).
2. Reproducibility: same seeds → identical output across runs.
3. Full suite `pytest tools/sprite-gen/tests/ -q` green.

### 2.2 Non-Goals

1. Composer / primitive implementation — owned by TECH-717/718/720.
2. Signature extractor tests — that suite lives alongside TECH-719.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Regression-guard legacy form | Pre-change golden PNG still byte-identical |
| 2 | Sprite-gen dev | Guard noise-mask bleed | Zero accent pixels outside diamond |
| 3 | Sprite-gen dev | Guard jitter leakage | Zero jitter ⇒ byte-identical variants |

## 4. Current State

### 4.1 Domain behavior

No Stage 6.4 tests — ground surface is currently untested beyond generic render integration.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_ground_variation.py` (new).
- Deps: `src/spec.py` (TECH-715, 720), `src/compose.py` (TECH-718), `src/primitives/iso_ground_noise.py` (TECH-717).

### 4.3 Implementation investigation notes

Use inline YAML / inline dict fixtures via `tmp_path` or `load_spec_from_dict` (following TECH-713's pattern). Diamond mask validation: render noise on blank canvas, assert all non-background pixels fall inside diamond geometry.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py -q
......                                                                    [100%]
6 passed in 0.5s
```

### 5.2 Architecture / implementation

- One module, six tests, minimal per-test fixtures.
- Pixel comparisons via tuple equality on `list(img.getdata())`.
- Mask validation: programmatic diamond hull via helper from TECH-717.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | One file for all six cases | Surface is tightly coupled; readers find everything together | Split per-lock — rejected, spreads the regression net |
| 2026-04-23 | Inline dict fixtures | Self-contained; easier to review | External YAML files — rejected, lifecycle churn |

## 7. Implementation Plan

### Phase 1 — Legacy + object form

### Phase 2 — Materials pool

### Phase 3 — Jitter diffs (non-zero + zero)

### Phase 4 — Noise primitive mask / density monotonicity

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Legacy byte-identical | Python | `pytest tests/test_ground_variation.py::test_legacy_string_byte_identical -q` | Pre-change golden PNG |
| Object form round-trip | Python | `pytest tests/test_ground_variation.py::test_object_form_round_trip -q` | Composer reads same fields as string path |
| Pool one-of-each | Python | `pytest tests/test_ground_variation.py::test_materials_pool_variants -q` | 4 variants span both materials |
| Non-zero jitter diverges | Python | `pytest tests/test_ground_variation.py::test_non_zero_jitter_diverges -q` | Variants pairwise-distinct |
| Zero jitter identity | Python | `pytest tests/test_ground_variation.py::test_zero_jitter_byte_identical -q` | All variants equal |
| Noise mask | Python | `pytest tests/test_ground_variation.py::test_noise_mask_and_density -q` | Outside mask = 0 accent; density monotonic |

## 8. Acceptance Criteria

- [ ] Six named test cases present + green.
- [ ] Reproducibility: same seeds across runs → identical output.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.
- [ ] Noise mask test asserts zero accent pixels outside diamond bounds.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- A single regression file for a tightly-coupled feature keeps the net taut — future readers find the full contract in one place.

## §Plan Digest

### §Goal

One tight pytest file exercises every surface Stage 6.4 touches, with named cases for each lock (L8/L9/L10) so a future regression gets named in CI.

### §Acceptance

- [ ] `tools/sprite-gen/tests/test_ground_variation.py` exists with six named test cases
- [ ] `test_legacy_string_byte_identical` passes (pixel-identical to pre-change golden)
- [ ] `test_object_form_round_trip` passes
- [ ] `test_materials_pool_variants` passes (4 variants, both materials represented)
- [ ] `test_non_zero_jitter_diverges` passes (variants pairwise-distinct)
- [ ] `test_zero_jitter_byte_identical` passes (all variants equal)
- [ ] `test_noise_mask_and_density` passes (zero pixels outside diamond; density 0 < 0.05 < 0.15 monotonic pixel count)
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_legacy_string_byte_identical | `ground: grass_flat` | pre-change golden PNG pixel-identical | pytest |
| test_object_form_round_trip | `ground: {material: grass_flat, hue_jitter: {min: 0, max: 0}, ...}` | pixel-identical to string form | pytest |
| test_materials_pool_variants | 4-variant spec with `materials: [grass_flat, dirt]` | both materials represented | pytest |
| test_non_zero_jitter_diverges | 4 variants with `hue_jitter: {min: -5, max: 5}` | `len(set(ground_band_bytes)) == 4` | pytest |
| test_zero_jitter_byte_identical | 4 variants with zero-range jitter | all ground bands equal bytes | pytest |
| test_noise_mask_and_density | noise on blank canvas at density 0 / 0.05 / 0.15 | zero outside diamond; pixel count strictly monotonic | pytest |

### §Examples

```python
# tools/sprite-gen/tests/test_ground_variation.py
import pytest
from PIL import Image
from src.spec import load_spec_from_dict
from src.compose import render
from src.primitives.iso_ground_noise import iso_ground_noise, _diamond_mask
from src.palette import load_palette

BASELINE_GOLDEN = "tests/golden/residential_small_grass_flat.png"


def test_legacy_string_byte_identical():
    spec = load_spec_from_dict({
        "class": "residential_small",
        "footprint": [1, 1],
        "canvas": [64, 64],
        "ground": "grass_flat",
        "building": {"footprint_px": [28, 28], "padding": {"n": 0, "e": 0, "s": 0, "w": 0}, "align": "center"},
        "composition": [],
    })
    got = render(spec)
    want = Image.open(BASELINE_GOLDEN)
    assert list(got.getdata()) == list(want.getdata())


def test_noise_mask_and_density():
    palette = load_palette()
    counts = {}
    for d in (0.0, 0.05, 0.15):
        img = Image.new("RGB", (64, 64), (0, 0, 0))
        iso_ground_noise(img, 32, 48, material="grass_flat", density=d,
                         seed=1, palette=palette)
        mask_set = set(_diamond_mask(32, 48))
        non_bg = [(x, y) for x in range(64) for y in range(64)
                  if img.getpixel((x, y)) != (0, 0, 0)]
        assert all((x, y) in mask_set for (x, y) in non_bg)
        counts[d] = len(non_bg)
    assert counts[0.0] < counts[0.05] < counts[0.15]
```

### §Mechanical Steps

#### Step 1 — Legacy + object form

**Edits:**

- `tools/sprite-gen/tests/test_ground_variation.py` — file skeleton + first two tests; commit baseline golden under `tests/golden/` if missing.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py::test_legacy_string_byte_identical tests/test_ground_variation.py::test_object_form_round_trip -q
```

#### Step 2 — Materials pool

**Edits:**

- Same file — `test_materials_pool_variants`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py::test_materials_pool_variants -q
```

#### Step 3 — Jitter diffs

**Edits:**

- Same file — zero + non-zero jitter tests.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py::test_non_zero_jitter_diverges tests/test_ground_variation.py::test_zero_jitter_byte_identical -q
```

#### Step 4 — Noise mask / density

**Edits:**

- Same file — `test_noise_mask_and_density`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py -q
```

#### Step 5 — Full-suite regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Need a `load_palette()` test helper? **Resolution:** use TECH-716's loader directly; avoid parallel test-only loader path.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
