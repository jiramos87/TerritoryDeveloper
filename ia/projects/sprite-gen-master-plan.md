# Isometric Sprite Generator — Master Plan (Tools / Art Pipeline)

> **Status:** In Progress — Stage 7+ (Stage 6 Done 2026-04-23; Stages 6–14 filed as scale-calibration + decoration + footprint-unlock extension, per `docs/sprite-gen-art-design-system.md`)
>
> **Scope:** Build `tools/sprite-gen/` — a Python CLI + N-layer hybrid composer that renders isometric pixel art building sprites from YAML archetype specs, with slope-aware foundations, per-class palette management, a decoration primitive library, multi-footprint support (1×1 / 2×2 / 3×3), tall-canvas growth for multi-floor towers, and a curation workflow that promotes approved PNGs to `Assets/Sprites/Generated/`. Diffusion overlay (Phase 2) and EA bulk render (Phase 3) follow once geometry MVP ships. Non-square footprints (2×1, 3×2, etc.) and animation frames remain out of scope for v1.
>
> **Last updated:** 2026-04-23 (Stages 6–14 appended as the DAS-driven scale-calibration + decoration + footprint-unlock extension. Lock L9 supersedes the earlier "v1 all 1×1" lock; water-facing slopes move in-scope; v1 primitive set expands from 3 to 20).
>
> **Exploration source:**
>
> - `docs/isometric-sprite-generator-exploration.md` (§2 Locked decisions, §3 Architecture, §5–§9 Primitive/Palette/Slope/YAML/Folder design, §13 Phase plan, §15 Success criteria — ground truth for Stages 1–5).
> - `docs/asset-snapshot-mvp-exploration.md` (§7.5 L6 + L7 + L8, §9.1 Architecture, §9.5.A) — extension source for Stage 5 push hook.
> - `docs/sprite-gen-art-design-system.md` — **canonical DAS** (dimensional math, palette anchors, outline policy, 17-primitive decoration set, archetype YAML schema v2) — ground truth for Stages 6–14.
> - `/tmp/sprite-gen-style-audit.md` — DAS polling transcript and audit raw data (197-sprite catalog inventory + bbox measurements + palette extraction).
>
> **Locked decisions (do not reopen in this plan):**
>
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
> - **L6 (2026-04-22):** SOON finish-line = Stage 4 close + Stage 5 push hook. Animation descriptor / EA bulk render / anim-gen / archetype expansion (Steps 2–5 of exploration 5-step spine) stay deferred until MVP triangle closes.
> - **L7 (2026-04-22):** Sprite-gen emits PNG + `.meta` only. Postgres (registry) owns catalog rows; composite objects (panels / buttons / prefabs) are registry-side tables authored post-hoc. No composite sidecar YAML emitted by sprite-gen.
> - **L8 (2026-04-22):** Clean authoring/wiring split — sprite-gen writes catalog rows via HTTP POST `/api/catalog/assets` (never direct SQL, never file bundle). Unity bridge stays read-only from snapshot.
> - **L9 (2026-04-23):** Footprint lock amended — 1×1 + 2×2 + 3×3 all in v1 scope. Non-square footprints (2×1, 3×2, etc.) remain deferred. Water-facing slopes move into v1 (reverses the earlier "water-facing deferred to v2" line). v1 primitive set expands from 3 (iso_cube / iso_prism / iso_stepped_foundation) to 20 (adds `iso_ground_diamond`, `iso_slope_wedge`, plus the 17-primitive decoration set — see DAS R9). Legacy `iso_stepped_foundation` remains available but is no longer the default under-building foundation.
> - **L10 (2026-04-23):** Art calibration ground truth = `docs/sprite-gen-art-design-system.md` (DAS). Every Stage 6+ task cites a DAS section (e.g. "per DAS §4.2") rather than re-specifying rules inline. Audit corpus = all 197 sprites under `Assets/Sprites/` excluding Icons/Buttons/State/Roads. Primary reference: `House1-64.png` for 1×1; `LightResidentialBuilding-2-128.png` for 2×2; `HeavyIndustrialBuilding-1-192.png` for 3×3.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
>
> - `ia/projects/multi-scale-master-plan.md` — adds `RegionCell` / `CountryCell` types + parent-scale stubs + save-schema bumps. Sprite-gen v1 renders only city-scale 1×1 buildings; region / country scale sprite needs (cell sprites, city-node-at-region-zoom, region-node-at-country-zoom) surface when multi-scale Step 4 opens — see Deferred decomposition below.
> - `ia/projects/blip-master-plan.md` — audio subsystem. Disjoint surfaces (Python tool vs Unity C#); no sprite-gen collision.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
>
> - `docs/isometric-sprite-generator-exploration.md` — full design + architecture + examples. §2 Locked decisions + §3 Architecture + §13 Phase plan are ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — no runtime C# invariants at risk (tool is Python, Unity-isolated). Unity import pivot/PPU correctness enforced by `unity_meta.py` in Stage 1.4.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending`_ (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

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


| Task | Name                  | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                 |
| ---- | --------------------- | ------------ | --------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T1.1 | Folder scaffold       | **TECH-123** | Done            | Create `tools/sprite-gen/` folder skeleton: `src/__init__.py`, `src/primitives/__init__.py`, `tests/fixtures/` dir, `out/` dir (add to `.gitignore`), `requirements.txt` (pillow, numpy, scipy, pyyaml), `README.md` stub                                                                                                              |
| T1.2 | Canvas math module    | **TECH-124** | Done            | `src/canvas.py` — implement `canvas_size(fx, fy, extra_h=0) → (w, h)` using `(fx+fy)*32` width formula; `pivot_uv(canvas_h) → (0.5, 16/canvas_h)`; docstring cites §4 Canvas math from exploration doc                                                                                                                                 |
| T1.3 | iso_cube primitive    | **TECH-125** | Done            | `src/primitives/iso_cube.py` — `iso_cube(canvas, x0, y0, w, d, h, material)`: draw top rhombus (bright), south parallelogram (mid), east parallelogram (dark) using Pillow polygon fills; NW-light direction hardcoded; pixel coordinates computed from 2:1 isometric projection (tileWidth=1, tileHeight=0.5 per **Tile dimensions**) |
| T1.4 | iso_prism primitive   | **TECH-126** | Done            | `src/primitives/iso_prism.py` — `iso_prism(canvas, x0, y0, w, d, h, pitch, axis, material)`: two sloped top faces + two triangular end-faces; `axis ∈ {'ns','ew'}` selects ridge direction; same bright/mid/dark ramp as iso_cube                                                                                                      |
| T1.5 | Canvas unit tests     | **TECH-127** | Done (archived) | `tests/test_canvas.py` — assert `canvas_size(1,1)=(64,0)`, `canvas_size(1,1,32)=(64,32)`, `canvas_size(3,3,96)=(192,96)`; assert `pivot_uv(64)=(0.5,0.25)`, `pivot_uv(128)=(0.5,0.125)`, `pivot_uv(192)=(0.5, 16/192)` — matches §4 Examples table                                                                                     |
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


| Task | Name                   | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                            |
| ---- | ---------------------- | ------------ | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T2.1 | Compose layer          | **TECH-147** | Done (archived) | `src/compose.py` — `compose_sprite(spec: dict) → PIL.Image`: create canvas via `canvas_size(fx, fy, extra_h=0)`, iterate `composition:` list, dispatch each entry to matching primitive (iso_cube / iso_prism), return composited image; `extra_h` derived from tallest primitive stack                                                           |
| T2.2 | YAML spec loader       | **TECH-148** | Done (archived) | `src/spec.py` — `load_spec(path) → dict`: load YAML + validate required keys (id, class, footprint, terrain, composition, palette, output); `SpecValidationError` raised on missing/malformed fields; CLI catches and exits with code 1 (per §10 exit codes)                                                                                      |
| T2.3 | Render CLI command     | **TECH-149** | Done (archived) | `src/cli.py` — `render {archetype}` command: resolve `specs/{archetype}.yaml`, load + validate spec, call `compose_sprite` N times (variants count from spec), apply seed-based permutations (material swap within class, prism pitch ±20%), write `out/{name}_v01.png` … `_v{N:02d}.png`                                                         |
| T2.4 | Render --all command   | **TECH-150** | Done (archived) | `src/cli.py` — `render --all` command: glob `specs/*.yaml`, iterate, call `render {archetype}` logic per spec; collect errors per spec (exit 0 only if all succeeded, else print failed archetypes + exit 1); `--terrain {slope_id}` CLI flag overrides spec `terrain` field (matches §10 CLI interface)                                          |
| T2.5 | First archetype YAML   | **TECH-151** | Done            | `specs/building_residential_small.yaml` — first archetype: `id: building_residential_small_v1`, `class: residential`, `footprint: [1,1]`, `terrain: flat`, `levels: 2`, `seed: 42`, `variants: 4`; composition: iso_cube×2 (wall_brick_red) + iso_prism (roof_tile_brown, pitch=0.5, axis=ns); `palette: residential`; `diffusion.enabled: false` |
| T2.6 | Integration smoke test | **TECH-152** | Done            | Integration smoke: run `python -m sprite_gen render building_residential_small` in CI-friendly subprocess; assert `out/building_residential_small_v01.png` exists + PIL open succeeds + image size == (64, 64); assert 4 variant files written; no exception raised                                                                               |


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


| Task | Name                          | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ---- | ----------------------------- | ------------ | --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T3.1 | K-means extractor             | **TECH-153** | Done (archived) | `src/palette.py` — `extract_palette(cls, source_paths, n_clusters=8) → dict`: open PNGs with Pillow, flatten non-transparent pixels to numpy array, run `scipy.cluster.vq.kmeans2`, for each centroid synthesize 3-level ramp (HSV value ×1.2/1.0/0.6, clamped 0–255); return dict `{cluster_idx: {bright, mid, dark}}` ready for human naming                                                                                                                                            |
| T3.2 | Palette extract CLI           | **TECH-154** | In Progress     | `src/cli.py` — `palette extract {class} --sources "glob_pattern"` command: call `extract_palette`, print each cluster's color swatch (ANSI 24-bit color block), prompt stdin for material name per cluster, write named result to `tools/sprite-gen/palettes/{class}.json` (matches §6 Palette system JSON schema)                                                                                                                                                                        |
| T3.3 | Palette apply_ramp            | **TECH-155** | Done (archived) | `src/palette.py` — `load_palette(cls) → dict`: read `palettes/{cls}.json`; `apply_ramp(palette, material_name, face) → (R,G,B)`: face ∈ {'top','south','east'} → bright/mid/dark; raise `PaletteKeyError` if material_name not in palette (caught by compose layer, exits code 2 per §10). **Merged with T1.3.4 into TECH-155** — API + sole consumer land atomic.                                                                                                                        |
| T3.4 | Palette-driven compose        | **TECH-155** | Done (archived) | Update `src/compose.py` to call `load_palette(spec['palette'])` once per sprite, pass palette to each primitive call; primitives accept `material: str` + `palette: dict` replacing stub color; `compose_sprite` now fully palette-driven. **Merged with T1.3.3 into TECH-155**.                                                                                                                                                                                                          |
| T3.5 | Palette unit tests            | **TECH-156** | Done (archived) | `tests/test_palette.py` — mock K-means centroids (3 fixed RGB values), assert 3-level ramp values (bright = centroid HSV-V ×1.2 clamped, dark ×0.6); assert `apply_ramp(palette, 'wall_brick_red', 'top')` returns bright tuple; assert `apply_ramp(..., 'east')` returns dark tuple                                                                                                                                                                                                      |
| T3.6 | Bootstrap residential palette | **TECH-157** | Done (archived) | Run `palette extract residential --sources "Assets/Sprites/Residential/House1-64.png"` (or equivalent direct call); hand-name 8 clusters → produce `tools/sprite-gen/palettes/residential.json` with at minimum: wall_brick_red, roof_tile_brown, window_glass, concrete; check in JSON file                                                                                                                                                                                              |
| T3.7 | GPL export command            | **TECH-158** | Done (archived) | `src/palette.py` — `export_gpl(cls, dest_path=None) → str`: read `palettes/{cls}.json`, emit GIMP palette format (`GIMP Palette` header + `Name:` + `Columns:` + `R G B name` rows); swatch naming `{material}_{level}` where level ∈ {bright,mid,dark}; 3N rows for N materials; `src/cli.py` — `palette export {class}` command writes `palettes/{class}.gpl`; add `.gpl` to `.gitignore` (JSON is source of truth). **Merged with T1.3.8+T1.3.9 into TECH-158** — round-trip symmetry. |
| T3.8 | GPL import command            | **TECH-158** | Done (archived) | `src/palette.py` — `import_gpl(cls, gpl_path) → dict`: parse `.gpl` (skip header, read R G B name rows), group rows by material name (strip `_bright/_mid/_dark` suffix), emit JSON in Stage 1.3 schema; raise `GplParseError` on malformed rows; `src/cli.py` — `palette import {class} --gpl path` command writes/overwrites `palettes/{class}.json`, prints diff vs prior JSON. **Merged into TECH-158**.                                                                              |
| T3.9 | GPL round-trip test           | **TECH-158** | Done (archived) | `tests/test_palette_gpl.py` — round-trip test: start from fixture `palettes/residential.json` → `export_gpl` → parse back with `import_gpl` → assert deep-equal with original (every material × face RGB identical); assert `.gpl` output contains `GIMP Palette` header + 12 swatch rows for 4 materials; assert malformed `.gpl` raises `GplParseError`. **Merged into TECH-158**.                                                                                                      |


---

### Stage 4 — Geometry MVP / Slope-Aware Foundation

**Status:** Done — 4 tasks complete (TECH-175..TECH-178). Curation CLI (promote/reject + Unity `.meta`) + Aseprite Tier-2 integration (layered `.aseprite` emit + `promote --edit` round-trip) relocated to Stage 5 on 2026-04-22 so they sequence atomically with the snapshot push hook (promote → push in one pipeline).

**Objectives:** Implement `iso_stepped_foundation` primitive and `slopes.yaml` per-corner Z table. Wire auto-insert logic into the compose layer so any non-flat `terrain` spec field automatically prepends the foundation primitive and grows the canvas.

**Exit:**

- `tools/sprite-gen/slopes.yaml` covers all 17 land slope variants; slope codes match **Slope variant naming** (`{CODE}-slope.png` in `Assets/Sprites/Slopes/`)
- `iso_stepped_foundation(fx, fy, slope_id, material)` renders stair/wedge geometry from sloped ground plane to flat building base for all 17 variants
- `compose.py` auto-inserts foundation for any `terrain != flat`; canvas height grows by `max_corner_z`; pivot recomputed
- Slope regression: `render building_residential_small --terrain N` → output PNG canvas height > 64
- Phase 1 — slopes.yaml + iso_stepped_foundation primitive.
- Phase 2 — Composer slope auto-insert + canvas auto-grow.

**Tasks:**


| Task | Name                   | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                                                                           |
| ---- | ---------------------- | ------------ | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| T4.1 | Slopes YAML table      | **TECH-175** | Done            | `tools/sprite-gen/slopes.yaml` — per-corner Z table (in pixels) for 17 land slope variants: flat, N, S, E, W, NE, NW, SE, SW, NE-up, NW-up, SE-up, SW-up, NE-bay, NW-bay, NW-bay-2, SE-bay, SW-bay; corner keys: n/e/s/w; values: 0 or 16 (per §7 Slope-aware foundation table); codes must match `Assets/Sprites/Slopes/` filename stems exactly per **Slope variant naming**                   |
| T4.2 | iso_stepped_foundation | **TECH-176** | Done (archived) | `src/primitives/iso_stepped_foundation.py` — `iso_stepped_foundation(canvas, x0, y0, fx, fy, slope_id, material, palette)`: read `slopes.yaml` per-corner Z for slope_id; build stair/wedge pixel geometry bridging sloped ground plane (variable corners) to flat top at `max(n,e,s,w)+2` lip px; draw using `apply_ramp(material, 'south')` / `apply_ramp(material, 'east')` for visible faces |
| T4.3 | Slope auto-insert      | **TECH-177** | Done (archived) | Update `src/compose.py` `compose_sprite`: if `spec['terrain'] != 'flat'`, prepend `iso_stepped_foundation(...)` to primitive stack; recalculate `extra_h = max_corner_z` from slopes.yaml; recompute canvas size + pivot via `canvas_size(fx, fy, extra_h)` + `pivot_uv(canvas_h)`; raise `SlopeKeyError` (exit code 1) if slope_id not in slopes.yaml                                           |
| T4.4 | Slope regression tests | **TECH-178** | Done (archived) | Slope regression test spec `specs/building_residential_small_N.yaml` (copy of small, terrain: N); run `python -m sprite_gen render building_residential_small_N`; assert output PNG height > 64 (canvas grew by max_corner_z=16); assert pivot_uv != (0.5, 0.25); render all 17 slope variants via `--terrain` CLI flag; assert no crash                                                         |


---

### Stage 5 — Layer 5 Curation + Snapshot push hook / Unity meta + Aseprite Tier-2 + Registry catalog integration

**Status:** Final — tasks **TECH-179..183** + **TECH-674..679** archived 2026-04-22. Dependency gate (TECH-640..645) satisfied (archived).

**Objectives:** (1) Close Layer 5 of the composer by shipping the curation CLI (`promote` / `reject`) + Unity `.meta` writer + Aseprite Tier-2 integration (layered `.aseprite` emission + `promote --edit` round-trip) — relocated from Stage 4. (2) On `promote` success, POST the promoted sprite metadata to the `tg-catalog-api` `/api/catalog/assets` endpoint so each approved PNG lands as a Postgres catalog row in the live snapshot automatically. Idempotent by slug — 409 Conflict triggers PATCH via `updated_at` optimistic lock when asset metadata drifted, or skip when already identical. `--no-push` CLI flag for offline / air-gapped work. Closes the sprite-gen → registry feed contract per asset-snapshot-mvp L6 + L7 + L8 (sprite-gen emits PNG + `.meta` only; Postgres owns the catalog; writes go through HTTP not SQL).

**Exit:**

- `promote out/X.png --as final_name` copies PNG to `Assets/Sprites/Generated/` + writes `.meta` (PPU=64, pivot=(0.5, 16/h), Point filter, no compression)
- `reject {archetype}` deletes all `out/{archetype}_*.png` files
- `render --layered {archetype}` emits `.aseprite` alongside flat PNG with named layers `top`, `south`, `east`, `foundation` (only when non-flat); opening in Aseprite shows layers editable separately
- `promote out/X.aseprite --as name --edit` launches Aseprite CLI to flatten, writes PNG + `.meta` to `Assets/Sprites/Generated/`; exits code 4 when Aseprite binary not found with install hint
- `src/registry_client.py` — `RegistryClient(url, timeout=5)` with `create_asset(payload)` + `patch_asset(id, payload, updated_at)` + `get_asset_by_slug(slug) -> Optional[dict]`; error hierarchy `RegistryClientError` → `ConnectionError` / `ConflictError` / `ValidationError`; `requests` added to `requirements.txt`.
- Endpoint resolution order: env `TG_CATALOG_API_URL` → `tools/sprite-gen/config.toml` `[catalog] url` → raise `CatalogConfigError` when push=True and neither set.
- `curate.promote(src, dest_name, edit=False, push=True)` — after PNG + `.meta` land, builds payload (slug = `dest_name`, `world_sprite_path` = `Assets/Sprites/Generated/{dest_name}.png`, `ppu=64`, `pivot = (0.5, 16/canvas_h)`, `generator_archetype_id` from spec meta, `category` from spec class) and POSTs via `RegistryClient.create_asset`.
- Conflict handling: 409 → `get_asset_by_slug` → compare `world_sprite_path` + `generator_archetype_id` → match ⇒ skip (idempotent); mismatch ⇒ `patch_asset` with fresh `updated_at`; retry on 409 at most once; other 4xx ⇒ exit code 5.
- `cli.py promote ... --no-push` flag short-circuits the HTTP step entirely; `promote --edit` path respects `--no-push` (single push per promote).
- `tests/test_registry_client.py` — `responses` library fixtures covering 200 create, 409 identical (skip), 409 drift (PATCH), 422 validation, connection refused.
- `pytest tools/sprite-gen/tests/` exit 0; README CLI table + `docs/sprite-gen-usage.md` updated with `--no-push` + env var + config.toml stanza.

**Dependency gate:** Registry-push half of the stage (T5.6..T5.11) opens only after `grid-asset-visual-registry-master-plan.md` Step 1 Stage 1.3 archives `TECH-640`..`TECH-645` (HTTP `POST /api/catalog/assets` + `PATCH /api/catalog/assets/:id` + 409 optimistic-lock contract live). Curation half (T5.1..T5.5) carries no external dependency — can proceed immediately since issues TECH-179..TECH-183 are already filed.

**Phases:**

- Phase 1 — Curation CLI (promote / reject) + Unity `.meta` writer.
- Phase 2 — Layered `.aseprite` emission + `promote --edit` round-trip (Tier 2 editor integration).
- Phase 3 — HTTP client module + config resolution.
- Phase 4 — Promote integration + `--no-push` CLI flag.
- Phase 5 — Conflict handling + tests + docs.

**Tasks:**


| Task  | Name                      | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| ----- | ------------------------- | ------------ | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| T5.1  | Unity meta writer         | **TECH-179** | Done (archived) | `src/unity_meta.py` — `write_meta(png_path, canvas_h) → str`: emit Unity `.meta` YAML string with guid (uuid4), textureImporter settings: PPU=64, spritePivot=(0.5, 16/canvas_h), filterMode=Point, textureCompression=None, spriteMode=Single; `src/curate.py` — `promote(src_png, dest_name)`: copy PNG to `Assets/Sprites/Generated/{dest_name}.png`, call `write_meta`, write `.meta` file alongside. *(Relocated from Stage 4 T4.5 on 2026-04-22.)*                                                                                                                             |
| T5.2  | Promote/reject CLI        | **TECH-180** | Done (archived) | `src/cli.py` — `promote out/X.png --as name` command: call `curate.promote()`; assert dest file exists + `.meta` exists; `reject {archetype}` command: glob `out/{archetype}_*.png`, delete all; integration test: promote then reject the same file, assert `Assets/Sprites/Generated/` has promoted file, `out/` is clean after reject. *(Relocated from Stage 4 T4.6 on 2026-04-22.)*                                                                                                                                                                                             |
| T5.3  | Aseprite bin resolver     | **TECH-181** | Done (archived) | `src/aseprite_bin.py` — `find_aseprite_bin() → Path`: resolve in order `$ASEPRITE_BIN` env var → `tools/sprite-gen/config.toml` `[aseprite] bin` → platform default probes (macOS: `/Applications/Aseprite.app/Contents/MacOS/aseprite`, then `~/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite`); raise `AsepriteBinNotFoundError` on miss (caught by CLI, exit code 4 with install hint); unit test mocks filesystem + env var. *(Relocated from Stage 4 T4.7 on 2026-04-22.)*                                                   |
| T5.4  | Layered aseprite emit     | **TECH-182** | Done (archived) | `src/aseprite_io.py` — `write_layered_aseprite(dest_path, layers: dict[str, PIL.Image], canvas_size)`: write `.aseprite` via `py_aseprite` (add to `requirements.txt`) with named layers in stacking order (`foundation`, `east`, `south`, `top`); transparent alpha preserved per layer; update `src/compose.py` to split per-face buffers when `layered=True` flag passed; add `--layered` flag to `cli.py render`; composer always co-emits flat PNG so non-Aseprite users stay unblocked. *(Relocated from Stage 4 T4.8 on 2026-04-22.)*                                         |
| T5.5  | Promote --edit round-trip | **TECH-183** | Done (archived) | `src/curate.py` — extend `promote(src, dest_name, edit=False)`: if `src.suffix == '.aseprite'` and `edit=True`, shell-out `{aseprite_bin} --batch {src} --save-as {tmp}.png` (subprocess, check returncode), then run existing PNG promote pipeline on `{tmp}.png`; cleanup tmp after; `src/cli.py` — `promote ... --edit` flag; integration test: render --layered → modify one layer pixel via PIL → promote --edit → assert flattened PNG + `.meta` exist in `Assets/Sprites/Generated/`, assert modified pixel present in output. *(Relocated from Stage 4 T4.9 on 2026-04-22.)* |
| T5.6  | RegistryClient scaffold   | **TECH-674** | Done (archived) | `src/registry_client.py` — class `RegistryClient(url: str, timeout: int = 5)` with `create_asset(payload) -> dict`, `patch_asset(id: int, payload: dict, updated_at: str) -> dict`, `get_asset_by_slug(slug: str) -> Optional[dict]`; exception hierarchy `RegistryClientError` → `ConnectionError` / `ConflictError(existing_row)` / `ValidationError(errors)`; add `requests` to `tools/sprite-gen/requirements.txt`.                                                                                                                                                              |
| T5.7  | Catalog URL resolver      | **TECH-675** | Done (archived) | `src/registry_client.py` — `resolve_catalog_url() -> str`: read env `TG_CATALOG_API_URL` first, `tools/sprite-gen/config.toml` `[catalog] url` second; raise `CatalogConfigError` with hint when neither set and push=True; `--no-push` short-circuits (not called); unit test covers env precedence + config fallback + both-missing.                                                                                                                                                                                                                                               |
| T5.8  | Promote payload + push    | **TECH-676** | Done (archived) | Update `src/curate.py` `promote(src, dest_name, edit=False, push=True)` — after `.meta` writes succeed, call `_build_catalog_payload(dest_name, canvas_h, spec_meta) -> dict` (slug, world_sprite_path, ppu=64, pivot, generator_archetype_id, category) + `RegistryClient(resolve_catalog_url()).create_asset(payload)`. Catch `ConflictError` → compare rows → `patch_asset` on drift; noop on match.                                                                                                                                                                              |
| T5.9  | CLI --no-push flag        | **TECH-677** | Done (archived) | `src/cli.py` — extend `promote` command signature with `--no-push` (default false = push); pass through to `curate.promote(..., push=not args.no_push)`; ensure `promote --edit --no-push` skips HTTP once (single push path across flattened + direct PNG variants); `README.md` CLI usage table updated.                                                                                                                                                                                                                                                                           |
| T5.10 | RegistryClient tests      | **TECH-678** | Done (archived) | `tests/test_registry_client.py` — use `responses` fixture; cases: 200 create happy, 409 with matching existing row (skip, no PATCH), 409 with drifted existing row (PATCH issued with `updated_at`), 422 validation (ValidationError raised + CLI exit 1), `ConnectionError` (exit 5); assert no HTTP call made when `push=False`.                                                                                                                                                                                                                                                   |
| T5.11 | Promote integration smoke | **TECH-679** | Done (archived) | `tests/test_promote_push.py` — end-to-end: spin up `responses`-mocked catalog server; `render building_residential_small` → `promote out/X.png --as residential-small-01` → assert POST `/api/catalog/assets` issued with expected JSON payload; run `--no-push` variant → assert zero HTTP calls; document exit code 5 handling in `docs/sprite-gen-usage.md`.                                                                                                                                                                                                                      |


### §Stage File Plan



```yaml
- reserved_id: ""
  title: "RegistryClient scaffold (sprite-gen → tg-catalog-api)"
  priority: high
  notes: |
    `tools/sprite-gen/src/registry_client.py` — shared HTTP client for catalog rows; add `requests` to requirements. Depends on grid registry Stage 1.3 (TECH-640..645 archived) for live POST/PATCH/409 contract.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      New `RegistryClient` class in sprite-gen: create/patch/get-by-slug against tg-catalog-api; exception hierarchy; timeout + `requests` session.
    goals: |
      1. `create_asset`, `patch_asset`, `get_asset_by_slug` on JSON API. 2. Typed errors. 3. `requests` dependency declared.
    systems_map: |
      `tools/sprite-gen/src/registry_client.py` (new); `tools/sprite-gen/requirements.txt`.
    impl_plan_sketch: |
      Phase 1 — Implement client + exception classes; wire minimal JSON encode/decode; no CLI yet.
- reserved_id: ""
  title: "Catalog URL resolver (env + config.toml)"
  priority: high
  notes: |
    `resolve_catalog_url()` in `registry_client` module — env `TG_CATALOG_API_URL` beats `config.toml` `[catalog] url`; `CatalogConfigError` when push needs URL and both missing.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Central URL resolution for registry HTTP calls; testable without live server.
    goals: |
      1. Precedence order. 2. Error when no URL and push path requires one. 3. Unit tests for env, config, both-missing.
    systems_map: |
      `tools/sprite-gen/src/registry_client.py`; `tools/sprite-gen/config.toml` (add `[catalog]` if absent).
    impl_plan_sketch: |
      Phase 1 — Pure function + tests; no promote wiring yet.
- reserved_id: ""
  title: "Promote payload build + create/patch (409 drift)"
  priority: high
  notes: |
    Extend `curate.promote` to POST catalog asset after `.meta` write; 409 path — compare row, idempotent skip or `patch_asset` w/ `updated_at`; single retry.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Connect promotion pipeline to `RegistryClient` — slug, world path, ppu, pivot, generator_archetype_id, category.
    goals: |
      1. `_build_catalog_payload` from dest + spec meta. 2. 409: skip identical; PATCH on drift. 3. other 4xx → exit 5.
    systems_map: |
      `tools/sprite-gen/src/curate.py`; `registry_client.py`.
    impl_plan_sketch: |
      Phase 1 — Helper + call site after successful promote writes.
- reserved_id: ""
  title: "CLI `promote` `--no-push` flag + README"
  priority: high
  notes: |
    `cli.py` promote subcommand: `--no-push` skips HTTP; pass through to `curate.promote(..., push=...)`; works with `--edit` path; README table update.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      User-facing opt-out of catalog push (offline / local Unity workflow).
    goals: |
      1. Argparse flag default false. 2. No HTTP when set. 3. Document in README.
    systems_map: |
      `tools/sprite-gen/src/cli.py`; `tools/sprite-gen/README.md`.
    impl_plan_sketch: |
      Phase 1 — Wire flag + one regression test.
- reserved_id: ""
  title: "RegistryClient + promote HTTP contract tests (responses)"
  priority: high
  notes: |
    `tests/test_registry_client.py` + responses fixtures — 200, 409 skip, 409 patch, 422, connection error; `push=False` no HTTP.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Isolated unit/integration tests for HTTP edge cases; `responses` added to dev requirements if not already.
    goals: |
      1. Cover all exit branches in client + curate error mapping. 2. pytest clean.
    systems_map: |
      `tools/sprite-gen/tests/test_registry_client.py` (new).
    impl_plan_sketch: |
      Phase 1 — responses mocks for `/api/catalog/assets`.
- reserved_id: ""
  title: "Promote → catalog smoke test + `sprite-gen-usage` exit codes"
  priority: high
  notes: |
    `tests/test_promote_push.py` end-to-end with mocked base URL; document exit code 5 in `docs/sprite-gen-usage.md` for registry failures.
  depends_on:
    - TECH-640
    - TECH-641
    - TECH-642
    - TECH-643
    - TECH-644
    - TECH-645
  related: []
  stub_body:
    summary: |
      Higher-level E2E: render fixture archetype, promote, assert POST body shape; `--no-push` → zero calls.
    goals: |
      1. E2E test file. 2. Docs: exit 5, env/config.
    systems_map: |
      `tools/sprite-gen/tests/test_promote_push.py`; `docs/sprite-gen-usage.md`.
    impl_plan_sketch: |
      Phase 1 — Smoke + doc edits.

```

### §Plan Review (Stage 5 — registry file batch)

`PASS` — 2026-04-22 — six new tasks **TECH-674**..**TECH-679** carry §Plan Digest + aggregate `docs/implementation/sprite-gen-stage-5-plan.md`. Prior curation issues **TECH-179**..**183** unchanged (already had digest).

---

### Stage 6 — Scale calibration + ground diamond primitive (DAS hotfix)

**Status:** In Progress — 2026-04-23. Planned to ship as a **standalone hotfix PR** ahead of Stages 7–14 (Lock H2). Closes the 3× scale bug in one pass so the current `building_residential_small` archetype visually matches `House1-64.png`.

**Objectives:** Introduce pixel-native primitive units, ground-diamond primitive, spec-level `footprint_ratio`, and the per-class `level_h` table. Re-calibrate the only live archetype (`building_residential_small`) so generated output matches the hand-drawn reference within ±3 px bbox tolerance.

**Exit:**

- `src/primitives/`* — each primitive accepts `w_px`, `d_px`, `h_px` (pixel-native). Back-compat: `w`, `d`, `h` (tile-unit) accepted and translated to px via `w_px = w * 32` etc.
- `src/primitives/iso_ground_diamond.py` — new primitive; renders full-tile flat diamond with 1-px rim-shade; materials per DAS §4.1 `grass_flat`, `grass_dense`, `pavement`, `water_deep`, `zoning_*`, `mustard_industrial`.
- `src/compose.py` — auto-prepends `iso_ground_diamond(fx, fy, ground)` unless `spec.ground: none`; applies spec-level `footprint_ratio: [wr, dr]` by scaling each composition primitive's `w_px`/`d_px` by the ratio.
- `src/constants.py` (new) — per-class `level_h` table: `{residential_small: 12, commercial_small: 12, residential_heavy: 16, commercial_dense: 16, industrial_*: 16}`.
- `specs/building_residential_small.yaml` — rewritten to the DAS §5 R11 schema with `footprint_ratio: [0.45, 0.45]`, `ground: grass_flat`, `levels: 1`, pixel-native primitives.
- `tests/test_ground_diamond.py` — bbox of rendered flat 1×1 ground diamond = `(0,15)→64×33`; all 8 materials produce non-empty PNGs.
- `tests/test_scale_calibration.py` — render `building_residential_small_v01.png`; assert content bbox height within `35 ± 3 px`, content bbox y0 within `13 ± 3 px` (matches `House1-64.png` signature per DAS §2.3).
- `docs/sprite-gen-usage.md` updated with `footprint_ratio` + `ground` spec fields.

**Phases:**

- Phase 1 — Pixel-native primitives + back-compat translation.
- Phase 2 — `iso_ground_diamond` primitive + 8 materials.
- Phase 3 — Composer auto-prepend + `footprint_ratio` scaling.
- Phase 4 — `level_h` constants + re-calibrated `building_residential_small` spec + calibration regression test.

**Tasks:**


| Task | Name                                             | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                                          |
| ---- | ------------------------------------------------ | ------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.1 | Pixel-native primitive signatures                | **TECH-693** | Done   | Extend `iso_cube`, `iso_prism`, `iso_stepped_foundation` (and any other existing primitive) to accept `w_px`, `d_px`, `h_px` kwargs; keep `w,d,h` as deprecated tile-unit aliases (multiplied by 32). Update all internal call sites in `compose.py`.                                                                                                                           |
| T6.2 | `iso_ground_diamond` primitive + materials       | **TECH-694** | Done   | New `src/primitives/iso_ground_diamond.py`; draws 64×32 px diamond (or `(fx+fy)×32` × `(fx+fy)×16`) at standard y0=15 offset on a 1×1 canvas; renders 1-px rim-shade via `apply_ramp(material, 'dark')`. Materials: `grass_flat`, `grass_dense`, `pavement`, `water_deep`, `zoning_residential`, `zoning_commercial`, `zoning_industrial`, `mustard_industrial`.                |
| T6.3 | Composer ground auto-prepend + `footprint_ratio` | **TECH-695** | Done   | Update `compose_sprite`: read `spec.ground` (default per-class from DAS §4.2, fallback `grass_flat`); prepend `iso_ground_diamond` call; read `spec.building.footprint_ratio` (default `[1.0, 1.0]` back-compat); scale each composition primitive `w_px *= footprint_ratio[0]`, `d_px *= footprint_ratio[1]`. Recompute x-offset to center the scaled building on the diamond. |
| T6.4 | `level_h` constants + spec expansion             | **TECH-696** | Done   | `src/constants.py` — `LEVEL_H = {"residential_small": 12, "commercial_small": 12, ..., "industrial_heavy": 16}`; composer honors `spec.levels` when set (overrides raw `h_px` on stacked cubes).                                                                                                                                                                                |
| T6.5 | Re-calibrated `building_residential_small` spec  | **TECH-697** | Done   | Rewrite `specs/building_residential_small.yaml` to DAS §5 R11 schema: `class: residential_small`, `footprint: [1,1]`, `ground: grass_flat`, `levels: 1`, `building.footprint_ratio: [0.45, 0.45]`, pixel-native composition with 10-px-tall wall cube + 8-px-tall roof prism, 4 seeded variants.                                                                                |
| T6.6 | Ground diamond tests                             | **TECH-698** | Done   | `tests/test_ground_diamond.py` — assert 1×1 `iso_ground_diamond('grass_flat')` produces bbox `(0,15)→64×33`; loop through all 8 materials, assert non-empty bbox + expected dominant color; assert 2×2 variant produces `(0,31)→128×65`.                                                                                                                                        |
| T6.7 | Scale-calibration regression test                | **TECH-699** | Done   | `tests/test_scale_calibration.py` — render `building_residential_small_v01.png` via compose; assert content bbox height `35 ± 3 px`, y0 `13 ± 3 px`, x0=0, x1=63; assert dominant colors in top 20% pixels match `House1-64.png` dominant colors within HSV ΔE=15.                                                                                                              |
| T6.8 | `README` / usage doc update                      | **TECH-700** | Done   | Update `tools/sprite-gen/README.md` + `docs/sprite-gen-usage.md` with new spec fields `ground` + `footprint_ratio` + `levels`; link to DAS sections §2.5 + §4.1.                                                                                                                                                                                                                |


### §Stage File Plan



```yaml
- reserved_id: TECH-693
  title: Pixel-native primitive signatures
  priority: high
  issue_type: TECH
  notes: |
    Extend primitives (`iso_cube`, `iso_prism`, `iso_stepped_foundation`) with `w_px`/`d_px`/`h_px`; keep tile-unit `w`,`d`,`h` as aliases (*32). Touch `tools/sprite-gen/src/primitives/`* + `compose.py` call sites. DAS pixel-native calibration (Stage 6 hotfix).
  depends_on: []
  related:
    - TECH-694
    - TECH-695
    - TECH-696
    - TECH-697
    - TECH-698
    - TECH-699
    - TECH-700
  stub_body:
    summary: |
      Primitives accept pixel kwargs; deprecated tile kwargs multiply by 32. Internal compose paths updated so downstream ground + footprint_ratio land on consistent units.
    goals: |
      1. Public primitive APIs accept `w_px`,`d_px`,`h_px`. 2. `w`,`d`,`h` still work via *32. 3. All `compose.py` call sites use pixel or translated paths.
    systems_map: |
      `tools/sprite-gen/src/primitives/` (iso_cube, iso_prism, iso_stepped_foundation); `tools/sprite-gen/src/compose.py`.
    impl_plan_sketch: |
      Phase 1 — Signature + alias layer + grep-driven call-site migration; pytest smoke unchanged.
- reserved_id: TECH-694
  title: iso_ground_diamond primitive + materials
  priority: high
  issue_type: TECH
  notes: |
    New `iso_ground_diamond` for full-tile flat diamond; 8 materials; rim-shade via `apply_ramp(...,'dark')`. DAS §4.1 materials list.
  depends_on: []
  related:
    - TECH-693
    - TECH-695
    - TECH-696
    - TECH-697
    - TECH-698
    - TECH-699
    - TECH-700
  stub_body:
    summary: |
      Ground plate primitive draws 64×32 (1×1) or scaled by fx,fy; y0=15; 1px rim dark ramp; materials wired to palette keys.
    goals: |
      1. Module `iso_ground_diamond.py`. 2. Eight materials render non-empty. 3. Rim-shade rule applied.
    systems_map: |
      `tools/sprite-gen/src/primitives/iso_ground_diamond.py`; palette JSON; `apply_ramp` helpers.
    impl_plan_sketch: |
      Phase 1 — Implement primitive + material branch table; hook from compose in follow-on task.
- reserved_id: TECH-695
  title: Composer ground auto-prepend + footprint_ratio
  priority: high
  issue_type: TECH
  notes: |
    `compose_sprite` reads `spec.ground` (class default DAS §4.2, fallback grass_flat); prepends ground diamond; `spec.building.footprint_ratio` scales primitive w_px/d_px; re-center building on diamond.
  depends_on: []
  related:
    - TECH-693
    - TECH-694
    - TECH-696
    - TECH-697
    - TECH-698
    - TECH-699
    - TECH-700
  stub_body:
    summary: |
      Composer layers ground under building geometry and scales footprint via ratio tuple with centered offset math.
    goals: |
      1. Ground prepend unless `none`. 2. Default ground per class. 3. footprint_ratio scales dims + recenters.
    systems_map: |
      `tools/sprite-gen/src/compose.py`; YAML spec loader; `iso_ground_diamond`.
    impl_plan_sketch: |
      Phase 1 — Loader defaults + compose order + scaling pass before primitive dispatch.
- reserved_id: TECH-696
  title: level_h constants + spec expansion
  priority: high
  issue_type: TECH
  notes: |
    New `src/constants.py` with `LEVEL_H` per class; composer uses `spec.levels` to drive stacked cube height / floor spacing per DAS.
  depends_on: []
  related:
    - TECH-693
    - TECH-694
    - TECH-695
    - TECH-697
    - TECH-698
    - TECH-699
    - TECH-700
  stub_body:
    summary: |
      Central per-class story height table; optional `levels` in spec overrides raw h_px stacking for residential/commercial/industrial classes.
    goals: |
      1. `LEVEL_H` dict matches Stage exit list. 2. Composer honors `spec.levels`. 3. Back-compat when field absent.
    systems_map: |
      `tools/sprite-gen/src/constants.py`; `compose.py` stacking paths.
    impl_plan_sketch: |
      Phase 1 — Add constants module + wire compose stacking.
- reserved_id: TECH-697
  title: Re-calibrated building_residential_small spec
  priority: high
  issue_type: TECH
  notes: |
    Rewrite `specs/building_residential_small.yaml` to DAS §5 R11: class, footprint, ground, levels, footprint_ratio, pixel-native wall+roof stack, 4 variants.
  depends_on: []
  related:
    - TECH-693
    - TECH-694
    - TECH-695
    - TECH-696
    - TECH-698
    - TECH-699
    - TECH-700
  stub_body:
    summary: |
      Single live archetype aligned to House1-64 calibration targets using new schema fields and primitive kwargs.
    goals: |
      1. Valid YAML per R11. 2. Four seeded variants. 3. Compose uses new ground + ratio + levels.
    systems_map: |
      `tools/sprite-gen/specs/building_residential_small.yaml`; compose + primitives.
    impl_plan_sketch: |
      Phase 1 — Author YAML + spot-render smoke.
- reserved_id: TECH-698
  title: Ground diamond tests
  priority: high
  issue_type: TECH
  notes: |
    `tests/test_ground_diamond.py` bbox asserts 1×1 and 2×2; material loop; dominant color checks.
  depends_on: []
  related:
    - TECH-693
    - TECH-694
    - TECH-695
    - TECH-696
    - TECH-697
    - TECH-699
    - TECH-700
  stub_body:
    summary: |
      Unit tests lock ground diamond geometry and material coverage per Stage exit criteria.
    goals: |
      1. Bbox (0,15)→64×33 for 1×1 grass_flat. 2. All 8 materials non-empty bbox. 3. 2×2 bbox (0,31)→128×65.
    systems_map: |
      `tools/sprite-gen/tests/test_ground_diamond.py`; primitive + render helpers.
    impl_plan_sketch: |
      Phase 1 — pytest file with bbox + color assertions.
- reserved_id: TECH-699
  title: Scale-calibration regression test
  priority: high
  issue_type: TECH
  notes: |
    `tests/test_scale_calibration.py` renders residential small variant; asserts bbox vs House1-64 signature; HSV dominant color proximity.
  depends_on: []
  related:
    - TECH-693
    - TECH-694
    - TECH-695
    - TECH-696
    - TECH-697
    - TECH-698
    - TECH-700
  stub_body:
    summary: |
      Regression locks archetype output to reference sprite tolerances (height, y0, x band, top-fraction colors).
    goals: |
      1. Content bbox height 35±3, y0 13±3. 2. x0=0 x1=63. 3. Top 20% dominant colors ΔE≤15 vs reference.
    systems_map: |
      `tools/sprite-gen/tests/test_scale_calibration.py`; `Assets/Sprites/.../House1-64.png` reference path in test.
    impl_plan_sketch: |
      Phase 1 — Compose render + numpy/PIL stats vs fixture PNG.
- reserved_id: TECH-700
  title: README / usage doc update
  priority: medium
  issue_type: TECH
  notes: |
    Document `ground`, `footprint_ratio`, `levels` in README + sprite-gen-usage; link DAS §2.5 + §4.1.
  depends_on: []
  related:
    - TECH-693
    - TECH-694
    - TECH-695
    - TECH-696
    - TECH-697
    - TECH-698
    - TECH-699
  stub_body:
    summary: |
      Developer-facing docs explain new YAML fields and defaults for Stage 6 pipeline.
    goals: |
      1. README updated. 2. `docs/sprite-gen-usage.md` updated. 3. DAS cross-links.
    systems_map: |
      `tools/sprite-gen/README.md`; `docs/sprite-gen-usage.md`.
    impl_plan_sketch: |
      Phase 1 — Doc sections + example YAML snippets.
```

**Dependency gate:** None. Independent hotfix; can be branched off `master` directly.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6 tasks **TECH-693**..**TECH-700** aligned; no fix tuples. Aggregate doc: `docs/implementation/sprite-gen-stage-6-plan.md`. Downstream pipeline continue.

---

### Stage 6.1 — Pivot hotfix + regression tighten

**Status:** Done — 2026-04-23. Retroactive filing of the in-session pivot hotfix applied during the 2026-04-23 sprite-gen improvement session (`/tmp/sprite-gen-improvement-session.md` §3 Stage 6.1). The composer patch (`pivot_pad = 17 if spec.get("ground") != "none" else 0`) is already live in the working tree at `tools/sprite-gen/src/compose.py:256`; this stage produces the issue trail and tightens the regression suite that the in-session work skipped. **Locks consumed:** L1 (pivot_pad=17 per DAS §2.1/§2.2). **Issues closed:** I1 (composer anchors buildings above ground diamond), I2 (regression loose).

**Objectives:** Lock the in-session pivot_pad patch behind a DAS-cited comment; replace the loose `10 <= y0 <= 16` scale-calibration bound with the tight DAS §2.3 envelope (`y1 == 48`, `content_h ∈ [32, 36]`); add a parametrized bbox regression to `tests/test_render_integration.py` covering every live spec under `tools/sprite-gen/specs/`.

**Exit:**

- `tools/sprite-gen/src/compose.py:256` — `pivot_pad = 17 if spec.get("ground") != "none" else 0`; `adjusted_y0 = y0 - pivot_pad - offset_z`; inline comment cites DAS §2.1 (diamond bottom at `y = canvas_h - 17`) + §2.2 (pivot 16 px from canvas bottom; +1 for PIL inclusive pixel indexing).
- `tools/sprite-gen/tests/test_scale_calibration.py` — assertions tightened to `y1 == 48` and `content_h ∈ [32, 36]`; loose `10 <= y0 <= 16` bound removed.
- `tools/sprite-gen/tests/test_render_integration.py` — parametrized `test_every_live_spec_has_bbox_below_diamond` across all `specs/*.yaml` in the tool tree; asserts bbox `(0, 15, 64, 48)` for every 1×1 live spec (`building_residential_small`, `building_residential_light_a|b|c`).
- `pytest tools/sprite-gen/tests/` exits 0 — 218+ tests green.

**Phases:**

- Phase 1 — Formalize pivot_pad comment at `compose.py:256` (retroactive; no code change — lock wording + DAS citation).
- Phase 2 — Tighten `test_scale_calibration.py` bounds.
- Phase 3 — Parametrized per-spec bbox regression in `test_render_integration.py`.

**Tasks:**


| Task   | Name                                                     | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                          |
| ------ | -------------------------------------------------------- | ------------ | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.1.1 | Formalize pivot_pad patch + DAS-cited comment            | **TECH-701** | Done   | `tools/sprite-gen/src/compose.py:256` — confirm in-session hotfix (`pivot_pad = 17 if spec.get("ground") != "none" else 0`; `adjusted_y0 = y0 - pivot_pad - offset_z`); inline comment cites DAS §2.1 (diamond bottom y = canvas_h − 17) + §2.2 (pivot UV 16/canvas_h; +1 for PIL inclusive pixel indexing). Retroactive — code already landed in working tree. |
| T6.1.2 | Tighten `test_scale_calibration.py` regression bounds    | **TECH-702** | Done   | `tools/sprite-gen/tests/test_scale_calibration.py` — replace loose `10 <= y0 <= 16` with tight DAS §2.3 envelope: assert rendered bbox `y1 == 48`, `content_h ∈ [32, 36]`. House1-64 reference signature stays authoritative.                                                                                                                                   |
| T6.1.3 | Per-spec bbox regression in `test_render_integration.py` | **TECH-703** | Done   | `tools/sprite-gen/tests/test_render_integration.py` — parametrized fixture iterating `specs/*.yaml` (1×1 live specs only: `building_residential_small`, `building_residential_light_{a,b,c}`). For each spec: compose → assert bbox `(0, 15, 64, 48)`. Skip non-1×1 specs.                                                                                      |


### §Stage File Plan



```yaml
- reserved_id: TECH-701
  title: Formalize pivot_pad patch + DAS-cited comment at compose.py:256
  priority: high
  issue_type: TECH
  notes: |
    Retroactive filing. In-session hotfix `pivot_pad = 17 if spec.get("ground") != "none" else 0`; `adjusted_y0 = y0 - pivot_pad - offset_z` already applied at `tools/sprite-gen/src/compose.py:256`. Task locks the wording + DAS §2.1/§2.2 citation in the inline comment so the invariant does not drift. No code path change.
  depends_on: []
  related:
    - TECH-702
    - TECH-703
  stub_body:
    summary: |
      Retroactive filing of pivot_pad hotfix. Composer anchors building primitives 17 px above canvas bottom when ground diamond is present; comment cites DAS §2.1 (diamond bottom 16 px + 1 inclusive pixel) + §2.2 (pivot UV = 16/canvas_h).
    goals: |
      1. Confirm `pivot_pad = 17 if spec.get("ground") != "none" else 0` at `compose.py:256`.
      2. Confirm `adjusted_y0 = y0 - pivot_pad - offset_z` at `compose.py:260`.
      3. Inline comment explicitly names DAS §2.1 + §2.2.
    systems_map: |
      `tools/sprite-gen/src/compose.py` (pivot_pad block around line 256); `docs/sprite-gen-art-design-system.md` §2.1 / §2.2 (source of truth for 17-px pad derivation).
    impl_plan_sketch: |
      Phase 1 — Read compose.py around the patch; verify DAS citation on the inline comment; adjust wording if drifted (no functional change). Gate: `pytest tools/sprite-gen/tests/ -q` stays green (218 tests).
- reserved_id: TECH-702
  title: Tighten test_scale_calibration.py regression bounds
  priority: high
  issue_type: TECH
  notes: |
    Replace loose `10 <= y0 <= 16` bound with the tight DAS §2.3 House1-64 envelope: `y1 == 48`, `content_h ∈ [32, 36]`. Closes the regression hole that let the original pivot bug ship.
  depends_on:
    - TECH-701
  related:
    - TECH-703
  stub_body:
    summary: |
      Scale-calibration regression tightened to the exact DAS §2.3 envelope from House1-64 (y1 = 48, content_h between 32 and 36 inclusive). No more loose y0 range.
    goals: |
      1. Add `assert y1 == 48` on rendered bbox.
      2. Add `assert 32 <= content_h <= 36`.
      3. Remove or deprecate the loose `10 <= y0 <= 16` check.
    systems_map: |
      `tools/sprite-gen/tests/test_scale_calibration.py` (`test_residential_small_bbox_y0_in_envelope` + neighbouring bbox checks); DAS §2.3 anchor metrics table.
    impl_plan_sketch: |
      Phase 1 — Rewrite `test_residential_small_bbox_*` functions with tight assertions referencing DAS §2.3. Gate: `pytest tools/sprite-gen/tests/test_scale_calibration.py -q` green against the current `building_residential_small.yaml` render.
- reserved_id: TECH-703
  title: Per-spec bbox regression in test_render_integration.py
  priority: high
  issue_type: TECH
  notes: |
    Parametrize a new `test_every_live_1x1_spec_bbox` across every `specs/*.yaml` in the sprite-gen tool tree; assert bbox equals exactly `(0, 15, 64, 48)` for each 1×1 live spec (`building_residential_small` + 3 `building_residential_light_{a,b,c}` variants). Closes I2 regression hole — any spec that anchors a building too high / too low fails at CI.
  depends_on:
    - TECH-701
  related:
    - TECH-702
  stub_body:
    summary: |
      Parametrized bbox regression proves the pivot hotfix holds for every live 1×1 spec, not only the canonical `building_residential_small`.
    goals: |
      1. Glob `tools/sprite-gen/specs/*.yaml`; collect 1×1 specs into a pytest parametrize.
      2. Render each spec via `compose_sprite(load_spec(path))`.
      3. Assert `rendered.getbbox() == (0, 15, 64, 48)` per spec.
    systems_map: |
      `tools/sprite-gen/tests/test_render_integration.py` (new parametrized test); `tools/sprite-gen/specs/` (glob source); `src/spec.py`, `src/compose.py` (render path).
    impl_plan_sketch: |
      Phase 1 — Add `@pytest.mark.parametrize("spec_path", _live_1x1_specs())` test; `_live_1x1_specs()` filters specs with `footprint: [1,1]`. Gate: `pytest tools/sprite-gen/tests/test_render_integration.py -q` green — 4+ parametrized cases.
```

**Dependency gate:** None. Independent hotfix filing ahead of Stage 7.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.1 tasks **TECH-701**..**TECH-703** aligned with §3 Stage 6.1 block of `/tmp/sprite-gen-improvement-session.md`; no fix tuples. Aggregate doc: `docs/implementation/sprite-gen-stage-6.1-plan.md`. Downstream: file Stage 6.2.

---

### Stage 6.2 — Art Signatures per class

**Status:** Draft — 2026-04-23. Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 6.2 block (`/tmp/sprite-gen-improvement-session.md`). **Locks consumed:** L2 (Calibration = summarized Art Signatures per class; runtime never reads raw sprites), L3 (signature JSON carries `source_checksum`; stale raises actionable refresh), L4 (Spec YAML `include_in_signature: false` per-sprite override), L15 (sample-size policy: 0 → fallback, 1 → point-match, ≥2 → envelope).

**Objectives:** Replace ad-hoc scale-calibration with per-class calibration signatures committed under `tools/sprite-gen/signatures/<class>.signature.json`. Build a `src/signature.py` module that (a) extracts bbox / palette / silhouette / ground / decoration-hints summaries from `Assets/Sprites/<class>/*.png`, (b) validates generator output against the envelope, (c) fails fast with `SignatureStaleError` when `source_checksum` drifts. Introduce CLI `refresh-signatures [class?]` to regenerate summaries on demand. Replace `test_scale_calibration.py` with parametrized `test_signature_calibration.py` once `residential_small.signature.json` lands.

**Exit:**

- `tools/sprite-gen/src/signature.py` — `compute_signature(class_name, folder_glob) -> dict`; `validate_against(signature, rendered_img) -> ValidationReport`; `SignatureStaleError` on checksum mismatch; implements L15 sample-size branches (`source_count == 0 → mode: fallback`, `== 1 → point-match`, `>= 2 → envelope`).
- `tools/sprite-gen/src/__main__.py` (or `src/cli.py`) — new subcommand `python3 -m src refresh-signatures [class?]`; writes / rewrites `signatures/<class>.signature.json`.
- `tools/sprite-gen/signatures/` — dir scaffold with `_fallback.json` fallback-class graph (e.g. `residential_small → residential_row`) and `residential_small.signature.json` bootstrap (computed from `Assets/Sprites/residential_small/*.png`; ≥2 samples → envelope mode).
- `tools/sprite-gen/src/spec.py` — accepts `include_in_signature: false` on spec-level override (per-sprite exclusion from signature ingestion).
- `tools/sprite-gen/tests/test_signature_calibration.py` — parametrized over every `signatures/*.signature.json`; asserts `validate_against(signature, compose_sprite(load_spec(<class canonical spec>)))` returns `.ok == True`.
- `tools/sprite-gen/tests/test_scale_calibration.py` deprecated (file deleted or reduced to a `pytest.mark.skip("superseded by test_signature_calibration")` stub) once `residential_small.signature.json` lands; TECH-702's tight-bound assertions absorbed into the signature envelope.
- `docs/sprite-gen-art-design-system.md` §2.6 new pointer block — "Calibration signatures are the canonical runtime calibration source; see `tools/sprite-gen/signatures/` + `src/signature.py`."
- `pytest tools/sprite-gen/tests/` exits 0 — 221+ tests green (TECH-703 baseline + at least one new signature calibration case).

**Phases:**

- Phase 1 — Author `src/signature.py` core module (JSON shape, L15 sample-size policy, `compute_signature` + `validate_against` + `SignatureStaleError`).
- Phase 2 — CLI `refresh-signatures` subcommand + `signatures/` dir scaffold + `_fallback.json` fallback graph + `residential_small.signature.json` bootstrap.
- Phase 3 — Spec loader `include_in_signature: false` per-sprite override.
- Phase 4 — `tests/test_signature_calibration.py` parametrized + retire `test_scale_calibration.py`.
- Phase 5 — DAS §2.6 pointer doc block.

**Tasks:**


| Task   | Name                                                                       | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| ------ | -------------------------------------------------------------------------- | ------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.2.1 | Signature module core (`src/signature.py`)                                 | **TECH-704** | Done   | New module with `compute_signature(class_name, folder_glob) -> dict`, `validate_against(signature, rendered_img) -> ValidationReport`, `SignatureStaleError`. JSON shape per handoff §3 Stage 6.2 spec (class / refreshed_at / source_count / source_checksum / mode / bbox / palette / silhouette / ground / decoration_hints). L15 sample-size policy: `0 → mode: fallback` (copy from `_fallback.json` target class), `1 → mode: point-match` (single-sprite values), `>=2 → mode: envelope` (min/max/mean). L3 staleness guard: `validate_against` recomputes checksum and raises `SignatureStaleError("signature stale — run python3 -m src refresh-signatures <class>")` on mismatch. |
| T6.2.2 | CLI `refresh-signatures` + `signatures/` scaffold                          | **TECH-705** | Done   | New subcommand `python3 -m src refresh-signatures [class?]`; writes or rewrites `tools/sprite-gen/signatures/<class>.signature.json`. Create `tools/sprite-gen/signatures/` dir with `_fallback.json` (fallback-class graph per L15), plus bootstrap `residential_small.signature.json` computed from `Assets/Sprites/residential_small/*.png` (≥2 samples → envelope mode). Committed to git.                                                                                                                                                                                                                                                                                              |
| T6.2.3 | Spec loader `include_in_signature: false` override                         | **TECH-706** | Done   | `tools/sprite-gen/src/spec.py` — accept optional top-level `include_in_signature: <bool>` (default `true`) on YAML specs. Signature refresh skips sprites whose source YAML opts out. Default preserves existing behaviour; no migration needed.                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| T6.2.4 | `tests/test_signature_calibration.py` + retire `test_scale_calibration.py` | **TECH-707** | Done   | New parametrized test iterating every `signatures/*.signature.json`; runs `validate_against(signature, compose_sprite(load_spec(<class canonical spec>)))` and asserts `.ok == True`. Once `residential_small.signature.json` lands + the parametrized case is green, delete `tests/test_scale_calibration.py` (or replace with `pytest.mark.skip("superseded by test_signature_calibration")`). Full suite still exits 0.                                                                                                                                                                                                                                                                  |
| T6.2.5 | DAS §2.6 pointer block                                                     | **TECH-708** | Done   | `docs/sprite-gen-art-design-system.md` — add §2.6 "Calibration signatures are the canonical runtime calibration source. See `tools/sprite-gen/signatures/` + `src/signature.py`." Brief; forward-pointer only.                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |


### §Stage File Plan



```yaml
- reserved_id: TECH-704
  title: Signature module core (src/signature.py) with L15 sample-size policy
  priority: high
  issue_type: TECH
  notes: |
    New module `tools/sprite-gen/src/signature.py` exposing `compute_signature(class_name, folder_glob) -> dict`, `validate_against(signature, rendered_img) -> ValidationReport`, and `SignatureStaleError`. JSON shape per handoff §3 Stage 6.2 (class / refreshed_at / source_count / source_checksum / mode / fallback_of / bbox / palette / silhouette / ground / decoration_hints). L15 branches: source_count 0 → fallback, 1 → point-match, ≥2 → envelope. L3 staleness guard: recompute checksum on validate; mismatch raises `SignatureStaleError("signature stale — run python3 -m src refresh-signatures <class>")`.
  depends_on:
    - TECH-701
    - TECH-702
    - TECH-703
  related: []
  stub_body:
    summary: |
      Core signature module: extracts bbox / palette / silhouette / ground / decoration-hints summaries from a class folder of reference sprites; validates rendered images against the envelope; fails fast on stale checksum.
    goals: |
      1. `compute_signature(class_name, folder_glob)` returns the documented JSON dict.
      2. `validate_against(signature, rendered_img)` returns `ValidationReport(ok, failures)`; raises `SignatureStaleError` on checksum mismatch.
      3. L15 sample-size policy fully wired: 0 → fallback, 1 → point-match, ≥2 → envelope.
    systems_map: |
      New `tools/sprite-gen/src/signature.py`; consumers: `tests/test_signature_calibration.py` (T6.2.4), `src/__main__.py` refresh CLI (T6.2.2), eventual composer render-time gate (Stage 6.5).
    impl_plan_sketch: |
      Phase 1 — JSON shape + checksum helper; Phase 2 — extractor (bbox/palette/silhouette/ground/decoration_hints); Phase 3 — L15 branches + fallback graph resolution; Phase 4 — `validate_against` + `SignatureStaleError`. Gate: `pytest tools/sprite-gen/tests/test_signature.py -q` (new unit tests live with the module).
- reserved_id: TECH-705
  title: CLI refresh-signatures + signatures/ scaffold + residential_small bootstrap
  priority: high
  issue_type: TECH
  notes: |
    New CLI subcommand `python3 -m src refresh-signatures [class?]`. Creates/updates `tools/sprite-gen/signatures/<class>.signature.json`. Scaffolds `tools/sprite-gen/signatures/` dir with `_fallback.json` fallback-class graph and bootstrap `residential_small.signature.json` (source: `Assets/Sprites/residential_small/*.png`; ≥2 samples → envelope mode). All JSON committed to git so CI reads same snapshot.
  depends_on:
    - TECH-704
  related:
    - TECH-706
  stub_body:
    summary: |
      Ship the operator surface for signatures — one CLI entry point, one dir of canonical JSON, one fallback graph. `residential_small.signature.json` lands with the stage so downstream tests have a real envelope to assert against.
    goals: |
      1. `python3 -m src refresh-signatures` regenerates every signature in `signatures/`.
      2. `python3 -m src refresh-signatures <class>` regenerates one class.
      3. `signatures/_fallback.json` + `signatures/residential_small.signature.json` committed.
    systems_map: |
      `tools/sprite-gen/src/__main__.py` (or new `src/cli.py`); `tools/sprite-gen/signatures/`; `Assets/Sprites/<class>/*.png` (ingestion source); depends on `src/signature.py` from TECH-704.
    impl_plan_sketch: |
      Phase 1 — Wire subcommand into existing argparse; Phase 2 — `signatures/` dir + `_fallback.json` seeded with residential_small → residential_row; Phase 3 — Bootstrap `residential_small.signature.json` by running `refresh-signatures residential_small` against live catalog.
- reserved_id: TECH-706
  title: Spec loader include_in_signature per-sprite override
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` accepts optional top-level boolean `include_in_signature` (default `true`). Signature refresh ingestion skips sprites whose source YAML opts out via `include_in_signature: false`. Back-compat by construction — existing specs unchanged.
  depends_on:
    - TECH-704
  related:
    - TECH-705
  stub_body:
    summary: |
      Per-sprite opt-out so experimental / reference / deprecated specs don't contaminate class envelopes.
    goals: |
      1. `load_spec` surfaces `include_in_signature` (default `true`).
      2. Refresh pipeline (T6.2.2) filters out opted-out specs.
    systems_map: |
      `tools/sprite-gen/src/spec.py` (loader); `src/signature.py::compute_signature` (consumer, reads flag via `load_spec` when iterating).
    impl_plan_sketch: |
      Phase 1 — Add field to spec schema + loader; Phase 2 — filter in `compute_signature` source iteration. Gate: unit test with one opt-out spec confirms exclusion.
- reserved_id: TECH-707
  title: tests/test_signature_calibration.py parametrized + retire test_scale_calibration
  priority: high
  issue_type: TECH
  notes: |
    New parametrized test iterating every `signatures/*.signature.json`; for each class: `validate_against(signature, compose_sprite(load_spec(<class canonical spec>)))` must return `.ok == True`. Once green, delete or skip `tests/test_scale_calibration.py` — TECH-702 tight bounds now live in the signature envelope.
  depends_on:
    - TECH-704
    - TECH-705
  related:
    - TECH-702
  stub_body:
    summary: |
      Replaces per-spec bbox regression with full signature validation; auto-covers new classes as their signature JSON lands.
    goals: |
      1. `test_signature_calibration[residential_small]` green against live signature.
      2. `test_scale_calibration.py` retired (deleted or pytest.mark.skip).
      3. Full suite `pytest tools/sprite-gen/tests/ -q` exits 0 with same or higher test count.
    systems_map: |
      `tools/sprite-gen/tests/test_signature_calibration.py` (new); `tools/sprite-gen/tests/test_scale_calibration.py` (retired); `signatures/residential_small.signature.json` (reference envelope).
    impl_plan_sketch: |
      Phase 1 — Parametrize over `signatures/*.signature.json` glob; Phase 2 — Drop `test_scale_calibration.py`; Phase 3 — Run full suite.
- reserved_id: TECH-708
  title: DAS §2.6 pointer — signatures are canonical calibration source
  priority: medium
  issue_type: TECH
  notes: |
    Add DAS §2.6 section: "Calibration signatures are the canonical runtime calibration source. See `tools/sprite-gen/signatures/` + `src/signature.py`." Forward-pointer; no re-documentation of the JSON shape (keep authoritative spec in signature module docstring).
  depends_on:
    - TECH-704
  related: []
  stub_body:
    summary: |
      DAS §2.6 delta — point readers at signatures/ as the canonical calibration source. No detailed schema duplication; trust the module docstring.
    goals: |
      1. DAS §2.6 new section authored.
      2. Pointer cites `tools/sprite-gen/signatures/` + `src/signature.py`.
    systems_map: |
      `docs/sprite-gen-art-design-system.md`.
    impl_plan_sketch: |
      Phase 1 — Insert §2.6 block after existing §2.5 (or wherever the current §2 chain ends); commit as doc-only change.
```

**Dependency gate:** Stage 6.1 merged (TECH-701..703). L12 stage order lock.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.2 tasks **TECH-704**..**TECH-708** aligned with §3 Stage 6.2 block of `/tmp/sprite-gen-improvement-session.md`; JSON shape (L20 verbatim) + L15 sample-size policy carried into TECH-704. Aggregate doc: `docs/implementation/sprite-gen-stage-6.2-plan.md`. Downstream: file Stage 6.3.

---

### Stage 6.3 — Placement + variant randomness + split seeds

**Status:** Done — 2026-04-23 (all 6 tasks Done). Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 6.3 block (`/tmp/sprite-gen-improvement-session.md`). **Locks consumed:** L5 (Spec gains `building.footprint_px`, `building.padding`, `building.align`), L6 (`variants:` becomes block `{count, vary, seed_scope}` with legacy scalar back-compat), L7 (`bootstrap-variants --from-signature` CLI; never auto-rewrites), L14 (split seeds `palette_seed` + `geometry_seed`).

**Objectives:** Grow the spec schema so authors can express non-centered placement, per-axis variation ranges, and split seeds for palette vs geometry independence. Wire the composer to honour the new fields via a `resolve_building_box` helper and a variant sampling loop. Add a `bootstrap-variants --from-signature` CLI that reads `signatures/<class>.signature.json` and writes sensible `vary:` defaults into a spec (opt-in; never auto-rewrites). Land three new test files covering placement combinatorics, geometric variants determinism, and split-seed independence.

**Exit:**

- `tools/sprite-gen/src/spec.py` — accepts `building.footprint_px`, `building.padding: {n,e,s,w}`, `building.align ∈ {center, sw, ne, nw, se, custom}` (default `center`); `variants:` block `{count, vary, seed_scope}` with legacy scalar `variants: N` back-compat; top-level `palette_seed` + `geometry_seed` (legacy scalar `seed: N` fans to both when split seeds absent).
- `tools/sprite-gen/src/compose.py` — new helper `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` honouring footprint_px / ratio / align / padding; variant loop samples each `vary:` range deterministically from `geometry_seed + i`; palette samples from `palette_seed + i`; `variants.seed_scope` default `palette` preserves legacy behaviour.
- `tools/sprite-gen/src/__main__.py` — new subcommand `python3 -m src bootstrap-variants <stem> --from-signature` reads `signatures/<class>.signature.json` and writes sensible `vary:` defaults into the named spec; opt-in only; never auto-rewrites during render.
- `tools/sprite-gen/tests/test_building_placement.py` — matrix over footprint_px / ratio / padding / align; asserts resolved mass bbox per case.
- `tools/sprite-gen/tests/test_variants_geometric.py` — same spec + `vary:` produces 4 variants with pairwise distinct bboxes; identical outputs across runs with same seeds.
- `tools/sprite-gen/tests/test_split_seeds.py` — freezing `palette_seed` varies only geometry when `geometry_seed` advances, and vice versa.
- `docs/sprite-gen-art-design-system.md` R11 addendum — new placement fields + split seed semantics + `vary:` grammar.
- `pytest tools/sprite-gen/tests/` exits 0.

**Phases:**

- Phase 1 — Spec schema additions: `building.footprint_px` / `padding` / `align` + loader normalization.
- Phase 2 — `variants:` block + split seeds loader normalization.
- Phase 3 — Composer `resolve_building_box` helper + variant loop sampling.
- Phase 4 — CLI `bootstrap-variants --from-signature`.
- Phase 5 — Tests: placement + variants + split seeds.
- Phase 6 — DAS R11 addendum.

**Tasks:**


| Task   | Name                                                            | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| ------ | --------------------------------------------------------------- | ------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.3.1 | Placement schema: `building.footprint_px` / `padding` / `align` | **TECH-709** | Done  | `tools/sprite-gen/src/spec.py` — accept optional `building.footprint_px: [bx, by]` (wins over `footprint_ratio` when both present), `building.padding: {n, e, s, w}` in px (default all 0), `building.align ∈ {center, sw, ne, nw, se, custom}` (default `center`). Back-compat: existing specs without these fields render byte-identical. Consumes L5.                                                                                             |
| T6.3.2 | `variants:` block + split seeds normalization                   | **TECH-710** | Done  | `tools/sprite-gen/src/spec.py` — accept `variants: {count, vary, seed_scope}` object; legacy scalar `variants: N` normalises to `{count: N, vary: {}, seed_scope: palette}`. Accept top-level `palette_seed: int` + `geometry_seed: int`; legacy scalar `seed: N` fans out to both when split seeds absent. `seed_scope` default `palette` preserves legacy behaviour. Consumes L6, L14.                                                             |
| T6.3.3 | Composer `resolve_building_box` + variant loop sampling         | **TECH-711** | Done  | `tools/sprite-gen/src/compose.py` — new helper `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` encapsulating footprint_px / ratio / align / padding math (pure function, unit-tested). Variant loop samples each `vary:` range deterministically from `geometry_seed + i` for geometry axes and `palette_seed + i` for palette. Centering falls out of SE-corner anchor math; no geometry change when `align: center` and padding zero. |
| T6.3.4 | CLI `bootstrap-variants --from-signature`                       | **TECH-712** | Done  | `tools/sprite-gen/src/__main__.py` — new subcommand `python3 -m src bootstrap-variants <stem> --from-signature`. Reads `signatures/<class>.signature.json` (class derived from spec), writes sensible `vary:` defaults into the named spec (e.g. `vary.roof.h_px` from signature's silhouette band). Opt-in; never auto-rewrites during render. Consumes L7.                                                                                         |
| T6.3.5 | Tests: placement + variants + split seeds                       | **TECH-713** | Done  | `tools/sprite-gen/tests/test_building_placement.py` — matrix over footprint_px / ratio / padding / align combinations; asserts resolved mass bbox per case. `test_variants_geometric.py` — same spec + `vary:` → 4 variants pairwise-distinct bboxes; identical outputs across runs with same seeds. `test_split_seeds.py` — freezing `palette_seed` varies geometry only when `geometry_seed` advances, and vice versa.                             |
| T6.3.6 | DAS R11 addendum                                                | **TECH-714** | Done  | `docs/sprite-gen-art-design-system.md` — extend R11 with new placement fields (`building.footprint_px`, `padding`, `align`), split seed semantics (`palette_seed`, `geometry_seed`, legacy `seed` fan-out), and `vary:` grammar (range objects + `seed_scope`).                                                                                                                                                                                      |


### §Stage File Plan



```yaml
- reserved_id: TECH-709
  title: Placement schema — building.footprint_px / padding / align
  priority: high
  issue_type: TECH
  notes: |
    Extend `tools/sprite-gen/src/spec.py` to accept optional `building.footprint_px: [bx, by]` (wins over `footprint_ratio` when both present), `building.padding: {n, e, s, w}` in px (default all 0), `building.align ∈ {center, sw, ne, nw, se, custom}` (default `center`). Back-compat: specs without these fields render byte-identical. Consumes L5.
  depends_on:
    - TECH-704
    - TECH-705
    - TECH-706
    - TECH-707
    - TECH-708
  related:
    - TECH-710
    - TECH-711
  stub_body:
    summary: |
      Introduces pixel-exact building placement on the 64-px canvas — footprint_px, asymmetric padding, and alignment anchor. No composer change in this task (pure schema); composer wiring lives in TECH-711.
    goals: |
      1. `building.footprint_px: [bx, by]` accepted and surfaced via `load_spec`.
      2. `building.padding: {n, e, s, w}` accepted; default `{0, 0, 0, 0}`.
      3. `building.align` accepted with values `{center, sw, ne, nw, se, custom}`; default `center`.
      4. Back-compat: existing specs produce byte-identical output.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: `src/compose.py::resolve_building_box` (TECH-711), `tests/test_building_placement.py` (TECH-713).
    impl_plan_sketch: |
      Phase 1 — Schema validation + defaults in loader; Phase 2 — Unit tests for default/explicit cases; Phase 3 — Full suite regression.
- reserved_id: TECH-710
  title: variants block + split seeds loader normalization
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` accepts `variants: {count, vary, seed_scope}` object; legacy scalar `variants: N` normalises to `{count: N, vary: {}, seed_scope: palette}`. Top-level `palette_seed: int` + `geometry_seed: int`; legacy scalar `seed: N` fans to both when split seeds absent. `seed_scope` default `palette` preserves legacy behaviour. Consumes L6, L14.
  depends_on:
    - TECH-704
    - TECH-705
    - TECH-706
    - TECH-707
    - TECH-708
  related:
    - TECH-709
    - TECH-711
  stub_body:
    summary: |
      Normalise the variants + split-seed surface in one loader pass. Back-compat by construction: every legacy shape maps cleanly to the new object shape with documented defaults.
    goals: |
      1. `variants: {count, vary, seed_scope}` accepted; scalar `variants: N` still works.
      2. `palette_seed` + `geometry_seed` accepted; legacy `seed: N` fans to both.
      3. `seed_scope` default `palette` preserved.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: `src/compose.py` variant loop (TECH-711), `tests/test_variants_geometric.py` + `tests/test_split_seeds.py` (TECH-713).
    impl_plan_sketch: |
      Phase 1 — normalise scalar → object in loader; Phase 2 — Fan-out legacy `seed`; Phase 3 — Unit tests for each legacy shape + object shape.
- reserved_id: TECH-711
  title: Composer resolve_building_box helper + variant loop sampling
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — new pure helper `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` encapsulating footprint_px / ratio / align / padding math. Variant loop samples each `vary:` range deterministically from `geometry_seed + i` for geometry axes and `palette_seed + i` for palette. Centering falls out of SE-corner anchor math; no geometry change when `align: center` and padding zero.
  depends_on:
    - TECH-709
    - TECH-710
  related:
    - TECH-713
  stub_body:
    summary: |
      Composer honours placement + variant fields with a pure helper + seed-split sampler. Back-compat: existing specs render byte-identical thanks to default `align: center` + zero padding + `seed_scope: palette`.
    goals: |
      1. `resolve_building_box(spec)` returns `(bx, by, offset_x, offset_y)` consistent across all placement combos.
      2. Variant loop uses `geometry_seed + i` for geometry, `palette_seed + i` for palette.
      3. Legacy specs render byte-identical (zero diff against baseline).
    systems_map: |
      `tools/sprite-gen/src/compose.py` (new helper + variant loop). Consumers: `tests/test_building_placement.py` + `tests/test_variants_geometric.py` (TECH-713).
    impl_plan_sketch: |
      Phase 1 — Author `resolve_building_box` + unit tests; Phase 2 — Wire into composer pre-render; Phase 3 — Variant loop split-seed sampling; Phase 4 — Byte-identical regression on live specs.
- reserved_id: TECH-712
  title: CLI bootstrap-variants --from-signature
  priority: medium
  issue_type: TECH
  notes: |
    New subcommand `python3 -m src bootstrap-variants <stem> --from-signature`. Reads `signatures/<class>.signature.json` (class derived from spec), writes sensible `vary:` defaults into the named spec (e.g. `vary.roof.h_px` from signature silhouette band). Opt-in; never auto-rewrites during render. Consumes L7.
  depends_on:
    - TECH-710
  related:
    - TECH-705
  stub_body:
    summary: |
      Let authors bootstrap a `vary:` block from their class signature — saves them deriving ranges by hand. Opt-in only.
    goals: |
      1. CLI subcommand parses `<stem>` + `--from-signature` flag.
      2. Reads `signatures/<class>.signature.json` for the spec's class.
      3. Writes `vary:` block into the spec (preserving author-authored keys; never overwrites).
    systems_map: |
      `tools/sprite-gen/src/__main__.py` (new subcommand); `tools/sprite-gen/signatures/` (read-only); target: `tools/sprite-gen/specs/<stem>.yaml`.
    impl_plan_sketch: |
      Phase 1 — Wire subparser; Phase 2 — Derive `vary:` defaults from signature envelope fields; Phase 3 — Non-destructive write (merge, don't overwrite).
- reserved_id: TECH-713
  title: Tests — placement + variants + split seeds
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_building_placement.py` — matrix over footprint_px / ratio / padding / align combinations; asserts resolved mass bbox per case. `test_variants_geometric.py` — same spec + `vary:` → 4 variants pairwise-distinct bboxes; identical outputs across runs with same seeds. `test_split_seeds.py` — freezing `palette_seed` varies only geometry when `geometry_seed` advances, and vice versa.
  depends_on:
    - TECH-711
    - TECH-712
  related: []
  stub_body:
    summary: |
      Three new test files pin placement combinatorics + variant determinism + split-seed independence. Full matrix coverage so drift surfaces in CI.
    goals: |
      1. `test_building_placement.py` exercises ≥12 combos (4 aligns × 3 padding profiles).
      2. `test_variants_geometric.py` asserts 4 distinct bboxes + reproducibility.
      3. `test_split_seeds.py` asserts seed-split independence.
    systems_map: |
      `tools/sprite-gen/tests/test_building_placement.py` + `test_variants_geometric.py` + `test_split_seeds.py` (all new); depends on composer helper (TECH-711) + CLI bootstrap (TECH-712).
    impl_plan_sketch: |
      Phase 1 — placement matrix; Phase 2 — geometric variants determinism; Phase 3 — split-seed independence; Phase 4 — Full suite regression.
- reserved_id: TECH-714
  title: DAS R11 addendum — placement + split seeds + vary grammar
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` R11 addendum — new placement fields (`building.footprint_px`, `padding`, `align`), split seed semantics (`palette_seed`, `geometry_seed`, legacy `seed` fan-out), and `vary:` grammar (range objects + `seed_scope`).
  depends_on:
    - TECH-709
    - TECH-710
  related:
    - TECH-711
  stub_body:
    summary: |
      Doc-only addendum: makes R11 the canonical reference for the new schema surface.
    goals: |
      1. R11 documents placement fields.
      2. R11 documents split seeds + legacy fan-out.
      3. R11 documents `vary:` grammar (range objects + `seed_scope`).
    systems_map: |
      `docs/sprite-gen-art-design-system.md` R11 section.
    impl_plan_sketch: |
      Phase 1 — Draft addendum text; Phase 2 — Cross-link to spec files under `tools/sprite-gen/specs/`; Phase 3 — Grep-check for updated fields.
```

**Dependency gate:** Stage 6.2 merged (TECH-704..708). L12 stage order lock. `bootstrap-variants --from-signature` (TECH-712) specifically depends on `signatures/` directory existing (TECH-705) and `variants:` block loader (TECH-710).

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.3 tasks **TECH-709**..**TECH-714** aligned with §3 Stage 6.3 block of `/tmp/sprite-gen-improvement-session.md`; locks L5/L6/L7/L14 mapped one-to-one. Aggregate doc: `docs/implementation/sprite-gen-stage-6.3-plan.md`. Downstream: file Stage 6.4.

---

### Stage 6.4 — Ground variation

**Status:** Final — 2026-04-23. Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 6.4 block (`/tmp/sprite-gen-improvement-session.md`). **Locks consumed:** L8 (`ground:` accepts string or object; back-compat by construction), L9 (`ground.`* joins `vary:` vocabulary; signature bounds jitter), L10 (new primitive `iso_ground_noise`; palette gains `accent_dark`/`accent_light`).

**Objectives:** Extend the ground surface beyond a single material string. Accept an object form `{material, materials, hue_jitter, value_jitter, texture}` on the spec (string form still valid; normalises to `{material: <str>}` with zero jitter / no texture). Wire the composer to sample hue/value jitter per variant and auto-insert an `iso_ground_noise` pass when `ground.texture` is set. Ship the new `iso_ground_noise` primitive + palette JSON `accent_dark`/`accent_light` extensions. Extend the signature extractor (TECH-704) so `ground.dominant` + `ground.variance` drive `vary.ground.`* bounds. Add `vary.ground.*` grammar (consumes L9). Land `tests/test_ground_variation.py`. DAS §4.1 addendum.

**Exit:**

- `tools/sprite-gen/src/spec.py` — `ground:` accepts string (normalise to `{material: <str>}`, zero jitter, no texture) or full object (`material` OR `materials: [...]`, `hue_jitter`, `value_jitter`, `texture: {primitive, density, palette}`); also accepts `vary.ground.{material, hue_jitter, value_jitter, texture.density}` inside `variants.vary`.
- `tools/sprite-gen/src/primitives/iso_ground_noise.py` — new primitive `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)`; scatters accent pixels inside diamond mask only (density 0..0.15 guardrail).
- `tools/sprite-gen/palettes/*.json` — schema gains optional `accent_dark` / `accent_light` per material key (absent → noise primitive no-ops).
- `tools/sprite-gen/src/compose.py` — applies ground `hue_jitter` / `value_jitter` per variant (sampled from `palette_seed + i`) before rendering `iso_ground_diamond`; auto-inserts `iso_ground_noise` pass when `ground.texture` set (author does not hand-add to `composition:`).
- `tools/sprite-gen/src/signature.py` — extractor writes `ground.dominant` + `ground.variance` (consumed by `bootstrap-variants --from-signature` when deriving `vary.ground.`* bounds).
- `tools/sprite-gen/tests/test_ground_variation.py` — covers legacy string form (byte-identical), object form, material pool, jitter non-zero diff, noise primitive mask + density.
- `docs/sprite-gen-art-design-system.md` §4.1 addendum — documents `accent_dark` / `accent_light` palette keys + `iso_ground_noise` density range.
- `pytest tools/sprite-gen/tests/` exits 0.

**Phases:**

- Phase 1 — Ground schema: string/object form loader normalization.
- Phase 2 — Palette JSON `accent_dark`/`accent_light` keys.
- Phase 3 — `iso_ground_noise` primitive.
- Phase 4 — Composer ground jitter + texture auto-insert.
- Phase 5 — Signature extractor `ground.`* extension.
- Phase 6 — `vary.ground.*` grammar.
- Phase 7 — Tests: `test_ground_variation.py`.
- Phase 8 — DAS §4.1 addendum.

**Tasks:**


| Task   | Name                                                     | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                                        |
| ------ | -------------------------------------------------------- | ------------ | ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.4.1 | Ground schema: string / object form loader normalization | **TECH-715** | Done  | `tools/sprite-gen/src/spec.py` — `ground:` accepts either string (normalises to `{material: <str>}`, zero jitter, no texture) or full object (`material` OR `materials: [...]`, optional `hue_jitter: float`, `value_jitter: float`, `texture: {primitive, density, palette}`). Back-compat: string form stays byte-identical. Consumes L8.                                   |
| T6.4.2 | Palette JSON `accent_dark` / `accent_light` keys         | **TECH-716** | Done  | `tools/sprite-gen/palettes/*.json` — schema gains optional `accent_dark` / `accent_light` per material key. Palette loader surfaces both; absent → consumer no-ops (noise primitive skips scatter). Seed default values for `grass_flat` + `pavement` so Stage 6.4 ships with at least 2 materials texturable. Consumes L10 (palette surface).                                |
| T6.4.3 | `iso_ground_noise` primitive                             | **TECH-717** | Done  | `tools/sprite-gen/src/primitives/iso_ground_noise.py` — new primitive `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)`. Scatters accent pixels inside diamond mask only (no bleed onto building area). Density clamped 0..0.15 (guardrail). Deterministic under seed. Consumes L10 (primitive surface).                                                   |
| T6.4.4 | Composer ground jitter + texture auto-insert             | **TECH-718** | Done  | `tools/sprite-gen/src/compose.py` — before rendering `iso_ground_diamond`, apply `hue_jitter` / `value_jitter` sampled from `palette_seed + i` to the material's ramp. When `ground.texture` set, auto-insert an `iso_ground_noise` pass between the diamond and the first building primitive; author never hand-adds to `composition:`. Legacy string-form specs unchanged.  |
| T6.4.5 | Signature extractor `ground.`* extension                 | **TECH-719** | Done  | `tools/sprite-gen/src/signature.py` — extend extractor to populate `ground.dominant` (dominant palette on ground-only band of reference sprite) + `ground.variance` (hue_stddev + value_stddev across samples). Matches JSON shape from TECH-704 spec. Consumed by `bootstrap-variants --from-signature` to derive `vary.ground.`* bounds. Consumes L9 upstream.              |
| T6.4.6 | `vary.ground.*` grammar                                  | **TECH-720** | Done  | `tools/sprite-gen/src/spec.py` — accept `vary.ground.{material: {values: [...]}, hue_jitter: {min, max}, value_jitter: {min, max}, texture: {density: {min, max}}}` inside `variants.vary`. Loader validates range objects (from TECH-710). Composer variant loop samples these from `palette_seed + i`. Consumes L9 (vary surface).                                          |
| T6.4.7 | Tests: `test_ground_variation.py`                        | **TECH-721** | Done  | `tools/sprite-gen/tests/test_ground_variation.py` — covers (a) legacy string form byte-identical, (b) object form round-trip, (c) `materials: [...]` pool renders one of each seeded, (d) non-zero jitter produces per-variant diffs (`!= 0` pixel diff), (e) zero jitter produces byte-identical variants, (f) noise primitive: mask clipped to diamond + density monotonic. |
| T6.4.8 | DAS §4.1 addendum — palette accent keys + noise density  | **TECH-722** | Done  | `docs/sprite-gen-art-design-system.md` §4.1 — document `accent_dark` / `accent_light` palette keys + `iso_ground_noise` density range (0..0.15 guardrail). Forward-pointer to `signatures/` for authoring `vary.ground.`* bounds.                                                                                                                                             |


### §Stage File Plan



```yaml
- reserved_id: TECH-715
  title: Ground schema — string / object form loader normalization
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — `ground:` accepts string (normalises to `{material: <str>}`, zero jitter, no texture) or full object (`material` OR `materials: [...]`, optional `hue_jitter: float`, `value_jitter: float`, `texture: {primitive, density, palette}`). Back-compat: string form stays byte-identical. Consumes L8.
  depends_on:
    - TECH-709
    - TECH-710
    - TECH-711
    - TECH-712
    - TECH-713
    - TECH-714
  related:
    - TECH-718
    - TECH-720
  stub_body:
    summary: |
      Normalise `ground:` at the loader so the composer reads one object shape regardless of author form. Byte-identical output for legacy string specs.
    goals: |
      1. String form normalises to `{material: <str>}`, zero jitter, no texture.
      2. Object form round-trips with defaults filled in.
      3. Explicit `materials: [...]` accepted (pool for variant sampling).
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: composer (TECH-718), vary loader (TECH-720).
    impl_plan_sketch: |
      Phase 1 — Detect str vs dict; emit object; Phase 2 — Fill defaults; Phase 3 — Unit tests.
- reserved_id: TECH-716
  title: Palette JSON accent_dark / accent_light keys
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/palettes/*.json` — schema gains optional `accent_dark` / `accent_light` per material key. Palette loader surfaces both; absent → noise primitive no-ops. Seed `grass_flat` + `pavement` so Stage 6.4 ships with texturable materials.
  depends_on:
    - TECH-714
  related:
    - TECH-717
  stub_body:
    summary: |
      Extend palette schema with optional accent keys; seed two materials so noise primitive has consumers at Stage 6.4 close.
    goals: |
      1. Palette loader surfaces `accent_dark` / `accent_light` (or None).
      2. `grass_flat` + `pavement` seeded with both keys in the active palette.
      3. Existing palette entries unchanged.
    systems_map: |
      `tools/sprite-gen/palettes/*.json`; `tools/sprite-gen/src/palette.py` (loader).
    impl_plan_sketch: |
      Phase 1 — Loader surfaces optional keys; Phase 2 — Seed values in active palette JSON; Phase 3 — Unit test for absence → None.
- reserved_id: TECH-717
  title: iso_ground_noise primitive
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/primitives/iso_ground_noise.py` — new primitive `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)`. Scatters accent pixels inside diamond mask only (no bleed onto building area). Density clamped 0..0.15. Deterministic under seed.
  depends_on:
    - TECH-716
  related:
    - TECH-718
  stub_body:
    summary: |
      Scatter-pixel primitive that textures the ground diamond with palette accent colours. Self-masks to diamond shape; respects density guardrail; deterministic under seed.
    goals: |
      1. Primitive signature matches composer dispatch contract.
      2. Scatter confined to diamond mask (verified via pixel accounting).
      3. Density 0..0.15 hard-clamp.
      4. Seed determinism: same (x0, y0, material, density, seed, palette) → identical pixels.
    systems_map: |
      `tools/sprite-gen/src/primitives/iso_ground_noise.py`; register in `src/primitives/__init__.py` + `src/compose.py::_DISPATCH`.
    impl_plan_sketch: |
      Phase 1 — Diamond mask from `iso_ground_diamond` geometry; Phase 2 — Pixel scatter from `random.Random(seed)`; Phase 3 — Density clamp + palette accent lookup.
- reserved_id: TECH-718
  title: Composer ground jitter + texture auto-insert
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — before rendering `iso_ground_diamond`, apply `hue_jitter` / `value_jitter` sampled from `palette_seed + i` to the material's ramp. When `ground.texture` set, auto-insert an `iso_ground_noise` pass between the diamond and the first building primitive. Legacy string-form specs unchanged.
  depends_on:
    - TECH-715
    - TECH-717
  related:
    - TECH-720
  stub_body:
    summary: |
      Composer honours jitter + texture fields. Legacy byte-identical guard still passes.
    goals: |
      1. Jitter sampled from `palette_seed + i`; zero jitter → byte-identical vs baseline.
      2. `ground.texture` set → noise pass auto-inserted.
      3. Author never hand-adds `iso_ground_noise` to `composition:`.
    systems_map: |
      `tools/sprite-gen/src/compose.py`; depends on `src/primitives/iso_ground_noise.py` (TECH-717).
    impl_plan_sketch: |
      Phase 1 — Jitter helper on palette ramp; Phase 2 — Wire into `iso_ground_diamond` call; Phase 3 — Conditional noise-pass insertion; Phase 4 — Byte-identical legacy regression.
- reserved_id: TECH-719
  title: Signature extractor ground.* extension
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/signature.py` — extend extractor to populate `ground.dominant` (dominant palette on ground-only band of reference sprite) + `ground.variance` (hue_stddev + value_stddev across samples). Matches JSON shape from TECH-704. Consumed by `bootstrap-variants --from-signature` to derive `vary.ground.`* bounds.
  depends_on:
    - TECH-714
  related:
    - TECH-712
    - TECH-720
  stub_body:
    summary: |
      Signature extractor now fills the `ground` block — lets `bootstrap-variants` propose data-driven jitter ranges instead of hand-tuned guesses.
    goals: |
      1. `ground.dominant` populated from ground-band pixels.
      2. `ground.variance.hue_stddev` + `value_stddev` populated.
      3. L15 policy respected (fallback mode leaves ground fields null).
    systems_map: |
      `tools/sprite-gen/src/signature.py`; consumers: `src/__main__.py::bootstrap-variants` (TECH-712 extension).
    impl_plan_sketch: |
      Phase 1 — Ground-band isolation on reference sprite; Phase 2 — Palette dominant + stddev math; Phase 3 — JSON shape verification against TECH-704 spec.
- reserved_id: TECH-720
  title: vary.ground.* grammar
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — accept `vary.ground.{material: {values: [...]}, hue_jitter: {min, max}, value_jitter: {min, max}, texture: {density: {min, max}}}` inside `variants.vary`. Loader validates range objects (TECH-710). Composer variant loop samples these from `palette_seed + i`.
  depends_on:
    - TECH-710
    - TECH-715
  related:
    - TECH-718
  stub_body:
    summary: |
      Extend the `vary:` grammar with a ground axis so variants can randomise material / jitter / noise density without hand-rolling a per-spec loop.
    goals: |
      1. Loader validates `vary.ground.`* range objects.
      2. Composer samples these from `palette_seed + i`.
      3. Back-compat: specs without `vary.ground` unchanged.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: composer variant loop (TECH-718).
    impl_plan_sketch: |
      Phase 1 — Extend `_normalize_variants` validation to accept ground axis; Phase 2 — Composer samples during variant iteration; Phase 3 — Unit tests.
- reserved_id: TECH-721
  title: Tests — test_ground_variation.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_ground_variation.py` — covers (a) legacy string form byte-identical, (b) object form round-trip, (c) `materials: [...]` pool renders one of each seeded, (d) non-zero jitter produces per-variant diffs, (e) zero jitter produces byte-identical variants, (f) noise primitive: mask clipped to diamond + density monotonic.
  depends_on:
    - TECH-718
    - TECH-720
  related: []
  stub_body:
    summary: |
      One test file covering every surface touched by Stage 6.4. Catches jitter leakage, noise-mask bleed, and materials pool non-determinism.
    goals: |
      1. Six named cases (legacy, object, pool, non-zero jitter, zero jitter, noise mask).
      2. Reproducibility: same seeds → identical output across runs.
      3. Full suite `pytest tools/sprite-gen/tests/ -q` green.
    systems_map: |
      `tools/sprite-gen/tests/test_ground_variation.py` (new); consumers: `src/compose.py` + `src/primitives/iso_ground_noise.py`.
    impl_plan_sketch: |
      Phase 1 — Legacy + object form; Phase 2 — Pool; Phase 3 — Jitter diffs; Phase 4 — Noise primitive mask / density.
- reserved_id: TECH-722
  title: DAS §4.1 addendum — accent keys + noise density
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §4.1 — document `accent_dark` / `accent_light` palette keys + `iso_ground_noise` density range (0..0.15 guardrail). Forward-pointer to `signatures/` for authoring `vary.ground.*` bounds.
  depends_on:
    - TECH-716
    - TECH-717
  related:
    - TECH-708
  stub_body:
    summary: |
      Doc addendum that closes the loop — authors learn about accent keys and noise density limits in the design system doc, not in code comments.
    goals: |
      1. §4.1 lists `accent_dark` / `accent_light` as optional per-material palette keys.
      2. §4.1 documents noise density guardrail 0..0.15.
      3. §4.1 forward-points to `signatures/` for `vary.ground.*` authoring.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §4.1.
    impl_plan_sketch: |
      Phase 1 — Locate §4.1 table; Phase 2 — Append 3 short subsections; Phase 3 — Grep check.
```

**Dependency gate:** Stage 6.2 merged (TECH-704..708) + Stage 6.3 merged (TECH-709..714). L12 stage order lock. Signature extension (TECH-719) specifically extends TECH-704's extractor.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.4 tasks **TECH-715**..**TECH-722** aligned with §3 Stage 6.4 block of `/tmp/sprite-gen-improvement-session.md`; locks L8/L9/L10 mapped one-to-one. Aggregate doc: `docs/implementation/sprite-gen-stage-6.4-plan.md`. Downstream: file Stage 6.5.

### §Stage Closeout Plan

> Scope: Stage 6.4 — Ground variation (TECH-715..722). All 8 tasks PASS code-review + audit. 330/330 tests green. Ready for archive.

```yaml
stage: "6.4"
status_flip:
  - file: ia/projects/sprite-gen-master-plan.md
    targets:
      - {row: T6.4.1, field: Status, from: Draft, to: Done}
      - {row: T6.4.2, field: Status, from: Draft, to: Done}
      - {row: T6.4.3, field: Status, from: Draft, to: Done}
      - {row: T6.4.4, field: Status, from: Draft, to: Done}
      - {row: T6.4.5, field: Status, from: Draft, to: Done}
      - {row: T6.4.6, field: Status, from: Draft, to: Done}
      - {row: T6.4.7, field: Status, from: Draft, to: Done}
      - {row: T6.4.8, field: Status, from: Draft, to: Done}
      - {row: "Stage 6.4", field: Status, from: Draft, to: Final}

archive_records:
  - id: TECH-715
    src: ia/backlog/TECH-715.yaml
    dst: ia/backlog-archive/TECH-715.yaml
  - id: TECH-716
    src: ia/backlog/TECH-716.yaml
    dst: ia/backlog-archive/TECH-716.yaml
  - id: TECH-717
    src: ia/backlog/TECH-717.yaml
    dst: ia/backlog-archive/TECH-717.yaml
  - id: TECH-718
    src: ia/backlog/TECH-718.yaml
    dst: ia/backlog-archive/TECH-718.yaml
  - id: TECH-719
    src: ia/backlog/TECH-719.yaml
    dst: ia/backlog-archive/TECH-719.yaml
  - id: TECH-720
    src: ia/backlog/TECH-720.yaml
    dst: ia/backlog-archive/TECH-720.yaml
  - id: TECH-721
    src: ia/backlog/TECH-721.yaml
    dst: ia/backlog-archive/TECH-721.yaml
  - id: TECH-722
    src: ia/backlog/TECH-722.yaml
    dst: ia/backlog-archive/TECH-722.yaml

delete_specs: []  # executed at closeout 2026-04-23; spec files removed

validation_gate:
  - cmd: "bash tools/scripts/materialize-backlog.sh"
  - cmd: "npm run validate:all"
```

---

### Stage 6.5 — Curation-trained quality gate

**Status:** Draft — 2026-04-23. Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 6.5 block (`/tmp/sprite-gen-improvement-session.md`). **Locks consumed:** L11 (curation/promoted.jsonl + rejected.jsonl feed the signature aggregator; composer gates renders against the evolving envelope).

**Objectives:** Close the feedback loop from artist curation back into the generator. `curate.py` gains `log-promote` + `log-reject --reason` subcommands that append JSONL rows (verb names disambiguate from existing `promote` = PNG→Unity ship + `reject` = glob-delete — TECH-179). The signature extractor becomes a three-source aggregator: `envelope = catalog ∪ promoted − rejected-zones` (rejection reasons carve out floor zones in `vary.`*). The composer adds a render-time gate: sample `vary:` → render → score against the evolving envelope → re-sample up to N times; after N, write best-scoring variant and mark a `.needs_review` metadata sidecar. Ship tests + DAS §5 addendum.

**Exit:**

- `tools/sprite-gen/src/curate.py` — `log-promote` appends JSONL row to `curation/promoted.jsonl` (rendered variant + sampled `vary:` values + measured bbox/palette stats); `log-reject --reason <tag>` appends to `curation/rejected.jsonl`.
- `tools/sprite-gen/src/signature.py` — aggregator `envelope = catalog ∪ promoted − rejected-zones`; rejection reasons map to `vary.`* floor zones (e.g. `roof-too-shallow` → floor on `vary.roof.h_px`).
- `tools/sprite-gen/src/compose.py` — render-time score-and-retry loop: sample `vary:` → render → score → if below floor, re-sample (configurable N, default 5).
- `tools/sprite-gen/src/compose.py` — after N retries without meeting floor, write best-scoring output + `.needs_review` sidecar in metadata.
- `tools/sprite-gen/tests/test_curation_loop.py` — (a) envelope tightens toward promoted samples after N promotes (before/after fixture); (b) `vary:` range shrinks in direction of rejection reasons (before/after fixture); (c) `.needs_review` flag set when floor not met in N tries.
- `docs/sprite-gen-art-design-system.md` §5 addendum — curation loop + scoring floor + `.needs_review` semantics.
- `pytest tools/sprite-gen/tests/` exits 0.

**Phases:**

- Phase 1 — `curate.py log-promote` subcommand + `promoted.jsonl` writer.
- Phase 2 — `curate.py log-reject --reason` subcommand + `rejected.jsonl` writer.
- Phase 3 — Signature three-source aggregator (catalog ∪ promoted − rejected-zones).
- Phase 4 — Composer render-time score-and-retry gate (N retries, default 5).
- Phase 5 — `.needs_review` sidecar writer on floor-miss.
- Phase 6 — Tests: `test_curation_loop.py`.
- Phase 7 — DAS §5 addendum.

**Tasks:**


| Task   | Name                                              | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                               |
| ------ | ------------------------------------------------- | ------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.5.1 | `curate.py log-promote` → `promoted.jsonl`        | **TECH-723** | Done   | `tools/sprite-gen/src/curate.py` — add `log-promote <variant>` subcommand that appends JSONL row to `curation/promoted.jsonl`. Row carries: rendered variant path, sampled `vary:` values, measured bbox/palette stats from the rendered image. Idempotent append; no mutation. Verb disambiguates from existing `promote` (TECH-179 PNG→Unity ship). Consumes L11.                                             |
| T6.5.2 | `curate.py log-reject --reason` → `rejected.jsonl`| **TECH-724** | Done   | `tools/sprite-gen/src/curate.py` — add `log-reject <variant> --reason <tag>` subcommand. `<tag>` is a controlled vocabulary (initial set: `roof-too-shallow`, `roof-too-tall`, `facade-too-saturated`, `ground-too-uniform`). Row format mirrors `promoted.jsonl` plus `reason: <tag>`. Verb disambiguates from existing `reject` (TECH-179 glob-delete). Consumes L11.                                           |
| T6.5.3 | Signature three-source aggregator                 | **TECH-725** | Done   | `tools/sprite-gen/src/signature.py` — `compute_envelope(catalog, promoted, rejected)` returns `vary.`* bounds where `envelope = catalog ∪ promoted − rejected-zones`. Each rejection `reason` maps to a zone carve-out (e.g. `roof-too-shallow` floors `vary.roof.h_px.min`). Deterministic. Consumes L11.                                           |
| T6.5.4 | Composer render-time score-and-retry gate         | **TECH-726** | Done   | `tools/sprite-gen/src/compose.py` — wrap variant render in score-and-retry loop: sample `vary:` from envelope → render → score variant against envelope floor → if below, re-sample (new `palette_seed + i + retry`). Configurable N (default 5). Scoring heuristic: normalized distance from envelope centroid + hard-fail penalty on carved zones. |
| T6.5.5 | `.needs_review` sidecar on floor-miss             | **TECH-727** | Done   | `tools/sprite-gen/src/compose.py` — after N retries without meeting floor, emit best-scoring variant and write `<sprite>.needs_review.json` sidecar containing: final score, envelope snapshot, attempted seeds, failing zones. CI / curator consumes sidecars to surface low-confidence renders.                                                    |
| T6.5.6 | Tests: `test_curation_loop.py`                    | **TECH-728** | Done   | `tools/sprite-gen/tests/test_curation_loop.py` — three cases: (a) envelope tightens toward promoted samples after N promotes (before/after fixture); (b) `vary:` range shrinks in direction of rejection reasons (before/after); (c) `.needs_review` flag set when floor not met in N tries. Deterministic seeds throughout.                         |
| T6.5.7 | DAS §5 addendum — curation loop + floor + sidecar | **TECH-729** | Done   | `docs/sprite-gen-art-design-system.md` §5 — new/extended section covering promotion/rejection JSONL schema, envelope aggregator rule, rejection-reason → `vary.`* zone map, composer score-and-retry contract, and `.needs_review` sidecar semantics.                                                                                                |


### §Stage File Plan



```yaml
- reserved_id: TECH-723
  title: curate.py log-promote → promoted.jsonl
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/curate.py` — new `log-promote <variant>` subcommand appending a JSONL row to `curation/promoted.jsonl`. Row carries rendered variant path, sampled `vary:` values, measured bbox/palette stats. Verb disambiguates from existing `promote` (TECH-179 PNG→Unity ship + catalog push).
  depends_on:
    - TECH-704
    - TECH-705
    - TECH-706
    - TECH-707
    - TECH-708
  related:
    - TECH-724
    - TECH-725
  stub_body:
    summary: |
      `log-promote` subcommand captures curator approvals into a JSONL log so the signature aggregator can tighten the envelope toward real artist-validated variants.
    goals: |
      1. `log-promote <variant>` appends one JSON row to `curation/promoted.jsonl`.
      2. Row carries variant path + sampled `vary:` values + measured bbox/palette stats.
      3. Idempotent append; no mutation of prior rows.
    systems_map: |
      `tools/sprite-gen/src/curate.py`; consumers: `src/signature.py::compute_envelope` (TECH-725).
    impl_plan_sketch: |
      Phase 1 — CLI subcommand scaffold; Phase 2 — Measurement helpers (bbox + palette stats); Phase 3 — JSONL writer + idempotency test.
- reserved_id: TECH-724
  title: curate.py log-reject --reason → rejected.jsonl
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/curate.py` — new `log-reject <variant> --reason <tag>` subcommand appending to `curation/rejected.jsonl`. Controlled reason vocabulary: `roof-too-shallow`, `roof-too-tall`, `facade-too-saturated`, `ground-too-uniform`. Verb disambiguates from existing `reject` (TECH-179 glob-delete).
  depends_on:
    - TECH-723
  related:
    - TECH-725
  stub_body:
    summary: |
      `log-reject` captures artist vetoes with a controlled reason tag, so the signature aggregator can carve out `vary.*` zones that produce undesirable variants.
    goals: |
      1. `log-reject <variant> --reason <tag>` appends JSONL row.
      2. Row shape mirrors `promoted.jsonl` plus `reason: <tag>`.
      3. Invalid `<tag>` → CLI error (controlled vocab enforced).
    systems_map: |
      `tools/sprite-gen/src/curate.py`; consumers: `src/signature.py::compute_envelope` (TECH-725).
    impl_plan_sketch: |
      Phase 1 — Controlled vocab constant; Phase 2 — Row writer reuses TECH-723 helpers; Phase 3 — Unit test for invalid reason.
- reserved_id: TECH-725
  title: Signature three-source aggregator
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/signature.py` — `compute_envelope(catalog, promoted, rejected)` returns `vary.*` bounds where `envelope = catalog ∪ promoted − rejected-zones`. Rejection reasons map to zone carve-outs.
  depends_on:
    - TECH-723
    - TECH-724
  related:
    - TECH-726
    - TECH-729
  stub_body:
    summary: |
      Aggregator consumes catalog signatures + promoted samples and subtracts rejected-zones, producing the live envelope the composer gate consults.
    goals: |
      1. Union of catalog + promoted tightens bounds toward validated variants.
      2. Rejection reasons carve out `vary.*` floor zones via a reason→axis map.
      3. Deterministic: same inputs → same envelope.
    systems_map: |
      `tools/sprite-gen/src/signature.py`; consumers: composer score-and-retry gate (TECH-726).
    impl_plan_sketch: |
      Phase 1 — Reason→axis carve-out table; Phase 2 — Envelope math (union + subtraction); Phase 3 — Unit tests.
- reserved_id: TECH-726
  title: Composer render-time score-and-retry gate
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — wrap variant render in score-and-retry loop: sample → render → score against envelope → re-sample up to N times (default 5). Scoring = normalized distance from envelope centroid + hard-fail penalty on carved zones.
  depends_on:
    - TECH-725
  related:
    - TECH-727
  stub_body:
    summary: |
      Composer gate rejects variants that land in carved zones or drift too far from the envelope, re-sampling until a variant passes or N retries exhausted.
    goals: |
      1. Retry count configurable; default 5.
      2. Deterministic: same seeds → same retry trajectory.
      3. Zero retries case = byte-identical to pre-gate render (feature flag off).
    systems_map: |
      `tools/sprite-gen/src/compose.py`; consumes `src/signature.py::compute_envelope` (TECH-725).
    impl_plan_sketch: |
      Phase 1 — Score function; Phase 2 — Retry loop with seed advancement; Phase 3 — Feature-flag for back-compat.
- reserved_id: TECH-727
  title: .needs_review sidecar on floor-miss
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — on N-retries exhaustion, write `<sprite>.needs_review.json` sidecar with final score, envelope snapshot, attempted seeds, failing zones. Curator consumes sidecars to surface low-confidence renders.
  depends_on:
    - TECH-726
  related: []
  stub_body:
    summary: |
      Sidecar metadata file surfaces low-confidence renders for curator review without blocking the pipeline.
    goals: |
      1. File name `<sprite>.needs_review.json` adjacent to rendered sprite.
      2. Contents: final score, envelope snapshot, attempted seeds, failing zones.
      3. Absent when variant meets floor within retries.
    systems_map: |
      `tools/sprite-gen/src/compose.py`; consumer: curator tooling / CI gate (future).
    impl_plan_sketch: |
      Phase 1 — Sidecar schema dataclass; Phase 2 — Writer on floor-miss branch; Phase 3 — Absence test on floor-met branch.
- reserved_id: TECH-728
  title: Tests — test_curation_loop.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_curation_loop.py` — (a) envelope tightens toward promoted samples after N promotes (before/after fixture); (b) `vary:` range shrinks in direction of rejection reasons (before/after); (c) `.needs_review` flag set when floor not met in N tries.
  depends_on:
    - TECH-726
    - TECH-727
  related: []
  stub_body:
    summary: |
      One test file exercising the full curation → aggregator → gate → sidecar loop with deterministic before/after fixtures.
    goals: |
      1. Before/after envelope comparison after N promotes.
      2. Before/after `vary.*` range after N rejects with a named reason.
      3. `.needs_review` sidecar presence/absence assertion.
    systems_map: |
      `tools/sprite-gen/tests/test_curation_loop.py`; consumers: `curate.py`, `signature.py`, `compose.py`.
    impl_plan_sketch: |
      Phase 1 — Before/after envelope test; Phase 2 — Rejection-zone test; Phase 3 — Needs_review test.
- reserved_id: TECH-729
  title: DAS §5 addendum — curation loop + floor + sidecar
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §5 — promotion/rejection JSONL schema, envelope aggregator rule, rejection-reason → `vary.*` zone map, composer score-and-retry contract, `.needs_review` sidecar semantics.
  depends_on:
    - TECH-723
    - TECH-724
    - TECH-725
    - TECH-726
    - TECH-727
  related: []
  stub_body:
    summary: |
      Docs close the loop — artists learn the curation contract + reason vocabulary + what `.needs_review` means from the design system doc, not code comments.
    goals: |
      1. §5 documents JSONL schema for promoted / rejected rows.
      2. §5 publishes rejection-reason → `vary.*` zone carve-out map.
      3. §5 documents `.needs_review` sidecar semantics.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §5.
    impl_plan_sketch: |
      Phase 1 — JSONL schema table; Phase 2 — Reason→axis map table; Phase 3 — Sidecar semantics subsection.
```

**Dependency gate:** Stage 6.2 merged (TECH-704..708). L12 stage order lock. Signature aggregator (TECH-725) specifically extends TECH-704's extractor with new inputs.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.5 tasks **TECH-723**..**TECH-729** aligned with §3 Stage 6.5 block of `/tmp/sprite-gen-improvement-session.md`; lock L11 threaded through all 7 tasks. Aggregate doc: `docs/implementation/sprite-gen-stage-6.5-plan.md`. Downstream: file Stage 6.6.

---

### Stage 6.6 — Preset system

**Status:** Draft — 2026-04-23. Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 6.6 block (`/tmp/sprite-gen-improvement-session.md`). **Locks consumed:** L13 (`preset: <name>` top-level key injects a base spec; author fields override; `vary:` block from preset is preserved — author may extend / override individual `vary.*` entries but not wipe the block).

**Objectives:** Let authors bootstrap a sprite from a named preset that already carries geometry, palette, placement, and `vary:` decisions. `tools/sprite-gen/presets/<name>.yaml` holds fully-valid specs (minus `id` / `output.name`). The loader recognises `preset: <name>`, injects the preset as base, applies author overrides, and preserves the preset's `vary:` block under a strict merge rule (union on axes; non-wipe on the block itself). Seed three presets — `suburban_house_with_yard`, `strip_mall_with_parking`, `row_houses_3x` — so Stage 6.6 ships with live consumers. Tests lock override + preservation + preset-referenced-twice determinism. DAS §6 addendum.

**Exit:**

- `tools/sprite-gen/src/spec.py` — `preset: <name>` top-level key resolves to `tools/sprite-gen/presets/<name>.yaml`, merged with author fields (author wins per-field); `vary:` merge rule preserves preset axes, allows author to add / override individual axes, and raises `SpecError` on author attempting to wipe the whole `vary:` block.
- `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — fully-valid spec minus `id` / `output.name`.
- `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — fully-valid spec minus `id` / `output.name`.
- `tools/sprite-gen/presets/row_houses_3x.yaml` — fully-valid spec minus `id` / `output.name`.
- `tools/sprite-gen/tests/test_preset_system.py` — override semantics; `vary:` preservation; preset-referenced-twice determinism; missing-preset error.
- `docs/sprite-gen-art-design-system.md` §6 addendum — preset contract, merge rule, seeded presets catalogue.
- `pytest tools/sprite-gen/tests/` exits 0.

**Phases:**

- [ ] Phase 1 — Loader: `preset: <name>` key + base-inject + author-override merge.
- [ ] Phase 2 — `vary:` block merge rule (union + non-wipe guard).
- [ ] Phase 3 — Seed preset `suburban_house_with_yard.yaml`.
- [ ] Phase 4 — Seed preset `strip_mall_with_parking.yaml`.
- [ ] Phase 5 — Seed preset `row_houses_3x.yaml`.
- [ ] Phase 6 — Tests: `test_preset_system.py`.
- [ ] Phase 7 — DAS §6 addendum.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.6.1 | Loader: `preset: <name>` inject + author override | **TECH-730** | Done | `tools/sprite-gen/src/spec.py` — new `preset: <name>` top-level key. Loader resolves to `tools/sprite-gen/presets/<name>.yaml`, parses as base, then applies author-provided fields as overrides (author wins per-field). Missing preset file → `SpecError` listing valid preset names. Consumes L13. |
| T6.6.2 | `vary:` block merge rule (union + non-wipe) | **TECH-731** | Done | `tools/sprite-gen/src/spec.py` — `vary:` merge: preset axes preserved by default; author may add new `vary.*` axes or override individual axis values; author writing `vary: {}` or `vary: null` to wipe the block raises `SpecError`. Ensures preset-driven variation can't be silently disabled. |
| T6.6.3 | Seed `presets/suburban_house_with_yard.yaml` | **TECH-732** | Done | `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — fully-valid spec (minus `id` / `output.name`) tuned for `residential_small` class: house footprint + grass ground (texture on) + `vary:` block over roof / facade / ground. Renders without author overrides. |
| T6.6.4 | Seed `presets/strip_mall_with_parking.yaml` | **TECH-733** | Done | `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — fully-valid spec (minus `id` / `output.name`) tuned for commercial strip: wide footprint + pavement ground + `vary:` block over facade + ground. Renders without author overrides. |
| T6.6.5 | Seed `presets/row_houses_3x.yaml` | **TECH-734** | Done | `tools/sprite-gen/presets/row_houses_3x.yaml` — fully-valid spec (minus `id` / `output.name`) using `tiled-row-3` slot (Stage 9 addendum) + shared grass ground + per-row `vary:` block. Renders without author overrides when Stage 9 addendum lands. |
| T6.6.6 | Tests: `test_preset_system.py` | **TECH-735** | Done | `tools/sprite-gen/tests/test_preset_system.py` — (a) author field wins merge; (b) author `vary.padding` doesn't erase preset `vary.roof`; (c) author `vary: null` raises; (d) preset referenced twice with same seed → byte-identical output; (e) missing preset → `SpecError` with valid list. |
| T6.6.7 | DAS §6 addendum — preset contract + catalogue | **TECH-736** | Done | `docs/sprite-gen-art-design-system.md` §6 — document `preset: <name>` key + merge rule + `vary:` preservation semantic + the three seeded presets. Forward-pointer to `presets/` dir for discoverability. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-730
  title: Loader — preset key inject + author override
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — new `preset: <name>` top-level key. Loader resolves to `tools/sprite-gen/presets/<name>.yaml`, parses as base, applies author-provided fields as overrides (author wins per-field). Missing preset → `SpecError` with valid list.
  depends_on:
    - TECH-709
    - TECH-710
    - TECH-711
    - TECH-712
    - TECH-713
    - TECH-714
  related:
    - TECH-731
  stub_body:
    summary: |
      Top-level `preset:` key resolves to a base YAML, merged with author overrides. Errors early + points to valid preset names.
    goals: |
      1. `preset: <name>` resolves + parses base YAML.
      2. Author fields override preset fields per-key.
      3. Missing preset → `SpecError` listing valid names.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumes `tools/sprite-gen/presets/*.yaml` (TECH-732..734).
    impl_plan_sketch: |
      Phase 1 — Detect `preset` key; Phase 2 — Load base YAML; Phase 3 — Deep-merge author fields; Phase 4 — Error on missing preset.
- reserved_id: TECH-731
  title: vary block merge rule (union + non-wipe)
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — `vary:` merge: preset axes preserved by default; author may add / override individual axes; author attempting to wipe the block (`vary: null` or `vary: {}`) raises `SpecError`.
  depends_on:
    - TECH-730
  related: []
  stub_body:
    summary: |
      `vary:` merge preserves preset-supplied axes; author can extend or override per axis; wiping the block raises.
    goals: |
      1. Preset axes survive unless explicitly overridden per axis.
      2. Author new axes merge in (union).
      3. `vary: {}` / `vary: null` from author → `SpecError`.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumer: `tests/test_preset_system.py` (TECH-735).
    impl_plan_sketch: |
      Phase 1 — Detect author `vary` shape; Phase 2 — Union merge with preset; Phase 3 — Wipe-guard raises.
- reserved_id: TECH-732
  title: Seed preset — suburban_house_with_yard
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — fully-valid spec (minus `id`/`output.name`) for `residential_small` class with grass ground (texture on) + `vary:` covering roof / facade / ground.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-715
    - TECH-718
  related:
    - TECH-735
  stub_body:
    summary: |
      First seed preset — residential_small with grass yard + ground texture + variation over roof / facade / ground.
    goals: |
      1. Renders cleanly with `preset: suburban_house_with_yard` and no author overrides.
      2. `vary:` covers ≥3 axes (roof, facade, ground).
      3. Ground uses Stage 6.4 object form (material + texture).
    systems_map: |
      `tools/sprite-gen/presets/suburban_house_with_yard.yaml` (new).
    impl_plan_sketch: |
      Phase 1 — Copy base from `building_residential_small.yaml`; Phase 2 — Strip `id` / `output.name`; Phase 3 — Add yard ground + `vary:`.
- reserved_id: TECH-733
  title: Seed preset — strip_mall_with_parking
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — fully-valid spec (minus `id`/`output.name`) for commercial strip: wide footprint + pavement ground + `vary:` over facade + ground.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-715
    - TECH-718
  related:
    - TECH-735
  stub_body:
    summary: |
      Second seed preset — commercial strip with pavement ground and variation over facade + ground.
    goals: |
      1. Renders cleanly with `preset: strip_mall_with_parking` and no author overrides.
      2. Pavement ground uses Stage 6.4 accent keys (TECH-716 seeded).
      3. `vary:` covers ≥2 axes (facade, ground).
    systems_map: |
      `tools/sprite-gen/presets/strip_mall_with_parking.yaml` (new).
    impl_plan_sketch: |
      Phase 1 — Scaffold spec with wide footprint; Phase 2 — Pavement ground + texture; Phase 3 — `vary:` block.
- reserved_id: TECH-734
  title: Seed preset — row_houses_3x
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/presets/row_houses_3x.yaml` — fully-valid spec (minus `id`/`output.name`) using `tiled-row-3` slot (Stage 9 addendum) + shared grass ground + per-row `vary:` block.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-744
  related:
    - TECH-735
  stub_body:
    summary: |
      Third seed preset — row-houses 3x pattern riding Stage 9's parametric `tiled-row-N` slot.
    goals: |
      1. Renders cleanly with `preset: row_houses_3x` once Stage 9 addendum lands.
      2. Uses `tiled-row-3` slot (parametric slot grammar).
      3. Shared grass ground across row; `vary:` applies per-house.
    systems_map: |
      `tools/sprite-gen/presets/row_houses_3x.yaml` (new).
    impl_plan_sketch: |
      Phase 1 — Scaffold with `tiled-row-3`; Phase 2 — Shared ground; Phase 3 — Per-house `vary:`.
- reserved_id: TECH-735
  title: Tests — test_preset_system.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_preset_system.py` — (a) author field wins merge; (b) author `vary.padding` doesn't erase preset `vary.roof`; (c) author `vary: null` raises; (d) preset-referenced-twice determinism; (e) missing preset → `SpecError` with valid list.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-732
    - TECH-733
    - TECH-734
  related: []
  stub_body:
    summary: |
      One test file locking preset loader behaviour end-to-end.
    goals: |
      1. Five named tests (override, vary preservation, vary wipe raises, determinism, missing preset).
      2. Uses each seeded preset at least once.
      3. Deterministic seeds throughout.
    systems_map: |
      `tools/sprite-gen/tests/test_preset_system.py`; consumers: spec loader (TECH-730/731).
    impl_plan_sketch: |
      Phase 1 — Override test; Phase 2 — Vary preservation + wipe guard; Phase 3 — Determinism + missing preset.
- reserved_id: TECH-736
  title: DAS §6 addendum — preset contract + catalogue
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §6 — document `preset: <name>` key + merge rule + `vary:` preservation + the three seeded presets. Forward-pointer to `presets/` dir.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-732
    - TECH-733
    - TECH-734
  related: []
  stub_body:
    summary: |
      Doc addendum — preset contract + merge rule + catalogue of three seeded presets.
    goals: |
      1. §6 documents `preset: <name>` grammar + resolution rule.
      2. §6 documents merge rule (author overrides; `vary:` union; wipe raises).
      3. §6 catalogues the three seeded presets with short descriptions.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §6.
    impl_plan_sketch: |
      Phase 1 — Locate §6; Phase 2 — Append contract + merge rule; Phase 3 — Catalogue table.
```

**Dependency gate:** Stage 6.3 merged (TECH-709..714) for `vary:` grammar that presets carry; Stage 6.4 merged (TECH-715/718) for ground object form used by seeded presets. `row_houses_3x` additionally waits on Stage 9 addendum (TECH-744 — parametric `tiled-row-N` slot) before it renders cleanly.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.6 tasks **TECH-730**..**TECH-736** aligned with §3 Stage 6.6 block of `/tmp/sprite-gen-improvement-session.md`; lock L13 threaded through loader + merge rule + seeded presets. Aggregate doc: `docs/implementation/sprite-gen-stage-6.6-plan.md`. Downstream: file Stage 6.7.

---

### Stage 6.7 — Animation schema reservation (tiny)

**Status:** Draft — 2026-04-23. Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 6.7 block (`/tmp/sprite-gen-improvement-session.md`). **Locks consumed:** L16 (reserve animation schema today; implementation deferred).

**Objectives:** Reserve the animation schema in the spec grammar without implementing any frame-based rendering. Spec loader accepts `output.animation:` reserved block but the only permitted value in v1 is `enabled: false`; anything else raises. Per-primitive `animate: none` key is accepted; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. DAS §12 gets a new "Animation (reserved; not yet implemented)" stub documenting the reserved keys. Independent stage — no code-path dependency.

**Exit:**

- `tools/sprite-gen/src/spec.py` — recognises `output.animation:` block; validates `enabled: false` only (other values → `SpecError`); permits reserved sibling keys (`frames`, `fps`, `loop`, `phase_offset`, `layers`) without interpreting them.
- `tools/sprite-gen/src/primitives/*.py` (or `compose.py` per-primitive dispatch) — accepts `animate: none`; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`.
- `tools/sprite-gen/tests/test_animation_reservation.py` — reserved block accepted; `enabled: true` raises; `animate: flicker` raises `NotImplementedError`.
- `docs/sprite-gen-art-design-system.md` §12 — new stub "Animation (reserved; not yet implemented)" documenting the reserved schema + acceptable v1 values.
- `pytest tools/sprite-gen/tests/` exits 0.

**Phases:**

- [ ] Phase 1 — Spec loader accepts reserved `output.animation:` block.
- [ ] Phase 2 — Per-primitive `animate:` reservation.
- [ ] Phase 3 — Tests: `test_animation_reservation.py`.
- [ ] Phase 4 — DAS §12 stub.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.7.1 | Spec loader: reserved `output.animation:` block | **TECH-737** | Draft | `tools/sprite-gen/src/spec.py` — recognise top-level `output.animation:` dict; validate only `enabled: false` passes; raise `SpecError` on `enabled: true` (reserved but not implemented). Sibling keys `frames`, `fps`, `loop`, `phase_offset`, `layers` accepted without interpretation. Consumes L16. |
| T6.7.2 | Per-primitive `animate:` reservation | **TECH-738** | Draft | Composer / primitive dispatch — accepts `animate: none` on any decoration entry; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. Centralised check so every primitive inherits the guard. |
| T6.7.3 | Tests: `test_animation_reservation.py` | **TECH-739** | Draft | `tools/sprite-gen/tests/test_animation_reservation.py` — (a) `enabled: false` block parses cleanly; (b) `enabled: true` raises `SpecError`; (c) primitive with `animate: none` renders; (d) `animate: flicker` raises `NotImplementedError` with "DAS §12" in message. |
| T6.7.4 | DAS §12 stub — "Animation (reserved; not yet implemented)" | **TECH-740** | Draft | `docs/sprite-gen-art-design-system.md` §12 — new stub documents reserved keys (`output.animation.*`, per-primitive `animate:`), enumerates v1 permitted values (`enabled: false`, `animate: none`), and forward-points to future animation milestone. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-737
  title: Spec loader — reserved output.animation block
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — recognise top-level `output.animation:` dict; validate only `enabled: false`; reserved siblings (`frames`, `fps`, `loop`, `phase_offset`, `layers`) accepted without interpretation; `enabled: true` raises `SpecError`.
  depends_on: []
  related:
    - TECH-738
  stub_body:
    summary: |
      Accept `output.animation:` reserved block in spec grammar; `enabled: false` is the only permitted runtime value in v1.
    goals: |
      1. `output.animation:` block parses without breaking v1 composer.
      2. `enabled: false` passes; `enabled: true` raises `SpecError`.
      3. Reserved siblings accepted and preserved in the resolved spec.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: `tests/test_animation_reservation.py` (TECH-739).
    impl_plan_sketch: |
      Phase 1 — Detect `output.animation`; Phase 2 — Validate `enabled`; Phase 3 — Tolerate reserved siblings.
- reserved_id: TECH-738
  title: Per-primitive animate reservation
  priority: medium
  issue_type: TECH
  notes: |
    Composer / primitive dispatch — accepts `animate: none`; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. Single shared guard so every primitive inherits the check.
  depends_on:
    - TECH-737
  related:
    - TECH-739
  stub_body:
    summary: |
      Per-primitive `animate:` key accepts `none`; any other value raises `NotImplementedError` with DAS §12 pointer.
    goals: |
      1. Centralised guard — every primitive inherits without duplication.
      2. `animate: none` is a no-op passthrough.
      3. Any other value raises with actionable DAS pointer.
    systems_map: |
      Composer / primitive dispatch in `tools/sprite-gen/src/compose.py` (or equivalent).
    impl_plan_sketch: |
      Phase 1 — Centralise check in composer dispatch; Phase 2 — Raise on unknown values.
- reserved_id: TECH-739
  title: Tests — test_animation_reservation.py
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_animation_reservation.py` — (a) reserved block parses; (b) `enabled: true` raises `SpecError`; (c) `animate: none` primitive renders; (d) `animate: flicker` raises `NotImplementedError` with "DAS §12" in message.
  depends_on:
    - TECH-737
    - TECH-738
  related: []
  stub_body:
    summary: |
      One test file locking the reservation contract end-to-end.
    goals: |
      1. Four named tests green.
      2. Error paths assert message content (not just type).
      3. `pytest tools/sprite-gen/tests/ -q` green overall.
    systems_map: |
      `tools/sprite-gen/tests/test_animation_reservation.py` (new).
    impl_plan_sketch: |
      Phase 1 — Spec-block parse/raise tests; Phase 2 — Primitive animate no-op + raise tests.
- reserved_id: TECH-740
  title: DAS §12 stub — Animation (reserved; not yet implemented)
  priority: low
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §12 — new stub "Animation (reserved; not yet implemented)" documenting reserved keys + v1 permitted values + forward pointer.
  depends_on:
    - TECH-737
    - TECH-738
  related: []
  stub_body:
    summary: |
      Doc stub reserving DAS §12 for future animation work.
    goals: |
      1. §12 documents `output.animation.*` reserved keys.
      2. §12 documents per-primitive `animate:` with permitted v1 value list.
      3. §12 forward-points to future animation milestone.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §12.
    impl_plan_sketch: |
      Phase 1 — Insert §12 heading; Phase 2 — Reserved-keys table; Phase 3 — v1 permitted values + forward pointer.
```

**Dependency gate:** None. Independent stage; can ship alongside or ahead of Stage 6.6.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.7 tasks **TECH-737**..**TECH-740** aligned with §3 Stage 6.7 block of `/tmp/sprite-gen-improvement-session.md`; lock L16 threaded through spec loader + per-primitive guard + doc stub. Aggregate doc: `docs/implementation/sprite-gen-stage-6.7-plan.md`. Downstream: Stage 9 addendum (`tiled-row-N`) next.

---

### Stage 9 addendum — Parametric `tiled-row-N` / `tiled-column-N`

**Status:** Draft — 2026-04-23. Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 9 addendum (`/tmp/sprite-gen-improvement-session.md`). **Issues closed:** I7. **Filing hint:** amend Stage 9 T9.2 before it becomes an issue — this block stands in until Stage 9 is itself filed with full task YAMLs.

**Objectives:** Upgrade the Stage 9 slot grammar from fixed names (`tiled-row-3`, `tiled-row-4`, `tiled-column-3`) to a parametric form: `tiled-row-N` / `tiled-column-N` for any `N ≥ 2`. `resolve_slot` distributes N buildings evenly across the relevant axis while respecting footprint. Unblocks `MediumResidentialBuilding-2-128.png` (5-house row) as T9.3's visual target, and — cross-stage — is what makes `row_houses_3x` (TECH-734 / Stage 6.6) render cleanly.

**Exit:**

- `tools/sprite-gen/src/slots.py` — `tiled-(row|column)-N` name grammar parsed via regex; `N < 2` or non-int `N` raises `SpecError`.
- `tools/sprite-gen/src/slots.py` — `resolve_slot("tiled-row-N", footprint, idx, count)` distributes `count` buildings evenly across the row axis with integer-pixel anchors; ditto for `tiled-column-N`.
- `tools/sprite-gen/tests/test_parametric_slots.py` — parse valid + invalid names; distribute for N ∈ {2, 3, 4, 5}; anchors equal-spaced + integer-pixel.
- `docs/sprite-gen-art-design-system.md` §5 R11 amended — table row for parametric slot grammar (replaces hard-coded `tiled-row-3/4`).
- `pytest tools/sprite-gen/tests/` exits 0.

**Phases:**

- [ ] Phase 1 — Parser regex + validation.
- [ ] Phase 2 — Even-distribution resolver (row + column axes).
- [ ] Phase 3 — Tests: `test_parametric_slots.py`.
- [ ] Phase 4 — DAS §5 R11 amendment.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.add.1 | Slot name grammar — `tiled-(row\|column)-N` parser | **TECH-741** | Draft | `tools/sprite-gen/src/slots.py` — parse slot name via regex `^tiled-(row\|column)-(\d+)$`; capture axis + `N`; validate `N ≥ 2`; otherwise raise `SpecError` with the offending name. Hard-coded names from T9.2 stay accepted transitionally (alias through parser). |
| T9.add.2 | `resolve_slot` distribute N evenly across axis | **TECH-742** | Draft | `tools/sprite-gen/src/slots.py` — `resolve_slot(name, footprint, idx, count)` returns `(x_px, y_px)` for the `idx`-th of `count` buildings, equal-spaced along the named axis. Integer-pixel anchors (no subpixel). Footprint respected so anchors stay inside the ground diamond. |
| T9.add.3 | Tests — `test_parametric_slots.py` | **TECH-743** | Draft | `tools/sprite-gen/tests/test_parametric_slots.py` — (a) parser accepts `tiled-row-2..5` + `tiled-column-2..5`; (b) `tiled-row-1` raises; (c) `tiled-foo-3` raises; (d) distribute equal-spaced integer-pixel anchors for N ∈ {2,3,4,5}; (e) column axis mirrored. |
| T9.add.4 | DAS §5 R11 amendment — parametric slot grammar | **TECH-744** | Draft | `docs/sprite-gen-art-design-system.md` §5 R11 — replace hard-coded `tiled-row-3/4` entries with a parametric row documenting `tiled-(row\|column)-N` for `N ≥ 2`. Forward-pointer to `row_houses_3x` preset (TECH-734) as a consumer. Capstone — merges last to reflect actual parser. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-741
  title: Slot name grammar — tiled-(row|column)-N parser
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/slots.py` — parse slot name via regex; capture axis + `N`; validate `N ≥ 2`; otherwise raise `SpecError`. Hard-coded legacy names accepted transitionally (alias through parser).
  depends_on: []
  related:
    - TECH-742
  stub_body:
    summary: |
      Parametric slot name parser: `tiled-(row|column)-N` for N ≥ 2.
    goals: |
      1. Regex parse captures axis ∈ {row, column} and `N`.
      2. `N < 2` or non-int raises `SpecError`.
      3. Hard-coded legacy names (`tiled-row-3`, etc.) alias through the parser.
    systems_map: |
      `tools/sprite-gen/src/slots.py`; consumer: `resolve_slot` (TECH-742).
    impl_plan_sketch: |
      Phase 1 — Regex + capture; Phase 2 — N ≥ 2 validation; Phase 3 — Legacy alias.
- reserved_id: TECH-742
  title: resolve_slot distribute N evenly across axis
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/slots.py` — `resolve_slot(name, footprint, idx, count)` returns `(x_px, y_px)` for the `idx`-th of `count` buildings, equal-spaced along the named axis. Integer-pixel anchors; footprint respected.
  depends_on:
    - TECH-741
  related:
    - TECH-743
  stub_body:
    summary: |
      Distribute N buildings evenly along the row or column axis with integer-pixel anchors.
    goals: |
      1. `resolve_slot` accepts `(name, footprint, idx, count)`.
      2. Anchors are equal-spaced integers along the named axis.
      3. Anchors stay inside the ground diamond (footprint-aware).
    systems_map: |
      `tools/sprite-gen/src/slots.py`; consumer: composer building-dispatch + TECH-734 preset.
    impl_plan_sketch: |
      Phase 1 — Axis dispatch; Phase 2 — Equal-space math; Phase 3 — Integer-pixel clamp.
- reserved_id: TECH-743
  title: Tests — test_parametric_slots.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_parametric_slots.py` — parser accept + reject paths; distribute for N ∈ {2,3,4,5}; anchors equal-spaced integer-pixel; column mirrored.
  depends_on:
    - TECH-741
    - TECH-742
  related: []
  stub_body:
    summary: |
      One test file locking parser + distributor end-to-end.
    goals: |
      1. Parser accept/reject asserted.
      2. Distribute correctness for N ∈ {2,3,4,5}.
      3. Column axis mirror of row.
    systems_map: |
      `tools/sprite-gen/tests/test_parametric_slots.py` (new).
    impl_plan_sketch: |
      Phase 1 — Parser tests; Phase 2 — Distribute tests (row); Phase 3 — Column mirror.
- reserved_id: TECH-744
  title: DAS §5 R11 amendment — parametric slot grammar
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §5 R11 — replace hard-coded `tiled-row-3/4` entries with a parametric row documenting `tiled-(row|column)-N` for `N ≥ 2`. Forward-pointer to `row_houses_3x` preset (TECH-734).
  depends_on:
    - TECH-741
    - TECH-742
    - TECH-743
  related:
    - TECH-734
  stub_body:
    summary: |
      Doc capstone — replace hard-coded slot rows with the parametric grammar.
    goals: |
      1. §5 R11 documents `tiled-(row|column)-N` with `N ≥ 2`.
      2. Hard-coded row entries removed or redirected.
      3. Forward pointer to `row_houses_3x` preset (TECH-734).
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §5 R11.
    impl_plan_sketch: |
      Phase 1 — Locate R11; Phase 2 — Replace rows; Phase 3 — Forward pointer.
```

**Dependency gate:** None for the addendum itself. Consumer chain: TECH-734 (`row_houses_3x`, Stage 6.6) depends on TECH-744 — renders cleanly only once the addendum lands. Stage 9 master block T9.2 will fold this grammar when filed proper.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 9 addendum tasks **TECH-741**..**TECH-744** aligned with §3 Stage 9 addendum block of `/tmp/sprite-gen-improvement-session.md`; parametric `tiled-(row|column)-N` grammar threaded through parser + resolver + tests + doc. TECH-744 is the capstone consumed by TECH-734 (Stage 6.6 `row_houses_3x`). Aggregate doc: `docs/implementation/sprite-gen-stage-9-addendum-plan.md`. Downstream: file Stage 7 addendum (cross-tile passthrough).

---

### Stage 7 addendum — Cross-tile passthrough pattern

**Status:** Draft — 2026-04-23. Filed from the 2026-04-23 sprite-gen improvement session §3 Stage 7 addendum (`/tmp/sprite-gen-improvement-session.md`). **Locks consumed:** L17. **Filing hint:** amend Stage 7 decoration authoring guidance — this block stands in until Stage 7 is itself merged proper.

**Objectives:** Document the existing slope-sprite "empty lot / natural-park-walkway passthrough" pattern (where adjacent tiles visually continue through a neighbor-blending bridge) and extend it to flat archetypes via a new `ground.passthrough: true` flag. When true, the composer inhibits `iso_ground_noise` and clamps `hue_jitter` to its narrowest value so the tile reads as a seamless continuation of its neighbors.

**Exit:**

- `tools/sprite-gen/src/spec.py` — accepts `ground.passthrough: bool` (default `false`); validates type.
- `tools/sprite-gen/src/compose.py` (or ground render path) — when `passthrough=true`: skip `iso_ground_noise`, force `hue_jitter ≤ 0.01`, preserve base material colour so neighbor tiles blend.
- `tools/sprite-gen/tests/test_ground_passthrough.py` — flag parses; render skips noise + clamps jitter; byte-difference vs. `passthrough=false` non-zero but bounded.
- `docs/sprite-gen-art-design-system.md` §3 — new subsection documenting the existing slope pattern + the flat-archetype extension with the new flag.
- `pytest tools/sprite-gen/tests/` exits 0.

**Phases:**

- [ ] Phase 1 — Spec schema: `ground.passthrough` flag.
- [ ] Phase 2 — Composer: inhibit noise + clamp hue jitter on passthrough tiles.
- [ ] Phase 3 — Tests: `test_ground_passthrough.py`.
- [ ] Phase 4 — DAS §3 amendment.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.10.1 | Spec schema: `ground.passthrough` flag | **TECH-745** | Draft | `tools/sprite-gen/src/spec.py` — accept `ground.passthrough: bool` sibling of `material`; default `false`; non-bool raises `SpecError`. Consumes L17. |
| T7.10.2 | Composer: inhibit noise + clamp jitter | **TECH-746** | Draft | `tools/sprite-gen/src/compose.py` — ground render path checks `spec.ground.passthrough`; when true: skip `iso_ground_noise` call; force `hue_jitter = min(hue_jitter, 0.01)`; `value_jitter = 0`. Base material colour preserved so neighbor tiles blend. |
| T7.10.3 | Tests — `test_ground_passthrough.py` | **TECH-747** | Draft | `tools/sprite-gen/tests/test_ground_passthrough.py` — (a) flag parses; (b) non-bool raises; (c) `passthrough=true` render skips noise (visual diff vs. baseline); (d) `hue_jitter` clamped even if author sets higher; (e) `passthrough=false` (default) unchanged. |
| T7.10.4 | DAS §3 amendment — passthrough pattern | **TECH-748** | Draft | `docs/sprite-gen-art-design-system.md` §3 — new subsection "Cross-tile passthrough" documenting the existing slope-sprite "empty lot / natural-park-walkway" pattern + the flat-archetype extension via `ground.passthrough: true`. Explains rendering implications (no noise; narrowest jitter). |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-745
  title: Spec schema — ground.passthrough flag
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — accept `ground.passthrough: bool` sibling of `material`; default `false`; non-bool raises `SpecError`.
  depends_on: []
  related:
    - TECH-746
  stub_body:
    summary: |
      Add `ground.passthrough: bool` flag with validation and default.
    goals: |
      1. `ground.passthrough: true|false` parses.
      2. Default value `false` when absent.
      3. Non-bool value raises `SpecError`.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumer: composer ground path (TECH-746).
    impl_plan_sketch: |
      Phase 1 — Type guard on ground block; Phase 2 — Default propagation.
- reserved_id: TECH-746
  title: Composer — inhibit noise + clamp jitter on passthrough tiles
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — ground render path: when `passthrough=true`, skip `iso_ground_noise`; clamp `hue_jitter ≤ 0.01`; `value_jitter = 0`. Base material colour preserved.
  depends_on:
    - TECH-745
  related:
    - TECH-747
  stub_body:
    summary: |
      Passthrough tiles render as neighbor-blending bridges — no noise, narrowest jitter.
    goals: |
      1. `iso_ground_noise` skipped when passthrough=true.
      2. `hue_jitter` clamped to ≤0.01; `value_jitter` forced to 0.
      3. Base material colour preserved so neighbors blend.
    systems_map: |
      `tools/sprite-gen/src/compose.py` ground render path; consumer: `tests/test_ground_passthrough.py` (TECH-747).
    impl_plan_sketch: |
      Phase 1 — Branch on passthrough; Phase 2 — Skip noise call; Phase 3 — Clamp jitter.
- reserved_id: TECH-747
  title: Tests — test_ground_passthrough.py
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_ground_passthrough.py` — (a) flag parses; (b) non-bool raises; (c) passthrough render skips noise (visual diff vs. baseline); (d) `hue_jitter` clamp enforced; (e) `passthrough=false` unchanged.
  depends_on:
    - TECH-745
    - TECH-746
  related: []
  stub_body:
    summary: |
      One test file locking passthrough semantics end-to-end.
    goals: |
      1. Five named tests green.
      2. Visual-diff tests use bounded byte-count difference (not exact pixel).
      3. Default-false path byte-identical to pre-addendum baseline.
    systems_map: |
      `tools/sprite-gen/tests/test_ground_passthrough.py` (new).
    impl_plan_sketch: |
      Phase 1 — Flag parse / raise tests; Phase 2 — Render skip-noise test; Phase 3 — Jitter clamp + default-unchanged tests.
- reserved_id: TECH-748
  title: DAS §3 amendment — cross-tile passthrough pattern
  priority: low
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §3 — new subsection documenting existing slope-sprite passthrough pattern + flat-archetype extension via `ground.passthrough: true`; rendering implications (no noise; narrowest jitter).
  depends_on:
    - TECH-745
    - TECH-746
  related: []
  stub_body:
    summary: |
      Doc amendment — passthrough pattern for slope + flat archetypes.
    goals: |
      1. §3 documents existing slope-sprite passthrough pattern.
      2. §3 documents `ground.passthrough: true` flat-archetype extension.
      3. §3 documents rendering implications (no noise; narrowest jitter).
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §3.
    impl_plan_sketch: |
      Phase 1 — Locate §3 insertion point; Phase 2 — Write slope-pattern doc; Phase 3 — Flat-archetype extension + rendering implications.
```

**Dependency gate:** None. Independent stage addendum; can ship alongside or ahead of Stage 7 proper. Stage 7 master block (when filed) folds this in as authoring guidance on decoration placement.

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 7 addendum tasks **TECH-745**..**TECH-748** aligned with §3 Stage 7 addendum block of `/tmp/sprite-gen-improvement-session.md`; lock L17 threaded through schema flag + composer inhibit/clamp + tests + DAS §3 doc. Aggregate doc: `docs/implementation/sprite-gen-stage-7-addendum-plan.md`. Downstream: handoff exhausted — all 9 stages filed.

---

### Stage 7 — Decoration primitives — vegetation & yard

**Status:** Draft — 2026-04-23.

**Objectives:** Ship the yard-and-vegetation half of the DAS R9 primitive set — the primitives that make residential/suburban sprites feel alive (trees, bushes, grass tufts, pool, path, pavement patch, fence). Wire seed-based placement strategies so YAML specs stay short and deterministic.

**Exit:**

- Seven new primitives under `src/primitives/`: `iso_tree_fir`, `iso_tree_deciduous`, `iso_bush`, `iso_grass_tuft`, `iso_pool`, `iso_path`, `iso_pavement_patch`, `iso_fence`.
- Each primitive: pure function `(canvas, x0, y0, scale=1.0, variant=0, palette, **kwargs)`; writes pixels with its own internal 2–3-level ramp; no outline pass.
- `src/placement.py` — `place(decorations: list, footprint, seed) → list[(primitive, x_px, y_px, kwargs)]`; strategies: `corners`, `perimeter`, `random_border`, `grid`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`.
- `src/compose.py` reads `spec.decorations: list[...]`, calls `placement.place(...)`, dispatches each to its primitive, draws on top of ground + under building (z-order: ground → yard decorations → building → roof decorations).
- Pool primitive hard-gated: composer raises `DecorationScopeError` if `iso_pool` appears on a 1×1 archetype.
- `tests/test_decorations_vegetation.py` — per-primitive smoke (non-empty bbox, expected palette); `tests/test_placement.py` — each strategy places the declared count of items at stable coords given the same seed.
- DAS §5 R9 rows 1–8 implemented.

**Phases:**

- Phase 1 — Tree + bush + grass-tuft primitives.
- Phase 2 — Pool + path + pavement patch + fence.
- Phase 3 — Placement strategies + composer integration.
- Phase 4 — Per-primitive tests + placement regression.

**Tasks:**


| Task | Name                                | Issue     | Status | Intent                                                                                                                                                                                                                                                                             |
| ---- | ----------------------------------- | --------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T7.1 | `iso_tree_fir` primitive            | *pending* | Draft  | 2–3 green domes stacked + dark-green shadow base; scale 0.5–1.5; palette key `tree_fir`. Visual target: `House1-64.png` trees and `Forest1-64.png` dense fill per DAS §3.                                                                                                          |
| T7.2 | `iso_tree_deciduous` primitive      | *pending* | Draft  | Round-crown tree; `color_var ∈ {green, green_yellow, green_blue}`; palette key `tree_deciduous`.                                                                                                                                                                                   |
| T7.3 | `iso_bush` + `iso_grass_tuft`       | *pending* | Draft  | Low green puff (bush ~6×6 px) + single-pixel accents (grass tuft); palette keys `bush`, `grass_tuft`.                                                                                                                                                                              |
| T7.4 | `iso_pool` primitive                | *pending* | Draft  | Light-blue rectangle with white rim; sizes: `w_px/d_px ∈ [8..20]`; palette key `pool`. Composer validates: 2×2+ only.                                                                                                                                                              |
| T7.5 | `iso_path` + `iso_pavement_patch`   | *pending* | Draft  | Beige/grey walkway strip; `axis ∈ {ns, ew}`; path width 2–4 px; pavement patch fills arbitrary rect; palette key `pavement`.                                                                                                                                                       |
| T7.6 | `iso_fence` primitive               | *pending* | Draft  | Thin 1–2 px beige/tan line along one side; `side ∈ {n,s,e,w}`; palette key `fence`.                                                                                                                                                                                                |
| T7.7 | Placement strategies                | *pending* | Draft  | `src/placement.py` — pure function: given decoration list + footprint + seed → list of (primitive_call, x, y, kwargs). Strategies: `corners`, `perimeter`, `random_border`, `grid(rows,cols)`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`. Deterministic per seed. |
| T7.8 | Composer `decorations:` integration | *pending* | Draft  | `compose_sprite` reads `spec.decorations`; calls `placement.place`; draws in z-order ground → yard-deco → building → roof-deco. Raises `DecorationScopeError` on 1×1 + `iso_pool`.                                                                                                 |
| T7.9 | Vegetation + placement tests        | *pending* | Draft  | `tests/test_decorations_vegetation.py` + `tests/test_placement.py`; smoke each primitive; seed-stability test.                                                                                                                                                                     |


**Dependency gate:** Stage 6 archived (need pixel-native primitives + ground diamond + `footprint_ratio` scaling).

---

### Stage 8 — Decoration primitives — building details (windows, doors, roof, signage)

**Status:** Draft — 2026-04-23.

**Objectives:** Ship the on-building half of the DAS R9 primitive set — per-face window grids, doors, chimneys, roof vents, storefront signage, parapet caps. These primitives attach to an existing building face rather than placing in the yard.

**Exit:**

- Seven new primitives: `iso_window_grid`, `iso_door`, `iso_storefront_sign`, `iso_parapet_cap`, `iso_chimney`, `iso_roof_vent`, `iso_pipe_column`.
- Each primitive: pure function + face-anchored draw (face ∈ {top, south, east}); primitives validate `face` compatibility (e.g., `iso_chimney` only on top; `iso_door` only on south/east).
- Spec schema: `building.details: list[...]` processed after `composition:`, drawn in face-order with proper z-clipping.
- Palette keys per DAS §4: `window_blue`, `window_dark`, `door_dark`, `sign_teal`, `sign_cyan`, `parapet_pink`, `parapet_peach`, `chimney_red`, `vent_grey`.
- `tests/test_decorations_building.py` — per-primitive smoke; face-validation tests.

**Phases:**

- Phase 1 — Window grid + door primitives.
- Phase 2 — Storefront sign + parapet cap (commercial-focused).
- Phase 3 — Chimney + roof vent + pipe column.
- Phase 4 — Composer `details:` block + face validation tests.

**Tasks:**


| Task | Name                                       | Issue     | Status | Intent                                                                                                                                                                                       |
| ---- | ------------------------------------------ | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T8.1 | `iso_window_grid` primitive                | *pending* | Draft  | Draw grid of N×M windows on a face; `rows`, `cols`, `face ∈ {south, east}`, `material ∈ {window_blue, window_dark}`. Visual target: `DenseCommercialBuilding-2.png` horizontal band pattern. |
| T8.2 | `iso_door` primitive                       | *pending* | Draft  | Draw dark rectangle at face ground level; `w_px, h_px`, `face ∈ {south, east}`.                                                                                                              |
| T8.3 | `iso_storefront_sign` primitive            | *pending* | Draft  | Facade band across south face; `h_px`, `color` picked from commercial sign palette. Visual target: `Store-1.png` teal signage strip.                                                         |
| T8.4 | `iso_parapet_cap` primitive                | *pending* | Draft  | Top-edge band drawn at the roof seam; `color` from `parapet_pink/peach`. Visual target: `DenseCommercialBuilding-1.png` pink cap.                                                            |
| T8.5 | `iso_chimney` + `iso_roof_vent` primitives | *pending* | Draft  | Vertical rect (chimney) / small box (vent) anchored on top face; `h_px`, `material`.                                                                                                         |
| T8.6 | `iso_pipe_column` primitive                | *pending* | Draft  | Vertical pipe + darker cap on south/east face; `h_px`, `material`. Visual target: `WaterPlant-1-128.png` blue pipe columns.                                                                  |
| T8.7 | Composer `details:` block                  | *pending* | Draft  | `compose_sprite` reads `spec.building.details`; validates face per primitive; draws in correct z-order (walls → window_grid → door → chimney/vent on top).                                   |
| T8.8 | Building-detail tests                      | *pending* | Draft  | `tests/test_decorations_building.py` — smoke each primitive; test face-validation raises on invalid face.                                                                                    |


**Dependency gate:** Stage 6 archived.

---

### Stage 9 — Footprint unlock — 2×2 composites + multi-building clusters

**Status:** Draft — 2026-04-23. First archetype lock-break per L9.

**Objectives:** Break the "v1 all 1×1" lock. Support `footprint: [2, 2]` specs, multi-building clusters within a single tile (e.g., 3-house row, single house + yard, retail strip), and named placement slots.

**Exit:**

- `spec.footprint: [2, 2]` rendered on 128×128 canvas (formula `(2+2)×32 = 128` confirmed).
- `spec.building` becomes `spec.buildings: list[...]` (back-compat: singular `building:` still accepted, rewritten to one-element list internally).
- Each building entry carries `slot: <slot_name>` (or explicit `anchor_px: [x, y]`); slot names: `centered`, `front-left`, `front-right`, `back-left`, `back-right`, `back-center`, `front-center`, `tiled-row-3`, `tiled-row-4`, `tiled-column-3`.
- Composer resolves slots → anchor pixel coords deterministically.
- 3 new archetype specs shipped: `residential_row_medium_2x2.yaml` (3 colored houses tiled-row-3), `residential_suburban_2x2.yaml` (1 centered house + pool + trees), `commercial_light_2x2.yaml` (single larger store + paved surround).
- Regression tests per archetype: render → assert bbox height matches reference (`MediumResidentialBuilding-2-128.png` / `LightResidentialBuilding-2-128.png` / `LightCommercialBuilding-2-128.png`) within ±3 px.

**Phases:**

- Phase 1 — `footprint: [2,2]` canvas math + composer support.
- Phase 2 — `buildings:` list + named slots.
- Phase 3 — 3 reference archetype specs + regression tests.

**Tasks:**


| Task | Name                                | Issue     | Status | Intent                                                                                                                                                                                                         |
| ---- | ----------------------------------- | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T9.1 | `footprint: [2,2]` canvas + compose | *pending* | Draft  | `canvas_size(2, 2)` returns `(128, 0)`; `iso_ground_diamond(2, 2, ...)` renders 128×64 diamond at y0=31; assert pivot = `(0.5, 16/128)`.                                                                       |
| T9.2 | `buildings:` list + slot resolver   | *pending* | Draft  | `src/slots.py` — `resolve_slot(slot_name, footprint, building_bbox) → (x_px, y_px)`; slot table per DAS §5 R11. Back-compat: `spec.building: {...}` lifted to `spec.buildings: [{...}]` with `slot: centered`. |
| T9.3 | `residential_row_medium_2x2.yaml`   | *pending* | Draft  | 3 small houses tiled N→S (`slot: tiled-row-3`), each with random pastel wall color from `{cyan, red, yellow}` per variant. Visual target: `MediumResidentialBuilding-2-128.png`.                               |
| T9.4 | `residential_suburban_2x2.yaml`     | *pending* | Draft  | 1 centered house + front-yard path + pool on back-right + trees on corners. Visual target: `LightResidentialBuilding-2-128.png`.                                                                               |
| T9.5 | `commercial_light_2x2.yaml`         | *pending* | Draft  | 1 centered larger commercial block with glass blue facade + paved perimeter + parapet cap. Visual target: `LightCommercialBuilding-2-128.png`.                                                                 |
| T9.6 | 2×2 regression tests                | *pending* | Draft  | Per-archetype: render → assert bbox matches reference within ±3 px; dominant colors match within HSV ΔE=15.                                                                                                    |


**Dependency gate:** Stages 6 + 7 archived (ground diamond + yard decorations). Stage 8 optional (buildings render without details for basic match).

---

### Stage 10 — Footprint unlock — 3×3 industrial + paved-yard composition

**Status:** Draft — 2026-04-23.

**Objectives:** Extend `footprint: [3, 3]` support (192×192 canvas); add `iso_paved_parking` primitive with painted stripes; ship 2 flagship 3×3 archetypes (`industrial_heavy_3x3`, `powerplant_nuclear_3x3`).

**Exit:**

- `footprint: [3, 3]` rendered on 192×192 canvas.
- `iso_paved_parking` primitive — rectangular paved area with optional painted parking stripes (yellow `#e0f018` or white).
- `industrial_heavy_3x3.yaml` — office + warehouse cluster + paved driveway + parking stripes.
- `powerplant_nuclear_3x3.yaml` — office slab + 3 cooling towers (static, animation deferred) + mustard ground.
- Regression tests vs `HeavyIndustrialBuilding-1-192.png` and the first frame of `power-plant-nuclear-sprite-sheet.png` (bbox ±3 px tolerance).

**Phases:**

- Phase 1 — `footprint: [3,3]` canvas + ground.
- Phase 2 — `iso_paved_parking` primitive.
- Phase 3 — Industrial + power archetypes + regression tests.

**Tasks:**


| Task  | Name                                   | Issue     | Status | Intent                                                                                                                                                           |
| ----- | -------------------------------------- | --------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T10.1 | `footprint: [3,3]` canvas support      | *pending* | Draft  | `iso_ground_diamond(3, 3, 'mustard_industrial')` renders 192×96 diamond; pivot = `(0.5, 16/192)`; composer handles slot resolver for 3×3.                        |
| T10.2 | `iso_paved_parking` primitive          | *pending* | Draft  | Rect pavement fill + 1-px yellow stripes at configurable spacing; palette key `pavement` + `stripe_yellow`.                                                      |
| T10.3 | `industrial_heavy_3x3.yaml`            | *pending* | Draft  | Office + warehouse on back-left / back-right slots, paved parking filling front half, yellow painted stripes. Target: `HeavyIndustrialBuilding-1-192.png`.       |
| T10.4 | `powerplant_nuclear_3x3.yaml`          | *pending* | Draft  | Office slab back-center + 3× `iso_cooling_tower` primitives arranged front, mustard ground plate. Cooling tower primitive stub (static, single frame, no smoke). |
| T10.5 | `iso_cooling_tower` primitive (static) | *pending* | Draft  | Tapered cylinder — trapezoid front face + ellipse top; `h_px`, `material: cooling_tower_grey`. No smoke plume in v1 (animation deferred).                        |
| T10.6 | `iso_smokestack` primitive             | *pending* | Draft  | Thin tall cylinder; `h_px`, `material`. For heavy industrial rooftops.                                                                                           |
| T10.7 | 3×3 regression tests                   | *pending* | Draft  | Per-archetype render + bbox + dominant color match vs references.                                                                                                |


**Dependency gate:** Stage 9 archived (needs 2×2 machinery; 3×3 is a direct extension).

---

### Stage 11 — Vertical unlock — tall canvases (+64 per floor tier)

**Status:** Draft — 2026-04-23.

**Objectives:** Allow canvas height to grow by `+64 px` per extra floor tier (per DAS §2.2). Ship tall-tower archetypes on both 1×1 and 2×2 footprints.

**Exit:**

- `canvas_size(fx, fy, extra_floors=0)` returns `(w, h)` where `h = (fx+fy)*32 + extra_floors*64` — extra_floors ∈ {0,1,2,3}.
- Composer auto-selects `extra_floors` based on `spec.levels × level_h > (fx+fy)*32`.
- Window-band repeat: `iso_window_grid` handles multi-floor automatic replication when `rows` is set high enough.
- Reference specs: `residential_heavy_tall_1x1.yaml` → 64×128, `commercial_dense_tall_1x1.yaml` → 64×128, `commercial_dense_mega_2x2.yaml` → 128×256.
- Pivot UV recomputes correctly: `(0.5, 16/128)`, `(0.5, 16/256)`.

**Phases:**

- Phase 1 — `canvas_size` extra_floors param + composer auto-select.
- Phase 2 — Multi-floor window band replication.
- Phase 3 — Tall-tower archetype specs + regression tests.

**Tasks:**


| Task  | Name                              | Issue     | Status | Intent                                                                                                                                    |
| ----- | --------------------------------- | --------- | ------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| T11.1 | `canvas_size(extra_floors)` param | *pending* | Draft  | Extend canvas math to accept `extra_floors ∈ {0,1,2,3}`; composer auto-picks based on building height vs base canvas.                     |
| T11.2 | Multi-floor window band           | *pending* | Draft  | `iso_window_grid` with `rows ≥ 3` automatically tiles the grid vertically per-floor with `level_h` spacing.                               |
| T11.3 | `residential_heavy_tall_1x1.yaml` | *pending* | Draft  | `levels: 6`, `footprint_ratio: [0.9, 0.9]`, cool-grey facade, cyan window band × 6. Target: `HeavyResidentialBuilding-1-64.png` (64×128). |
| T11.4 | `commercial_dense_tall_1x1.yaml`  | *pending* | Draft  | `levels: 6`, glass blue facade, pink parapet cap. Target: `DenseCommercialBuilding-2.png`.                                                |
| T11.5 | `commercial_dense_mega_2x2.yaml`  | *pending* | Draft  | `footprint: [2,2]`, `levels: 12`, `extra_floors: 3` → 128×256 canvas. Target: `DenseCommercialBuilding-1.png`.                            |
| T11.6 | Tall-canvas regression tests      | *pending* | Draft  | Per-archetype bbox + pivot UV assertion.                                                                                                  |


**Dependency gate:** Stages 6, 8 archived (needs pixel-native + window-grid). Stage 9 archived (for 2×2 tall mega).

---

### Stage 12 — Palette system v2 + outline policy

**Status:** Draft — 2026-04-23.

**Objectives:** Formalize the DAS §4 palette tables. Expand palette JSON schema to sub-objects (`materials`, `ground`, `decorations`). Bootstrap palettes for all six production classes. Implement the 2-concept outline pass (silhouette for small/medium buildings; rim-shade for ground tiles).

**Exit:**

- `palettes/*.json` schema v2: `{materials, ground, decorations}` sub-objects (DAS R10).
- Bootstrap palettes (extracted + hand-named per DAS §4.2): `residential.json`, `commercial.json`, `industrial.json`, `power.json`, `water.json`, `environmental.json`.
- `src/outline.py` — `draw_silhouette(canvas, mask)`: 1-px black outline on exterior edges of a mask; invoked by composer for classes flagged `outline_silhouette: true`.
- Rim-shade handled inside `iso_ground_diamond` (no separate outline pass).
- Per-class outline policy in `src/constants.py`: `OUTLINE_SILHOUETTE = {"residential_small": True, "commercial_small": True, "industrial_light": True, "commercial_dense": False, "residential_heavy": False, ...}`.
- `tests/test_palette_v2.py` — load each palette, assert schema; `tests/test_outline.py` — silhouette pass produces 1-px black ring.

**Phases:**

- Phase 1 — Palette JSON schema v2 + migration of existing `residential.json`.
- Phase 2 — Bootstrap 5 additional class palettes from DAS §4.2.
- Phase 3 — Silhouette outline pass + per-class policy.

**Tasks:**


| Task  | Name                         | Issue     | Status | Intent                                                                                                                                                                     |
| ----- | ---------------------------- | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T12.1 | Palette schema v2            | *pending* | Draft  | Migrate `residential.json` to `{materials, ground, decorations}`; `load_palette` reads v2 schema, falls back to v1 flat for back-compat.                                   |
| T12.2 | Bootstrap class palettes     | *pending* | Draft  | Create `commercial.json`, `industrial.json`, `power.json`, `water.json`, `environmental.json` using values from DAS §4.2.                                                  |
| T12.3 | Silhouette outline primitive | *pending* | Draft  | `src/outline.py` — scan alpha channel, draw 1-px black on exterior edges of building-only mask (exclude ground + decorations). Applied last, before composition to canvas. |
| T12.4 | Per-class outline policy     | *pending* | Draft  | `OUTLINE_SILHOUETTE` constant; composer honors.                                                                                                                            |
| T12.5 | Palette + outline tests      | *pending* | Draft  | `tests/test_palette_v2.py` + `tests/test_outline.py`.                                                                                                                      |


**Dependency gate:** Stage 6 archived.

---

### Stage 13 — Slope refactor — 2-tone cliff + water-facing slopes

**Status:** Draft — 2026-04-23.

**Objectives:** Replace `iso_stepped_foundation` as the default under-building foundation with a cleaner `iso_slope_wedge` primitive (2-tone brown cliff sides per DAS §5 R8). Unlock water-facing slopes (17 variants) per L9. Update `slopes.yaml` to cover both land and water slope sets.

**Exit:**

- `iso_slope_wedge(fx, fy, slope_id, material='earth_brown')` — renders 2-tone brown cliff wedge under a tilted grass top. Reads per-corner Z table from `slopes.yaml`.
- `slopes.yaml` extended with 17 water-facing variants (adds `-water` suffix; renders water strip along low edge using `water_deep` bright color).
- Composer default: when `spec.terrain != 'flat'`, uses `iso_slope_wedge` (not `iso_stepped_foundation`).
- Legacy `iso_stepped_foundation` kept as opt-in (`spec.foundation_primitive: iso_stepped_foundation`) for multi-floor buildings that need stepping.
- Regression tests: all 34 (17 land + 17 water) slope variants render without crash; bbox matches existing `Slopes/*.png` counterparts within ±3 px.

**Phases:**

- Phase 1 — `iso_slope_wedge` primitive + `slopes.yaml` water extension.
- Phase 2 — Composer default swap.
- Phase 3 — 34-variant regression matrix.

**Tasks:**


| Task  | Name                          | Issue     | Status | Intent                                                                                                                                                |
| ----- | ----------------------------- | --------- | ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| T13.1 | `iso_slope_wedge` primitive   | *pending* | Draft  | Renders tilted grass top + 2-tone brown side faces; handles all 17 land slope codes. Palette key `earth_brown` (2-tone, no bright).                   |
| T13.2 | `slopes.yaml` water extension | *pending* | Draft  | Add 17 `*-water` variants; each carries `water_strip_edges` metadata telling the primitive where to paint the water strip.                            |
| T13.3 | Water-strip rendering         | *pending* | Draft  | `iso_slope_wedge` reads `water_strip_edges`, paints water_deep bright color on the low edge.                                                          |
| T13.4 | Composer default swap         | *pending* | Draft  | `compose.py`: when `terrain != 'flat'`, use `iso_slope_wedge` by default; legacy `iso_stepped_foundation` accessible via `spec.foundation_primitive`. |
| T13.5 | 34-variant regression test    | *pending* | Draft  | `tests/test_slopes_matrix.py` — parametrized test across 34 slope codes; render + bbox + dominant color vs hand-drawn reference.                      |


**Dependency gate:** Stage 6 archived.

---

### Stage 14 — Archetype library expansion + slope matrix per archetype

**Status:** Draft — 2026-04-23. **No archetype cap (Lock H3).**

**Objectives:** Ship the v1 archetype catalog (≥17 archetypes, extensible). Every building **and** zoning archetype ships its **full slope matrix** (17 land + 17 water-facing = 34 variants). Slope variants are *auto-derived from the flat spec* via the existing `--terrain <slope_id>` CLI flag (no per-slope YAML authoring).

**Exit:**

- Each archetype: one `specs/<archetype>.yaml` file + a slope-matrix test (`pytest tests/test_archetype_slopes.py::test_<archetype>_matrix`) that iterates over 34 slope codes and asserts no-crash + bbox tolerance vs any matching hand-drawn reference.
- Catalog populated on both `tools/sprite-gen/specs/` and `Assets/Sprites/Generated/` (after promote).
- Initial list (no cap — more archetypes filed opportunistically):


| #   | Archetype                                                        | Footprint | Notes                                     |
| --- | ---------------------------------------------------------------- | --------- | ----------------------------------------- |
| A1  | `residential_small`                                              | 1×1       | Stage 6 calibration target                |
| A2  | `residential_row_medium`                                         | 2×2       | Stage 9 reference                         |
| A3  | `residential_suburban`                                           | 2×2       | Stage 9 reference                         |
| A4  | `residential_heavy_tall`                                         | 1×1 × 128 | Stage 11 reference                        |
| A5  | `commercial_store`                                               | 1×1       | Stage 6/7 extension                       |
| A6  | `commercial_medium`                                              | 1×1       |                                           |
| A7  | `commercial_light`                                               | 2×2       | Stage 9 reference                         |
| A8  | `commercial_dense_tall`                                          | 1×1 × 128 | Stage 11 reference                        |
| A9  | `commercial_dense_mega`                                          | 2×2 × 256 | Stage 11 reference                        |
| A10 | `industrial_light`                                               | 1×1       |                                           |
| A11 | `industrial_medium`                                              | 2×2       |                                           |
| A12 | `industrial_heavy`                                               | 3×3       | Stage 10 reference                        |
| A13 | `powerplant_nuclear`                                             | 3×3       | Stage 10 reference (static, no animation) |
| A14 | `waterplant`                                                     | 2×2       |                                           |
| A15 | `forest_fill`                                                    | 1×1       | Environmental                             |
| A16 | `zoning_grass`                                                   | 1×1       | Empty-lot default                         |
| A17 | `zoning_residential` / `zoning_commercial` / `zoning_industrial` | 1×1 × 3   | Empty-lot per-class                       |


**Phases:**

- Phase 1 — Slope-matrix CLI infrastructure (if not already in Stage 13: batch `--terrain` expansion + filename convention `<archetype>_<slope_code>.png`).
- Phase 2 — Residential archetypes (A1–A4).
- Phase 3 — Commercial archetypes (A5–A9).
- Phase 4 — Industrial + power + water archetypes (A10–A14).
- Phase 5 — Environmental + zoning archetypes (A15–A17).
- Phase 6 — Opportunistic additions (no cap).

**Tasks:** Filed per archetype — task format `T14.<An>.flat` (flat archetype spec) + `T14.<An>.matrix` (34-variant regression test). Full task list filed when each archetype is picked up.

**Dependency gate:** Stages 6–13 archived for full catalog to reach quality bar. Individual flat-archetype tasks (A1, A5, etc.) can ship as each prior stage lands.

---

### Stage 15 — (Deferred) Effects & animation

**Status:** Deferred — separate future exploration per Lock I4.

**Objectives:** Animation descriptors — cooling-tower steam plumes (4-frame), smokestack smoke (loop), bulldozer 5-frame sheet (existing ref), generic 4-frame animation sheets per DAS §1 Effects entries.

Not detailed here; a new exploration doc will scope animation support once Stages 6–14 close.

---

