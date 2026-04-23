# sprite-gen — Stage 6 Plan Digest

Compiled 2026-04-23 from 8 task spec(s): **TECH-693** .. **TECH-700**.

**Master plan:** `ia/projects/sprite-gen-master-plan.md` — Stage 6 — Scale calibration + ground diamond primitive (DAS hotfix).

**Closed:** project specs removed on Stage 6 closeout. Issue history: `BACKLOG-ARCHIVE.md` (TECH-693 .. TECH-700); yaml: `ia/backlog-archive/TECH-693.yaml` .. `ia/backlog-archive/TECH-700.yaml`. Digest below is the surviving implementation record.

---

## Stage exit criteria (orchestrator)

- Pixel-native primitive kwargs + tile aliases on `iso_cube` / `iso_prism` / `iso_stepped_foundation`.
- `iso_ground_diamond` + 8 materials; composer prepend + `footprint_ratio` + `LEVEL_H` / `spec.levels`.
- `building_residential_small.yaml` R11 rewrite; `test_ground_diamond.py` + `test_scale_calibration.py`.
- README + `docs/sprite-gen-usage.md` updated for new YAML fields.

---

## §Plan Digest — TECH-693 (excerpt)

### §Goal

Primitives + composer speak **pixel-native** dimensions (DAS §2, R2). Root cause of the 3× scale bug: `iso_cube`/`iso_prism`/`iso_stepped_foundation` accept `w`, `d` in tile units and `h` in pixels. New `_kwargs.normalize_dims` helper resolves `(w|w_px, d|d_px, h|h_px) → (w_px, d_px, h_px)`; `w_px` wins on conflict with a `DeprecationWarning`.

### §Mechanical Steps (summary)

1. Author `tools/sprite-gen/src/primitives/_kwargs.py::normalize_dims` — shared unit resolver.
2. Wire into `iso_cube.py`, `iso_prism.py`, `iso_stepped_foundation.py` at function top.
3. `compose.py` L177-186 kwargs dict — forward `w_px/d_px/h_px/w/d/h` from entry dict. Gate: `pytest` + `npm run validate:all`.

---

## §Plan Digest — TECH-694 (excerpt)

### §Goal

Implement `iso_ground_diamond(fx, fy, material, palette)` per **DAS §2.1, R3, R7, §4.1**. 1×1 bbox = `(0, 15, 64, 48)`; rim-shade is 1 px `apply_ramp(..., "dark")`. Eight DAS §4.1 materials: `grass_flat`, `grass_dense`, `pavement`, `water_deep`, `zoning_residential`, `zoning_commercial`, `zoning_industrial`, `mustard_industrial`.

### §Mechanical Steps (summary)

1. New `tools/sprite-gen/src/primitives/iso_ground_diamond.py` — module-level `MATERIALS` tuple, filled polygon + 1-px rim via `ImageDraw.line`.
2. Register in `primitives/__init__.py` + `compose.py._DISPATCH`. Gate: `pytest test_primitives test_compose`.

---

## §Plan Digest — TECH-695 (excerpt)

### §Goal

Composer auto-prepends `iso_ground_diamond` keyed on `spec.ground` (class default from DAS §4.2, `none` skips) and applies `spec.building.footprint_ratio` to every primitive's `w_px`/`d_px` before dispatch. Centering falls out of the SE-corner anchor math (no geometry change). Sequencing-critical: must merge **after** TECH-693 (shares compose.py kwargs block).

### §Mechanical Steps (summary)

1. `spec.py` — `_DEFAULT_GROUND` + `_DEFAULT_FOOTPRINT_RATIO` dicts + `default_*` resolvers (9 classes).
2. `compose.py` — insert ground prepend + ratio read between `y0 = h_px` and the foundation branch; multiply `w_px`/`d_px` in the per-entry kwargs block.
3. Mirror inside `compose_layers` for Aseprite co-emit parity.

---

## §Plan Digest — TECH-696 (excerpt)

### §Goal

`LEVEL_H` table (DAS §2.4) drives stacked floors. `spec.levels: N` repeats `role: "wall"` composition entries `N` times at `h_px = LEVEL_H[class]` per repeat; a `role: "roof"` entry with `offset_z_role: "above_walls"` gets auto-positioned at `N * LEVEL_H[class]`. Legacy YAML without `role` is untouched (zero regression).

### §Mechanical Steps (summary)

1. New `tools/sprite-gen/src/constants.py` with `LEVEL_H` (10 class keys) + `DEFAULT_LEVEL_H = 12`.
2. `compose.py` — pre-expand composition list based on `role`/`levels` before the dispatch loop.

---

## §Plan Digest — TECH-697 (excerpt)

### §Goal

Replace `tools/sprite-gen/specs/building_residential_small.yaml` with the DAS R11 schema (`class: residential_small`, `footprint: [1,1]`, `ground: grass_flat`, `levels: 1`, `building.footprint_ratio: [0.45, 0.45]`, 28×28 wall cube + 28×28×8 roof prism). Consumes all 4 upstream tasks. Visible bbox target: `64×35, y0=13` — matches House1-64.

### §Mechanical Steps (summary)

1. Palette-key preflight: grep `palettes/residential.json` for `wall_cream`, `roof_red`; fall back to legacy keys if absent.
2. Full YAML rewrite per §Acceptance block.
3. `python -m src render building_residential_small` — verify `v01..v04` bbox = `(0, 13, 64, 48) ± 2 px`.

---

## §Plan Digest — TECH-698 (excerpt)

### §Goal

New `tools/sprite-gen/tests/test_ground_diamond.py` locks DAS §2.1 geometry: 1×1 → `(0,15,64,48)`, 2×2 → `(0,31,128,96)`, 3×3 → `(0,47,192,144)`. Parametrizes all 8 materials for non-empty render; asserts rim is exactly 1 px thick (dark at `(32,15)`, bright at `(32,17)`).

### §Mechanical Steps (summary)

1. Author test with `_render(fx,fy,material)` helper + 5 test functions.
2. Gate: `pytest tools/sprite-gen/tests -q && npm run validate:all`.

---

## §Plan Digest — TECH-699 (excerpt)

### §Goal

New `tools/sprite-gen/tests/test_scale_calibration.py` — the guardrail that catches any future 3× scale regression. Renders `building_residential_small`, compares bbox + top-20% color histogram vs `Assets/Sprites/Residential/House1-64.png`. Tolerances: height 35±3, y0 13±3, x0=0, x1=64, HSV ΔE ≤ 15. Skips locally if reference missing; hard-fails in CI.

### §Mechanical Steps (summary)

1. Author test with `_dominant_rgb_top_band` + `_hsv_delta` helpers (pure stdlib + Pillow).
2. `REPO_ROOT` via `parents[3]` + `.git/` sanity assertion.
3. Reference path: `Assets/Sprites/Residential/House1-64.png` (verified present).

---

## §Plan Digest — TECH-700 (excerpt)

### §Goal

Teach YAML authors the Stage 6 schema (`ground`, `footprint_ratio`, `levels`, `role`, `offset_z_role`) at two touchpoints: `tools/sprite-gen/README.md` quickref table + full example, and `docs/sprite-gen-usage.md` prose walkthrough. Both link DAS §2.5 (ratios) + §4.1 (materials) + R11 (schema) by relative path.

### §Mechanical Steps (summary)

1. README — new "## YAML schema (Stage 6+)" section between Usage and Dependencies.
2. `docs/sprite-gen-usage.md` — append "## Stage 6 fields" prose section.
3. Gate: grep fields in both files + `npm run validate:all`.

---

*Implementers: use per-issue specs as source of truth for full §Mechanical Steps, §Decision Log, §Test Blueprint, MCP hints, and STOP clauses.*
