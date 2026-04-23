---
purpose: "TECH-738 — composer per-primitive animate guard: accepts none, raises NotImplementedError otherwise."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.7.2
---
# TECH-738 — Per-primitive `animate:` reservation

> **Issue:** [TECH-738](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Add a centralised guard in the composer's primitive dispatch: any decoration entry may carry `animate: none` (a no-op passthrough); any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. The guard sits in the dispatch path so every primitive (existing and future) inherits the check with zero duplication.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Centralised guard — every primitive inherits without duplication.
2. `animate: none` is a no-op passthrough; primitive renders normally.
3. Any other value raises `NotImplementedError` with `DAS §12` pointer.

### 2.2 Non-Goals

1. Spec loader's `output.animation:` block — TECH-737.
2. Implementing actual animation — deferred.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Mark a primitive as explicitly non-animated | `animate: none` accepted; render unchanged |
| 2 | Spec author | Accidentally request animation | `NotImplementedError` with clear DAS pointer |
| 3 | Future primitive author | Inherit the guard for free | Dispatch-level check covers new primitive automatically |

## 4. Current State

### 4.1 Domain behavior

Composer dispatch reads primitive kwargs directly; no centralised key validation.

### 4.2 Systems map

- `tools/sprite-gen/src/compose.py` — dispatch entry (primary edit site).
- Consumers: `tests/test_animation_reservation.py` (TECH-739).

### 4.3 Implementation investigation notes

Guard runs before kwargs pass into the primitive callable, so primitives never see `animate` key. Error message must contain literal string "DAS §12" — asserted in TECH-739 test.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
decorations:
  - type: iso_tree_fir
    x_px: 10
    y_px: 20
    animate: none   # explicit no-op
```

Any value other than `none`:

```yaml
decorations:
  - type: iso_tree_fir
    animate: flicker   # raises NotImplementedError("Animation deferred; see DAS §12")
```

### 5.2 Architecture / implementation

- `_check_animate(entry)` helper inside composer dispatch.
- Called once per decoration / per building detail entry before primitive call.
- `animate` key stripped from kwargs before primitive call.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Centralised guard in dispatch | Zero duplication; primitives stay pure | Per-primitive check — rejected, N×duplication |
| 2026-04-23 | Strip `animate` before kwargs pass | Primitives don't see reserved key | Pass-through — rejected, every primitive would need to ignore it |

## 7. Implementation Plan

### Phase 1 — Centralise check in composer dispatch

### Phase 2 — Raise on unknown values

### Phase 3 — Strip key before kwargs pass

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| `animate: none` renders | Python | `pytest tests/test_animation_reservation.py::test_animate_none_renders -q` | TECH-739 |
| Other value raises | Python | `pytest tests/test_animation_reservation.py::test_animate_value_raises -q` | TECH-739 |
| Msg mentions DAS §12 | Python | `pytest tests/test_animation_reservation.py::test_animate_raise_msg -q` | TECH-739 |

## 8. Acceptance Criteria

- [ ] `animate: none` is a no-op passthrough.
- [ ] Any other value raises `NotImplementedError`.
- [ ] Raised message contains `DAS §12`.
- [ ] Guard centralised in composer dispatch (not per-primitive duplication).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Single-point guards scale with the primitive count for free; per-primitive checks drift as soon as a new primitive lands.

## §Plan Digest

### §Goal

Centralise a `NotImplementedError` guard for per-primitive `animate:` in the composer dispatch so every current and future primitive inherits the reservation for free.

### §Acceptance

- [ ] Guard lives in `compose.py` dispatch (single site)
- [ ] `animate: none` passes; primitive renders normally with `animate` stripped from kwargs
- [ ] `animate: <anything else>` raises `NotImplementedError`
- [ ] Error message contains literal `DAS §12`
- [ ] No per-primitive duplication of the check
- [ ] `pytest tools/sprite-gen/tests/test_animation_reservation.py -q` green for this subset

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_animate_none_renders | decoration with `animate: none` | sprite renders; primitive doesn't see `animate` kwarg | pytest |
| test_animate_value_raises | `animate: flicker` | NotImplementedError | pytest |
| test_animate_raise_msg | any non-`none` value | exception msg contains "DAS §12" | pytest |

### §Examples

```python
# tools/sprite-gen/src/compose.py (excerpt)

def _check_animate(entry: dict) -> dict:
    animate = entry.get("animate")
    if animate is None or animate == "none":
        return {k: v for k, v in entry.items() if k != "animate"}
    raise NotImplementedError(
        f"Animation deferred; see DAS §12 "
        f"(unsupported animate value: {animate!r})"
    )

def _dispatch_primitive(entry: dict, ctx) -> None:
    clean_kwargs = _check_animate(entry)
    primitive = _resolve_primitive(clean_kwargs.pop("type"))
    primitive(ctx.canvas, **clean_kwargs)
```

### §Mechanical Steps

#### Step 1 — Guard helper

**Edits:**

- `tools/sprite-gen/src/compose.py` — `_check_animate(entry)`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.compose import _check_animate; _check_animate({'animate':'none'})"
```

#### Step 2 — Wire into dispatch

**Edits:**

- Same file — call `_check_animate` at every primitive dispatch site; use returned kwargs.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_animation_reservation.py -q -k "animate_none or animate_value"
```

#### Step 3 — Strip key + assertion on msg

**Edits:**

- Same file — ensure `animate` absent from kwargs reaching primitive.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_animation_reservation.py::test_animate_raise_msg -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does the guard also apply to building `details:` entries, not just decorations? **Resolution:** yes — dispatch path is shared for both; single guard covers both surface areas.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
