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
- **Score contract (pinned):**
  - **Inputs per variant:** sampled `vary_values` (recovered via `compose.sample_variant(spec, variant_idx)` — same mechanism as TECH-723 `_load_vary_values`).
  - **Per-axis normalized deviation** `d_a`: for each leaf axis `a` with envelope `{min, max}` and sampled value `v_a`, compute centroid `c_a = (min+max)/2`, half-range `h_a = (max-min)/2`, then `d_a = abs(v_a - c_a) / h_a` clamped to `[0, 1]` (degenerate `h_a = 0` → `d_a = 0`).
  - **Aggregate metric:** L2 Euclidean mean — `L2 = sqrt(sum(d_a^2) / n_axes)`. Stays in `[0, 1]`; penalizes single large deviations more than an L1 mean would.
  - **Score:** `1.0 - L2` (higher = closer to centroid = better).
  - **Carved-zone hard-fail:** if any axis in `failing_zones` (computed via TECH-725 `REASON_AXIS_MAP` / floor-and-ceiling comparison against `env[axis][bound]`), return `{"score": 0.0, "failing_zones": [...]}` short-circuit before L2 math.
- **Floor** `_FLOOR = 0.5` — module-level constant (revisitable via TECH-728 fixture retry-rate distribution); `gate_enabled=False` feature flag (default off) bypasses the gate entirely and renders identically to pre-Stage-6.5.
- Retry loop: while `attempt < retry_cap and score < _FLOOR`: advance seed (`palette_seed + variant_idx * (retry_cap + 1) + retry`); re-sample via `sample_variant`; re-render via `compose_sprite`; re-score.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Hard-fail on carved zones | Carve-outs are "known bad"; don't soft-score them | Soft penalty — rejected, defeats reason-carve semantics |
| 2026-04-23 | Feature-flag gate (`gate_enabled=False` default) | Back-compat before Stage 6.5 merges everywhere | Unconditional — rejected, breaks golden PNGs before curation dataset exists |
| 2026-04-23 | Seed = `palette_seed + variant_index * (N+1) + retry` | Distinct across (variant, retry) pairs | `palette_seed + retry` — rejected, crosses variant boundaries |
| 2026-04-23 | L2 Euclidean aggregate (per-axis normalized, `[0, 1]`) | Penalizes a single large axis deviation more than L1 would; stays bounded so `_FLOOR = 0.5` has consistent semantics across axis count | L1 mean — rejected, masks single-axis outliers; Chebyshev — rejected, overreacts to one noisy axis |
| 2026-04-23 | `_FLOOR = 0.5` starting value | Permissive enough to not over-retry on sparse curation data; TECH-728 fixture can calibrate | 0.7 — rejected, likely forces exhaustion before dataset density exists; 0.3 — rejected, gate becomes ineffective |
| 2026-04-23 | Re-sample `vary_values` via `compose.sample_variant(spec, variant_idx)` | Deterministic; matches TECH-723 `_load_vary_values` mechanism — one source of truth for how vary_values are reconstructed | Track inside render loop — rejected, duplicates sampling state across two code paths |

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

- [ ] `compose.render(spec, envelope=env, retry_cap=5, gate_enabled=True)` retries below-floor variants up to 5 times
- [ ] `compose.render(spec, envelope=None)` and `compose.render(spec, envelope=env, gate_enabled=False)` both yield byte-identical output to pre-Stage-6.5 `compose_sprite(spec)` (existing golden tests pass untouched — parity baseline = current goldens)
- [ ] Seed advancement: `palette_seed + variant_index * (retry_cap + 1) + retry_index`
- [ ] Score function: per-axis `d_a = clamp(abs(v_a - c_a) / h_a, 0, 1)`; aggregate `L2 = sqrt(mean(d_a^2))`; `score = 1.0 - L2`; hard-fail `score = 0.0` on any carved-zone hit
- [ ] `_FLOOR = 0.5` module-level constant
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
import math

_FLOOR = 0.5  # score threshold; below → retry

def render(spec, *, envelope=None, retry_cap=5, gate_enabled=False):
    """Variant generator. Flag-off or envelope-None → pre-Stage-6.5 path."""
    palette_seed = int(spec.get("palette_seed", spec.get("seed", 0)) or 0)
    count = spec.get("variants", {}).get("count", 1)
    for i in range(count):
        if envelope is None or not gate_enabled:
            yield compose_sprite(sample_variant(spec, i))  # byte-identical pre-gate
            continue
        best = None
        attempts = []
        for retry in range(retry_cap):
            seed = palette_seed + i * (retry_cap + 1) + retry
            spec_i = {**spec, "palette_seed": seed}
            sampled = sample_variant(spec_i, i)
            variant = compose_sprite(sampled)
            vary_values = _diff_vary_leaves(spec_i, sampled)  # same helper as TECH-723
            score = _score_variant(vary_values, envelope)
            attempts.append(seed)
            if best is None or score["score"] > best["score"]:
                best = {"variant": variant, "score": score["score"],
                        "failing_zones": score["failing_zones"],
                        "retry": retry, "seed": seed, "attempts": list(attempts)}
            if score["score"] >= _FLOOR:
                yield variant
                break
        else:
            yield best  # consumed by TECH-727 sidecar path

def _score_variant(vary_values: dict, envelope: dict) -> dict:
    """L2 Euclidean mean of per-axis normalized deviations; hard-fail carved zones."""
    failing = _carved_zone_hits(vary_values, envelope)
    if failing:
        return {"score": 0.0, "failing_zones": failing}
    squares, n = 0.0, 0
    for axis_path, v in _walk_leaves(vary_values):
        bounds = _resolve_env_bounds(envelope, axis_path)
        if bounds is None:
            continue
        lo, hi = bounds["min"], bounds["max"]
        c, h = (lo + hi) / 2.0, (hi - lo) / 2.0
        d = 0.0 if h == 0 else min(abs(v - c) / h, 1.0)
        squares += d * d
        n += 1
    if n == 0:
        return {"score": 1.0, "failing_zones": []}
    l2 = math.sqrt(squares / n)
    return {"score": 1.0 - l2, "failing_zones": []}
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

1. ~~`_FLOOR` value — 0.5?~~ **Resolved 2026-04-23:** `_FLOOR = 0.5` module-level; revisit once TECH-728 fixtures show retry-rate distribution (Acceptance footnote on Stage 6.5 close).
2. ~~Parity baseline fixture?~~ **Resolved 2026-04-23:** None needed. Flag-off / `envelope=None` path delegates to existing `compose_sprite(sample_variant(spec, i))` unchanged — existing golden-PNG tests act as the parity oracle. Test `test_flag_off_byte_identical` = assert new `render` generator with flag off yields pixels equal to direct `compose_sprite(sample_variant(spec, i))` for each variant.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
