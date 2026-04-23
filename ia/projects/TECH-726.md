---
purpose: "TECH-726 — Composer render-time score-and-retry gate sampling against envelope with N-retry cap."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.5.4
---
# TECH-726 — Composer render-time score-and-retry gate

> **Issue:** [TECH-726](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Wrap the composer's variant render in a score-and-retry loop: sample `vary:` from the envelope (TECH-725) → render → score → if below floor, re-sample with a new seed (`palette_seed + i + retry`). Configurable retry cap `N` (default 5). Feature-flag off → byte-identical pre-gate behaviour.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Retry count `N` configurable on the composer entry (env var + function arg).
2. Deterministic: same seeds + same envelope → same retry trajectory.
3. Feature-flag off (gate disabled) → byte-identical pre-gate render.
4. Scoring: normalized distance from envelope centroid + hard-fail penalty on carved zones.

### 2.2 Non-Goals

1. `.needs_review` sidecar on exhaustion — TECH-727.
2. Envelope construction — TECH-725 owns it.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Gate low-quality variants | Below-floor variants re-sample up to N times |
| 2 | Repo guardian | Back-compat | Flag off → byte-identical to pre-Stage-6.5 baseline |
| 3 | Artist | Predictable outputs | Same seeds → same retry path + final variant |

## 4. Current State

### 4.1 Domain behavior

Composer renders variant once from sampled `vary:` — no quality gate.

### 4.2 Systems map

- `tools/sprite-gen/src/compose.py` — wrap variant loop.
- Consumers: `src/signature.py::compute_envelope` (TECH-725).

### 4.3 Implementation investigation notes

Seed advancement per retry: use `palette_seed + variant_index * (N+1) + retry`. Guarantees each retry is a distinct seed without crossing variant-index boundaries (retry i of variant j never equals retry k of variant l for different variant indexes).

## 5. Proposed Design

### 5.1 Target behavior

```python
# flag off — byte-identical
compose.render(spec, envelope=None)  # no gate

# flag on — score-and-retry
compose.render(spec, envelope=env, retry_cap=5)
# per variant: up to 5 attempts to land above floor
```

### 5.2 Architecture / implementation

- `_score_variant(variant, envelope)` → `{score: float, failing_zones: list[str]}`.
- Score = 1.0 − mean-normalized-distance-from-centroid across axes; `0.0` if any axis hits carved zone.
- Retry loop: while `attempt < retry_cap and score < floor`: advance seed; re-sample; re-render; re-score.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Hard-fail on carved zones | Carve-outs are "known bad"; don't soft-score them | Soft penalty — rejected, defeats reason-carve semantics |
| 2026-04-23 | Feature-flag gate | Back-compat before Stage 6.5 merges everywhere | Unconditional — rejected, breaks golden PNGs before curation dataset exists |
| 2026-04-23 | Seed = `palette_seed + variant_index * (N+1) + retry` | Distinct across (variant, retry) pairs | `palette_seed + retry` — rejected, crosses variant boundaries |

## 7. Implementation Plan

### Phase 1 — `_score_variant`

### Phase 2 — Retry loop with seed advancement

### Phase 3 — Feature-flag / back-compat regression

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Retry trajectory | Python | test in TECH-728 | Below-floor run retries until pass or N |
| Determinism | Python | test in TECH-728 | Same seeds → same trajectory |
| Flag-off byte-identical | Python | `pytest tools/sprite-gen/tests/ -q` | Pre-change goldens match |
| Carved zone fail | Python | test in TECH-728 | Any carved-zone hit = score 0 |

## 8. Acceptance Criteria

- [ ] Retry count configurable; default 5.
- [ ] Deterministic: same seeds → same retry trajectory.
- [ ] Feature-flag off → byte-identical to pre-gate render.
- [ ] Score function penalises carved zones as hard-fail.
- [ ] Unit test covers retry trajectory + feature-flag regression.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Feature-flag-off as a byte-identical baseline lets the gate ship before the curation dataset is dense enough to be useful.

## §Plan Digest

### §Goal

Composer picks up an envelope-aware quality gate: sample, render, score, retry — up to N times. Flag off preserves pre-Stage-6.5 output exactly so the gate can merge independently of curation density.

### §Acceptance

- [ ] `compose.render(spec, envelope=env, retry_cap=5)` retries below-floor variants up to 5 times
- [ ] `compose.render(spec, envelope=None)` is byte-identical to pre-change baseline
- [ ] Seed advancement: `palette_seed + variant_index * (retry_cap + 1) + retry_index`
- [ ] Score function returns `0.0` on any carved-zone hit (hard-fail)
- [ ] Determinism: same `(spec, envelope, palette_seed)` → same trajectory + same final variant
- [ ] `pytest tools/sprite-gen/tests/ -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_gate_retries_below_floor | spec producing low-score variant | retry count > 0 | pytest |
| test_gate_determinism | same seeds twice | same trajectory | pytest |
| test_gate_carved_zone_hard_fail | variant hits carved axis | score == 0; triggers retry | pytest |
| test_flag_off_byte_identical | `envelope=None` | pixel-identical to baseline | pytest |

### §Examples

```python
# tools/sprite-gen/src/compose.py (excerpt)
def render(spec, *, envelope=None, retry_cap=5, palette_seed=0):
    for i in range(spec.variants.count):
        if envelope is None:
            variant = _render_single(spec, palette_seed=palette_seed + i)
            yield variant
            continue
        best = None
        for retry in range(retry_cap):
            seed = palette_seed + i * (retry_cap + 1) + retry
            variant = _render_single(spec, palette_seed=seed)
            score = _score_variant(variant, envelope)
            if best is None or score["score"] > best["score"]:
                best = {"variant": variant, "score": score["score"],
                        "failing_zones": score["failing_zones"], "retry": retry,
                        "seed": seed}
            if score["score"] >= _FLOOR:
                yield variant
                break
        else:
            yield best  # consumed by TECH-727 sidecar path

def _score_variant(variant, envelope) -> dict:
    failing = _carved_zone_hits(variant, envelope)
    if failing:
        return {"score": 0.0, "failing_zones": failing}
    distance = _normalized_distance_from_centroid(variant, envelope)
    return {"score": 1.0 - distance, "failing_zones": []}
```

### §Mechanical Steps

#### Step 1 — `_score_variant`

**Edits:**

- `tools/sprite-gen/src/compose.py` — scoring helper + carved-zone detector.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py::test_gate_carved_zone_hard_fail -q
```

#### Step 2 — Retry loop

**Edits:**

- Same file — wrap `render`; advance seed per retry; yield best on exhaustion (TECH-727 picks this up).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py::test_gate_retries_below_floor tests/test_curation_loop.py::test_gate_determinism -q
```

#### Step 3 — Flag-off back-compat

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. `_FLOOR` value — 0.5? **Resolution:** start at 0.5; revisit once TECH-728 fixtures show retry-rate distribution.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
