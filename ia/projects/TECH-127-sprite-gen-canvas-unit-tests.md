---
purpose: "TECH-127 — Sprite gen canvas unit tests."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-127 — Sprite gen canvas unit tests

> **Issue:** [TECH-127](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Pytest coverage for `src/canvas.py` — asserts `canvas_size` + `pivot_uv` match exploration §4 Examples table exactly. First test file under `tools/sprite-gen/tests/`. Manual gate until CI integration (candidate fold-in point: Stage 1.3 palette tests).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `tests/test_canvas.py` covers all six example rows from exploration §4.
2. `pytest tools/sprite-gen/tests/test_canvas.py` exits 0.
3. Tests self-documenting — each assert names the case (1×1, 1×1 w/ 32 extra, 3×3 w/ 96 extra, pivot 64/128/192).

### 2.2 Non-Goals

1. No primitive tests — separate task (TECH-128).
2. No CI wiring — manual pytest gate.
3. No property-based / fuzz tests.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run pytest; see green bar confirming canvas math correctness. | All 6 asserts pass. |

## 4. Current State

### 4.1 Domain behavior

No tests. Canvas math lands in TECH-124.

### 4.2 Systems map

- `docs/isometric-sprite-generator-exploration.md` §4 Canvas math — examples table is test oracle.
- `tools/sprite-gen/src/canvas.py` — under test (TECH-124).
- `tools/sprite-gen/tests/test_canvas.py` — new.

## 5. Proposed Design

### 5.1 Target behavior

```
assert canvas_size(1,1) == (64, 0)
assert canvas_size(1,1,32) == (64, 32)
assert canvas_size(3,3,96) == (192, 96)
assert pivot_uv(64) == (0.5, 0.25)
assert pivot_uv(128) == (0.5, 0.125)
assert pivot_uv(192) == (0.5, 16/192)
```

### 5.2 Architecture

Plain pytest functions; no fixtures needed. Import from `src.canvas`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | Plain asserts vs parametrize. | Six cases; readability wins over DRY. | `@pytest.mark.parametrize`. |

## 7. Implementation Plan

### Phase 1 — tests

- [ ] Add `tests/test_canvas.py` w/ six asserts.
- [ ] Confirm pytest discovers + passes.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Canvas math asserts pass | pytest (manual) | `pytest tools/sprite-gen/tests/test_canvas.py` | Manual until CI fold-in. |
| Repo IA gates green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `test_canvas.py` present w/ six asserts.
- [ ] `pytest` exits 0.
- [ ] Covers all §4 Examples rows.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
