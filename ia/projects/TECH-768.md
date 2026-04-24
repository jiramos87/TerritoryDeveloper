---
purpose: "TECH-768 — Placement strategies."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.7"
---
# TECH-768 — Placement strategies

> **Issue:** [TECH-768](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Ship `placement.py` — pure decoration placement engine. Given a decoration list + footprint + seed, returns deterministic pixel coords for each primitive call.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `place(decorations, footprint, seed)` returns `list[(primitive_call, x_px, y_px, kwargs)]`.
2. Seven strategies supported: `corners`, `perimeter`, `random_border`, `grid(rows, cols)`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`.
3. Output deterministic per seed — same inputs always produce identical coord list.

### 2.2 Non-Goals (Out of Scope)

1. Collision detection (placement assumes caller validates overlap).
2. 3D depth sorting (2D pixel coords only).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Define yard decoration layout | Place returns stable coords per seed; all strategies execute without exception |

## 4. Current State

### 4.1 Domain behavior

No placement engine exists yet. DAS §5 R9 defines strategy semantics.

### 4.2 Systems map

New file `tools/sprite-gen/src/placement.py`. Consumer: composer `decorations:` dispatch (T7.8 / TECH-769). Test surface: `tests/test_placement.py` (T7.9b / TECH-771).

## 5. Proposed Design

### 5.1 Target behavior (product)

Given a decoration spec list + footprint + seed, compute deterministic pixel positions for each decoration. Seven strategies define placement rules: corners (fixed corner positions), perimeter (random along border), random_border (seeded random border), grid (regular grid with row/col params), centered_front (centered on front edge), centered_back (centered on back edge), explicit (user-specified pixel coords).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Pure function signature: `place(decorations, footprint, seed) → list[(primitive_name, x_px, y_px, kwargs_dict)]`. Each decoration entry specifies strategy + params.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Seeded random via `random.Random(seed)` | Deterministic, portable across platforms | Global random state (non-deterministic), seed as int only (works fine) |

## 7. Implementation Plan

### Phase 1 — `place()` signature + per-strategy dispatch skeleton

- [ ] Define `place(decorations, footprint, seed)` signature
- [ ] Create strategy dispatch table

### Phase 2 — Deterministic strategies (`corners`, `centered_*`, `grid`, explicit)

- [ ] Implement corners strategy
- [ ] Implement centered_front / centered_back
- [ ] Implement grid(rows, cols)
- [ ] Implement explicit coordinate list

### Phase 3 — Seeded-random strategies (`perimeter`, `random_border`) via `random.Random(seed)`

- [ ] Implement perimeter with `random.Random(seed)`
- [ ] Implement random_border with `random.Random(seed)`

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Each strategy produces expected item count | Unit test | `tests/test_placement.py` (T7.9b) | Validates count accuracy |
| Same seed → identical coord list | Regression test | `tests/test_placement.py` | Verifies determinism |

## 8. Acceptance Criteria

- [ ] `place(decorations, footprint, seed)` returns `list[(primitive_call, x_px, y_px, kwargs)]`.
- [ ] Seven strategies supported: `corners`, `perimeter`, `random_border`, `grid(rows, cols)`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`.
- [ ] Output deterministic per seed — same inputs always produce identical coord list.

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

Ship `tools/sprite-gen/src/placement.py` — pure decoration placement engine. `place(decorations, footprint, seed)` dispatches 7 strategies, returns deterministic `list[tuple[str, int, int, dict]]`. Seeded strategies use `random.Random(seed + i)` per index — never global RNG.

### §Acceptance

- [ ] `tools/sprite-gen/src/placement.py` exports `place(decorations: list[dict], footprint: tuple[int, int], seed: int) -> list[tuple[str, int, int, dict]]`.
- [ ] Seven strategies dispatched: `corners`, `perimeter`, `random_border`, `grid`, `centered_front`, `centered_back`, `explicit`.
- [ ] `_STRATEGIES = ('corners', 'perimeter', 'random_border', 'grid', 'centered_front', 'centered_back', 'explicit')` literal present.
- [ ] Unknown strategy raises `ValueError` with canonical set.
- [ ] Malformed `footprint` (not length-2 tuple/list, or non-positive ints) raises `ValueError`.
- [ ] Seeded strategies (`perimeter`, `random_border`) use `random.Random(seed + i)` per decoration index `i` — identical seed → byte-identical output.
- [ ] Module docstring documents return shape `list[tuple[str, int, int, dict]]` as the contract consumed by `compose_sprite` (TECH-769 / T7.8).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_place_corners_always_4` | `corners` strategy, 2×2 footprint | 4 tuples; coords at 4 corners | pytest |
| `test_place_grid_count` | `grid(rows=2, cols=3)` | 6 tuples | pytest |
| `test_place_perimeter_deterministic` | `perimeter`, count=5, seed=42 run twice | identical coord list | pytest |
| `test_place_random_border_seed_stable` | `random_border`, count=5, seed=0 vs seed=1 | coord sets differ; each stable per seed | pytest |
| `test_place_explicit_passthrough` | `explicit` with 2 coords | exactly those 2 coords in output | pytest |
| `test_place_footprint_validation` | `footprint=(0, 2)` | `ValueError` on malformed footprint | pytest |
| `test_place_return_shape` | any strategy | each element = `tuple[str, int, int, dict]` | pytest |
| `test_place_unknown_strategy` | `strategy='spiral'` | `ValueError` with canonical set | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `place([{'primitive': 'iso_tree_fir', 'strategy': 'corners'}], (2, 2), seed=0)` | 4 tuples at corner coords | Always 4 |
| `place([{'primitive': 'iso_bush', 'strategy': 'grid', 'rows': 2, 'cols': 3}], (3, 3), seed=0)` | 6 tuples at grid positions | `2 * 3` |
| `place([{'primitive': 'iso_tree_fir', 'strategy': 'random_border', 'count': 5}], (4, 4), seed=0)` | 5 tuples on border; stable per seed | Seeded |
| `place([{'primitive': 'iso_fence', 'strategy': 'explicit', 'coords': [[0, 0], [10, 0]]}], (2, 2), seed=0)` | 2 tuples at exact coords | Pass-through |
| `place([{'primitive': 'x', 'strategy': 'spiral'}], (2, 2), seed=0)` | `ValueError` canonical set | Invalid |

### §Mechanical Steps

#### Step 1 — Create `placement.py` module with dispatch + 7 strategies

**Goal:** Author `tools/sprite-gen/src/placement.py` — pure `place()` function with strategy dispatch and per-index seeded RNG.

**Edits:**
- `tools/sprite-gen/src/placement.py` — **operation**: create; **after** — new file exporting `place(decorations: list[dict], footprint: tuple[int, int], seed: int) -> list[tuple[str, int, int, dict]]`. Body:
  - Module docstring: "Pure placement engine. Consumed by compose_sprite (TECH-769). Return shape `list[tuple[primitive_name: str, x_px: int, y_px: int, kwargs: dict]]`. Decoration dict shape: `{primitive, strategy, count?, rows?, cols?, coords?, kwargs?}`."
  - Import `random as _random_mod`.
  - `_STRATEGIES = ('corners', 'perimeter', 'random_border', 'grid', 'centered_front', 'centered_back', 'explicit')`.
  - Tile pixel constants: `_TILE_W_PX = 32`, `_TILE_H_PX = 16` (consistent with existing sprite tile geometry).
  - Validate `footprint`: length-2 sequence of positive ints else `raise ValueError("footprint must be length-2 tuple of positive ints")`.
  - For each decoration `i`, extract `strategy`; validate `strategy in _STRATEGIES` else `raise ValueError(f"strategy must be in {set(_STRATEGIES)}")`.
  - Create per-index `rng = _random_mod.Random(int(seed) + i)` (fresh RNG per decoration).
  - Dispatch via a mapping `_STRATEGY_FUNCS = {...}` to private helpers:
    - `_strategy_corners(deco, fx, fy, rng)` → 4 tuples at tile corners (top-left, top-right, bottom-left, bottom-right of footprint in pixel coords).
    - `_strategy_perimeter(deco, fx, fy, rng)` → `count` evenly-spaced tuples along border (count from `deco['count']`).
    - `_strategy_random_border(deco, fx, fy, rng)` → `count` tuples via `rng.choice` over enumerated border pixel coords.
    - `_strategy_grid(deco, fx, fy, rng)` → `rows * cols` tuples at evenly-spaced grid positions (from `deco['rows']`, `deco['cols']`).
    - `_strategy_centered_front(deco, fx, fy, rng)` → 1 tuple at centered front-edge pixel.
    - `_strategy_centered_back(deco, fx, fy, rng)` → 1 tuple at centered back-edge pixel.
    - `_strategy_explicit(deco, fx, fy, rng)` → tuples from `deco['coords']` list of `[x, y]` pairs.
  - Each helper returns `list[tuple[str, int, int, dict]]` using `deco['primitive']` as name and `deco.get('kwargs', {})` as kwargs dict.
  - Flatten helper results into a single list and return.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/placement.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/placement.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Global-RNG leak (uses `random.randint` instead of `_random_mod.Random(seed + i)`) → re-open Step 1. Strategy tuple drift → re-open to canonical `_STRATEGIES`.

**MCP hints:** `plan_digest_verify_paths`, `glossary_lookup`.

## Open Questions (resolve before / during implementation)

None — placement semantics fully specified in master plan Stage 7.
