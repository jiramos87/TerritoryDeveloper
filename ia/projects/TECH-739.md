---
purpose: "TECH-739 — tests/test_animation_reservation.py: reserved block, enabled:true raises, animate:none renders, animate:flicker raises."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.7.3
---
# TECH-739 — Tests — `test_animation_reservation.py`

> **Issue:** [TECH-739](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

One test file locking the Stage 6.7 animation-reservation contract end-to-end: (a) reserved `output.animation:` block with `enabled: false` parses; (b) `enabled: true` raises `SpecError`; (c) primitive with `animate: none` renders; (d) primitive with any other `animate:` value raises `NotImplementedError` with "DAS §12" in the message.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Four named test cases present and green.
2. Error-path tests assert message content, not only exception type.
3. `pytest tools/sprite-gen/tests/ -q` green overall (no regressions).

### 2.2 Non-Goals

1. Implementing the loader / guard — TECH-737/738.
2. Testing actual frame rendering — deferred.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Repo guardian | Prevent reservation regressions | 4 tests green |
| 2 | Code reviewer | Read one file to see the whole contract | All 4 cases locally runnable |
| 3 | Future animation dev | Know where reservation seams live | Error messages reference DAS §12 |

## 4. Current State

### 4.1 Domain behavior

No animation-reservation test file yet.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_animation_reservation.py` — new file.
- Exercises: `src/spec.py` (TECH-737), `src/compose.py` (TECH-738).

### 4.3 Implementation investigation notes

Error-content assertions matter more than type here — a `NotImplementedError` without a DAS pointer reads as a bug rather than a reservation.

## 5. Proposed Design

### 5.1 Target behavior

Four tests:

1. `test_reserved_block_parses` — `enabled: false` block + siblings parses clean.
2. `test_enabled_true_raises` — `enabled: true` raises `SpecError` mentioning DAS §12.
3. `test_animate_none_renders` — primitive with `animate: none` renders without errors.
4. `test_animate_value_raises` — primitive with `animate: flicker` raises `NotImplementedError` with "DAS §12".

### 5.2 Architecture / implementation

- pytest.
- Renders via existing `render(spec, path)` harness.
- Uses `pytest.raises(..., match=...)` for message assertions.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Assert exception message content | Loose type-only assertions pass even if reservation wiring degrades | Type-only — rejected, silent drift |
| 2026-04-23 | Minimal 4-test surface | Reservation is tiny; more tests would inflate without value | Parametrize over many animate values — rejected, noise |

## 7. Implementation Plan

### Phase 1 — Spec-block parse + raise tests

### Phase 2 — Primitive animate no-op + raise tests

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| 4 tests green | Python | `pytest tools/sprite-gen/tests/test_animation_reservation.py -q` | — |
| Suite green | Python | `pytest tools/sprite-gen/tests/ -q` | No regressions |
| Message content | Python | `pytest -q -k "raise"` | Match "DAS §12" |

## 8. Acceptance Criteria

- [ ] Four named test cases present + green.
- [ ] Error-path tests assert message content, not only type.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Tiny regressions gates (four tests) still prevent slow drift on reserved schemas — reservation without a test turns into "silent allow" within a few merges.

## §Plan Digest

### §Goal

Lock the animation-reservation contract under four explicit tests so reserved-block acceptance and primitive-guard raises can't silently drift between merges.

### §Acceptance

- [ ] `tools/sprite-gen/tests/test_animation_reservation.py` exists with 4 named tests
- [ ] All 4 tests exit green under `pytest -q`
- [ ] Error-path tests use `match=` or equivalent to assert "DAS §12" in message
- [ ] File imports from `src.spec` and `src.compose` (exercises both surfaces)

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_reserved_block_parses | spec with full `output.animation` block (enabled:false) | load_spec succeeds | pytest |
| test_enabled_true_raises | `enabled: true` | SpecError matching "DAS §12" | pytest |
| test_animate_none_renders | decoration with `animate: none` | render succeeds; non-empty output | pytest |
| test_animate_value_raises | decoration with `animate: flicker` | NotImplementedError matching "DAS §12" | pytest |

### §Examples

```python
# tools/sprite-gen/tests/test_animation_reservation.py
import pytest
from src.spec import load_spec_from_dict, SpecError
from src.compose import compose_sprite

def test_reserved_block_parses():
    spec = load_spec_from_dict({
        "id": "t01",
        "output": {
            "name": "t01.png",
            "animation": {
                "enabled": False,
                "frames": 4,
                "fps": 8,
                "loop": True,
                "phase_offset": 0,
                "layers": ["smoke"],
            },
        },
    })
    assert spec["output"]["animation"]["enabled"] is False

def test_enabled_true_raises():
    with pytest.raises(SpecError, match=r"DAS §12"):
        load_spec_from_dict({
            "id": "t02",
            "output": {"name": "t02.png", "animation": {"enabled": True}},
        })

def test_animate_none_renders(tmp_path):
    spec = load_spec_from_dict({
        "id": "t03", "output": {"name": "t03.png"},
        "decorations": [{"type": "iso_tree_fir", "x_px": 10, "y_px": 20, "animate": "none"}],
    })
    out = tmp_path / "t03.png"
    compose_sprite(spec, out)
    assert out.exists() and out.stat().st_size > 0

def test_animate_value_raises(tmp_path):
    spec = load_spec_from_dict({
        "id": "t04", "output": {"name": "t04.png"},
        "decorations": [{"type": "iso_tree_fir", "x_px": 10, "y_px": 20, "animate": "flicker"}],
    })
    with pytest.raises(NotImplementedError, match=r"DAS §12"):
        compose_sprite(spec, tmp_path / "t04.png")
```

### §Mechanical Steps

#### Step 1 — Spec-block tests

**Edits:**

- `tools/sprite-gen/tests/test_animation_reservation.py` — new file; `test_reserved_block_parses`, `test_enabled_true_raises`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_animation_reservation.py -q -k "reserved_block or enabled_true"
```

#### Step 2 — Primitive animate tests

**Edits:**

- Same file — `test_animate_none_renders`, `test_animate_value_raises`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_animation_reservation.py -q
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Is `load_spec_from_dict` exported today? **Resolution:** add thin wrapper if only `load_spec(path)` exists; keeps tests readable without tmp yaml files.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
