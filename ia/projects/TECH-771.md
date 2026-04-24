---
purpose: "TECH-771 — Placement seed-stability tests."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.9b"
---
# TECH-771 — Placement seed-stability tests

> **Issue:** [TECH-771](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

One test file locking placement determinism. For each strategy, assert the declared decoration count is produced at stable coords under a fixed seed.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Each strategy (`corners`, `perimeter`, `random_border`, `grid`, `centered_front`, `centered_back`, explicit) produces expected item count.
2. Same seed + same inputs → byte-identical coord list across runs.
3. Composer integration (T7.8) raises `DecorationScopeError` on 1×1 + `iso_pool` regression case.

### 2.2 Non-Goals (Out of Scope)

1. Visual validation of placement (coord stability only).
2. Cross-platform floating-point tolerance (exact match v1).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Lock decoration placement behaviour | Same seed always produces same layout; regression test for 1×1 + pool gate |

## 4. Current State

### 4.1 Domain behavior

Placement engine (T7.7 / TECH-768) and composer integration (T7.8 / TECH-769) will be shipped. No regression test exists yet.

### 4.2 Systems map

New file `tools/sprite-gen/tests/test_placement.py`. Depends on `placement.place` from T7.7 / TECH-768 + composer integration from T7.8 / TECH-769.

## 5. Proposed Design

### 5.1 Target behavior (product)

For each placement strategy, instantiate a decoration list + footprint. Call `placement.place(...)` twice with same seed. Assert identical coord output both times. Also verify expected item count per strategy. Regression: verify composer raises `DecorationScopeError` on 1×1 + `iso_pool`.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Parametrized pytest tests per strategy. Each test defines a spec, calls `place()` twice with same seed, compares results. Separate regression test for composer gate.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Two-run determinism check | Catches seed regression | Single render (less robust) |

## 7. Implementation Plan

### Phase 1 — Per-strategy count + coord-stability tests

- [ ] Parametrize per strategy
- [ ] Call `place()` twice with same seed
- [ ] Assert identical output both times
- [ ] Assert expected item count

### Phase 2 — Cross-run determinism check (run twice, diff coord lists)

- [ ] Run placement twice in separate test runs (if CI supports)
- [ ] Verify results match

### Phase 3 — `DecorationScopeError` regression on 1×1 + `iso_pool` spec

- [ ] Instantiate 1×1 archetype + `iso_pool` decoration
- [ ] Assert composer raises `DecorationScopeError`

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| All strategies produce correct item count | Unit test | `pytest tools/sprite-gen/tests/test_placement.py` | Part of CI test suite |
| Same seed → byte-identical coords | Determinism test | `pytest tests/test_placement.py` | Verifies determinism across runs |

## 8. Acceptance Criteria

- [ ] Each strategy (`corners`, `perimeter`, `random_border`, `grid`, `centered_front`, `centered_back`, explicit) produces expected item count.
- [ ] Same seed + same inputs → byte-identical coord list across runs.
- [ ] Composer integration (T7.8) raises `DecorationScopeError` on 1×1 + `iso_pool` regression case.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- —

## §Plan Digest

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

### §Goal

Ship `tools/sprite-gen/tests/test_placement.py` — determinism + count + scope-gate coverage for `place()` (TECH-768) and composer integration (TECH-769). In-process re-invocation asserts byte-identical `list[tuple]` output per seed. `DecorationScopeError` regression on 1×1 + `iso_pool`.

### §Acceptance

- [ ] New file `tools/sprite-gen/tests/test_placement.py` created.
- [ ] `_STRATEGY_COUNT_CASES` parametrize array covers all 7 strategies with expected counts (corners=4, perimeter=N, random_border=count, grid=rows*cols, centered_front=1, centered_back=1, explicit=len(coords)).
- [ ] `test_placement_count[strategy]` asserts `len(output) == expected_count` per strategy.
- [ ] `test_placement_determinism_same_seed[strategy]` calls `place()` twice in-process with identical inputs, asserts coord lists byte-identical.
- [ ] `test_placement_seed_sensitivity[strategy]` over seeded strategies (`perimeter`, `random_border`) asserts seed 42 output != seed 43 output.
- [ ] `test_compose_decoration_scope_error_1x1_pool` asserts `compose_sprite({'footprint': [1, 1], 'decorations': [{'primitive': 'iso_pool'}], ...})` raises `DecorationScopeError`.
- [ ] `test_compose_decoration_scope_ok_2x2_pool` asserts 2×2 + `iso_pool` renders without exception.
- [ ] Test file runs green under `cd tools/sprite-gen && pytest tests/test_placement.py`.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_placement_count[strategy]` | parametrize 7 strategies + expected count | `len(output) == expected_count` | pytest |
| `test_placement_determinism_same_seed[strategy]` | 7 strategies; run `place()` twice same inputs + seed | identical `list[tuple]` | pytest |
| `test_placement_seed_sensitivity[strategy]` | `perimeter` + `random_border` at seed 42 vs 43 | coord lists differ | pytest |
| `test_compose_decoration_scope_error_1x1_pool` | `compose_sprite({'footprint': [1, 1], 'decorations': [{'primitive': 'iso_pool'}], ...})` | `DecorationScopeError` | pytest |
| `test_compose_decoration_scope_ok_2x2_pool` | 2×2 + `iso_pool` | no exception; pool rendered | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `place([{'primitive': 'iso_tree_fir', 'strategy': 'corners'}], (2, 2), seed=0)` | `len == 4` | Corners fixed |
| `place([{'primitive': 'iso_bush', 'strategy': 'grid', 'rows': 2, 'cols': 3}], (3, 3), seed=0)` | `len == 6` | rows*cols |
| `place([{'primitive': 'iso_bush', 'strategy': 'random_border', 'count': 5}], (3, 3), seed=42)` twice | identical list | Determinism |
| `place(..., seed=42)` vs `place(..., seed=43)` on `random_border` | different lists | Seed sensitivity |
| `compose_sprite({'footprint': [1, 1], 'decorations': [{'primitive': 'iso_pool'}], ...})` | `DecorationScopeError` | Scope gate |

### §Mechanical Steps

#### Step 1 — Create `test_placement.py` determinism + count + scope-gate test file

**Goal:** Author pytest file covering placement counts, in-process determinism, seed sensitivity, and composer scope-gate regression.

**Edits:**
- `tools/sprite-gen/tests/test_placement.py` — **operation**: create; **after** — new file. Body:
  - Module docstring: "Placement determinism + composer scope-gate tests (TECH-768 + TECH-769)."
  - Imports: `pytest`, `from src.placement import place`, `from src.compose import compose_sprite, DecorationScopeError`.
  - `_STRATEGY_COUNT_CASES = [` parametrize entries per strategy, each `(strategy_name, decoration_dict, footprint, expected_count)`:
    - `('corners', {'primitive': 'iso_bush', 'strategy': 'corners'}, (2, 2), 4)`
    - `('perimeter', {'primitive': 'iso_bush', 'strategy': 'perimeter', 'count': 6}, (3, 3), 6)`
    - `('random_border', {'primitive': 'iso_bush', 'strategy': 'random_border', 'count': 5}, (4, 4), 5)`
    - `('grid', {'primitive': 'iso_bush', 'strategy': 'grid', 'rows': 2, 'cols': 3}, (3, 3), 6)`
    - `('centered_front', {'primitive': 'iso_bush', 'strategy': 'centered_front'}, (2, 2), 1)`
    - `('centered_back', {'primitive': 'iso_bush', 'strategy': 'centered_back'}, (2, 2), 1)`
    - `('explicit', {'primitive': 'iso_bush', 'strategy': 'explicit', 'coords': [[0, 0], [10, 0]]}, (2, 2), 2)`
  - `_SEEDED_STRATEGIES = ('perimeter', 'random_border')`.
  - `@pytest.mark.parametrize('name,deco,fp,n', _STRATEGY_COUNT_CASES)` — `def test_placement_count(name, deco, fp, n)`: `assert len(place([deco], fp, seed=0)) == n`.
  - `@pytest.mark.parametrize('name,deco,fp,n', _STRATEGY_COUNT_CASES)` — `def test_placement_determinism_same_seed(name, deco, fp, n)`:
    - `first = place([deco], fp, seed=42)`; `second = place([deco], fp, seed=42)`
    - `assert first == second`
  - `@pytest.mark.parametrize('name', _SEEDED_STRATEGIES)` — `def test_placement_seed_sensitivity(name)`:
    - build decoration dict from `_STRATEGY_COUNT_CASES` lookup for `name`; footprint=(4, 4)
    - `a = place([deco], fp, seed=42)`; `b = place([deco], fp, seed=43)`; `assert a != b`
  - `def _minimal_spec(footprint, decorations)`: return dict with `footprint`, `decorations`, `seed=0`, and the minimum other keys `compose_sprite` requires (composition body can be empty — agent audits and fills from existing golden specs during implementation).
  - `def test_compose_decoration_scope_error_1x1_pool()`:
    - `spec = _minimal_spec([1, 1], [{'primitive': 'iso_pool', 'strategy': 'centered_front'}])`
    - `with pytest.raises(DecorationScopeError): compose_sprite(spec)`
  - `def test_compose_decoration_scope_ok_2x2_pool()`:
    - `spec = _minimal_spec([2, 2], [{'primitive': 'iso_pool', 'strategy': 'centered_front'}])`
    - `compose_sprite(spec)` returns without exception
- `invariant_touchpoints`: none (test authoring)
- `validator_gate`: `test -f tools/sprite-gen/tests/test_placement.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/tests/test_placement.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. `place` import fail → re-open TECH-768. `DecorationScopeError` import fail → re-open TECH-769. `_minimal_spec` missing keys required by `compose_sprite` → re-open Step 1 to add them (reference existing golden spec under `tools/sprite-gen/specs/`).

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

## Open Questions (resolve before / during implementation)

None — test design fully specified in master plan Stage 7.
