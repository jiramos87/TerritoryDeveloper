---
purpose: "TECH-728 — Single pytest file locking the curation → aggregator → gate → sidecar loop with deterministic fixtures."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.5.6
---
# TECH-728 — Tests — test_curation_loop.py

> **Issue:** [TECH-728](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

One new test file `tools/sprite-gen/tests/test_curation_loop.py` covers the full Stage 6.5 loop: (a) envelope tightens toward promoted samples after N promotes (before/after fixture); (b) `vary.*` range shrinks in the direction of rejection reasons (before/after); (c) `.needs_review` sidecar flag set when floor not met in N tries. Deterministic seeds throughout.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Before/after envelope comparison after 5 promotes asserts tightening.
2. Before/after `vary.*` range after N rejects with a named reason asserts carve-out direction.
3. `.needs_review` sidecar presence on floor-miss / absence on floor-met.
4. Full suite `pytest tools/sprite-gen/tests/ -q` green.

### 2.2 Non-Goals

1. Curate CLI internals — those are TECH-723/724's own unit tests.
2. Gate internals — TECH-726's own unit tests.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Guard loop regression | Envelope tightening measurable in before/after |
| 2 | Sprite-gen dev | Guard carve-out direction | Reject-heavy fixture shifts bounds away from rejects |
| 3 | Curator | Trust sidecar gate | Sidecar presence test locks the contract |

## 4. Current State

### 4.1 Domain behavior

No Stage 6.5 loop tests — the loop doesn't exist yet. This file materialises alongside TECH-726/727.

### 4.2 Systems map

- `tools/sprite-gen/tests/test_curation_loop.py` (new).
- Deps: `src/curate.py` (TECH-723, 724), `src/signature.py::compute_envelope` (TECH-725), `src/compose.py` gate + sidecar (TECH-726, 727).

### 4.3 Implementation investigation notes

Before/after fixtures: use inline JSONL strings / seeded Python lists rather than filesystem fixtures so tests stay self-contained.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py -q
......                                                                    [100%]
6 passed in 0.7s
```

### 5.2 Architecture / implementation

- One module, 6 tests.
- Helpers: `_envelope_range(env, axis)` for compact range comparisons; `_build_spec()` for minimal fixture spec.
- Pixel comparisons not required — we test state flow, not rendered output.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Inline fixtures | Self-contained; no fs churn | External fixture dir — rejected |
| 2026-04-23 | One file per stage loop | Matches Stage 6.4 `test_ground_variation.py` pattern | Per-lock split — rejected, breaks net |

## 7. Implementation Plan

### Phase 1 — Before/after envelope test (union tightening)

### Phase 2 — Before/after `vary.*` carve-out test (rejection-zone direction)

### Phase 3 — `.needs_review` presence / absence tests

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Envelope tightens | Python | `pytest tests/test_curation_loop.py::test_envelope_tightens_after_promotes -q` | ≥1 axis narrowed |
| Carve-out direction | Python | `pytest tests/test_curation_loop.py::test_varies_shrink_toward_reason -q` | Floor/ceiling pushed away from rejects |
| Sidecar on exhaust | Python | `pytest tests/test_curation_loop.py::test_sidecar_on_exhaustion -q` | File exists, schema valid |
| No sidecar on pass | Python | `pytest tests/test_curation_loop.py::test_no_sidecar_on_pass -q` | File absent |
| Retry determinism | Python | `pytest tests/test_curation_loop.py::test_retry_trajectory_deterministic -q` | Two runs match |
| Full suite | Python | `pytest tools/sprite-gen/tests/ -q` | Nothing else regresses |

## 8. Acceptance Criteria

- [ ] Before/after envelope comparison after N promotes asserts tightening.
- [ ] Before/after `vary.*` range after N rejects shows carve-out toward reason.
- [ ] `.needs_review` sidecar presence asserted on floor-miss.
- [ ] Deterministic seeds throughout.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- State-flow tests (before/after) are the right regression shape when the feature is a feedback loop rather than a single transformation.

## §Plan Digest

### §Goal

One test file locks the full Stage 6.5 feedback loop end-to-end, with before/after fixtures for envelope tightening, carve-out direction, and sidecar presence/absence.

### §Acceptance

- [ ] `tools/sprite-gen/tests/test_curation_loop.py` exists with six named tests
- [ ] `test_envelope_tightens_after_promotes` — ≥1 axis range narrows after 5 promotes vs catalog-only baseline
- [ ] `test_varies_shrink_toward_reason` — after 5 `roof-too-shallow` rejects, `vary.roof.h_px.min` ≥ max rejected `h_px` + 1
- [ ] `test_sidecar_on_exhaustion` — sidecar present; JSON schema matches TECH-727
- [ ] `test_no_sidecar_on_pass` — sidecar absent
- [ ] `test_retry_trajectory_deterministic` — two runs same seeds → same attempted_seeds list
- [ ] `test_flag_off_byte_identical` — gate-off render byte-identical to baseline
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_envelope_tightens_after_promotes | catalog + 5 narrow-vary promoted rows | range < catalog range on ≥1 axis | pytest |
| test_varies_shrink_toward_reason | 5 `roof-too-shallow` rejects at `h_px=6` | `env["roof"]["h_px"]["min"] ≥ 7` | pytest |
| test_sidecar_on_exhaustion | spec + envelope impossible to satisfy | `<v>.needs_review.json` exists | pytest |
| test_no_sidecar_on_pass | spec + envelope trivially satisfied | no sidecar | pytest |
| test_retry_trajectory_deterministic | two runs same seeds | same `attempted_seeds` | pytest |
| test_flag_off_byte_identical | `envelope=None` | pre-change golden pixel-identical | pytest |

### §Examples

```python
# tools/sprite-gen/tests/test_curation_loop.py
import json
from pathlib import Path
import pytest
from src.signature import compute_envelope
from src.compose import render


def _envelope_range(env, axis_path):
    axis = env
    for part in axis_path.split("."):
        axis = axis[part]
    return axis["max"] - axis["min"]


def test_envelope_tightens_after_promotes():
    catalog = _catalog_fixture()
    promoted = [{"vary_values": {"roof": {"h_px": 10 + i % 2}}} for i in range(5)]
    before = compute_envelope(catalog, [], [])
    after = compute_envelope(catalog, promoted, [])
    assert _envelope_range(after, "roof.h_px") < _envelope_range(before, "roof.h_px")


def test_varies_shrink_toward_reason():
    catalog = _catalog_fixture()
    rejected = [{"vary_values": {"roof": {"h_px": 6}}, "reason": "roof-too-shallow"}
                for _ in range(5)]
    env = compute_envelope(catalog, [], rejected)
    assert env["roof"]["h_px"]["min"] >= 7


def test_sidecar_on_exhaustion(tmp_path):
    spec = _build_spec(tmp_path)
    env = _unsatisfiable_envelope()
    _consume(render(spec, envelope=env, retry_cap=5, palette_seed=42))
    sidecar = tmp_path / "variant.needs_review.json"
    assert sidecar.exists()
    data = json.loads(sidecar.read_text())
    assert data["schema_version"] == 1
    assert len(data["attempted_seeds"]) == 5
```

### §Mechanical Steps

#### Step 1 — Envelope tightening

**Edits:**

- `tools/sprite-gen/tests/test_curation_loop.py` — file skeleton + `test_envelope_tightens_after_promotes`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py::test_envelope_tightens_after_promotes -q
```

#### Step 2 — Carve-out direction

**Edits:**

- Same file — `test_varies_shrink_toward_reason`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py::test_varies_shrink_toward_reason -q
```

#### Step 3 — Sidecar + determinism + flag-off

**Edits:**

- Same file — four remaining tests.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py -q
```

#### Step 4 — Full-suite regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. `_catalog_fixture()` size — 1 or 3 catalog entries? **Resolution:** 3 entries for baseline envelope spread; 1 masks aggregation math under coincidence.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
