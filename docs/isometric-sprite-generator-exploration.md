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
| 17 | Editor integration | Aseprite v1.3.17 (licensed, Steam/dmg). Tier 1 `.gpl` palette exchange (lands in Stage 1.3). Tier 2 layered `.aseprite` emission + `promote --edit` round-trip (lands in Stage 1.4). Tier 3 Aseprite Lua YAML runner deferred — duplicates composer. |

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

### Aseprite palette interop (Tier 1)

`palette export {class}` writes `tools/sprite-gen/palettes/{class}.gpl` (GIMP palette format, 3 swatches per material: `{material}_bright`, `_mid`, `_dark`) — loadable in Aseprite via **Palette → Presets → Load**.

`palette import {class} --gpl path.gpl` parses `.gpl`, matches swatch names back to materials by suffix, writes `palettes/{class}.json`. Bypasses K-means when human-curated palette preferred over extracted.

Round-trip: K-means extract → `.json` → `.gpl` → hand-tune in Aseprite → save → `.gpl` → `.json` overwrite. JSON is authoritative at render time; `.gpl` is the editor-facing mirror.

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

# export palette JSON to .gpl for Aseprite editing (Tier 1)
python -m sprite_gen palette export residential

# import Aseprite-edited .gpl back to palette JSON (Tier 1)
python -m sprite_gen palette import residential \
  --gpl tools/sprite-gen/palettes/residential.gpl

# render with layered .aseprite output (Tier 2) — top/south/east/foundation as named layers
python -m sprite_gen render building_residential_small --layered

# promote a rendered variant (copies PNG + writes .meta)
python -m sprite_gen promote out/building_residential_small_v02.png \
  --as building_residential_small_01

# promote with editor round-trip (Tier 2): opens .aseprite in Aseprite,
# waits for save, flattens via CLI, writes PNG + .meta
python -m sprite_gen promote out/building_residential_small_v02.aseprite \
  --as building_residential_small_01 --edit

# reject all variants of an archetype (cleans out/)
python -m sprite_gen reject building_residential_small

# Phase 2: re-render with diffusion pass
python -m sprite_gen render building_residential_small --diffusion
```

Exit codes: 0 = success, 1 = spec invalid, 2 = palette missing, 3 = diffusion backend unavailable, 4 = Aseprite binary not found (Tier 2 commands only).

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

### Editor round-trip (Tier 2)

Alternate path when a variant is close but needs hand-polish before promote:

1. `render --layered {archetype}` writes `out/{name}_vNN.aseprite` (named layers: `top`, `south`, `east`, `foundation` if present) alongside the flat PNG.
2. Author opens `.aseprite` in Aseprite, edits pixels per layer, saves in place.
3. `promote out/{name}_vNN.aseprite --as final_name --edit` invokes Aseprite CLI `--batch --save-as {tmp}.png` to flatten, then runs normal promote (copy PNG to `Assets/Sprites/Generated/`, write `.meta`).

Aseprite binary discovered in this order: `$ASEPRITE_BIN` env var → `tools/sprite-gen/config.toml` `[aseprite] bin = ...` → platform default probe (macOS: `/Applications/Aseprite.app/Contents/MacOS/aseprite`, then Steam `~/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite`). Missing binary → exit code 4 with install hint.

Layered `.aseprite` emission uses `py_aseprite` (or equivalent) writer in `src/aseprite_io.py`; flat PNG always co-emits so non-Aseprite users stay unblocked.

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

## 18. Aseprite editor integration

### Rationale

Human-authored pixel art is part of the pipeline — generator produces mass, editor applies taste. Aseprite v1.3.17 (licensed via Steam/dmg) chosen over Libresprite: active Lua API (`Dialog`, `app.command`, `Tilemap`), richer CLI (`--filename-format`, `--sheet-data`), modern `.aseprite` chunk support (tilesets, external files) without round-trip loss. Libresprite's frozen 2016 fork would bite on layered emission + scripted curation UIs (see research log in conversation history).

### Tier 1 — Palette exchange (Stage 1.3)

**Scope:** `.gpl` ⇄ palette JSON round-trip. Human can hand-curate per-class palette in Aseprite instead of accepting K-means output.

**Touch points:**
- `src/palette.py` — add `export_gpl(cls)` + `import_gpl(cls, path)` functions.
- `src/cli.py` — add `palette export {class}` + `palette import {class} --gpl`.
- `tools/sprite-gen/palettes/{class}.gpl` — generated, gitignored by default (JSON is source of truth) or checked in when Aseprite is authoritative (per-class toggle in config).

**Exit:** `palette export residential && palette import residential --gpl palettes/residential.gpl` round-trips without material-name loss.

### Tier 2 — Layered `.aseprite` round-trip (Stage 1.4)

**Scope:** Composer emits editable `.aseprite` alongside flat PNG. `promote --edit` opens in Aseprite, waits for save, flattens via Aseprite CLI, writes `.meta` on the PNG.

**Touch points:**
- `src/aseprite_io.py` — new module. `write_layered_aseprite(path, layers: dict[str, PIL.Image])` using `py_aseprite` (or equivalent `.aseprite` writer). Layer names: `top`, `south`, `east`, `foundation` (only when non-flat terrain).
- `src/compose.py` — composer already paints per-face; split into per-layer buffers when `--layered` flag set, emit both flat PNG + `.aseprite`.
- `src/curate.py` — `promote(..., edit=False)` branch: if input is `.aseprite` and `--edit` set, launch Aseprite subprocess `{bin} --batch {src}.aseprite --save-as {tmp}.png`, then normal promote pipeline on the flattened PNG.
- `src/aseprite_bin.py` — binary discovery (`$ASEPRITE_BIN` → config.toml → macOS default probes). Exit code 4 on not-found with install hint.
- `src/cli.py` — `render --layered` flag, `promote --edit` flag.

**Exit:** `render --layered building_residential_small` emits `.aseprite` with 3-4 named layers; opening in Aseprite shows editable layers; `promote ... --edit` round-trips to `Assets/Sprites/Generated/` with correct `.meta`.

### Deferred (Tier 3)

Aseprite Lua script that reads sprite-gen YAML specs and draws primitives natively in-editor. Duplicates Python composer. Defer until EA ships and a clear authoring-in-editor use case emerges.

### Not in scope

- Aseprite Dialog UI for curation (approve/reject buttons inside editor) — curation stays CLI.
- Aseprite tilemap mode for slope variants — slopes stay in `slopes.yaml`.
- Auto-launch Aseprite on `render` — editor opens on demand via `promote --edit` only.

---

## 19. Next step

**Master plan already seeded** (`ia/projects/sprite-gen-master-plan.md`, Step 1 / Stage 1.1 pending). Amend master plan to absorb §18 Tier 1 + Tier 2 touch points:

- **Stage 1.3** gains Phase 2 / Phase 3 tasks for `.gpl` export/import + CLI wiring.
- **Stage 1.4** gains Phase 3 tasks for layered `.aseprite` emission, binary discovery, `promote --edit` + exit code 4.

Then `/stage-file sprite-gen-master-plan.md Stage 1.1` → `/kickoff` first issue → build.

---

## Design Expansion — MVP Alignment

> **Purpose:** Closes three gaps between the sprite-gen master plan (3 steps) and the full-game MVP umbrella (Bucket 5 = 5 steps, exit gate requires animation descriptor YAML locked + archetype coverage for Zone S + utilities + landmarks). Source: `ia/projects/full-game-mvp-master-plan.md` bucket table + `docs/full-game-mvp-exploration.md` Bucket 5 step outline. Does NOT overwrite prior sections — appends only.

---

### A. Animation descriptor YAML contract

#### What city-sim-depth needs from Bucket 5

Bucket 2 (city-sim-depth) is gated on the animation descriptor YAML being locked. Specifically, Bucket 2 consumes:

- **Construction evolution** — buildings progress through `N` construction stages per building type × density tier. Each stage = a distinct sprite (or sprite row in a sheet). Bucket 2's `ConstructionStageController` must know: how many stages exist, which sprite asset corresponds to each stage, and what triggers the switch (sim tick + desirability threshold).
- **Traffic flow anim swap** — road strokes render different sprites per traffic level. Bucket 2 Step 3 locks the state machine: `low / medium / high / jammed`. `SignalOverlayRenderer` reads descriptor to know which sprite row to display.
- **Crime / protest / violence events** — hotspot event emitter triggers a short animation overlay on the building tile. Descriptor must declare: which buildings have a crime event variant, what the sprite sheet layout is, how many frames.

Bucket 2 does NOT need actual finished animation frames to start — it needs the **descriptor schema locked** so C# consumer stubs can be written against a stable contract. Sprites can be placeholder stubs (even single-frame PNGs) as long as the descriptor fields are final.

#### Schema design — `tools/sprite-gen/anim-descriptors/{archetype}.anim.yaml`

```yaml
id: building_residential_small             # matches specs/{archetype}.yaml id prefix
class: residential                          # asset class — drives palette + consumer routing
static_sprite: building_residential_small  # base promoted sprite id (Assets/Sprites/Generated/)

construction_stages:                        # list ordered 0..N-1; 0 = ground-cleared / foundation
  - stage: 0
    label: foundation
    sprite: building_residential_small_construction_00
    duration_ticks: 2                       # game-months at default speed; tunable
  - stage: 1
    label: framing
    sprite: building_residential_small_construction_01
    duration_ticks: 3
  - stage: 2
    label: complete
    sprite: building_residential_small     # final = static_sprite reference (no separate asset needed)

traffic_anim:                              # optional; only present on road-stroke archetypes
  enabled: false

event_anims:                               # per-event overlay sheet descriptors
  - event: crime_protest
    enabled: true
    sheet: building_residential_small_crime_sheet
    frame_count: 4
    frame_w: 64                            # px; must match static sprite canvas width
    frame_h: 64
    fps: 6
    loop: false
    trigger: crime_hotspot_threshold       # C# enum value — wired by SignalOverlayRenderer

diffusion_prompt_hint: >                   # optional; fed to Step 3 agent-driven path
  isometric pixel art small brick house under construction, scaffolding visible, clean edges
```

Road-stroke archetype variant (for traffic):

```yaml
id: road_stroke_2lane
class: road
static_sprite: road_stroke_2lane_flat
traffic_anim:
  enabled: true
  states:
    low:    { sprite: road_stroke_2lane_traffic_low }
    medium: { sprite: road_stroke_2lane_traffic_med }
    high:   { sprite: road_stroke_2lane_traffic_high }
    jammed: { sprite: road_stroke_2lane_traffic_jam }
```

#### Who produces the descriptor

**Sprite-gen CLI generates a stub descriptor** alongside every promoted sprite, with all fields present but construction stages defaulting to a single `complete` stage pointing to the static sprite, and `event_anims[*].enabled: false`. Human (or agent-driven Step 3 pass) enriches the descriptor with actual construction stage sprites and event sheet references as those assets land.

CLI addition: `python -m sprite_gen promote out/X.png --as name` → also writes `tools/sprite-gen/anim-descriptors/{archetype}.anim.yaml` stub if not present. If present, leaves untouched (human-curated takes precedence).

#### When descriptor schema locks (unlock condition)

Schema locked at **Step 2 close** (animation architecture step). C# consumer stubs in Bucket 2 (`ConstructionStageController`, `SignalOverlayRenderer`, event emitter) can be authored against the locked schema from that point. Sprite assets filling the descriptors land progressively in Steps 3–4. Step 5 (archetype expansion) extends coverage to S + utility + landmark descriptors using the same schema — no breaking change.

**Bucket 2 Tier B entry condition:** Step 2 must be `Final` (not just the schema draft — actual stub descriptors checked in for all Step 3 EA archetypes so C# consumer wiring can be smoke-tested).

#### Unity C# consumer contract

Consumer reads `tools/sprite-gen/anim-descriptors/*.anim.yaml` at build time (or runtime via Resources/Addressables — decision deferred to Step 2 kickoff). Key access patterns:

- `AnimDescriptor.GetConstructionSprite(archetype, stage)` → `Sprite`
- `AnimDescriptor.GetTrafficSprite(archetype, trafficLevel)` → `Sprite`
- `AnimDescriptor.GetEventSheet(archetype, eventType)` → `SpriteSheet`

No new C# classes authored in sprite-gen steps — consumer stubs are Bucket 2 scope. Sprite-gen Step 2 owns the schema; Bucket 2 Step 6 (`ConstructionStageController`) wires the stubs.

---

### B. Zone S + utilities + landmarks archetype scope

#### Class inventory (new classes beyond R/C/I)

| Class slug | Zone/source | Examples | Notes |
|---|---|---|---|
| `service` | Zone S (Bucket 3) | police station, fire station, school, hospital, park entrance | 5 service sub-types per Bucket 2 Step 4 (FEAT-52). One primary archetype + 1–2 density variants per sub-type |
| `utility` | Bucket 4 utility pools | water tower, power plant (small), sewage treatment, water pump | Multi-cell footprints likely (2×2 or 3×3); confirm at Bucket 4 spec time |
| `landmark` | Bucket 4 progression | city hall (scale-unlock city), regional parliament (scale-unlock region), country monument (scale-unlock country), big-project civic building | Scale-unlock and saved-for project types; 1–2 per scale |

#### Archetype count estimate

| Class | Sub-types | Archetypes per sub-type | Total archetypes | Variants per archetype | Estimated sprite total (×17 slopes) |
|---|---|---|---|---|---|
| `service` | 5 (police / fire / school / hospital / park) | 2 (small + medium) | 10 | 2 | 340 |
| `utility` | 3 (water / power / sewage) | 2 (small + large) | 6 | 2 | 204 |
| `landmark` | 3 per scale × 3 scales + 1 big-project | ~4 (scale-unlock × 3 + 1 civic) | 4 | 1 (landmarks = unique) | 68 |
| **Total new** | | | **~20** | | **~612** |
| **Existing R/C/I (Step 3)** | | | 15 | 4 | 1020 |
| **Grand total** | | | **~35** | | **~1630 rendered → ~140–180 promoted** |

Sprite library target = ~300 variants (umbrella locked). 140–180 promoted from ~1630 rendered is achievable with 4:1+ curation ratio. Non-1×1 footprints (utility buildings) need the footprint system deferred from v1 — this is the first hard dependency: utility archetypes require the non-square footprint feature (currently deferred to v2, §16).

#### Footprint dependency flag

Utility buildings (water tower 2×2, sewage plant 3×3) need non-square footprint support (`fx ≠ 1` or `fy ≠ 1`). Currently listed under §16 Deferred to v2. **This is a constraint:** either (a) Step 5 (archetype expansion) must wait for non-square footprint to land in a Step 2/3/4 side-stage, or (b) utility archetypes are authored as 1×1 approximations for MVP and upgraded post-beta. Decision deferred to Step 5 kickoff — flag as open question D.1 below.

#### When do they land in the step sequence

Archetype specs for S / utility / landmark classes **cannot be fully authored until Bucket 3/4 define the exact building inventory** (sub-types, footprints, upgrade paths). However, **palette files and class-level scaffolding** can be authored speculatively in Step 3 (after R/C/I bulk render):

- Step 3 exit: `palettes/service.json`, `palettes/utility.json`, `palettes/landmark.json` checked in (K-means from any placeholder sprites or hand-authored).
- Step 5 opens when Bucket 3 Step 1 provides Zone S building inventory + Bucket 4 Step 1 provides utility building inventory. Step 5 authors the actual archetype YAML specs, renders, curates, and locks descriptors for all new classes.

**Step 5 entry gate (hard dependency):** Bucket 3 Step 1 Final + Bucket 4 Step 1 Final. Otherwise archetype spec authoring is blind (field counts, footprints, visual design language unknown).

---

### C. Revised 5-step spine

The existing 3-step spine maps to Steps 1, 3 (closest), and 5. Steps 2 and 4 are insertions.

| Step | Name | Objectives | Exit criteria | Cross-bucket dependencies | Timing note |
|---|---|---|---|---|---|
| **1** | Geometry MVP | Canvas math, `iso_cube`/`iso_prism`, palette system, slope-aware foundation, curation CLI, Aseprite Tier 1+2. | `render --all` ≥5 archetypes × 17 slopes; promoted sprites Unity-correct; `palette extract` round-trip; `promote --edit` functional. | None (Tier A — can start immediately) | In progress — Stage 1.4 active |
| **2** | Animation architecture + descriptor schema lock | Author `anim-descriptors/` schema. CLI stub-emit on promote. Bootstrap stub descriptors for all Step 3 archetypes. Define C# consumer interface (authored in Bucket 2, but interface locked here). Pilot construction stage sprites for 2 archetypes. | `*.anim.yaml` schema published + stable. Stub descriptors checked in for all Step 3 archetypes. C# consumer interface documented (not implemented — Bucket 2 scope). Pilot construction PNGs promoted for ≥2 archetypes. | Feeds Bucket 2 Tier B entry. No inbound blocker beyond Step 1 Final. | Opens immediately after Step 1 Final |
| **3** | EA bulk render + agent-driven anim path | Author all 15 R/C/I archetype specs. Batch render ≈1000 sprites. Curation session → promote ~60–80 to `Assets/Sprites/Generated/`. Agent-driven prompt-to-animated-sprite path for construction stages. Traffic anim sprite variants for road archetypes. | All 15 R/C/I archetype specs checked in. ~60–80 sprites promoted. Anim descriptors filled for construction stages (≥5 archetypes). Traffic anim sprites exist for road stroke variants (low/med/high/jammed). Unity import audit passes. | Feeds Bucket 3 (RCIS tile assets needed at Bucket 3 Step 1 kickoff). Bucket 2 Step 3 (traffic anim) needs traffic sprite variants. | Opens after Step 2 Final; parallel with Bucket 2 in Tier B |
| **4** | Dedicated animation-generator tool + anim finalization | Build `tools/anim-gen/` (or sub-module) for deterministic frame-by-frame animation generation. Cover: fire, smoke, crime/protest/violence event overlays, water wave. Finalize anim descriptors for all Step 3 archetypes. | `anim-gen` CLI produces ≥4-frame sheets for fire + smoke + crime event overlays. All Step 3 archetype anim descriptors complete (all `event_anims` fields filled). Anim sheet `.meta` correct. | Feeds Bucket 2 Step 6 (ConstructionStageController + CrimeSystem event trigger) — must land before Bucket 2 Step 6 kickoff. | Opens after Step 3 Final; can overlap with Bucket 2 Steps 1–5 |
| **5** | Archetype library expansion (S + utilities + landmarks) | Author ~20 new archetype YAML specs (service × 10, utility × 6, landmark × 4). Extract palettes for new classes. Batch render + curate. Bootstrap anim descriptors for all new classes. Verify ~300-variant target reachable. | All ~20 new archetype specs checked in + renderable. Palette JSON files for `service`, `utility`, `landmark` classes present. ~70–100 new sprites promoted (cumulative library ~140–180). Anim descriptor stubs for all new archetypes checked in. Footprint scope decision documented (1×1 MVP vs non-square). | Hard entry gate: Bucket 3 Step 1 Final + Bucket 4 Step 1 Final (building inventory defined). Feeds Bucket 4 (landmark/utility sprite variants), Bucket 6 (icons/splash assets). | Opens in Tier C — after Bucket 3 Step 1 + Bucket 4 Step 1 Final |

**Mapping to umbrella exit gate ("Geometry MVP Final; animation descriptor YAML locked; archetype coverage S + utilities + landmarks"):**

- "Geometry MVP Final" → Step 1 Final.
- "Animation descriptor YAML locked" → Step 2 Final (schema + stubs).
- "Archetype coverage (S + utilities + landmarks)" → Step 5 Final.
- Steps 3 + 4 = intermediate deliverables (EA bulk render + anim pipeline) required before Step 5 can close cleanly.

**Step count: 5. Matches umbrella "In progress — Step 1 of 5".**

---

### D. Open questions

| # | Question | Blocks | Earliest resolution |
|---|---|---|---|
| D.1 | **Non-square footprints for utility archetypes.** Utility buildings (water tower 2×2, sewage plant 3×3) need `fx ≠ 1 / fy ≠ 1`. Currently deferred to v2. Option A: land non-square support in Step 2 or Step 3 side-stage. Option B: utility archetypes render as 1×1 approximations for MVP; non-square post-beta. | Step 5 archetype spec authoring for utility class | Step 5 kickoff (need inventory from Bucket 4 Step 1 first) |
| D.2 | **C# consumer loading path.** Anim descriptors loaded at build time (Resources/Addressables) or runtime YAML parse? Determines where schema-version management lives. | Step 2 (consumer interface doc needs this locked) | Step 2 kickoff |
| D.3 | **Construction stage count per building type.** Bucket 2 Step 6 owns the C# side; sprite-gen Step 3 authors the sprites. Need agreed default (3 stages = foundation / framing / complete?) before Step 3 archetype spec authoring. Otherwise descriptors are placeholders. | Step 3 archetype spec authoring completeness | Bucket 2 Step 6 kickoff (should precede Step 3 close, or at minimum agree contract at Step 2) |
| D.4 | **Landmark visual design language.** Scale-unlock landmarks + big-project buildings need a distinct visual vocabulary to signal "special" vs ordinary R/C/I. No palette/primitive guidance exists yet. | Step 5 landmark archetype specs | Bucket 4 Step 3 kickoff (landmarks progression step) |
| D.5 | **~300-variant target achievable with 1×1-only footprints?** Umbrella locks ~300 variants. Gap analysis: 15 R/C/I × ~4 promoted variants = ~60; 20 new classes × ~4 variants = ~80; total = ~140. To reach 300, need either slope variants counted, non-square multi-cell footprints, or higher variant-per-archetype curation. Clarify counting convention (does each slope terrain variant count as a separate "variant" toward the ~300 target?). | Curation target-setting for Steps 3 + 5 | Before Step 3 decomposes |

---

### Review Notes

> **Subagent Plan review result (blocking items + non-blocking carried verbatim).**

**BLOCKING items reviewed (all resolved):**

1. **Schema fields missing `frame_h` default.** Event sheet `frame_h` field added to schema (§A, `event_anims` block now includes `frame_w: 64`, `frame_h: 64`). ✓ Resolved in-place.

2. **Step 2 exit criteria gap — C# interface "documented" is too weak.** Clarified: Step 2 exit requires "C# consumer interface documented (not implemented — Bucket 2 scope)" — explicit parenthetical distinguishes Bucket 2 implementation scope from sprite-gen doc scope. ✓ Resolved in-place.

3. **Step 5 entry gate ambiguity — "Bucket 3 Step 1 + Bucket 4 Step 1 Final" leaves utility palette extraction orphaned.** Added explicit guidance: palette JSON for `service`/`utility`/`landmark` classes authored speculatively in Step 3 (no inventory needed for palette skeleton); Step 5 authors the full archetype YAML specs against confirmed Bucket 3/4 inventories. ✓ Resolved in-place.

**NON-BLOCKING items (carried verbatim):**

- "D.5 variant count ambiguity re: slope terrain — the ~300 target counting convention (does each of the 17 slope variants of the same archetype count as a separate variant?) should be answered before Step 3 curation guidance is written. Currently carried as open question D.5. Recommend confirming with umbrella author before Step 3 decompose."
- "The `diffusion_prompt_hint` field in the anim descriptor is useful but optional. Risk: if the field is omitted on most archetypes, Step 3's agent-driven path has no per-archetype context. Consider making it required (even if value is a class-level fallback string) to avoid blank-prompt diffusion passes."
- "Step 4 'dedicated anim-gen tool' scope is under-specified (no CLI sketch, no primitive type, no frame-interpolation strategy). Sufficient for master-plan-extend input but will need a design-explore sub-pass before Step 4 decomposes."
