# Sprite-gen calibration work log

Running log of tuning sessions for the **sprite-gen** composer. Each entry captures a calibration axis, the visual failure mode it addresses, the parameter(s) changed, the value range explored, and the final value accepted by human visual review.

- Process: user invokes `/sprite-gen-review {SPEC_ID}` → agent regenerates variants → `python -m src inspect` runs mechanical bbox + variation check → human-eye verdict is authoritative.
- Scope: calibration-phase only — once the generator is tuned per archetype / preset, review runs retire (see `ia/skills/sprite-gen-visual-review/SKILL.md`).
- Values live in `tools/sprite-gen/src/compose.py` and preset / spec YAML. This doc is the narrative; the code is source of truth.

---

## Axis 1 — Building footprint position (align + padding clamp)

**Date:** 2026-04-23
**Spec:** `tools/sprite-gen/specs/demo_position_padding.yaml` (preset `suburban_house_with_yard`, 4 variants, `footprint_ratio` 0.35..0.55, `padding` 0..10 px, `align` center/sw/se/nw/ne).
**Function:** `resolve_building_box` in `src/compose.py`.

### Problem

Building footprint was drawing 2–3 px outside the tile diamond on variants that combined large `footprint_ratio` (0.55) with max `padding` values + corner `align`. Mechanical bbox-corner check passed because roof pixels dominate the top corners; wall-base overflow on the sides was human-eye-only.

### Iteration trail

| Iter | Mechanism | Value | Result |
|------|-----------|-------|--------|
| 0 | Axis-aligned anchor offsets (pre-iso) | — | v01 / v04 visibly 3–4 px outside tile (diagonal directions wrong) |
| 1 | Iso-space anchor + slack-scaled safety margin | `safety = 0.25` (scaled by `1 - wr`) | v02 / v03 pass; v01 / v04 still ~2 px out |
| 2 | Iso-space anchor + fixed grid-unit inset | `inset = 0.15` | All 4 still 1–3 px out (asymmetric — v01 east, v02 south, v03 south, v04 east) |
| 3 | Same as iter 2 with bumped inset | `inset = 0.28` | **PASS** — all 4 variants inside diamond per user visual |

### Final parameters

- `resolve_building_box` uses fixed grid-unit inset clamp (not slack-scaled):
  - `inset = 0.28` grid-units on each of the 4 footprint sides.
  - `legal_gx_min = wr_eff + inset`, `legal_gx_max = 1.0 - inset` (collapse to center if `wr_eff > 1 - 2·inset`).
  - Same shape for `gy_a` with `dr_eff`.
- Padding-to-grid conversion stays asymmetric: `(pad_w - pad_e) / 64.0` for gx, `(pad_n - pad_s) / 32.0` for gy (matches tile-diamond aspect 2:1).
- Containment is **footprint-only** — rooflines / height pixels rising above the tile top apex are expected for 3D buildings and not penalized.

### Why 0.28 (not lower)

- 0.15 → ~2.4 px iso-diagonal buffer; still short of actual wall-base + outline bleed + iso-projection rounding.
- 0.28 → ~4.5 px iso-diagonal buffer. First value that put all 4 variants inside the diamond per human eye.
- Trade-off: tighter clamp compresses visible padding range (variation `bbox_shift_px` dropped 13 → 5 px between iter 0 and iter 3). Padding still meaningful, just bounded.
- If an archetype needs more spatial range later, lower inset per-spec / per-preset instead of relaxing the global default.

### Supporting helper

- `tools/sprite-gen/src/inspect.py` — footprint-only tile-diamond containment + variation report.
- Exposed as `python -m src inspect <png>...` (single or batch).
- Diamond geometry: `cx = (w-1)/2`, `hw = (w-1)/2`, `cy = h-1-hh`, `hh = (h-1)/4` — east apex at x=63 on a 64-wide canvas.
- Footprint check uses only the bottom two bbox corners (sw, se); top corners (nw, ne) are reported but do not affect the verdict.

### Next calibration candidates

- Roof height / pitch ratio — how far above the tile top apex is acceptable before silhouettes read wrong at zoom.
- Palette stability across variants — variation verdict currently keys on pixel count + bbox shift; may need palette-distance check.
- `footprint_ratio` lower bound — at very small footprints (e.g. 0.25), wall-base may not have enough pixels to resolve cleanly.
- Multi-tile slot grammar (`tiled-row-N`, `tiled-column-N`) — out of scope for the 1×1 skill; will need its own calibration axis.

---

## Axis 2 — Roof pitch + eave height tolerance (residential_small 1×1)

**Date:** 2026-04-24
**Specs:**
- `tools/sprite-gen/specs/demo_roof_pitch_low.yaml` — pitch 0.25, h_px 8
- `tools/sprite-gen/specs/demo_roof_pitch_mid.yaml` — pitch 0.5, h_px 8 (current default)
- `tools/sprite-gen/specs/demo_roof_pitch_high.yaml` — pitch 0.9, h_px 8
- `tools/sprite-gen/specs/demo_roof_h_tall.yaml` — pitch 0.5, h_px 16
**Primitive:** `iso_prism` in `src/primitives/iso_prism.py`; ridge math: `ridge_z = h_px * pitch`.

### Problem

Establish upper / lower bounds on roof pitch (0..1 multiplier) + eave height (`h_px`) before silhouette reads wrong for `residential_small` at 1×1 tile (64×64 canvas) zoom. Containment math (Axis 1) applies only to footprint; roof / ridge pixels crossing tile top apex are expected for 3D buildings — what's not defined is *how far above* apex is still readable as "house" vs "church spire" or "no roof".

### Probe sweep

| Probe | pitch | h_px | Ridge y_min (px) | Above apex (px) | Pixel count |
|-------|-------|------|------------------|-----------------|-------------|
| low   | 0.25  | 8    | 21               | 10.5            | 578         |
| mid   | 0.5   | 8    | 20               | 11.5            | 592         |
| high  | 0.9   | 8    | 17               | 14.5            | 623         |
| tall  | 0.5   | 16   | 16               | 15.5            | 644         |

Tile diamond top apex at y=31.5 on 64-high canvas. All 4 pass footprint containment (Axis 1 rule).

### Verdict

- **All 4 variants acceptable** per user visual review — wide tolerance band.
- No lower bound needed on pitch (0.25 reads as shallow hip, not "no roof").
- No upper bound needed on pitch within tested range (0.9 reads steep but still house-like at 1×1 zoom).
- `h_px = 16` at pitch 0.5 reads as proportional "1.5-story" — acceptable even though `residential_small` nominal level height is 12 (DAS §2.4).

### Final parameters

- **No new clamp in `compose.py` / `iso_prism.py`.** Existing `_PITCH_MIN = 1e-3` min-clamp retained; no max clamp needed.
- Accepted roof parameter bands for `residential_small` 1×1:
  - `composition[roof].pitch`: **0.25..0.95** (full useful range; degenerate below ~0.1).
  - `composition[roof].h_px`: **6..16** (suburban preset currently 8; `vary` band 6..10 is a conservative subset).
- `residential_heavy` class (`LEVEL_H = 16`) likely wants higher `h_px` — separate calibration axis when that class gets probed.

### Sub-finding — `variants.vary.roof.h_px` dead path (FIXED in-session)

While authoring the probe specs, confirmed via `sample_variant` trace that `variants.vary.roof.*` did NOT propagate to `composition[role=roof]` entries. Path `('roof', 'h_px')` was written to `out['roof']['h_px']` by `_set_deep`, but `compose_sprite` reads from the composition list, not from a top-level `roof` key. Effective roof variability = `apply_variant` pitch jitter only (±~10% around base).

**Impact (pre-fix):** preset-level `variants.vary.roof.h_px: {min, max}` in `suburban_house_with_yard.yaml` was inert. `_derive_vary_from_signature` (cli.py L466) emits the same shape. `bootstrap-variants --from-signature` produced specs that looked parameterized but rendered uniformly.

**Fix applied** (`tools/sprite-gen/src/compose.py` `_set_deep`): role-aware routing — when vary path root ∈ `{wall, roof, foundation}` and `target['composition']` contains an entry with matching `role`, `_set_deep` recurses into the composition entry instead of the top-level key. Covers `roof.h_px`, `roof.pitch`, `roof.axis`, `wall.h_px`, `foundation.*`. Functional check: vary range `{6..14}` now produces h_px values 13 / 13 / 10 / 7 across 4 variants (was uniform pre-fix).

**Test state:** `pytest tools/sprite-gen/tests` = 62 pass, 1 pre-existing fail (`test_sw_anchor_shifts_west_and_south`) unrelated to this fix — caused by Axis 1's `inset=0.28` clamp collapsing SW corner anchors to center. Confirmed pre-existing via `git stash` bisect.

### Supporting helpers

- `src/inspect.py` bbox report captures ridge y_min as `building_bbox[1]` — works for roof overhead measurement unchanged from Axis 1.
- `above_apex_px = tile_diamond.cy - tile_diamond.hh - bbox[1]` is the derived metric (not yet exposed by helper — candidate for future `inspect` enhancement).

### Next calibration candidates (refreshed)

- **Axis 3 — Palette variation across variants** (variation verdict currently keys on pixel-count + bbox shift; add palette-distance check).
- **Axis 4 — `footprint_ratio` bounds** — see §Axis 4 below.
- **Axis 5 — `residential_heavy` / `commercial_dense` roof bounds** (LEVEL_H=16 class, taller buildings, different silhouette budget).
- **Axis 6 — Multi-tile slot grammar** (`tiled-row-N`, `tiled-column-N`) — separate multi-tile calibration helper needed.
- **Prerequisite for Axes 3+5:** ~~fix dead `variants.vary.{roof,wall}.*` plumbing~~ — done in this session (see sub-finding above). Axes 3+5 can now use one spec + vary block.

---

## Axis 4 — `footprint_ratio` full bounds (residential_small 1×1)

**Date:** 2026-04-24
**Specs:**
- Lower sweep: `demo_footprint_020.yaml` / `_025.yaml` / `_030.yaml` / `_035.yaml` (ratios 0.20 / 0.25 / 0.30 / 0.35)
- Upper sweep: `demo_footprint_050.yaml` / `_065.yaml` / `_080.yaml` / `_095.yaml` (ratios 0.50 / 0.65 / 0.80 / 0.95)
- All 8 specs identical except `building.footprint_ratio`; pitch 0.5, h_px 8, palette `residential`, seed 11.

**Reader:** `resolve_building_box` + compose-sprite bbox → `src/inspect.py`.

### Problem

Establish bounds on `building.footprint_ratio` (wr, dr) before silhouette fails to read as a building at 1×1 tile zoom (64×64 canvas). Axis 1 footprint containment is mechanical; Axis 4 probes whether the *semantic read* ("house" / "shed" / "industrial") holds across the full 0..1 range.

### Probe sweep

| Probe | ratio | bbox (w×h px) | Pixels | sw / se norm | Containment | Reads as |
|-------|-------|---------------|--------|--------------|-------------|----------|
| 020 | 0.20 | 13×22 | 223 | 0.19 / 0.22 | pass | tiny blob / marker |
| 025 | 0.25 | 17×23 | 296 | 0.25 / 0.29 | pass | shed / outhouse |
| 030 | 0.30 | 19×24 | 340 | 0.29 / 0.32 | pass | small house |
| 035 | 0.35 | 23×26 | 434 | 0.35 / 0.38 | pass | house (near suburban baseline) |
| 050 | 0.50 | 33×29 | 705 | 0.51 / 0.54 | pass | house (clean silhouette) |
| 065 | 0.65 | 41×33 | 955 | 0.64 / 0.67 | pass | large house, ground NE/SW still visible |
| 080 | 0.80 | 51×38 | 1311 | 0.79 / 0.83 | pass | dominates tile, thin ground sliver |
| 095 | 0.95 | 61×43 | 1731 | 0.95 / 0.98 | pass | fills tile, industrial block |

sw/se norms stay < 1.0 across all 8 probes — Axis 1 containment holds even at 0.95 because `resolve_building_box` collapses to center anchor above `wr > 1 - 2·inset` (= 0.44 with `inset = 0.28`), and the diamond geometry at 1×1 accommodates bbox up to ~0.95 centered.

### Verdict

- **All 8 variants acceptable** per user visual review. Full range 0.20..0.95 is usable.
- **Semantic banding** (emergent, not enforced):
  - 0.20..0.28 → marker / shed / outhouse (too small for `residential_small` but valid for accessory / decor sprites)
  - 0.30..0.55 → `residential_small` house silhouette (suburban preset territory)
  - 0.55..0.75 → large residential / mixed use
  - 0.75..0.95 → **industrial / commercial / warehouse** — user confirmed ("bigger ones will serve as industrial for sure")
- This means `footprint_ratio` doubles as a *class discriminator*, not just a size parameter. Class-to-ratio mapping should be captured in `default_footprint_ratio_for_class` (already exists — verify bands match above).

### Final parameters

- **No new clamp** on `footprint_ratio` in `compose.py`. Full 0.01..0.99 useful range retained (lower bound = `max(0.01, ...)` in `wr_eff` clamp at L596).
- Accepted ratio bands for single-tile archetypes:
  - `residential_small`: **0.30..0.55** (spec `suburban_house_with_yard` already in this range)
  - `industrial_small` / `commercial_dense` candidates: **0.70..0.95** (new presets TBD — currently no archetype tests this band)
- Recommendation: add archetype-specific `footprint_ratio` defaults so presets don't have to re-specify.

### Sub-finding — `variants.vary.footprint_ratio.*` dead path (second dead plumb)

While authoring Axis 4 probes, confirmed `variants.vary.footprint_ratio.{w,d} = {min, max}` in specs routes through `_walk_vary` → `_set_deep` → `target['footprint_ratio']['{w,d}']` **on the top-level spec dict**. But `compose_sprite` reads `building.footprint_ratio` as a 2-element *list* `[wr, dr]` (compose.py L403, L585), never as a dict. So:

1. Vary path writes `out['footprint_ratio']['d'] = 0.4` — lands on unused top-level dict key.
2. `compose_sprite` still reads `out['building']['footprint_ratio']` → original spec value.
3. `_derive_vary_from_signature` (cli.py L492) emits `vary['footprint_ratio']['d'] = {min, max}` — also dead.

Same shape as the roof/wall dead-plumb fixed earlier in-session, but requires *list-index* routing (`.w` → index 0, `.d` → index 1) into `building.footprint_ratio`, not dict-field routing. Not fixed in this session — Axis 4 used 8 direct specs instead.

**Fix candidate:** extend `_set_deep` with a second special-case: when `path[0] == 'footprint_ratio'` and `path[1] in {'w', 'd'}`, route into `target['building']['footprint_ratio']` as list-indexed write (w → idx 0, d → idx 1). Auto-promote list if missing.

**Impact if fixed:** Axis 4 sweep collapses from 8 specs to 1 spec + vary block — material token cost reduction for future lower-bound / upper-bound probes on similar list-typed fields.

### Supporting helpers

- `src/inspect.py` — bbox + containment verdict. Works unchanged.
- `open /abs/path/demo_footprint_*.png` — macOS Preview gallery. IDE-hidden (gitignored); use absolute paths.

### Next calibration candidates (refreshed)

- **Axis 3 — Palette variation across variants** (variation verdict currently keys on pixel-count + bbox shift; add palette-distance check to `inspect`).
- **Axis 5 — `residential_heavy` / `commercial_dense` / industrial bounds** (LEVEL_H=16 class, taller buildings, different silhouette budget — now with Axis 4 upper-ratio evidence that 0.75..0.95 reads as industrial).
- **Axis 6 — Multi-tile slot grammar** (`tiled-row-N`, `tiled-column-N`) — separate multi-tile calibration helper needed.
- **Follow-up fix:** plumb `variants.vary.footprint_ratio.{w,d}` into `building.footprint_ratio` list (companion to the roof/wall/foundation plumbing fix).
