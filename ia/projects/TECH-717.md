---
purpose: "TECH-717 — New scatter-pixel primitive iso_ground_noise that textures ground diamond deterministically."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.3
---
# TECH-717 — iso_ground_noise primitive

> **Issue:** [TECH-717](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

New primitive `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)` in `tools/sprite-gen/src/primitives/iso_ground_noise.py`. Scatters accent pixels only inside the diamond mask (no bleed onto building area). Density clamped `0..0.15`. Deterministic under `seed`. Registered in the composer `_DISPATCH` map so specs can reference it by name.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Primitive signature matches composer dispatch contract.
2. Scatter confined to diamond mask (verified via pixel accounting).
3. Density `0..0.15` hard-clamp.
4. Same `(x0, y0, material, density, seed, palette)` → identical pixel output.
5. Registered in `primitives/__init__.py` + `compose.py::_DISPATCH`.

### 2.2 Non-Goals

1. Composer auto-insertion — TECH-718.
2. Palette accent keys themselves — TECH-716.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Scatter accents onto ground | Primitive respects diamond mask |
| 2 | Sprite-gen dev | Reproducible renders | Same seed → identical pixels |
| 3 | Artist | Density guardrail | Density >0.15 clamps; zero = no-op |

## 4. Current State

### 4.1 Domain behavior

`iso_ground_diamond` paints a flat-material diamond. No texturing primitive exists; ground looks uniform.

### 4.2 Systems map

- `tools/sprite-gen/src/primitives/iso_ground_noise.py` (new).
- `tools/sprite-gen/src/primitives/__init__.py` (register).
- `tools/sprite-gen/src/compose.py::_DISPATCH` (register).

### 4.3 Implementation investigation notes

Reuse `iso_ground_diamond` geometry math for mask generation — same `diamond_half_w`, `diamond_half_h`. Palette accent fallback when both `accent_dark` / `accent_light` are None: no-op (zero pixels scattered).

## 5. Proposed Design

### 5.1 Target behavior

```python
from src.primitives.iso_ground_noise import iso_ground_noise
iso_ground_noise(img, 32, 48, material="grass_flat", density=0.08,
                 seed=1234, palette=palette)
# ~0.08 * diamond_pixel_count pixels painted with accent colours
```

### 5.2 Architecture / implementation

- Build diamond mask as set of `(x, y)` integer coords.
- Instantiate `rng = random.Random(seed)`.
- Target count = `int(round(density * len(mask)))` clamped to density range.
- For each target pixel: sample from `mask`; pick `accent_dark` or `accent_light` (50/50); paint.
- If `palette.accent_dark` and `accent_light` are both None → early return.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Density clamp 0..0.15 | Higher → visual noise overpowers ground colour | No clamp — rejected, artist footgun |
| 2026-04-23 | 50/50 dark/light accent split | Balanced specks both brighter + darker than ramp | Random weighted — deferred to future tuning |
| 2026-04-23 | No-op on both accents None | Lets author ship `texture:` even if palette not yet seeded | Raise error — rejected, breaks opt-in story |

## 7. Implementation Plan

### Phase 1 — Diamond mask from `iso_ground_diamond` geometry

### Phase 2 — Pixel scatter from `random.Random(seed)`

### Phase 3 — Density clamp + palette accent lookup

### Phase 4 — Register in `__init__` + `_DISPATCH`

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Mask clipped | Python | test in TECH-721 | Zero accent pixels outside diamond bounds |
| Determinism | Python | test in TECH-721 | Same args → byte-identical run |
| Density clamp | Python | test in TECH-721 | Density 0.3 → same as 0.15 |
| Registration | Python | `pytest tools/sprite-gen/tests/ -q` | Composer can dispatch by name |

## 8. Acceptance Criteria

- [ ] Primitive signature matches dispatch contract.
- [ ] Scatter confined to diamond mask.
- [ ] Density `0..0.15` hard-clamp.
- [ ] Deterministic under seed.
- [ ] Registered in `primitives/__init__.py` + `compose.py::_DISPATCH`.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Self-masking scatter primitives (build the mask from geometry, scatter inside) are easier to reason about than "paint then clip".

## §Plan Digest

### §Goal

Ship a deterministic scatter-pixel primitive that textures only the ground diamond, hard-clamped to a safe density range, so Stage 6.4 has a composable noise pass.

### §Acceptance

- [ ] `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)` exists under `tools/sprite-gen/src/primitives/`
- [ ] Zero pixels written outside the diamond mask (pixel-accounting test)
- [ ] Density > 0.15 clamps to 0.15; density == 0 → no pixels written
- [ ] Same `(x0, y0, material, density, seed, palette)` → byte-identical output across runs
- [ ] Registered in `primitives/__init__.py` + `compose.py::_DISPATCH`
- [ ] Both accents None for the material → primitive is a no-op

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_mask_confined | noise over blank img | zero non-background pixels outside diamond | pytest |
| test_density_clamp | density=0.3 vs 0.15 | identical outputs | pytest |
| test_zero_density_noop | density=0 | no pixels painted | pytest |
| test_seed_determinism | same args twice | pixel-identical | pytest |
| test_missing_accent_noop | material with both accents None | zero pixels | pytest |

### §Examples

```python
# tools/sprite-gen/src/primitives/iso_ground_noise.py
import random
from typing import Any

_MAX_DENSITY = 0.15

def iso_ground_noise(img, x0: int, y0: int, *, material: str,
                     density: float, seed: int, palette: Any) -> None:
    entry = palette.materials[material]
    if entry.accent_dark is None and entry.accent_light is None:
        return
    d = max(0.0, min(_MAX_DENSITY, density))
    if d == 0.0:
        return
    mask = _diamond_mask(x0, y0)  # list[(x, y)]
    rng = random.Random(seed)
    target = int(round(d * len(mask)))
    for _ in range(target):
        x, y = rng.choice(mask)
        colour = entry.accent_dark if rng.random() < 0.5 else entry.accent_light
        if colour is None:
            colour = entry.accent_dark or entry.accent_light
        img.putpixel((x, y), colour)
```

### §Mechanical Steps

#### Step 1 — Diamond mask helper

**Edits:**

- `tools/sprite-gen/src/primitives/iso_ground_noise.py` — `_diamond_mask` using same geometry as `iso_ground_diamond`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.primitives.iso_ground_noise import _diamond_mask; print(len(_diamond_mask(32, 48)))"
```

#### Step 2 — Scatter + density clamp + accent lookup

**Edits:**

- Same file — primitive body.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py::test_density_clamp tests/test_ground_variation.py::test_seed_determinism -q
```

#### Step 3 — Registration

**Edits:**

- `tools/sprite-gen/src/primitives/__init__.py` — export `iso_ground_noise`.
- `tools/sprite-gen/src/compose.py::_DISPATCH` — add entry.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. `diamond_half_w` / `diamond_half_h` constants — reuse from `iso_ground_diamond` module? **Resolution:** yes — expose a shared geometry helper if duplication grows.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
