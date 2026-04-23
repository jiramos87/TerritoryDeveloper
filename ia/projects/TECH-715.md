---
purpose: "TECH-715 — Normalise ground: field at spec loader to one object shape regardless of author form."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.1
---
# TECH-715 — Ground schema — string / object form loader normalization

> **Issue:** [TECH-715](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/spec.py` to accept `ground:` as either a bare string (`"grass_flat"`) **or** an object (`{material, materials, hue_jitter, value_jitter, texture}`). Normalise both forms to the same internal object so the composer has one shape to consume. Legacy string specs stay byte-identical through the renderer.

## 2. Goals and Non-Goals

### 2.1 Goals

1. String form → `{material: <str>, materials: null, hue_jitter: null, value_jitter: null, texture: null}`.
2. Object form accepted with any subset of fields; missing fields default to `null`.
3. `materials: [...]` list form accepted (pool for variant sampling); mutually exclusive with `material`.
4. Invalid combos rejected with clear errors at load time, not render time.

### 2.2 Non-Goals

1. Composer-side jitter / texture implementation — TECH-718.
2. `vary.ground.*` grammar — TECH-720.
3. Palette accent keys — TECH-716.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Keep writing `ground: grass_flat` | Legacy specs load + render byte-identical |
| 2 | Spec author | Opt into object form with jitter / texture | Loader accepts it; missing fields filled with null |
| 3 | Spec author | Supply a materials pool for variants | `materials: [grass_flat, dirt]` validated; `material` + `materials` together rejected |

## 4. Current State

### 4.1 Domain behavior

`ground:` is accepted only as a string; composer dereferences it directly. No room for jitter, texture, or material pools.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py` — loader + normalisation.
- Consumers: `src/compose.py` (TECH-718), `src/spec.py::_normalize_variants` (TECH-720).

### 4.3 Implementation investigation notes

`variants:` normalisation (TECH-710) sets the pattern for `str | dict` detection; follow the same helper style to keep loader surface consistent.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# legacy — still works
ground: grass_flat

# new object form
ground:
  material: grass_flat
  hue_jitter: {min: -3, max: 3}
  value_jitter: {min: -2, max: 2}
  texture:
    density: 0.08

# materials pool
ground:
  materials: [grass_flat, dirt]
```

### 5.2 Architecture / implementation

- New private helper `_normalize_ground(raw)`:
  - `isinstance(raw, str)` → `{material: raw, ...None}`.
  - `isinstance(raw, dict)` → merge with defaults, validate `material XOR materials`.
  - Other → `SpecError`.
- Called from top-level spec load alongside existing normalisers.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Normalise at loader, not composer | One shape downstream; composer logic stays tight | Runtime polymorphism — rejected, spreads detection code |
| 2026-04-23 | `material` XOR `materials` | Two anchors for the same slot would force tie-breaker rules | Allow both — rejected, ambiguity |

## 7. Implementation Plan

### Phase 1 — Detect str vs dict; emit object

### Phase 2 — Fill defaults for missing object fields

### Phase 3 — Unit tests for string form + object form + pool + error combos

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| String form normalises | Python | `pytest tests/test_spec_ground.py::test_string_form -q` | Compare to expected dict |
| Object form round-trips | Python | `pytest tests/test_spec_ground.py::test_object_form -q` | Missing fields → None |
| Pool form accepted | Python | `pytest tests/test_spec_ground.py::test_materials_pool -q` | `material` remains None |
| Error path | Python | `pytest tests/test_spec_ground.py::test_material_and_materials_raises -q` | `SpecError` |

## 8. Acceptance Criteria

- [ ] String form normalises to `{material: <str>}` + null others.
- [ ] Object form fills missing fields with None.
- [ ] `materials: [...]` list accepted.
- [ ] Mutually-exclusive `material` + `materials` raises `SpecError`.
- [ ] Unit tests green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Loader-side normalisation shields every downstream consumer from shape drift — cheapest place to enforce schema.

## §Plan Digest

### §Goal

Loader normalises `ground:` to a single object shape regardless of author form so the composer and vary-loader have one target to read.

### §Acceptance

- [ ] `ground: grass_flat` → `{material: "grass_flat", materials: None, hue_jitter: None, value_jitter: None, texture: None}`
- [ ] `ground: {material: ..., hue_jitter: {min, max}}` round-trips; missing fields default to None
- [ ] `ground: {materials: [a, b]}` accepted; `material` remains None
- [ ] `ground: {material: a, materials: [a, b]}` raises `SpecError`
- [ ] Legacy string-form specs render byte-identical to pre-change baseline

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_string_form | `ground: grass_flat` | normalised dict with `material="grass_flat"` | pytest |
| test_object_form | object with subset of fields | missing fields None | pytest |
| test_materials_pool | `ground: {materials: [a, b]}` | `materials=[a,b]`, `material=None` | pytest |
| test_material_and_materials_raises | both keys | `SpecError` | pytest |
| test_legacy_byte_identical | pre-change reference PNG | pixel-identical render | pytest |

### §Examples

```python
# tools/sprite-gen/src/spec.py
def _normalize_ground(raw: str | dict | None) -> dict:
    if raw is None:
        return {"material": None, "materials": None,
                "hue_jitter": None, "value_jitter": None, "texture": None}
    if isinstance(raw, str):
        return {"material": raw, "materials": None,
                "hue_jitter": None, "value_jitter": None, "texture": None}
    if isinstance(raw, dict):
        if raw.get("material") and raw.get("materials"):
            raise SpecError("ground: supply either 'material' or 'materials', not both")
        return {
            "material": raw.get("material"),
            "materials": raw.get("materials"),
            "hue_jitter": raw.get("hue_jitter"),
            "value_jitter": raw.get("value_jitter"),
            "texture": raw.get("texture"),
        }
    raise SpecError(f"ground: expected str or dict, got {type(raw).__name__}")
```

### §Mechanical Steps

#### Step 1 — Detect str vs dict; emit object

**Edits:**

- `tools/sprite-gen/src/spec.py` — add `_normalize_ground`; call from top-level loader.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec_ground.py::test_string_form tests/test_spec_ground.py::test_object_form -q
```

#### Step 2 — Fill defaults + `materials` pool

**Edits:**

- Defaults populated; `materials` accepted; error on both.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec_ground.py -q
```

#### Step 3 — Legacy byte-identical regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Should `material: null, materials: null` default to some repo-level default ground? **Resolution:** no — leave unset; composer TECH-718 decides the fallback (likely "no ground band").

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
