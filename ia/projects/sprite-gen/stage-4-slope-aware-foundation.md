### Stage 4 — Geometry MVP / Slope-Aware Foundation


**Status:** Final — 4 tasks archived (**TECH-175**..**TECH-178**). Curation CLI (promote/reject + Unity `.meta`) + Aseprite Tier-2 integration (layered `.aseprite` emit + `promote --edit` round-trip) relocated to Stage 5 on 2026-04-22 so they sequence atomically with the snapshot push hook (promote → push in one pipeline).

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

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
