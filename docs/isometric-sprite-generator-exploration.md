# Isometric Sprite Generator — Exploration

**Status**: Defined / ready to seed master plan
**Date**: 2026-04-13 (expanded from initial survey)
**Context**: Territory Developer needs systematic pipeline for isometric pixel art — buildings + terrain slopes. Off-the-shelf AI tools fail on fixed 2:1 projection, directional light, palette coherence, and grid alignment. This doc locks design for a custom hybrid sprite generator built inside the repo under `tools/sprite-gen/`.

---

## 1. Problem restatement

Isometric pixel art constraints that break generic AI:

1. **Fixed 2:1 projection** — arctan(0.5) ≈ 26.565°. Unity world uses `tileWidth=1, tileHeight=0.5` (see `Assets/Scripts/Managers/GameManagers/GridManager.cs:59`). Sprites must match exactly.
2. **Directional light** — NW source → SE shadow. Consistent across every sprite.
3. **Palette coherence** — restricted per-class palettes for lo-fi SC2K-adjacent look.
4. **Grid alignment** — footprint diamond anchored to canvas bottom-center; edges snap to 64×32-pixel diamond grid.
5. **Slope awareness** — buildings sit on terrain; slopes already hand-drawn under `Assets/Sprites/Slopes/` (32 variants). Generator must render foundations that bridge sloped ground to flat building base.

Current state: sprites hand-drawn at 64×64 (`diamond-tile-64.png`, `House1-64.png`, `Grass1-64.png`). Larger buildings exist at 64×128 (skyscraper) and 192×192 (nuclear plant). Manual authoring does not scale to EA-ship target (~15 archetypes × several variants).

---

## 2. Locked decisions

| # | Decision | Value |
|---|---|---|
| 1 | North star | Unblock EA shipping — generator exists to produce enough building+terrain sprite mass for first public build |
| 2 | Asset scope v1 | Buildings (+ slope-aware foundations). Terrain slope tiles stay hand-drawn. |
| 3 | Art reference | SimCity 2000 / Transport Tycoon, cleaner (no 1-px black outline) |
| 4 | Canvas math | `width = (fx+fy)×32`, `height = multiple of 32`. Diamond footprint anchored to bottom-center. |
| 5 | Palette scope | Per-asset-class (residential / commercial / industrial / …). Not global. |
| 6 | Generation architecture | 5-layer custom hybrid composer (see §3) |
| 7 | Authoring surface | YAML spec + Python CLI |
| 8 | Language | Python (see §12 rationale) |
| 9 | Primitives v1 | Iso cube + iso prism (lean set) |
| 10 | Palette source | K-means auto-extract from existing sprites, per class |
| 11 | Diffusion overlay | Phase 2 (weeks 3-4). Ship geometry-only MVP first. Local Stable Diffusion on Apple Silicon MPS + ControlNet. |
| 12 | Shading | 3-level per face (top=bright, S-face=mid, E-face=dark). No outline. NW light. |
| 13 | EA scope | ~15 archetypes (5 residential + 5 commercial + 5 industrial), all 1×1 footprint |
| 14 | Curation | Batch render → `tools/sprite-gen/out/` scratch dir → `--promote` CLI → `Assets/Sprites/Generated/` + auto `.meta` |
| 15 | Materials | K-means cluster → auto 3-level ramp (bright/mid/dark) → manual name (`wall_brick_red`, `roof_tile`, …) |
| 16 | Slope coverage | Full — 17 variants (flat + 4 cardinal + 4 diagonal + 4 up-diagonal + 4 bay). Water-facing deferred to v2. |

---

## 3. Architecture — 5-layer hybrid composer

```
┌─────────────────────────────────────────────────────────────┐
│ Layer 5: Curation CLI (promote / reject / regenerate)       │
├─────────────────────────────────────────────────────────────┤
│ Layer 4: Diffusion overlay (Phase 2 — optional per batch)   │
│          SD img2img + ControlNet depth, low-strength pass   │
├─────────────────────────────────────────────────────────────┤
│ Layer 3: Palette post-process                                │
│          quantize → per-class palette → 3-level ramp enforce │
├─────────────────────────────────────────────────────────────┤
│ Layer 2: Composition + shading                               │
│          stack primitives on footprint, NW-light shade each  │
│          auto-insert slope foundation primitive              │
├─────────────────────────────────────────────────────────────┤
│ Layer 1: Iso primitive renderer                              │
│          draw cube / prism to pixel grid at 2:1 ratio        │
└─────────────────────────────────────────────────────────────┘
```

Data flow per sprite:

```
spec.yaml → Layer1 (primitives) → Layer2 (compose+shade) →
  Layer3 (palette) → [Layer4 diffusion if enabled] → out/ → Layer5 promote
```

Each layer runs independently. Skippable individually (`--no-diffusion`, `--no-palette`). Deterministic with seed per layer.

---

## 4. Canvas math

### Baseline

- Diamond footprint cell: **64 px wide × 32 px tall**.
- Footprint of `(fx, fy)` tiles (in grid space) spans **`(fx+fy) × 32` pixels** wide on canvas.
- Height = multiple of 32, minimum = 64 (single-tile flat footprint with normal building = 64×64; skyscraper = 64×128; large plant = 192×192).

### Anchor

Diamond bottom corner sits at canvas bottom-center. Building extrusion grows upward from top of footprint diamond.

### Unity import defaults

- **PPU** (pixels per unit): 64 (matches `tileWidth=1` → 64 px).
- **Pivot**: `(0.5, 0.25)` — X center, Y at 1/4 from bottom (lands on diamond bottom corner when footprint fills bottom 32 px of a 64×64 sprite; recomputed for taller sprites as `(0.5, 16/height)`).
- **Filter**: Point (no filter).
- **Compression**: None.

### Examples

| Footprint | Extra height | Canvas | Pivot |
|---|---|---|---|
| 1×1 (small house) | 32 (1 level) | 64×64 | (0.5, 0.25) |
| 1×1 (skyscraper) | 96 (3 levels) | 64×128 | (0.5, 0.125) |
| 3×3 (nuclear plant) | 96 (3 levels) | 192×192 | (0.5, ~0.083) |

---

## 5. Primitive library v1

Lean set. Two primitives cover ~90% of SC2K-style buildings.

### `iso_cube(w, d, h, material)`

- Rectangular box. `w` = extent along grid-X, `d` = along grid-Y, `h` = vertical pixels.
- Renders 3 visible faces: **top** (rhombus), **south** (parallelogram), **east** (parallelogram).
- Each face filled with material's 3-level ramp index (top=bright, S=mid, E=dark).

### `iso_prism(w, d, h, pitch, axis, material)`

- Triangular prism for roofs. `pitch` = rise ratio; `axis ∈ {ns, ew}` = ridge direction.
- Renders top 2 sloped faces + 2 triangular end-faces. Same 3-level shading.

### Auto-inserted (not user-specified)

- `iso_stepped_foundation(fx, fy, slope_id, material=concrete)` — filler primitive the composer drops in when `terrain != flat`. Reads per-corner Z table from `slopes.yaml`, builds stair/wedge geometry from sloped ground plane to flat building base.

### Deferred to v2

`iso_pyramid`, `iso_cylinder`, `iso_cone`, arbitrary polygon extrusion. Added when an archetype demands it.

---

## 6. Palette system

### Per-class palettes

`tools/sprite-gen/palettes/{class}.json`:

```json
{
  "class": "residential",
  "source_sprites": ["Assets/Sprites/House1-64.png", "..."],
  "extracted": "2026-04-13",
  "materials": {
    "wall_brick_red":   { "bright": "#C74A3B", "mid": "#8E3428", "dark": "#5A2118" },
    "roof_tile_brown":  { "bright": "#7A4226", "mid": "#563019", "dark": "#33200F" },
    "window_glass":     { "bright": "#B8D4E8", "mid": "#7994A8", "dark": "#3F5668" },
    "concrete":         { "bright": "#B0B0A8", "mid": "#7E7E76", "dark": "#4E4E47" }
  }
}
```

### Extraction pipeline

1. Scan `Assets/Sprites/` by class tag (filename prefix or manual mapping list).
2. K-means cluster all non-transparent pixels → N clusters (N=8 default, per class).
3. For each cluster centroid, synthesize 3-level ramp: `bright = centroid × 1.2`, `mid = centroid`, `dark = centroid × 0.6` (HSV value scaling, clamped).
4. Human names the materials (one-shot CLI pass).
5. Cache JSON. Re-extract when new source sprites land (manual `--extract-palette` command).

### Shading rule at composition

Layer 2 picks material per primitive face:
- Top face → `bright`
- South face (visible to NW light) → `mid`
- East face (shadow side) → `dark`

No anti-aliasing. Hard color boundaries.

---

## 7. Slope-aware foundation rendering

### `tools/sprite-gen/slopes.yaml`

Per-corner Z offset table (in pixels, grid-space) keyed by slope id matching `Assets/Sprites/Slopes/` names:

```yaml
flat:     { n: 0,  e: 0,  s: 0,  w: 0 }
N:        { n: 16, e: 0,  s: 0,  w: 0 }
S:        { n: 0,  e: 0,  s: 16, w: 0 }
E:        { n: 0,  e: 16, s: 0,  w: 0 }
W:        { n: 0,  e: 0,  s: 0,  w: 16 }
NE:       { n: 16, e: 16, s: 0,  w: 0 }
NW:       { n: 16, e: 0,  s: 0,  w: 16 }
SE:       { n: 0,  e: 16, s: 16, w: 0 }
SW:       { n: 0,  e: 0,  s: 16, w: 16 }
N-up:     { n: 16, e: 0,  s: 0,  w: 0,  variant: up }  # single-corner raise
# ... 4 up-diagonals + 4 bays → 17 total
```

### Composer behavior

1. Read spec `terrain` field (default `flat`).
2. Lookup per-corner Z from `slopes.yaml`.
3. If any corner ≠ 0, insert `iso_stepped_foundation(fx, fy, slope_id, material=concrete)` primitive **first** in stack.
4. Foundation fills from sloped ground plane up to max corner Z + 2 px lip, so building base is flat.
5. Canvas auto-grows height by `max_corner_z` pixels to accommodate raised building.
6. Pivot recomputed to keep diamond bottom corner on canvas bottom-center.

### Material for foundation

New material per class: `foundation_concrete` / `foundation_stone` / `foundation_industrial`. K-means-extracted alongside walls.

---

## 8. YAML spec schema

`tools/sprite-gen/specs/{archetype}.yaml`:

```yaml
id: building_residential_small_v1
class: residential
footprint: [1, 1]          # fx, fy in grid tiles
terrain: flat              # or: N / S / E / W / NE / NW / SE / SW / N-up / ... (17 slope ids)
levels: 2                  # number of cube stacks
seed: 42                   # reproducibility
composition:
  - { type: iso_cube,  w: 2, d: 2, h: 32, material: wall_brick_red }
  - { type: iso_cube,  w: 2, d: 2, h: 32, material: wall_brick_red, offset_z: 32 }
  - { type: iso_prism, w: 2, d: 2, h: 16, pitch: 0.5, axis: ns, material: roof_tile_brown }
palette: residential       # palette json id
output:
  name: building_residential_small
  variants: 4              # render N variants with seed permutations
diffusion:                  # optional, Phase 2
  enabled: false
  strength: 0.15
  prompt: "isometric pixel art small brick house, clean edges"
```

### Variant strategy

`variants: N` → composer perturbs non-structural fields per seed:
- Material swap within class (pick alt from same material family).
- Window pattern shift.
- Prism pitch ±20%.
- Height ±1 level (v2).

Batch writes `{name}_v01.png` … `{name}_v04.png` to `out/`.

---

## 9. Folder layout

```
tools/sprite-gen/
├── README.md
├── requirements.txt            # pillow, numpy, scipy, pyyaml, (diffusers, torch for Phase 2)
├── specs/                      # YAML archetypes (checked in)
│   ├── building_residential_small.yaml
│   ├── building_commercial_office.yaml
│   └── ...
├── palettes/                   # extracted palette JSONs (checked in)
│   ├── residential.json
│   ├── commercial.json
│   └── industrial.json
├── slopes.yaml                 # per-corner Z table (checked in)
├── out/                        # scratch render dir (.gitignored)
├── src/
│   ├── __init__.py
│   ├── cli.py                  # entry: `python -m sprite_gen ...`
│   ├── canvas.py               # canvas sizing + pivot math
│   ├── primitives/
│   │   ├── iso_cube.py
│   │   ├── iso_prism.py
│   │   └── iso_stepped_foundation.py
│   ├── compose.py              # Layer 2
│   ├── palette.py              # Layer 3 + K-means extract
│   ├── diffusion.py            # Layer 4 (Phase 2)
│   ├── curate.py               # Layer 5: promote / reject CLI
│   └── unity_meta.py           # generate .meta files on promote
└── tests/
    ├── test_canvas.py
    ├── test_primitives.py
    └── fixtures/
```

`out/` and any `.cache/` under the tool are in `.gitignore`. Promoted sprites land in `Assets/Sprites/Generated/` and are tracked.

---

## 10. CLI interface

```bash
# render a single archetype (4 variants to out/)
python -m sprite_gen render building_residential_small

# render all archetypes in specs/
python -m sprite_gen render --all

# render with specific terrain override (CLI beats spec)
python -m sprite_gen render building_residential_small --terrain N-up

# extract palette from existing sprites (one-time per class)
python -m sprite_gen palette extract residential \
  --sources "Assets/Sprites/House*.png,Assets/Sprites/Apartment*.png"

# promote a rendered variant (copies PNG + writes .meta)
python -m sprite_gen promote out/building_residential_small_v02.png \
  --as building_residential_small_01

# reject all variants of an archetype (cleans out/)
python -m sprite_gen reject building_residential_small

# Phase 2: re-render with diffusion pass
python -m sprite_gen render building_residential_small --diffusion
```

Exit codes: 0 = success, 1 = spec invalid, 2 = palette missing, 3 = diffusion backend unavailable.

---

## 11. Curation workflow

```
┌──────────┐  render all   ┌──────────┐  eye-pick   ┌──────────────┐  unity   ┌──────┐
│ specs/   │ ─────────────▶│ out/     │ ───────────▶│ promote CLI  │ ────────▶│ game │
│ *.yaml   │               │ *.png    │             │ + .meta gen  │          │      │
└──────────┘               └──────────┘             └──────────────┘          └──────┘
                                │                          │
                                │ reject                   │
                                ▼                          ▼
                           (discarded)            Assets/Sprites/Generated/
```

1. Author writes `specs/{archetype}.yaml` (or copies + edits).
2. `render --all` dumps N variants per archetype to `out/`.
3. Human reviews PNGs (Finder preview, VSCode, itch.io-style grid view tool).
4. `promote out/X.png --as final_name` copies to `Assets/Sprites/Generated/` + writes `.meta` with PPU=64, pivot computed from canvas height, Point filter, no compression.
5. Unity auto-imports on focus.
6. Rejected variants: `reject {archetype}` nukes matching files from `out/`.

No git-tracked `out/`. Rendered-but-not-promoted artifacts are ephemeral by design.

---

## 12. Why Python

Considered C#, TypeScript, Rust. Python wins for this tool because:

1. **Diffusers ecosystem** — `diffusers`, `transformers`, `torch` with MPS backend all first-class in Python. Phase 2 diffusion overlay needs this. C# has no equivalent; TS via ONNX Runtime works but tooling is thinner; Rust `candle` is young.
2. **Pillow + numpy + scipy** — pixel-level image manipulation, K-means, array ops are idiomatic and fast enough. Equivalent stacks in TS (sharp + ml-kmeans) exist but are patchier; C# would need ImageSharp + manual K-means.
3. **Iteration speed** — no compile step. Edit primitive → re-render → diff output in seconds.
4. **CLI + YAML ergonomics** — `argparse`/`click` + `pyyaml` are batteries-included.
5. **Cross-platform** — macOS (primary dev), Windows, Linux without friction.
6. **Isolation from Unity** — tool runs outside Unity as a headless batch. Python keeps it that way; no risk of leaking C# runtime/engine deps into a build tool.

Trade-off: one more runtime to install on contributor machines (`brew install python@3.11 && pip install -r requirements.txt`). Acceptable.

---

## 13. Phase plan

### Phase 1 — Geometry MVP (weeks 1-2.5)

**Goal**: `render --all` produces usable flat-terrain building sprites end-to-end.

- Week 1: Canvas math, `iso_cube`, `iso_prism`, shade pass. Unit tests on pixel-perfect output.
- Week 1.5: Compose layer, YAML schema, CLI skeleton (`render` + `render --all`).
- Week 2: K-means palette extract, palette apply, per-class JSON files. 3-level ramp enforcement.
- Week 2.5 (slope add): `iso_stepped_foundation`, `slopes.yaml`, auto-insert logic, canvas auto-grow. Render all 17 slope variants for 1 archetype as regression test.
- End of Phase 1: ~5 archetypes × 4 variants × 17 slopes each = ~340 sprites in `out/`. Hand-pick best → promote flow.

### Phase 2 — Diffusion overlay (weeks 3-4)

**Goal**: optional detail pass sharpens geometry-baked sprites without breaking grid/palette.

- Week 3: Local SD on MPS, img2img at strength=0.1–0.2, ControlNet depth from Layer-2 output.
- Week 3.5: Re-quantize to palette after diffusion (Layer 3 runs again post-diffusion).
- Week 4: Evaluate quality vs hand-picked geometry. Decision: keep as opt-in, drop, or make default.

### Phase 3 — EA bulk render (week 5)

**Goal**: produce all EA-target sprites.

- 15 archetypes × 4 variants × 17 slopes ≈ 1000 sprites rendered.
- Curation session: promote ~60–80 final sprites to `Assets/Sprites/Generated/`.
- Unity import audit: PPU/pivot/filter correctness on all.

Total: **~5 weeks** from zero to EA-ready sprite mass.

---

## 14. Evolution mechanics

Tool grows with usage. Three extension points:

1. **Add primitive** — drop new file in `src/primitives/`, register in `compose.py`, document in spec schema. Zero migration — existing specs unaffected.
2. **Add material** — K-means re-extract bumps cluster count, human names new material, palette JSON regenerates. Existing specs using old material name keep working.
3. **Add archetype** — new `specs/{name}.yaml`. Rendered on next `render --all`.
4. **Re-extract palette cadence** — run `palette extract` after every ~10 new hand-drawn reference sprites land, or quarterly, whichever hits first.

Breaking changes (canvas math, schema v2) bump tool version and require re-render + re-promote. Versioned spec files (`id: ..._v2`) cohabit with v1 until migration done.

---

## 15. Success criteria (exit to master plan)

Phase 1 ships when:

- [ ] `python -m sprite_gen render --all` produces 5 archetypes × 17 slopes without errors.
- [ ] Promoted sprite loads in Unity with correct PPU/pivot (validated against `Assets/Sprites/House1-64.png` reference).
- [ ] Palette K-means extract reproduces existing sprite class look (eyeball test).
- [ ] Slope-aware foundation renders clean bridge from sloped ground to flat building base on all 17 slopes.
- [ ] Curation CLI (`promote` / `reject`) round-trips without manual `.meta` editing.

Phase 3 ships when:

- [ ] ~60 sprites promoted to `Assets/Sprites/Generated/` covering 15 archetypes.
- [ ] In-game placement test shows sprites render correctly on flat + all slope types.
- [ ] EA-build sprite inventory gap closed (per audit in §17).

When both hit → close this exploration → open `TECH-` issues for any follow-up work (diffusion tuning, v2 primitives, non-square footprints).

---

## 16. Deferred to v2

- **Non-square footprints** (2×1, 1×3, 3×3 irregular). EA ships 1×1 only.
- **Animation frames** (smoke stacks, flicker windows). Static only v1.
- **Water-facing slopes** (16 more variants). Land-only 17 for v1.
- **Additional primitives** (pyramid, cylinder, cone, polygon extrude).
- **Variant permutation expansion** (height ±1 level, window grid randomization).
- **Shadow on ground** — cast shadow offset from building base onto terrain tile. Unsure if Unity-composited or baked into sprite. Revisit after Phase 1 in-game look.
- **Diffusion as default** — stays opt-in unless Phase 2 eval proves quality win.

---

## 17. Open questions (narrowed)

Remaining items that do not block master plan but need answer before Phase 3 promote:

1. **EA archetype list exact** — need concrete naming for the 15 archetypes (e.g., `res_small_brick`, `res_medium_stucco`, `com_office_short`, …). Draft during Phase 1.
2. **Shadow on ground decision** — bake in sprite vs Unity runtime composite. Test both in Phase 1 dev scene.
3. **Existing sprite audit** — how many current sprites are keepers vs replace-with-generated. Audit pass before Phase 3.
4. **Palette cluster count per class** — default 8 likely fine; may need 10-12 for industrial (more material variety).

Non-blocking — answered in-flight.

---

## 18. Next step

**Seed master plan.** This doc is ready. Invoke `master-plan-new` skill with this file as the design input to produce `ia/projects/sprite-gen-master-plan.md` carrying:

- Stage 1: Phase 1 Geometry MVP (weeks 1-2.5) → ~5 TECH- issues
- Stage 2: Phase 2 Diffusion overlay (weeks 3-4) → ~3 TECH- issues
- Stage 3: Phase 3 EA bulk render + curation (week 5) → ~2 TECH- / ART- issues

Then `/stage-file` each stage → `/kickoff` first issue → build.
