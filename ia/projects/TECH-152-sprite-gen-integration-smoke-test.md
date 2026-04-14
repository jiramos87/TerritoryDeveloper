---
purpose: "TECH-152 — Sprite-gen Stage 1.2 integration smoke test."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-152 — Sprite-gen Stage 1.2 integration smoke test

> **Issue:** [TECH-152](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Stage 1.2 closeout — end-to-end integration smoke. `pytest` runs `python -m sprite_gen render building_residential_small` via `subprocess.run`, asserts `out/building_residential_small_v01.png` exists, PIL opens the image, size == `(64, 64)`, 4 variant files written, no exception. Locks Layer 2 contract before Stage 1.3 palette work.

## 2. Goals and Non-Goals

### 2.1 Goals

1. pytest test calls CLI via subprocess — not in-process — to exercise `__main__.py` entry.
2. Assert 4 variant PNGs present.
3. Assert PIL opens + size == (64, 64).
4. Assert return code 0.
5. Use `tmp_path` or clean `out/` before run for deterministic state.

### 2.2 Non-Goals (Out of Scope)

1. Pixel-exact diff against golden fixture — overkill for smoke; defer to Stage 1.3 (palette determinism matters more once real colors land).
2. Multi-archetype batch — TECH-150 `--all` test covers.
3. Slope variants — Stage 1.4.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | As pipeline dev, I want end-to-end smoke covering CLI→loader→compose→PNG so that Stage 1.2 regressions catch in pytest | `pytest tools/sprite-gen/tests/test_render_integration.py` exits 0 |

## 4. Current State

### 4.1 Domain behavior

Stage 1.1 tests (TECH-127 / TECH-128) cover canvas math + primitive smoke in isolation. No end-to-end test exists — CLI → loader → compose → PNG chain uncovered.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_render_integration.py` (new).
- `tools/sprite-gen/src/__main__.py` (TECH-149) — subprocess target.
- `tools/sprite-gen/specs/building_residential_small.yaml` (TECH-151) — fixture.
- `tools/sprite-gen/out/` (gitignored) — output dir.

## 5. Proposed Design

### 5.1 Target behavior (product)

Test uses `subprocess.run(['python', '-m', 'sprite_gen', 'render', 'building_residential_small'], cwd='tools/sprite-gen', check=True, capture_output=True)`. Asserts return code 0. Globs `out/building_residential_small_v*.png` — expects 4 entries. Opens each w/ `PIL.Image.open`, asserts `.size == (64, 64)`. Cleans `out/` before test (fixture).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- pytest fixture cleans `out/` pre/post test.
- Subprocess `cwd=` repo `tools/sprite-gen/` so relative `specs/` + `out/` resolve.
- Skip marker if `specs/building_residential_small.yaml` missing (friendly message, not hard fail) — so TECH-152 can merge slightly ahead of TECH-151 in event of sequencing hiccup.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | Subprocess vs in-process | Covers `__main__` + argparse entry | In-process faster but misses CLI layer |

## 7. Implementation Plan

### Phase 1 — Smoke test

- [ ] Create `tests/test_render_integration.py`.
- [ ] Subprocess invocation + fixture glob + PIL open asserts.
- [ ] Out/ cleanup fixture.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| CLI → PNG chain works | Python | `pytest tools/sprite-gen/tests/test_render_integration.py` | Manual gate — Python still outside `validate:all` until Stage 1.3 CI fold-in |
| 4 variant PNGs size 64x64 | Python | (same test file) | |
| IA / validate chain | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `pytest tools/sprite-gen/tests/test_render_integration.py` exits 0.
- [ ] 4 PNGs present post-run + readable via PIL + size (64, 64).
- [ ] Subprocess return code 0.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.
