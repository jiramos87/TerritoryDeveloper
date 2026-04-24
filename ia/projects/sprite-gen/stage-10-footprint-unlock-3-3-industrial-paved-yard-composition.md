### Stage 10 — Footprint unlock — 3×3 industrial + paved-yard composition


**Status:** Draft — 2026-04-23.

**Objectives:** Extend `footprint: [3, 3]` support (192×192 canvas); add `iso_paved_parking` primitive with painted stripes; ship 2 flagship 3×3 archetypes (`industrial_heavy_3x3`, `powerplant_nuclear_3x3`).

**Exit:**

- `footprint: [3, 3]` rendered on 192×192 canvas.
- `iso_paved_parking` primitive — rectangular paved area with optional painted parking stripes (yellow `#e0f018` or white).
- `industrial_heavy_3x3.yaml` — office + warehouse cluster + paved driveway + parking stripes.
- `powerplant_nuclear_3x3.yaml` — office slab + 3 cooling towers (static, animation deferred) + mustard ground.
- Regression tests vs `HeavyIndustrialBuilding-1-192.png` and the first frame of `power-plant-nuclear-sprite-sheet.png` (bbox ±3 px tolerance).

**Tasks:**


| Task  | Name                                   | Issue     | Status | Intent                                                                                                                                                           |
| ----- | -------------------------------------- | --------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T10.1 | `footprint: [3,3]` canvas support      | *pending* | pending  | `iso_ground_diamond(3, 3, 'mustard_industrial')` renders 192×96 diamond; pivot = `(0.5, 16/192)`; composer handles slot resolver for 3×3.                        |
| T10.2 | `iso_paved_parking` primitive          | *pending* | pending  | Rect pavement fill + 1-px yellow stripes at configurable spacing; palette key `pavement` + `stripe_yellow`.                                                      |
| T10.3 | `industrial_heavy_3x3.yaml`            | *pending* | pending  | Office + warehouse on back-left / back-right slots, paved parking filling front half, yellow painted stripes. Target: `HeavyIndustrialBuilding-1-192.png`.       |
| T10.4 | `powerplant_nuclear_3x3.yaml`          | *pending* | pending  | Office slab back-center + 3× `iso_cooling_tower` primitives arranged front, mustard ground plate. Cooling tower primitive stub (static, single frame, no smoke). |
| T10.5 | `iso_cooling_tower` primitive (static) | *pending* | pending  | Tapered cylinder — trapezoid front face + ellipse top; `h_px`, `material: cooling_tower_grey`. No smoke plume in v1 (animation deferred).                        |
| T10.6 | `iso_smokestack` primitive             | *pending* | pending  | Thin tall cylinder; `h_px`, `material`. For heavy industrial rooftops.                                                                                           |
| T10.7 | 3×3 regression tests                   | *pending* | pending  | Per-archetype render + bbox + dominant color match vs references.                                                                                                |


**Dependency gate:** Stage 9 archived (needs 2×2 machinery; 3×3 is a direct extension).

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
