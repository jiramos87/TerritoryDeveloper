---
purpose: "TECH-765 — `iso_pool` primitive."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.4"
---
# TECH-765 — `iso_pool` primitive

> **Issue:** [TECH-765](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Ship `iso_pool` primitive — light-blue rectangle with white rim. Size kwargs bounded to 8–20 px. Hard-gated by composer on 1×1 footprints (composer enforcement lives in T7.8 / TECH-769).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_pool(canvas, x0, y0, w_px, d_px, palette)` draws filled rectangle with 1-px white rim.
2. `w_px, d_px ∈ [8, 20]`; out-of-range raises `ValueError`.
3. Palette key `pool` resolves light-blue + white-rim colours from active palette.

### 2.2 Non-Goals (Out of Scope)

1. Water ripple animation (deferred).
2. Outline pass (deferred to Stage 12).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Place a pool in a 2×2 suburban yard | Pool renders correctly with water color + white rim |

## 4. Current State

### 4.1 Domain behavior

No `iso_pool` exists yet. DAS §5 R9 specifies pool placement constraints and palette keys.

### 4.2 Systems map

New file `tools/sprite-gen/src/primitives/iso_pool.py`; re-exported from `primitives/__init__.py`. Composer gate (1×1 rejection) lives in T7.8 / TECH-769 — scope boundary noted here; primitive itself is footprint-agnostic. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).

## 5. Proposed Design

### 5.1 Target behavior (product)

Draw a filled light-blue rectangle with 1-px white rim. Sizes bounded to 8–20 px to keep proportions reasonable for isometric tiles.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Pure function signature: `iso_pool(canvas, x0, y0, w_px, d_px, palette, **kwargs)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | 8–20 px bounds | Keeps pools proportional on tiles | 5–25 px (too permissive), 10–15 px (too restrictive) |
| 2026-04-24 | Composer gates 1×1 rejection | Pools require space; enforcing at caller | Primitive self-gates (less modular) |

## 7. Implementation Plan

### Phase 1 — Primitive signature + size-range validator

- [ ] Define signature with `w_px`, `d_px` parameters
- [ ] Add bounds validation [8, 20]

### Phase 2 — Light-blue rectangle fill + white rim draw

- [ ] Draw filled blue rectangle
- [ ] Draw 1-px white border

### Phase 3 — Palette key wiring + smoke render

- [ ] Wire palette key `pool` for colors
- [ ] Test render under residential palette

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Primitive renders without exception on residential palette | Smoke render | `tests/test_decorations_vegetation.py` | Part of vegetation batch test |
| Out-of-range sizes raise `ValueError` | Unit test | Developer test | Validates bounds check |

## 8. Acceptance Criteria

- [ ] `iso_pool(canvas, x0, y0, w_px, d_px, palette)` draws filled rectangle with 1-px white rim.
- [ ] `w_px, d_px ∈ [8, 20]`; out-of-range raises `ValueError`.
- [ ] Palette key `pool` resolves light-blue + white-rim colours from active palette.

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

Ship `iso_pool` primitive — light-blue rectangle with 1-px white rim, size-bound `[8, 20]` px. Primitive is footprint-agnostic; 1×1 scope gate lives in composer (TECH-769).

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_pool.py` exports `iso_pool(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`.
- [ ] Re-exported from `primitives/__init__.py`.
- [ ] `residential.json` `materials.pool` with `bright` / `mid` / `rim` keys added.
- [ ] `w_px` or `d_px` outside `[8, 20]` raises `ValueError` with canonical range.
- [ ] Light-blue filled rectangle rendered with 1-px white rim.
- [ ] Primitive contains no footprint knowledge (composer owns 1×1 gate per TECH-769).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_pool_smoke_residential` | `w_px=12, d_px=10` | non-empty bbox; light-blue + white pixels present | pytest |
| `test_iso_pool_bounds_low` | `w_px ∈ {7, 21}`, `d_px ∈ {7, 21}` | `ValueError` each | pytest |
| `test_iso_pool_bounds_inclusive` | `w_px=8, d_px=20` | no exception; rect rendered | pytest |
| `test_iso_pool_rim_is_1px_white` | render + inspect outermost ring | all ring pixels = `pool.rim` | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_pool(c, 32, 32, w_px=12, d_px=10, palette=res)` | 12×10 light-blue rect with 1-px white border | Baseline |
| `iso_pool(c, 32, 32, w_px=8, d_px=8, palette=res)` | Min-size 8×8 with rim | Lower bound |
| `iso_pool(c, 32, 32, w_px=20, d_px=20, palette=res)` | Max-size 20×20 with rim | Upper bound |
| `iso_pool(c, 32, 32, w_px=7, d_px=10, palette=res)` | `ValueError` "w_px must be in [8, 20]" | Invalid |
| `iso_pool(c, 32, 32, w_px=12, d_px=25, palette=res)` | `ValueError` "d_px must be in [8, 20]" | Invalid |

### §Mechanical Steps

#### Step 1 — Create `iso_pool` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_pool.py` — rectangle + rim renderer with explicit size bounds.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_pool.py` — **operation**: create; **after** — new file exporting `iso_pool(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`. Body:
  - Validate `8 <= w_px <= 20` else `raise ValueError("w_px must be in [8, 20]")`.
  - Validate `8 <= d_px <= 20` else `raise ValueError("d_px must be in [8, 20]")`.
  - Resolve `palette["materials"]["pool"]` (raise `PaletteKeyError` on missing).
  - `fill = tuple(ramp["bright"]) + (255,)`; `rim = tuple(ramp["rim"]) + (255,)`.
  - Fill inner rect: for `y in range(y0 + 1, y0 + d_px - 1)` and `x in range(x0 + 1, x0 + w_px - 1)` → `canvas.putpixel((x, y), fill)`.
  - Draw 1-px rim ring at `(x0, y0)`..`(x0 + w_px - 1, y0 + d_px - 1)` using `rim`.
  - Docstring notes: "Primitive is footprint-agnostic. 1×1 scope rejection lives in `compose_sprite` (TECH-769 / T7.8)."
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_pool.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_pool.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Scope-gate drift (primitive inspects footprint) → re-open Step 1 to remove footprint logic.

**MCP hints:** `plan_digest_verify_paths`, `backlog_issue` (for TECH-769 scope boundary cross-ref).

#### Step 2 — Add `pool` palette entry

**Goal:** Insert `materials.pool` with `bright`, `mid`, `rim` keys above `mustard_industrial`.

**Edits:**
- `tools/sprite-gen/palettes/residential.json` — **before**:
  ```
      "mustard_industrial": {
  ```
  **after**:
  ```
      "pool": {
        "bright": [112, 198, 232],
        "mid": [78, 158, 198],
        "rim": [248, 248, 248]
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

**STOP:** JSON parse fail → re-open Step 2 (trailing commas).

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

#### Step 3 — Re-export `iso_pool` from primitives `__init__`

**Goal:** Register `iso_pool` in primitives package alphabetically.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_prism import iso_prism
  ```
  **after**:
  ```
  from .iso_pool import iso_pool
  from .iso_prism import iso_prism
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_prism",
  ```
  **after**:
  ```
      "iso_pool",
      "iso_prism",
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
