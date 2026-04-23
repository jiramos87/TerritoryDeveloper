---
purpose: "TECH-747 — tests/test_ground_passthrough.py: flag parse, non-bool raise, noise-skip diff, jitter clamp, default unchanged."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T7.10.3
---
# TECH-747 — Tests — `test_ground_passthrough.py`

> **Issue:** [TECH-747](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

One test file locking the passthrough contract end-to-end: (a) flag parses; (b) non-bool raises `SpecError`; (c) passthrough=true render skips noise (bounded byte diff vs. baseline); (d) `hue_jitter` clamp enforced even if author sets higher; (e) passthrough=false render byte-identical to pre-addendum baseline.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Five named tests present and green.
2. Visual-diff tests use bounded byte-count difference (not exact pixel equality).
3. Default-false path byte-identical to pre-addendum baseline.
4. `pytest tools/sprite-gen/tests/ -q` green.

### 2.2 Non-Goals

1. Implementing schema flag — TECH-745.
2. Implementing composer branch — TECH-746.
3. DAS amendment — TECH-748.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Repo guardian | Prevent passthrough regressions | 5 tests green |
| 2 | Reviewer | See whole contract in one file | All 5 cases locally runnable |
| 3 | Future composer dev | Trust default-unchanged invariant | `passthrough=false` byte-stable |

## 4. Current State

### 4.1 Domain behavior

No passthrough tests yet.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_ground_passthrough.py` — new file.
- Exercises: `src/spec.py` (TECH-745), `src/compose.py` (TECH-746).

### 4.3 Implementation investigation notes

"Byte-identical to baseline" needs a stable baseline — capture hash of current ground render for the chosen test spec before landing TECH-746, then assert passthrough=false matches it.

## 5. Proposed Design

### 5.1 Target behavior

Five tests:

1. `test_passthrough_flag_parses` — `passthrough: true` parses.
2. `test_passthrough_non_bool_raises` — `passthrough: "yes"` raises.
3. `test_passthrough_skips_noise` — render diff vs. baseline nonzero-but-bounded.
4. `test_passthrough_clamps_jitter` — author `hue_jitter: 0.1` + passthrough → effective jitter ≤ 0.01.
5. `test_default_byte_identical` — `passthrough: false` (default) matches baseline hash.

### 5.2 Architecture / implementation

- pytest; `hashlib.sha256` on PNG bytes for baseline match.
- Bounded-diff test: assert byte-diff count >0 (noise inhibited changes something) and <threshold (not wildly different).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Byte-hash baseline | Strongest regression guarantee | Perceptual diff — rejected, slower + fuzzier |
| 2026-04-23 | Bounded-diff noise-skip test | Exact pixel equality unrealistic given noise randomness | Exact pixels — rejected, brittle |

## 7. Implementation Plan

### Phase 1 — Flag parse / raise tests

### Phase 2 — Render skip-noise test

### Phase 3 — Jitter clamp + default-unchanged tests

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| 5 tests green | Python | `pytest tools/sprite-gen/tests/test_ground_passthrough.py -q` | — |
| Full suite green | Python | `pytest tools/sprite-gen/tests/ -q` | — |

## 8. Acceptance Criteria

- [ ] Five named tests present + green.
- [ ] Visual-diff tests use bounded byte-count difference (not exact pixel).
- [ ] Default-false path byte-identical to pre-addendum baseline.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Byte-hash baselines plus bounded-diff tests give two complementary safety nets — the hash locks the no-change path, the bounded diff locks the change path.

## §Plan Digest

### §Goal

Lock the passthrough contract under a single test file so future composer changes can't silently regress the noise-skip or jitter-clamp behaviour.

### §Acceptance

- [ ] `tools/sprite-gen/tests/test_ground_passthrough.py` exists with 5 named tests
- [ ] All 5 tests exit green under `pytest -q`
- [ ] Default-false test asserts SHA256 byte-equality with pre-addendum baseline
- [ ] Passthrough-true test asserts bounded byte-diff (nonzero AND < threshold)
- [ ] Jitter-clamp test asserts effective jitter ≤ 0.01 regardless of author input

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_passthrough_flag_parses | `ground.passthrough: true` | resolved spec has `passthrough == True` | pytest |
| test_passthrough_non_bool_raises | `passthrough: "yes"` | SpecError | pytest |
| test_passthrough_skips_noise | render with passthrough=true vs. baseline | byte-diff count > 0 and < threshold | pytest |
| test_passthrough_clamps_jitter | author `hue_jitter: 0.1` + passthrough | effective jitter ≤0.01 (inspect via seed + render hash) | pytest |
| test_default_byte_identical | `passthrough: false` (default) | SHA256 matches baseline | pytest |

### §Examples

```python
# tools/sprite-gen/tests/test_ground_passthrough.py
import hashlib
import pytest
from pathlib import Path
from src.spec import load_spec_from_dict, SpecError
from src.compose import compose_sprite

_BASELINE_SHA = "<captured before TECH-746 lands>"

def _sha(p: Path) -> str:
    return hashlib.sha256(p.read_bytes()).hexdigest()

def test_passthrough_flag_parses():
    spec = load_spec_from_dict({
        "id": "t01", "output": {"name": "t01.png"},
        "ground": {"material": "grass", "passthrough": True},
    })
    assert spec["ground"]["passthrough"] is True

def test_passthrough_non_bool_raises():
    with pytest.raises(SpecError):
        load_spec_from_dict({
            "id": "t02", "output": {"name": "t02.png"},
            "ground": {"material": "grass", "passthrough": "yes"},
        })

def test_default_byte_identical(tmp_path):
    spec = load_spec_from_dict({
        "id": "t03", "output": {"name": "t03.png"},
        "ground": {"material": "grass"},
    })
    out = tmp_path / "t03.png"
    compose_sprite(spec, out, seed=42)
    assert _sha(out) == _BASELINE_SHA

def test_passthrough_skips_noise(tmp_path):
    spec_pt = load_spec_from_dict({
        "id": "t04", "output": {"name": "t04.png"},
        "ground": {"material": "grass", "passthrough": True},
    })
    spec_base = load_spec_from_dict({
        "id": "t05", "output": {"name": "t05.png"},
        "ground": {"material": "grass"},
    })
    a = tmp_path / "t04.png"; b = tmp_path / "t05.png"
    compose_sprite(spec_pt, a, seed=42)
    compose_sprite(spec_base, b, seed=42)
    diff = sum(x != y for x, y in zip(a.read_bytes(), b.read_bytes()))
    assert 0 < diff < 2000  # bounded

def test_passthrough_clamps_jitter(tmp_path):
    spec_high = load_spec_from_dict({
        "id": "t06", "output": {"name": "t06.png"},
        "ground": {"material": "grass", "passthrough": True, "hue_jitter": 0.1},
    })
    spec_low = load_spec_from_dict({
        "id": "t07", "output": {"name": "t07.png"},
        "ground": {"material": "grass", "passthrough": True, "hue_jitter": 0.01},
    })
    a = tmp_path / "t06.png"; b = tmp_path / "t07.png"
    compose_sprite(spec_high, a, seed=42)
    compose_sprite(spec_low, b, seed=42)
    # Effective jitter is clamped → both renders should be identical
    assert _sha(a) == _sha(b)
```

### §Mechanical Steps

#### Step 1 — Flag parse / raise tests

**Edits:**

- `tools/sprite-gen/tests/test_ground_passthrough.py` — new file; `test_passthrough_flag_parses`, `test_passthrough_non_bool_raises`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_passthrough.py -q -k "flag_parses or non_bool_raises"
```

#### Step 2 — Render skip-noise + default-identical tests

**Edits:**

- Same file — `test_passthrough_skips_noise`, `test_default_byte_identical`. Capture `_BASELINE_SHA` from a clean non-passthrough render.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_passthrough.py -q -k "skips_noise or default_byte_identical"
```

#### Step 3 — Jitter clamp test

**Edits:**

- Same file — `test_passthrough_clamps_jitter`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_passthrough.py -q
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. How do we compute a stable baseline SHA before TECH-746 lands? **Resolution:** run the test's non-passthrough spec through the pre-addendum composer with seed=42; record the hash in a fixture. Update after each stage merge that intentionally changes ground rendering.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
