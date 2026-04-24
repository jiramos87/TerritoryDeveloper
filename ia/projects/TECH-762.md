---
purpose: "TECH-762 — `iso_tree_fir` primitive."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.1"
---
# TECH-762 — `iso_tree_fir` primitive

> **Issue:** [TECH-762](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Ship `iso_tree_fir` primitive — first vegetation primitive of the DAS R9 set. Stacked green domes on dark-green shadow base; scale parameter drives overall footprint; palette key `tree_fir` (3-level ramp).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_tree_fir(canvas, x0, y0, scale, variant, palette)` draws 2–3 stacked green domes on dark shadow base.
2. `scale ∈ [0.5, 1.5]` controls dome cluster footprint; pixel positions snap to integer coords.
3. Palette key `tree_fir` resolves 3 ramp levels (bright/mid/dark) from the active palette.

### 2.2 Non-Goals (Out of Scope)

1. Animation or multi-frame variants (static, single frame v1).
2. Outline pass (DAS §6 deferred to Stage 12).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Compose a residential scene with trees | Sprite renders without exception; tree silhouette matches visual target within ±5 px |

## 4. Current State

### 4.1 Domain behavior

No `iso_tree_fir` exists yet. DAS §3 visual targets: `House1-64.png` + `Forest1-64.png` show reference tree styles.

### 4.2 Systems map

New file `tools/sprite-gen/src/primitives/iso_tree_fir.py`; re-exported from `tools/sprite-gen/src/primitives/__init__.py`. Consumer: composer dispatch (T7.8 / TECH-769); test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770). Visual references: `Assets/Sprites/House1-64.png`, `Assets/Sprites/Forest1-64.png` per DAS §3.

## 5. Proposed Design

### 5.1 Target behavior (product)

Draw 2–3 stacked green domes on a dark-green shadow base. Scale parameter scales the entire cluster. Palette key `tree_fir` provides 3 ramp levels (bright / mid / dark).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Pure function signature: `iso_tree_fir(canvas, x0, y0, scale=1.0, variant=0, palette, **kwargs)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | 2–3 domes stacked | Matches visual reference density | 1-dome (too sparse), 4+ domes (too busy) |

## 7. Implementation Plan

### Phase 1 — Primitive signature + palette ramp wiring

- [ ] Define `iso_tree_fir` function signature
- [ ] Wire palette key lookup for `tree_fir` ramp levels

### Phase 2 — Dome cluster geometry (2–3 domes, scale-driven layout)

- [ ] Implement stacked dome circles at correct positions
- [ ] Apply scale parameter to control footprint

### Phase 3 — Dark-green shadow base + smoke render check under default palette

- [ ] Draw shadow base polygon
- [ ] Test render under residential palette

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Primitive renders without exception on residential palette | Smoke render | `tests/test_decorations_vegetation.py` (T7.9a) | Part of vegetation batch test |
| Output bbox is non-empty | Assertion | `tests/test_decorations_vegetation.py` | Validates canvas write |

## 8. Acceptance Criteria

- [ ] `iso_tree_fir(canvas, x0, y0, scale, variant, palette)` draws 2–3 stacked green domes on dark shadow base.
- [ ] `scale ∈ [0.5, 1.5]` controls dome cluster footprint; pixel positions snap to integer coords.
- [ ] Palette key `tree_fir` resolves 3 ramp levels (bright/mid/dark) from the active palette.

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

Ship `iso_tree_fir` primitive — stacked-dome vegetation sprite with palette-driven 3-level ramp. Enables residential/suburban compositions via DAS R9.

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_tree_fir.py` exports `iso_tree_fir(canvas, x0, y0, *, scale=1.0, variant=0, palette, **kwargs)`.
- [ ] Re-exported from `tools/sprite-gen/src/primitives/__init__.py` (`__all__` + import).
- [ ] `tools/sprite-gen/palettes/residential.json` `materials.tree_fir` entry added (bright/mid/dark RGB triples).
- [ ] `scale` outside `[0.5, 1.5]` raises `ValueError` with canonical range in message.
- [ ] Dome-count ladder: `scale < 0.75` → 2 domes; `scale ≥ 0.75` → 3 domes.
- [ ] Dark-green shadow-base ellipse drawn under dome cluster.
- [ ] Smoke render under residential palette produces non-empty bbox.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_tree_fir_smoke_residential` | canvas 64×64, `scale=1.0`, residential palette | non-empty bbox; no exception | pytest |
| `test_iso_tree_fir_scale_bounds` | `scale ∈ {0.3, 1.7}` | `ValueError` with canonical range | pytest |
| `test_iso_tree_fir_palette_ramp_3_levels` | residential palette | rendered pixels drawn from bright/mid/dark of `tree_fir` ramp | pytest |
| `test_iso_tree_fir_dome_count_ladder` | `scale=0.5` vs `scale=1.0` | 2 domes vs 3 domes via vertical slice sweep | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_tree_fir(c, 32, 32, scale=1.0, variant=0, palette=res)` | 3 stacked domes; widest ~8 px; dark-green shadow ellipse below | Baseline |
| `iso_tree_fir(c, 32, 32, scale=0.5, variant=0, palette=res)` | 2 stacked domes; widest ~4 px | Lower bound |
| `iso_tree_fir(c, 32, 32, scale=1.5, variant=0, palette=res)` | 3 stacked domes; widest ~12 px | Upper bound |
| `iso_tree_fir(c, 32, 32, scale=0.3, variant=0, palette=res)` | `ValueError` "scale must be in [0.5, 1.5]" | Out-of-range |

### §Mechanical Steps

#### Step 1 — Create `iso_tree_fir` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_tree_fir.py` with pure-function signature, scale validator, dome-count ladder, shadow-base ellipse, palette-ramp wiring.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_tree_fir.py` — **operation**: create; **after** — new file exporting `iso_tree_fir(canvas, x0, y0, *, scale=1.0, variant=0, palette, **kwargs)` pure function. Body:
  - Validate `0.5 <= scale <= 1.5` else `raise ValueError("scale must be in [0.5, 1.5]")`.
  - Resolve ramp via `palette["materials"]["tree_fir"]` (keys `bright`, `mid`, `dark`); raise `PaletteKeyError` (imported from `..palette`) on missing key.
  - Domes: `3 if scale >= 0.75 else 2`; widest dome width = `int(round(8 * scale))`; centred horizontally at `x0`; stacked vertically with `int(round(2 * scale))` px overlap.
  - Draw each dome as filled half-ellipse (bright top, mid sides) using `canvas.putpixel`.
  - Draw shadow ellipse below lowest dome using `dark` colour; integer-snap coordinates.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_tree_fir.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_tree_fir.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing after write → re-run Step 1 (Write tool). File present but signature drift → re-open Step 1 with correct signature literal.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_verify_paths`, `glossary_lookup` (for `primitive` / `palette` semantics).

#### Step 2 — Add `tree_fir` entry to residential palette

**Goal:** Insert `materials.tree_fir` ramp (bright/mid/dark) into `residential.json` directly above the existing `mustard_industrial` entry.

**Edits:**
- `tools/sprite-gen/palettes/residential.json` — **before**:
  ```
      "mustard_industrial": {
  ```
  **after**:
  ```
      "tree_fir": {
        "bright": [86, 156, 74],
        "mid": [58, 118, 56],
        "dark": [30, 72, 34]
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

**STOP:** JSON parse fail → re-open Step 2 (check trailing commas). `validate:all` red on unrelated rule → escalate to chain (do NOT silently patch).

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

#### Step 3 — Re-export `iso_tree_fir` from primitives `__init__`

**Goal:** Add `iso_tree_fir` to module exports + `__all__` list.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_stepped_foundation import iso_stepped_foundation
  ```
  **after**:
  ```
  from .iso_stepped_foundation import iso_stepped_foundation
  from .iso_tree_fir import iso_tree_fir
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_stepped_foundation",
  ]
  ```
  **after**:
  ```
      "iso_stepped_foundation",
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

**STOP:** Import fails at test collection → re-open Step 1 (module shape drift).

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_verify_paths`.

## Open Questions (resolve before / during implementation)

None — primitive design fully specified in master plan Stage 7.
