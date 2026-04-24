### Stage 9 — Footprint unlock — 2×2 composites + multi-building clusters


**Status:** Draft — 2026-04-23. First archetype lock-break per L9.

**Objectives:** Break the "v1 all 1×1" lock. Support `footprint: [2, 2]` specs, multi-building clusters within a single tile (e.g., 3-house row, single house + yard, retail strip), and named placement slots.

**Exit:**

- `spec.footprint: [2, 2]` rendered on 128×128 canvas (formula `(2+2)×32 = 128` confirmed).
- `spec.building` becomes `spec.buildings: list[...]` (back-compat: singular `building:` still accepted, rewritten to one-element list internally).
- Each building entry carries `slot: <slot_name>` (or explicit `anchor_px: [x, y]`); slot names: `centered`, `front-left`, `front-right`, `back-left`, `back-right`, `back-center`, `front-center`, `tiled-row-3`, `tiled-row-4`, `tiled-column-3`.
- Composer resolves slots → anchor pixel coords deterministically.
- 3 new archetype specs shipped: `residential_row_medium_2x2.yaml` (3 colored houses tiled-row-3), `residential_suburban_2x2.yaml` (1 centered house + pool + trees), `commercial_light_2x2.yaml` (single larger store + paved surround).
- Regression tests per archetype: render → assert bbox height matches reference (`MediumResidentialBuilding-2-128.png` / `LightResidentialBuilding-2-128.png` / `LightCommercialBuilding-2-128.png`) within ±3 px.

**Tasks:**


| Task | Name                                | Issue     | Status | Intent                                                                                                                                                                                                         |
| ---- | ----------------------------------- | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T9.1 | `footprint: [2,2]` canvas + compose | *pending* | pending  | `canvas_size(2, 2)` returns `(128, 0)`; `iso_ground_diamond(2, 2, ...)` renders 128×64 diamond at y0=31; assert pivot = `(0.5, 16/128)`.                                                                       |
| T9.2 | `buildings:` list + slot resolver   | *pending* | pending  | `src/slots.py` — `resolve_slot(slot_name, footprint, building_bbox) → (x_px, y_px)`; slot table per DAS §5 R11. Back-compat: `spec.building: {...}` lifted to `spec.buildings: [{...}]` with `slot: centered`. |
| T9.3 | `residential_row_medium_2x2.yaml`   | *pending* | pending  | 3 small houses tiled N→S (`slot: tiled-row-3`), each with random pastel wall color from `{cyan, red, yellow}` per variant. Visual target: `MediumResidentialBuilding-2-128.png`.                               |
| T9.4 | `residential_suburban_2x2.yaml`     | *pending* | pending  | 1 centered house + front-yard path + pool on back-right + trees on corners. Visual target: `LightResidentialBuilding-2-128.png`.                                                                               |
| T9.5 | `commercial_light_2x2.yaml`         | *pending* | pending  | 1 centered larger commercial block with glass blue facade + paved perimeter + parapet cap. Visual target: `LightCommercialBuilding-2-128.png`.                                                                 |
| T9.6 | 2×2 regression tests                | *pending* | pending  | Per-archetype: render → assert bbox matches reference within ±3 px; dominant colors match within HSV ΔE=15.                                                                                                    |


**Dependency gate:** Stages 6 + 7 archived (ground diamond + yard decorations). Stage 8 optional (buildings render without details for basic match).

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
