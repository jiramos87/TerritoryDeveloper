---
purpose: "TECH-725 — Three-source envelope aggregator: catalog ∪ promoted − rejected-zones → vary.* bounds."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.5.3
---
# TECH-725 — Signature three-source aggregator

> **Issue:** [TECH-725](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/signature.py` with `compute_envelope(catalog, promoted, rejected)` producing `vary.*` bounds where `envelope = catalog ∪ promoted − rejected-zones`. Rejection reasons map deterministically to `vary.*` axis carve-outs via a module-level `REASON_AXIS_MAP`. Same inputs → same envelope bytes.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `compute_envelope(catalog, promoted, rejected)` returns a `vary.*` dict.
2. Union of catalog + promoted tightens bounds toward validated variants.
3. Rejection reasons carve out floor zones via `REASON_AXIS_MAP`.
4. Deterministic across runs.

### 2.2 Non-Goals

1. Composer integration — TECH-726.
2. JSONL reader — cheap inline helper; not a new module.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Tightening envelope from promotes | After 5 promotes, `vary.*` ranges shrink toward promoted centroid |
| 2 | Sprite-gen dev | Carve-out from rejects | `roof-too-shallow` rejects raise `vary.roof.h_px.min` |
| 3 | Repo guardian | Determinism | Same inputs + same order → same envelope |

## 4. Current State

### 4.1 Domain behavior

`signature.py` extracts catalog signatures only. No aggregation across promoted / rejected sources.

### 4.2 Systems map

- `tools/sprite-gen/src/signature.py` — extend with aggregator.
- Inputs: catalog signatures (existing), `curation/promoted.jsonl` (TECH-723), `curation/rejected.jsonl` (TECH-724).

### 4.3 Implementation investigation notes

Promoted rows' `vary_values` already carry the exact sampled point per variant; aggregator treats them as catalog-equivalent data points. Rejected rows' `vary_values` mark the point to avoid; the `reason` names the dominant axis to push the bound away from.

## 5. Proposed Design

### 5.1 Target behavior

```python
env = compute_envelope(catalog_sigs, promoted_rows, rejected_rows)
# env["roof"]["h_px"] == {"min": 8, "max": 14}   # tightened vs catalog
# env["roof"]["h_px"]["min"] >= 8                # floor from roof-too-shallow rejects
```

### 5.2 Architecture / implementation

- Stage A — build tentative envelope from `catalog ∪ promoted` (min / max per axis).
- Stage B — apply `REASON_AXIS_MAP` to raise floor / lower ceiling per rejection (e.g. `roof-too-shallow` → `env["roof"]["h_px"]["min"] = max(existing, nearest_reject_h_px + 1)`).
- Stage C — return immutable dict.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Sort rows before aggregation | Determinism independent of filesystem read order | Rely on natural order — rejected, flaky on some filesystems |
| 2026-04-23 | Carve-out = nudge floor by 1 | Simple, monotonic; incremental tightening without oscillation | Learn carve-out magnitude — deferred |

## 7. Implementation Plan

### Phase 1 — `REASON_AXIS_MAP` constant

### Phase 2 — Envelope math (union + subtraction)

### Phase 3 — Unit tests (three-source combos + empty inputs)

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Union tightens | Python | `pytest tests/test_signature.py::test_envelope_union_tightens -q` | Promoted narrows range |
| Carve-out | Python | `pytest tests/test_signature.py::test_envelope_carveout -q` | Reject raises floor |
| Determinism | Python | `pytest tests/test_signature.py::test_envelope_deterministic -q` | Same inputs → same bytes |
| Empty inputs | Python | `pytest tests/test_signature.py::test_envelope_empty_fallback -q` | Fallback to catalog |

## 8. Acceptance Criteria

- [ ] `compute_envelope` returns `vary.*` bounds from three inputs.
- [ ] Union of catalog + promoted tightens bounds toward validated variants.
- [ ] Rejection reasons carve out `vary.*` floor zones via reason→axis map.
- [ ] Deterministic: same inputs → same envelope.
- [ ] Unit tests cover all three source combinations + empty-input fallback.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Sort-before-aggregate is free insurance against filesystem-order flakiness — cheap determinism wins.

## §Plan Digest

### §Goal

Turn three inputs (catalog signatures, promoted rows, rejected rows) into a single live envelope the composer can gate renders against, with rejection reasons carving `vary.*` axes away from known failures.

### §Acceptance

- [ ] `compute_envelope(catalog, promoted, rejected)` returns a dict with `vary.*` bounds shape
- [ ] Promoted rows tighten bounds toward their sampled `vary_values` centroid
- [ ] Each rejection reason carves its target axis per `REASON_AXIS_MAP` (raises floor or lowers ceiling)
- [ ] Determinism: sorted inputs → byte-identical envelope across runs
- [ ] Empty `promoted` + empty `rejected` → envelope equals catalog envelope
- [ ] `pytest tools/sprite-gen/tests/test_signature.py -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_envelope_union_tightens | 5 promoted rows with narrow `vary.roof.h_px` | env range < catalog range | pytest |
| test_envelope_carveout | 3 rejects with `roof-too-shallow` | `env["roof"]["h_px"]["min"]` raised above rejects | pytest |
| test_envelope_deterministic | same inputs twice (shuffled) | byte-identical output | pytest |
| test_envelope_empty_fallback | empty promoted + empty rejected | equals catalog envelope | pytest |
| test_reason_axis_map_coverage | each reason in `REJECTION_REASONS` | all have an axis mapping | pytest |

### §Examples

```python
# tools/sprite-gen/src/signature.py (excerpt)
from .curate import REJECTION_REASONS  # TECH-724 constant

REASON_AXIS_MAP = {
    "roof-too-shallow": ("roof.h_px", "min"),
    "roof-too-tall":    ("roof.h_px", "max"),
    "facade-too-saturated": ("facade.saturation", "max"),
    "ground-too-uniform":   ("ground.hue_jitter", "min"),
}

def compute_envelope(catalog, promoted, rejected):
    env = _initial_envelope_from_catalog(catalog)
    for row in sorted(promoted, key=_row_key):
        _tighten_towards(env, row["vary_values"])
    for row in sorted(rejected, key=_row_key):
        axis_path, bound = REASON_AXIS_MAP[row["reason"]]
        _carve_out(env, axis_path, bound, row["vary_values"])
    return env
```

### §Mechanical Steps

#### Step 1 — `REASON_AXIS_MAP`

**Edits:**

- `tools/sprite-gen/src/signature.py` — add map; import `REJECTION_REASONS` from TECH-724 module; unit-test parity.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py::test_reason_axis_map_coverage -q
```

#### Step 2 — Envelope math

**Edits:**

- Same file — `compute_envelope`, `_tighten_towards`, `_carve_out`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py::test_envelope_union_tightens tests/test_signature.py::test_envelope_carveout -q
```

#### Step 3 — Determinism + empty fallback

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Carve-out delta — +1 unit vs +measured-distance? **Resolution:** start with +1; revisit if composer score-and-retry (TECH-726) shows convergence problems.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
