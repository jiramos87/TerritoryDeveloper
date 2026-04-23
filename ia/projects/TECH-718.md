---
purpose: "TECH-718 — Composer applies ground jitter + auto-inserts iso_ground_noise pass when texture set."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.4
---
# TECH-718 — Composer ground jitter + texture auto-insert

> **Issue:** [TECH-718](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Teach `tools/sprite-gen/src/compose.py` to honour the object-form `ground:` fields normalised by TECH-715. Before rendering `iso_ground_diamond`, apply `hue_jitter` / `value_jitter` sampled from `palette_seed + i` to the material's ramp. When `ground.texture` is set, auto-insert an `iso_ground_noise` pass between the diamond and the first building primitive. Legacy string-form specs render byte-identical.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Jitter sampled from `palette_seed + i`; zero jitter → byte-identical vs baseline.
2. `ground.texture` set → noise pass auto-inserted (author never hand-adds it to `composition:`).
3. Legacy string-form specs render byte-identical.

### 2.2 Non-Goals

1. `vary.ground.*` grammar — TECH-720.
2. Primitive implementation itself — TECH-717.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Opt into jitter | `hue_jitter: {min: -3, max: 3}` varies ground across seeds |
| 2 | Spec author | Opt into texture with no hand-wiring | `texture: {density: 0.08}` auto-inserts noise pass |
| 3 | Repo guardian | Zero regression | Legacy specs byte-identical |

## 4. Current State

### 4.1 Domain behavior

Composer reads `ground:` string, dereferences palette material, passes ramp straight to `iso_ground_diamond`. No jitter, no texture hook.

### 4.2 Systems map

- `tools/sprite-gen/src/compose.py` — ground handling + variant loop.
- Deps: TECH-715 (loader shape), TECH-717 (primitive).

### 4.3 Implementation investigation notes

Variant loop (TECH-711) already splits `palette_seed` / `geometry_seed`. Jitter belongs in the palette side — use `palette_seed + variant_index` as the per-variant jitter seed.

## 5. Proposed Design

### 5.1 Target behavior

```python
# zero jitter → byte-identical
spec.ground = {"material": "grass_flat", "hue_jitter": {"min": 0, "max": 0}, ...}
render(spec) == baseline_png

# non-zero jitter → variant differs
spec.ground = {"material": "grass_flat", "hue_jitter": {"min": -3, "max": 3}, ...}
render(spec, palette_seed=100) != render(spec, palette_seed=101)

# texture auto-insert
spec.ground = {"material": "grass_flat", "texture": {"density": 0.08}, ...}
# composition: [iso_ground_diamond, <building primitives>]  (author writes this)
# actual render order: [iso_ground_diamond, iso_ground_noise, <building primitives>]
```

### 5.2 Architecture / implementation

- New helper `_jittered_ramp(ramp, hue_jitter, value_jitter, seed)` — HSV-space add jitter sampled via `random.Random(seed)` uniform within ranges.
- Call site: right before `iso_ground_diamond` in the variant loop; pass jittered ramp.
- After diamond render, if `spec.ground.texture`: call `iso_ground_noise` with derived seed + density.
- Zero jitter → helper returns original ramp (identity path for byte-identical regression).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | HSV-space jitter | Matches typical artist intent ("darker green", "lighter green") | RGB jitter — rejected, colour drift |
| 2026-04-23 | Auto-insert noise | Author wrote the intent (`texture: {...}`); don't force them to wire two primitives | Require manual wiring — rejected, leaky abstraction |
| 2026-04-23 | `palette_seed + i` for jitter | Keeps jitter under palette seed domain, not geometry | Separate seed field — rejected, over-specified |

## 7. Implementation Plan

### Phase 1 — `_jittered_ramp` helper

### Phase 2 — Wire jitter into `iso_ground_diamond` call

### Phase 3 — Conditional noise-pass auto-insertion

### Phase 4 — Byte-identical legacy regression

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Zero jitter byte-identical | Python | test in TECH-721 | Pixel-diff 0 vs baseline |
| Non-zero jitter varies | Python | test in TECH-721 | Variants pairwise-distinct |
| Texture auto-insert | Python | test in TECH-721 | Author omits `iso_ground_noise` from composition |
| Legacy string form | Python | `pytest tools/sprite-gen/tests/ -q` | Pre-change golden PNGs pixel-identical |

## 8. Acceptance Criteria

- [ ] Jitter sampled from `palette_seed + i`.
- [ ] Zero jitter → byte-identical vs baseline.
- [ ] `ground.texture` set → noise pass auto-inserted.
- [ ] Author never hand-adds `iso_ground_noise` to `composition:`.
- [ ] Legacy string-form specs render byte-identical.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Auto-inserting derived primitives keeps authoring declarative — the composition list stays an expression of intent, not of pipeline plumbing.

## §Plan Digest

### §Goal

Composer honours the new ground object fields: jitter the palette ramp deterministically and auto-insert a noise pass when `texture:` is set, without forcing authors to hand-wire the extra primitive.

### §Acceptance

- [ ] `_jittered_ramp(ramp, hue_jitter=None, value_jitter=None, seed=...)` returns original ramp when both jitters are None or zero
- [ ] Jitter seed is derived from `palette_seed + variant_index`
- [ ] `ground.texture` set → `iso_ground_noise` inserted between `iso_ground_diamond` and first building primitive at render time, not spec time
- [ ] Byte-identical legacy regression: pre-change golden PNGs match post-change
- [ ] Variants with non-zero jitter produce pairwise-distinct ground bands

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_zero_jitter_byte_identical | string-form baseline vs object-form with zero jitter | pixel-identical | pytest |
| test_non_zero_jitter_varies | palette_seed 100 vs 101 | pixel-diff > 0 on ground band | pytest |
| test_texture_auto_insert | `texture: {density: 0.08}` + composition without noise | render shows scattered accents | pytest |
| test_legacy_string_form | pre-existing sprite specs | pixel-identical baselines | pytest |

### §Examples

```python
# tools/sprite-gen/src/compose.py (excerpt)
import random
from colorsys import rgb_to_hsv, hsv_to_rgb

def _jittered_ramp(ramp, hue_jitter, value_jitter, seed):
    if not hue_jitter and not value_jitter:
        return ramp
    hj_min, hj_max = (hue_jitter or {"min": 0, "max": 0}).values()
    vj_min, vj_max = (value_jitter or {"min": 0, "max": 0}).values()
    if hj_min == hj_max == 0 and vj_min == vj_max == 0:
        return ramp
    rng = random.Random(seed)
    dh = rng.uniform(hj_min, hj_max) / 360.0
    dv = rng.uniform(vj_min, vj_max) / 100.0
    out = []
    for r, g, b in ramp:
        h, s, v = rgb_to_hsv(r/255, g/255, b/255)
        h = (h + dh) % 1.0
        v = max(0.0, min(1.0, v + dv))
        nr, ng, nb = hsv_to_rgb(h, s, v)
        out.append((int(nr*255), int(ng*255), int(nb*255)))
    return out

# in variant loop
for i in range(variants.count):
    jseed = palette_seed + i
    ramp = _jittered_ramp(material.ramp, ground.hue_jitter, ground.value_jitter, jseed)
    render_iso_ground_diamond(img, x0, y0, ramp=ramp, ...)
    if ground.texture:
        iso_ground_noise(img, x0, y0, material=ground.material,
                         density=ground.texture["density"],
                         seed=jseed + 1, palette=palette)
    render_building_primitives(img, ...)
```

### §Mechanical Steps

#### Step 1 — `_jittered_ramp` helper

**Edits:**

- `tools/sprite-gen/src/compose.py` — add helper + identity short-circuit.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py::test_zero_jitter_byte_identical -q
```

#### Step 2 — Wire jitter into `iso_ground_diamond` call

**Edits:**

- Same file — variant loop passes `_jittered_ramp(...)`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py::test_non_zero_jitter_varies -q
```

#### Step 3 — Auto-insert noise pass

**Edits:**

- Same file — conditional `iso_ground_noise` call after diamond, before building primitives.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py::test_texture_auto_insert -q
```

#### Step 4 — Legacy regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Jitter HSV vs RGB — confirmed HSV (Decision Log). Unit: hue in degrees (-360..360 feasible, typical -10..10); value in percent (-100..100, typical -10..10). **Resolution:** document units in TECH-715's loader docstring; Stage 6.4 uses `hue_jitter` in degrees, `value_jitter` in percent.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
