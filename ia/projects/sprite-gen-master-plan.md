# Isometric Sprite Generator — Master Plan (Tools / Art Pipeline)

> **Status:** In Progress — Stage 7+ (Stage 6 Done 2026-04-23; Stages 6–14 filed as scale-calibration + decoration + footprint-unlock extension, per `docs/sprite-gen-art-design-system.md`)
>
> **Scope:** Build `tools/sprite-gen/` — a Python CLI + N-layer hybrid composer that renders isometric pixel art building sprites from YAML archetype specs, with slope-aware foundations, per-class palette management, a decoration primitive library, multi-footprint support (1×1 / 2×2 / 3×3), tall-canvas growth for multi-floor towers, and a curation workflow that promotes approved PNGs to `Assets/Sprites/Generated/`. Diffusion overlay (Phase 2) and EA bulk render (Phase 3) follow once geometry MVP ships. Non-square footprints (2×1, 3×2, etc.) and animation frames remain out of scope for v1.
>
> **Last updated:** 2026-04-23 (Stages 6–14 appended as the DAS-driven scale-calibration + decoration + footprint-unlock extension. Lock L9 supersedes the earlier "v1 all 1×1" lock; water-facing slopes move in-scope; v1 primitive set expands from 3 to 20).
>
> **Exploration source:**
> - `docs/isometric-sprite-generator-exploration.md` (§2 Locked decisions, §3 Architecture, §5–§9 Primitive/Palette/Slope/YAML/Folder design, §13 Phase plan, §15 Success criteria — ground truth for Stages 1–5).
> - `docs/asset-snapshot-mvp-exploration.md` (§7.5 L6 + L7 + L8, §9.1 Architecture, §9.5.A) — extension source for Stage 5 push hook.
> - `docs/sprite-gen-art-design-system.md` — **canonical DAS** (dimensional math, palette anchors, outline policy, 17-primitive decoration set, archetype YAML schema v2) — ground truth for Stages 6–14.
> - `/tmp/sprite-gen-style-audit.md` — DAS polling transcript and audit raw data (197-sprite catalog inventory + bbox measurements + palette extraction).
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
> - **L6 (2026-04-22):** SOON finish-line = Stage 4 close + Stage 5 push hook. Animation descriptor / EA bulk render / anim-gen / archetype expansion (Steps 2–5 of exploration 5-step spine) stay deferred until MVP triangle closes.
> - **L7 (2026-04-22):** Sprite-gen emits PNG + `.meta` only. Postgres (registry) owns catalog rows; composite objects (panels / buttons / prefabs) are registry-side tables authored post-hoc. No composite sidecar YAML emitted by sprite-gen.
> - **L8 (2026-04-22):** Clean authoring/wiring split — sprite-gen writes catalog rows via HTTP POST `/api/catalog/assets` (never direct SQL, never file bundle). Unity bridge stays read-only from snapshot.
> - **L9 (2026-04-23):** Footprint lock amended — 1×1 + 2×2 + 3×3 all in v1 scope. Non-square footprints (2×1, 3×2, etc.) remain deferred. Water-facing slopes move into v1 (reverses the earlier "water-facing deferred to v2" line). v1 primitive set expands from 3 (iso_cube / iso_prism / iso_stepped_foundation) to 20 (adds `iso_ground_diamond`, `iso_slope_wedge`, plus the 17-primitive decoration set — see DAS R9). Legacy `iso_stepped_foundation` remains available but is no longer the default under-building foundation.
> - **L10 (2026-04-23):** Art calibration ground truth = `docs/sprite-gen-art-design-system.md` (DAS). Every Stage 6+ task cites a DAS section (e.g. "per DAS §4.2") rather than re-specifying rules inline. Audit corpus = all 197 sprites under `Assets/Sprites/` excluding Icons/Buttons/State/Roads. Primary reference: `House1-64.png` for 1×1; `LightResidentialBuilding-2-128.png` for 2×2; `HeavyIndustrialBuilding-1-192.png` for 3×3.
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

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

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

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Slopes YAML table | **TECH-175** | Done | `tools/sprite-gen/slopes.yaml` — per-corner Z table (in pixels) for 17 land slope variants: flat, N, S, E, W, NE, NW, SE, SW, NE-up, NW-up, SE-up, SW-up, NE-bay, NW-bay, NW-bay-2, SE-bay, SW-bay; corner keys: n/e/s/w; values: 0 or 16 (per §7 Slope-aware foundation table); codes must match `Assets/Sprites/Slopes/` filename stems exactly per **Slope variant naming** |
| T4.2 | iso_stepped_foundation | **TECH-176** | Done (archived) | `src/primitives/iso_stepped_foundation.py` — `iso_stepped_foundation(canvas, x0, y0, fx, fy, slope_id, material, palette)`: read `slopes.yaml` per-corner Z for slope_id; build stair/wedge pixel geometry bridging sloped ground plane (variable corners) to flat top at `max(n,e,s,w)+2` lip px; draw using `apply_ramp(material, 'south')` / `apply_ramp(material, 'east')` for visible faces |
| T4.3 | Slope auto-insert | **TECH-177** | Done (archived) | Update `src/compose.py` `compose_sprite`: if `spec['terrain'] != 'flat'`, prepend `iso_stepped_foundation(...)` to primitive stack; recalculate `extra_h = max_corner_z` from slopes.yaml; recompute canvas size + pivot via `canvas_size(fx, fy, extra_h)` + `pivot_uv(canvas_h)`; raise `SlopeKeyError` (exit code 1) if slope_id not in slopes.yaml |
| T4.4 | Slope regression tests | **TECH-178** | Done (archived) | Slope regression test spec `specs/building_residential_small_N.yaml` (copy of small, terrain: N); run `python -m sprite_gen render building_residential_small_N`; assert output PNG height > 64 (canvas grew by max_corner_z=16); assert pivot_uv != (0.5, 0.25); render all 17 slope variants via `--terrain` CLI flag; assert no crash |

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

- [ ] Phase 1 — Curation CLI (promote / reject) + Unity `.meta` writer.
- [ ] Phase 2 — Layered `.aseprite` emission + `promote --edit` round-trip (Tier 2 editor integration).
- [ ] Phase 3 — HTTP client module + config resolution.
- [ ] Phase 4 — Promote integration + `--no-push` CLI flag.
- [ ] Phase 5 — Conflict handling + tests + docs.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Unity meta writer | **TECH-179** | Done (archived) | `src/unity_meta.py` — `write_meta(png_path, canvas_h) → str`: emit Unity `.meta` YAML string with guid (uuid4), textureImporter settings: PPU=64, spritePivot=(0.5, 16/canvas_h), filterMode=Point, textureCompression=None, spriteMode=Single; `src/curate.py` — `promote(src_png, dest_name)`: copy PNG to `Assets/Sprites/Generated/{dest_name}.png`, call `write_meta`, write `.meta` file alongside. _(Relocated from Stage 4 T4.5 on 2026-04-22.)_ |
| T5.2 | Promote/reject CLI | **TECH-180** | Done (archived) | `src/cli.py` — `promote out/X.png --as name` command: call `curate.promote()`; assert dest file exists + `.meta` exists; `reject {archetype}` command: glob `out/{archetype}_*.png`, delete all; integration test: promote then reject the same file, assert `Assets/Sprites/Generated/` has promoted file, `out/` is clean after reject. _(Relocated from Stage 4 T4.6 on 2026-04-22.)_ |
| T5.3 | Aseprite bin resolver | **TECH-181** | Done (archived) | `src/aseprite_bin.py` — `find_aseprite_bin() → Path`: resolve in order `$ASEPRITE_BIN` env var → `tools/sprite-gen/config.toml` `[aseprite] bin` → platform default probes (macOS: `/Applications/Aseprite.app/Contents/MacOS/aseprite`, then `~/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite`); raise `AsepriteBinNotFoundError` on miss (caught by CLI, exit code 4 with install hint); unit test mocks filesystem + env var. _(Relocated from Stage 4 T4.7 on 2026-04-22.)_ |
| T5.4 | Layered aseprite emit | **TECH-182** | Done (archived) | `src/aseprite_io.py` — `write_layered_aseprite(dest_path, layers: dict[str, PIL.Image], canvas_size)`: write `.aseprite` via `py_aseprite` (add to `requirements.txt`) with named layers in stacking order (`foundation`, `east`, `south`, `top`); transparent alpha preserved per layer; update `src/compose.py` to split per-face buffers when `layered=True` flag passed; add `--layered` flag to `cli.py render`; composer always co-emits flat PNG so non-Aseprite users stay unblocked. _(Relocated from Stage 4 T4.8 on 2026-04-22.)_ |
| T5.5 | Promote --edit round-trip | **TECH-183** | Done (archived) | `src/curate.py` — extend `promote(src, dest_name, edit=False)`: if `src.suffix == '.aseprite'` and `edit=True`, shell-out `{aseprite_bin} --batch {src} --save-as {tmp}.png` (subprocess, check returncode), then run existing PNG promote pipeline on `{tmp}.png`; cleanup tmp after; `src/cli.py` — `promote ... --edit` flag; integration test: render --layered → modify one layer pixel via PIL → promote --edit → assert flattened PNG + `.meta` exist in `Assets/Sprites/Generated/`, assert modified pixel present in output. _(Relocated from Stage 4 T4.9 on 2026-04-22.)_ |
| T5.6 | RegistryClient scaffold | **TECH-674** | Done (archived) | `src/registry_client.py` — class `RegistryClient(url: str, timeout: int = 5)` with `create_asset(payload) -> dict`, `patch_asset(id: int, payload: dict, updated_at: str) -> dict`, `get_asset_by_slug(slug: str) -> Optional[dict]`; exception hierarchy `RegistryClientError` → `ConnectionError` / `ConflictError(existing_row)` / `ValidationError(errors)`; add `requests` to `tools/sprite-gen/requirements.txt`. |
| T5.7 | Catalog URL resolver | **TECH-675** | Done (archived) | `src/registry_client.py` — `resolve_catalog_url() -> str`: read env `TG_CATALOG_API_URL` first, `tools/sprite-gen/config.toml` `[catalog] url` second; raise `CatalogConfigError` with hint when neither set and push=True; `--no-push` short-circuits (not called); unit test covers env precedence + config fallback + both-missing. |
| T5.8 | Promote payload + push | **TECH-676** | Done (archived) | Update `src/curate.py` `promote(src, dest_name, edit=False, push=True)` — after `.meta` writes succeed, call `_build_catalog_payload(dest_name, canvas_h, spec_meta) -> dict` (slug, world_sprite_path, ppu=64, pivot, generator_archetype_id, category) + `RegistryClient(resolve_catalog_url()).create_asset(payload)`. Catch `ConflictError` → compare rows → `patch_asset` on drift; noop on match. |
| T5.9 | CLI --no-push flag | **TECH-677** | Done (archived) | `src/cli.py` — extend `promote` command signature with `--no-push` (default false = push); pass through to `curate.promote(..., push=not args.no_push)`; ensure `promote --edit --no-push` skips HTTP once (single push path across flattened + direct PNG variants); `README.md` CLI usage table updated. |
| T5.10 | RegistryClient tests | **TECH-678** | Done (archived) | `tests/test_registry_client.py` — use `responses` fixture; cases: 200 create happy, 409 with matching existing row (skip, no PATCH), 409 with drifted existing row (PATCH issued with `updated_at`), 422 validation (ValidationError raised + CLI exit 1), `ConnectionError` (exit 5); assert no HTTP call made when `push=False`. |
| T5.11 | Promote integration smoke | **TECH-679** | Done (archived) | `tests/test_promote_push.py` — end-to-end: spin up `responses`-mocked catalog server; `render building_residential_small` → `promote out/X.png --as residential-small-01` → assert POST `/api/catalog/assets` issued with expected JSON payload; run `--no-push` variant → assert zero HTTP calls; document exit code 5 handling in `docs/sprite-gen-usage.md`. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

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

- `src/primitives/*` — each primitive accepts `w_px`, `d_px`, `h_px` (pixel-native). Back-compat: `w`, `d`, `h` (tile-unit) accepted and translated to px via `w_px = w * 32` etc.
- `src/primitives/iso_ground_diamond.py` — new primitive; renders full-tile flat diamond with 1-px rim-shade; materials per DAS §4.1 `grass_flat`, `grass_dense`, `pavement`, `water_deep`, `zoning_*`, `mustard_industrial`.
- `src/compose.py` — auto-prepends `iso_ground_diamond(fx, fy, ground)` unless `spec.ground: none`; applies spec-level `footprint_ratio: [wr, dr]` by scaling each composition primitive's `w_px`/`d_px` by the ratio.
- `src/constants.py` (new) — per-class `level_h` table: `{residential_small: 12, commercial_small: 12, residential_heavy: 16, commercial_dense: 16, industrial_*: 16}`.
- `specs/building_residential_small.yaml` — rewritten to the DAS §5 R11 schema with `footprint_ratio: [0.45, 0.45]`, `ground: grass_flat`, `levels: 1`, pixel-native primitives.
- `tests/test_ground_diamond.py` — bbox of rendered flat 1×1 ground diamond = `(0,15)→64×33`; all 8 materials produce non-empty PNGs.
- `tests/test_scale_calibration.py` — render `building_residential_small_v01.png`; assert content bbox height within `35 ± 3 px`, content bbox y0 within `13 ± 3 px` (matches `House1-64.png` signature per DAS §2.3).
- `docs/sprite-gen-usage.md` updated with `footprint_ratio` + `ground` spec fields.

**Phases:**

- [ ] Phase 1 — Pixel-native primitives + back-compat translation.
- [ ] Phase 2 — `iso_ground_diamond` primitive + 8 materials.
- [ ] Phase 3 — Composer auto-prepend + `footprint_ratio` scaling.
- [ ] Phase 4 — `level_h` constants + re-calibrated `building_residential_small` spec + calibration regression test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Pixel-native primitive signatures | **TECH-693** | Done | Extend `iso_cube`, `iso_prism`, `iso_stepped_foundation` (and any other existing primitive) to accept `w_px`, `d_px`, `h_px` kwargs; keep `w,d,h` as deprecated tile-unit aliases (multiplied by 32). Update all internal call sites in `compose.py`. |
| T6.2 | `iso_ground_diamond` primitive + materials | **TECH-694** | Done | New `src/primitives/iso_ground_diamond.py`; draws 64×32 px diamond (or `(fx+fy)×32` × `(fx+fy)×16`) at standard y0=15 offset on a 1×1 canvas; renders 1-px rim-shade via `apply_ramp(material, 'dark')`. Materials: `grass_flat`, `grass_dense`, `pavement`, `water_deep`, `zoning_residential`, `zoning_commercial`, `zoning_industrial`, `mustard_industrial`. |
| T6.3 | Composer ground auto-prepend + `footprint_ratio` | **TECH-695** | Done | Update `compose_sprite`: read `spec.ground` (default per-class from DAS §4.2, fallback `grass_flat`); prepend `iso_ground_diamond` call; read `spec.building.footprint_ratio` (default `[1.0, 1.0]` back-compat); scale each composition primitive `w_px *= footprint_ratio[0]`, `d_px *= footprint_ratio[1]`. Recompute x-offset to center the scaled building on the diamond. |
| T6.4 | `level_h` constants + spec expansion | **TECH-696** | Done | `src/constants.py` — `LEVEL_H = {"residential_small": 12, "commercial_small": 12, ..., "industrial_heavy": 16}`; composer honors `spec.levels` when set (overrides raw `h_px` on stacked cubes). |
| T6.5 | Re-calibrated `building_residential_small` spec | **TECH-697** | Done | Rewrite `specs/building_residential_small.yaml` to DAS §5 R11 schema: `class: residential_small`, `footprint: [1,1]`, `ground: grass_flat`, `levels: 1`, `building.footprint_ratio: [0.45, 0.45]`, pixel-native composition with 10-px-tall wall cube + 8-px-tall roof prism, 4 seeded variants. |
| T6.6 | Ground diamond tests | **TECH-698** | Done | `tests/test_ground_diamond.py` — assert 1×1 `iso_ground_diamond('grass_flat')` produces bbox `(0,15)→64×33`; loop through all 8 materials, assert non-empty bbox + expected dominant color; assert 2×2 variant produces `(0,31)→128×65`. |
| T6.7 | Scale-calibration regression test | **TECH-699** | Done | `tests/test_scale_calibration.py` — render `building_residential_small_v01.png` via compose; assert content bbox height `35 ± 3 px`, y0 `13 ± 3 px`, x0=0, x1=63; assert dominant colors in top 20% pixels match `House1-64.png` dominant colors within HSV ΔE=15. |
| T6.8 | `README` / usage doc update | **TECH-700** | Done | Update `tools/sprite-gen/README.md` + `docs/sprite-gen-usage.md` with new spec fields `ground` + `footprint_ratio` + `levels`; link to DAS sections §2.5 + §4.1. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-693
  title: Pixel-native primitive signatures
  priority: high
  issue_type: TECH
  notes: |
    Extend primitives (`iso_cube`, `iso_prism`, `iso_stepped_foundation`) with `w_px`/`d_px`/`h_px`; keep tile-unit `w`,`d`,`h` as aliases (*32). Touch `tools/sprite-gen/src/primitives/*` + `compose.py` call sites. DAS pixel-native calibration (Stage 6 hotfix).
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

- [ ] Phase 1 — Tree + bush + grass-tuft primitives.
- [ ] Phase 2 — Pool + path + pavement patch + fence.
- [ ] Phase 3 — Placement strategies + composer integration.
- [ ] Phase 4 — Per-primitive tests + placement regression.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | `iso_tree_fir` primitive | _pending_ | Draft | 2–3 green domes stacked + dark-green shadow base; scale 0.5–1.5; palette key `tree_fir`. Visual target: `House1-64.png` trees and `Forest1-64.png` dense fill per DAS §3. |
| T7.2 | `iso_tree_deciduous` primitive | _pending_ | Draft | Round-crown tree; `color_var ∈ {green, green_yellow, green_blue}`; palette key `tree_deciduous`. |
| T7.3 | `iso_bush` + `iso_grass_tuft` | _pending_ | Draft | Low green puff (bush ~6×6 px) + single-pixel accents (grass tuft); palette keys `bush`, `grass_tuft`. |
| T7.4 | `iso_pool` primitive | _pending_ | Draft | Light-blue rectangle with white rim; sizes: `w_px/d_px ∈ [8..20]`; palette key `pool`. Composer validates: 2×2+ only. |
| T7.5 | `iso_path` + `iso_pavement_patch` | _pending_ | Draft | Beige/grey walkway strip; `axis ∈ {ns, ew}`; path width 2–4 px; pavement patch fills arbitrary rect; palette key `pavement`. |
| T7.6 | `iso_fence` primitive | _pending_ | Draft | Thin 1–2 px beige/tan line along one side; `side ∈ {n,s,e,w}`; palette key `fence`. |
| T7.7 | Placement strategies | _pending_ | Draft | `src/placement.py` — pure function: given decoration list + footprint + seed → list of (primitive_call, x, y, kwargs). Strategies: `corners`, `perimeter`, `random_border`, `grid(rows,cols)`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`. Deterministic per seed. |
| T7.8 | Composer `decorations:` integration | _pending_ | Draft | `compose_sprite` reads `spec.decorations`; calls `placement.place`; draws in z-order ground → yard-deco → building → roof-deco. Raises `DecorationScopeError` on 1×1 + `iso_pool`. |
| T7.9 | Vegetation + placement tests | _pending_ | Draft | `tests/test_decorations_vegetation.py` + `tests/test_placement.py`; smoke each primitive; seed-stability test. |

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

- [ ] Phase 1 — Window grid + door primitives.
- [ ] Phase 2 — Storefront sign + parapet cap (commercial-focused).
- [ ] Phase 3 — Chimney + roof vent + pipe column.
- [ ] Phase 4 — Composer `details:` block + face validation tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | `iso_window_grid` primitive | _pending_ | Draft | Draw grid of N×M windows on a face; `rows`, `cols`, `face ∈ {south, east}`, `material ∈ {window_blue, window_dark}`. Visual target: `DenseCommercialBuilding-2.png` horizontal band pattern. |
| T8.2 | `iso_door` primitive | _pending_ | Draft | Draw dark rectangle at face ground level; `w_px, h_px`, `face ∈ {south, east}`. |
| T8.3 | `iso_storefront_sign` primitive | _pending_ | Draft | Facade band across south face; `h_px`, `color` picked from commercial sign palette. Visual target: `Store-1.png` teal signage strip. |
| T8.4 | `iso_parapet_cap` primitive | _pending_ | Draft | Top-edge band drawn at the roof seam; `color` from `parapet_pink/peach`. Visual target: `DenseCommercialBuilding-1.png` pink cap. |
| T8.5 | `iso_chimney` + `iso_roof_vent` primitives | _pending_ | Draft | Vertical rect (chimney) / small box (vent) anchored on top face; `h_px`, `material`. |
| T8.6 | `iso_pipe_column` primitive | _pending_ | Draft | Vertical pipe + darker cap on south/east face; `h_px`, `material`. Visual target: `WaterPlant-1-128.png` blue pipe columns. |
| T8.7 | Composer `details:` block | _pending_ | Draft | `compose_sprite` reads `spec.building.details`; validates face per primitive; draws in correct z-order (walls → window_grid → door → chimney/vent on top). |
| T8.8 | Building-detail tests | _pending_ | Draft | `tests/test_decorations_building.py` — smoke each primitive; test face-validation raises on invalid face. |

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

- [ ] Phase 1 — `footprint: [2,2]` canvas math + composer support.
- [ ] Phase 2 — `buildings:` list + named slots.
- [ ] Phase 3 — 3 reference archetype specs + regression tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | `footprint: [2,2]` canvas + compose | _pending_ | Draft | `canvas_size(2, 2)` returns `(128, 0)`; `iso_ground_diamond(2, 2, ...)` renders 128×64 diamond at y0=31; assert pivot = `(0.5, 16/128)`. |
| T9.2 | `buildings:` list + slot resolver | _pending_ | Draft | `src/slots.py` — `resolve_slot(slot_name, footprint, building_bbox) → (x_px, y_px)`; slot table per DAS §5 R11. Back-compat: `spec.building: {...}` lifted to `spec.buildings: [{...}]` with `slot: centered`. |
| T9.3 | `residential_row_medium_2x2.yaml` | _pending_ | Draft | 3 small houses tiled N→S (`slot: tiled-row-3`), each with random pastel wall color from `{cyan, red, yellow}` per variant. Visual target: `MediumResidentialBuilding-2-128.png`. |
| T9.4 | `residential_suburban_2x2.yaml` | _pending_ | Draft | 1 centered house + front-yard path + pool on back-right + trees on corners. Visual target: `LightResidentialBuilding-2-128.png`. |
| T9.5 | `commercial_light_2x2.yaml` | _pending_ | Draft | 1 centered larger commercial block with glass blue facade + paved perimeter + parapet cap. Visual target: `LightCommercialBuilding-2-128.png`. |
| T9.6 | 2×2 regression tests | _pending_ | Draft | Per-archetype: render → assert bbox matches reference within ±3 px; dominant colors match within HSV ΔE=15. |

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

- [ ] Phase 1 — `footprint: [3,3]` canvas + ground.
- [ ] Phase 2 — `iso_paved_parking` primitive.
- [ ] Phase 3 — Industrial + power archetypes + regression tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | `footprint: [3,3]` canvas support | _pending_ | Draft | `iso_ground_diamond(3, 3, 'mustard_industrial')` renders 192×96 diamond; pivot = `(0.5, 16/192)`; composer handles slot resolver for 3×3. |
| T10.2 | `iso_paved_parking` primitive | _pending_ | Draft | Rect pavement fill + 1-px yellow stripes at configurable spacing; palette key `pavement` + `stripe_yellow`. |
| T10.3 | `industrial_heavy_3x3.yaml` | _pending_ | Draft | Office + warehouse on back-left / back-right slots, paved parking filling front half, yellow painted stripes. Target: `HeavyIndustrialBuilding-1-192.png`. |
| T10.4 | `powerplant_nuclear_3x3.yaml` | _pending_ | Draft | Office slab back-center + 3× `iso_cooling_tower` primitives arranged front, mustard ground plate. Cooling tower primitive stub (static, single frame, no smoke). |
| T10.5 | `iso_cooling_tower` primitive (static) | _pending_ | Draft | Tapered cylinder — trapezoid front face + ellipse top; `h_px`, `material: cooling_tower_grey`. No smoke plume in v1 (animation deferred). |
| T10.6 | `iso_smokestack` primitive | _pending_ | Draft | Thin tall cylinder; `h_px`, `material`. For heavy industrial rooftops. |
| T10.7 | 3×3 regression tests | _pending_ | Draft | Per-archetype render + bbox + dominant color match vs references. |

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

- [ ] Phase 1 — `canvas_size` extra_floors param + composer auto-select.
- [ ] Phase 2 — Multi-floor window band replication.
- [ ] Phase 3 — Tall-tower archetype specs + regression tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | `canvas_size(extra_floors)` param | _pending_ | Draft | Extend canvas math to accept `extra_floors ∈ {0,1,2,3}`; composer auto-picks based on building height vs base canvas. |
| T11.2 | Multi-floor window band | _pending_ | Draft | `iso_window_grid` with `rows ≥ 3` automatically tiles the grid vertically per-floor with `level_h` spacing. |
| T11.3 | `residential_heavy_tall_1x1.yaml` | _pending_ | Draft | `levels: 6`, `footprint_ratio: [0.9, 0.9]`, cool-grey facade, cyan window band × 6. Target: `HeavyResidentialBuilding-1-64.png` (64×128). |
| T11.4 | `commercial_dense_tall_1x1.yaml` | _pending_ | Draft | `levels: 6`, glass blue facade, pink parapet cap. Target: `DenseCommercialBuilding-2.png`. |
| T11.5 | `commercial_dense_mega_2x2.yaml` | _pending_ | Draft | `footprint: [2,2]`, `levels: 12`, `extra_floors: 3` → 128×256 canvas. Target: `DenseCommercialBuilding-1.png`. |
| T11.6 | Tall-canvas regression tests | _pending_ | Draft | Per-archetype bbox + pivot UV assertion. |

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

- [ ] Phase 1 — Palette JSON schema v2 + migration of existing `residential.json`.
- [ ] Phase 2 — Bootstrap 5 additional class palettes from DAS §4.2.
- [ ] Phase 3 — Silhouette outline pass + per-class policy.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | Palette schema v2 | _pending_ | Draft | Migrate `residential.json` to `{materials, ground, decorations}`; `load_palette` reads v2 schema, falls back to v1 flat for back-compat. |
| T12.2 | Bootstrap class palettes | _pending_ | Draft | Create `commercial.json`, `industrial.json`, `power.json`, `water.json`, `environmental.json` using values from DAS §4.2. |
| T12.3 | Silhouette outline primitive | _pending_ | Draft | `src/outline.py` — scan alpha channel, draw 1-px black on exterior edges of building-only mask (exclude ground + decorations). Applied last, before composition to canvas. |
| T12.4 | Per-class outline policy | _pending_ | Draft | `OUTLINE_SILHOUETTE` constant; composer honors. |
| T12.5 | Palette + outline tests | _pending_ | Draft | `tests/test_palette_v2.py` + `tests/test_outline.py`. |

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

- [ ] Phase 1 — `iso_slope_wedge` primitive + `slopes.yaml` water extension.
- [ ] Phase 2 — Composer default swap.
- [ ] Phase 3 — 34-variant regression matrix.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | `iso_slope_wedge` primitive | _pending_ | Draft | Renders tilted grass top + 2-tone brown side faces; handles all 17 land slope codes. Palette key `earth_brown` (2-tone, no bright). |
| T13.2 | `slopes.yaml` water extension | _pending_ | Draft | Add 17 `*-water` variants; each carries `water_strip_edges` metadata telling the primitive where to paint the water strip. |
| T13.3 | Water-strip rendering | _pending_ | Draft | `iso_slope_wedge` reads `water_strip_edges`, paints water_deep bright color on the low edge. |
| T13.4 | Composer default swap | _pending_ | Draft | `compose.py`: when `terrain != 'flat'`, use `iso_slope_wedge` by default; legacy `iso_stepped_foundation` accessible via `spec.foundation_primitive`. |
| T13.5 | 34-variant regression test | _pending_ | Draft | `tests/test_slopes_matrix.py` — parametrized test across 34 slope codes; render + bbox + dominant color vs hand-drawn reference. |

**Dependency gate:** Stage 6 archived.

---

### Stage 14 — Archetype library expansion + slope matrix per archetype

**Status:** Draft — 2026-04-23. **No archetype cap (Lock H3).**

**Objectives:** Ship the v1 archetype catalog (≥17 archetypes, extensible). Every building **and** zoning archetype ships its **full slope matrix** (17 land + 17 water-facing = 34 variants). Slope variants are *auto-derived from the flat spec* via the existing `--terrain <slope_id>` CLI flag (no per-slope YAML authoring).

**Exit:**

- Each archetype: one `specs/<archetype>.yaml` file + a slope-matrix test (`pytest tests/test_archetype_slopes.py::test_<archetype>_matrix`) that iterates over 34 slope codes and asserts no-crash + bbox tolerance vs any matching hand-drawn reference.
- Catalog populated on both `tools/sprite-gen/specs/` and `Assets/Sprites/Generated/` (after promote).
- Initial list (no cap — more archetypes filed opportunistically):

| # | Archetype | Footprint | Notes |
|---|---|---|---|
| A1  | `residential_small` | 1×1 | Stage 6 calibration target |
| A2  | `residential_row_medium` | 2×2 | Stage 9 reference |
| A3  | `residential_suburban` | 2×2 | Stage 9 reference |
| A4  | `residential_heavy_tall` | 1×1 × 128 | Stage 11 reference |
| A5  | `commercial_store` | 1×1 | Stage 6/7 extension |
| A6  | `commercial_medium` | 1×1 | |
| A7  | `commercial_light` | 2×2 | Stage 9 reference |
| A8  | `commercial_dense_tall` | 1×1 × 128 | Stage 11 reference |
| A9  | `commercial_dense_mega` | 2×2 × 256 | Stage 11 reference |
| A10 | `industrial_light` | 1×1 | |
| A11 | `industrial_medium` | 2×2 | |
| A12 | `industrial_heavy` | 3×3 | Stage 10 reference |
| A13 | `powerplant_nuclear` | 3×3 | Stage 10 reference (static, no animation) |
| A14 | `waterplant` | 2×2 | |
| A15 | `forest_fill` | 1×1 | Environmental |
| A16 | `zoning_grass` | 1×1 | Empty-lot default |
| A17 | `zoning_residential` / `zoning_commercial` / `zoning_industrial` | 1×1 × 3 | Empty-lot per-class |

**Phases:**

- [ ] Phase 1 — Slope-matrix CLI infrastructure (if not already in Stage 13: batch `--terrain` expansion + filename convention `<archetype>_<slope_code>.png`).
- [ ] Phase 2 — Residential archetypes (A1–A4).
- [ ] Phase 3 — Commercial archetypes (A5–A9).
- [ ] Phase 4 — Industrial + power + water archetypes (A10–A14).
- [ ] Phase 5 — Environmental + zoning archetypes (A15–A17).
- [ ] Phase 6 — Opportunistic additions (no cap).

**Tasks:** Filed per archetype — task format `T14.<An>.flat` (flat archetype spec) + `T14.<An>.matrix` (34-variant regression test). Full task list filed when each archetype is picked up.

**Dependency gate:** Stages 6–13 archived for full catalog to reach quality bar. Individual flat-archetype tasks (A1, A5, etc.) can ship as each prior stage lands.

---

### Stage 15 — (Deferred) Effects & animation

**Status:** Deferred — separate future exploration per Lock I4.

**Objectives:** Animation descriptors — cooling-tower steam plumes (4-frame), smokestack smoke (loop), bulldozer 5-frame sheet (existing ref), generic 4-frame animation sheets per DAS §1 Effects entries.

Not detailed here; a new exploration doc will scope animation support once Stages 6–14 close.

---
