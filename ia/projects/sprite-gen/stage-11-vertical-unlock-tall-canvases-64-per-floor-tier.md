### Stage 11 — Vertical unlock — tall canvases (+64 per floor tier)


**Status:** Draft — 2026-04-23.

**Objectives:** Allow canvas height to grow by `+64 px` per extra floor tier (per DAS §2.2). Ship tall-tower archetypes on both 1×1 and 2×2 footprints.

**Exit:**

- `canvas_size(fx, fy, extra_floors=0)` returns `(w, h)` where `h = (fx+fy)*32 + extra_floors*64` — extra_floors ∈ {0,1,2,3}.
- Composer auto-selects `extra_floors` based on `spec.levels × level_h > (fx+fy)*32`.
- Window-band repeat: `iso_window_grid` handles multi-floor automatic replication when `rows` is set high enough.
- Reference specs: `residential_heavy_tall_1x1.yaml` → 64×128, `commercial_dense_tall_1x1.yaml` → 64×128, `commercial_dense_mega_2x2.yaml` → 128×256.
- Pivot UV recomputes correctly: `(0.5, 16/128)`, `(0.5, 16/256)`.

**Tasks:**


| Task  | Name                              | Issue     | Status | Intent                                                                                                                                    |
| ----- | --------------------------------- | --------- | ------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| T11.1 | `canvas_size(extra_floors)` param | *pending* | pending  | Extend canvas math to accept `extra_floors ∈ {0,1,2,3}`; composer auto-picks based on building height vs base canvas.                     |
| T11.2 | Multi-floor window band           | *pending* | pending  | `iso_window_grid` with `rows ≥ 3` automatically tiles the grid vertically per-floor with `level_h` spacing.                               |
| T11.3 | `residential_heavy_tall_1x1.yaml` | *pending* | pending  | `levels: 6`, `footprint_ratio: [0.9, 0.9]`, cool-grey facade, cyan window band × 6. Target: `HeavyResidentialBuilding-1-64.png` (64×128). |
| T11.4 | `commercial_dense_tall_1x1.yaml`  | *pending* | pending  | `levels: 6`, glass blue facade, pink parapet cap. Target: `DenseCommercialBuilding-2.png`.                                                |
| T11.5 | `commercial_dense_mega_2x2.yaml`  | *pending* | pending  | `footprint: [2,2]`, `levels: 12`, `extra_floors: 3` → 128×256 canvas. Target: `DenseCommercialBuilding-1.png`.                            |
| T11.6 | Tall-canvas regression tests      | *pending* | pending  | Per-archetype bbox + pivot UV assertion.                                                                                                  |


**Dependency gate:** Stages 6, 8 archived (needs pixel-native + window-grid). Stage 9 archived (for 2×2 tall mega).

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
