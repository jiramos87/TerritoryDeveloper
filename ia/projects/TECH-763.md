---
purpose: "TECH-763 — `iso_tree_deciduous` primitive."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.2"
---
# TECH-763 — `iso_tree_deciduous` primitive

> **Issue:** [TECH-763](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Ship `iso_tree_deciduous` primitive — round-crown counterpart to the fir. `color_var` kwarg selects one of three ramp variants under palette key `tree_deciduous`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_tree_deciduous(canvas, x0, y0, scale, variant, palette, color_var=…)` draws round-crown tree.
2. `color_var ∈ {green, green_yellow, green_blue}` picks three distinct ramps from `tree_deciduous`.
3. Invalid `color_var` raises `ValueError` with canonical list in message.

### 2.2 Non-Goals (Out of Scope)

1. Animation or seasonal variants (static v1).
2. Outline pass (deferred to Stage 12).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Vary tree colors in a forest scene | Correct `color_var` selects the intended ramp; invalid value raises clear error |

## 4. Current State

### 4.1 Domain behavior

No `iso_tree_deciduous` exists yet. DAS §3 describes round-crown silhouette and three color variants.

### 4.2 Systems map

New file `tools/sprite-gen/src/primitives/iso_tree_deciduous.py`; re-exported from `primitives/__init__.py`. Palette consumer: `palettes/*.json` entries under key `tree_deciduous`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).

## 5. Proposed Design

### 5.1 Target behavior (product)

Draw round-crown tree with three distinct color options via `color_var` kwarg. Palette key `tree_deciduous` provides three ramp sets for green/green_yellow/green_blue variants.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Pure function signature: `iso_tree_deciduous(canvas, x0, y0, scale=1.0, variant=0, palette, color_var='green', **kwargs)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Three color variants | DAS R9 design variety requirement | 1 color (limiting), 5+ colors (palette bloat) |

## 7. Implementation Plan

### Phase 1 — Primitive signature + `color_var` validation

- [ ] Define signature with `color_var` parameter
- [ ] Add validation enum for {green, green_yellow, green_blue}

### Phase 2 — Round-crown ellipse geometry + trunk base

- [ ] Draw crown ellipse
- [ ] Draw trunk base

### Phase 3 — Wire `color_var` to ramp selection under palette key `tree_deciduous`

- [ ] Map `color_var` to correct ramp index
- [ ] Apply colors to crown and trunk

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Primitive renders without exception on all three `color_var` values | Smoke render | `tests/test_decorations_vegetation.py` | Part of vegetation batch test |
| Invalid `color_var` raises `ValueError` | Unit test | Developer test | Covers error path |

## 8. Acceptance Criteria

- [ ] `iso_tree_deciduous(canvas, x0, y0, scale, variant, palette, color_var=…)` draws round-crown tree.
- [ ] `color_var ∈ {green, green_yellow, green_blue}` picks three distinct ramps from `tree_deciduous`.
- [ ] Invalid `color_var` raises `ValueError` with canonical list in message.

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

Ship `iso_tree_deciduous` — round-crown counterpart to `iso_tree_fir`. `color_var` kwarg selects one of three nested ramps (`green` / `green_yellow` / `green_blue`) under palette key `tree_deciduous`.

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_tree_deciduous.py` exports `iso_tree_deciduous(canvas, x0, y0, *, scale=1.0, variant=0, palette, color_var='green', **kwargs)`.
- [ ] Re-exported from `primitives/__init__.py`.
- [ ] `residential.json` adds `materials.tree_deciduous.{green|green_yellow|green_blue}.{bright|mid|dark}` nested ramps.
- [ ] Invalid `color_var` raises `ValueError` with canonical set in message.
- [ ] Round-crown ellipse + trunk base render; trunk centred below crown.
- [ ] Three variants produce distinct dominant colours (pairwise histogram diff non-zero).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_tree_deciduous_smoke_all_variants` | 3 `color_var` values, residential palette | 3 renders; non-empty bbox each | pytest |
| `test_iso_tree_deciduous_invalid_color_var` | `color_var='purple'` | `ValueError` with canonical list | pytest |
| `test_iso_tree_deciduous_variant_ramp_distinct` | pairwise pixel diff `green` vs `green_yellow` | dominant colour differs per variant | pytest |
| `test_iso_tree_deciduous_default_color_var` | omit `color_var` | output identical to `color_var='green'` | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_tree_deciduous(c, 32, 32, scale=1.0, color_var='green')` | Round crown ~10 px wide; green ramp; trunk below | Default |
| `iso_tree_deciduous(c, 32, 32, scale=1.0, color_var='green_yellow')` | Same geometry; yellow-green ramp | Variant |
| `iso_tree_deciduous(c, 32, 32, scale=1.0, color_var='green_blue')` | Same geometry; blue-green ramp | Variant |
| `iso_tree_deciduous(c, 32, 32, color_var='red')` | `ValueError` "color_var must be in {green, green_yellow, green_blue}" | Invalid |

### §Mechanical Steps

#### Step 1 — Create `iso_tree_deciduous` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_tree_deciduous.py` — pure function with `color_var` validator, crown ellipse + trunk geometry, nested-ramp palette resolver.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_tree_deciduous.py` — **operation**: create; **after** — new file exporting `iso_tree_deciduous(canvas, x0, y0, *, scale=1.0, variant=0, palette, color_var='green', **kwargs)`. Body:
  - `_COLOR_VARS = ('green', 'green_yellow', 'green_blue')`; validate `color_var` in set else `raise ValueError(f"color_var must be in {set(_COLOR_VARS)}")`.
  - Validate `0.5 <= scale <= 1.5` else `raise ValueError("scale must be in [0.5, 1.5]")`.
  - Resolve ramp: `palette["materials"]["tree_deciduous"][color_var]` (raise `PaletteKeyError` on missing).
  - Crown: filled ellipse width `int(round(10 * scale))`, height `int(round(8 * scale))`, centred at `(x0, y0)`; bright top band + mid body + dark rim (1 px).
  - Trunk: 1×2 dark rectangle centred under crown.
  - Integer-snap all coords.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_tree_deciduous.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_tree_deciduous.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Enum drift → re-open with canonical `_COLOR_VARS` tuple literal.

**MCP hints:** `plan_digest_verify_paths`, `glossary_lookup`.

#### Step 2 — Add `tree_deciduous` nested ramps to palette

**Goal:** Insert `materials.tree_deciduous` nested block (3 variants × 3 levels) above `mustard_industrial` in residential palette.

**Edits:**
- `tools/sprite-gen/palettes/residential.json` — **before**:
  ```
      "mustard_industrial": {
  ```
  **after**:
  ```
      "tree_deciduous": {
        "green": {
          "bright": [120, 196, 96],
          "mid": [84, 150, 70],
          "dark": [44, 90, 42]
        },
        "green_yellow": {
          "bright": [180, 214, 88],
          "mid": [140, 172, 62],
          "dark": [82, 110, 34]
        },
        "green_blue": {
          "bright": [96, 178, 158],
          "mid": [62, 130, 124],
          "dark": [28, 72, 76]
        }
      },
      "mustard_industrial": {
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** JSON parse fail → re-open Step 2 (trailing commas). `validate:all` red unrelated → escalate.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

#### Step 3 — Re-export `iso_tree_deciduous` from primitives `__init__`

**Goal:** Register `iso_tree_deciduous` in primitives package.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_tree_fir import iso_tree_fir
  ```
  **after**:
  ```
  from .iso_tree_deciduous import iso_tree_deciduous
  from .iso_tree_fir import iso_tree_fir
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_tree_fir",
  ]
  ```
  **after**:
  ```
      "iso_tree_deciduous",
      "iso_tree_fir",
  ]
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Collection import red → re-open Step 1 (signature drift).

**MCP hints:** `plan_digest_resolve_anchor`.

## Open Questions (resolve before / during implementation)

None — primitive design fully specified in master plan Stage 7.
