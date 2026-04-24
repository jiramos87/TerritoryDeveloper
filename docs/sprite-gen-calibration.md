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

### Sub-finding — `variants.vary.roof.h_px` dead path (BUG)

While authoring the probe specs, confirmed via `sample_variant` trace that `variants.vary.roof.*` does NOT propagate to `composition[role=roof]` entries. Path `('roof', 'h_px')` is written to `out['roof']['h_px']` by `_set_deep`, but `compose_sprite` reads from the composition list, not from a top-level `roof` key. Effective roof variability today = `apply_variant` pitch jitter only (±~10% around base).

**Impact:** preset-level `variants.vary.roof.h_px: {min, max}` in `suburban_house_with_yard.yaml` is inert. `_derive_vary_from_signature` (cli.py L466) emits the same dead shape. `bootstrap-variants --from-signature` currently produces specs that look parameterized but render uniformly.

**Fix candidate:** in `_set_deep` (or a new `_apply_vary_composition` helper), when a vary path root is `roof` / `wall` / composition-role name, route writes into the matching `composition[*]` entry instead of top-level. Covers `roof.h_px`, `roof.pitch`, `roof.axis`, future `wall.h_px`.

**Next step:** open BUG issue via `/project-new` — "sprite-gen: `variants.vary.roof.*` / `wall.*` paths don't plumb to composition entries (dead vary path)".

### Supporting helpers

- `src/inspect.py` bbox report captures ridge y_min as `building_bbox[1]` — works for roof overhead measurement unchanged from Axis 1.
- `above_apex_px = tile_diamond.cy - tile_diamond.hh - bbox[1]` is the derived metric (not yet exposed by helper — candidate for future `inspect` enhancement).

### Next calibration candidates (refreshed)

- **Axis 3 — Palette variation across variants** (variation verdict currently keys on pixel-count + bbox shift; add palette-distance check).
- **Axis 4 — `footprint_ratio` lower bound** (at 0.25..0.30, wall-base pixel resolution).
- **Axis 5 — `residential_heavy` / `commercial_dense` roof bounds** (LEVEL_H=16 class, taller buildings, different silhouette budget).
- **Axis 6 — Multi-tile slot grammar** (`tiled-row-N`, `tiled-column-N`) — separate multi-tile calibration helper needed.
- **Prerequisite for Axes 3+5:** fix dead `variants.vary.{roof,wall}.*` plumbing so sweeps can use one spec + vary block instead of N specs.
