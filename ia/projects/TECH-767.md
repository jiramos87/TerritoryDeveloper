---
purpose: "TECH-767 — `iso_fence` primitive."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.6"
---
# TECH-767 — `iso_fence` primitive

> **Issue:** [TECH-767](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Ship `iso_fence` primitive — thin 1–2 px line bordering one side of a footprint. Side kwarg selects cardinal direction.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_fence(canvas, x0, y0, length_px, side, palette, thickness_px=1)` draws 1–2 px line along one side.
2. `side ∈ {n, s, e, w}`; invalid side raises `ValueError`.
3. Palette key `fence` resolves beige/tan colour from active palette.

### 2.2 Non-Goals (Out of Scope)

1. Multi-segment fence (single side only v1).
2. Gate/door openings (deferred).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Border a yard with fencing | Fence renders on correct side without exception |

## 4. Current State

### 4.1 Domain behavior

No `iso_fence` exists yet. DAS §5 R9 specifies fence placement and sides.

### 4.2 Systems map

New file `tools/sprite-gen/src/primitives/iso_fence.py`; re-exported from `primitives/__init__.py`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).

## 5. Proposed Design

### 5.1 Target behavior (product)

Draw thin line (1–2 px) along one cardinal side (N/S/E/W). Palette key `fence` provides beige/tan color.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Pure function signature: `iso_fence(canvas, x0, y0, length_px, side, palette, thickness_px=1, **kwargs)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Single side per call | Composable; no complex multi-side logic | Multi-side in one call (more complex) |

## 7. Implementation Plan

### Phase 1 — Primitive signature + side validator

- [ ] Define signature with `side` parameter
- [ ] Add validation enum for {n, s, e, w}

### Phase 2 — Per-side line-draw geometry (thickness 1–2 px)

- [ ] Implement line drawing for each cardinal direction
- [ ] Support 1–2 px thickness

### Phase 3 — Palette key wiring + smoke render

- [ ] Wire palette key `fence`
- [ ] Test render on residential palette

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Primitive renders without exception on residential palette | Smoke render | `tests/test_decorations_vegetation.py` | Part of vegetation batch test |
| Invalid side raises `ValueError` | Unit test | Developer test | Validates enum check |

## 8. Acceptance Criteria

- [ ] `iso_fence(canvas, x0, y0, length_px, side, palette, thickness_px=1)` draws 1–2 px line along one side.
- [ ] `side ∈ {n, s, e, w}`; invalid side raises `ValueError`.
- [ ] Palette key `fence` resolves beige/tan colour from active palette.

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

Ship `iso_fence` primitive — thin 1–2 px beige/tan line bordering one cardinal side of an anchor point. Palette key `fence`.

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_fence.py` exports `iso_fence(canvas, x0, y0, *, length_px, side, palette, thickness_px=1, **kwargs)`.
- [ ] Re-exported from `primitives/__init__.py`.
- [ ] `residential.json` `materials.fence` with `bright` / `mid` keys added.
- [ ] `side` outside `{'n', 's', 'e', 'w'}` raises `ValueError` with canonical set.
- [ ] `thickness_px` outside `[1, 2]` raises `ValueError` with canonical range.
- [ ] Per-side geometry: `n` = horizontal north, `s` = horizontal south, `e` = vertical east, `w` = vertical west.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_fence_smoke_all_sides` | `side ∈ {'n', 's', 'e', 'w'}`, residential palette | 4 renders; non-empty bbox; fence ramp pixels | pytest |
| `test_iso_fence_side_invalid` | `side='ne'` | `ValueError` with canonical set | pytest |
| `test_iso_fence_thickness_bounds` | `thickness_px ∈ {0, 3}` | `ValueError` each | pytest |
| `test_iso_fence_geometry_n_vs_s` | `side='n'` vs `side='s'` same anchor | y-coords differ (n above, s below) | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_fence(c, 32, 32, length_px=20, side='n', palette=res)` | 20×1 horizontal line north of anchor | Baseline |
| `iso_fence(c, 32, 32, length_px=20, side='e', thickness_px=2, palette=res)` | 2×20 vertical line east of anchor | Thick east |
| `iso_fence(c, 32, 32, length_px=20, side='ne', palette=res)` | `ValueError` "side must be in {'n', 's', 'e', 'w'}" | Invalid |
| `iso_fence(c, 32, 32, length_px=20, side='n', thickness_px=3, palette=res)` | `ValueError` "thickness_px must be in [1, 2]" | Invalid |

### §Mechanical Steps

#### Step 1 — Create `iso_fence` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_fence.py` — cardinal-direction line renderer with thickness bounds.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_fence.py` — **operation**: create; **after** — new file exporting `iso_fence(canvas, x0, y0, *, length_px, side, palette, thickness_px=1, **kwargs)`. Body:
  - `_SIDES = ('n', 's', 'e', 'w')`; validate `side in _SIDES` else `raise ValueError(f"side must be in {set(_SIDES)}")`.
  - Validate `1 <= thickness_px <= 2` else `raise ValueError("thickness_px must be in [1, 2]")`.
  - Resolve `palette["materials"]["fence"]["bright"]`.
  - Geometry per side:
    - `n`: rect `(x0, y0 - thickness_px)` → `(x0 + length_px - 1, y0 - 1)`.
    - `s`: rect `(x0, y0 + 1)` → `(x0 + length_px - 1, y0 + thickness_px)`.
    - `e`: rect `(x0 + 1, y0)` → `(x0 + thickness_px, y0 + length_px - 1)`.
    - `w`: rect `(x0 - thickness_px, y0)` → `(x0 - 1, y0 + length_px - 1)`.
  - Fill via `canvas.putpixel`.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_fence.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_fence.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Side tuple drift → re-open to canonical `_SIDES = ('n', 's', 'e', 'w')`.

**MCP hints:** `plan_digest_verify_paths`.

#### Step 2 — Add `fence` palette entry

**Goal:** Insert `materials.fence` with `bright` + `mid` above `mustard_industrial`.

**Edits:**
- `tools/sprite-gen/palettes/residential.json` — **before**:
  ```
      "mustard_industrial": {
  ```
  **after**:
  ```
      "fence": {
        "bright": [214, 188, 130],
        "mid": [168, 142, 92]
      },
      "mustard_industrial": {
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** JSON parse fail → re-open Step 2.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

#### Step 3 — Re-export `iso_fence` from primitives `__init__`

**Goal:** Register `iso_fence` in primitives package alphabetically.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_cube import iso_cube
  ```
  **after**:
  ```
  from .iso_cube import iso_cube
  from .iso_fence import iso_fence
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_cube",
  ```
  **after**:
  ```
      "iso_cube",
      "iso_fence",
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Import fail → re-open Step 1.

**MCP hints:** `plan_digest_resolve_anchor`.

## Open Questions (resolve before / during implementation)

None — primitive design fully specified in master plan Stage 7.
