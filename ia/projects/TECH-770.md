---
purpose: "TECH-770 — Vegetation primitive smoke tests."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.9a"
---
# TECH-770 — Vegetation primitive smoke tests

> **Issue:** [TECH-770](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

One test file covering smoke render + palette assertions for all 8 yard/vegetation primitives shipped in T7.1–T7.6 (7 task rows → 8 primitive modules: `iso_bush` + `iso_grass_tuft` in T7.3; `iso_path` + `iso_pavement_patch` in T7.5).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Each of `iso_tree_fir`, `iso_tree_deciduous`, `iso_bush`, `iso_grass_tuft`, `iso_pool`, `iso_path`, `iso_pavement_patch`, `iso_fence` renders without exception under residential palette.
2. Each render produces non-empty bounding box on the output canvas.
3. Dominant colour of each render matches expected palette key (bright/mid ramp level).

### 2.2 Non-Goals (Out of Scope)

1. Pixel-perfect bitmap comparison (smoke + color histogram only).
2. Cross-palette testing (residential palette only v1).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Verify all vegetation primitives work | All 7 render without exception; colors match palette expectations |

## 4. Current State

### 4.1 Domain behavior

Individual primitives (T7.1–T7.6 / TECH-762..TECH-767) will be shipped. No composite test exists yet.

### 4.2 Systems map

New file `tools/sprite-gen/tests/test_decorations_vegetation.py`. Depends on primitives shipped by T7.1–T7.6 (TECH-762..TECH-767). Uses default residential palette under `tools/sprite-gen/palettes/residential.json`.

## 5. Proposed Design

### 5.1 Target behavior (product)

For each of the 7 vegetation/yard primitives, instantiate a test canvas, call the primitive with standard params, verify output bbox is non-empty and dominant color matches expected palette key.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Parametrized pytest test per primitive. Each test instantiates a canvas, calls the primitive, asserts non-empty bbox and histogram-based color validation.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Parametrized per-primitive test | Scales easily; error messages clear | Single monolithic test (hard to debug) |

## 7. Implementation Plan

### Phase 1 — Smoke-render test per primitive (non-empty bbox)

- [ ] Create parametrized test fixture for each primitive
- [ ] Assert non-empty output bbox

### Phase 2 — Dominant-colour assertion per primitive against expected palette key

- [ ] Implement histogram-based dominant color detection
- [ ] Assert dominant color matches palette key expectation

### Phase 3 — Parametrize across the 7-primitive set to keep the file short

- [ ] Consolidate test logic using parametrization
- [ ] Run on residential palette

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| All 7 primitives render without exception | Smoke render | `pytest tools/sprite-gen/tests/test_decorations_vegetation.py` | Part of CI test suite |
| Non-empty bbox per primitive | Assertion | `pytest tests/test_decorations_vegetation.py` | Validates render success |

## 8. Acceptance Criteria

- [ ] Each of `iso_tree_fir`, `iso_tree_deciduous`, `iso_bush`, `iso_grass_tuft`, `iso_pool`, `iso_path`, `iso_pavement_patch`, `iso_fence` renders without exception under residential palette.
- [ ] Each render produces non-empty bounding box on the output canvas.
- [ ] Dominant colour of each render matches expected palette key (bright/mid ramp level).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- —

## §Plan Digest

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

### §Goal

Ship `tools/sprite-gen/tests/test_decorations_vegetation.py` — parametrized smoke + histogram tests across 8 decoration primitives (TECH-762..TECH-767). Residential palette only. Each primitive renders without exception; bbox >= 1 pixel; dominant colour matches palette ramp.

### §Acceptance

- [ ] New file `tools/sprite-gen/tests/test_decorations_vegetation.py` created.
- [ ] `_PRIMITIVES = ('iso_tree_fir', 'iso_tree_deciduous', 'iso_bush', 'iso_grass_tuft', 'iso_pool', 'iso_path', 'iso_pavement_patch', 'iso_fence')` literal present.
- [ ] Parametrized test `test_vegetation_smoke_all` covers 8 primitives.
- [ ] Parametrized test `test_vegetation_dominant_colour` covers 8 primitives; `iso_pool` uses top-2 tolerance (rim or fill); others use top-1 (fill).
- [ ] `test_vegetation_palette_keys_present` asserts residential palette contains `tree_fir`, `tree_deciduous`, `bush`, `grass_tuft`, `pool`, `fence`, `pavement`.
- [ ] Module docstring clarifies "7 task rows (T7.1–T7.6), 8 primitives".
- [ ] Test file runs green under `cd tools/sprite-gen && pytest tests/test_decorations_vegetation.py`.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_vegetation_smoke_all[primitive_name]` | 8 primitives parametrized, default kwargs, residential palette | no exception; bbox >= 1 pixel | pytest |
| `test_vegetation_dominant_colour[primitive_name]` | 8 primitives parametrized | dominant colour matches palette ramp (pool: top-2 rim/fill; others: top-1 fill) | pytest |
| `test_vegetation_palette_keys_present` | load residential palette JSON | 7 keys resolvable: `tree_fir`, `tree_deciduous`, `bush`, `grass_tuft`, `pool`, `fence`, `pavement` | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_tree_fir(canvas, 32, 32, scale=1.0, variant=0, palette=res)` on 64×64 canvas | no exception; bbox >= 1 px; dominant in `tree_fir` ramp | Smoke |
| `iso_pool(canvas, 32, 32, w_px=12, d_px=10, palette=res)` histogram top-2 | includes `pool.rim` + `pool.bright` | Rim tolerance |
| `iso_grass_tuft(canvas, 32, 32, variant=0, palette=res)` bbox | >= 1 pixel | Min-bbox |

### §Mechanical Steps

#### Step 1 — Create `test_decorations_vegetation.py` parametrized test file

**Goal:** Author pytest file covering 8 decoration primitives under residential palette.

**Edits:**
- `tools/sprite-gen/tests/test_decorations_vegetation.py` — **operation**: create; **after** — new file. Body:
  - Module docstring: "Smoke + histogram tests for 7 task rows (T7.1–T7.6), 8 primitives. Residential palette only."
  - Imports: `json`, `pathlib.Path`, `pytest`, `from PIL import Image`, `from src.primitives import (iso_tree_fir, iso_tree_deciduous, iso_bush, iso_grass_tuft, iso_pool, iso_path, iso_pavement_patch, iso_fence)`.
  - `_PRIMITIVES = ('iso_tree_fir', 'iso_tree_deciduous', 'iso_bush', 'iso_grass_tuft', 'iso_pool', 'iso_path', 'iso_pavement_patch', 'iso_fence')`.
  - `_PALETTE_KEYS = ('tree_fir', 'tree_deciduous', 'bush', 'grass_tuft', 'pool', 'fence', 'pavement')`.
  - Fixture `residential_palette()` loads `tools/sprite-gen/palettes/residential.json` → dict.
  - Fixture `blank_canvas()` returns `Image.new('RGBA', (64, 64), (0, 0, 0, 0))`.
  - `_PRIMITIVE_KWARGS` dict mapping each name → minimal default call kwargs (e.g. `iso_tree_fir → {'scale': 1.0, 'variant': 0}`; `iso_pool → {'w_px': 12, 'd_px': 10}`; `iso_path → {'length_px': 10, 'axis': 'ns', 'width_px': 2}`; `iso_fence → {'length_px': 10, 'side': 'n'}`; `iso_pavement_patch → {'w_px': 8, 'd_px': 8}`).
  - `_PRIMITIVE_DISPATCH` dict mapping name → callable.
  - `_EXPECTED_RAMP` dict mapping name → palette key the dominant colour should match (e.g. `iso_tree_fir → 'tree_fir'`).
  - `@pytest.mark.parametrize('name', _PRIMITIVES)` — `def test_vegetation_smoke_all(name, residential_palette, blank_canvas)`:
    - call `_PRIMITIVE_DISPATCH[name](blank_canvas, 32, 32, palette=residential_palette, **_PRIMITIVE_KWARGS[name])`
    - assert `blank_canvas.getbbox()` is not None
    - assert bbox area >= 1
  - `@pytest.mark.parametrize('name', _PRIMITIVES)` — `def test_vegetation_dominant_colour(name, residential_palette, blank_canvas)`:
    - render primitive; extract non-transparent pixels; compute top-2 RGB histogram
    - look up expected ramp under `residential_palette['materials'][_EXPECTED_RAMP[name]]`
    - for `iso_pool`: assert either `bright` or `rim` present in top-2
    - for `iso_tree_deciduous`: use `tree_deciduous.green` nested ramp (default `color_var='green'`)
    - for others: assert dominant matches any ramp level (`bright`, `mid`, or `dark` if present)
  - `def test_vegetation_palette_keys_present(residential_palette)`:
    - `for key in _PALETTE_KEYS: assert key in residential_palette['materials']`
- `invariant_touchpoints`: none (test authoring)
- `validator_gate`: `test -f tools/sprite-gen/tests/test_decorations_vegetation.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/tests/test_decorations_vegetation.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Missing primitive import → re-open originating Task (TECH-762..TECH-767). Palette key drift → re-open originating Task that added the ramp.

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

## Open Questions (resolve before / during implementation)

None — test design fully specified in master plan Stage 7.
