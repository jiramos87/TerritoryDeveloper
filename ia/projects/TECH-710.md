---
purpose: "TECH-710 — variants block + split seeds loader normalization."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.3.2
---
# TECH-710 — `variants:` block + split seeds loader normalization

> **Issue:** [TECH-710](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/spec.py` to accept the new `variants: {count, vary, seed_scope}` object while normalising legacy scalar `variants: N` into `{count: N, vary: {}, seed_scope: palette}`. Accept top-level `palette_seed: int` + `geometry_seed: int`; legacy scalar `seed: N` fans to both when split seeds absent. `seed_scope` default `palette` preserves legacy behaviour. Consumes L6, L14.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Object `variants: {count, vary, seed_scope}` surfaced as-is.
2. Scalar `variants: N` normalises to `{count: N, vary: {}, seed_scope: palette}`.
3. Top-level `palette_seed` + `geometry_seed` surfaced when present.
4. Scalar `seed: N` fans out to `palette_seed = geometry_seed = N` when neither split seed authored.
5. Existing specs render byte-identical.

### 2.2 Non-Goals

1. Composer variant loop — TECH-711.
2. `bootstrap-variants` CLI — TECH-712.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Declare variants as a block with `vary:` ranges | `variants: {count:4, vary:{roof:{h_px:{min:6,max:12}}}, seed_scope: geometry}` surfaced |
| 2 | Legacy spec | Keep `variants: 4` working | Normalised to object form |
| 3 | Spec author | Freeze palette, randomize geometry | `palette_seed: 101`, `geometry_seed: 202` surfaced independently |

## 4. Current State

### 4.1 Domain behavior

Today `variants: <int>` produces N palette-randomised renders. No `vary:` grammar; no split seeds.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py` — target.
- Consumers: `src/compose.py` variant loop (TECH-711), `tests/test_variants_geometric.py` + `test_split_seeds.py` (TECH-713).

### 4.3 Implementation investigation notes

Normalization belongs fully in the loader. Composer reads only the normalised shape — simpler to unit-test.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# New object form
variants:
  count: 4
  vary:
    footprint_ratio: { w: {min: 0.4, max: 0.6}, d: {min: 0.4, max: 0.6} }
    padding: { n: {min: 0, max: 4} }
    roof: { h_px: {min: 6, max: 12}, pitch: { values: [1.0, 1.2, 1.5] } }
  seed_scope: palette+geometry

palette_seed: 101
geometry_seed: 202
```

Legacy:

```yaml
variants: 4
seed: 42
# normalises to:
# variants: { count: 4, vary: {}, seed_scope: palette }
# palette_seed: 42
# geometry_seed: 42
```

### 5.2 Architecture / implementation

- `_normalize_variants(data)` detects int vs object and emits object form.
- `_normalize_seeds(data)` detects single `seed` vs split seeds; fans out on absence.
- `seed_scope` default `palette`; accepted values `{palette, geometry, palette+geometry}`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Normalize in loader, not composer | Single spot for back-compat logic; composer stays simple | Composer handles both shapes — rejected, duplication risk |
| 2026-04-23 | `seed_scope` default `palette` (not `palette+geometry`) | Preserves legacy behaviour (palette-only variance) | Default `palette+geometry` — rejected, changes output for existing specs |

## 7. Implementation Plan

### Phase 1 — `_normalize_variants`

- [ ] Detect int vs dict; emit `{count, vary, seed_scope}`.
- [ ] Validate `seed_scope` enum.

### Phase 2 — `_normalize_seeds`

- [ ] Fan out legacy `seed: N` when split seeds absent.
- [ ] Preserve explicit split seeds.

### Phase 3 — Tests

- [ ] Unit tests for each legacy/object shape + seed fan-out.

### Phase 4 — Full-suite regression

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Scalar `variants: N` | Python | `pytest tests/test_spec.py::test_variants_scalar -q` | Normalises to object form |
| Object `variants:` | Python | `pytest tests/test_spec.py::test_variants_object -q` | Round-trips |
| `seed` fan-out | Python | `pytest tests/test_spec.py::test_seed_fanout -q` | `palette_seed = geometry_seed = N` |
| Explicit split seeds | Python | `pytest tests/test_spec.py::test_split_seeds_explicit -q` | Preserved independently |
| Full suite | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ green |

## 8. Acceptance Criteria

- [ ] Scalar + object `variants:` both surface as object.
- [ ] Scalar + split seeds both surface as split.
- [ ] `seed_scope` default `palette`; invalid value raises.
- [ ] Full pytest green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Loader-level normalization concentrates back-compat logic — composer reads one shape forever.

## §Plan Digest

### §Goal

Extend `tools/sprite-gen/src/spec.py` to accept `variants: {count, vary, seed_scope}` (with scalar `variants: N` legacy back-compat) and `palette_seed` + `geometry_seed` (with scalar `seed: N` fan-out). `seed_scope` default `palette` preserves legacy behaviour. Consumes L6, L14.

### §Acceptance

- [ ] Scalar `variants: N` normalises to `{count: N, vary: {}, seed_scope: palette}`
- [ ] Object `variants: {count, vary, seed_scope}` surfaced as-is
- [ ] `seed: N` fans to `palette_seed = geometry_seed = N` when split seeds absent
- [ ] Explicit split seeds are preserved
- [ ] Invalid `seed_scope` raises `SpecValidationError`
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_variants_scalar | `variants: 4` | `{count:4, vary:{}, seed_scope:"palette"}` | pytest |
| test_variants_object | `variants: {count:4, vary:{...}, seed_scope:"geometry"}` | round-trips | pytest |
| test_seed_fanout | `seed: 42` (no split seeds) | `palette_seed == 42`, `geometry_seed == 42` | pytest |
| test_split_seeds_explicit | `palette_seed: 101`, `geometry_seed: 202` | preserved independently | pytest |
| test_seed_scope_invalid | `seed_scope: weird` | `SpecValidationError` | pytest |

### §Examples

```python
# tools/sprite-gen/src/spec.py
_VALID_SEED_SCOPES = {"palette", "geometry", "palette+geometry"}

def _normalize_variants(data: dict) -> dict:
    v = data.get("variants")
    if isinstance(v, int):
        data["variants"] = {"count": v, "vary": {}, "seed_scope": "palette"}
    elif isinstance(v, dict):
        v.setdefault("count", 1)
        v.setdefault("vary", {})
        scope = v.setdefault("seed_scope", "palette")
        if scope not in _VALID_SEED_SCOPES:
            raise SpecValidationError(
                f"variants.seed_scope={scope!r} not in {sorted(_VALID_SEED_SCOPES)}"
            )
    elif v is not None:
        raise SpecValidationError(f"variants must be int or dict, got {type(v).__name__}")
    return data

def _normalize_seeds(data: dict) -> dict:
    palette = data.get("palette_seed")
    geometry = data.get("geometry_seed")
    legacy = data.get("seed")
    if palette is None and geometry is None and legacy is not None:
        data["palette_seed"] = int(legacy)
        data["geometry_seed"] = int(legacy)
    return data
```

### §Mechanical Steps

#### Step 1 — Author `_normalize_variants`

**Edits:**

- `tools/sprite-gen/src/spec.py` — helper + call from `load_spec`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec.py::test_variants_scalar tests/test_spec.py::test_variants_object -q
```

#### Step 2 — Author `_normalize_seeds`

**Edits:**

- `tools/sprite-gen/src/spec.py` — helper + call from `load_spec`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec.py::test_seed_fanout tests/test_spec.py::test_split_seeds_explicit -q
```

#### Step 3 — Enum validation on `seed_scope`

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec.py::test_seed_scope_invalid -q
```

#### Step 4 — Full-suite regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Any variant-related test red → unlikely (loader-only); revert and check if composer already reads these fields.

**MCP hints:** none — loader-only.

## Open Questions (resolve before / during implementation)

1. Should we also accept `variants: null`? **Resolution:** yes — treat as absent (single render). Loader sets `data.pop("variants", None)` when value is None.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
