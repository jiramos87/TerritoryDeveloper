### Stage 12 — Palette system v2 + outline policy


**Status:** Draft — 2026-04-23.

**Objectives:** Formalize the DAS §4 palette tables. Expand palette JSON schema to sub-objects (`materials`, `ground`, `decorations`). Bootstrap palettes for all six production classes. Implement the 2-concept outline pass (silhouette for small/medium buildings; rim-shade for ground tiles).

**Exit:**

- `palettes/*.json` schema v2: `{materials, ground, decorations}` sub-objects (DAS R10).
- Bootstrap palettes (extracted + hand-named per DAS §4.2): `residential.json`, `commercial.json`, `industrial.json`, `power.json`, `water.json`, `environmental.json`.
- `src/outline.py` — `draw_silhouette(canvas, mask)`: 1-px black outline on exterior edges of a mask; invoked by composer for classes flagged `outline_silhouette: true`.
- Rim-shade handled inside `iso_ground_diamond` (no separate outline pass).
- Per-class outline policy in `src/constants.py`: `OUTLINE_SILHOUETTE = {"residential_small": True, "commercial_small": True, "industrial_light": True, "commercial_dense": False, "residential_heavy": False, ...}`.
- `tests/test_palette_v2.py` — load each palette, assert schema; `tests/test_outline.py` — silhouette pass produces 1-px black ring.

**Tasks:**


| Task  | Name                         | Issue     | Status | Intent                                                                                                                                                                     |
| ----- | ---------------------------- | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T12.1 | Palette schema v2            | *pending* | pending  | Migrate `residential.json` to `{materials, ground, decorations}`; `load_palette` reads v2 schema, falls back to v1 flat for back-compat.                                   |
| T12.2 | Bootstrap class palettes     | *pending* | pending  | Create `commercial.json`, `industrial.json`, `power.json`, `water.json`, `environmental.json` using values from DAS §4.2.                                                  |
| T12.3 | Silhouette outline primitive | *pending* | pending  | `src/outline.py` — scan alpha channel, draw 1-px black on exterior edges of building-only mask (exclude ground + decorations). Applied last, before composition to canvas. |
| T12.4 | Per-class outline policy     | *pending* | pending  | `OUTLINE_SILHOUETTE` constant; composer honors.                                                                                                                            |
| T12.5 | Palette + outline tests      | *pending* | pending  | `tests/test_palette_v2.py` + `tests/test_outline.py`.                                                                                                                      |


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
