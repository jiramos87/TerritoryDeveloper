---
purpose: "TECH-735 — tests/test_preset_system.py: override, vary preservation, wipe raises, determinism, missing preset."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.6.6
---
# TECH-735 — Tests — `test_preset_system.py`

> **Issue:** [TECH-735](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

One test file locking preset loader + merge rule + seeded presets end-to-end. Five named tests cover (a) author field wins merge, (b) author `vary.padding` doesn't erase preset `vary.roof`, (c) author `vary: null` raises, (d) preset-referenced-twice determinism, (e) missing preset → `SpecError` with valid list. Uses each seeded preset at least once.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Five named test cases present and green.
2. Uses each of the three seeded presets at least once.
3. Determinism: same preset + same seed → byte-identical output.
4. `pytest tools/sprite-gen/tests/ -q` exits 0.

### 2.2 Non-Goals

1. Implementing the merge rule — TECH-730/731.
2. Covering Stage 9's `tiled-row-3` slot semantics — TECH-744.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Prevent preset regressions on merge | 5 tests green |
| 2 | Code reviewer | Read one file to see whole contract | All 5 cases locally runnable |
| 3 | Future spec author | Trust preset determinism | Determinism test enforces byte-equal output |

## 4. Current State

### 4.1 Domain behavior

No preset-system test file yet.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_preset_system.py` — new file.
- Consumers / exercised: `src/spec.py` (TECH-730/731), `presets/*.yaml` (TECH-732/733/734).

### 4.3 Implementation investigation notes

Determinism test wants a fixed seed + two renders of the same preset — compare via `hashlib.sha256` on the PNG bytes. For TECH-734 (row_houses_3x), gate the render-assertion behind a `pytest.mark.skipif(not stage_9_addendum_available, ...)` so the test file is useful pre-Stage-9 merge; assertion on `_load_preset` shape can run unconditionally.

## 5. Proposed Design

### 5.1 Target behavior

Five tests:

1. `test_author_field_wins` — author `output.name` overrides preset.
2. `test_author_vary_padding_preserves_preset_vary_roof` — union merge lock.
3. `test_author_vary_null_raises` — wipe-guard.
4. `test_preset_twice_same_seed_deterministic` — SHA256 byte-equal.
5. `test_missing_preset_raises_with_valid_list` — `SpecError` msg lists all presets.

### 5.2 Architecture / implementation

- pytest; fixtures: `tmp_path`, `seed=42`.
- Renders via `render.render(spec, out_path)` existing harness.
- SHA256 on file bytes for determinism.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Determinism via byte-hash | Strongest possible guarantee | PIL-pixel-diff — rejected, slower + weaker |
| 2026-04-23 | Gate row_houses_3x render behind TECH-744 | File useful pre-Stage-9 merge | Skip whole test — rejected, removes scaffolding value |

## 7. Implementation Plan

### Phase 1 — Override test

### Phase 2 — Vary preservation + wipe guard

### Phase 3 — Determinism + missing preset

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| 5 tests green | Python | `pytest tools/sprite-gen/tests/test_preset_system.py -q` | — |
| Full suite green | Python | `pytest tools/sprite-gen/tests/ -q` | No regressions |
| Each seeded preset used ≥1× | Static | grep for preset names in test file | — |

## 8. Acceptance Criteria

- [ ] Five named test cases present + green.
- [ ] Uses each seeded preset at least once.
- [ ] Determinism: same preset + same seed → byte-identical output.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- One test file per subsystem keeps regression discovery cheap — readers see the whole contract on one screen.

## §Plan Digest

### §Goal

Lock the preset system's loader, merge rule, seeded presets, and determinism under a single test file so Stage 6.6 regressions fail loudly at merge time.

### §Acceptance

- [ ] `tools/sprite-gen/tests/test_preset_system.py` exists with 5 named tests
- [ ] All 5 tests exit green under `pytest -q`
- [ ] Each seeded preset (`suburban_house_with_yard`, `strip_mall_with_parking`, `row_houses_3x`) is referenced by at least one test
- [ ] Determinism test uses SHA256 byte-equality, not perceptual diff
- [ ] Missing-preset error message includes every `.yaml` stem under `presets/`

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_author_field_wins | preset + author `output.name` override | resolved spec has author's `output.name` | pytest |
| test_author_vary_padding_preserves_preset_vary_roof | preset w/ `vary.roof` + author `vary.padding` | both axes present | pytest |
| test_author_vary_null_raises | author `vary: null` | `SpecError` raised | pytest |
| test_preset_twice_same_seed_deterministic | render preset 2× with seed=42 | SHA256 equal | pytest |
| test_missing_preset_raises_with_valid_list | `preset: ghost` | `SpecError`; msg contains all 3 seed preset names | pytest |

### §Examples

```python
# tools/sprite-gen/tests/test_preset_system.py
import hashlib
import pytest
from pathlib import Path
from src.spec import load_spec, SpecError
from src.render import render

def _sha256(p: Path) -> str:
    return hashlib.sha256(p.read_bytes()).hexdigest()

def test_author_field_wins(tmp_path):
    spec = load_spec_from_dict({
        "preset": "suburban_house_with_yard",
        "id": "t01",
        "output": {"name": "t01.png"},
    })
    assert spec["output"]["name"] == "t01.png"

def test_author_vary_padding_preserves_preset_vary_roof(tmp_path):
    spec = load_spec_from_dict({
        "preset": "suburban_house_with_yard",
        "vary": {"padding": {"values": [0, 1]}},
    })
    assert "roof" in spec["vary"]
    assert "padding" in spec["vary"]

def test_author_vary_null_raises():
    with pytest.raises(SpecError):
        load_spec_from_dict({"preset": "suburban_house_with_yard", "vary": None})

def test_preset_twice_same_seed_deterministic(tmp_path):
    spec = load_spec_from_dict({
        "preset": "strip_mall_with_parking",
        "id": "t02", "output": {"name": "t02.png"},
    })
    a = tmp_path / "a.png"; b = tmp_path / "b.png"
    render(spec, a, seed=42); render(spec, b, seed=42)
    assert _sha256(a) == _sha256(b)

def test_missing_preset_raises_with_valid_list():
    with pytest.raises(SpecError) as ei:
        load_spec_from_dict({"preset": "ghost"})
    msg = str(ei.value)
    for name in ("suburban_house_with_yard", "strip_mall_with_parking", "row_houses_3x"):
        assert name in msg
```

### §Mechanical Steps

#### Step 1 — Override test

**Edits:**

- `tools/sprite-gen/tests/test_preset_system.py` — new file; `test_author_field_wins`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py::test_author_field_wins -q
```

#### Step 2 — Vary preservation + wipe guard

**Edits:**

- Same file — `test_author_vary_padding_preserves_preset_vary_roof`, `test_author_vary_null_raises`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py -q -k vary
```

#### Step 3 — Determinism + missing preset

**Edits:**

- Same file — `test_preset_twice_same_seed_deterministic`, `test_missing_preset_raises_with_valid_list`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py -q
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does `render(spec, out, seed)` accept an explicit seed? **Resolution:** confirm from Stage 6.3 variant loop (TECH-711); if not, wrap in a seeded shim for the test.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
