---
purpose: "TECH-128 — Sprite gen primitive smoke tests."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-128 — Sprite gen primitive smoke tests

> **Issue:** [TECH-128](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Pytest smoke coverage for `iso_cube` + `iso_prism`. Renders each primitive to a stub canvas, asserts non-transparent pixel count > 0 on all three visible faces (top/south/east for cube; sloped tops + gable ends for prism). Saves fixture PNGs to `tests/fixtures/` for visual regression reference. Closes Stage 1.1.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `tests/test_primitives.py` smoke-tests `iso_cube(w=2,d=2,h=32,...)` to 64×64 canvas.
2. Smoke-tests `iso_prism` both axes.
3. Asserts non-transparent pixel count > 0 on each visible face.
4. Emits fixture PNGs to `tests/fixtures/` for eyeball regression.

### 2.2 Non-Goals

1. No pixel-exact comparison — smoke level only.
2. No property-based tests, no fuzz.
3. No composer-level tests — Stage 1.2.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run pytest; confirm primitives draw; eyeball fixture PNGs. | Smoke asserts pass; fixtures written. |

## 4. Current State

### 4.1 Domain behavior

No tests. Primitives land in TECH-125 + TECH-126.

### 4.2 Systems map

- `tools/sprite-gen/src/primitives/iso_cube.py` — under test (TECH-125).
- `tools/sprite-gen/src/primitives/iso_prism.py` — under test (TECH-126).
- `tools/sprite-gen/tests/test_primitives.py` — new.
- `tools/sprite-gen/tests/fixtures/` — regression PNGs.

## 5. Proposed Design

### 5.1 Target behavior

```
def test_iso_cube_draws_three_faces():
    canvas = Image.new('RGBA', (64, 64), (0,0,0,0))
    iso_cube(canvas, 0, 0, 2, 2, 32, stub_bright_red)
    # partition canvas into expected face regions; assert non-zero alpha per region
    canvas.save('tests/fixtures/iso_cube_smoke.png')
```

### 5.2 Architecture

Pytest w/ PIL. Face-region partitioning derived from 2:1 iso geometry (rough bounding boxes per face). Fixture write unconditional (gitignored? decide at implement).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | Track fixture PNGs (not gitignore). | Visual regression reference; tiny binary. | Gitignore + regenerate on demand. |

## 7. Implementation Plan

### Phase 1 — tests

- [ ] Write `test_iso_cube_draws_three_faces` + `test_iso_prism_*` (ns, ew).
- [ ] Partition canvas into face regions; assert non-zero alpha per region.
- [ ] Save fixture PNGs.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Primitive smoke asserts pass | pytest (manual) | `pytest tools/sprite-gen/tests/test_primitives.py` | Manual until CI fold-in. |
| Fixture PNGs written | pytest artifact | `tools/sprite-gen/tests/fixtures/iso_*.png` | Visual eyeball regression. |
| Repo IA gates green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `test_primitives.py` present w/ cube + prism smoke tests.
- [ ] `pytest` exits 0.
- [ ] Fixture PNGs emitted.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
