---
purpose: "TECH-746 — compose.py passthrough branch: skip iso_ground_noise + clamp hue_jitter ≤ 0.01 + value_jitter = 0."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T7.10.2
---
# TECH-746 — Composer: inhibit noise + clamp jitter on passthrough tiles

> **Issue:** [TECH-746](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend the composer's ground render path: when `spec.ground.passthrough` is `true`, skip the `iso_ground_noise` call, clamp `hue_jitter ≤ 0.01`, and force `value_jitter = 0`. Base material colour stays intact so the tile reads as a seamless continuation of its neighbors. Consumes lock **L17**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_ground_noise` skipped when `passthrough=true`.
2. `hue_jitter` clamped to `≤ 0.01` (author values higher than this cap are silently narrowed).
3. `value_jitter` forced to `0`.
4. Base material colour preserved so neighbor tiles blend.

### 2.2 Non-Goals

1. Schema flag — TECH-745.
2. Tests — TECH-747.
3. DAS amendment — TECH-748.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Place an "empty lot" tile that blends with neighbors | Passthrough render has no noise + narrowest jitter |
| 2 | Reviewer | Trust blending correctness | Visual diff vs. non-passthrough baseline is bounded |
| 3 | Future-curator UI | Know which tiles are bridges | `spec.ground.passthrough` is the canonical flag |

## 4. Current State

### 4.1 Domain behavior

Stage 6.4 ground path calls `iso_ground_noise` unconditionally and applies `hue_jitter` / `value_jitter` from `spec.ground`.

### 4.2 Systems map

- `tools/sprite-gen/src/compose.py` — ground render path (primary edit site).
- Consumers: `tests/test_ground_passthrough.py` (TECH-747).

### 4.3 Implementation investigation notes

Clamp `hue_jitter = min(hue_jitter, 0.01)` so authors don't accidentally defeat passthrough by bumping jitter. Keep the branch narrow — only two lines of behaviour change.

## 5. Proposed Design

### 5.1 Target behavior

```python
# Before (Stage 6.4):
iso_ground_diamond(canvas, footprint, material)
iso_ground_noise(canvas, material)
_apply_jitter(canvas, hue_jitter, value_jitter)

# After (with passthrough branch):
iso_ground_diamond(canvas, footprint, material)
if not ground.passthrough:
    iso_ground_noise(canvas, material)
    _apply_jitter(canvas, hue_jitter, value_jitter)
else:
    _apply_jitter(canvas, min(hue_jitter, 0.01), 0.0)
```

### 5.2 Architecture / implementation

- One `if spec.ground.get("passthrough", False):` branch in ground render.
- No new primitive; reuses existing ones with adjusted args.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Clamp (silent narrow) rather than raise on jitter > 0.01 | Authors may copy jitter from a non-passthrough preset; silent narrow is less surprising | Raise — rejected, spec author friction |
| 2026-04-23 | Keep base material; skip noise only | Colour continuity with neighbors is the whole point | Swap to neutral grey — rejected, defeats purpose |

## 7. Implementation Plan

### Phase 1 — Branch on passthrough

### Phase 2 — Skip noise call

### Phase 3 — Clamp jitter

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Noise skip | Python | `pytest tests/test_ground_passthrough.py::test_passthrough_skips_noise -q` | TECH-747 |
| Jitter clamp | Python | `pytest tests/test_ground_passthrough.py::test_passthrough_clamps_jitter -q` | TECH-747 |
| Default unchanged | Python | `pytest tests/test_ground_passthrough.py::test_default_byte_identical -q` | TECH-747 |

## 8. Acceptance Criteria

- [ ] `iso_ground_noise` skipped when `passthrough=true`.
- [ ] `hue_jitter` clamped to `≤0.01`.
- [ ] `value_jitter` forced to `0`.
- [ ] Base material colour preserved.
- [ ] Unit tests cover render diff + jitter clamp (in TECH-747).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Passthrough rendering is not about a new primitive — it's about selectively _skipping_ existing ones. Cheapest possible implementation, highest author leverage.

## §Plan Digest

### §Goal

Make `ground.passthrough: true` produce a seamless neighbor-blending render by skipping `iso_ground_noise` and narrowing jitter, without introducing any new primitive.

### §Acceptance

- [ ] Render with `passthrough=true` has no `iso_ground_noise` output (verified via diff vs. baseline)
- [ ] `hue_jitter` value effectively ≤0.01 regardless of author input
- [ ] `value_jitter` effectively 0 on passthrough tiles
- [ ] Render with `passthrough=false` byte-identical to pre-addendum baseline
- [ ] Base material colour unchanged — only noise/jitter altered

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_passthrough_skips_noise | passthrough=true spec | render differs from baseline; difference bounded | pytest |
| test_passthrough_clamps_jitter | author `hue_jitter: 0.1` + passthrough | effective jitter ≤0.01 | pytest |
| test_passthrough_zero_value_jitter | author `value_jitter: 0.1` + passthrough | effective value_jitter == 0 | pytest |
| test_default_byte_identical | passthrough=false | SHA256 matches pre-addendum baseline | pytest |

### §Examples

```python
# tools/sprite-gen/src/compose.py (excerpt)

def _render_ground(canvas, spec_ground, footprint) -> None:
    material = spec_ground["material"]
    iso_ground_diamond(canvas, footprint, material)

    if spec_ground.get("passthrough", False):
        # Neighbor-blending bridge — no noise, narrowest jitter
        _apply_jitter(canvas, min(spec_ground.get("hue_jitter", 0.0), 0.01), 0.0)
        return

    iso_ground_noise(canvas, material)
    _apply_jitter(
        canvas,
        spec_ground.get("hue_jitter", 0.0),
        spec_ground.get("value_jitter", 0.0),
    )
```

### §Mechanical Steps

#### Step 1 — Branch on passthrough

**Edits:**

- `tools/sprite-gen/src/compose.py` — `if spec_ground.get("passthrough"):` branch.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_passthrough.py -q -k default_byte_identical
```

#### Step 2 — Skip noise call

**Edits:**

- Same file — early-return after diamond + clamped jitter when passthrough.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_passthrough.py -q -k skips_noise
```

#### Step 3 — Clamp jitter

**Edits:**

- Same file — `min(hue_jitter, 0.01)` and `value_jitter = 0.0`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_ground_passthrough.py -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does `iso_ground_noise` already accept a seed / stateless invocation? **Resolution:** immaterial — we skip the call entirely when passthrough=true.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
