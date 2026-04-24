---
purpose: "TECH-769 — Composer `decorations:` integration."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.8"
---
# TECH-769 — Composer `decorations:` integration

> **Issue:** [TECH-769](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Wire `spec.decorations` into `compose_sprite`. Dispatch each placed primitive in correct z-order. Hard-gate `iso_pool` on 1×1 footprints via `DecorationScopeError`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `compose_sprite` reads `spec.decorations: list[...]`; iterates `placement.place(...)` output.
2. Z-order enforced: ground diamond → yard decorations → building → roof decorations.
3. Footprint-scope gate raises `DecorationScopeError` when a 1×1 spec includes `iso_pool`.

### 2.2 Non-Goals (Out of Scope)

1. Collision detection.
2. Dynamic placement at load time (spec-driven placement only v1).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Compose archetype with yard decorations | Decorations render in correct z-order; 1×1 + pool raises error |

## 4. Current State

### 4.1 Domain behavior

Composer exists but does not read `spec.decorations`. Placement engine (T7.7 / TECH-768) not yet integrated.

### 4.2 Systems map

Modify `tools/sprite-gen/src/compose.py` — consume `placement.place` from T7.7 / TECH-768. Dispatch table maps primitive names → primitive callables from `primitives/` (T7.1–T7.6). New exception `DecorationScopeError` in `compose.py` (or `exceptions.py`). Test surface: `tests/test_placement.py` (T7.9b / TECH-771).

## 5. Proposed Design

### 5.1 Target behavior (product)

Read `spec.decorations` as list of decoration specs. For each, call `placement.place(...)` to get pixel coords. Dispatch each result to the corresponding primitive function. Render in z-order: ground → yard → building → roof. Reject 1×1 footprints with `iso_pool` at validation time.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Modify `compose_sprite()` to:
1. Check footprint + `iso_pool` presence early; raise `DecorationScopeError` if invalid.
2. Call `placement.place(spec.decorations, footprint, seed)`.
3. Iterate results and dispatch to primitive callables.
4. Render in z-order (ground before building before roof).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Early 1×1 + pool gate | Fail fast with clear error | Late detection (harder to debug) |
| 2026-04-24 | Z-order in compose | Single point of control | Scattered across primitives (fragile) |

## 7. Implementation Plan

### Phase 1 — Spec field read + `placement.place` call

- [ ] Add `decorations:` field read to spec schema
- [ ] Wire `placement.place(...)` call with footprint + seed

### Phase 2 — Z-order dispatch for ground / yard / building / roof layers

- [ ] Build primitive dispatch table
- [ ] Implement z-order rendering loop

### Phase 3 — `DecorationScopeError` guard for 1×1 + `iso_pool` combo

- [ ] Define exception class
- [ ] Add early validation gate

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Decorations render in correct z-order | Regression test | `tests/test_placement.py` (T7.9b) | Visual ordering verification |
| 1×1 + pool raises `DecorationScopeError` | Unit test | `tests/test_placement.py` | Validates gate |

## 8. Acceptance Criteria

- [ ] `compose_sprite` reads `spec.decorations: list[...]`; iterates `placement.place(...)` output.
- [ ] Z-order enforced: ground diamond → yard decorations → building → roof decorations.
- [ ] Footprint-scope gate raises `DecorationScopeError` when a 1×1 spec includes `iso_pool`.

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

Wire `spec['decorations']` into existing `compose_sprite` (at `tools/sprite-gen/src/compose.py:259`). Add `DecorationScopeError` inline near `UnknownPrimitiveError` (line 69) — `tools/sprite-gen/src/exceptions.py` does not exist. Dispatch decoration primitives in z-order ground → yard → building. Raise `DecorationScopeError` on 1×1 + `iso_pool` combo BEFORE render.

### §Acceptance

- [ ] `class DecorationScopeError(ValueError)` defined in `tools/sprite-gen/src/compose.py` adjacent to existing `UnknownPrimitiveError`.
- [ ] `compose_sprite` reads `spec.get('decorations', [])` — absent key renders unchanged (backward compat).
- [ ] 1×1 + `iso_pool` scope gate fires before any render (fail-fast) — raises `DecorationScopeError("iso_pool requires footprint >= 2x2")`.
- [ ] `compose_sprite` calls `place(decorations, footprint, seed)` from `tools/sprite-gen/src/placement.py` (imported).
- [ ] `_DECORATION_DISPATCH` dict maps `{iso_tree_fir, iso_tree_deciduous, iso_bush, iso_grass_tuft, iso_pool, iso_path, iso_pavement_patch, iso_fence}` → primitive callables imported from `primitives/`.
- [ ] Unknown decoration primitive raises existing `UnknownPrimitiveError`.
- [ ] Z-order preserved: ground diamond → yard decorations → building composition (roof-deco deferred to Stage 8).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_compose_sprite_decorations_absent_backward_compat` | spec without `decorations:` key | sprite renders unchanged vs pre-T7.8 baseline | pytest |
| `test_compose_sprite_decorations_empty_list` | `decorations: []` | sprite renders unchanged | pytest |
| `test_compose_sprite_decoration_placed_corners` | 2×2 + `iso_tree_fir` corners | 4 trees at corner pixel coords | pytest |
| `test_compose_sprite_z_order_ground_yard_building` | ground + yard-deco + building | pixel-inspect: building over yard; yard over ground | pytest |
| `test_compose_sprite_scope_error_1x1_pool` | 1×1 + `iso_pool` | `DecorationScopeError` raised before any render | pytest |
| `test_compose_sprite_scope_ok_2x2_pool` | 2×2 + `iso_pool` | pool renders; no exception | pytest |
| `test_compose_sprite_unknown_decoration_primitive` | `decorations: [{'primitive': 'iso_mystery'}]` | `UnknownPrimitiveError` | pytest |

### §Examples

| Input spec | Expected output | Notes |
|-----------|-----------------|-------|
| `{'footprint': [2, 2], 'decorations': [{'primitive': 'iso_tree_fir', 'strategy': 'corners'}], ...}` | 4 trees at corners; z-order ground→trees→building | Baseline |
| `{'footprint': [2, 2], 'decorations': [], ...}` | Sprite unchanged | Empty list |
| `{'footprint': [2, 2], ...}` (no `decorations` key) | Sprite unchanged | Default |
| `{'footprint': [1, 1], 'decorations': [{'primitive': 'iso_pool'}], ...}` | `DecorationScopeError` | Scope gate |
| `{'footprint': [2, 2], 'decorations': [{'primitive': 'iso_pool', 'strategy': 'centered_front'}], ...}` | Pool rendered | Valid |

### §Mechanical Steps

#### Step 1 — Add `DecorationScopeError` class adjacent to `UnknownPrimitiveError`

**Goal:** Inline new exception alongside existing error classes in `compose.py`.

**Edits:**
- `tools/sprite-gen/src/compose.py` — **before**:
  ```
  class UnknownPrimitiveError(ValueError):
  ```
  **after**:
  ```
  class DecorationScopeError(ValueError):
      """Raised when decoration primitive exceeds footprint scope (e.g. iso_pool on 1x1)."""


  class UnknownPrimitiveError(ValueError):
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `cd tools/sprite-gen && python -c "from src.compose import DecorationScopeError; print('OK')"`

**Gate:**
```bash
cd tools/sprite-gen && python -c "from src.compose import DecorationScopeError; print('OK')"
```
Expectation: prints `OK`.

**STOP:** Import fail → re-open Step 1 (class body malformed).

**MCP hints:** `plan_digest_resolve_anchor`.

#### Step 2 — Import `place` + primitive callables; build `_DECORATION_DISPATCH` table

**Goal:** Add top-level imports for placement engine + 8 decoration primitives and module-level dispatch dict in `compose.py`.

**Edits:**
- `tools/sprite-gen/src/compose.py` — **before**:
  ```
  class DecorationScopeError(ValueError):
  ```
  **after**:
  ```
  from .placement import place as _place_decorations
  from .primitives import (
      iso_bush,
      iso_fence,
      iso_grass_tuft,
      iso_path,
      iso_pavement_patch,
      iso_pool,
      iso_tree_deciduous,
      iso_tree_fir,
  )


  _DECORATION_DISPATCH = {
      "iso_bush": iso_bush,
      "iso_fence": iso_fence,
      "iso_grass_tuft": iso_grass_tuft,
      "iso_path": iso_path,
      "iso_pavement_patch": iso_pavement_patch,
      "iso_pool": iso_pool,
      "iso_tree_deciduous": iso_tree_deciduous,
      "iso_tree_fir": iso_tree_fir,
  }


  class DecorationScopeError(ValueError):
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `cd tools/sprite-gen && python -c "from src.compose import _DECORATION_DISPATCH; assert set(_DECORATION_DISPATCH) == {'iso_bush','iso_fence','iso_grass_tuft','iso_path','iso_pavement_patch','iso_pool','iso_tree_deciduous','iso_tree_fir'}; print('OK')"`

**Gate:**
```bash
cd tools/sprite-gen && python -c "from src.compose import _DECORATION_DISPATCH; assert set(_DECORATION_DISPATCH) == {'iso_bush','iso_fence','iso_grass_tuft','iso_path','iso_pavement_patch','iso_pool','iso_tree_deciduous','iso_tree_fir'}; print('OK')"
```
Expectation: prints `OK`.

**STOP:** Import fail — missing primitive module → re-open originating Task (TECH-762..TECH-767). Dispatch key drift → re-open Step 2.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_verify_paths`.

#### Step 3 — Wire `decorations` read + scope gate + dispatch loop inside `compose_sprite`

**Goal:** Extend `compose_sprite(spec)` body to (a) read decorations with default `[]`, (b) run 1×1 + `iso_pool` scope gate, (c) call `_place_decorations`, (d) dispatch each placed entry to `_DECORATION_DISPATCH`, (e) render yard-deco layer AFTER ground diamond and BEFORE building layer.

**Edits:**
- `tools/sprite-gen/src/compose.py` — **before**:
  ```
  def compose_sprite(spec: dict) -> Image.Image:
  ```
  **after**:
  ```
  def _apply_decorations(canvas, spec: dict, palette: dict) -> None:
      """Dispatch spec['decorations'] onto canvas via _DECORATION_DISPATCH.

      Pre-condition: 1x1 + iso_pool scope gate already passed.
      Called between ground-diamond render and building composition.
      """
      decorations = spec.get("decorations", []) or []
      if not decorations:
          return
      footprint = tuple(spec.get("footprint", [1, 1]))
      seed = int(spec.get("seed", 0))
      placed = _place_decorations(decorations, footprint, seed)
      for primitive_name, x_px, y_px, kwargs in placed:
          fn = _DECORATION_DISPATCH.get(primitive_name)
          if fn is None:
              raise UnknownPrimitiveError(
                  f"Unknown decoration primitive: {primitive_name!r}"
              )
          fn(canvas, x_px, y_px, palette=palette, **kwargs)


  def _scope_gate_decorations(spec: dict) -> None:
      """Raise DecorationScopeError on 1x1 + iso_pool before any render pass."""
      footprint = tuple(spec.get("footprint", [1, 1]))
      decorations = spec.get("decorations", []) or []
      if footprint == (1, 1):
          for deco in decorations:
              if deco.get("primitive") == "iso_pool":
                  raise DecorationScopeError(
                      "iso_pool requires footprint >= 2x2"
                  )


  def compose_sprite(spec: dict) -> Image.Image:
  ```
- `tools/sprite-gen/src/compose.py` — **before**:
  ```
  def compose_sprite(spec: dict) -> Image.Image:
      """Compose a sprite from an archetype spec dict.
  ```
  **after**:
  ```
  def compose_sprite(spec: dict) -> Image.Image:
      """Compose a sprite from an archetype spec dict.

      Decoration pipeline (TECH-769): scope-gate spec['decorations'] at entry
      (raises DecorationScopeError on 1x1 + iso_pool), then apply via
      _apply_decorations between ground-diamond render and building pass.
  ```
- `tools/sprite-gen/src/compose.py` — **before**:
  ```
      fx, fy = spec["footprint"]
      composition = composition_entries(spec)
  ```
  **after**:
  ```
      _scope_gate_decorations(spec)
      fx, fy = spec["footprint"]
      composition = composition_entries(spec)
  ```
- `tools/sprite-gen/src/compose.py` — **before**:
  ```
      # --- Iterate composition in order (later entries on top) ---
      for entry in composition:
  ```
  **after**:
  ```
      _apply_decorations(canvas, spec, palette)

      # --- Iterate composition in order (later entries on top) ---
      for entry in composition:
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `cd tools/sprite-gen && python -c "import inspect; from src import compose; assert '_apply_decorations' in dir(compose) and '_scope_gate_decorations' in dir(compose); print('OK')"`

**Gate:**
```bash
cd tools/sprite-gen && python -c "import inspect; from src import compose; assert '_apply_decorations' in dir(compose) and '_scope_gate_decorations' in dir(compose); print('OK')"
```
Expectation: prints `OK`.

**STOP:** Helper symbols missing → re-open Step 3. Scope gate not invoked before render (test `test_compose_sprite_scope_error_1x1_pool` fails) → re-open Step 3 and move `_scope_gate_decorations(spec)` to function entry. Z-order regression (building renders under yard-deco) → re-open Step 3 and relocate `_apply_decorations` call between ground and building layers.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

## Open Questions (resolve before / during implementation)

None — integration pattern fully specified in master plan Stage 7.
