---
purpose: "TECH-766 — `iso_path` + `iso_pavement_patch` primitives."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.5"
---
# TECH-766 — `iso_path` + `iso_pavement_patch` primitives

> **Issue:** [TECH-766](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Two pavement-family primitives. `iso_path` = narrow directional walkway; `iso_pavement_patch` = rectangular surface fill. Shared palette key `pavement`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_path(canvas, x0, y0, length_px, axis, palette, width_px=2)` draws strip; `axis ∈ {ns, ew}`; `width_px ∈ [2, 4]`.
2. `iso_pavement_patch(canvas, x0, y0, w_px, d_px, palette)` fills rect with beige/grey under palette key `pavement`.
3. Invalid axis or width raises `ValueError` with canonical list in message.

### 2.2 Non-Goals (Out of Scope)

1. Texture variation or pattern (solid color v1).
2. Outline pass (deferred to Stage 12).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Add walkways and paved yards to designs | Paths and patches render without exception; colors match palette |

## 4. Current State

### 4.1 Domain behavior

No `iso_path` or `iso_pavement_patch` exist yet. DAS §5 R9 specifies placement and constraints.

### 4.2 Systems map

New files `tools/sprite-gen/src/primitives/iso_path.py` + `iso_pavement_patch.py`; both re-exported from `primitives/__init__.py`. Shared palette key `pavement` in `palettes/*.json`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).

## 5. Proposed Design

### 5.1 Target behavior (product)

`iso_path`: Narrow strip walkway in cardinal directions (ns = north-south, ew = east-west), width 2–4 px.
`iso_pavement_patch`: Rectangular fill, same palette.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Two pure functions: `iso_path(canvas, x0, y0, length_px, axis, palette, width_px=2, **kwargs)` and `iso_pavement_patch(canvas, x0, y0, w_px, d_px, palette, **kwargs)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Colocate in same task | Both pavement-family, simple, palette-driven | Separate tasks (overkill) |

## 7. Implementation Plan

### Phase 1 — `iso_path` axis + width validation + strip draw

- [ ] Define signature with `axis` and `width_px` parameters
- [ ] Add validation for axis ∈ {ns, ew} and width ∈ [2, 4]
- [ ] Draw strip per axis

### Phase 2 — `iso_pavement_patch` rect-fill draw

- [ ] Draw filled rectangle with palette color

### Phase 3 — Re-export + smoke-render both primitives

- [ ] Add to `__init__.py`
- [ ] Smoke test both on residential palette

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Both render without exception on residential palette | Smoke render | `tests/test_decorations_vegetation.py` | Part of vegetation batch test |
| Invalid axis or width raise `ValueError` | Unit test | Developer test | Validates bounds and enum checks |

## 8. Acceptance Criteria

- [ ] `iso_path(canvas, x0, y0, length_px, axis, palette, width_px=2)` draws strip; `axis ∈ {ns, ew}`; `width_px ∈ [2, 4]`.
- [ ] `iso_pavement_patch(canvas, x0, y0, w_px, d_px, palette)` fills rect with beige/grey under palette key `pavement`.
- [ ] Invalid axis or width raises `ValueError` with canonical list in message.

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

Ship `iso_path` (narrow directional walkway) + `iso_pavement_patch` (rect fill) primitives. Both reuse existing `materials.pavement` ramp — no palette edit required.

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_path.py` exports `iso_path(canvas, x0, y0, *, length_px, axis, palette, width_px=2, **kwargs)`.
- [ ] `tools/sprite-gen/src/primitives/iso_pavement_patch.py` exports `iso_pavement_patch(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`.
- [ ] Both re-exported from `primitives/__init__.py`.
- [ ] `axis` outside `{'ns', 'ew'}` raises `ValueError` with canonical set.
- [ ] `width_px` outside `[2, 4]` raises `ValueError` with canonical range.
- [ ] `w_px` or `d_px` `< 1` raises `ValueError` on `iso_pavement_patch`.
- [ ] Both primitives consume existing `palette["materials"]["pavement"]` — no new palette entry.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_path_smoke_both_axes` | `axis ∈ {'ns', 'ew'}`, residential palette | both render; non-empty bbox; pavement ramp pixels present | pytest |
| `test_iso_path_axis_invalid` | `axis='diag'` | `ValueError` with canonical set | pytest |
| `test_iso_path_width_bounds` | `width_px ∈ {1, 5}` | `ValueError` each | pytest |
| `test_iso_pavement_patch_smoke` | `w_px=10, d_px=10` | non-empty bbox; rect filled with pavement ramp | pytest |
| `test_iso_pavement_patch_size_positive` | `w_px=0` or `d_px=0` | `ValueError` | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_path(c, 32, 32, length_px=20, axis='ns', width_px=2, palette=res)` | 2×20 strip oriented NS | Baseline |
| `iso_path(c, 32, 32, length_px=20, axis='ew', width_px=4, palette=res)` | 20×4 strip oriented EW | Wide |
| `iso_path(c, 32, 32, length_px=20, axis='diag', width_px=2, palette=res)` | `ValueError` "axis must be in {'ns', 'ew'}" | Invalid |
| `iso_path(c, 32, 32, length_px=20, axis='ns', width_px=5, palette=res)` | `ValueError` "width_px must be in [2, 4]" | Invalid |
| `iso_pavement_patch(c, 32, 32, w_px=10, d_px=10, palette=res)` | 10×10 filled pavement rect | Baseline |
| `iso_pavement_patch(c, 32, 32, w_px=0, d_px=10, palette=res)` | `ValueError` "w_px must be >= 1" | Invalid |

### §Mechanical Steps

#### Step 1 — Create `iso_path` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_path.py` — strip renderer with axis enum + width bounds.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_path.py` — **operation**: create; **after** — new file exporting `iso_path(canvas, x0, y0, *, length_px, axis, palette, width_px=2, **kwargs)`. Body:
  - `_AXES = ('ns', 'ew')`; validate `axis in _AXES` else `raise ValueError(f"axis must be in {set(_AXES)}")`.
  - Validate `2 <= width_px <= 4` else `raise ValueError("width_px must be in [2, 4]")`.
  - Resolve `palette["materials"]["pavement"]["mid"]` (raise `PaletteKeyError` on missing).
  - If `axis == 'ns'`: draw rect `(x0, y0)` to `(x0 + width_px - 1, y0 + length_px - 1)`.
  - If `axis == 'ew'`: draw rect `(x0, y0)` to `(x0 + length_px - 1, y0 + width_px - 1)`.
  - Fill each pixel via `canvas.putpixel`.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_path.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_path.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Axis tuple drift → re-open to canonical `_AXES = ('ns', 'ew')`.

**MCP hints:** `plan_digest_verify_paths`.

#### Step 2 — Create `iso_pavement_patch` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_pavement_patch.py` — free-form rect fill.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_pavement_patch.py` — **operation**: create; **after** — new file exporting `iso_pavement_patch(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`. Body:
  - Validate `w_px >= 1` else `raise ValueError("w_px must be >= 1")`.
  - Validate `d_px >= 1` else `raise ValueError("d_px must be >= 1")`.
  - Resolve `palette["materials"]["pavement"]["mid"]`.
  - Fill rect `(x0, y0)` to `(x0 + w_px - 1, y0 + d_px - 1)` via `canvas.putpixel`.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_pavement_patch.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_pavement_patch.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 2.

**MCP hints:** `plan_digest_verify_paths`.

#### Step 3 — Re-export `iso_path` + `iso_pavement_patch` from primitives `__init__`

**Goal:** Register both primitives alphabetically in package.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_ground_noise import iso_ground_noise
  ```
  **after**:
  ```
  from .iso_ground_noise import iso_ground_noise
  from .iso_path import iso_path
  from .iso_pavement_patch import iso_pavement_patch
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_ground_noise",
  ```
  **after**:
  ```
      "iso_ground_noise",
      "iso_path",
      "iso_pavement_patch",
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Import fail → re-open Step 1 or Step 2.

**MCP hints:** `plan_digest_resolve_anchor`.

## Open Questions (resolve before / during implementation)

None — primitive design fully specified in master plan Stage 7.
