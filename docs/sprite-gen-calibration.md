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
