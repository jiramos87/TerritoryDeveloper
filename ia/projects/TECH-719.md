---
purpose: "TECH-719 — Signature extractor populates ground.dominant + ground.variance fields per JSON shape."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.5
---
# TECH-719 — Signature extractor ground.* extension

> **Issue:** [TECH-719](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/signature.py` (from Stage 6.2 / TECH-704) to populate `ground.dominant` (dominant palette colour on ground-only band of reference sprite) and `ground.variance` (`hue_stddev` + `value_stddev` across samples). Matches the JSON shape reserved in TECH-704. Consumed by `bootstrap-variants --from-signature` (TECH-712 extension) to derive `vary.ground.*` bounds instead of hand-tuned guesses.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ground.dominant` populated from ground-band pixels (RGB tuple).
2. `ground.variance.hue_stddev` + `value_stddev` populated (numbers, HSV-space stddev).
3. L15 policy respected: fallback mode (zero samples) leaves ground fields null.
4. Output matches TECH-704 JSON shape contract (no new top-level keys).

### 2.2 Non-Goals

1. `bootstrap-variants` flag wiring — extension in TECH-712 lineage (Stage 6.3); TECH-719 only populates the signature.
2. Composer jitter implementation — TECH-718.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Data-driven jitter bounds | Signature carries dominant + variance after extraction |
| 2 | Sprite-gen dev | Fallback safety | Zero-sample cases leave ground fields null, no crash |
| 3 | Repo guardian | Shape stability | JSON shape matches TECH-704 spec |

## 4. Current State

### 4.1 Domain behavior

TECH-704 landed the extractor with mass / roof / facade signatures only. `ground.*` fields reserved in JSON shape but left unpopulated.

### 4.2 Systems map

- `tools/sprite-gen/src/signature.py` — extractor.
- Consumer: `src/__main__.py::bootstrap-variants` (TECH-712 + its future extension).

### 4.3 Implementation investigation notes

Ground band on reference sprites is typically the bottom ~8–12 px of the diamond; reuse the same diamond geometry helper the composer uses. Fallback (L15) mode is already branched at extraction top-level from TECH-704 — just leave `ground` dict keys as `None`.

## 5. Proposed Design

### 5.1 Target behavior

```json
{
  "class": "residential_small",
  "mass": { "...": "..." },
  "roof": { "...": "..." },
  "facade": { "...": "..." },
  "ground": {
    "dominant": [34, 110, 58],
    "variance": { "hue_stddev": 2.4, "value_stddev": 1.7 }
  }
}
```

Fallback (zero samples) → `"ground": { "dominant": null, "variance": { "hue_stddev": null, "value_stddev": null } }`.

### 5.2 Architecture / implementation

- Reuse diamond geometry to isolate the ground band pixel set.
- Dominant: bucket colours, return modal RGB.
- Variance: convert each sampled pixel to HSV, compute per-channel stddev.
- L15 check: if sample count from TECH-704 fallback path is zero, populate with nulls.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | HSV stddev | Matches composer jitter units | RGB stddev — rejected, units don't match |
| 2026-04-23 | Modal colour for dominant | Robust to antialias noise | Mean colour — rejected, smears specks |
| 2026-04-23 | Nulls on fallback | Consistent with TECH-704 null-on-fallback convention | Zeros — rejected, reads as "no variance" not "no data" |

## 7. Implementation Plan

### Phase 1 — Ground-band isolation on reference sprite

### Phase 2 — Palette dominant + HSV stddev math

### Phase 3 — JSON shape verification against TECH-704 spec

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Dominant populated | Python | `pytest tests/test_signature.py::test_ground_dominant -q` | Tuple matches seeded fixture |
| Variance populated | Python | `pytest tests/test_signature.py::test_ground_variance -q` | Non-zero on noisy fixture |
| Fallback → nulls | Python | `pytest tests/test_signature.py::test_ground_fallback -q` | L15 mode leaves fields null |
| JSON shape | Python | `pytest tests/test_signature.py::test_shape_tech704_stable -q` | Schema-compare to TECH-704 reference |

## 8. Acceptance Criteria

- [ ] `ground.dominant` populated from ground-band pixels.
- [ ] `ground.variance.hue_stddev` + `value_stddev` populated.
- [ ] L15 fallback mode leaves ground fields null.
- [ ] Unit tests green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Null-vs-zero carries semantic weight when "no data" is a distinct outcome from "zero measurement".

## §Plan Digest

### §Goal

Signature extractor fills in the `ground` block so `bootstrap-variants` can propose data-driven `vary.ground.*` bounds instead of hand-tuned guesses, while respecting the L15 null-on-fallback policy.

### §Acceptance

- [ ] `ground.dominant` is a 3-tuple `[r, g, b]` derived from ground-band pixels of the reference sprite
- [ ] `ground.variance.hue_stddev` + `value_stddev` are non-negative floats in HSV units (hue degrees, value percent)
- [ ] Fallback mode (zero samples) → `ground.dominant = null`, `ground.variance.hue_stddev = null`, `ground.variance.value_stddev = null`
- [ ] Signature JSON shape unchanged from TECH-704 spec (no new top-level keys)
- [ ] `pytest tools/sprite-gen/tests/test_signature.py -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_ground_dominant | seeded reference sprite (solid green ground) | `[34, 110, 58]` tuple | pytest |
| test_ground_variance | noisy ground fixture | non-zero `hue_stddev` + `value_stddev` | pytest |
| test_ground_fallback | zero-sample mode | all ground fields null | pytest |
| test_shape_tech704_stable | any reference sprite | JSON keys match TECH-704 contract | pytest |

### §Examples

```python
# tools/sprite-gen/src/signature.py (excerpt)
from statistics import stdev
from colorsys import rgb_to_hsv

def _extract_ground(img, diamond_geom, fallback: bool) -> dict:
    if fallback:
        return {"dominant": None, "variance": {"hue_stddev": None, "value_stddev": None}}
    pixels = _ground_band_pixels(img, diamond_geom)
    if not pixels:
        return {"dominant": None, "variance": {"hue_stddev": None, "value_stddev": None}}
    dominant = _modal_rgb(pixels)
    hsvs = [rgb_to_hsv(r/255, g/255, b/255) for (r, g, b) in pixels]
    h_stddev = stdev([h * 360.0 for h, _, _ in hsvs]) if len(hsvs) > 1 else 0.0
    v_stddev = stdev([v * 100.0 for _, _, v in hsvs]) if len(hsvs) > 1 else 0.0
    return {
        "dominant": list(dominant),
        "variance": {"hue_stddev": h_stddev, "value_stddev": v_stddev},
    }
```

### §Mechanical Steps

#### Step 1 — Ground-band isolation

**Edits:**

- `tools/sprite-gen/src/signature.py` — add `_ground_band_pixels` using diamond geometry.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.signature import _ground_band_pixels; print('ok')"
```

#### Step 2 — Dominant + HSV stddev math

**Edits:**

- Same file — `_extract_ground` producing the required JSON shape.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py::test_ground_dominant tests/test_signature.py::test_ground_variance -q
```

#### Step 3 — Fallback wiring + shape test

**Edits:**

- Same file — thread fallback flag from TECH-704 path.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Ground band thickness — `8 px` or `12 px`? **Resolution:** start at `8` (conservative); tune during TECH-712 extension if dominant drifts.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
