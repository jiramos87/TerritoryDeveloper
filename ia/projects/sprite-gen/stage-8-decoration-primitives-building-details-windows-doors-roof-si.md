### Stage 8 — Decoration primitives — building details (windows, doors, roof, signage)


**Status:** Draft — 2026-04-23.

**Objectives:** Ship the on-building half of the DAS R9 primitive set — per-face window grids, doors, chimneys, roof vents, storefront signage, parapet caps. These primitives attach to an existing building face rather than placing in the yard.

**Exit:**

- Seven new primitives: `iso_window_grid`, `iso_door`, `iso_storefront_sign`, `iso_parapet_cap`, `iso_chimney`, `iso_roof_vent`, `iso_pipe_column`.
- Each primitive: pure function + face-anchored draw (face ∈ {top, south, east}); primitives validate `face` compatibility (e.g., `iso_chimney` only on top; `iso_door` only on south/east).
- Spec schema: `building.details: list[...]` processed after `composition:`, drawn in face-order with proper z-clipping.
- Palette keys per DAS §4: `window_blue`, `window_dark`, `door_dark`, `sign_teal`, `sign_cyan`, `parapet_pink`, `parapet_peach`, `chimney_red`, `vent_grey`.
- `tests/test_decorations_building.py` — per-primitive smoke; face-validation tests.

**Tasks:**


| Task | Name                                       | Issue     | Status | Intent                                                                                                                                                                                       |
| ---- | ------------------------------------------ | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T8.1 | `iso_window_grid` primitive                | *pending* | pending  | Draw grid of N×M windows on a face; `rows`, `cols`, `face ∈ {south, east}`, `material ∈ {window_blue, window_dark}`. Visual target: `DenseCommercialBuilding-2.png` horizontal band pattern. |
| T8.2 | `iso_door` primitive                       | *pending* | pending  | Draw dark rectangle at face ground level; `w_px, h_px`, `face ∈ {south, east}`.                                                                                                              |
| T8.3 | `iso_storefront_sign` primitive            | *pending* | pending  | Facade band across south face; `h_px`, `color` picked from commercial sign palette. Visual target: `Store-1.png` teal signage strip.                                                         |
| T8.4 | `iso_parapet_cap` primitive                | *pending* | pending  | Top-edge band drawn at the roof seam; `color` from `parapet_pink/peach`. Visual target: `DenseCommercialBuilding-1.png` pink cap.                                                            |
| T8.5 | `iso_chimney` + `iso_roof_vent` primitives | *pending* | pending  | Vertical rect (chimney) / small box (vent) anchored on top face; `h_px`, `material`.                                                                                                         |
| T8.6 | `iso_pipe_column` primitive                | *pending* | pending  | Vertical pipe + darker cap on south/east face; `h_px`, `material`. Visual target: `WaterPlant-1-128.png` blue pipe columns.                                                                  |
| T8.7 | Composer `details:` block                  | *pending* | pending  | `compose_sprite` reads `spec.building.details`; validates face per primitive; draws in correct z-order (walls → window_grid → door → chimney/vent on top).                                   |
| T8.8 | Building-detail tests                      | *pending* | pending  | `tests/test_decorations_building.py` — smoke each primitive; test face-validation raises on invalid face.                                                                                    |


**Dependency gate:** Stage 6 archived.

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
