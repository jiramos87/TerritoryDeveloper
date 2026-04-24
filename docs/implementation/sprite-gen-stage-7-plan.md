# sprite-gen — Stage 7 Plan Digest

Compiled 2026-04-24 from 10 task spec(s).

---

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

---
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

---
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

---
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

Ship `iso_pool` primitive — light-blue rectangle with 1-px white rim, size-bound `[8, 20]` px. Primitive is footprint-agnostic; 1×1 scope gate lives in composer (TECH-769).

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_pool.py` exports `iso_pool(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`.
- [ ] Re-exported from `primitives/__init__.py`.
- [ ] `residential.json` `materials.pool` with `bright` / `mid` / `rim` keys added.
- [ ] `w_px` or `d_px` outside `[8, 20]` raises `ValueError` with canonical range.
- [ ] Light-blue filled rectangle rendered with 1-px white rim.
- [ ] Primitive contains no footprint knowledge (composer owns 1×1 gate per TECH-769).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_pool_smoke_residential` | `w_px=12, d_px=10` | non-empty bbox; light-blue + white pixels present | pytest |
| `test_iso_pool_bounds_low` | `w_px ∈ {7, 21}`, `d_px ∈ {7, 21}` | `ValueError` each | pytest |
| `test_iso_pool_bounds_inclusive` | `w_px=8, d_px=20` | no exception; rect rendered | pytest |
| `test_iso_pool_rim_is_1px_white` | render + inspect outermost ring | all ring pixels = `pool.rim` | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_pool(c, 32, 32, w_px=12, d_px=10, palette=res)` | 12×10 light-blue rect with 1-px white border | Baseline |
| `iso_pool(c, 32, 32, w_px=8, d_px=8, palette=res)` | Min-size 8×8 with rim | Lower bound |
| `iso_pool(c, 32, 32, w_px=20, d_px=20, palette=res)` | Max-size 20×20 with rim | Upper bound |
| `iso_pool(c, 32, 32, w_px=7, d_px=10, palette=res)` | `ValueError` "w_px must be in [8, 20]" | Invalid |
| `iso_pool(c, 32, 32, w_px=12, d_px=25, palette=res)` | `ValueError` "d_px must be in [8, 20]" | Invalid |

### §Mechanical Steps

#### Step 1 — Create `iso_pool` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_pool.py` — rectangle + rim renderer with explicit size bounds.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_pool.py` — **operation**: create; **after** — new file exporting `iso_pool(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`. Body:
  - Validate `8 <= w_px <= 20` else `raise ValueError("w_px must be in [8, 20]")`.
  - Validate `8 <= d_px <= 20` else `raise ValueError("d_px must be in [8, 20]")`.
  - Resolve `palette["materials"]["pool"]` (raise `PaletteKeyError` on missing).
  - `fill = tuple(ramp["bright"]) + (255,)`; `rim = tuple(ramp["rim"]) + (255,)`.
  - Fill inner rect: for `y in range(y0 + 1, y0 + d_px - 1)` and `x in range(x0 + 1, x0 + w_px - 1)` → `canvas.putpixel((x, y), fill)`.
  - Draw 1-px rim ring at `(x0, y0)`..`(x0 + w_px - 1, y0 + d_px - 1)` using `rim`.
  - Docstring notes: "Primitive is footprint-agnostic. 1×1 scope rejection lives in `compose_sprite` (TECH-769 / T7.8)."
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_pool.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_pool.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Scope-gate drift (primitive inspects footprint) → re-open Step 1 to remove footprint logic.

**MCP hints:** `plan_digest_verify_paths`, `backlog_issue` (for TECH-769 scope boundary cross-ref).

#### Step 2 — Add `pool` palette entry

**Goal:** Insert `materials.pool` with `bright`, `mid`, `rim` keys above `mustard_industrial`.

**Edits:**
- `tools/sprite-gen/palettes/residential.json` — **before**:
  ```
      "mustard_industrial": {
  ```
  **after**:
  ```
      "pool": {
        "bright": [112, 198, 232],
        "mid": [78, 158, 198],
        "rim": [248, 248, 248]
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

**STOP:** JSON parse fail → re-open Step 2 (trailing commas).

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

#### Step 3 — Re-export `iso_pool` from primitives `__init__`

**Goal:** Register `iso_pool` in primitives package alphabetically.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_prism import iso_prism
  ```
  **after**:
  ```
  from .iso_pool import iso_pool
  from .iso_prism import iso_prism
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_prism",
  ```
  **after**:
  ```
      "iso_pool",
      "iso_prism",
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Import fail → re-open Step 1.

**MCP hints:** `plan_digest_resolve_anchor`.

---
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

Ship `iso_path` (narrow directional walkway) + `iso_pavement_patch` (rect fill) primitives. Both reuse existing `materials.pavement` ramp — no palette edit required.

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_path.py` exports `iso_path(canvas, x0, y0, *, length_px, axis, palette, width_px=2, **kwargs)`.
- [ ] `tools/sprite-gen/src/primitives/iso_pavement_patch.py` exports `iso_pavement_patch(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`.
- [ ] Both re-exported from `primitives/__init__.py`.
- [ ] `axis` outside `{'ns', 'ew'}` raises `ValueError` with canonical set.
- [ ] `width_px` outside `[2, 4]` raises `ValueError` with canonical range.
- [ ] `w_px` or `d_px` `< 1` raises `ValueError` on `iso_pavement_patch`.
- [ ] Both primitives consume existing `palette["materials"]["pavement"]` — no new palette entry.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_path_smoke_both_axes` | `axis ∈ {'ns', 'ew'}`, residential palette | both render; non-empty bbox; pavement ramp pixels present | pytest |
| `test_iso_path_axis_invalid` | `axis='diag'` | `ValueError` with canonical set | pytest |
| `test_iso_path_width_bounds` | `width_px ∈ {1, 5}` | `ValueError` each | pytest |
| `test_iso_pavement_patch_smoke` | `w_px=10, d_px=10` | non-empty bbox; rect filled with pavement ramp | pytest |
| `test_iso_pavement_patch_size_positive` | `w_px=0` or `d_px=0` | `ValueError` | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_path(c, 32, 32, length_px=20, axis='ns', width_px=2, palette=res)` | 2×20 strip oriented NS | Baseline |
| `iso_path(c, 32, 32, length_px=20, axis='ew', width_px=4, palette=res)` | 20×4 strip oriented EW | Wide |
| `iso_path(c, 32, 32, length_px=20, axis='diag', width_px=2, palette=res)` | `ValueError` "axis must be in {'ns', 'ew'}" | Invalid |
| `iso_path(c, 32, 32, length_px=20, axis='ns', width_px=5, palette=res)` | `ValueError` "width_px must be in [2, 4]" | Invalid |
| `iso_pavement_patch(c, 32, 32, w_px=10, d_px=10, palette=res)` | 10×10 filled pavement rect | Baseline |
| `iso_pavement_patch(c, 32, 32, w_px=0, d_px=10, palette=res)` | `ValueError` "w_px must be >= 1" | Invalid |

### §Mechanical Steps

#### Step 1 — Create `iso_path` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_path.py` — strip renderer with axis enum + width bounds.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_path.py` — **operation**: create; **after** — new file exporting `iso_path(canvas, x0, y0, *, length_px, axis, palette, width_px=2, **kwargs)`. Body:
  - `_AXES = ('ns', 'ew')`; validate `axis in _AXES` else `raise ValueError(f"axis must be in {set(_AXES)}")`.
  - Validate `2 <= width_px <= 4` else `raise ValueError("width_px must be in [2, 4]")`.
  - Resolve `palette["materials"]["pavement"]["mid"]` (raise `PaletteKeyError` on missing).
  - If `axis == 'ns'`: draw rect `(x0, y0)` to `(x0 + width_px - 1, y0 + length_px - 1)`.
  - If `axis == 'ew'`: draw rect `(x0, y0)` to `(x0 + length_px - 1, y0 + width_px - 1)`.
  - Fill each pixel via `canvas.putpixel`.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_path.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_path.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Axis tuple drift → re-open to canonical `_AXES = ('ns', 'ew')`.

**MCP hints:** `plan_digest_verify_paths`.

#### Step 2 — Create `iso_pavement_patch` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_pavement_patch.py` — free-form rect fill.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_pavement_patch.py` — **operation**: create; **after** — new file exporting `iso_pavement_patch(canvas, x0, y0, *, w_px, d_px, palette, **kwargs)`. Body:
  - Validate `w_px >= 1` else `raise ValueError("w_px must be >= 1")`.
  - Validate `d_px >= 1` else `raise ValueError("d_px must be >= 1")`.
  - Resolve `palette["materials"]["pavement"]["mid"]`.
  - Fill rect `(x0, y0)` to `(x0 + w_px - 1, y0 + d_px - 1)` via `canvas.putpixel`.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_pavement_patch.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_pavement_patch.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 2.

**MCP hints:** `plan_digest_verify_paths`.

#### Step 3 — Re-export `iso_path` + `iso_pavement_patch` from primitives `__init__`

**Goal:** Register both primitives alphabetically in package.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_ground_noise import iso_ground_noise
  ```
  **after**:
  ```
  from .iso_ground_noise import iso_ground_noise
  from .iso_path import iso_path
  from .iso_pavement_patch import iso_pavement_patch
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_ground_noise",
  ```
  **after**:
  ```
      "iso_ground_noise",
      "iso_path",
      "iso_pavement_patch",
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Import fail → re-open Step 1 or Step 2.

**MCP hints:** `plan_digest_resolve_anchor`.

---
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

Ship `iso_fence` primitive — thin 1–2 px beige/tan line bordering one cardinal side of an anchor point. Palette key `fence`.

### §Acceptance

- [ ] `tools/sprite-gen/src/primitives/iso_fence.py` exports `iso_fence(canvas, x0, y0, *, length_px, side, palette, thickness_px=1, **kwargs)`.
- [ ] Re-exported from `primitives/__init__.py`.
- [ ] `residential.json` `materials.fence` with `bright` / `mid` keys added.
- [ ] `side` outside `{'n', 's', 'e', 'w'}` raises `ValueError` with canonical set.
- [ ] `thickness_px` outside `[1, 2]` raises `ValueError` with canonical range.
- [ ] Per-side geometry: `n` = horizontal north, `s` = horizontal south, `e` = vertical east, `w` = vertical west.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_iso_fence_smoke_all_sides` | `side ∈ {'n', 's', 'e', 'w'}`, residential palette | 4 renders; non-empty bbox; fence ramp pixels | pytest |
| `test_iso_fence_side_invalid` | `side='ne'` | `ValueError` with canonical set | pytest |
| `test_iso_fence_thickness_bounds` | `thickness_px ∈ {0, 3}` | `ValueError` each | pytest |
| `test_iso_fence_geometry_n_vs_s` | `side='n'` vs `side='s'` same anchor | y-coords differ (n above, s below) | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_fence(c, 32, 32, length_px=20, side='n', palette=res)` | 20×1 horizontal line north of anchor | Baseline |
| `iso_fence(c, 32, 32, length_px=20, side='e', thickness_px=2, palette=res)` | 2×20 vertical line east of anchor | Thick east |
| `iso_fence(c, 32, 32, length_px=20, side='ne', palette=res)` | `ValueError` "side must be in {'n', 's', 'e', 'w'}" | Invalid |
| `iso_fence(c, 32, 32, length_px=20, side='n', thickness_px=3, palette=res)` | `ValueError` "thickness_px must be in [1, 2]" | Invalid |

### §Mechanical Steps

#### Step 1 — Create `iso_fence` primitive module

**Goal:** Author `tools/sprite-gen/src/primitives/iso_fence.py` — cardinal-direction line renderer with thickness bounds.

**Edits:**
- `tools/sprite-gen/src/primitives/iso_fence.py` — **operation**: create; **after** — new file exporting `iso_fence(canvas, x0, y0, *, length_px, side, palette, thickness_px=1, **kwargs)`. Body:
  - `_SIDES = ('n', 's', 'e', 'w')`; validate `side in _SIDES` else `raise ValueError(f"side must be in {set(_SIDES)}")`.
  - Validate `1 <= thickness_px <= 2` else `raise ValueError("thickness_px must be in [1, 2]")`.
  - Resolve `palette["materials"]["fence"]["bright"]`.
  - Geometry per side:
    - `n`: rect `(x0, y0 - thickness_px)` → `(x0 + length_px - 1, y0 - 1)`.
    - `s`: rect `(x0, y0 + 1)` → `(x0 + length_px - 1, y0 + thickness_px)`.
    - `e`: rect `(x0 + 1, y0)` → `(x0 + thickness_px, y0 + length_px - 1)`.
    - `w`: rect `(x0 - thickness_px, y0)` → `(x0 - 1, y0 + length_px - 1)`.
  - Fill via `canvas.putpixel`.
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `test -f tools/sprite-gen/src/primitives/iso_fence.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/src/primitives/iso_fence.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Side tuple drift → re-open to canonical `_SIDES = ('n', 's', 'e', 'w')`.

**MCP hints:** `plan_digest_verify_paths`.

#### Step 2 — Add `fence` palette entry

**Goal:** Insert `materials.fence` with `bright` + `mid` above `mustard_industrial`.

**Edits:**
- `tools/sprite-gen/palettes/residential.json` — **before**:
  ```
      "mustard_industrial": {
  ```
  **after**:
  ```
      "fence": {
        "bright": [214, 188, 130],
        "mid": [168, 142, 92]
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

**STOP:** JSON parse fail → re-open Step 2.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal`.

#### Step 3 — Re-export `iso_fence` from primitives `__init__`

**Goal:** Register `iso_fence` in primitives package alphabetically.

**Edits:**
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
  from .iso_cube import iso_cube
  ```
  **after**:
  ```
  from .iso_cube import iso_cube
  from .iso_fence import iso_fence
  ```
- `tools/sprite-gen/src/primitives/__init__.py` — **before**:
  ```
      "iso_cube",
  ```
  **after**:
  ```
      "iso_cube",
      "iso_fence",
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Import fail → re-open Step 1.

**MCP hints:** `plan_digest_resolve_anchor`.

---
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

---
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

---
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

Ship `tools/sprite-gen/tests/test_decorations_vegetation.py` — parametrized smoke + histogram tests across 8 decoration primitives (TECH-762..TECH-767). Residential palette only. Each primitive renders without exception; bbox >= 1 pixel; dominant colour matches palette ramp.

### §Acceptance

- [ ] New file `tools/sprite-gen/tests/test_decorations_vegetation.py` created.
- [ ] `_PRIMITIVES = ('iso_tree_fir', 'iso_tree_deciduous', 'iso_bush', 'iso_grass_tuft', 'iso_pool', 'iso_path', 'iso_pavement_patch', 'iso_fence')` literal present.
- [ ] Parametrized test `test_vegetation_smoke_all` covers 8 primitives.
- [ ] Parametrized test `test_vegetation_dominant_colour` covers 8 primitives; `iso_pool` uses top-2 tolerance (rim or fill); others use top-1 (fill).
- [ ] `test_vegetation_palette_keys_present` asserts residential palette contains `tree_fir`, `tree_deciduous`, `bush`, `grass_tuft`, `pool`, `fence`, `pavement`.
- [ ] Module docstring clarifies "7 task rows (T7.1–T7.6), 8 primitives".
- [ ] Test file runs green under `cd tools/sprite-gen && pytest tests/test_decorations_vegetation.py`.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `test_vegetation_smoke_all[primitive_name]` | 8 primitives parametrized, default kwargs, residential palette | no exception; bbox >= 1 pixel | pytest |
| `test_vegetation_dominant_colour[primitive_name]` | 8 primitives parametrized | dominant colour matches palette ramp (pool: top-2 rim/fill; others: top-1 fill) | pytest |
| `test_vegetation_palette_keys_present` | load residential palette JSON | 7 keys resolvable: `tree_fir`, `tree_deciduous`, `bush`, `grass_tuft`, `pool`, `fence`, `pavement` | pytest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `iso_tree_fir(canvas, 32, 32, scale=1.0, variant=0, palette=res)` on 64×64 canvas | no exception; bbox >= 1 px; dominant in `tree_fir` ramp | Smoke |
| `iso_pool(canvas, 32, 32, w_px=12, d_px=10, palette=res)` histogram top-2 | includes `pool.rim` + `pool.bright` | Rim tolerance |
| `iso_grass_tuft(canvas, 32, 32, variant=0, palette=res)` bbox | >= 1 pixel | Min-bbox |

### §Mechanical Steps

#### Step 1 — Create `test_decorations_vegetation.py` parametrized test file

**Goal:** Author pytest file covering 8 decoration primitives under residential palette.

**Edits:**
- `tools/sprite-gen/tests/test_decorations_vegetation.py` — **operation**: create; **after** — new file. Body:
  - Module docstring: "Smoke + histogram tests for 7 task rows (T7.1–T7.6), 8 primitives. Residential palette only."
  - Imports: `json`, `pathlib.Path`, `pytest`, `from PIL import Image`, `from src.primitives import (iso_tree_fir, iso_tree_deciduous, iso_bush, iso_grass_tuft, iso_pool, iso_path, iso_pavement_patch, iso_fence)`.
  - `_PRIMITIVES = ('iso_tree_fir', 'iso_tree_deciduous', 'iso_bush', 'iso_grass_tuft', 'iso_pool', 'iso_path', 'iso_pavement_patch', 'iso_fence')`.
  - `_PALETTE_KEYS = ('tree_fir', 'tree_deciduous', 'bush', 'grass_tuft', 'pool', 'fence', 'pavement')`.
  - Fixture `residential_palette()` loads `tools/sprite-gen/palettes/residential.json` → dict.
  - Fixture `blank_canvas()` returns `Image.new('RGBA', (64, 64), (0, 0, 0, 0))`.
  - `_PRIMITIVE_KWARGS` dict mapping each name → minimal default call kwargs (e.g. `iso_tree_fir → {'scale': 1.0, 'variant': 0}`; `iso_pool → {'w_px': 12, 'd_px': 10}`; `iso_path → {'length_px': 10, 'axis': 'ns', 'width_px': 2}`; `iso_fence → {'length_px': 10, 'side': 'n'}`; `iso_pavement_patch → {'w_px': 8, 'd_px': 8}`).
  - `_PRIMITIVE_DISPATCH` dict mapping name → callable.
  - `_EXPECTED_RAMP` dict mapping name → palette key the dominant colour should match (e.g. `iso_tree_fir → 'tree_fir'`).
  - `@pytest.mark.parametrize('name', _PRIMITIVES)` — `def test_vegetation_smoke_all(name, residential_palette, blank_canvas)`:
    - call `_PRIMITIVE_DISPATCH[name](blank_canvas, 32, 32, palette=residential_palette, **_PRIMITIVE_KWARGS[name])`
    - assert `blank_canvas.getbbox()` is not None
    - assert bbox area >= 1
  - `@pytest.mark.parametrize('name', _PRIMITIVES)` — `def test_vegetation_dominant_colour(name, residential_palette, blank_canvas)`:
    - render primitive; extract non-transparent pixels; compute top-2 RGB histogram
    - look up expected ramp under `residential_palette['materials'][_EXPECTED_RAMP[name]]`
    - for `iso_pool`: assert either `bright` or `rim` present in top-2
    - for `iso_tree_deciduous`: use `tree_deciduous.green` nested ramp (default `color_var='green'`)
    - for others: assert dominant matches any ramp level (`bright`, `mid`, or `dark` if present)
  - `def test_vegetation_palette_keys_present(residential_palette)`:
    - `for key in _PALETTE_KEYS: assert key in residential_palette['materials']`
- `invariant_touchpoints`: none (test authoring)
- `validator_gate`: `test -f tools/sprite-gen/tests/test_decorations_vegetation.py && echo OK`

**Gate:**
```bash
test -f tools/sprite-gen/tests/test_decorations_vegetation.py && echo OK
```
Expectation: prints `OK`.

**STOP:** File missing → re-run Step 1. Missing primitive import → re-open originating Task (TECH-762..TECH-767). Palette key drift → re-open originating Task that added the ramp.

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

---
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


## Final gate

```bash
npm run validate:all
```
