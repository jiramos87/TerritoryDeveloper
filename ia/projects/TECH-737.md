---
purpose: "TECH-737 — spec.py accepts reserved output.animation block; enabled:false only; raises on enabled:true."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.7.1
---
# TECH-737 — Spec loader: reserved `output.animation:` block

> **Issue:** [TECH-737](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/spec.py` to recognise a reserved `output.animation:` dict. In v1, the only permitted runtime value is `enabled: false`; `enabled: true` raises `SpecError`. Sibling keys (`frames`, `fps`, `loop`, `phase_offset`, `layers`) are accepted and preserved without interpretation so future animation work can consume them unchanged. Consumes lock **L16**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `output.animation:` block parses without breaking the v1 composer.
2. `enabled: false` passes; `enabled: true` raises `SpecError`.
3. Reserved siblings accepted and preserved in the resolved spec.

### 2.2 Non-Goals

1. Per-primitive `animate:` guard — TECH-738.
2. DAS §12 stub — TECH-740.
3. Any actual frame rendering — deferred.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Write the animation block today for a future animated sprite | `enabled: false` passes; composer ignores block |
| 2 | Repo guardian | Prevent accidental `enabled: true` merges | `SpecError` with pointer to DAS §12 |
| 3 | Future animation-milestone dev | Read preserved siblings | Resolved spec still carries `frames`, `fps`, etc. |

## 4. Current State

### 4.1 Domain behavior

`spec.py` treats unknown top-level keys permissively today but doesn't carve out `output.animation` specifically — `enabled: true` would pass silently.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py` — loader entry.
- Consumers: `tests/test_animation_reservation.py` (TECH-739).

### 4.3 Implementation investigation notes

Validation sits in the post-load pass. No composer change — composer never reads `output.animation`. Keep the reserved-sibling list as a constant so TECH-740 can cite the exact set.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
output:
  name: my_sprite.png
  animation:
    enabled: false
    frames: 4
    fps: 8
    loop: true
    phase_offset: 0
    layers: [smoke, cooling_tower_glow]
```

Spec above loads clean. Swap `enabled: true` → `SpecError("output.animation.enabled: only 'false' permitted in v1; see DAS §12")`.

### 5.2 Architecture / implementation

- `_validate_animation(output)` helper called from `load_spec`.
- Reserved siblings: `ANIMATION_RESERVED_KEYS = {"frames", "fps", "loop", "phase_offset", "layers"}`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | `enabled: true` raises, not warns | Silent accept risks users expecting rendering that won't happen | Warn — rejected, easy to miss |
| 2026-04-23 | Sibling keys preserved, not stripped | Future animation dev reads them unchanged | Strip on load — rejected, destroys author intent |

## 7. Implementation Plan

### Phase 1 — Detect `output.animation:` block

### Phase 2 — Validate `enabled` value

### Phase 3 — Preserve reserved siblings

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Block with `enabled: false` parses | Python | `pytest tests/test_animation_reservation.py::test_reserved_block_parses -q` | TECH-739 |
| `enabled: true` raises | Python | `pytest tests/test_animation_reservation.py::test_enabled_true_raises -q` | TECH-739 |
| Reserved siblings preserved | Python | `pytest tests/test_animation_reservation.py::test_siblings_preserved -q` | TECH-739 |

## 8. Acceptance Criteria

- [ ] `output.animation:` block with `enabled: false` parses clean.
- [ ] `enabled: true` raises `SpecError`.
- [ ] Reserved siblings (`frames`, `fps`, `loop`, `phase_offset`, `layers`) accepted and preserved.
- [ ] Unit tests cover permit + raise paths (in TECH-739).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Reservation schemas are cheap safety nets — a five-line guard today prevents painful spec-migration work when animation ships.

## §Plan Digest

### §Goal

Reserve the animation schema in the spec grammar today so future animation work can land without breaking existing specs or silently misleading authors.

### §Acceptance

- [ ] `output.animation: {enabled: false, ...}` parses clean
- [ ] `output.animation: {enabled: true}` raises `SpecError` whose message references DAS §12
- [ ] Reserved sibling keys (`frames`, `fps`, `loop`, `phase_offset`, `layers`) preserved in resolved spec
- [ ] `ANIMATION_RESERVED_KEYS` constant exists and is consumed by the validator
- [ ] `pytest tools/sprite-gen/tests/test_animation_reservation.py -q` green for the subset

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_reserved_block_parses | spec with full `output.animation` block | load succeeds | pytest |
| test_enabled_true_raises | `output.animation.enabled: true` | SpecError with "DAS §12" in msg | pytest |
| test_siblings_preserved | reserved siblings populated | resolved spec keeps all siblings | pytest |

### §Examples

```python
# tools/sprite-gen/src/spec.py (excerpt)
ANIMATION_RESERVED_KEYS = frozenset({"frames", "fps", "loop", "phase_offset", "layers"})

def _validate_animation(output: dict) -> None:
    anim = output.get("animation")
    if anim is None:
        return
    enabled = anim.get("enabled", False)
    if enabled is not False:
        raise SpecError(
            "output.animation.enabled: only 'false' permitted in v1; see DAS §12"
        )
    # siblings are preserved as-is — no interpretation
```

### §Mechanical Steps

#### Step 1 — Detect block + validator helper

**Edits:**

- `tools/sprite-gen/src/spec.py` — add `ANIMATION_RESERVED_KEYS` + `_validate_animation(output)`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.spec import ANIMATION_RESERVED_KEYS; print(sorted(ANIMATION_RESERVED_KEYS))"
```

#### Step 2 — Wire into `load_spec`

**Edits:**

- Same file — call `_validate_animation(spec.get('output', {}))` after post-merge resolution.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_animation_reservation.py -q -k "reserved_block or enabled_true"
```

#### Step 3 — Preserve siblings

**Edits:**

- Same file — no-op preservation (default dict passthrough); add sanity assertion in test.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_animation_reservation.py::test_siblings_preserved -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Should `output.animation:` without `enabled` key default to `enabled: false`? **Resolution:** yes — author omission means not-animated; only explicit `enabled: true` raises.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
