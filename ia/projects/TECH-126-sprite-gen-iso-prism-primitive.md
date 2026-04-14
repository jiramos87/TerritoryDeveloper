---
purpose: "TECH-126 — Sprite gen iso_prism primitive."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-126 — Sprite gen iso_prism primitive

> **Issue:** [TECH-126](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Second core primitive — `iso_prism` renders a pitched-roof prism in 2:1 isometric projection. Two sloped top-faces + two triangular end-faces; ridge runs along NS or EW axis per `axis` arg. Same NW-light 3-level shade ramp as `iso_cube`. Enables all pitched-roof archetypes in Stage 1.2+ YAML specs.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_prism(canvas, x0, y0, w, d, h, pitch, axis, material)` draws pitched prism.
2. `axis ∈ {'ns','ew'}` selects ridge direction.
3. `pitch` (0..1) scales ridge height relative to `h`.
4. Shade mapping — sloped faces bright (top-ish) vs mid (shadowed side); triangular ends use east/dark.

### 2.2 Non-Goals

1. No palette lookup — material stays stub tuple until Stage 1.3.
2. No hipped roofs, no dormers — post-MVP per exploration.
3. No non-square footprints — v1 locks 1×1 family.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Call `iso_prism(canvas, 0, 0, 2, 2, 16, 0.5, 'ns', stub_brown)` → prism with NS ridge renders. | Smoke test passes (T1.1.6). |

## 4. Current State

### 4.1 Domain behavior

No primitives exist. Exploration §5 specifies prism geometry + axis/pitch semantics.

### 4.2 Systems map

- `docs/isometric-sprite-generator-exploration.md` §5 Primitives — ground truth.
- `tools/sprite-gen/src/primitives/iso_prism.py` — new.
- `tools/sprite-gen/src/canvas.py` — consumed (TECH-124).

## 5. Proposed Design

### 5.1 Target behavior

Two sloped quads meeting at ridge + two triangular gables. Ridge height = `h * pitch`. Base at stacked top of preceding `iso_cube` call (composer's job to layer).

### 5.2 Architecture

Pure function; mutates canvas via Pillow polygons. Axis switch: `ns` → ridge runs between north + south corners; `ew` → ridge between east + west corners. Shade ramp mirrors `iso_cube` (bright top-facing slope, mid shadow slope, dark gable ends).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | `axis` string enum `'ns'`/`'ew'`. | Readable in YAML specs; no extra imports. | Int flag 0/1; Enum class. |

## 7. Implementation Plan

### Phase 1 — primitive

- [ ] Compute ridge endpoints from `axis` + `pitch`.
- [ ] Build two slope quads + two triangle gables.
- [ ] Fill w/ stub bright/mid/dark ramp.
- [ ] Docstring cites §5.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Prism renders non-transparent pixels on all visible faces | pytest (manual) | `pytest tools/sprite-gen/tests/test_primitives.py` | Wired in T1.1.6 (TECH-128). |
| Repo IA gates green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `iso_prism` signature matches Stage 1.1 Exit.
- [ ] Both `axis` values render cleanly.
- [ ] Pitch 0..1 scales ridge as expected.
- [ ] Same shade ramp as `iso_cube`.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
