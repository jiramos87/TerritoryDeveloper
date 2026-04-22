# Isometric Sprite Generator — Master Plan (Tools / Art Pipeline)

> **Status:** In Progress — Stage 1.3 (Stages 1.1–1.2 Final — 12 tasks archived; Stage 1.3 filed 2026-04-15 — 6 tasks **TECH-153..158**)
>
> **Scope:** Build `tools/sprite-gen/` — a Python CLI + 5-layer hybrid composer that renders isometric pixel art building sprites from YAML archetype specs, with slope-aware foundations, per-class palette management, and a curation workflow that promotes approved PNGs to `Assets/Sprites/Generated/`. Diffusion overlay (Phase 2) and EA bulk render (Phase 3) follow once geometry MVP ships. Non-square footprints, animation frames, water-facing slopes, and additional primitives are out of scope for v1.
>
> **Last updated:** 2026-04-22 (Stage 5 appended from `docs/asset-snapshot-mvp-exploration.md` — registry push hook).
>
> **Exploration source:**
> - `docs/isometric-sprite-generator-exploration.md` (§2 Locked decisions, §3 Architecture, §5–§9 Primitive/Palette/Slope/YAML/Folder design, §13 Phase plan, §15 Success criteria — all are ground truth).
> - `docs/asset-snapshot-mvp-exploration.md` (§7.5 L6 + L7 + L8, §9.1 Architecture, §9.5.A) — extension source for Stage 5 push hook.
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
