### Stage 14 — Archetype library expansion + slope matrix per archetype


**Status:** Draft — 2026-04-23. **No archetype cap (Lock H3).**

**Objectives:** Ship the v1 archetype catalog (≥17 archetypes, extensible). Every building **and** zoning archetype ships its **full slope matrix** (17 land + 17 water-facing = 34 variants). Slope variants are *auto-derived from the flat spec* via the existing `--terrain <slope_id>` CLI flag (no per-slope YAML authoring).

**Exit:**

- Each archetype: one `specs/<archetype>.yaml` file + a slope-matrix test (`pytest tests/test_archetype_slopes.py::test_<archetype>_matrix`) that iterates over 34 slope codes and asserts no-crash + bbox tolerance vs any matching hand-drawn reference.
- Catalog populated on both `tools/sprite-gen/specs/` and `Assets/Sprites/Generated/` (after promote).
- Initial list (no cap — more archetypes filed opportunistically):


| #   | Archetype                                                        | Footprint | Notes                                     |
| --- | ---------------------------------------------------------------- | --------- | ----------------------------------------- |
| A1  | `residential_small`                                              | 1×1       | Stage 6 calibration target                |
| A2  | `residential_row_medium`                                         | 2×2       | Stage 9 reference                         |
| A3  | `residential_suburban`                                           | 2×2       | Stage 9 reference                         |
| A4  | `residential_heavy_tall`                                         | 1×1 × 128 | Stage 11 reference                        |
| A5  | `commercial_store`                                               | 1×1       | Stage 6/7 extension                       |
| A6  | `commercial_medium`                                              | 1×1       |                                           |
| A7  | `commercial_light`                                               | 2×2       | Stage 9 reference                         |
| A8  | `commercial_dense_tall`                                          | 1×1 × 128 | Stage 11 reference                        |
| A9  | `commercial_dense_mega`                                          | 2×2 × 256 | Stage 11 reference                        |
| A10 | `industrial_light`                                               | 1×1       |                                           |
| A11 | `industrial_medium`                                              | 2×2       |                                           |
| A12 | `industrial_heavy`                                               | 3×3       | Stage 10 reference                        |
| A13 | `powerplant_nuclear`                                             | 3×3       | Stage 10 reference (static, no animation) |
| A14 | `waterplant`                                                     | 2×2       |                                           |
| A15 | `forest_fill`                                                    | 1×1       | Environmental                             |
| A16 | `zoning_grass`                                                   | 1×1       | Empty-lot default                         |
| A17 | `zoning_residential` / `zoning_commercial` / `zoning_industrial` | 1×1 × 3   | Empty-lot per-class                       |


**Tasks:** Filed per archetype — task format `T14.<An>.flat` (flat archetype spec) + `T14.<An>.matrix` (34-variant regression test). Full task list filed when each archetype is picked up.

**Dependency gate:** Stages 6–13 archived for full catalog to reach quality bar. Individual flat-archetype tasks (A1, A5, etc.) can ship as each prior stage lands.

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
