---
purpose: "TECH-709 — Placement schema additions (building.footprint_px / padding / align)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.3.1
---
# TECH-709 — Placement schema: `building.footprint_px` / `padding` / `align`

> **Issue:** [TECH-709](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/spec.py` schema to accept three new placement fields on the `building:` block: `footprint_px: [bx, by]` (pixel-exact footprint; wins over `footprint_ratio`), `padding: {n, e, s, w}` (asymmetric empty space per side in px), and `align ∈ {center, sw, ne, nw, se, custom}` (anchor side). Back-compat by construction — any spec without these fields renders byte-identical to today. Consumes L5.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `building.footprint_px` surfaced via `load_spec` when present.
2. `building.padding` surfaced with default `{n: 0, e: 0, s: 0, w: 0}`.
3. `building.align` surfaced with default `center`; accepted values enumerated.
4. Existing specs produce byte-identical render output.

### 2.2 Non-Goals

1. Composer wiring — TECH-711 consumes the schema.
2. Variant + seed changes — TECH-710.
3. DAS doc update — TECH-714.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Place building pixel-exact instead of ratio-scaled | `building.footprint_px: [28, 28]` overrides `footprint_ratio` |
| 2 | Spec author | Pad the south side of the tile (front yard) | `building.padding: {s: 10}` shifts building north |
| 3 | Spec author | Anchor a building to the SW corner | `building.align: sw` moves building to SW corner |

## 4. Current State

### 4.1 Domain behavior

Today `building.footprint_ratio: [wr, dr]` controls building size as a fraction of the 1×1 diamond. No padding; no alignment; building always centered.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py` — target loader.
- Consumers: `src/compose.py::resolve_building_box` (TECH-711); `tests/test_building_placement.py` (TECH-713).

### 4.3 Implementation investigation notes

`building.footprint_px` wins over `footprint_ratio` when both present — loader emits a `DeprecationWarning` on conflict, to match the same behaviour from Stage 6 TECH-693's `normalize_dims` helper.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# New fields (all optional)
building:
  footprint_px: [28, 28]           # optional; wins over footprint_ratio
  footprint_ratio: [0.45, 0.45]    # fallback (existing)
  padding: { n: 0, e: 0, s: 0, w: 0 }  # default all 0
  align: center                    # {center, sw, ne, nw, se, custom}; default center
```

### 5.2 Architecture / implementation

- `load_spec` normalises `building.padding` to the 4-key dict (even if author supplies only some keys).
- `load_spec` validates `building.align` against the enum; unknown value raises `SpecValidationError`.
- When both `footprint_px` and `footprint_ratio` present, `footprint_px` wins; warn once.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | `footprint_px` wins over `footprint_ratio` on conflict | Explicit pixel sizing is always more precise; ratio is a convenience fallback | Raise on conflict — rejected, over-strict for iterative authoring |
| 2026-04-23 | Padding default `{0, 0, 0, 0}` (not `null`) | Composer can always read `padding.n` without `None` checks | `null` default — rejected, forces every consumer to handle absence |
| 2026-04-23 | `align: custom` accepted but deferred to consumer (TECH-711) | Escape hatch for composer-level `offset_x`/`offset_y` fields | Drop `custom` — rejected, future-proofing for explicit placement |

## 7. Implementation Plan

### Phase 1 — Schema additions

- [ ] Extend loader to accept 3 new fields with defaults.
- [ ] Validate `align` enum; raise `SpecValidationError` on invalid.

### Phase 2 — Conflict policy

- [ ] Warn (`warnings.warn(DeprecationWarning, ...)`) when both `footprint_px` and `footprint_ratio` set.

### Phase 3 — Tests

- [ ] Unit tests for defaults, explicit values, enum validation, conflict warn.

### Phase 4 — Full-suite regression

- [ ] `pytest tools/sprite-gen/tests/ -q` — byte-identical for existing specs.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Defaults surfaced | Python | `pytest tests/test_spec.py::test_placement_defaults -q` | `padding == {n:0,e:0,s:0,w:0}`, `align == "center"` |
| Explicit fields surfaced | Python | `pytest tests/test_spec.py::test_placement_explicit -q` | All three fields round-trip |
| Invalid align raises | Python | `pytest tests/test_spec.py::test_align_invalid -q` | `SpecValidationError` |
| Conflict warns | Python | `pytest tests/test_spec.py::test_footprint_conflict_warn -q` | `DeprecationWarning` emitted |
| Full suite | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ green |

## 8. Acceptance Criteria

- [ ] `load_spec` surfaces all three new fields with documented defaults.
- [ ] Enum validation on `align`; conflict warning on dual footprint.
- [ ] Existing specs render byte-identical.
- [ ] Full pytest green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Defaulting to concrete shapes (empty dict → `{n:0,e:0,s:0,w:0}`) eliminates None-check sprawl in every downstream consumer.

## §Plan Digest

### §Goal

Add pixel-exact placement fields (`building.footprint_px`, `padding`, `align`) to the spec loader with sensible defaults + enum validation, preserving byte-identical render output for existing specs. Consumes L5.

### §Acceptance

- [ ] `load_spec` surfaces `building.footprint_px` when present
- [ ] `load_spec` surfaces `building.padding` with default `{n:0,e:0,s:0,w:0}`
- [ ] `load_spec` surfaces `building.align` with default `center`; invalid raises `SpecValidationError`
- [ ] Conflict `footprint_px` + `footprint_ratio` emits `DeprecationWarning`; `footprint_px` wins
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed (byte-identical on existing specs)

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_placement_defaults | spec YAML without placement fields | `padding == {n:0,e:0,s:0,w:0}`, `align == "center"`, `footprint_px` absent | pytest |
| test_placement_explicit | spec YAML with all 3 fields | fields surfaced exactly | pytest |
| test_align_invalid | spec with `align: diagonal` | `SpecValidationError` | pytest |
| test_footprint_conflict_warn | spec with both `footprint_px` + `footprint_ratio` | `DeprecationWarning`; `footprint_px` wins | pytest |
| full_suite | all tests | 221+ passed | `cd tools/sprite-gen && python3 -m pytest tests/ -q` |

### §Examples

```yaml
# specs/building_residential_compact.yaml
class: residential_small
footprint: [1, 1]
building:
  footprint_px: [28, 28]
  padding: { n: 0, e: 0, s: 10, w: 0 }
  align: sw
composition:
  - primitive: iso_cube
    w_px: 28
    d_px: 28
    h_px: 28
    material: wall_brick_red
```

Loader skeleton:

```python
# tools/sprite-gen/src/spec.py
_VALID_ALIGNS = {"center", "sw", "ne", "nw", "se", "custom"}

def _normalize_building_placement(building: dict) -> dict:
    padding = building.get("padding") or {}
    building["padding"] = {
        "n": int(padding.get("n", 0)),
        "e": int(padding.get("e", 0)),
        "s": int(padding.get("s", 0)),
        "w": int(padding.get("w", 0)),
    }
    align = building.get("align", "center")
    if align not in _VALID_ALIGNS:
        raise SpecValidationError(
            f"building.align={align!r} not in {sorted(_VALID_ALIGNS)}"
        )
    building["align"] = align
    if "footprint_px" in building and "footprint_ratio" in building:
        warnings.warn(
            "building.footprint_px wins over footprint_ratio; drop one.",
            DeprecationWarning,
            stacklevel=2,
        )
    return building
```

### §Mechanical Steps

#### Step 1 — Add defaults + enum validation

**Edits:**

- `tools/sprite-gen/src/spec.py` — `_normalize_building_placement` helper; call from `load_spec`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.spec import load_spec; s = load_spec('specs/building_residential_small.yaml'); assert s['building']['align'] == 'center'; assert s['building']['padding'] == {'n':0,'e':0,'s':0,'w':0}; print('ok')"
```

#### Step 2 — Conflict warning

**Edits:**

- `tools/sprite-gen/src/spec.py` — `warnings.warn(..., DeprecationWarning)` when both fields present.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec.py::test_footprint_conflict_warn -q
```

#### Step 3 — Full-suite regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Any render regression → placement defaults must not affect composer paths until TECH-711; revert and check whether composer is reading these fields prematurely.

**MCP hints:** none — loader-only.

## Open Questions (resolve before / during implementation)

1. Should `padding` accept strings like `"10px"`? **Resolution:** no — integer px only; `SpecValidationError` on non-int.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
