# Isometric Sprite Generator — Master Plan (Tools / Art Pipeline)

> **Status:** In Progress — Stage 1.3 (Stages 1.1–1.2 Final — 12 tasks archived; Stage 1.3 filed 2026-04-15 — 6 tasks **TECH-153..158**)
>
> **Scope:** Build `tools/sprite-gen/` — a Python CLI + 5-layer hybrid composer that renders isometric pixel art building sprites from YAML archetype specs, with slope-aware foundations, per-class palette management, and a curation workflow that promotes approved PNGs to `Assets/Sprites/Generated/`. Diffusion overlay (Phase 2) and EA bulk render (Phase 3) follow once geometry MVP ships. Non-square footprints, animation frames, water-facing slopes, and additional primitives are out of scope for v1.
>
> **Exploration source:** `docs/isometric-sprite-generator-exploration.md` (§2 Locked decisions, §3 Architecture, §5–§9 Primitive/Palette/Slope/YAML/Folder design, §13 Phase plan, §15 Success criteria — all are ground truth).
>
> **Locked decisions (do not reopen in this plan):**
> - North star: unblock EA shipping — geometry-only MVP ships first; diffusion is opt-in.
> - Asset scope v1: buildings + slope-aware foundations only; terrain slope tiles stay hand-drawn.
> - Canvas math: `width = (fx+fy)×32`, `height = multiple of 32`; diamond bottom-center anchor.
> - Language: Python (diffusers ecosystem, Pillow/numpy/scipy, no compile step, Unity-isolated).
> - Primitives v1: `iso_cube` + `iso_prism` only; `iso_stepped_foundation` auto-inserted.
> - Palette: K-means auto-extract per class; 3-level ramp (bright/mid/dark); per-class JSON.
> - Generation architecture: 5-layer composer (primitive → compose+shade → palette → diffusion → curation).
> - Slope coverage: 17 land variants; water-facing deferred to v2.
> - EA scope: ~15 archetypes, all 1×1 **building footprint**.
> - Editor integration: Aseprite v1.3.17 (licensed). Tier 1 `.gpl` palette exchange in Stage 1.3. Tier 2 layered `.aseprite` emission + `promote --edit` round-trip in Stage 1.4. Tier 3 (Lua YAML runner) deferred.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
> - `ia/projects/multi-scale-master-plan.md` — adds `RegionCell` / `CountryCell` types + parent-scale stubs + save-schema bumps. Sprite-gen v1 renders only city-scale 1×1 buildings; region / country scale sprite needs (cell sprites, city-node-at-region-zoom, region-node-at-country-zoom) surface when multi-scale Step 4 opens — see Deferred decomposition below.
> - `ia/projects/blip-master-plan.md` — audio subsystem. Disjoint surfaces (Python tool vs Unity C#); no sprite-gen collision.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
> - `docs/isometric-sprite-generator-exploration.md` — full design + architecture + examples. §2 Locked decisions + §3 Architecture + §13 Phase plan are ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — no runtime C# invariants at risk (tool is Python, Unity-isolated). Unity import pivot/PPU correctness enforced by `unity_meta.py` in Stage 1.4.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

---

### Stage 1 — Geometry MVP / Scaffolding + Primitive Renderer (Layer 1)

**Status:** Final (6 tasks archived as **TECH-123** through **TECH-128**; BACKLOG state: 6 archived / 6)

**Objectives:** Bootstrap `tools/sprite-gen/` folder structure and implement the two core primitives (`iso_cube`, `iso_prism`) with NW-light 3-level shade pass. Canvas sizing + Unity pivot math extracted to `canvas.py`. Unit tests validate pixel-perfect output against canonical canvas examples from the exploration doc.

**Exit:**

- `tools/sprite-gen/` layout matches §9 Folder layout: `src/`, `tests/`, `specs/`, `palettes/`, `out/` (.gitignored), `requirements.txt`, `slopes.yaml` stub
- `iso_cube(w, d, h, material)` renders top rhombus + south parallelogram + east parallelogram with correct 3-level ramp (bright/mid/dark per face)
- `iso_prism(w, d, h, pitch, axis, material)` renders sloped top-faces + triangular end-faces with same shade logic
- `canvas_size(fx, fy, extra_h)` returns `((fx+fy)*32, extra_h)` matching §4 Baseline formula
- `pivot_uv(canvas_h)` returns `(0.5, 16/canvas_h)` matching §4 Unity import defaults
- `pytest tools/sprite-gen/tests/` exits 0 — `test_canvas.py` + `test_primitives.py` pass with no errors. `npm run validate:all` does NOT yet cover Python; pytest stays a manual gate until CI integration lands (candidate fold-in point: Stage 1.3 palette tests, when test surface stabilizes)
- Phase 1 — Project bootstrap + canvas math module.
- Phase 2 — iso_cube + iso_prism primitives with NW-light shade pass.
- Phase 3 — Unit tests for canvas math + primitives.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Folder scaffold | **TECH-123** | Done | Create `tools/sprite-gen/` folder skeleton: `src/__init__.py`, `src/primitives/__init__.py`, `tests/fixtures/` dir, `out/` dir (add to `.gitignore`), `requirements.txt` (pillow, numpy, scipy, pyyaml), `README.md` stub |
| T1.2 | Canvas math module | **TECH-124** | Done | `src/canvas.py` — implement `canvas_size(fx, fy, extra_h=0) → (w, h)` using `(fx+fy)*32` width formula; `pivot_uv(canvas_h) → (0.5, 16/canvas_h)`; docstring cites §4 Canvas math from exploration doc |
| T1.3 | iso_cube primitive | **TECH-125** | Done | `src/primitives/iso_cube.py` — `iso_cube(canvas, x0, y0, w, d, h, material)`: draw top rhombus (bright), south parallelogram (mid), east parallelogram (dark) using Pillow polygon fills; NW-light direction hardcoded; pixel coordinates computed from 2:1 isometric projection (tileWidth=1, tileHeight=0.5 per **Tile dimensions**) |
| T1.4 | iso_prism primitive | **TECH-126** | Done | `src/primitives/iso_prism.py` — `iso_prism(canvas, x0, y0, w, d, h, pitch, axis, material)`: two sloped top faces + two triangular end-faces; `axis ∈ {'ns','ew'}` selects ridge direction; same bright/mid/dark ramp as iso_cube |
| T1.5 | Canvas unit tests | **TECH-127** | Done (archived) | `tests/test_canvas.py` — assert `canvas_size(1,1)=(64,0)`, `canvas_size(1,1,32)=(64,32)`, `canvas_size(3,3,96)=(192,96)`; assert `pivot_uv(64)=(0.5,0.25)`, `pivot_uv(128)=(0.5,0.125)`, `pivot_uv(192)=(0.5, 16/192)` — matches §4 Examples table |
| T1.6 | Primitive smoke tests | **TECH-128** | Done (archived) | `tests/test_primitives.py` — render `iso_cube(w=1,d=1,h=32,material=STUB_RED)` on `canvas_size(1,1,32)=(64,32)` canvas; assert non-zero alpha per face bbox (top/south/east); same smoke for `iso_prism` both axes (pitch=0.5); save fixtures to `tests/fixtures/` tracked in git; re-export `iso_prism` from `primitives/__init__.py` |

---

### Stage 2 — Geometry MVP / Composition + YAML Schema + CLI Skeleton (Layer 2)

**Status:** Final (6 tasks archived as **TECH-147** through **TECH-152**; closed 2026-04-15)

**Objectives:** Wire primitives into a compose layer that reads YAML archetype specs and stacks primitives onto a canvas buffer. Implement CLI `render {archetype}` + `render --all` commands with seed-based variant permutation. Ship first archetype spec `building_residential_small.yaml` and validate round-trip to `out/`.

**Exit:**

- `compose.py` `compose_sprite(spec_dict) → PIL.Image` stacks all primitives from spec `composition:` list in order
- `spec.py` validates required YAML fields (id, class, footprint, terrain, composition, palette, output); exits with code 1 on invalid
- `cli.py render building_residential_small` writes `out/building_residential_small_v01.png` … `_v04.png` at correct canvas size
- `cli.py render --all` discovers all `specs/*.yaml` and renders all without crash
- Seed-based variant permutation applies material swap within class, window pattern shift, prism pitch ±20%
- `specs/building_residential_small.yaml` checked in with 4 variants, flat terrain, palette=residential
- Phase 1 — compose.py (Layer 2) + YAML spec loader/validator.
- Phase 2 — CLI render + render --all commands.
- Phase 3 — First archetype spec + integration smoke test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Compose layer | **TECH-147** | Done (archived) | `src/compose.py` — `compose_sprite(spec: dict) → PIL.Image`: create canvas via `canvas_size(fx, fy, extra_h=0)`, iterate `composition:` list, dispatch each entry to matching primitive (iso_cube / iso_prism), return composited image; `extra_h` derived from tallest primitive stack |
| T2.2 | YAML spec loader | **TECH-148** | Done (archived) | `src/spec.py` — `load_spec(path) → dict`: load YAML + validate required keys (id, class, footprint, terrain, composition, palette, output); `SpecValidationError` raised on missing/malformed fields; CLI catches and exits with code 1 (per §10 exit codes) |
| T2.3 | Render CLI command | **TECH-149** | Done (archived) | `src/cli.py` — `render {archetype}` command: resolve `specs/{archetype}.yaml`, load + validate spec, call `compose_sprite` N times (variants count from spec), apply seed-based permutations (material swap within class, prism pitch ±20%), write `out/{name}_v01.png` … `_v{N:02d}.png` |
| T2.4 | Render --all command | **TECH-150** | Done (archived) | `src/cli.py` — `render --all` command: glob `specs/*.yaml`, iterate, call `render {archetype}` logic per spec; collect errors per spec (exit 0 only if all succeeded, else print failed archetypes + exit 1); `--terrain {slope_id}` CLI flag overrides spec `terrain` field (matches §10 CLI interface) |
| T2.5 | First archetype YAML | **TECH-151** | Done | `specs/building_residential_small.yaml` — first archetype: `id: building_residential_small_v1`, `class: residential`, `footprint: [1,1]`, `terrain: flat`, `levels: 2`, `seed: 42`, `variants: 4`; composition: iso_cube×2 (wall_brick_red) + iso_prism (roof_tile_brown, pitch=0.5, axis=ns); `palette: residential`; `diffusion.enabled: false` |
| T2.6 | Integration smoke test | **TECH-152** | Done | Integration smoke: run `python -m sprite_gen render building_residential_small` in CI-friendly subprocess; assert `out/building_residential_small_v01.png` exists + PIL open succeeds + image size == (64, 64); assert 4 variant files written; no exception raised |

---

### Stage 3 — Geometry MVP / Palette System (Layer 3)

**Status:** Done (all 9 tasks **TECH-153** through **TECH-158** complete; T1.3.3+T1.3.4 merged into **TECH-155**; T1.3.7+T1.3.8+T1.3.9 merged into **TECH-158**)

**Objectives:** Implement K-means palette extraction from existing sprites, per-class palette JSON files, and 3-level ramp enforcement at composition time. Wire palette into `compose.py` so each primitive face pulls correct ramp color from the loaded palette. Bootstrap `palettes/residential.json` from `Assets/Sprites/Residential/House1-64.png`. Add Aseprite `.gpl` round-trip (Tier 1 editor integration) so human-curated palettes can override K-means output per class.

**Exit:**

- `palette.py` `extract_palette(class, sources, n_clusters=8)` produces `palettes/{class}.json` with named materials and 3-level ramp (bright/mid/dark)
- `cli.py palette extract {class} --sources "..."` runs extraction + prompts human to name each cluster material
- `apply_ramp(material_name, face) → RGB` resolves correct ramp level per face (top=bright, S=mid, E=dark)
- `compose.py` uses `apply_ramp()` per primitive face instead of hardcoded color
- `palettes/residential.json` checked in with materials: wall_brick_red, roof_tile_brown, window_glass, concrete
- `tests/test_palette.py` passes; ramp HSV scaling verified (bright ×1.2, mid ×1.0, dark ×0.6, clamped)
- `palette export residential` writes `palettes/residential.gpl` loadable in Aseprite **Palette → Presets → Load**; swatch names `{material}_bright/_mid/_dark`
- `palette import residential --gpl path` parses `.gpl` back to JSON without material-name loss (round-trip equality on every material × face)
- Phase 1 — K-means extract + palette JSON writer + CLI command.
- Phase 2 — Palette apply at composition (integrate with compose.py).
- Phase 3 — Palette tests + bootstrap residential palette JSON.
- Phase 4 — Aseprite `.gpl` export / import (Tier 1 editor integration).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | K-means extractor | **TECH-153** | Done (archived) | `src/palette.py` — `extract_palette(cls, source_paths, n_clusters=8) → dict`: open PNGs with Pillow, flatten non-transparent pixels to numpy array, run `scipy.cluster.vq.kmeans2`, for each centroid synthesize 3-level ramp (HSV value ×1.2/1.0/0.6, clamped 0–255); return dict `{cluster_idx: {bright, mid, dark}}` ready for human naming |
| T3.2 | Palette extract CLI | **TECH-154** | In Progress | `src/cli.py` — `palette extract {class} --sources "glob_pattern"` command: call `extract_palette`, print each cluster's color swatch (ANSI 24-bit color block), prompt stdin for material name per cluster, write named result to `tools/sprite-gen/palettes/{class}.json` (matches §6 Palette system JSON schema) |
| T3.3 | Palette apply_ramp | **TECH-155** | Done (archived) | `src/palette.py` — `load_palette(cls) → dict`: read `palettes/{cls}.json`; `apply_ramp(palette, material_name, face) → (R,G,B)`: face ∈ {'top','south','east'} → bright/mid/dark; raise `PaletteKeyError` if material_name not in palette (caught by compose layer, exits code 2 per §10). **Merged with T1.3.4 into TECH-155** — API + sole consumer land atomic. |
| T3.4 | Palette-driven compose | **TECH-155** | Done (archived) | Update `src/compose.py` to call `load_palette(spec['palette'])` once per sprite, pass palette to each primitive call; primitives accept `material: str` + `palette: dict` replacing stub color; `compose_sprite` now fully palette-driven. **Merged with T1.3.3 into TECH-155**. |
| T3.5 | Palette unit tests | **TECH-156** | Done (archived) | `tests/test_palette.py` — mock K-means centroids (3 fixed RGB values), assert 3-level ramp values (bright = centroid HSV-V ×1.2 clamped, dark ×0.6); assert `apply_ramp(palette, 'wall_brick_red', 'top')` returns bright tuple; assert `apply_ramp(..., 'east')` returns dark tuple |
| T3.6 | Bootstrap residential palette | **TECH-157** | Done (archived) | Run `palette extract residential --sources "Assets/Sprites/Residential/House1-64.png"` (or equivalent direct call); hand-name 8 clusters → produce `tools/sprite-gen/palettes/residential.json` with at minimum: wall_brick_red, roof_tile_brown, window_glass, concrete; check in JSON file |
| T3.7 | GPL export command | **TECH-158** | Done (archived) | `src/palette.py` — `export_gpl(cls, dest_path=None) → str`: read `palettes/{cls}.json`, emit GIMP palette format (`GIMP Palette` header + `Name:` + `Columns:` + `R G B name` rows); swatch naming `{material}_{level}` where level ∈ {bright,mid,dark}; 3N rows for N materials; `src/cli.py` — `palette export {class}` command writes `palettes/{class}.gpl`; add `.gpl` to `.gitignore` (JSON is source of truth). **Merged with T1.3.8+T1.3.9 into TECH-158** — round-trip symmetry. |
| T3.8 | GPL import command | **TECH-158** | Done (archived) | `src/palette.py` — `import_gpl(cls, gpl_path) → dict`: parse `.gpl` (skip header, read R G B name rows), group rows by material name (strip `_bright/_mid/_dark` suffix), emit JSON in Stage 1.3 schema; raise `GplParseError` on malformed rows; `src/cli.py` — `palette import {class} --gpl path` command writes/overwrites `palettes/{class}.json`, prints diff vs prior JSON. **Merged into TECH-158**. |
| T3.9 | GPL round-trip test | **TECH-158** | Done (archived) | `tests/test_palette_gpl.py` — round-trip test: start from fixture `palettes/residential.json` → `export_gpl` → parse back with `import_gpl` → assert deep-equal with original (every material × face RGB identical); assert `.gpl` output contains `GIMP Palette` header + 12 swatch rows for 4 materials; assert malformed `.gpl` raises `GplParseError`. **Merged into TECH-158**. |

---

### Stage 4 — Geometry MVP / Slope-Aware Foundation + Curation CLI (Layer 5)

**Status:** In Progress (TECH-175, TECH-176, TECH-177, TECH-178, TECH-179, TECH-180, TECH-181, TECH-182, TECH-183 filed)

**Objectives:** Implement `iso_stepped_foundation` primitive and `slopes.yaml` per-corner Z table. Wire auto-insert logic into the compose layer so any non-flat `terrain` spec field automatically prepends the foundation primitive and grows the canvas. Implement `promote` / `reject` CLI (Layer 5) with `.meta` generation so promoted sprites land in `Assets/Sprites/Generated/` Unity-ready. Add layered `.aseprite` emission + `promote --edit` round-trip (Tier 2 editor integration) so hand-polished variants land in Unity without losing PNG/`.meta` correctness.

**Exit:**

- `tools/sprite-gen/slopes.yaml` covers all 17 land slope variants; slope codes match **Slope variant naming** (`{CODE}-slope.png` in `Assets/Sprites/Slopes/`)
- `iso_stepped_foundation(fx, fy, slope_id, material)` renders stair/wedge geometry from sloped ground plane to flat building base for all 17 variants
- `compose.py` auto-inserts foundation for any `terrain != flat`; canvas height grows by `max_corner_z`; pivot recomputed
- `promote out/X.png --as final_name` copies PNG to `Assets/Sprites/Generated/` + writes `.meta` (PPU=64, pivot=(0.5, 16/h), Point filter, no compression)
- `reject {archetype}` deletes all `out/{archetype}_*.png` files
- Slope regression: `render building_residential_small --terrain N` → output PNG canvas height > 64
- `render --layered {archetype}` emits `.aseprite` alongside flat PNG with named layers `top`, `south`, `east`, `foundation` (only when non-flat); opening in Aseprite shows layers editable separately
- `promote out/X.aseprite --as name --edit` launches Aseprite CLI to flatten, writes PNG + `.meta` to `Assets/Sprites/Generated/`; exits code 4 when Aseprite binary not found with install hint
- Phase 1 — slopes.yaml + iso_stepped_foundation primitive.
- Phase 2 — Composer slope auto-insert + canvas auto-grow.
- Phase 3 — Curation CLI (promote / reject) + .meta writer.
- Phase 4 — Layered `.aseprite` emission + `promote --edit` round-trip (Tier 2 editor integration).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Slopes YAML table | **TECH-175** | Done | `tools/sprite-gen/slopes.yaml` — per-corner Z table (in pixels) for 17 land slope variants: flat, N, S, E, W, NE, NW, SE, SW, NE-up, NW-up, SE-up, SW-up, NE-bay, NW-bay, NW-bay-2, SE-bay, SW-bay; corner keys: n/e/s/w; values: 0 or 16 (per §7 Slope-aware foundation table); codes must match `Assets/Sprites/Slopes/` filename stems exactly per **Slope variant naming** |
| T4.2 | iso_stepped_foundation | **TECH-176** | Done (archived) | `src/primitives/iso_stepped_foundation.py` — `iso_stepped_foundation(canvas, x0, y0, fx, fy, slope_id, material, palette)`: read `slopes.yaml` per-corner Z for slope_id; build stair/wedge pixel geometry bridging sloped ground plane (variable corners) to flat top at `max(n,e,s,w)+2` lip px; draw using `apply_ramp(material, 'south')` / `apply_ramp(material, 'east')` for visible faces |
| T4.3 | Slope auto-insert | **TECH-177** | Done (archived) | Update `src/compose.py` `compose_sprite`: if `spec['terrain'] != 'flat'`, prepend `iso_stepped_foundation(...)` to primitive stack; recalculate `extra_h = max_corner_z` from slopes.yaml; recompute canvas size + pivot via `canvas_size(fx, fy, extra_h)` + `pivot_uv(canvas_h)`; raise `SlopeKeyError` (exit code 1) if slope_id not in slopes.yaml |
| T4.4 | Slope regression tests | **TECH-178** | Done (archived) | Slope regression test spec `specs/building_residential_small_N.yaml` (copy of small, terrain: N); run `python -m sprite_gen render building_residential_small_N`; assert output PNG height > 64 (canvas grew by max_corner_z=16); assert pivot_uv != (0.5, 0.25); render all 17 slope variants via `--terrain` CLI flag; assert no crash |
| T4.5 | Unity meta writer | **TECH-179** | Draft | `src/unity_meta.py` — `write_meta(png_path, canvas_h) → str`: emit Unity `.meta` YAML string with guid (uuid4), textureImporter settings: PPU=64, spritePivot=(0.5, 16/canvas_h), filterMode=Point, textureCompression=None, spriteMode=Single; `src/curate.py` — `promote(src_png, dest_name)`: copy PNG to `Assets/Sprites/Generated/{dest_name}.png`, call `write_meta`, write `.meta` file alongside |
| T4.6 | Promote/reject CLI | **TECH-180** | Draft | `src/cli.py` — `promote out/X.png --as name` command: call `curate.promote()`; assert dest file exists + `.meta` exists; `reject {archetype}` command: glob `out/{archetype}_*.png`, delete all; integration test: promote then reject the same file, assert `Assets/Sprites/Generated/` has promoted file, `out/` is clean after reject |
| T4.7 | Aseprite bin resolver | **TECH-181** | Draft | `src/aseprite_bin.py` — `find_aseprite_bin() → Path`: resolve in order `$ASEPRITE_BIN` env var → `tools/sprite-gen/config.toml` `[aseprite] bin` → platform default probes (macOS: `/Applications/Aseprite.app/Contents/MacOS/aseprite`, then `~/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite`); raise `AsepriteBinNotFoundError` on miss (caught by CLI, exit code 4 with install hint); unit test mocks filesystem + env var |
| T4.8 | Layered aseprite emit | **TECH-182** | Draft | `src/aseprite_io.py` — `write_layered_aseprite(dest_path, layers: dict[str, PIL.Image], canvas_size)`: write `.aseprite` via `py_aseprite` (add to `requirements.txt`) with named layers in stacking order (`foundation`, `east`, `south`, `top`); transparent alpha preserved per layer; update `src/compose.py` to split per-face buffers when `layered=True` flag passed; add `--layered` flag to `cli.py render`; composer always co-emits flat PNG so non-Aseprite users stay unblocked |
| T4.9 | Promote --edit round-trip | **TECH-183** | Draft | `src/curate.py` — extend `promote(src, dest_name, edit=False)`: if `src.suffix == '.aseprite'` and `edit=True`, shell-out `{aseprite_bin} --batch {src} --save-as {tmp}.png` (subprocess, check returncode), then run existing PNG promote pipeline on `{tmp}.png`; cleanup tmp after; `src/cli.py` — `promote ... --edit` flag; integration test: render --layered → modify one layer pixel via PIL → promote --edit → assert flattened PNG + `.meta` exist in `Assets/Sprites/Generated/`, assert modified pixel present in output |

---
