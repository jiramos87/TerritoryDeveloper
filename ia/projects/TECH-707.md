---
purpose: "TECH-707 — Parametrized test_signature_calibration.py + retire test_scale_calibration."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.2.4
---
# TECH-707 — `tests/test_signature_calibration.py` parametrized + retire `test_scale_calibration.py`

> **Issue:** [TECH-707](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Author `tools/sprite-gen/tests/test_signature_calibration.py` — parametrized over every `signatures/*.signature.json`. For each class, render the canonical spec and assert `validate_against(signature, rendered_img).ok is True`. Once `residential_small.signature.json` lands (TECH-705) + the parametrized case is green, retire `tests/test_scale_calibration.py` (TECH-702's tight bounds are now absorbed into the signature envelope).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Glob `signatures/*.signature.json`; parametrize one pytest case per signature.
2. Each case: `validate_against(sig, compose_sprite(load_spec(<class canonical spec>))).ok is True`.
3. `tests/test_scale_calibration.py` deleted (or reduced to `pytest.mark.skip("superseded by test_signature_calibration")`).
4. Full suite stays at or above prior test count.

### 2.2 Non-Goals

1. Inventing new signature classes — only `residential_small` lands in 6.2; future classes auto-pick-up.
2. Asserting bit-exact palette — envelope tolerances live in the signature itself.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Add a new class signature and get calibration coverage for free | New `signatures/<class>.signature.json` auto-picked up by parametrize |

## 4. Current State

### 4.1 Domain behavior

`tests/test_scale_calibration.py` hand-codes bbox bounds for `residential_small`. Works but doesn't extend.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_signature_calibration.py` (new).
- `tools/sprite-gen/tests/test_scale_calibration.py` (retire).
- Upstream deps: `src/signature.py::validate_against` (TECH-704); `signatures/residential_small.signature.json` (TECH-705).

### 4.3 Implementation investigation notes

Canonical-spec lookup per class needs a simple mapping. **Decision:** naming convention `specs/building_<class>.yaml` (so `residential_small` → `specs/building_residential_small.yaml`). Document it in the test file; future classes must follow.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ cd tools/sprite-gen && python3 -m pytest tests/test_signature_calibration.py -q
test_signature_calibration.py::test_class_calibration[residential_small] PASSED
1 passed in 0.34s
```

### 5.2 Architecture / implementation

- Helper `_signature_paths()` globs `signatures/*.signature.json` (excludes `_fallback.json`).
- Parametrize test receives `sig_path`, loads JSON, looks up `specs/building_<class>.yaml`, renders, validates.
- Assert `report.ok`; on failure, dump `report.failures` in the assertion message.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Canonical spec path convention `specs/building_<class>.yaml` | Deterministic; no per-class registry | Explicit map in test — rejected, extra edit point per class |
| 2026-04-23 | Delete `test_scale_calibration.py` outright | Envelope now lives in signature JSON; duplicate coverage is churn | `pytest.mark.skip` stub — acceptable if deletion is controversial |

## 7. Implementation Plan

### Phase 1 — Author parametrized test

- [ ] `_signature_paths()` helper globbing `signatures/*.signature.json` excluding `_fallback.json`.
- [ ] `test_class_calibration(sig_path)` parametrized with IDs derived from class name.
- [ ] Assert `validate_against(...)` returns `.ok is True`.

### Phase 2 — Retire `test_scale_calibration.py`

- [ ] Delete file (or replace with `pytest.mark.skip` stub).
- [ ] Confirm full suite still green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Per-signature calibration | Python | `cd tools/sprite-gen && python3 -m pytest tests/test_signature_calibration.py -q` | Expect ≥1 parametrized case |
| Full suite green | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ minus `test_scale_calibration` count + new parametrized |

## 8. Acceptance Criteria

- [ ] `test_class_calibration[residential_small]` green.
- [ ] `tests/test_scale_calibration.py` retired.
- [ ] Full suite exits 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Parametrize over committed JSON artefacts — new calibration surfaces inherit coverage without new test functions.

## §Plan Digest

### §Goal

Replace `tests/test_scale_calibration.py` with a signature-driven parametrized calibration that auto-covers every class whose signature JSON lands in `tools/sprite-gen/signatures/`.

### §Acceptance

- [ ] `_signature_paths()` helper returns ≥1 path today (`residential_small.signature.json`)
- [ ] `test_class_calibration[residential_small]` asserts `validate_against(...).ok is True`
- [ ] `tests/test_scale_calibration.py` deleted (or `pytest.mark.skip` stub)
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/test_signature_calibration.py -q` → green
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_class_calibration[residential_small] | `signatures/residential_small.signature.json` + `specs/building_residential_small.yaml` | `validate_against(...).ok is True` | parametrized |
| full_suite | all tests | exits 0 | `cd tools/sprite-gen && python3 -m pytest tests/ -q` |

### §Examples

```python
# tools/sprite-gen/tests/test_signature_calibration.py
from pathlib import Path
import json
import pytest

from src.compose import compose_sprite
from src.spec import load_spec
from src.signature import validate_against

_TOOL_ROOT = Path(__file__).resolve().parents[1]
_SIGNATURES_DIR = _TOOL_ROOT / "signatures"
_SPECS_DIR = _TOOL_ROOT / "specs"


def _signature_paths() -> list[Path]:
    """Every committed `<class>.signature.json` (excludes `_fallback.json`)."""
    return sorted(
        p for p in _SIGNATURES_DIR.glob("*.signature.json")
        if p.name != "_fallback.json"
    )


@pytest.mark.parametrize(
    "sig_path",
    _signature_paths(),
    ids=lambda p: p.stem.replace(".signature", ""),
)
def test_class_calibration(sig_path: Path) -> None:
    """Every committed signature validates against its canonical spec render."""
    signature = json.loads(sig_path.read_text())
    class_name = signature["class"]
    spec_path = _SPECS_DIR / f"building_{class_name}.yaml"
    rendered = compose_sprite(load_spec(spec_path))
    report = validate_against(signature, rendered)
    assert report.ok, f"{class_name} failed envelope: {report.failures}"
```

### §Mechanical Steps

#### Step 1 — Author parametrized test

**Goal:** Create `tests/test_signature_calibration.py` per §Examples.

**Edits:**

- `tools/sprite-gen/tests/test_signature_calibration.py` — new file.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature_calibration.py -q
```

**STOP:** `ImportError: src.signature` → confirm TECH-704 landed. `FileNotFoundError: signatures/residential_small.signature.json` → confirm TECH-705 landed.

#### Step 2 — Retire `test_scale_calibration.py`

**Goal:** Delete file.

**Edits:**

- Remove `tools/sprite-gen/tests/test_scale_calibration.py`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** New reds → were other tests importing `test_scale_calibration`? grep for references before deletion.

**MCP hints:** none — pure test swap.

## Open Questions (resolve before / during implementation)

1. If `residential_small.signature.json` lands in `mode: point-match` (only 1 sample in `Assets/Sprites/residential_small/`), does `validate_against` still behave correctly? **Resolution:** yes — point-match envelope is just `min == max == mean` per field; TECH-704 unit tests cover this branch.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
