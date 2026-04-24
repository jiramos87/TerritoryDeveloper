### Stage 7 — Decoration primitives — vegetation & yard


**Status:** Draft — 2026-04-24. Filed (10 tasks T7.1..T7.9b → TECH-762..TECH-771, all Draft status). Ready for `/plan-author`.

**Objectives:** Ship the yard-and-vegetation half of the DAS R9 primitive set — the primitives that make residential/suburban sprites feel alive (trees, bushes, grass tufts, pool, path, pavement patch, fence). Wire seed-based placement strategies so YAML specs stay short and deterministic.

**Exit:**

- Seven new primitives under `src/primitives/`: `iso_tree_fir`, `iso_tree_deciduous`, `iso_bush`, `iso_grass_tuft`, `iso_pool`, `iso_path`, `iso_pavement_patch`, `iso_fence`.
- Each primitive: pure function `(canvas, x0, y0, scale=1.0, variant=0, palette, **kwargs)`; writes pixels with its own internal 2–3-level ramp; no outline pass.
- `src/placement.py` — `place(decorations: list, footprint, seed) → list[(primitive, x_px, y_px, kwargs)]`; strategies: `corners`, `perimeter`, `random_border`, `grid`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`.
- `src/compose.py` reads `spec.decorations: list[...]`, calls `placement.place(...)`, dispatches each to its primitive, draws on top of ground + under building (z-order: ground → yard decorations → building → roof decorations).
- Pool primitive hard-gated: composer raises `DecorationScopeError` if `iso_pool` appears on a 1×1 archetype.
- `tests/test_decorations_vegetation.py` — per-primitive smoke (non-empty bbox, expected palette); `tests/test_placement.py` — each strategy places the declared count of items at stable coords given the same seed.
- DAS §5 R9 rows 1–8 implemented.

**Tasks:**


| Task | Name                                | Issue     | Status | Intent                                                                                                                                                                                                                                                                             |
| ---- | ----------------------------------- | --------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T7.1 | `iso_tree_fir` primitive            | **TECH-762** | Draft  | 2–3 green domes stacked + dark-green shadow base; scale 0.5–1.5; palette key `tree_fir`. Visual target: `House1-64.png` trees and `Forest1-64.png` dense fill per DAS §3.                                                                                                          |
| T7.2 | `iso_tree_deciduous` primitive      | **TECH-763** | Draft  | Round-crown tree; `color_var ∈ {green, green_yellow, green_blue}`; palette key `tree_deciduous`.                                                                                                                                                                                   |
| T7.3 | `iso_bush` + `iso_grass_tuft`       | **TECH-764** | Draft  | Low green puff (bush ~6×6 px) + single-pixel accents (grass tuft); palette keys `bush`, `grass_tuft`.                                                                                                                                                                              |
| T7.4 | `iso_pool` primitive                | **TECH-765** | Draft  | Light-blue rectangle with white rim; sizes: `w_px/d_px ∈ [8..20]`; palette key `pool`. Composer validates: 2×2+ only.                                                                                                                                                              |
| T7.5 | `iso_path` + `iso_pavement_patch`   | **TECH-766** | Draft  | Beige/grey walkway strip; `axis ∈ {ns, ew}`; path width 2–4 px; pavement patch fills arbitrary rect; palette key `pavement`.                                                                                                                                                       |
| T7.6 | `iso_fence` primitive               | **TECH-767** | Draft  | Thin 1–2 px beige/tan line along one side; `side ∈ {n,s,e,w}`; palette key `fence`.                                                                                                                                                                                                |
| T7.7 | Placement strategies                | **TECH-768** | Draft  | `src/placement.py` — pure function: given decoration list + footprint + seed → list of (primitive_call, x, y, kwargs). Strategies: `corners`, `perimeter`, `random_border`, `grid(rows,cols)`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`. Deterministic per seed. |
| T7.8 | Composer `decorations:` integration | **TECH-769** | Draft  | `compose_sprite` reads `spec.decorations`; calls `placement.place`; draws in z-order ground → yard-deco → building → roof-deco. Raises `DecorationScopeError` on 1×1 + `iso_pool`.                                                                                                 |
| T7.9a | Vegetation primitive smoke tests   | **TECH-770** | Draft  | `tests/test_decorations_vegetation.py` — per-primitive smoke (non-empty bbox, expected palette) across all 7 vegetation/yard primitives from T7.1–T7.6.                                                                                                                            |
| T7.9b | Placement seed-stability tests     | **TECH-771** | Draft  | `tests/test_placement.py` — each strategy (`corners`, `perimeter`, `random_border`, `grid`, `centered_front/back`, explicit) places declared count at stable coords given same seed. Regression for composer integration from T7.7–T7.8.                                          |


**Dependency gate:** Stage 6 archived (need pixel-native primitives + ground diamond + `footprint_ratio` scaling).

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
- reserved_id: TECH-762
  title: "`iso_tree_fir` primitive"
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/primitives/iso_tree_fir.py` — pure function `(canvas, x0, y0, scale=1.0, variant=0, palette, **kwargs)`. Draws 2–3 stacked green domes + dark-green shadow base; scale 0.5–1.5; palette key `tree_fir`; internal 2–3-level ramp; no outline pass. Visual target: `House1-64.png` + `Forest1-64.png` dense fill per DAS §3.
  depends_on: []
  related:
    - TECH-763
    - TECH-764
    - TECH-770
  stub_body:
    summary: |
      Ship `iso_tree_fir` primitive — first vegetation primitive of the DAS R9 set. Stacked green domes on dark-green shadow base; scale parameter drives overall footprint; palette key `tree_fir` (3-level ramp).
    goals: |
      1. `iso_tree_fir(canvas, x0, y0, scale, variant, palette)` draws 2–3 stacked green domes on dark shadow base.
      2. `scale ∈ [0.5, 1.5]` controls dome cluster footprint; pixel positions snap to integer coords.
      3. Palette key `tree_fir` resolves 3 ramp levels (bright/mid/dark) from the active palette.
    systems_map: |
      New file `tools/sprite-gen/src/primitives/iso_tree_fir.py`; re-exported from `tools/sprite-gen/src/primitives/__init__.py`. Consumer: composer dispatch (T7.8 / TECH-769); test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770). Visual references: `Assets/Sprites/House1-64.png`, `Assets/Sprites/Forest1-64.png` per DAS §3.
    impl_plan_sketch: |
      Phase 1 — Primitive signature + palette ramp wiring.
      Phase 2 — Dome cluster geometry (2–3 domes, scale-driven layout).
      Phase 3 — Dark-green shadow base + smoke render check under default palette.
- reserved_id: TECH-763
  title: "`iso_tree_deciduous` primitive"
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/primitives/iso_tree_deciduous.py` — round-crown tree; `color_var ∈ {green, green_yellow, green_blue}` selects ramp variant; palette key `tree_deciduous`. Same pure-function signature as `iso_tree_fir`; no outline pass.
  depends_on: []
  related:
    - TECH-762
    - TECH-764
    - TECH-770
  stub_body:
    summary: |
      Ship `iso_tree_deciduous` primitive — round-crown counterpart to the fir. `color_var` kwarg selects one of three ramp variants under palette key `tree_deciduous`.
    goals: |
      1. `iso_tree_deciduous(canvas, x0, y0, scale, variant, palette, color_var=…)` draws round-crown tree.
      2. `color_var ∈ {green, green_yellow, green_blue}` picks three distinct ramps from `tree_deciduous`.
      3. Invalid `color_var` raises `ValueError` with canonical list in message.
    systems_map: |
      New file `tools/sprite-gen/src/primitives/iso_tree_deciduous.py`; re-exported from `primitives/__init__.py`. Palette consumer: `palettes/*.json` entries under key `tree_deciduous`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).
    impl_plan_sketch: |
      Phase 1 — Primitive signature + `color_var` validation.
      Phase 2 — Round-crown ellipse geometry + trunk base.
      Phase 3 — Wire `color_var` to ramp selection under palette key `tree_deciduous`.
- reserved_id: TECH-764
  title: "`iso_bush` + `iso_grass_tuft` primitives"
  priority: medium
  issue_type: TECH
  notes: |
    Two small vegetation primitives colocated: `iso_bush` (low green puff ~6×6 px; palette key `bush`) and `iso_grass_tuft` (1-pixel accents; palette key `grass_tuft`). Both pure functions; no outline pass.
  depends_on: []
  related:
    - TECH-762
    - TECH-763
    - TECH-770
  stub_body:
    summary: |
      Two low-profile vegetation primitives. `iso_bush` = ~6×6 px green puff; `iso_grass_tuft` = single-pixel green accents scattered at the anchor. Both palette-driven, scale-aware.
    goals: |
      1. `iso_bush` renders a ~6×6 px green puff with 2-level internal ramp under palette key `bush`.
      2. `iso_grass_tuft` renders 1–3 single-pixel accents under palette key `grass_tuft`.
      3. Both primitives honour `scale` kwarg and variant seed.
    systems_map: |
      New files `tools/sprite-gen/src/primitives/iso_bush.py` + `iso_grass_tuft.py`; both re-exported from `primitives/__init__.py`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).
    impl_plan_sketch: |
      Phase 1 — `iso_bush` puff geometry + palette wiring.
      Phase 2 — `iso_grass_tuft` pixel-accent drawer.
      Phase 3 — Re-export both; smoke-render under residential palette.
- reserved_id: TECH-765
  title: "`iso_pool` primitive"
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/primitives/iso_pool.py` — light-blue rectangle with white rim; `w_px`/`d_px ∈ [8..20]`; palette key `pool`. Composer gate (T7.8 / TECH-769) rejects `iso_pool` on 1×1 archetype via `DecorationScopeError`.
  depends_on: []
  related:
    - TECH-769
    - TECH-770
  stub_body:
    summary: |
      Ship `iso_pool` primitive — light-blue rectangle with white rim. Size kwargs bounded to 8–20 px. Hard-gated by composer on 1×1 footprints (composer enforcement lives in T7.8 / TECH-769).
    goals: |
      1. `iso_pool(canvas, x0, y0, w_px, d_px, palette)` draws filled rectangle with 1-px white rim.
      2. `w_px, d_px ∈ [8, 20]`; out-of-range raises `ValueError`.
      3. Palette key `pool` resolves light-blue + white-rim colours from active palette.
    systems_map: |
      New file `tools/sprite-gen/src/primitives/iso_pool.py`; re-exported from `primitives/__init__.py`. Composer gate (1×1 rejection) lives in T7.8 / TECH-769 — scope boundary noted here; primitive itself is footprint-agnostic.
    impl_plan_sketch: |
      Phase 1 — Primitive signature + size-range validator.
      Phase 2 — Light-blue rectangle fill + white rim draw.
      Phase 3 — Palette key wiring + smoke render.
- reserved_id: TECH-766
  title: "`iso_path` + `iso_pavement_patch` primitives"
  priority: medium
  issue_type: TECH
  notes: |
    Two yard-surface primitives colocated. `iso_path` — beige/grey walkway strip; `axis ∈ {ns, ew}`; width 2–4 px. `iso_pavement_patch` — fills arbitrary rect. Both under palette key `pavement`; no outline pass.
  depends_on: []
  related:
    - TECH-767
    - TECH-770
  stub_body:
    summary: |
      Two pavement-family primitives. `iso_path` = narrow directional walkway; `iso_pavement_patch` = rectangular surface fill. Shared palette key `pavement`.
    goals: |
      1. `iso_path(canvas, x0, y0, length_px, axis, palette, width_px=2)` draws strip; `axis ∈ {ns, ew}`; `width_px ∈ [2, 4]`.
      2. `iso_pavement_patch(canvas, x0, y0, w_px, d_px, palette)` fills rect with beige/grey under palette key `pavement`.
      3. Invalid axis or width raises `ValueError` with canonical list in message.
    systems_map: |
      New files `tools/sprite-gen/src/primitives/iso_path.py` + `iso_pavement_patch.py`; both re-exported from `primitives/__init__.py`. Shared palette key `pavement` in `palettes/*.json`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).
    impl_plan_sketch: |
      Phase 1 — `iso_path` axis + width validation + strip draw.
      Phase 2 — `iso_pavement_patch` rect-fill draw.
      Phase 3 — Re-export + smoke-render both primitives.
- reserved_id: TECH-767
  title: "`iso_fence` primitive"
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/primitives/iso_fence.py` — thin 1–2 px beige/tan line along one side; `side ∈ {n, s, e, w}`; palette key `fence`. Pure function; no outline pass.
  depends_on: []
  related:
    - TECH-770
  stub_body:
    summary: |
      Ship `iso_fence` primitive — thin 1–2 px line bordering one side of a footprint. Side kwarg selects cardinal direction.
    goals: |
      1. `iso_fence(canvas, x0, y0, length_px, side, palette, thickness_px=1)` draws 1–2 px line along one side.
      2. `side ∈ {n, s, e, w}`; invalid side raises `ValueError`.
      3. Palette key `fence` resolves beige/tan colour from active palette.
    systems_map: |
      New file `tools/sprite-gen/src/primitives/iso_fence.py`; re-exported from `primitives/__init__.py`. Test surface: `tests/test_decorations_vegetation.py` (T7.9a / TECH-770).
    impl_plan_sketch: |
      Phase 1 — Primitive signature + side validator.
      Phase 2 — Per-side line-draw geometry (thickness 1–2 px).
      Phase 3 — Palette key wiring + smoke render.
- reserved_id: TECH-768
  title: Placement strategies
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/placement.py` — pure function `place(decorations, footprint, seed) → list[(primitive_call, x_px, y_px, kwargs)]`. Strategies: `corners`, `perimeter`, `random_border`, `grid(rows, cols)`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`. Deterministic per seed.
  depends_on: []
  related:
    - TECH-769
    - TECH-771
  stub_body:
    summary: |
      Ship `placement.py` — pure decoration placement engine. Given a decoration list + footprint + seed, returns deterministic pixel coords for each primitive call.
    goals: |
      1. `place(decorations, footprint, seed)` returns `list[(primitive_call, x_px, y_px, kwargs)]`.
      2. Seven strategies supported: `corners`, `perimeter`, `random_border`, `grid(rows, cols)`, `centered_front`, `centered_back`, explicit `[x_px, y_px]`.
      3. Output deterministic per seed — same inputs always produce identical coord list.
    systems_map: |
      New file `tools/sprite-gen/src/placement.py`. Consumer: composer `decorations:` dispatch (T7.8 / TECH-769). Test surface: `tests/test_placement.py` (T7.9b / TECH-771).
    impl_plan_sketch: |
      Phase 1 — `place()` signature + per-strategy dispatch skeleton.
      Phase 2 — Deterministic strategies (`corners`, `centered_*`, `grid`, explicit).
      Phase 3 — Seeded-random strategies (`perimeter`, `random_border`) via `random.Random(seed)`.
- reserved_id: TECH-769
  title: Composer `decorations:` integration
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — `compose_sprite` reads `spec.decorations`; calls `placement.place(...)`; draws z-order ground → yard-deco → building → roof-deco. Raises `DecorationScopeError` on 1×1 footprint + `iso_pool`.
  depends_on:
    - TECH-768
  related:
    - TECH-765
    - TECH-771
  stub_body:
    summary: |
      Wire `spec.decorations` into `compose_sprite`. Dispatch each placed primitive in correct z-order. Hard-gate `iso_pool` on 1×1 footprints via `DecorationScopeError`.
    goals: |
      1. `compose_sprite` reads `spec.decorations: list[...]`; iterates `placement.place(...)` output.
      2. Z-order enforced: ground diamond → yard decorations → building → roof decorations.
      3. Footprint-scope gate raises `DecorationScopeError` when a 1×1 spec includes `iso_pool`.
    systems_map: |
      Modify `tools/sprite-gen/src/compose.py` — consume `placement.place` from T7.7 / TECH-768. Dispatch table maps primitive names → primitive callables from `primitives/` (T7.1–T7.6). New exception `DecorationScopeError` in `compose.py` (or `exceptions.py`). Test surface: `tests/test_placement.py` (T7.9b / TECH-771).
    impl_plan_sketch: |
      Phase 1 — Spec field read + `placement.place` call.
      Phase 2 — Z-order dispatch for ground / yard / building / roof layers.
      Phase 3 — `DecorationScopeError` guard for 1×1 + `iso_pool` combo.
- reserved_id: TECH-770
  title: Vegetation primitive smoke tests
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_decorations_vegetation.py` — per-primitive smoke tests across 7 vegetation/yard primitives from T7.1–T7.6 (TECH-762..TECH-767). Assert non-empty bbox + expected palette colours present.
  depends_on:
    - TECH-762
    - TECH-763
    - TECH-764
    - TECH-765
    - TECH-766
    - TECH-767
  related:
    - TECH-771
  stub_body:
    summary: |
      One test file covering smoke render + palette assertions for all 7 yard/vegetation primitives shipped in T7.1–T7.6.
    goals: |
      1. Each of `iso_tree_fir`, `iso_tree_deciduous`, `iso_bush`, `iso_grass_tuft`, `iso_pool`, `iso_path`, `iso_pavement_patch`, `iso_fence` renders without exception under residential palette.
      2. Each render produces non-empty bounding box on the output canvas.
      3. Dominant colour of each render matches expected palette key (bright/mid ramp level).
    systems_map: |
      New file `tools/sprite-gen/tests/test_decorations_vegetation.py`. Depends on primitives shipped by T7.1–T7.6 (TECH-762..TECH-767). Uses default residential palette under `tools/sprite-gen/palettes/residential.json`.
    impl_plan_sketch: |
      Phase 1 — Smoke-render test per primitive (non-empty bbox).
      Phase 2 — Dominant-colour assertion per primitive against expected palette key.
      Phase 3 — Parametrize across the 7-primitive set to keep the file short.
- reserved_id: TECH-771
  title: Placement seed-stability tests
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_placement.py` — each placement strategy places declared count at stable coords given same seed. Covers composer `decorations:` integration from T7.7 + T7.8 (TECH-768 + TECH-769).
  depends_on:
    - TECH-768
    - TECH-769
  related:
    - TECH-770
  stub_body:
    summary: |
      One test file locking placement determinism. For each strategy, assert the declared decoration count is produced at stable coords under a fixed seed.
    goals: |
      1. Each strategy (`corners`, `perimeter`, `random_border`, `grid`, `centered_front`, `centered_back`, explicit) produces expected item count.
      2. Same seed + same inputs → byte-identical coord list across runs.
      3. Composer integration (T7.8) raises `DecorationScopeError` on 1×1 + `iso_pool` regression case.
    systems_map: |
      New file `tools/sprite-gen/tests/test_placement.py`. Depends on `placement.place` from T7.7 / TECH-768 + composer integration from T7.8 / TECH-769.
    impl_plan_sketch: |
      Phase 1 — Per-strategy count + coord-stability tests.
      Phase 2 — Cross-run determinism check (run twice, diff coord lists).
      Phase 3 — `DecorationScopeError` regression on 1×1 + `iso_pool` spec.
```

#### §Plan Fix — PASS (re-entry, no new drift)

> plan-review exit 0 (re-entry pass, cap=1) — Stage 7 tasks TECH-762..TECH-771 aligned. Prior tuple (TECH-770 §1 Summary "7"→"8") applied successfully. Two residual stale "7" occurrences remain in TECH-770 §3 and §7b (non-critical, no goal/acceptance divergence); second-pass cap reached — emitting PASS. Downstream: proceed to `/author`.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
