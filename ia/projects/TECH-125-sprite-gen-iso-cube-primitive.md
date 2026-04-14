---
purpose: "TECH-125 — Sprite gen iso_cube primitive."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-125 — Sprite gen iso_cube primitive

> **Issue:** [TECH-125](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

First core primitive — `iso_cube` renders a rectangular box in 2:1 isometric projection with NW-light 3-level shade pass (top=bright, south=mid, east=dark). Draws three Pillow polygons (top rhombus + south parallelogram + east parallelogram) onto a target canvas. Shared basis for all building wall + mass primitives.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_cube(canvas, x0, y0, w, d, h, material)` draws three faces on `canvas` (PIL.Image) at origin `(x0, y0)`.
2. Pixel coords computed from 2:1 isometric projection using **Tile dimensions** (tileWidth=1, tileHeight=0.5).
3. NW-light direction hardcoded MVP — bright top, mid south, dark east.
4. `material` param stays a stub RGB tuple this task; palette wiring lands Stage 1.3.

### 2.2 Non-Goals

1. No palette lookup — material stays stub tuple until T1.3.4 (not yet filed).
2. No slope foundation — Stage 1.4.
3. No roof / prism shape — separate primitive (TECH-126).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Call `iso_cube(canvas, 0, 0, 2, 2, 32, stub_red)` → three visible faces with distinct bright/mid/dark shade. | Smoke test passes (T1.1.6). |

## 4. Current State

### 4.1 Domain behavior

No primitives exist. Exploration §5 specifies shape + shade semantics.

### 4.2 Systems map

- `docs/isometric-sprite-generator-exploration.md` §5 Primitives — ground truth (iso_cube polygon geometry + shade mapping).
- `docs/isometric-sprite-generator-exploration.md` §4 Canvas math — projection basis.
- `tools/sprite-gen/src/primitives/iso_cube.py` — new.
- `tools/sprite-gen/src/canvas.py` — consumed (TECH-124).

## 5. Proposed Design

### 5.1 Target behavior

Three filled polygons drawn: top rhombus (bright), south parallelogram (mid), east parallelogram (dark). Width `w` runs along SE diagonal; depth `d` runs along SW diagonal; height `h` pixels vertical.

### 5.2 Architecture

Pure function; mutates passed `canvas` via `ImageDraw.Draw(canvas).polygon(...)`. Material tuple destructured into per-face shade tuples (stub: bright / mid / dark bands around base RGB). Palette integration deferred.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | NW-light hardcoded for v1. | Exploration §5 locks NW as canonical; avoids configurable shade matrix complexity. | Arg-driven light direction. |

## 7. Implementation Plan

### Phase 1 — primitive

- [ ] Project `(w,d,h)` corners to pixel coords using 2:1 iso basis.
- [ ] Build three polygon vertex lists (top, south, east).
- [ ] Fill w/ stub bright/mid/dark derived from `material` tuple.
- [ ] Docstring cites §5 Primitives.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Three faces render non-transparent pixels | pytest (manual) | `pytest tools/sprite-gen/tests/test_primitives.py` | Wired in T1.1.6 (TECH-128). |
| Repo IA gates green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `iso_cube(canvas, x0, y0, w, d, h, material)` signature matches Stage 1.1 Exit.
- [ ] Three faces filled w/ distinct bright/mid/dark ramp.
- [ ] Coords derived from 2:1 iso projection (not hardcoded).
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
