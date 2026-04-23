---
purpose: "TECH-703 — Parametrized per-spec bbox regression in test_render_integration.py."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.1.3
---
# TECH-703 — Per-spec bbox regression in test_render_integration.py

> **Issue:** [TECH-703](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Add a parametrized pytest case to `test_render_integration.py` that iterates every live 1×1 `specs/*.yaml` (`building_residential_small` + `building_residential_light_{a,b,c}`) and asserts each renders with bbox exactly `(0, 15, 64, 48)`. Closes the hole where TECH-702 locks only the canonical spec; future specs under `specs/` silently drift.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Glob `tools/sprite-gen/specs/*.yaml`; filter to `footprint: [1, 1]` specs.
2. Parametrize one pytest case per live 1×1 spec.
3. Assert `compose_sprite(load_spec(path)).getbbox() == (0, 15, 64, 48)`.

### 2.2 Non-Goals (Out of Scope)

1. 2×2 / 3×3 spec coverage — no live specs of those footprints yet (Stage 9+ territory).
2. Color / HSV match assertions — live in TECH-699 (Stage 6) and out of scope here.
3. Re-running or changing existing integration tests (`test_render_integration_smoke` unchanged).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Add a new 1×1 spec and get bbox coverage for free | New `specs/*.yaml` with `footprint:[1,1]` auto-picked up by parametrize |

## 4. Current State

### 4.1 Domain behavior

`test_render_integration.py` currently runs one subprocess smoke (`python -m src render building_residential_small`). No coverage for the 3 `building_residential_light_{a,b,c}.yaml` review sprites added during the 2026-04-23 session.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_render_integration.py` — target file.
- `tools/sprite-gen/specs/*.yaml` — glob source (4 live 1×1 specs today).
- `tools/sprite-gen/src/spec.py::load_spec` + `src/compose.py::compose_sprite` — render path used by the new test.

### 4.3 Implementation investigation notes (optional)

`building_residential_small_N.yaml` is a *slope* variant (`terrain: N`); excluded from the 1×1-flat envelope because its bbox grows by slope-lift. Filter by `spec.get("terrain") in (None, "flat")` to be safe.

## 5. Proposed Design

### 5.1 Target behavior (product)

Running `pytest tools/sprite-gen/tests/test_render_integration.py -q` reports ≥4 parametrized cases (`test_every_live_1x1_spec_bbox[…]`), each passing with `bbox == (0, 15, 64, 48)`.

### 5.2 Architecture / implementation

- Helper `_live_1x1_flat_specs()` globs `specs/*.yaml`, loads each via `load_spec`, filters `footprint == [1,1]` AND `terrain in (None, "flat")`. Returns sorted list of spec paths.
- Parametrized test receives `spec_path`, renders via `compose_sprite(load_spec(spec_path))`, asserts `bbox == (0, 15, 64, 48)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | Parametrize over `pytest.mark.parametrize` with IDs derived from spec stem | Stable CI output; new specs auto-covered | Looped assertions inside one test function — rejected, single failure masks rest |
| 2026-04-23 | Filter out sloped / non-1×1 specs inside helper | Stage 6.1 scope is flat 1×1 only per handoff acceptance criteria | Assert universally — rejected, slope variants have different bbox |

## 7. Implementation Plan

### Phase 1 — Parametrized regression

- [ ] Add `_live_1x1_flat_specs()` helper to `tests/test_render_integration.py`.
- [ ] Add `test_every_live_1x1_spec_bbox` parametrized with that helper.
- [ ] Assert `bbox == (0, 15, 64, 48)` per case.
- [ ] Run `pytest tools/sprite-gen/tests/test_render_integration.py -q` — expect ≥4 cases pass.
- [ ] Run full suite to confirm no fallout.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Per-spec bbox pass | Python | `cd tools/sprite-gen && python3 -m pytest tests/test_render_integration.py -q` | Expect ≥4 parametrized cases green |
| Full suite green | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | Expect 221+ passed (218 baseline + 4 new) |

## 8. Acceptance Criteria

- [ ] `_live_1x1_flat_specs()` helper globs `specs/*.yaml` and filters to 1×1 flat.
- [ ] `test_every_live_1x1_spec_bbox` parametrized with the helper.
- [ ] Each case asserts `bbox == (0, 15, 64, 48)`.
- [ ] `pytest tools/sprite-gen/tests/` exits 0; 221+ tests green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Parametrize regression tests over `specs/*.yaml` so new specs inherit coverage automatically; avoids the silent-drift pattern where each new YAML needs a hand-written test.

## §Plan Digest

### §Goal

Close I2: every live flat 1×1 spec in `tools/sprite-gen/specs/` must render with bbox `(0, 15, 64, 48)`; assert via pytest parametrize so new specs get coverage for free.

### §Acceptance

- [ ] `_live_1x1_flat_specs()` helper returns ≥4 spec paths today (`building_residential_small` + `building_residential_light_{a,b,c}`)
- [ ] `test_every_live_1x1_spec_bbox[…]` runs one case per spec and asserts `bbox == (0, 15, 64, 48)`
- [ ] Sloped specs (`building_residential_small_N.yaml`) excluded by helper filter
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/test_render_integration.py -q` → ≥4 new parametrized cases pass
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_every_live_1x1_spec_bbox[building_residential_small] | spec path | `bbox == (0, 15, 64, 48)` | parametrized |
| test_every_live_1x1_spec_bbox[building_residential_light_a] | spec path | `bbox == (0, 15, 64, 48)` | parametrized |
| test_every_live_1x1_spec_bbox[building_residential_light_b] | spec path | `bbox == (0, 15, 64, 48)` | parametrized |
| test_every_live_1x1_spec_bbox[building_residential_light_c] | spec path | `bbox == (0, 15, 64, 48)` | parametrized |
| full_suite | all tests | 221+ passed | `cd tools/sprite-gen && python3 -m pytest tests/ -q` |

### §Examples

Target code block:

```python
# ---------------------------------------------------------------------------
# Stage 6.1 T6.1.3: per-spec bbox regression (closes I2)
# ---------------------------------------------------------------------------

from src.compose import compose_sprite
from src.spec import load_spec

_SPECS_DIR = _TOOL_ROOT / "specs"


def _live_1x1_flat_specs() -> list[Path]:
    """Live `specs/*.yaml` filtered to 1×1 flat footprint (sloped excluded)."""
    out: list[Path] = []
    for path in sorted(_SPECS_DIR.glob("*.yaml")):
        spec = load_spec(path)
        if spec.get("footprint") != [1, 1]:
            continue
        if spec.get("terrain") not in (None, "flat"):
            continue
        out.append(path)
    return out


@pytest.mark.parametrize(
    "spec_path",
    _live_1x1_flat_specs(),
    ids=lambda p: p.stem,
)
def test_every_live_1x1_spec_bbox(spec_path: Path) -> None:
    """DAS §2.3: every live 1×1 flat spec renders with bbox (0, 15, 64, 48)."""
    rendered = compose_sprite(load_spec(spec_path))
    box = rendered.getbbox()
    assert box == (0, 15, 64, 48), f"{spec_path.stem}: bbox={box}"
```

### §Mechanical Steps

#### Step 1 — Add helper + parametrized test

**Goal:** Append §Examples block to `tests/test_render_integration.py`.

**Edits:**

- `tools/sprite-gen/tests/test_render_integration.py` — append §Examples code at end of file (after existing `test_render_integration_smoke`). Ensure `from pathlib import Path` already imported (present in current file).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_render_integration.py -q
```

**STOP:** If any parametrized case fails → investigate whether the failing spec genuinely drifted or the filter missed a non-flat entry. Append finding to §9 Issues Found + this §Plan Digest before widening or narrowing assertions.

#### Step 2 — Full suite regression

**Goal:** Confirm 218 baseline + 4 new cases = 222+ passed.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Any red outside the new parametrized block → revert + diagnose.

**MCP hints:** none — pure test addition.

## Open Questions (resolve before / during implementation)

1. None — DAS §2.3 envelope is authoritative and the glob filter is deterministic.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
