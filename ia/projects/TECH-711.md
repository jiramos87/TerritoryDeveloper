---
purpose: "TECH-711 — Composer resolve_building_box helper + variant loop sampling."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.3.3
---
# TECH-711 — Composer `resolve_building_box` helper + variant loop sampling

> **Issue:** [TECH-711](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Author a pure helper `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` in `tools/sprite-gen/src/compose.py` that encapsulates the footprint_px / ratio / align / padding math introduced by TECH-709. Wire the composer's variant loop to sample each `vary:` range deterministically from `geometry_seed + i` for geometry axes and `palette_seed + i` for palette — per split-seed semantics from TECH-710. Legacy specs (no placement fields, scalar variants, scalar seed) render byte-identical.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `resolve_building_box(spec)` is a pure function; returns `(bx, by, offset_x, offset_y)` tuple.
2. Variant loop uses `geometry_seed + i` for geometry axes, `palette_seed + i` for palette.
3. Legacy specs produce byte-identical output (zero diff vs baseline).

### 2.2 Non-Goals

1. Schema loading — TECH-709 + TECH-710.
2. CLI bootstrap — TECH-712.
3. Tests — TECH-713 (own task for placement + variant determinism).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | `align: sw` shifts building to SW corner | Rendered bbox left-aligned to diamond SW |
| 2 | Spec author | `palette_seed=101 geometry_seed=202` | Two runs with same seeds produce identical output |
| 3 | Legacy spec | No regression | Byte-identical render vs pre-change |

## 4. Current State

### 4.1 Domain behavior

`compose.py` centers the building on the canvas via `(h_px - b_h) // 2`-style math. No padding / align support; single seed fans to all random calls.

### 4.2 Systems map

- `tools/sprite-gen/src/compose.py` — target.
- Inputs: `spec` dict from `load_spec` (TECH-709 + TECH-710 surface the new fields).
- Consumers: `tests/test_building_placement.py` + `test_variants_geometric.py` + `test_split_seeds.py` (TECH-713).

### 4.3 Implementation investigation notes

`resolve_building_box` should take only the `spec` dict — no I/O. Return type: NamedTuple-like `(bx, by, offset_x, offset_y)`. Composer callers read tuple positionally to keep call sites concise.

## 5. Proposed Design

### 5.1 Target behavior

```python
from src.spec import load_spec
from src.compose import resolve_building_box

spec = load_spec("specs/building_residential_sw.yaml")
bx, by, offset_x, offset_y = resolve_building_box(spec)
# bx, by = footprint_px (or canvas_w * ratio); offset_x, offset_y = anchor + padding delta
```

### 5.2 Architecture / implementation

- Step 1: derive `(bx, by)` from `footprint_px` (preferred) or `footprint_ratio * canvas_dim`.
- Step 2: derive base SE-corner anchor; adjust by `align` to SW/NE/NW/SE/center.
- Step 3: apply `padding.{n,e,s,w}` as signed offsets.
- Variant loop: `palette_rng = random.Random(spec["palette_seed"] + i)`; `geometry_rng = random.Random(spec["geometry_seed"] + i)`; sample `vary:` ranges from whichever rng is relevant for the axis.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | `resolve_building_box` returns plain tuple | Concise destructure at call sites; no class overhead | NamedTuple — acceptable; adds field names at call sites but costs an import |
| 2026-04-23 | Separate `random.Random` instances per axis | Guarantees split-seed independence | Single rng with axis-dependent consumes — rejected, fragile under reordering |

## 7. Implementation Plan

### Phase 1 — `resolve_building_box` pure helper

- [ ] Author helper + unit tests (no composer wiring yet).

### Phase 2 — Composer wiring

- [ ] Replace current centering math with `resolve_building_box` call.
- [ ] Assert byte-identical output for existing specs (`building_residential_small` + 3 `light_*`).

### Phase 3 — Variant loop split-seed sampling

- [ ] Construct `palette_rng` + `geometry_rng`; sample `vary:` ranges.
- [ ] `seed_scope: palette` → variants only palette samples change; `seed_scope: geometry` → geometry only; `palette+geometry` → both.

### Phase 4 — Full-suite regression

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Helper pure | Python | `pytest tests/test_compose.py::test_resolve_building_box -q` | Unit tests for each placement combo |
| Byte-identical legacy | Python | `cd tools/sprite-gen && python3 -m pytest tests/test_render_integration.py -q` | TECH-703 parametrized regression still green |
| Split seeds | Python | `pytest tools/sprite-gen/tests/test_split_seeds.py -q` (authored in TECH-713) | Independence holds |
| Full suite | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ green |

## 8. Acceptance Criteria

- [ ] `resolve_building_box` unit-tested.
- [ ] Composer wires helper.
- [ ] Variant loop uses split seeds.
- [ ] Legacy specs byte-identical.
- [ ] Full pytest green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Pure helpers (no PIL / no I/O) at the composer's seam points make unit testing trivial and prevent regressions in rendering pipelines.

## §Plan Digest

### §Goal

Honour new placement + variant surface from TECH-709/710 by (a) shipping pure `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` and (b) wiring the composer variant loop to use `palette_seed + i` for palette samples and `geometry_seed + i` for geometry samples. Legacy specs render byte-identical.

### §Acceptance

- [ ] `resolve_building_box(spec)` returns `(bx, by, offset_x, offset_y)` across all placement combos (footprint_px, ratio, padding, align)
- [ ] Composer replaces hand-rolled centering with helper call
- [ ] Variant loop constructs separate rng per axis: `palette_rng = Random(palette_seed + i)`, `geometry_rng = Random(geometry_seed + i)`
- [ ] `seed_scope: palette` / `geometry` / `palette+geometry` each behave per spec
- [ ] Legacy specs render byte-identical (TECH-703 parametrized regression still green)
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_resolve_building_box_center | legacy spec | offsets match current centering math | pytest |
| test_resolve_building_box_sw | `align: sw` | anchor to SW corner | pytest |
| test_resolve_building_box_footprint_px | `footprint_px: [28, 28]` | bx, by = 28 regardless of ratio | pytest |
| test_resolve_building_box_padding | `padding: {s: 10}` | building shifted 10 px north | pytest |
| test_variant_loop_split_seed | `palette_seed=101, geometry_seed=202, seed_scope: palette+geometry` | deterministic across runs; distinct across variants | pytest |
| legacy_byte_identical | all live 1×1 specs | bbox == (0,15,64,48) | TECH-703 parametrized regression |

### §Examples

```python
# tools/sprite-gen/src/compose.py
from random import Random

def resolve_building_box(spec: dict) -> tuple[int, int, int, int]:
    building = spec.get("building", {})
    canvas_w, canvas_h = spec.get("canvas", [64, 64])
    if "footprint_px" in building:
        bx, by = building["footprint_px"]
    else:
        ratio = building.get("footprint_ratio", [1.0, 1.0])
        bx = int(round(canvas_w * ratio[0]))
        by = int(round(canvas_h * ratio[1]))
    align = building.get("align", "center")
    padding = building["padding"]  # already normalized by TECH-709
    # anchor math: derive (offset_x, offset_y) from align + padding
    ox, oy = _anchor_offset(canvas_w, canvas_h, bx, by, align)
    ox += padding["w"] - padding["e"]
    oy += padding["n"] - padding["s"]
    return bx, by, ox, oy

def _sample_variant(spec: dict, i: int) -> dict:
    palette_rng = Random(spec["palette_seed"] + i)
    geometry_rng = Random(spec["geometry_seed"] + i)
    scope = spec["variants"]["seed_scope"]
    vary = spec["variants"]["vary"]
    # sample each vary axis using rng indicated by scope
    return _apply_vary(spec, vary, palette_rng, geometry_rng, scope)
```

### §Mechanical Steps

#### Step 1 — Author `resolve_building_box` + unit tests

**Edits:**

- `tools/sprite-gen/src/compose.py` — new helper.
- `tools/sprite-gen/tests/test_compose.py` — unit tests.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_compose.py::test_resolve_building_box -q
```

#### Step 2 — Wire composer to use helper

**Edits:**

- `tools/sprite-gen/src/compose.py` — replace centering math with `resolve_building_box(spec)`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_render_integration.py -q
```

**STOP:** Byte-diff on legacy specs → inspect default path; `align: center` + zero padding must produce the same `(offset_x, offset_y)` as old math.

#### Step 3 — Variant loop split-seed sampling

**Edits:**

- `tools/sprite-gen/src/compose.py` — `_sample_variant(spec, i)` helper; wire into variant loop.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Determinism test red → check that rngs are constructed fresh per variant iteration (not shared).

**MCP hints:** none — pure composer edit.

## Open Questions (resolve before / during implementation)

1. `canvas` field — where does it come from? **Resolution:** `spec.canvas = [w, h]` already set at render time (existing); default `[64, 64]`.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
