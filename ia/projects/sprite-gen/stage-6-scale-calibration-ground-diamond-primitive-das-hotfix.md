### Stage 6 — Scale calibration + ground diamond primitive (DAS hotfix)


**Status:** Final — closed 2026-04-23 (8 tasks **TECH-693**..**TECH-700** archived via `0837d3f`). Shipped as a **standalone hotfix PR** ahead of Stages 7–14 (Lock H2). Closes the 3× scale bug so the current `building_residential_small` archetype visually matches `House1-64.png`.

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


#### §Stage File Plan



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

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6 tasks **TECH-693**..**TECH-700** aligned; no fix tuples. Aggregate doc: `docs/implementation/sprite-gen-stage-6-plan.md`. Downstream pipeline continue.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
