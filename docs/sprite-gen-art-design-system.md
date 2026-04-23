# Sprite-gen — Art Design System (DAS)

> **Status:** Canonical v1 — 2026-04-23
> **Owner:** Javier (art direction) · Codified from the existing `Assets/Sprites/` catalog (197 sprites across 15 subfolders).
> **Scope:** Rules and measurements the sprite-gen tool must follow to reproduce Javier's hand-drawn isometric look — dimensional math, per-category palette, primitive set, shading, decoration vocabulary, outline policy.
> **Consumed by:** `ia/projects/sprite-gen-master-plan.md` Stages 6–14 (all stages reference "DAS §N" instead of re-specifying rules inline).
> **Audit trail:** `/tmp/sprite-gen-style-audit.md` (working scratchpad with polling transcript, kept for history).

---

## 0. Locked decisions (2026-04-22 / 2026-04-23)

### High-level scope (Q1–Q5)
| # | Decision |
|---|---|
| Q1 | `House1-64.png` baseline for 1×1; full `Assets/Sprites` catalog is the corpus; 2×2 follows `LightResidentialBuilding-2-128.png` family. |
| Q2 | Multiple new stages appended to `sprite-gen-master-plan.md` as extensions (not a child orchestrator). |
| Q3 | **New primitives only** (parametric, YAML-declared). No stamp route. |
| Q4 | Unlock 2×2 and 3×3. Non-square (2×1, 3×2, etc.) **deferred**. |
| Q5 | This doc is the canonical DAS. |

### Design-system locks
| Block | Decision |
|---|---|
| **A1** | `level_h = 12 px` for residential + commercial small. `= 16 px` for industrial, dense tower, residential heavy. |
| **A2** | Canvas height grows in **+64 px tiers** per extra floor band. |
| **A3** | 1×1 single-family footprint_ratio default = `0.45 × 0.45`. |
| **A4** | 2×2 suburban footprint_ratio default = `0.40 × 0.40`. |
| **A5** | 1×1 dense tower footprint_ratio default = `0.9 × 0.9`. |
| **B1** | Primitives are **pixel-native** (`w_px`, `d_px`, `h_px` in px). `footprint_ratio` lives at spec level. |
| **C1** | Canonical grass = `#68a838` top / `#204808` rim-shade. |
| **C2** | Grass dense (forest fill) = 3-value ramp `#30a048/#108028/#085818`. |
| **C3** | Cliff/earth = **2-tone only** `#503810` / `#382810`. |
| **C4** | Water = base `#1818c0` / dark `#1010a8` / sparkle `#9090e8`. |
| **C5** | Per-class wall ramps in §4 table bootstrap verbatim; palettes are editable JSON. |
| **C6** | Residential row 2×2 uses pastel palette — random house color per variant from `{cyan, red, yellow, teal, peach}`. |
| **D1/D2** | Two distinct outline concepts: (1) silhouette outline `#000000` 1-px on small/medium buildings only (legacy, kept as signature); (2) rim-shade 1-px darker perimeter on zoning/grass/water/slope ground tiles. Dense towers + decorations = no silhouette. |
| **E1** | 17 primitives in the R9 set (§5). Extended candidates queued as "future decoration backlog". |
| **E2** | Decorations placed via **seed-based strategies** (`corners`, `perimeter`, `random_border`, `grid`, `centered_front`) + explicit-coords fallback. |
| **E3** | Tree default = **coniferous fir**; deciduous available as secondary primitive. |
| **E4** | Pool only on 2×2+ residential. |
| **E5** | Split: `iso_storefront_sign` (facade band) + `iso_parapet_cap` (roof edge). |
| **F1** | Break §2 "all 1×1" lock (amended on master plan). |
| **F2** | Non-square footprints **deferred**. |
| **F3** | Multi-building placement via named grid slots + explicit-coords fallback. |
| **G1** | Adopt 2-tone cliff sides; new `iso_slope_wedge` primitive; deprecate `iso_stepped_foundation` as default. |
| **G2** | Water-facing slopes **in scope**. |
| **G3** | Building on slope = plain cliff wedge under tilted diamond. |
| **I1** | Per-category art voice = **hard rule** (residential=warm+pitched+grass+trees; commercial=cool+flat+pavement; industrial=grey+corrugated+yellow+paved; power=mustard+towers+steam; water=pale steel+pipes). |

**Matrix rule:** every building + zoning archetype ships its **full slope matrix** (17 land + 17 water-facing = 34 slope variants), auto-derived from the flat spec via `--terrain` CLI flag.

---

## 1. Catalog inventory

Total: **197 PNG sprites** across 15 subfolders.

| Category | Count | Canvas sizes observed | Notes |
|---|---|---|---|
| Residential | 35 | 32², 64², 64×128, 128², 264² (atlas) | Full slope set (House1_*Slope) + zoning tiles; 2×2 Light/Medium composites |
| Commercial | 38 | 64², 64×128, 128², 128×256 | Full slope set (Store-1_*Slope); Dense tower variants at 64×128 & 128×256 |
| Industrial | 21 | 64², 128², 192² | Heavy=192² (3×3); full slope set for Light zoning |
| PowerPlant | 1 | 768×192 (4-frame sheet) | Each frame 192×192 = 3×3; cooling tower animation |
| WaterPlant | 1 | 128² | 2×2 footprint with pipe columns |
| Water | 4 | 64² | Flat water tile + sea level + 2 cliff-water joins |
| Grass | 5 | 32², 64², 128², 192² | Flat zoning tiles, all footprint sizes proven |
| Cliff | 2 | 64² | Pure brown earth slope faces (south, east) |
| Slopes | 29 | 64² | 17 land + 12 water-facing |
| Enviromental | 14 | 64², 128² | Forest fill + slope-matched forest variants |
| Effects | 4 | 320×64, 768×192, 256×64 | Sprite-sheet animations |
| Icons / Buttons / State / Roads | ~43 | mixed | UI layer — **out of scope for DAS** |

**Slope naming** — canonical stems: `N,S,E,W,NE,NW,SE,SW,NE-up,NW-up,SE-up,SW-up,NE-bay,NW-bay,NW-bay-2,SE-bay,SW-bay` (17 land) × optional `-water` suffix.

---

## 2. Dimensional signature

### 2.1 Universal diamond ground plate
Every 1×1 sprite's ground diamond: `bbox = y0=15, h=33, w=64` — a **32 px tall × 64 px wide** diamond, centered vertically on y=31.

### 2.2 Canvas formula (confirmed)
- `canvas_w = (fx + fy) × 32`
- `canvas_h = (fx + fy) × 32 + 64 × extra_floors` where `extra_floors ∈ {0, 1, 2, 3}` (gives 64²/64×128/64×192/64×256 patterns)
- **Pivot UV** = `(0.5, 16 / canvas_h)` — bottom of diamond always 16 px from canvas bottom.

### 2.3 Reference metrics table (anchor sprites)

| Sprite | Canvas | Content bbox (w×h) | Coverage | Height above diamond top (y=15) |
|---|---|---|---|---|
| `Residential/Residential-light-zoning-64.png` | 64×64 | 64×33 | 27% | 0 (ground only) |
| `Residential/House1-64.png` | 64×64 | 64×35 | 28% | ~2 px |
| `Commercial/Store-1.png` | 64×64 | 64×41 | 35% | ~8 px |
| `Commercial/Commercial-medium-building-64-1.png` | 64×64 | 64×47 | 47% | ~14 px |
| `Commercial/DenseCommercialBuilding-2.png` | 64×128 | 64×92 | 54% | ~60 px (~6 floors) |
| `Residential/HeavyResidentialBuilding-1-64.png` | 64×128 | 64×104 | 68% | ~72 px (~6-7 floors) |
| `Commercial/DenseCommercialBuilding-1.png` | 128×256 | 128×209 | 64% | ~177 px (mega-tower) |
| `Residential/LightResidentialBuilding-2-128.png` | 128×128 | 128×78 | 31% | ~13 px |
| `Residential/MediumResidentialBuilding-2-128.png` | 128×128 | 128×73 | 29% | ~8 px (row of 3) |
| `Industrial/MediumIndustrialBuilding-1-128.png` | 128×128 | 128×91 | 41% | ~26 px |
| `Industrial/HeavyIndustrialBuilding-1-192.png` | 192×192 | 192×112 | 32% | ~32 px (3×3 + yard) |
| `WaterPlant/WaterPlant-1-128.png` | 128×128 | 128×67 | 27% | ~3 px + pipes 10-15 px |
| `Grass/Grass1-64.png` (forest fill) | 64×64 | 64×47 | 42% | ~14 px |
| `Enviromental/Forest1-64.png` | 64×64 | 64×48 | 47% | ~15 px |
| `Water/Water1-64.png` | 64×64 | 64×33 | 27% | 0 (flat blue diamond) |
| `Slopes/NE-up-slope.png` | 64×64 | 64×48 | 44% | diamond rises 15 px |

### 2.4 Level heights (per "floor" of building)
- **Residential single-family / commercial small:** `level_h = 12 px`
- **Commercial tower / residential heavy / industrial:** `level_h = 16 px`

### 2.5 Footprint ratios (building area vs diamond)

| Building type | Default (w × d ratio) | Yard % |
|---|---|---|
| residential_small (1×1) | 0.45 × 0.45 | 55% |
| residential_dense_tower (1×1) | 0.9 × 0.9 | 10% |
| commercial_store (1×1) | 0.55 × 0.55 | 45% (paved) |
| commercial_dense_tower (1×1) | 0.95 × 0.95 | 5% |
| industrial_light (1×1) | 0.7 × 0.7 | 30% (paved) |
| residential_suburban (2×2) | 0.40 × 0.40 | 60% |
| commercial_strip (2×2) | 0.8 × 0.5 | sides only |
| industrial_heavy (3×3) | 0.7 × 0.7 cluster | paved yard + roads |

### 2.6 Calibration signatures

Calibration signatures are the canonical runtime calibration source. See `tools/sprite-gen/signatures/` + `tools/sprite-gen/src/signature.py` (Stage 6.2, TECH-704..708). Envelopes there supersede the ad-hoc per-spec bounds previously asserted in `tests/test_scale_calibration.py`.

---

## 3. Visual style cues per category

### Residential
- Small 1×1: single-gable house, red/brown pitched roof, cream/beige walls, 2–4 perimeter trees.
- Medium 2×2 (row): 3–4 small houses tiled N–S, shared front-yard with trees.
- Medium 2×2 (suburban): single larger house + pool + trees.
- Heavy 1×1×128: mid-rise apartment slab, 6 floors, cyan window bands, parapet flat roof.
- Signature: warm palette, pitched gable/hipped roofs, horizontal clapboard hint, trees as framing, grass under everything.

### Commercial
- Store 1×1: flat-roof storefront, cyan/teal signage, pavement replaces grass.
- Medium 1×1: retail block, flat roof, asphalt perimeter.
- Dense tower (64×128 / 128×256): glass towers, cool blue-grey facade, pink/peach parapet cap, no silhouette outline.
- Signature: cooler palette (blues, teals, greys), flat/angular roofs, paved surroundings, dense window grids.

### Industrial
- Light 1×1: boxy factory, yellow warning stripe, corrugated roof.
- Medium 2×2: warehouse + office cluster, red roof vents, paved driveway.
- Heavy 3×3: multi-building lot with paved roads, loading docks, parking stripes.
- Signature: grey-dominant, corrugated stripe texture, yellow/olive accents, paved ground, roof equipment.

### Power (Nuclear, static v1)
- 3×3: office building + 3 cooling towers + steam plumes (animation deferred), mustard ground plate.
- Signature: tapered cooling cylinders, mustard/tan ground, warning stripes.

### Water Plant
- 2×2: flat warehouse + 3–4 blue pipe columns with darker caps, grass diamond.
- Signature: exposed pipes, pale steel facade.

### Grass / Forest / Water
- Grass 1×1: flat `#68a838` with `#204808` rim stripe.
- Forest: chunky coniferous clumps, 3-value green ramp, clean tile edges.
- Water: ultramarine `#1818c0` + sparkle pixels.

### Slopes / Cliffs
- Top face = flat grass (same as zoning).
- Side faces = 2-tone brown `#503810` / `#382810`.
- Bay slopes include water strip on low edge.

---

## 4. Palette signature per category

### 4.1 Universal materials (reusable across categories)

| Material | Bright | Mid | Dark | Notes |
|---|---|---|---|---|
| grass_flat | `#68a838` | — | `#204808` (rim only) | Zoning diamond base. |
| grass_dense | `#30a048` | `#108028` | `#085818` | Forest / grass-tuft fill. |
| earth_brown | `#503810` | — | `#382810` | Cliff/slope side faces. 2-tone only. |
| water_deep | `#9090e8` (sparkle) | `#1818c0` | `#1010a8` | Water diamond. |
| pavement | `#a8a8a8` | `#909090` | `#686868` | Commercial/industrial ground. |
| mustard_industrial | `#b8a858` | `#888040` | `#585028` | Power-plant ground. |
| outline | — | — | `#000000` | 1-px silhouette on small/medium buildings. |

#### §4.1.A Accent keys (`accent_dark` / `accent_light`)

Each material entry may optionally declare `accent_dark` and `accent_light` RGB tuples (TECH-716). Consumed by scatter primitives (e.g. `iso_ground_noise`) to paint specks that read darker/lighter than the base ramp. Absent keys → consumers no-op for that material.

```json
"grass_flat": {
  "bright": [104, 168, 56],
  "mid":    [78,  126, 42],
  "dark":   [32,   72,  8],
  "accent_dark":  [22, 52, 8],
  "accent_light": [140, 196, 80]
}
```

#### §4.1.B `iso_ground_noise` density range

`iso_ground_noise` accepts a `density` parameter in the range `0..0.15`. Values outside this range are clamped. Zero density = no-op. The guardrail prevents accent scatter from overpowering the ramp and breaking the silhouette-first reading of a building sprite.

#### §4.1.C Signature-derived `vary.ground.*` bounds

Rather than hand-tuning jitter ranges, authors may consult extracted `signatures/` JSON (shape defined TECH-704, ground fields populated TECH-719). `ground.variance.hue_stddev` / `value_stddev` from the signature → sensible `vary.ground.hue_jitter` / `value_jitter` bounds without guessing.

### 4.2 Per-class building wall palettes

| Class | Bright / Mid / Dark | Roof accent |
|---|---|---|
| residential_light (small) | `#e0c8b0` / `#c0a898` / `#987860` | red `#d84848` / `#a04848` |
| residential_row_colorful | `#88c8e8` · `#d06060` · `#d8d078` · `#98a840` (pastels picked per-variant) | matching pastel |
| residential_heavy | `#d0d0d8` / `#a8a8a8` / `#808088` | window band `#58a8f8` / `#3878b8` |
| commercial_store | `#88d0d8` / `#88a8a8` / `#809090` | teal/white parapet |
| commercial_dense | `#c8d0d0` / `#60b0f8` / `#4888c0` | pink cap `#f0c8c8` |
| industrial_light | `#b0b0a8` / `#989890` / `#808078` | yellow stripe `#98a830` / `#e0f018` |
| industrial_heavy | `#d0d0d0` / `#b0b0b0` / `#888080` | red vents `#a04848` |
| power_nuclear | grey + cooling-tower white `#c8d0d8` | mustard ground, steam white |
| waterplant | `#b0c8d8` / `#98a8b0` / `#788898` | pipe caps `#2850a0` |

### 4.3 Shading model (unchanged from Stage 1)
- **NW-light**: top = bright, south (front) = mid, east (right) = dark.
- 3-level HSV ramp clamped, per `palette.py apply_ramp()`.

---

## 5. Design rules (R1–R12)

### R1 — Canvas math — **keep as implemented**
`canvas_w = (fx + fy) × 32`; `canvas_h = (fx + fy) × 32 + 64 × extra_floors`; pivot UV = `(0.5, 16 / canvas_h)`.

### R2 — Primitive unit system — **pixel-native**
Primitives take `w_px`, `d_px`, `h_px` in pixels. Spec carries `footprint_ratio: [wr, dr]` (scalar pair, ratios of diamond span) that the composer applies at render time.

### R3 — Ground diamond primitive — **new**
`iso_ground_diamond(fx, fy, material)` rasterizes the full-tile flat diamond with 1-px rim-shade. Drawn first. Materials: `grass_flat`, `grass_dense`, `pavement`, `water_deep`, `zoning_residential`, `zoning_commercial`, `zoning_industrial`, `mustard_industrial`. Auto-prepended by composer unless `spec.ground: none`.

### R4 — Level-height constants — **locked**
- `level_h = 12 px` — residential_small, commercial_small
- `level_h = 16 px` — industrial_*, commercial_dense, residential_heavy
- Spec exposes `levels: <int>` → building height = `levels × level_h_for_class`.

### R5 — Footprint-ratio table — **§2.5**

### R6 — 3-face NW-light shading — **keep**

### R7 — Outline policy
- **Silhouette outline** (`#000000`, 1 px, exterior only): small/medium buildings (residential_small, commercial_store, commercial_medium, industrial_light).
- **Rim-shade** (darker ring, 1 px, perimeter of face): zoning/grass/water/slope ground tiles.
- **No outline**: dense towers, decorations.

### R8 — Slope rendering
- Top face: flat grass color (no ramp).
- Side faces: 2-tone earth_brown (bright/dark — no 3rd level).
- Water-facing variants: add water strip at low edge.
- Buildings on slopes sit on a tilted `iso_ground_diamond` over an `iso_slope_wedge`; no stepped foundation by default.

### R9 — Decoration primitive set (17 primitives, v1)

| Primitive | Purpose | Parameters |
|---|---|---|
| `iso_tree_fir` | Coniferous tree | `scale`, `shadow` |
| `iso_tree_deciduous` | Round-crown tree | `scale`, `color_var` |
| `iso_bush` | Low green puff | `scale` |
| `iso_grass_tuft` | Tiny accent | — |
| `iso_pool` | Blue rect + white rim (2×2+ only) | `w_px`, `d_px` |
| `iso_path` | Beige/grey walkway | `w_px`, `d_px`, `axis` |
| `iso_pavement_patch` | Larger paved area | `w_px`, `d_px` |
| `iso_fence` | Thin edge strip | `side` |
| `iso_chimney` | Vertical rect on roof | `h_px`, `material` |
| `iso_roof_vent` | Small roof box | `scale` |
| `iso_window_grid` | Face window pattern | `rows`, `cols`, `face`, `material` |
| `iso_door` | Ground-level dark rect | `w_px`, `h_px`, `face` |
| `iso_storefront_sign` | Facade band (commercial) | `h_px`, `color` |
| `iso_parapet_cap` | Roof edge band (commercial dense) | `color` |
| `iso_pipe_column` | Vertical pipe + cap | `h_px`, `material` |
| `iso_cooling_tower` | Tapered cylinder (static v1) | `h_px`, `smoke=false` |
| `iso_smokestack` | Thin tall cylinder | `h_px` |
| `iso_paved_parking` | Pavement + painted stripes | `w_px`, `d_px` |

### R10 — Palette JSON schema v2
```json
{
  "materials": { "<name>": { "bright": [r,g,b], "mid": [r,g,b], "dark": [r,g,b] } },
  "ground":     { "<name>": { ... } },
  "decorations": { "<name>": { "<role>": [r,g,b] } }
}
```

### R11 — Archetype YAML schema v2
```yaml
id: building_residential_small_v2
class: residential_small
footprint: [1, 1]
terrain: flat
ground: grass_flat
seed: 42
variants: 4
palette: residential
levels: 1
building:
  footprint_ratio: [0.45, 0.45]
  composition:
    - { type: iso_cube,  material: wall_cream, h_px: 10 }
    - { type: iso_prism, material: roof_red, pitch: 0.5, axis: ns, h_px: 8, offset_z: 10 }
  details:
    - { type: iso_door, face: south, w_px: 4, h_px: 6, material: door_dark }
    - { type: iso_window_grid, face: south, rows: 1, cols: 2, material: window_blue }
decorations:
  - { type: iso_tree_fir, count: 3, placement: corners }
  - { type: iso_bush,     count: 2, placement: random_border }
diffusion:
  enabled: false
```

### R11.1 Stage 6.3 additions (placement + split seeds + vary grammar)

Stage 6.3 (TECH-709..714) extends R11 with three surface groups — all optional, all back-compat.

**Placement (under `building:`):**

- `footprint_px: [bx, by]` — pixel-exact footprint; wins over `footprint_ratio` when both present (emits `DeprecationWarning: footprint_px wins`).
- `padding: { n, e, s, w }` — asymmetric empty space per side, px integers; each subkey defaults to `0` and omitted subkeys fill in.
- `align: center | sw | ne | nw | se | custom` — anchor for the building mass; default `center` preserves byte-identical legacy render. `custom` returns zero offset for callers that supply explicit shifts.

**Split seeds (top-level):**

- `palette_seed: int` + `geometry_seed: int` — independent rng streams for palette-scoped vs geometry-scoped `vary.` axes.
- Legacy scalar `seed: int` fans out to both (`palette_seed = geometry_seed = seed`) when neither split seed is present. Explicit split seeds always win.

**`vary:` grammar (under `variants.vary`):**

- Numeric range leaf: `{min, max}` — `randint` for int endpoints, `uniform` for floats.
- Categorical leaf: `{values: [...]}` — `rng.choice`.
- Scope selector: `variants.seed_scope ∈ {palette, geometry, palette+geometry}`; default `palette` preserves pre-Stage-6.3 behaviour.
- Axis classification (which rng drives which leaf): path roots `palette` / `material` / `materials` + leaf names starting with `color` / `hue` / `value` / `tint` route through the palette rng; everything else (roof, footprint, padding, …) routes through the geometry rng.

**Object form of `variants:` (scalar still supported):**

`variants: 4` normalises to `{count: 4, vary: {}, seed_scope: "palette"}`. Object form accepts all three subkeys directly. `variants.count` defaults to `1`, `vary` to `{}`, `seed_scope` to `"palette"`.

**End-to-end example:**

```yaml
id: building_residential_small_v2
class: residential_small
footprint: [1, 1]
terrain: flat
palette: residential
palette_seed: 101
geometry_seed: 4
variants:
  count: 4
  vary:
    roof: { h_px: { min: 6, max: 14 } }
    palette: { color_wall: { values: [cream, sand, ochre] } }
  seed_scope: palette+geometry
building:
  footprint_px: [28, 28]
  padding: { n: 2, e: 0, s: 10, w: 0 }
  align: sw
  composition:
    - { type: iso_cube,  material: wall_cream, h_px: 10 }
    - { type: iso_prism, material: roof_red,   pitch: 0.5, axis: ns, h_px: 8, offset_z: 10 }
```

### R12 — Stage-6+ roadmap
See master plan `ia/projects/sprite-gen-master-plan.md` Stages 6–14 (+15 optional).

### §5.13 Curation log schema (Stage 6.5, TECH-723/724)

`curation/promoted.jsonl` and `curation/rejected.jsonl` are append-only JSONL feedback logs under `tools/sprite-gen/curation/`. One row per curator action. Verbs `log-promote` (TECH-723) and `log-reject` (TECH-724) are orthogonal to the TECH-179 `promote` (PNG → Unity ship) and `reject` (glob-delete) verbs.

| Field | Type | Description |
|-------|------|-------------|
| `variant_path` | string | Path to rendered variant PNG |
| `vary_values` | object | Nested `vary:` leaves recovered via `compose.sample_variant(spec, idx)` |
| `bbox` | object | Measured alpha bbox (`x0`, `y0`, `width`, `height`) |
| `palette_stats` | object | `opaque_count`, `distinct_colors`, `mean_rgb` |
| `timestamp` | number | Unix seconds |
| `reason` | string | `rejected.jsonl` only — one of `REJECTION_REASONS` |

CLI:

```bash
python3 -m src log-promote out/residential_small_v03.png
python3 -m src log-reject  out/residential_small_v07.png --reason roof-too-shallow
```

### §5.14 Rejection reasons → vary.* zone map (TECH-724/725)

Controlled vocabulary `REJECTION_REASONS`:

- `roof-too-shallow`
- `roof-too-tall`
- `facade-too-saturated`
- `ground-too-uniform`

Each reason carves one `vary.*` axis bound away from the rejected sample (`REASON_AXIS_MAP`, `src/signature.py`):

| Reason | `vary.*` axis | Bound |
|--------|---------------|-------|
| `roof-too-shallow` | `roof.h_px` | `min` |
| `roof-too-tall` | `roof.h_px` | `max` |
| `facade-too-saturated` | `facade.saturation` | `max` |
| `ground-too-uniform` | `ground.hue_jitter` | `min` |

Carve-out = nudge bound by 1 unit (`new_min = rejected_value + 1`, `new_max = rejected_value - 1`). The aggregator `compute_envelope(catalog, promoted, rejected)` produces the live `vary.*` envelope as `catalog ∪ promoted − rejected-zones` — promoted rows tighten bounds toward the validated centroid, rejected rows carve floors/ceilings. Sort-before-aggregate determinism.

### §5.15 Composer score-and-retry contract (TECH-726)

`compose.render(spec, *, envelope=None, retry_cap=5, gate_enabled=False)` wraps the variant loop with an envelope-aware quality gate:

1. Sample `vary:` via `sample_variant(spec, i)`.
2. Render via `compose_sprite(sampled_spec)`.
3. Score: per-axis normalized deviation `d_a = clamp(|v_a - c_a| / h_a, 0, 1)` where `c_a = (min+max)/2`, `h_a = (max-min)/2`; aggregate `L2 = sqrt(mean(d_a^2))`; `score = 1.0 − L2`. Carved-zone hits (sample outside envelope bounds) hard-fail to `score = 0.0`.
4. If `score < _FLOOR` (`0.5`) and retries remain: advance seed `palette_seed + variant_idx * (retry_cap + 1) + retry`; re-sample.
5. `gate_enabled=False` or `envelope=None` → byte-identical pre-Stage-6.5 path (existing golden tests as parity oracle).

### §5.16 .needs_review sidecar semantics (TECH-727)

When the gate exhausts `retry_cap` without meeting `_FLOOR`, a versioned JSON sidecar `<variant>.needs_review.json` is written next to the best-scoring variant. Non-blocking — pipeline continues. Schema v1:

| Field | Type | Description |
|-------|------|-------------|
| `schema_version` | int | `1` |
| `final_score` | float | Score of the best-scoring attempt |
| `envelope_snapshot` | object | Envelope used at render time |
| `attempted_seeds` | int[] | Full trajectory of seeds tried |
| `failing_zones` | string[] | Carved `vary.*` axes hit on the best attempt |

Curator UI / CI consume the sidecar to surface low-confidence renders without blocking the pipeline.

---

## 6. Future decoration backlog (not in Stages 6–14)

Queued for post-v1 stages:
- `iso_street_lamp`
- `iso_signboard`
- `iso_mailbox`
- `iso_flower_patch`
- `iso_hedge`
- `iso_garden_bed`
- `iso_car`
- `iso_statue`
- `iso_antenna`
- Animation primitives (cooling-tower steam, smokestack smoke, bulldozer frames, generic 4-frame sheets) — separate future exploration.
