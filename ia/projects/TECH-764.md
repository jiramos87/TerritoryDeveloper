---
purpose: "TECH-764 — `iso_bush` + `iso_grass_tuft` primitives."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/sprite-gen-master-plan.md"
task_key: "T7.3"
---
# TECH-764 — `iso_bush` + `iso_grass_tuft` primitives

> **Issue:** [TECH-764](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Two low-profile vegetation primitives. `iso_bush` = ~6×6 px green puff; `iso_grass_tuft` = single-pixel green accents scattered at the anchor. Both palette-driven, scale-aware.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `iso_bush` renders a ~6×6 px green puff with 2-level internal ramp under palette key `bush`.
2. `iso_grass_tuft` renders 1–3 single-pixel accents under palette key `grass_tuft`.
3. Both primitives honour `scale` kwarg and variant seed.

### 2.2 Non-Goals (Out of Scope)

1. Complex geometry (stay pixel-minimal).
2. Outline pass (deferred to Stage 12).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Add fine detail to yard scenes | Bush + grass accents render correctly without exceptions |

## 4. Current State

### 4.1 Domain behavior

No `iso_bush` or `iso_grass_tuft` exist yet. Visual targets in DAS §3 show small green accents in yard compositions.

### 4.2 Systems map

New files `tools/sprite-gen/src/primitives/iso_bush.py` + `iso_grass_tuft.py`; both re-exported from `primitives/__init__.py`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).

## 5. Proposed Design

### 5.1 Target behavior (product)

`iso_bush`: Small green puff (~6×6 px footprint) with 2-level ramp for depth.
`iso_grass_tuft`: 1–3 single-pixel green accents scattered pseudo-randomly per variant seed.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Two pure functions: `iso_bush(canvas, x0, y0, scale, variant, palette, **kwargs)` and `iso_grass_tuft(canvas, x0, y0, scale, variant, palette, **kwargs)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Colocate in same task | Both are minimal, palette-driven accents | Separate tasks (overkill for simple primitives) |

## 7. Implementation Plan

### Phase 1 — `iso_bush` puff geometry + palette wiring

- [ ] Draw ~6×6 px ellipse or puff shape
- [ ] Wire palette key `bush`

### Phase 2 — `iso_grass_tuft` pixel-accent drawer

- [ ] Implement 1–3 single-pixel accent scatter
- [ ] Wire palette key `grass_tuft`

### Phase 3 — Re-export both; smoke-render under residential palette

- [ ] Add to `__init__.py`
- [ ] Smoke test both primitives

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Both render without exception on residential palette | Smoke render | `tests/test_decorations_vegetation.py` | Part of vegetation batch test |
| Both honor scale parameter | Assertion | Developer test | Scale affects output size |

## 8. Acceptance Criteria

- [ ] `iso_bush` renders a ~6×6 px green puff with 2-level internal ramp under palette key `bush`.
- [ ] `iso_grass_tuft` renders 1–3 single-pixel accents under palette key `grass_tuft`.
- [ ] Both primitives honour `scale` kwarg and variant seed.

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

Ship `iso_bush` (low green puff) + `iso_grass_tuft` (1-pixel accents) primitives. Bush uses 2-level ramp; tuft uses `random.Random(variant)` for deterministic pixel scatter.

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_bush.py` exports `iso_bush(canvas, x0, y0, *, scale=1.0, variant=0, palette, **kwargs)`.
- [ ] `tools/sprite-gen/src/primitives/iso_grass_tuft.py` exports `iso_grass_tuft(canvas, x0, y0, *, scale=1.0, variant=0, palette, **kwargs)`.
- [ ] Both re-exported from `primitives/__init__.py`.
- [ ] `residential.json` adds `materials.bush.{bright|mid}` + `materials.grass_tuft.{bright}`.
- [ ] `iso_bush` draws ~6×6 px puff using 2-level ramp; scale maps geometric.
- [ ] `iso_grass_tuft` draws 1–3 single-pixel accents; `random.Random(variant)` seeds local RNG only (never global).
- [ ] Tuft count = `max(1, min(3, round(2 * scale)))` per scale.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_bush_smoke_residential` | `scale=1.0`, residential palette | non-empty bbox ~6×6 px; bright + mid present | pytest |
| `test_iso_grass_tuft_deterministic_per_variant` | `variant=7` run twice | identical pixel-coord set both runs | pytest |
| `test_iso_grass_tuft_accent_count_bounded` | `variant ∈ range(20)` | each run writes 1–3 pixels | pytest |
| `test_iso_bush_scale_affects_size` | `scale=0.5` vs `scale=1.0` bbox | bbox area smaller at 0.5 | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_bush(c, 32, 32, scale=1.0, palette=res)` | ~6×6 px green puff; 2-level ramp | Baseline |
| `iso_bush(c, 32, 32, scale=0.5, palette=res)` | ~3×3 px puff | Scaled |
| `iso_grass_tuft(c, 32, 32, variant=0, palette=res)` | 2 single-pixel accents at deterministic coords | Seeded |
| `iso_grass_tuft(c, 32, 32, variant=42, palette=res)` | Different coord pair; 1–3 accents | Re-seed |

### §Mechanical Steps

#### Step 1 — Create `iso_bush` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_bush.py` — green puff sprite with 2-level internal ramp.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_bush.py` — **operation**: create; **after** — new file exporting `iso_bush(canvas, x0, y0, *, scale=1.0, variant=0, palette, **kwargs)`. Body:
  - Resolve ramp: `palette["materials"]["bush"]["bright"]` + `["mid"]` (raise `PaletteKeyError` on missing).
  - Puff size: width = `int(round(6 * scale))`, height = `int(round(6 * scale))`; minimum 1×1.
  - Draw filled ellipse centred at `(x0, y0)` with `mid` body + `bright` top arc (top 40% of ellipse height).
  - Integer-snap coords.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_bush.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_bush.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1.

**MCP hints:** `plan_digest_verify_paths`.

#### Step 2 — Create `iso_grass_tuft` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_grass_tuft.py` — deterministic single-pixel accent scatter seeded by `variant`.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_grass_tuft.py` — **operation**: create; **after** — new file exporting `iso_grass_tuft(canvas, x0, y0, *, scale=1.0, variant=0, palette, **kwargs)`. Body:
  - Import `random as _random_mod` (follow `iso_ground_noise.py` pattern).
  - Resolve colour: `palette["materials"]["grass_tuft"]["bright"]`.
  - `count = max(1, min(3, int(round(2 * scale))))`.
  - `rng = _random_mod.Random(int(variant))`.
  - For `count` iterations: `dx = rng.randint(-2, 2); dy = rng.randint(-1, 1); canvas.putpixel((x0 + dx, y0 + dy), tuple(colour) + (255,))`.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_grass_tuft.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_grass_tuft.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 2. Global-RNG leak (uses `random.randint` instead of local RNG) → re-open Step 2 to use `_random_mod.Random(int(variant))`.

**MCP hints:** `plan_digest_verify_paths`, `glossary_lookup`.

#### Step 3 — Add `bush` + `grass_tuft` palette entries

**Goal:** Insert `materials.bush` (bright/mid) + `materials.grass_tuft` (bright) blocks above `mustard_industrial` in residential palette.

**Edits:**
- `tools/sprite-gen/palettes/residential.json` — **before**:
  ```
      "mustard_industrial": {
  ```
  **after**:
  ```
      "bush": {
        "bright": [118, 180, 82],
        "mid": [76, 130, 58]
      },
      "grass_tuft": {
        "bright": [148, 210, 96]
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

**STOP:** JSON parse fail → re-open Step 3 (trailing commas).

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

#### Step 4 — Re-export `iso_bush` + `iso_grass_tuft` from primitives `__init__`

**Goal:** Register both primitives in package.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_cube import iso_cube
  from .iso_ground_diamond import iso_ground_diamond
  ```
  **after**:
  ```
  from .iso_bush import iso_bush
  from .iso_cube import iso_cube
  from .iso_grass_tuft import iso_grass_tuft
  from .iso_ground_diamond import iso_ground_diamond
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  __all__ = [
      "iso_cube",
  ```
  **after**:
  ```
  __all__ = [
      "iso_bush",
      "iso_cube",
      "iso_grass_tuft",
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Import error → re-open Step 1 or Step 2 (module shape drift).

**MCP hints:** `plan_digest_resolve_anchor`.

## Open Questions (resolve before / during implementation)

None — primitive design fully specified in master plan Stage 7.
