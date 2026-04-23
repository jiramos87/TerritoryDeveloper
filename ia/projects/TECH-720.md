---
purpose: "TECH-720 — Extend variants.vary grammar to accept vary.ground.* range objects; composer samples per variant."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.6
---
# TECH-720 — vary.ground.* grammar

> **Issue:** [TECH-720](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/spec.py::_normalize_variants` to accept a `ground` axis inside `variants.vary`, with nested grammar `{material: {values: [...]}, hue_jitter: {min, max}, value_jitter: {min, max}, texture: {density: {min, max}}}`. The composer's variant loop samples those from `palette_seed + i`. Specs without `vary.ground` render unchanged.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Loader validates `vary.ground.*` range objects against the same validator shape used elsewhere in TECH-710.
2. Composer samples each axis from `palette_seed + i`.
3. Specs without `vary.ground` render byte-identical to pre-change baseline.

### 2.2 Non-Goals

1. Ground object-form loader — TECH-715.
2. Composer jitter / texture implementation — TECH-718 (consumed, not implemented here).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Randomise material across variants | `vary.ground.material.values: [a, b]` produces a mix across variants |
| 2 | Spec author | Randomise jitter per variant | `vary.ground.hue_jitter: {min: -3, max: 3}` sampled independently per variant |
| 3 | Spec author | Randomise texture density | `vary.ground.texture.density: {min: 0.04, max: 0.12}` sampled per variant |

## 4. Current State

### 4.1 Domain behavior

TECH-710 ships `variants.vary` with axes like `roof.h_px`, `facade.color`, etc. No ground axis today.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py::_normalize_variants` — validator.
- `tools/sprite-gen/src/compose.py` — variant loop (TECH-711, extended by TECH-718).

### 4.3 Implementation investigation notes

Reuse TECH-710's range-object validator (`_validate_range`). `values: [...]` style already supported elsewhere; apply same helper to `material.values`.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
variants:
  count: 4
  seed_scope: palette
  vary:
    ground:
      material:
        values: [grass_flat, dirt]
      hue_jitter: {min: -3, max: 3}
      value_jitter: {min: -2, max: 2}
      texture:
        density: {min: 0.04, max: 0.12}
```

Each of 4 variants: material sampled from `[grass_flat, dirt]`, hue/value jitter sampled from range, texture density sampled from range. All via `random.Random(palette_seed + i)` → reproducible.

### 5.2 Architecture / implementation

- Extend `_normalize_variants` to recognise `ground` key.
- Validate `material.values` as non-empty list of material names.
- Validate `hue_jitter`, `value_jitter`, `texture.density` as range objects (reuse helper).
- Composer: per variant `i`, `rng = random.Random(palette_seed + i)`; sample each axis; merge into the variant's effective `ground` object before TECH-718's handler runs.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Samples from `palette_seed + i` | Keeps palette-axis randomness in palette seed domain | Separate `ground_seed` — rejected, over-specified |
| 2026-04-23 | `material.values` list (not range) | Material is enumerable, not a scalar range | Tag with weights — deferred to future |
| 2026-04-23 | Loader-side range validation | Errors at load; no surprise at render | Validate at render — rejected, late failure |

## 7. Implementation Plan

### Phase 1 — Extend `_normalize_variants` to accept `ground` axis with validation

### Phase 2 — Composer sampling per variant

### Phase 3 — Unit tests for grammar + back-compat

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Validation | Python | `pytest tests/test_spec_variants.py::test_vary_ground_valid -q` | Accept well-formed grammar |
| Validation errors | Python | `pytest tests/test_spec_variants.py::test_vary_ground_invalid_shape -q` | Malformed range raises |
| Composer sampling | Python | test in TECH-721 | Variants show material mix + per-variant jitter |
| Back-compat | Python | `pytest tests/ -q` | Specs without `vary.ground` byte-identical |

## 8. Acceptance Criteria

- [ ] Loader validates `vary.ground.*` range objects.
- [ ] Composer samples these from `palette_seed + i`.
- [ ] Specs without `vary.ground` unchanged.
- [ ] Unit tests cover grammar.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Adding a new axis to `vary:` stays cheap as long as axis validators are composable — the work lives in the axis, not the dispatcher.

## §Plan Digest

### §Goal

Give `variants.vary` a `ground` axis with nested material-pool + jitter + texture-density grammar, validated at load, sampled by the composer from the palette seed domain.

### §Acceptance

- [ ] `_normalize_variants` accepts `vary.ground.material.values: [str, ...]`, `vary.ground.hue_jitter: {min, max}`, `vary.ground.value_jitter: {min, max}`, `vary.ground.texture.density: {min, max}`
- [ ] Malformed shapes raise `SpecError` at load time with clear messages
- [ ] Composer samples via `random.Random(palette_seed + i)` per variant `i`
- [ ] Specs with no `vary.ground` render byte-identical to pre-change baseline
- [ ] `pytest tools/sprite-gen/tests/ -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_vary_ground_valid | fully-populated grammar | normalised object | pytest |
| test_vary_ground_invalid_shape | `hue_jitter: 5` (not range) | `SpecError` | pytest |
| test_vary_ground_empty_material_values | `material.values: []` | `SpecError` | pytest |
| test_back_compat_no_vary_ground | pre-existing spec | byte-identical render | pytest |

### §Examples

```python
# tools/sprite-gen/src/spec.py (excerpt)
def _normalize_vary_ground(raw: dict) -> dict:
    out = {}
    if "material" in raw:
        values = raw["material"].get("values")
        if not values or not isinstance(values, list):
            raise SpecError("vary.ground.material.values: expected non-empty list")
        out["material"] = {"values": list(values)}
    for axis in ("hue_jitter", "value_jitter"):
        if axis in raw:
            out[axis] = _validate_range(raw[axis], f"vary.ground.{axis}")
    if "texture" in raw:
        tex = raw["texture"]
        if "density" in tex:
            out["texture"] = {"density": _validate_range(tex["density"],
                                                         "vary.ground.texture.density")}
    return out

# composer per-variant
rng = random.Random(palette_seed + i)
eff = dict(base_ground)  # from TECH-715 normalised form
if "material" in vary_ground:
    eff["material"] = rng.choice(vary_ground["material"]["values"])
if "hue_jitter" in vary_ground:
    r = vary_ground["hue_jitter"]
    eff["hue_jitter"] = {"min": rng.uniform(r["min"], r["max"]),
                         "max": rng.uniform(r["min"], r["max"])}
# ... similar for value_jitter + texture.density
```

### §Mechanical Steps

#### Step 1 — Validator

**Edits:**

- `tools/sprite-gen/src/spec.py` — add `_normalize_vary_ground` helper; call from `_normalize_variants`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec_variants.py -q
```

#### Step 2 — Composer sampling

**Edits:**

- `tools/sprite-gen/src/compose.py` — per-variant sampling via `random.Random(palette_seed + i)`; merge into effective ground object.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_variation.py -q
```

#### Step 3 — Back-compat regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Semantics of `hue_jitter: {min, max}` under `vary:` — does it re-sample _per variant_, or set a _shared range_ used inside TECH-718's existing jitter? **Resolution:** `vary:` re-samples; if author wants a shared range, they set it on `ground.hue_jitter` directly (TECH-715 object form). Document in loader docstring.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
